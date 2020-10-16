using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ConsoleAppASM
{
    public class DynamicProxy
    {
        private static string _WRAPPER_ = "Wrapper";

        public static T GetInstance<T>(params object[] param)
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

            //_type.AddInterfaceImplementation(typeof(I));

            ConstructorInfo[] _lstc = typeof(T).GetConstructors();
            foreach (ConstructorInfo _ci in _lstc)
            {
                Type[] _types = _ci.GetParameters().Select(el => el.ParameterType).ToArray();

                ConstructorBuilder _ctor = _type.DefineConstructor(
                      MethodAttributes.Public,
                      CallingConventions.Standard,
                       _types);

                ILGenerator ctorIL = _ctor.GetILGenerator();
                
                ctorIL.Emit(OpCodes.Ldarg_0);
                for (int i = 0; i < _types.Length; i++)
                    ctorIL.Emit(OpCodes.Ldarg_S, i + 1);

                ctorIL.Emit(OpCodes.Call, _type.BaseType.GetConstructor(_types));
                ctorIL.Emit(OpCodes.Ret);
            }

            MethodInfo[] _lstm = typeof(T).GetMethods().Cast<MethodInfo>()
                .Where(el => el.IsPublic 
                        && el.IsVirtual 
                        && el.GetCustomAttributes(typeof(LoggerAttribute)).Any()).ToArray();

            foreach (MethodInfo _mi in _lstm)
            {
                Type[] _types = _mi.GetParameters().Select(el => el.ParameterType).ToArray();

                MethodBuilder _method = _type.DefineMethod(
                        _mi.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot,
                        _mi.ReturnType, _types);

                _type.DefineMethodOverride(_method, _mi);

                ILGenerator _il = _method.GetILGenerator();

                _il.Emit(OpCodes.Ldarg_0);
                for (int i = 0; i < _types.Length; i++)
                    _il.Emit(OpCodes.Ldarg_S, i + 1);

                _il.Emit(OpCodes.Ldstr, "START");
                _il.Emit(OpCodes.Call, typeof(Logger).GetMethod("Debug", new Type[]{typeof(string)}));
                _il.Emit(OpCodes.Call, _type.BaseType.GetMethod(_mi.Name));
                _il.Emit(OpCodes.Ldstr, "END");
                _il.Emit(OpCodes.Call, typeof(Logger).GetMethod("Debug", new Type[]{typeof(string)}));
                _il.Emit(OpCodes.Ret);
            }

            Type type = _type.CreateType();
            return (T)Activator.CreateInstance(type, param);
        }
    }
}
