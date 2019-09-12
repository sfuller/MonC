using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonC.Parsing.ParseTreeLeaves;
using MonC.SyntaxTree;
using MonC.VM;

namespace MonC.DotNetInterop
{
    public class InteropResolver
    {
        private Dictionary<string, VMEnumerable> _bindings = new Dictionary<string, VMEnumerable>();
        private readonly Dictionary<string, FunctionDefinitionLeaf> _definitions = new Dictionary<string, FunctionDefinitionLeaf>();
        private readonly List<Type> _linkableModules = new List<Type>();
        
        public IEnumerable<FunctionDefinitionLeaf> Definitions => _definitions.Values;
        public IEnumerable<KeyValuePair<string, VMEnumerable>> Bindings => _bindings;
        
        public void FindBindings(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes()) {
                object[] attributes = type.GetCustomAttributes(typeof(LinkableModuleAttribute), inherit: false);
                if (attributes.Length > 0) {
                    ImportModule(type);
                }
            }
        }

        public bool PrepareForExecution(VMModule module, IList<string> errors)
        {
            Dictionary<string, int> exporetedFunctions = module.Module.ExportedFunctions.ToDictionary(p => p.Key, p => p.Value);
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

        private void ImportModule(Type type)
        {
            _linkableModules.Add(type);
            
            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);

            foreach (MethodInfo method in methods) {
                ProcessFunction(method);
            }
        }

        private void ProcessFunction(MethodInfo method)
        {
            object[] attribs = method.GetCustomAttributes(typeof(LinkableFunctionAttribute), inherit: false);
            if (attribs.Length == 0) {
                return;
            }

            LinkableFunctionAttribute attribute = (LinkableFunctionAttribute) attribs[0];
            ParameterInfo[] parameters = method.GetParameters();

            if (ProcessSimpleBinding(attribute, method, parameters)) {
                return;
            }

            if (ProcessEnumeratorBinding(attribute, method, parameters)) {
                return;
            }
        }

        private bool ProcessSimpleBinding(LinkableFunctionAttribute attribute, MethodInfo method, ParameterInfo[] parameters)
        {
            if (method.ReturnType != typeof(int)) {
                return false;
            }
            if (parameters.Length != 1 && parameters[0].ParameterType != typeof(int[])) {
                return false;
            }

            AddBinding(method, attribute, WrapSimpleBinding(method));
            return true;
        }

        private bool ProcessEnumeratorBinding(LinkableFunctionAttribute attribute, MethodInfo method, ParameterInfo[] parameters)
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
            if (parameters[1].ParameterType != typeof(int[])) {
                return false;
            }

            AddBinding(method, attribute, (VMEnumerable) Delegate.CreateDelegate(typeof(VMEnumerable), method));
            return true;
        }

        private void AddBinding(MethodInfo method, LinkableFunctionAttribute attribute, VMEnumerable binding)
        {
            FunctionDefinitionLeaf def = new FunctionDefinitionLeaf(
                name: method.Name,
                returnType: "int",
                parameters: FunctionAttributeToDeclarations(attribute),
                body: new BodyLeaf(new IASTLeaf[0]), 
                isExported: true
            );

            _definitions[method.Name] = def;
            _bindings[method.Name] = binding;
        }

        private static VMEnumerable WrapSimpleBinding(MethodInfo info)
        {
            VMFunction func = (VMFunction)Delegate.CreateDelegate(typeof(VMFunction), info);
            return (context, args) => SimpleBindingEnumerator(func, args);
        }

        private static IEnumerator<Continuation> SimpleBindingEnumerator(VMFunction func, int[] arguments)
        {
            int rv = func(arguments);
            yield return Continuation.Return(rv);
        }

        private static IEnumerable<DeclarationLeaf> FunctionAttributeToDeclarations(LinkableFunctionAttribute attribute)
        {
            for (int i = 0, ilen = attribute.ArgumentCount; i < ilen; ++i) {
                yield return new DeclarationLeaf("int", "", new Optional<IASTLeaf>()); 
            }
        }

    }
}