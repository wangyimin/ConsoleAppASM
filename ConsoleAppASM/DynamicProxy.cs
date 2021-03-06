﻿using System;
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
            _eh = eh;
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
                LocalBuilder _e = _eh != null ? _methodIL.DeclareLocal(typeof(Exception)) : default(LocalBuilder);
                
                if (_eh != null) _methodIL.BeginExceptionBlock();

                LoggerAttribute _attr = ((LoggerAttribute)(_mi.GetCustomAttributes(typeof(LoggerAttribute)).First()));

                // Log(START)
                if (_attr.Log) _methodIL.Emit(OpCodes.Ldstr, typeof(T).Name + ":" + _mi.Name + " START");
                if (_attr.Log) _methodIL.Emit(OpCodes.Call, typeof(Logger).GetMethod("Debug", new Type[] { typeof(string) }));

                // Before
                if (_attr.Before != null)
                {
                    LocalBuilder _before = _methodIL.DeclareLocal(typeof(IBefore));

                    _methodIL.Emit(OpCodes.Ldtoken, _attr.Before);
                    _methodIL.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) }));
                    _methodIL.Emit(OpCodes.Call, typeof(Activator).GetMethod("CreateInstance", new Type[] { typeof(Type) }));
                    _methodIL.Emit(OpCodes.Castclass, typeof(IBefore));
                    _methodIL.Emit(OpCodes.Stloc_S, _before);
                    
                    _methodIL.Emit(OpCodes.Ldloc_S, _before);

                    _methodIL.Emit(OpCodes.Ldc_I4_S, _types.Length);
                    _methodIL.Emit(OpCodes.Newarr, typeof(object));
                    for (int i = 0; i < _types.Length; i++)
                    {
                        _methodIL.Emit(OpCodes.Dup);
                        _methodIL.Emit(OpCodes.Ldc_I4_S, i);
                        _methodIL.Emit(OpCodes.Ldarg_S, i + 1);
                        _methodIL.Emit(OpCodes.Box, _types[i]);
                        _methodIL.Emit(OpCodes.Stelem_Ref);
                    }
                    _methodIL.Emit(OpCodes.Call, _before.LocalType.GetMethod("Before"));
                }

                // Main
                _methodIL.Emit(OpCodes.Ldarg_0);
                for (int i = 0; i < _types.Length; i++)
                    _methodIL.Emit(OpCodes.Ldarg_S, i + 1);

                _methodIL.Emit(OpCodes.Call, _type.BaseType.GetMethod(_mi.Name));
                if (_r != null) _methodIL.Emit(OpCodes.Stloc_S, _r);

                // Log(END)
                if (_attr.Log) _methodIL.Emit(OpCodes.Ldstr, typeof(T).Name + ":" + _mi.Name + " END");
                if (_attr.Log) _methodIL.Emit(OpCodes.Call, typeof(Logger).GetMethod("Debug", new Type[]{typeof(string)}));

                // Exception
                if (_eh != null) _methodIL.BeginCatchBlock(typeof(Exception));
                if (_eh != null) _methodIL.Emit(OpCodes.Stloc_S, _e);
                if (_eh != null) _methodIL.Emit(OpCodes.Ldarg_0);
                if (_eh != null) _methodIL.Emit(OpCodes.Ldloc_S, _e);
                if (_eh != null) _methodIL.Emit(OpCodes.Call, _eh.GetType().GetMethod("ExceptionHandler"));
                if (_eh != null) _methodIL.EndExceptionBlock();
                
                // ReturnValue
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
            System.Diagnostics.Trace.WriteLine(e.StackTrace.ToString());
            throw new Exception(e.Message, e);
        }
    }

    public interface IBefore
    {
        void Before(params object[] obj);
    }

    public class SampleBefore : IBefore
    {
        public void Before(params object[] obj)
        {
            System.Diagnostics.Trace.WriteLine($"Before medthod is called with parameter: [{String.Join(",", obj)}]");
        }
    }
}
