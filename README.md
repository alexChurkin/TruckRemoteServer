Truck Remote Server
====================
Server app for [Truck Remote Control](https://github.com/alexChurkin/TruckRemoteControl) written on C#.

#### [Russian guide](README_ru.md)

### [VIDEO INSTRUCTION](https://www.youtube.com/watch?v=qJOPYtYDHOo)

### [DOWNLOAD THE LATEST VERSION](https://github.com/alexChurkin/TruckRemoteServer/releases)

### Supported OS
**Windows 7 SP1, 8.1, 10 or 11 (64-bit)**.
**.NET Framework 4.7.2** or newer is required. It's built into Windows 10 (version 1803 and newer) and Windows 11,
on older systems install it from [Microsoft](https://dotnet.microsoft.com/download/dotnet-framework/net472).

### Supported games

- Euro Truck Simulator 2 (64-bit) version 1.35+. Multiplayer versions are supported as well.
- American Truck Simulator version 1.35+

## Setup

Launching:
1) Install **vJoy** on your PC by launching **vJoySetup.exe** (it's in the release archive)
2) Open **Configure vjoy**, configure 1-st virtual joystick as on this screenshot:

    ![](https://github.com/alexChurkin/TruckRemoteServer/raw/master/Screenshot_vjoy_conf.png)
	
3) Put **TruckRemoteServer.exe** wherever you want: it's a single file, everything it needs is inside
4) Launch **TruckRemoteServer.exe**. On the first start it:
   - installs the telemetry plugin into ETS2 and ATS (the games are looked for in all Steam libraries;
     if a game isn't found, the server asks for its folder, **Cancel** skips the game);
   - offers to allow the server in Windows Firewall (administrator rights are asked for), otherwise the firewall
     may block the phone. Later the window shows a link to do it if the server is still blocked.
5) Connect the phone to the same network as the PC and start Truck Remote Control: it finds the server by itself.
   If it doesn't, enter the address shown in the server window in the app settings.

The port, steering sensitivity and language (the language of Windows by default) are set in the server window.

### Analog pedals (optional)

By default gas and brake are emulated with the arrow keys (full press). To control the press force
(the **Analog** pedal mode in the app settings):
1) In **Configure vJoy** additionally enable the **Y** and **Z** axes of the 1-st device and press **Apply**
2) In the game, open *Options → Controls*, choose the vJoy device and bind **Throttle** to the Y axis
   and **Brake** to the Z axis (both are 0 when released; invert them in the game if needed)

If the axes aren't enabled, the app shows a hint and keeps using the arrow keys.

### Keys

The server presses the default game keys, so keep them in the game settings:
gas and brake **↑**/**↓** (in the digital pedal mode), blinkers **[** and **]**, hazard lights **F**,
parking brake **Space**, lights **L**, high beam **K**, horn **H**, air horn **N**, cruise control **C**.

The quick actions panel in the app (button at the bottom center) uses:
engine **E**, trailer **T**, activate **Enter**, flash lights **J**, wipers **P**, beacon **O**,
differential lock **V**, lift axle **U**.

Enjoy using!

![Screenshot](https://github.com/alexChurkin/TruckRemoteServer/raw/master/Screenshot.png)


## Building

.NET SDK 8+ is needed (Visual Studio 2022 works too). The server targets .NET Framework 4.7.2, which is built into Windows 10/11,
and can be built on any OS:

    dotnet build TruckRemoteServer.sln -c Release
    dotnet test tests/TruckRemoteServer.Tests/TruckRemoteServer.Tests.csproj

The result is a single `src/TruckRemoteServer/bin/Release/TruckRemoteServer.exe`: references, translations,
the native vJoy library and the telemetry plugin are embedded into it ([Costura](https://github.com/Fody/Costura)).
The process is 32-bit, as the vJoy libraries in `lib` are x86 only.

### Architecture

- `src/TruckRemoteServer.Core` (.NET Standard 2.0) — the logic without Windows dependencies:
  the protocol, `ControllerServer` (UDP sessions of the phone), `ControllerInputMapper` (controller messages
  to keys and joystick axes), the telemetry shared memory layout (`ScsTelemetryMap`), firewall rules,
  local addresses and `MainPresenter` of the main window.
  Platform services are interfaces (`IKeyboard`, `IVirtualJoystick`, `ITelemetrySource`, `IFirewall`, ...).
- `src/TruckRemoteServer` (.NET Framework 4.7.2, WinForms) — the application: `Program` is the composition root
  (Microsoft.Extensions.DependencyInjection), `UI/MainForm` is a passive view (MVP), `Infrastructure`
  has the Windows implementations (vJoy, SendInput, telemetry, Windows Firewall, settings), `Telemetry/Setup` installs
  the plugin into the games, `Localization` has the texts (English, Russian, Belarusian, Ukrainian).
- `tests/TruckRemoteServer.Tests` (.NET 8, xUnit) — tests of the core, including real UDP sessions on the loopback interface.

### Telemetry plugin

The truck state (engine, lights, blinkers, wipers, trailer) is read from
[scs-sdk-plugin](https://github.com/RenCloud/scs-sdk-plugin) 1.12.1 by RenCloud (MIT license,
`src/TruckRemoteServer/Ets2Plugins`). The server installs it as `bin\win_x64\plugins\scs-telemetry.dll` into ETS2 and ATS
and reads its shared memory `Local\SCSTelemetry` (revision 12, offsets are in `Core/Telemetry/ScsTelemetryMap`).

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