using System.Collections.Generic;

public static class ListPool<T>
{
    private static readonly Stack<List<T>> Stack = new();

    public static List<T> Get()
        => Stack.Count > 0 ? Stack.Pop() : [];

    public static void Add(List<T> list)
    {
        list.Clear();
        Stack.Push(list);
    }
}