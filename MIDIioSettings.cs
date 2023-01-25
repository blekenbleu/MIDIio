namespace blekenbleu.MIDIspace
{
    internal class MIDIioSettings // saved while plugin restarts
    {
        internal byte[] Slider { get; set; } = { 0, 0, 0, 0, 0, 0, 0, 0 };

        internal byte[] Knob { get; set; } = { 0, 0, 0, 0, 0, 0, 0, 0 };

        internal byte[] Sent { get; set; } = { 0, 0, 0, 0, 0, 0, 0, 0 };

        internal ulong[] CCbits { get; set; } = { 0, 0 }; // track initialized CCvalue properties 
    }
}
