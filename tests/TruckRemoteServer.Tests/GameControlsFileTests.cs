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

        [Fact]
        public void OtherFilesAreNotChanged()
        {
            string content = "mix cruiectrlinc `semantical.cruiectrlinc?0`";

            Assert.Same(content, GameControlsFile.AddMissingKeys(content));
            Assert.Null(GameControlsFile.AddMissingKeys(null));
        }
    }
}
