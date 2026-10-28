using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using WarriorsSim.Simulation;

/// <summary>
///     Starts and runs the simulation for the simulation UI.
///     Change the values in <see cref="Tuning" /> (or through <see cref="TuningSettings" />) first,
///     then call <see cref="StartSimulation" />. After that, use <see cref="Play" />, <see cref="Pause" />
///     and <see cref="TicksPerSecond" />.
/// </summary>
public partial class SimulationUi : Control {
    private const string LogFile = "res://logs/simulation.log";
    private const int MaxTicksPerFrame = 500;

    /// <summary>Emitted when a new simulation has been started.</summary>
    [Signal]
    public delegate void SimulationStartedEventHandler();

    /// <summary>Emitted for every line of the log. isSummary is true for important events (births, deaths, ...).</summary>
    [Signal]
    public delegate void LogLineEventHandler(string line, bool isSummary);

    /// <summary>Emitted once per frame after the simulation has advanced, so the UI can update itself.</summary>
    [Signal]
    public delegate void SimulationUpdatedEventHandler();

    /// <summary>Emitted when the last cat of the Clan has died. The simulation pauses.</summary>
    [Signal]
    public delegate void ClanDiedOutEventHandler();

    private StreamWriter _logFile;
    private float _tickProgress;

    /// <summary>The Clan the next simulation starts with. Created when needed, so cats can be added before starting.</summary>
    private Clan _nextStartingClan;

    // Kept as fields, so the cat list can be updated later
    private Tree _catList;
    private TreeItem _catListRoot;

    /// <summary>Which row of the cat list shows which cat.</summary>
    private readonly Dictionary<Cat, TreeItem> _catRows = new();

    private static readonly Color ChangedTuningColor = new(1f, 0.75f, 0.3f);

    private const int MaxLogLines = 500;

    // Simulation tab
    private Label _timeLabel;
    private Label _clanLabel;
    private Button _playPauseButton;
    private RichTextLabel _log;
    private bool _showDetails;

    /// <summary>Log lines since the last frame, written into the log once per frame.</summary>
    private readonly List<(LogLevel Level, string Line)> _pendingLog = new();

    private Tree _tuningTable;

    /// <summary>Which row of the tuning table shows which value, by the value's name.</summary>
    private readonly Dictionary<string, TreeItem> _tuningRows = new();

    /// <summary>How fast the simulation runs. 1 = one tick (6 hours) per second.</summary>
    [Export]
    public float TicksPerSecond { get; set; } = 10f;

    /// <summary>The running simulation, or null before <see cref="StartSimulation" /> was called.</summary>
    public World World { get; private set; }

    public bool IsPaused { get; private set; } = true;

    /// <summary>
    ///     Starts a new simulation with the current <see cref="Tuning" /> values. A running simulation is replaced.
    ///     The new simulation starts paused, call <see cref="Play" /> to let it run.
    /// </summary>
    public void StartSimulation() {
        _logFile?.Dispose();
        var logPath = ProjectSettings.GlobalizePath(LogFile);
        Directory.CreateDirectory(Path.GetDirectoryName(logPath));
        _logFile = new StreamWriter(logPath);

        World = new World(_nextStartingClan ?? StartingClans.CreateThunderClan(), Tuning.RandomSeed, OnLog);
        _nextStartingClan = null;
        IsPaused = true;
        _tickProgress = 0f;
        RefreshCatList();
        EmitSignal(SignalName.SimulationStarted);
    }

    /// <summary>
    ///     Creates a new cat. Before <see cref="StartSimulation" /> it joins the Clan the simulation starts with,
    ///     while a simulation is running it joins the Clan right away.
    ///     Hunting and diligence go from 0 to 100. The name decides the prefix and suffix, e.g. "Fireheart".
    ///     Returns false (and adds nothing) if a living cat already has this name or the name is empty.
    /// </summary>
    public bool AddCat(string name, Rank rank, Sex sex, int ageMoons, int hunting, int diligence) {
        var clan = World?.Clan ?? (_nextStartingClan ??= StartingClans.CreateThunderClan());

        if (string.IsNullOrWhiteSpace(name) || clan.Cats.Any(c => c.Name == name)) {
            GD.PushWarning($"AddCat: can't add a cat called \"{name}\", the name is empty or already taken.");
            return false;
        }

        var cat = new Cat(name, rank, sex, ageMoons, hunting, diligence);
        clan.AddCat(cat);

        if (World != null)
            OnLog(LogLevel.Summary, $"{cat.Name} ({rank}) has joined {clan.Name}.");
        return true;
    }

