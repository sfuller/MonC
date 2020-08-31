using System;
using System.Collections.Generic;
using System.IO;
using MonC.Parsing;
using MonC.SyntaxTree;

namespace MonC.LLVM
{
    using StackLayoutGenerator = MonC.Codegen.StackLayoutGenerator;
    using FunctionStackLayout = MonC.Codegen.FunctionStackLayout;

    /// <summary>
    /// Frequently referenced objects within the scope of CodeGenerator.Generate
    /// </summary>
    internal class CodeGeneratorContext
    {
        public Context Context { get; }
        public ParseModule ParseModule { get; }
        public Module Module { get; }
        public Metadata DiFile { get; }
        public Metadata DiModule { get; }
        public DIBuilder? DiBuilder => Module.DiBuilder;

        public CodeGeneratorContext(Context context, ParseModule parseModule, string fileName, string dirName,
            bool debugInfo)
        {
            Context = context;
            ParseModule = parseModule;

            Module = context.CreateModule(fileName, debugInfo);

            if (DiBuilder != null) {
                Module.AddModuleFlag(CAPI.LLVMModuleFlagBehavior.Warning, "Debug Info Version",
                    Context.DebugMetadataVersion);

                DiFile = DiBuilder.CreateFile(fileName, dirName);
                // Just say that MonC is C89; hopefully debuggers won't care
                Metadata diCompileUnit = DiBuilder.CreateCompileUnit(CAPI.DI.LLVMDWARFSourceLanguage.C89, DiFile,
                    "MonC", false, "", 0, "", CAPI.DI.LLVMDWARFEmissionKind.Full, 0, false, false, "", "");
                DiModule = DiBuilder.CreateModule(diCompileUnit, fileName, "", "", "");
            }
        }

        public Type LookupType(TypeSpecifierLeaf specifierLeaf)
        {
            Type? returnType = Context.LookupType(specifierLeaf.Name);
            if (returnType == null) {
                throw new InvalidOperationException($"undefined type '{specifierLeaf.Name}'");
            }

            Type useType = returnType.Value;
            if (specifierLeaf.PointerType != PointerType.NotAPointer) {
                useType = useType.PointerType();
            }

            return useType;
        }

        public Metadata LookupDiType(TypeSpecifierLeaf specifierLeaf)
        {
            if (DiBuilder == null) {
                throw new InvalidOperationException("LookupDiType called without a DiBuilder");
            }

            Metadata? returnType = DiBuilder.LookupType(specifierLeaf.Name);
            if (returnType == null) {
                throw new InvalidOperationException($"undefined type '{specifierLeaf.Name}'");
            }

            Metadata useType = returnType.Value;
            if (specifierLeaf.PointerType != PointerType.NotAPointer) {
                useType = DiBuilder.CreatePointerType(useType);
            }

            return useType;
        }

        public bool TryGetTokenSymbol(IASTLeaf leaf, out Symbol symbol) =>
            ParseModule.TokenMap.TryGetValue(leaf, out symbol);

        public class Function
        {
            public Type FunctionType { get; }
            public Value FunctionValue { get; }
            public BasicBlock? ReturnBlock { get; private set; }
            public Value? RetvalStorage { get; private set; }
            public Metadata DiFwdDecl { get; }
            public Metadata DiFunctionDef { get; private set; }
            private readonly FunctionDefinitionLeaf _leaf;

            public Function(CodeGeneratorContext genContext, FunctionDefinitionLeaf leaf)
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

                if (genContext.DiBuilder != null) {
                    genContext.TryGetTokenSymbol(leaf, out Symbol range);
                    DiFwdDecl = genContext.DiBuilder.CreateReplaceableCompositeType(
                        CAPI.DI.LLVMDWARFTag.subroutine_type, leaf.Name, genContext.DiFile, genContext.DiFile,
                        range.LLVMLine, 0, 0, 0, CAPI.DI.LLVMDIFlags.FwdDecl, "");
                }
            }

            public readonly Dictionary<DeclarationLeaf, Value>
                VariableValues = new Dictionary<DeclarationLeaf, Value>();

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
                _leaf.Accept(layoutGenerator);
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
                    DeclarationLeaf parameter = _leaf.Parameters[i];
                    Value paramValue = funcParams[i];
                    VariableValues.TryGetValue(parameter, out Value varStorage);
                    varStorage.SetName($"{parameter.Name}.addr");
                    builder.BuildStore(paramValue, varStorage);
                    paramValue.SetName(parameter.Name);
                }

