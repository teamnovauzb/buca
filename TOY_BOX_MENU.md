# Toy Box 3D main menu

The redesigned menu is installed in `Assets/Scenes/MainMenu.unity`. In Unity, stop the current Play session, wait for compilation, then press Play again. The editor starts MainMenu even when an Untitled scene is open.

To regenerate the prefabs and reinstall the menu, choose:

**RealBuca → Toy Box 3D → 1 - Generate and Assign Main Menu**

The command generates the content, saves nested prefabs, opens `Assets/Scenes/MainMenu.unity`, assigns the new menu, and saves the scene. Press Play to use it. The command also assigns five matching board/puck skins and the saved wooden playroom in Game. Original level layouts and colliders are preserved.

The **RealBuca → Boot From Main Menu** setting is remembered per project, including across editor restarts; turn it off explicitly when testing other scenes directly.

## What is generated

- Blue rounded wooden cabinet, birch legs, playing surface and raised inlays.
- Thick, rounded and beveled BUCA letters with separate birch backing, raised button lettering and solid scenery lettering. These are mesh assets, not TextMeshPro quads or a screenshot background.
- Coral PLAY, yellow LEVELS, mint SKINS, and blue QUIT puck controls with physical selection rings.
- Rounded mint light tubes beneath each selected cap, with a solid ivory marker. Focus appears immediately on page entry and follows mouse hover, arrow keys or the arcade stick.
- An inset clock with fourteen prebuilt 3D segments displaying the 30-second countdown.
- Matching 30-level selection tray, saved ratings and locked-level indicators.
- A five-skin tray: Classic Toy Box, Mint Garden, Honey Bee, Berry Cream and Ocean Club. Each 3D preview pairs a board frame and player puck; the chosen pair has an EQUIPPED label.
- A physical returning-player tray: CONTINUE, START 1, BACK.
- An open wooden crate, toy-room props, a perspective camera, warm lighting and a saved post-processing profile.
- Editor-baked grain and micro-normal textures, a saved studio reflection cubemap and custom reflection probe. Dedicated menu renderers add contact shading while retaining the desktop/mobile rendering modes and the gameplay renderer settings.

All generated assets live in `Assets/ToyBoxMenu/`. The complete prefab is `Assets/ToyBoxMenu/Prefabs/ToyBoxMainMenu.prefab`. Table, sign, scenery, controls and lighting also have separate nested prefabs.

The source is `Assets/Editor/ToyBox/BuildToyBoxMainMenu.cs`, with `ToyBoxGeometry.cs`, `ToyBoxSurfaceBaker.cs` and the bundled outline data alongside it. Keep those files together. Generation needs no Blender, font installation, network connection, or additional package. Generation registers the saved menu renderers with the project's URP quality assets; the menu camera uses that renderer index.

## Runtime behavior

`Assets/Scripts/ToyBoxMenuController.cs` only operates the saved scene objects: raycasts, selection, cap animation, segment visibility, existing material assignment and scene navigation. It does not create GameObjects, meshes, materials, textures, text or Canvas UI.

- Mouse: point and click a physical button.
- Arcade joystick or arrow keys: navigate. Black or Enter: confirm. White or Escape: return from a tray.
- Main menu and Levels share one continuous 30-second countdown. Timeout starts Level 1.
- SKINS pauses the countdown. Click a puck or use arrows/joystick and Enter/Black to equip; BACK or Escape/White returns home. Selection is saved locally under `BucaCosmeticSkin`.
- Manual Play offers the saved-level choice when appropriate. That choice pauses the countdown.
- Level availability and ratings read the existing PlayerPrefs keys, including later Luxodd progress updates.
- Quit uses the existing Luxodd bridge when available. In the editor it stops Play Mode.

Existing Luxodd and audio services are preserved. Old menu presentation is disabled. The old editor-only neon/gloss builders skip the new menu so they do not generate hidden UI behind it.

## Preview, regeneration and recovery

**2 - Generate Separate Preview Scene** produces `Assets/ToyBoxMenu/ToyBoxPreview.unity` without replacing MainMenu. This scene is for visual inspection; it does not contain the original Luxodd or audio services. The command disables Boot From Main Menu so Play uses the open preview. Installing the main menu enables it again.

Generation can be run again. Named assets are updated in place so their GUIDs remain stable. Changes made directly to generated objects can be overwritten; make lasting design changes in the generator.

The first install saves `Assets/ToyBoxMenu/Backup/MainMenu-BeforeToyBox.unity`. **3 - Restore Original Main Menu** restores that copy. Later regeneration does not replace the first backup.

## Validation

`ToyBoxMenuValidation.BatchValidate` is an optional batch-mode check intended for a disposable project copy: it generates previews, tests clock digits, navigation and locked levels, checks mouse hit targets and camera framing, then exercises the main-scene installation and backup. It writes renders to `output/toy-box/`. It does not test a live Luxodd cabinet session.

