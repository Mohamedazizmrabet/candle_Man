# Wickfall — Game Core and Teacher Q&A

Use this sheet to revise how the current project works. The answers below describe the code in this Unity project; you can shorten them when answering out loud.

## 30-second explanation

**Wickfall is a 2D top-down dungeon action game about a living candle. Wax is both the player's survival resource and the source of their light. Light helps the player see, but it can also make enemies notice them sooner. The player can trim the wick to reduce light and detection at the cost of slower movement and weaker attacks. Braziers act as checkpoints, and the demo ends with a victory screen after the Demon Lord is defeated.**

## Likely questions and answers

### 1. What is the game about?

You explore dungeons as Candleman, fight enemies, collect wax, and try to keep your flame alive. The main decision is balancing light and safety: a brighter flame helps with visibility but makes it easier for enemies to detect you.

### 2. What did you build the game with?

The project is made in Unity using C#. It uses Unity's 2D tools and Universal Render Pipeline for lighting, plus the Input System for keyboard, mouse, and gamepad controls.

**Technical detail:** Gameplay behavior is written as `MonoBehaviour` components attached to scene objects or prefabs. Tuning data is stored in `ScriptableObject` assets under `Assets/Data`, while the input actions are in `Assets/Settings/Wickfall_Input.inputactions`.

### 3. What is the core gameplay loop?

Explore a room, manage wax, avoid or fight enemies, collect wax drops, rest at braziers, and find the stairs to the next level. Wax continually drains, so players need to keep moving and use checkpoints and pickups wisely.

### 4. What does wax do?

Wax is the player's life resource. It drains over time, powers the candle's light, and is spent on attacks. Taking damage removes wax. If it reaches zero, the player is extinguished and respawns after a short delay at the latest brazier or level start.

**Technical detail:** `Assets/Data/Player/PlayerConfig.asset` sets a maximum and starting amount of 100, passive loss of 0.5 wax per second, and an attack cost of 2. `WaxSystem.Update()` subtracts `passiveLossPerSecond * LossMultiplier * deltaTime`; damage and attacks also change its `Current` value. At zero, it raises the `PlayerExtinguished` event. Every change raises a `WaxChanged` event for `WaxBarHUD` and `LightController`. While lit, `LightConfigSO.RadiusFor()` interpolates the light radius from 1.2 units at empty wax to 6 at full wax; the extinguished state turns the light off.

### 5. Why does light affect enemy difficulty?

Enemy detection range is calculated from a base distance plus the player's light radius, multiplied by the player's detection multiplier. A larger flame gives enemies a larger detection range. Trimming the wick reduces that range.

**Technical detail:** `EnemyConfigSO.DetectionRange()` calculates `(detectionBase + lightRadius * detectionPerRadius) * detectionMultiplier`. The default base is 2 and the per-radius multiplier is 1.5. The Dripling remembers the player for 3 seconds; the Demon Lord config uses 8 seconds. `EnemyPerception.Sense()` updates this each frame and can also use a `Physics2D.Linecast` when sight-blocker layers are assigned.

### 6. What does trimming the wick do?

Holding the Dim key enters a sneaking state. In the current tuning, the candle's light radius becomes 35%, wax drains at 25% of its normal rate, enemy detection is multiplied by 0.4, damage is halved, and movement speed is halved. It trades speed and damage for stealth and longer wax life.

**Technical detail:** `DimController.Update()` checks whether Dim is held and whether the player is allowed to trim. Its `Apply()` method sets multipliers on `WaxSystem`, `LightController`, and `PlayerRuntimeSO`. `PlayerDimState` applies the slower speed; `PlayerController.AttackDamage` applies the damage multiplier. The numbers come from `Assets/Data/Player/DimConfig.asset`.

### 7. How does the checkpoint system work?

Move close to a brazier and press **E**. It lights, refills wax, and broadcasts a respawn position just in front of itself. The player controller uses that position for respawning and saves the scene and position with `PlayerPrefs`, so Continue can restore it later.

