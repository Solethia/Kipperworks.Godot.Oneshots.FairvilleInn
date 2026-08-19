# Oneshot - Fairville Inn

Living game-design document. This captures the current direction at a broad level; details should be refined as the prototype develops.

## Project summary

**Working title:** Oneshot - Fairville Inn
**Engine:** Godot 4.7
**Current project type:** 3D prototype
**Status:** Early exploration

Fairville Inn is an oneshot-scoped scenario based on TTRPG-inspired world systems. It is a small, focused game set around an inn in the town of Fairville. The player experience, genre, and final scope are intentionally still being defined.

## Vision

Create a memorable, approachable inn-centered experience with a strong sense of place, readable interactions, and a scope that can be completed as a small solo project.

## Player experience

The player should feel:

- Curious about the inn and its visitors.
- Able to understand the world through exploration and interaction.
- Rewarded for making meaningful choices.
- A growing sense of ownership over the inn.

## Setting

Fairville is a small community whose inn acts as a meeting point for locals, travelers, and unusual events. The inn should provide a compact setting that can be expanded gradually without requiring a large open world.

### Tone and style

- Warm, characterful, and inviting.
- Grounded enough to make the inn feel lived-in.
- Room for humor, mystery, or light fantasy as the concept develops.

## Core gameplay loop

The intended loop is:

1. Explore the inn and notice what needs attention.
2. Talk to visitors or inspect objects.
3. Choose an action, solve a small problem, or complete a task.
4. See the inn and its relationships change.
5. Prepare for the next visit, day, or event.

This loop is provisional and should be validated with a small playable prototype.

## Design pillars

1. **A small world with depth** - Prefer meaningful details and recurring characters over large areas.
2. **Clear interactions** - The player should quickly understand what can be examined or used.
3. **Consequences that are easy to read** - Choices should change dialogue, relationships, the inn, or future events.
4. **Finishable scope** - Build a complete small experience before adding breadth.

## Object interaction

Interactions with physical inanimate objects in the world.

### Doors

- Open door.
- Unlock door with a key.
- Unlock door with lockpicking.
- Break down door with hands or an item, such as an axe.

### Levers

- Pull lever.
- Trigger a world effect, such as opening a door or casting an explosion at a target position.

### Loose items

- Pick up item.
- Add item to the player's inventory.

## Environment interaction

Environmental effects provide unique interactions and configurable modifiers.

### Terrain effects

| Terrain | Effects |
| --- | --- |
| Mud | Slowed: Medium |
| Water | Wet: Low; Slowed: Low |
| Deep water | Traversable only while flying, swimming, or using a similar effect; Wet when not flying |
| Electrified water | Electric damage per second |
| Stone floor | Footstep sound modifier |
| Wood floor | Footstep sound modifier |
| Dirt floor | Footstep sound modifier |

## Effects

Effects are intended to be data-driven so their values can be configured per source, terrain, item, or spell.

### Slowed

- **Low:** 10% reduction in movement speed.
- **Medium:** 33% reduction in movement speed.
- **High:** 66% reduction in movement speed.
- **Immobilized:** 100% reduction in movement speed.

Additional effects, such as Wet and electrical damage over time, remain to be defined.

## Fighting enemies

Turn-based, TTRPG-inspired fighting system.

- Starting a fight rolls initiative.
- Combat proceeds in turns.
- Weapons provide hit and damage modifiers.
- Armor affects the chance to hit.
- Characters can attack with weapons or other equipped items.
- Spells use a rune-based casting system and require rune-bearing implements.
- Scrolls provide another way to cast a spell.

The first combat prototype should focus on one player, one enemy, initiative, one attack, and one meaningful outcome.

## Spells

To cast a spell, the player combines runes. Runes must be written on a medium and attached to an implement. Some implements can enhance the runes assigned to them.

Generally, the runes determine the spell being cast, while the medium and implement modify its strength, uses, targets, or other behavior. Mediums affect how many uses a spell has and how strong it is. The better a medium's type matches the spell being cast, the stronger the spell should be.

The number of uses is generally tied to the magical potential of the specific medium rather than only its type. Once the medium or mediums in an implement are used up, they become inert and cannot be used for spells again.

