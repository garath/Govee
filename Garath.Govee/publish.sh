dotnet publish --configuration Release --self-contained=false --output /usr/local/lib/govee-monitor/
chmod --verbose o-rwx /usr/local/lib/govee-monitor/*
cp --verbose ./govee-monitor.service /etc/systemd/system/