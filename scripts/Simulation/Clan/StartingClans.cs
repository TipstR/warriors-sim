using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>The Clans as they are at the start of the game.</summary>
public static class StartingClans {
	public static Clan CreateThunderClan() {
		var clan = new Clan("ThunderClan");

		// Skills are new(level, gift). Gifts: 50 = average.
		// Skills that aren't listed get the default (see Cat.Skills): healing gifts are rare, so everyone without
		// one has the low average (Tuning.AverageHealingGift). Mentoring starts at Tuning.StartingMentoring,
		// except for cats who have taught before.
		var frostfur = new Cat("Frostfur", Rank.Warrior, Sex.SheCat, ageMoons: 50, diligence: 60,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(40, 45),
				[SkillType.Mentoring] = new(35, 55)
			});
		
		var brindleface = new Cat("Brindleface", Rank.Warrior, Sex.SheCat, ageMoons: 40, diligence: 60,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(50, 50),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 60)
			});

		clan.AddCat(new Cat("Bluestar", Rank.Leader, Sex.SheCat, ageMoons: 110, diligence: 80,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(70, 70),
				[SkillType.Mentoring] = new(75, 75)
			}));
		
		clan.AddCat(new Cat("Redtail", Rank.Deputy, Sex.Tom, ageMoons: 60, diligence: 75,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(65, 65),
				[SkillType.Mentoring] = new(50, 65)
			}));
		
		// Spottedleaf is the medicine cat
		clan.AddCat(new Cat("Spottedleaf", Rank.MedicineCat, Sex.SheCat, ageMoons: 20, diligence: 80,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(45, 45),
				[SkillType.Healing] = new(60, 75),
				[SkillType.Mentoring] = new(35, 60)
			}));
		
		clan.AddCat(new Cat("Tigerclaw", Rank.Warrior, Sex.Tom, ageMoons: 50, diligence: 90,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(85, 90),
				[SkillType.Mentoring] = new(25, 15)
			}));
		
		clan.AddCat(new Cat("Lionheart", Rank.Warrior, Sex.Tom, ageMoons: 70, diligence: 70,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(70, 70),
				[SkillType.Mentoring] = new(80, 85)
			}));
		
		clan.AddCat(new Cat("Whitestorm", Rank.Warrior, Sex.Tom, ageMoons: 65, diligence: 70,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(75, 75),
				[SkillType.Mentoring] = new(75, 80)
			}));
		
		clan.AddCat(new Cat("Runningwind", Rank.Warrior, Sex.Tom, ageMoons: 70, diligence: 70,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(70, 70),
				[SkillType.Mentoring] = new(35, 50)
			}));
		
		clan.AddCat(new Cat("Willowpelt", Rank.Warrior, Sex.SheCat, ageMoons: 30, diligence: 50,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(66, 65),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 55)
			}));
		
		clan.AddCat(new Cat("Mousefur", Rank.Warrior, Sex.SheCat, ageMoons: 115, diligence: 75,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(55, 55),
				[SkillType.Mentoring] = new(35, 45)
			}));
		
		clan.AddCat(frostfur);
		clan.AddCat(brindleface);
		
		clan.AddCat(new Cat("Darkstripe", Rank.Warrior, Sex.Tom, ageMoons: 40, diligence: 30,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(55, 55),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 25)
			}));
		
		clan.AddCat(new Cat("Longtail", Rank.Warrior, Sex.Tom, ageMoons: 43, diligence: 40,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(60, 60),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 30)
			}));
		
		clan.AddCat(new Cat("Ravenpaw", Rank.Apprentice, Sex.Tom, ageMoons: 8, diligence: 60,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(35, 50),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 40)
			}));
		
		clan.AddCat(new Cat("Graypaw", Rank.Apprentice, Sex.Tom, ageMoons: 7, diligence: 40,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(30, 55),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 60)
			}));
		
		clan.AddCat(new Cat("Dustpaw", Rank.Apprentice, Sex.Tom, ageMoons: 9, diligence: 65,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(40, 55),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 50)
			}));
		
		clan.AddCat(new Cat("Sandpaw", Rank.Apprentice, Sex.SheCat, ageMoons: 9, diligence: 80,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(45, 65),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 55)
			}));
		
		clan.AddCat(new Cat("Halftail", Rank.Elder, Sex.Tom, ageMoons: 140, diligence: 50,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(20, 40),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 60)
			}));
		
		clan.AddCat(new Cat("Smallear", Rank.Elder, Sex.Tom, ageMoons: 130, diligence: 40,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(15, 35),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 50)
			}));
		
		clan.AddCat(new Cat("Patchpelt", Rank.Elder, Sex.Tom, ageMoons: 135, diligence: 35,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(20, 40),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 55)
			}));
		
		clan.AddCat(new Cat("One-Eye", Rank.Elder, Sex.SheCat, ageMoons: 138, diligence: 40,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(15, 35),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 40)
			}));

		// Frostfur's litter.
		// Cinderkit has a gift for healing, like in the books. Whether she becomes a medicine cat is up to the simulation.
		clan.AddCat(new Cat("Cinderkit", Rank.Kit, Sex.SheCat, ageMoons: 3, diligence: 70,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(0, 40),
				[SkillType.Healing] = new(0, 80),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 70)
			}, mother: frostfur));
		
		clan.AddCat(new Cat("Brackenkit", Rank.Kit, Sex.Tom, ageMoons: 3, diligence: 60,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(0, 55),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 65)
			}, mother: frostfur));
		
		clan.AddCat(new Cat("Brightkit", Rank.Kit, Sex.SheCat, ageMoons: 3, diligence: 55,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(0, 50),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 50)
			}, mother: frostfur));
		
		clan.AddCat(new Cat("Thornkit", Rank.Kit, Sex.Tom, ageMoons: 3, diligence: 65,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(0, 55),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 50)
			}, mother: frostfur));

		// Brindleface's litter
		clan.AddCat(new Cat("Ashkit", Rank.Kit, Sex.Tom, ageMoons: 2, diligence: 50,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(0, 50),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 45)
			}, mother: brindleface));
		
		clan.AddCat(new Cat("Fernkit", Rank.Kit, Sex.SheCat, ageMoons: 2, diligence: 55,
			new Dictionary<SkillType, Skill> {
				[SkillType.Hunting] = new(0, 45),
				[SkillType.Mentoring] = new(Tuning.StartingMentoring, 60)
			}, mother: brindleface));

		// The apprentices' mentors from the book
		Cat Find(string name) => clan.Cats.First(c => c.Name == name);
		Find("Ravenpaw").SetMentor(Find("Tigerclaw"));
		Find("Graypaw").SetMentor(Find("Lionheart"));
		Find("Dustpaw").SetMentor(Find("Redtail"));
		Find("Sandpaw").SetMentor(Find("Whitestorm"));

		// Spottedleaf's herb store
		foreach (var herb in Enum.GetValues<Herb>())
			clan.HerbStore.Add(herb, Tuning.StartingHerbDoses);

		return clan;
	}
}
