
dotnet publish --configuration Release --self-contained=false --output /usr/local/lib/govee-api/
chmod --verbose o-rwx /usr/local/lib/govee-api/*
cp --verbose ./govee-api.service /etc/systemd/system/