    public void Play() {
        if (World != null)
            IsPaused = false;
    }

    public void Pause() {
        IsPaused = true;
    }

    public void BuildCatList() {
        var tree = GetNode<Tree>("%CatList");

        tree.Columns = 7;
        tree.ColumnTitlesVisible = true;
        tree.SetColumnTitle(0, "Name");
        tree.SetColumnTitle(1, "Rank");
        tree.SetColumnTitle(2, "Sex");
        tree.SetColumnTitle(3, "Age");
        tree.SetColumnTitle(4, "Hunting skill");
        tree.SetColumnTitle(5, "Diligence");
        tree.SetColumnTitle(6, "Mother");

        tree.HideRoot = true;

        TreeItem root = tree.CreateItem();
        _catList = tree;
        _catListRoot = root;

        // ??= only creates the Clan if there isn't one yet, so cats added before aren't lost
        _nextStartingClan ??= StartingClans.CreateThunderClan();

        RefreshCatList();
    }

    /// <summary>
    ///     Makes the cat list match the Clan: the running simulation's Clan, or the starting Clan before it starts.
    ///     Adds rows for new cats, removes rows of cats that are gone (died, or from an earlier simulation)
    ///     and updates the values of all other rows.
    /// </summary>
    private void RefreshCatList() {
        if (_catList == null)
            return;

        var cats = (World?.Clan ?? _nextStartingClan)?.Cats ?? Array.Empty<Cat>();

        // ToList() makes a copy, because rows are removed from _catRows while looping
        foreach (var goneCat in _catRows.Keys.Except(cats).ToList()) {
            _catRows[goneCat].Free();
            _catRows.Remove(goneCat);
        }

        foreach (var cat in cats) {
            if (!_catRows.TryGetValue(cat, out var row)) {
                row = _catList.CreateItem(_catListRoot);
                _catRows[cat] = row;
            }

            FillRow(row, cat);
        }
    }

    public void BuildCatForm() {
        var rankDropdown = GetNode<OptionButton>("TabContainer/Clan/NewCat/Rank");
        var sexDropdown = GetNode<OptionButton>("TabContainer/Clan/NewCat/Sex");
        var createCatButton = GetNode<Button>("TabContainer/Clan/NewCat/CreateCat");

        foreach (var rank in Enum.GetValues<Rank>()) {
            rankDropdown.AddItem(rank.ToString(), (int)rank);
        }
        
        sexDropdown.AddItem(Sex.SheCat.ToString(), (int)Sex.SheCat);
        sexDropdown.AddItem(Sex.Tom.ToString(), (int)Sex.Tom);

        createCatButton.Pressed += OnCreateCatPressed;
    }

    /// <summary>Reads the form, creates the cat and adds it to the cat list. Called by the Create Cat button.</summary>
    private void OnCreateCatPressed() {
        var nameEdit = GetNode<LineEdit>("TabContainer/Clan/NewCat/Name");
        var rankDropdown = GetNode<OptionButton>("TabContainer/Clan/NewCat/Rank");
        var sexDropdown = GetNode<OptionButton>("TabContainer/Clan/NewCat/Sex");
        var ageBox = GetNode<SpinBox>("TabContainer/Clan/NewCat/Age");
        var huntingBox = GetNode<SpinBox>("TabContainer/Clan/NewCat/Hunting");
        var diligenceBox = GetNode<SpinBox>("TabContainer/Clan/NewCat/Diligence");

        var name = nameEdit.Text.Trim();
        // The id of each item is the enum's number (see BuildCatForm), so it turns straight back into the enum
        var rank = (Rank)rankDropdown.GetSelectedId();
        var sex = (Sex)sexDropdown.GetSelectedId();
        // SpinBox values are doubles, the cat needs whole numbers
        var age = (int)ageBox.Value;
        var hunting = (int)huntingBox.Value;
        var diligence = (int)diligenceBox.Value;

        // AddCat refuses empty or taken names and shows a warning in the Output panel
        if (!AddCat(name, rank, sex, age, hunting, diligence))
            return;

        RefreshCatList();

        // Ready for the next cat
        nameEdit.Clear();
    }

