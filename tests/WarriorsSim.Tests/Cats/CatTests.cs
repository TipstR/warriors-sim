using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

public class CatTests {
    /// <summary>xUnit creates a new instance for every test, so every test starts with the default balancing values.</summary>
    public CatTests() {
        TuningSettings.ResetToDefaults();
    }

    [Theory]
    [InlineData(0, 1f)] // newborn kit: fully frail
    [InlineData(3, 0.5f)] // halfway to KitFrailtyEndsMoons
    [InlineData(6, 0f)] // apprentice age: sturdy
    [InlineData(50, 0f)]
    [InlineData(96, 0f)] // OldAgeStartMoons: frailty starts growing again
    [InlineData(138, 0.5f)] // halfway to FullyFrailAgeMoons
    [InlineData(180, 1f)] // FullyFrailAgeMoons
    [InlineData(250, 1f)] // never more than fully frail
    public void Frailty_DependsOnAge(int ageMoons, float expected) {
        var cat = new Cat("Graypaw", Rank.Apprentice, Sex.Tom, ageMoons, 30, 50);

        Assert.Equal(expected, cat.Frailty, 3);
    }

    [Fact]
    public void Frailty_DoesNotDependOnRank() {
        var elder = new Cat("Halftail", Rank.Elder, Sex.Tom, 138, 20, 50);
        var leader = new Cat("Bluestar", Rank.Leader, Sex.SheCat, 138, 70, 80);

        Assert.Equal(elder.Frailty, leader.Frailty);
    }
}
