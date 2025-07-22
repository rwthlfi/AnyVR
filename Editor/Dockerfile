# https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/concepts/container-builds

# ======================================================== #
#                  Unity base image stuff                  #
# ======================================================== #

FROM ubuntu:20.04 AS mpuk

RUN addgroup --gid 2000 mpukgame && \
    useradd -g 2000 -u 2000 -ms /bin/sh mpukgame && \
    mkdir /game && \
    chown mpukgame:mpukgame /game && \
    apt update && \
    apt upgrade -y && \
    apt install -y ca-certificates
USER mpukgame

# ======================================================== #
#                    Custom game stuff                     #
# ======================================================== #

FROM mpuk AS game

# copy game files here
# for example:
WORKDIR /game
COPY --chown=mpukgame . .

# set your game binary as the entrypoint
ENTRYPOINT [ "./gamebinary" ]
