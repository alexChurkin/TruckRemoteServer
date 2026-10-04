using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace TruckRemoteServer.Firewall
{
    //Windows Firewall access: rules are read through COM (no administrator rights needed),
    //changes are made by netsh started with elevation
    public static class WindowsFirewall
    {
        public const string RULE_NAME = "Truck Remote Server";

        private const int NET_FW_RULE_DIR_IN = 1;
        private const int NET_FW_ACTION_ALLOW = 1;
        private static readonly int[] PROFILE_TYPES = { 1, 2, 4 };

        public static FirewallStatus Check(string programPath, int port)
        {
            try
            {
                Type policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                if (policyType == null) return FirewallStatus.Unknown;
                dynamic policy = Activator.CreateInstance(policyType);

                int activeProfiles = policy.CurrentProfileTypes;
                bool enabled = false;
                bool blockAll = false;
                foreach (int profile in PROFILE_TYPES)
                {
                    if ((activeProfiles & profile) == 0) continue;
                    if (policy.FirewallEnabled[profile])
                    {
                        enabled = true;
                        if (policy.BlockAllInboundTraffic[profile]) blockAll = true;
                    }
                }

                var rules = new List<FirewallRule>();
                foreach (dynamic rule in policy.Rules)
                {
                    rules.Add(new FirewallRule
                    {
                        Name = rule.Name,
                        Enabled = rule.Enabled,
                        Inbound = rule.Direction == NET_FW_RULE_DIR_IN,
                        Allow = rule.Action == NET_FW_ACTION_ALLOW,
                        Application = rule.ApplicationName,
                        Protocol = rule.Protocol,
                        LocalPorts = rule.LocalPorts,
                        Profiles = rule.Profiles
                    });
                }
                return FirewallRules.Evaluate(rules, programPath, port, activeProfiles, enabled, blockAll);
            }
            catch (Exception e)
            {
                Console.WriteLine("INFO: Can't check firewall: " + e.Message);
                return FirewallStatus.Unknown;
            }
        }

        //Replaces all inbound rules of the program (including blocking ones) with one allowing rule.
        //Returns false if the user refused elevation or netsh failed to start
        public static bool AllowProgram(string programPath)
        {
            string program = "program=\"" + programPath + "\"";
            //Deleting fails when there are no rules yet, so commands are joined with "&", not "&&"
            string commands =
                "netsh advfirewall firewall delete rule name=all dir=in " + program +
                " & netsh advfirewall firewall add rule name=\"" + RULE_NAME + "\" dir=in action=allow " +
                program + " protocol=UDP enable=yes profile=any";

            var startInfo = new ProcessStartInfo("cmd.exe", "/c " + commands)
            {
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    process?.WaitForExit();
                }
                return true;
            }
            catch (Win32Exception e)
            {
                //Elevation was cancelled
                Console.WriteLine("INFO: Firewall rule wasn't added: " + e.Message);
                return false;
            }
        }
    }
}
