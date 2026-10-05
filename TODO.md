# TODO

- **Player's key bindings.** The server presses the default ETS2/ATS keys (`SendInputKeyboard`), so a button doesn't work
  if the player has changed its key or the action has no key by default. Read the bindings of the active profile instead:
  `Documents\Euro Truck Simulator 2\profiles\<profile>\controls.sii` (also `steam_profiles`, and the same for ATS).
  Lines look like `config_lines[381]: "mix cruiectrlinc `keyboard.equal?0 | semantical.cruiectrlinc?0`"`:
  take the first `keyboard.<key>` of a mix (`modifier(...)`, `long_press(...)` and joystick inputs need care),
  map key names to scan codes, and fall back to the defaults. Mix names of the panel's actions: `engine`, `attach`,
  `activate`, `lighthorn`, `wipers`, `beacon`, `diflock`, `liftaxle`, `retarderup`, `retarderdown`, `motorbrake`,
  `cruiectrl`, `cruiectrlinc`, `cruiectrldec`, `cruiectrlres`, `quickpark`, `cam1`, `cam2`, `camcycle`, `navmap`,
  `display`, `showhud`, `radionext`, `quicksave`. Actions without a key could be reported to the phone
  ("bind a key in the game").
- **Alternative: SCS SDK input device.** Since SDK 1.14 a plugin can be an input device of the game and trigger its
  actions directly, without keys and bindings (see [ETS2LA scs-sdk-controller](https://github.com/ETS2LA/scs-sdk-controller)).
  It would replace both vJoy and SendInput, but needs a native plugin of our own.
