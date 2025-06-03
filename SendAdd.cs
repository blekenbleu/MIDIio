using SimHub.Plugins;
using System.Collections.Generic;
using System.Linq;

namespace blekenbleu
{
	internal partial class IOproperties
	{
		bool first = false;

		// configure MIDIio.ini MIDIsends Events and Actions
		void SendAdd(MIDIio I, char ABC, byte addr, string prop)
		{
			byte dt = 3;
			int ct = CCmap.Count;
			// to do: plumb CC Event triggers
			bool CCevent = "MIDIio." == prop.Substring(0, 7);
            byte cc = 0;

			if (0 == CCmap.Count)
				first = true;
			else if (!first)
				return;							// ct out of range

			CCmap.Add(0);						// entries for all events

            if (CCevent)
			{
				int L = prop.Length - 7;		// lop off 'MIDIio.'
	            string prop7 = prop.Substring(7, L);

				for (; cc < CCname.Length; cc++)
					if (L == CCname[cc].Length && CCname[cc] == prop7)
					{
						Which[cc] |= SendEvent;
						CCmap[ct] = cc;			// this entry for a CC event
						break;
					}
				if (127 < cc)
				{
					MIDIio.Log(0, MIDIio.oops = $"IOproperties.SendAdd({prop}): not found in CCname[]");
					CCevent = false;
				}
			}

			switch (ABC)
			{
				case 'A':
					if (CCevent)
						tmap[cc] = (byte)IOevent[0].Count;
					else ActMap.Add(new byte[] { 0, (byte)SourceList[0].Count });	// used by Act()
					IOevent[dt = 0].Add("Event"+ct);				   // used by TriggerEvent()
					break;
				case 'B':
					if (CCevent)
						tmap[cc] = (byte)IOevent[1].Count;
					else ActMap.Add(new byte[] { 1, (byte)SourceList[1].Count });
					IOevent[dt = 1].Add("Event"+ct);
					break;		
				case 'C':
					if (CCevent)
						tmap[cc] = (byte)IOevent[2].Count;
					else ActMap.Add(new byte[] { 2, (byte)SourceList[2].Count });
					IOevent[dt = 2].Add("Event"+ct);
					break;
				default:
					MIDIio.Log(0, MIDIio.oops = $"IOproperties.SendAdd(): unknown send type '{ABC}'");
					return;
			}

			switch (prop.Substring(0, 7))
			{
				case "MIDIio.":
					byte[] devs = new byte[DestDev.Length];
					devs[dt] = addr;
					ListCC.Add(devs);
					break;
				case "Joystic":										 		// JoyStick
					SourceList[1].Add(new Source() { Name = prop, Device = dt, Addr = addr });
					break;
				case "InputSt":											 	// any SimHub controller
					SourceList[2].Add(new Source() { Name = prop, Device = dt, Addr = addr });
					break;
				default:													// "game"
					SourceList[0].Add(new Source() { Name = prop, Device = dt, Addr = addr });
					break;
			}

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
