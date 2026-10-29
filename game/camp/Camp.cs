using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WarriorsSim.Simulation;

/// <summary>
///     Runs the simulation in real time and shows it in the camp. Every <see cref="SecondsPerTick" /> seconds the world
///     ticks once (6 hours), and every cat walks to where its <see cref="Activity" /> takes it: patrols leave through
///     the camp exit and come back later with prey, sleeping cats go to their den, hungry cats to the fresh-kill pile.
///     Click a cat to follow it with the camera.
///     <para>
///         The scene needs these nodes with a unique name (%): <c>Cats</c> (a Node2D the cat views go into),
///         <c>Camera</c> (a Camera2D), and Marker2D spots: <c>CampExit</c>, <c>Clearing</c>, <c>FreshKillPile</c>,
///         and one per den: <c>WarriorsDen</c>, <c>ApprenticesDen</c>, <c>NurseryDen</c>, <c>EldersDen</c>,
///         <c>LeaderDen</c>, <c>MedicineDen</c>. A Label <c>InfoLabel</c> is optional.
///     </para>
/// </summary>
public partial class Camp : Node2D {
    /// <summary>How long one tick (6 hours) takes in real time. 15 seconds make a day one minute.</summary>
    [Export] public float SecondsPerTick { get; set; } = 15f;

    /// <summary>The scene every cat is shown with. Its root needs the <see cref="CatView" /> script.</summary>
    [Export] public PackedScene CatScene { get; set; }

    /// <summary>When patrols and herb gatherers come back, as a share of the tick (0.6 = after 60% of it).</summary>
    [Export] public float ReturnAt { get; set; } = 0.6f;

    /// <summary>When the cats who ate this tick go to the fresh-kill pile, as a share of the tick.</summary>
    [Export] public float EatAt { get; set; } = 0.75f;

    /// <summary>How far from the middle of the clearing the cats spread out, in pixels.</summary>
    [Export] public float ClearingRadius { get; set; } = 350f;

    /// <summary>How far from the middle of a den the cats spread out, in pixels.</summary>
    [Export] public float DenRadius { get; set; } = 90f;

    /// <summary>How close to a cat a click has to be to follow it, in pixels.</summary>
    [Export] public float ClickRadius { get; set; } = 50f;

    /// <summary>The running simulation.</summary>
    public World World { get; private set; }

    /// <summary>The cat the camera follows, or null.</summary>
    public Cat FollowedCat { get; private set; }

    private readonly Dictionary<Cat, CatView> _views = new();
    private Node2D _catsNode;
    private Camera2D _camera;
    private Label _infoLabel;
    private float _secondsSinceTick;

    /// <summary>The time of the tick the cats are showing. The world is already at the next one.</summary>
    private (int Day, TimeOfDay Time, Season Season) _shownTime;

    // What happened during the last tick, collected from the world's events
    private readonly List<Patrol> _patrolsThisTick = new();
    private readonly List<Cat> _ateThisTick = new();

    /// <summary>
    ///     What happens later in this tick, e.g. "after 9 seconds the patrol comes back". Sorted by time.
    ///     Cleared at every tick.
    /// </summary>
    private readonly List<(float AtSecond, Action Do)> _timetable = new();

    public override void _Ready() {
        _catsNode = GetNode<Node2D>("%Cats");
        _camera = GetNode<Camera2D>("%Camera");
        _infoLabel = GetNodeOrNull<Label>("%InfoLabel");

        World = new World(StartingClans.CreateThunderClan(), Tuning.RandomSeed, OnLog);
        World.PatrolSent += _patrolsThisTick.Add;
        World.CatAte += _ateThisTick.Add;
        World.Clan.CatAdded += cat => AddView(cat);
        World.Clan.CatDied += RemoveView;

        foreach (var cat in World.Clan.Cats) {
            var view = AddView(cat);
            view.Position = HomeSpot(cat).Spot;
        }

        FollowedCat = World.Clan.Leader;
        // The first tick right away, so something happens from the start
        _secondsSinceTick = SecondsPerTick;
    }

    public override void _Process(double delta) {
        _secondsSinceTick += (float)delta;

        if (_secondsSinceTick >= SecondsPerTick) {
            _secondsSinceTick = 0f;
            Tick();
        }

        while (_timetable.Count > 0 && _timetable[0].AtSecond <= _secondsSinceTick) {
            var next = _timetable[0];
            _timetable.RemoveAt(0);
            next.Do();
        }

        if (FollowedCat != null && _views.TryGetValue(FollowedCat, out var followed))
            _camera.Position = _catsNode.Position + followed.Position;

        UpdateInfoLabel();
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            return;

        var mouse = _catsNode.GetLocalMousePosition();
        var nearest = _views.Values
            .Where(v => v.Visible && v.Position.DistanceTo(mouse) <= ClickRadius)
            .OrderBy(v => v.Position.DistanceTo(mouse))
            .FirstOrDefault();
        if (nearest != null)
            FollowedCat = nearest.Cat;
    }

