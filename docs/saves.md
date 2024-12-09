# Save Files

## Location

### Directory

General

```
"%APPDATA%\LocalLow\Inkle Ltd\Heaven's Vault"
```

Steam Deck

```
"/home/deck/.steam/steam/steamapps/compatdata/774201/pfx/drive_c/users/steamuser/AppData/LocalLow/Inkle Ltd/Heaven's Vault"
```

### Files

`heavensVaultSave.json` - Your save file. Contrary to the file extension, this is not normal `json`. See [Formats](#formats).

`heavensVaultSave_Backup.json` - Your save file, but from the beginning of the current story scene (hence, older).

`DevSaves` - A folder containing DevSaves of the form `HVSave_YYYY_MM_DD_hh_mm_ss.txt`. See below.

### Slots

This mod adds multiple save slot support. Filenames look like `heavensVaultSave_Slot_2.json`.

### Editing Saves

This mod adds "raw" saves next to the existing save files. These look like `heavensVaultSave_Slot_2_raw.json`. The game
can also now load raw format files in place of crypt-format files.

## Contents

Within each save there are five JSON dictionaries: 

- `saveVersion`
- `metaInformationJSON`
- `stateInformationJSON`
- `gameDataJSON`
- `inkJson` - Story script data with the choices you've made.

## Disk Formats

### Normal Format

The normal save file, while ending in `.json`, is not plain JSON. It begins with three magic bytes (`HV` + `0x01`)
with all following bytes XOR'd with `0x61` (the letter `a`). Next is a length prefix encoded as a one to
five byte [7-bit encoded integer](https://stackoverflow.com/a/1550568/84041) as written by [`System.IO.BinaryWriter.Write`](https://learn.microsoft.com/en-us/dotnet/api/system.io.binarywriter.write). All following bytes are the raw savedata (UTF-8 JSON).
This is annoying to edit and there's only one save file maintained.

### DevSave Format

A DevSave is an "unwrapped" normal save file without the prefixes and XOR encoding.

When running `ruinvault` DevSaves are saved automatically whenever the game saves a normal save (which is pretty frequent).
These are unencrypted and named by the current time / date, thus enabling save rollback and editing.

You can load a DevSave by pressing `shift+L` or `LR Bumpers + LR Triggers + DPad-Down` to bring up an UI
for selecting one and `Enter` or `Gamepad X` to load.

Sometimes loading a DevSave fails; this can softlock you.

DevSaves are stored in a directory named `DevSaves` next to your main encrypted save.

> [!IMPORTANT]
> There is no limit on the number of saves created automatically.
>
> This number of saves can grow very large.

- `Shift + L` or `LR Bumpers + LR Triggers + DPad-Down` to toggle a menu for loading a DevSave.
- `Left`, `Right`, `Left Bumper`, `Right Bumper` to select a DevSave.
- `Enter` or `Gamepad X` to load a DevSave. This may result in a softlock.