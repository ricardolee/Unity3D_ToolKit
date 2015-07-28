using UnityEngine;
using Toolkit;

[RequireComponent(typeof(StateManager))]
public class StateUpdateTest : MonoBehaviour {
    
    [HideInInspector]
    public string state;

    [HideInInspector]
    public bool flag = false;

    StateManager fsm;

    public enum GameState {
        Play, Success, Failure
    }
    // Use this for initialization
    
    void Start () {
        fsm = GetComponent<StateManager>();
        fsm.ChangeState(GameState.Success);
    }

    void Update() {

    }
    
    [StateListener(state = GameState.Success, on = StateEvent.Enter)]
    void SuccessUpdate()
    {
        flag = true;
    }
}
