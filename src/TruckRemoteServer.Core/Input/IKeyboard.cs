namespace TruckRemoteServer.Input
{
    //Keyboard of the PC as the game sees it
    public interface IKeyboard
    {
        void Press(GameKey key);

        void Release(GameKey key);

        //Returns at once, the key is held for a while later, so the game doesn't miss it
        void Click(GameKey key);
    }
}