**Technical detail:** Each level builder places a `Brazier` prefab where the map contains `B`. `Brazier.Update()` checks whether the registered player is within its 2.2-unit interaction radius. Pressing Interact calls `Rest()`, which raises the `BrazierRested` ScriptableObject event with the brazier position plus its respawn offset. `PlayerController` listens, updates its in-memory `respawnPoint`, and calls `GameProgress.SaveCheckpoint(sceneName, point)`. `WaxSystem` listens to the same event and refills to maximum. If wax reaches zero, `PlayerExtinguishedState` waits the configured 2 seconds, then `PlayerController.Respawn()` teleports back and raises `PlayerRespawned`, which refills wax again.

### 8. What happens when the player reaches a new level?

The stairs trigger a scene transition. The game records the destination level, then saves that level's authored player start when the player spawns there. If the player rests at a brazier in that level, the brazier position replaces the level start as the saved checkpoint.

**Technical detail:** The stairs GameObject has a trigger collider and `LevelExit`. `OnTriggerEnter2D()` accepts only the player's non-trigger collider with the `Player` tag. When `FadeToScene()` starts successfully, the exit calls `GameProgress.PrepareForLevel(nextScene)`. The new level's `PlayerController.Awake()` calls `ResolveSpawn()` using its authored `P` position; because the new scene has no checkpoint yet, that position is saved as the Continue point.

### 9. What is the difference between New Game and Continue?

New Game clears the saved run and loads Level 1. Continue loads the most recently saved scene and spawn point. If there is no save yet, Continue starts at Level 1.

**Technical detail:** The main menu's `Button_NewGame` Inspector event calls `MainMenuController.StartNewGame()`. That calls `GameProgress.BeginNewGame()`, which deletes the progress keys, then loads `Level_01` immediately. `Button_Continue` calls `ContinueGame()`, which passes `GameProgress.ContinueScene` to `SceneFader.FadeToScene()`. `ContinueScene` reads the saved scene key and falls back to `Level_01` when there is no run yet.

### 10. Where is the progress saved?

`GameProgress` stores the level name and spawn coordinates in Unity's `PlayerPrefs`. This is local save data on the device; it is not a cloud save system.

**Technical detail:** The saved keys are `wickfall.progress.has`, `wickfall.progress.scene`, `wickfall.progress.spawn.has`, `wickfall.progress.spawn.x`, and `wickfall.progress.spawn.y`. `SaveCheckpoint()` writes the scene and coordinates, `PrepareForLevel()` writes the new scene and marks that its spawn still needs to be recorded, and `BeginNewGame()` deletes those keys. The script is a static class, so it is not attached to a GameObject.

### 11. How does the menu hover effect work?

Each menu entry has a `MenuEntry` component. When the pointer enters a button, it selects that button through Unity's UI `Selectable` system. Selecting an entry moves the `MenuHighlight` frame by copying the entry's anchors. The same selection event also supports keyboard and gamepad navigation.

**Technical detail:** `MenuEntry` implements `IPointerEnterHandler` and `ISelectHandler`. `OnPointerEnter()` calls `Selectable.Select()` if the button is interactable. Unity then invokes `OnSelect()`, which calls `MoveHighlightHere()`. The `MenuEntry` reference to the shared `MenuHighlight` calls `MoveTo()` with that button's `RectTransform`; the highlight copies `anchorMin` and `anchorMax` so it occupies that entry's slot. Keyboard/gamepad navigation selects a `Selectable` too, so it uses the same highlight path.

### 12. How are the menu buttons connected?

The main menu's public methods—`StartNewGame`, `ContinueGame`, `OpenSettings`, `CloseSettings`, and `Quit`—are assigned through each button's **On Click()** list in the Inspector. The menu script no longer adds those button callbacks at runtime.

**Technical detail:** In the scene file, each Button's serialized `m_OnClick` persistent call targets the `MainMenu` GameObject and names one public `MainMenuController` method. For example, `Button_Continue` calls `ContinueGame`. `MenuEntry` handles hover/selection separately; it does not start the game. The main-menu `Button.AddListener` setup was removed so the Inspector is the source of those button click connections.

### 13. How do scene transitions work?

`SceneFader` puts a full-screen black UI overlay above the scene, fades it using unscaled time, loads the destination scene, then fades back in. Using unscaled time lets the transition work even when gameplay is paused. Scenes must be included in Unity's Build Profiles scene list.

