using Godot;
using System;

public partial class EnableLevelComponent : VBoxContainer
{
    public bool Enable
    {
        get => _enableBut.ButtonPressed;
        set => _enableBut.ButtonPressed = value;
    }

    public int Level
    {
        get => (int)_levelSlider.Value;
        set => _levelSlider.Value = value;
    }

    [Export] private CheckButton _enableBut;
    [Export] private HSlider _levelSlider;

    public void Init(string labelName, (int min, int max) levelRange)
    {
        _levelSlider.ValueChanged += (value) =>
        {
            Level = (int)value;
            _enableBut.Text = $"{labelName} : {Level}";
        };

        _levelSlider.MinValue = levelRange.min;
        _levelSlider.MaxValue = levelRange.max;
        _levelSlider.TickCount = levelRange.max - levelRange.min + 1;

        _enableBut.Text = $"{labelName} : {Level}";
    }
}