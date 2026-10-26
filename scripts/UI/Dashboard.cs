using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using WarriorsSim.Simulation;

/// <summary>
///     A simple dashboard for watching the simulation: time and speed controls, a Clan overview,
///     a list of all cats with their needs, and the log. The layout is built in code.
/// </summary>
public partial class Dashboard : Control {
    private const string LogFile = "res://logs/simulation.log";
    private const int MaxLogLines = 300;
    private const int MaxTicksPerFrame = 500;

    /// <summary>Ticks per second for each speed button. 1× = one tick (6 hours) per second.</summary>
    private static readonly (string Label, float TicksPerSecond)[] Speeds = {
        ("1×", 1f), ("10×", 10f), ("100×", 100f), ("1000×", 1000f)
    };

    private readonly Dictionary<Cat, CatRow> _rows = new();
    private readonly List<(LogLevel Level, string Line)> _pendingLog = new();

    private World _world;
    private StreamWriter _logFile;
    private float _ticksPerSecond = 1f;
    private bool _paused = true;
    private float _tickProgress;
    private bool _showDetails;
    private string _catListKey = "";

    private Label _timeLabel;
    private Label _clanLabel;
    private Button _pauseButton;
    private GridContainer _catGrid;
    private RichTextLabel _log;

    public override void _Ready() {
        var logPath = ProjectSettings.GlobalizePath(LogFile);
        Directory.CreateDirectory(Path.GetDirectoryName(logPath));
        _logFile = new StreamWriter(logPath);

        _world = new World(StartingClans.CreateThunderClan(), Tuning.RandomSeed, OnLog);

        BuildLayout();

        // For testing: "godot ... -- --autoplay" starts running at the highest speed right away
        if (OS.GetCmdlineUserArgs().Contains("--autoplay")) {
            _ticksPerSecond = Speeds[^1].TicksPerSecond;
            SetPaused(false);
        }

        RefreshAll();
    }

    public override void _ExitTree() {
        _logFile?.Dispose();
    }

    public override void _Process(double delta) {
        if (!_paused && _world.Clan.Cats.Count > 0) {
            _tickProgress += (float)delta * _ticksPerSecond;

            var ticks = 0;
            while (_tickProgress >= 1f && ticks < MaxTicksPerFrame) {
                _world.Tick();
                _tickProgress -= 1f;
                ticks++;
            }

            // If the computer can't keep up, skip the rest instead of falling further behind
            if (ticks == MaxTicksPerFrame)
                _tickProgress = 0f;

            if (_world.Clan.Cats.Count == 0) {
                OnLog(LogLevel.Summary, $"{_world.Clan.Name} has died out.");
                SetPaused(true);
            }
        }

        RefreshAll();
    }

    private void OnLog(LogLevel level, string line) {
        _logFile.WriteLine(line);
        _pendingLog.Add((level, line));
    }

    // ---- Layout ----

