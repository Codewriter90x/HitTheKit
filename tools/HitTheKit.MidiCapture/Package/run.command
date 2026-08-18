#!/bin/sh
set -eu
SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$SCRIPT_DIR"
./hitthekit-midi-capture doctor
./hitthekit-midi-capture list
printf '\nNext: ./hitthekit-midi-capture guided-capture --device 0\n'
