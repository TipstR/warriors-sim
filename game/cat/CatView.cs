using System;
using Godot;
using WarriorsSim.Simulation;

/// <summary>
///     Shows one cat of the simulation. The camp tells it where to go, and it walks there by itself, around walls if
///     the scene has navigation (a <c>NavigationAgent2D</c> called <c>NavigationAgent</c>, and a NavigationRegion2D in
///     the camp). While it waits, it can wander around its spot a little. It knows nothing about the keyboard, so later
///     a player controller can move it the same way.
/// </summary>
public partial class CatView : Node2D {
	/// <summary>Walking speed in pixels per second.</summary>
	[Export] public float Speed { get; set; } = 50f;

	/// <summary>How long a wandering cat waits before it moves to another place near its spot, in seconds.</summary>
	[Export] public float MinWaitSeconds { get; set; } = 3f;
	[Export] public float MaxWaitSeconds { get; set; } = 10f;

	public Cat Cat { get; private set; }

	/// <summary>Where the cat is going and how fast, in pixels per second. Zero while it stands still. For the animation.</summary>
	public Vector2 Velocity { get; private set; }

	/// <summary>
	///     The middle of the cat's body, in the parent's coordinates. <see cref="Node2D.Position" /> is where its paws
	///     touch the ground, so cats further down are drawn in front.
	/// </summary>
	public Vector2 BodyCenter => Position + (_sprite?.Position ?? Vector2.Zero);

	private Vector2 _target;
	private bool _walking;

	/// <summary>Whether the cat follows a path around walls, or walks straight (no navigation yet).</summary>
	private bool _followsPath;
	private Action _onArrived;
	private Vector2 _spot;
	private float _spotRadius;
	private bool _wanders;
	private float _waitLeft;
	private Label _nameLabel;
	private CatSprite _sprite;
	private NavigationAgent2D _agent;

	public void Init(Cat cat) {
		Cat = cat;
	}

	public override void _Ready() {
		_nameLabel = GetNodeOrNull<Label>("%NameLabel");
		_sprite = GetNodeOrNull<CatSprite>("%CatSprite");
		_agent = GetNodeOrNull<NavigationAgent2D>("%NavigationAgent");
	}

	/// <summary>
	///     Whether the walkable area is known. It isn't in the very first frame, and not at all in a scene without a
	///     NavigationRegion2D.
	/// </summary>
	public static bool IsNavigationReady(Node2D node) {
		return NavigationServer2D.MapGetIterationId(node.GetWorld2D().NavigationMap) > 0;
	}

	/// <summary>
	///     Walks to a random place at most <paramref name="radius" /> pixels from <paramref name="spot" /> and stays
	///     there. If <paramref name="wander" /> is true, it moves to another place near the spot now and then.
	///     A cat that is away (hidden) comes back right there.
	/// </summary>
	public void StayAt(Vector2 spot, float radius, bool wander) {
		if (!Visible) {
			Position = spot;
			Visible = true;
		}

		_spot = spot;
		_spotRadius = radius;
		_wanders = wander;
		MoveTo(NearSpot(), null);
	}

	/// <summary>Walks to <paramref name="target" /> and stops wandering. Calls <paramref name="onArrived" /> there.</summary>
	public void WalkTo(Vector2 target, Action onArrived = null) {
		_wanders = false;
		MoveTo(target, onArrived);
	}

	/// <summary>Walks out of camp and is hidden until it comes back.</summary>
	public void LeaveThrough(Vector2 exit) {
		WalkTo(exit, () => Visible = false);
	}

	/// <summary>Appears at the camp exit, then walks to <paramref name="target" /> and calls <paramref name="onArrived" />.</summary>
	public void ComeBackThrough(Vector2 exit, Vector2 target, Action onArrived = null) {
		Position = exit;
		Visible = true;
		WalkTo(target, onArrived);
	}

