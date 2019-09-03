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
        
        public IEnumerable<FunctionDefinitionLeaf> Definitions => _definitions.Values;
        
        public void FindBindings(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes()) {
                object[] attributes = type.GetCustomAttributes(typeof(LinkableModuleAttribute), inherit: false);
                if (attributes.Length > 0) {
                    ImportModule(type);
                }
            }
        }

        private void ImportModule(Type type)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);

            foreach (MethodInfo method in methods) {
                ProcessFunction(method);
            }
        }

        private void ProcessFunction(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();

            if (ProcessSimpleBinding(method, parameters)) {
                return;
            }

            if (ProcessEnumeratorBinding(method, parameters)) {
                return;
            }
            
            throw new InvalidOperationException("TODO");
        }

        private bool ProcessSimpleBinding(MethodInfo method, ParameterInfo[] parameters)
        {
            if (method.ReturnType != typeof(int)) {
                return false;
            }

            foreach (ParameterInfo param in parameters) {
                if (param.ParameterType != typeof(int)) {
                    return false;
                }
            }
            
            AddBinding(method, parameters, WrapSimpleBinding(method, parameters));
            return true;
        }

        private bool ProcessEnumeratorBinding(MethodInfo method, ParameterInfo[] parameters)
        {
            if (method.ReturnType != typeof(IEnumerator<Continuation>)) {
                return false;
            }

            if (parameters[0].ParameterType != typeof(IVMBindingContext)) {
                return false;
            }

            for (int i = 1, ilen = parameters.Length; i < ilen; ++i) {
                if (parameters[i].ParameterType != typeof(int)) {
                    return false;
                }
            }
            
            AddBinding(method, parameters, WrapEnumeratorBinding(method, parameters));
            return true;
        }

        private void AddBinding(MethodInfo method, ParameterInfo[] parameters, VMEnumerable binding)
        {
            FunctionDefinitionLeaf def = new FunctionDefinitionLeaf(
                name: method.Name,
                returnType: "int",
                parameters: parameters.Select(ParamToDeclaration),
                body: new PlaceholderLeaf(), 
                isExported: true
            );

            _definitions[method.Name] = def;
            _bindings[method.Name] = binding;
        }

        private static VMEnumerable WrapSimpleBinding(MethodInfo info, ParameterInfo[] parameters)
        {
            return (context, args) => SimpleBindingEnumerator(info, parameters, args);
        }
        
        private static IEnumerator<Continuation> SimpleBindingEnumerator(MethodInfo info, ParameterInfo[] parameters, int[] arguments)
        {
            // TODO: This sucks
            
            object[] invokeArgs = new object[parameters.Length];
            for (int i = 0, ilen = parameters.Length; i < ilen; ++i) {
                invokeArgs[i] = arguments[i];
            }

            object rv = info.Invoke(null, invokeArgs);
            yield return Continuation.Return((int) rv);
        }

        private static DeclarationLeaf ParamToDeclaration(ParameterInfo parameter)
        {
            return new DeclarationLeaf("int", parameter.Name, new Optional<IASTLeaf>(), new Token());
        }

        private static VMEnumerable WrapEnumeratorBinding(MethodInfo info, ParameterInfo[] parameters)
        {
            // TODO: This sucks
            
            VMEnumerable enumerable = (context, args) => {
                object[] invokeArgs = new object[parameters.Length + 1];
                invokeArgs[0] = context;
                for (int i = 0, ilen = parameters.Length; i < ilen; ++i) {
                    invokeArgs[i + 1] = args[i];
                }

                object rv = info.Invoke(null, invokeArgs);
                return (IEnumerator<Continuation>) rv;
            };

            return enumerable;
        }
        


    }
}