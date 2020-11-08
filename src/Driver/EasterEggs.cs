namespace Driver
{
    // TODO: Is this too obvious?
    [CommandLineCategory("Easter Eggs")]
    public class EasterEggs
    {
        [CommandLine("--owo", "*Notices your easter eggs*")]
        public bool Owo;

        [CommandLine("--transrights", "That's right, we're saying it!")]
        public bool TransRights;

        public bool PrintEasterEggs(string versionText)
        {
            if (Owo) {
                CommandLine.WriteWithPride(versionText + "w0", CommandLine.GayColors);
            }
            else if (TransRights) {
                CommandLine.WriteWithPride(versionText + " says trans rights!", CommandLine.TransColors);
            } else {
                return false;
            }

            return true;
        }
    }
}
