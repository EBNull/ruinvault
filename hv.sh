#!/usr/bin/bash

#exec > hv-stdout.txt
#exec 2> hv-stderr.txt

echo "$@"

export WINEDLLOVERRIDES="winhttp=n,b"

# If first arg is "slot=#", export that to subprocess
case "${1}" in
    slot=*)
      SLOTENV="${1}"
      shift
      eval "export ${SLOTENV}"
    ;;
esac

set -x
exec "$@"
