using Godot;

public partial class Player : Area2D {
    public Vector2 ScreenSize;
    [Export] public int Speed { get; set; } = 400;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        ScreenSize = GetViewportRect().Size;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        var velocity = Vector2.Zero;
        
        if (Input.IsActionPressed("move_north")) {
            velocity.Y -= 1;
        }

        if (Input.IsActionPressed("move_south")) {
            velocity.Y += 1;
        }

        if (Input.IsActionPressed("move_west")) {
            velocity.X -= 1;
        }

        if (Input.IsActionPressed("move_east")) {
            velocity.X += 1;
        }
        
        
            
        if (velocity.Length() > 0) {
            velocity = velocity.Normalized() * Speed;
        }

        // Animation (shared with the simulated cats, see CatSprite)
        GetNode<CatSprite>("AnimatedSprite2D").Animate(velocity);

        Position += velocity * (float)delta;
        Position = new Vector2(
            x: Mathf.Clamp(Position.X, 0, ScreenSize.X),
            y: Mathf.Clamp(Position.Y, 0, ScreenSize.Y)
        );
    }
    
}