    /// <summary>Lets the world tick once and plans what the cats do until the next tick.</summary>
    private void Tick() {
        _patrolsThisTick.Clear();
        _ateThisTick.Clear();
        _timetable.Clear();

        _shownTime = (World.Day, World.TimeOfDay, World.Season);
        World.Tick();

        var exit = Spot("CampExit");
        foreach (var (cat, view) in _views) {
            if (IsAway(cat))
                view.LeaveThrough(exit);
            else
                SendHome(cat, view);
        }

        var returnAt = ReturnAt * SecondsPerTick;
        foreach (var patrol in _patrolsThisTick)
            Plan(returnAt, () => ComeBack(patrol.Cats, Spot("FreshKillPile")));

        var gatherers = _views.Keys.Where(c => c.Activity == Activity.GatheringHerbs).ToList();
        Plan(returnAt, () => ComeBack(gatherers, Spot("MedicineDen")));

        // Copied, because the list is cleared at the next tick
        var ate = _ateThisTick.ToList();
        Plan(EatAt * SecondsPerTick, () => GoEat(ate));
    }

    /// <summary>Adds something to the timetable of this tick, keeping it sorted by time.</summary>
    private void Plan(float atSecond, Action action) {
        _timetable.Add((atSecond, action));
        _timetable.Sort((a, b) => a.AtSecond.CompareTo(b.AtSecond));
    }

    /// <summary>The cats come back into camp, drop their prey or herbs at <paramref name="dropAt" />, then go home.</summary>
    private void ComeBack(IEnumerable<Cat> cats, Vector2 dropAt) {
        var exit = Spot("CampExit");
        foreach (var cat in cats) {
            // A cat can die while it is out
            if (!_views.TryGetValue(cat, out var view))
                continue;
            view.ComeBackThrough(exit, dropAt, () => SendHome(cat, view));
        }
    }

    /// <summary>The cats walk to the fresh-kill pile, eat, and go back home.</summary>
    private void GoEat(IEnumerable<Cat> cats) {
        var pile = Spot("FreshKillPile");
        foreach (var cat in cats) {
            if (!_views.TryGetValue(cat, out var view) || !view.Visible)
                continue;
            view.WalkTo(pile, () => SendHome(cat, view));
        }
    }

    private void SendHome(Cat cat, CatView view) {
        var (spot, radius) = HomeSpot(cat);
        view.StayAt(spot, radius, Wanders(cat));
    }

    private static bool IsAway(Cat cat) {
        return cat.Activity is Activity.Hunting or Activity.Training or Activity.GatheringHerbs;
    }

    /// <summary>Awake cats move around a little (also a patrol back in camp), sleeping and sick cats stay in their nest.</summary>
    private static bool Wanders(Cat cat) {
        return cat.Activity is not (Activity.Sleeping or Activity.Resting);
    }

    /// <summary>
    ///     Where in camp the cat stays this tick, and how far it spreads out. Sleeping and resting cats lie in their den.
    ///     Awake warriors and apprentices spend their time in the clearing, the others near their den: kits and queens
    ///     by the nursery, elders by the elders' den, medicine cats by the medicine den.
    /// </summary>
    private (Vector2 Spot, float Radius) HomeSpot(Cat cat) {
        if (cat.Activity == Activity.Caring)
            return (Spot("MedicineDen"), DenRadius);

        var staysNearDen = cat.Activity is Activity.Sleeping or Activity.Resting
                           || cat.Den is Den.Nursery or Den.Elders or Den.Medicine;
        return staysNearDen ? (Spot($"{cat.Den}Den"), DenRadius) : (Spot("Clearing"), ClearingRadius);
    }

    /// <summary>The position of a Marker2D in the camp, in the coordinates of the cats.</summary>
    private Vector2 Spot(string name) {
        return _catsNode.ToLocal(GetNode<Marker2D>($"%{name}").GlobalPosition);
    }

    private CatView AddView(Cat cat) {
        var view = CatScene.Instantiate<CatView>();
        view.Init(cat);
        // A newborn kit appears in the nursery. The starting cats are placed by _Ready.
        view.Position = Spot("NurseryDen");
        _catsNode.AddChild(view);
        _views[cat] = view;
        return view;
    }

    private void RemoveView(Cat cat) {
        if (!_views.Remove(cat, out var view))
            return;

        view.QueueFree();
        if (FollowedCat == cat)
            FollowedCat = null;
    }

    /// <summary>A first, plain overview: the time, and what the followed cat is doing.</summary>
    private void UpdateInfoLabel() {
        if (_infoLabel == null)
            return;

        var (day, time, season) = _shownTime;
        var text = $"{World.Clan.Name}, day {day}, {time}, {season}. "
                   + $"{World.Clan.Cats.Count} cats, pile: {World.Clan.FreshKillPile.Mice} mice";
        var cat = FollowedCat;
        if (cat != null) {
            var state = new List<string> { cat.Activity.ToString() };
            if (cat.IsStarving)
                state.Add("starving");
            else if (cat.IsHungry)
                state.Add("hungry");
            state.AddRange(cat.Conditions.Select(c => c.Name));
            text += $"\nFollowing {cat.Name} ({cat.Rank}, {cat.AgeMoons} moons): {string.Join(", ", state)}";
        }
        else {
            text += "\nClick a cat to follow it.";
        }

        _infoLabel.Text = text;
    }

    /// <summary>For now the important lines go to Godot's output. Later the camp gets its own log.</summary>
    private static void OnLog(LogLevel level, string line) {
        if (level == LogLevel.Summary)
            GD.Print(line);
    }
}
