# Milestone 1 – First Simulation

Everything here is a starting point and will change once we see the
simulation running.

## Goal

One Clan (ThunderClan) with about 10 cats runs for **30 in-game days**. The
leader assigns hunting patrols, cats eat from the fresh-kill pile, and
everything is written to a text log. No graphics.

**Done when:** a 30-day run finishes and the log shows patrols, prey caught,
cats eating and cats going hungry.

## Cats

Each cat has:

| Field     | Type / Range                                          |
|-----------|-------------------------------------------------------|
| Name      | text                                                  |
| Rank      | kit, apprentice, warrior, deputy, leader, elder       |
| Age       | in moons                                              |
| Hunting   | skill, 0–100                                          |
| Diligence | personality trait, 0–100 (lazy cats hunt worse)       |
| Satiation | 0–100 (100 = full, 0 = starving)                      |

Medicine cats and queens come later.

## Time

- One day has **4 ticks**, each 6 hours long:
  1. **Dawn** (06:00)
  2. **Sunhigh** (12:00)
  3. **Evening** (18:00)
  4. **Night** (00:00)
- Patrols happen at dawn, sunhigh and evening. Night is for sleeping.
- A patrol lasts one tick.

## Rules

### Satiation

- A full cat (100) is empty after **36 hours = 6 ticks**, so satiation drops
  by **100 / 6 ≈ 16.7 per tick**.
- At 0 the cat is **starving**. Debuffs come later. For now this is only
  logged.
- Different prey gives different satiation. For now there are only **mice:
  25 satiation each**.

### Eating

- Cats can eat at any tick except night.
- A cat eats when its satiation is **below 75** and there is prey on the
  pile, so it keeps a reserve and survives a missed meal and the night.
- A cat eats one piece per tick at most. *(Proposed)*
- If there isn't enough prey for everyone, cats eat in this order:
  1. Kits
  2. Elders
  3. Sick (no sickness yet, so skipped)
  4. Leader
  5. Deputy
  6. Warriors
  7. Apprentices

### Hunting patrols

- Checked at dawn, sunhigh and evening: if the pile has **fewer pieces than
  there are cats**, the leader sends hunting patrols. The leader keeps sending
  patrols until they are expected to catch enough to make up the difference,
  or until no hunters are left. The leader plans with a margin of 20% and
  doesn't account for the season.
- **Who can hunt:** warriors and apprentices. The leader and deputy can
  join if needed. Kits and elders never hunt. *(Proposed)*
- **Patrol size:** 4 cats.
- **Who is picked:** the best hunters who were not on the previous patrol.
  ("Best rested" needs a sleep need, which comes later.) *(Proposed)*
- **Result:** each cat gets 6 attempts per patrol. *(Proposed)*
  - chance per attempt = 20% + hunting × 0.5% + (diligence − 50) × 0.2%
  - each success = 1 mouse on the pile
  - no carrying limit, so good hunters make a real difference: a weak, lazy
    cat (hunting 0, diligence 0) catches about 0.6 mice per patrol, an
    average cat about 2.7 and the best possible hunter about 4.8
- An average patrol brings back about 11 mice, so up to about 33 a day if all
  three patrols go out. 10 cats need about 27.

### Seasons

- The year starts with newleaf. Each season lasts **3 moons** (90 days).
- The season changes the catch chance of every hunting attempt:

| Season    | Catch chance | Goal                                      |
|-----------|--------------|-------------------------------------------|
| Newleaf   | × 1.0        | not everyone is always fed, nobody starves |
| Greenleaf | × 1.5        | no cat goes hungry                        |
| Leaf-fall | × 1.0        | not everyone is always fed, nobody starves |
| Leaf-bare | × 0.8        | starving cats are to be expected          |

## Tuning values

All numbers above live in **one place in the code**, so they can be
adjusted without searching through the rules.

| Value                     | Start value |
|---------------------------|-------------|
| Ticks per day             | 4           |
| Satiation max             | 100         |
| Ticks until empty         | 6           |
| Mouse satiation           | 25          |
| Eat below satiation       | 75          |
| Patrol size               | 4           |
| Patrol planning margin    | × 1.2       |
| Attempts per cat          | 6           |
| Base catch chance         | 20%         |
| Per hunting skill point   | +0.5%       |
| Per diligence point (±50) | ±0.2%       |
| Days to simulate          | 30          |
| Random seed               | 12345       |

## Technical decisions

- The simulation is **plain C#** with no Godot code in it. A small Godot
  scene starts it and shows the log.
- Randomness uses a **fixed seed**, so the same seed always gives the same
  run.

## Not in Milestone 1

Sleep, thirst, social and other needs; health and injuries; medicine cats;
other Clans; sim LODs; map and graphics; saving and loading.

## Open questions

- With these numbers, the Clan usually catches more than it eats, so the pile
  grows. That's fine, because the leader only sends patrols when the pile is
  low. Later, prey could go stale after a few days (crow-food) so the pile
  can't grow forever.
