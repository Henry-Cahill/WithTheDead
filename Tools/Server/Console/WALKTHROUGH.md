# Release console walkthrough

The buttons to press on the release console, in order. What each one runs is in the README's
[Release console](../README.md#release-console) section.

Open **http://192.168.1.205:8095/** (or **http://hank-dev:8095/**) on any device on the network. From top to bottom the
page has: **Now**, then **Release** and **Releases on this PC** side by side, then **Job**, then **Code** and
**itch.io and butler**.

## 1. One-time itch.io setup (skip it until you want itch)

In the **itch.io and butler** card:

1. Type the project as `user/game` in **itch.io project**, then press **Save**.
2. Press **Log in to itch.io**, then **OK**. A browser opens on this PC's screen (HANK-DEV), not on the device you're
   using. Log in there and allow butler. The job ends by itself, and the card then says *logged in*.
   - Or get an API key from itch.io/user/settings/api-keys, paste it into **itch.io API key**, and press **Save**.

After that, the **Also push it to itch.io** box and the **Push to itch.io** button unlock.

## 2. Normal release (one button)

1. In the **Now** card, check that the server line says *nobody connected; no raid open*. A release restarts the
   server, and any raid in progress is lost.
2. In the **Release** card, under **Code to send**, pick one. Under each choice the page shows the commit it would send.
   - **This PC's working copy**: the checked-out commit plus every uncommitted change. The page shows how many files
     have changed.
   - **The latest commit on GitHub main**: only what's committed on GitHub. It fetches from GitHub first by itself. The
     radio's music comes along only once main has the radio.

   The page remembers your choice.
3. Optionally, tick **Also push it to itch.io**.
4. Press **Release**, read the warning, and press **OK**.
5. Watch the **Job** card. The log updates live, and the badge goes from *running* to *succeeded*. The job runs in this
   order: build in Unity, restart the server, zip and upload the game (about 1.2 GB), then check the download page.
6. When it's done:
   - **Now** shows the server and the download page on the new build.
   - In **Releases on this PC**, the new row is tagged *newest*, *on the server* and *on the page*.
   - Friends' title screens say a new build is out.

The first build after a big change (a merge that adds thousands of assets, say) takes noticeably longer, because Unity
has to import the new files into the build copy first.

## 3. Step by step (to test before going live)

1. In the **Release** card, pick the **Code to send** and press **Build only**. Nothing outside this PC changes.
2. In **Releases on this PC**, click the row you want. The newest is picked by default.
3. Press **Check**. It changes nothing; it just names the release and the address its game joins.
4. Press **Deploy to the server**, then **OK**. The server restarts on that build.
5. Press **Publish to the download page**, then **OK**. This uploads about 1.2 GB.
6. Optionally, press **Push to itch.io**, then **OK**.

Always do steps 4 and 5 together for the same release: the server only lets in players on its own build.

## Rolling back

In **Releases on this PC**, click an older row, press **Deploy to the server**, then press
**Publish to the download page**.

## Other buttons

- **Refresh** (Now card) checks the server, the download page and git again straight away.
- **Fetch from GitHub** (Code card) updates the GitHub main line. Releases from main fetch by themselves.
- **Upgrade butler** gets the latest butler. Press it now and then. Before butler is installed, it says
  **Install butler**.
- **Cancel** (Job card) appears only while a job runs. Avoid it during the deploy step, or the server may be left down.
- **History** (under Job): click any past job to read its log again.
