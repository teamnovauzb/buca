# Win cheer source

HappyYeah.wav: Children saying Yay - Praise and Worship Jesus, by JesusChristIsGod (Pixabay).
Source: https://pixabay.com/sound-effects/people-children-saying-yay-praise-and-worship-jesus-299607/
Audio: https://cdn.pixabay.com/audio/2025/02/10/audio_0aa9ae7d75.mp3
License: Pixabay Content License, https://pixabay.com/service/license-summary/
Downloaded 2026-09-27. Source labels the audio AI generated.
Converted MP3 to PCM WAV for integration into the game. No pitch change.
Attribution is optional; standalone redistribution is not permitted.
Replaces the previous BudgetPixel cheer.

# Twin fireworks sound

ResultsFireworksFanfare.wav is an original offline-synthesized stereo effect for the score screen: two fireworks at 0.32 and 0.64 seconds, followed by a bell figure and a resolving chime at 1.8 seconds. No external recordings or spoken voice. Rebuild with `python3 Tools/Audio/bake_results_fanfare.py`; playback uses the saved WAV and the game's SFX volume.

HoleInOneTwinFireworks.wav is an original offline-synthesized sound effect for BUCA: two launch swishes, rounded boom impacts at 0.55 and 0.73 seconds, and short crackling tails. No external recordings or voice samples are used. Rebuild from the project root with `python3 Tools/Audio/bake_hole_in_one_fireworks.py`. The WAV is imported and saved ahead of play; audio is not synthesized at runtime.
