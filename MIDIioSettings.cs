namespace blekenbleu.MIDIspace
{
    internal class MIDIioSettings // saved while plugin restarts
    {
        internal byte[] Slider { get; set; } = { 0, 0, 0, 0, 0, 0, 0, 0 };

        internal byte[] Knob { get; set; } = { 0, 0, 0, 0, 0, 0, 0, 0 };

        internal byte[] Button { get; set; } = { 0, 0, 0, 0, 0, 0, 0, 0 };

        internal byte[] Sent { get; set; } = { 0, 0, 0, 0, 0, 0, 0, 0 };	// track values from SimHub

        internal ulong[] CCbits { get; set; } = { 0, 0 };	// track other initialized CCvalue properties
    }
}
