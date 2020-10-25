using System;
using System.Collections.Generic;
using System.IO;
using MonC.Parsing;
using MonC.Semantics;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.TypeSystem.Types;
using MonC.TypeSystem.Types.Impl;

namespace MonC.LLVM
{
    using StackLayoutGenerator = Codegen.StackLayoutGenerator;
    using FunctionStackLayout = Codegen.FunctionStackLayout;
    using StructLayoutGenerator = Codegen.StructLayoutGenerator;
    using StructLayoutManager = Codegen.StructLayoutManager;
    using TypeSizeManager = Codegen.TypeSizeManager;

    /// <summary>
    /// Frequently referenced objects within the scope of CodeGenerator.Generate
    /// </summary>
    public class CodeGeneratorContext
    {
        public Context Context { get; }
        public SemanticModule SemanticModule { get; }
        public ParseModule ParseModule => SemanticModule.BaseModule;
        public SemanticContext SemanticContext { get; }
        public Module Module { get; }
        public Metadata DiFile { get; }
        public Metadata DiModule { get; }
        public DIBuilder DiBuilder => Module.DiBuilder;
        public bool DebugInfo { get; }
        public bool ColumnInfo { get; }
        public readonly StructLayoutManager StructLayoutManager = new StructLayoutManager();
        public TargetData TargetDataLayout { get; }

        public CodeGeneratorContext(Context context, SemanticModule semanticModule, SemanticContext semanticContext,
            string fileName, string dirName, string targetTriple, bool optimized, bool debugInfo, bool columnInfo)
        {
            Context = context;
            SemanticModule = semanticModule;
            SemanticContext = semanticContext;

            Module = context.CreateModule(fileName);
            Module.SetTarget(targetTriple);

            // Debug compile unit is always emitted even if debug info is not requested
            // This provides a standardized way to export enums in the module
            Module.AddModuleFlag(CAPI.LLVMModuleFlagBehavior.Warning, "Debug Info Version",
                Context.DebugMetadataVersion);
            if (targetTriple.Contains("-msvc")) {
                Module.AddModuleFlag(CAPI.LLVMModuleFlagBehavior.Warning, "CodeView",
                    Metadata.FromValue(Value.ConstInt(Context.Int32Type, 1, false)));
            }

            DiFile = DiBuilder.CreateFile(fileName, dirName);
            // Just say that MonC is C89; hopefully debuggers won't care
            Metadata diCompileUnit = DiBuilder.CreateCompileUnit(CAPI.LLVMDWARFSourceLanguage.C89, DiFile,
                "MonC", optimized, "", 0, "", CAPI.LLVMDWARFEmissionKind.Full, 0, false, false, "", "");
            DiModule = DiBuilder.CreateModule(diCompileUnit, fileName, "", "", "");

            DebugInfo = debugInfo;
            ColumnInfo = columnInfo && debugInfo;

            // IR-independent manager to generate struct type layouts
            StructLayoutManager.Setup(new StructLayoutGenerator());

            // Struct sizes need to be resolved for debug info (which is target-dependent)
            Target target = Target.FromTriple(targetTriple);
            TargetMachine machine = target.CreateTargetMachine(targetTriple, "", "",
                CAPI.LLVMCodeGenOptLevel.Default, CAPI.LLVMRelocMode.Default, CAPI.LLVMCodeModel.Default);
            TargetDataLayout = machine.CreateTargetDataLayout();
        }

        public void CreateStruct(StructNode structNode)
        {
            List<Type> memberTypes = structNode.Members.ConvertAll(d => LookupType(((TypeSpecifierNode) d.Type).Type));
            Type structType = Context.CreateStruct(structNode.Name, memberTypes.ToArray());

            ulong sizeInBits = TargetDataLayout!.SizeOfTypeInBits(structType);
            uint alignInBits = TargetDataLayout.PreferredAlignmentOfType(structType);
            List<Metadata> diMemberTypes =
                structNode.Members.ConvertAll(d => LookupDiType(((TypeSpecifierNode) d.Type).Type));
            // TODO: resolve declaration file and line
            DiBuilder.CreateStruct(structNode.Name, DiFile, 0, sizeInBits, alignInBits, diMemberTypes.ToArray());
        }

        public Type LookupType(IType type)
        {
            switch (type) {
                case IPointerType pointerType:
                    return LookupType(pointerType.DestinationType).PointerType();
                case IPrimitiveType primitiveType: {
                    Type? returnType = Context.LookupPrimitiveType(primitiveType.Primitive);
                    if (returnType == null) {
                        throw new InvalidOperationException($"undefined type '{primitiveType.Primitive}'");
                    }
                    return returnType.Value;
                }
                case StructType structType: {
                    Type? returnType = Context.LookupStructType(structType.Name);
                    if (returnType == null) {
                        throw new InvalidOperationException($"undefined type '{structType.Name}'");
                    }
                    return returnType.Value;
                }
                default:
                    throw new InvalidOperationException("unhandled IType");
            }
        }

