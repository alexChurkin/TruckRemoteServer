using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TruckRemoteServer.Input
{
    //controls.sii of a game profile. A new profile can't be driven by the server as it is: the joystick of the game
    //isn't chosen (the steering and the analog pedals come through vJoy), the pedals share one axis, and some actions
    //of the controller's panel have no key. The server puts that right in the profiles
    public static class GameControlsFile
    {
        //Game action -> the key (as the game names it) the server presses for it
        public static readonly IReadOnlyDictionary<string, string> Keys = new Dictionary<string, string>
        {
            { "cruiectrlinc", "period" },
            { "cruiectrldec", "comma" },
            { "cruiectrlres", "slash" }
        };

        //The axes of vJoy device 1 as the server sets them (see the joystick implementation)
        private const string SteeringAxis = "joy.x";
        private const string GasAxis = "joy.y";
        private const string BrakeAxis = "joy.z";

        //A pedal axis is 0 when released and isn't inverted
        private static readonly string[] PedalConstants = { "c_jzthrottle", "c_jithrottle", "c_jzbrake", "c_jibrake" };

        private const string RelativeSteering = "c_relatsteer";

        //vJoy as the game names it in global_controls.sii: its instance and product ids (the product id is the same
        //everywhere, the instance id depends on the PC)
        private static readonly Regex VJoyDevice = new Regex(@"di8\.'\{[0-9A-Fa-f-]+\}\|\{BEAD1234-[0-9A-Fa-f-]+\}'");

        //Everything the server needs in a profile; returns the same content if there is nothing to change.
        //vJoyDevice is null if the game hasn't seen vJoy (see FindVJoyDevice): only the keys are added then
        public static string SetUp(string content, string vJoyDevice)
        {
            return SetUpJoystick(AddMissingKeys(content), vJoyDevice);
        }

        //The name of the vJoy device in global_controls.sii of the game, null if the game hasn't seen it
        public static string FindVJoyDevice(string globalControls)
        {
            if (globalControls == null) return null;
            Match match = VJoyDevice.Match(globalControls);
            return match.Success ? match.Value : null;
        }

        //Returns the content with the missing keys added, or the same content if there is nothing to add.
        //A key the profile already uses for something else isn't added: the user's bindings aren't broken
        public static string AddMissingKeys(string content)
        {
            if (!IsControls(content)) return content;

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

        //Makes vJoy the joystick of a profile that has none, with the steering, the gas and the brake on their axes.
        //A profile with another controller isn't touched; one that already has vJoy keeps its axes, except
        //the default "both pedals on one axis", which can't work with the server, and gets the absolute steering
        public static string SetUpJoystick(string content, string vJoyDevice)
        {
            if (!IsControls(content) || string.IsNullOrEmpty(vJoyDevice)) return content;

            string device = Value(content, "device joy");
            if (device == null) return content;
            bool noDevice = device.Length == 0;
            bool ours = string.Equals(device, vJoyDevice, StringComparison.OrdinalIgnoreCase);
            if (!noDevice && !ours) return content;

            string result = content;
            if (noDevice) result = SetValue(result, "device joy", vJoyDevice);
            if (noDevice || string.IsNullOrEmpty(Value(result, "input j_steer")))
            {
                result = SetValue(result, "input j_steer", SteeringAxis);
            }
            //The steering of the phone is the position of the wheel. In the relative mode (the default of a new
            //profile) the game only turns its wheel towards it at a limited speed: smooth, but slow and late
            result = SetConstant(result, RelativeSteering, "0.000000");

            bool pedalsOnOneAxis = Value(content, "input j_throttle") == Value(content, "input j_brake");
            if (noDevice || pedalsOnOneAxis)
            {
                result = SetValue(result, "input j_throttle", GasAxis);
                result = SetValue(result, "input j_brake", BrakeAxis);
                foreach (string constant in PedalConstants) result = SetConstant(result, constant, "0.000000");
            }
            return result == content ? content : result;
        }

        private static string SetConstant(string content, string name, string value)
        {
            return new Regex("(\"constant " + name + " )[-0-9.]+(\")").Replace(content, "${1}" + value + "${2}", 1);
        }

        private static bool IsControls(string content)
        {
            return content != null && content.StartsWith("SiiNunit", StringComparison.Ordinal);
        }

        //config_lines[2]: "device joy `...`" -> the text between the backquotes, null if there is no such line
        private static string Value(string content, string name)
        {
            Match match = ValueLine(name).Match(content);
            return match.Success ? match.Groups[2].Value : null;
        }

        private static string SetValue(string content, string name, string value)
        {
            return ValueLine(name).Replace(content, match => match.Groups[1].Value + value + match.Groups[3].Value, 1);
        }

        private static Regex ValueLine(string name)
        {
            return new Regex("(\"" + Regex.Escape(name) + " `)([^`\"\r\n]*)(`\")");
        }
    }
}
