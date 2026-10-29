using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

public class SkillTests {
	public SkillTests() {
		TuningSettings.ResetToDefaults();
	}

	[Fact]
	public void Improve_RaisesPeak() {
		var skill = new Skill(40, 50);

		skill.Improve(10);

		Assert.True(skill.Level > 40);
		Assert.Equal(skill.Level, skill.Peak);
	}

	[Fact]
	public void FadeIfUnused_UnusedSkillFades() {
		Tuning.SkillFadePerMoon = 2f;
		var skill = new Skill(60, 50);

		skill.FadeIfUnused();

		Assert.Equal(58f, skill.Level, 3);
		Assert.Equal(60f, skill.Peak, 3);
	}

	[Fact]
	public void FadeIfUnused_PracticedSkillDoesNotFade() {
		var skill = new Skill(60, 50);

		skill.Practice();
		skill.FadeIfUnused();

		Assert.Equal(60f, skill.Level, 3);
	}

	[Fact]
	public void FadeIfUnused_LearningCountsAsPractice() {
		var skill = new Skill(60, 50);

		skill.Improve(1);
		var learned = skill.Level;
		skill.FadeIfUnused();

		Assert.Equal(learned, skill.Level, 3);
	}

	[Fact]
	public void FadeIfUnused_PracticeOnlyCountsForOneMoon() {
		Tuning.SkillFadePerMoon = 2f;
		var skill = new Skill(60, 50);

		skill.Practice();
		skill.FadeIfUnused();
		skill.FadeIfUnused();

		Assert.Equal(58f, skill.Level, 3);
	}

	[Fact]
	public void Improve_RustyCatRelearnsFaster() {
		Tuning.SkillFadePerMoon = 5f;
		var fresh = new Skill(55, 50);
		var rusty = new Skill(60, 50);
		rusty.FadeIfUnused();

		fresh.Improve(0.1f);
		rusty.Improve(0.1f);

		Assert.True(rusty.Level - 55 > (fresh.Level - 55) * 5);
	}

	[Fact]
	public void Improve_RelearningStopsAtPeak() {
		Tuning.SkillFadePerMoon = 1f;
		var skill = new Skill(60, 50);
		skill.FadeIfUnused();

		skill.Improve(10);

		Assert.Equal(60f, skill.Level, 3);
		Assert.Equal(60f, skill.Peak, 3);
	}

	[Fact]
	public void FadeIfUnused_NeverBelowFloor() {
		Tuning.SkillFadePerMoon = 2f;
		Tuning.SkillFadeFloorShare = 0.5f;
		var skill = new Skill(60, 50);

		for (var moon = 0; moon < 100; moon++)
			skill.FadeIfUnused();

		Assert.Equal(30f, skill.Level, 3);
	}
}
