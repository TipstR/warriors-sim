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
        
        
            
        // Animation
        var animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        
        string vertical = velocity.Y > 0 ? "south" : (velocity.Y < 0 ? "north" : "");
        string horizontal = velocity.X > 0 ? "east" : (velocity.X < 0 ? "west" : "");
        
        string direction = "";
        if (vertical != "" && horizontal != "")
        {
            direction = $"{vertical}_{horizontal}";
        }
        else
        {
            direction = vertical + horizontal; // Falls nur eine Achse aktiv ist (z.B. nur "north" oder "east")
        }

        
        

        if (velocity.Length() > 0)
        {
            velocity = velocity.Normalized() * Speed;
            animatedSprite2D.Play($"walk_{direction}");
            animatedSprite2D.Play();
        }
        else
        {
            animatedSprite2D.Stop();
        }
        
        Position += velocity * (float)delta;
        Position = new Vector2(
            x: Mathf.Clamp(Position.X, 0, ScreenSize.X),
            y: Mathf.Clamp(Position.Y, 0, ScreenSize.Y)
        );
    }
    
}