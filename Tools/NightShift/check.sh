#!/usr/bin/env bash
# Compile check for CLONE-Z's C# scripts without the Unity editor (the Spark and the helpers have none).
# Builds Assets/**/*.cs, then the Editor folders, against Unity's own reference assemblies, which the PC
# exports with `NightShift.ps1 refs`. Prints "COMPILE OK", or "COMPILE FAILED" and the errors.
#   usage: bash Tools/NightShift/check.sh
set -uo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
REFS="${NIGHTSHIFT_UNITYREFS:-$HERE/.unityrefs}"
OUT="$HERE/.state/build"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 MSBUILDTERMINALLOGGER=off
for d in /opt/dotnet "$HOME/.dotnet"; do
  if [ -x "$d/dotnet" ]; then export DOTNET_ROOT="$d" PATH="$d:$PATH"; break; fi
done

if ! command -v dotnet >/dev/null; then
  echo "COMPILE CHECK UNAVAILABLE: the .NET SDK is not installed."; exit 2
fi
if [ ! -f "$REFS/Runtime.csproj" ]; then
  echo "COMPILE CHECK UNAVAILABLE: Unity's reference assemblies are missing (run NightShift.ps1 refs on the PC)."; exit 2
fi

mkdir -p "$OUT"
build() { dotnet build "$REFS/$1.csproj" -nologo -v:q -clp:NoSummary -p:ProjectRoot="$ROOT/" -p:NightShiftOut="$OUT/" 2>&1; }
out="$(build Runtime)"; rc=$?
if [ $rc -eq 0 ]; then out="$(build Editor)"; rc=$?; fi
if [ $rc -eq 0 ]; then echo "COMPILE OK"; exit 0; fi

errors="$(printf '%s\n' "$out" | grep -E ': error [A-Z]+[0-9]+' | sed -E 's# \[[^]]*\]$##' | sed "s#^$ROOT/##" | sort -u)"
count="$(printf '%s' "$errors" | grep -c . || true)"
echo "COMPILE FAILED ($count errors)"
if [ "$count" -gt 0 ]; then printf '%s\n' "$errors" | head -n 40; else printf '%s\n' "$out" | tail -n 25; fi
exit 1
