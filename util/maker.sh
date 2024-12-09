#!/bin/bash

HERE=$(
  cd "$(dirname "$BASH_SOURCE")"
  cd -P "$(dirname "$(readlink "$BASH_SOURCE" || echo .)")"
  pwd
)
cd "${HERE}"

. "${HERE}/prereq.sh"

PLUGIN_OUT_CYG="bin/Debug/net35/ruinvault.dll"

makeclean() {
  rm -rf ../bin
  rm -rf ../obj
  rm -rf ../dist
  rm ../refasm/Assembly-CSharp.dll
  rm ../refasm/Assembly-CSharp-firstpass.dll
  rm ../refasm/TextMeshPro.dll
}

makepreclean() {
  (
    cd /tmp/build-refasmer/Refasmer/src
    dotnet clean
    git am --abort
    git clean -f
  )
}

makerefasm() {
  xtrace_one make_refasmer
  xtrace_one run_refasmer
}

makebuild() {
  (
    cd ../ruinvault
    (
      set -x
      dotnet build
    )
  )
}

makedist() {
  (
    cd ..
    mkdir -p dist/base/BepInEx/plugins
    cp $PLUGIN_OUT_CYG dist/base/BepInEx/plugins
    cp hv.sh dist/base

    mkdir -p dist/full
    unzip -o /tmp/BepInEx.zip -d dist/full
    cp -R dist/base/* dist/full

    mkdir -p dist/zips
    (
      cd dist/base
      zip -r ../zips/base.zip .
    )
    (
      cd dist/full
      zip -r ../zips/full.zip .
    )
  )
}

makeinstall-full() {
  set -x
  cp -R ../dist/full/* "$HOST_GAMEDIR"
}

makeinstall() {
  set -x
  cp -R ../dist/base/* "$HOST_GAMEDIR"
}

makepush() {
  DEST=deck@${STEAMDECK:-ebsteamdeck}
  scp ../dist/zips/full.zip $DEST:/tmp/
  scp ../dist/zips/base.zip $DEST:/tmp/
  set -x
  OUTDIR_E=$(printf '%q' "${DECK_GAMEDIR}")
  UNZIP_E=$(printf 'unzip -o /tmp/base.zip -d "%q"' "$OUTDIR_E")
  BSH_E=$(printf 'bash -c "%s"' "$UNZIP_E")
  ssh $DEST "$BSH_E"

}

makerun() {
  "${HOST_GAMEEXE}" -slot=${slot:-4}
}

makelogs() {
  cat "${HOST_GAMEDIR}\\BepInEx\\LogOutput.log"
}

makegamelogs() {
  cat "${HOST_DATADIR}\\output_log.txt"
}

makepaths() {
  "${HERE}/paths.sh"
}

makemakefile() {
  echo -e "# This file is autogenerated by \`make makefile\`" >../Makefile
  echo -e ".PHONY: default\ndefault: build\n" >>../Makefile
  echo -e "Makefile: util/maker.sh\n\t@util/maker.sh makemakefile\n" >>../Makefile
  declare -A tgts

  for n in clean dist build refasm run logs gamelogs makefile preclean paths install install-full; do
    tgts["$n"]=""
  done

  tgts[build]="refasm"
  tgts[run]="dist install"
  tgts[dist]="build"
  tgts[install]="build dist"
  tgts["install-full"]="build dist"
  tgts[push]="build dist"

  (
    exec >>../Makefile
    for cmd in ${!tgts[@]}; do
      echo ".PHONY: $cmd"
      echo "$cmd: ${tgts[$cmd]}"
      echo -e "\t@./util/maker.sh make$cmd"
      echo
    done
  )
}

maker_main() {
  CMD="$1"
  shift
  if command -v "$CMD" >/dev/null; then
    xtrace_one $CMD "$@"
  else
    echo "Bad command" >&2
    exit 1
  fi
}

if [ ! -d "${SOURCE_MAKER}" ]; then
  maker_main "$@"
fi
