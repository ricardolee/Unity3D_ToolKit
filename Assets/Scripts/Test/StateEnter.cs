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


    public string state;
    

    void Awake()
    {
        fsm.Init<State>(this);        
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
