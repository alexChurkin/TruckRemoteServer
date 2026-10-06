namespace TruckRemoteServer.Updates
{
    //Releases of the server and its update in place
    public interface IUpdater
    {
        //The latest release, null if it can't be known now (no internet, GitHub doesn't answer)
        ReleaseInfo GetLatestRelease();

        //Downloads the exe of the release and puts it in place of the running one, then starts it: the new server
        //waits for this one to exit. Returns false if it can't be done (no exe in the release, a folder that can't
        //be written, a failed download): nothing is changed then and the page of the release should be opened
        bool Install(ReleaseInfo release);

        void OpenPage(ReleaseInfo release);
    }
}
