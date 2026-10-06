using System.Collections.Generic;
using TruckRemoteServer.Input;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class PlayerBindingsTests
    {
        private static string Controls(params string[] mixes)
        {
            string content = "SiiNunit\r\n{\r\ncontrols : .controls {\r\n config_lines: " + mixes.Length + "\r\n";
            for (int i = 0; i < mixes.Length; i++) content += " config_lines[" + i + "]: \"mix " + mixes[i] + "\"\r\n";
            return content + "}\r\n}\r\n";
        }

        [Fact]
        public void RebindedKeysAreRead()
        {
            IReadOnlyDictionary<GameKey, KeyStroke> keys = PlayerBindings.Parse(Controls(
                "engine `keyboard.y?0 | semantical.engine?0`",
                "attach `semantical.attach?0 | keyboard.f8?0`",
                "lblinker `keyboard.num4?0`",
                "radionext `keyboard.pgdn?0`"));

            Assert.Equal(new KeyStroke(0x15), keys[GameKey.Engine]);
            Assert.Equal(new KeyStroke(0x42), keys[GameKey.Trailer]);
            Assert.Equal(new KeyStroke(0x4B), keys[GameKey.LeftBlinker]);
            Assert.Equal(new KeyStroke(0x51, extended: true), keys[GameKey.RadioNext]);
            Assert.False(keys.ContainsKey(GameKey.Wipers));
        }

        [Fact]
        public void KeyWithModifierIsReadWithIt()
        {
            KeyStroke key = PlayerBindings.FirstKey("joy.b4?0 | modifier(keyboard.lshift?0, keyboard.e?0)");

            Assert.Equal(new KeyStroke(0x12, modifier: new KeyStroke(0x2A)), key);
        }

        [Fact]
        public void ActionWithoutAKeyIsKnownToHaveNone()
        {
            IReadOnlyDictionary<GameKey, KeyStroke> keys = PlayerBindings.Parse(Controls(
                "wipers `joy.b2?0 | semantical.wipers?0`",
                "beacon ``",
                "lighthorn `long_press(keyboard.j?0)`",
                "diflock `keyboard.unknownkey?0`"));

            Assert.True(keys.ContainsKey(GameKey.Wipers));
            Assert.Null(keys[GameKey.Wipers]);
            Assert.Null(keys[GameKey.Beacon]);
            Assert.Null(keys[GameKey.LightHorn]);
            Assert.Null(keys[GameKey.DiffLock]);
        }

        [Fact]
        public void PlayerKeysComeFirstDefaultsForTheRest()
        {
            var bindings = new KeyBindings();
            bindings.Use(PlayerBindings.Parse(Controls("engine `keyboard.y?0`", "wipers `joy.b2?0`")));

            Assert.Equal(new KeyStroke(0x15), bindings.For(GameKey.Engine));
            Assert.Null(bindings.For(GameKey.Wipers));
            Assert.Equal(DefaultKeys.Keys[GameKey.Trailer], bindings.For(GameKey.Trailer));

            bindings.Use(null);
            Assert.Equal(DefaultKeys.Keys[GameKey.Engine], bindings.For(GameKey.Engine));
        }

        [Fact]
        public void DefaultKeysAreThoseOfTheGamesNamedKeys()
        {
            //The default bindings of the games, by the names their controls.sii uses
            var defaults = new Dictionary<GameKey, string>
            {
                { GameKey.Gas, "up" }, { GameKey.Brake, "down" }, { GameKey.LeftBlinker, "lbracket" },
                { GameKey.ParkingBrake, "space" }, { GameKey.Activate, "enter" }, { GameKey.RetarderUp, "semicolon" },
                { GameKey.RetarderDown, "apostrophe" }, { GameKey.CruiseUp, "period" }, { GameKey.CruiseResume, "slash" },
                { GameKey.QuickSave, "scroll" }, { GameKey.RadioPrevious, "pgup" }, { GameKey.LookLeft, "divide" },
                { GameKey.LookRight, "multiply" }, { GameKey.GearUp, "lshift" }, { GameKey.GearDown, "lctrl" },
                { GameKey.Menu, "escape" }, { GameKey.Screenshot, "f10" }, { GameKey.CameraInterior, "1" }
            };
            foreach (KeyValuePair<GameKey, string> key in defaults)
            {
                Assert.True(ScsKeyNames.TryGet(key.Value, out KeyStroke stroke), key.Value);
                Assert.Equal(DefaultKeys.Keys[key.Key], stroke);
            }
        }

        [Fact]
        public void EveryGameKeyHasADefaultKeyAndAMix()
        {
            foreach (GameKey key in System.Enum.GetValues<GameKey>())
            {
                Assert.True(DefaultKeys.Keys.ContainsKey(key), key.ToString());
                Assert.True(PlayerBindings.Mixes.ContainsKey(key), key.ToString());
            }
        }

        [Fact]
        public void NoBindingsInABrokenFile()
        {
            Assert.Empty(PlayerBindings.Parse(null));
            Assert.Empty(PlayerBindings.Parse("garbage"));
        }
    }
}