                if (genContext.DiBuilder != null) {
                    // Create subroutine type debug info
                    genContext.TryGetTokenSymbol(_leaf, out Symbol range);
                    Metadata subroutineType = genContext.DiBuilder.CreateSubroutineType(genContext.DiFile,
                        Array.ConvertAll(_leaf.Parameters, param => genContext.LookupDiType(param.Type)),
                        CAPI.DI.LLVMDIFlags.Zero);
                    Metadata funcLocation = genContext.Context.CreateDebugLocation(range.LLVMLine, range.LLVMColumn,
                        DiFwdDecl, Metadata.Null);

                    // Create subroutine debug info and substitute over forward declaration
                    DiFunctionDef = genContext.DiBuilder.CreateFunction(genContext.DiFile, _leaf.Name, _leaf.Name,
                        genContext.DiFile, range.LLVMLine, subroutineType, true, true, range.LLVMLine,
                        CAPI.DI.LLVMDIFlags.Zero, false);
                    DiFwdDecl.ReplaceAllUsesWith(DiFunctionDef);

                    // Create llvm.dbg.declare calls for each parameter's storage
                    Metadata paramExpression = genContext.DiBuilder.CreateExpression();
                    for (int i = 0, ilen = _leaf.Parameters.Length; i < ilen; ++i) {
                        DeclarationLeaf parameter = _leaf.Parameters[i];
                        genContext.TryGetTokenSymbol(parameter, out Symbol paramRange);
                        Metadata paramType = genContext.LookupDiType(parameter.Type);
                        Metadata paramMetadata = genContext.DiBuilder.CreateParameterVariable(DiFunctionDef,
                            parameter.Name, (uint) i + 1, genContext.DiFile, paramRange.LLVMLine, paramType, true,
                            CAPI.DI.LLVMDIFlags.Zero);
                        VariableValues.TryGetValue(parameter, out Value varStorage);
                        genContext.DiBuilder.InsertDeclareAtEnd(varStorage, paramMetadata, paramExpression,
                            funcLocation, basicBlock);
                    }

                    // Associate debug info nodes with function
                    FunctionValue.SetFuncSubprogram(DiFunctionDef);
                }

                return basicBlock;
            }
        }

        public readonly Dictionary<string, Function> FunctionTable = new Dictionary<string, Function>();
        public readonly Dictionary<string, Function> DefinedFunctions = new Dictionary<string, Function>();
        public readonly Dictionary<string, Function> UndefinedFunctions = new Dictionary<string, Function>();

        public void RegisterDefinedFunction(FunctionDefinitionLeaf leaf)
        {
            if (DefinedFunctions.ContainsKey(leaf.Name)) {
                return;
            }

            Function function = new Function(this, leaf);
            DefinedFunctions.Add(leaf.Name, function);
            FunctionTable.Add(leaf.Name, function);
        }

        public Function GetFunctionDeclaration(FunctionDefinitionLeaf leaf)
        {
            if (!FunctionTable.TryGetValue(leaf.Name, out Function function)) {
                function = new Function(this, leaf);
                UndefinedFunctions.Add(leaf.Name, function);
                FunctionTable.Add(leaf.Name, function);
            }

            return function;
        }

        public Function GetFunctionDefinition(FunctionDefinitionLeaf leaf)
        {
            if (!DefinedFunctions.TryGetValue(leaf.Name, out Function function)) {
                throw new InvalidOperationException($"{leaf.Name} was not registered with RegisterDefinedFunction");
            }

            return function;
        }
    }

    public static class CodeGenerator
    {
        public static Module Generate(Context context, string path, ParseModule parseModule, bool debugInfo)
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
            CodeGeneratorContext genContext = new CodeGeneratorContext(context, parseModule, fileName, dirName,
                debugInfo);

            // Declaration pass
            foreach (FunctionDefinitionLeaf function in parseModule.Functions) {
                genContext.RegisterDefinedFunction(function);
            }

            // Definition pass
            foreach (FunctionDefinitionLeaf function in parseModule.Functions) {
                CodeGeneratorContext.Function ctxFunction = genContext.GetFunctionDefinition(function);
                CodeGenVisitor codeGenVisitor = new CodeGenVisitor(genContext, ctxFunction);
                function.Accept(codeGenVisitor);
            }

            // Finalize debug info
            genContext.DiBuilder?.BuilderFinalize();

            // Done with everything in CodeGeneratorContext besides the Module
            return genContext.Module;
        }
    }
}