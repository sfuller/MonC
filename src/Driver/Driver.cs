using System;

namespace Driver
{
    public static class Driver
    {
        public static int Main(string[] args)
        {
            CommandLine commandLine = new CommandLine(args);
            if (commandLine.HelpRequested) {
                PrintHelp(commandLine);
                return 0;
            }

            try {
                using Job job = new Job(commandLine);
                commandLine.OutputUnusedArguments();
                return job.Execute();
            } catch (DiagnosticsException) {
                return 1;
            }
        }

        private static void PrintHelp(CommandLine commandLine)
        {
            // TODO: Versioning scheme? Someday...
            string versionText = "MonC v1.0";

            EasterEggs eggs = new EasterEggs();
            commandLine.ApplyTo(eggs);

            if (!eggs.PrintEasterEggs(versionText)) {
                Console.WriteLine(versionText);
            }

            commandLine.OutputHelp();
        }
    }
}
