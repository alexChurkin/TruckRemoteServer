using System.Collections.Generic;
using TruckRemoteServer.Firewall;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class FirewallRulesTests
    {
        private const string Program = @"C:\Apps\TruckRemoteServer\TruckRemoteServer.exe";
        private const int Port = 18250;
        private const int PrivateProfile = 2;
        private const int PublicProfile = 4;

        private static FirewallStatus Evaluate(params FirewallRule[] rules)
        {
            return FirewallRules.Evaluate(rules, Program, Port, PrivateProfile, firewallEnabled: true,
                blockAllInbound: false);
        }

        private static FirewallRule ProgramRule(bool allow, int profiles = 7, int protocol = FirewallRule.ProtocolUdp)
        {
            return new FirewallRule
            {
                Enabled = true,
                Inbound = true,
                Allow = allow,
                Application = Program.ToLowerInvariant(),
                Protocol = protocol,
                Profiles = profiles
            };
        }

        [Fact]
        public void NoRulesMeansBlockedByDefault()
        {
            Assert.Equal(FirewallStatus.NoRule, Evaluate());
        }

        [Fact]
        public void AllowRuleOfProgram()
        {
            Assert.Equal(FirewallStatus.Allowed, Evaluate(ProgramRule(allow: true)));
        }

        [Fact]
        public void BlockRuleWinsOverAllowRule()
        {
            Assert.Equal(FirewallStatus.Blocked, Evaluate(ProgramRule(allow: true), ProgramRule(allow: false)));
        }

        [Fact]
        public void RulesOfInactiveProfilesAndOtherProtocolsDontApply()
        {
            Assert.Equal(FirewallStatus.NoRule, Evaluate(
                ProgramRule(allow: true, profiles: PublicProfile),
                ProgramRule(allow: true, protocol: 6),
                new FirewallRule { Enabled = false, Inbound = true, Allow = true },
                new FirewallRule { Enabled = true, Inbound = false, Allow = true },
                new FirewallRule { Enabled = true, Inbound = true, Allow = true, Application = @"C:\Other.exe" }));
        }

        [Fact]
        public void PortRuleOfAnyProgram()
        {
            var rule = new FirewallRule { Enabled = true, Inbound = true, Allow = true, LocalPorts = "18000-19000" };
            Assert.Equal(FirewallStatus.Allowed, Evaluate(rule));
        }

        [Fact]
        public void DisabledFirewallAndBlockAll()
        {
            var noRules = new List<FirewallRule>();
            Assert.Equal(FirewallStatus.Allowed,
                FirewallRules.Evaluate(noRules, Program, Port, PrivateProfile, false, true));
            Assert.Equal(FirewallStatus.Blocked,
                FirewallRules.Evaluate(new[] { ProgramRule(allow: true) }, Program, Port, PrivateProfile, true, true));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("*", true)]
        [InlineData("18250", true)]
        [InlineData("80, 18250", true)]
        [InlineData("18000-18300", true)]
        [InlineData("18251", false)]
        [InlineData("1-100,200", false)]
        [InlineData("RPC", false)]
        public void PortMatches(string ports, bool expected)
        {
            Assert.Equal(expected, FirewallRules.PortMatches(ports, Port));
        }
    }
}
