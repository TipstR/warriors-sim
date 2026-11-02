using System.Collections.Generic;
using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

/// <summary>
///     How likely a cat catches its prey: skill and diligence decide, age, tiredness and sickness take away.
///     Skill must matter, so a good hunter has to be clearly better than a weak one.
/// </summary>
public class HuntingTests {
	public HuntingTests() {
		TuningSettings.ResetToDefaults();
	}

	private static Cat Hunter(float hunting = 50, int diligence = 50, int ageMoons = 30) {
		return new Cat("Longtail", Rank.Warrior, Sex.Tom, ageMoons, diligence, new Dictionary<SkillType, Skill> {
			[SkillType.Hunting] = new(hunting, 50)
		});
	}

	[Fact]
	public void CatchChance_ABetterHunterCatchesMore() {
		Assert.True(Hunter(80).CatchChance > Hunter(40).CatchChance);
		Assert.True(Hunter(40).CatchChance > Hunter(10).CatchChance);
	}

	[Fact]
	public void CatchChance_ADiligentCatCatchesMoreThanALazyOne() {
		Assert.True(Hunter(diligence: 90).CatchChance > Hunter(diligence: 50).CatchChance);
		Assert.True(Hunter(diligence: 50).CatchChance > Hunter(diligence: 10).CatchChance);
	}

	[Fact]
	public void CatchChance_AverageDiligenceIsNeitherBonusNorPenalty() {
		var withDiligence = Hunter(diligence: Tuning.AverageDiligence).CatchChance;
		Tuning.CatchChancePerDiligencePoint = 0f;

		Assert.Equal(withDiligence, Hunter(diligence: Tuning.AverageDiligence).CatchChance, 5);
	}

	[Fact]
	public void CatchChance_HuntingSkillAddsToTheBaseChance() {
		var withSkill = Hunter(60).CatchChance;
		Tuning.CatchChancePerHuntingPoint = 0f;

		Assert.Equal(Tuning.BaseCatchChance, Hunter(60).CatchChance, 5);
		Assert.True(withSkill > Tuning.BaseCatchChance);
	}

	[Fact]
	public void CatchChance_AFullyFrailOldCatHuntsAtTheFrailMultiplier() {
		var prime = Hunter(60, ageMoons: 40);
		var old = Hunter(60, ageMoons: Tuning.FullyFrailAgeMoons);

		Assert.Equal(prime.CatchChance * Tuning.FrailHuntingMultiplier, old.CatchChance, 4);
	}

	[Fact]
	public void CatchChance_AnAgingCatHuntsWorseButNotAsBadAsAFullyFrailOne() {
		var prime = Hunter(60, ageMoons: 40).CatchChance;
		var aging = Hunter(60, ageMoons: (Tuning.OldAgeStartMoons + Tuning.FullyFrailAgeMoons) / 2).CatchChance;
		var frail = Hunter(60, ageMoons: Tuning.FullyFrailAgeMoons).CatchChance;

		Assert.InRange(aging, frail + 0.01f, prime - 0.01f);
	}

	[Fact]
	public void CatchChance_ASickCatHuntsWorseByItsSeverity() {
		var healthy = Hunter(60);
		var sick = Hunter(60);
		sick.AddCondition(new Condition(ConditionType.Wound, 40f, 1f));

		Assert.Equal(healthy.CatchChance * 0.6f, sick.CatchChance, 4);
	}

	[Fact]
	public void CatchChance_ATiredCatCatchesLessAnExhaustedOneNothing() {
		var rested = Hunter(60);
		var tired = Hunter(60);
		var exhausted = Hunter(60);
		tired.LoseEnergy(Tuning.EnergyMax - (Tuning.EnergyPenaltyStart + Tuning.EnergyPenaltyFull) / 2);
		exhausted.LoseEnergy(Tuning.EnergyMax - Tuning.EnergyPenaltyFull);

		Assert.True(tired.CatchChance < rested.CatchChance);
		Assert.True(tired.CatchChance > 0f);
		Assert.Equal(0f, exhausted.CatchChance);
	}

	[Fact]
	public void CatchChance_NeverAboveOne() {
		Tuning.CatchChancePerHuntingPoint = 0.05f;

		Assert.Equal(1f, Hunter(100, 100).CatchChance);
	}

	[Fact]
	public void CatchChance_NeverBelowZero() {
		Tuning.BaseCatchChance = 0f;

		Assert.Equal(0f, Hunter(0, 0).CatchChance);
	}

	[Fact]
	public void EnergyFactor_NoEffectFromThePenaltyStartUp() {
		var full = Hunter();
		var atStart = Hunter();
		atStart.LoseEnergy(Tuning.EnergyMax - Tuning.EnergyPenaltyStart);

		Assert.Equal(1f, full.EnergyFactor, 5);
		Assert.Equal(1f, atStart.EnergyFactor, 5);
	}

	[Fact]
	public void EnergyFactor_NothingWorksAtThePenaltyFullAndBelow() {
		var atFull = Hunter();
		var empty = Hunter();
		atFull.LoseEnergy(Tuning.EnergyMax - Tuning.EnergyPenaltyFull);
		empty.LoseEnergy(Tuning.EnergyMax);

		Assert.Equal(0f, atFull.EnergyFactor, 5);
		Assert.Equal(0f, empty.EnergyFactor, 5);
	}

	[Fact]
	public void EnergyFactor_HalfWayGivesHalf() {
		var cat = Hunter();
		cat.LoseEnergy(Tuning.EnergyMax - (Tuning.EnergyPenaltyStart + Tuning.EnergyPenaltyFull) / 2);

		Assert.Equal(0.5f, cat.EnergyFactor, 4);
	}

	[Fact]
	public void EnergyFactor_ALittleTirednessBarelyMattersALotHurtsMuch() {
		var span = Tuning.EnergyPenaltyStart - Tuning.EnergyPenaltyFull;
		var littleTired = Hunter();
		var veryTired = Hunter();
		littleTired.LoseEnergy(Tuning.EnergyMax - (Tuning.EnergyPenaltyFull + span * 0.9f));
		veryTired.LoseEnergy(Tuning.EnergyMax - (Tuning.EnergyPenaltyFull + span * 0.1f));

		// An S-curve: above a straight line near the top, below it near the bottom
		Assert.True(littleTired.EnergyFactor > 0.9f);
		Assert.True(veryTired.EnergyFactor < 0.1f);
	}

	[Fact]
	public void EnergyFactor_LessEnergyNeverHelps() {
		var previous = 2f;
		for (var energy = Tuning.EnergyMax; energy >= 0f; energy -= 5f) {
			var cat = Hunter();
			cat.LoseEnergy(Tuning.EnergyMax - energy);

			Assert.True(cat.EnergyFactor <= previous);
			previous = cat.EnergyFactor;
		}
	}
}
