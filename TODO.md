# TODO

- **Telemetry plugin.** `src/TruckRemoteServer/Ets2Plugins/win_x64/plugins/ets2-telemetry-server.dll` (Funbit's ets2-telemetry-server) is old.
  Check that it still provides all used values (engine, lights, blinkers, wipers, beacon, trailer) with current ETS2 and ATS,
  and consider switching to a maintained SCS SDK plugin (e.g. RenCloud's scs-sdk-plugin). The reader
  (`src/TruckRemoteServer/Telemetry/Data/Reader`, adapted to the core by `Infrastructure/Ets2TelemetrySource`) depends on the plugin's shared memory layout and would have to be changed with it.
