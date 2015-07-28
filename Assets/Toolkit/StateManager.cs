using UnityEngine;
using System;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Toolkit
{

    public class StateManager : MonoBehaviour
    {
        public bool _AutoManageChildren = true;
        private Dictionary<Type, System.Object> _StateMachineLookup = new Dictionary<Type, System.Object>();
        private static BindingFlags _MethodBindingFlags = BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;

        void Awake()
        {
            List<MonoBehaviour> scripts = new List<MonoBehaviour>();
            GetComponentsInChildren(_AutoManageChildren, scripts);
            foreach (MonoBehaviour script in scripts)
            {
                MethodInfo[] methods = script.GetType().GetMethods(_MethodBindingFlags);
                foreach (MethodInfo method in methods)
                {
                    foreach (StateListenerAttribute sa in method.GetCustomAttributes(typeof(StateListenerAttribute), true))
                    {
                        System.Object state = sa.state;
                        Type stateType = state.GetType();
                        System.Object stateMachine;

                        if (!_StateMachineLookup.TryGetValue(stateType, out stateMachine))
                        {
                            Type stateMachineType = Type.GetType("Toolkit.StateMachine`1[" + stateType + "]");
                            stateMachine = Activator.CreateInstance(stateMachineType);
                            _StateMachineLookup.Add(stateType, stateMachine);
                        }
                        System.Object stateLookup = stateMachine.GetType().GetField("mStateLookup", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(stateMachine);
                        StateMapping stateMapping = (StateMapping)stateLookup.GetType().GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public).Invoke(stateLookup, new object[] { state });
                        switch (sa.on)
                        {
                            case StateEvent.Enter:
                                stateMapping.Enter = CreateDelegate<Action>(method, script);
                                break;
                            case StateEvent.Exit:
                                stateMapping.Exit = CreateDelegate<Action>(method, script);
                                break;
                            case StateEvent.Finally:
                                stateMapping.Finally = CreateDelegate<Action>(method, script);
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
            return ((StateMachine<T>)_StateMachineLookup[typeof(T)]);
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
        public Action Finally = DoNothing;
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

    public enum StateEvent
    {
        Enter, Exit, Finally, Update, LateUpdate, FixedUpdate
    }

}