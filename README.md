# HttpServer
A HTTP/2 web server written in F#
## Visual Studio Code extensions:
* Ionide-fsharp
* C# (C# for Visual Studio Code (powered by OmniSharp).)
* Git History
* Todo Tree
## Certificates
```sudo openssl pkcs12 -export -out certificate.pfx -inkey /etc/letsencrypt/live/uriegel.de/privkey.pem -in /etc/letsencrypt/live/uriegel.de/cert.pem```

```sudo chown uwe certificate.pfx```

### build for Ubuntu:
```dotnet publish -c Release -r ubuntu.18.04-x64```

### Run on Raspberry
build for linux 32bit:

```dotnet publish -c Release -r linux-arm```

### Run for Windows
build for linux 32bit:

```dotnet publish -c Release -r win-x64```

access raspis folder:

```
mkdir pi
sshfs pi@raspberrypi: pi
```cd 

on Raspberry:
```
sudo apt-get update
sudo apt-get install curl libunwind8 gettext apt-transport-https
```

Now compile ```starter.c``` on raspi in folder ```publish```:
```gcc starter.c -o starter```

Start ```starter``` as sudo:
```
sudo bash
cd /home/pi/publish
./starter
```
