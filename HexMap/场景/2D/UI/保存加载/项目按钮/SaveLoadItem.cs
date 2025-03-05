using Godot;
using System;

public partial class SaveLoadItem : Button
{
    public string MapName
    {
        get => _mapName;
        set
        {
            _mapName = value;
            Text = value;
        }
    }

    private string _mapName;
}