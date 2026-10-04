Truck Remote Server
====================
Server app for [Truck Remote Control](https://github.com/alexChurkin/TruckRemoteControl) written on C#.

#### [Russian guide](README_ru.md)

### [VIDEO INSTRUCTION](https://www.youtube.com/watch?v=qJOPYtYDHOo)

### [DOWNLOAD THE LATEST VERSION](https://github.com/alexChurkin/TruckRemoteServer/releases)

### Supported OS
**Windows Vista, Windows 7, 8, 10 or 11 (64-bit)**.
**.NET Framework 4.7.2** is required. If it is not installed you will be prompted to install it when you run the server.

### Supported games

- Euro Truck Simulator 2 (64-bit) version 1.35+. Multiplayer versions are supported as well.
- American Truck Simulator version 1.35+

## Setup

ATTENTION: Do not delete files from the **TruckRemoteServer** folder. It's important for correct Server work.

Launching:
1) Install **vJoy** on your PC by launching **vJoySetup.exe**
2) Open **Configure vjoy**, configure 1-st virtual joystick as on this screenshot:

    ![](https://github.com/alexChurkin/TruckRemoteServer/raw/master/Screenshot_vjoy_conf.png)
	
3) Сopy the **TruckRemoteServer** folder wherever you want
4) Launch **TruckRemoteServer.exe** and agree to install the telemetry plugin for ETS2/ATS

### Analog pedals (optional)

By default gas and brake are emulated with the arrow keys (full press). To control the press force
(the **Analog** pedal mode in the app settings):
1) In **Configure vJoy** additionally enable the **Y** and **Z** axes of the 1-st device and press **Apply**
2) In the game, open *Options → Controls*, choose the vJoy device and bind **Throttle** to the Y axis
   and **Brake** to the Z axis (both are 0 when released; invert them in the game if needed)

If the axes aren't enabled, the app shows a hint and keeps using the arrow keys.

### Quick actions

The quick actions panel in the app (button at the bottom center) uses the default game keys:
engine **E**, trailer **T**, activate **Enter**, flash lights **J**, wipers **P**, beacon **O**,
differential lock **V**, lift axle **U**.

Enjoy using!

![Screenshot](https://github.com/alexChurkin/TruckRemoteServer/raw/master/Screenshot.png)


## Building

.NET SDK 8+ is needed (Visual Studio 2022 works too). The server targets .NET Framework 4.7.2, which is built into Windows 10/11,
and can be built on any OS:

    dotnet build TruckRemoteServer.sln -c Release
    dotnet test tests/TruckRemoteServer.Tests/TruckRemoteServer.Tests.csproj

The result is in `src/TruckRemoteServer/bin/Release`.

### Architecture

- `src/TruckRemoteServer.Core` (.NET Standard 2.0) — the logic without Windows dependencies:
  the protocol, `ControllerServer` (UDP sessions of the phone), `ControllerInputMapper` (controller messages
  to keys and joystick axes), firewall rules, local addresses and `MainPresenter` of the main window.
  Platform services are interfaces (`IKeyboard`, `IVirtualJoystick`, `ITelemetrySource`, `IFirewall`, ...).
- `src/TruckRemoteServer` (.NET Framework 4.7.2, WinForms) — the application: `Program` is the composition root
  (Microsoft.Extensions.DependencyInjection), `UI/MainForm` is a passive view (MVP), `Infrastructure`
  has the Windows implementations (vJoy, SendInput, telemetry plugin, Windows Firewall, settings).
- `tests/TruckRemoteServer.Tests` — tests of the core, including real UDP sessions on the loopback interface.

## License

    Copyright 2021 Alex Churkin.

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.