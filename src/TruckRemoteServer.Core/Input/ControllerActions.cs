using System.Collections.Generic;

namespace TruckRemoteServer.Input
{
    //Actions of the controller's panel by their ids in the protocol (see ControllerMessage.Actions).
    //Ids are fixed (they don't depend on the place of a button), unknown ids (of newer controllers) are ignored
    public static class ControllerActions
    {
        //A click per counter increment
        public static readonly IReadOnlyDictionary<int, GameKey> Clicks = new Dictionary<int, GameKey>
        {
            { 1, GameKey.Engine },
            { 2, GameKey.Trailer },
            { 3, GameKey.Activate },
            { 4, GameKey.LightHorn },
            { 5, GameKey.Wipers },
            { 6, GameKey.Beacon },
            { 7, GameKey.DiffLock },
            { 8, GameKey.LiftAxle },
            { 9, GameKey.RetarderUp },
            { 10, GameKey.RetarderDown },
            { 12, GameKey.CruiseUp },
            { 13, GameKey.CruiseDown },
            { 14, GameKey.CruiseResume },
            { 15, GameKey.QuickPark },
            { 16, GameKey.CameraInterior },
            { 17, GameKey.CameraChase },
            { 18, GameKey.CameraCycle },
            { 19, GameKey.Map },
            { 20, GameKey.DashboardDisplay },
            { 21, GameKey.Hud },
            { 22, GameKey.RadioNext },
            { 23, GameKey.QuickSave },
            { 24, GameKey.Mirrors },
            { 25, GameKey.CameraTop },
            { 26, GameKey.CameraRoof },
            { 27, GameKey.CameraLeanOut },
            { 28, GameKey.CameraBumper },
            { 29, GameKey.CameraWheel },
            { 30, GameKey.CameraDriveBy },
            { 33, GameKey.GearUp },
            { 34, GameKey.GearDown },
            { 35, GameKey.RadioPrevious },
            { 36, GameKey.Radio },
            { 37, GameKey.AdvisorZoom },
            { 38, GameKey.AdvisorMode },
            { 39, GameKey.RoadAssistance },
            { 40, GameKey.Screenshot },
            { 41, GameKey.Menu }
        };

        //The key is held while the value isn't 0
        public static readonly IReadOnlyDictionary<int, GameKey> Holds = new Dictionary<int, GameKey>
        {
            { 11, GameKey.EngineBrake },
            { 31, GameKey.LookLeft },
            { 32, GameKey.LookRight },
            //The high beam is on while the button is held (revision 7; older controllers click it by 4)
            { 42, GameKey.LightHorn },
            //Held to refuel (revision 7; older controllers click it by 3)
            { 43, GameKey.Activate }
        };

        //The controls of the main screen, by their ids in the unbound actions message only (revision 8, see
        //BinaryProtocol): they are sent as flags, not as actions. The lights button uses both of its keys.
        //The pedals aren't here: without the keys of the gas and the brake the game can't be driven at all
        public static readonly IReadOnlyDictionary<int, GameKey> MainControls = new Dictionary<int, GameKey>
        {
            { 200, GameKey.LeftBlinker },
            { 201, GameKey.RightBlinker },
            { 202, GameKey.HazardLights },
            { 203, GameKey.ParkingBrake },
            { 204, GameKey.Lights },
            { 205, GameKey.HighBeam },
            { 206, GameKey.Horn },
            { 207, GameKey.AirHorn },
            { 208, GameKey.CruiseControl }
        };
    }
}
