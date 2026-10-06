using TruckRemoteServer.Input;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class GameControlsFileTests
    {
        private static string Controls(params string[] mixes)
        {
            string content = "SiiNunit\r\n{\r\ninput_config : _nameless.1 {\r\n config_lines: " + mixes.Length + "\r\n";
            for (int i = 0; i < mixes.Length; i++) content += " config_lines[" + i + "]: \"mix " + mixes[i] + "\"\r\n";
            return content + "}\r\n\r\n}\r\n";
        }

        [Fact]
        public void KeysAreAddedToActionsWithoutThem()
        {
            string content = Controls(
                "cruiectrl `keyboard.c?0 | semantical.cruiectrl?0`",
                "cruiectrlinc `semantical.cruiectrlinc?0`",
                "cruiectrldec `semantical.cruiectrldec?0`",
                "cruiectrlres ``");

            string expected = Controls(
                "cruiectrl `keyboard.c?0 | semantical.cruiectrl?0`",
                "cruiectrlinc `keyboard.period?0 | semantical.cruiectrlinc?0`",
                "cruiectrldec `keyboard.comma?0 | semantical.cruiectrldec?0`",
                "cruiectrlres `keyboard.slash?0`");
            Assert.Equal(expected, GameControlsFile.AddMissingKeys(content));
        }

        [Fact]
        public void ChangedControlsAreNotChangedAgain()
        {
            string changed = GameControlsFile.AddMissingKeys(Controls("cruiectrlinc `semantical.cruiectrlinc?0`"));

            Assert.Same(changed, GameControlsFile.AddMissingKeys(changed));
        }

        [Fact]
        public void KeyUsedByAnotherActionIsNotAdded()
        {
            string content = Controls(
                "wipers `keyboard.period?0 | semantical.wipers?0`",
                "cruiectrlinc `semantical.cruiectrlinc?0`");

            Assert.Same(content, GameControlsFile.AddMissingKeys(content));
        }

        private const string VJoy = "di8.'{A17053A0-C03C-11F1-8002-444553540000}|{BEAD1234-0000-0000-0000-504944564944}'";

        //Lines of the first block of controls.sii (devices, inputs, constants)
        private static string Profile(params string[] lines)
        {
            string content = "SiiNunit\r\n{\r\ninput_config : _nameless.1 {\r\n config_lines: " + lines.Length + "\r\n";
            for (int i = 0; i < lines.Length; i++) content += " config_lines[" + i + "]: \"" + lines[i] + "\"\r\n";
            return content + "}\r\n\r\n}\r\n";
        }

        [Fact]
        public void VJoyIsFoundAmongTheControllersOfTheGame()
        {
            string global = "SiiNunit\r\n{\r\n display_names[2]: \"`di8.keyboard`|@@keyboard@@\"\r\n"
                + " display_names[4]: \"`" + VJoy + "`|vJoy Device\"\r\n}\r\n";

            Assert.Equal(VJoy, GameControlsFile.FindVJoyDevice(global));
            Assert.Null(GameControlsFile.FindVJoyDevice("SiiNunit\r\n{\r\n display_names[2]: \"`di8.keyboard`|@@keyboard@@\"\r\n}"));
            Assert.Null(GameControlsFile.FindVJoyDevice(null));
        }

        [Fact]
        public void NewProfileGetsVJoyWithSeparatePedalAxes()
        {
            //The defaults of the game: no joystick, both pedals on one axis (zero in the middle, inverted gas)
            string content = Profile(
                "device joy ``",
                "device joy2 ``",
                "input j_steer `joy.x`",
                "input j_throttle `joy.y`",
                "input j_brake `joy.y`",
                "input j_clutch ``",
                "constant c_jzthrottle 1.000000",
                "constant c_jithrottle 1.000000",
                "constant c_jzbrake 1.000000",
                "constant c_jibrake 0.000000",
                "constant c_jiclutch 1.000000",
                "constant c_relatsteer 1.000000");

            string expected = Profile(
                "device joy `" + VJoy + "`",
                "device joy2 ``",
                "input j_steer `joy.x`",
                "input j_throttle `joy.y`",
                "input j_brake `joy.z`",
                "input j_clutch ``",
                "constant c_jzthrottle 0.000000",
                "constant c_jithrottle 0.000000",
                "constant c_jzbrake 0.000000",
                "constant c_jibrake 0.000000",
                "constant c_jiclutch 1.000000",
                "constant c_relatsteer 0.000000");
            string changed = GameControlsFile.SetUpJoystick(content, VJoy);
            Assert.Equal(expected, changed);
            Assert.Same(changed, GameControlsFile.SetUpJoystick(changed, VJoy));
        }

        [Fact]
        public void VJoyChosenInTheGameGetsSeparatePedalAxes()
        {
            string content = Profile(
                "device joy `" + VJoy + "`",
                "input j_steer `joy.x`",
                "input j_throttle `joy.y`",
                "input j_brake `joy.y`",
                "constant c_jzthrottle 1.000000",
                "constant c_jzbrake 1.000000");

            string changed = GameControlsFile.SetUpJoystick(content, VJoy);

            Assert.Contains("input j_brake `joy.z`", changed);
            Assert.Contains("constant c_jzthrottle 0.000000", changed);
            Assert.Contains("constant c_jzbrake 0.000000", changed);
        }

        [Fact]
        public void ProfileSetUpByTheUserIsNotChanged()
        {
            //Another controller is the joystick of the game
            string wheel = Profile(
                "device joy `di8.'{11111111-0000-0000-0000-000000000000}|{C24F046D-0000-0000-0000-504944564944}'`",
                "input j_steer `joy.x`",
                "input j_throttle `joy.y`",
                "input j_brake `joy.y`",
                "constant c_jzthrottle 1.000000");
            Assert.Same(wheel, GameControlsFile.SetUpJoystick(wheel, VJoy));

            //vJoy with the axes the user chose
            string own = Profile(
                "device joy `" + VJoy + "`",
                "input j_steer `joy.x`",
                "input j_throttle `joy.rz`",
                "input j_brake `joy.z`",
                "constant c_jzthrottle 1.000000",
                "constant c_relatsteer 0.000000");
            Assert.Same(own, GameControlsFile.SetUpJoystick(own, VJoy));

            //The game hasn't seen vJoy
            string fresh = Profile("device joy ``", "input j_throttle `joy.y`", "input j_brake `joy.y`");
            Assert.Same(fresh, GameControlsFile.SetUpJoystick(fresh, null));
        }

        [Fact]
        public void OtherFilesAreNotChanged()
        {
            string content = "mix cruiectrlinc `semantical.cruiectrlinc?0`";

            Assert.Same(content, GameControlsFile.AddMissingKeys(content));
            Assert.Null(GameControlsFile.AddMissingKeys(null));
        }
    }
}
