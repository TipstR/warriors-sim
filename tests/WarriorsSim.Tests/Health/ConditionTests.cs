using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

/// <summary>
///     A sickness or injury is a race: the severity grows, the body fights back. If the body wins, the cat heals;
///     if the severity reaches the maximum, the cat dies.
/// </summary>
public class ConditionTests {
	public ConditionTests() {
		TuningSettings.ResetToDefaults();
	}

	[Fact]
	public void New_SeverityAndRecoveryStayBetweenZeroAndMax() {
		var tooHigh = new Condition(ConditionType.Greencough, Condition.Max + 50f, 1f, Condition.Max + 50f);
		var tooLow = new Condition(ConditionType.Greencough, -10f, 1f, -10f);

		Assert.Equal(Condition.Max, tooHigh.Severity);
		Assert.Equal(Condition.Max, tooHigh.Recovery);
		Assert.Equal(0f, tooLow.Severity);
		Assert.Equal(0f, tooLow.Recovery);
	}

	[Theory]
	[InlineData(ConditionType.Whitecough, "whitecough")]
	[InlineData(ConditionType.Greencough, "greencough")]
	[InlineData(ConditionType.Wound, "a wound")]
	[InlineData(ConditionType.InfectedWound, "an infected wound")]
	public void Name_TheUsualNameForTheLog(ConditionType type, string name) {
		Assert.Equal(name, new Condition(type, 10f, 1f).Name);
	}

	[Fact]
	public void Name_ADescriptionReplacesTheUsualName() {
		Assert.Equal("a sprained paw", new Condition(ConditionType.Wound, 10f, 1f, description: "a sprained paw").Name);
	}

	[Fact]
	public void Progress_BeforeHealingTheSeverityGrowsByTheCaseSpeedAndTheBodyFightsBack() {
		var condition = new Condition(ConditionType.Whitecough, 10f, 1.5f);

		condition.Progress(4f, 20f, 99f);

		Assert.Equal(16f, condition.Severity, 4);
		Assert.Equal(20f, condition.Recovery, 4);
		Assert.False(condition.IsHealing);
	}

	[Fact]
	public void Progress_OnceRecoveryIsFullTheCatHeals() {
		var condition = new Condition(ConditionType.Whitecough, 30f, 1f, Condition.Max - 5f);

		condition.Progress(2f, 10f, 99f);
		Assert.True(condition.IsHealing);
		Assert.Equal(Condition.Max, condition.Recovery);
		Assert.Equal(32f, condition.Severity, 4);

		condition.Progress(2f, 10f, 5f);
		Assert.Equal(27f, condition.Severity, 4);
		Assert.Equal(Condition.Max, condition.Recovery);
	}

	[Fact]
	public void Progress_HealingEndsInACure() {
		var condition = new Condition(ConditionType.Whitecough, 3f, 1f, Condition.Max);

		Assert.False(condition.IsCured);
		condition.Progress(2f, 10f, 5f);

		Assert.Equal(0f, condition.Severity);
		Assert.True(condition.IsCured);
	}

	[Fact]
	public void IsCured_NotWhileTheBodyIsStillFighting() {
		var fresh = new Condition(ConditionType.Wound, 0f, 1f);

		Assert.False(fresh.IsCured);
	}

	[Fact]
	public void IsHealing_OnlyWithFullRecovery() {
		Assert.False(new Condition(ConditionType.Wound, 10f, 1f, Condition.Max - 1f).IsHealing);
		Assert.True(new Condition(ConditionType.Wound, 10f, 1f, Condition.Max).IsHealing);
	}

	[Fact]
	public void Progress_TheSeverityStopsAtTheMaximumAndKills() {
		var condition = new Condition(ConditionType.Greencough, Condition.Max - 5f, 2f);

		Assert.False(condition.IsDeadly);
		condition.Progress(10f, 1f, 0f);

		Assert.Equal(Condition.Max, condition.Severity);
		Assert.True(condition.IsDeadly);
	}

	[Theory]
	[InlineData(ConditionType.Whitecough, true)]
	[InlineData(ConditionType.Greencough, true)]
	[InlineData(ConditionType.Wound, false)]
	[InlineData(ConditionType.InfectedWound, false)]
	public void IsContagious_OnlyCoughs(ConditionType type, bool contagious) {
		Assert.Equal(contagious, new Condition(type, 30f, 1f).IsContagious);
	}

	[Fact]
	public void IsContagious_NotAnymoreOnceTheCatIsHealing() {
		Assert.False(new Condition(ConditionType.Greencough, 30f, 1f, Condition.Max).IsContagious);
	}
}
