# ruinvault

ruinvault is a mod for [Heaven's Vault](https://store.steampowered.com/app/774201/Heavens_Vault/).

Currently most patches are around speeding up the gameplay to make additional
playthroughs less monotonous.

It's called `ruinvault` because you find ruins in-game, but also because if you use this before beating the game you could potentially ruin your experience.

Behind the scenes `ruinvault` uses the [BepInEx](https://github.com/BepInEx/BepInEx) mod framework.

## Installation

### Common

Extract the distribution zip to the game's directory. No files will be overwritten.

```
"C:\Program Files (x86)\Steam\steamapps\common\Heaven's Vault"
```

```
"/home/deck/.steam/steam/steamapps/common/Heaven's Vault"
```

### Steam Deck

> [!IMPORTANT]
> Without this step Heaven's Vault mods will not run on the Steam Deck.

- Edit the game's launch options to read
  ```shell
  ./hv.sh %command%
  ```

## Usage

Run the game normally.

> [!NOTE]
> You must run the game from Steam if you want achievements to register.

## Features:

### Visuals

- Removed ghost trails of player and other characters

### Gameplay

#### Fast Screen Transitions

- Load screens that fade to/from black screens are much faster.
- Quote screens (start of game; change location text) now display text immediately and let you continue immediately.
- Title and splash screens are skipped 
- Story scenes can be skipped by `left clicking` (only). This is untested.

#### Fast Movement & Animation Hotkey

> [!IMPORTANT]
> Hold `shift` to vastly increase walk speed and animations.

> [!TIP]
> Bind `shift` to a button with Steam Input. I `L4` and making it a `toggle` button.

#### River Teleportation & Tweaks

- Use gamepad `X` or `right click` while using the map and sailing to
  instantly teleport to a specific river.
- The prompt to give sailing control to your robot appears nearly instantly.
- You can now exit the sailing map without setting a destination.
- The time before a reset prompt coming up when missing a return has decreased.
- The minimum speed on rivers has been increased, but this is not well tested.
- Ruins appear a bit more frequently.

#### Multiple Save Files

See `DevSaves` below.

#### Cheats

##### Moon

- `H` to heal character

##### Translations

Translation cheats can only be used during the spacing phase of the game.

- `C` to cheat fully (and solve the translation).
- `I` to insert each word into a single correct slot position. This is equivilent to `C` unless there are duplicates.

###### Debug Cheats

Generally not useful. Using these during the disambiguation phase can result in missing glyphs or a softlocked game.

- `O` to randomly cross out a word during the sentence spacing phase, possibly a correct one. 
- `U` to remove all words from slots, even those you already know for sure. This can easily softlock you.

#### Photo Mode / Free Camera

The classes within the `DevCinematographer` namespace are enabled, providing
access to an Free camera and an Orbit Camera.

Controls:

- `F4` - Toggle photo mode
- `F5` - Toggle a 3x3 visual grid 
- `Space | Gamepad Back | Touchpad` - Reset camera position
- `,` or `.` - Switch between Free and Orbit cameras

Free Camera Only:

- `Z | DPad-Left` and `C | DPad-Right` - Roll the camera
- `X` - Reset Roll camera
- `[ | DPad-Up` and `] | DPad-Down` - Change FOV (default `60`; free camera only)
- `WSAD` - Move Camera Forward/Back/Left/Right (but forward / back are reversed)

Orbit Camera Only:

- `+` and `-` - Zoom In / Out (orbit camera only)
- `WSAD` Move Character AND Camera, but unsynchonized

#### Disabled Options

The code must be recompiled to enable these; no friendlier config currently exists.

- Autoreset position on river when making a wrong turn off the path. Can result in infinite loops if current directs you to the wrong path.
- Skip startup quote screen.
- Skip all position-change summary screens.
- Autocontinue at all "continue" prompts.
- Show all rivers on map screen.

#### Saves & DevSaves

The normal save file, while ending in `.json`, is not plain JSON. It begins with three magic bytes (`HV` + `0x01`)
with all following bytes XOR'd with `0x61` (the letter `a`). Next is a length prefix encoded as a one to
five byte [7-bit encoded integer](https://stackoverflow.com/a/1550568/84041) as written by [`System.IO.BinaryWriter.Write`](https://learn.microsoft.com/en-us/dotnet/api/system.io.binarywriter.write). All following bytes are the raw savedata (UTF-8 JSON).
This is annoying to edit and there's only one save file maintained.

A DevSave is an "unwrapped" normal save file without the prefixes and XOR encoding.

Within each save there are five JSON dictionaries: `saveVersion`, `metaInformationJSON`, `stateInformationJSON`, `gameDataJSON`, and `inkJson`. Most
of these are self explainatory, except for `ink` - that's the story script data with the choices you've made.

DevSaves are saved automatically whenever the game saves a normal save (which is pretty frequent).
These are unencrypted and named by the current time / date, thus enabling save rollback and editing.

You can load a DevSave by pressing `shift+L` or `LR Bumpers + LR Triggers + DPad-Down` to bring up an UI
for selecting one and `Enter` or `Gamepad X` to load.

Sometimes loading a DevSave fails; this can softlock you.

DevSaves are stored in a directory named `DevSaves` next to your main encrypted save.

There is no limit on the number of saves; this list can grow very large.

## Development Notes

### Environment

The build scripts expect to be run from within [cygwin](https://www.cygwin.com/).

> [!TIP]
> Install [apt-cyg](https://github.com/kou1okada/apt-cyg) and use `apt-cyg install pkg...`

### Dependencies

Building this mod requires [reference assemblies](https://learn.microsoft.com/en-us/dotnet/standard/assembly/reference-assemblies) for
Heaven's Vault that have been patched to make private fields and members public. That requries a generator for these
assemblies, for which [Refasmer](https://github.com/JetBrains/Refasmer) exists, and patches to Refasmer, [which exist in this repository](patches/refasmer).
Since that's annoying to deal with this repository includes scripts to download, patch, and build Refasmer as part of the build process.

You'll also need Heaven's Vault installed (to use as a source for the reference assemblies), which prevents this project
from easily being used in CI.

### Distribution

The build scripts included create three "distributions" in the `dist` directory:

- `dist/base` - The raw DLL for the mod and supporting scripts.
- `dist/full` - The same as above, but with [BepInEx](https://github.com/BepInEx/BepInEx) included.
- `dist/zips` - ZIP files containing the above.

The zip files you download as part of a release come from `dist/zips`.

### Workflow

Build & Install Everything on PC for the first time:

```shell
$ git clone ...
$ cd ruinvault
$ make dist
$ make install-full
```

PC Development Cycle:

```shell
$ make install
$ make run
```

Pushing only the DLL to Steam Deck:

```shell
$ ssh-add
$ STEAMDECK=mydeckhostname make push
```