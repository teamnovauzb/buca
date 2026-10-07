# Playful Race — standalone movie preview

Selected concept: gold, coral, and blue pucks race across a wooden BUCA board. The coral and blue pucks collide; gold escapes, banks off the side, circles the goal, and wins.

Duration: 35 seconds. Output: 1920×1080, 30 fps, H.264/AAC MP4.

Timeline:
- 0–2.5: three-puck lineup and small anticipation hops.
- 2.5–7.2: the start and approach to the first bumper.
- 7.2–11: comic collision; gold slips past.
- 11–18: competitors recover while gold makes a bank shot.
- 18–23: the pack approaches the goal.
- 23–26: gold circles the rim and drops in.
- 26–31.5: star celebration and synchronized group cheer.
- 31.5–35: BUCA invitation card.

This is authored cinematic animation, not footage of a multiplayer game. The temporary set uses project assets plus cinematic-only bumper placement. No menu, gameplay scene, or automatic video playback has been changed.

The editor-only renderer is `Assets/Editor/ToyBox/BucaAttractMovie.cs`; use `BuildToyBoxMainMenu.StartAttractMovie(true)` for keyframes, or `StartAttractMovie()` for the full sequence. Capture restores the original scene and lighting when finished or interrupted by Play Mode. `make_soundtrack_race.py` generates the original music and effects and mixes the project's existing licensed cheer (source in `Assets/Audio/Celebration/SOURCE.md`). `encode_race.py` combines the frames, soundtrack and end card.

Final preview: `output/attract-race/BUCA-Playful-Race.mp4`.
