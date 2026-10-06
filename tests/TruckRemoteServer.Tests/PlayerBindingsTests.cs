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

        //The lines of a profile of American Truck Simulator 1.61 with the default bindings, after the server
        //has added the keys of the cruise control; Euro Truck Simulator 2 has the same ones
        private static readonly string RealProfile = Controls(
                "dforward `keyboard.uarrow?0 | keyboard.w?0`",
                "dbackward `keyboard.darrow?0 | keyboard.s?0`",
                "lblinker `keyboard.lbracket?0 | semantical.lblinker?0`",
                "rblinker `keyboard.rbracket?0 | semantical.rblinker?0`",
                "flasher4way `keyboard.f?0 | semantical.flasher4way?0`",
                "parkingbrake `keyboard.space?0 | semantical.parkingbrake?0`",
                "light `keyboard.l?0 | semantical.light?0`",
                "hblight `keyboard.k?0 | semantical.hblight?0`",
                "horn `keyboard.h?0 | semantical.horn?0`",
                "airhorn `keyboard.n?0 | semantical.airhorn?0`",
                "cruiectrl `keyboard.c?0 | semantical.cruiectrl?0`",
                "engine `keyboard.e?0 | semantical.engine?0`",
                "attach `keyboard.t?0 | semantical.attach?0`",
                "activate `keyboard.enter?0 | keyboard.numenter?0 | semantical.activate?0`",
                "wipers `keyboard.p?0 | semantical.wipers?0`",
                "diflock `keyboard.v?0 | semantical.diflock?0`",
                "liftaxle `keyboard.u?0 | semantical.liftaxle?0`",
                "beacon `keyboard.o?0 | semantical.beacon?0`",
                "lighthorn `keyboard.j?0 | semantical.lighthorn?0`",
                "retarderup `keyboard.semicolon?0 | semantical.retarderup?0`",
                "retarderdown `keyboard.apostrophe?0 | semantical.retarderdown?0`",
                "motorbrake `keyboard.b?0 | semantical.motorbrake?0`",
                "cruiectrlinc `keyboard.period?0 | semantical.cruiectrlinc?0`",
                "cruiectrldec `keyboard.comma?0 | semantical.cruiectrldec?0`",
                "cruiectrlres `keyboard.slash?0 | semantical.cruiectrlres?0`",
                "quickpark `keyboard.q?0 | semantical.quickpark?0`",
                "cam1 `keyboard.key1?0 | semantical.cam1?0`",
                "cam2 `keyboard.key2?0 | semantical.cam2?0`",
                "cam3 `keyboard.key3?0 | semantical.cam3?0`",
                "cam4 `keyboard.key4?0 | semantical.cam4?0`",
                "cam5 `keyboard.key5?0 | semantical.cam5?0`",
                "cam6 `keyboard.key6?0 | semantical.cam6?0`",
                "cam7 `keyboard.key7?0 | semantical.cam7?0`",
                "cam8 `keyboard.key8?0 | semantical.cam8?0`",
                "camcycle `keyboard.key9?0 | semantical.camcycle?0`",
                "navmap `keyboard.m?0 | semantical.navmap?0`",
                "display `keyboard.i?0 | semantical.display?0`",
                "showhud `keyboard.f3?0 | semantical.showhud?0`",
                "radionext `keyboard.pgdn?0 | semantical.radionext?0`",
                "radioprev `keyboard.pgup?0 | semantical.radioprev?0`",
                "radio `keyboard.r?0 | semantical.radio?0`",
                "quicksave `keyboard.scrollock?0 | semantical.quicksave?0`",
                "showmirrors `keyboard.f2?0 | semantical.showmirrors?0`",
                "lookleft `keyboard.numslash?0 | semantical.lookleft?0`",
                "lookright `keyboard.nummultiply?0 | semantical.lookright?0`",
                "gearup `keyboard.lshift?0 | keyboard.rshift?0 | semantical.gearup?0`",
                "geardown `keyboard.lctrl?0 | keyboard.rctrl?0 | semantical.geardown?0`",
                "advzoomout `keyboard.f5?0 | semantical.advzoomout?0`",
                "advoptions `keyboard.f6?0 | semantical.advoptions?0`",
                "services `keyboard.f7?0 | semantical.services?0`",
                "screenshot `keyboard.f10?0 | semantical.screenshot?0`",
                "menu `keyboard.esc?0 | semantical.menu?0`"
            );

        [Fact]
        public void DefaultBindingsOfARealProfileAreTheDefaultKeys()
        {
            IReadOnlyDictionary<GameKey, KeyStroke> keys = PlayerBindings.Parse(RealProfile);

            foreach (GameKey key in System.Enum.GetValues(typeof(GameKey)))
            {
                Assert.True(keys.ContainsKey(key), key + " isn't found in the profile");
                Assert.True(DefaultKeys.Keys[key].Equals(keys[key]), key + ": " + keys[key] + " instead of " + DefaultKeys.Keys[key]);
            }
        }

        [Fact]
        public void ModifierOfTheGameIsRead()
        {
            //As the games write a key with a modifier: the first argument is a mix of the held modifiers
            Assert.Equal(new KeyStroke(0x12, modifier: new KeyStroke(0x2A)), PlayerBindings.FirstKey("modifier(shift_only, keyboard.e?0)"));
            Assert.Equal(new KeyStroke(0x43, modifier: new KeyStroke(0x1D)), PlayerBindings.FirstKey("modifier(ctrl_only, keyboard.f9?0)"));
            Assert.Equal(new KeyStroke(0x2C, modifier: new KeyStroke(0x38)), PlayerBindings.FirstKey("modifier(alt_only, keyboard.z?0)"));
            Assert.Equal(new KeyStroke(0x2E), PlayerBindings.FirstKey("modifier(no_modifier, keyboard.c?0) | semantical.j_tr_cam_swi?0"));
            //Two modifiers at once aren't pressed
            Assert.Null(PlayerBindings.FirstKey("modifier(ctr_shf, keyboard.f12?0)"));
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
