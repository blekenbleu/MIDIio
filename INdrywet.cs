﻿using System;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
//using Melanchall.DryWetMidi.Multimedia;		// replaces .Devices in newer versions than SimHub's

namespace blekenbleu
{
	/// <summary>
	/// from Input device https://melanchall.github.io/drywetmidi/articles/devices/Input-device.html
	/// </summary>
	internal class INdrywet
	{
		private MIDIio M;								// for M.ReceivedCC()
		private static IInputDevice _inputDevice;
		private static IInputDevice InputDevice { get => _inputDevice; set => _inputDevice = value; }

		internal bool Init(String MIDIin, MIDIio that)
		{
			try
			{
				InputDevice = Melanchall.DryWetMidi.Devices.InputDevice.GetByName(MIDIin);
				InputDevice.EventReceived += OnEventReceived;
				InputDevice.StartEventsListening();
				MIDIio.Log(4, $"INdrywet() is listening for {MIDIin} messages.");
			}
			
			catch (Exception)
			{
				string s = $"Reader.Init() Failed to find {MIDIin};\nKnown devices:";

				foreach (var inputDevice in Melanchall.DryWetMidi.Devices.InputDevice.GetAll())
					s += "\n\t" + inputDevice.Name;
				MIDIio.Info(s);
				return false;
			}
			M = that;
			return true;
		}

		// callback
		void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
		{
			if (e.Event is ControlChangeEvent CC)	// this cute syntax is called pattern matching
				M.ReceivedCC((byte)CC.ControlNumber, (byte)CC.ControlValue);	// in Send.cs
			else MIDIio.Log(2, $"Reader() ignoring {e.Event} received from {((MidiDevice)sender).Name}");
		}

		internal void End()
		{
			(InputDevice as IDisposable)?.Dispose();
		}
	}
}