	public override void _Process(double delta) {
		if (_nameLabel != null && Cat != null)
			_nameLabel.Text = Cat.Name;

		var seconds = (float)delta;
		if (_walking)
			Walk(seconds);
		else if (_wanders) {
			_waitLeft -= seconds;
			if (_waitLeft <= 0f)
				MoveTo(NearSpot(), null);
		}

		_sprite?.Animate(Velocity);
	}

	private void Walk(float seconds) {
		var step = Speed * seconds;
		if (Position.DistanceTo(_target) <= step) {
			Position = _target;
			Arrive();
			return;
		}

		// The path ends early when the target can't be reached: the cat stops as close as it gets
		if (_followsPath && _agent.IsNavigationFinished()) {
			Arrive();
			return;
		}

		var waypoint = _followsPath ? ToParent(_agent.GetNextPathPosition()) : _target;
		Velocity = (waypoint - Position).Normalized() * Speed;
		Position += Velocity * seconds;
	}

	private void Arrive() {
		Velocity = Vector2.Zero;
		_walking = false;
		_waitLeft = (float)GD.RandRange(MinWaitSeconds, MaxWaitSeconds);

		// Taken out first, because the callback may start the next walk with a new one
		var onArrived = _onArrived;
		_onArrived = null;
		onArrived?.Invoke();
	}

	private void MoveTo(Vector2 target, Action onArrived) {
		_followsPath = _agent != null && IsNavigationReady(this);
		_target = _followsPath ? ClosestWalkable(target) : target;
		_onArrived = onArrived;
		_walking = true;

		if (_followsPath)
			_agent.TargetPosition = FromParent(_target);
	}

	/// <summary>The walkable point closest to <paramref name="point" />, e.g. when a random spot lies inside a tree.</summary>
	private Vector2 ClosestWalkable(Vector2 point) {
		var map = GetWorld2D().NavigationMap;
		return ToParent(NavigationServer2D.MapGetClosestPoint(map, FromParent(point)));
	}

	// The camp gives positions in the coordinates of the cats' parent node, the navigation works with global ones
	private Vector2 FromParent(Vector2 point) {
		return GetParent<Node2D>().ToGlobal(point);
	}

	private Vector2 ToParent(Vector2 globalPoint) {
		return GetParent<Node2D>().ToLocal(globalPoint);
	}

	/// <summary>
	///     A random walkable place near the spot. Near on foot: a place just behind a thin wall is close as the crow
	///     flies, but the cat would have to walk around the whole camp to get there, so it tries another one.
	///     Uses Godot's random, not the simulation's, so watching changes nothing.
	/// </summary>
	private Vector2 NearSpot() {
		if (_agent == null || !IsNavigationReady(this))
			return RandomPointNear(_spot);

		for (var attempt = 0; attempt < MaxSpotAttempts; attempt++) {
			var point = ClosestWalkable(RandomPointNear(_spot));
			if (WalkingDistance(_spot, point) <= _spotRadius * MaxDetour)
				return point;
		}

		return ClosestWalkable(_spot);
	}

	private const int MaxSpotAttempts = 10;

	/// <summary>How much longer than the spot's radius the way from the spot to a random place may be.</summary>
	private const float MaxDetour = 1.5f;

	private Vector2 RandomPointNear(Vector2 spot) {
		var angle = (float)GD.RandRange(0, Mathf.Tau);
		// The square root spreads the cats evenly instead of crowding them in the middle
		var distance = _spotRadius * Mathf.Sqrt(GD.Randf());
		return spot + Vector2.FromAngle(angle) * distance;
	}

	/// <summary>How far a cat walks from <paramref name="from" /> to <paramref name="to" />, around walls.</summary>
	private float WalkingDistance(Vector2 from, Vector2 to) {
		var path = NavigationServer2D.MapGetPath(GetWorld2D().NavigationMap, FromParent(from), FromParent(to), true);
		if (path.Length == 0)
			return float.MaxValue;

		var length = 0f;
		for (var i = 1; i < path.Length; i++)
			length += path[i - 1].DistanceTo(path[i]);
		return length;
	}
}
