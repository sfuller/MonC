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

        public const string UNOP_NEGATE = "-";
        public const string UNOP_LOGICAL_NOT = "!";

        public const string BINOP_ADD = "+";
        public const string BINOP_SUBTRACT = "-";
        public const string BINOP_MULTIPLY = "*";
        public const string BINOP_DIVIDE = "/";
        public const string BINOP_MODULO = "%";
        public const string BINOP_LESS_THAN = "<";
        public const string BINOP_GREATER_THAN = ">";
        public const string BINOP_GREATER_THAN_OR_EQUAL_TO = ">=";
        public const string BINOP_LESS_THAN_OR_EQUAL_TO = "<=";
        public const string BINOP_EQUALS = "==";
        public const string BINOP_NOT_EQUALS = "!=";
        public const string BINOP_LOGICAL_AND = "&&";
        public const string BINOP_LOGICAL_OR = "||";
        public const string BINOP_ASSIGN = "=";

        public const string POINTER_SHARED = "*";
        public const string POINTER_WEAK = "?";
        public const string POINTER_OWNED = "^";
        public const string POINTER_BORROWED = "&";

        public const string HEX_NUMERIC_PREFIX = "0x";

        public const string STRUCT_FUNCTION_ASSOCIATION_STARTER = ":";
        public const string STRUCT_FUNCTION_ASSOCIATION_SEPARATOR = "=";

        public const string FUNCTION_ATTRIBUTE_DROP = "drop";

        // TODO: Needs better name -- 2-character tokens are defined here for use by GetTokensByLength.
        // TODO: It's easy for this to get out of date.. Use reflection, or later on use codegen to generate this.
        private static readonly string[] SYNTAX_VALUES = {
            BINOP_GREATER_THAN_OR_EQUAL_TO,
            BINOP_LESS_THAN_OR_EQUAL_TO,
            BINOP_EQUALS,
            BINOP_NOT_EQUALS,
            BINOP_LOGICAL_AND,
            BINOP_LOGICAL_OR
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
