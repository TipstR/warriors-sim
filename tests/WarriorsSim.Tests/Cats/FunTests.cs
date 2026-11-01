using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

/// <summary>Bored, rested warriors go hunting alone. Diligent cats get bored faster.</summary>
public class FunTests {
	public FunTests() {
		TuningSettings.ResetToDefaults();
	}

	private static Cat Warrior(int diligence = 50) {
		return new Cat("Longtail", Rank.Warrior, Sex.Tom, 30, diligence);
	}

	[Fact]
	public void BoredomFactor_DiligentCatsGetBoredFaster() {
		Assert.True(Warrior(90).BoredomFactor > Warrior(10).BoredomFactor);
	}

	[Fact]
	public void HuntAloneChance_ZeroWhenFunIsFull() {
		Assert.Equal(0f, Warrior().HuntAloneChance);
	}

	[Fact]
	public void HuntAloneChance_ZeroWithoutEnergyForAHunt() {
		var cat = Warrior();
		cat.ChangeFun(-Tuning.FunMax);
		cat.LoseEnergy(Tuning.EnergyMax - Tuning.PatrolEnergyCost);

		Assert.Equal(0f, cat.HuntAloneChance);
	}

	[Fact]
	public void HuntAloneChance_RestedCatsGoMoreOftenThanTiredOnes() {
		var rested = Warrior();
		var tired = Warrior();
		rested.ChangeFun(-Tuning.FunMax);
		tired.ChangeFun(-Tuning.FunMax);
		tired.LoseEnergy(40f);

		Assert.True(rested.HuntAloneChance > tired.HuntAloneChance);
	}

	[Fact]
	public void HuntAloneChance_BoredRestedCatGoesWithTheMaxChanceByDiligence() {
		var lazy = Warrior(0);
		var diligent = Warrior(100);
		lazy.ChangeFun(-Tuning.FunMax);
		diligent.ChangeFun(-Tuning.FunMax);

		Assert.Equal(Tuning.MinHuntAloneChance, lazy.HuntAloneChance, 3);
		Assert.Equal(Tuning.MaxHuntAloneChance, diligent.HuntAloneChance, 3);
	}

	[Fact]
	public void HasFunNeed_OnlyWarriorsDeputyAndLeader() {
		Assert.True(Warrior().HasFunNeed);
		Assert.False(new Cat("Halftail", Rank.Elder, Sex.Tom, 130, 50).HasFunNeed);
		Assert.False(new Cat("Graypaw", Rank.Apprentice, Sex.Tom, 7, 50).HasFunNeed);
	}
}
