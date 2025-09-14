using SimHub.Plugins;
using System.Collections.Generic;

namespace blekenbleu
{
	public partial class MIDIio
	{
		bool first = false;
		internal byte[] CCevent = new byte[128];				// CC number for applicable events
		// map Action target number to src and index for SourceList[]
		internal List<byte[]> ActList = new List<byte[]> { };	// Act() target src, SourceList[src] index or CC parms
		// map SourceList source and index to Event trigger number
		internal List<byte>[] IOevent = new List<byte>[3] {		// Event numbers per nonCC source
											new List<byte> {},	// game SourceType events
											new List<byte> {},	// Joystick axis SourceType events
											new List<byte> {}};	// Joystick button SourceType events

		// configure MIDIio.ini MIDIsends Events and Actions
		internal void SendAdd(char ABC, byte addr, string prop)	// called only in EnumActions() after all non-Event configuration
		{
			bool notCC = true;
			byte dt = 3, cc = 0, src = 0;
			byte ct = (byte)((null == ActList) ? 0 : ActList.Count);// ActList gets appended for ALL Events

			if (0 == ct)
				first = true;
			else if (!first)
				return;							// ct out of range

			switch (ABC)
			{
				case 'A':
					dt = 0;
					break;
				case 'B':
					dt = 1;
					break;		
				case 'C':
					dt = 2;
					break;
				default:
					MIDIio.Log(0, MIDIio.oops = $"IOproperties.SendAdd(): unknown send type '{ABC}'");
					return;
			}

			if ("MIDIio." == prop.Substring(0, 7))
			{
				int L = prop.Length - 7;		// lop off 'MIDIio.'
				string prop7 = prop.Substring(7, L);

				for (; cc < MidiProps.CCname.Length; cc++)
					if (L == MidiProps.CCname[cc].Length && MidiProps.CCname[cc] == prop7)
						break;

				if (notCC = cc >= MidiProps.CCname.Length)
					MIDIio.Log(0, MIDIio.oops = $"IOproperties.SendAdd({prop}): not found in CCname[]");
				else {
					src = 3;
					MidiProps.Which[cc] |= MidiProps.SendEvent;
					if (0 == (MidiProps.CC & MidiProps.Which[cc]))
						MidiProps.CCprop(cc, false);
					CCevent[cc]	= ct;												// for CC Event (trigger)
					ActList.Add(new byte[] { src, cc, dt, addr }); 					// used by Act() (target)
				}
			}

			switch (prop.Substring(0, 7))
			{
				case "Joystic":										 				// JoyStick
					src = 1;
					break;
				case "InputSt":
					src = 2;														// any SimHub controller
					break;
				default:															// "game"
					if (notCC)
					src = 0;
					break;
			}
			if (3 > src)
			{
				IOevent[src].Add(ct);													// used by TriggerEvent()
				MidiProps.SourceList[src].Add(new Source() { Name = prop, Device = dt, Addr = addr });
				ActList.Add(new byte[] { src, (byte)MidiProps.SourceList[dt].Count });			// used by Act()
			}


			switch (ct)				// configure action and event
			{
				case 0:
					this.AddEvent("Event0");
					this.AddAction("send0",(a, b) => this.Act(0));
					break;
				case 1:
					this.AddEvent("Event1");
					this.AddAction("send1",(a, b) => this.Act(1));
					break;
				case 2:
					this.AddEvent("Event2");
					this.AddAction("send2",(a, b) => this.Act(2));
					break;
				case 3:
					this.AddEvent("Event3");
					this.AddAction("send3",(a, b) => this.Act(3));
					break;
				case 4:
					this.AddEvent("Event4");
					this.AddAction("send4",(a, b) => this.Act(4));
					break;
				case 5:
					this.AddEvent("Event5");
					this.AddAction("send5",(a, b) => this.Act(5));
					break;
				case 6:
					this.AddEvent("Event6");
					this.AddAction("send6",(a, b) => this.Act(6));
					break;
				case 7:
					this.AddEvent("Event7");
					this.AddAction("send7",(a, b) => this.Act(7));
					break;
				case 8:
					this.AddEvent("Event8");
					this.AddAction("send8",(a, b) => this.Act(8));
					break;
				case 9:
					this.AddEvent("Event9");
					this.AddAction("send9",(a, b) => this.Act(9));
					break;
				case 10:
					this.AddEvent("Event10");
					this.AddAction("send10",(a, b) => this.Act(10));
					break;
				case 11:
					this.AddEvent("Event11");
					this.AddAction("send11",(a, b) => this.Act(11));
					break;
				case 12:
					this.AddEvent("Event12");
					this.AddAction("send12",(a, b) => this.Act(12));
					break;
				case 13:
					this.AddEvent("Event13");
					this.AddAction("send13",(a, b) => this.Act(13));
					break;
				case 14:
					this.AddEvent("Event14");
					this.AddAction("send14",(a, b) => this.Act(14));
					break;
				case 15:
					this.AddEvent("Event15");
					this.AddAction("send15",(a, b) => this.Act(15));
					break;
				default:
					if (first)
						MIDIio.Info(MIDIio.oops = $"IOproperties.SendAdd(): Action {ct} out of range");
					first = false;							// reporting once should suffice
					break;
			}
		}
	}
}
