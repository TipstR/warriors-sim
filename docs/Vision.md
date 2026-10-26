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

Genre: **an emergent storytelling game** with a **RimWorld-like feeling**,
not a kids' game like most Warriors games. Stories aren't scripted. They come
out of systems interacting: a bad leaf-bare, a lazy hunter, an ambitious
warrior, a leader who makes the wrong call. **[Decided]**

What this means for the design **[Proposed]**:

- **Consequences stick.** Cats can die from starvation, injuries, illness or
  fights. Scars, grudges and losses stay.
- **Systems connect.** Hunger affects mood, mood affects loyalty, loyalty
  affects whether a cat follows the leader. The more systems touch each other,
  the more stories they produce.
- **Cats remember.** Relationships and memories ("Darkstripe let Graypaw go
  hungry in leaf-bare") turn simulation results into stories.
- **The log is the story.** Events should be written so they read like a
  chronicle of the Clan, not like debug output.
- **Pacing.** Like RimWorld's storytellers, something may eventually decide
  when big events (fires, floods, badger attacks) happen, so there are quiet
  moons and hard ones.

Design principle: **skill matters.** A new apprentice can't feed the Clan, but
a very good hunter can. A sick or injured cat should make a noticeable
difference. Balancing should keep the gap between weak and strong cats large.
**[Decided]**

## 2. The Player

- The player controls **one cat at a time**. **[Decided]**
- Inspired by **Crusader Kings 3** and the books: main characters die, but the
  story goes on. It's the story of all the Clans, not of one cat.
  **[Decided]**
- Like in **The Sims**, the player can **switch to another cat at any time**,
  not only when their cat dies. It's a singleplayer game, and players make
  their own rules. **[Decided]** Examples:
  - continue as your former apprentice after your cat dies
  - let your old cat retire to the elders' den and take over a young warrior
  - try out being a medicine cat by taking over the new medicine cat
    apprentice
- The goal is a **story across generations**: playing as a cat and looking at
  an older cat with the thought "I trained him when he was my apprentice."
  Past player cats stay part of the world and its history. **[Decided]**
- Whether the player can switch to any cat in **any Clan** (or loners,
  rogues, kittypets) or only within their own Clan is **[Open]**.
- Because any cat can become the player's cat, **every cat has to be
  simulated as a full character**: needs, skills, relationships and memories,
  not just numbers. **[Proposed]**
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
- Details and rules: see `docs/Milestone1.md`.
- **Status: the core works.** ThunderClan (16 cats) runs for two years with
  seasons. The leader sends hunting patrols, cats eat from the fresh-kill
  pile, and the full log is written to `logs/simulation.log`.

### Milestone 2 – First Territory

- The first explorable world, something tangible to take part in.
  **[Decided]**
- First RPG elements: basic hunting and fighting. **[Decided]**
- This adds the **full simulation level** for one territory. **[Proposed]**

### Later **[Proposed]**

- Switching between levels at borders and after time skips
- Other Clans, Gatherings, predators, disasters
- Rank progression (apprentice → warrior / medicine cat → deputy → leader)

## 9. Current State and Next Steps

### Already built

- Cats with rank, age, hunting skill, diligence and satiation
- ThunderClan from the start of *Into the Wild* (16 cats)
- Time: 4 ticks per day, moons and the four seasons
- Fresh-kill pile and eating order
- Leader AI for hunting patrols (several patrols at once, best hunters first,
  planning margin, no back-to-back patrols)
- Seasons change the hunting chance
- Full log to a file, moon summaries in Godot's Output panel

### Possible next steps **[Proposed]**

Ordered so that each step makes the next one more interesting:

1. **Consequences of hunger:** starving cats hunt worse, get weaker, and can
   die. First real stories ("the hard leaf-bare").
2. **Aging and training:** cats age every moon. Apprentices get better at
   hunting, kits become apprentices, warriors become elders. Fits "skill
   matters".
3. **Mentors:** every apprentice has a mentor, and a good mentor trains
   faster. First relationship between cats, and the basis for "I trained him".
4. **Injuries:** hunting can hurt a cat, and an injured cat hunts worse. First
   step toward the health system.
5. **Chronicle:** important events (deaths, new apprentices, new warriors) go
   into a separate log that reads like the Clan's history.

## 10. Story Mode (Idea)

A mode in which the story of the books *can* happen again, but doesn't have
to. **[Proposed]**

- **Book start (scenario):** the world starts at the beginning of *Into the
  Wild*, with the cats, ranks, mentors, relationships and personalities from
  the book. After that, the simulation runs freely. Like CK3's bookmarks or
  RimWorld's scenarios. `StartingClans` is already a first version of this.
- **Story events with conditions:** important moments from the books are
  events that only happen when the simulation makes them possible. Example:
  *Tigerclaw's betrayal* only happens if Tigerclaw is ambitious, isn't leader
  yet and his loyalty to Bluestar is low. If things go differently, the event
  doesn't happen or happens to another cat.
- **Story director (optional):** gently pushes the world toward the books,
  e.g. by letting Tigerclaw's ambition grow a little faster, without forcing
  anything.
- **No fixed script:** events are never forced against the simulation (e.g.
  "Redtail must die at Sunningrocks"). That would fight the simulation and
  needs a lot of special cases.

What this means for later systems:

- Story events use the **same event system** as emergent events (fires,
  badger attacks, rivalries), so story mode is just a set of special events.
- That event system needs to check conditions about cats, like "is
  ambitious", "loyalty to the leader is low" or "is deputy".

Note: replaying the books' plot is closer to the original work than using the
setting alone. Fine for a personal or fan project, but relevant if the game is
ever released commercially.
