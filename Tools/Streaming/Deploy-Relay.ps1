<#
.SYNOPSIS
    Puts the stream relay (Tools\Streaming\relay) on hts-llm, the machine with the headless OBS, and (re)starts it.

.DESCRIPTION
    The relay takes the one stream the headless OBS makes and sends it on to Twitch and YouTube at once
    (Tools\Streaming\README.md). This copies relay\ to ~/wtd-relay on -ServerHost, builds its image there and starts the
    container wtd-relay, which comes back by itself after a reboot. It also has OBS's service (obs-headless) start after
    Docker and wait for the relay, because OBS goes live the moment it starts.

    Running it again redeploys. If you're live through the relay then, Twitch and YouTube drop for a few seconds while it
    restarts, and OBS reconnects by itself.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Streaming\Deploy-Relay.ps1
#>
param(
    # The machine with the headless OBS, and the folder in its home that holds the relay.
    [string]$ServerHost = 'htsadmin@192.168.1.202',
    [string]$RemoteDir = 'wtd-relay'
)

$ErrorActionPreference = 'Stop'

# Runs a bash script on the server machine. It travels base64-encoded: PowerShell 5 mangles quotes on native command
# lines and appends CRLF when piping to them.
function Invoke-Remote([string]$Script) {
    $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Script.Replace("`r`n", "`n")))
    & ssh -o BatchMode=yes -o ConnectTimeout=10 $ServerHost "echo $b64 | base64 -d | bash"
    if ($LASTEXITCODE -ne 0) { throw "The script on $ServerHost failed (exit $LASTEXITCODE)." }
}

# The files go up with LF line endings whatever the checkout has: nginx, Docker and bash want them that way.
$staging = Join-Path ([IO.Path]::GetTempPath()) ('wtd-relay-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $staging | Out-Null
try {
    $utf8 = New-Object Text.UTF8Encoding $false
    $files = foreach ($name in 'Dockerfile', 'compose.yaml', 'nginx.conf', 'relay.py') {
        $text = [IO.File]::ReadAllText((Join-Path $PSScriptRoot "relay\$name"))
        $path = Join-Path $staging $name
        [IO.File]::WriteAllText($path, $text.Replace("`r`n", "`n"), $utf8)
        $path
    }
    Invoke-Remote "mkdir -p ~/$RemoteDir"
    & scp -q -o BatchMode=yes @files "$($ServerHost):$RemoteDir/"
    if ($LASTEXITCODE -ne 0) { throw "scp to $ServerHost failed (exit $LASTEXITCODE)." }
} finally {
    Remove-Item -Recurse -Force $staging
}

Write-Host "Building and starting the relay on $ServerHost..."
Invoke-Remote (@'
set -e
cd ~/__DIR__
docker compose build --quiet
docker compose up -d
sudo -n mkdir -p /etc/systemd/system/obs-headless.service.d
sudo -n tee /etc/systemd/system/obs-headless.service.d/wait-for-relay.conf >/dev/null <<'EOF'
# Tools/Streaming/Deploy-Relay.ps1 puts this here. OBS goes live the moment it starts (--startstreaming), through the
# stream relay in Docker (~/__DIR__): at boot, start after Docker and give the relay up to a minute to listen first, so
# that first connection doesn't fail.
[Unit]
After=docker.service
Wants=docker.service

[Service]
ExecStartPre=/bin/bash -c 'for i in {1..60}; do (exec 3<>/dev/tcp/127.0.0.1/1936) 2>/dev/null && exit 0; sleep 1; done; exit 0'
EOF
sudo -n systemctl daemon-reload
for i in $(seq 30); do
  if docker logs wtd-relay 2>&1 | grep -q 'Connected to OBS'; then break; fi
  sleep 1
done
docker compose ps --format '{{.Name}}: {{.Status}}'
echo '--- log:'
docker logs --tail 15 wtd-relay 2>&1
docker logs wtd-relay 2>&1 | grep -q 'Connected to OBS' || { echo 'The relay has not reached OBS yet; see its log above.'; exit 1; }
'@).Replace('__DIR__', $RemoteDir)
Write-Host 'The relay is up. Go live from Tools\Streaming\obs-twitch.html.'
