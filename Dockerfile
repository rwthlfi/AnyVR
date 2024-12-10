FROM unitymultiplay/linux-base-image:latest

USER root
COPY . /usr/local/bin/
USER mpukgame

EXPOSE 7777

ENTRYPOINT ["anyvr.x86_64"]
