using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonC.Parsing;
using MonC.SyntaxTree;
using MonC.VM;

namespace MonC.DotNetInterop
{
    public class InteropResolver
    {
        private readonly bool _includeImplementations;
        private readonly Dictionary<string, Binding> _bindings = new Dictionary<string, Binding>();
        private readonly HashSet<Type> _linkableModules = new HashSet<Type>();
        private readonly List<EnumLeaf> _enums = new List<EnumLeaf>();
        private readonly List<string> _errors = new List<string>();

        public InteropResolver(bool includeImplementations = true)
        {
            _includeImplementations = includeImplementations;
        }
        
        public IEnumerable<Binding> Bindings => _bindings.Values;
        public IEnumerable<EnumLeaf> Enums => _enums;
        public IEnumerable<string> Errors => _errors;

        public ParseModule CreateHeaderModule()
        {
            ParseModule module = new ParseModule();
            module.Functions.AddRange(_bindings.Values.Select(binding => binding.Prototype));
            module.Enums.AddRange(_enums);
            return module;
        }

        public void ImportAssembly(Assembly assembly, BindingFlags flags)
        {
            foreach (Type type in assembly.GetTypes()) {
                object[] attributes = type.GetCustomAttributes(typeof(LinkableModuleAttribute), inherit: false);
                if (attributes.Length > 0) {
                    ImportType(type, flags);
                }
            }
        }
        
        /// <summary>
        /// Import bindings from the given type using the given binding flags and the given instance of the type.
        /// Returns true if any bindings were imported, else false.
        /// </summary>
        public bool ImportType(Type type, BindingFlags flags, object? target = null)
        {
            bool imported = false;
            
            _linkableModules.Add(type);

            if ((flags & BindingFlags.Static) > 0) {
                MethodInfo[] staticMethods = type.GetMethods((flags | BindingFlags.Instance) ^ BindingFlags.Instance);
                foreach (MethodInfo method in staticMethods) {
                    imported |= ImportMethod(method);
                }
            }

            if ((flags & BindingFlags.Instance) > 0) {
                if (target == null && _includeImplementations) {
                    _errors.Add($"Attempted to include implementations for instance methods of type {type.Name}, but no target was given.");
                } else {
                    MethodInfo[] instanceMethods = type.GetMethods((flags | BindingFlags.Static) ^ BindingFlags.Static);
                    foreach (MethodInfo method in instanceMethods) {
                        imported |= ImportMethod(method, target);
                    }    
                }
            }

            if (type.IsEnum) {
                imported |= ImportEnum(type);
            }

            return imported;
        }

        /// <summary>
        /// Try to import the given method with the given target.
        /// Returns true if the method was imported, otherwise false. 
        /// </summary>
        public bool ImportMethod(MethodInfo method, object? target = null)
        {
            object[] attribs = method.GetCustomAttributes(typeof(LinkableFunctionAttribute), inherit: false);
            if (attribs.Length == 0) {
                return false;
            }

            LinkableFunctionAttribute attribute = (LinkableFunctionAttribute) attribs[0];
            ParameterInfo[] parameters = method.GetParameters();

            if (ProcessSimpleBinding(attribute, method, parameters, target)) {
                return true;
            }

            if (ProcessEnumeratorBinding(attribute, method, parameters, target)) {
                return true;
            }
            
            _errors.Add($"Could not import method {method.Name}, please check that it's signature is compatible.");
            return false;
        }

        public bool PrepareForExecution(VMModule module, IList<string> errors)
        {
            Dictionary<string, int> exporetedFunctions = module.ILModule.ExportedFunctions.ToDictionary(p => p.Key, p => p.Value);
            bool success = true;
            
            foreach (Type type in _linkableModules) {
                FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields) {
                    object[] attribs = field.GetCustomAttributes(typeof(ExternalFunctionAttribute), inherit: false);
                    foreach (ExternalFunctionAttribute attrib in attribs) {
                        int index;
                        if (!exporetedFunctions.TryGetValue(attrib.Name, out index)) {
                            errors.Add($"Cannot find exported method {attrib.Name}");
                            success = false;
                        }
                        field.SetValue(null, index);
                    }
                }
            }

            return success;
        }
        
        private bool ProcessSimpleBinding(
            LinkableFunctionAttribute attribute,
            MethodInfo method, 
            ParameterInfo[] parameters, 
            object? target)
        {
            if (method.ReturnType != typeof(int)) {
                return false;
            }
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(ArgumentSource)) {
                return false;
            }

