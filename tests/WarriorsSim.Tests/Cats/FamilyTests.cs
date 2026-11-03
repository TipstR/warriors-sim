using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

/// <summary>Kin, mates, pregnancy and queens.</summary>
public class FamilyTests {
	public FamilyTests() {
		TuningSettings.ResetToDefaults();
	}

	private static Cat Tom(string name = "Lionheart", Cat mother = null, Cat father = null) {
		return new Cat(name, Rank.Warrior, Sex.Tom, 30, 50, null, mother, father);
	}

	private static Cat SheCat(string name = "Brindleface", Cat mother = null, Cat father = null) {
		return new Cat(name, Rank.Warrior, Sex.SheCat, 30, 50, null, mother, father);
	}

	// Kin

	[Fact]
	public void IsCloseKinOf_MotherAndChildBothWays() {
		var mother = SheCat();
		var child = Tom("Graystripe", mother);

		Assert.True(child.IsCloseKinOf(mother));
		Assert.True(mother.IsCloseKinOf(child));
	}

	[Fact]
	public void IsCloseKinOf_FatherAndChildBothWays() {
		var father = Tom();
		var child = SheCat("Sandstorm", father: father);

		Assert.True(child.IsCloseKinOf(father));
		Assert.True(father.IsCloseKinOf(child));
	}

	[Fact]
	public void IsCloseKinOf_HalfSiblingsOnTheMothersSide() {
		var mother = SheCat("Frostfur");
		var a = Tom("Cinderpaw", mother, Tom("Smallear"));
		var b = SheCat("Brackenfur", mother, Tom("Patchpelt"));

		Assert.True(a.IsCloseKinOf(b));
	}

	[Fact]
	public void IsCloseKinOf_HalfSiblingsOnTheFathersSide() {
		var father = Tom("Tigerclaw");
		var a = Tom("Bramblepaw", SheCat("Goldenflower"), father);
		var b = Tom("Hawkfrost", SheCat("Sasha"), father);

		Assert.True(a.IsCloseKinOf(b));
	}

	[Fact]
	public void IsCloseKinOf_CatsWithUnknownParentsAreNotKin() {
		Assert.False(Tom().IsCloseKinOf(SheCat()));
	}

	[Fact]
	public void IsCloseKinOf_UnrelatedCatsAreNotKin() {
		var a = Tom("Graystripe", SheCat("Willowpelt"), Tom("Patchpelt"));
		var b = SheCat("Silverstream", SheCat("Mistyfoot"), Tom("Crookedstar"));

		Assert.False(a.IsCloseKinOf(b));
	}

	[Fact]
	public void IsCloseKinOf_CousinsAreNotCloseKin() {
		var grandmother = SheCat("Moonflower");
		var a = Tom("Fireheart", SheCat("Bluefur", grandmother));
		var b = SheCat("Sandstorm", SheCat("Snowfur", grandmother));

		Assert.False(a.IsCloseKinOf(b));
	}

	// Mates

	[Fact]
	public void BecomeMates_BothKnowTheirMate() {
		var tom = Tom();
		var sheCat = SheCat();

		tom.BecomeMates(sheCat);

		Assert.Same(sheCat, tom.Mate);
		Assert.Same(tom, sheCat.Mate);
	}

	[Fact]
	public void LoseMate_TheCatIsFree() {
		var tom = Tom();
		tom.BecomeMates(SheCat());

		tom.LoseMate();

		Assert.Null(tom.Mate);
	}

	// Pregnancy

	[Fact]
	public void BecomePregnant_SheIsAQueenExpectingKitsOfTheFather() {
		var queen = SheCat();
		var father = Tom();

		queen.BecomePregnant(father);

		Assert.True(queen.IsPregnant);
		Assert.True(queen.IsQueen);
		Assert.Same(father, queen.FatherOfUnbornKits);
		Assert.Equal(Tuning.PregnancyDays, queen.PregnancyDaysLeft);
	}

	[Fact]
	public void AdvancePregnancy_TheKitsAreBornExactlyOnceAtTheEnd() {
		var queen = SheCat();
		queen.BecomePregnant(Tom());

		for (var day = 1; day < Tuning.PregnancyDays; day++)
			Assert.False(queen.AdvancePregnancy());
		Assert.True(queen.AdvancePregnancy());

		Assert.False(queen.IsPregnant);
		Assert.Null(queen.PregnancyDaysLeft);
		Assert.False(queen.AdvancePregnancy());
	}

	[Fact]
	public void AdvancePregnancy_NothingHappensWithoutPregnancy() {
		var sheCat = SheCat();

		Assert.False(sheCat.AdvancePregnancy());
		Assert.False(sheCat.IsPregnant);
	}

	[Fact]
	public void IsInNursery_OnlyTheLastDaysBeforeTheBirth() {
		var queen = SheCat();
		queen.BecomePregnant(Tom());
		var daysOutside = Tuning.PregnancyDays - Tuning.NurseryDaysBeforeBirth;

		for (var day = 1; day < daysOutside; day++)
			queen.AdvancePregnancy();
		Assert.False(queen.IsInNursery);

		queen.AdvancePregnancy();
		Assert.True(queen.IsInNursery);
	}

	[Fact]
	public void IsInNursery_ANursingQueenStaysInTheNursery() {
		var queen = SheCat();

		queen.IsNursing = true;

		Assert.True(queen.IsQueen);
		Assert.True(queen.IsInNursery);
	}

	[Fact]
	public void IsQueen_NotWithoutKits() {
		var sheCat = SheCat();

		Assert.False(sheCat.IsQueen);
		Assert.False(sheCat.IsInNursery);
	}

	// Growing up

	[Fact]
	public void CanEatPrey_YoungKitsOnlyDrinkMilk() {
		var newborn = new Cat("Cloudkit", Rank.Kit, Sex.Tom, 0, 50);
		var weaning = new Cat("Cloudkit", Rank.Kit, Sex.Tom, Tuning.FirstPreyAgeMoons, 50);
		var apprentice = new Cat("Cloudpaw", Rank.Apprentice, Sex.Tom, 6, 50);

		Assert.False(newborn.CanEatPrey);
		Assert.True(weaning.CanEatPrey);
		Assert.True(apprentice.CanEatPrey);
	}

	[Fact]
	public void AgeOneMoon_TheCatIsAMoonOlder() {
		var cat = Tom();

		cat.AgeOneMoon();

		Assert.Equal(31, cat.AgeMoons);
	}
}