    private void BuildLayout() {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var background = new PanelContainer();
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var margin = new MarginContainer();
        foreach (var side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", 12);
        background.AddChild(margin);

        var main = new VBoxContainer();
        main.AddThemeConstantOverride("separation", 10);
        margin.AddChild(main);

        main.AddChild(BuildTopBar());

        _clanLabel = new Label();
        main.AddChild(_clanLabel);

        var split = new HSplitContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        main.AddChild(split);
        split.AddChild(BuildCatList());
        split.AddChild(BuildLogPanel());
    }

    private Control BuildTopBar() {
        var bar = new HBoxContainer();
        bar.AddThemeConstantOverride("separation", 8);

        _timeLabel = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _timeLabel.AddThemeFontSizeOverride("font_size", 20);
        bar.AddChild(_timeLabel);

        _pauseButton = new Button { CustomMinimumSize = new Vector2(80, 0) };
        _pauseButton.Pressed += () => SetPaused(!_paused);
        bar.AddChild(_pauseButton);

        var speedGroup = new ButtonGroup();
        foreach (var (label, ticksPerSecond) in Speeds) {
            var button = new Button {
                Text = label,
                ToggleMode = true,
                ButtonGroup = speedGroup,
                ButtonPressed = ticksPerSecond == _ticksPerSecond,
                CustomMinimumSize = new Vector2(60, 0)
            };
            button.Pressed += () => _ticksPerSecond = ticksPerSecond;
            bar.AddChild(button);
        }

        SetPaused(true);
        return bar;
    }

    private Control BuildCatList() {
        var scroll = new ScrollContainer {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 2f,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };

        _catGrid = new GridContainer { Columns = 7, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _catGrid.AddThemeConstantOverride("h_separation", 14);
        scroll.AddChild(_catGrid);
        return scroll;
    }

    private Control BuildLogPanel() {
        var panel = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

        var details = new CheckBox { Text = "Show details" };
        details.Toggled += on => _showDetails = on;
        panel.AddChild(details);

        _log = new RichTextLabel {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            ScrollFollowing = true,
            SelectionEnabled = true
        };
        panel.AddChild(_log);
        return panel;
    }

    private void SetPaused(bool paused) {
        _paused = paused;
        if (_pauseButton != null)
            _pauseButton.Text = paused ? "▶ Play" : "⏸ Pause";
    }

    // ---- Updating ----

    private void RefreshAll() {
        RefreshHeader();
        RefreshCatList();
        FlushLog();
    }

    private void RefreshHeader() {
        _timeLabel.Text = $"Day {_world.Day}  ·  {_world.TimeOfDay}  ·  Moon {_world.Moon}  ·  {_world.Season}";

        var clan = _world.Clan;
        var famine = clan.IsInFamine ? "   ·   FAMINE" : "";
        _clanLabel.Text =
            $"{clan.Name}: {clan.Cats.Count} cats ({clan.Cats.Count(c => c.Rank == Rank.Kit)} kits, "
            + $"{clan.Cats.Count(c => c.IsQueen)} queens)   ·   Fresh-kill pile: {clan.FreshKillPile.Mice} mice   ·   "
            + $"Leader: {clan.Leader?.Name ?? "none"}   ·   Deputy: {clan.Deputy?.Name ?? "none"}{famine}";
    }

    /// <summary>Rebuilds the rows when cats are born, die or change rank, and updates the values every frame.</summary>
    private void RefreshCatList() {
        var cats = _world.Clan.Cats
            .OrderBy(c => DisplayOrder(c.Rank))
            .ThenByDescending(c => c.AgeMoons)
            .ToList();

        var key = string.Join(",", cats.Select(c => c.Name));
        if (key != _catListKey) {
            RebuildCatRows(cats);
            _catListKey = key;
        }

        foreach (var cat in cats)
            _rows[cat].Update(cat, _world.IsSleeping(cat));
    }

    private void RebuildCatRows(List<Cat> cats) {
        foreach (var child in _catGrid.GetChildren())
            child.QueueFree();
        _rows.Clear();

        foreach (var header in new[] { "Name", "Rank", "Age", "Satiation", "Energy", "Stacks", "Status" })
            _catGrid.AddChild(new Label { Text = header, Modulate = new Color(1, 1, 1, 0.6f) });

        foreach (var cat in cats)
            _rows[cat] = new CatRow(_catGrid);
    }

    private static int DisplayOrder(Rank rank) {
        return rank switch {
            Rank.Leader => 0,
            Rank.Deputy => 1,
            Rank.Warrior => 2,
            Rank.Apprentice => 3,
            Rank.Kit => 4,
            Rank.Elder => 5,
            _ => 6
        };
    }

    private void FlushLog() {
        var lines = _pendingLog
            .Where(entry => _showDetails || entry.Level == LogLevel.Summary)
            .Select(entry => entry.Line)
            .TakeLast(MaxLogLines)
            .ToList();
        _pendingLog.Clear();

        foreach (var line in lines) {
            _log.AddText(line);
            _log.Newline();
        }

        while (_log.GetParagraphCount() > MaxLogLines)
            _log.RemoveParagraph(0);
    }

    /// <summary>The controls of one cat's row in the list.</summary>
    private class CatRow {
        private static readonly Color StarvingColor = new(1f, 0.45f, 0.45f);

        private readonly Label _name = new() { MouseFilter = MouseFilterEnum.Stop };
        private readonly Label _rank = new();
        private readonly Label _age = new();
        private readonly ProgressBar _satiation = NewBar();
        private readonly ProgressBar _energy = NewBar();
        private readonly Label _stacks = new();
        private readonly Label _status = new();

        public CatRow(GridContainer grid) {
            grid.AddChild(_name);
            grid.AddChild(_rank);
            grid.AddChild(_age);
            grid.AddChild(_satiation);
            grid.AddChild(_energy);
            grid.AddChild(_stacks);
            grid.AddChild(_status);
        }

        public void Update(Cat cat, bool sleeping) {
            _name.Text = cat.Name;
            _name.TooltipText = Tooltip(cat);
            _rank.Text = cat.Rank.ToString();
            _age.Text = $"{cat.AgeMoons} moons";
            _satiation.Value = cat.Satiation;
            _satiation.Modulate = cat.IsStarving ? StarvingColor : Colors.White;
            _energy.Value = cat.Energy;
            _stacks.Text = cat.StarvationStacks > 0 ? cat.StarvationStacks.ToString() : "";
            _stacks.Modulate = StarvingColor;
            _status.Text = Status(cat, sleeping);
        }

        private static ProgressBar NewBar() {
            return new ProgressBar {
                MinValue = 0,
                MaxValue = 100,
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(110, 14),
                SizeFlagsVertical = SizeFlags.ShrinkCenter
            };
        }

        private static string Status(Cat cat, bool sleeping) {
            var parts = new List<string>();
            if (cat.IsPregnant)
                parts.Add($"expecting kits ({cat.PregnancyDaysLeft} days)");
            if (cat.IsNursing)
                parts.Add("nursing");
            if (cat.IsStarving)
                parts.Add("starving");
            if (sleeping)
                parts.Add("sleeping");
            return string.Join(", ", parts);
        }

        private static string Tooltip(Cat cat) {
            return $"{cat.Name} ({cat.Sex}, {cat.Rank})\n"
                   + $"Hunting: {cat.Hunting}   Diligence: {cat.Diligence}\n"
                   + $"Mother: {cat.Mother?.Name ?? "unknown"}   Father: {cat.Father?.Name ?? "unknown"}\n"
                   + $"Mate: {cat.Mate?.Name ?? "none"}";
        }
    }
}
