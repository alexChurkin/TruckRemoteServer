using System;
using System.Text.RegularExpressions;

namespace TruckRemoteServer.Updates
{
    //A release of the server on GitHub
    public sealed class ReleaseInfo
    {
        public const string LatestReleaseApi = "https://api.github.com/repos/alexChurkin/TruckRemoteServer/releases/latest";
        public const string ExeName = "TruckRemoteServer.exe";

        private static readonly Regex TagName = new Regex("\"tag_name\"\\s*:\\s*\"([^\"]*)\"");
        private static readonly Regex PageUrl = new Regex("\"html_url\"\\s*:\\s*\"(https://github\\.com/[^\"]*/releases/tag/[^\"]*)\"");
        private static readonly Regex ExeUrl = new Regex(
            "\"browser_download_url\"\\s*:\\s*\"(https://github\\.com/[^\"]*/releases/download/[^\"]*/" + Regex.Escape(ExeName) + ")\"",
            RegexOptions.IgnoreCase);
        //"1.3", "v1.3.1", "1.3-beta" -> 1.3, 1.3.1, 1.3
        private static readonly Regex VersionNumber = new Regex(@"^[vV]?(\d+(\.\d+){0,3})");

        public ReleaseInfo(Version version, string page, string exe)
        {
            Version = version;
            Page = page;
            Exe = exe;
        }

        public Version Version { get; }

        //The page of the release (what's new, the files)
        public string Page { get; }

        //The exe of the release, null if the release has none (it is updated through its page then)
        public string Exe { get; }

        //The answer of the GitHub API about the latest release; null if it can't be read
        public static ReleaseInfo Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            Match tag = TagName.Match(json);
            Match page = PageUrl.Match(json);
            if (!tag.Success || !page.Success) return null;
            Version version = ParseVersion(tag.Groups[1].Value);
            if (version == null) return null;
            Match exe = ExeUrl.Match(json);
            return new ReleaseInfo(version, page.Groups[1].Value, exe.Success ? exe.Groups[1].Value : null);
        }

        public static Version ParseVersion(string tag)
        {
            Match number = VersionNumber.Match(tag ?? "");
            if (!number.Success) return null;
            string text = number.Groups[1].Value;
            //A single number is a major version
            if (text.IndexOf('.') < 0) text += ".0";
            return Version.TryParse(text, out Version version) ? Normalize(version) : null;
        }

        //1.3 and 1.3.0.0 are the same version
        public bool IsNewerThan(Version current) => current == null || Version > Normalize(current);

        public override string ToString() => Version.ToString(Version.Revision > 0 ? 4 : Version.Build > 0 ? 3 : 2);

        private static Version Normalize(Version version) => new Version(version.Major, version.Minor,
            Math.Max(0, version.Build), Math.Max(0, version.Revision));
    }
}
