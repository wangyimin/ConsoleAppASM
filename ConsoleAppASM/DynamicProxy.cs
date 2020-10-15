using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ConsoleAppASM
{
    public class DynamicProxy
    {
        public static T GetInstance<T>()
        {
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = "DynamicWrapper";
            //AppDomain thisDomain = Thread.GetDomain();
            AppDomain thisDomain = AppDomain.CurrentDomain;
            var _asmBuilder = thisDomain.DefineDynamicAssembly(assemblyName,
                         AssemblyBuilderAccess.Run);

            var _modBuilder = _asmBuilder.DefineDynamicModule(
                         _asmBuilder.GetName().Name, false);

            TypeBuilder _typeBuilder = _modBuilder.DefineType("Proxy" + typeof(T).Name,
               TypeAttributes.Public |
               TypeAttributes.Class |
               TypeAttributes.AutoClass |
               TypeAttributes.AnsiClass |
               TypeAttributes.BeforeFieldInit |
               TypeAttributes.AutoLayout,
               typeof(T));

            //typeBuilder.AddInterfaceImplementation(typeof(I));

            MethodInfo[] _lst = typeof(A).GetMethods().Cast<MethodInfo>()
                .Where(el => el.GetCustomAttributes(typeof(LoggerAttribute)).Any()).ToArray();

            foreach (MethodInfo _mi in _lst)
            {
                var _args = _mi.GetParameters();
                MethodBuilder methodBuilder = _typeBuilder.DefineMethod(
                    _mi.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot,
                    _mi.ReturnType, (from _arg in _args select _arg.ParameterType).ToArray()
                    );
                _typeBuilder.DefineMethodOverride(methodBuilder, _mi);

                ILGenerator _il = methodBuilder.GetILGenerator();

                _il.Emit(OpCodes.Ldarg_0);
                for (int i = 0; i < _args.Length; i++)
                {
                    _il.Emit(OpCodes.Ldarg_S, i + 1);
                }
                _il.Emit(OpCodes.Ldstr, "START");
                _il.Emit(OpCodes.Call, typeof(Logger).GetMethod("Debug",
                    new Type[] { typeof(string) }));
                _il.Emit(OpCodes.Call, _typeBuilder.BaseType.GetMethod(_mi.Name));
                _il.Emit(OpCodes.Ldstr, "END");
                _il.Emit(OpCodes.Call, typeof(Logger).GetMethod("Debug",
                    new Type[] { typeof(string) }));
                _il.Emit(OpCodes.Ret);
            }

            Type type = _typeBuilder.CreateType();
            return (T)Activator.CreateInstance(type);
        }
    }
}
