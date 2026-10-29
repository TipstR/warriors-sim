using System;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>The Clans as they are at the start of the game.</summary>
public static class StartingClans {
    public static Clan CreateThunderClan() {
        var clan = new Clan("ThunderClan");

        var frostfur = new Cat("Frostfur", Rank.Warrior, Sex.SheCat, 50, 40, 60, 45, 55);
        var brindleface = new Cat("Brindleface", Rank.Warrior, Sex.SheCat, 40, 50, 60, 50, 60);

        // Gifts: 50 = average. How well they mentor right now is set further down.
        // Healing gifts are rare, so everyone without one set below has the low average (Tuning.AverageHealingGift).
        //                  name           rank             sex          age  hunting  diligence  gift: hunting  mentoring
        clan.AddCat(new Cat("Bluestar",    Rank.Leader,     Sex.SheCat,  110, 70,      80,              70,      75));
        clan.AddCat(new Cat("Redtail",     Rank.Deputy,     Sex.Tom,     60,  65,      75,              65,      65));
        clan.AddCat(new Cat("Spottedleaf", Rank.MedicineCat, Sex.SheCat, 20,  45,      80,              45,      60));
        // A great hunter, but a bad mentor (ask Ravenpaw)
        clan.AddCat(new Cat("Tigerclaw",   Rank.Warrior,    Sex.Tom,     50,  85,      90,              90,      15));
        clan.AddCat(new Cat("Lionheart",   Rank.Warrior,    Sex.Tom,     70,  70,      70,              70,      85));
        clan.AddCat(new Cat("Whitestorm",  Rank.Warrior,    Sex.Tom,     65,  75,      70,              75,      80));
        clan.AddCat(new Cat("Runningwind", Rank.Warrior,    Sex.Tom,     70,  70,      70,              70,      50));
        clan.AddCat(new Cat("Willowpelt",  Rank.Warrior,    Sex.SheCat,  30,  66,      50,              65,      55));
        clan.AddCat(new Cat("Mousefur",    Rank.Warrior,    Sex.SheCat,  115, 55,      75,              55,      45));
        clan.AddCat(frostfur);
        clan.AddCat(brindleface);
        clan.AddCat(new Cat("Darkstripe",  Rank.Warrior,    Sex.Tom,     40,  55,      30,              55,      25));
        clan.AddCat(new Cat("Longtail",    Rank.Warrior,    Sex.Tom,     43,  60,      40,              60,      30));
        clan.AddCat(new Cat("Ravenpaw",    Rank.Apprentice, Sex.Tom,     8,   35,      60,              50,      40));
        clan.AddCat(new Cat("Graypaw",     Rank.Apprentice, Sex.Tom,     7,   30,      40,              55,      60));
        clan.AddCat(new Cat("Dustpaw",     Rank.Apprentice, Sex.Tom,     9,   40,      65,              55,      50));
        clan.AddCat(new Cat("Sandpaw",     Rank.Apprentice, Sex.SheCat,  9,   45,      80,              65,      55));
        clan.AddCat(new Cat("Halftail",    Rank.Elder,      Sex.Tom,     140, 20,      50,              40,      60));
        clan.AddCat(new Cat("Smallear",    Rank.Elder,      Sex.Tom,     130, 15,      40,              35,      50));
        clan.AddCat(new Cat("Patchpelt",   Rank.Elder,      Sex.Tom,     135, 20,      35,              40,      55));
        clan.AddCat(new Cat("One-Eye",     Rank.Elder,      Sex.SheCat,  138, 15,      40,              35,      40));
        // Frostfur's litter
        clan.AddCat(new Cat("Cinderkit",   Rank.Kit,        Sex.SheCat,  3,   0,       70,              40,      70, frostfur));
        clan.AddCat(new Cat("Brackenkit",  Rank.Kit,        Sex.Tom,     3,   0,       60,              55,      65, frostfur));
        clan.AddCat(new Cat("Brightkit",   Rank.Kit,        Sex.SheCat,  3,   0,       55,              50,      50, frostfur));
        clan.AddCat(new Cat("Thornkit",    Rank.Kit,        Sex.Tom,     3,   0,       65,              55,      50, frostfur));
        // Brindleface's litter
        clan.AddCat(new Cat("Ashkit",      Rank.Kit,        Sex.Tom,     2,   0,       50,              50,      45, brindleface));
        clan.AddCat(new Cat("Fernkit",     Rank.Kit,        Sex.SheCat,  2,   0,       55,              45,      60, brindleface));

        // The apprentices' mentors from the book
        Cat Find(string name) => clan.Cats.First(c => c.Name == name);
        Find("Ravenpaw").SetMentor(Find("Tigerclaw"));
        Find("Graypaw").SetMentor(Find("Lionheart"));
        Find("Dustpaw").SetMentor(Find("Redtail"));
        Find("Sandpaw").SetMentor(Find("Whitestorm"));

        // Mentoring: cats who have taught before. Everyone else starts at Tuning.StartingMentoring.
        void SetMentoring(string name, float level) {
            var cat = Find(name);
            cat.SetSkill(SkillType.Mentoring, level, cat.Skills[SkillType.Mentoring].Gift);
        }

        SetMentoring("Bluestar", 75);
        SetMentoring("Lionheart", 80);
        SetMentoring("Whitestorm", 75);
        SetMentoring("Redtail", 50);
        SetMentoring("Runningwind", 35);
        SetMentoring("Mousefur", 35);
        SetMentoring("Frostfur", 35);
        SetMentoring("Spottedleaf", 35);
        SetMentoring("Tigerclaw", 25);

        // Healing: Spottedleaf is the medicine cat. Cinderkit has a gift for healing, like in the books.
        // Whether she becomes a medicine cat is up to the simulation.
        Find("Spottedleaf").SetSkill(SkillType.Healing, 60, 75);
        Find("Cinderkit").SetSkill(SkillType.Healing, 0, 80);

        // Spottedleaf's herb store
        foreach (var herb in Enum.GetValues<Herb>())
            clan.HerbStore.Add(herb, Tuning.StartingHerbDoses);

        return clan;
    }
}
