using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MonC;
using MonC.DotNetInterop;
using MonC.Parsing;

namespace Driver
{
    using ToolChainSelection = KeyValuePair<string, Type>;

    [CommandLineCategory]
    public class Job : IDisposable, ILinkInput
    {
        [CommandLine("-i", "Enter MonC source code with an interactive prompt")]
        private bool _interactive = false;

        private List<FileInfo> _inputFiles;

        [CommandLine("-o", "Path of output file", "path")]
        private string _outputPath = null;

        private FileInfo _outputFile;

        [CommandLine("-toolchain", "Toolchain to use after parsing phase", "monc|llvm")]
        private string _requestedToolChain = null;

        private ToolChainSelection _toolChainSelection;
        private ToolChain _toolChain;

        [CommandLine("-c", "Output module as relocatable object")]
        private bool _reloc = false;

        [CommandLine("-S", "Output module as instruction listing")]
        private bool _asm = false;

        [CommandLine("-vm", "Execute linked code in a virtual machine rather than outputting to a file")]
        private bool _vm = false;

        [CommandLine("-debugger", "Run virtual machine with an interactive debugger")]
        internal bool _debugger = false;

        private Phase _targetPhase;

        private List<IModuleTool> _moduleFileTools;

        [CommandLine("-l", "Add library module to link list", "name")]
        private List<string> _libraryNames = new List<string>();

        internal InteropResolver InteropResolver;

        internal ParseModule InteropHeaderModule;

        [CommandLine("-a", "Add argument to pass to VM invocation", "int")]
        internal List<int> _argsToPass = new List<int>();

        [CommandLine("-showtools", "List tools that will be invoked for each file")]
        private bool _showTools = false;

        private List<IModuleArtifact> _moduleArtifacts;

        private IExecutableTool _executableTool;

        private ToolChainSelection SelectToolChain()
        {
            // See if user requested a specific toolchain via -toolchain= or output file extension
            ToolChainSelection requestedToolChainType = new ToolChainSelection();
            if (_requestedToolChain != null) {
                if (!ToolChain.TryGetToolchain(_requestedToolChain, out Type requestedType)) {
                    throw Diagnostics.ThrowError(
                        $"{_requestedToolChain} is not a supported toolchain",
                        writer => {
                            writer.WriteLine("Accepted toolchains:");
                            ToolChain.WriteOutToolChains(writer);
                        });
                }

                requestedToolChainType = new ToolChainSelection(_requestedToolChain, requestedType);
            }

            // When an explicit toolchain is not selected, build a closed set of incompatible toolchains
            HashSet<string> closedToolChains = new HashSet<string>(ToolChain.ToolChains.Length);

            void CheckFileToolChainCompatibilty(FileInfo fileInfo)
            {
                if (requestedToolChainType.Value != null) {
                    // Ensure file is compatible with user-requested toolchain
                    if (!fileInfo.IsCompatibleWithToolchainType(requestedToolChainType.Value)) {
                        Diagnostics.Report(Diagnostics.Severity.Error,
                            $"{fileInfo.OriginalPath} is not compatible with {requestedToolChainType.Key}");
                    }
                } else {
                    // Add incompatible toolchains to closed set
                    foreach (var pair in ToolChain.ToolChains) {
                        if (!fileInfo.IsCompatibleWithToolchainType(pair.Value)) {
                            closedToolChains.Add(pair.Key);
                        }
                    }
                }
            }

            // If specified, eliminate incompatible toolchains based on output file extension
            if (_outputFile != null && _outputFile.Kind != FileType.Kind.UNCLASSIFIED) {
                CheckFileToolChainCompatibilty(_outputFile);
            }

            // Ensure each input file requires a consistent toolchain
            foreach (FileInfo inputFile in _inputFiles) {
                CheckFileToolChainCompatibilty(inputFile);
            }

            Diagnostics.ThrowIfErrors();

            // Toolchain set as user request
            if (requestedToolChainType.Value != null) {
                return requestedToolChainType;
            }

            // Otherwise take first non-closed toolchain
            foreach (var pair in ToolChain.ToolChains) {
                if (closedToolChains.Contains(pair.Key))
                    continue;

                return pair;
            }

            throw Diagnostics.ThrowError("unable to select a mutually compatible toolchain with provided files");
        }

        private Phase SelectTargetPhase()
        {
            // Always target VM if requested
            if (_vm)
                return Phase.VM;

            Phase producingPhase = _outputFile?.ProducingPhase ?? Phase.Null;

            // Always target toolchain's preferred reloc target phase if requested
            if (_reloc)
                return _toolChain.SelectRelocTargetPhase(producingPhase);

            // If we can unambiguously determine the final phase based on file extension, do that
            if (producingPhase != Phase.Null)
                return producingPhase;

            // Otherwise assume a linked artifact for output
            return Phase.Link;
        }

