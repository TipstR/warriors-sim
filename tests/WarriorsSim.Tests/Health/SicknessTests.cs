using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

/// <summary>A sick cat: how sick it is, whether it spreads its sickness, and when it can catch one again.</summary>
public class SicknessTests {
	public SicknessTests() {
		TuningSettings.ResetToDefaults();
	}

	private static Cat Warrior() {
		return new Cat("Longtail", Rank.Warrior, Sex.Tom, 30, 50);
	}

	private static Condition Whitecough(float severity = 20f, float recovery = 0f) {
		return new Condition(ConditionType.Whitecough, severity, 1f, recovery);
	}

	private static Condition Wound(float severity = 20f) {
		return new Condition(ConditionType.Wound, severity, 1f);
	}

	[Fact]
	public void IsSick_OnlyWithACondition() {
		var cat = Warrior();
		Assert.False(cat.IsSick);

		cat.AddCondition(Wound());

		Assert.True(cat.IsSick);
	}

	[Fact]
	public void TotalSeverity_AllConditionsTogetherButNoMoreThanMax() {
		var cat = Warrior();
		cat.AddCondition(Wound(30f));
		cat.AddCondition(Whitecough(25f));
		Assert.Equal(55f, cat.TotalSeverity, 4);

		cat.AddCondition(Whitecough(60f));
		Assert.Equal(Condition.Max, cat.TotalSeverity, 4);
	}

	[Fact]
	public void IsConfined_FromTheConfinedSeverityOnTogether() {
		var cat = Warrior();
		cat.AddCondition(Wound(Tuning.ConfinedSeverity / 2));
		Assert.False(cat.IsConfined);

		cat.AddCondition(Whitecough(Tuning.ConfinedSeverity / 2));
		Assert.True(cat.IsConfined);
	}

	[Fact]
	public void HealthFactor_FullWhenHealthyLessTheSickerTheCat() {
		var cat = Warrior();
		Assert.Equal(1f, cat.HealthFactor, 4);

		cat.AddCondition(Wound(25f));
		Assert.Equal(0.75f, cat.HealthFactor, 4);
	}

	[Fact]
	public void HasCondition_OnlyTheConditionsTheCatHas() {
		var cat = Warrior();
		cat.AddCondition(Wound());
		cat.AddCondition(Whitecough());

		Assert.True(cat.HasCondition(ConditionType.Wound));
		Assert.True(cat.HasCondition(ConditionType.Whitecough));
		Assert.False(cat.HasCondition(ConditionType.Greencough));
	}

	[Fact]
	public void IsContagious_OneContagiousConditionIsEnough() {
		var wounded = Warrior();
		var coughing = Warrior();
		wounded.AddCondition(Wound());
		coughing.AddCondition(Wound());
		coughing.AddCondition(Whitecough());

		Assert.False(wounded.IsContagious);
		Assert.True(coughing.IsContagious);
	}

	[Fact]
	public void IsContagious_NotOnceTheCatIsHealing() {
		var cat = Warrior();
		cat.AddCondition(Whitecough(recovery: Condition.Max));

		Assert.False(cat.IsContagious);
	}

	[Fact]
	public void Spreads_OnlyTheSicknessTheCatHas() {
		var cat = Warrior();
		cat.AddCondition(Wound());
		cat.AddCondition(Whitecough());

		Assert.True(cat.Spreads(ConditionType.Whitecough));
		Assert.False(cat.Spreads(ConditionType.Greencough));
		Assert.False(cat.Spreads(ConditionType.Wound));
	}

	[Fact]
	public void CanCatch_NotWhileItHasIt() {
		var cat = Warrior();
		cat.AddCondition(Whitecough());

		Assert.False(cat.CanCatch(ConditionType.Whitecough, 10));
		Assert.True(cat.CanCatch(ConditionType.Greencough, 10));
	}

	[Fact]
	public void CanCatch_AfterGettingOverItOnlyOnceTheImmunityHasPassed() {
		var cat = Warrior();
		var whitecough = Whitecough();
		cat.AddCondition(whitecough);

		cat.RemoveCondition(whitecough, 100);

		Assert.False(cat.IsSick);
		Assert.False(cat.CanCatch(ConditionType.Whitecough, 50));
		Assert.False(cat.CanCatch(ConditionType.Whitecough, 100));
		Assert.True(cat.CanCatch(ConditionType.Whitecough, 101));
		Assert.True(cat.CanCatch(ConditionType.Greencough, 50));
	}

	[Fact]
	public void ReplaceCondition_TheWorseOneTakesItsPlaceWithoutImmunity() {
		var cat = Warrior();
		var whitecough = Whitecough();
		var greencough = new Condition(ConditionType.Greencough, 30f, 1f);
		cat.AddCondition(Wound());
		cat.AddCondition(whitecough);

		cat.ReplaceCondition(whitecough, greencough);

		Assert.Equal(2, cat.Conditions.Count);
		Assert.Same(greencough, cat.Conditions[1]);
		Assert.False(cat.HasCondition(ConditionType.Whitecough));
		Assert.True(cat.CanCatch(ConditionType.Whitecough, 10));
	}
}
