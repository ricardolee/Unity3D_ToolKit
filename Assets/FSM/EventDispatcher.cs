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
                        Register(ea.name, GetExecuteDelegate(method, script));
                    }
                }
            }
        }

        private static EventFunc GetExecuteDelegate(MethodInfo methodInfo, object instance)
        {
            // parameters to execute
            ParameterExpression instanceParameter = 
                Expression.Parameter(typeof(object), "instance");
            ParameterExpression parametersParameter = 
                Expression.Parameter(typeof(object[]), "parameters");

            // build parameter list
            List<Expression> parameterExpressions = new List<Expression>();
            ParameterInfo[] paramInfos = methodInfo.GetParameters();
            for (int i = 0; i < paramInfos.Length; i++)
            {
                // (Ti)parameters[i]
                BinaryExpression valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                UnaryExpression valueCast = Expression.Convert(valueObj, paramInfos[i].ParameterType);
                parameterExpressions.Add(valueCast);
            }

            // non-instance for static method, or ((TInstance)instance)
            Expression instanceCast = methodInfo.IsStatic ? null : 
                Expression.Convert(instanceParameter, methodInfo.ReflectedType);
            
            // static invoke or ((TInstance)instance).Method
            MethodCallExpression methodCall = Expression.Call(instanceCast, methodInfo, parameterExpressions);

            // ((TInstance)instance).Method((T0)parameters[0], (T1)parameters[1], ...)
            if (methodCall.Type != typeof(bool))
            {
                Expression<Action<object, object[]>> lambda = 
                    Expression.Lambda<Action<object, object[]>>(methodCall, instanceParameter, parametersParameter);

                Action<object, object[]> execute = lambda.Compile();
                return (ref object arg1, ref object arg2, ref object arg3, ref object arg4, ref object arg5) =>
                    {
                        object[] args = {arg1, arg2, arg3, arg4, arg5};
                        execute(instance, args);
                        return true;
                    };
            }
            else
            {
                UnaryExpression castMethodCall = Expression.Convert(
                                                                    methodCall, typeof(object));
                Expression<Func<object, object[], object>> lambda = 
                    Expression.Lambda<Func<object, object[], object>>(
                                                                      castMethodCall, instanceParameter, parametersParameter);
                Func<object, object[], object> execute = lambda.Compile();
                return (ref object arg1, ref object arg2, ref object arg3, ref object arg4, ref object arg5) =>
                    {
                        object[] args = {arg1, arg2, arg3, arg4, arg5};                        
                        return (bool)execute(instance, args);
                    };
            }
        }


        public void Trigger(string eventName) {
            Call(eventName, null, null, null, null, null);
        }

        public void Trigger(string eventName, object param1) {
            Call(eventName, param1, null, null, null, null);
        }

        public void Trigger(string eventName, object param1, object param2) {
            Call(eventName, param1, param2, null, null, null);
        }

        public void Trigger(string eventName, object param1, object param2, object param3) {
            Call(eventName, param1, param2, param3, null, null);
        }
        
        public void Trigger(string eventName, object param1, object param2, object param3, object param4) {
            Call(eventName, param1, param2, param3, param4, null);
        }
        
        public void Trigger(string eventName, object param1, object param2, object param3, object param4, object param5) {
            Call(eventName, param1, param2, param3, param4, param5);
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

        int Register(string eventName, EventFunc action)
        {
            Listener  listener = new Listener();
            listener.mId = mNextListenID++;
            listener.mEventName = eventName;
            listener.mAction = action;
            List<Listener> listenerList;
            if (!mRegisteredEvents.TryGetValue(eventName, out listenerList))
            {
                listenerList = new List<Listener>();
                mRegisteredEvents.Add(eventName, listenerList);
            }
            listenerList.Insert(0, listener);
            mRegisteredListener.Add(listener.mId, listener);
            return listener.mId;
        }
        
        void Call(String eventName, object param1, object param2, object param3, object param4, object param5)
        {
            List<Listener> listenerList;
            if( mRegisteredEvents.TryGetValue(eventName, out listenerList))
            {
                foreach(Listener listener in listenerList)
                {
                    if(!listener.mAction(ref param1,ref param2,ref param3,ref param4,ref param5))
                    {
                        break;
                    }
                }
            }
        }

        delegate bool EventFunc(ref object arg1, ref object arg2,ref object arg3,ref object arg4,ref object arg5);
        
        private class Listener
        {
            public int           mId;
            public string        mEventName;
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

        public string name
        {
            get { return _name; }
            set { _name = value; }
        }
    }

}