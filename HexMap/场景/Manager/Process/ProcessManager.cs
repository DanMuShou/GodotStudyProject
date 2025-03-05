using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class ProcessManager : Node, IManager
{
    private Dictionary<bool, List<ProcessComponent>> _processDicForPhy;

    public void Init()
    {
        _processDicForPhy = new Dictionary<bool, List<ProcessComponent>>
        {
            { true, [] },
            { false, [] }
        };
    }

    public override void _Process(double delta)
    {
        foreach (var process in _processDicForPhy[false].Where(process => process.ProcessEnable))
            process.Process((float)delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        foreach (var process in _processDicForPhy[true].Where(process => process.ProcessEnable))
            process.Process((float)delta);
    }

    public void AddProcess(ProcessComponent node)
    {
        if (node.IsPhy)
            _processDicForPhy[true].Add(node);
        else
            _processDicForPhy[false].Add(node);
    }

    public void RemoveProcess(ProcessComponent node)
    {
        if (node.IsPhy)
            _processDicForPhy[true].Remove(node);
        else
            _processDicForPhy[false].Remove(node);
    }

    public void SetAllState(Type[] types, bool enable)
    {
        foreach (var type in types)
            SetAllState(type, enable);
    }

    public void SetAllState(Type type, bool enable)
    {
        foreach (var process in _processDicForPhy[false].Where(process => process.GetType() == type))
            process.ProcessEnable = enable;
        foreach (var process in _processDicForPhy[true].Where(process => process.GetType() == type))
            process.ProcessEnable = enable;
    }
}