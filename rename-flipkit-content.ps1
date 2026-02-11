# Rename CardLister to FlipKit in FlipKit.* folders (for files copied after initial rename)
$files = Get-ChildItem -Path FlipKit.Desktop, FlipKit.Core, FlipKit.Web, FlipKit.Api -Include *.cs,*.csproj,*.cshtml,*.json,*.axaml,*.xaml -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notlike '*\obj\*' -and $_.FullName -notlike '*\bin\*' }

$count = 0
foreach ($file in $files) {
    try {
        $content = Get-Content $file.FullName -Raw -ErrorAction Stop

        $updated = $content `
            -creplace 'CardLister\.Desktop', 'FlipKit.Desktop' `
            -creplace 'CardLister\.Core', 'FlipKit.Core' `
            -creplace 'CardLister\.Web', 'FlipKit.Web' `
            -creplace 'CardLister\.Api', 'FlipKit.Api' `
            -creplace 'CardListerDbContext', 'FlipKitDbContext' `
            -creplace 'CardLister', 'FlipKit' `
            -creplace 'cardlister', 'flipkit'

        if ($content -ne $updated) {
            Set-Content $file.FullName $updated -NoNewline
            $count++
            Write-Host "Updated: $($file.Name)"
        }
    } catch {
        # Skip files that can't be read
    }
}

Write-Host "Total files updated: $count"
