#!/usr/bin/env bash
# Stop at the first failure, so a broken build can never push (and deploy) the previous image.
set -euo pipefail
set -x

REGION=us-east-1
REGISTRY=222779217717.dkr.ecr.us-east-1.amazonaws.com
IMAGE=websiteinstance
CLUSTER=fydardev-cluster
SERVICE=WebsiteInstance

# Login (password over stdin, so the trace above never prints it)

aws ecr get-login-password --region "$REGION" | docker login --username AWS --password-stdin "$REGISTRY"

# Build

docker buildx build --platform linux/arm64 --load -t "$IMAGE" -f Fydar.Dev.WebApp/Dockerfile .

# Tag

docker tag "$IMAGE:latest" "$REGISTRY/$IMAGE:latest"

# Push

docker push "$REGISTRY/$IMAGE:latest"

# Deploy

aws ecs update-service \
   --region "$REGION" \
   --cluster "$CLUSTER" \
   --service "$SERVICE" \
   --force-new-deployment \
   --deployment-configuration "maximumPercent=100,minimumHealthyPercent=0,deploymentCircuitBreaker={enable=true,rollback=true}" \
   --query 'service.deployments[0].{id:id,status:status,rollout:rolloutState}' \
   --output table

aws ecs wait services-stable --region "$REGION" --cluster "$CLUSTER" --services "$SERVICE"
