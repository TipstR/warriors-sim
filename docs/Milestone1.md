# Milestone 1 – First Simulation

The rules of the simulation as they are in the code right now. They started
as a small plan (10 cats, 30 days) and grew while balancing. Everything here
is still a starting point and will change.

All numbers live in `scripts/Simulation/Tuning.cs` and can be changed in the
Tuning tab of the simulation UI. The numbers below are the defaults. If this
document and `Tuning.cs` disagree, `Tuning.cs` is right.

## Goal

One Clan (ThunderClan) runs on its own. The leader sends hunting patrols,
cats eat from the fresh-kill pile, and everything is written to a text log.
No graphics.

**Done when:** a run finishes and the log shows patrols, prey caught, cats
eating and cats going hungry. **Status: done.** The Clan now sustains itself
over decades with births, ceremonies, retirement and deaths.

## Cats

Each cat has:

| Field              | Type / Range                                         |
|--------------------|------------------------------------------------------|
| Name               | prefix + suffix, e.g. Gray + paw                     |
| Rank               | kit, apprentice, warrior, deputy, leader, elder      |
| Sex                | she-cat or tom                                       |
| Age                | in moons                                             |
| Hunting            | skill, 0–100                                         |
| Diligence          | personality trait, 0–100 (lazy cats hunt worse)      |
| Hunting gift       | born with it, 0–100, 50 = average                    |
| Mentoring gift     | born with it, 0–100, 50 = average                    |
| Mentor             | the cat that trains or trained it, or none           |
| Moons mentored     | how long it has trained apprentices                  |
| Satiation          | 0–100 (100 = full, 0 = starving)                     |
| Energy             | 0–100 (100 = rested, 0 = exhausted)                  |
| Starvation stacks  | how long the cat has gone without food while starving |
| Mother, father     | or unknown                                           |
| Mate               | or none                                              |
| Pregnancy          | days until the kits are born, or not pregnant        |

The suffix changes with rank: kits get "kit", apprentices "paw", new
warriors a random warrior suffix, leaders "star". Deputies and elders keep
their warrior name.

Medicine cats come later.

## Time

- One day has **4 ticks**, each 6 hours long:
  1. **Dawn** (06:00)
  2. **Sunhigh** (12:00)
  3. **Evening** (18:00)
  4. **Night** (00:00)
- Patrols and eating happen at dawn, sunhigh and evening. Night is for
  sleeping.
- A patrol lasts one tick.
- A moon has **30 days**, a season **3 moons**. The year starts with newleaf.

## Rules

### Satiation

- A full cat (100) is empty after **6 ticks** (36 hours), so satiation drops
  by **100 / 6 ≈ 16.7 per tick** for an adult.
- At 0 the cat is **starving** (see Starvation).
- For now there is only one kind of prey: **mice, 25 satiation each**.
- How much a cat needs depends on its age, compared to an adult (= 1). Ages
  in between are interpolated. Based on real kittens:

| Age (moons) | 0–1 | 2   | 3    | 4    | 6   | 9    | 12+ |
|-------------|-----|-----|------|------|-----|------|-----|
| Food need   | 0   | 0.8 | 0.95 | 1.05 | 1.1 | 1.15 | 1   |

- Pregnant queens need **+0.25**, nursing queens **+0.3 per kit** they feed.

### Eating

- Cats eat at dawn, sunhigh and evening, never at night.
- A cat eats when its satiation is **below 75** and there is prey on the
  pile, so it keeps a reserve for a missed meal and the night.
- A cat eats **one mouse per tick** at most.
- Kits younger than **1 moon** can't eat prey, only milk (see Nursery).
- If there isn't enough prey for everyone, the leader's **feeding policy**
  decides who eats first. Every cat gets a score, and the highest score eats
  first. With the same score, the hungriest cat eats first.

| Weight           | Means                                              |
|------------------|----------------------------------------------------|
| Starving first   | bonus for cats that are starving right now         |
| Urgency          | the more starvation stacks, the sooner a cat eats  |
| Compassion       | kits, queens, elders (1), apprentices (0.5)        |
| Pragmatism       | hunters, by hunting skill                          |
| Warrior code     | kits, queens, elders, leader, deputy, warriors, apprentices |
| Famine priority  | added to pragmatism during a famine                |

