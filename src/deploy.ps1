$ErrorActionPreference = "Stop"

function Assert-LastExitCode([string]$Step) { if ($LASTEXITCODE -ne 0) { throw "$Step failed with exit code $LASTEXITCODE." } }

function Get-Emoji([int]$hex) { return [char]::ConvertFromUtf32($hex) }

# Configuration
$AwsRegion = "us-east-1"
$RegistryUrl = "222779217717.dkr.ecr.us-east-1.amazonaws.com"
$ImageName = "websiteinstance"
$ClusterName = "fydardev-cluster"
$ServiceName = "WebsiteInstance"
$FullRegistryPath = "${RegistryUrl}/${ImageName}"

# Metadata
$GitHash = git rev-parse --short HEAD
$IsDirty = git status --porcelain
$DirtySuffix = if ($IsDirty) { "-dirty" } else { "" }
$VersionTag = "$GitHash$DirtySuffix"

# Step 1: Initialize & Print Configuration
Write-Host "`n[1/6] $(Get-Emoji 0x2619) Initializing configuration..." -ForegroundColor Cyan

# This creates a vertical list (Multiple Rows) instead of one wide row
$ConfigData = @(
    [PSCustomObject]@{ Setting = "AWS Region";   Value = $AwsRegion }
    [PSCustomObject]@{ Setting = "Registry URL"; Value = $RegistryUrl }
    [PSCustomObject]@{ Setting = "Image Name";   Value = $ImageName }
    [PSCustomObject]@{ Setting = "Git Hash";     Value = $GitHash }
    [PSCustomObject]@{ Setting = "Workspace";    Value = if ($IsDirty) { "Dirty (Uncommitted Changes)" } else { "Clean" } }
    [PSCustomObject]@{ Setting = "Version Tag";  Value = $VersionTag }
)

$ConfigData | Format-Table -AutoSize | Out-String | Write-Host -ForegroundColor Gray

# Step 2: Build
Write-Host "`n[2/6] $(Get-Emoji 0x1F6E0) Building Docker image (linux/arm64)..." -ForegroundColor Cyan
docker buildx build --platform linux/arm64 `
    -t "${ImageName}:latest" `
    -t "${ImageName}:${VersionTag}" `
    --load `
    -f Fydar.Dev.WebApp/Dockerfile .
Assert-LastExitCode "Docker build"

# Step 3: Tag
Write-Host "`n[3/6] $(Get-Emoji 0x1F3F7) Tagging images for ECR..." -ForegroundColor Cyan
docker tag "${ImageName}:latest" "${FullRegistryPath}:latest"
docker tag "${ImageName}:${VersionTag}" "${FullRegistryPath}:${VersionTag}"

# Step 4: Auth
Write-Host "`n[4/6] $(Get-Emoji 0x1F512) Logging into AWS ECR..." -ForegroundColor Cyan
aws ecr get-login-password --region $AwsRegion | docker login --username AWS --password-stdin $RegistryUrl
Assert-LastExitCode "ECR login"

# Step 5: Push
Write-Host "`n[5/6] $(Get-Emoji 0x1F680) Pushing images to AWS ECR..." -ForegroundColor Cyan
docker push "${FullRegistryPath}:latest"
Assert-LastExitCode "Docker push (latest)"
docker push "${FullRegistryPath}:${VersionTag}"
Assert-LastExitCode "Docker push ($VersionTag)"

# Step 6: Deploy
# ECS pins ":latest" to a digest when a deployment starts, and restarted tasks reuse it, so only a
# new deployment picks up the image just pushed. One host-networked task on one small instance
# can't run two copies, so the old task stops first (see the CloudFormation template).
Write-Host "`n[6/6] $(Get-Emoji 0x1F504) Starting a new ECS deployment..." -ForegroundColor Cyan
aws ecs update-service `
    --region $AwsRegion `
    --cluster $ClusterName `
    --service $ServiceName `
    --force-new-deployment `
    --deployment-configuration "maximumPercent=100,minimumHealthyPercent=0,deploymentCircuitBreaker={enable=true,rollback=true}" `
    --query "service.deployments[0].{id:id,status:status,rollout:rolloutState}" `
    --output table
Assert-LastExitCode "ECS update-service"

Write-Host "Waiting for the service to become stable..." -ForegroundColor Gray
aws ecs wait services-stable --region $AwsRegion --cluster $ClusterName --services $ServiceName
Assert-LastExitCode "Waiting for ECS"

Write-Host "`n$(Get-Emoji 0x2705) Deployment completed successfully!" -ForegroundColor Green
