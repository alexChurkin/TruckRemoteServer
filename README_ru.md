Truck Remote Server
====================
Сервер для [Truck Remote Control](https://github.com/alexChurkin/TruckRemoteControl), написанный при помощи C#.

### [ВИДЕОИНСТРУКЦИЯ](https://www.youtube.com/watch?v=ZbeGOxA96dY)

### [ЗАГРУЗИТЬ ПОСЛЕДНЮЮ ВЕРСИЮ](https://github.com/alexChurkin/TruckRemoteServer/releases)

### Поддерживаемые ОС
**Windows 7 SP1, 8.1, 10 или 11 (64-bit)**.
Необходим **.NET Framework 4.7.2** или новее. Он встроен в Windows 10 (версии 1803 и новее) и Windows 11,
на более старых системах установите его с сайта [Microsoft](https://dotnet.microsoft.com/download/dotnet-framework/net472).

### Поддерживаемые игры

- Euro Truck Simulator 2 (x64) версии 1.35+. Мультиплеерная версия также поддерживается.
- American Truck Simulator версии 1.35+

## Как установить

Запуск:
1) Установите **vJoy** на ваш ПК/ноутбук, запустив **vJoySetup.exe** (он есть в архиве релиза)
2) Откройте утилиту **Configure vjoy** и настройте 1-ый виртуальный джойстик, как показано на этом скриншоте:

    ![](https://github.com/alexChurkin/TruckRemoteServer/raw/master/Screenshot_vjoy_conf.png)
	
3) Положите **TruckRemoteServer.exe** в удобное для вас место: это один файл, всё нужное уже внутри
4) Запустите **TruckRemoteServer.exe**. При первом запуске он:
   - устанавливает плагин телеметрии в ETS2 и ATS (игры ищутся во всех библиотеках Steam; если игра не найдена,
     сервер попросит указать её папку, **Отмена** пропускает игру);
   - предлагает разрешить сервер в брандмауэре Windows (потребуются права администратора), иначе брандмауэр
     может блокировать телефон. Позже, если сервер всё ещё заблокирован, в окне будет ссылка для этого.
5) Подключите телефон к той же сети, что и ПК, и запустите Truck Remote Control: он сам найдёт сервер.
   Если не нашёл, укажите в настройках приложения адрес, показанный в окне сервера.

Порт, чувствительность руля и язык (по умолчанию — язык Windows) настраиваются в окне сервера.

### Аналоговые педали (необязательно)

По умолчанию газ и тормоз эмулируются стрелками на клавиатуре (нажатие до упора). Чтобы управлять силой
нажатия (режим педалей **Аналоговый** в настройках приложения):
1) В **Configure vJoy** дополнительно включите оси **Y** и **Z** у 1-го устройства и нажмите **Apply**
2) В игре откройте *Настройки → Управление*, выберите устройство vJoy и назначьте **Газ** на ось Y,
   а **Тормоз** на ось Z (в отпущенном состоянии обе оси равны 0; при необходимости инвертируйте их в игре)

Если оси не включены, приложение покажет подсказку и продолжит использовать стрелки.

### Клавиши

Сервер нажимает стандартные клавиши игры, поэтому не меняйте их в настройках игры:
газ и тормоз **↑**/**↓** (в цифровом режиме педалей), поворотники **[** и **]**, аварийка **F**,
стояночный тормоз **Пробел**, свет **L**, дальний свет **K**, гудок **H**, пневмогудок **N**, круиз-контроль **C**.

Панель быстрых действий в приложении (кнопка внизу по центру) использует:
двигатель **E**, прицеп **T**, действие **Enter**, моргнуть фарами **J**, дворники **P**, маячки **O**,
блокировка дифференциала **V**, подъём оси **U**.

Приятного управления!

![Screenshot](https://github.com/alexChurkin/TruckRemoteServer/raw/master/Screenshot.png)


## Сборка

Нужен .NET SDK 8+ (или Visual Studio 2022). Сервер работает на .NET Framework 4.7.2, который встроен в Windows 10/11,
а собрать его можно на любой ОС:

    dotnet build TruckRemoteServer.sln -c Release
    dotnet test tests/TruckRemoteServer.Tests/TruckRemoteServer.Tests.csproj

Результат — один файл `src/TruckRemoteServer/bin/Release/TruckRemoteServer.exe`: зависимости, переводы,
нативная библиотека vJoy и плагин телеметрии встроены в него ([Costura](https://github.com/Fody/Costura)).
Процесс 32-битный, так как библиотеки vJoy в `lib` есть только для x86.

### Архитектура

- `src/TruckRemoteServer.Core` (.NET Standard 2.0) — логика без зависимостей от Windows: протокол,
  `ControllerServer` (UDP-сессии телефона), `ControllerInputMapper` (сообщения контроллера в клавиши и оси джойстика),
  раскладка разделяемой памяти телеметрии (`ScsTelemetryMap`), правила брандмауэра, локальные адреса
  и `MainPresenter` главного окна. Платформенные сервисы — интерфейсы
  (`IKeyboard`, `IVirtualJoystick`, `ITelemetrySource`, `IFirewall`, ...).
- `src/TruckRemoteServer` (.NET Framework 4.7.2, WinForms) — приложение: `Program` собирает зависимости
  (Microsoft.Extensions.DependencyInjection), `UI/MainForm` — пассивное представление (MVP), в `Infrastructure` —
  реализации для Windows (vJoy, SendInput, телеметрия, брандмауэр Windows, настройки), `Telemetry/Setup` устанавливает
  плагин в игры, в `Localization` — тексты (английский, русский, белорусский, украинский).
- `tests/TruckRemoteServer.Tests` (.NET 8, xUnit) — тесты ядра, в том числе настоящие UDP-сессии через loopback.

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