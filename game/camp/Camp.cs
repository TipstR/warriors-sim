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
///         <c>LeaderDen</c>, <c>MedicineDen</c>. A Label <c>InfoLabel</c> is optional, and so is a NavigationRegion2D
///         <c>Navigation</c>: without it, the cats walk straight through walls.
///     </para>
/// </summary>
public partial class Camp : Node2D {
	/// <summary>How long one tick (6 hours) takes in real time. 15 seconds make a day one minute.</summary>
	[Export] public float SecondsPerTick { get; set; } = 15f;

	/// <summary>The scene every cat is shown with. Its root needs the <see cref="CatView" /> script.</summary>
	[Export] public PackedScene CatScene { get; set; }

	// All times below are shares of a tick (0.3 = after 30% of it). Every cat gets its own random time in the range,
	// so they don't all move at once.

	/// <summary>Cats that stay in camp take up their new activity within this share of the tick.</summary>
	[Export] public float StartWithin { get; set; } = 0.3f;

	/// <summary>Patrols and herb gatherers leave within this share of the tick.</summary>
	[Export] public float LeaveWithin { get; set; } = 0.25f;

	/// <summary>How long a patrol or herb gatherer stays away, at least and at most.</summary>
	[Export] public float AwayMin { get; set; } = 0.35f;
	[Export] public float AwayMax { get; set; } = 0.6f;

	/// <summary>When the cats who ate this tick go to the fresh-kill pile, if they weren't out.</summary>
	[Export] public float EatFrom { get; set; } = 0.15f;
	[Export] public float EatTo { get; set; } = 0.9f;

	/// <summary>How long a cat back from a patrol waits before it eats, in seconds.</summary>
	[Export] public float EatAfterReturnSeconds { get; set; } = 2f;

	/// <summary>How far apart the cats of a patrol walk, so they don't all stand on the same spot, in pixels.</summary>
	[Export] public float GroupSpread { get; set; } = 13f;

	/// <summary>How far from the middle of the clearing the cats spread out, in pixels.</summary>
	[Export] public float ClearingRadius { get; set; } = 160f;

	/// <summary>How far from the middle of a den the cats spread out, in pixels.</summary>
	[Export] public float DenRadius { get; set; } = 48f;

	/// <summary>How close to a cat a click has to be to follow it, in pixels.</summary>
	[Export] public float ClickRadius { get; set; } = 16f;

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

		// Works out where cats can walk from the blocking tiles painted right now, so painting is all it needs
		GetNodeOrNull<NavigationRegion2D>("%Navigation")?.BakeNavigationPolygon(false);

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

		// The walkable area is only ready after the first frame. Without it, the first patrols would walk through walls.
		var waitsForNavigation = HasNode("%Navigation") && !CatView.IsNavigationReady(this);
		if (_secondsSinceTick >= SecondsPerTick && !waitsForNavigation) {
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
			.Where(v => v.Visible && v.BodyCenter.DistanceTo(mouse) <= ClickRadius)
			.OrderBy(v => v.BodyCenter.DistanceTo(mouse))
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

		// When each cat that went out is back in camp, in seconds since the tick
		var backAt = new Dictionary<Cat, float>();
		foreach (var patrol in _patrolsThisTick)
			PlanTrip(patrol.Cats, Spot("FreshKillPile"), backAt);
		foreach (var gatherer in _views.Keys.Where(c => c.Activity == Activity.GatheringHerbs))
			PlanTrip(new[] { gatherer }, Spot("MedicineDen"), backAt);

		foreach (var cat in _views.Keys.Where(c => !backAt.ContainsKey(c)))
			Plan(RandomTime(0f, StartWithin), () => SendHome(cat));

		foreach (var cat in _ateThisTick) {
			var eatAt = backAt.TryGetValue(cat, out var back)
				? back + EatAfterReturnSeconds
				: RandomTime(EatFrom, EatTo);
			Plan(eatAt, () => GoEat(cat));
		}
	}

	/// <summary>
	///     Plans a trip out of camp for a group of cats: they leave together at a random time, stay away for a random
	///     while, then bring what they found to <paramref name="dropAt" />. Writes when they are back into
	///     <paramref name="backAt" />.
	/// </summary>
	private void PlanTrip(IReadOnlyList<Cat> cats, Vector2 dropAt, Dictionary<Cat, float> backAt) {
		var leave = RandomTime(0f, LeaveWithin);
		var back = leave + RandomTime(AwayMin, AwayMax);
		foreach (var cat in cats)
			backAt[cat] = back;

		Plan(leave, () => {
			foreach (var cat in cats)
				if (_views.TryGetValue(cat, out var view))
					view.LeaveThrough(Spot("CampExit") + RandomOffset(GroupSpread));
		});
		Plan(back, () => ComeBack(cats, dropAt));
	}

	/// <summary>A random time between two shares of the tick, in seconds since the tick.</summary>
	private float RandomTime(float fromShare, float toShare) {
		return (float)GD.RandRange(fromShare, toShare) * SecondsPerTick;
	}

	private static Vector2 RandomOffset(float radius) {
		return Vector2.FromAngle((float)GD.RandRange(0, Mathf.Tau)) * radius * GD.Randf();
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
			view.ComeBackThrough(exit + RandomOffset(GroupSpread), dropAt + RandomOffset(GroupSpread),
				() => SendHome(cat));
		}
	}

	/// <summary>The cat walks to the fresh-kill pile, eats, and goes back home.</summary>
	private void GoEat(Cat cat) {
		if (!_views.TryGetValue(cat, out var view) || !view.Visible)
			return;
		view.WalkTo(Spot("FreshKillPile") + RandomOffset(GroupSpread), () => SendHome(cat));
	}

	private void SendHome(Cat cat) {
		if (!_views.TryGetValue(cat, out var view))
			return;
		var (spot, radius) = HomeSpot(cat);
		view.StayAt(spot, radius, Wanders(cat));
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
