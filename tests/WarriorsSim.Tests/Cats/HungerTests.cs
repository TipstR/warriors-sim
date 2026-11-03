using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

/// <summary>How much a cat needs to eat, how it gets hungry and weak, and when starving kills it.</summary>
public class HungerTests {
	public HungerTests() {
		TuningSettings.ResetToDefaults();
	}

	private static Cat Warrior(int ageMoons = 30, Sex sex = Sex.Tom) {
		return new Cat("Longtail", Rank.Warrior, sex, ageMoons, 50);
	}

	private static Cat Starving(int stacks, int ageMoons = 30) {
		var cat = Warrior(ageMoons);
		for (var i = 0; i < stacks; i++)
			cat.AddStarvationStack();
		return cat;
	}

	// Food need

	[Fact]
	public void FoodNeed_AtEveryAgeInTheTableItIsTheTableValue() {
		foreach (var (age, need) in Tuning.FoodNeedByAge)
			Assert.Equal(need, Warrior(age).FoodNeed, 4);
	}

	[Fact]
	public void FoodNeed_BetweenTwoAgesInTheTableItGoesInAStraightLine() {
		var points = Tuning.FoodNeedByAge;
		for (var i = 1; i < points.Length; i++) {
			var (fromAge, fromNeed) = points[i - 1];
			var (toAge, toNeed) = points[i];
			for (var age = fromAge + 1; age < toAge; age++) {
				var expected = fromNeed + (toNeed - fromNeed) * (age - fromAge) / (toAge - fromAge);
				Assert.Equal(expected, Warrior(age).FoodNeed, 4);
			}
		}
	}

	[Fact]
	public void FoodNeed_OlderThanTheTableKeepsTheLastValue() {
		Assert.Equal(Tuning.FoodNeedByAge[^1].FoodNeed, Warrior(150).FoodNeed, 4);
	}

	[Fact]
	public void FoodNeed_APregnantQueenNeedsMore() {
		var queen = Warrior(sex: Sex.SheCat);
		var before = queen.FoodNeed;

		queen.BecomePregnant(Warrior());

		Assert.Equal(before + Tuning.PregnancyExtraFoodNeed, queen.FoodNeed, 4);
	}

	[Fact]
	public void FoodNeed_ANursingQueenNeedsMoreForEveryKit() {
		var queen = Warrior(sex: Sex.SheCat);
		var before = queen.FoodNeed;

		queen.IsNursing = true;
		queen.MilkLoad = 2f;

		Assert.Equal(before + 2f * Tuning.MilkFoodNeedPerKit, queen.FoodNeed, 4);
	}

	// Satiation

	[Fact]
	public void Eat_NeverMoreThanFull() {
		var cat = Warrior();
		cat.LoseSatiation(10f);

		cat.Eat(Tuning.PortionSatiation * 3);

		Assert.Equal(Tuning.SatiationMax, cat.Satiation, 4);
	}

	[Fact]
	public void Eat_AddsTheFood() {
		var cat = Warrior();
		cat.LoseSatiation(60f);

		cat.Eat(25f);

		Assert.Equal(Tuning.SatiationMax - 35f, cat.Satiation, 4);
	}

	[Fact]
	public void LoseSatiation_NeverBelowEmpty() {
		var cat = Warrior();

		cat.LoseSatiation(Tuning.SatiationMax * 2);

		Assert.Equal(0f, cat.Satiation);
		Assert.True(cat.IsStarving);
	}

	[Fact]
	public void IsHungry_BelowTheEatingLimit() {
		var atLimit = Warrior();
		var below = Warrior();
		atLimit.LoseSatiation(Tuning.SatiationMax - Tuning.EatBelowSatiation);
		below.LoseSatiation(Tuning.SatiationMax - Tuning.EatBelowSatiation + 1f);

		Assert.False(Warrior().IsHungry);
		Assert.False(atLimit.IsHungry);
		Assert.True(below.IsHungry);
	}

	[Fact]
	public void IsStarving_OnlyWhenEmpty() {
		var almostEmpty = Warrior();
		almostEmpty.LoseSatiation(Tuning.SatiationMax - 1f);

		Assert.False(almostEmpty.IsStarving);
		Assert.False(Warrior().IsStarving);
	}

	// Starvation stacks

	[Fact]
	public void Eat_AMealOfOneTicksFoodRemovesAStarvationStack() {
		var cat = Starving(2);

		cat.Eat(Tuning.SatiationLossPerTick);

		Assert.Equal(1, cat.StarvationStacks);
	}

	[Fact]
	public void Eat_LessThanOneTicksFoodKeepsTheStacks() {
		var cat = Starving(2);

		cat.Eat(Tuning.SatiationLossPerTick * 0.9f);

		Assert.Equal(2, cat.StarvationStacks);
	}

