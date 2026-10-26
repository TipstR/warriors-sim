using System.IO;
using Godot;
using WarriorsSim.Simulation;

/// <summary>
///     Runs the Milestone 1 simulation. The full log goes to a file, and only the
///     summaries are printed to Godot's Output panel.
/// </summary>
public partial class SimulationRunner : Node {
    private const string LogFile = "res://logs/simulation.log";

    public override void _Ready() {
        var logPath = ProjectSettings.GlobalizePath(LogFile);
        Directory.CreateDirectory(Path.GetDirectoryName(logPath));

        using var file = new StreamWriter(logPath);
        var world = new World(StartingClans.CreateThunderClan(), Tuning.RandomSeed, (level, line) => {
            file.WriteLine(line);
            if (level == LogLevel.Summary)
                GD.Print(line);
        });

        var ticks = Tuning.DaysToSimulate * Tuning.TicksPerDay;
        for (var i = 0; i < ticks; i++)
            world.Tick();

        GD.Print($"Full log: {logPath}");
    }
}