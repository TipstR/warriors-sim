# Warriors Sim – Notes

## Idea

The goal is to create a game in which you can feel like you are in the
young-adult bestseller *Warrior Cats*.

The player controls a cat through its life and experiences the typical stages
of a cat's life: from being made an apprentice, through training as a warrior
or medicine cat, all the way to becoming deputy or even leader.

The world around you is alive as well. It's not just you – your Clanmates also
live in this world, as do other Clans and other outside influences. Hunting and
guard patrols are assigned, foxes and badgers are sighted, Clans interact with
one another, and disasters have to be dealt with.

Every cat has its own personality. Even objectively evil cats should have their
reasons.

A Clan's actions are determined by the Clan's needs and by the personality and
competence of its leader. Not every leader acts the same way, and not every cat
is happy with every decision.

## Implementation Fundamentals

- The game is to be programmed in **Godot** using **C#**.
- The game itself is to be playable as a **top-down (2D) RPG**.
- The world is simulated at all times (using **sim LODs**).

## Roadmap

### First Simulation

The game has no visuals and is little more than a (possibly interactive) log.
The first needs and the logic for the leader's decisions are implemented at a
basic level.

- A clan consists of a leader, a deputy, 

### First Territory

The first explorable world. No longer just a simulation, but something tangible
to interact with and take part in. The first RPG elements are introduced, with
hunting and fighting implemented at a basic level.

## Systems

### Health

All Cats have a health value that can be affected by various factors. 

Injury affects the cat's ability to hunt and fight. Every limb has a health value.
A cat with a broken leg is slower and less effective in hunting or combat.
Also Consciousness should be a factor. A cat with too much pain is strapped to their bed.

Many of these Systems will be inspired by Rimworld.

### Needs

All Cats have basic needs to survive.

- Hunger
- Thirst
- Sleep
- Social
- Mattering?
- Boredom

### Leader AI

The leader of a clan needs to decide what happens. Basic decisions can be quite complex.

*Hunting patrol:* Who goes? Where does the patrol go? How many cats go? What time does the patrol go?
*Territory patrol:* Who goes? Where does the patrol go? How many cats go? What time does the patrol go?

### Sim LODs

The whole forest is simulated at all times, but not everything needs the same
amount of detail. Things the player can see are played out in full. Everything
else only needs a believable result.

#### Activities

The world is modeled as **activities**, not as cats doing things every frame.
An activity is a hunting patrol, a border patrol, a fight, a Gathering, etc.
Each activity has:

- participants (which cats)
- a place (which part of the territory)
- a time (start and duration)
- a result (prey caught, injuries, scent marks, encounters)

The LOD level only changes **how the result is worked out**. Everything else
(fresh-kill pile, needs, leader decisions) only sees the result and doesn't
care how it was calculated.

The day has fixed slots for activities: **dawn patrol, sunhigh patrol and
evening patrol**. The leader (or deputy) assigns cats to these slots. When the
player becomes deputy or leader, they use the same system as the Leader AI.

#### Levels

Two levels to start with:

- **Full:** cats are rendered and actually walk, stalk, hunt and fight. Used
  for the territory the player is currently in.
- **Abstract:** no positions, no rendering. When an activity ends, its result
  is rolled based on probabilities (skill, personality, season, weather,
  territory). Used for all other territories.

Personality matters at both levels. A lazy or distracted cat should catch less
prey in the abstract roll too, otherwise personalities stop mattering as soon
as the player looks away.

A middle level (cats have rough positions, but are not rendered) is only added
if the full level turns out to be too slow.

#### Fast-forward

The player can speed up time or skip days, weeks or even moons. During a
skip, **everything runs at the abstract level**, including the player's own
Clan. Simulating every cat in full detail for a month would be far too slow.

Skips always end at a calm moment (e.g. at dawn, with the cats in camp). That
way the game can easily place every cat when switching back to the full level.

#### Switching levels

The level changes when:

- the player crosses a border into another territory
- a time skip ends

When switching from abstract to full, the game creates the cats based on the
activities that are currently running. Example: ShadowClan has a hunting patrol
near the border, so when the player enters, the patrol is placed there and
continues from that point.

When switching from full to abstract, running activities keep what has already
happened (prey already caught stays caught) and only the rest is rolled.

#### Scale

Classic first-season forest: four Clans with about 25–35 cats each, plus
loners, rogues and kittypets. Around 120–150 cats in total.

#### Open questions

- Can the player see across borders? (e.g. watching RiverClan cats on the
  other side of the river) Then the full level might need to include a strip
  along the borders.
- What does the player's own cat do during a time skip? Automatically take
  part in patrols? Can it get injured or die?
- How long is one in-game day in real time at normal speed?