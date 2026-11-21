param([string]$A, [string]$B)
# Prints the key numbers of two map-harness reports side by side (before = A, after = B).
function Read-Report([string]$path) {
    $md = Get-Content $path -Encoding UTF8
    $r = [ordered]@{}
    foreach ($l in $md) {
        if ($l -match '^\| build time \(cold[^|]*\| ([\d,]+) ms') { $r['square cold ms'] = $matches[1] }
        if ($l -match '^\| the same square again, warm, pass by pass \| ([\d,]+) ms') { $r['square warm ms'] = $matches[1] }
        if ($l -match '^\| \*\*all\*\* \| ([\d,]+) \| ([\d,]+) \| [\d,]+ \| [\d,]+ \| ([\d,]+) \| [\d,]+ \| ([\d,]+) \| ([\d,]+) \| [^|]+ \| ([\d,]+) \|') {
            $r['square objects'] = $matches[1]; $r['square renderers'] = $matches[2]; $r['square vertices'] = $matches[3]
            $r['square materials'] = $matches[4]; $r['square slots'] = $matches[5]; $r['square colliders'] = $matches[6]
        }
        if ($l -match '^\| (start-centre|rural-5km|louisville-downtown|lexington-downtown|hazard) \| [^|]+ \| [^|]+ \| [^|]+ \| \*\*([\d,]+)\*\* \| ([^|]+) \| [\d]+ \| ([\d,]+) \| ([\d,]+) \|') {
            $k = 'chunk ' + $matches[1]; if (-not $r.Contains($k)) { $r[$k] = $matches[2] + ' ms (slowest ' + $matches[3].Trim() + '; ' + $matches[4] + ' obj, ' + $matches[5] + ' rend)' }
        }
        if ($l -match '^Nine chunks in ([\d,]+) ms \(slowest ([\d,]+) ms\)') { $r['louisville 3x3'] = $matches[1] + ' ms (slowest ' + $matches[2] + ')' }
        if ($l -match '^\| Louisville 3x3 \| ([\d,]+) \| ([\d,]+) \| [\d,]+ \| [\d,]+ \| ([\d,]+) \| [\d,]+ \| ([\d,]+) \| ([\d,]+) \| [^|]+ \| ([\d,]+) \|') {
            $r['block objects/renderers/slots/colliders'] = $matches[1] + ' / ' + $matches[2] + ' / ' + $matches[5] + ' / ' + $matches[6]
        }
        if ($l -match '^Chunk \d+_\d+ \(start-centre\) built twice: \*\*(\w+)\*\*') { $r['determinism'] = $matches[1] }
        if ($l -match '^\| (start-centre|rural-5km|louisville-downtown|lexington-downtown|hazard)( \(identical\))? \| ([\d,]+) \| ([\d,]+) \| (\d+)% \| ([^|]+) \|') {
            $r['lite/full ' + $matches[1]] = $matches[3] + ' / ' + $matches[4] + ' ms (' + $matches[5] + '%; Full slowest ' + $matches[6].Trim() + ')'
        }
        if ($l -match '^\| (settle|drive-east|teleport-louisville|drive-louisville) \| (\d+) \((\d+)\) \| ([\d.]+) \| ([\d.]+) \| ([\d.]+) \| ([\d.]+) \| ([\d.]+) \| ([^|]+) \| ([^|]+) \| ([^|]+) \| (\d+) \| (\d+) \| ([^|]+) \| ([^|]+) \| ([^|]+) \|') {
            $r['route ' + $matches[1]] = 'mean ' + $matches[4] + ', p95 ' + $matches[6] + ', p99 ' + $matches[7] + ', max ' + $matches[8] + ' (' + $matches[10].Trim() + '); >5/>8/>16/>33: ' + $matches[9].Trim() + '; built ' + $matches[11].Trim() + ', upgraded ' + $matches[12] + '; ring Full after ' + $matches[16].Trim() + ' s'
        }
        if ($l -match 'equivalence \| the fast RaidMesh[^|]+\| [^|]+\| [^|]+\| [^|]+\| ([^|]+)\|') { $r['equivalence'] = $matches[1].Trim().Substring(0, [Math]::Min(120, $matches[1].Trim().Length)) }
        if ($l -match 'its start square built at once \(([^)]+)\)') { $r['streamer start'] = $matches[1] }
    }
    return $r
}
$ra = Read-Report $A; $rb = Read-Report $B
$keys = New-Object System.Collections.Generic.List[string]
foreach ($k in $ra.Keys) { if (-not $keys.Contains($k)) { $keys.Add($k) } }
foreach ($k in $rb.Keys) { if (-not $keys.Contains($k)) { $keys.Add($k) } }
foreach ($k in $keys) {
    "== $k"
    "   before: " + $(if ($ra.Contains($k)) { $ra[$k] } else { '-' })
    "   after:  " + $(if ($rb.Contains($k)) { $rb[$k] } else { '-' })
}
