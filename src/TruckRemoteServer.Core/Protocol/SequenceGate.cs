namespace TruckRemoteServer.Protocol
{
    //UDP may deliver messages out of order. An old message with toggle values applied after a newer one
    //would make an extra click, so messages older than the last applied one are dropped.
    //Messages without a number (older controllers) are always accepted
    public class SequenceGate
    {
        private long last = -1;

        public bool Accept(long? sequence)
        {
            if (sequence == null) return true;
            if (sequence.Value <= last) return false;
            last = sequence.Value;
            return true;
        }

        //New or resumed controller session: its numbering may start again
        public void Reset()
        {
            last = -1;
        }
    }
}
