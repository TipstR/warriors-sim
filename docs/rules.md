## Rules for 1st implementation
- Hunger works more like satiation.
- A cat should survive for a few days without food.
- after 36 hours satiation will be depleted.
- debuffs start after that and increase over time. (not yet important)
- Different food gives different satiation values.
- We start with only mice that give 25 satiation.
- Eating hierarchy (first to last):
  - Kits
  - Elders
  - Sick
  - Leader
  - Deputy
  - Warriors
  - Apprentices

- Eating can happen at any ticks, (but night should be avoided)
- if the pile has fewer pieces than there are cats,
Leader sends a hunting patrol, picking the best rested or best hunters.