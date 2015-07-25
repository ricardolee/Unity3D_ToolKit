using UnityEngine;
using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

namespace Toolkit
{
    
    [RequireComponent(typeof(EventDispatcher))]
    public class StateManager : MonoBehaviour
    {
        [HideInInspector]
        EventDispatcher mEvents;
        public EventDispatcher Events { get { return mEvents; } }

        private BindingFlags mMethodBindingFlags = BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;
        void Awake()
        {
            mEvents = GetComponent<EventDispatcher>();
            List<MonoBehaviour> scripts = new List<MonoBehaviour>();
            GetComponentsInChildren(true, scripts);
            foreach (MonoBehaviour script in scripts)
            {
                MethodInfo[] methods = script.GetType().GetMethods(mMethodBindingFlags);
                foreach (MethodInfo method in methods)
                {
                    foreach (StateListenerAttribute sa in method.GetCustomAttributes(typeof(StateListenerAttribute), true))
                    {
                        mEvents.Register(GetEventName(sa.state, sa.when, sa.on), method, script, 1000, false);
                    }
                }
            }
        }
        
        public static String GetEventName(string state, string when, string on)
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

        public StateMachine GetStateMachine(string stateName) {
            StateMachine sm;
            if (!mStateMachineLookup.TryGetValue(stateName, out sm))
            {
                sm = new StateMachine(mEvents, stateName);
                mStateMachineLookup.Add(stateName, sm);
            }
            return sm;
        }
        public bool ChangeState(string stateName, string when)
        {
            return GetStateMachine(stateName).ChangeState(when);
        }

        public string GetCurrentState(string stateName)
        {
            return GetStateMachine(stateName).mCurrent;
        }
        
        public void Trigger(string stateName, string on, params object[] args)
        {
            StateMachine sm;
            if(mStateMachineLookup.TryGetValue(stateName, out sm))
            {
                sm.Trigger(on);
            }

        }
        
        private const String EVENT_PREFIX = "FSM";

        private Dictionary<string, StateMachine> mStateMachineLookup = new Dictionary<string, StateMachine>();
    }
    
    public static class StateCallback
    {
        public const string Enter = "Enter";
        public const string Exit = "Exit";
    }

    public class StateMachine
    {

        public StateMachine(EventDispatcher events, string stateName)
        {
            this.mEvents = events;
            this.mStateName = stateName;
        }
        public EventDispatcher mEvents;
        // public EventDispatcher Events { get { return mEvents; } }
        public string mStateName;
        public string mCurrent;
        public Dictionary<string, EventTrigger> mCurrentTriggerLookup;
        public Dictionary<string, Dictionary<string, EventTrigger>> mTriggerDictLoockup = new Dictionary<string, Dictionary<string, EventTrigger>>();
        public void Trigger(string on, params object[] args) {
            if (mCurrent == null)
            {
                return;
            }
            EventTrigger trigger;
            if(!mCurrentTriggerLookup.TryGetValue(on, out trigger))
            {
                trigger = mEvents.GetEventTrigger(StateManager.GetEventName(mStateName, mCurrent, on));
                mCurrentTriggerLookup.Add(on, trigger);
            }
            trigger(args);
        }

        public bool ChangeState(string when)
        {
            if (when == null || when == mCurrent) {
                return false;
            }
            Trigger(StateCallback.Exit);
            mCurrent = when;
            if(!mTriggerDictLoockup.TryGetValue(mCurrent, out mCurrentTriggerLookup))
            {
                mCurrentTriggerLookup = new Dictionary<string, EventTrigger>();
                mTriggerDictLoockup.Add(mCurrent, mCurrentTriggerLookup);
            }
            Trigger(StateCallback.Enter);
            return true;
        }
    }


    [AttributeUsage(AttributeTargets.Method)]
    public class StateListenerAttribute : Attribute
    {
        private string _state;
        private string _when;
        private string _on = StateCallback.Enter;

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