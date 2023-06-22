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
		private String CCout;	   			// Output MIDI destination, used by log messages

		private bool SendCC(byte control, byte value)
		{   // wasted a day not finding this documented
			try
			{
				MIDIio.Log(8, $"SendCC(): OutputDevice.SendEvent({control}, {value}, 0)");
				OutputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber)control,
									   (SevenBitNumber)value) {Channel = (FourBitNumber)0});
			}
			catch (Exception e)
			{
				string oops = e?.ToString();
				MIDIio.Info("SendCC() Failed: " + oops);
				return Connected = false;
			}
			return true;
		}

		internal bool SendCCval(byte sv, byte input) => (Connected) && SendCC(sv, input);
		internal byte Latest = 0;								// needs to get set by INdrywet()
		internal bool Ping(SevenBitNumber num)					// gets called (indirectly, event->action) by INdrywet()
		{
			if (SendCCval(num, Latest)) {						// Ping(): drop pass from Active()
				MIDIio.Info($"Ping(): {CCout} CC{num} {Latest}");
				return true;
			}
			else MIDIio.Info(CCout + " disabled");
			return false;
		}

		internal void Init(String MIDIout)
		{
			CCout = MIDIout;
			Connected = true;	   								// assume the best

			try
			{
				OutputDevice = Melanchall.DryWetMidi.Devices.OutputDevice.GetByName(MIDIout);
				OutputDevice.EventSent += OnEventSent;
				OutputDevice.PrepareForEventsSending();
				MIDIio.Info("OUTwetdry(): Found " + MIDIout);
			}
			catch (Exception)
			{
				Connected = false;
				MIDIio.Size[2] = 0;
				string s = $"OUTdrywet.Init():  Failed to find {MIDIout};  found devices:";
				foreach (var outputDevice in Melanchall.DryWetMidi.Devices.OutputDevice.GetAll())
					s += "\n\t" + outputDevice.Name;
				MIDIio.Info(s + "\n");
			}
		}

		internal void End()
		{
			Connected = false;
			(_outputDevice as IDisposable)?.Dispose();
		}

		static byte mystery = 0;
		// callback
		void OnEventSent(object sender, MidiEventSentEventArgs e)
		{
			var midiDevice = (MidiDevice)sender;
			// this cute syntax is called pattern matching
			if (Connected && e.Event is ControlChangeEvent CC)
			{
				MIDIio.Log(8, $"OnEventSent():  ControlNumber = {CC.ControlNumber}; ControlValue = {CC.ControlValue}");
				if ((0 == MIDIio.Properties.Which[CC.ControlNumber]) && !MIDIio.DoEcho && 5 > mystery)		// unassigned ?
				{
					MIDIio.Info("OnEventSent(): Mystery CC{CC.ControlNumber}" + MIDIio.Properties.CCname[CC.ControlNumber]);
					mystery++;
				}
			}
			else MIDIio.Info($"OnEventSent(): Ignoring {midiDevice.Name} {e.Event} reported for {CCout}");
		}
	}
}
