#!/usr/bin/env bash
# Connects this Linux machine to the CLONE-Z world store on the NAS, once: installs the NFS client if it's missing,
# mounts 192.168.1.170:/volume2/CLONE-Z at /mnt/clonez (on first use, so a boot never waits for the NAS) and adds it to
# /etc/fstab so it comes back after a reboot. Safe to run again. Needs root:  sudo bash connect-nas.sh
# The NAS lets in only the machines named in its NFS rules for the CLONE-Z shared folder (see Tools/World/README.md).
set -euo pipefail
NAS="${CLONEZ_NAS:-192.168.1.170}"
EXPORT="${CLONEZ_EXPORT:-/volume2/CLONE-Z}"
MNT="${CLONEZ_MOUNT:-/mnt/clonez}"
OPTIONS="rw,noatime,nofail,_netdev,vers=4.1,hard,timeo=600,retrans=2,x-systemd.automount,x-systemd.mount-timeout=30"

[ "$(id -u)" = 0 ] || { echo "Run it as root: sudo bash $0"; exit 1; }

if ! command -v mount.nfs >/dev/null; then
  echo "Installing the NFS client (nfs-common)..."
  export DEBIAN_FRONTEND=noninteractive
  apt-get install -y -q nfs-common >/dev/null 2>&1 || { apt-get update -q >/dev/null && apt-get install -y -q nfs-common >/dev/null; }
fi

mkdir -p "$MNT"
line="$NAS:$EXPORT $MNT nfs4 $OPTIONS 0 0"
if ! grep -qxF "$line" /etc/fstab; then
  cp /etc/fstab /etc/fstab.clonez.bak
  tmp="$(mktemp)"
  grep -vE "[[:space:]]${MNT}[[:space:]]|^# CLONE-Z world store" /etc/fstab >"$tmp" || true
  printf '%s\n%s\n' "# CLONE-Z world store on the NAS (Tools/World/connect-nas.sh)" "$line" >>"$tmp"
  cat "$tmp" >/etc/fstab  # in place, so the file keeps its owner and mode
  rm -f "$tmp"
  echo "Added $MNT to /etc/fstab (the old one is /etc/fstab.clonez.bak)."
fi

systemctl daemon-reload
systemctl restart remote-fs.target 2>/dev/null || true
ls "$MNT" >/dev/null 2>&1 || true  # the first look mounts it
findmnt -n -t nfs4 "$MNT" >/dev/null || mount "$MNT"
findmnt -n -t nfs4 "$MNT" >/dev/null || { echo "Couldn't mount $NAS:$EXPORT at $MNT"; exit 1; }

user="${SUDO_USER:-root}"
probe="$MNT/.connect-test-$(hostname)-$$"
if sudo -u "$user" sh -c "date >'$probe' && rm -f '$probe'"; then
  echo "OK: $(hostname) reads and writes $NAS:$EXPORT at $MNT (checked as $user)."
else
  echo "$MNT is mounted, but $user can't write to it: check the CLONE-Z shared folder's NFS rule for this machine in DSM."
  exit 1
fi
