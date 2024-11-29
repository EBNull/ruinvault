#!/bin/bash

# Use unix path delimiters everywhere and use cygpath if windows paths
# are needed. This makes escaping and paths consistent.

APPID=774201

datadir_suffix() {
  echo "AppData/LocalLow/Inkle Ltd/Heaven's Vault"
}

gamedir_suffix() {
  echo "common/Heaven's Vault"
}

gameexe() {
  echo "Heaven's Vault.exe"
}

main() {
  echo APPID=$APPID
  echo GAMEDIR_SUFFIX=$(gamedir_suffix)
  echo GAMEEXE=$(gameexe)
}

if [ ! -n "${SOURCE_GAMEDATA:-}" ]; then
  main "$@"
fi
unset -f main