**Technical detail:** The Continue button uses `MainMenuController.ContinueGame()` → `GameProgress.ContinueScene` → `SceneFader.FadeToScene(scene, 0.6f, 0.8f)`. `MainMenuController.Start()` already called `FadeFromBlack()` when the menu opened, so the persistent fader normally exists before Continue is clicked. The runtime-created `SceneFader` GameObject has a screen-space `Canvas` at sorting order 1000, a `GraphicRaycaster`, and a full-screen child `Image` named `Overlay`. `Run()` sets `IsFading`, blocks raycasts, fades alpha to 1, calls `SceneManager.LoadSceneAsync`, waits for loading and one extra frame, then fades alpha back to 0 and re-enables clicks. The timings are serialized on the `MainMenu` object in `MainMenu.unity`. The menu `CanvasGroup` also becomes non-interactive when the transition begins, which prevents repeated clicks. See the detailed Continue walkthrough below for the complete object-by-object flow.

#### Detailed walkthrough: Continue's fade

1. **The button and menu object are in `Assets/Scenes/MainMenu/MainMenu.unity`.** The `Button_Continue` object has a Unity `Button` and a `MenuEntry`. The scene's Button **On Click()** event targets the `MainMenu` object and calls `MainMenuController.ContinueGame()`.
2. **Unity dispatches the click.** The scene's `EventSystem` and the menu Canvas `GraphicRaycaster` detect the pointer click. The Button invokes the Inspector event stored in the scene file.
3. **`ContinueGame()` chooses the destination.** In `Assets/Scripts/UI/MainMenuController.cs`, it asks `GameProgress.ContinueScene` for the saved scene (or `Level_01` if there is no save), then calls `SceneFader.FadeToScene(...)`. The MainMenu scene sets the fade-out duration to **0.6 seconds** and fade-in to **0.8 seconds**.
4. **The fader is already created when Continue is clicked.** `MainMenuController.Start()` calls `SceneFader.FadeFromBlack()` when the menu opens. That call accesses the lazy `Instance` property; if no fader exists yet, `Create()` makes a runtime GameObject named `SceneFader`, adds a screen-space overlay Canvas and a `GraphicRaycaster`, and creates a full-screen child Image named `Overlay`. Continue reuses this same fader. You do not add a SceneFader object to the menu in the Editor. The canvas sorting order is 1000 so it appears above the menu and game UI, and the fader uses `DontDestroyOnLoad` to survive the scene change.
5. **The `Run()` coroutine performs the transition.** It sets `IsFading`, enables the overlay's raycast blocking, fades the Image alpha from its current value to black, loads the selected scene asynchronously, waits one extra frame for the new scene's `Start()` methods and camera to initialize, then fades the Image back to transparent. It finally turns off raycast blocking and clears `IsFading`.
6. **The menu prevents repeat clicks.** When `FadeToScene()` reports that it started successfully, `ContinueGame()` makes the main menu `CanvasGroup` non-interactive while the transition runs.

The order is: **Button_Continue → Inspector On Click() → MainMenuController.ContinueGame() → GameProgress.ContinueScene → SceneFader.FadeToScene() → SceneFader.Run() → load level → fade back in.**

New Game intentionally uses another path: `StartNewGame()` clears the saved run and calls `LoadSceneImmediately("Level_01")`. It cancels any opening fade and loads without a transition fade.

### 14. How is the player programmed?

`PlayerController` uses a state machine. Separate states handle idle, walking, trimming the wick, attacking, dashing, being hurt, and being extinguished. The controller reads input in `Update`, updates the current state, and applies physics movement in `FixedUpdate`.

**Technical detail:** In `Awake()`, the player validates its references, finds its `Rigidbody2D`, resolves the saved spawn, constructs each state object, and registers its live position/alive state in `PlayerRuntimeSO`. In `OnEnable()`, it enables the gameplay input map and subscribes to events. `Update()` reads and snaps movement to four directions, updates facing and ticks the current state; `FixedUpdate()` calls that state's physics tick; `LateUpdate()` publishes the player's current position for other systems.

### 15. What is a state machine?

A state machine keeps track of one active behavior at a time. When the player changes state, the old state's `Exit` runs and the new state's `Enter` runs. Each state then handles its own updates and transitions. This keeps attack, dash, hurt, and movement behavior separate.

**Technical detail:** The shared `StateMachine` stores one `IState` in `Current`. `ChangeState(next)` exits the previous state, assigns the new state, then enters it. `Tick()` and `FixedTick()` forward updates to the active state. The interface requires `Enter`, `Tick`, `FixedTick`, and `Exit`, so each behavior has a predictable lifecycle.

