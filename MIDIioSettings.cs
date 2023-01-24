namespace blekenbleu.MIDIspace
{
    public class MIDIioSettings
    {
        public byte[] Slider { get; set; } = { 0, 0, 0, 0, 0, 0, 0, 0 };

        public byte[] Knob { get; set; } = { 0, 0, 0, 0, 0, 0, 0, 0 };

        public ulong[] CCbits { get; set; } = { 0, 0 }; // track initialized CCvalue properties 
    }
}
