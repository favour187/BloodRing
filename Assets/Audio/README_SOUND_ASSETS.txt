================================================================================
                    BLOODRING ONLINE ROYALE - AUDIO PIPELINE
================================================================================

To elevate BloodRing from its procedural synthesized audio base to a true 
high-fidelity commercial release, we have established a pre-configured 
Audio Pipeline.

The AudioManager is designed to automatically load high-quality .wav files 
from the `Assets/Audio/` and `Assets/Resources/Audio/` folders. If these files 
are not found during a fresh clone build, the engine flawlessly falls back to 
synthesized procedural waveforms (AudioClip.Create) to guarantee zero errors.

Below are the verified, top-tier CC0 / Royalty-Free production sound libraries 
matching the exact acoustic profile of high-end mobile shooters:

1. THE OPEN FIREARM SOUND LIBRARY (CC0 Public Domain)
   - Description: Professionally recorded high-definition firing, reload, and 
     bolt action sounds for assault rifles, sniper rifles, shotguns, and SMGs.
   - Author: Ben Jaszczak et al.
   - Source: https://opengameart.org/content/weapon-sounds
   - Usage: Place downloaded clips in `Assets/Resources/Audio/Weapons/`.

2. JACK MENHORN FPS PLACEHOLDER SOUNDS (CC-BY 3.0)
   - Description: Excellent punchy UI clicks, armor pickups, footsteps across 
     multiple surfaces, body impacts, and power-up chimes.
   - Source: https://opengameart.org/content/fps-placeholder-sounds
   - Usage: Place clips in `Assets/Resources/Audio/SFX/`.

3. SONNISS GDC GAME AUDIO ARCHIVES (Royalty-Free Commercial)
   - Description: Massive, multi-gigabyte annual sound effect bundles featuring 
     cinematic explosions, vehicle engines, ambient wind, and heavy foley.
   - Source: https://sonniss.com/sound-effects/free-download-game-audio/
   - Usage: Place ambient and vehicle clips in `Assets/Resources/Audio/Environment/`.

4. FREESOUND & INCOMPETECH (CC0 / CC-BY)
   - Description: Industry-standard repositories for ambient wind hums, tension 
     background music, and localized battle foley.
   - Source: https://freesound.org | http://incompetech.com

================================================================================
                   GENERATED VOICEOVER ASSETS (ALREADY INCLUDED)
================================================================================
The following professional AI synthesized voiceover clips have been generated 
and are actively wired into the game loop:
- `VO_Welcome.wav`: "Welcome to BloodRing Royale. Prepare for battle."
- `VO_Victory.wav`: "Victory! Blood Ring champions! Outstanding work, soldier." (Used for Apex Victory)
- `VO_ZoneWarning.wav`: "Warning. The safe zone is collapsing. Move to safety immediately."
- `VO_Countdown.wav`: "Ten, nine, eight, seven, six, five, four, three, two, one, battle initiation."


