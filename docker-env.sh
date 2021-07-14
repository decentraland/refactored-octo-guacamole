#!/bin/bash

set -u # fail if env vars are unbound

docker run -it \
  -e "BUILD_TARGET=WebGL" \
  -e "BUILD_PATH=/project/unity-renderer/Builds/unity" \
  -e "BUILD_NAME=unity" \
  -e "UNITY_LICENSE_CONTENT_2020_3_BASE64=${UNITY_LICENSE_CONTENT_2020_3_BASE64}" \
  -w "/project" \
  -v "$(pwd):/project" \
  decentraland/renderer-build:2020.3.0 \
  bash