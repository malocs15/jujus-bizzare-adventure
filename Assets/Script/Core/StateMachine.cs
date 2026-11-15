using System;
using System.Collections.Generic;

public class StateMachine<T> where T : Enum
{
    class State
    {
        public Action enter, tick, exit;
    }

    readonly Dictionary<T, State> states = new Dictionary<T, State>();
    bool started;

    public T Current { get; private set; }

    public void Add(T id, Action enter = null, Action tick = null, Action exit = null)
    {
        states[id] = new State { enter = enter, tick = tick, exit = exit };
    }

    public void ChangeState(T next)
    {
        if (started) states[Current].exit?.Invoke();
        Current = next;
        started = true;
        states[next].enter?.Invoke();
    }

    public void Tick()
    {
        if (started) states[Current].tick?.Invoke();
    }
}