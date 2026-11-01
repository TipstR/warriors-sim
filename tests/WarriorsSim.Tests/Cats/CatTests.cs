using System.Collections.Generic;
using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

public class CatTests {
	/// <summary>xUnit creates a new instance for every test, so every test starts with the default balancing values.</summary>
	public CatTests() {
		TuningSettings.ResetToDefaults();
	}

	// Frailty Test
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
		var cat = new Cat("Graypaw", Rank.Apprentice, Sex.Tom, ageMoons, 50);

		Assert.Equal(expected, cat.Frailty, 3);
	}

	[Fact]
	public void Frailty_DoesNotDependOnRank() {
		var elder = new Cat("Halftail", Rank.Elder, Sex.Tom, 138, 50);
		var leader = new Cat("Bluestar", Rank.Leader, Sex.SheCat, 138, 80);

		Assert.Equal(elder.Frailty, leader.Frailty);
	}

	// Mentoring Test
	[Theory]
	[InlineData(0, 0.5f)] // WorstMentorFactor
	[InlineData(50, 1f)]
	[InlineData(100, 1.5f)] // BestMentorFactor
	public void MentorFactor_DependsOnMentoringSkill(float mentoring, float expected) {
		var cat = new Cat("Lionheart", Rank.Warrior, Sex.Tom, 70, 70, new Dictionary<SkillType, Skill> {
			[SkillType.Mentoring] = new(mentoring, 50)
		});

		Assert.Equal(expected, cat.MentorFactor, 3);
	}

	// Skills Test
	[Fact]
	public void Skills_MissingSkillsGetDefaults() {
		var cat = new Cat("Sandstorm", Rank.Warrior, Sex.SheCat, 12, 50, new Dictionary<SkillType, Skill> {
			[SkillType.Hunting] = new(45, 65)
		});

		Assert.Equal(45, cat.Skills[SkillType.Hunting].Level, 3);
		Assert.Equal(65, cat.Skills[SkillType.Hunting].Gift);
		Assert.Equal(0, cat.Skills[SkillType.Healing].Level, 3);
		Assert.Equal(Tuning.AverageHealingGift, cat.Skills[SkillType.Healing].Gift);
		Assert.Equal(Tuning.StartingMentoring, cat.Skills[SkillType.Mentoring].Level, 3);
		Assert.Equal(Tuning.AverageGift, cat.Skills[SkillType.Mentoring].Gift);
	}

	[Fact]
	public void Skills_EveryCatGetsItsOwnCopy() {
		var skills = new Dictionary<SkillType, Skill> { [SkillType.Hunting] = new(30, 50) };
		var graypaw = new Cat("Graypaw", Rank.Apprentice, Sex.Tom, 7, 40, skills);
		var ravenpaw = new Cat("Ravenpaw", Rank.Apprentice, Sex.Tom, 8, 60, skills);

		graypaw.Skills[SkillType.Hunting].Improve(10);

		Assert.Equal(30, ravenpaw.Skills[SkillType.Hunting].Level, 3);
		Assert.Equal(30, skills[SkillType.Hunting].Level, 3);
	}

	[Fact]
	public void Eat_AMouseHelpsAStarvingCatRecoverAShrewDoesNot() {
		var withMouse = new Cat("Ravenpaw", Rank.Apprentice, Sex.Tom, 8, 50);
		var withShrew = new Cat("Graypaw", Rank.Apprentice, Sex.Tom, 8, 50);
		withMouse.AddStarvationStack();
		withShrew.AddStarvationStack();

		withMouse.Eat(Tuning.PortionSatiation);
		withShrew.Eat(Tuning.PortionSatiation * Tuning.PreyKinds[PreyKind.Shrew].Portions);

		Assert.Equal(0, withMouse.StarvationStacks);
		Assert.Equal(1, withShrew.StarvationStacks);
	}
}
