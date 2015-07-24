using UnityEngine;
using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

namespace Toolkit
{
    [RequireComponent(typeof(EventDispatcher))]
    public class FiniteStateMachine : MonoBehaviour
    {
        [HideInInspector]
        public EventDispatcher events;

        private BindingFlags mMethodBindingFlags = BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;
        void Awake() {
            events = GetComponent<EventDispatcher>();
            List<MonoBehaviour> scripts = new List<MonoBehaviour>();
            GetComponentsInChildren(true, scripts);
            foreach (MonoBehaviour script in scripts)
            {
                MethodInfo[] methods = script.GetType().GetMethods(mMethodBindingFlags);
                foreach (MethodInfo method in methods)
                {
                    foreach (StateListenerAttribute sa in method.GetCustomAttributes(typeof(StateListenerAttribute), true))
                    {
                        events.Register(GetEventName(sa.state, sa.when, sa.on), method, script, 1000, false);
                    }
                }
            }
        }

        public String GetEventName(string state, string when, string on)
        {
            StringBuilder b = new StringBuilder();
            b.Append(EVENT_PREFIX);
            b.Append('_');
            b.Append(state);
            b.Append('_');
            b.Append(when);
            b.Append('_');
            b.Append(on);
            return b.ToString();
        }
        
        public bool ChangeState(string state, string when)
        {
            string stateCurrent;
            if(mStateWhen.TryGetValue(state, out stateCurrent))
            {
                if (stateCurrent == when )
                {
                    return false;
                }
                events.Trigger(GetEventName(state, stateCurrent, StateCallback.Exit));
                mStateWhen[state] = when;
            }
            else
            {
                mStateWhen.Add(state, when);
            }
            events.Trigger(GetEventName(state, when, StateCallback.Enter));
            return true;
        }

        public string GetCurrentState(string state)
        {
            string stateCurrent;
            if(mStateWhen.TryGetValue(state, out stateCurrent))
            {
                return stateCurrent;
            }
            else
            {
                return null;
            }
        }

        public void Trigger(string state, string on)
        {
            string cur = GetCurrentState(state);
            if (cur != null)
            {
                events.Trigger(GetEventName(state, cur, on));
            }
        }
        
        public static class StateCallback
        {
            public const string Enter = "Enter";
            public const string Exit = "Exit";
        }

        private const String EVENT_PREFIX = "FSM";
        private Dictionary<string, string> mStateWhen = new Dictionary<string, string>();
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class StateListenerAttribute : Attribute
    {
        private string _state;
        private string _when;
        private string _on = FiniteStateMachine.StateCallback.Enter;

        public string state
        {
            get { return _state; }
            set { _state = value; }
        }

        public string when
        {
            get { return _when; }
            set { _when = value; }
        }

        public string on
        {
            get { return _on; }
            set { _on = value; }
        }
    }
}