using System;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;

namespace blekenbleu.MIDIspace
{
    /// <summary>
    /// from Input device https://melanchall.github.io/drywetmidi/articles/devices/Input-device.html
    /// </summary>
    public class MIDIdrywet
    {
        private static IInputDevice _inputDevice;
        public static IInputDevice InputDevice { get => _inputDevice; set => _inputDevice = value; }

        public MIDIioSettings Settings;

        public void Init(String MIDIin, String MIDIout, MIDIioSettings savedSettings )
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
            SimHub.Logging.Current.Info($"Event received from '{midiDevice.Name}': {e.Event}");
            // this cute syntax is called pattern matching
            if (e.Event is ControlChangeEvent foo)
            {
//              SimHub.Logging.Current.Info($"ControlNumber = '{foo.ControlNumber}'; ControlValue = '{foo.ControlValue}");
                if (8 > foo.ControlNumber)	// unsigned
                    Settings.Slider[foo.ControlNumber] = foo.ControlValue;
                else if (16 <= foo.ControlNumber && 24 > foo.ControlNumber)
                    Settings.Knob[foo.ControlNumber - 16] = foo.ControlValue;
            }
        }
    }
}
