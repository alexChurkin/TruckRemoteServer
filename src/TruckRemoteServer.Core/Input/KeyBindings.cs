using System.Collections.Generic;

namespace TruckRemoteServer.Input
{
    //The keys the server presses: the player's bindings of the profile being played (see PlayerBindings),
    //the default keys of the games for the rest. Updated from another thread while the keys are pressed
    public sealed class KeyBindings
    {
        private volatile IReadOnlyDictionary<GameKey, KeyStroke> player = new Dictionary<GameKey, KeyStroke>();

        //The key of the action; null: the player has no key for it, nothing is pressed
        public KeyStroke For(GameKey key)
        {
            if (player.TryGetValue(key, out KeyStroke bound)) return bound;
            return DefaultKeys.Keys.TryGetValue(key, out KeyStroke fallback) ? fallback : null;
        }

        public void Use(IReadOnlyDictionary<GameKey, KeyStroke> bindings)
        {
            player = bindings ?? new Dictionary<GameKey, KeyStroke>();
        }
    }
}
