# Warriors Sim – Vision

A summary of everything decided or discussed about the game so far.

Every point is marked with how settled it is:

- **[Decided]** – you said so explicitly
- **[Proposed]** – suggested during planning and put into the notes, but not
  explicitly confirmed by you yet
- **[Open]** – not decided yet

---

## 1. The Core Idea

A game in which you feel like you are living inside the *Warriors* books
(*Warrior Cats*). **[Decided]**

Three pillars:

1. **Your own cat's life.** You play one cat and live through the typical
   stages of a Clan cat's life. **[Decided]**
2. **A living world.** The forest doesn't revolve around the player. Clanmates,
   other Clans, predators and disasters all keep going whether you are there or
   not. **[Decided]**
3. **Cats with real personalities.** Every cat has its own character, needs and
   motivations. Clan politics come out of these personalities instead of being
   scripted. **[Decided]**

## 2. The Player

- The player controls **exactly one cat** for the whole game. **[Decided]**
- Life path: made an apprentice → trained as a **warrior** or **medicine cat**
  → possibly **deputy** → possibly **leader**. **[Decided]**
- As deputy or leader, the player can **manage** other cats (e.g. assign them
  to patrols), but can **never directly control** them. Other cats always act
  on their own. **[Decided]**
- When the player manages the Clan, they use **the same system the Leader AI
  uses**, so it only has to be built once. **[Proposed]**
- Life before apprenticeship (being a kit) has not been mentioned yet.
  **[Open]**

## 3. The World

- Setting: the **classic forest from the first series**, with the four Clans
  ThunderClan, RiverClan, WindClan and ShadowClan. **[Decided]**
- The player can travel into other Clans' territories. **[Decided]**
- The world is **much smaller than real life**, so distances stay short.
  **[Decided]**
- Outside influences: foxes, badgers, other Clans, disasters (e.g. fire,
  flood). **[Decided]**
- Number of cats: **similar to the books**. **[Decided]** As a working
  estimate: 4 Clans of about 25–35 cats each, plus loners, rogues and
  kittypets, around **120–150 cats** in total. **[Proposed]**

## 4. Clans and Politics

- A Clan's actions come from **the Clan's needs** combined with **the
  personality and competence of its leader**. **[Decided]**
- Different leaders make different decisions in the same situation.
  **[Decided]**
- Not every cat agrees with every decision. Disagreement and unhappiness are
  part of the simulation. **[Decided]**
- Even objectively evil cats have **their own reasons**. Nobody is evil just
  because the story needs a villain. **[Decided]**

## 5. Time

- Time does **not** run in real time. **[Decided]**
- One day has room for **three patrols**: dawn, sunhigh and evening.
  **[Decided]**
- The player can **speed up time**, even skipping **weeks or months**.
  **[Decided]**
- How long one in-game day takes at normal speed is a balancing question.
  **[Open]**

## 6. Systems

### 6.1 Health

Heavily inspired by **RimWorld**. **[Decided]**

- Every cat has a health value affected by various factors. **[Decided]**
- **Every limb has its own health value.** A cat with a broken leg is slower
  and worse at hunting and fighting. **[Decided]**
- **Consciousness** is a factor. A cat in too much pain is confined to its
  nest. **[Decided]**
- Illness and its connection to medicine cats are not worked out yet.
  **[Open]**

### 6.2 Needs

Every cat has basic needs. Current list **[Decided]**:

- Hunger
- Thirst
- Sleep
- Social
- Boredom
- "Mattering" (feeling important or useful), with a question mark in your
  notes **[Open]**

Still to decide **[Open]**:

- What happens when a need isn't met (penalties, illness, bad mood, death)?
- How needs are met. Does every cat eat when it's hungry, or are there Clan
  rules (e.g. elders and queens eat first, prey goes to the fresh-kill pile)?
- Whether thirst stays. (It was suggested to drop it, since in the books cats
  rarely go out of their way to drink. It is still on your list.)

### 6.3 Personality

