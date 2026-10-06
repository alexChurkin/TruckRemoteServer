using System.Collections.Generic;

namespace TruckRemoteServer.Input
{
    //Default key bindings of ETS2 and ATS; they are pressed when the player's profile doesn't tell otherwise
    public static class DefaultKeys
    {
        public static readonly IReadOnlyDictionary<GameKey, KeyStroke> Keys = new Dictionary<GameKey, KeyStroke>
        {
            { GameKey.Gas, new KeyStroke(0x48, extended: true) },         //Up arrow
            { GameKey.Brake, new KeyStroke(0x50, extended: true) },       //Down arrow
            { GameKey.LeftBlinker, new KeyStroke(0x1A) },                 //[
            { GameKey.RightBlinker, new KeyStroke(0x1B) },                //]
            { GameKey.HazardLights, new KeyStroke(0x21) },                //F
            { GameKey.ParkingBrake, new KeyStroke(0x39) },                //Space
            { GameKey.Lights, new KeyStroke(0x26) },                      //L
            { GameKey.HighBeam, new KeyStroke(0x25) },                    //K
            { GameKey.Horn, new KeyStroke(0x23) },                        //H
            { GameKey.AirHorn, new KeyStroke(0x31) },                     //N
            { GameKey.CruiseControl, new KeyStroke(0x2E) },               //C
            { GameKey.Engine, new KeyStroke(0x12) },                      //E
            { GameKey.Trailer, new KeyStroke(0x14) },                     //T
            { GameKey.Activate, new KeyStroke(0x1C) },                    //Enter
            { GameKey.Wipers, new KeyStroke(0x19) },                      //P
            { GameKey.DiffLock, new KeyStroke(0x2F) },                    //V
            { GameKey.LiftAxle, new KeyStroke(0x16) },                    //U
            { GameKey.Beacon, new KeyStroke(0x18) },                      //O
            { GameKey.LightHorn, new KeyStroke(0x24) },                   //J
            { GameKey.RetarderUp, new KeyStroke(0x27) },                  //;
            { GameKey.RetarderDown, new KeyStroke(0x28) },                //'
            { GameKey.EngineBrake, new KeyStroke(0x30) },                 //B
            //No default keys in the games: these ones are added to their bindings (see GameControlsFile)
            { GameKey.CruiseUp, new KeyStroke(0x34) },                    //.
            { GameKey.CruiseDown, new KeyStroke(0x33) },                  //,
            { GameKey.CruiseResume, new KeyStroke(0x35) },                // /
            { GameKey.QuickPark, new KeyStroke(0x10) },                   //Q
            { GameKey.CameraInterior, new KeyStroke(0x02) },              //1
            { GameKey.CameraChase, new KeyStroke(0x03) },                 //2
            { GameKey.CameraCycle, new KeyStroke(0x0A) },                 //9
            { GameKey.Map, new KeyStroke(0x32) },                         //M
            { GameKey.DashboardDisplay, new KeyStroke(0x17) },            //I
            { GameKey.Hud, new KeyStroke(0x3D) },                         //F3
            { GameKey.RadioNext, new KeyStroke(0x51, extended: true) },   //Page Down
            { GameKey.QuickSave, new KeyStroke(0x46) },                   //Scroll Lock
            { GameKey.Mirrors, new KeyStroke(0x3C) },                     //F2
            { GameKey.CameraTop, new KeyStroke(0x04) },                   //3
            { GameKey.CameraRoof, new KeyStroke(0x05) },                  //4
            { GameKey.CameraLeanOut, new KeyStroke(0x06) },               //5
            { GameKey.CameraBumper, new KeyStroke(0x07) },                //6
            { GameKey.CameraWheel, new KeyStroke(0x08) },                 //7
            { GameKey.CameraDriveBy, new KeyStroke(0x09) },               //8
            { GameKey.LookLeft, new KeyStroke(0x35, extended: true) },    //Numpad /
            { GameKey.LookRight, new KeyStroke(0x37) },                   //Numpad *
            { GameKey.GearUp, new KeyStroke(0x2A) },                      //Left Shift
            { GameKey.GearDown, new KeyStroke(0x1D) },                    //Left Ctrl
            { GameKey.RadioPrevious, new KeyStroke(0x49, extended: true) }, //Page Up
            { GameKey.Radio, new KeyStroke(0x13) },                       //R
            { GameKey.AdvisorZoom, new KeyStroke(0x3F) },                 //F5
            { GameKey.AdvisorMode, new KeyStroke(0x40) },                 //F6
            { GameKey.RoadAssistance, new KeyStroke(0x41) },              //F7
            { GameKey.Screenshot, new KeyStroke(0x44) },                  //F10
            { GameKey.Menu, new KeyStroke(0x01) }                         //Esc
        };
    }
}
