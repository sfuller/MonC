using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Driver
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class CommandLineCategoryAttribute : Attribute
    {
        public string Category;

        public CommandLineCategoryAttribute(string category = null) => Category = category;
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class CommandLineAttribute : Attribute
    {
        public string Prefix;
        public string Doc;
        public string Value;

        public CommandLineAttribute(string prefix = null, string doc = null, string value = null)
        {
            Prefix = prefix;
            Doc = doc;
            Value = value;

            if (prefix != null) {
                if (!prefix.StartsWith("-")) {
                    throw new Exception("Command-line arguments must begin with a '-' character");
                }
            }
        }
    }

    [CommandLineCategory("Command Line")]
    public class CommandLine
    {
        private static readonly Type[] CommandLineClasses =
        {
            typeof(CommandLine),
            typeof(Job),
            typeof(LexTool),
            typeof(ParseTool),
            typeof(ToolChains.LLVM)
        };

        [CommandLine("-h", "Print this help message")]
        private bool _printHelp = false;

        private readonly Dictionary<string, List<string>> _flagArguments = new Dictionary<string, List<string>>();
        private readonly List<string> _positionalArguments = new List<string>();
        private readonly HashSet<string> _usedArguments = new HashSet<string>();

        private void InsertFlagArgument(string key, string value)
        {
            if (_flagArguments.TryGetValue(key, out List<string> values)) {
                values.Add(value);
            } else {
                _flagArguments.Add(key, new List<string> {value});
            }
        }

        public CommandLine(string[] args)
        {
            foreach (string arg in args) {
                if (arg.StartsWith("-")) {
                    string[] kvStr = arg.Split("=", 2);
                    InsertFlagArgument(kvStr[0], kvStr.Length == 2 ? kvStr[1] : string.Empty);
                } else {
                    _positionalArguments.Add(arg);
                }
            }

            ApplyTo(this);
        }

        private bool TryGetValue(string key, out List<string> value) => _flagArguments.TryGetValue(key, out value);

        private bool TryGetValue(string key, out bool value) => value = _flagArguments.ContainsKey(key);

        public void ApplyTo(Object obj)
        {
            for (Type type = obj.GetType(); type != typeof(object); type = type.BaseType) {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.GetField |
                                                           BindingFlags.GetProperty | BindingFlags.Public |
                                                           BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                    foreach (CommandLineAttribute attribute in field.GetCustomAttributes<CommandLineAttribute>()) {
                        if (field.FieldType == typeof(bool)) {
                            if (TryGetValue(attribute.Prefix, out bool value)) {
                                _usedArguments.Add(attribute.Prefix);
                                field.SetValue(obj, value);
                            }
                        } else if (TryGetValue(attribute.Prefix, out List<string> values)) {
                            if (field.FieldType.IsGenericType) {
                                if (field.FieldType.GetGenericTypeDefinition() == typeof(List<>)) {
                                    Type listElementType = field.FieldType.GenericTypeArguments[0];
                                    TypeConverter listTypeConverter = TypeDescriptor.GetConverter(listElementType);
                                    if (listTypeConverter.CanConvertFrom(typeof(string))) {
                                        IList list = (IList) field.GetValue(obj);
                                        if (list != null) {
                                            _usedArguments.Add(attribute.Prefix);
                                            foreach (string value in values)
                                                list.Add(listTypeConverter.ConvertFrom(value));
                                        }
                                    }
                                    continue;
                                }
                            }
                            TypeConverter typeConverter = TypeDescriptor.GetConverter(field.FieldType);
                            if (typeConverter.CanConvertFrom(typeof(string))) {
                                _usedArguments.Add(attribute.Prefix);
                                field.SetValue(obj, typeConverter.ConvertFrom(values[0]));
                            }
                        }
                    }
                }
            }
        }

        public IReadOnlyList<string> PositionalArguments => _positionalArguments;

        private static readonly int ColumnChars = 30;

        private static readonly ConsoleColor[] GayColors =
            {ConsoleColor.Red, ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Blue, ConsoleColor.Magenta};

        private static void WriteWithPride(string str)
        {
            ConsoleColor oldConsoleColor = Console.ForegroundColor;
            for (int i = 0, iend = str.Length; i < iend; ++i) {
                Console.ForegroundColor = GayColors[i % GayColors.Length];
                Console.Write(str[i]);
            }

            Console.ForegroundColor = oldConsoleColor;
        }

        private static void WriteWithColor(string str, ConsoleColor color)
        {
            ConsoleColor oldConsoleColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(str);
            Console.ForegroundColor = oldConsoleColor;
        }

        public void OutputUnusedArguments()
        {
            foreach (var pair in _flagArguments.Where(pair => !_usedArguments.Contains(pair.Key))) {
                foreach (string value in pair.Value) {
                    string argStr = value.Length > 0 ? $"{pair.Key}={value}" : pair.Key;
                    Diagnostics.Report(Diagnostics.Severity.Warning, $"{argStr} argument is unused");
                }
            }
        }

        private static void OutputOptionStr(string optionStr)
        {
            Console.Write(optionStr);
            for (int i = 0, iend = Math.Max(1, ColumnChars - optionStr.Length); i < iend; i++)
                Console.Write(' ');
        }

        public bool OutputHelpIfRequested(string versionString)
        {
            if (_printHelp) {
                WriteWithPride(versionString);
                Console.WriteLine();
                Console.WriteLine();
                foreach (Type clClass in CommandLineClasses) {
                    for (Type type = clClass; type != typeof(object); type = type.BaseType) {
                        foreach (CommandLineCategoryAttribute cattribute in type
                            .GetCustomAttributes<CommandLineCategoryAttribute>()) {
                            WriteWithColor($"{cattribute.Category ?? type.Name} Options:", ConsoleColor.White);
                            Console.WriteLine();
                            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.GetField |
                                                                       BindingFlags.GetProperty | BindingFlags.Public |
                                                                       BindingFlags.NonPublic |
                                                                       BindingFlags.DeclaredOnly)) {
                                foreach (CommandLineAttribute attribute in field
                                    .GetCustomAttributes<CommandLineAttribute>()) {
                                    if (field.FieldType == typeof(bool)) {
                                        OutputOptionStr($"  {attribute.Prefix}");
                                    } else {
                                        OutputOptionStr($"  {attribute.Prefix}=<{attribute.Value ?? "value"}>");
                                    }

                                    Console.WriteLine($"{attribute.Doc ?? string.Empty}");
                                }
                            }

                            Console.WriteLine();
                            break;
                        }
                    }
                }

                return true;
            }

            return false;
        }
    }
}