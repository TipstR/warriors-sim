using System.Collections.Generic;
using System.Linq;
using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

/// <summary>What the game can see of the simulation: what every cat does, patrols, meals, births and deaths.</summary>
public class ActivityTests {
	public ActivityTests() {
		TuningSettings.ResetToDefaults();
	}

	private static World NewWorld(int seed = 1) {
		return new World(StartingClans.CreateThunderClan(), seed, (_, _) => { });
	}

	[Fact]
	public void Night_EveryCatSleeps() {
		var world = NewWorld();
		// Dawn, sunhigh, evening, night
		for (var i = 0; i < 4; i++)
			world.Tick();

		Assert.All(world.Clan.Cats, c => Assert.Equal(Activity.Sleeping, c.Activity));
	}

	[Fact]
	public void Patrol_EveryCatOnItIsOut() {
		var world = NewWorld();
		var patrols = new List<Patrol>();
		world.PatrolSent += patrols.Add;

		world.Tick();

		Assert.NotEmpty(patrols);
		foreach (var patrol in patrols)
			Assert.All(patrol.Cats, c =>
				Assert.Equal(patrol.IsTraining ? Activity.Training : Activity.Hunting, c.Activity));
	}

	[Fact]
	public void Patrol_CatsNotOnAPatrolAreNotOut() {
		var world = NewWorld();
		var onPatrol = new HashSet<Cat>();
		world.PatrolSent += p => onPatrol.UnionWith(p.Cats);

		world.Tick();

		Assert.All(world.Clan.Cats.Where(c => !onPatrol.Contains(c)),
			c => Assert.DoesNotContain(c.Activity, new[] { Activity.Hunting, Activity.Training }));
	}

	[Fact]
	public void Meals_PileHasWhatPatrolsCaughtMinusWhatWasEaten() {
		var world = NewWorld();
		var pileBefore = world.Clan.FreshKillPile.Mice;
		var caught = 0;
		var eaten = 0;
		world.PatrolSent += p => caught += p.MiceCaught;
		world.CatAte += _ => eaten++;

		// The cats start full, so it takes a while until they eat
		for (var i = 0; i < 8; i++)
			world.Tick();

		Assert.True(eaten > 0);
		Assert.Equal(pileBefore + caught - eaten, world.Clan.FreshKillPile.Mice);
	}

	[Fact]
	public void Clan_TellsAboutNewAndDeadCats() {
		var clan = new Clan("TestClan");
		var added = new List<Cat>();
		var died = new List<Cat>();
		clan.CatAdded += added.Add;
		clan.CatDied += died.Add;
		var cat = new Cat("Graystripe", Rank.Warrior, Sex.Tom, 20, 50, 50);

		clan.AddCat(cat);
		clan.RecordDeath(cat);
		clan.RecordDeath(cat); // already dead: nothing happens

		Assert.Equal(new[] { cat }, added);
		Assert.Equal(new[] { cat }, died);
	}
}