| Leader style   | Starving first | Urgency | Compassion | Pragmatism | Warrior code | Famine priority |
|----------------|----------------|---------|------------|------------|--------------|-----------------|
| Balanced       | 10             | 1       | 0.5        | 0.5        | 0.5          | 2               |
| Compassionate  | 10             | 1       | 2          | 0          | 0.5          | 0.5             |
| Ruthless       | 0              | 0       | 0          | 2          | 0            | 2               |
| By the code    | 0              | 0       | 0          | 0          | 1            | 0               |

The default is **Balanced**.

### Energy and sleep

- An awake cat loses **20 energy per tick** (empty after 5 ticks).
- Every cat sleeps at night. During the day, a cat with **33 energy or less**
  is tired and sleeps instead of going on patrol.
- A sleeping cat gets **+70 energy** per tick, up to its max energy.
- Tiredness makes a cat hunt worse, along an S-curve: above **80 energy** it
  barely matters, towards **5 energy** the cat can do almost nothing.

### Starvation

- Every tick a starving cat doesn't eat, it gets a **starvation stack**.
  Every meal removes one.
- Every stack lowers the cat's max energy by **4**.
- Up to **8 stacks**, starving can't kill a cat. Every stack above that adds
  **1% chance per tick** to die (e.g. 14 stacks = 6%). At **20 stacks**
  (5 days without food) the cat always dies.
- Kits and elders are frail: their chance to die is **doubled**.
- This way not every starving cat dies at the same moment.

### Famine

- A famine starts when at least **3 cats** have **2 or more stacks**. (One
  stack after a hungry night is normal.)
- It ends after there were fewer than that for **12 ticks in a row** (three
  days), so the usual hunger at night doesn't start and end a famine every
  day.
- During a famine:
  - elders and queens in the nursery help hunting (elders catch only
    **60%** as well as they used to)
  - most leader styles feed the hunters first (famine priority)

### Hunting patrols

- Checked at dawn, sunhigh and evening: if the pile has **fewer mice than
  there are cats**, the leader sends hunting patrols. The leader keeps
  sending patrols until they are expected to catch **20% more** than the
  difference, or until no hunters are left. The leader plans with normal
  chances and doesn't account for the season or the prey left.
- **Who can hunt:** apprentices, warriors, the deputy and the leader. Queens
  in the nursery (nursing, or less than **21 days** before the birth), kits
  and elders don't hunt, except in a famine (see above).
- **Who is picked:** cats that are awake and were not on the last patrols.
  First warriors and apprentices, then the leader and deputy, then the famine
  helpers. Within each group, the best catch chance first, so tired cats are
  picked later.
- **Patrol size:** 4 cats.
- **Result:** each cat gets **6 attempts** per patrol. Each success = 1 mouse
  on the pile. No carrying limit, so good hunters make a real difference.
- **Catch chance** per attempt:
  - 10% + hunting × 0.6% + (diligence − 50) × 0.2%
  - × energy factor (S-curve, see Energy)
  - × season (see Seasons)
  - × prey left in the territory today
- **Prey limit:** the territory has about **100 mice per day** (× season).
  The more was caught today, the harder the next catch: almost normal until
  half is caught (94%), then it drops quickly (75% caught: 68%, 90% caught:
  34%, all caught: 0%). This limits how many cats the territory can feed.

### Seasons

| Season    | Catch chance | Chance of kits |
|-----------|--------------|----------------|
| Newleaf   | × 1.0        | × 1.0          |
| Greenleaf | × 1.3        | × 1.0          |
| Leaf-fall | × 1.0        | × 0.3          |
| Leaf-bare | × 0.8        | × 0.1          |

The season also changes the prey limit, so leaf-bare is hard twice: each
attempt is harder, and there is less prey in total.

### Nursery

- Kits younger than **2 moons** drink milk. Kits from 1 moon on can also eat
  prey.
- The queens in the nursery share their milk: all nursing kits are fed by all
  nursing queens together, orphans included.
- One queen can feed at most **6 kits**. The youngest kits are fed first.
- A starving queen has no milk. Kits without milk go hungry.

### Mates and kits

- **Mates** (placeholder until relationships exist): every moon, a single
  she-cat warrior or deputy has a **15% chance** to become mates with a
  single tom warrior or deputy. He must not be close kin (parent, child,
  sibling or half-sibling) and at most **36 moons** older or younger. When a
  cat dies, its mate is free again.
