using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Toolkit
{
    public class EventDispatcher : MonoBehaviour {
        private BindingFlags mMethodBindingFlags = BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;
        void Awake() {
            List<MonoBehaviour> scripts = new List<MonoBehaviour>();
            GetComponentsInChildren(true, scripts);
            foreach (MonoBehaviour script in scripts)
            {
                MethodInfo[] methods = script.GetType().GetMethods(mMethodBindingFlags);
                foreach (MethodInfo method in methods)
                {
                    foreach (EventListenerAttribute ea in method.GetCustomAttributes(typeof(EventListenerAttribute), true))
                    {
                        Register(ea.name, method, script, ea.weight, false);
                    }
                    foreach (EventFilterAttribute ea in method.GetCustomAttributes(typeof(EventFilterAttribute), true))
                    {
                        Register(ea.name, method, script, ea.weight, true);
                    }
                }
            }
        }

        delegate void mAction(object[] args);
        
        private EventFunc GetExecuteDelegate(MethodInfo methodInfo, object instance, bool isFilter)
        {
            if (methodInfo.ReturnType != typeof(bool) && methodInfo.ReturnType != typeof(void))
            {
                throw new Exception("Not suport that return type not bool or void");
            }
            ParameterExpression paramsExp = Expression.Parameter(typeof(object[]), "params");
            
            ConstantExpression instanceExp = methodInfo.IsStatic? null : Expression.Constant(instance, instance.GetType());
            ParameterInfo[] paramInfos = methodInfo.GetParameters();
            List<Expression> parameterExpressions = new List<Expression>();
            if(isFilter)
            {
                if (paramInfos.Length != 1 || paramInfos[0].ParameterType != typeof(object[]))
                {
                    throw new Exception("The filer must only one param whith object[]");
                }
                parameterExpressions.Add(paramsExp);
            }
            else
            {
                for (int i = 0; i < paramInfos.Length; i++)
                {
                    parameterExpressions.Add(Expression.Convert(Expression.ArrayIndex(paramsExp, Expression.Constant(i, typeof(int))), paramInfos[i].ParameterType));
                }
            }
            
            MethodCallExpression methodCall = Expression.Call(instanceExp, methodInfo, parameterExpressions);

            if (methodCall.Type == typeof(bool))
            {
                Expression<EventFunc> lambda = Expression.Lambda<EventFunc>(methodCall, paramsExp);
                return lambda.Compile();
            }
            else
            {
                Expression<mAction> lambda = 
                    Expression.Lambda<mAction>(methodCall, paramsExp);

                mAction execute = lambda.Compile();

                return (object[] args) => { execute(args); return true; };
            }
        }


        public int Trigger(string eventName, params object[] args) {
            return Call(eventName, args);
        }

        /*
        public bool Cancel(Listener listener)
        {
            List<Listener> listeners = null;
            if (mRegisteredEvents.TryGetValue(listener.mEventName, out listeners))
            {
                return listeners.Remove(listener);
            }
            else
            {
                return false;
            }
        }
        */
        Listener Register(Listener listener)
        {
            if(listener == null) {
                throw new Exception("Listener can't be null");
            }
            
            List<Listener> listenerList;
            if (!mRegisteredEvents.TryGetValue(listener.mEventName, out listenerList))
            {
                listenerList = new List<Listener>();
                mRegisteredEvents.Add(listener.mEventName, listenerList);
            }
            listenerList.Add(listener);
            listenerList.Sort();
            return listener;

        }
        
        internal Listener Register(string eventName, EventFunc action, int weight, bool isFilter)
        {
            Listener  listener = new Listener();
            listener.mId = mNextListenID++;
            listener.mEventName = eventName;
            listener.mAction = action;
            listener.mWeight = weight;
            listener.mIsFilter = isFilter;
            return Register(listener);
        }
        
        internal Listener Register(string eventName, MethodInfo methodInfo, object instance, int weight, bool isFilter)
        {
            return Register(eventName, GetExecuteDelegate(methodInfo, instance, isFilter), weight, isFilter);
        }
        
        internal Listener Register(string eventName, string methodName, object instance, int weight = 1000, bool isFilter = false)
        {
            MethodInfo methodInfo = instance.GetType().GetMethod(methodName, mMethodBindingFlags);
            if (methodInfo ==  null)
            {
                throw new Exception("Not find the method : " + methodName);
            }
            return Register(eventName,GetExecuteDelegate(methodInfo, instance, isFilter), weight, isFilter);
        }
        
        /*
        public Listener Register(string eventName, string methodName, object instance, int weight = 1000)
        {
            return Register(eventName, methodName, instance, weight, false);
        }
        */

        int Call(String eventName, object[] args)
        {
            List<Listener> listenerList = null;
            if(mRegisteredEvents.TryGetValue(eventName, out listenerList))
            {
                foreach(Listener listener in listenerList)
                {
                    if(!listener.mAction(args))
                    {
                        return 2;
                    }
                }
            }

            if(listenerList != null && listenerList.Count !=0 )
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        Dictionary<string, List<Listener>> mRegisteredEvents  = new Dictionary<string, List<Listener>>();
        int mNextListenID = 1;
        
    }

    delegate bool EventFunc(object[] args);

    class Listener : IComparable<Listener>
    {
        internal int       mId;
        internal string    mEventName;
        internal int       mWeight;
        internal bool      mIsFilter;
        internal EventFunc mAction;

        public int CompareTo(Listener other)
        {
            return mWeight.CompareTo(other.mWeight);
        }
    }


    [AttributeUsage(AttributeTargets.Method)]
    public class EventListenerAttribute : Attribute
    {
        private string _name;
        private int _weight = 1000;
        public string name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int weight
        {
            get {return _weight; }
            set { _weight = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class EventFilterAttribute : Attribute
    {
        private string _name;
        private int _weight = -1000;
        public string name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int weight
        {
            get {return _weight; }
            set { _weight = value; }
        }
    }


}