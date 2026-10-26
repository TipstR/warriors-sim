using System;

namespace WarriorsSim.Simulation;

public class Cat
{
	public string Name { get; }
	public Rank Rank { get; set; }
	public int AgeMoons { get; set; }

	/// <summary>Hunting skill, from <see cref="Tuning.SkillMin"/> to <see cref="Tuning.SkillMax"/>.</summary>
	public int Hunting { get; }

	/// <summary>How hard-working the cat is. Lazy cats hunt worse.</summary>
	public int Diligence { get; }

	/// <summary>How full the cat is. 0 = starving, <see cref="Tuning.SatiationMax"/> = full.</summary>
	public float Satiation { get; private set; }

	public Cat(string name, Rank rank, int ageMoons, int hunting, int diligence)
	{
		Name = name;
		Rank = rank;
		AgeMoons = ageMoons;
		Hunting = Math.Clamp(hunting, Tuning.SkillMin, Tuning.SkillMax);
		Diligence = Math.Clamp(diligence, Tuning.SkillMin, Tuning.SkillMax);
		Satiation = Tuning.SatiationMax;
	}

	public bool IsHungry => Satiation < Tuning.EatBelowSatiation;
	public bool IsStarving => Satiation <= 0f;

	public bool CanHunt => Rank is Rank.Apprentice or Rank.Warrior or Rank.Deputy or Rank.Leader;

	/// <summary>Chance (0 to 1) that a single hunting attempt catches a mouse.</summary>
	public float CatchChance
	{
		get
		{
			float chance = Tuning.BaseCatchChance
				+ Hunting * Tuning.CatchChancePerHuntingPoint
				+ (Diligence - Tuning.AverageDiligence) * Tuning.CatchChancePerDiligencePoint;
			return Math.Clamp(chance, 0f, 1f);
		}
	}

	public void LoseSatiation(float amount)
	{
		Satiation = Math.Max(0f, Satiation - amount);
	}

	public void Eat(float satiation)
	{
		Satiation = Math.Min(Tuning.SatiationMax, Satiation + satiation);
	}

	public override string ToString() => $"{Name} ({Rank})";
}
