$dir = 'C:\Users\xverse\AppData\Local\Temp\endfield-per-bundle2'
$rows = @()
foreach ($f in Get-ChildItem "$dir\*.txt") {
    $pending = $null
    foreach ($line in Get-Content $f.FullName) {
        if ($line -match '^Light\tCLR=Object') {
            $goMatch = [regex]::Match($line, 'GO=(.*?)\s+ID=')
            $pending = [pscustomobject]@{ GO = $goMatch.Groups[1].Value.Trim() }
        }
        if ($pending -and $line -match '^ \| m_Type=') {
            $rows += [pscustomobject]@{
                File = $f.BaseName
                GO = $pending.GO
                Type = ([regex]::Match($line, 'm_Type=([^|]+)').Groups[1].Value.Trim())
                Intensity = ([regex]::Match($line, 'm_Intensity=([^|]+)').Groups[1].Value.Trim())
                Range = ([regex]::Match($line, 'm_Range=([^|]+)').Groups[1].Value.Trim())
                Color = ([regex]::Match($line, 'm_Color=([^|]+)').Groups[1].Value.Trim())
            }
            $pending = $null
        }
    }
}
$rows | Export-Csv -NoTypeInformation -Encoding utf8 'C:\Users\xverse\AppData\Local\Temp\endfield-target-light-rows.csv'
$rows | Group-Object File | Sort-Object Name | ForEach-Object {
    $g = $_.Group
    $max = ($g | ForEach-Object { [double]$_.Intensity } | Measure-Object -Maximum).Maximum
    [pscustomobject]@{
        File = $_.Name
        Lights = $g.Count
        Spots = ($g | Where-Object Type -like 'Spot*').Count
        Points = ($g | Where-Object Type -like 'Point*').Count
        MaxIntensity = $max
        GOs = (($g | ForEach-Object GO | Sort-Object -Unique) -join ';')
    }
}
