# WithTheDead co-op server

Play WithTheDead with friends over the internet while you develop it. A headless dedicated server runs in Docker on
hts-llm (192.168.1.202), and your friends download the game from its own page on your domain,
**https://withthedead.howto.software**, behind one shared login. The game tells them when a new build is out. One
script builds both from your working copy, restarts the server and publishes the game: [Release.ps1](Release.ps1).

## How it works

- **The dedicated server** is the game's Linux Dedicated Server build ([DedicatedServer.cs](../../Assets/Scripts/Net/DedicatedServer.cs)).
  It has no title screen and no runner of its own. It waits with no world built until a runner deploys from the bar.
  It then opens that runner's Zone for everyone: it builds the raid's world, runs its dead, its people and its clock, and
  sends the runner in. The others can follow from the bar's door, just as they follow a host. When nobody has been in
  the raid for a minute and a half, the server closes it and waits again. It runs one raid at a time.
- **The download page** is a set of static files on hts-llm (`~/clonez-server/site`, served on port 8077 by the
  `clonez-site` container). The edge Caddy on 2htswebserver (192.168.1.210, the one your router sends ports 80 and 443
  to) serves them at withthedead.howto.software with HTTPS and the shared login. The files are the page itself (from
  [site/index.html](site/index.html)), the game zipped (`files/WithTheDead-<stamp>.zip`, the newest three kept) and
  `latest.json`, the build the page hands out. `latest.json` needs no login: the game reads it (address in
  `updates.txt` beside the executable, see [UpdateCheck.cs](../../Assets/Scripts/Net/UpdateCheck.cs)) and puts a
  *new build is out* card on the title screen when it isn't its own build.
- **Builds must match.** The server and the game you hand out are built together and stamped
  `0.1.0+<date>.<time>.<commit>` (`Application.version`). The server turns away any other build with "Update
  WithTheDead, then join again". The editor always gets in, so you can play from it. If you've changed the netcode since
  the server was built, release again.
- **The map travels with the game.** Each release carries a copy of the world store's current build (about 120 MB, in
  `WorldStore\` beside the executable). A friend's game therefore builds the same Kentucky as the server without
  reaching the NAS.
- **The item catalogue matches too.** Item ids cross the network, so a release's game keeps its own content database
  (`clonez_content_release.db`) and rebuilds it whenever its build changes. The editor's `clonez_content.db` is left
  alone, even on your PC, where the two share a folder.
- **Builds carry the shaders the game asks for by name.** The world's materials are made at runtime with
  `Shader.Find`, which finds a shader in a build only if something in the build uses it.
  `Assets/Resources/RuntimeShaders` holds a material for each one, plus its transparent, alpha-clipped and emissive
  variants. Every CloneZBatch build first brings that folder up to date from the scripts
  ([RuntimeShaders.cs](../../Assets/Editor/RuntimeShaders.cs)); *CloneZ ▸ Build ▸ Update Runtime Shaders* does it
  by hand.

## One-time setup

All of this was done when the server was set up (1 November 2026); it's here so it can be done again.

1. **Router:** forward **UDP 7777** to **192.168.1.202** (hts-llm, which has a static address) for the game, and TCP 80
   and 443 to 192.168.1.210 (2htswebserver) for the web, as for your other sites.
2. **DNS (Cloudflare, howto.software):** an A record `withthedead` → your public address (136.34.124.155), **DNS only**
   (grey cloud): a 1.2 GB download shouldn't go through Cloudflare's proxy, whose terms restrict serving large files
   through it. The game joins this name too, so if your public address changes, only this record needs updating.
3. **The edge Caddy:** the `withthedead.howto.software` block at the end of `/home/htsadmin/caddy/Caddyfile` on
   2htswebserver. It asks for the login everywhere except `/latest.json` and passes requests on to
   `192.168.1.202:8077`. Caddy got the site's certificate by itself.

## Releasing

    powershell -ExecutionPolicy Bypass -File Tools\Server\Release.ps1

The script does three things:

1. **Build:** it mirrors the project, including uncommitted changes, into `..\CLONE-Z-build`, so the editor you have
   open is left alone. It then runs `CloneZ.EditorTools.CloneZBatch.BuildRelease` there in a batch editor. The output
   goes to `Builds\Release\<stamp>\client` (`WithTheDead.exe`) and `\server` (`WithTheDead.x86_64`), with the build log
   beside them. The first build seeds the copy's `Library` from yours (4.4 GB) and takes a while; later builds only
   import what changed. Both get the map from `Builds\WorldStore`, a copy of the world store's current build that each
   release brings up to date from the NAS. The client also gets `server.txt` (`withthedead.howto.software:7777`), which
   its Join box starts at, and `updates.txt`.
2. **Deploy:** it copies the server to `~/clonez-server` on hts-llm, builds the `clonez-server` image
   ([docker/Dockerfile](docker/Dockerfile)) and restarts the containers ([docker/compose.yaml](docker/compose.yaml)). It
   waits until the server says it's waiting for a runner. A raid under way on the old server is lost.
3. **Publish:** it zips the game (about 1.2 GB, most of it the item models' textures), uploads it to the page with a new
   `latest.json` and page, and checks that the page hands out the new build. With `-Itch <user>/<game>` it also pushes the
   game to that itch.io project with butler.

To run only some of the steps, use `-NoBuild`, `-NoDeploy` or `-NoPublish`. Without a build, the script uses the newest
release, or the one named by `-Stamp <stamp>`. `-Port` changes the port everywhere: the server, the container and
`server.txt`. `-SiteUrl` and `-PublicAddress` point the game at another page or address.

## Playing

- **Your friend:** opens https://withthedead.howto.software, logs in with the shared login, downloads the zip, unzips it
  and runs `WithTheDead.exe`. They make a runner, then *PLAY TOGETHER → Join* (the address is already filled in). In
  the bar, they open the deploy screen from the door, pick a Zone and press *Go*. If a raid is already under way, they
  join that one instead. When you release again, their title screen says a new build is out, with a button to the page.
- **You:** in the editor or a release build, join **withthedead.howto.software** (or **192.168.1.202**) on port 7777.
  The title screen remembers the last address you joined. In a build, though, `server.txt` comes first.
- **Server settings** (max players, PvP, the dead's numbers) come from `clonez_sandbox.json` in the server's data
  volume. The hub's Sandbox screen writes yours to `%USERPROFILE%\AppData\LocalLow\DefaultCompany\WithTheDead\`. To use
  it on the server, run this from that folder:

      scp clonez_sandbox.json htsadmin@192.168.1.202:
      ssh htsadmin@192.168.1.202 "docker cp clonez_sandbox.json clonez-server:/home/clonez/.config/unity3d/DefaultCompany/WithTheDead/ && docker restart clonez-server"

## Looking after the server

On hts-llm, in `~/clonez-server`:

| | |
|---|---|
| `docker compose logs -f --tail 100` | the server's log: who joined, raids opened and closed, errors |
| `docker compose restart` | restart it (a raid under way is lost) |
| `docker compose down` / `up -d` | stop it / start it again |
| `cat server/release.txt` | the build it runs |
| `ls site site/files` | the download page's files |

The container restarts by itself after a crash or a reboot. It's capped at 4 cores and 8 GB, because hts-llm does
other work too. The last three releases' images are kept. To go back to one, run
`docker tag clonez-server:<stamp> clonez-server:latest && docker compose up -d --force-recreate`, then publish the
matching game with `Release.ps1 -NoBuild -NoDeploy -Stamp <stamp>`.

**Reading the log:** the log names each runner by name and connection number (`Runner #3`), since runners often share
a name. Every line about the same connection has the same number. A join line says where the runner connects from and
how many are on; a leave line says how long they stayed. Every 5 minutes a `[Server] Status:` line says how long the
server has been up, who's connected and where (in the raid or at the bar), and which raid is open and for how long. It
also gives the frame rate and slowest frame since the last status line, and the memory the server holds against its
8 GB cap. A quiet log with status lines still coming means the server is fine. If they stop, it's stuck. Pass
`-clonez-status <seconds>` (or `CLONEZ_STATUS`) to change how often they come, or `0` to turn them off.

