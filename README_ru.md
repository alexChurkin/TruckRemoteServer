Truck Remote Server
====================
Сервер для [Truck Remote Control](https://github.com/alexChurkin/TruckRemoteControl), написанный при помощи C#.

### [ВИДЕОИНСТРУКЦИЯ](https://www.youtube.com/watch?v=ZbeGOxA96dY)

### [ЗАГРУЗИТЬ ПОСЛЕДНЮЮ ВЕРСИЮ](https://github.com/alexChurkin/TruckRemoteServer/releases)

### Поддерживаемые ОС
**Windows Vista, Windows 7, 8, 10 or 11 (64-bit)**.
Необходим **.NET Framework 4.7.2**.

### Поддерживаемые игры

- Euro Truck Simulator 2 (x64) версии 1.35+. Мультиплеерная версия также поддерживается.
- American Truck Simulator версии 1.35+

## Как установить

ВНИМАНИЕ: Не удаляйте файлы из папки **TruckRemoteServer**. Они необходимы для корректной работы сервера.

Запуск:
1) Установите **vJoy** на ваш ПК/ноутбук, запустив **vJoySetup.exe**
2) Откройте утилиту **Configure vjoy** и настройте 1-ый виртуальный джойстик, как показано на этом скриншоте:

    ![](https://github.com/alexChurkin/TruckRemoteServer/raw/master/Screenshot_vjoy_conf.png)
	
3) Скопируйте папку **TruckRemoteServer** в удобное для вас место
4) Запустите **TruckRemoteServer.exe** и установите предложенный программой плагин телеметрии для ETS2 и/или ATS

### Аналоговые педали (необязательно)

По умолчанию газ и тормоз эмулируются стрелками на клавиатуре (нажатие до упора). Чтобы управлять силой
нажатия (режим педалей **Аналоговый** в настройках приложения):
1) В **Configure vJoy** дополнительно включите оси **Y** и **Z** у 1-го устройства и нажмите **Apply**
2) В игре откройте *Настройки → Управление*, выберите устройство vJoy и назначьте **Газ** на ось Y,
   а **Тормоз** на ось Z (в отпущенном состоянии обе оси равны 0; при необходимости инвертируйте их в игре)

Если оси не включены, приложение покажет подсказку и продолжит использовать стрелки.

### Быстрые действия

Панель быстрых действий в приложении (кнопка внизу по центру) использует стандартные клавиши игры:
двигатель **E**, прицеп **T**, действие **Enter**, моргнуть фарами **J**, дворники **P**, маячки **O**,
блокировка дифференциала **V**, подъём оси **U**.

Приятного управления!

![Screenshot](https://github.com/alexChurkin/TruckRemoteServer/raw/master/Screenshot.png)


## Сборка

Нужен .NET SDK 8+ (или Visual Studio 2022). Сервер работает на .NET Framework 4.7.2, который встроен в Windows 10/11,
а собрать его можно на любой ОС:

    dotnet build TruckRemoteServer.sln -c Release
    dotnet test tests/TruckRemoteServer.Tests/TruckRemoteServer.Tests.csproj

Результат — в папке `src/TruckRemoteServer/bin/Release`.

### Архитектура

- `src/TruckRemoteServer.Core` (.NET Standard 2.0) — логика без зависимостей от Windows: протокол,
  `ControllerServer` (UDP-сессии телефона), `ControllerInputMapper` (сообщения контроллера в клавиши и оси джойстика),
  правила брандмауэра, локальные адреса и `MainPresenter` главного окна. Платформенные сервисы — интерфейсы
  (`IKeyboard`, `IVirtualJoystick`, `ITelemetrySource`, `IFirewall`, ...).
- `src/TruckRemoteServer` (.NET Framework 4.7.2, WinForms) — приложение: `Program` собирает зависимости
  (Microsoft.Extensions.DependencyInjection), `UI/MainForm` — пассивное представление (MVP), в `Infrastructure` —
  реализации для Windows (vJoy, SendInput, плагин телеметрии, брандмауэр Windows, настройки).
- `tests/TruckRemoteServer.Tests` — тесты ядра, в том числе настоящие UDP-сессии через loopback.

### Плагин телеметрии

Состояние грузовика (двигатель, свет, поворотники, дворники, прицеп) читается из
[scs-sdk-plugin](https://github.com/RenCloud/scs-sdk-plugin) 1.12.1 от RenCloud (лицензия MIT,
`src/TruckRemoteServer/Ets2Plugins`). Сервер устанавливает его как `bin\win_x64\plugins\scs-telemetry.dll` в ETS2 и ATS
и читает его разделяемую память `Local\SCSTelemetry` (ревизия 12, смещения — в `Core/Telemetry/ScsTelemetryMap`).

## Лицензия

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