        public Type LookupType(ITypeSpecifierNode typeSpecifier)
        {
            TypeSpecifierNode typeSpecifierNode = (TypeSpecifierNode) typeSpecifier;
            return LookupType(typeSpecifierNode.Type);
        }

        public Metadata LookupDiType(IType type)
        {
            switch (type) {
                case IPointerType pointerType:
                    return DiBuilder.CreatePointerType(LookupDiType(pointerType.DestinationType));
                case IPrimitiveType primitiveType: {
                    Metadata? returnType = DiBuilder.LookupPrimitiveType(primitiveType.Primitive);
                    if (returnType == null) {
                        throw new InvalidOperationException($"undefined type '{primitiveType.Primitive}'");
                    }
                    return returnType.Value;
                }
                case StructType structType: {
                    Metadata? returnType = DiBuilder.LookupStructType(structType.Name);
                    if (returnType == null) {
                        throw new InvalidOperationException($"undefined type '{structType.Name}'");
                    }
                    return returnType.Value;
                }
                default:
                    throw new InvalidOperationException("unhandled IType");
            }
        }

        public Metadata LookupDiType(ITypeSpecifierNode typeSpecifier)
        {
            TypeSpecifierNode typeSpecifierNode = (TypeSpecifierNode) typeSpecifier;
            return LookupDiType(typeSpecifierNode.Type);
        }

        public bool TryGetNodeSymbol(ISyntaxTreeNode leaf, out Symbol symbol) =>
            ParseModule.SymbolMap.TryGetValue(leaf, out symbol);

        public class Function
        {
            public Type FunctionType { get; }
            public Value FunctionValue { get; }
            public BasicBlock? ReturnBlock { get; private set; }
            public Value? RetvalStorage { get; private set; }
            public Metadata DiFwdDecl { get; }
            public Metadata DiFunctionDef { get; private set; }
            private readonly FunctionDefinitionNode _leaf;

            public Function(CodeGeneratorContext genContext, FunctionDefinitionNode leaf)
            {
                _leaf = leaf;

                // Create function type and add the function node without appending any basic blocks
                // This results in a function declaration in LLVM-IR
                Type[] paramTypes = Array.ConvertAll(leaf.Parameters, param => genContext.LookupType(param.Type));
                FunctionType =
                    genContext.Context.FunctionType(genContext.LookupType(leaf.ReturnType), paramTypes, false);
                FunctionValue = genContext.Module.AddFunction(leaf.Name, FunctionType);

                // Process the static keyword in the same manner as clang
                FunctionValue.SetLinkage(leaf.IsExported ? CAPI.LLVMLinkage.External : CAPI.LLVMLinkage.Internal);

                genContext.TryGetNodeSymbol(leaf, out Symbol range);
                DiFwdDecl = genContext.DiBuilder.CreateReplaceableCompositeType(
                    CAPI.LLVMDWARFTag.subroutine_type, leaf.Name, genContext.DiFile, genContext.DiFile,
                    range.LLVMLine, 0, 0, 0, CAPI.LLVMDIFlags.FwdDecl, "");
            }

            public readonly Dictionary<DeclarationNode, Value>
                VariableValues = new Dictionary<DeclarationNode, Value>();

