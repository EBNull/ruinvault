#!/bin/bash

function xtrace_one() {
  (
    set -x
    # noop
    : "$@"
  ) 2>&1 | sed 's/^\+ :/+/' 1>&2
  "$@"
}

import_paths() {
  eval "$(SOURCE_PATHS=1 bash paths.sh)"
}
import_paths

clone_bepin() {
  BP=/tmp/BepInEx.zip
  if [ ! -s $BP ]; then
    curl -L -o /tmp/BepInEx.zip https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.2/BepInEx_win_x86_5.4.23.2.zip
  fi
}

clone_and_patch_refasm() {
  ABS_PATCH_DIR=$(realpath ../patches/refasmer)
  (
    mkdir -p /tmp/build-refasmer
    cd /tmp/build-refasmer
    if [ ! -e Refasmer ]; then
      rm -rf Refasmer
      git clone https://github.com/JetBrains/Refasmer.git
    fi
    cd Refasmer
    if [ "$(git rev-parse HEAD)" = "264997930c7d5130a9d430190f9d15ef0e7ea931" -a ! -n "${NOPATCH:-}" ]; then
      return
    fi
    # https://github.com/JetBrains/Refasmer/tree/v2.0.0
    # https://github.com/JetBrains/Refasmer/tree/dea5ef565854209bb2dfb8c911e830f5abbf9422
    if [ ! "$(git rev-parse HEAD)" = "dea5ef565854209bb2dfb8c911e830f5abbf9422" ]; then
      git reset --hard dea5ef565854209bb2dfb8c911e830f5abbf9422
    fi
    if [ ! -n "${NOPATCH:-}" ]; then
      git am ${ABS_PATCH_DIR}/*.patch
    fi
  )
}

make_refasmer() {
  clone_and_patch_refasm
  (
    cd /tmp/build-refasmer
    cd Refasmer/src
    if [ ! -e RefasmerExe/bin/Debug/net7.0/RefasmerExe.exe -o -n "${REBUILD:-}" ]; then
      dotnet clean
      dotnet build
    fi
  )
}

run_refasmer() {
  local SRC_DIR="${HOST_GAMEDIR}/Heaven's Vault_Data/Managed"
  local SRC_BASE=("Assembly-CSharp.dll" "Assembly-CSharp-firstpass.dll")

  local DEST_DIR="$(realpath ../refasm)"
  local DEST_DIR_WIN="$(cygpath -w "${DEST_DIR}")"
  local SRCS_ABS_WIN=()
  local DESTS_ABS=()
  local sb as ad d
  for sb in "${SRC_BASE[@]}"; do
    as="${SRC_DIR}/${sb}"
    ad="${DEST_DIR}/${sb}"
    SRCS_ABS_WIN+=("$(cygpath -w "$as")")
    DESTS_ABS+=("$ad")
    test -e "$ad" && rm -f "$ad"
  done
  (
    cd /tmp/build-refasmer
    cd Refasmer/src/RefasmerExe/bin/Debug/net7.0
    (
      set -x
      ./RefasmerExe.exe --all --omit-non-api-members=false -O="${DEST_DIR_WIN}" "${SRCS_ABS_WIN[@]}"
    )
  )
  MISSING=()
  for d in "${DESTS_ABS[@]}"; do
    if [ ! -e "$d" ]; then
      MISSING+=($(basename "$d"))
    fi
  done
  if [ -n "${MISSING[*]}" ]; then
    echo "Could not generate refasms: ${MISSING[@]}" >&2
    exit 1
  fi

}

full_gen_refasmer() {
  make_refasmer
  run_refasmer
}

all_prereq() {
  clone_bepin
  full_gen_refasmer
}

prereq_main() {
  CMD="$1"
  shift
  if command -v "$CMD" >/dev/null; then
    $CMD "$@"
  fi
}

if [ ! -d "${SOURCE_UTILS}" ]; then
  prereq_main "$@"
fi
