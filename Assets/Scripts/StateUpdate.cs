using UnityEngine;
using System.Collections;
using FSM;

public class StateUpdate :  StateBehaviour {

	public enum State
	{
		Success,
		Failure
	}

	[HideInInspector]
	public string state;

	[HideInInspector]
	public bool isCalledUpdate = false;
	// Use this for initialization
	void Start () {
		InitState(State.Success);
	}
	

	[StateBehaviour(state = "Success", on = "Update")]
	void SuccessUpdate()
	{
		isCalledUpdate = true;
	}
}
