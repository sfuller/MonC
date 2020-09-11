using System;

namespace Driver
{
    public static class Driver
    {
        public static void Main(string[] args)
        {
            CommandLine commandLine = new CommandLine(args);
            if (commandLine.OutputHelpIfRequested("MonC v1.OwO")) {
                Environment.Exit(0);
            }

            try {
                using Job job = new Job(commandLine);
                commandLine.OutputUnusedArguments();
                job.Execute();
            } catch (DiagnosticsException) {
                Environment.Exit(1);
            }
        }
    }
}
