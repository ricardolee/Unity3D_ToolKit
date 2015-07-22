using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FSM
{
    public class EventDispatcher : MonoBehaviour {

        
        void Awake() {
            List<MonoBehaviour> scripts = new List<MonoBehaviour>();
            GetComponentsInChildren(true, scripts);
            foreach (MonoBehaviour script in scripts)
            {
                MethodInfo[] methods = script.GetType().GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (MethodInfo method in methods)
                {
                    foreach (EventAttribute ea in method.GetCustomAttributes(typeof(EventAttribute), true))
                    {
                        
                    }
                }
            }
        }
        
        public void On(string eventName, Action action) {
            Register(eventName, action, (arg1,arg2,arg3,arg4) => {
                    action();
                    return true;
                });
        }

        public void On<T1>(string eventName, Action<T1> action) {
            Register(eventName, action, (arg1,arg2,arg3,arg4) => {
                    T1		param1;
                    try { param1 = (T1)arg1; } catch { param1 = default(T1); }
                    action(param1);
                    return true;
                });
        }

        public void On<T1,T2>(string eventName, Action<T1,T2> action) {
            Register(eventName, action, (arg1,arg2,arg3,arg4) => {
                    T1		param1;
                    T2		param2;
                    try { param1 = (T1)arg1; } catch { param1 = default(T1); }
                    try { param2 = (T2)arg2; } catch { param2 = default(T2); }
                    action(param1, param2);
                    return true;
                });
        }

        public void On<T1,T2,T3>(string eventName, Action<T1,T2,T3> action) {
            Register(eventName, action, (arg1,arg2,arg3,arg4) => {
                    T1		param1;
                    T2		param2;
                    T3		param3;
                    try { param1 = (T1)arg1; } catch { param1 = default(T1); }
                    try { param2 = (T2)arg2; } catch { param2 = default(T2); }
                    try { param3 = (T3)arg3; } catch { param3 = default(T3); }
                    action(param1, param2, param3);
                    return true;
                });
        }


        public void On(string eventName, Func<bool> action) {
            Register(eventName, action, (arg1,arg2,arg3,arg4) => {
                    return action();
                });
        }

        public void On<T1>(string eventName, Func<T1,bool> action) {
            Register(eventName, action, (arg1,arg2,arg3,arg4) => {
                    T1		param1;
                    try { param1 = (T1)arg1; } catch { param1 = default(T1); }
                    return action(param1) ;
                });
        }
        
        public void On<T1,T2>(string eventName, Func<T1,T2,bool> action) {
            Register(eventName, action, (arg1,arg2,arg3,arg4) => {
                    T1		param1;
                    T2		param2;
                    try { param1 = (T1)arg1; } catch { param1 = default(T1); }
                    try { param2 = (T2)arg2; } catch { param2 = default(T2); }
                    return action(param1, param2);
                });
        }

        public void On<T1,T2,T3>(string eventName, Func<T1,T2,T3,bool> action) {
            Register(eventName, action, (arg1,arg2,arg3,arg4) => {
                    T1		param1;
                    T2		param2;
                    T3		param3;
                    try { param1 = (T1)arg1; } catch { param1 = default(T1); }
                    try { param2 = (T2)arg2; } catch { param2 = default(T2); }
                    try { param3 = (T3)arg3; } catch { param3 = default(T3); }
                    return(action(param1, param2, param3));
                });
        }

        public void On<T1,T2,T3,T4>(string eventName, Func<T1,T2,T3,T4,bool> action) {
            Register(eventName, action, (arg1,arg2,arg3,arg4) => {
                    T1		param1;
                    T2		param2;
                    T3		param3;
                    T4          param4;
                    try { param1 = (T1)arg1; } catch { param1 = default(T1); }
                    try { param2 = (T2)arg2; } catch { param2 = default(T2); }
                    try { param3 = (T3)arg3; } catch { param3 = default(T3); }
                    try { param4 = (T4)arg4; } catch { param4 = default(T4); }
                    return(action(param1, param2, param3, param4));
                });
        }
        
        public void Trigger(string eventName) {
            Call(eventName, null, null, null, null);
        }

        public void Trigger(string eventName, object param1) {
            Call(eventName, param1, null, null, null);
        }

        public void Trigger(string eventName, object param1, object param2) {
            Call(eventName, param1, param2, null, null);
        }

        public void Trigger(string eventName, object param1, object param2, object param3) {
            Call(eventName, param1, param2, param3, null);
        }
        
        public void Trigger(string eventName, object param1, object param2, object param3, object param4) {
            Call(eventName, param1, param2, param3, param4);
        }

        public void Cancel(string eventName, Delegate action) {
            var key = Key.Create(eventName, action);
            Listener listener = mListenerLookup[key];
            if (listener == null)
            {
                throw new Exception("Action " + action + " not registered on Event " + eventName);
            }
            mRegisteredEvents[eventName].RemoveAll((l) => l.mAction == action);
            mRegisteredDelegate[action].RemoveAll((l) => l.mEventName == eventName);
            mListenerLookup.Remove(key);
        }

        public void Cancel(string eventName)
        {
            List<Listener> list = mRegisteredEvents[eventName];
            if (list == null)
            {
                throw new Exception("Event: " + eventName + " not registered");
            } 
            foreach(Listener listener in list)
            {
                mListenerLookup.Remove(Key.Create(listener.mEventName, listener.mOrigin));
                mRegisteredDelegate[listener.mOrigin].RemoveAll((l) => l.mEventName == eventName);
            }
            list.Clear();
        }

        public void Cancel(Delegate action)
        {
            List<Listener> list = mRegisteredDelegate[action];
            if (list == null)
            {
                throw new Exception("Action: " + action + " not registered");
            } 
            foreach(Listener listener in list)
            {
                mListenerLookup.Remove(Key.Create(listener.mEventName, listener.mOrigin));
                mRegisteredEvents[listener.mEventName].RemoveAll((l) => l.mAction == action);
            }
            list.Clear();
            
        }

        void Register(string eventName, Delegate origin, Func<object,object,object,object,bool> action)
        {
            Listener  listener = new Listener();
            listener.mEventName = eventName;
            listener.mOrigin = origin;
            listener.mAction = action;
            List<Listener> listenerList;
            if (!mRegisteredEvents.TryGetValue(eventName, out listenerList))
            {
                listenerList = new List<Listener>();
            }
            listenerList.Insert(0, listener);
            if (!mRegisteredDelegate.TryGetValue(origin, out listenerList))
            {
                listenerList = new List<Listener>();
            }
            listenerList.Add(listener);
            mListenerLookup.Add(Key.Create(eventName, origin), listener);
        }

        void Call(String eventName, object param1, object param2, object param3, object param4)
        {
            List<Listener> listenerList = mRegisteredEvents[eventName];
            foreach(Listener listener in listenerList)
            {
                if(!listener.mAction(param1, param2, param3, param4))
                {
                    break;
                }
            }
        }

        private class Listener
        {
            public string mEventName;
            public Delegate mOrigin;
            public Func<object,object,object,object,bool> mAction;
        }

        private struct Key
        {
            public static Key Create(string eventName, Delegate origin)
            {
                Key key = new Key();
                key.mEventName = eventName;
                key.mOrigin = origin;
                return key;
            }
            
            string mEventName;
            Delegate mOrigin;
            
            
            public override bool Equals(object obj)
            {
                if (!(obj is Key))
                {
                    return false;
                }
                
                Key other = (Key)obj;
                
                if(other.mEventName == mEventName && other.mOrigin == mOrigin)
                {
                    return true;
                }
                else 
                {
                    return false;
                }

            }

            public override int GetHashCode()
            {
                return (mEventName + mOrigin).GetHashCode();
            }

            public override string ToString()
            {
                return "Event: " + mEventName + " Delegate: " + mOrigin;
            }
            
        }
        
        Dictionary<string, List<Listener>> mRegisteredEvents  = new Dictionary<string, List<Listener>>();
        Dictionary<Delegate, List<Listener>> mRegisteredDelegate = new Dictionary<Delegate, List<Listener>>();
        Dictionary<Key, Listener> mListenerLookup = new Dictionary<Key, Listener>();
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class EventAttribute : Attribute
    {
        private string _name;

        public string name
        {
            get { return _name; }
            set { _name = value; }
        }
    }

}