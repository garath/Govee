dotnet publish --configuration Release --self-contained=false --output /usr/local/lib/govee-monitor/
sudo chmod o-rwx /usr/local/lib/govee-monitor/*
sudo cp ./govee-monitor.service /etc/systemd/system/