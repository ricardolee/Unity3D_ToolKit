using UnityEngine;
using Toolkit;

[RequireComponent(typeof(StateManager))]
public class StateEnterTest : MonoBehaviour
{
    private StateManager fsm;

    [HideInInspector]
    public bool flag = false;
    public StateMachine<GameState> sm;

    public enum GameState {
        Play,
        Success,
        Failure
    }

    void Awake()
    {
        fsm = GetComponent<StateManager>();
        sm = fsm.GetStateMachine<GameState>();
        sm.Init(GameState.Play);
    }
    
    void Start()
    {
        sm.ChangeState(GameState.Success);
    }
    
    [StateListener(state = GameState.Success, on = StateEvent.Enter)]
    void ChangeStateToSuccess()
    {
        Debug.Log("Success Enter");
        flag = true;
    }

}