### 16. How does attacking work?

An attack spends wax first, stops the player briefly, and aims in the direction Candleman is facing. A trigger hitbox activates only during the middle part of the swing. The hitbox checks teams so the player cannot damage their own team.

**Technical detail:** Attack presses are buffered for 0.15 seconds. `PlayerController.TryStartAction()` prioritizes a ready dash, then checks the attack cooldown and calls `WaxSystem.TrySpend(attackWaxCost)`. The attack state rotates the attack pivot toward `Facing`, then enables the hitbox from 30% to 70% of the swing duration. The current player config sets 10 damage, a 2-wax cost, and a 0.3-second duration. `Hitbox` finds `IDamageable` on the collided object or its parent and ignores targets on the same `Team`.

### 17. How does dashing work?

Dash uses the current movement direction, or the facing direction if the player is standing still. It briefly increases movement speed and makes the player invulnerable. A cooldown prevents continuous dashing.

**Technical detail:** `PlayerDashState.Enter()` chooses the direction and disables wick trimming. Its `FixedTick()` applies `moveSpeed * dashSpeedMultiplier`; the current config uses 3× speed for 0.15 seconds, with a 0.6-second cooldown. `PlayerController.IsInvulnerable` returns true while the active state is Dash, so incoming hits are ignored during the dash.

### 18. How do enemies find and chase the player?

Enemies read shared player data from `PlayerRuntimeSO`, including position, light radius, alive state, and detection multiplier. They can remember the last seen position for a few seconds, then move through states such as wander, alert, chase, attack, return, hurt, and dead. Optional sight-blocker layers can make walls block line of sight.

**Technical detail:** `EnemyBrain.Update()` calls `EnemyPerception.Sense()` before ticking its state machine. Perception checks whether the player is registered and alive, calculates range from `EnemyConfigSO`, checks distance, and optionally runs a linecast. When the player is lost, `TimeSinceSeen` increases; `RemembersPlayer` stays true until the config's `memoryDuration` expires. The AI states live under `Assets/Scripts/Enemies/States/`. Enemy stats are data assets, so different enemies can share the brain code with different tuning.

### 19. How does combat damage work?

An active `Hitbox` looks for an `IDamageable` target and sends it a `DamageInfo` containing damage, source position, team, and flame type. Enemies use `Damageable` health; the player has a `PlayerHurtbox` instead, which passes a hit to `PlayerController` and removes wax. Damage and death events let enemies react and the Demon Lord trigger the ending.

**Technical detail:** `Hitbox.OnTriggerEnter2D()` (and optionally `OnTriggerStay2D()` for repeated hazards) calls `TryHit()`. It uses `GetComponentInParent<IDamageable>()`, filters same-team targets, and remembers which targets were hit during this activation. `Damageable.TakeHit()` rejects dead, friendly, or temporarily invulnerable targets, updates health, invokes `Damaged`, and invokes `Died` at zero. `PlayerHurtbox` implements the same interface but forwards damage to `PlayerController.ApplyHit()`, which applies wax loss, invulnerability, knockback, and the Hurt state.

### 20. How does the game give wax back to the player?

Enemies can drop wax when they die. Pickups briefly pop out, bob, and drift toward the player when nearby. On collection, they raise a wax-gain event; `WaxSystem` receives that event and restores wax. Braziers also refill wax.

**Technical detail:** `WaxDropper` listens to `Damageable.Died`, chooses an integer amount between the enemy config's minimum and maximum (inclusive), instantiates a pickup, and gives it a scatter velocity. `WaxPickup` waits 0.3 seconds before collection and begins magnetizing inside its 2-unit radius. On touch it raises the `WaxGainRequested` channel; `WaxSystem.Gain()` clamps the result to max wax. If the player is extinguished, the pickup is left in the world rather than collected.

### 21. What happens when the Demon Lord is defeated?

The boss's `Damageable` raises its death event. `BossStatusReporter` records the win and shows a victory screen with the message that more of Wickfall is coming soon. The screen pauses gameplay and offers a button back to the main menu.

