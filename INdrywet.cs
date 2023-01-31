using System;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
//using Melanchall.DryWetMidi.Multimedia;
using SimHub.Plugins;

namespace blekenbleu.MIDIspace
{
    /// <summary>
    /// from Input device https://melanchall.github.io/drywetmidi/articles/devices/Input-device.html
    /// </summary>
    internal class INdrywet
    {
        private MIDIio M;	    	// needed for e.g. CCProperties()
        private static IInputDevice _inputDevice;
        private static IInputDevice InputDevice { get => _inputDevice; set => _inputDevice = value; }

        internal void Init(String MIDIin, MIDIioSettings savedSettings, MIDIio that)
        {
            try
            {
                InputDevice = Melanchall.DryWetMidi.Devices.InputDevice.GetByName(MIDIin);
                InputDevice.EventReceived += OnEventReceived;
                InputDevice.StartEventsListening();
                SimHub.Logging.Current.Info($"{that.my}INdrywet() is listening for {MIDIin} messages.");
            }
            
            catch (Exception)
            {
                SimHub.Logging.Current.Info($"{that.my}INdrywet() Failed to find {MIDIin};\nKnown devices:");
                foreach (var inputDevice in Melanchall.DryWetMidi.Devices.InputDevice.GetAll())
                    SimHub.Logging.Current.Info("\t" + inputDevice.Name);
            }

            M = that;
            M.Properties.Attach(M);		// AttachDelegate buttons, sliders and knobs
        }

        internal void End()
        {
            (InputDevice as IDisposable)?.Dispose();
        }

        // callback
        void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            // this cute syntax is called pattern matching
            if (e.Event is ControlChangeEvent foo)
            {
//              SimHub.Logging.Current.Info($"{M.my}ControlNumber = {foo.ControlNumber}; ControlValue = {foo.ControlValue}");
                M.Active((byte)foo.ControlNumber, (byte)foo.ControlValue);	// add unconfigured CC properties
            }
            else SimHub.Logging.Current.Info($"{M.my}INdrywet() ignoring {e.Event} received from {midiDevice.Name}");
        }
    }
}
