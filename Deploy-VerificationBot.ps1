# AWS ECR Docker Build and Push Script
param(
    [string]$ImageName = "unseckeys/verification",
    [string]$Region = "us-east-1",
    [string]$Profile = "SysOpsCLI",
    [string]$ECRRegistry = "216989099580.dkr.ecr.us-east-1.amazonaws.com",
    [string]$Tag = "latest",
    [string]$ClusterName = "TestsCluster",
    [string]$ServiceName = "Test-Verification-service-oxzrshhk"
)

# Set error handling
$ErrorActionPreference = "Stop"

clear-host;

Write-Host "=== AWS ECR Docker Build and Push Script ===" -ForegroundColor Green
Write-Host "Image: $ImageName" -ForegroundColor Yellow
Write-Host "Registry: $ECRRegistry" -ForegroundColor Yellow
Write-Host "Profile: $Profile" -ForegroundColor Yellow
Write-Host "Cluster: $ClusterName" -ForegroundColor Yellow
Write-Host "Service: $ServiceName" -ForegroundColor Yellow
Write-Host ""

try {
    # Step 1: AWS ECR Login
    Write-Host "Step 1: Logging into AWS ECR..." -ForegroundColor Cyan
    $loginCommand = "aws ecr get-login-password --region $Region --profile $Profile | docker login --username AWS --password-stdin $ECRRegistry"
    Invoke-Expression $loginCommand
    if ($LASTEXITCODE -ne 0) { throw "ECR login failed" }
    Write-Host "✓ ECR login successful" -ForegroundColor Green
    Write-Host ""

    # Step 2: Docker Build
    Write-Host "Step 2: Building Docker image..." -ForegroundColor Cyan
    docker build -t "$ImageName`:$Tag" -f UnsecuredAPIKeys.Bots.Verifier/Dockerfile .
    if ($LASTEXITCODE -ne 0) { throw "Docker build failed" }
    Write-Host "✓ Docker build successful" -ForegroundColor Green
    Write-Host ""

    # Step 3: Tag for ECR
    Write-Host "Step 3: Tagging image for ECR..." -ForegroundColor Cyan
    docker tag "$ImageName`:$Tag" "$ECRRegistry/$ImageName`:$Tag"
    if ($LASTEXITCODE -ne 0) { throw "Docker tag failed" }
    Write-Host "✓ Image tagged successfully" -ForegroundColor Green
    Write-Host ""

    # Step 4: Push to ECR
    Write-Host "Step 4: Pushing image to ECR..." -ForegroundColor Cyan
    docker push "$ECRRegistry/$ImageName`:$Tag"
    if ($LASTEXITCODE -ne 0) { throw "Docker push failed" }
    Write-Host "✓ Image pushed successfully" -ForegroundColor Green
    Write-Host ""

	# Step 5: Force ECS Service Deployment  
	Write-Host "Step 5: Forcing ECS service deployment..." -ForegroundColor Cyan  
	$ecsUpdateResult = aws ecs update-service --cluster $ClusterName --service $ServiceName --force-new-deployment --region $Region --profile $Profile | Out-Null  
	if ($LASTEXITCODE -ne 0) { throw "ECS force deployment failed" }  
	Write-Host "✓ ECS service deployment initiated" -ForegroundColor Green  
	Write-Host ""

    # Success message
    Write-Host "=== SUCCESS ===" -ForegroundColor Green
    Write-Host "Image successfully built and pushed to:" -ForegroundColor Green
    Write-Host "$ECRRegistry/$ImageName`:$Tag" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "ECS service deployment initiated for:" -ForegroundColor Green
    Write-Host "Cluster: $ClusterName" -ForegroundColor Yellow
    Write-Host "Service: $ServiceName" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "You can monitor the deployment in the AWS Console or with:" -ForegroundColor Cyan
    Write-Host "aws ecs describe-services --cluster $ClusterName --services $ServiceName --region $Region --profile $Profile" -ForegroundColor Gray

} catch {
    Write-Host "=== ERROR ===" -ForegroundColor Red
    Write-Host "Script failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}