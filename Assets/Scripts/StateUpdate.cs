using UnityEngine;
using System.Collections;
using FSM;

[RequireComponent(typeof(FiniteStateMachine))]
public class StateUpdate : MonoBehaviour {
    
    [HideInInspector]
    public string state;

    [HideInInspector]
    public bool flag = false;

    FiniteStateMachine fsm;
    // Use this for initialization
    
    void Start () {
        fsm = GetComponent<FiniteStateMachine>();
        fsm.ChangeState("GameState", "Success");
    }

    void Update() {
        fsm.Trigger("GameState", "Update");
    }

    [StateListener(state = "GameState", when = "Success", on = "Update")]
    void SuccessUpdate()
    {
        flag = true;
    }
}
