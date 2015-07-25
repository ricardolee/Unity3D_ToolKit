using UnityEngine;
using Toolkit;

[RequireComponent(typeof(StateManager))]
public class StateEnterTest : MonoBehaviour
{
    private StateManager fsm;

    [HideInInspector]
    public bool flag = false;
    
    void Awake()
    {
        fsm = GetComponent<StateManager>();
        fsm.ChangeState("GameState", "Failure");
    }
    
    void Start()
    {
        fsm.ChangeState("GameState", "Success");
    }
    
    [StateListener(state = "GameState", when = "Success", on = "Enter")]
    void ChangeStateToSuccess()
    {
        Debug.Log("Success Enter");
        flag = true;
    }

}
