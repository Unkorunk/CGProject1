#!/bin/sh

PLATFORM="netcoreapp3.1"

BIN_DIR="CGProject1/bin"
DEBUG_PATH=$BIN_DIR/Debug/$PLATFORM
RELEASE_PATH=$BIN_DIR/Release/$PLATFORM

mkdir -p $DEBUG_PATH
mkdir -p $RELEASE_PATH

wget "https://ffmpeg.zeranoe.com/builds/win64/shared/ffmpeg-4.3-win64-shared-lgpl.zip"
unzip ffmpeg-4.3-win64-shared-lgpl.zip

cp -r ffmpeg-4.3-win64-shared-lgpl $DEBUG_PATH
cp -r ffmpeg-4.3-win64-shared-lgpl $RELEASE_PATH

rm ffmpeg-4.3-win64-shared-lgpl.zip
rm -R ffmpeg-4.3-win64-shared-lgpl
