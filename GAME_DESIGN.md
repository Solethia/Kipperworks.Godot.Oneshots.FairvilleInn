# Fairville Inn

Living game-design document. This captures the current direction at a broad level; details should be refined as the prototype develops.

## Project summary

**Working title:** Fairville Inn  
**Engine:** Godot 4.7  
**Current project type:** 3D prototype  
**Status:** Early exploration

Fairville Inn is a small, focused game set around an inn in the town of Fairville. The player experience, genre, and final scope are intentionally still being defined.

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

## Decisions log

Record important decisions here, including the date, the decision, and why it was made.

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-19 | Start with a compact 3D inn prototype. | Keeps the first milestone focused and finishable. |

