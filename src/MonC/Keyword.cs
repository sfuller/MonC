using System.Linq;

namespace MonC
{
    public static class Keyword
    {
        public const string IF = "if";
        public const string ELSE = "else";
        public const string WHILE = "while";
        public const string FOR = "for";

        private static readonly string[] KEYWORDS = new string[] {IF, ELSE, WHILE, FOR};

        public static bool IsKeyword(string value)
        {
            // TODO: PERF
            return KEYWORDS.Contains(value);
        }
    }
}