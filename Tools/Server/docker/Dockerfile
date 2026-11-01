# The WithTheDead dedicated server: the Linux Dedicated Server build, which Tools/Server/Release.ps1 unpacks into
# ./server beside this file (with its copy of the world store in ./server/WorldStore), on Ubuntu.
FROM ubuntu:24.04

# The game's SQLite binding imports "sqlite3", which Mono looks for as libsqlite3.so (the runtime package has only .so.0).
RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates libsqlite3-0 \
 && rm -rf /var/lib/apt/lists/* \
 && ln -s "$(readlink -f /usr/lib/x86_64-linux-gnu/libsqlite3.so.0)" /usr/lib/x86_64-linux-gnu/libsqlite3.so \
 && ldconfig

# Unity keeps the server's own files (sandbox settings, economy, content database) in ~/.config/unity3d: a volume
# (compose.yaml), made here first so it belongs to the server's user.
RUN useradd --create-home --uid 1001 clonez \
 && mkdir -p /home/clonez/.config/unity3d \
 && chown -R clonez:clonez /home/clonez/.config

COPY --chown=clonez:clonez server/ /server/
RUN chmod 755 /server/WithTheDead.x86_64

USER clonez
WORKDIR /server
EXPOSE 7777/udp
# The log goes to stdout (docker logs); -clonez-port picks the port (compose.yaml passes it).
ENTRYPOINT ["/server/WithTheDead.x86_64", "-batchmode", "-nographics", "-logFile", "-"]
CMD ["-clonez-port", "7777"]