`ToyBoxStartupValidation.RunBatch` checks Play Mode from an empty Untitled scene through MainMenu and its Play action into Game, including a running countdown, preserved services, an active puck and all 30 level references. It also checks the startup setting after leaving Play Mode and restores the progress preferences used by the test. Run it only in a disposable batch-mode project copy. The check passed on Unity 6000.5.0f1; that editor's separate batch Search index exception is logged as an editor diagnostic rather than a gameplay failure.

The project previously had an editor compile dependency on the optional WebGL module in `BuildBucaQuickPreview.cs`. That import and the platform-specific optimization settings are now guarded; the build command reports missing WebGL support explicitly.

## Board and player skins

`BuildToyBoxSkins.cs` and `BuildToyBoxPlayroom.cs` extend the same Editor generator. Five saved material palettes and inlay meshes are shared by the menu preview and gameplay. `BucaSkinBinding` assigns existing material/mesh references on enable; it never constructs geometry or materials. The generated playroom, player inlay and skin selector are saved prefabs.

Generated campaign variants are in `Assets/ToyBoxMenu/Levels/`. All 30 original prefabs remain in `Assets/Prefabs/Levels/`. The generated variants preserve their collision shapes, obstacle positions, starts and goal triggers; skins add only visual shells/materials. Game's LevelManager points to the generated variants. Existing campaign logic still instantiates these authored prefabs when changing levels.

`Assets/ToyBoxMenu/Backup/Game-BeforeSkins.unity` preserves the Game scene before the first skin installation. Restore that scene into `Assets/Scenes/Game.unity` to undo gameplay skin/environment assignment. Restoring the original main menu alone does not undo gameplay skins.

Validation now includes all five physical skin clicks, keyboard navigation, equipped markers, selection persistence into Game, paused countdown while selecting, and collider comparisons across every campaign level. Actual Unity renders are written to `output/toy-box/Skins.png` and `Gameplay-Skin-1.png` through `Gameplay-Skin-5.png`.

## Solid gameplay polish

The skin generator now bakes raised wooden obstacle meshes onto their existing motion transforms, a thicker cabinet with visible legs, and a maple surface with a real goal cutout, dark recessed wall and beveled rim. The room floor sits below the table legs. Collision shapes and obstacle motion scripts remain authored by the original campaign.

`SolidGameplayHud.prefab` contains raised wooden level/time plaques, an inset cabinet scoreboard and physical control instructions. Its live values use ten pre-baked extruded digit meshes, selected by `ToyBoxGameplayHud`; the timer reads the same remaining-time value used by gameplay and turns coral in the final five seconds. Strokes, par, if-sunk relation and points remain visible. Leading zeroes are retained only for the level and timer. Legacy flat gameplay stats/hints are hidden; results, leaderboard and transaction panels remain functional.

Polish sources: `ToyBoxGameplayGeometry.cs`, `BuildToyBoxGameplayHud.cs`, and `ToyBoxGameplayHud.cs`. Use the same **Generate and Assign Main Menu** command to rebuild and install all saved assets. No visual geometry is generated during play.

## Premium presentation pass

Gameplay uses a fitted centered perspective camera, inset cabinet stats, birch joinery details, curved wood grain, individual oak floor planks, a modeled window alcove and a fabric-finish teddy with limbs. Directional key light, restrained ambient fill, a window bounce light, saved reflections emphasize the physical geometry. All five skins share this authored scene and retain their distinct board/puck palettes. The rendering changes remain Editor-generated and saved; the original campaign physics remain unchanged.

## Comfortable gameplay camera

The gameplay camera is centered, lower, and framed around the board and cabinet controls. Depth of field and automatic puck-follow are disabled so the playing surface stays sharp and steady. The same camera setup is retained when regenerating all assets. To update only the installed view, use **RealBuca → Toy Box 3D → 4 - Apply Comfortable Gameplay Camera** while outside Play Mode.

## Full-size skin preview

Click any skin puck to equip it and open a full-size 3D board and player preview. PREV/NEXT cycle and equip the five saved skins; BACK or Escape returns to the tray. Auto-start pauses while browsing. The preview uses saved board meshes, skin materials and room assets, with its own fitted camera. No preview geometry is created at runtime. Rebuild with Generate and Assign Main Menu.

Skin preview framing aims toward the front half of the board, making the board fill the screen while retaining the skin title and all three controls. Gameplay camera settings are unchanged.

## Chapter Islands level map

LEVELS opens five solid hexagonal islands with six numbered puck buttons each, connected in campaign order by wooden plank bridges. Island colors match the five skin palettes. Raised chapter badges, toy trees, lock tokens and earned stars are saved mesh geometry. The map fills its own perspective camera and supports mouse/arcade/arrow navigation; selection begins at the highest unlocked level. Auto-start pauses while browsing. Existing progress keys and original campaign levels are retained.

