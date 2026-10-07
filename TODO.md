# TODO

- **Unbound keys of the main screen.** The buttons of the panel without a key in the player's profile are marked on
  the phone (the unbound actions message, revision 8); the blinkers, the lights, the horn and the cruise control of
  the main screen aren't yet. The names of the actions and keys follow controls.sii of ETS2 and ATS 1.61: a later
  version that renames one keeps its default key, so check `PlayerBindings.Mixes` and `ScsKeyNames` against a new
  profile after a game update.
- **Alternative: SCS SDK input device.** Since SDK 1.14 a plugin can be an input device of the game and trigger its
  actions directly, without keys and bindings (see [ETS2LA scs-sdk-controller](https://github.com/ETS2LA/scs-sdk-controller)).
  It would replace both vJoy and SendInput, but needs a native plugin of our own.