    /// <summary>Writes the cat's values into its row, one column per value.</summary>
    private static void FillRow(TreeItem row, Cat cat) {
        row.SetText(0, cat.Name);
        // ToString() turns an enum value into its name: Rank.Warrior becomes "Warrior"
        row.SetText(1, cat.Rank.ToString());
        // For a nicer text than the name in the code ("SheCat"), choose the text yourself
        row.SetText(2, cat.Sex == Sex.SheCat ? "She-cat" : "Tom");
        row.SetText(3, $"{cat.AgeMoons} moons");
        row.SetText(4, cat.Skills[SkillType.Hunting].Level.ToString("0"));
        row.SetText(5, cat.Diligence.ToString());
        // ?. and ?? handle cats without a known mother: then the cell stays empty
        row.SetText(6, cat.Mother?.Name ?? "");
    }

    /// <summary>
    ///     Fills the tuning table from <see cref="TuningSettings" />: one collapsible row per group,
    ///     and one row per value that can be edited right in the table.
    /// </summary>
    private void BuildTuningTable() {
        _tuningTable = GetNode<Tree>("%TuningTable");
        _tuningTable.SetColumnTitle(0, "Setting");
        _tuningTable.SetColumnTitle(1, "Value");
        _tuningTable.SetColumnTitle(2, "Default");
        // Narrow, fixed number columns, so the spin arrows stay next to the value
        _tuningTable.SetColumnExpand(1, false);
        _tuningTable.SetColumnCustomMinimumWidth(1, 220);
        _tuningTable.SetColumnExpand(2, false);
        _tuningTable.SetColumnCustomMinimumWidth(2, 160);

        var root = _tuningTable.CreateItem();
        foreach (var group in TuningSettings.ByGroup()) {
            var groupRow = _tuningTable.CreateItem(root);
            groupRow.SetText(0, group.Key);

            foreach (var setting in group) {
                var row = _tuningTable.CreateItem(groupRow);
                row.SetText(0, setting.Name);
                // InvariantCulture: always "0.005", like the table, not "0,005" from the German Windows settings
                row.SetText(2, Convert.ToString(setting.DefaultValue, CultureInfo.InvariantCulture));
                // Dimmed and centered, so it's clearly just for comparison and apart from the value's spin arrows
                row.SetTextAlignment(2, HorizontalAlignment.Center);
                row.SetCustomColor(2, new Color(1, 1, 1, 0.5f));
                SetUpValueCell(row, setting);
                _tuningRows[setting.Name] = row;
            }
        }

        _tuningTable.ItemEdited += OnTuningValueEdited;
        GetNode<Button>("%ResetTuning").Pressed += OnResetTuningPressed;
    }

    /// <summary>Makes the value cell editable: a dropdown for enums, a number field for everything else.</summary>
    private static void SetUpValueCell(TreeItem row, TuningSetting setting) {
        row.SetCellMode(1, TreeItem.TreeCellMode.Range);
        row.SetEditable(1, true);

        if (setting.Type.IsEnum) {
            // A Range cell with comma-separated text shows a dropdown with these options
            row.SetText(1, string.Join(",", Enum.GetNames(setting.Type)));
        }
        else {
            var step = setting.Type == typeof(int) ? 1 : 0.001;
            row.SetRangeConfig(1, 0, 1_000_000_000, step);
        }

        ShowTuningValue(row, setting);
    }

    /// <summary>Shows the current value in the row and highlights it if it differs from the default.</summary>
    private static void ShowTuningValue(TreeItem row, TuningSetting setting) {
        row.SetRange(1, Convert.ToDouble(setting.Value));

        var changed = !setting.Value.Equals(setting.DefaultValue);
        if (changed) {
            row.SetCustomColor(0, ChangedTuningColor);
            row.SetCustomColor(1, ChangedTuningColor);
        }
        else {
            row.ClearCustomColor(0);
            row.ClearCustomColor(1);
        }
    }

    private void OnTuningValueEdited() {
        var row = _tuningTable.GetEdited();
        if (row == null)
            return;

        var setting = TuningSettings.Get(row.GetText(0));
        if (setting == null)
            return;

        // TuningSettings converts the double from the table to int, float or the enum
        setting.Value = row.GetRange(1);
        ShowTuningValue(row, setting);
    }

    private void OnResetTuningPressed() {
        TuningSettings.ResetToDefaults();
        foreach (var setting in TuningSettings.All)
            ShowTuningValue(_tuningRows[setting.Name], setting);
    }

