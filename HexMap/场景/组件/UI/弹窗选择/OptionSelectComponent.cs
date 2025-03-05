using Godot;
using System;

public partial class OptionSelectComponent : HBoxContainer
{
    [Export] private Label _label;
    [Export] private OptionButton _optionBut;

    public int ModeIndex
    {
        get => _optionBut.Selected;
        set => _optionBut.Selected = value;
    }

    public void Init(string labelName, string[] itemsNames)
    {
        _label.Text = $" {labelName} ";

        foreach (var item in itemsNames)
            _optionBut.AddItem(item);
    }
}