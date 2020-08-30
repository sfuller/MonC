using System;
using System.Collections.Generic;
using System.IO;
using MonC.Parsing;
using MonC.SyntaxTree;

namespace MonC.LLVM
{
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
        public DIBuilder DiBuilder => Module.DiBuilder;

        public CodeGeneratorContext(Context context, ParseModule parseModule, string fileName, string dirName)
        {
            Context = context;
            ParseModule = parseModule;

            Module = context.CreateModule(fileName);
            DiFile = DiBuilder.CreateFile(fileName, dirName);
            // Just say that MonC is C89; hopefully debuggers won't care
            Metadata diCompileUnit = DiBuilder.CreateCompileUnit(CAPI.DI.LLVMDWARFSourceLanguage.C89, DiFile,
                "MonC", false, "", 0, "", CAPI.DI.LLVMDWARFEmissionKind.Full, 0, false, false, "", "");
            DiModule = DiBuilder.CreateModule(diCompileUnit, fileName, "", "", "");
        }

        public Type LookupType(TypeSpecifierLeaf specifierLeaf)
        {
            Type? returnType = Context.LookupType(specifierLeaf.Name);
            if (returnType == null)
                throw new InvalidOperationException($"undefined type '{specifierLeaf.Name}'");

            Type useType = returnType.Value;
            if (specifierLeaf.PointerType != PointerType.NotAPointer)
                useType = useType.PointerType();

            return useType;
        }

        public Metadata LookupDIType(TypeSpecifierLeaf specifierLeaf)
        {
            Metadata? returnType = DiBuilder.LookupType(specifierLeaf.Name);
            if (returnType == null)
                throw new InvalidOperationException($"undefined type '{specifierLeaf.Name}'");

            Metadata useType = returnType.Value;
            if (specifierLeaf.PointerType != PointerType.NotAPointer)
                useType = DiBuilder.CreatePointerType(useType);

            return useType;
        }

        public bool TryGetTokenSymbol(IASTLeaf leaf, out Symbol symbol) =>
            ParseModule.TokenMap.TryGetValue(leaf, out symbol);

        public class Function
        {
            public Value FunctionValue { get; }
            public Metadata DiFwdDecl { get; }
            public Metadata DiFunctionDef { get; private set; }
            private FunctionDefinitionLeaf _leaf;

            public Function(CodeGeneratorContext genContext, FunctionDefinitionLeaf leaf)
            {
                _leaf = leaf;

                Type[] paramTypes = Array.ConvertAll(leaf.Parameters, param => genContext.LookupType(param.Type));
                Type funcType =
                    genContext.Context.FunctionType(genContext.LookupType(leaf.ReturnType), paramTypes, false);
                FunctionValue = genContext.Module.AddFunction(leaf.Name, funcType);

                FunctionValue.SetLinkage(leaf.IsExported ? CAPI.LLVMLinkage.External : CAPI.LLVMLinkage.Internal);

                genContext.TryGetTokenSymbol(leaf, out Symbol range);
                DiFwdDecl = genContext.DiBuilder.CreateReplaceableCompositeType(
                    CAPI.DI.LLVMDWARFTag.subroutine_type, leaf.Name, genContext.DiFile, genContext.DiFile,
                    range.Start.Line, 0, 0, 0, CAPI.DI.LLVMDIFlags.FwdDecl, "");
            }

            /// <summary>
            /// Add entry basic block to this function and fill in additional debug info.
            /// If not called, this is a function declaration.
            /// </summary>
            /// <param name="genContext"></param>
            /// <returns>Entry basic block</returns>
            public BasicBlock StartDefinition(CodeGeneratorContext genContext)
            {
                BasicBlock basicBlock = genContext.Context.AppendBasicBlock(FunctionValue);

                genContext.TryGetTokenSymbol(_leaf, out Symbol range);
                Metadata subroutineType = genContext.DiBuilder.CreateSubroutineType(genContext.DiFile,
                    Array.ConvertAll(_leaf.Parameters, param => genContext.LookupDIType(param.Type)),
                    CAPI.DI.LLVMDIFlags.Zero);
                Metadata funcLocation = genContext.Context.CreateDebugLocation(range.Start.Line, range.Start.Column,
                    DiFwdDecl, Metadata.Null);

                DiFunctionDef = genContext.DiBuilder.CreateFunction(genContext.DiFile, _leaf.Name, _leaf.Name,
                    genContext.DiFile, range.Start.Line, subroutineType, true, true, range.Start.Line,
                    CAPI.DI.LLVMDIFlags.Zero, false);
                DiFwdDecl.ReplaceAllUsesWith(DiFunctionDef);

                Value[] funcParams = FunctionValue.Params;
                Metadata paramExpression = genContext.DiBuilder.CreateExpression();
                for (int i = 0, ilen = _leaf.Parameters.Length; i < ilen; ++i) {
                    DeclarationLeaf parameter = _leaf.Parameters[i];
                    funcParams[i].SetName(parameter.Name);
                    genContext.TryGetTokenSymbol(parameter, out Symbol paramRange);
                    Metadata paramType = genContext.LookupDIType(parameter.Type);
                    Metadata paramMetadata = genContext.DiBuilder.CreateParameterVariable(DiFunctionDef, parameter.Name,
                        (uint) i + 1, genContext.DiFile, paramRange.Start.Line, paramType, true,
                        CAPI.DI.LLVMDIFlags.Zero);
                    genContext.DiBuilder.InsertDbgValueAtEnd(funcParams[i], paramMetadata, paramExpression,
                        funcLocation, basicBlock);
                }

                FunctionValue.SetFuncSubprogram(DiFunctionDef);

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
        public static Module Generate(Context context, string path, ParseModule parseModule)
        {
            // Path information for debug info nodes
            string fileName = Path.GetFileName(path);
            string dirName = Path.GetDirectoryName(path);
            if (dirName == null)
                dirName = "/";
            else if (dirName.Length == 0)
                dirName = ".";

            // Create module and file-level debug info nodes
            CodeGeneratorContext genContext = new CodeGeneratorContext(context, parseModule, fileName, dirName);

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
            genContext.DiBuilder.BuilderFinalize();

            // Done with everything in CodeGeneratorContext besides the Module
            return genContext.Module;
        }
    }
}