using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TruckRemoteServer.Input
{
    //The keys the player has bound to the actions in controls.sii of a profile.
    //A line looks like: config_lines[381]: "mix engine `keyboard.e?0 | semantical.engine?0`"
    public static class PlayerBindings
    {
        //The mixes (actions of the game) of the keys the server presses, as controls.sii of ETS2 and ATS 1.61 names
        //them. The gas and the brake are the digital mixes: the analog ones are the axes of the joystick.
        //An action that isn't found keeps its default key
        public static readonly IReadOnlyDictionary<GameKey, string> Mixes = new Dictionary<GameKey, string>
        {
            { GameKey.Gas, "dforward" },
            { GameKey.Brake, "dbackward" },
            { GameKey.LeftBlinker, "lblinker" },
            { GameKey.RightBlinker, "rblinker" },
            { GameKey.HazardLights, "flasher4way" },
            { GameKey.ParkingBrake, "parkingbrake" },
            { GameKey.Lights, "light" },
            { GameKey.HighBeam, "hblight" },
            { GameKey.Horn, "horn" },
            { GameKey.AirHorn, "airhorn" },
            { GameKey.CruiseControl, "cruiectrl" },
            { GameKey.Engine, "engine" },
            { GameKey.Trailer, "attach" },
            { GameKey.Activate, "activate" },
            { GameKey.Wipers, "wipers" },
            { GameKey.DiffLock, "diflock" },
            { GameKey.LiftAxle, "liftaxle" },
            { GameKey.Beacon, "beacon" },
            { GameKey.LightHorn, "lighthorn" },
            { GameKey.RetarderUp, "retarderup" },
            { GameKey.RetarderDown, "retarderdown" },
            { GameKey.EngineBrake, "motorbrake" },
            { GameKey.CruiseUp, "cruiectrlinc" },
            { GameKey.CruiseDown, "cruiectrldec" },
            { GameKey.CruiseResume, "cruiectrlres" },
            { GameKey.QuickPark, "quickpark" },
            { GameKey.CameraInterior, "cam1" },
            { GameKey.CameraChase, "cam2" },
            { GameKey.CameraTop, "cam3" },
            { GameKey.CameraRoof, "cam4" },
            { GameKey.CameraLeanOut, "cam5" },
            { GameKey.CameraBumper, "cam6" },
            { GameKey.CameraWheel, "cam7" },
            { GameKey.CameraDriveBy, "cam8" },
            { GameKey.CameraCycle, "camcycle" },
            { GameKey.Map, "navmap" },
            { GameKey.DashboardDisplay, "display" },
            { GameKey.Hud, "showhud" },
            { GameKey.RadioNext, "radionext" },
            { GameKey.RadioPrevious, "radioprev" },
            { GameKey.Radio, "radio" },
            { GameKey.QuickSave, "quicksave" },
            { GameKey.Mirrors, "showmirrors" },
            { GameKey.LookLeft, "lookleft" },
            { GameKey.LookRight, "lookright" },
            { GameKey.GearUp, "gearup" },
            { GameKey.GearDown, "geardown" },
            { GameKey.AdvisorZoom, "advzoomout" },
            { GameKey.AdvisorMode, "advoptions" },
            { GameKey.RoadAssistance, "services" },
            { GameKey.Screenshot, "screenshot" },
            { GameKey.Menu, "menu" }
        };

        private static readonly Regex MixLine = new Regex("\"mix ([A-Za-z0-9_]+) `([^`\"\\r\\n]*)`\"");
        //keyboard.e?0 or keyboard.e
        private static readonly Regex KeyTerm = new Regex(@"^keyboard\.([a-z0-9_]+)(\?\d+)?$", RegexOptions.IgnoreCase);
        private static readonly Regex ModifierTerm = new Regex(
            @"^modifier\(\s*keyboard\.([a-z0-9_]+)(\?\d+)?\s*,\s*keyboard\.([a-z0-9_]+)(\?\d+)?\s*\)$", RegexOptions.IgnoreCase);
        //modifier(shift_only, keyboard.e?0): the games write a key with a modifier so, the first argument is a mix
        //that tells which of Shift, Ctrl and Alt are held
        private static readonly Regex ModifierMixTerm = new Regex(
            @"^modifier\(\s*([a-z0-9_]+)\s*,\s*keyboard\.([a-z0-9_]+)(\?\d+)?\s*\)$", RegexOptions.IgnoreCase);

        //The key held by a modifier mix, null for no_modifier. Two modifiers at once aren't pressed by the server
        private static readonly Dictionary<string, string> ModifierMixes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "no_modifier", null },
                { "shift_only", "lshift" },
                { "ctrl_only", "lctrl" },
                { "alt_only", "lalt" }
            };

        //The keys of the actions found in the file. An action the file has but with no key the server can press
        //(only a joystick, a long press, nothing) is in the result as null: no key is pressed for it, a default key
        //would make another action then. Actions that aren't in the file aren't in the result
        public static IReadOnlyDictionary<GameKey, KeyStroke> Parse(string controls)
        {
            var result = new Dictionary<GameKey, KeyStroke>();
            if (string.IsNullOrEmpty(controls)) return result;

            var expressions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in MixLine.Matches(controls))
            {
                expressions[match.Groups[1].Value] = match.Groups[2].Value;
            }
            foreach (KeyValuePair<GameKey, string> mix in Mixes)
            {
                if (expressions.TryGetValue(mix.Value, out string expression)) result[mix.Key] = FirstKey(expression);
            }
            return result;
        }

        //The first alternative ("a | b") that is a key or a key with a modifier
        public static KeyStroke FirstKey(string expression)
        {
            foreach (string alternative in Alternatives(expression))
            {
                string term = alternative.Trim();
                Match key = KeyTerm.Match(term);
                if (key.Success && ScsKeyNames.TryGet(key.Groups[1].Value, out KeyStroke stroke)) return stroke;

                Match modified = ModifierTerm.Match(term);
                if (modified.Success
                    && ScsKeyNames.TryGet(modified.Groups[1].Value, out KeyStroke modifier)
                    && ScsKeyNames.TryGet(modified.Groups[3].Value, out KeyStroke main))
                {
                    return main.With(modifier);
                }

                Match mixed = ModifierMixTerm.Match(term);
                if (mixed.Success
                    && ModifierMixes.TryGetValue(mixed.Groups[1].Value, out string held)
                    && ScsKeyNames.TryGet(mixed.Groups[2].Value, out KeyStroke plain))
                {
                    return held != null && ScsKeyNames.TryGet(held, out KeyStroke holder) ? plain.With(holder) : plain;
                }
            }
            return null;
        }

        //Splits by "|" outside of brackets
        private static IEnumerable<string> Alternatives(string expression)
        {
            int depth = 0;
            int start = 0;
            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];
                if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (c == '|' && depth == 0)
                {
                    yield return expression.Substring(start, i - start);
                    start = i + 1;
                }
            }
            yield return expression.Substring(start);
        }
    }
}
