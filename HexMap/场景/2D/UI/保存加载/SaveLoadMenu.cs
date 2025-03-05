using System;
using Godot;
using System.IO;

public partial class SaveLoadMenu : ColorRect
{
    [Export] private Label _title;
    [Export] private LineEdit _inputText;
    [Export] private VBoxContainer _container;
    [Export] private Button _actionBut, _deleteBut, _cancelBut;

    [Export] private PackedScene _itemPacked;

    private HexGrid _hexGrid;
    private bool _saveMode;

    public void Init(HexGrid hexGrid)
    {
        _hexGrid = hexGrid;

        _actionBut.Pressed += Action;
        _deleteBut.Pressed += Delete;
        _cancelBut.Pressed += Close;

        Clear();
        Hide();
    }

    private void Save(string path)
    {
        using var writer = new BinaryWriter(File.Open(path, FileMode.Create));
        writer.Write(2);
        _hexGrid.Save(writer);
    }

    private void Load(string path)
    {
        if (!File.Exists(path))
        {
            GD.PrintErr("文件不存在");
            return;
        }

        using var reader = new BinaryReader(File.Open(path, FileMode.Open));
        var header = reader.ReadInt32();
        if (header <= 2)
        {
            _hexGrid.Load(reader, header);
            HexMapCamera.ValidatePosition();
        }
        else
            GD.PrintErr("文件头错误");
    }

    private void FileList()
    {
        Clear();

        var folderPath = CreateFolder();
        var paths = Directory.GetFiles(folderPath, "*.map");
        Array.Sort(paths);
        foreach (var path in paths)
        {
            var item = _itemPacked.Instantiate<SaveLoadItem>();
            _container.AddChild(item);
            item.MapName = Path.GetFileNameWithoutExtension(path);
            item.Pressed += () => SelectItem(item.MapName);
        }
    }

    private string GetSelectedPath()
    {
        var mapName = _inputText.Text;

        if (string.IsNullOrEmpty(mapName))
            return null;

        var folderPath = CreateFolder();

        var filePath = Path.Combine(folderPath, mapName + ".map");

        return filePath;
    }

    private string CreateFolder()
    {
        var folderPath = Path.Combine(OS.GetDataDir(), HexMetrics.ProjectName);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        return folderPath;
    }

    private void Clear()
    {
        foreach (var node in _container.GetChildren())
            node.QueueFree();
    }

    private void Delete()
    {
        var path = GetSelectedPath();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return;
        File.Delete(path);
        _inputText.Text = "";
        FileList();
    }

    private void SelectItem(string name)
    {
        _inputText.Text = name;
    }

    private void Action()
    {
        var path = GetSelectedPath();
        if (string.IsNullOrEmpty(path))
            return;

        if (_saveMode)
            Save(path);
        else
            Load(path);

        Close();
    }

    private void Close()
    {
        Hide();
        ProcessMode = ProcessModeEnum.Disabled;

        SystemManager.GetManager<ProcessManager>().SetAllState(
            [typeof(GameUiPhyProcess), typeof(CameraPhyProcess)], true);
    }

    public void Open(bool saveMode)
    {
        _saveMode = saveMode;

        if (saveMode)
        {
            _title.Text = "保存地图";
            _actionBut.Text = "保存";
        }
        else
        {
            _title.Text = "加载地图";
            _actionBut.Text = "加载";
        }

        FileList();
        Show();
        ProcessMode = ProcessModeEnum.Inherit;

        SystemManager.GetManager<ProcessManager>().SetAllState(
            [typeof(GameUiPhyProcess), typeof(CameraPhyProcess)], false);
    }
}