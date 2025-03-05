using Godot;
using System;

public partial class Ui : Control
{
    [Export] private HexGrid _hexGrid;

    [Export] private HexMapEditor _editor;
    [Export] private NewMapMenu _newMapMenu;
    [Export] private SaveLoadMenu _saveLoadMenu;
    [Export] private HexGameUi _gameUi;

    public void Init()
    {
        _editor.OnNewMap += _newMapMenu.Open;
        _editor.OnSaveOrLoad += (mode) => _saveLoadMenu.Open(mode);
        _editor.OnChangeEditMode += (mode) => _gameUi.SetEditMode(mode);

        _gameUi.Init(_hexGrid);
        _editor.Init(_hexGrid);
        _newMapMenu.Init(_hexGrid);
        _saveLoadMenu.Init(_hexGrid);
    }
}