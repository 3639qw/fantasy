// Assets/Scripts/Tutorial/TutorialEvent.cs
using System;
using System.Collections.Generic;

public static class TutorialEvent
{
    private static readonly Dictionary<string, Action> map = new();

    public static void On(string key, Action cb)
    {
        if (!map.ContainsKey(key)) map[key] = cb;
        else map[key] += cb;
    }

    public static void Off(string key, Action cb)
    {
        if (map.TryGetValue(key, out var a)) map[key] -= cb;
    }

    public static void Trigger(string key)
    {
        if (map.TryGetValue(key, out var a)) a?.Invoke();
    }

    public static void ClearAll() => map.Clear();
}
