using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

/// <summary>What a cat's rank means: its name, its den, and who hunts and mentors.</summary>
public class RankTests {
	public RankTests() {
		TuningSettings.ResetToDefaults();
	}

	private static Cat Cat(Rank rank, int ageMoons = 30, Sex sex = Sex.Tom) {
		var name = rank switch {
			Rank.Kit => "Graykit",
			Rank.Apprentice or Rank.MedicineCatApprentice => "Graypaw",
			Rank.Leader => "Graystar",
			_ => "Graystripe"
		};
		return new Cat(name, rank, sex, ageMoons, 50);
	}

	private static Cat NursingQueen() {
		var queen = Cat(Rank.Warrior, sex: Sex.SheCat);
		queen.IsNursing = true;
		return queen;
	}

	// Names

	[Fact]
	public void ChangeRank_KitsApprenticesAndLeadersGetTheirSuffix() {
		var cat = Cat(Rank.Kit);

		cat.ChangeRank(Rank.Apprentice);
		Assert.Equal("Graypaw", cat.Name);

		cat.ChangeRank(Rank.Warrior, "stripe");
		Assert.Equal("Graystripe", cat.Name);

		cat.ChangeRank(Rank.Leader);
		Assert.Equal("Graystar", cat.Name);
	}

	[Fact]
	public void ChangeRank_AMedicineCatApprenticeIsAPawAndGetsAWarriorName() {
		var cat = Cat(Rank.Kit);

		cat.ChangeRank(Rank.MedicineCatApprentice);
		Assert.Equal("Graypaw", cat.Name);

		cat.ChangeRank(Rank.MedicineCat, "leaf");
		Assert.Equal("Grayleaf", cat.Name);
	}

	[Fact]
	public void ChangeRank_DeputiesAndEldersKeepTheirName() {
		var cat = Cat(Rank.Warrior);

		cat.ChangeRank(Rank.Deputy);
		Assert.Equal("Graystripe", cat.Name);

		cat.ChangeRank(Rank.Elder);
		Assert.Equal("Graystripe", cat.Name);
		Assert.Equal(Rank.Elder, cat.Rank);
	}

	[Fact]
	public void ChangeRank_AWarriorWithoutANewSuffixKeepsItsName() {
		var cat = Cat(Rank.Deputy);

		cat.ChangeRank(Rank.Warrior);

		Assert.Equal("Graystripe", cat.Name);
	}

	// Hunting

	[Theory]
	[InlineData(Rank.Apprentice, true)]
	[InlineData(Rank.Warrior, true)]
	[InlineData(Rank.Deputy, true)]
	[InlineData(Rank.Leader, true)]
	[InlineData(Rank.Kit, false)]
	[InlineData(Rank.Elder, false)]
	[InlineData(Rank.MedicineCat, false)]
	[InlineData(Rank.MedicineCatApprentice, false)]
	public void CanHunt_ByRank(Rank rank, bool canHunt) {
		Assert.Equal(canHunt, Cat(rank).CanHunt);
	}

	[Fact]
	public void CanHunt_NotAQueenInTheNursery() {
		Assert.False(NursingQueen().CanHunt);
	}

	[Fact]
	public void CanHunt_NotWhenConfinedButWithASmallWound() {
		var confined = Cat(Rank.Warrior);
		var scratched = Cat(Rank.Warrior);
		confined.AddCondition(new Condition(ConditionType.Wound, Tuning.ConfinedSeverity, 1f));
		scratched.AddCondition(new Condition(ConditionType.Wound, Tuning.ConfinedSeverity / 2, 1f));

		Assert.False(confined.CanHunt);
		Assert.True(scratched.CanHunt);
	}

	[Fact]
	public void CanHunt_NotWhileRestingInTheMedicineDen() {
		var patient = Cat(Rank.Warrior);

		patient.InMedicineDen = true;

		Assert.False(patient.CanHunt);
	}

	[Fact]
	public void CanHuntInFamine_EldersAndQueensHelp() {
		Assert.True(Cat(Rank.Elder, 130).CanHuntInFamine);
		Assert.True(NursingQueen().CanHuntInFamine);
		Assert.True(Cat(Rank.Warrior).CanHuntInFamine);
	}

	[Fact]
	public void CanHuntInFamine_NotKitsOrMedicineCats() {
		Assert.False(Cat(Rank.Kit, 3).CanHuntInFamine);
		Assert.False(Cat(Rank.MedicineCat).CanHuntInFamine);
	}

	[Fact]
	public void CanHuntInFamine_NotWhenRestingOrConfined() {
		var patient = Cat(Rank.Elder, 130);
		var confined = NursingQueen();
		patient.InMedicineDen = true;
		confined.AddCondition(new Condition(ConditionType.Wound, Tuning.ConfinedSeverity, 1f));

		Assert.False(patient.CanHuntInFamine);
		Assert.False(confined.CanHuntInFamine);
	}

	// Mentoring

	[Theory]
	[InlineData(Rank.Warrior, true)]
	[InlineData(Rank.Deputy, true)]
	[InlineData(Rank.Leader, true)]
	[InlineData(Rank.Apprentice, false)]
	[InlineData(Rank.Elder, false)]
	[InlineData(Rank.Kit, false)]
	public void CanMentor_ByRank(Rank rank, bool canMentor) {
		Assert.Equal(canMentor, Cat(rank).CanMentor);
	}

	[Fact]
	public void CanMentor_NotAQueenInTheNursery() {
		Assert.False(NursingQueen().CanMentor);
	}

	[Fact]
	public void HasFunNeed_NotAQueenInTheNursery() {
		Assert.False(NursingQueen().HasFunNeed);
	}

	[Theory]
	[InlineData(Rank.MedicineCat, true)]
	[InlineData(Rank.MedicineCatApprentice, true)]
	[InlineData(Rank.Warrior, false)]
	[InlineData(Rank.Apprentice, false)]
	public void IsMedicineCat_ByRank(Rank rank, bool isMedicineCat) {
		Assert.Equal(isMedicineCat, Cat(rank).IsMedicineCat);
	}

	// Dens

	[Theory]
	[InlineData(Rank.Kit, Den.Nursery)]
	[InlineData(Rank.Apprentice, Den.Apprentices)]
	[InlineData(Rank.Warrior, Den.Warriors)]
	[InlineData(Rank.Deputy, Den.Warriors)]
	[InlineData(Rank.Leader, Den.Leader)]
	[InlineData(Rank.Elder, Den.Elders)]
	[InlineData(Rank.MedicineCat, Den.Medicine)]
	[InlineData(Rank.MedicineCatApprentice, Den.Medicine)]
	public void Den_ByRank(Rank rank, Den den) {
		Assert.Equal(den, Cat(rank).Den);
	}

	[Fact]
	public void Den_QueensSleepInTheNursery() {
		Assert.Equal(Den.Nursery, NursingQueen().Den);
	}

	[Fact]
	public void Den_PatientsSleepInTheMedicineDen() {
		var kit = Cat(Rank.Kit, 3);
		var warrior = Cat(Rank.Warrior);
		kit.InMedicineDen = true;
		warrior.InMedicineDen = true;

		Assert.Equal(Den.Medicine, kit.Den);
		Assert.Equal(Den.Medicine, warrior.Den);
	}
}