            /// <summary>
            /// Add entry basic block to this function and fill in additional debug info.
            /// If not called, this is a function declaration.
            /// </summary>
            /// <param name="genContext"></param>
            /// <param name="builder"></param>
            /// <returns>Entry basic block</returns>
            public BasicBlock StartDefinition(CodeGeneratorContext genContext, Builder builder)
            {
                BasicBlock basicBlock = genContext.Context.AppendBasicBlock(FunctionValue, "entry");
                builder.PositionAtEnd(basicBlock);

                // Build return value storage if necessary
                Type returnType = FunctionType.ReturnType;
                if (returnType.Kind != CAPI.LLVMTypeKind.Void) {
                    ReturnBlock = genContext.Context.CreateBasicBlock("return");
                    RetvalStorage = builder.BuildAlloca(returnType, "retval");
                }

                // Visit all declaration nodes and create storage (parameters and variables)
                StackLayoutGenerator layoutGenerator = new StackLayoutGenerator();
                layoutGenerator.VisitFunctionDefinition(_leaf);
                FunctionStackLayout layout = layoutGenerator.GetLayout();
                foreach (var v in layout.Variables) {
                    Type varType = genContext.LookupType(v.Key.Type);
                    Value varStorage = builder.BuildAlloca(varType, v.Key.Name);
                    VariableValues[v.Key] = varStorage;
                }

                // Emit store instruction for return value
                if (RetvalStorage != null) {
                    builder.BuildStore(Value.ConstInt(returnType, 0, true), RetvalStorage.Value);
                }

                // Emit store instructions for parameters
                Value[] funcParams = FunctionValue.Params;
                for (int i = 0, ilen = funcParams.Length; i < ilen; ++i) {
                    DeclarationNode parameter = _leaf.Parameters[i];
                    Value paramValue = funcParams[i];
                    VariableValues.TryGetValue(parameter, out Value varStorage);
                    varStorage.SetName($"{parameter.Name}.addr");
                    builder.BuildStore(paramValue, varStorage);
                    paramValue.SetName(parameter.Name);
                }

                // Create subroutine type debug info
                genContext.TryGetNodeSymbol(_leaf, out Symbol range);
                Metadata subroutineType = genContext.DiBuilder.CreateSubroutineType(genContext.DiFile,
                    Array.ConvertAll(_leaf.Parameters, param => genContext.LookupDiType(param.Type)),
                    CAPI.LLVMDIFlags.Zero);
                Metadata funcLocation = genContext.Context.CreateDebugLocation(range.LLVMLine,
                    genContext.ColumnInfo ? range.LLVMColumn : 0, DiFwdDecl, Metadata.Null);

                // Create subroutine debug info and substitute over forward declaration
                DiFunctionDef = genContext.DiBuilder.CreateFunction(genContext.DiFile, _leaf.Name, _leaf.Name,
                    genContext.DiFile, range.LLVMLine, subroutineType, true, true, range.LLVMLine,
                    CAPI.LLVMDIFlags.Zero, false);
                DiFwdDecl.ReplaceAllUsesWith(DiFunctionDef);

                // Create llvm.dbg.declare calls for each parameter's storage
                if (genContext.DebugInfo) {
                    Metadata paramExpression = genContext.DiBuilder.CreateExpression();
                    for (int i = 0, ilen = _leaf.Parameters.Length; i < ilen; ++i) {
                        DeclarationNode parameter = _leaf.Parameters[i];
                        genContext.TryGetNodeSymbol(parameter, out Symbol paramRange);
                        Metadata paramType = genContext.LookupDiType(parameter.Type);
                        Metadata paramMetadata = genContext.DiBuilder.CreateParameterVariable(DiFunctionDef,
                            parameter.Name, (uint) i + 1, genContext.DiFile, paramRange.LLVMLine, paramType, true,
                            CAPI.LLVMDIFlags.Zero);
                        VariableValues.TryGetValue(parameter, out Value varStorage);
                        genContext.DiBuilder.InsertDeclareAtEnd(varStorage, paramMetadata, paramExpression,
                            funcLocation, basicBlock);
                    }
                }

                // Associate debug info nodes with function
                FunctionValue.SetFuncSubprogram(DiFunctionDef);

                return basicBlock;
            }

            public void FinalizeFunction()
            {
                if (DiFwdDecl.IsValid && !DiFunctionDef.IsValid) {
                    DiFwdDecl.DisposeTemporaryMDNode();
                }
            }
        }

        public readonly Dictionary<string, Function> FunctionTable = new Dictionary<string, Function>();
        public readonly Dictionary<string, Function> DefinedFunctions = new Dictionary<string, Function>();
        public readonly Dictionary<string, Function> UndefinedFunctions = new Dictionary<string, Function>();

        public void RegisterDefinedFunction(FunctionDefinitionNode leaf)
        {
            if (DefinedFunctions.ContainsKey(leaf.Name)) {
                return;
            }

            Function function = new Function(this, leaf);
            DefinedFunctions.Add(leaf.Name, function);
            FunctionTable.Add(leaf.Name, function);
        }

        public Function GetFunctionDeclaration(FunctionDefinitionNode leaf)
        {
            if (!FunctionTable.TryGetValue(leaf.Name, out Function function)) {
                function = new Function(this, leaf);
                UndefinedFunctions.Add(leaf.Name, function);
                FunctionTable.Add(leaf.Name, function);
            }

            return function;
        }

        public Function GetFunctionDefinition(FunctionDefinitionNode leaf)
        {
            if (!DefinedFunctions.TryGetValue(leaf.Name, out Function function)) {
                throw new InvalidOperationException($"{leaf.Name} was not registered with RegisterDefinedFunction");
            }

            return function;
        }

        public void FinalizeFunctions()
        {
            foreach (var function in FunctionTable) {
                function.Value.FinalizeFunction();
            }
        }
    }

