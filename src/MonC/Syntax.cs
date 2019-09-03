using System.Linq;

namespace MonC
{
    public static class Syntax
    {
        public const string GREATER_THAN_OR_EQUAL_TO = ">=";
        public const string LESS_THAN_OR_EQUAL_TO = "<=";
        public const string EQUALS = "==";
        public const string NOT_EQUALS = "!=";
        public const string LOGICAL_AND = "&&";
        public const string LOGICAL_OR = "||";

        private static readonly string[] SYNTAX_VALUES = {
            GREATER_THAN_OR_EQUAL_TO,
            LESS_THAN_OR_EQUAL_TO,
            EQUALS,
            NOT_EQUALS,
            LOGICAL_AND,
            LOGICAL_OR
        };

        public static string[] GetTokensByLength(int length)
        {
            // TODO: Performance
            
            if (length == 2) {
                return SYNTAX_VALUES.ToArray();
            }

            return new string[0];
        }
        
    }
}