Source: Assets/Editor/ToyBox/BuildToyBoxChapterMap.cs. Generated prefabs: ChapterIslands.prefab and ChapterIsland1–5.prefab under Assets/ToyBoxMenu/Prefabs. Rebuild using RealBuca → Toy Box 3D → Generate and Assign Main Menu. Geometry and materials are created only by the Editor generator. Runtime toggles existing objects/materials and handles input. Validation covers all 31 controls, locks, portrait framing, stars wiring, skin previews and map-to-game launch.

Chapter map depth polish: thicker beveled island foundations and painted rims, raised arched plank bridges, taller puck controls with chapter-colored sockets, a lower 36-degree camera and restrained directional lighting. All geometry remains Editor-baked into saved prefabs.

Map spacing: expanded cabinet and island separation, longer bridges, 20% wider horizontal button spacing and 24% wider row spacing. Background moved back to clear the larger board. All map controls and camera framing revalidated.

## Watch-only new-mechanic tutorials

Only unintroduced mechanics in the selected real level trigger tutorials. The short saved 3D demonstration plays automatically; each clip ends with a five-second choice window. REPEAT restarts that demonstration; CONTINUE immediately advances to the next queued tutorial or resumes the real level. If no button is chosen, the countdown automatically continues. No practice board or test shot is required. Legacy practice prefabs remain stored but are not generated or activated by this flow.

A large yellow SKIP button remains visible on the front-right control shelf throughout playback. Repeat, Pause/Resume and Skip have saved 3D selection highlights. The end screen shows Repeat and Continue with five saved 3D countdown labels. Selected buttons lift and glow; activation depresses the cap for 0.16 seconds and plays the existing click sound before acting. Use the mouse, joystick or arrow keys to select; Black/Enter/Space activates the highlighted control. R replays; Escape/White/Purple skips. Input must return to neutral when entering a tutorial so the click that selected a level cannot accidentally skip it.

Completing or explicitly skipping the current mechanic marks it introduced using the existing BucaPracticeCompleted_<key> preference for compatibility. Unwatched queued mechanics remain eligible. Level 1 and levels with only familiar mechanics start normally.

Menu and tutorial pointer selection tests the active controls directly, so scenery or hidden-page colliders cannot intercept clicks. Level locking rules remain unchanged. Regenerate the tutorial prefab with RealBuca → Toy Box 3D → 5 - Generate Watch Then Try Tutorials.

Validation covers all 18 saved clips, automatic queue/return, no active practice, Skip visibility and click action, Replay/Pause selection, persistent new-mechanic scheduling, and existing level/skin selector flows. See output/toy-box/tutorial-countdown-validation.log.

## Premium win celebration

Game now assigns PremiumWinCelebration.prefab on LevelManager. Every win presents a beveled wooden medal with a cobalt face and raised gold star, staggered earned-rating stars, extruded result lettering and 36 saved solid confetti pieces. Hole-in-one wins use a special title. The award lasts 3.6 seconds, follows the gameplay camera with aspect-aware framing, and shrinks away before the existing score results. The old screen flash, ring burst and FOV kick are suppressed when the saved premium rig is assigned; existing score calculation, unlocks, platform reporting and result actions are preserved.

Regenerate with RealBuca → Toy Box 3D → 6 - Generate Premium Win Celebration. Full skin/menu generation also installs it. Runtime only animates saved transforms and toggles earned star meshes. Validation covers saved meshes, actual rating visibility, both titles and cleanup, plus existing startup, tutorial and selector flows. See output/toy-box/premium-win-validation.log.

## Guard pegs and results leaderboard

The PEGS tutorial detects the saved NM2_GuardPeg colliders, first present at level 7. Its saved 3D clip shoots between three posts into the hole; validation samples the full path to check puck clearance. It follows existing first-encounter, Skip/Repeat and five-second end-countdown rules. There are now 19 saved demonstration clips.

Ordinary level results now use ResultsAndLeaderboard.prefab instead of the old flat score breakdown and golf scorecard. The left wooden board shows the earned stars, breakdown, combo multiplier and total. The right board shows up to five real Luxodd leaderboard entries, current-player highlighting and the server-reported personal rank/score. Standalone/offline mode is explicitly labeled and shows only the current local course score with no invented global rank or players. Player names disable rich-text interpretation and truncate within their saved rows. Portrait stacks the boards. NEXT supports mouse and Black/Enter/Space, has press feedback and retains five-second automatic continuation.

