namespace WarriorsSim.Simulation;

/// <summary>The Clans as they are at the start of the game.</summary>
public static class StartingClans
{
	public static Clan CreateThunderClan()
	{
		var clan = new Clan("ThunderClan");

		//                    name          rank            age  hunting  diligence
		clan.AddCat(new Cat("Bluestar",   Rank.Leader,     110,   70,      80));
		clan.AddCat(new Cat("Redtail",    Rank.Deputy,      60,   65,      75));
		clan.AddCat(new Cat("Tigerclaw",  Rank.Warrior,     50,   85,      90));
		clan.AddCat(new Cat("Lionheart",  Rank.Warrior,     70,   70,      70));
		clan.AddCat(new Cat("Whitestorm", Rank.Warrior,     65,   75,      70));
		clan.AddCat(new Cat("Darkstripe", Rank.Warrior,     40,   55,      30));
		clan.AddCat(new Cat("Ravenpaw",   Rank.Apprentice,   8,   35,      60));
		clan.AddCat(new Cat("Graypaw",    Rank.Apprentice,   7,   30,      40));
		clan.AddCat(new Cat("Dustpaw",    Rank.Apprentice,   8,   40,      65));
		clan.AddCat(new Cat("Halftail",   Rank.Elder,      140,   20,      50));
		clan.AddCat(new Cat("Smallear",   Rank.Elder,      130,   15,      40));
		clan.AddCat(new Cat("Cinderkit",  Rank.Kit,          3,    0,      70));

		return clan;
	}
}
