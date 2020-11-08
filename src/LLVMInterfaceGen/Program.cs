using System;
using System.IO;

namespace LLVMInterfaceGen
{
    class Program
    {
        // Keep in sync with LLVM_ALL_TARGETS in LLVM's root CMakeLists.txt
        private static readonly string[] Targets = {
            "AArch64",
            "AMDGPU",
            "ARM",
            "AVR",
            "BPF",
            "Hexagon",
            "Lanai",
            "Mips",
            "MSP430",
            "NVPTX",
            "PowerPC",
            "RISCV",
            "Sparc",
            "SystemZ",
            "WebAssembly",
            "X86",
            "XCore"
        };

        static void Main(string[] args)
        {
            if (args.Length == 0)
                throw new InvalidOperationException("Must pass source path to MonC project");

            string pathOut = Path.Combine(args[0], "LLVM", "API", "CAPIGEN.cs");
            using StreamWriter writer = new StreamWriter(pathOut);

            void WriteLine0(string str)
            {
                writer.WriteLine(str);
            }

            void WriteLine1(string str)
            {
                WriteLine0($"    {str}");
            }

            void WriteLine2(string str)
            {
                WriteLine0($"        {str}");
            }

            void WriteLine3(string str)
            {
                WriteLine0($"            {str}");
            }

            void WriteLine4(string str)
            {
                WriteLine0($"                {str}");
            }

            WriteLine0("using System;");
            WriteLine0("using System.Runtime.InteropServices;");

            WriteLine0("namespace MonC.LLVM");
            WriteLine0("{");
            WriteLine1("public static class CAPIGEN");
            WriteLine1("{");

            foreach (string target in Targets) {
                void WriteDeclaration(string suffix)
                {
                    WriteLine2("[DllImport(\"LLVM-C\")]");
                    WriteLine2($"public static extern void LLVMInitialize{target}{suffix}();");
                    writer.WriteLine(string.Empty);
                }

                WriteDeclaration("TargetInfo");
                WriteDeclaration("Target");
                WriteDeclaration("TargetMC");
                WriteDeclaration("AsmPrinter");
            }

            WriteLine2("public static void LLVMInitializeAllTargets()");
            WriteLine2("{");

            foreach (string target in Targets) {
                WriteLine3("try {");

                void WriteCall(string suffix)
                {
                    WriteLine4($"LLVMInitialize{target}{suffix}();");
                }

                WriteCall("TargetInfo");
                WriteCall("Target");
                WriteCall("TargetMC");
                WriteCall("AsmPrinter");

                WriteLine3("} catch (EntryPointNotFoundException) { }");
            }

            WriteLine2("}");
            WriteLine1("}");
            WriteLine0("}");
        }
    }
}
