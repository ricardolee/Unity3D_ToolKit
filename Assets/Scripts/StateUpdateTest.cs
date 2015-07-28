using UnityEngine;
using Toolkit;

[RequireComponent(typeof(StateManager))]
public class StateUpdateTest : MonoBehaviour {
    
    [HideInInspector]
    public string state;

    [HideInInspector]
    public bool flag = false;

    [StateMachineInject]
    StateMachine<GameState> sm;

    public enum GameState {
        Play, Success, Failure
    }
    // Use this for initialization
    
    void Start () {
        sm.Init(GameState.Success);
    }

    void Update() {
        sm.Update();
    }
    
    [StateListener(state = GameState.Success, on = StateEvent.Enter)]
    void SuccessUpdate()
    {
        flag = true;
    }
}
