using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Toolkit
{

    public class StateManager : MonoBehaviour
    {
        public bool _AutoManageChildren = true;
        private Dictionary<Type, System.Object> _StateMachineLookup = new Dictionary<Type, System.Object>();

        void Awake()
        {
            List<MonoBehaviour> scripts = new List<MonoBehaviour>();
            GetComponentsInChildren(_AutoManageChildren, scripts);
            foreach (MonoBehaviour script in scripts)
            {
                MethodInfo[] methods = script.GetType().GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (MethodInfo method in methods)
                {
                    foreach (StateListenerAttribute sa in method.GetCustomAttributes(typeof(StateListenerAttribute), true))
                    {
                        System.Object state = sa.state;
                        Type stateType = state.GetType();
                        System.Object stateMachine;
                        Type stateMachineType = Type.GetType("Toolkit.StateMachine`1[" + stateType + "]");
                        if (!_StateMachineLookup.TryGetValue(stateMachineType, out stateMachine))
                        {
                            stateMachine = Activator.CreateInstance(stateMachineType);
                            _StateMachineLookup.Add(stateMachineType, stateMachine);
                        }
                        System.Object stateLookup = stateMachine.GetType().GetField("_StateLookup", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(stateMachine);
                        StateMapping stateMapping = (StateMapping)stateLookup.GetType().GetMethod("get_Item", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(stateLookup, new object[] { state });
                        switch (sa.on)
                        {
                            case StateEvent.Enter:
                                if (method.ReturnType == typeof(IEnumerator))
                                {
                                    Func<IEnumerator> func = CreateDelegate<Func<IEnumerator>>(method, script);
                                    stateMapping.Enter = () => { script.StartCoroutine(func());};
                                }
                                else
                                {
                                    stateMapping.Enter = CreateDelegate<Action>(method, script);
                                }
                                break;
                            case StateEvent.Exit:
                                stateMapping.Exit = CreateDelegate<Action>(method, script);
                                break;
                            case StateEvent.FixedUpdate:
                                stateMapping.FixedUpdate = CreateDelegate<Action>(method, script);
                                break;
                            case StateEvent.LateUpdate:
                                stateMapping.LateUpdate = CreateDelegate<Action>(method, script);
                                break;
                            case StateEvent.Update:
                                stateMapping.Update = CreateDelegate<Action>(method, script);
                                break;
                        }
                    }
                }
                FieldInfo[] fields = script.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (FieldInfo field in fields)
                {
                    
                    if(field.GetCustomAttributes(typeof(StateMachineInjectAttribute), true).Length > 0)
                    {
                        if (!field.FieldType.ToString().Contains("StateMachine"))
                        {
                            throw new Exception("The filed type not a state machine");
                        }

                        System.Object stateMachine;
                        Type stateMachineType = field.FieldType;
                        if (!_StateMachineLookup.TryGetValue(stateMachineType, out stateMachine))
                        {
                            stateMachine = Activator.CreateInstance(stateMachineType); ;
                            _StateMachineLookup.Add(stateMachineType, stateMachine);
                        }
                        field.SetValue(script, stateMachine);
                    }
                }
            }
        }


        public bool ChangeState<T>(T state)
         {
            return GetStateMachine<T>().ChangeState(state);
        }

        public T GetCurrentState<T>()
        {
            return GetStateMachine<T>().CurrentState;
        }

        public StateMachine<T> GetStateMachine<T>()
        {
            Type stateMachineType = Type.GetType("Toolkit.StateMachine`1[" + typeof(T) + "]");
            return (StateMachine<T>)_StateMachineLookup[stateMachineType];
        }

        internal V CreateDelegate<V>(MethodInfo method, System.Object target) where V : class
        {
            var ret = (Delegate.CreateDelegate(typeof(V), target, method) as V);

            if (ret == null)
            {
                throw new ArgumentException("Unabled to create delegate for method called " + method.Name);
            }
            return ret;

        }
    }

    public class StateMachine<T>
    {
        T _CurrentState;
        internal StateMapping _CurrentMapping;
        internal Dictionary<T, StateMapping> _StateLookup = new Dictionary<T, StateMapping>();
        public T CurrentState { get { return _CurrentState; } }

        public StateMapping GetMapping(T state)
        {
            return _StateLookup[state];
        }

        public StateMachine()
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }
            var values = Enum.GetValues(typeof(T));
            foreach (var v in values)
            {
                _StateLookup.Add((T)v, new StateMapping());
            }
        }

        public void Init(T initState)
        {
            _CurrentState = initState;
            _CurrentMapping = _StateLookup[initState];
            _CurrentMapping.Enter();
        }
        // Make sure you init the Machine
        public bool ChangeState(T state)
        {
            if (state.Equals(_CurrentState))
            {
                return false;
            }
            else
            {
                _CurrentMapping.Exit();
                _CurrentState = state;
                _CurrentMapping = _StateLookup[state];
                _CurrentMapping.Enter();
                return true;
            }
        }

        public void Update() { _CurrentMapping.Update(); }
        public void LateUpdate() { _CurrentMapping.LateUpdate(); }
        public void FixedUpdate() { _CurrentMapping.FixedUpdate(); }

    }

    public enum StateTransition
    {
        Overwrite,
        Safe
    }

    public class StateMapping
    {
        public Action Enter = DoNothing;
        public Action Exit = DoNothing;
        public Action Update = DoNothing;
        public Action LateUpdate = DoNothing;
        public Action FixedUpdate = DoNothing;

        public static void DoNothing()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class StateListenerAttribute : Attribute
    {
        public System.Object state { get; set; }
        public StateEvent on { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class StateMachineInjectAttribute : Attribute { }


    public enum StateEvent
    {
        Enter, Exit, Finally, Update, LateUpdate, FixedUpdate
    }

}