**Changing the page's login:** on 2htswebserver, back up the Caddyfile and get a hash for the new password from Caddy:

    ssh htsadmin@192.168.1.210
    cd ~/caddy && cp -p Caddyfile Caddyfile.bak.$(date +%Y%m%d-%H%M%S)
    docker exec -it caddy-proxy caddy hash-password

Put the hash in place of the old one on the `withthedead $2a$…` line of the `withthedead.howto.software` block. Then run
`docker exec caddy-proxy caddy reload --config /etc/caddy/Caddyfile --adapter caddyfile`. The container sees the
Caddyfile through a bind mount: if your editor saved it as a new file, the reload still sees the old one, so run
`docker restart caddy-proxy` instead.

**Checking it without a friend:** a release's game can play the smoke test's client against the server. It joins, opens
a raid in Kentucky, checks that the server's dead reach it, throws a grenade and builds a wall, then leaves. The game
exits with 0 when every check passes, and the results are the `[Smoke]` lines in the log. Joining the public address
from home also proves the router's port forward, since the router loops it back:

    Builds\Release\<stamp>\client\WithTheDead.exe -batchmode -nographics -clonez-smoke client -clonez-smoke-join withthedead.howto.software -clonez-smoke-zone bardstown -logFile smoke.log

It uses its own runner and leaves yours alone. Run it while nobody is in a raid, or it joins theirs.

**Without Docker:** any build, or the editor, becomes the dedicated server with `-clonez-server` on its command line
(`-clonez-port <port>` or `CLONEZ_PORT` sets the port, 7777 by default; `-clonez-status <seconds>` or `CLONEZ_STATUS`
how often the status line comes).

## Limits

- One raid at a time, and the server never plays. A raid closes a minute and a half after the last runner leaves it.
- The game server has no password: anyone who has the game and the address can join. The page's login is what keeps the
  game to your friends.
- The server and every player must be on the same release. When you release, your friends download the new build before
  they can join again: the whole 1.2 GB, as the page has no patching.
- `withthedead.howto.software` is new to DNS: a resolver that looked it up before the record existed (your router did)
  says it doesn't exist for up to 30 minutes afterwards.
