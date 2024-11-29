#!/bin/bash

HERE=$(
  cd "$(dirname "$BASH_SOURCE")"
  cd -P "$(dirname "$(readlink "$BASH_SOURCE" || echo .)")"
  pwd
)

# Use unix path delimiters everywhere and use cygpath if windows paths
# are needed. This makes escaping and paths consistent.

pathtowin() {
  echo cygpath -w "$@"
}

panic() {
  echo "$@" >&2
  exit 1
}

# Common to all supported systems

import_gamedata() {
  SOURCE_GAMEDATA=1
  . "${HERE}/gamedata.sh"

  # imports:
  #
  # APPID
  # datadir_suffix
  # gamedir_suffix
  # gameexe
}
import_gamedata

# Specific to each system

steamdir_for() {
  case "$1" in
  win) echo 'C:/Program Files (x86)/Steam/steamapps' ;;
  deck) echo '/home/deck/.steam/steam/steamapps' ;;
  *) panic "need system type" ;;
  esac
}

gamedir_abs_for() {
  echo "$(steamdir_for "$1")/$(gamedir_suffix)"
}

gameexe_abs_for() {
  echo "$(gamedir_abs_for "$1")/$(gameexe)"
}

pluginsdir_abs_for() {
  echo "$(gamedir_abs_for "$1")/BepInEx/plugins"
}

datadir_abs_for() {
  case "$1" in
  win) echo "C:/Users/${2}/$(datadir_suffix)" ;;
  deck) echo "$(steamdir_for "$1")/compatdata/${APPID}/pfx/drive_c/users/steamuser/$(datadir_suffix)" ;;
  *) panic "need system type" ;;
  esac
}

has() {
  command -v "$1" >/dev/null
}

host_sys() {
  UNAME=$(has uname && uname -s)
  case "$UNAME" in
  CYGWIN*)
    echo win
    return
    ;;
  MSYS*)
    echo win
    return
    ;;
  MINGW*)
    echo win
    return
    ;;
  esac

  DID=$(has lsb_release && lsb_release -i -s)
  case "$DID" in
  SteamOS) echo deck ;;
  *) ;;
  esac
}

exports_for() {
  local SYS="$1"
  local PREFIX="$2"
  local USR="$3"
  printf "${PREFIX}_GAMEDIR=%q\\n" "$(gamedir_abs_for $SYS)"
  printf "${PREFIX}_GAMEEXE=%q\\n" "$(gameexe_abs_for $SYS)"
  printf "${PREFIX}_PLUGINSDIR=%q\\n" "$(pluginsdir_abs_for $SYS)"
  printf "${PREFIX}_DATADIR=%q\n" "$(datadir_abs_for $SYS $USR)"
}

check_exists() {
  eval "$1"
  VARS=$(cut -f1 -d = <(echo "$1"))
  local v
  for v in $VARS; do
    printf '%s=%q # %s\n' "${v}" "${!v}" "$(if [ -e "${!v}" ]; then echo "exists"; else echo "missing"; fi)"
  done
}

do_exports() {
  exports_for win WIN "${USER}"
  exports_for deck DECK deck
  exports_for $(host_sys) HOST "${USER:-unknownuser}"
}

paths_main() {
  echo "Windows paths:"
  echo
  exports_for win WIN "${USER}"
  echo
  echo "Deck paths:"
  echo
  exports_for deck DECK deck
  echo
  echo "Current System: $(host_sys)"
  echo
  check_exists "$(exports_for $(host_sys) HOST "${USER:-unknownuser}")"
}

if [ ! -n "${SOURCE_PATHS:-}" ]; then
  paths_main "$@"
else
  do_exports
  unset -f main
fi
