# The WithTheDead dedicated server: the Linux Dedicated Server build, which Tools/Server/Release.ps1 unpacks into
# ./server beside this file (with its copy of the world store in ./server/WorldStore), on Ubuntu.

# The Java runtime the server's Java scripts run on (Java/README.md). Temurin ships no jmods and jlink links only for the
# platform it runs on, so it's linked here, on Linux, from the JDK and with the modules the build names in
# WithTheDead_Data/Java/runtime.env (checked against the pinned SHA-256); the layer stays cached until those change. A
# build that names no modules (one from before the game had Java scripts) gets an empty runtime. jlink's --strip-debug
# strips the native libraries too, with binutils' objcopy.
FROM ubuntu:24.04 AS java
RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates curl binutils \
 && rm -rf /var/lib/apt/lists/*
COPY server/WithTheDead_Data/Java/runtime.env /tmp/runtime.env
RUN set -eu; . /tmp/runtime.env; \
    mkdir -p /java-runtime; \
    if [ -n "${JAVA_MODULES:-}" ]; then \
        curl -fsSL --retry 3 -o /tmp/jdk.tar.gz "$JDK_URL"; \
        echo "$JDK_SHA256  /tmp/jdk.tar.gz" | sha256sum -c -; \
        mkdir /tmp/jdk; \
        tar -xzf /tmp/jdk.tar.gz -C /tmp/jdk --strip-components=1; \
        rmdir /java-runtime; \
        /tmp/jdk/bin/jlink --add-modules "$JAVA_MODULES" --output /java-runtime --strip-debug --no-header-files \
            --no-man-pages --strip-native-commands --compress zip-6; \
        rm -rf /tmp/jdk /tmp/jdk.tar.gz; \
    fi

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
COPY --from=java --chown=clonez:clonez /java-runtime /server/WithTheDead_Data/Java/runtime
RUN chmod 755 /server/WithTheDead.x86_64

USER clonez
WORKDIR /server
EXPOSE 7777/udp
# The log goes to stdout (docker logs); -clonez-port picks the port (compose.yaml passes it).
ENTRYPOINT ["/server/WithTheDead.x86_64", "-batchmode", "-nographics", "-logFile", "-"]
CMD ["-clonez-port", "7777"]