            VMEnumerableDelegate impl;
            if (_includeImplementations) {
                impl = WrapSimpleBinding(method, target);
            } else {
                impl = NoOpBinding;
            }
            
            AddBinding(method, attribute, impl);
            return true;
        }

        private bool ProcessEnumeratorBinding(
                LinkableFunctionAttribute attribute,
                MethodInfo method,
                ParameterInfo[] parameters,
                object? target)
        {
            if (method.ReturnType != typeof(IEnumerator<Continuation>)) {
                return false;
            }

            if (parameters.Length != 2) {
                return false;
            }
            if (parameters[0].ParameterType != typeof(IVMBindingContext)) {
                return false;
            }
            if (parameters[1].ParameterType != typeof(ArgumentSource)) {
                return false;
            }

            VMEnumerableDelegate impl;
            if (_includeImplementations) {
                impl = CreateDelegate<VMEnumerableDelegate>(method, target);
            } else {
                impl = NoOpBinding;
            }
            
            AddBinding(method, attribute, impl);
            return true;
        }

        private void AddBinding(MethodInfo method, LinkableFunctionAttribute attribute, VMEnumerableDelegate implementation)
        {
            FunctionDefinitionLeaf def = new FunctionDefinitionLeaf(
                name: method.Name,
                returnType: new TypeSpecifierLeaf("int", PointerType.NotAPointer),
                parameters: FunctionAttributeToDeclarations(attribute),
                body: new BodyLeaf(new IASTLeaf[0]), 
                isExported: true
            );

            Binding binding = new Binding {
                Prototype = def,
                Implementation = new VMFunction {
                    ArgumentMemorySize = def.Parameters.Length,
                    Delegate = implementation
                }
            };
            _bindings[method.Name] = binding;
        }

        private static T CreateDelegate<T>(MethodInfo method, object? target) where T : class
        {
            T? del;
            
            if (target != null) {
                del = Delegate.CreateDelegate(typeof(T), target, method) as T;
                if (del == null) {
                    throw new InvalidCastException(
                        $"Cannot create delegate of {typeof(T)} with method {method} and target {target}.");
                }
                return del;
            }
            
            del = Delegate.CreateDelegate(typeof(T), method) as T;
            if (del == null) {
                throw new InvalidCastException($"Cannot create delegate of {typeof(T)} with method {method}.");
            }
            return del;
        }

        private static VMEnumerableDelegate WrapSimpleBinding(MethodInfo info, object? target)
        {
            VMFunctionDelegate function = CreateDelegate<VMFunctionDelegate>(info, target);
            return (context, args) => SimpleBindingEnumerator(function, args);
        }

        private static IEnumerator<Continuation> SimpleBindingEnumerator(VMFunctionDelegate func, ArgumentSource arguments)
        {
            int rv = func(arguments);
            yield return Continuation.Return(rv);
        }

        private static IEnumerable<DeclarationLeaf> FunctionAttributeToDeclarations(LinkableFunctionAttribute attribute)
        {
            for (int i = 0, ilen = attribute.ArgumentCount; i < ilen; ++i) {
                yield return new DeclarationLeaf(new TypeSpecifierLeaf("int", PointerType.NotAPointer), "", null); 
            }
        }

        private static IEnumerator<Continuation> NoOpBinding(IVMBindingContext context, ArgumentSource args)
        {
            yield break;
        }

        /// <summary>
        /// Try to import the given type as an enum.
        /// Returns true if an enum was imported, otherwise false. 
        /// </summary>
        private bool ImportEnum(Type type)
        {
            object[] customAttributes = type.GetCustomAttributes(typeof(LinkableEnumAttribute), inherit: false);
            if (customAttributes.Length == 0) {
                return false;
            }

            LinkableEnumAttribute attribute = (LinkableEnumAttribute) customAttributes[0];
            ImportEnum(type, attribute);
            return true;
        }

        public void ImportEnum(Type type, LinkableEnumAttribute attribute)
        {
            string[] names = Enum.GetNames(type);
            List<KeyValuePair<string, int>> enumerations = new List<KeyValuePair<string, int>>();

            string prefix = attribute.Prefix ?? "";
            
            foreach (string name in names) {
                enumerations.Add(new KeyValuePair<string, int>(prefix + name, (int)Enum.Parse(type, name)));                
            }
            
            _enums.Add(new EnumLeaf(enumerations, isExported: true));
        }

    }
}
