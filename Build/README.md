# UltraBeat
🎵 A mod that syncs various events in ULTRAKILL to the beat *without* requiring you to actually do anything to the beat. It handles the timing for you :)

If the mod comes across a song it doesn't have a map for the game will go back to default behaviour

## Features
- Default revolver reloads in sync to the beat (you'll quickly realise why the alt revolver doesn't)
- Freeze-frame unfreezes in sync with the beat
    - Splits freeze frames into long and short
    - Short freeze frame waits for minor beats (Projectile parrying)
    - Long freeze frame waits for major beats (All other freeze frames)

## Installing

Install [r2modman](https://thunderstore.io/c/ultrakill/p/ebkr/r2modman/) and install the plugin through there

## Manual install

Place `UltraBeat` in `ULTRAKILL/BepInEx/plugins`.

## Adding new beat maps
Currently mapped songs can be found here: https://github.com/Recessive/UltraBeat/tree/master/maps. When the mod runs for the first time it will automatically download all maps found in that directory.

If you would like to add your own song mappings, simply add a new `json` file to `ULTRAKILL/BepInEx/config/UltraBeat`. The `json` file should be the **exact** name of the song you are mapping.

### Mapping tutorial
**UltraBeat** expects the name of a song mapping to be exactly the same as the name of the song in game. For example, [Meganeko - The Cyber Grind](https://www.youtube.com/watch?v=e9EqU9y69vU) is split into two songs:

- The Cyber Grind - Final Version 1 (Intro only)
- The Cyber Grind - Final Version 1 (seamless loop without intro)

So to fully map this song, you would have to create two different `json` files for each of these songs.

### Json Format:

Below is an example `json` file:
```json
{
  "offset": 0,
  "bpm": 120,
  "maps": {
    "base": [1,1,1,1,1,1,1,1,1],
    "freeze_short": [1,0,1,0,1,0,1,0,1],
    "freeze_long": [1,0,0,0,1,0,0,0,1],
    "fast": [1,0,1,0,0,0,1,0,1]
  }
}
```

This would definitely be too short for a full length song. To know how many beats you need, multiply the length of the song (in minutes) by the bpm. So a 3 minute song would need: `3*120=360` beats.

If you want higher precision, multiply the bpm by some multiple. To map half notes, the bpm needs to be *at least twice* the base bpm. To map quarter notes, it needs to be *at least four times* the base and so on.

## Mapping software
I built some mapping software a few years ago. The format differs, but it's trivial to convert to the `json` format found here. Download the executable from releases: https://github.com/Recessive/BeatMapper
