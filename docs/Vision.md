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

Built so far: **hunger** (satiation and starvation stacks) and **sleep**
(energy). Hunger has consequences: starving cats get weaker (lower max energy)
and can die. Who eats first is decided by the leader's feeding policy, see
6.4.

Still to decide **[Open]**:

- What happens when the other needs aren't met (bad mood, illness, ...)?
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

**Feeding policy** (built): when there isn't enough prey, the leader decides
who eats first with weighted scores. The weights are the leader's style and
will later come from personality:

| Weight           | Means                                              |
|------------------|----------------------------------------------------|
| Starving first   | starving cats always eat first                     |
| Urgency          | the closer to death, the sooner a cat eats         |
| Compassion       | protects kits, queens, elders, apprentices         |
| Pragmatism       | feeds the best hunters                             |
| Warrior code     | kits, queens, elders, leader, deputy, warriors ... |
| Famine priority  | in a famine, hunters are fed so the Clan can hunt  |

Leader styles built so far: Balanced, Compassionate (a Firestar), Ruthless
(a Tigerclaw) and By the code (follows the code even if all apprentices
starve). Every style has its own way of failing, which creates stories.

**Succession** (built): when the leader dies, the deputy becomes leader and a
new deputy is chosen (for now: the best hunter).

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
- **Status: done and extended.** ThunderClan sustains itself over decades:
  cats are born, grow up, find mates, have kits, retire and die. A
  simulation UI in Godot shows it live and lets you change cats and tuning
  values. See section 9.

### Milestone 2 – First Territory

- The first explorable world, something tangible to take part in.
  **[Decided]**
- First RPG elements: basic hunting and fighting. **[Decided]**
- This adds the **full simulation level** for one territory. **[Proposed]**

### Later **[Proposed]**

- Switching between levels at borders and after time skips
- Other Clans, Gatherings, predators, disasters
- Medicine cats as a rank and career

## 9. Current State and Next Steps

### Already built

**World and time**

- 4 ticks per day (dawn, sunhigh, evening, night), moons, four seasons
- Territory with limited prey per day, less in bad seasons

**Cats**

- Name (prefix + suffix that changes with rank: Graykit, Graypaw,
  Graycloud, Graystar), sex, age, rank, hunting skill, diligence
- Gifts they are born with (hunting, mentoring), half inherited from the
  parents, half luck
- Mother, father and mate
- Satiation, energy (sleep), starvation stacks
- Food need by age (based on real kittens): nursing kits need no prey,
  growing cats eat about as much as adults

**Hunting and food**

- Leader sends as many patrols as needed, best hunters first, cats don't
  hunt twice in a row, tired and sleeping cats are left out
- Catch chance from skill, diligence, energy (S-curve), season and how much
  prey the territory has left today
- Fresh-kill pile with the feeding policy (see 6.4)
- Starvation: stacks lower max energy, death becomes a growing risk above 8
  stacks (twice as high for kits and elders), certain at 20
- Famine: starts when several cats are starving and only ends after three
  calmer days. Hunters are fed first, elders and queens help hunting

**Life cycle**

- Aging every moon, ceremonies (apprentice at 6 moons, warrior at 12),
  retirement at 120 moons
- Mentors: the leader gives every apprentice a mentor. How fast it learns
  depends on its hunting gift and on the mentor's mentoring gift and
  experience. Some cats are just bad mentors. The gap between the weakest
  and the best new warriors is large (below 20 to 90+ hunting)
- Death of old age from 96 moons, succession of leader and deputy
- Mates (placeholder rule), pregnancy (63 days), litters of 1–6 kits
- Nursery: queens share their milk with all kits, orphans included
- Fewer births in hunger, when crowded, and outside newleaf and greenleaf

**Tools**

- Simulation UI in Godot (`ui/simulation_ui/simulation_ui.tscn`), the main
  scene, with three tabs:
  - **Clan:** list of all cats, and a form to add cats before or during a
    run
  - **Tuning:** every balancing value, editable, changed values highlighted,
    reset to defaults
  - **Simulation:** start/restart, play/pause, speeds, Clan overview and the
    log (summary only or with details)
- Full log in `logs/simulation.log`
- All balancing values in `scripts/Simulation/Core/Tuning.cs`, the rules in
  `docs/Milestone1.md`

### Lessons from balancing

- The simulation tips over easily. Every downward spiral (hunger → weakness
  → less prey) needs a counterweight (sharing, famine rules, elders and
  queens helping).
- Without a prey limit the Clan grows forever, without births it dies out,
  and a birth brake that is too strong lets the Clan age until it collapses.
- Feeding everyone evenly makes everyone die at the same moment. Starvation
  death is a growing risk instead of a fixed limit for that reason.
- The food balance is very sensitive to the average hunting skill. When
  mentors were added, new warriors ended up only 2–3 points worse on
  average, and about four times as many Clans died out. The spread of skill
  (gifts) barely mattered, the average did.
- Right now cats only die of hunger and old age. Predators, sickness and
  Clan battles will take over part of this. Births, crowding and old age
  will need to be retuned then.

### Possible next steps **[Proposed]**

1. **Mentors, part 2:** remember every apprentice a cat has trained (not only
   the last mentor), warriors keep learning after their apprenticeship, and
   the warrior code's rule that a deputy must have had an apprentice.
2. **Danger:** injuries while hunting, predators (foxes, badgers), sickness.
   Gives the Clan other ways to lose cats than hunger and old age.
3. **Medicine cats:** heal injuries and sickness, a separate career.
4. **Chronicle:** important events (births, deaths, ceremonies, new
   leaders) in a separate log that reads like the Clan's history.
5. **Relationships and personality:** replace the placeholder rule for
   mates, leader styles from personality.
6. **Milestone 2:** the first territory as a 2D world.

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
