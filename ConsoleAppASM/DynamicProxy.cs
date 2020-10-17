using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ConsoleAppASM
{
    public class DynamicProxy
    {
        private static string _WRAPPER_ = "Wrapper";

        private IExceptionHandler _eh = null;

        public DynamicProxy(IExceptionHandler eh = null)
        {
            _eh = eh ?? new SampleExceptionHandler();
        }

        public T GetInstance<T>(params object[] param)
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

                ILGenerator _ctorIL = _ctor.GetILGenerator();
                
                _ctorIL.Emit(OpCodes.Ldarg_0);
                for (int i = 0; i < _types.Length; i++)
                    _ctorIL.Emit(OpCodes.Ldarg_S, i + 1);

                _ctorIL.Emit(OpCodes.Call, _type.BaseType.GetConstructor(_types));
                _ctorIL.Emit(OpCodes.Ret);
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


                ILGenerator _methodIL = _method.GetILGenerator();

                LocalBuilder _r = _mi.ReturnType != typeof(void) ? _methodIL.DeclareLocal(_mi.ReturnType) : default(LocalBuilder);
                LocalBuilder _e = _methodIL.DeclareLocal(typeof(Exception));

                _methodIL.BeginExceptionBlock();

                _methodIL.Emit(OpCodes.Ldstr, typeof(T).Name + ":" + _mi.Name + " START");
                _methodIL.Emit(OpCodes.Call, typeof(Logger).GetMethod("Debug", new Type[] { typeof(string) }));

                _methodIL.Emit(OpCodes.Ldarg_0);
                for (int i = 0; i < _types.Length; i++)
                    _methodIL.Emit(OpCodes.Ldarg_S, i + 1);

                _methodIL.Emit(OpCodes.Call, _type.BaseType.GetMethod(_mi.Name));
                if (_r != null) _methodIL.Emit(OpCodes.Stloc_S, _r);

                _methodIL.Emit(OpCodes.Ldstr, typeof(T).Name + ":" + _mi.Name + " END");
                _methodIL.Emit(OpCodes.Call, typeof(Logger).GetMethod("Debug", new Type[]{typeof(string)}));

                _methodIL.BeginCatchBlock(typeof(Exception));
                _methodIL.Emit(OpCodes.Stloc_S, _e);
                _methodIL.Emit(OpCodes.Ldarg_0);
                _methodIL.Emit(OpCodes.Ldloc_S, _e);
                _methodIL.Emit(OpCodes.Call, _eh.GetType().GetMethod("ExceptionHandler"));
                _methodIL.EndExceptionBlock();

                if (_r != null) _methodIL.Emit(OpCodes.Ldloc_S, _r);
                _methodIL.Emit(OpCodes.Ret);
            }

            Type type = _type.CreateType();
            return (T)Activator.CreateInstance(type, param);
        }

    }

    public interface IExceptionHandler
    {
        void ExceptionHandler(Exception e);
    }

    public class SampleExceptionHandler : IExceptionHandler
    {
        public void ExceptionHandler(Exception e)
        {
            System.Diagnostics.Trace.WriteLine(e.Message);
        }
    }
}
