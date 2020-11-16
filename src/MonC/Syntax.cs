using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

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
        public const string UNOP_BORROW = "&";
        public const string UNOP_DEREFERENCE = "*";

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
        public const string BINOP_ACCESS = ".";

        public const string POINTER_SHARED = "*";
        public const string POINTER_WEAK = "?";
        public const string POINTER_OWNED = "^";
        public const string POINTER_BORROWED = "&";

        public const string HEX_NUMERIC_PREFIX = "0x";

        public const string CAST_REINTERPRET = "!";

        public const string STRUCT_FUNCTION_ASSOCIATION_STARTER = ":";
        public const string STRUCT_FUNCTION_ASSOCIATION_SEPARATOR = "=";

        // TODO: Currently the lexer only recognizes tokens as syntax if they begin with a non alphabet character.
        // Should these constants below be moved somewhere else? Should we change how the lexer works?

        public const string FUNCTION_ATTRIBUTE_DROP = "drop";

        public const string BODY_TYPE_DANGEROUS = "danger";


        private static Dictionary<int, ReadOnlyCollection<string>>? _tokensByLength;

        private static Dictionary<int, ReadOnlyCollection<string>> GetTokensByLength()
        {
            if (_tokensByLength != null) {
                return _tokensByLength;
            }

            Dictionary<int, List<string>> tokensByLength = new Dictionary<int, List<string>>();

            FieldInfo[] fields = typeof(Syntax).GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (FieldInfo field in fields) {
                if (field.FieldType == typeof(string) && field.IsLiteral) {
                    string value = (string) field.GetValue(null);
                    if (!tokensByLength.TryGetValue(value.Length, out List<string> tokenList)) {
                        tokenList = new List<string>(1);
                        tokensByLength[value.Length] = tokenList;
                    }
                    tokenList.Add(value);
                }
            }

            _tokensByLength = new Dictionary<int, ReadOnlyCollection<string>>();
            foreach (KeyValuePair<int, List<string>> kvp in tokensByLength) {
                _tokensByLength[kvp.Key] = new ReadOnlyCollection<string>(kvp.Value);
            }
            return _tokensByLength;
        }

        public static ReadOnlyCollection<string> GetTokensByLength(int length)
        {
            if (GetTokensByLength().TryGetValue(length, out ReadOnlyCollection<string> tokens)) {
                return tokens;
            }

            // TODO: Use a shared empty read only collection
            return new ReadOnlyCollection<string>(new List<string>());
        }

    }
}