**Technical detail:** `BossStatusReporter.OnEnable()` subscribes to the boss's `Damageable.Died` event. When it fires, the reporter sends a final inactive boss status, calls `GameProgress.MarkWon()`, and calls `VictoryScreen.ShowVictory()`. The victory screen creates an overlay Canvas, text, and a Back to Main Menu button at runtime, then sets `Time.timeScale` to zero. Its button fades back to `MainMenu` and restores time scale. Boss defeat itself is not serialized, so the boss can respawn if Level 3 is loaded again.

### 22. How is the menu music made?

`GameMusic` generates a short ambient clip at runtime from low notes and a sparse melody, then loops it. It persists between scenes and ensures the active camera has an `AudioListener`. There is no imported music file for this track.

**Technical detail:** `MainMenuController.Start()` calls `GameMusic.EnsurePlaying()`. The singleton creates a persistent `Game Music` GameObject and an `AudioSource` if one does not already exist. It synthesizes a 20-second, 22,050-Hz mono clip: slow C-minor/B-flat/G-minor/B-flat chords with a sparse upper melody. The source loops at 0.28 volume. On each scene load, the script checks the `MainCamera` and attaches an `AudioListener` if the camera lacks one.

### 23. How are the levels built?

The editor scripts describe each dungeon as a text map. Each character represents a 2-by-2 world-unit tile: for example, `#` is wall, `P` is the player start, `E` is the exit, and `B` is a brazier. The level builder turns the map into a Unity scene and places the walls, props, enemies, traps, and exits.

**Technical detail:** `LevelOneBuilder`, `LevelTwoBuilder`, and `LevelThreeBuilder` provide a `string[] Map` and level settings to `DungeonLevelBuilder.Build()`. The builder checks that rows are the same width, creates tilemaps and colliders, then scans each character to place the matching prefab or special object. `P` places the player, `E` creates a trigger with `LevelExit`, and `B` places a brazier checkpoint. The build scripts are under `Assets/Scripts/Editor/`; they run in the Unity Editor, not as gameplay scripts.

### 24. What are ScriptableObjects used for?

They hold reusable configuration and shared event channels. For example, player/enemy configs store tuning values, while event channels let systems announce events such as wax gained or a brazier rested without directly depending on each other.

**Technical detail:** `PlayerConfigSO`, `EnemyConfigSO`, `DimConfigSO`, and `LightConfigSO` are asset-based settings that designers can tune in the Inspector without changing code. `EventChannelSO<T>` is a generic ScriptableObject with a `Raise()` method and an `OnRaised` event. A sender raises the channel and listeners subscribe in `OnEnable()` and unsubscribe in `OnDisable()`. For example, Brazier raises `BrazierRested`; PlayerController saves the respawn point and WaxSystem refills, without Brazier needing direct references to either component.

### 25. What are the three demo levels?

1. **Level 1 — Champs de Suif**
2. **Level 2 — Sunken Crypt**
3. **Level 3 — Demon Gate**, ending with the Demon Lord

**Technical detail:** The playable scenes are `Assets/Scenes/lvls/01_ChampsDeSuif/Level_01.unity`, `Assets/Scenes/lvls/02_SunkenCrypt/Level_02.unity`, and `Assets/Scenes/lvls/03_DemonGate/Level_03.unity`. Each has a corresponding editor map script: `LevelOneBuilder`, `LevelTwoBuilder`, or `LevelThreeBuilder`.

### 26. How do the Settings controls work?

Settings lets the player change master volume and fullscreen mode.

**Technical detail:** `MainMenuController.OpenSettings()` swaps the main and settings `CanvasGroup`s with a 0.2-second UI fade. `SettingsMenu` initializes a 0–10 whole-number volume slider from `GameSettings.Volume`; changing it writes a 0–1 value to `AudioListener.volume`. The fullscreen toggle reads and writes `GameSettings.Fullscreen`. `GameSettings` stores both preferences in `PlayerPrefs`, and the settings panel saves them when disabled.

### 27. How does the game read player input?

Gameplay input is gathered in one place and sent to the player and interactive objects.

**Technical detail:** `InputReaderSO` wraps the `Gameplay` map in `Wickfall_Input.inputactions`. It reads movement and Dim directly, and publishes events for Attack, Dash, Interact, and Pause. `PlayerController`, `Brazier`, and `Chest` subscribe to the events they need; they unsubscribe when disabled. When the pause menu sets `Paused`, movement, Dim, attack, dash, and Interact are ignored, while Pause can still be received.

