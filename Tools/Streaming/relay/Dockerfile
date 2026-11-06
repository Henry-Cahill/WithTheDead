# The WTD stream relay (relay.py, compose.yaml): nginx's RTMP module takes the one stream the headless OBS on hts-llm
# makes, and ffmpeg sends it on to Twitch and YouTube as it is. relay.py talks to OBS over its WebSocket.
FROM ubuntu:24.04

RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates ffmpeg libnginx-mod-rtmp nginx python3 python3-websocket \
 && rm -rf /var/lib/apt/lists/*

COPY nginx.conf relay.py /relay/
WORKDIR /relay
ENTRYPOINT ["python3", "-u", "/relay/relay.py"]
