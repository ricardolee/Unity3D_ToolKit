using UnityEngine;
using System.Collections;
using FSM;
public class EventTriger : MonoBehaviour
{

    EventDispatcher events;
    public int flag = 0;
    const string FLAG_CHANGE_EVENT = "FlagChange";
    // Use this for initialization
    void Start()
    {
        events = GetComponent<EventDispatcher>();
        events.Trigger(FLAG_CHANGE_EVENT, 10);
    }

    // Update is called once per frame
    void Update()
    {

    }

    [Event(name = FLAG_CHANGE_EVENT)]
    void ChangeFlag(int f)
    {
        flag = f;
    }

    [Event(name = FLAG_CHANGE_EVENT)]
    void FilterFlag(ref int f)
    {
        f = f * 2;
    }
}
