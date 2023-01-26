using System;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
//using Melanchall.DryWetMidi.Multimedia;
using SimHub.Plugins;

namespace blekenbleu.MIDIspace
{
    /// <summary>
    /// from Output device https://melanchall.github.io/drywetmidi/articles/devices/Output-device.html
    /// </summary>
    internal class OUTdrywet
    {
        private static IOutputDevice _outputDevice;
        private static IOutputDevice OutputDevice { get => _outputDevice; set => _outputDevice = value; }
        private bool Connected = false;
        private byte val = 63;		// used in Ping()
        private String CCout;       	// Output MIDI destination, used by Log message

        private bool SendCC(byte control, byte value)
        {   // wasted a day not finding this documented
            try
            {
//              SimHub.Logging.Current.Info($"MIDIio OutputDevice.SendEvent() {control} {value} 0");
                OutputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber)control, (SevenBitNumber)value) {Channel = (FourBitNumber)0});
            }
            catch (Exception e)
            {
                string oops = e?.ToString();
                SimHub.Logging.Current.Info($"MIDIio SendCC()Failed: {oops}");
                return Connected = false;
            }
            return true;
        }

        internal bool Ping(SevenBitNumber num) // gets called (indirectly, event->action) by INdrywet()
        {
            if (SendCCvalue(num, val)) {
                SimHub.Logging.Current.Info($"{CCout} CC{num} pinged {val}");
                val = (byte)((63 == val) ? 127 : 63);
                return true;
            }
            else SimHub.Logging.Current.Info($"{CCout} disabled");
            return false;
        }

        // used Only for i < SendCt
        internal bool SendCCvalue(byte sv, byte input) => (Connected) && SendCC(sv, input);

        internal void Init(MIDIio M, String MIDIout)
        {
            if (null == MIDIout)
                return;
            CCout = MIDIout;
            Connected = true;       	// assume the best

            try
            {
                OutputDevice = Melanchall.DryWetMidi.Devices.OutputDevice.GetByName(MIDIout);
                OutputDevice.EventSent += OnEventSent;
                OutputDevice.PrepareForEventsSending();
                SimHub.Logging.Current.Info($"MIDIio OUTdrywet output is ready to send {MIDIout} messages.");
                // resend saved CCs
                for (byte i = 0; i < M.CCProperties.SendCt; i++)
                    SendCC(i, M.Settings.Sent[i]);    // time may have passed;  reinitialize MIDI destination
            }
            
            catch (Exception)
            {
                Connected = false;
                SimHub.Logging.Current.Info($"Failed to find OUTdrywet output device {MIDIout};\nKnown devices:");
                foreach (var outputDevice in Melanchall.DryWetMidi.Devices.OutputDevice.GetAll())
                    SimHub.Logging.Current.Info(outputDevice.Name);
            }
        }

        internal void End()
        {
            Connected = false;
            SimHub.Logging.Current.Info($"MIDIio OUTdrywet.END()");
            (_outputDevice as IDisposable)?.Dispose();
        }

        // callback
        void OnEventSent(object sender, MidiEventSentEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            // this cute syntax is called pattern matching
            if (Connected && e.Event is ControlChangeEvent foo)
            {
//              SimHub.Logging.Current.Info($"MIDIio ControlNumber = {foo.ControlNumber}; ControlValue = {foo.ControlValue}");
                if (7 < foo.ControlNumber)	// unsigned
                    SimHub.Logging.Current.Info($"Mystery {CCout} ControlChangeEvent : {foo}");
            }
            else SimHub.Logging.Current.Info($"Ignoring {midiDevice.Name} {e.Event} reported for {CCout}");
        }
    }
}
