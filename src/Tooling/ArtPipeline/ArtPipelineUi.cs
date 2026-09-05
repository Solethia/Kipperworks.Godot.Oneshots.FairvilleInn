using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FairvilleInn.Tooling.ArtPipeline;

// Art Pipeline window (tools/art_pipeline.tscn). Run it from the editor with F6 or via
// `godot --path . tools/art_pipeline.tscn`. Create -> paint -> Test.
public partial class ArtPipelineUi : Control
{
    private OptionButton _type = null!;
    private LineEdit _name = null!;
    private LineEdit _display = null!;
    private HBoxContainer _propOptions = null!;
    private SpinBox _footW = null!;
    private SpinBox _footH = null!;
    private SpinBox _height = null!;
    private Label _typeInfo = null!;
    private ItemList _assets = null!;
    private RichTextLabel _log = null!;
    private Button _test = null!;
    private Button _open = null!;
    private LineEdit _roomName = null!;
    private SpinBox _roomW = null!;
    private SpinBox _roomH = null!;
    private OptionButton _roomFloor = null!;
    private OptionButton _roomWall = null!;

    private readonly List<AssetMeta> _listed = [];

    public override void _Ready()
    {
        _type = GetNode<OptionButton>("%Type");
        _name = GetNode<LineEdit>("%Name");
        _display = GetNode<LineEdit>("%Display");
        _propOptions = GetNode<HBoxContainer>("%PropOptions");
        _footW = GetNode<SpinBox>("%FootW");
        _footH = GetNode<SpinBox>("%FootH");
        _height = GetNode<SpinBox>("%Height");
        _typeInfo = GetNode<Label>("%TypeInfo");
        _assets = GetNode<ItemList>("%Assets");
        _log = GetNode<RichTextLabel>("%Log");
        _test = GetNode<Button>("%Test");
        _open = GetNode<Button>("%Open");
        _roomName = GetNode<LineEdit>("%RoomName");
        _roomW = GetNode<SpinBox>("%RoomW");
        _roomH = GetNode<SpinBox>("%RoomH");
        _roomFloor = GetNode<OptionButton>("%RoomFloor");
        _roomWall = GetNode<OptionButton>("%RoomWall");

        foreach (var type in AssetTypes.All)
        {
            _type.AddItem(type.Key);
        }

        _type.ItemSelected += _ => OnTypeChanged();
        GetNode<Button>("%Create").Pressed += () => Guard(Create);
        _open.Pressed += () => Guard(OpenFolder);
        _test.Pressed += () => Guard(Test);
        GetNode<Button>("%PackAll").Pressed += () => Guard(() =>
        {
            Packer.PackAll();
            RefreshRoomDefaults();
            Log("Packed all assets. Rooms pick up the new palette when reopened in the editor.");
        });
        GetNode<Button>("%NewRoom").Pressed += () => Guard(NewRoom);
        GetNode<Button>("%Refresh").Pressed += RefreshAssets;
        _assets.ItemSelected += _ => UpdateButtons();

        OnTypeChanged();
        RefreshAssets();
        Guard(RefreshRoomDefaults);
    }

    private AssetType SelectedType => AssetTypes.Get(_type.GetItemText(_type.Selected));

    private AssetMeta? SelectedAsset =>
        _assets.GetSelectedItems().Length > 0 ? _listed[_assets.GetSelectedItems()[0]] : null;

    private void OnTypeChanged()
    {
        _typeInfo.Text = SelectedType.Description;
        _propOptions.Visible = SelectedType.HasFootprint;
        RefreshAssets();
    }

    private void RefreshAssets()
    {
        _listed.Clear();
        _assets.Clear();
        foreach (var meta in AssetMeta.All(SelectedType.Key))
        {
            var problems = SelectedType.Validate(meta);
            _listed.Add(meta);
            _assets.AddItem(problems.Count == 0 ? meta.Name : $"{meta.Name}  ⚠ {problems[0]}");
        }

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var has = SelectedAsset is not null;
        _test.Disabled = !has;
        _open.Disabled = !has;
    }

    private void Create()
    {
        var meta = Scaffolder.Create(SelectedType.Key, _name.Text, _display.Text,
            (int)_footW.Value, (int)_footH.Value, (int)_height.Value);
        Log($"Created {meta.FolderRes}");
        RefreshAssets();
        _assets.Select(_listed.FindIndex(m => m.Name == meta.Name));
        UpdateButtons();
        OS.ShellOpen(Paths.Global(meta.FolderRes));
    }

    private void OpenFolder()
    {
        OS.ShellOpen(Paths.Global(SelectedAsset!.FolderRes));
    }

    private void RefreshRoomDefaults()
    {
        var index = Packer.LoadIndex();
        Fill(_roomFloor, index.Floor.Keys, "wood");
        Fill(_roomWall, index.Wall.Keys, "plaster");

        static void Fill(OptionButton button, IEnumerable<string> names, string preferred)
        {
            var current = button.Selected >= 0 ? button.GetItemText(button.Selected) : null;
            var ordered = names.OrderBy(n => n).ToList();

            button.Clear();
            foreach (var name in ordered)
            {
                button.AddItem(name);
            }

            if (ordered.Count == 0)
            {
                return;
            }

            var desired = current is not null && ordered.Contains(current) ? current
                : ordered.Contains(preferred) ? preferred
                : ordered[0];
            button.Select(ordered.IndexOf(desired));
        }
    }

    private void NewRoom()
    {
        if (_roomFloor.Selected < 0 || _roomWall.Selected < 0)
        {
            throw new PipelineException("pack at least one floor and one wall first");
        }

        var scene = RoomScene.Scaffold(_roomName.Text.Trim(), (int)_roomW.Value, (int)_roomH.Value,
            _roomFloor.GetItemText(_roomFloor.Selected), _roomWall.GetItemText(_roomWall.Selected));
        Log($"Created {scene} — open it in the Godot editor and paint with the TileMap panel.");
    }

    private void Test()
    {
        var meta = SelectedAsset!;
        var problems = SelectedType.Validate(meta);
        if (problems.Count > 0)
        {
            throw new PipelineException(string.Join("\n", problems));
        }

        Packer.PackAll();
        var room = PreviewRoom.Build(meta);
        Log("Packed. Importing textures…");

        // Godot only picks up new/changed PNGs through its importer; run it headless, then
        // launch the game in a separate process pointed at the preview room.
        var godot = OS.GetExecutablePath();
        var project = ProjectSettings.GlobalizePath("res://");
        var importOutput = new Godot.Collections.Array();
        var code = OS.Execute(godot, ["--headless", "--path", project, "--import"], importOutput, readStderr: true);
        if (code != 0)
        {
            Log($"[color=orange]Import exited with {code}[/color]");
        }

        OS.CreateProcess(godot, ["--path", project, "--", $"--room={room}"]);
        Log($"Launched preview of {meta.Type}/{meta.Name}.");
        RefreshAssets();
    }

    private void Guard(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e) when (e is PipelineException or ArgumentException or InvalidOperationException or System.IO.IOException)
        {
            Log($"[color=red]{e.Message}[/color]");
        }
    }

    private void Log(string message)
    {
        _log.AppendText(message + "\n");
    }
}
