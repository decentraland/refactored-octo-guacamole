#!/usr/bin/env bash

export PROJECT_PATH
PROJECT_PATH="$(pwd)/unity-renderer"

set -e
set -x
mkdir -p /root/.cache/unity3d
mkdir -p /root/.local/share/unity3d/Unity/
set +x

ls -lah /root/.cache/unity3d

echo "UNITY PATH is $UNITY_PATH"

if [ -z "$UNITY_LICENSE_CONTENT_2020_3_BASE64" ]; then
  echo 'UNITY_LICENSE_CONTENT_2020_3_BASE64 not present. License won''t be configured'
else
  LICENSE=$(echo "${UNITY_LICENSE_CONTENT_2020_3_BASE64}" | base64 -di | tr -d '\r')

  echo "Writing LICENSE to license file /root/.local/share/unity3d/Unity/Unity_lic.ulf"
  echo "$LICENSE" > /root/.local/share/unity3d/Unity/Unity_lic.ulf
fi

set -x
