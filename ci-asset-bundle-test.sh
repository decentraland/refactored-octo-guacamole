#!/usr/bin/env bash
# THIS FILE IS INTENDED TO BE USED IN THE CI ONLY

set -u  # fail if any env var is not set

source ci-setup.sh

SCENE_ID="$(curl -s "https://peer.decentraland.org/content/entities/scene?pointer=0,0" | jq -r '.[0].id')"
echo "Main plaza deployment: $SCENE_ID"

# Parameters are passed as ENV vars
export SCENE_ID
export OUTPUT_DIR=ab-output
export LOCAL_LOG_FILE=ab-log.txt
export CONTENT_URL=https://peer.decentraland.org/lambdas/contentv2/contents/
# call the conversor
bash ./convert-asset-bundles.sh

# print results to terminal
find "$OUTPUT_DIR"
cat "$LOCAL_LOG_FILE"

exit $UNITY_EXIT_CODE
