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
                        Register(ea.name, GetExecuteDelegate(method, script, ea.isFilter));
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


        public void Trigger(string eventName, params object[] args) {
            Call(eventName, args);
        }

        public void Cancel(int listenerID)
        {
            Listener listener = mRegisteredListener[listenerID];
            if (listener == null)
            {
                throw new Exception("Listener Id: " + listenerID + " not registered");
            } 
            mRegisteredEvents[listener.mEventName].RemoveAll((t) => t.mId == listenerID);
            mRegisteredListener.Remove(listenerID);
        }

        int Register(Listener listener)
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
            listenerList.Insert(0, listener);
            mRegisteredListener.Add(listener.mId, listener);
            return listener.mId;

        }
        int Register(string eventName, EventFunc action)
        {
            Listener  listener = new Listener();
            listener.mId = mNextListenID++;
            listener.mEventName = eventName;
            listener.mAction = action;
            return Register(listener);
        }
        
        void Call(String eventName, object[] args)
        {
            List<Listener> listenerList;
            if(mRegisteredEvents.TryGetValue(eventName, out listenerList))
            {
                foreach(Listener listener in listenerList)
                {
                    if(!listener.mAction(args))
                    {
                        break;
                    }
                }
            }
        }

        
        delegate bool EventFunc(object[] args);

        class Listener
        {
            public int       mId;
            public string    mEventName;
            public EventFunc mAction;
        }

        Dictionary<string, List<Listener>> mRegisteredEvents  = new Dictionary<string, List<Listener>>();
        Dictionary<int, Listener> mRegisteredListener = new Dictionary<int, Listener>();
        int mNextListenID = 1;
        
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class EventAttribute : Attribute
    {
        private string _name;
        private bool _isFilter = false;
        public string name
        {
            get { return _name; }
            set { _name = value; }
        }

        public bool isFilter
        {
            get { return _isFilter; }
            set { _isFilter = value; } 
        }
    }

}