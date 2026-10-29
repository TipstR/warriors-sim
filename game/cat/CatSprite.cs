using Godot;

/// <summary>
///     The animated sprite of a cat, used by the player and by the simulated cats, so new animations only have to be
///     added once (in <c>cat_sprite.tscn</c>). Walking animations are called "walk_" plus one of 8 directions (e.g.
///     "walk_north_east"), sitting animations "sitting_" plus east, west, north or south.
/// </summary>
public partial class CatSprite : AnimatedSprite2D {
	/// <summary>
	///     How much of the movement has to go one way to count (sin 22.5°). Without it, a cat walking almost straight
	///     east would look north-east.
	/// </summary>
	private const float DirectionThreshold = 0.38f;

	/// <summary>Which way the cat faces when it sits down. Sitting only has four directions.</summary>
	private string _sittingDirection = "south";

	/// <summary>Plays the walking animation for the direction the cat moves in, or sits down when it stands still.</summary>
	public void Animate(Vector2 velocity) {
		if (velocity.LengthSquared() < 0.01f) {
			Play($"sitting_{_sittingDirection}");
			return;
		}

		var direction = velocity.Normalized();
		var vertical = direction.Y > DirectionThreshold ? "south" : direction.Y < -DirectionThreshold ? "north" : "";
		var horizontal = direction.X > DirectionThreshold ? "east" : direction.X < -DirectionThreshold ? "west" : "";

		// Like the player's old code: when walking diagonally, the cat sits facing sideways
		_sittingDirection = horizontal != "" ? horizontal : vertical;
		var name = vertical != "" && horizontal != "" ? $"{vertical}_{horizontal}" : vertical + horizontal;
		Play($"walk_{name}");
	}
}
