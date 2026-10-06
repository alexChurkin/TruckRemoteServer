# TODO

- **Unbound actions on the phone.** The server presses the keys of the player's profile (`PlayerBindings`); an action
  the profile has without a key isn't pressed at all. The phone could show such buttons as "bind a key in the game".
  The mix names of the actions added in revision 5 (`mirrors`, `lookleft`, `radioprev`, `advzoom`, `assistance`, ...)
  are guesses: check them in a real controls.sii, an unknown name keeps the default key.
- **Alternative: SCS SDK input device.** Since SDK 1.14 a plugin can be an input device of the game and trigger its
  actions directly, without keys and bindings (see [ETS2LA scs-sdk-controller](https://github.com/ETS2LA/scs-sdk-controller)).
  It would replace both vJoy and SendInput, but needs a native plugin of our own.