    /// <summary>Connects the controls of the Simulation tab: start, play/pause, speeds and the log.</summary>
    private void BuildSimulationControls() {
        _timeLabel = GetNode<Label>("%TimeLabel");
        _clanLabel = GetNode<Label>("%ClanLabel");
        _log = GetNode<RichTextLabel>("%Log");
        _playPauseButton = GetNode<Button>("%PlayPause");

        GetNode<Button>("%StartSimulation").Pressed += OnStartSimulationPressed;
        _playPauseButton.Pressed += OnPlayPausePressed;
        GetNode<CheckBox>("%ShowDetails").Toggled += on => _showDetails = on;

        // Every speed button has its speed as metadata "ticks_per_second" (see the Inspector, at the bottom)
        foreach (var button in GetNode("%Speeds").GetChildren().OfType<Button>()) {
            var ticksPerSecond = (float)button.GetMeta("ticks_per_second").AsDouble();
            button.ButtonPressed = Mathf.IsEqualApprox(ticksPerSecond, TicksPerSecond);
            button.Pressed += () => TicksPerSecond = ticksPerSecond;
        }
    }

    /// <summary>Starts a new simulation (or restarts it) and lets it run right away.</summary>
    private void OnStartSimulationPressed() {
        _log.Clear();
        _pendingLog.Clear();

        StartSimulation();
        Play();

        GetNode<Button>("%StartSimulation").Text = "Restart simulation";
        UpdatePlayPauseButton();
    }

    private void OnPlayPausePressed() {
        if (IsPaused)
            Play();
        else
            Pause();
        UpdatePlayPauseButton();
    }

    private void UpdatePlayPauseButton() {
        _playPauseButton.Disabled = World == null;
        _playPauseButton.Text = IsPaused ? "Play" : "Pause";
    }

    /// <summary>Shows the time and an overview of the Clan.</summary>
    private void RefreshSimulationInfo() {
        if (World == null)
            return;

        _timeLabel.Text = $"Day {World.Day}  ·  {World.TimeOfDay}  ·  Moon {World.Moon}  ·  {World.Season}";

        var clan = World.Clan;
        var famine = clan.IsInFamine ? "   ·   FAMINE" : "";
        _clanLabel.Text =
            $"{clan.Name}: {clan.Cats.Count} cats ({clan.Cats.Count(c => c.Rank == Rank.Kit)} kits, "
            + $"{clan.Cats.Count(c => c.IsQueen)} queens)   ·   Fresh-kill pile: {clan.FreshKillPile.Mice} mice   ·   "
            + $"Leader: {clan.Leader?.Name ?? "none"}   ·   Deputy: {clan.Deputy?.Name ?? "none"}{famine}";
    }

    /// <summary>
    ///     Writes the log lines collected since the last frame into the log. Only once per frame,
    ///     because at high speed there can be thousands of lines per second.
    /// </summary>
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

        // Only keep the newest lines, the full log is in logs/simulation.log
        while (_log.GetParagraphCount() > MaxLogLines)
            _log.RemoveParagraph(0);
    }

    public override void _Ready() {
        BuildCatList();
        BuildCatForm();
        BuildTuningTable();
        BuildSimulationControls();
    }

    public override void _Process(double delta) {
        if (World != null && !IsPaused)
            AdvanceSimulation(delta);

        RefreshSimulationInfo();
        FlushLog();
    }

    /// <summary>Runs as many ticks as fit into this frame at the current speed.</summary>
    private void AdvanceSimulation(double delta) {
        _tickProgress += (float)delta * TicksPerSecond;

        var ticks = 0;
        while (_tickProgress >= 1f && ticks < MaxTicksPerFrame) {
            World.Tick();
            _tickProgress -= 1f;
            ticks++;
        }

        // If the computer can't keep up, skip the rest instead of falling further behind
        if (ticks == MaxTicksPerFrame)
            _tickProgress = 0f;

        RefreshCatList();

        EmitSignal(SignalName.SimulationUpdated);

        if (World.Clan.Cats.Count == 0) {
            Pause();
            OnLog(LogLevel.Summary, $"{World.Clan.Name} has died out.");
            UpdatePlayPauseButton();
            EmitSignal(SignalName.ClanDiedOut);
        }
    }

    public override void _ExitTree() {
        _logFile?.Dispose();
    }

    private void OnLog(LogLevel level, string line) {
        _logFile.WriteLine(line);
        _pendingLog.Add((level, line));
        EmitSignal(SignalName.LogLine, line, level == LogLevel.Summary);
    }
}
