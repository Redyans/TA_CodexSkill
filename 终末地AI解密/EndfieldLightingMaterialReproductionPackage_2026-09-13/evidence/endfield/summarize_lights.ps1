$expanded = 'C:\Users\xverse\AppData\Local\Temp\endfield-lighting-expanded.txt'
$bundleFiles = @(Get-ChildItem 'C:\Users\xverse\Documents\工作\HowellWork\EndfieldExtract\bundles\main\*.ab' |
    Sort-Object Name | ForEach-Object Name)
$idx = -1
$current = ''
$pending = $null
$rows = @()
foreach ($line in Get-Content $expanded) {
    if ($line -like 'BUNDLE *') {
        $idx++
        $current = if ($idx -lt $bundleFiles.Count) { $bundleFiles[$idx] } else { '?' }
    }
    if ($line -match '^Light\tCLR=Object') {
        $goMatch = [regex]::Match($line, 'GO=(.*?)\s+ID=')
        $pending = [pscustomobject]@{ File = $current; GO = $goMatch.Groups[1].Value.Trim() }
    }
    if ($pending -and $line -match '^ \| m_Type=') {
        $typeMatch = [regex]::Match($line, 'm_Type=([^|]+)')
        $intensityMatch = [regex]::Match($line, 'm_Intensity=([^|]+)')
        $rangeMatch = [regex]::Match($line, 'm_Range=([^|]+)')
        $colorMatch = [regex]::Match($line, 'm_Color=([^|]+)')
        $rows += [pscustomobject]@{
            File = $pending.File
            GO = $pending.GO
            Type = $typeMatch.Groups[1].Value.Trim()
            Intensity = $intensityMatch.Groups[1].Value.Trim()
            Range = $rangeMatch.Groups[1].Value.Trim()
            Color = $colorMatch.Groups[1].Value.Trim()
        }
        $pending = $null
    }
}
$rows | Export-Csv -NoTypeInformation -Encoding utf8 'C:\Users\xverse\AppData\Local\Temp\endfield-light-rows.csv'
$rows | Group-Object File | Sort-Object Name | ForEach-Object {
    $g = $_.Group
    $max = ($g | ForEach-Object { [double]$_.Intensity } | Measure-Object -Maximum).Maximum
    [pscustomobject]@{
        File = $_.Name
        Lights = $g.Count
        Spots = ($g | Where-Object Type -like 'Spot*').Count
        Points = ($g | Where-Object Type -like 'Point*').Count
        Directional = ($g | Where-Object Type -like 'Directional*').Count
        Areas = ($g | Where-Object Type -like 'Area*').Count
        MaxIntensity = $max
        Intensities = (($g | ForEach-Object Intensity | Sort-Object -Unique) -join ',')
        GOs = (($g | ForEach-Object GO | Sort-Object -Unique) -join ';')
    }
} | Export-Csv -NoTypeInformation -Encoding utf8 'C:\Users\xverse\AppData\Local\Temp\endfield-expanded-summary.csv'
Import-Csv 'C:\Users\xverse\AppData\Local\Temp\endfield-expanded-summary.csv'
