using System.Collections.Generic;
using System.Linq;
using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

/// <summary>Who belongs to the Clan, who died, who leads.</summary>
public class ClanTests {
	public ClanTests() {
		TuningSettings.ResetToDefaults();
	}

	private static Cat Warrior(string name = "Longtail") {
		return new Cat(name, Rank.Warrior, Sex.Tom, 30, 50);
	}

	[Fact]
	public void New_TheClanHasItsName() {
		Assert.Equal("ThunderClan", new Clan("ThunderClan").Name);
	}

	[Fact]
	public void RecordDeath_MovesTheCatToTheDeadRightAway() {
		var clan = new Clan("ThunderClan");
		var died = new List<Cat>();
		clan.CatDied += died.Add;
		var cat = Warrior();
		clan.AddCat(cat);

		clan.RecordDeath(cat);

		Assert.Empty(clan.Cats);
		Assert.Equal(new[] { cat }, clan.Dead);
		Assert.Equal(new[] { cat }, died);
	}

	[Fact]
	public void RecordDeath_ADeadCatCanOnlyDieOnce() {
		var clan = new Clan("ThunderClan");
		var died = new List<Cat>();
		clan.CatDied += died.Add;
		var cat = Warrior();
		clan.AddCat(cat);

		clan.RecordDeath(cat);
		clan.RecordDeath(cat);

		Assert.Equal(new[] { cat }, clan.Dead);
		Assert.Equal(new[] { cat }, died);
	}

	[Fact]
	public void RecordDeath_ACatThatIsNotInTheClanDoesNotDie() {
		var clan = new Clan("ThunderClan");

		clan.RecordDeath(Warrior());

		Assert.Empty(clan.Dead);
	}

	[Fact]
	public void RecordDeath_TheMateIsFreeAgain() {
		var clan = new Clan("ThunderClan");
		var tom = Warrior();
		var sheCat = new Cat("Brindleface", Rank.Warrior, Sex.SheCat, 30, 50);
		clan.AddCat(tom);
		clan.AddCat(sheCat);
		tom.BecomeMates(sheCat);

		clan.RecordDeath(tom);

		Assert.Null(sheCat.Mate);
	}

	[Fact]
	public void LeaderAndDeputy_FoundByRank() {
		var clan = new Clan("ThunderClan");
		var leader = new Cat("Bluestar", Rank.Leader, Sex.SheCat, 100, 80);
		var deputy = new Cat("Redtail", Rank.Deputy, Sex.Tom, 60, 70);
		clan.AddCat(Warrior());
		clan.AddCat(deputy);
		clan.AddCat(leader);

		Assert.Same(leader, clan.Leader);
		Assert.Same(deputy, clan.Deputy);
	}

	[Fact]
	public void LeaderAndDeputy_NullWhenThereIsNone() {
		var clan = new Clan("ThunderClan");
		clan.AddCat(Warrior());

		Assert.Null(clan.Leader);
		Assert.Null(clan.Deputy);
	}

	[Fact]
	public void ApprenticeOf_FindsOnlyThatMentorsApprentice() {
		var clan = new Clan("ThunderClan");
		var lionheart = Warrior("Lionheart");
		var tigerclaw = Warrior("Tigerclaw");
		var graypaw = new Cat("Graypaw", Rank.Apprentice, Sex.Tom, 7, 50);
		var ravenpaw = new Cat("Ravenpaw", Rank.Apprentice, Sex.Tom, 7, 50);
		graypaw.SetMentor(lionheart);
		ravenpaw.SetMentor(tigerclaw);
		foreach (var cat in new[] { lionheart, tigerclaw, graypaw, ravenpaw })
			clan.AddCat(cat);

		Assert.Same(graypaw, clan.ApprenticeOf(lionheart));
		Assert.Same(ravenpaw, clan.ApprenticeOf(tigerclaw));
		Assert.Null(clan.ApprenticeOf(graypaw));
	}

	[Fact]
	public void ApprenticeOf_AWarriorTrainedByTheMentorDoesNotCount() {
		var clan = new Clan("ThunderClan");
		var lionheart = Warrior("Lionheart");
		var formerApprentice = Warrior("Graystripe");
		formerApprentice.SetMentor(lionheart);
		clan.AddCat(lionheart);
		clan.AddCat(formerApprentice);

		Assert.Null(clan.ApprenticeOf(lionheart));
	}

	[Fact]
	public void StarvingCatCount_CountsFromFamineMinStacksOn() {
		var clan = new Clan("ThunderClan");
		var justBelow = Warrior("Longtail");
		var atTheLimit = Warrior("Darkstripe");
		for (var i = 0; i < Tuning.FamineMinStacks - 1; i++)
			justBelow.AddStarvationStack();
		for (var i = 0; i < Tuning.FamineMinStacks; i++)
			atTheLimit.AddStarvationStack();
		clan.AddCat(justBelow);
		clan.AddCat(atTheLimit);
		clan.AddCat(Warrior("Mousefur"));

		Assert.Equal(1, clan.StarvingCatCount);
	}

	[Fact]
	public void CatsInEatingOrder_WithTheSameScoreTheHungrierCatEatsFirst() {
		var clan = new Clan("ThunderClan");
		var full = Warrior("Longtail");
		var hungry = Warrior("Darkstripe");
		hungry.LoseSatiation(30f);
		clan.AddCat(full);
		clan.AddCat(hungry);

		Assert.Equal(new[] { hungry, full }, clan.CatsInEatingOrder().ToArray());
	}
}
