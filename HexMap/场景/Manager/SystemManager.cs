using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class SystemManager : Node
{
    public static SystemManager Instance { get; private set; }

    private List<IManager> _managers;

    public void Init()
    {
        Instance = this;
        _managers = [];

        foreach (var node in GetChildren())
            if (node is IManager manager)
                _managers.Add(manager);

        foreach (var manager in _managers)
            manager.Init();
    }

    public static T GetManager<T>() where T : class, IManager
        => Instance._managers.OfType<T>().FirstOrDefault();
}