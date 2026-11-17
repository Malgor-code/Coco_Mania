using System;
using System.Collections.Generic;
using UnityEngine;

public interface IEvent { }
public static class EventBus
{
    static readonly List<Action> clearActions = new List<Action>();

    internal static void Register(Action clear) => clearActions.Add(clear);

    public static void Subscribe<T>(Action<T> handler) where T : struct, IEvent => EventBus<T>.Subscribe(handler);
    public static void Unsubscribe<T>(Action<T> handler) where T : struct, IEvent => EventBus<T>.Unsubscribe(handler);
    public static void Publish<T>(T evt) where T : struct, IEvent => EventBus<T>.Publish(evt);

    public static void ClearAll()
    {
        for (int i = 0; i < clearActions.Count; i++) clearActions[i]();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => ClearAll();
}

public static class EventBus<T> where T : struct, IEvent
{
    static readonly List<Action<T>> handlers = new List<Action<T>>();
    static Action<T>[] snapshot = Array.Empty<Action<T>>();
    static bool dirty;

    static EventBus() { EventBus.Register(Clear); }

    public static void Subscribe(Action<T> handler)
    {
        if (handler == null || handlers.Contains(handler)) return;
        handlers.Add(handler);
        dirty = true;
    }

    public static void Unsubscribe(Action<T> handler)
    {
        if (handler == null) return;
        if (handlers.Remove(handler)) dirty = true;
    }

    public static void Publish(T evt)
    {
        if (dirty) { snapshot = handlers.ToArray(); dirty = false; }
        Action<T>[] current = snapshot;
        for (int i = 0; i < current.Length; i++)
        {
            Action<T> h = current[i];
            if (!handlers.Contains(h)) continue; 
            try { h(evt); }
            catch (Exception ex) { Debug.LogException(ex); } 
        }
    }

    public static void Clear()
    {
        handlers.Clear();
        snapshot = Array.Empty<Action<T>>();
        dirty = false;
    }
}