param(
    [Parameter(Mandatory = $true)]
    [string[]] $Path,

    [string] $Manifest = "$env:LOCALAPPDATA\Temp\manifest.hgmmap"
)

$ErrorActionPreference = 'Stop'

function Read-LengthPrefixedUnicode([System.IO.BinaryReader] $reader) {
    $charCount = $reader.ReadInt32()
    return [System.Text.Encoding]::Unicode.GetString($reader.ReadBytes($charCount * 2)).TrimEnd([char]0)
}

function Read-DataString(
    [System.IO.BinaryReader] $reader,
    [long] $dataAddress,
    [int] $relativeOffset,
    [switch] $Compressed
) {
    $savedPosition = $reader.BaseStream.Position
    try {
        $reader.BaseStream.Position = $dataAddress + $relativeOffset
        $byteCount = $reader.ReadInt32()
        $bytes = $reader.ReadBytes($byteCount)
        if ($Compressed) {
            try {
                $input = [System.IO.MemoryStream]::new($bytes, $false)
                $brotli = [System.IO.Compression.BrotliStream]::new(
                    $input,
                    [System.IO.Compression.CompressionMode]::Decompress
                )
                $output = [System.IO.MemoryStream]::new()
                try {
                    $brotli.CopyTo($output)
                    $bytes = $output.ToArray()
                }
                finally {
                    $output.Dispose()
                    $brotli.Dispose()
                    $input.Dispose()
                }
            }
            catch {
                # Some manifest builds store selected paths as plain UTF-16.
                # Match the Python parser's compatibility fallback.
            }
        }
        return [System.Text.Encoding]::Unicode.GetString($bytes).TrimEnd([char]0)
    }
    finally {
        $reader.BaseStream.Position = $savedPosition
    }
}

$wanted = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($item in $Path) {
    [void] $wanted.Add($item.Replace('\', '/'))
}

$stream = [System.IO.File]::OpenRead($Manifest)
$reader = [System.IO.BinaryReader]::new($stream)
try {
    if ($reader.ReadUInt32() -ne [Convert]::ToUInt32('FF11FF11', 16)) { throw 'Unexpected manifest header.' }
    [void] (Read-LengthPrefixedUnicode $reader)
    if ($reader.ReadUInt32() -ne [Convert]::ToUInt32('F1F2F3F4', 16)) { throw 'Unexpected manifest second header.' }
    [void] (Read-LengthPrefixedUnicode $reader)
    [void] (Read-LengthPrefixedUnicode $reader)

    $assetSize = $reader.ReadInt32()
    $assetAddress = $reader.BaseStream.Position
    $reader.BaseStream.Position += $assetSize
    $bundleSize = $reader.ReadInt32()
    $reader.BaseStream.Position += $bundleSize
    $bundleArraySize = $reader.ReadInt32()
    $bundleArrayAddress = $reader.BaseStream.Position
    $dataAddress = $bundleArrayAddress + $bundleArraySize + 4

    $reader.BaseStream.Position = $assetAddress
    $assetCount = $reader.ReadInt32()
    $reader.BaseStream.Position += [long] $assetCount * 8
    $hits = [System.Collections.Generic.List[object]]::new()
    for ($index = 0; $index -lt $assetCount -and $wanted.Count -gt 0; $index++) {
        $recordAddress = $reader.BaseStream.Position
        $reader.BaseStream.Position += 8
        $pathOffset = $reader.ReadInt32()
        $reader.BaseStream.Position = $recordAddress + 40
        $bundleIndex = $reader.ReadInt32()
        $assetBytes = $reader.ReadInt32()
        $assetPath = Read-DataString $reader $dataAddress $pathOffset -Compressed
        if ($wanted.Remove($assetPath)) {
            $hits.Add([pscustomobject]@{
                Path = $assetPath
                BundleIndex = $bundleIndex
                AssetBytes = $assetBytes
            })
        }
        $reader.BaseStream.Position = $recordAddress + 48
    }

    if ($hits.Count -eq 0) { throw 'No requested manifest assets were found.' }

    $targetBundleIndexes = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($hit in $hits) { [void] $targetBundleIndexes.Add($hit.BundleIndex) }
    $bundleNames = @{}
    $reader.BaseStream.Position = $bundleArrayAddress
    $bundleCount = $reader.ReadInt32()
    for ($index = 0; $index -lt $bundleCount -and $bundleNames.Count -lt $targetBundleIndexes.Count; $index++) {
        $bundleIndex = $reader.ReadInt32()
        $nameOffset = $reader.ReadInt32()
        $reader.BaseStream.Position += 12
        $reader.BaseStream.Position += 4 + 8 + 8 + 4 + 4
        if ($targetBundleIndexes.Contains($bundleIndex)) {
            $bundleNames[$bundleIndex] = Read-DataString $reader $dataAddress $nameOffset
        }
    }

    foreach ($hit in $hits) {
        [pscustomobject]@{
            Path = $hit.Path
            BundleIndex = $hit.BundleIndex
            Bundle = $bundleNames[$hit.BundleIndex]
            AssetBytes = $hit.AssetBytes
        }
    }
    foreach ($missing in $wanted) {
        Write-Error "Manifest asset not found: $missing"
    }
}
finally {
    $reader.Dispose()
    $stream.Dispose()
}