### Initial spell ideas

#### Life spells

- Identify.

#### Earth spells

- Stone bullet.
- Sink hole.

#### Fire spells

- Fire arrow.

#### Ice spells

- Ice arrow.

#### Other spells

- Conjure illusion.

These spell names are examples for prototyping, not a final spell list.

### Spell medium types

All medium types apply a 2x spell-strength multiplier to their matching affinity.

| Medium | Affinity |
| --- | --- |
| Stone | Earth |
| Wood | Fire |
| Glass | Air |
| Fabric | Life |
| Paper | Earth, Fire, Air, Life |

Mediums can come in many variations with different magical potential and attributes.

### Spell medium attributes

- **Common:** Adds `1 / spell complexity` spell uses when runes are inserted into an implement.
- **Magical:** Adds `1 / (spell complexity * 2)` spell uses when runes are inserted into an implement.
- **Unique: Chaos:** 50% chance to change a rune when the spell is cast.
- **Unique: Twin-spell:** Adds one extra target to the spell.

The exact rounding rules, strength calculations, and interpretation of "spell complexity" need to be defined during implementation.

## Inventory

The player inventory stores loose items, quest items, keys, spell media, runes, and scrolls. Capacity, stacking, sorting, and persistence are to be defined.

## Equippable items

Weapons, armor, spell implements, and other usable equipment can be equipped into appropriate slots. Equipment should communicate its effects clearly and support the combat and interaction systems.

## Traders

Traders can offer items, buy items, and provide information or story hooks. The trading economy and currency are to be defined.

## Story

The game is structured as a contained oneshot scenario. Story content should be achievable within a focused play session while allowing exploration, choices, and multiple approaches to problems.

## Locations

Locations that can be explored in the game.

### Inn common room

The social hub and likely starting area for the scenario.

### Inn guest room 1

### Inn guest room 2

### Inn guest room 3

### Inn guest room 4

### Inn guest room 5

### Inn kitchen

### Inn basement

### Inn basement cave 1

Entered through a hole in the Inn basement.

### Inn basement cave 2

### Inn basement cave 3

### Inn basement cave 4

## Initial feature scope

### Prototype

- One explorable inn interior.
- Basic third-person or first-person movement.
- A small set of interactable objects.
- One or two visitors with simple dialogue.
- One complete task or short story beat.
- A clear beginning and end to the prototype loop.

### Possible later additions

- Multiple rooms or nearby Fairville locations.
- More visitors with schedules and relationships.
- Inn upgrades, decoration, or management.
- Inventory and item-based interactions.
- Branching events and multiple outcomes.
- Save/load and progression across days.

## Technical direction

- Use modular Godot scenes for rooms, characters, interactables, and UI.
- Keep game rules separate from presentation where practical.
- Prefer signals for communication between independent objects.
- Keep global state small and intentional.
- Test frequently with an exported build.

## Art and audio direction

To be decided. The first art pass should prioritize readability, atmosphere, and rapid iteration over production detail.

## Milestones

### Milestone 1: Walkable prototype

- Player can enter and move through the inn.
- Camera and collision feel reliable.
- Basic lighting and navigation establish the space.

### Milestone 2: Interaction prototype

- Player can inspect objects and speak with a visitor.
- One task can be completed from start to finish.

### Milestone 3: Vertical slice

- A polished short sequence demonstrates the intended tone and gameplay loop.
- Temporary assets are replaced where they affect the player experience.

## Open questions

- What is the primary genre: narrative adventure, management, mystery, or another direction?
- What viewpoint and movement style best support the experience?
- What does the player do that makes Fairville Inn distinct from other inn games?
- Is the game real-time, day-based, or event-based?
- What is the smallest complete release that would feel satisfying?
- What tone and visual style should guide the final art?
- How much of the TTRPG-inspired rules should be visible to the player?
- Which systems are essential to the oneshot, and which should remain future expansions?

## Decisions log

Record important decisions here, including the date, the decision, and why it was made.

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-19 | Start with a compact 3D inn prototype. | Keeps the first milestone focused and finishable. |
