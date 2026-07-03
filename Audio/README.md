# BloodRing Production Audio Architecture

The `/Audio` root directory contains master audio configurations, channel routings, and asset maps linking physical sound libraries to Unity's runtime `AudioManager`.

## Audio Categories:
- **Music**: Lobby themes, action combat beats (`Assets/Resources/Audio/Music/`)
- **SFX**: Weapons, explosions, footsteps, UI clicks, power-ups (`Assets/Audio/SFX/` & `Assets/Resources/Audio/`)
- **VO**: Professional voiceovers for countdown, zone warnings, and victory announcements (`Assets/Audio/VO/`)
- **Ambience**: Wind gusts, island weather, rain, thunder (`Assets/Audio/RealProduction/`)

## Runtime Engine Features:
- Automatic fallback to synthesized waveforms if physical WAV files are missing or streaming.
- 3D spatialized audio attenuation with logarithmic Doppler pitch shifting.
