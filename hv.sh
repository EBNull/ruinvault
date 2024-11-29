#!/usr/bin/bash

#exec > hv-stdout.txt
#exec 2> hv-stderr.txt

echo "$@"

export WINEDLLOVERRIDES="winhttp=n,b"
set -x
exec "$@"