    public static class CodeGenerator
    {
        public static Module Generate(Context context, string path, SemanticModule module,
            SemanticContext semanticContext, string targetTriple, PassManagerBuilder? optBuilder, bool debugInfo,
            bool columnInfo)
        {
            // Path information for debug info nodes
            string fileName = Path.GetFileName(path);
            string dirName = Path.GetDirectoryName(path);
            if (dirName == null) {
                dirName = "/";
            } else if (dirName.Length == 0) {
                dirName = ".";
            }

            // Create module and file-level debug info nodes
            CodeGeneratorContext genContext = new CodeGeneratorContext(context, module, semanticContext, fileName,
                dirName, targetTriple, optBuilder != null, debugInfo, columnInfo);

            // Enum pass
            foreach (EnumNode enumNode in genContext.ParseModule.Enums) {
                Metadata[] enumerators = new Metadata[enumNode.Declarations.Count];
                for (int i = 0, ilen = enumNode.Declarations.Count; i < ilen; ++i) {
                    var enumeration = enumNode.Declarations[i];
                    enumerators[i] = genContext.DiBuilder.CreateEnumerator(enumeration.Name, i, false);
                }

                genContext.TryGetNodeSymbol(enumNode, out Symbol range);
                genContext.DiBuilder.CreateEnumerationType(genContext.DiFile,
                    (enumNode.IsExported ? "export." : "") + $"enum.{fileName}.{range.LLVMLine}", genContext.DiFile,
                    range.LLVMLine, genContext.DiBuilder.Int32Type.GetTypeSizeInBits(),
                    genContext.DiBuilder.Int32Type.GetTypeAlignInBits(), enumerators, genContext.DiBuilder.Int32Type);
            }

            // Struct pass
            foreach (StructNode structNode in genContext.ParseModule.Structs) {
                genContext.CreateStruct(structNode);
            }

            // Declaration pass
            foreach (FunctionDefinitionNode function in genContext.ParseModule.Functions) {
                genContext.RegisterDefinedFunction(function);
            }

            // Definition pass
            foreach (FunctionDefinitionNode function in genContext.ParseModule.Functions) {
                CodeGeneratorContext.Function ctxFunction = genContext.GetFunctionDefinition(function);

                using Builder builder = genContext.Context.CreateBuilder();
                FunctionCodeGenVisitor functionCodeGenVisitor = new FunctionCodeGenVisitor(genContext, ctxFunction,
                    builder, ctxFunction.StartDefinition(genContext, builder));
                builder.PositionAtEnd(functionCodeGenVisitor._basicBlock);
                function.Body.VisitStatements(functionCodeGenVisitor);

                if (genContext.DebugInfo) {
                    // TODO: Use the body end rather than the function end
                    genContext.TryGetNodeSymbol(function, out Symbol range);
                    Metadata location = genContext.Context.CreateDebugLocation(range.End.Line + 1,
                        genContext.ColumnInfo ? range.End.Column + 1 : 0, ctxFunction.DiFunctionDef,
                        Metadata.Null);
                    builder.SetCurrentDebugLocation(location);
                }

                // If we still have a valid insert block, this function did not end with a return; Insert one now
                if (builder.InsertBlock.IsValid) {
                    if (ctxFunction.ReturnBlock != null) {
                        builder.BuildBr(ctxFunction.ReturnBlock.Value);
                    } else {
                        builder.BuildRetVoid();
                    }
                }

                if (ctxFunction.ReturnBlock != null && ctxFunction.RetvalStorage != null) {
                    ctxFunction.FunctionValue.AppendExistingBasicBlock(ctxFunction.ReturnBlock.Value);
                    builder.PositionAtEnd(ctxFunction.ReturnBlock.Value);
                    Value retVal = builder.BuildLoad(ctxFunction.FunctionType.ReturnType,
                        ctxFunction.RetvalStorage.Value);
                    builder.BuildRet(retVal);
                }
            }

            // Remove unused metadata nodes for undefined functions
            genContext.FinalizeFunctions();

            // Finalize debug info
            genContext.DiBuilder.BuilderFinalize();

            // Run optimization passes on functions and module if a builder is supplied
            if (optBuilder != null) {
                using ModulePassManager modulePassManager = new ModulePassManager(optBuilder);
                using FunctionPassManager functionPassManager = new FunctionPassManager(genContext.Module, optBuilder);

                functionPassManager.Initialize();
                foreach (var function in genContext.DefinedFunctions) {
                    functionPassManager.Run(function.Value.FunctionValue);
                }

                functionPassManager.FinalizeFunctionPassManager();

                modulePassManager.Run(genContext.Module);
            }

            // Done with everything in CodeGeneratorContext besides the Module
            return genContext.Module;
        }
    }
}
