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
        private MIDIio I;
        private bool Connected = false;
        private String CCout;       	// Output MIDI destination, used by Log message

        private bool SendCC(byte control, byte value)
        {   // wasted a day not finding this documented
            try
            {
//              SimHub.Logging.Current.Info($"{I.my}SendCC(): OutputDevice.SendEvent() {control} {value} 0");
                OutputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber)control, (SevenBitNumber)value) {Channel = (FourBitNumber)0});
            }
            catch (Exception e)
            {
                string oops = e?.ToString();
                SimHub.Logging.Current.Info($"{I.my}SendCC()Failed: {oops}");
                return Connected = false;
            }
            return true;
        }

        internal bool SendCCval(byte sv, byte input) => (Connected) && SendCC(sv, input);
        internal byte Latest = 0;		// needs to get set by INdrywet()
        internal bool Ping(SevenBitNumber num)	// gets called (indirectly, event->action) by INdrywet()
        {
            if (SendCCval(num, Latest)) {					// drop pass from Active()
                SimHub.Logging.Current.Info($"{CCout} CC{num} pinged {Latest}");
                return true;
            }
            else SimHub.Logging.Current.Info($"{CCout} disabled");
            return false;
        }

        internal void Init(MIDIio M, String MIDIout, int count)
        {
            I = M;			// only for logging
            if (null == MIDIout)
                return;
            CCout = MIDIout;
            Connected = true;       	// assume the best

            try
            {
                OutputDevice = Melanchall.DryWetMidi.Devices.OutputDevice.GetByName(MIDIout);
                OutputDevice.EventSent += OnEventSent;
                OutputDevice.PrepareForEventsSending();
                SimHub.Logging.Current.Info($"{M.my}OUTwetdry is ready to send CC messages to {MIDIout}.");
                byte j = 0;
                for (byte i = 0; j < count && i < 128; i++)	// resend saved CCs
                {
                    if (3 < M.Properties.Which[i])		// unconfigured CC number?
                    {
                        SendCC(i, M.Settings.Sent[i]);		// time may have passed;  reinitialize MIDIout
                        j++;
                    }
                }
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
            (_outputDevice as IDisposable)?.Dispose();
        }

        // callback
        void OnEventSent(object sender, MidiEventSentEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            // this cute syntax is called pattern matching
            if (Connected && e.Event is ControlChangeEvent foo)
            {
//              SimHub.Logging.Current.Info($"{I.my}ControlNumber = {foo.ControlNumber}; ControlValue = {foo.ControlValue}");
                if (7 < foo.ControlNumber)	// unsigned
                    SimHub.Logging.Current.Info($"{I.my}OnEventSent(): Mystery {CCout} ControlChangeEvent : {foo}");
            }
            else SimHub.Logging.Current.Info($"{I.my}OnEventSent(): Ignoring {midiDevice.Name} {e.Event} reported for {CCout}");
        }
    }
}
