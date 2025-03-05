using Godot;

public partial class ProcessComponent : Node
{
    public bool IsPhy;
    public bool ProcessEnable { get; set; }

    protected void SetProcessMode(bool isPhy, bool isEnable)
    {
        IsPhy = isPhy;
        ProcessEnable = isEnable;
        SystemManager.GetManager<ProcessManager>().AddProcess(this);
    }

    public virtual void Process(float delta)
    {
    }

    public void Remove()
    {
        SystemManager.GetManager<ProcessManager>().RemoveProcess(this);
    }
}