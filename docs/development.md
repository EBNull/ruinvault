# Development

## Building & Workflow

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

## Using a debugger

Instructions adapted from [dnSpy's wiki](https://github.com/dnSpy/dnSpy/wiki/Debugging-Unity-Games#turning-a-release-build-into-a-debug-build)

1. Install Unity Editor [2017.4.30](https://unity.com/releases/editor/whats-new/2017.4.30#installs). This is the version on ``Heaven`'s Vault.exe``.
2. Navigate to ``C:\Program Files\Unity\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win32_development_mono``.
3. Copy `UnityPlayer.dll` and `WindowsPlayer.exe` into the game directory.
4. Delete ``Heaven's Vault.exe``, and rename `WindowsPlayer.exe` to ``Heaven's Vault.exe``
5. Copy all files in the `Managed` directory to the game's ``Heaven's Vault_Data`` directory, overwriting a few hundred files.
6. Use [dnSpyEx](https://github.com/dnSpyEx/dnSpy)' menu `Debug -> Start Debugging` to open the ``Heaven's Vault.exe`` file.