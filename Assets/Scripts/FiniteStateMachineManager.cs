using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;

namespace FSM
{
    public class FiniteStateMachineManager : MonoBehaviour
    {
        private List<FiniteStateMachine> fsmList = new List<FiniteStateMachine>();
        public void AddFSM(FiniteStateMachine fsm)
        {
            if (fsm.script == null)
            {
                throw new Exception("finite state match not init by script");
            }
            fsmList.Add(fsm);
        }
	
	
        void FixedUpdate()
        {
            foreach (FiniteStateMachine fsm in fsmList)
            {
                if (fsm.script.isActiveAndEnabled)
                {
                    fsm.FixedUpdate();
                }
            }
        }
	
        void Update()
        {
            foreach (FiniteStateMachine fsm in fsmList)
            {
                if (fsm.script.isActiveAndEnabled)
                {
                    fsm.Update();
                }
            }
        }
	
        void LateUpdate()
        {
            foreach (FiniteStateMachine fsm in fsmList)
            {
                if (fsm.script.isActiveAndEnabled)
                {
                    fsm.LateUpdate();
                }
            }
        }
    }
}
