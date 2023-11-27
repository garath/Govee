#!/bin/sh
dotnet publish --configuration Release --self-contained=true --runtime linux-arm64 --output /usr/local/lib/govee-api/
chmod --verbose o-rwx /usr/local/lib/govee-api/*
cp --verbose ./govee-api.service /etc/systemd/system/