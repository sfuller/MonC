FROM mcr.microsoft.com/dotnet/core/sdk:3.1

ENV DEBIAN_FRONTEND noninteractive

RUN echo 'deb http://deb.debian.org/debian testing main' >> /etc/apt/sources.list
RUN echo 'APT::Default-Release "stable";' >> /etc/apt/apt.conf.d/00local

RUN apt-get update
RUN apt-get -t testing install -o APT::Immediate-Configure=false -y python3

WORKDIR /root

COPY MonC.sln MonC.sln
COPY src src
COPY test test
COPY testrunner.py testrunner.py

CMD dotnet build && ./testrunner.py --showlex --showast --showil