- Every cat has its own personality. **[Decided]**
- Personality affects behavior **at every simulation level**, including
  results calculated off-screen. A lazy cat catches less prey even when nobody
  is watching. **[Proposed]**
- The actual model (which traits exist, how they affect decisions) has not been
  designed yet. **[Open]** This is important early on, because the Leader AI
  and Clan politics both depend on it.

### 6.4 Leader AI

The leader decides what the Clan does. **[Decided]** For each patrol, the
leader has to answer:

- **Hunting patrol:** Who goes? Where? How many cats? When?
- **Border patrol:** Who goes? Where? How many cats? When?

Example rule: *If the fresh-kill pile is empty, a hunting patrol must be
organized.* **[Decided]**

### 6.5 Activities

**[Proposed]** The world is modeled as **activities** instead of cats doing
things every frame. Activities include hunting patrols, border patrols, fights
and Gatherings. Each one has:

- participants
- a place
- a start time and a duration
- a result (prey caught, injuries, scent marks, encounters)

The rest of the game (fresh-kill pile, needs, leader decisions) only looks at
the results. The three daily patrol slots are the main points where activities
get assigned.

### 6.6 Simulation Levels (Sim LODs)

The whole world is simulated at all times. **[Decided]** Things the player
can see are played out in full. Everything else only needs a believable result.

**[Proposed]** Two levels to start with:

| Level    | Rendered | Cats have positions | How results come about                 |
|----------|----------|---------------------|----------------------------------------|
| Full     | yes      | yes                 | cats actually hunt and fight           |
| Abstract | no       | no                  | one dice roll when the activity ends   |

- **Full** is used for the territory the player is in, **Abstract** for all
  other territories. **[Proposed]**
- A middle level (positions, but no rendering) is only added if the game turns
  out to be too slow. **[Proposed]**
- **During a time skip, everything runs at the abstract level**, including the
  player's own Clan. **[Proposed]**
- Skips end at a calm moment (e.g. dawn, cats in camp), so it's easy to place
  every cat again. **[Proposed]**
- Levels switch when the player crosses a border and when a time skip ends.
  When switching to Full, cats are placed according to their running
  activities. When switching to Abstract, what has already happened is kept
  and only the rest is rolled. **[Proposed]**

Still to decide **[Open]**:

- Can the player see across borders (e.g. RiverClan cats on the other side of
  the river)? Then the full level might have to include a strip along the
  borders.
- What does the player's own cat do during a time skip? Does it take part in
  patrols automatically? Can it get hurt or die?

## 7. Technology

- Engine: **Godot 4.7** with **C#**. **[Decided]**
- Playable as a **top-down 2D RPG**. **[Decided]**

## 8. Roadmap

### Milestone 1 – First Simulation

- No graphics. The game is little more than a log, possibly interactive.
  **[Decided]**
- First needs and basic leader decisions are implemented. **[Decided]**
- This is essentially the **abstract simulation level**. **[Proposed]**
- Clan composition (leader, deputy, ...) is being written down in your notes.
  **[Open]**
- Suggested definition of done **[Proposed]**: one Clan of about 10 cats runs
  for 100 simulated days. The leader assigns hunting and border patrols, cats
  eat from the fresh-kill pile, and everything is written to a log.

### Milestone 2 – First Territory

- The first explorable world, something tangible to take part in.
  **[Decided]**
- First RPG elements: basic hunting and fighting. **[Decided]**
- This adds the **full simulation level** for one territory. **[Proposed]**

### Later **[Proposed]**

- Switching between levels at borders and after time skips
- Other Clans, Gatherings, predators, disasters
- Rank progression (apprentice → warrior / medicine cat → deputy → leader)

## 9. Suggested Next Step

Write down the exact rules for **one hunting patrol at the abstract level**:
which values go into the roll (skill, personality, season, weather, territory,
number of cats) and what comes out (amount of prey, injuries, events). It's
small enough to code as the first real piece of the game.
