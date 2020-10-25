using System;
using MonC.LLVM;
using MonC.Parsing;
using MonC.Semantics;

namespace Driver.ToolChains
{
    [CommandLineCategory]
    public class LLVM : ToolChain
    {
        [CommandLine("-g", "Include debug information in module")]
        private bool _debugInfo = false;

        [CommandLine("-gcolumn-info", "Include column debug information in module")]
        private bool _debugColumnInfo = false;

        [CommandLine("-native", "Output native relocatable, assembly, or linked executable")]
        private bool _native = false;

        [CommandLine("-target", "Specify output target triple", "target")]
        private string _targetTriple = String.Empty;

        private Target _target;
        internal TargetMachine TargetMachine { get; private set; }

        [CommandLine("-O0", "Optimization level 0")]
        private bool _O0 = false;

        [CommandLine("-O1", "Optimization level 1")]
        private bool _O1 = false;

        [CommandLine("-O2", "Optimization level 2")]
        private bool _O2 = false;

        [CommandLine("-Os", "Optimization level 2 minimum size priority")]
        private bool _Os = false;

        [CommandLine("-Oz", "Optimization level 2 extra minimum size priority")]
        private bool _Oz = false;

        [CommandLine("-O3", "Optimization level 3")]
        private bool _O3 = false;

        private Context _context;
        private PassManagerBuilder _optBuilder;
        private CAPI.LLVMCodeGenOptLevel _optLevel;

        public override Phase SelectRelocTargetPhase(Phase outputFilePhase)
        {
            if (_native)
                return Phase.Backend;

            if (outputFilePhase == Phase.Backend) {
                // Implied native if output file extension calls for it
                _native = true;
                return Phase.Backend;
            }

            return Phase.CodeGen;
        }

        public override void Initialize()
        {
            _context ??= new Context();

            if (_targetTriple.Length == 0) {
                _targetTriple = Target.DefaultTargetTriple;
            }
            _targetTriple = Target.NormalizeTargetTriple(_targetTriple);

            _optBuilder ??= _O0 || _O1 || _O2 || _Os || _Oz || _O3 ? new PassManagerBuilder() : null;
            _optLevel = CAPI.LLVMCodeGenOptLevel.None;
            if (_optBuilder != null) {
                if (_O0) {
                    _optBuilder.SetOptLevels(0, 0);
                    _optLevel = CAPI.LLVMCodeGenOptLevel.None;
                } else if (_O1) {
                    _optBuilder.SetOptLevels(1, 0);
                    _optLevel = CAPI.LLVMCodeGenOptLevel.Less;
                } else if (_O2) {
                    _optBuilder.SetOptLevels(2, 0);
                    _optLevel = CAPI.LLVMCodeGenOptLevel.Default;
                } else if (_Os) {
                    _optBuilder.SetOptLevels(2, 1);
                    _optLevel = CAPI.LLVMCodeGenOptLevel.Default;
                } else if (_Oz) {
                    _optBuilder.SetOptLevels(2, 2);
                    _optLevel = CAPI.LLVMCodeGenOptLevel.Default;
                } else if (_O3) {
                    _optBuilder.SetOptLevels(3, 0);
                    _optLevel = CAPI.LLVMCodeGenOptLevel.Aggressive;
                }
            }

            Target.InitializeAllTargets();

            if (_native) {
                _target = Target.FromTriple(_targetTriple);
                TargetMachine = _target.CreateTargetMachine(_targetTriple, "", "", _optLevel,
                    CAPI.LLVMRelocMode.Default, CAPI.LLVMCodeModel.Default);
                TargetMachine.SetAsmVerbosity(true);
            }
        }

        public override void Dispose()
        {
            _context?.Dispose();
            _optBuilder?.Dispose();
            TargetMachine?.Dispose();
        }

        private static readonly PhaseSet PossiblePhases = ~new PhaseSet(Phase.Backend);

        public override PhaseSet FilterPhases(PhaseSet phases)
        {
            if (!_native)
                return phases & ~new PhaseSet(Phase.Backend);
            return phases;
        }

        public override IModuleTool BuildCodeGenJobTool(Job job, ICodeGenInput input) =>
            LLVMCodeGenTool.Construct(job, this, input);

        public override IModuleTool BuildBackendJobTool(Job job, IBackendInput input) =>
            LLVMBackendTool.Construct(job, this, input);

        public override IModuleTool BuildLinkerInputFileTool(Job job, FileInfo fileInfo) =>
            LLVMLinkerInputFileTool.Construct(job, this, fileInfo);

        public override IExecutableTool BuildLinkJobTool(Job job, ILinkInput input) =>
            LLVMLinkTool.Construct(job, this, input);

        public override IExecutableTool BuildVMJobTool(Job job, IVMInput input) =>
            LLVMVMTool.Construct(job, this, input);

        internal Module CreateModule(FileInfo fileInfo, SemanticModule semanticModule,
            SemanticContext semanticContext) =>
            CodeGenerator.Generate(_context, fileInfo.FullPath, semanticModule, semanticContext, _targetTriple,
                _optBuilder, _debugInfo, _debugColumnInfo);

        internal Module ParseIR(FileInfo fileInfo)
        {
            using MemoryBuffer memBuf = MemoryBuffer.WithContentsOfFile(fileInfo.FullPath);
            return _context.ParseIR(memBuf);
        }

        internal GenericValue CreateIntGenericValue(int val) =>
            GenericValue.FromInt(_context.Int32Type, (ulong) val, true);
    }
}
