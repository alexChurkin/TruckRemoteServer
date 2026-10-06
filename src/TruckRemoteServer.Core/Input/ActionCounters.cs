using System.Collections.Generic;

namespace TruckRemoteServer.Input
{
    //Every click action has a click counter (mod 256) on the controller's side.
    //A lost message can't lose a click: the next one has the counter increased all the same
    public class ActionCounters
    {
        //A bigger jump is considered as a counter of another session, not as clicks
        public const int MaxClicksAtOnce = 5;
        private const int Modulo = 256;

        private readonly Dictionary<int, int> previous = new Dictionary<int, int>();

        //Counters of a new controller are taken as they are, without clicks
        public void Sync(IReadOnlyDictionary<int, int> counters)
        {
            previous.Clear();
            foreach (KeyValuePair<int, int> counter in counters) previous[counter.Key] = counter.Value;
        }

        //Returns the number of clicks of the actions that were clicked.
        //A counter that isn't sent is 0 (the controller sends only clicked actions)
        public Dictionary<int, int> Update(IReadOnlyDictionary<int, int> counters)
        {
            var clicks = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> counter in counters)
            {
                previous.TryGetValue(counter.Key, out int last);
                int difference = ((counter.Value - last) % Modulo + Modulo) % Modulo;
                if (difference == 0) continue;

                if (difference <= MaxClicksAtOnce)
                {
                    clicks[counter.Key] = difference;
                    previous[counter.Key] = counter.Value;
                }
                else if (difference < Modulo - MaxClicksAtOnce)
                {
                    //Counter was reset (e.g. the app was restarted): taken without clicks
                    previous[counter.Key] = counter.Value;
                }
                //A slightly smaller value is an old message that came late: it must not move the counter back,
                //otherwise the next message would repeat the clicks
            }
            return clicks;
        }
    }
}
