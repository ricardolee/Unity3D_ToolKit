using UnityEngine;
using Toolkit;

[RequireComponent(typeof(FiniteStateMachine))]
public class StateEnter : MonoBehaviour
{
    private FiniteStateMachine fsm;

    [HideInInspector]
    public bool flag = false;
    
    void Awake()
    {
        fsm = GetComponent<FiniteStateMachine>();
        fsm.ChangeState("GameState", "Failure");
    }
    
    void Start()
    {
        fsm.ChangeState("GameState", "Success");
    }
    
    [StateListener(state = "GameState", when = "Success", on = "Enter")]
    void ChangeStateToSuccess()
    {
        flag = true;
    }

}
