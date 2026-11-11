# WithTheDead stream

The WithTheDead stream goes out on Twitch and YouTube at once. A headless OBS on hts-llm (192.168.1.202) makes it, and
a small relay beside it sends it to both. You run it from one page,
[obs-twitch.html](obs-twitch.html): open the file in a browser on this PC.

## How it works

```
This PC                            hts-llm (192.168.1.202)
OBS: your screen, the computer's   headless OBS (obs-headless.service): MainStream shows your
sound and your headset mic  ─────► screen or the Be-right-back card
       rtmp://192.168.1.202:1935     │ one stream, rtmp://127.0.0.1:1936/live
                                     ▼
                                   the relay (Docker, wtd-relay) ──► Twitch
                                                                 └─► YouTube
```

- **OBS on this PC** sends your main display, its sound and your headset mic to the headless OBS. It starts that way when
  you sign in to Windows. The page sets the two sound levels.
- **The headless OBS** builds the stream from that feed, or from the Be-right-back card while your screen is hidden. It
  streams once, to the relay. It goes live the moment it starts (`--startstreaming`), so after a reboot or a restart
  the stream goes out again by itself, to every destination that's switched on. A reboot that stops it mid-save can
  lose its scenes, so a copy is kept in `~/wtd/Untitled.json.last-good` on hts-llm and put back before it starts.
- **The relay** ([relay/](relay)) takes that stream in nginx's RTMP module and runs one ffmpeg per destination, which
  sends it on unchanged: no second encode, and both get the same picture. When one drops, it reconnects on its own,
  waiting a little longer each time it fails in a row, while the other carries on. It talks to OBS over OBS's WebSocket,
  with OBS's own password. It reads the stream keys from there and reports how each destination is doing about every
  2 seconds. It opens no port to the network.
- **The keys** are in OBS's persistent data (slot `wtd-destinations`, in
  `~/.config/obs-studio/plugin_config/obs-websocket/persistent_data.json` on hts-llm). The page writes them there and
  never shows them again. Anyone with OBS's WebSocket password can read them, as they could read OBS's own stream key.
- **Keyframes:** Twitch and YouTube both want one every 2 seconds. OBS takes that from the streaming service it sends
  to, and the relay is a custom server that sets none. So when the page routes OBS to the relay, it sets x264's
  `keyint` to 2 seconds' worth of frames, in OBS's custom x264 options (Simple output mode).
- **Upload:** each destination gets the whole stream, so going live on both sends it twice: about 12.5 Mbps at OBS's
  6,000 kbps. Twitch's bandwidth test (section 7) shows whether one copy gets through whole.

Twitch allows simulcasting as long as Twitch's stream isn't worse than the others, which it can't be here because it's
the same stream. You may link your YouTube channel, but don't urge Twitch viewers to go and watch there.

## One-time setup

1. **The relay.** Run `powershell -ExecutionPolicy Bypass -File Tools\Streaming\Deploy-Relay.ps1`. It copies
   [relay/](relay) to `~/wtd-relay` on hts-llm, builds the image and starts the `wtd-relay` container, which comes back
   by itself after a reboot. It also adds two files to `/etc/systemd/system/obs-headless.service.d/`:
   `wait-for-relay.conf`, so that at boot OBS starts after Docker and waits for the relay, and `keep-mainstream.conf`,
   which puts OBS's scenes back before it starts if it has lost them. Run it again after changing anything in `relay/`.
2. **YouTube.** In [YouTube Studio](https://studio.youtube.com), choose **Create → Go live**. The first time, YouTube
   asks you to verify the channel (with a phone number), and live streaming can take up to 24 hours to turn on.
3. In the Live Control Room's **Stream** tab, set the title, the description, the category and who can watch: Public,
   Unlisted or Private. Turn on **Auto-start** and **Auto-stop**, so that YouTube's broadcast starts and ends with the
   relay's stream. Then copy the **Stream key**. YouTube keeps the key and these settings from one stream to the next.
4. **The key.** On the page, in section **6 · Twitch and YouTube**, paste it into YouTube's box and press
   **Save to OBS**. Twitch's key is already there: the page copied it from OBS's own stream settings, where it was
   before the relay. If you reset a key, save the new one the same way.

## Going live

On the page, press **Go live** in section 2. It asks first and says what viewers will see, then waits until each
destination has the stream. Under the button there's a line for each one: *● LIVE* with its time and bitrate,
*connecting*, *dropped (why); trying again*, or *switched off*. **End stream** ends it everywhere.

- To leave one out, untick its **Go live here** in section 6. Mid-stream, that ends the stream there alone, and ticking
  it again brings it back.
- To try YouTube without anyone watching, set the stream to Private in YouTube Studio first.
- The first time after setting up the relay, OBS may still send straight to Twitch, as it did before the relay. The
  page says so (*Streams to: Twitch directly*). Going live from the page routes it through the relay from then on.

## The relay by hand

On hts-llm, in `~/wtd-relay`:

| Command | What it does |
| --- | --- |
| `docker compose logs -f --tail 100` | what it's doing: each destination connecting, going live, dropping, and why |
| `docker compose restart` | restart it (if you're live, Twitch and YouTube drop for a few seconds and come back) |
| `docker compose down` / `up -d` | stop it / start it again |

Each destination's ingest can be changed with an environment variable in [relay/compose.yaml](relay/compose.yaml):
`WTD_TWITCH_INGEST` (default `rtmp://ingest.global-contribute.live-video.net/app/`, the one OBS's *Auto* picks) and
`WTD_YOUTUBE_INGEST` (default `rtmps://a.rtmps.youtube.com:443/live2/`).

## When something's wrong

- **Relay: not answering** (section 5): the container isn't running or can't reach OBS. Check
  `ssh htsadmin@192.168.1.202 "docker ps; docker logs --tail 50 wtd-relay"`, and run Deploy-Relay.ps1 again.
- **dropped (it refused the stream as it started, which usually means a wrong or reset key):** copy that key again and
  save it in section 6. Twitch and YouTube both hang up on a key they don't know just as the stream starts.
- **YouTube is live on the page but not in YouTube Studio:** check Auto-start is on, or press **Go live** in the Live
  Control Room.
- **The picture is frozen on both:** the headless OBS's *Windows Screen* source is stuck on a dead connection from this
  PC. Restart it with `ssh htsadmin@192.168.1.202 "sudo systemctl restart obs-headless"`. OBS comes back and goes
  live again by itself.
- **Section 3 says *Not set up*, and viewers see a black picture:** the headless OBS lost its scenes (a reboot that
  stops it mid-save can do that) and started on an empty one. Restart it the same way: before it starts, it puts back
  the last copy that had MainStream (`~/wtd/Untitled.json.last-good`), and its journal says so.
- **Section 3 says *Feed from this PC: waiting*:** OBS on this PC isn't sending your screen. It starts sending by itself
  only when Windows starts it at sign-in (the *WTD screen feed (OBS)* shortcut in your Startup folder), and once the
  OBS box has been away a while, it tries again only every 15 minutes. Press **Re-sync picture & sound** (section 4
  has to be connected) to send it now.