        public Job(CommandLine commandLine)
        {
            commandLine.ApplyTo(this);

            // Build input file list
            _inputFiles = new List<FileInfo>(commandLine.PositionalArguments.Count + (_interactive ? 1 : 0));
            foreach (string filePath in commandLine.PositionalArguments) {
                FileInfo tmpFile = new FileInfo(filePath);
                if (!tmpFile.Exists) {
                    Diagnostics.Report(Diagnostics.Severity.Error, $"{filePath} does not exist");
                } else if (tmpFile.Kind == FileType.Kind.UNCLASSIFIED) {
                    Diagnostics.Report(Diagnostics.Severity.Error, $"{filePath} is of unknown type");
                } else if (tmpFile.Kind == FileType.Kind.TARGET_ASSEMBLY ||
                           tmpFile.Kind == FileType.Kind.TARGET_OBJECT) {
                    Diagnostics.Report(Diagnostics.Severity.Error,
                        "MonC cannot load target assembly or object files. External tools must be used for these.");
                } else {
                    _inputFiles.Add(tmpFile);
                }
            }

            // Additional interactive input file if requested
            if (_interactive) {
                _inputFiles.Add(FileInfo.Interactive);
            }

            Diagnostics.ThrowIfErrors();

            if (_asm) {
                // Relocatable processing is implied for assembly output
                _reloc = true;
            }

            if (_reloc && _inputFiles.Count > 1) {
                throw Diagnostics.ThrowError("-c or -S flag may only be set for one input file");
            }

            // VM is implied for debugger
            if (_debugger) {
                _vm = true;
            }

            // Set output file if provided and not using VM
            _outputFile = _outputPath != null && !_vm ? new FileInfo(_outputPath) : null;

            // Assembly output defaults to stdout if not specified
            if (_asm && _outputFile == null) {
                _outputFile = FileInfo.StdIo;
            }

            // Select toolchain by user request or input files / output file
            _toolChainSelection = SelectToolChain();

            // Construct toolchain
            _toolChain = (ToolChain) Activator.CreateInstance(_toolChainSelection.Value);
            commandLine.ApplyTo(_toolChain);

            // Determine target phase based on output file (if provided) and command line flags
            _targetPhase = SelectTargetPhase();

            // Initialize module phase open set with pre-link phases up to target
            PhaseSet modulePhaseOpenSet =
                _toolChain.FilterPhases(
                    PhaseSet.AllPhasesTo((Phase) Math.Min((int) _targetPhase, (int) Phase.Link - 1)));

            // Visit JobActions for each file; building a chain of tools
            _moduleFileTools = new List<IModuleTool>(_inputFiles.Count);
            foreach (FileInfo inputFile in _inputFiles) {
                ITool tool = null;
                foreach (Phase phase in inputFile.PossiblePhases & modulePhaseOpenSet) {
                    tool = IJobAction.FromPhase(phase)
                        .Accept(_toolChain, this, tool != null ? (IInput) tool : inputFile);
                    commandLine.ApplyTo(tool);
                }

                if (tool is IModuleTool moduleTool)
                    _moduleFileTools.Add(moduleTool);
                else
                    throw new InvalidOperationException("toolchain did not produce a module input file tool");
            }

            // Set up link and subsequent tools
            if (_targetPhase >= Phase.Link) {
                PhaseSet linkPhaseOpenSet =
                    _toolChain.FilterPhases(PhaseSet.AllPhasesTo(_targetPhase) & PhaseSet.AllPhasesFrom(Phase.Link));

                ITool tool = null;
                foreach (Phase phase in linkPhaseOpenSet) {
                    tool = IJobAction.FromPhase(phase)
                        .Accept(_toolChain, this, tool != null ? (IInput) tool : this);
                    commandLine.ApplyTo(tool);
                }

                if (tool is IExecutableTool executableTool)
                    _executableTool = executableTool;
                else
                    throw new InvalidOperationException("toolchain did not produce an executable tool");
            }
        }

        private void ResolveInteropLibs()
        {
            InteropResolver = new InteropResolver();

            foreach (string libraryName in _libraryNames) {
                Assembly lib = Assembly.LoadFile(Path.GetFullPath(libraryName));
                InteropResolver.ImportAssembly(lib,
                    BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static);
            }

            InteropHeaderModule = InteropResolver.CreateHeaderModule();
        }

        public int Execute()
        {
            if (_moduleFileTools.Count == 0) {
                Diagnostics.Report(Diagnostics.Severity.Error, "No valid input files");
                return 1;
            }

            if (_showTools) {
                for (int i = 0, iend = _inputFiles.Count; i < iend; ++i) {
                    Diagnostics.Report(Diagnostics.Severity.Info, $"{_inputFiles[i].OriginalPath}:",
                        _moduleFileTools[i].WriteInputChain);
                }

                if (_executableTool != null) {
                    Diagnostics.Report(Diagnostics.Severity.Info, "Link:", _executableTool.WriteInputChain);
                }
            }

            // This only applies to LLVM which has an unmanaged context
            _toolChain.Initialize();

            ResolveInteropLibs();

            // Produce per-module artifacts
            _moduleArtifacts = _moduleFileTools.ConvertAll(tool => tool.GetModuleArtifact());

            // Output assembly if requested
            if (_asm) {
                _moduleArtifacts[0].WriteListing(_outputFile.GetTextWriter());
            }

            // Run link-phase and beyond tools
            int ret = _executableTool?.Execute() ?? 0;

            // Dispose per-module artifacts
            _moduleArtifacts.ForEach(artifact => artifact.Dispose());

            return ret;
        }

        public void WriteInputChain(TextWriter writer)
        {
            writer.WriteLine("  -Module Job Tools");
        }

        public List<IModuleArtifact> GetModuleArtifacts() => _moduleArtifacts;

        public void Dispose()
        {
            // This only applies to LLVM which has an unmanaged context
            _toolChain.Dispose();
        }
    }
}
