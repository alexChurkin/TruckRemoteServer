using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TruckRemoteServer.Input
{
    //controls.sii of a game profile. Some actions of the controller's panel have no key in the default bindings
    //of the game, so their keys (see the keyboard implementation) are added to the bindings of the profile
    public static class GameControlsFile
    {
        //Game action -> the key (as the game names it) the server presses for it
        public static readonly IReadOnlyDictionary<string, string> Keys = new Dictionary<string, string>
        {
            { "cruiectrlinc", "period" },
            { "cruiectrldec", "comma" },
            { "cruiectrlres", "slash" }
        };

        //Returns the content with the missing keys added, or the same content if there is nothing to add.
        //A key the profile already uses for something else isn't added: the user's bindings aren't broken
        public static string AddMissingKeys(string content)
        {
            if (content == null || !content.StartsWith("SiiNunit", System.StringComparison.Ordinal)) return content;

            string result = content;
            foreach (KeyValuePair<string, string> binding in Keys)
            {
                string key = "keyboard." + binding.Value + "?";
                if (result.Contains(key)) continue;
                //config_lines[406]: "mix cruiectrlinc `semantical.cruiectrlinc?0`"
                var line = new Regex("(\"mix " + Regex.Escape(binding.Key) + " `)([^`\"\r\n]*)(`\")");
                result = line.Replace(result, match =>
                {
                    string expression = match.Groups[2].Value.Trim();
                    return match.Groups[1].Value + key + "0" + (expression.Length > 0 ? " | " + expression : "")
                        + match.Groups[3].Value;
                }, 1);
            }
            return result;
        }
    }
}
