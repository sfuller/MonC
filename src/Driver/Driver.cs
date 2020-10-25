namespace Driver
{
    public static class Driver
    {
        public static int Main(string[] args)
        {
            CommandLine commandLine = new CommandLine(args);
            if (commandLine.OutputHelpIfRequested("MonC v1.OwO")) {
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
    }
}
