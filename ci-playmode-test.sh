#!/usr/bash

source ci-setup.sh

xvfb-run --auto-servernum --server-args='-screen 0 640x480x24' $UNITY_PATH/Editor/Unity \
        -projectPath $(pwd) \
        -batchmode \
        -logFile "$PROJECT_PATH/playmode-logs.txt" \
        -runTests \
        -testPlatform PlayMode \
        -testResults "$PROJECT_PATH/playmode-results.xml" \

# Catch exit code
UNITY_EXIT_CODE=$?

mkdir -p test-results/playmode
cp playmode-results.xml test-results/playmode/results.xml || true

cat "$PROJECT_PATH/playmode-results.xml"

# Display results
if [ $UNITY_EXIT_CODE -eq 0 ]; then
  echo "Run succeeded, no failures occurred";
elif [ $UNITY_EXIT_CODE -eq 2 ]; then
  echo "Run succeeded, some tests failed";
elif [ $UNITY_EXIT_CODE -eq 3 ]; then
  echo "Run failure (other failure)";
else
  echo "Unexpected exit code $UNITY_EXIT_CODE";
fi

exit $UNITY_EXIT_CODE
