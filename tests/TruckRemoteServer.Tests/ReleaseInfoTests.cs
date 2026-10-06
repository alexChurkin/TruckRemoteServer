using System;
using TruckRemoteServer.Updates;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class ReleaseInfoTests
    {
        private const string Json = @"{""url"":""https://api.github.com/repos/alexChurkin/TruckRemoteServer/releases/1"",
            ""html_url"":""https://github.com/alexChurkin/TruckRemoteServer/releases/tag/v1.3.1"",
            ""tag_name"":""v1.3.1"",""name"":""Truck Remote Server 1.3.1"",
            ""author"":{""html_url"":""https://github.com/alexChurkin""},
            ""assets"":[
              {""name"":""THIRD-PARTY-NOTICES.txt"",""browser_download_url"":""https://github.com/alexChurkin/TruckRemoteServer/releases/download/v1.3.1/THIRD-PARTY-NOTICES.txt""},
              {""name"":""TruckRemoteServer.exe"",""browser_download_url"":""https://github.com/alexChurkin/TruckRemoteServer/releases/download/v1.3.1/TruckRemoteServer.exe""}]}";

        [Fact]
        public void LatestReleaseIsReadFromTheApiAnswer()
        {
            ReleaseInfo release = ReleaseInfo.Parse(Json);

            Assert.Equal(new Version(1, 3, 1, 0), release.Version);
            Assert.Equal("1.3.1", release.ToString());
            Assert.Equal("https://github.com/alexChurkin/TruckRemoteServer/releases/tag/v1.3.1", release.Page);
            Assert.Equal("https://github.com/alexChurkin/TruckRemoteServer/releases/download/v1.3.1/TruckRemoteServer.exe", release.Exe);
        }

        [Fact]
        public void ReleaseWithAnArchiveHasNoExe()
        {
            //The release 1.2 had a RAR archive
            string json = Json.Replace("TruckRemoteServer.exe", "TruckRemoteServer.rar");

            Assert.Null(ReleaseInfo.Parse(json).Exe);
        }

        [Theory]
        [InlineData("1.2", 1, 2, 0)]
        [InlineData("v1.3", 1, 3, 0)]
        [InlineData("1.3.2-beta", 1, 3, 2)]
        [InlineData("2", 2, 0, 0)]
        public void TagsAreVersions(string tag, int major, int minor, int build)
        {
            Assert.Equal(new Version(major, minor, build, 0), ReleaseInfo.ParseVersion(tag));
        }

        [Fact]
        public void NewerIsComparedWithoutTheTrailingZeros()
        {
            var release = new ReleaseInfo(new Version(1, 3, 0, 0), "page", null);

            Assert.False(release.IsNewerThan(new Version(1, 3)));
            Assert.False(release.IsNewerThan(new Version(1, 3, 0, 0)));
            Assert.True(release.IsNewerThan(new Version(1, 2, 0, 0)));
            Assert.False(release.IsNewerThan(new Version(1, 4)));
        }

        [Fact]
        public void BrokenAnswerIsNoRelease()
        {
            Assert.Null(ReleaseInfo.Parse(null));
            Assert.Null(ReleaseInfo.Parse("{\"message\":\"API rate limit exceeded\"}"));
            Assert.Null(ReleaseInfo.Parse("{\"tag_name\":\"latest\",\"html_url\":\"https://github.com/a/b/releases/tag/latest\"}"));
        }
    }
}
