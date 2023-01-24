using System;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using SimHub.Plugins;

namespace blekenbleu.MIDIspace
{
    /// <summary>
    /// from Output device https://melanchall.github.io/drywetmidi/articles/devices/Output-device.html
    /// </summary>
    public class OUTdrywet
    {
        private static MIDIio M;    // needed for AttachDelegate(), AddEvent() and TriggerEvent()
        private static IOutputDevice _outputDevice;
        private static IOutputDevice OutputDevice { get => _outputDevice; set => _outputDevice = value; }
        public ControlChangeEvent GetFoo;
        private bool Connected;
        private MIDIioSettings Settings;
        public String SendName;     // source property prefix 
        private String CCout;       // Output MIDI destination

        public void SendCC(byte number, byte value)
        {
            GetFoo.ControlNumber = (SevenBitNumber)number;
            GetFoo.ControlValue = (SevenBitNumber)value;
            OutputDevice.SendEvent(GetFoo);
        }

        private static byte val = 63;
        public void Ping(SevenBitNumber num)
        {
            SendCC(num, val);
            SimHub.Logging.Current.Info($"{CCout} CC{num} pinged {val}");
            val = (byte)((63 == val) ? 127 : 63);
        }

        public void SendProp(byte i)
        {
            if (Connected)
            {
                // Send available properties, if changed
                // append '0' - '7' to SendName for properties to send via MIDIout
                object data = M.PluginManager.GetPropertyValue($"{SendName}{i}");
                String input = data?.ToString();
                if ((null != input) && Settings.Sent[i] != Convert.ToByte(input))
                    SendCC(0, Settings.Sent[i] = Convert.ToByte(input));
            }
        }

        public void Init(String MIDIout, MIDIioSettings savedSettings, MIDIio that )
        {
            M = that;
            CCout = MIDIout;
            if (null == SendName)
            {
                Connected = false;  // missing data source
                return;
            }
            Connected = true;       // assume the best
            // Load settings
            Settings = savedSettings;
            try
            {
                OutputDevice = Melanchall.DryWetMidi.Devices.OutputDevice.GetByName(MIDIout);
                OutputDevice.EventSent += OnEventSent;
                OutputDevice.PrepareForEventsSending();
                SimHub.Logging.Current.Info($"OUTdrywet output is ready for {MIDIout} messages.");
                // resend saved CCs
                ControlChangeEvent foo = new ControlChangeEvent();
                for (byte i = 0; i < 8; i++)
                    SendCC(i, Settings.Sent[i]);    // time may have passed;  reinitialize MIDI destination
            }
            
            catch (Exception)
            {
                Connected = false;
                SimHub.Logging.Current.Info($"Failed to find OUTdrywet output device {MIDIout};\nKnown devices:");
                foreach (var outputDevice in Melanchall.DryWetMidi.Devices.OutputDevice.GetAll())
                    SimHub.Logging.Current.Info(outputDevice.Name);
            }
        }

        public void End()
        {
            (_outputDevice as IDisposable)?.Dispose();
            SimHub.Logging.Current.Info($"OUTdrywet.End()");
        }

        // callback
        void OnEventSent(object sender, MidiEventSentEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            // this cute syntax is called pattern matching
            if (Connected && e.Event is ControlChangeEvent foo)
            {
//              SimHub.Logging.Current.Info($"ControlNumber = {foo.ControlNumber}; ControlValue = {foo.ControlValue}");
                if (7 < foo.ControlNumber)	// unsigned
                    SimHub.Logging.Current.Info($"Mystery {CCout} ControlChangeEvent : {foo}");
            }
            else SimHub.Logging.Current.Info($"Ignoring {midiDevice.Name} {e.Event} reported for {CCout}");
        }
    }
}