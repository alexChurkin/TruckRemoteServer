using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

using TruckRemoteServer.Firewall;

namespace TruckRemoteServer.Infrastructure
{
    //Windows Firewall access: rules are read through COM (no administrator rights needed),
    //changes are made by netsh started with elevation
    public sealed class WindowsFirewall : IFirewall
    {
        public const string RuleName = "Truck Remote Server";

        private const int NetFwRuleDirIn = 1;
        private const int NetFwActionAllow = 1;
        private static readonly int[] ProfileTypes = { 1, 2, 4 };
        //How long a rule that netsh has added is waited for
        private const int RuleChecks = 20;
        private const int RuleCheckIntervalMs = 250;

        public FirewallStatus Check(string programPath, int port)
        {
            try
            {
                Type policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                if (policyType == null) return FirewallStatus.Unknown;
                dynamic policy = Activator.CreateInstance(policyType);

                int activeProfiles = policy.CurrentProfileTypes;
                bool enabled = false;
                bool blockAll = false;
                foreach (int profile in ProfileTypes)
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
                        Inbound = rule.Direction == NetFwRuleDirIn,
                        Allow = rule.Action == NetFwActionAllow,
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
                Debug.WriteLine("Can't check firewall: " + e.Message);
                return FirewallStatus.Unknown;
            }
        }

        //Replaces all inbound rules of the program (including blocking ones) with one allowing rule.
        //Returns false if the user refused elevation or netsh failed to start
        public bool AllowProgram(string programPath)
        {
            string program = "program=\"" + programPath + "\"";
            //Deleting fails when there are no rules yet, so commands are joined with "&", not "&&"
            string commands =
                "netsh advfirewall firewall delete rule name=all dir=in " + program +
                " & netsh advfirewall firewall add rule name=\"" + RuleName + "\" dir=in action=allow " +
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
                //The firewall shows a new rule a moment after netsh has finished: a check made at once still
                //finds no rule, and the warning would stay until the program is allowed once more
                WaitForRule(programPath);
                return true;
            }
            catch (Win32Exception e)
            {
                //Elevation was cancelled
                Debug.WriteLine("Firewall rule wasn't added: " + e.Message);
                return false;
            }
        }

        private static void WaitForRule(string programPath)
        {
            for (int i = 0; i < RuleChecks; i++)
            {
                if (HasRule(programPath)) return;
                Thread.Sleep(RuleCheckIntervalMs);
            }
        }

        //The rule AllowProgram adds; true as well when the rules can't be read (there is nothing to wait for)
        private static bool HasRule(string programPath)
        {
            try
            {
                Type policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                if (policyType == null) return true;
                dynamic policy = Activator.CreateInstance(policyType);
                foreach (dynamic rule in policy.Rules)
                {
                    if (rule.Name == RuleName && rule.Enabled && rule.Direction == NetFwRuleDirIn
                        && rule.Action == NetFwActionAllow
                        && string.Equals((string)rule.ApplicationName, programPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Can't check firewall: " + e.Message);
                return true;
            }
        }
    }
}