	[Fact]
	public void Eat_NeverBelowZeroStacks() {
		var cat = Warrior();
		cat.LoseSatiation(50f);

		cat.Eat(Tuning.PortionSatiation);

		Assert.Equal(0, cat.StarvationStacks);
	}

	[Fact]
	public void IsWeakFromHunger_FromFamineMinStacksOn() {
		Assert.False(Starving(Tuning.FamineMinStacks - 1).IsWeakFromHunger);
		Assert.True(Starving(Tuning.FamineMinStacks).IsWeakFromHunger);
	}

	// Energy

	[Fact]
	public void MaxEnergy_EveryStarvationStackLowersIt() {
		Assert.Equal(Tuning.EnergyMax, Warrior().MaxEnergy, 4);
		Assert.Equal(Tuning.EnergyMax - 3 * Tuning.MaxEnergyLossPerStarvationStack, Starving(3).MaxEnergy, 4);
	}

	[Fact]
	public void MaxEnergy_NeverBelowZero() {
		Tuning.MaxEnergyLossPerStarvationStack = 50f;

		Assert.Equal(0f, Starving(3).MaxEnergy);
	}

	[Fact]
	public void AddStarvationStack_ARestedCatLosesTheEnergyAboveTheNewMaximum() {
		var cat = Starving(1);

		Assert.Equal(cat.MaxEnergy, cat.Energy, 4);
	}

	[Fact]
	public void AddStarvationStack_ATiredCatKeepsItsEnergy() {
		var cat = Warrior();
		cat.LoseEnergy(50f);

		cat.AddStarvationStack();

		Assert.Equal(Tuning.EnergyMax - 50f, cat.Energy, 4);
	}

	[Fact]
	public void Sleep_AddsEnergyUpToTheMaximum() {
		var tired = Starving(2);
		var veryTired = Starving(2);
		tired.LoseEnergy(50f);
		veryTired.LoseEnergy(80f);
		var before = tired.Energy;

		tired.Sleep(20f);
		veryTired.Sleep(Tuning.EnergyMax);

		Assert.Equal(before + 20f, tired.Energy, 4);
		Assert.Equal(veryTired.MaxEnergy, veryTired.Energy, 4);
	}

	[Fact]
	public void LoseEnergy_NeverBelowZero() {
		var cat = Warrior();

		cat.LoseEnergy(Tuning.EnergyMax * 2);

		Assert.Equal(0f, cat.Energy);
		Assert.True(cat.IsExhausted);
	}

	[Fact]
	public void IsTired_AtTheSleepingLimitAndBelow() {
		var atLimit = Warrior();
		var above = Warrior();
		atLimit.LoseEnergy(Tuning.EnergyMax - Tuning.SleepBelowEnergy);
		above.LoseEnergy(Tuning.EnergyMax - Tuning.SleepBelowEnergy - 1f);

		Assert.True(atLimit.IsTired);
		Assert.False(above.IsTired);
	}

	[Fact]
	public void IsExhausted_NotWhileThereIsEnergyLeft() {
		var tired = Warrior();
		tired.LoseEnergy(Tuning.EnergyMax - 10f);

		Assert.False(tired.IsExhausted);
		Assert.False(Warrior().IsExhausted);
	}

	// Dying of starvation

	[Fact]
	public void StarvationDeathChance_NoneUpToTheSafeStacks() {
		Assert.Equal(0f, Warrior().StarvationDeathChance);
		Assert.Equal(0f, Starving(Tuning.StarvationSafeStacks).StarvationDeathChance);
	}

	[Fact]
	public void StarvationDeathChance_GrowsWithEveryStackAboveTheSafeOnes() {
		var previous = 0f;
		for (var stacks = Tuning.StarvationSafeStacks + 1; stacks < Tuning.StarvationStacksToDie; stacks++) {
			var chance = Starving(stacks).StarvationDeathChance;

			Assert.True(chance > previous);
			Assert.True(chance < 1f);
			previous = chance;
		}
	}

	[Fact]
	public void StarvationDeathChance_CertainAtTheLimit() {
		Assert.Equal(1f, Starving(Tuning.StarvationStacksToDie).StarvationDeathChance);
		Assert.Equal(1f, Starving(Tuning.StarvationStacksToDie + 3).StarvationDeathChance);
	}

	[Fact]
	public void StarvationDeathChance_AFrailCatDiesSooner() {
		var stacks = Tuning.StarvationSafeStacks + 2;
		var prime = Starving(stacks).StarvationDeathChance;
		var old = Starving(stacks, Tuning.FullyFrailAgeMoons).StarvationDeathChance;

		Assert.Equal(prime * Tuning.FrailStarvationMultiplier, old, 4);
	}

	[Fact]
	public void StarvationDeathChance_NeverAboveOne() {
		Tuning.StarvationDeathChancePerStack = 0.6f;

		Assert.Equal(1f, Starving(Tuning.StarvationSafeStacks + 3).StarvationDeathChance);
	}
}
