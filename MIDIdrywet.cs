using System;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using SimHub.Plugins;

namespace blekenbleu.MIDIspace
{
    /// <summary>
    /// from Input device https://melanchall.github.io/drywetmidi/articles/devices/Input-device.html
    /// </summary>
    public class MIDIdrywet
    {
        private static MIDIio M;    // needed for AttachDelegate(), AddEvent() and TriggerEvent()
        private static IInputDevice _inputDevice;
        public static IInputDevice InputDevice { get => _inputDevice; set => _inputDevice = value; }

        public MIDIioSettings Settings;
        public MIDIioProperties CC = new MIDIioProperties();

        public void Init(String MIDIin, String MIDIout, MIDIioSettings savedSettings, MIDIio that )
        {
            try
            {
                InputDevice = Melanchall.DryWetMidi.Devices.InputDevice.GetByName(MIDIin);
                InputDevice.EventReceived += OnEventReceived;
                InputDevice.StartEventsListening();
                SimHub.Logging.Current.Info($"MIDIdrywet input is listening for {MIDIin} messages.");
            }
            
            catch (Exception)
            {
                SimHub.Logging.Current.Info($"Failed to find MIDIdrywet input device {MIDIin};\nKnown devices:");
                foreach (var inputDevice in Melanchall.DryWetMidi.Devices.InputDevice.GetAll())
                {
                    SimHub.Logging.Current.Info(inputDevice.Name);
                }
            }
            Settings = savedSettings;
            CC.init();
            M = that;
            M.AttachDelegate($"slider0", () => Settings.Slider[0]);
            M.AttachDelegate($"knob0", () => Settings.Knob[0]);
            M.AttachDelegate($"slider1", () => Settings.Slider[1]);
            M.AttachDelegate($"knob1", () => Settings.Knob[1]);
            M.AttachDelegate($"slider2", () => Settings.Slider[2]);
            M.AttachDelegate($"knob2", () => Settings.Knob[2]);
            M.AttachDelegate($"slider3", () => Settings.Slider[3]);
            M.AttachDelegate($"knob3", () => Settings.Knob[3]);
            M.AttachDelegate($"slider4", () => Settings.Slider[4]);
            M.AttachDelegate($"knob4", () => Settings.Knob[4]);
            M.AttachDelegate($"slider5", () => Settings.Slider[5]);
            M.AttachDelegate($"knob5", () => Settings.Knob[5]);
            M.AttachDelegate($"slider6", () => Settings.Slider[6]);
            M.AttachDelegate($"knob6", () => Settings.Knob[6]);
            M.AttachDelegate($"slider7", () => Settings.Slider[7]);
            M.AttachDelegate($"knob7", () => Settings.Knob[7]);
            M.AddEvent("Warning");
            M.PluginManager.SetPropertyValue("test", M.GetType(), 10);
        }

        public MIDIioSettings End()
        {
            (InputDevice as IDisposable)?.Dispose();

            return Settings;
        }

        // callback
        void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            // this cute syntax is called pattern matching
            if (e.Event is ControlChangeEvent foo)
            {
//              SimHub.Logging.Current.Info($"ControlNumber = {foo.ControlNumber}; ControlValue = {foo.ControlValue}");
                if (8 > foo.ControlNumber)	// unsigned
                    Settings.Slider[foo.ControlNumber] = foo.ControlValue;
                else if (16 <= foo.ControlNumber && 24 > foo.ControlNumber)
                    Settings.Knob[foo.ControlNumber - 16] = foo.ControlValue;
                else if (CCactive(foo.ControlNumber, foo.ControlValue))
                    SimHub.Logging.Current.Info($"Setting {midiDevice.Name} ControlChangeEvent : {e.Event}");
            }
            else SimHub.Logging.Current.Info($"Ignoring {e.Event} received from {midiDevice.Name}");
        }

        // track active CCs
        private static ulong[] CCbits { get; set; } = { 0, 0 }; // track initialized CCvalue properties
        private bool CCactive(byte CCnumber, byte value)
        {
            ulong mask = 1;
            byte index = 0;
            byte C63 = (byte)(63 & CCnumber);

            CCnumber &= 127;

            if (63 < CCnumber)
                index++;    // switch ulong

            mask <<= C63;
            if (0 < (mask & CCbits[index]))	// already set?
            {
                CC.SetVal(CCnumber, value);
                if (0 < value)
                    M.TriggerEvent(MIDIioProperties.Properties[CCnumber]);
                return false;			// do not log
            }

            CCbits[index] |= mask;
            CC.SetProp(M, CCnumber, value);
            return true;
        }
    }
}
