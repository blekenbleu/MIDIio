using SimHub.Plugins;
using System;

namespace blekenbleu
{
	public partial class MIDIio
	{
/*
 ; Properties.SourceName[][] is an array of property names for other than MIDI
 ; My + CCname[cc] MIDI properties correspond to Settings.CCvalue[cc]
 ;
 ; Accomodate device value range differences:
 ; 0 <= MIDI cc and value < 128
 ; 0 <= JoyStick property <= VJDmaxval
 ; 0 <= ShakeIt property <= 100.0
 */
		/// <summary>
		/// Called by SendIf() and Active() for each property change sent;
		/// d (destination): 0=VJD.Axis; 1=VJD.Button; 2=Outer.SendCCval;	i: destination address
		/// s (source): 0=game; 1=Joy axis, 2=Joy button 3=MIDIin;			p: source address
		/// prop: source property name for error log
		/// </summary>
		internal void Send(double property, byte d, byte i, byte p, byte s, string prop)
		{
			int	a = (int)(0.5 + scale[d, s] * property);

			if ( 0 > a || (3 > d && Sent[s][p] == a))		// Active() discards CC repeats
				return;										// send only changed values

			Sent[s][p] = a;
			switch (d)
			{
				case 0:
					if (VJD.Usage.Length > i)
						VJD.Axis(i, a);						// 0-based axes
					else Info($"Send({Properties.DestType[d]}): invalid axis {i} from {prop}");
					break;
				case 1:										// SimHub lists 0-based buttons; vJoy wants 1-based...
					VJD.Button(++i, VJDmaxval < (2*a));	// VJDmaxval-based threshold
					break;
				case 2:
					Outer.SendCCval(i, (byte)(0x7F & a));
					break;
				default:									// should be impossible
					Log(1, $"Send(): property {prop} for mystery destination {d} with source {s}, address {p}");
					break;
			}
		}													// Send()

		/// <summary>
		/// Called by DataUpdate(); calls Send()
		/// index: 0=game; 1=Joystick Source property types
		/// https://github.com/blekenbleu/MIDIio/blob/main/docs/Which.md
		/// </summary>
		private void SendIf(PluginManager pluginManager,
							byte always)									// 0: game;	1: always
		{																	// Active handles 3: CC
			byte dt, address;												// dt: destination type
			string value;

			for (byte source = always; source < 3; source++)
				for (byte p = 0; p < Properties.SourceCt[source]; p++)		// index properties of a type
				{
					prop = Properties.SourceName[source][p];				// source: game, axis, button, CC
					dt = Properties.SourceArray[source, 0, p];				// dt (destination): Axis, Button or CC
					address = Properties.SourceArray[source, 1, p];

					if (!Once[source][p])
						continue;											// skip unavailable properties

					if (null == (value = pluginManager.GetPropertyValue(prop)?.ToString()))
					{
						if (2 == source || 1 == always)						// null buttons until pressed
							continue;

						if (Once[source][p]) {								// 0 == always:  game running
							Once[source][p] = false;						// configured property not available
							Log(1, $"SendIf({Properties.DestType[dt]}): null {prop} from SourceName[{source}][{p}]");
						}
					}
					else if (0 == value.Length)
						Log(1, $"SendIf({Properties.DestType[dt]}): 0 length {prop}");
					else Send(Convert.ToDouble(value), dt, address, p, source, prop);
				}
		}			// SendIf()


		/// <summary>
		/// Called by INdrywet OnEventReceived() for each MIDIin ControlChangeEvent
		/// track active CCs and save values
		/// https://github.com/blekenbleu/MIDIio/blob/main/docs/Which.md
		/// </summary>
		internal bool Active(byte CCnumber, byte value)						// returns true if first time
		{
			byte which = Properties.Which[CCnumber];

			CCin = $"Active({CCnumber}, {value})";
			if (0 < which && Settings.CCvalue[CCnumber] == value)
				return false;												// ignore known unchanged values

			Settings.CCvalue[CCnumber] = value;
			if (0 < (Properties.Button & which))
			{
				Outer.Latest = value;										// drop pass to Ping()
				this.TriggerEvent(Properties.CCname[CCnumber]);
			}
			if (0 < (56 & which))                                           // call Send()?
			{
				for (byte dt = 0; dt < Properties.Route.Length; dt++)		// at most one Send() per DestType and CC
					if (0 < (Properties.Route[dt] & which))					// DestType flag
					{
						byte address = Properties.CCarray[dt, Properties.Map[CCnumber]];
						Send(value, dt, address, 0, 3, Properties.CCname[CCnumber]);
					}

				return false;
			}

			if (DoEcho)
				Outer.SendCCval(CCnumber, value);

			if (0 < which)
				return false;

			Properties.Which[CCnumber] = Properties.Unc;					// First time CC number seen
			return Properties.CCprop(this, CCnumber);						// dynamic CC configuration
		}	// Active()
	}
}
