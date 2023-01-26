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
    internal class INdrywet
    {
        private MIDIio M;	    	// needed for e.g. CCProperties()
        private MIDIioSettings Settings;
        private static IInputDevice _inputDevice;
        private static IInputDevice InputDevice { get => _inputDevice; set => _inputDevice = value; }

        internal void Init(String MIDIin, MIDIioSettings savedSettings, MIDIio that)
        {
            try
            {
                InputDevice = Melanchall.DryWetMidi.Devices.InputDevice.GetByName(MIDIin);
                InputDevice.EventReceived += OnEventReceived;
                InputDevice.StartEventsListening();
                SimHub.Logging.Current.Info($"MIDIio INdrywet input is listening for {MIDIin} messages.");
            }
            
            catch (Exception)
            {
                SimHub.Logging.Current.Info($"MIDIio Failed to find INdrywet input device {MIDIin};\nKnown devices:");
                foreach (var inputDevice in Melanchall.DryWetMidi.Devices.InputDevice.GetAll())
                {
                    SimHub.Logging.Current.Info("   " + inputDevice.Name);
                }
            }

            Settings = savedSettings;
            M = that;
            M.CCProperties.Attach(M);		// AttachDelegate buttons, sliders and knobs
        }

        internal MIDIioSettings End()
        {
            SimHub.Logging.Current.Info($"MIDIio MIDIioSettings End: CCbits #{Settings.CCbits[0]}, #{Settings.CCbits[1]}");
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
//              SimHub.Logging.Current.Info($"MIDIio ControlNumber = {foo.ControlNumber}; ControlValue = {foo.ControlValue}");
                M.CCProperties.Active(M, (byte)foo.ControlNumber, (byte)foo.ControlValue);	// potentially add unconfigured CC properties
            }
            else SimHub.Logging.Current.Info($"MIDIio ignoring {e.Event} received from {midiDevice.Name}");
        }
    }
}
