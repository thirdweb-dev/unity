using Mono.Cecil;
using Mono.Cecil.Cil;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WebGLThreadingPatcher.Editor
{
    public class ThreadingPatcher : IPostBuildPlayerScriptDLLs
    {
        public int callbackOrder => 0;

        public void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;

#if UNITY_2022_1_OR_NEWER
            var mscorLibDll = report.GetFiles().FirstOrDefault(f => f.path.EndsWith("mscorlib.dll")).path;
#else
            var mscorLibDll = report.files.FirstOrDefault(f => f.path.EndsWith("mscorlib.dll")).path;
#endif
            if (mscorLibDll == null)
            {
                Debug.LogError("Can't find mscorlib.dll in build dll files");
                return;
            }

            using (var assembly = AssemblyDefinition.ReadAssembly(Path.Combine(mscorLibDll), new ReaderParameters(ReadingMode.Immediate) { ReadWrite = true }))
            {
                var mainModule = assembly.MainModule;
                if (!TryGetTypes(mainModule, out var threadPool, out var synchronizationContext, out var postCallback, out var waitCallback, out var taskExecutionItem, out var timeScheduler))
                    return;

                PatchThreadPool(mainModule, threadPool, synchronizationContext, postCallback, waitCallback, taskExecutionItem);

#if !UNITY_2021_2_OR_NEWER
                PatchTimerScheduler(mainModule, timeScheduler, threadPool, waitCallback);
#endif
                assembly.Write();
            }
        }

        [MenuItem("Tools/PatchDll")]
        public static void TestMethod()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly("D:\\mscorlib.dll", new ReaderParameters(ReadingMode.Immediate) { ReadWrite = true }))
            {
                var mainModule = assembly.MainModule;
                if (!TryGetTypes(mainModule, out var threadPool, out var synchronizationContext, out var postCallback, out var waitCallback, out var taskExecutionItem, out var timeScheduler))
                    return;

                PatchThreadPool(mainModule, threadPool, synchronizationContext, postCallback, waitCallback, taskExecutionItem);

#if !UNITY_2021_2_OR_NEWER
                PatchTimerScheduler(mainModule, timeScheduler, threadPool, waitCallback);
#endif
                assembly.Write("D:\\mscorlib_p.dll");
            }
        }

        private static void PatchThreadPool(
            ModuleDefinition mainModule,
            TypeDefinition threadPool,
            TypeDefinition synchronizationContext,
            TypeDefinition postCallback,
            TypeDefinition waitCallback,
            TypeDefinition threadPoolWorkItem
        )
        {
            var taskExecutionCallcack = AddTaskExecutionPostCallback(threadPool, threadPoolWorkItem, mainModule);

            foreach (var methodDefinition in threadPool.Methods)
            {
                switch (methodDefinition.Name)
                {
                    case "QueueUserWorkItem" when methodDefinition.HasGenericParameters:
                    case "UnsafeQueueUserWorkItem" when methodDefinition.HasGenericParameters:
                        PatchQueueUserWorkItemGeneric(mainModule, methodDefinition, synchronizationContext, waitCallback, postCallback);
                        break;
                    case "QueueUserWorkItem":
                    case "UnsafeQueueUserWorkItem":
                        PatchQueueUserWorkItem(mainModule, methodDefinition, synchronizationContext, waitCallback, postCallback);
                        break;
                    case "UnsafeQueueCustomWorkItem":
                        PatchUnsafeQueueCustomWorkItem(mainModule, methodDefinition, synchronizationContext, taskExecutionCallcack, postCallback);
                        break;
                    case "TryPopCustomWorkItem":
                        PatchTryPopCustomWorkItem(methodDefinition);
                        break;
                    case "GetAvailableThreads":
                    case "GetMaxThreads":
                    case "GetMinThreads":
                        PatchGetThreads(methodDefinition);
                        break;
                    case "SetMaxThreads":
                    case "SetMinThreads":
                        PatchSetThreads(methodDefinition);
                        break;
                }
            }
        }

        /// <summary>
        /// Creates following class
        /// <code>
        /// class <>_GenericWrapper<T>
        /// {
        ///     public Action<T> callabck;
        ///
        ///     public void Invoke(object state)
        ///     {
        ///         callback((T)state);
        ///     }
        /// }
        /// </code>
        /// </summary>
        /// <param name="moduleDefinition"></param>
        /// <returns></returns>
        private static TypeDefinition GetGenericToObjectDelegateWrapper(ModuleDefinition moduleDefinition)
        {
            const string Namespace = "System.Threading";
            const string ClassName = "<>_GenericWrapper";
            if (moduleDefinition.Types.FirstOrDefault(t => t.Namespace == Namespace && t.Name == ClassName) is { } wrapper)
            {
                return wrapper;
            }

            var genericWrapper = new TypeDefinition(Namespace, ClassName, TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            var genericParameter = new GenericParameter("T", genericWrapper);
            genericWrapper.GenericParameters.Add(genericParameter);

            var (actionOfT, callbackField) = CreateCallbackField(moduleDefinition, genericWrapper, genericParameter);
            var ctor = CreateConstructor(moduleDefinition, callbackField);
            var wrapMethod = CreateInvokeMethod(moduleDefinition, genericParameter, actionOfT, callbackField);

            genericWrapper.Methods.Add(ctor);
            genericWrapper.Methods.Add(wrapMethod);

            moduleDefinition.Types.Add(genericWrapper);
            return genericWrapper;

            static (TypeReference, FieldReference) CreateCallbackField(ModuleDefinition moduleDefinition, TypeDefinition genericWrapper, GenericParameter genericParameter)
            {
                var actionType = moduleDefinition.Types.First(t => t.FullName == "System.Action`1" && t.GenericParameters.Count == 1);
                var actionOfT = new GenericInstanceType(actionType);
                actionOfT.GenericArguments.Add(genericParameter);
                FieldDefinition callback = new FieldDefinition("callback", FieldAttributes.Public, actionOfT);
                genericWrapper.Fields.Add(callback);

                var wrapperOfT = new GenericInstanceType(genericWrapper);
                wrapperOfT.GenericArguments.Add(genericParameter);
                return (actionOfT, new FieldReference(callback.Name, actionOfT, wrapperOfT));
            }

            static MethodDefinition CreateInvokeMethod(ModuleDefinition moduleDefinition, GenericParameter genericParameter, TypeReference actionOfT, FieldReference callbackField)
            {
                var wrapMethod = new MethodDefinition("Invoke", MethodAttributes.Public, moduleDefinition.TypeSystem.Void);
                wrapMethod.Parameters.Add(new ParameterDefinition(moduleDefinition.TypeSystem.Object) { Name = "state" });
                var ilProcessor = wrapMethod.Body.GetILProcessor();
                ilProcessor.Emit(OpCodes.Ldarg_0);
                ilProcessor.Emit(OpCodes.Ldfld, callbackField);
                ilProcessor.Emit(OpCodes.Ldarg_1);
                ilProcessor.Emit(OpCodes.Unbox_Any, genericParameter);
                var invokeMethod = new MethodReference("Invoke", moduleDefinition.TypeSystem.Void, actionOfT) { HasThis = true };
                invokeMethod.Parameters.Add(new ParameterDefinition(genericParameter));
                ilProcessor.Emit(OpCodes.Callvirt, invokeMethod);
                ilProcessor.Emit(OpCodes.Ret);

                return wrapMethod;
            }

            static MethodDefinition CreateConstructor(ModuleDefinition moduleDefinition, FieldReference callbackField)
            {
                var ctor = new MethodDefinition(
                    ".ctor",
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
                    moduleDefinition.TypeSystem.Void
                );
                ctor.Parameters.Add(new ParameterDefinition(callbackField.FieldType));
                var ilProcessor = ctor.Body.GetILProcessor();
                ilProcessor.Emit(OpCodes.Ldarg_0);
                ilProcessor.Emit(OpCodes.Call, new MethodReference(".ctor", moduleDefinition.TypeSystem.Void, moduleDefinition.TypeSystem.Object) { HasThis = true });
                ilProcessor.Emit(OpCodes.Ldarg_0);
                ilProcessor.Emit(OpCodes.Ldarg_1);
                ilProcessor.Emit(OpCodes.Stfld, callbackField);
                ilProcessor.Emit(OpCodes.Ret);
                return ctor;
            }
        }

        private static void PatchQueueUserWorkItemGeneric(
            ModuleDefinition moduleDefinition,
            MethodDefinition methodDefinition,
            TypeDefinition synchronizationContext,
            TypeDefinition waitCallback,
            TypeDefinition postCallback
        )
        {
            var genericWrapper = GetGenericToObjectDelegateWrapper(moduleDefinition);
            var wrapperOfT = new GenericInstanceType(genericWrapper);
            wrapperOfT.GenericArguments.Add(methodDefinition.GenericParameters[0]);

            var ilPProcessor = methodDefinition.Body.GetILProcessor();
            ilPProcessor.Body.Instructions.Clear();
            methodDefinition.Body.ExceptionHandlers.Clear();

            var actionType = moduleDefinition.Types.First(t => t.FullName == "System.Action`1" && t.GenericParameters.Count == 1);
            var actionOfT = new GenericInstanceType(actionType);
            actionOfT.GenericArguments.Add(genericWrapper.GenericParameters[0]);

            ilPProcessor.Emit(OpCodes.Ldarg_0);
            var wrapperCtor = new MethodReference(".ctor", moduleDefinition.TypeSystem.Void, wrapperOfT);
            wrapperCtor.Parameters.Add(new ParameterDefinition(actionOfT));
            wrapperCtor.HasThis = true;
            ilPProcessor.Emit(OpCodes.Newobj, wrapperCtor);

            var wrapperInvoke = new MethodReference("Invoke", moduleDefinition.TypeSystem.Void, wrapperOfT);
            wrapperInvoke.Parameters.Add(new ParameterDefinition(moduleDefinition.TypeSystem.Object));
            wrapperInvoke.HasThis = true;
            ilPProcessor.Emit(OpCodes.Ldftn, wrapperInvoke);

            ilPProcessor.Emit(OpCodes.Newobj, waitCallback.Methods.First(m => m.IsConstructor && m.Parameters.Count == 2));

            ilPProcessor.Emit(OpCodes.Ldarg_1);
            ilPProcessor.Emit(OpCodes.Box, methodDefinition.GenericParameters[0]);
            var notGenericVariant = new MethodReference(methodDefinition.Name, methodDefinition.ReturnType, methodDefinition.DeclaringType);
            notGenericVariant.Parameters.Add(new ParameterDefinition(moduleDefinition.Types.First(t => t.FullName == "System.Threading.WaitCallback")));
            notGenericVariant.Parameters.Add(new ParameterDefinition(moduleDefinition.TypeSystem.Object));
            ilPProcessor.Emit(OpCodes.Call, notGenericVariant);

            ilPProcessor.Emit(OpCodes.Ret);
        }

        private static void PatchQueueUserWorkItem(
            ModuleDefinition moduleDefinition,
            MethodDefinition methodDefinition,
            TypeDefinition synchronizationContext,
            TypeDefinition waitCallback,
            TypeDefinition postCallback
        )
        {
            var ilPProcessor = methodDefinition.Body.GetILProcessor();
            ilPProcessor.Body.Instructions.Clear();
            methodDefinition.Body.ExceptionHandlers.Clear();
            ilPProcessor.Emit(OpCodes.Call, moduleDefinition.ImportReference(synchronizationContext.Methods.Single(s => s.Name == "get_Current")));
            ilPProcessor.Emit(OpCodes.Ldarg_0);
            ilPProcessor.Emit(OpCodes.Ldftn, moduleDefinition.ImportReference(waitCallback.Methods.Single(s => s.Name == "Invoke")));
            ilPProcessor.Emit(OpCodes.Newobj, moduleDefinition.ImportReference(postCallback.Methods.First(s => s.IsConstructor)));
            if (methodDefinition.Parameters.Count == 2)
                ilPProcessor.Emit(OpCodes.Ldarg_1);
            else
                ilPProcessor.Emit(OpCodes.Ldnull);
            ilPProcessor.Emit(OpCodes.Callvirt, moduleDefinition.ImportReference(synchronizationContext.Methods.Single(s => s.Name == "Post")));

            ilPProcessor.Emit(OpCodes.Ldc_I4_1);
            ilPProcessor.Emit(OpCodes.Ret);
        }

        private static void PatchUnsafeQueueCustomWorkItem(
            ModuleDefinition moduleDefinition,
            MethodDefinition methodDefinition,
            TypeDefinition synchronizationContext,
            MethodDefinition taskExecutionCallcack,
            TypeDefinition postCallback
        )
        {
            var p = methodDefinition.Body.GetILProcessor();
            p.Body.Instructions.Clear();
            methodDefinition.Body.ExceptionHandlers.Clear();
            p.Emit(OpCodes.Call, moduleDefinition.ImportReference(synchronizationContext.Methods.Single(s => s.Name == "get_Current")));
            p.Emit(OpCodes.Ldnull);
            p.Emit(OpCodes.Ldftn, moduleDefinition.ImportReference(taskExecutionCallcack));
            p.Emit(OpCodes.Newobj, moduleDefinition.ImportReference(postCallback.Methods.First(s => s.IsConstructor)));
            p.Emit(OpCodes.Ldarg_0);
            p.Emit(OpCodes.Callvirt, moduleDefinition.ImportReference(synchronizationContext.Methods.Single(s => s.Name == "Post")));

            p.Emit(OpCodes.Ret);
        }

        private static void PatchTryPopCustomWorkItem(MethodDefinition methodDefinition)
        {
            var ilPProcessor = methodDefinition.Body.GetILProcessor();
            ilPProcessor.Body.Instructions.Clear();
            methodDefinition.Body.ExceptionHandlers.Clear();
            ilPProcessor.Emit(OpCodes.Ldc_I4_0);
            ilPProcessor.Emit(OpCodes.Ret);
        }

        private static void PatchGetThreads(MethodDefinition methodDefinition)
        {
            var ilPProcessor = methodDefinition.Body.GetILProcessor();
            ilPProcessor.Body.Instructions.Clear();
            methodDefinition.Body.ExceptionHandlers.Clear();
            ilPProcessor.Emit(OpCodes.Ldarg_0);
            ilPProcessor.Emit(OpCodes.Ldc_I4_1);
            ilPProcessor.Emit(OpCodes.Stind_I4);

            ilPProcessor.Emit(OpCodes.Ldarg_1);
            ilPProcessor.Emit(OpCodes.Ldc_I4_1);
            ilPProcessor.Emit(OpCodes.Stind_I4);
            ilPProcessor.Emit(OpCodes.Ret);
        }

        private static void PatchSetThreads(MethodDefinition methodDefinition)
        {
            var ilPProcessor = methodDefinition.Body.GetILProcessor();
            ilPProcessor.Body.Instructions.Clear();
            methodDefinition.Body.ExceptionHandlers.Clear();
            var falseRet = ilPProcessor.Create(OpCodes.Ldc_I4_0);

            ilPProcessor.Emit(OpCodes.Ldarg_0);
            ilPProcessor.Emit(OpCodes.Ldc_I4_1);
            ilPProcessor.Emit(OpCodes.Bne_Un_S, falseRet);

            ilPProcessor.Emit(OpCodes.Ldarg_1);
            ilPProcessor.Emit(OpCodes.Ldc_I4_1);
            ilPProcessor.Emit(OpCodes.Ceq);
            ilPProcessor.Emit(OpCodes.Ret);

            ilPProcessor.Append(falseRet);
            ilPProcessor.Emit(OpCodes.Ret);
        }

        private static MethodDefinition AddTaskExecutionPostCallback(TypeDefinition threadPool, TypeDefinition taskExecutionItem, ModuleDefinition moduleDefinition)
        {
            var method = new MethodDefinition("TaskExecutionItemExecute", MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig, moduleDefinition.TypeSystem.Void);

            method.Parameters.Add(new ParameterDefinition("state", ParameterAttributes.None, moduleDefinition.TypeSystem.Object));

            var ilProcessor = method.Body.GetILProcessor();
            ilProcessor.Emit(OpCodes.Ldarg_0);
            ilProcessor.Emit(OpCodes.Callvirt, moduleDefinition.ImportReference(taskExecutionItem.Methods.Single(s => s.Name == "ExecuteWorkItem")));
            ilProcessor.Emit(OpCodes.Ret);

            threadPool.Methods.Add(method);

            return method;
        }

        private static void PatchTimerScheduler(ModuleDefinition moduleDefinition, TypeDefinition timerScheduler, TypeDefinition threadPool, TypeDefinition waitCallback)
        {
            var monoPinvoke = AddMonoPInvokeCallbackAttribute(moduleDefinition);

            var timer = moduleDefinition.Types.Single(m => m.FullName == "System.Threading.Timer");
            var listGeneric = moduleDefinition.Types.Single(t => t.HasGenericParameters && t.FullName == "System.Collections.Generic.List`1");
            var timerListRef = MakeGenericType(listGeneric, timer);
            var tempTimerListField = new FieldDefinition("tempList", FieldAttributes.Private, timerListRef);
            timerScheduler.Fields.Add(tempTimerListField);

            var internalAssemblyReference = new ModuleReference("__Internal");
            moduleDefinition.ModuleReferences.Add(internalAssemblyReference);

            MethodDefinition setCallbackMethod = AddSetCallbackPImplMethod(moduleDefinition, internalAssemblyReference);
            MethodDefinition updateTimer = AddUpdateTimerPImplMethod(moduleDefinition, internalAssemblyReference);
            MethodDefinition processTimerMethods = AddProcessTimerMethod(moduleDefinition, timerScheduler, threadPool, waitCallback, updateTimer, tempTimerListField, monoPinvoke);

            PatchChangeMethod(moduleDefinition, timerScheduler, processTimerMethods);
            PatchTimerSchedulerCtor(moduleDefinition, timerScheduler, processTimerMethods, setCallbackMethod, tempTimerListField);

            timerScheduler.Methods.Add(setCallbackMethod);
            timerScheduler.Methods.Add(updateTimer);
            timerScheduler.Methods.Add(processTimerMethods);

            timerScheduler.Methods.Remove(timerScheduler.Methods.Single(m => m.Name == "SchedulerThread"));
            timerScheduler.Fields.Remove(timerScheduler.Fields.Single(m => m.Name == "changed"));
        }

        private static void PatchTimerSchedulerCtor(
            ModuleDefinition moduleDefinition,
            TypeDefinition timerScheduler,
            MethodDefinition precessTimers,
            MethodDefinition setCallback,
            FieldDefinition tempList
        )
        {
            var ctor = timerScheduler.Methods.Single(m => m.IsConstructor && !m.IsStatic);
            ctor.Body.Instructions.Clear();
            ctor.Body.ExceptionHandlers.Clear();

            var @object = moduleDefinition.Types.Single(m => m.FullName == "System.Object");
            var sortedList = moduleDefinition.Types.Single(m => m.FullName == "System.Collections.SortedList");
            var listGeneric = moduleDefinition.Types.Single(m => m.FullName == "System.Collections.Generic.List`1");
            var timer = moduleDefinition.Types.Single(m => m.FullName == "System.Threading.Timer");
            var timerComparer = timer.NestedTypes.Single(m => m.Name.Contains("TimerComparer"));
            var action = moduleDefinition.Types.Single(m => m.FullName == "System.Action");

            var sortedListCtor = sortedList.Methods.Single(m => m.IsConstructor && m.Parameters.Count == 2 && m.Parameters[1].ParameterType.FullName == "System.Int32");
            var timerComparerCtor = timerComparer.Methods.Single(m => m.IsConstructor && m.Parameters.Count == 0);
            var actionCtor = action.Methods.Single(m => m.IsConstructor);
            var objectCtor = @object.Methods.Single(m => m.IsConstructor);
            var listCtor = listGeneric.Methods.Single(m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Int32");

            var ilProcessor = ctor.Body.GetILProcessor();
            ilProcessor.Emit(OpCodes.Ldarg_0);
            ilProcessor.Emit(OpCodes.Call, objectCtor);

            ilProcessor.Emit(OpCodes.Ldarg_0);
            ilProcessor.Emit(OpCodes.Newobj, timerComparerCtor);
            ilProcessor.Emit(OpCodes.Ldc_I4, 1024);
            ilProcessor.Emit(OpCodes.Newobj, sortedListCtor);
            ilProcessor.Emit(OpCodes.Stfld, timerScheduler.Fields.Single(f => f.Name == "list"));

            ilProcessor.Emit(OpCodes.Ldarg_0);
            ilProcessor.Emit(OpCodes.Ldc_I4, 512);
            ilProcessor.Emit(OpCodes.Newobj, MakeGeneric(listCtor, timer));
            ilProcessor.Emit(OpCodes.Stfld, tempList);

            ilProcessor.Emit(OpCodes.Ldnull);
            ilProcessor.Emit(OpCodes.Ldftn, precessTimers);
            ilProcessor.Emit(OpCodes.Newobj, actionCtor);
            ilProcessor.Emit(OpCodes.Call, setCallback);
            ilProcessor.Emit(OpCodes.Ret);
        }

        private static void PatchChangeMethod(ModuleDefinition moduleDefinition, TypeDefinition timerScheduler, MethodDefinition precessTimers)
        {
            var timer = moduleDefinition.Types.Single(m => m.FullName == "System.Threading.Timer");
            var sortedList = moduleDefinition.Types.Single(m => m.FullName == "System.Collections.SortedList");
            var method = timerScheduler.Methods.Single(m => m.Name == "Change");
            method.Body.Instructions.Clear();
            method.Body.ExceptionHandlers.Clear();

            method.Body.Variables.Clear();

            var ilProcessor = method.Body.GetILProcessor();

            ilProcessor.Emit(OpCodes.Ldarg_0);
            ilProcessor.Emit(OpCodes.Ldarg_1);
            ilProcessor.Emit(OpCodes.Call, timerScheduler.Methods.Single(m => m.Name == "InternalRemove"));
            ilProcessor.Emit(OpCodes.Pop);

            var checkDisposed = ilProcessor.Create(OpCodes.Ldarg_1);

            ilProcessor.Emit(OpCodes.Ldarg_2);
            ilProcessor.Emit(OpCodes.Ldc_I8, long.MaxValue);
            ilProcessor.Emit(OpCodes.Bne_Un_S, checkDisposed);
            ilProcessor.Emit(OpCodes.Ldarg_1);
            ilProcessor.Emit(OpCodes.Ldarg_2);
            ilProcessor.Emit(OpCodes.Stfld, timer.Fields.Single(f => f.Name == "next_run"));
            ilProcessor.Emit(OpCodes.Ret);

            var finalReturn = ilProcessor.Create(OpCodes.Ret);

            ilProcessor.Append(checkDisposed);
            ilProcessor.Emit(OpCodes.Ldfld, timer.Fields.Single(f => f.Name == "disposed"));
            ilProcessor.Emit(OpCodes.Brtrue_S, finalReturn);

            ilProcessor.Emit(OpCodes.Ldarg_1);
            ilProcessor.Emit(OpCodes.Ldarg_2);
            ilProcessor.Emit(OpCodes.Stfld, timer.Fields.Single(f => f.Name == "next_run"));
            ilProcessor.Emit(OpCodes.Ldarg_0);
            ilProcessor.Emit(OpCodes.Ldarg_1);
            ilProcessor.Emit(OpCodes.Call, timerScheduler.Methods.Single(m => m.Name == "Add"));

            ilProcessor.Emit(OpCodes.Ldarg_0);
            ilProcessor.Emit(OpCodes.Ldfld, timerScheduler.Fields.Single(f => f.Name == "list"));
            ilProcessor.Emit(OpCodes.Ldc_I4_0);
            ilProcessor.Emit(OpCodes.Call, sortedList.Methods.Single(f => f.Name == "GetByIndex"));
            ilProcessor.Emit(OpCodes.Ldarg_1);
            ilProcessor.Emit(OpCodes.Bne_Un_S, finalReturn);

            ilProcessor.Emit(OpCodes.Call, precessTimers);

            ilProcessor.Append(finalReturn);
        }

        private static MethodDefinition AddSetCallbackPImplMethod(ModuleDefinition moduleDefinition, ModuleReference internalAssemblyReference)
        {
            var setCallbackMethod = new MethodDefinition(
                "SetCallback",
                MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.Private | MethodAttributes.PInvokeImpl,
                moduleDefinition.TypeSystem.Void
            );
            setCallbackMethod.PInvokeInfo = new PInvokeInfo(PInvokeAttributes.CallConvWinapi, "SetCallback", internalAssemblyReference) { IsCharSetNotSpec = true, IsCallConvWinapi = true };
            setCallbackMethod.Parameters.Add(
                new ParameterDefinition(
                    "callback",
                    ParameterAttributes.None,
                    moduleDefinition.ImportReference(moduleDefinition.Types.First(t => t.FullName == "System.Action" && !t.HasGenericParameters))
                )
            );
            return setCallbackMethod;
        }

        private static MethodDefinition AddUpdateTimerPImplMethod(ModuleDefinition moduleDefinition, ModuleReference internalAssemblyReference)
        {
            var updateTimer = new MethodDefinition(
                "UpdateTimer",
                MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.Private | MethodAttributes.PInvokeImpl,
                moduleDefinition.TypeSystem.Void
            );
            updateTimer.PInvokeInfo = new PInvokeInfo(PInvokeAttributes.CallConvWinapi, "UpdateTimer", internalAssemblyReference) { IsCharSetNotSpec = true, IsCallConvWinapi = true };
            updateTimer.Parameters.Add(new ParameterDefinition("interval", ParameterAttributes.None, moduleDefinition.TypeSystem.Int64));
            return updateTimer;
        }

        private static MethodDefinition AddProcessTimerMethod(
            ModuleDefinition moduleDefinition,
            TypeDefinition timerScheduler,
            TypeDefinition threadPool,
            TypeDefinition waitCallback,
            MethodDefinition updateTimer,
            FieldDefinition tempList,
            TypeDefinition monoPinvokeAttr
        )
        {
            var shrinkIfNeededMethod = timerScheduler.Methods.Single(m => m.Name == "ShrinkIfNeeded");
            var getInstance = timerScheduler.Methods.Single(m => m.Name == "get_Instance");
            var unsafeQueue = threadPool.Methods.Single(m => m.Name == "UnsafeQueueUserWorkItem");
            var timer = moduleDefinition.Types.Single(m => m.FullName == "System.Threading.Timer");
            var getTimeMonotonic = timer.Methods.Single(m => m.Name == "GetTimeMonotonic");
            var listGeneric = moduleDefinition.Types.Single(t => t.HasGenericParameters && t.FullName.StartsWith("System.Collections.Generic.List"));
            var sortedListType = moduleDefinition.Types.Single(t => t.FullName == "System.Collections.SortedList");
            var timerListRef = MakeGenericType(listGeneric, timer);

            var processTimerMethods = new MethodDefinition("ProcessTimers", MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.Private, moduleDefinition.TypeSystem.Void);
            var ilProcessor = processTimerMethods.Body.GetILProcessor();

            var TimeToNext = new VariableDefinition(moduleDefinition.TypeSystem.Int64);
            var currentTime = new VariableDefinition(moduleDefinition.TypeSystem.Int64);
            var loopIterator = new VariableDefinition(moduleDefinition.TypeSystem.Int32);
            var loopEnd = new VariableDefinition(moduleDefinition.TypeSystem.Int32);
            var sortedList = new VariableDefinition(sortedListType);
            var list = new VariableDefinition(timerListRef);
            var currentTimer = new VariableDefinition(timer);
            var periodMs = new VariableDefinition(moduleDefinition.TypeSystem.Int64);
            var dueTimeMs = new VariableDefinition(moduleDefinition.TypeSystem.Int64);
            var instance = new VariableDefinition(timerScheduler);

            processTimerMethods.Body.Variables.Add(list);
            processTimerMethods.Body.Variables.Add(TimeToNext);
            processTimerMethods.Body.Variables.Add(currentTime);
            processTimerMethods.Body.Variables.Add(loopIterator);
            processTimerMethods.Body.Variables.Add(loopEnd);
            processTimerMethods.Body.Variables.Add(sortedList);
            processTimerMethods.Body.Variables.Add(currentTimer);
            processTimerMethods.Body.Variables.Add(periodMs);
            processTimerMethods.Body.Variables.Add(dueTimeMs);
            processTimerMethods.Body.Variables.Add(instance);

            var ret = ilProcessor.Create(OpCodes.Ret);

            ilProcessor.Emit(OpCodes.Call, getTimeMonotonic);
            ilProcessor.Emit(OpCodes.Stloc, currentTime);

            var loopCheck = ilProcessor.Create(OpCodes.Ldloc, loopIterator);
            ilProcessor.Emit(OpCodes.Call, getInstance);
            ilProcessor.Emit(OpCodes.Dup);
            ilProcessor.Emit(OpCodes.Dup);
            ilProcessor.Emit(OpCodes.Stloc, instance);

            ilProcessor.Emit(OpCodes.Ldfld, timerScheduler.Fields.Single(f => f.Name == "list"));
            ilProcessor.Emit(OpCodes.Stloc, sortedList);

            ilProcessor.Emit(OpCodes.Ldfld, tempList);
            ilProcessor.Emit(OpCodes.Stloc, list);

            ilProcessor.Emit(OpCodes.Ldloc, sortedList);
            ilProcessor.Emit(OpCodes.Call, sortedListType.Methods.Single(m => m.Name == "get_Count"));
            ilProcessor.Emit(OpCodes.Stloc, loopEnd);

            ilProcessor.Emit(OpCodes.Ldc_I4_0);
            ilProcessor.Emit(OpCodes.Stloc, loopIterator);
            ilProcessor.Emit(OpCodes.Br, loopCheck);

            var loopStart = ilProcessor.Create(OpCodes.Ldloc, sortedList);
            var loopStart2 = ilProcessor.Create(OpCodes.Ldloc, instance);
            var loop2 = ilProcessor.Create(OpCodes.Ldloc, list);
            ilProcessor.Append(loopStart);
            ilProcessor.Emit(OpCodes.Ldloc, loopIterator);
            ilProcessor.Emit(OpCodes.Call, sortedListType.Methods.Single(m => m.Name == "GetByIndex"));
            ilProcessor.Emit(OpCodes.Stloc, currentTimer);
            ilProcessor.Emit(OpCodes.Ldloc, currentTimer);
            ilProcessor.Emit(OpCodes.Ldfld, timer.Fields.Single(f => f.Name == "next_run"));
            ilProcessor.Emit(OpCodes.Ldloc, currentTime);
            ilProcessor.Emit(OpCodes.Bgt, loop2);

            ilProcessor.Emit(OpCodes.Ldloc, sortedList);
            ilProcessor.Emit(OpCodes.Ldloc, loopIterator);
            ilProcessor.Emit(OpCodes.Call, sortedListType.Methods.Single(m => m.Name == "RemoveAt"));

            ilProcessor.Emit(OpCodes.Ldloc, loopIterator);
            ilProcessor.Emit(OpCodes.Ldc_I4_1);
            ilProcessor.Emit(OpCodes.Sub);
            ilProcessor.Emit(OpCodes.Stloc, loopIterator);

            ilProcessor.Emit(OpCodes.Ldloc, loopEnd);
            ilProcessor.Emit(OpCodes.Ldc_I4_1);
            ilProcessor.Emit(OpCodes.Sub);
            ilProcessor.Emit(OpCodes.Stloc, loopEnd);

            ilProcessor.Emit(OpCodes.Ldnull);
            ilProcessor.Emit(OpCodes.Ldftn, timerScheduler.Methods.Single(m => m.Name == "TimerCB"));
            ilProcessor.Emit(OpCodes.Newobj, waitCallback.Methods.Single(mbox => mbox.IsConstructor));
            ilProcessor.Emit(OpCodes.Ldloc, currentTimer);
            ilProcessor.Emit(OpCodes.Call, unsafeQueue);
            ilProcessor.Emit(OpCodes.Pop);

            ilProcessor.Emit(OpCodes.Ldloc, currentTimer);
            ilProcessor.Emit(OpCodes.Ldfld, timer.Fields.Single(f => f.Name == "period_ms"));
            ilProcessor.Emit(OpCodes.Stloc, periodMs);
            ilProcessor.Emit(OpCodes.Ldloc, periodMs);

            var setNextRunToMax = ilProcessor.Create(OpCodes.Ldloc, currentTimer);

            ilProcessor.Emit(OpCodes.Ldc_I4_M1);
            ilProcessor.Emit(OpCodes.Conv_I8);
            ilProcessor.Emit(OpCodes.Beq_S, setNextRunToMax);

            var checkDueTimeStart = ilProcessor.Create(OpCodes.Ldloc, currentTimer);
            ;

            ilProcessor.Emit(OpCodes.Ldloc, periodMs);
            ilProcessor.Emit(OpCodes.Ldc_I4_0);
            ilProcessor.Emit(OpCodes.Conv_I8);
            ilProcessor.Emit(OpCodes.Beq_S, checkDueTimeStart);

            ilProcessor.Emit(OpCodes.Ldloc, periodMs);
            ilProcessor.Emit(OpCodes.Ldc_I4_M1);
            ilProcessor.Emit(OpCodes.Conv_I8);
            ilProcessor.Emit(OpCodes.Bne_Un_S, setNextRunToMax);

            ilProcessor.Append(checkDueTimeStart);
            ilProcessor.Emit(OpCodes.Ldfld, timer.Fields.Single(f => f.Name == "due_time_ms"));

            ilProcessor.Emit(OpCodes.Ldc_I4_M1);
            ilProcessor.Emit(OpCodes.Conv_I8);
            ilProcessor.Emit(OpCodes.Bne_Un_S, setNextRunToMax);

            ilProcessor.Emit(OpCodes.Ldloc, list);
            ilProcessor.Emit(OpCodes.Ldloc, currentTimer);
            ilProcessor.Emit(OpCodes.Dup);
            ilProcessor.Emit(OpCodes.Call, getTimeMonotonic);
            ilProcessor.Emit(OpCodes.Ldc_I4, 10000);
            ilProcessor.Emit(OpCodes.Ldloc, periodMs);
            ilProcessor.Emit(OpCodes.Mul);
            ilProcessor.Emit(OpCodes.Add);
            ilProcessor.Emit(OpCodes.Stfld, timer.Fields.Single(f => f.Name == "next_run"));
            var listAdd = listGeneric.Methods.Single(m => m.Name == "Add");
            ilProcessor.Emit(OpCodes.Call, MakeGeneric(listAdd, timer));

            var incrementStart = ilProcessor.Create(OpCodes.Ldloc, loopIterator);
            ilProcessor.Emit(OpCodes.Br, incrementStart);

            ilProcessor.Append(setNextRunToMax);
            ilProcessor.Emit(OpCodes.Ldc_I8, long.MaxValue);
            ilProcessor.Emit(OpCodes.Stfld, timer.Fields.Single(f => f.Name == "next_run"));

            ilProcessor.Append(incrementStart);
            ilProcessor.Emit(OpCodes.Ldc_I4_1);
            ilProcessor.Emit(OpCodes.Add);
            ilProcessor.Emit(OpCodes.Stloc, loopIterator);
            ilProcessor.Append(loopCheck);
            ilProcessor.Emit(OpCodes.Ldloc, loopEnd);
            ilProcessor.Emit(OpCodes.Blt, loopStart);

            var loopCheck2 = ilProcessor.Create(OpCodes.Ldloc, loopIterator);
            ilProcessor.Append(loop2);
            var listCount = listGeneric.Methods.Single(m => m.Name == "get_Count");
            ilProcessor.Emit(OpCodes.Call, MakeGeneric(listCount, timer));
            ilProcessor.Emit(OpCodes.Stloc, loopEnd);

            ilProcessor.Emit(OpCodes.Ldc_I4_0);
            ilProcessor.Emit(OpCodes.Stloc, loopIterator);
            ilProcessor.Emit(OpCodes.Br_S, loopCheck2);

            ilProcessor.Append(loopStart2);
            ilProcessor.Emit(OpCodes.Ldloc, list);
            ilProcessor.Emit(OpCodes.Ldloc, loopIterator);
            var getItems = listGeneric.Methods.Single(m => m.Name == "get_Item");
            ilProcessor.Emit(OpCodes.Callvirt, MakeGeneric(getItems, timer));
            ilProcessor.Emit(OpCodes.Call, timerScheduler.Methods.Single(m => m.Name == "Add"));

            ilProcessor.Emit(OpCodes.Ldloc, loopIterator);
            ilProcessor.Emit(OpCodes.Ldc_I4_1);
            ilProcessor.Emit(OpCodes.Add);
            ilProcessor.Emit(OpCodes.Stloc, loopIterator);

            ilProcessor.Append(loopCheck2);
            ilProcessor.Emit(OpCodes.Ldloc, loopEnd);
            ilProcessor.Emit(OpCodes.Blt, loopStart2);

            ilProcessor.Emit(OpCodes.Ldloc, instance);
            ilProcessor.Emit(OpCodes.Ldloc, list);
            ilProcessor.Emit(OpCodes.Dup);
            var clearList = listGeneric.Methods.Single(m => m.Name == "Clear");
            ilProcessor.Emit(OpCodes.Call, MakeGeneric(clearList, timer));
            ilProcessor.Emit(OpCodes.Ldc_I4, 512);
            ilProcessor.Emit(OpCodes.Call, shrinkIfNeededMethod);

            var afterCapacityCheck = ilProcessor.Create(OpCodes.Ldloc, loopEnd);

            ilProcessor.Emit(OpCodes.Ldloc, sortedList);
            ilProcessor.Emit(OpCodes.Callvirt, sortedListType.Methods.Single(m => m.Name == "get_Capacity"));
            ilProcessor.Emit(OpCodes.Stloc, loopIterator);
            ilProcessor.Emit(OpCodes.Ldloc, sortedList);
            ilProcessor.Emit(OpCodes.Callvirt, sortedListType.Methods.Single(m => m.Name == "get_Count"));
            ilProcessor.Emit(OpCodes.Stloc, loopEnd);
            ilProcessor.Emit(OpCodes.Ldloc, loopIterator);
            ilProcessor.Emit(OpCodes.Ldc_I4, 1024);
            ilProcessor.Emit(OpCodes.Blt_S, afterCapacityCheck);

            ilProcessor.Emit(OpCodes.Ldloc, loopEnd);
            ilProcessor.Emit(OpCodes.Ldc_I4_0);
            ilProcessor.Emit(OpCodes.Ble_S, afterCapacityCheck);

            ilProcessor.Emit(OpCodes.Ldloc, loopIterator);
            ilProcessor.Emit(OpCodes.Ldloc, loopEnd);
            ilProcessor.Emit(OpCodes.Div);
            ilProcessor.Emit(OpCodes.Ldc_I4_3);
            ilProcessor.Emit(OpCodes.Ble_S, afterCapacityCheck);

            ilProcessor.Emit(OpCodes.Ldloc, sortedList);
            ilProcessor.Emit(OpCodes.Ldloc, loopEnd);
            ilProcessor.Emit(OpCodes.Ldc_I4_2);
            ilProcessor.Emit(OpCodes.Mul);
            ilProcessor.Emit(OpCodes.Callvirt, sortedListType.Methods.Single(m => m.Name == "set_Capacity"));

            ilProcessor.Append(afterCapacityCheck);
            ilProcessor.Emit(OpCodes.Ldc_I4_0);
            ilProcessor.Emit(OpCodes.Ble_S, ret);

            ilProcessor.Emit(OpCodes.Ldloc, sortedList);
            ilProcessor.Emit(OpCodes.Ldc_I4_0);
            ilProcessor.Emit(OpCodes.Call, sortedListType.Methods.Single(m => m.Name == "GetByIndex"));
            ilProcessor.Emit(OpCodes.Stloc, currentTimer);
            ilProcessor.Emit(OpCodes.Ldloc, currentTimer);
            ilProcessor.Emit(OpCodes.Ldfld, timer.Fields.Single(f => f.Name == "next_run"));
            ilProcessor.Emit(OpCodes.Call, getTimeMonotonic);
            ilProcessor.Emit(OpCodes.Sub);
            ilProcessor.Emit(OpCodes.Ldc_I4, 10000);
            ilProcessor.Emit(OpCodes.Div);
            ilProcessor.Emit(OpCodes.Stloc, periodMs);

            var updateTimerLabel = ilProcessor.Create(OpCodes.Ldloc, periodMs);
            var setMaxTime = ilProcessor.Create(OpCodes.Ldc_I8, 2147483646L);

            ilProcessor.Emit(OpCodes.Ldloc, periodMs);
            ilProcessor.Emit(OpCodes.Ldc_I8, 2147483647L);
            ilProcessor.Emit(OpCodes.Bgt_S, setMaxTime);

            ilProcessor.Emit(OpCodes.Ldloc, periodMs);
            ilProcessor.Emit(OpCodes.Ldc_I4_0);
            ilProcessor.Emit(OpCodes.Conv_I8);
            ilProcessor.Emit(OpCodes.Bgt_S, updateTimerLabel);
            ilProcessor.Emit(OpCodes.Ldc_I4_0);
            ilProcessor.Emit(OpCodes.Conv_I8);
            ilProcessor.Emit(OpCodes.Stloc, periodMs);
            ilProcessor.Emit(OpCodes.Br_S, updateTimerLabel);

            ilProcessor.Append(setMaxTime);
            ilProcessor.Emit(OpCodes.Stloc, periodMs);

            ilProcessor.Append(updateTimerLabel);
            ilProcessor.Emit(OpCodes.Call, updateTimer);

            ilProcessor.Append(ret);

            var token = System.Text.Encoding.UTF8.GetBytes("System.Action");
            var blob = new byte[5 + token.Length];
            blob[0] = 1;
            blob[2] = (byte)token.Length;
            System.Array.Copy(token, 0, blob, 3, token.Length);

            processTimerMethods.CustomAttributes.Add(new CustomAttribute(monoPinvokeAttr.Methods.Single(m => m.IsConstructor), blob));
            return processTimerMethods;
        }

        public static TypeReference MakeGenericType(TypeReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
                throw new System.ArgumentException();

            var instance = new GenericInstanceType(self);
            foreach (var argument in arguments)
                instance.GenericArguments.Add(argument);

            return instance;
        }

        public static MethodReference MakeGeneric(MethodReference self, params TypeReference[] arguments)
        {
            var reference = new MethodReference(self.Name, self.ReturnType)
            {
                DeclaringType = MakeGenericType(self.DeclaringType, arguments),
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention,
            };

            foreach (var parameter in self.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            foreach (var generic_parameter in self.GenericParameters)
                reference.GenericParameters.Add(new GenericParameter(generic_parameter.Name, reference));

            return reference;
        }

        private static TypeDefinition AddMonoPInvokeCallbackAttribute(ModuleDefinition moduleDefinition)
        {
            var type = new TypeDefinition("AOT", "MonoPInvokeCallbackAttribute", TypeAttributes.AnsiClass | TypeAttributes.AutoClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public)
            {
                BaseType = moduleDefinition.ImportReference(moduleDefinition.Types.First(t => t.FullName == "System.Attribute"))
            };
            var ctor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                moduleDefinition.TypeSystem.Void
            );
            ctor.Parameters.Add(new ParameterDefinition("type", ParameterAttributes.None, moduleDefinition.ImportReference(moduleDefinition.Types.First(t => t.FullName == "System.Type"))));
            ctor.Body.GetILProcessor().Emit(OpCodes.Ret);

            type.Methods.Add(ctor);

            moduleDefinition.Types.Add(type);
            return type;
        }

        private static bool TryGetTypes(
            ModuleDefinition moduleDefinition,
            out TypeDefinition threadPool,
            out TypeDefinition synchronizationContext,
            out TypeDefinition sendOrPostCallback,
            out TypeDefinition waitCallback,
            out TypeDefinition threadPoolWorkItem,
            out TypeDefinition timerScheduler
        )
        {
            threadPool = null;
            synchronizationContext = null;
            sendOrPostCallback = null;
            waitCallback = null;
            threadPoolWorkItem = null;
            timerScheduler = null;

            foreach (var type in moduleDefinition.Types)
            {
                if (type.FullName == "System.Threading.ThreadPool")
                    threadPool = type;
                if (type.FullName == "System.Threading.SynchronizationContext")
                    synchronizationContext = type;
                if (type.FullName == "System.Threading.SendOrPostCallback")
                    sendOrPostCallback = type;
                if (type.FullName == "System.Threading.WaitCallback")
                    waitCallback = type;
                if (type.FullName == "System.Threading.IThreadPoolWorkItem")
                    threadPoolWorkItem = type;
                if (type.FullName == "System.Threading.Timer")
                    foreach (var nested in type.NestedTypes)
                        if (nested.FullName.Contains("Scheduler"))
                            timerScheduler = nested;
            }

            return CheckTypeAssigned("System.Threading.ThreadPool", threadPool)
                && CheckTypeAssigned("System.Threading.SynchronizationContext", synchronizationContext)
                && CheckTypeAssigned("System.Threading.SendOrPostCallback", sendOrPostCallback)
                && CheckTypeAssigned("System.Threading.WaitCallback", waitCallback)
                && CheckTypeAssigned("System.Threading.IThreadPoolWorkItem", threadPoolWorkItem)
                && CheckTypeAssigned("System.Threading.Timer.Scheduler", timerScheduler);

            bool CheckTypeAssigned(string name, TypeDefinition type)
            {
                if (type != null)
                    return true;

                Debug.LogError("Can't find " + name);
                return false;
            }
        }
    }
}
