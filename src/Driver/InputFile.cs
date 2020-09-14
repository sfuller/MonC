using System;
using System.IO;

namespace Driver
{
    public sealed class FileInfo : ILexInput
    {
        public System.IO.FileInfo SystemFileInfo { get; }

        public string FullPath {
            get {
                if (IsInteractive)
                    return "<interactive>";
                if (IsStdIo)
                    return "<stdin>";
                return SystemFileInfo.FullName;
            }
        }

        public string OriginalPath => SystemFileInfo.ToString();
        public bool IsStdIo => OriginalPath == "-";
        public bool IsInteractive => OriginalPath == "<interactive>";
        public bool Exists => IsStdIo || IsInteractive || SystemFileInfo.Exists;

        public FileType.Kind Kind { get; }

        public FileInfo(string path)
        {
            SystemFileInfo = new System.IO.FileInfo(path);

            // TODO: Support other types via stdin (-x flag?)
            if (IsStdIo || IsInteractive) {
                Kind = FileType.Kind.MONC_SOURCE;
                return;
            }

            // Just using file-extension heuristic for now
            // Deep file inspection could also be used
            switch (SystemFileInfo.Extension.ToLower()) {
                default:
                    Kind = FileType.Kind.UNCLASSIFIED;
                    break;
                case ".monc":
                    Kind = FileType.Kind.MONC_SOURCE;
                    break;
                case ".moncil":
                    Kind = FileType.Kind.MONC_IL;
                    break;
                case ".ll":
                    Kind = FileType.Kind.LLVM_ASSEMBLY;
                    break;
                case ".bc":
                    Kind = FileType.Kind.LLVM_BITCODE;
                    break;
                case ".s":
                case ".asm":
                    Kind = FileType.Kind.TARGET_ASSEMBLY;
                    break;
                case ".o":
                case ".obj":
                    Kind = FileType.Kind.TARGET_OBJECT;
                    break;
            }
        }

        public static FileInfo Interactive => new FileInfo("<interactive>");

        public static FileInfo StdIo => new FileInfo("-");

        public PhaseSet PossiblePhases => FileType.GetPossiblePhases(Kind);

        public Phase ProducingPhase => FileType.GetProducingPhase(Kind);

        public bool IsAssembly => FileType.IsAssembly(Kind);

        public bool IsLinkerInput => FileType.IsLinkerInput(Kind);

        public bool IsCompatibleWithToolchainType(Type toolChainType) =>
            FileType.IsCompatibleWithToolchainType(Kind, toolChainType);

        public TextReader GetTextReader()
        {
            try {
                return IsStdIo ? Console.In : new StreamReader(FullPath);
            } catch (Exception ex) {
                throw Diagnostics.ThrowError($"{ex.GetType()} exception while opening StreamReader: {ex.Message}");
            }
        }

        public TextWriter GetTextWriter()
        {
            try {
                return IsStdIo ? Console.Out : new StreamWriter(FullPath);
            } catch (Exception ex) {
                throw Diagnostics.ThrowError($"{ex.GetType()} exception while opening StreamWriter: {ex.Message}");
            }
        }

        public BinaryWriter GetBinaryWriter()
        {
            try {
                return new BinaryWriter(IsStdIo ? Console.OpenStandardOutput() : File.Open(FullPath, FileMode.Create));
            } catch (Exception ex) {
                throw Diagnostics.ThrowError($"{ex.GetType()} exception while opening BinaryWriter: {ex.Message}");
            }
        }

        public void WriteInputChain(TextWriter writer)
        {
            if (IsInteractive)
                writer.WriteLine("  -Read Interactive Input");
            else if (IsStdIo)
                writer.WriteLine("  -Read Standard Input");
            else
                writer.WriteLine($"  -Read {FullPath}");
        }
    }
}
