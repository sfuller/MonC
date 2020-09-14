using System.Linq;

namespace MonC
{
    public static class Syntax
    {
        public const string OPENING_BRACKET = "{";
        public const string CLOSING_BRACKET = "}";
        public const string OPENING_PAREN = "(";
        public const string CLOSING_PAREN = ")";
        public const string SEMICOLON = ";";
        public const string COMMA = ",";

        public const string ADD = "+";
        public const string SUBTRACT = "-";
        public const string MULTIPLY = "*";
        public const string DIVIDE = "/";
        public const string MODULO = "%";
        public const string LESS_THAN = "<";
        public const string GREATER_THAN = ">";
        public const string GREATER_THAN_OR_EQUAL_TO = ">=";
        public const string LESS_THAN_OR_EQUAL_TO = "<=";
        public const string EQUALS = "==";
        public const string NOT_EQUALS = "!=";
        public const string LOGICAL_AND = "&&";
        public const string LOGICAL_OR = "||";
        public const string ASSIGN = "=";

        public const string POINTER_SHARED = "*";
        public const string POINTER_WEAK = "?";
        public const string POINTER_OWNED = "^";
        public const string POINTER_BORROWED = "&";

        public const string HEX_NUMERIC_PREFIX = "0x";

        // TODO: Needs better name -- 2-character tokens are defined here for use by GetTokensByLength.
        // TODO: It's easy for this to get out of date.. Use reflection, or later on use codegen to generate this.
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
