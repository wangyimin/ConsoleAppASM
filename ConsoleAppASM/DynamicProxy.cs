using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ConsoleAppASM
{
    public class DynamicProxy
    {
        private static string _WRAPPER_ = "Wrapper";

        public static T GetInstance<T>()
        {
            var _asm = AppDomain.CurrentDomain.DefineDynamicAssembly(
                        new AssemblyName("Dynamic" + _WRAPPER_)
                        , AssemblyBuilderAccess.RunAndCollect);

            var _module = _asm.DefineDynamicModule(_asm.GetName().Name);

            TypeBuilder _type = _module.DefineType(_WRAPPER_ + typeof(T).Name,
                        TypeAttributes.Public |
                        TypeAttributes.Class |
                        TypeAttributes.AutoClass |
                        TypeAttributes.AnsiClass |
                        TypeAttributes.BeforeFieldInit |
                        TypeAttributes.AutoLayout,
                         typeof(T));

            //typeBuilder.AddInterfaceImplementation(typeof(I));

            MethodInfo[] _lst = typeof(T).GetMethods().Cast<MethodInfo>()
                .Where(el => el.IsPublic 
                        && el.IsVirtual 
                        && el.GetCustomAttributes(typeof(LoggerAttribute)).Any()).ToArray();

            foreach (MethodInfo _mi in _lst)
            {
                var _params = _mi.GetParameters();

                MethodBuilder _method = _type.DefineMethod(
                        _mi.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot,
                        _mi.ReturnType, _params.Select(el => el.ParameterType).ToArray());
                _type.DefineMethodOverride(_method, _mi);

                ILGenerator _il = _method.GetILGenerator();

                _il.Emit(OpCodes.Ldarg_0);
                for (int i = 0; i < _params.Length; i++)
                {
                    _il.Emit(OpCodes.Ldarg_S, i + 1);
                }
                _il.Emit(OpCodes.Ldstr, "START");
                _il.Emit(OpCodes.Call, typeof(Logger).GetMethod("Debug", new Type[]{typeof(string)}));
                _il.Emit(OpCodes.Call, _type.BaseType.GetMethod(_mi.Name));
                _il.Emit(OpCodes.Ldstr, "END");
                _il.Emit(OpCodes.Call, typeof(Logger).GetMethod("Debug", new Type[]{typeof(string)}));
                _il.Emit(OpCodes.Ret);
            }

            Type type = _type.CreateType();
            return (T)Activator.CreateInstance(type);
        }
    }
}
