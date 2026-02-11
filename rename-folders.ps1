Write-Host "Renaming project folders..."

if (Test-Path "CardLister") {
    Write-Host "Renaming CardLister to FlipKit.Desktop..."
    Rename-Item -Path "CardLister" -NewName "FlipKit.Desktop"
    Write-Host "Done"
}

if (Test-Path "CardLister.Core") {
    Write-Host "Renaming CardLister.Core to FlipKit.Core..."
    Rename-Item -Path "CardLister.Core" -NewName "FlipKit.Core"
    Write-Host "Done"
}

if (Test-Path "CardLister.Web") {
    Write-Host "Renaming CardLister.Web to FlipKit.Web..."
    Rename-Item -Path "CardLister.Web" -NewName "FlipKit.Web"
    Write-Host "Done"
}

if (Test-Path "CardLister.Api") {
    Write-Host "Renaming CardLister.Api to FlipKit.Api..."
    Rename-Item -Path "CardLister.Api" -NewName "FlipKit.Api"
    Write-Host "Done"
}

if (Test-Path "CardLister.sln") {
    Write-Host "Renaming CardLister.sln to FlipKit.sln..."
    Rename-Item -Path "CardLister.sln" -NewName "FlipKit.sln"
    Write-Host "Done"
}

Write-Host "Folder rename complete!"
