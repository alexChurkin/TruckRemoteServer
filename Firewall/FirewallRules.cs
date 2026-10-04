using System;
using System.Collections.Generic;
using System.Linq;

namespace TruckRemoteServer.Firewall
{
    public enum FirewallStatus
    {
        //Status can't be read (e.g. firewall service is stopped or another firewall is used)
        Unknown,
        Allowed,
        //Inbound packets are blocked by default and there is no allowing rule
        NoRule,
        //A blocking rule exists (e.g. created when "Allow access" wasn't pressed on the first start)
        Blocked
    }

    public class FirewallRule
    {
        public const int PROTOCOL_UDP = 17;
        public const int PROTOCOL_ANY = 256;

        public string Name;
        public bool Enabled;
        public bool Inbound;
        public bool Allow;
        //Full path of the program, empty for any program
        public string Application;
        public int Protocol = PROTOCOL_ANY;
        //"*", a port, a range ("1000-2000") or a comma separated list of them
        public string LocalPorts = "*";
        //Bit mask of NET_FW_PROFILE_TYPE2 (domain 1, private 2, public 4)
        public int Profiles = 0x7FFFFFFF;
    }

    //Decides whether UDP packets to the server pass Windows Firewall.
    //Block rules win over allow rules, as in Windows Firewall itself
    public static class FirewallRules
    {
        public static FirewallStatus Evaluate(IEnumerable<FirewallRule> rules, string programPath, int port,
            int activeProfiles, bool firewallEnabled, bool blockAllInbound)
        {
            if (!firewallEnabled) return FirewallStatus.Allowed;
            if (blockAllInbound) return FirewallStatus.Blocked;

            var applied = rules.Where(rule => Applies(rule, programPath, port, activeProfiles)).ToList();
            if (applied.Any(rule => !rule.Allow)) return FirewallStatus.Blocked;
            if (applied.Any(rule => rule.Allow)) return FirewallStatus.Allowed;
            return FirewallStatus.NoRule;
        }

        private static bool Applies(FirewallRule rule, string programPath, int port, int activeProfiles)
        {
            return rule.Enabled
                && rule.Inbound
                && (rule.Profiles & activeProfiles) != 0
                && (rule.Protocol == FirewallRule.PROTOCOL_UDP || rule.Protocol == FirewallRule.PROTOCOL_ANY)
                && (string.IsNullOrEmpty(rule.Application)
                    || string.Equals(rule.Application, programPath, StringComparison.OrdinalIgnoreCase))
                && PortMatches(rule.LocalPorts, port);
        }

        public static bool PortMatches(string ports, int port)
        {
            if (string.IsNullOrWhiteSpace(ports)) return true;

            foreach (string part in ports.Split(','))
            {
                string item = part.Trim();
                if (item == "*") return true;

                int dash = item.IndexOf('-');
                if (dash > 0)
                {
                    if (int.TryParse(item.Substring(0, dash), out int from)
                        && int.TryParse(item.Substring(dash + 1), out int to)
                        && port >= from && port <= to)
                    {
                        return true;
                    }
                }
                else if (int.TryParse(item, out int single) && single == port)
                {
                    return true;
                }
            }
            //Keywords (e.g. "RPC") don't match a usual UDP port
            return false;
        }
    }
}
