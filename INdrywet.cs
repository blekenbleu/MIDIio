using System;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using SimHub.Plugins;

namespace blekenbleu.MIDIspace
{
    /// <summary>
    /// from Input device https://melanchall.github.io/drywetmidi/articles/devices/Input-device.html
    /// </summary>
    public class INdrywet
    {
        private static MIDIio M;    // needed for AttachDelegate(), AddEvent() and TriggerEvent()
        private static IInputDevice _inputDevice;
        public static IInputDevice InputDevice { get => _inputDevice; set => _inputDevice = value; }

        public MIDIioSettings Settings;
        public CCProperties CC = new CCProperties();

        public void Init(String MIDIin, MIDIioSettings savedSettings, MIDIio that )
        {
            try
            {
                InputDevice = Melanchall.DryWetMidi.Devices.InputDevice.GetByName(MIDIin);
                InputDevice.EventReceived += OnEventReceived;
                InputDevice.StartEventsListening();
                SimHub.Logging.Current.Info($"INdrywet input is listening for {MIDIin} messages.");
            }
            
            catch (Exception)
            {
                SimHub.Logging.Current.Info($"Failed to find INdrywet input device {MIDIin};\nKnown devices:");
                foreach (var inputDevice in Melanchall.DryWetMidi.Devices.InputDevice.GetAll())
                {
                    SimHub.Logging.Current.Info(inputDevice.Name);
                }
            }
            Settings = savedSettings;
            CC.Init();
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
            CCrestore();
        }

        public MIDIioSettings End()
        {
            (InputDevice as IDisposable)?.Dispose();
//          SimHub.Logging.Current.Info($"End: CCbits #{Settings.CCbits[0]}, #{Settings.CCbits[1]}");
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
        public bool CCactive(SevenBitNumber CCnumber, SevenBitNumber value)
        {
            ulong mask = 1;
            byte index = 0;
            byte C63 = (byte)(63 & CCnumber);

            if (63 < CCnumber)
                index++;    // switch ulong

            mask <<= C63;
            if (0 < (mask & Settings.CCbits[index]))	// already set?
            {
                CC.SetVal(CCnumber, value);
                if (0 < value)
                    M.TriggerEvent(CCProperties.Properties[CCnumber]);
                return false;			// do not log
            }

            Settings.CCbits[index] |= mask;
            CC.SetProp(M, CCnumber, value);
            return true;
        }

        // restore active CCs after restart
        internal void CCrestore()
        {
            ulong mask = 1;

//          SimHub.Logging.Current.Info($"CCrestore(): CCbits #{Settings.CCbits[0]}, #{Settings.CCbits[1]}");
            for (byte i = 0; i < 64; i++)
            {
                if (0 < (mask & Settings.CCbits[0]))
                    CC.SetProp(M, i, 0);
                if (0 < (mask & Settings.CCbits[1]))
                    CC.SetProp(M, (byte)(64 + i), 0);
                mask <<= 1;
            }
        }
    }
}