The leaderboard request is read-only and separate from terminal loss/transaction flows; stale callbacks are ignored after closing. Live Luxodd service retrieval requires LUXODD_INTEGRATION and a connected SDK; local tests validate row population and the offline path, not a live server response. Regenerate using RealBuca → Toy Box 3D → 7 - Generate Results and Leaderboard. Full generation also preserves the assignment. See output/toy-box/results-leaderboard-validation.log.


## Option 3 — Winners’ Podium

Results use the selected round cobalt medal and gold earned stars on the left, an arched mint leaderboard with five spacious rows on the right, and a shared maple base with a wide pressable NEXT control. The existing five-second countdown, mouse/arcade activation, score calculation and leaderboard service remain connected. An off-list personal rank is retained in the footer. Portrait stacks the panels and hides base branding to keep the control clear.

The Editor generator saves all furniture, glyph meshes, materials and control geometry in ResultsAndLeaderboard.prefab, already assigned in Game.unity. Dynamic Latin labels and scores select pre-baked solid glyphs rather than generating geometry. Names outside the baked alphabet retain the existing TMP font fallback. The total supports all ten digits of a nonnegative 32-bit score and scales to fit. No sample players are stored in the prefab or game state.

Use RealBuca → Toy Box 3D → 7 - Generate Results and Leaderboard to rebuild. output/toy-box/Results-Podium-Example.png is an actual Unity capture with explicit test-fixture rankings; Results-Leaderboard.png shows the offline game state. See podium-validation.log for the saved-mesh, score, glyph fallback, selection, countdown and startup regression results. Live Luxodd service access was not exercised locally.

## Empty editor scene recovery

On editor initialization/script reload, BootFromMainMenu opens the saved MainMenu scene when the active scene is an untouched, unsaved default camera/light scene. The existing Play-mode MainMenu override remains enabled by default. Saved scenes, dirty scenes, custom objects and the explicit boot opt-out are preserved. RealBuca → Open Main Menu Scene provides an explicit recovery action, with Unity's normal save prompt if needed. Validation covers empty-scene recovery, unsaved-work preservation, the opt-out, and the menu-to-game flow; see output/toy-box/empty-editor-validation.log.

Results backdrop now captures the actual completed level through the gameplay camera before opening the podium. The three render targets, Gaussian blur material and backdrop mesh/material are saved Editor assets. Runtime renders into those existing targets once, applies three separable blur passes, and displays the frozen result with a mild dim behind the sharp 3D panels. The duplicate results room is removed. Framing leaves more visible board around both panels. The gameplay HUD is excluded during capture, then restored; camera target and automatic/custom projection state are preserved. Countdown, Next, rankings and portrait layout pass the startup regression. See output/toy-box/blurred-results-validation.log.

## Selected celebration: Hole Sparkle (replaces the large award)

PremiumWinCelebration.prefab now contains only one small gold torus and six solid diamond sparkles. LevelManager anchors the effect to the completed hole; it gently rises/expands and shrinks away over 1.1 seconds. The camera push-in is suppressed for this rig, in addition to the existing suppression of camera shake, flash and large win burst. Earned stars remain on the results podium. All seven meshes and the gold material are Editor-saved; runtime only updates their transforms. The existing menu item 6 regenerates this replacement, and full generation installs the same effect.

Validated saved assets, local effect bounds, duration, exit, replay at a different hole, unchanged camera and existing startup/results flow. See output/toy-box/Hole-Sparkle-Win.png and hole-sparkle-validation.log.

Visibility revision: thicker .065-radius gold torus, larger .60-high diamond sparkles, brighter gold emission, higher lift above obstacles, faster entrance and 1.6-second duration with a longer hold. Unity render and regression verified.

## Learning progression, levels 1–21, and score-only results

RealBuca → Toy Box 3D → 8 - Balance Learning Levels 1-21 applies repeatable tuning to both source and saved skinned prefabs. Existing geometry, launch-pad directions/speeds and tutorial triggers are retained. Introductory spinners, sliding walls, moving goals, wind, gravity, mud, ice, boosts and kickers are gentler; later appearances raise the challenge. Layout-specific par targets allow more strokes on mixed-mechanic levels. Gameplay now reads the first 21 saved time budgets (36–50 seconds) instead of ignoring them and forcing 30 seconds. Levels 22–30 retain their assets and 30-second runtime budget. Detailed table: output/toy-box/Difficulty-Levels-01-21.md. This is a first balance pass; child-player playtesting is still needed to establish actual completion rates.

Level results now show a single centered 3D score award and Next over the blurred completed board. The ranking board and automatic ranking fetch were removed from this flow. The existing prefab path is retained to preserve references. Regenerate with RealBuca → Toy Box 3D → 7 - Generate Level Results. Score, stars, five-second countdown and manual Next remain connected. Validation checks all authored budgets at runtime, prefab tuning/idempotency, skin collision preservation, tutorials, selectors and score-only landscape/portrait renders. See output/toy-box/learning-difficulty-validation.log.
