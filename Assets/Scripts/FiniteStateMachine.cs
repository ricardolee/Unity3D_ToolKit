using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace FSM
{
    public class FiniteStateMachine
    {

        private StateMapping currentState;
        private StateMapping destinationState;
        private Dictionary<Enum, StateMapping> stateLookup;
        private bool isInTransition = false;
        private IEnumerator currentTransitioin;
        private IEnumerator exitRoutine;
        private IEnumerator enterRoutine;
        private IEnumerator queuedChange;
        private MonoBehaviour script;
        private Type enumType;
  
        public void Init(MonoBehaviour script, Enum initState)
        {
            this.script = script;
            enumType = initState.GetType();
            Array values = Enum.GetValues(enumType);
            stateLookup = new Dictionary<Enum, StateMapping>();
            foreach (Enum v in values)
            {
                StateMapping mapping = new StateMapping(v);
                stateLookup.Add(v, mapping);
            }


            MethodInfo[] methods = script.GetType().GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo method in methods)
            {
                foreach (StateBehaviourAttribute sba in method.GetCustomAttributes(typeof(StateBehaviourAttribute), true))
                {
                    StateMapping targetState = stateLookup[(Enum)Enum.Parse(enumType, sba.state)];

                    switch (sba.on)
                    {
                        case StateCallback.Enter:
                            if (method.ReturnType == typeof(IEnumerator))
                            {
                                targetState.Enter = CreateDelegate<Func<IEnumerator>>(method, script);
                            }
                            else
                            {
                                Action action = CreateDelegate<Action>(method, script);
                                targetState.Enter = () =>
                                {
                                    action();
                                    return null;
                                };
                            }
                            break;
                        case StateCallback.Exit:
                            if (method.ReturnType == typeof(IEnumerator))
                            {
                                targetState.Exit = CreateDelegate<Func<IEnumerator>>(method, script);
                            }
                            else
                            {
                                Action action = CreateDelegate<Action>(method, script);
                                targetState.Exit = () =>
                                {
                                    action();
                                    return null;
                                };
                            }
                            break;
                        case StateCallback.Finally:
                            targetState.Finally = CreateDelegate<Action>(method, script);
                            break;
                        case StateCallback.Update:
                            targetState.Update = CreateDelegate<Action>(method, script);
                            break;
                        case StateCallback.LateUpdate:
                            targetState.LateUpdate = CreateDelegate<Action>(method, script);
                            break;
                        case StateCallback.FixedUpdate:
                            targetState.FixedUpdate = CreateDelegate<Action>(method, script);
                            break;
                    }
                }
            }
            ChangeState(initState);
        }

        public void ChangeState(Enum newState, StateTransition transition = StateTransition.Safe)
        {
            StateMapping nextState = stateLookup[newState];
            if (currentState == nextState)
                return;
            if (queuedChange != null)
            {
                script.StopCoroutine(queuedChange);
                queuedChange = null;
            }

            switch (transition)
            {
                case StateTransition.Safe:
                    if (isInTransition)
                    {
                        if (exitRoutine != null)
                        {
                            destinationState = nextState;
                            return;
                        }

                        if (enterRoutine != null)
                        {
                            queuedChange = WaitForPreviousTransition(nextState);
                            script.StartCoroutine(queuedChange);
                            return;
                        }
                    }
                    break;
                case StateTransition.Overwrite:
                    if (currentTransitioin != null)
                    {
                        script.StopCoroutine(currentTransitioin);
                    }
                    if (exitRoutine != null)
                    {
                        script.StopCoroutine(exitRoutine);
                    }
                    if (enterRoutine != null)
                    {
                        script.StopCoroutine(enterRoutine);
                    }

                    if (currentState != null)
                        currentState.Finally();
                    currentState = null;
                    break;
            }
            isInTransition = true;
            currentTransitioin = ChangeToNewStateRouting(nextState);
            script.StartCoroutine(currentTransitioin);
        }

        private IEnumerator ChangeToNewStateRouting(StateMapping newState)
        {
            destinationState = newState;
            if (currentState != null)
            {
                exitRoutine = currentState.Exit();
                if (exitRoutine != null)
                {
                    yield return script.StartCoroutine(exitRoutine);
                }
                exitRoutine = null;
                currentState.Finally();
            }

            currentState = destinationState;

            if (currentState != null)
            {
                enterRoutine = currentState.Enter();

                if (enterRoutine != null)
                {
                    yield return script.StartCoroutine(enterRoutine);
                }

                enterRoutine = null;
            }

            isInTransition = false;
        }

        private IEnumerator WaitForPreviousTransition(StateMapping nextState)
        {
            while (isInTransition)
            {
                yield return null;
            }
            ChangeState(nextState.state);
        }


        private V CreateDelegate<V>(MethodInfo method, System.Object target) where V : class
        {
            V ret = Delegate.CreateDelegate(typeof(V), target, method) as V;

            if (ret == null)
            {
                throw new ArgumentException("Unabled to create delegate for method called " + method.Name);
            }
            return ret;
        }

        private class StateMapping
        {
            public Enum state;

            public Func<IEnumerator> Enter = DoNothingCoroutine;
            public Func<IEnumerator> Exit = DoNothingCoroutine;
            public Action Finally = DoNothing;
            public Action Update = DoNothing;
            public Action LateUpdate = DoNothing;
            public Action FixedUpdate = DoNothing;

            public StateMapping(Enum state)
            {
                this.state = state;
            }


            public static void DoNothing()
            {
            }

            public static void DoNothingCollider(Collider other)
            {
            }

            public static void DoNothingCollision(Collision other)
            {
            }

            public static IEnumerator DoNothingCoroutine()
            {
                yield break;
            }
        }

        public void FixedUpdate()
        {
            if (currentState != null)
            {
                currentState.FixedUpdate();
            }
        }

        public void Update()
        {
            if (currentState != null && isInTransition)
            {
                currentState.Update();
            }
        }

        public void LateUpdate()
        {
            if (currentState != null && isInTransition)
            {
                currentState.LateUpdate();
            }
        }
    }

    public enum StateTransition
    {
        Overwrite,
        Safe
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class StateBehaviourAttribute : Attribute
    {
        private string _state;
        private string _on;

        public string state
        {
            get { return _state; }
            set { _state = value; }
        }

        public string on
        {
            get { return _on; }
            set { _on = value; }
        }
    }

    public static class StateCallback
    {
        public const string Enter = "Enter";
        public const string Exit = "Exit";
        public const string Finally = "Finally";
        public const string Update = "Update";
        public const string LateUpdate = "LateUpdate";
        public const string FixedUpdate = "FixedUpdate";
    }
}