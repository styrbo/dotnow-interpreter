using System;
using System.Collections.Generic;
using System.Reflection;
using dotnow;
using dotnow.Interop;
using UnityEngine.Profiling;
using AppDomain = dotnow.AppDomain;

namespace UnityEngine
{
    [CLRProxyBinding(typeof(MonoBehaviour))]
    public class MonoBehaviourProxy : MonoBehaviour, ICLRProxy
    {
        private class MonoBehaviourEvents
        {
            public MonoBehaviourEvents(CLRType type)
            {
                AwakeHook = type.GetMethod("Awake", bindings);
                StartHook = type.GetMethod("Start", bindings);
                OnDestroyHook = type.GetMethod("OnDestroy", bindings);
                OnEnableHook = type.GetMethod("OnEnable", bindings);
                OnDisableHook = type.GetMethod("OnDisable", bindings);
                UpdateHook = type.GetMethod("Update", bindings);
                LateUpdateHook = type.GetMethod("LateUpdate", bindings);
                FixedUpdateHook = type.GetMethod("FixedUpdate", bindings);
                OnCollisionEnterHook = type.GetMethod("OnCollisionEnter", bindings);
                OnCollisionStayHook = type.GetMethod("OnCollisionStay", bindings);
                OnCollisionExitHook = type.GetMethod("OnCollisionExit", bindings);
            }
            
            private static BindingFlags bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            public MethodBase AwakeHook { get; }
            public MethodBase StartHook { get; }
            public MethodBase OnDestroyHook { get; }
            public MethodBase OnEnableHook { get; }
            public MethodBase OnDisableHook { get; }
            public MethodBase UpdateHook { get; }
            public MethodBase LateUpdateHook { get; }
            public MethodBase FixedUpdateHook { get; }
            public MethodBase OnCollisionEnterHook { get; }
            public MethodBase OnCollisionStayHook { get; }
            public MethodBase OnCollisionExitHook { get; }
        }

        private static Dictionary<CLRType, MonoBehaviourEvents> _typesCache;

        // Private
        private AppDomain domain = null;
        private CLRType instanceType = null;
        private CLRInstance instance = null;
        private MonoBehaviourEvents _events;

        // Methods
        public void InitializeProxy(AppDomain domain, CLRInstance instance)
        {
            if (_typesCache == null)
                _typesCache = new Dictionary<CLRType, MonoBehaviourEvents>();

            if (_typesCache.TryGetValue(instance.Type, out var value))
                _events = value;
            else
            {
                _events = new MonoBehaviourEvents(instance.Type);
                _typesCache.Add(instance.Type, _events);
            }
            
            this.domain = domain;
            this.instanceType = instance.Type;
            this.instance = instance;

            // Manually call awake and OnEnable since they will do nother when called by Unity
            Awake();
            OnEnable();
        }

        public void Awake()
        {
            if (domain == null)
                return;

            if (_events != null && _events.AwakeHook != null)
                _events.AwakeHook.Invoke(instance, null);
        }

        public void Start()
        {
            if (_events != null && _events.StartHook != null)
                _events.StartHook.Invoke(instance, null);
        }

        public void OnDestroy()
        {
            if (_events != null && _events.OnDestroyHook != null)
                _events.OnDestroyHook.Invoke(instance, null);
        }

        public void OnEnable()
        {
            if (domain == null)
                return;

            if (_events != null && _events.OnEnableHook != null)
                _events.OnEnableHook.Invoke(instance, null);
        }

        public void OnDisable()
        {
            if (_events != null && _events.OnDisableHook != null)
                _events.OnDisableHook.Invoke(instance, null);
        }

        public void Update()
        {
            if (_events != null && _events.UpdateHook != null)
                _events.UpdateHook.Invoke(instance, null);
        }

        public void LateUpdate()
        {
            if (_events != null && _events.LateUpdateHook != null)
                _events.LateUpdateHook.Invoke(instance, null);
        }

        public void FixedUpdate()
        {
            if (_events != null && _events.FixedUpdateHook != null)
                _events.FixedUpdateHook.Invoke(instance, null);
        }

        public void OnCollisionEnter(Collision collision)
        {
            if(_events != null && _events.OnCollisionEnterHook != null)
                _events.OnCollisionEnterHook.Invoke(instance, new object[] {collision});
        }

        public void OnCollisionStay(Collision collision)
        {
            if (_events != null && _events.OnCollisionStayHook != null)
                _events.OnCollisionStayHook.Invoke(instance, new object[] {collision});
        }

        public void OnCollisionExit(Collision collision)
        {
            if (_events != null && _events.OnCollisionExitHook != null)
                _events.OnCollisionExitHook.Invoke(instance, new object[] {collision});
        }

        [CLRMethodBinding(typeof(GameObject), "AddComponent", typeof(Type))]
        public static object AddComponentOverride(AppDomain domain, MethodInfo overrideMethod, object instance,
            object[] args)
        {
            // Get instance
            GameObject go = instance as GameObject;

            // Get argument
            Type componentType = args[0] as Type;

            // Check for clr type
            if (componentType.IsCLRType() == false)
            {
                // Use default unity behaviour
                return go.AddComponent(componentType);
            }

            // Handle add component manually
            Type proxyType = domain.GetCLRProxyBindingForType(componentType.BaseType);

            // Validate type
            if (typeof(MonoBehaviour).IsAssignableFrom(proxyType) == false)
                throw new InvalidOperationException("A type deriving from mono behaviour must be provided");

            // Create proxy instance
            ICLRProxy proxyInstance = (ICLRProxy) go.AddComponent(proxyType);

            // Create clr instance
            return domain.CreateInstanceFromProxy(componentType, proxyInstance);
        }

        [CLRMethodBinding(typeof(GameObject), "GetComponent", typeof(Type))]
        public static object GetComponentOverride(AppDomain domain, MethodInfo overrideMethod, object instance,
            object[] args)
        {
            // Get instance
            GameObject go = instance as GameObject;

            // Get argument
            Type componentType = args[0] as Type;

            // Check for clr type
            if (componentType.IsCLRType() == false)
            {
                // Use default unity behaviour
                return go.GetComponent(componentType);
            }

            // Get proxy types
            foreach (MonoBehaviourProxy proxy in go.GetComponents<MonoBehaviourProxy>())
            {
                if (proxy.instanceType == componentType)
                {
                    return proxy.instance;
                }
            }

            return null;
        }
    }
}