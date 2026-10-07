# BUCA — Teddy's First Shot

26-second, 1920×1080, 30 fps attract movie made from the project's current 3D assets.

Story: Teddy welcomes the player (0–4s), the board is revealed (4–7s), the gold puck anticipates its shot (7–10s), the camera follows its journey (10–16s), goal and star celebration (16–20s), BUCA / YOUR TURN invitation (20–26s).

Render in the Unity Editor with `BuildToyBoxMainMenu.StartAttractMovie()`. It creates an additive temporary set and restores existing scene lighting after capture. The renderer uses editor updates; wait for `output/attract/done.txt`. Preview stills: `StartAttractMovie(true)`.

Run `make_soundtrack.py` with NumPy installed. The melody and synthesized effects are original to this video. The existing children cheer is reused; its source/license is documented in `Assets/Audio/Celebration/SOURCE.md`.

Encode the numbered JPEG sequence at 30 fps with the WAV soundtrack using H.264, yuv420p, AAC and faststart. Master file: `output/attract/BUCA-Teddys-First-Shot.mp4`.

This is an authored cinematic demonstration, not recorded gameplay or a physics validation. The video does not change the game's existing menu timeout or automatic startup behavior. Cabinet playback integration has not been enabled.
