﻿using UnityEngine;
using Toolkit;

[RequireComponent(typeof(EventDispatcher))]
public class EventTriggerTest : MonoBehaviour
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

    [EventListener(name = FLAG_CHANGE_EVENT)]
    void ChangeFlag(int f)
    {
        flag = f;
    }

    [EventFilter(name = FLAG_CHANGE_EVENT)]
    void FilterFlag(object[] objs)
    {
        objs[0] = (int)objs[0] * 2;
    }
}
