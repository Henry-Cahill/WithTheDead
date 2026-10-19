#!/bin/bash
# This file contains bash commands that will be executed at the end of the container build process,
# after all system packages and programming language specific package have been installed.
#
# Note: This file may be removed if you don't need to use it
set -e
SUDO=""; [ "$(id -u)" -ne 0 ] && SUDO="sudo"
case "$(uname -m)" in x86_64) ARCH=x64 ;; aarch64|arm64) ARCH=arm64 ;; *) echo "unsupported CPU $(uname -m)"; exit 1 ;; esac

# Night Shift (Tools/NightShift): the .NET SDK compiles the game's scripts against Unity's assemblies
# (there's no Unity editor for this container) ...
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
$SUDO bash /tmp/dotnet-install.sh --channel 10.0 --install-dir /opt/dotnet
$SUDO ln -sf /opt/dotnet/dotnet /usr/local/bin/dotnet
rm -f /tmp/dotnet-install.sh

# ... and OpenCode is the coding agent, driven by a model in Ollama on the host.
OPENCODE_VERSION=1.19.3
curl -fsSL "https://github.com/anomalyco/opencode/releases/download/v${OPENCODE_VERSION}/opencode-linux-${ARCH}.tar.gz" \
  | $SUDO tar -xz -C /usr/local/bin opencode
$SUDO chmod 755 /usr/local/bin/opencode