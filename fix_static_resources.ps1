$files = Get-ChildItem -Path "RotinaClone.App" -Recurse -Include *.xaml
foreach ($file in $files) {
    Write-Host "Processing $($file.FullName)..."
    $content = [System.IO.File]::ReadAllText($file.FullName)
    
    $content = $content.Replace('{StaticResource TextPrimary}', '{DynamicResource TextPrimary}')
    $content = $content.Replace('{StaticResource TextSecondary}', '{DynamicResource TextSecondary}')
    $content = $content.Replace('{StaticResource PrimaryAccent}', '{DynamicResource PrimaryAccent}')
    $content = $content.Replace('{StaticResource CardBorder}', '{DynamicResource CardBorder}')
    $content = $content.Replace('{StaticResource GlassCard}', '{DynamicResource GlassCard}')
    $content = $content.Replace('{StaticResource AccentButton}', '{DynamicResource AccentButton}')
    $content = $content.Replace('{StaticResource ErrorColor}', '{DynamicResource ErrorColor}')
    $content = $content.Replace('{StaticResource WindowBackground}', '{DynamicResource WindowBackground}')
    $content = $content.Replace('{StaticResource WindowBackgroundGradient}', '{DynamicResource WindowBackgroundGradient}')
    $content = $content.Replace('{StaticResource CardBackground}', '{DynamicResource CardBackground}')
    $content = $content.Replace('{StaticResource CardHeaderBackground}', '{DynamicResource CardHeaderBackground}')
    $content = $content.Replace('{StaticResource PrimaryAccentHover}', '{DynamicResource PrimaryAccentHover}')
    $content = $content.Replace('{StaticResource InfoColor}', '{DynamicResource InfoColor}')
    $content = $content.Replace('{StaticResource SuccessColor}', '{DynamicResource SuccessColor}')
    $content = $content.Replace('{StaticResource ButtonBackground}', '{DynamicResource ButtonBackground}')
    $content = $content.Replace('{StaticResource ButtonBorder}', '{DynamicResource ButtonBorder}')
    $content = $content.Replace('{StaticResource ButtonHover}', '{DynamicResource ButtonHover}')
    $content = $content.Replace('{StaticResource TextDark}', '{DynamicResource TextDark}')
    
    [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.Encoding]::UTF8)
}
Write-Host "Completed conversion to DynamicResource."
