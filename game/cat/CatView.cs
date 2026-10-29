using System;
using Godot;
using WarriorsSim.Simulation;

/// <summary>
///     Shows one cat of the simulation. The camp tells it where to go, and it walks there by itself. While it waits, it
///     can wander around its spot a little. It knows nothing about the keyboard, so later a player controller can move
///     it the same way.
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
	private Action _onArrived;
	private Vector2 _spot;
	private float _spotRadius;
	private bool _wanders;
	private float _waitLeft;
	private Label _nameLabel;
	private CatSprite _sprite;

	public void Init(Cat cat) {
		Cat = cat;
	}

	public override void _Ready() {
		_nameLabel = GetNodeOrNull<Label>("%NameLabel");
		_sprite = GetNodeOrNull<CatSprite>("%CatSprite");
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

	/// <summary>Walks straight to <paramref name="target" /> and stops wandering. Calls <paramref name="onArrived" /> there.</summary>
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
		var toTarget = _target - Position;
		var step = Speed * seconds;
		if (toTarget.Length() > step) {
			Velocity = toTarget.Normalized() * Speed;
			Position += Velocity * seconds;
			return;
		}

		Position = _target;
		Velocity = Vector2.Zero;
		_walking = false;
		_waitLeft = (float)GD.RandRange(MinWaitSeconds, MaxWaitSeconds);

		// Taken out first, because the callback may start the next walk with a new one
		var onArrived = _onArrived;
		_onArrived = null;
		onArrived?.Invoke();
	}

	private void MoveTo(Vector2 target, Action onArrived) {
		_target = target;
		_onArrived = onArrived;
		_walking = true;
	}

	/// <summary>A random place near the spot. Uses Godot's random, not the simulation's, so watching changes nothing.</summary>
	private Vector2 NearSpot() {
		var angle = (float)GD.RandRange(0, Mathf.Tau);
		// The square root spreads the cats evenly instead of crowding them in the middle
		var distance = _spotRadius * Mathf.Sqrt(GD.Randf());
		return _spot + Vector2.FromAngle(angle) * distance;
	}
}
