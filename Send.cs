using SimHub.Plugins;
using System;

namespace blekenbleu
{
	public partial class MIDIio
	{
/*
		; Properties.SourceList[][].Name: non-MIDI property names
		; My + CCname[cc] MIDI property names correspond to Settings.CCvalue[cc]
		;
		; Accomodate device property range differences:
		; 0 <= MIDI cc and property < 128
		; 0 <= JoyStick property <= VJDmaxval
	 	; 0 <= ShakeIt property <= 100.0
 */
		/// <summary>
		/// Called by SendIf() and ActionCC() to send each property change
		/// dev: (destination): 0=VJD.Axis; 1=VJD.Button; 2=Outer.SendCCval
		///	addr: destination 
		/// Prop: src property name for debugging
		/// </summary>
		internal void Send(ushort value, byte dev, byte addr)
		{
			bool b;

			switch (dev)
			{
				case 0:
					if (VJD.Usage.Length > addr)
					{
						VJD.Axis(addr, value);						// 0-based axes
						VJsent = $"Send():  Axis{addr} {value} from " + Prop;
					}
					else Info($"Send({IOproperties.DestDev[dev]}): invalid axis {addr} from " + Prop);
					break;
				case 1:												// 0-based SimHub buttons vs vJoy 1-based...
					VJD.Button(++addr, b = VJDmaxval < (2*value));	// VJDmaxval-based threshold
					VJsent = $"Send():  Button{addr} " + (b ? "ON" : "OFF") + " from " + Prop;
					break;
				case 2:
					Outer.SendCCval(addr, (byte)(0x7F & value));
					break;
				default:											// should be impossible
					Log(1, $"Send():  mystery destination {dev}, address {addr} from " + Prop);
					break;
			}
		}															// Send()

		// actual values get set in IOproperties.cs;  used in Send.cs
		internal byte[] stop = new byte[] {2, 2, 2};		// non-MIDIio Events start here in SourceList[]
		bool ignore = true;

		/// <summary>
		/// Called by DataUpdate(); calls Send()
		/// index: 0=game; 1=Joystick Source property types
		/// https://github.com/blekenbleu/MIDIio/blob/main/docs/Which.md
		/// </summary>
		private void SendIf(PluginManager pluginManager, byte always)		// 0: game;	1: always
		{																	// ActionCC handles 3: CC
			byte dev, address;												// dev: destination type
			string property;

			for (byte src = always; src < 3; src++)
				for (byte p = 0; p < Properties.SourceList[src].Count; p++)	// property type index
				{
					string name = Properties.SourceList[src][p].Name;
                    dev = Properties.SourceList[src][p].Device;
					address = Properties.SourceList[src][p].Addr;

					if (!Once[src][p])
						continue;											// skip unavailable properties

					if (null == (property = pluginManager.GetPropertyValue(name)?.ToString()))
					{
						if (2 == src || 1 == always)						// null buttons until pressed
							continue;

						if (Once[src][p]) {									// 0 == always:  game running
							Once[src][p] = false;							// unavailable property
							Log(1, oops = $"SendIf({IOproperties.DestDev[dev]}): null "
								+ "{Prop = name} property from SourceList[{src}][{p}].Name");
							continue;
						}
					}
					else if (0 == property.Length)
					{
						Log(1, oops = $"SendIf({IOproperties.DestDev[dev]}): 0 length {Prop = name}");
						continue;
					}
					double stuff = Convert.ToDouble(property);
					if (0 > stuff)
						continue;

					ushort value = (ushort)(0.5 + scale[dev, src] * stuff);
					if (p >= Sent[src].Length)
					{
						oops = $"SendIf():  {p} > Sent[{src}].Length {Sent[src].Length} for {Prop = name}";
						continue;
					}

                     if (value == Sent[src][p])						// previous value for Source property index p
						continue;

					if (222 !=Sent[src][p])
						ignore = false;								// ignore the first change
                    Sent[src][p] = value;							// changed values
					if (ignore)										
						continue;									// first time thru: set initial Sent[][] values
				
                    Prop = name;
					if (p < stop[src])								// higher p are for Events
						Send(value, dev, address);
					else if (0 == always) {
						this.TriggerEvent(Trigger = "Event"+Properties.IOevent[src][p - stop[src]]);
						Log(4, Trigger = "SendIf():  " + Trigger + " = " + name);
					}
				}
		}			// SendIf()


		// called for SimHub Actions
		internal void Act(ushort a)
		{
			byte src = Properties.ActMap[a][0];
	 		byte p = Properties.ActMap[a][1];
			byte dev = Properties.SourceList[src][p].Device;
			byte addr = Properties.SourceList[src][p].Addr;

			if (3 == src)
			{
				Send((ushort)(0.5 + scale[dev, src] * Settings.CCvalue[Properties.ActMap[a][2]]), dev, addr);
				Action = $"Act({a}):  {Properties.CCname[Properties.ActMap[a][2]]}"; 
			} else {
				Send(Sent[src][p], dev, addr);
				Action = $"Act({a}):  {Properties.SourceList[src][p].Name}";
			}
		}

		/// <summary>
		/// Called by INdrywet OnEventReceived() for each MIDIin ControlChangeEvent
		/// track active CCs and save values
		/// Send CCs to any of 3 DestDev[]s
		/// trigger configured Events
		/// https://github.com/blekenbleu/MIDIio/blob/main/docs/Which.md
		/// </summary>
		internal void ActionCC(byte CCnumber, byte value)
		{
			byte which = Properties.Which[CCnumber];

			CCin = $"ActionCC({CCnumber}, {value})";						// debug property
			if (0 < which && Settings.CCvalue[CCnumber] == value)
				return;														// ignore known unchanged values

			Prop = Properties.CCname[CCnumber];								// debug
			Settings.CCvalue[CCnumber] = value;
			if (0 < (Properties.SendEvent & which))
				this.TriggerEvent(Trigger = "Event" + Properties.CCevent[CCnumber]);

			if (0 < (14 & which))                                           // flags 2+4+8:  call Send()?
			{
				bool sent = false;
				for (byte dt = 0; dt < Properties.ListCC.Count; dt++)		// at most one Send() per DestDev and CC
					if (0 < ((2 << dt) & which))							// DestDev flag
					{
						byte  address = Properties.ListCC[dt][Properties.Map[CCnumber]];
						ushort rescaled = (ushort)(0.5 + scale[dt, 3] * value);
						Send(rescaled, dt, address);
						sent = true;
					}
				if (!sent)
					Log(2, oops = $"ActionCC({Prop}):  unsent");
				return;
			}

			if (DoEcho)
				Outer.SendCCval(CCnumber, value);

			if (0 == which)
				Properties.CCprop(CCnumber, true);						// dynamic CC configuration
		}	// ActionCC()
	}
}
