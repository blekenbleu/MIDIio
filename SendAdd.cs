using SimHub.Plugins;
using System.Collections.Generic;
using System.Linq;

namespace blekenbleu
{
	internal partial class IOproperties
	{
		bool first = false;
		internal byte[] CCevent = new byte[128];                              // CC number for applicable events
		internal List<byte[]> ActMap = new List<byte[]> { };	// SourceType[] index, SourceList[] index
		internal List<byte>[] IOevent = new List<byte>[4] {		// Event numbers per source
											new List<byte> {},	// game SourceType events
											new List<byte> {},	// Joystick axis SourceType events
											new List<byte> {},	// Joystick button SourceType events
											new List<byte> {}};	// CC events

		// configure MIDIio.ini MIDIsends Events and Actions
		void SendAdd(MIDIio I, char ABC, byte addr, string prop)	// called only in EnumActions() after all non-Event configuration
		{
			bool notCC = true;
			byte dt = 3, cc = 0, src = 0;
			byte ct = (byte)((null == ActMap) ? 0 : ActMap.Count);// ActMap gets appended for ALL Events

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

				for (; cc < CCname.Length; cc++)
					if (L == CCname[cc].Length && CCname[cc] == prop7)
						break;

				if (notCC = cc >= CCname.Length)
					MIDIio.Log(0, MIDIio.oops = $"IOproperties.SendAdd({prop}): not found in CCname[]");
				else {
					src = 3;
					Which[cc] |= SendEvent;
					if (0 == (CC & Which[cc]))
						CCprop(cc, false);
					CCevent[cc]	= ct;												// for CC Event (trigger)
					IOevent[3].Add(ct);					
					byte[] devs = new byte[DestDev.Length];
					devs[dt] = addr;
					ListCC.Add(devs);
					ActMap.Add(new byte[] { dt, (byte)SourceList[dt].Count, cc });  // used by Act()
				}
			}

			if (notCC)
				ActMap.Add(new byte[] { dt, (byte)SourceList[dt].Count });			// used by Act()

			switch (prop.Substring(0, 7))
			{
				case "Joystic":										 				// JoyStick
					src = 1;
					break;
				case "InputSt":
					src = 2;											 	// any SimHub controller
					break;
				default:													// "game"
					break;
			}
			IOevent[src].Add(ct);										// used by TriggerEvent()
			if (3 > src)
				SourceList[src].Add(new Source() { Name = prop, Device = dt, Addr = addr });

			switch (ct)				// configure action and event
			{
				case 0:
					I.AddEvent("Event0");
					I.AddAction("send0",(a, b) => I.Act(0));
					break;
				case 1:
					I.AddEvent("Event1");
					I.AddAction("send1",(a, b) => I.Act(1));
					break;
				case 2:
					I.AddEvent("Event2");
					I.AddAction("send2",(a, b) => I.Act(2));
					break;
				case 3:
					I.AddEvent("Event3");
					I.AddAction("send3",(a, b) => I.Act(3));
					break;
				case 4:
					I.AddEvent("Event4");
					I.AddAction("send4",(a, b) => I.Act(4));
					break;
				case 5:
					I.AddEvent("Event5");
					I.AddAction("send5",(a, b) => I.Act(5));
					break;
				case 6:
					I.AddEvent("Event6");
					I.AddAction("send6",(a, b) => I.Act(6));
					break;
				case 7:
					I.AddEvent("Event7");
					I.AddAction("send7",(a, b) => I.Act(7));
					break;
				case 8:
					I.AddEvent("Event8");
					I.AddAction("send8",(a, b) => I.Act(8));
					break;
				case 9:
					I.AddEvent("Event9");
					I.AddAction("send9",(a, b) => I.Act(9));
					break;
				case 10:
					I.AddEvent("Event10");
					I.AddAction("send10",(a, b) => I.Act(10));
					break;
				case 11:
					I.AddEvent("Event11");
					I.AddAction("send11",(a, b) => I.Act(11));
					break;
				case 12:
					I.AddEvent("Event12");
					I.AddAction("send12",(a, b) => I.Act(12));
					break;
				case 13:
					I.AddEvent("Event13");
					I.AddAction("send13",(a, b) => I.Act(13));
					break;
				case 14:
					I.AddEvent("Event14");
					I.AddAction("send14",(a, b) => I.Act(14));
					break;
				case 15:
					I.AddEvent("Event15");
					I.AddAction("send15",(a, b) => I.Act(15));
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
