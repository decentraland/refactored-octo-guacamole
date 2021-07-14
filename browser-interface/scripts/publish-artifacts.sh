#!/bin/bash

######################################################
# This file deploys the ./dist folder to the S3 bucket
# and creates an invalidation in the cloudfront caché
######################################################

set -u # no unbound variables

# Upload artifacts
npx @dcl/cdn-uploader@next \
  --bucket "$S3_BUCKET" \
  --local-folder "/tmp/workspace/unity-renderer/browser-interface/dist" \
  --bucket-folder "branch/${CIRCLE_BRANCH}"

# Invalidate cache
aws configure set preview.cloudfront true
aws configure set preview.create-invalidation true
aws cloudfront create-invalidation --distribution-id "${CLOUDFRONT_DISTRIBUTION}" --paths "/branch/${CIRCLE_BRANCH}/*"