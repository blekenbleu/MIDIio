namespace blekenbleu.MIDIspace
{
    internal class MIDIioSettings // saved while plugin restarts
    {
        internal byte[] Sent { get; set; } = new byte[128];	// track values from MIDIio.DoSend()
    }
}
