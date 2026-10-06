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
1) Put **TruckRemoteServer.exe** wherever you want: it's a single file, everything it needs is inside
2) Launch **TruckRemoteServer.exe**. It installs the telemetry plugin into ETS2 and ATS by itself (the games are
   looked for in all Steam libraries; if a game isn't found, the server asks for its folder, **Cancel** skips it).
   On the first start the **setup wizard** shows what the server needs, each part with its state and a button
   that fixes it (later it's in **Settings → Setup wizard…**):
   - the **telemetry plugin** (the dashboard and the vibration of the phone);
   - **vJoy**, the virtual joystick driver the phone steers through: vJoy 2.1.9.1 is inside the server, it's
     installed silently and its 1-st device is configured (administrator rights are asked for, Windows may ask
     to trust the vJoy driver);
   - **Windows Firewall**: the server is allowed in it (administrator rights are asked for), otherwise it may
     block the phone;
   - the **game controls**, set up by the server (see below);
   - the **phone**: the QR code of the server and, once the phone is connected, its steering and pedals as bars,
     so the controls can be tried before the game is started.
   Without the wizard the main window shows an **Install** button while vJoy isn't ready and a link while the
   firewall blocks the server.
3) Connect the phone to the same network as the PC and start Truck Remote Control: it finds the server by itself.
   If it doesn't, scan the QR code of the server window in the app settings (or enter the address shown there).

The steering (sensitivity, dead zone, curve) is set in the app. The **Settings** menu of the server window has only
the port, the language (the language of Windows by default), **Minimize to the notification area** (off by default),
the setup wizard and **Check for updates**. The server checks for a newer version by itself at the start and offers it
in its window: **Update** replaces the exe and restarts the server.

A tablet or a second phone can show the instruments of the truck beside the phone that drives (the **Dashboard** mode of
the app): it finds the server like the controller does, up to 4 of them at once, and controls nothing.

To set vJoy up manually (e.g. if the automatic setup failed), install **vJoySetup.exe** from the
[vJoy project](https://sourceforge.net/projects/vjoystick/files/Beta%202.x/2.1.9.1-160719/) and configure the 1-st device
in **Configure vJoy** as on this screenshot:

![](https://github.com/alexChurkin/TruckRemoteServer/raw/master/Screenshot_vjoy_conf.png)

### Analog pedals (optional)

By default gas and brake are emulated with the arrow keys (full press). To control the press force
(the **Analog** pedal mode in the app settings):
1) In **Configure vJoy** additionally enable the **Y** and **Z** axes of the 1-st device and press **Apply**
2) In the game, open *Options → Controls*, choose the vJoy device and bind **Throttle** to the Y axis
   and **Brake** to the Z axis (both are 0 when released; invert them in the game if needed)

If the axes aren't enabled, the app shows a hint and keeps using the arrow keys.

### Keys

The server presses the keys bound in the profile played last (it reads `controls.sii` of the profile, also a key with
Shift, Ctrl or Alt), so keys changed in the game settings keep working; an action without a key in the profile isn't
pressed. An action the profile doesn't have gets the default key of the game:
gas and brake **↑**/**↓** (in the digital pedal mode), blinkers **[** and **]**, hazard lights **F**,
parking brake **Space**, lights **L**, high beam **K**, horn **H**, air horn **N**, cruise control **C**.

The quick actions panel in the app (button at the bottom center, swipe it for more pages) uses:

- truck: engine **E**, trailer **T**, activate **Enter** and flash lights **J** (both held while the button is pressed), wipers **P**, beacon **O**,
  differential lock **V**, lift axle **U**;
- driving: retarder **;** / **'**, engine brake **B** (held while the button is pressed), quick park **Q**,
  cruise control speed **.** / **,**, resume cruise control **/**;
- view: cab camera **1**, chase camera **2**, next camera **9**, map **M**, dashboard display **I**, HUD **F3**,
  next radio station **Page Down**, quick save **Scroll Lock**;
- more cameras: top **3**, roof **4**, window **5**, bumper **6**, wheel **7**, drive-by **8**, mirrors **F2**,
  screenshot **F10**;
- looking around and gears: look left **Numpad /** and right **Numpad \*** (held while the button is pressed),
  gear up **Shift** and down **Ctrl**, previous radio station **Page Up**, radio **R**,
  route advisor **F5** and **F6**, road assistance **F7**, menu **Esc**.

The games have no default keys for the cruise control speed and resume, so the server adds these keys to the key bindings
of every game profile (`controls.sii`, the original is kept beside it as `controls.truckremote.bak`). A key the profile
already uses isn't added: bind the action in the game then.

In the same file the server makes vJoy the controller of a profile that has none: the steering on the X axis, the gas
on Y and the brake on Z (a new profile has no controller and both pedals on one axis). A profile with another controller
or with your own axes isn't touched.

A running game writes its bindings on exit, so its profiles are changed when it's closed: after the first start of
a game with a new profile, close the game once while the server is running and start it again.

Enjoy using!

![Screenshot](https://github.com/alexChurkin/TruckRemoteServer/raw/master/Screenshot.png)


## Building

.NET SDK 8+ is needed (Visual Studio 2022 works too). The server targets .NET Framework 4.7.2, which is built into Windows 10/11,
and can be built on any OS:

    dotnet build TruckRemoteServer.sln -c Release
    dotnet test tests/TruckRemoteServer.Tests/TruckRemoteServer.Tests.csproj

Code checks (all of them run in CI and fail the build):

- [.NET code analyzers](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview)
  (`latest-recommended` rules) and the code style of `.editorconfig` (naming, unused code, formatting)
  are checked by every build, warnings are errors (`Directory.Build.props`)
- `dotnet format TruckRemoteServer.sln --verify-no-changes` checks formatting, `dotnet format TruckRemoteServer.sln` fixes it

The result is a single `src/TruckRemoteServer/bin/Release/TruckRemoteServer.exe`: references, translations,
the native vJoy library, the vJoy setup and the telemetry plugin are embedded into it ([Costura](https://github.com/Fody/Costura)).
The process is 32-bit, as the vJoy libraries in `lib` are x86 only.

### Releases and updates

The server checks the latest release on GitHub at the start (and from **Settings → Check for updates**) and offers
a newer version in its window: **Update** downloads `TruckRemoteServer.exe` of the release, puts it in place of the
running one and restarts the server (a folder that can't be written, e.g. Program Files, opens the release page instead).
To release a version, change `AssemblyVersion` in `src/TruckRemoteServer/Properties/AssemblyInfo.cs`, then publish
a release with the same tag (`1.3` or `v1.3`): the Release workflow builds it, checks the version and attaches the exe.

### Architecture

- `src/TruckRemoteServer.Core` (.NET Standard 2.0) — the logic without Windows dependencies:
  the protocol, `ControllerServer` (UDP sessions of the phone), `ControllerInputMapper` (controller messages
  to keys and joystick axes), the telemetry shared memory layout (`ScsTelemetryMap`), firewall rules,
  local addresses and `MainPresenter` of the main window.
  Platform services are interfaces (`IKeyboard`, `IVirtualJoystick`, `ITelemetrySource`, `IFirewall`, ...).
- `src/TruckRemoteServer` (.NET Framework 4.7.2, WinForms) — the application: `Program` is the composition root
  (Microsoft.Extensions.DependencyInjection), `UI/MainForm` is a passive view (MVP), `Infrastructure`
  has the Windows implementations (vJoy, SendInput, telemetry, Windows Firewall, settings), `Infrastructure/VJoySetup`
  installs and configures vJoy (in the same exe started with administrator rights), `Telemetry/Setup` installs
  the plugin into the games, `Localization` has the texts (English, Russian, Belarusian, Ukrainian).
- `tests/TruckRemoteServer.Tests` (.NET 8, xUnit) — tests of the core, including real UDP sessions on the loopback interface.

### Protocol

The phone and the server talk over UDP. Since version 2 (the phone's hello is `TruckRemoteHello2`, the server answers
`Hi!2`) the messages are binary: the phone's state is 16 bytes plus 2 bytes per clicked or held action, the truck state
is 22 bytes and more with the dashboard of the app: speed, speed limit, cruise speed, gear, engine rpm, fuel
(`Core/Protocol/BinaryProtocol`). Actions are sent by fixed codes, not by the places of their buttons.
Older apps and servers keep the text protocol.
The server state grows by revisions (its revision byte tells the app what it may read): the warnings, the route and
the job, the fuel range, and the haptics — the road vibration, the surface under the wheels and the counters of the
haptic events (bumps, collisions, the engine starting and stopping, the blinker relay, gear shifts, the retarder, the
trailer coupling, fines, deliveries...) found by `Core/Haptics/HapticDetector` in the telemetry. A dashboard (viewer)
says `TruckRemoteViewer2` and gets the state and the job without controlling anything. The format of every message is described in `BinaryProtocol`.

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