### 28. How does pausing work?

The Pause key stops gameplay and opens the pause panel; Resume restores gameplay.

**Technical detail:** `PauseMenu` listens to the input reader's Pause event. `SetPaused(true)` sets `Time.timeScale` to 0, marks `InputReaderSO.Paused`, and shows its `CanvasGroup`. Resume reverses those values. The `SceneFader` uses `Time.unscaledDeltaTime`, so scene fades can still run while the game is paused.

## Controls

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move | WASD or arrow keys | Left stick |
| Attack | J or left mouse button | West button |
| Dash | Space | South button |
| Trim the wick | Left Shift | Left trigger |
| Interact with braziers / chests | E | North button |
| Pause | Escape | Start |

## Main code files to know

| System | File |
|---|---|
| Menu buttons and menu flow | `Assets/Scripts/UI/MainMenuController.cs` |
| Menu hover and highlight | `Assets/Scripts/UI/MenuEntry.cs`, `Assets/Scripts/UI/MenuHighlight.cs` |
| Player movement and state changes | `Assets/Scripts/Player/PlayerController.cs`, `Assets/Scripts/Player/States/` |
| Wax, light, and wick trimming | `Assets/Scripts/Player/WaxSystem.cs`, `Assets/Scripts/Player/LightController.cs`, `Assets/Scripts/Player/DimController.cs` |
| Checkpoints and saved Continue progress | `Assets/Scripts/World/Brazier.cs`, `Assets/Scripts/Core/GameProgress.cs`, `Assets/Scripts/World/LevelExit.cs` |
| Combat and enemy AI | `Assets/Scripts/Combat/`, `Assets/Scripts/Enemies/` |
| Scene fades and victory ending | `Assets/Scripts/Core/SceneLoading/SceneFader.cs`, `Assets/Scripts/UI/VictoryScreen.cs` |
| Ambient music | `Assets/Scripts/Core/GameMusic.cs` |
| ASCII dungeon maps | `Assets/Scripts/Editor/LevelOneBuilder.cs`, `LevelTwoBuilder.cs`, `LevelThreeBuilder.cs` |

## Where the main scripts and objects live

| Scene object or runtime object | Components / role |
|---|---|
| `MainMenu` in `MainMenu.unity` | Menu Canvas and `MainMenuController`; holds references to buttons, CanvasGroups, and fade settings. |
| `Button_Continue` in `MainMenu.unity` | Unity `Button` plus `MenuEntry`; its saved Inspector **On Click()** event calls `MainMenuController.ContinueGame()`. |
| Other menu buttons | Their Inspector events call `StartNewGame()`, `OpenSettings()`, `CloseSettings()`, or `Quit()` on the same `MainMenuController`. |
| `EventSystem` in each UI scene | Routes pointer, keyboard, and gamepad UI input to Buttons and Selectables. |
| `SceneFader` (runtime-created) | Persistent Canvas and full-screen `Overlay` Image used for scene transitions; created automatically the first time `FadeToScene()`, `FadeFromBlack()`, or `LoadSceneImmediately()` is called. In a normal menu launch it is created by `FadeFromBlack()`. |
| Player prefab in each level | `PlayerController`, `WaxSystem`, `LightController`, `DimController`, and related combat/runtime components. `PlayerController` applies the chosen spawn point when the scene loads. |
| Each `Brazier` prefab instance | `Brazier` listens for Interact input and raises the `BrazierRested` event. `PlayerController` and `WaxSystem` receive it to save the checkpoint and refill wax. |
| Exit stairs in Levels 1 and 2 | `LevelExit`; its trigger starts the fade and records the next level for Continue. The Level 3 return-to-menu stairs do not overwrite the saved game position. |
| Demon Lord in Level 3 | `BossStatusReporter` observes the boss's death and asks the runtime `VictoryScreen` to show the ending. |

`GameProgress` and `VictoryScreen` are code systems, not objects that need to be dragged into the scene. `GameProgress` is a static class that reads and writes `PlayerPrefs`; `VictoryScreen` creates its UI at runtime after the boss dies.

## Useful honest limitation to mention

Continue saves the current level and checkpoint, but the game does not save defeated enemies. If you continue after returning to the menu from the victory screen, Level 3 is loaded from its saved checkpoint and the boss is created again with the scene.
