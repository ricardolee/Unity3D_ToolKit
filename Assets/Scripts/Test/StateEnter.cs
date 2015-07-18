using UnityEngine;
using System.Collections;
using FSM;

public class StateEnter : MonoBehaviour
{
    private FiniteStateMachine fsm = new FiniteStateMachine();

    public enum State
    {
        Success,
        Failure,
    }

    [HideInInspector]
    public string state;
    

    void Awake()
    {
        fsm.Init(this,State.Failure); 
    }
    
    void Start()
    {
        fsm.ChangeState(State.Success);
    }
    
    [StateBehaviour(state = "Success", on = "Enter")]
    void ChangeStateToSuccess()
    {
        state = State.Success.ToString();
    }

}
