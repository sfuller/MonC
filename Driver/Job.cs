using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MonC.DotNetInterop;
using MonC.Parsing;

namespace Driver
{
    using ToolChainSelection = KeyValuePair<string, Type>;

    [CommandLineCategory]
    public class Job
    {
        private List<FileInfo> _inputFiles;

        [CommandLine("-output", "Path of output file", "path")]
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

        private Phase _targetPhase;

        private List<IExecutableTool> _inputFileTools;

        [CommandLine("-l", "Add library module to link list", "name")]
        private List<string> _libraryNames = new List<string>();

        internal ParseModule InteropHeaderModule;

        [CommandLine("-showtools", "List tools that will be invoked for each file")]
        private bool _showTools = false;

        private ToolChainSelection SelectToolChain()
        {
            // See if user requested a specific toolchain via -toolchain= or output file extension
            ToolChainSelection requestedToolChainType = new ToolChainSelection();
            if (_requestedToolChain != null) {
                if (!ToolChain.TryGetToolchain(_requestedToolChain, out Type requestedType)) {
                    throw Diagnostics.ThrowError(
                        $"{_requestedToolChain} is not a supported toolchain",
                        writer =>
                        {
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
            _inputFiles = new List<FileInfo>(commandLine.PositionalArguments.Count);
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

            Diagnostics.ThrowIfErrors();

            if (_asm) {
                // Relocatable processing is implied for assembly output
                _reloc = true;
            }

            if (_reloc && _inputFiles.Count > 1) {
                throw Diagnostics.ThrowError("-c or -S flag may only be set for one input file");
            }

            // Set output file if provided and not using VM
            _outputFile = _outputPath != null && !_vm ? new FileInfo(_outputPath) : null;

            // Select toolchain by user request or input files / output file
            _toolChainSelection = SelectToolChain();

            // Construct toolchain
            _toolChain = (ToolChain) Activator.CreateInstance(_toolChainSelection.Value);
            commandLine.ApplyTo(_toolChain);

            // Determine target phase based on output file (if provided) and command line flags
            _targetPhase = SelectTargetPhase();
            
            // Initialize open input phase set with pre-link phases up to target
            PhaseSet openInputPhaseSet =
                _toolChain.FilterPhases(
                    PhaseSet.AllPhasesTo((Phase) Math.Min((int) _targetPhase, (int) Phase.Backend)));

            // Visit JobActions for each file; building a chain of tools
            _inputFileTools = new List<IExecutableTool>(_inputFiles.Count);
            foreach (FileInfo inputFile in _inputFiles) {
                ITool tool = null;
                foreach (Phase phase in inputFile.PossiblePhases & openInputPhaseSet) {
                    tool = IJobAction.FromPhase(phase)
                        .Accept(_toolChain, this, tool != null ? (IInput) tool : inputFile);
                    commandLine.ApplyTo(tool);
                }

                if (tool is IExecutableTool executableTool)
                    _inputFileTools.Add(executableTool);
                else
                    throw new InvalidOperationException("toolchain did not produce an executable input file tool");
            }
        }

        private void ResolveInteropLibs()
        {
            InteropResolver interopResolver = new InteropResolver();

            foreach (string libraryName in _libraryNames) {
                Assembly lib = Assembly.LoadFile(Path.GetFullPath(libraryName));
                interopResolver.ImportAssembly(lib,
                    BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static);
            }

            InteropHeaderModule = interopResolver.CreateHeaderModule();
        }

        public void Execute()
        {
            if (_showTools) {
                for (int i = 0, iend = _inputFiles.Count; i < iend; ++i) {
                    Diagnostics.Report(Diagnostics.Severity.Info,
                        $"The following tools will be ran on {_inputFiles[i].OriginalPath}:",
                        _inputFileTools[i].WriteInputChain);
                }
            }

            ResolveInteropLibs();

            foreach (IExecutableTool tool in _inputFileTools) {
                tool.Execute();
            }
        }
    }
}