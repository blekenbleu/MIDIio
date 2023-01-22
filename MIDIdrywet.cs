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
        private static IInputDevice _inputDevice;

        public byte[] Slider { get; set; } = { 0, 0, 0, 0, 0, 0, 0, 0 };

        public byte[] Knob { get; set; } = { 0, 0, 0, 0, 0, 0, 0, 0 };

        public void Init(String MIDIin)
        {
            try
            {
                _inputDevice = InputDevice.GetByName(MIDIin);
                _inputDevice.EventReceived += OnEventReceived;
                _inputDevice.StartEventsListening();
                SimHub.Logging.Current.Info($"MIDIdrywet input device {MIDIin} is listening for events.");
            }
            
            catch (Exception)
            {
                SimHub.Logging.Current.Info($"Failed to find MIDIdrywet input device {MIDIin};\nKnown devices:");
                foreach (var inputDevice in InputDevice.GetAll())
                {
                    SimHub.Logging.Current.Info(inputDevice.Name);
                }
            }
        }

        public void End()
        {
            (_inputDevice as IDisposable)?.Dispose();
        }

        // callback
        void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            SimHub.Logging.Current.Info($"Event received from '{midiDevice.Name}': {e.Event}");
            if (e.Event is ControlChangeEvent foo)
            {
//              SimHub.Logging.Current.Info($"ControlNumber = '{foo.ControlNumber}'; ControlValue = '{foo.ControlValue}");
                if (8 > foo.ControlNumber)
                {
                    Slider[foo.ControlNumber] = foo.ControlValue;
                }
                else if (16 <= foo.ControlNumber && 24 > foo.ControlNumber)
                {
                    int me = foo.ControlNumber - 16;
                    Knob[me] = foo.ControlValue;
                }
            }
        }
    }
}
