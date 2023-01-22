using System;
using Melanchall.DryWetMidi;
using Melanchall.DryWetMidi.Devices;

namespace blekenbleu.MIDIspace
{
    /// <summary>
    /// from Input device https://melanchall.github.io/drywetmidi/articles/devices/Input-device.html
    /// </summary>
    public class MIDIdrywet
    {
        private static IInputDevice _inputDevice;

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
                SimHub.Logging.Current.Info($"Failed to find MIDIdrywet input device {MIDIin}.");
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
        }
    }
}
