using System;

namespace TruckRemoteServer.Input
{
    //Every additional action has a click counter on the controller's side.
    //A lost message can't lose a click: the next one has the counter increased all the same
    public class ActionCounters
    {
        //A bigger jump is considered as a counter of another session, not as clicks
        public const int MAX_CLICKS_AT_ONCE = 5;

        private readonly int[] previous;

        public ActionCounters(int count)
        {
            previous = new int[count];
        }

        //Counters of a new controller are taken as they are, without clicks
        public void Sync(int[] counters)
        {
            Array.Copy(counters, previous, Math.Min(counters.Length, previous.Length));
        }

        //Returns the number of clicks for every action
        public int[] Update(int[] counters)
        {
            int[] clicks = new int[previous.Length];
            int count = Math.Min(counters.Length, previous.Length);
            for (int i = 0; i < count; i++)
            {
                int difference = counters[i] - previous[i];
                if (difference == 0) continue;

                if (difference > 0 && difference <= MAX_CLICKS_AT_ONCE)
                {
                    clicks[i] = difference;
                    previous[i] = counters[i];
                }
                else if (difference > MAX_CLICKS_AT_ONCE || difference < -MAX_CLICKS_AT_ONCE)
                {
                    //Counter was reset (e.g. the app was restarted): taken without clicks
                    previous[i] = counters[i];
                }
                //A slightly smaller value is an old message that came late: it must not move the counter back,
                //otherwise the next message would repeat the clicks
            }
            return clicks;
        }
    }
}
