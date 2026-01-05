# Week 7 – Improving Immersion, Game Flow, and VR UI in the Quickdraw Simulator

---

## Overview

The focus of the final lab week was on polishing the VR Quickdraw Simulator and making it feel like a complete and immersive experience rather than a technical prototype.
At the start of the week, I also addressed a timing issue identified in the previous lab, where the enemy’s draw animation did not correctly reflect the intended reaction time. I resolved this through minor animation controller tuning
For the rest of the week, I focused on three main areas:

- Improving immersion through environment layout
- Refactoring the duel system to remove automatic resets
- Adding VR-compatible UI, scoring, and a main menu

---

## Environment Design and Immersion

To make the duel feel more authentic, I redesigned the scene using assets from the Polygon Western Pack in Unity Asset Store. The goal was to recreate a classic western shootout setting instead of a flat test scene.

The buildings were arranged to form a narrow street, forcing the player’s focus forward toward the enemy. This helped reinforce tension and made the duel feel more grounded.

![Screenshot 2025-12-30 121100.png](Images/Week7/Screenshot%202025-12-30%20121100.png)

This layout also helped with VR comfort:

- Clear sightlines
- Minimal visual clutter
- Enemy positioned at a consistent and readable distance

---

## Refactoring the Duel Flow

Previously, the duel system automatically restarted after each win or loss. While this worked technically, it broke immersion and made it hard for the player to understand what just happened.

I refactored the DuelManager so that each duel now has a start, end, and an outcome state.

Instead of resetting the duel immediately, the game now:

- Displays a result message (win or loss)
- If the player wins, displays the reaction time
- Returns the player to the main menu scene

This change made the game feel more intentional and easier to follow.

---

## Measuring Reaction Time and Scoring

To reinforce the idea of a “quickdraw,” I added a reaction time measurement.

The time is recorded at the exact moment the “DRAW!” signal occurs:

```csharp
drawStartTime = Time.time;
```
When the player wins, the reaction time is calculated:

```csharp
float reactionTime = Time.time - drawStartTime;
```
This value is then compared against a stored best time using PlayerPrefs. If the player beats their previous record, the new time is saved and displayed.

This system adds:

- Clear performance feedback
- Motivation to play again
- A competitive element without complexity

---

## In-Game Player Feedback

To make the duel rules clear without external explanation, I added several in-world UI prompts:

- “Don’t grab the gun yet” during the waiting phase
- “Shoot!” when the duel begins
- Win or loss messages after the duel ends

These UI elements are toggled directly by the duel state.

![Screenshot 2025-12-30 110145.png](Images/Week7/Screenshot%202025-12-30%20110145.png)

![Screenshot 2025-12-30 114729.png](Images/Week7/Screenshot%202025-12-30%20114729.png)

![Screenshot 2025-12-30 110155.png](Images/Week7/Screenshot%202025-12-30%20110155.png)

This made the game easier to understand and reduced player confusion, especially for first-time players.

---

## VR Main Menu and UI Setup

To support the new duel flow, I added a VR-compatible main menu using Unity’s XR UI setup.

The menu uses:

- A World Space Canvas
- XR UI Input Module
- Tracked Device Graphic Raycaster

This allows the player to interact with the menu using VR controllers instead of traditional mouse input.

![Screenshot 2025-12-30 114803.png](Images/Week7/Screenshot%202025-12-30%20114803.png)

The menu is controlled by a simple MainMenuController script that:

- Loads the duel scene
- Displays the best recorded reaction time

```csharp
float bestTime = PlayerPrefs.GetFloat("BestReactionTime", 1000.0f);
```

This ensures the high score persists between sessions and reinforces the competitive aspect of the game.

---

## Results

By the end of this week, the VR Quickdraw Simulator had evolved from a technical demo into a complete gameplay loop:

- A believable western duel environment
- Clear visual and audio feedback
- Reaction-time-based scoring
- Persistent high scores
- A proper start and end flow via a main menu

The game now feels structured, readable, and immersive.

---

## Reflection

This final lab week showed how important polish and presentation are in XR applications. The core mechanics were already functional, but without clear feedback and flow, the experience felt unfinished.

Refactoring the duel logic and adding UI systems significantly improved clarity and immersion. If more time were available, I would focus on difficulty tuning and tutorial guidance, but this iteration successfully ties together the technical and experiential goals of the project.