- **Kits:** every moon, a she-cat warrior or deputy with a mate, not already
  a queen and at most **96 moons** old, expects kits with a **30% chance**,
  multiplied by:
  - the season (see Seasons)
  - hunger: 1 / (1 + share of meals missed this moon × 5). If the Clan
    missed 20% of its meals, the chance is halved.
  - crowding: 1 − (cats / cats the territory feeds in newleaf)². With the
    default values the territory feeds about 37 cats, so births get rarer
    long before that.
- Pregnancy lasts **63 days**.
- Litter size: 1 kit 5%, 2 kits 15%, 3 kits 30%, 4 kits 30%, 5 kits 15%,
  6 kits 5%.

### Aging and ranks

- Every cat gets a moon older at the end of each moon.
- **Apprentice** at **6 moons**, **warrior** at **12 moons**. Apprentices
  learn to hunt from their mentor (see Gifts and mentors).
- Warriors and deputies **retire** to the elders' den at **120 moons**.
  Leaders never retire.
- **Old age:** from **96 moons** on, a cat can die of old age. The chance
  starts at **0.5% per moon** and doubles every **18 moons**.
- **Succession:** when the leader dies, the deputy becomes leader. A new
  deputy is chosen right away: for now the best hunter among the warriors
  (the older one if two are equal).

### Gifts and mentors

Skill comes from two things: what a cat was born with (its **gift**) and
who trained it (its **mentor**). Only hunting is a skill so far, but every
new skill will get its own gift.

- **Gifts:** every kit is born with a hunting gift and a mentoring gift,
  0–100 (50 = average). Half of a gift comes from the parents (the average
  of both), the other half is luck. The luck is the average of three rolls,
  so very gifted and very ungifted kits are rare. Gifted parents often have
  gifted kits, but not always.
- **Training:** every moon, an apprentice's hunting skill grows by
  **8.7** × hunting gift factor × mentor factor. Parts of a point are kept,
  so nothing is lost to rounding. With 8.7, the average new warrior has
  about 50 hunting.
  - Hunting gift factor: from **0.3** (gift 0) over **1** (gift 50) to
    **1.7** (gift 100).
  - Mentor factor: from **0.5** (worst mentor) to **1.5** (best mentor).
    Without a mentor, an apprentice learns at **0.3** on its own.
- **Mentoring quality** = mentoring gift × experience. A mentor who has
  never mentored only teaches with **60%** of its gift. Every moon of
  mentoring adds experience, and after **24 moons** (about four
  apprentices) the mentor teaches with its full gift. A cat with a low gift
  stays a bad mentor, however long it mentors.
- **Choosing a mentor:** when a kit becomes an apprentice, the leader picks
  a mentor among the warriors, the deputy and the leader who don't have an
  apprentice yet and aren't in the nursery. The leader can't see who has a
  gift for mentoring. Good hunters and cats who have mentored before are
  more likely to be chosen, but every free cat has a chance.
- **Losing a mentor:** if a mentor dies, retires or moves to the nursery,
  the apprentice gets a new mentor at the next moon. If no cat is free, it
  waits for its old mentor and learns on its own in the meantime.
- **Result:** a gifted apprentice with a great mentor can become a warrior
  with 90+ hunting, an ungifted one with a bad mentor stays below 20.

## Starting Clan

ThunderClan as at the start of *Into the Wild* (`StartingClans.cs`): 27 cats
with Bluestar as leader and Redtail as deputy, 11 more warriors, 4
apprentices, 4 elders, and the 6 kits of Frostfur and Brindleface. The
apprentices have their mentors from the book (Ravenpaw: Tigerclaw, Graypaw:
Lionheart, Dustpaw: Redtail, Sandpaw: Whitestorm). Gifts fit the book:
Tigerclaw is a great hunter but a bad mentor, Lionheart and Whitestorm are
great mentors.

## Technical decisions

- The simulation is **plain C#** with no Godot code in it. The simulation UI
  (`scenes/Simulation_UI.tscn`) starts it, shows it and writes the full log
  to `logs/simulation.log`.
- Randomness uses a **fixed seed** (default 12345), so the same seed always
  gives the same run.

## Not in Milestone 1

Thirst, social and other needs; health and injuries; sickness; medicine cats;
skills other than hunting; personality beyond diligence; other Clans;
predators; sim LODs; map and graphics; saving and loading.

## Open questions

- In greenleaf, patrols catch more than the leader planned, and prey never
  goes bad, so the pile can grow. Later, prey could go stale after a few days
  (crow-food).
