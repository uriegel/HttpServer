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

on Raspberry:
```
sudo apt-get update
sudo apt-get install curl libunwind8 gettext apt-transport-https
```
