FROM mono:latest

ENV DEBIAN_FRONTEND noninteractive

RUN echo 'deb http://deb.debian.org/debian testing main' >> /etc/apt/sources.list
RUN echo 'APT::Default-Release "stable";' >> /etc/apt/apt.conf.d/00local

RUN apt-get update
RUN apt-get -t testing install -y python3

WORKDIR /root

COPY MonC.sln MonC.sln
COPY src src
COPY test test
COPY testrunner.py testrunner.py

CMD msbuild && ./testrunner.py --showlex --showast --showil

