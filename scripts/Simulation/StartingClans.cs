namespace WarriorsSim.Simulation;

/// <summary>The Clans as they are at the start of the game.</summary>
public static class StartingClans {
    public static Clan CreateThunderClan() {
        var clan = new Clan("ThunderClan");

        var frostfur = new Cat("Frostfur", Rank.Warrior, Sex.SheCat, 50, 40, 60);
        var brindleface = new Cat("Brindleface", Rank.Warrior, Sex.SheCat, 40, 50, 60);

        //                  name           rank             sex          age  hunting  diligence
        clan.AddCat(new Cat("Bluestar",    Rank.Leader,     Sex.SheCat,  110, 70,      80));
        clan.AddCat(new Cat("Redtail",     Rank.Deputy,     Sex.Tom,     60,  65,      75));
        // Spottedleaf will be medicine cat
        clan.AddCat(new Cat("Spottedleaf", Rank.Warrior,    Sex.SheCat,  20,  45,      80));
        clan.AddCat(new Cat("Tigerclaw",   Rank.Warrior,    Sex.Tom,     50,  85,      90));
        clan.AddCat(new Cat("Lionheart",   Rank.Warrior,    Sex.Tom,     70,  70,      70));
        clan.AddCat(new Cat("Whitestorm",  Rank.Warrior,    Sex.Tom,     65,  75,      70));
        clan.AddCat(new Cat("Runningwind", Rank.Warrior,    Sex.Tom,     70,  70,      70));
        clan.AddCat(new Cat("Willowpelt",  Rank.Warrior,    Sex.SheCat,  30,  66,      50));
        clan.AddCat(new Cat("Mousefur",    Rank.Warrior,    Sex.SheCat,  115, 55,      75));
        clan.AddCat(frostfur);
        clan.AddCat(brindleface);
        clan.AddCat(new Cat("Darkstripe",  Rank.Warrior,    Sex.Tom,     40,  55,      30));
        clan.AddCat(new Cat("Longtail",    Rank.Warrior,    Sex.Tom,     43,  60,      40));
        clan.AddCat(new Cat("Ravenpaw",    Rank.Apprentice, Sex.Tom,     8,   35,      60));
        clan.AddCat(new Cat("Graypaw",     Rank.Apprentice, Sex.Tom,     7,   30,      40));
        clan.AddCat(new Cat("Dustpaw",     Rank.Apprentice, Sex.Tom,     9,   40,      65));
        clan.AddCat(new Cat("Sandpaw",     Rank.Apprentice, Sex.SheCat,  9,   45,      80));
        clan.AddCat(new Cat("Halftail",    Rank.Elder,      Sex.Tom,     140, 20,      50));
        clan.AddCat(new Cat("Smallear",    Rank.Elder,      Sex.Tom,     130, 15,      40));
        clan.AddCat(new Cat("Patchpelt",   Rank.Elder,      Sex.Tom,     135, 20,      35));
        clan.AddCat(new Cat("One-Eye",     Rank.Elder,      Sex.SheCat,  138, 15,      40));
        // Frostfur's litter
        clan.AddCat(new Cat("Cinderkit",   Rank.Kit,        Sex.SheCat,  3,   0,       70, frostfur));
        clan.AddCat(new Cat("Brackenkit",  Rank.Kit,        Sex.Tom,     3,   0,       60, frostfur));
        clan.AddCat(new Cat("Brightkit",   Rank.Kit,        Sex.SheCat,  3,   0,       55, frostfur));
        clan.AddCat(new Cat("Thornkit",    Rank.Kit,        Sex.Tom,     3,   0,       65, frostfur));
        // Brindleface's litter
        clan.AddCat(new Cat("Ashkit",      Rank.Kit,        Sex.Tom,     2,   0,       50, brindleface));
        clan.AddCat(new Cat("Fernkit",     Rank.Kit,        Sex.SheCat,  2,   0,       55, brindleface));

        return clan;
    }
}
