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
        private String CCout;       	// Output MIDI destination, used by log messages

        private bool SendCC(byte control, byte value)
        {   // wasted a day not finding this documented
            try
            {
                I.Log(8, $"SendCC(): OutputDevice.SendEvent({control}, {value}, 0)");
                OutputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber)control, (SevenBitNumber)value) {Channel = (FourBitNumber)0});
            }
            catch (Exception e)
            {
                string oops = e?.ToString();
                I.Info("SendCC() Failed: " + oops);
                return Connected = false;
            }
            return true;
        }

        internal bool SendCCval(byte sv, byte input) => (Connected) && SendCC(sv, input);
        internal byte Latest = 0;		// needs to get set by INdrywet()
        internal bool Ping(SevenBitNumber num)	// gets called (indirectly, event->action) by INdrywet()
        {
            if (SendCCval(num, Latest)) {        		// Ping(): drop pass from Active()
                I.Info($"Ping(): {CCout} CC{num} {Latest}");
                return true;
            }
            else I.Info(CCout + " disabled");
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
                I.Log(4, "OUTwetdry() is ready to send CC messages to " + MIDIout + ".");
                byte j = 0;
                if (I.DoEcho)
                    for (byte i = 0; j < count && i < 128; i++)				// resend saved CCs
                    {
                        if (0 < (M.Properties.unconfigured & M.Properties.Which[i]))	// unconfigured CC number?
                        {
                            SendCC(i, M.Settings.Sent[i]);		// much time may have passed;  reinitialize MIDIout device
                            j++;
                        }
                    }
            }
            
            catch (Exception)
            {
                Connected = false;
                I.Info("Init(): Failed to find MIDIout device " + MIDIout + ";\nKnown devices:");
                foreach (var outputDevice in Melanchall.DryWetMidi.Devices.OutputDevice.GetAll())
                    I.Info("Init(): " +outputDevice.Name);
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
            if (Connected && e.Event is ControlChangeEvent CC)
            {
                I.Log(8, $"OnEventSent():  ControlNumber = {CC.ControlNumber}; ControlValue = {CC.ControlValue}");
                if ((I.Properties.SendCt[0] <= I.Properties.Unmap[CC.ControlNumber]) && !I.DoEcho)	// unassigned ?
                    I.Info("OnEventSent(): Mystery " + I.Properties.CCname[CC.ControlNumber]);
            }
            else I.Info($"OnEventSent(): Ignoring {midiDevice.Name} {e.Event} reported for {CCout}");
        }
    }
}
