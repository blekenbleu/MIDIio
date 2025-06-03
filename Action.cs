using SimHub.Plugins;
using System.Collections.Generic;
using System.Linq;

namespace blekenbleu
{
	internal partial class IOproperties
	{
		private  List<string> Send;					  		// send Actions
		internal byte[]		Map;							// Map CCnumber to ListCC[]
		// CCn names get replaced by configured CCtype's
		internal string[]   CCname, CCtype;			   		// for AttachDelegate()
		internal byte[]	 	Which, Wflag;					// CCtype
		internal readonly byte CC = 1, SendEvent = 2, Unc = 4;	// Wflag[] input type bits for Which[]
		// CC source properties, sent in INdrywet.OnEventReceived() by ActionCC()
		internal List<byte[]> ListCC = new List<byte[]> { };	
		internal List<byte[]> ActMap = new List<byte[]> { };
		internal List<string>[] IOevent = new List<string>[3] { new List<string> { }, new List<string> { }, new List<string> { } };
		internal byte[] tmap = new byte[128];				// map received CCs to Event triggers
		internal List<byte> CCmap;
		bool first = true;

		void InitCC()
		{
			byte ct, j;

			Send = new List<string> {};
			CCmap = new List<byte> {};
 
			for (ct = j = 0; j < 128; j++)
			{
				CCname[j] = "CC" + j;
				tmap[j] = 0;
			}

			if (MIDIio.DoEcho)
				for (j = 0; j < 128; j++)									// extract unconfigured CC flags
				{
					if (0 < (0x80 & M.Settings.CCvalue[j]))
					{
						Which[j] = Unc;
						ct++;
						M.Settings.CCvalue[j] &= 0x7F;
						M.Outer.SendCCval(j, M.Settings.CCvalue[j]); 		// reinitialize MIDIout device
					}
					else Which[j] = 0;
				}
			else for (j = 0; j < M.Settings.CCvalue.Length; j++)
				M.Settings.CCvalue[j] = 0;
			if (0 < ct)
				MIDIio.Log(4, $"IOProperties.InitCC(): {ct} CCs resent after restart");

			// Collect configured CCname[] and other Which[] properties from MIDIio.ini
			for (ct = 1; ct < CCtype.Length; ct++)				// skip CCtype[0]: Unconfigured
			{
				string type = MIDIio.Ini + CCtype[ct];			// "slider", "knob", "button"
				string property = M.PluginManager.GetPropertyValue(type + 's')?.ToString();
				if (null == property && MIDIio.Info($"Init(): '{type + 's'}' not found"))
					continue;

				// bless the Internet
				byte[] array = property.Split(',').Select(byte.Parse).ToArray();
//				MIDIio.Log(4, $"Init(): '{MIDIio.Ini + CCtype[ct]}' {string.Join(",", array.Select(p => p.ToString()).ToArray())}");

				j = 0;
				foreach (byte cc in array)		  // array has cc numbers assigned for this type
				{
					if (128 > cc)
					{
						if (0 == (3 & Which[cc]))
						{
							Which[cc] = Wflag[ct];
							CCname[cc] = CCtype[ct] + j++;	  // replacing "CCcc"
//							MIDIio.Log(4, $"IOProperties.Init(): CCname[{cc}] = " + CCname[cc]);
						}
						else MIDIio.Info($"Init({type + j}):  {cc} already configured as {CCname[cc]}");
					}
					else MIDIio.Info($"Init({type + j}) invalid CC value: {cc}");
				}
			}	  // ct < CCtype.Length
		}	// all configured MIDIin properties are now in CCname[] and Which[]

		internal void EnumActions(MIDIio I, PluginManager pluginManager, string[] actions)
		{
			for (byte a = 0; a < actions.Length; a++)
				if (2 > actions[a].Length)
					MIDIio.Log(0, $"IOproperties.EnumActions({actions[a]}): invalid MIDIsends value");
				else
				{
					string s = MIDIio.Ini + "send" + actions[a];
					string prop = (string)pluginManager.GetPropertyValue(s);

					if (null == prop || 8 > prop.Length)
						MIDIio.Log(0, $"IOproperties.Action({s}):  dubious property name :" + prop);
					if (byte.TryParse(actions[a].Substring(1), out byte addr))
						SendAdd(I, actions[a][0], addr, prop);
					else MIDIio.Log(0, $"IOproperties.Action({actions[a]}): invalid byte address");
				}
			MIDIio.Log(4, "Leaving EnumActions");
        }

		void SendAdd(MIDIio I, char ABC, byte addr, string prop)
		{
			byte dt = 3;
			int ct = CCmap.Count;
			// to do: plumb CC Event triggers
			bool CCevent = "MIDIio." == prop.Substring(0, 7);
            byte cc = 0;

			CCmap.Add(0);

            if (CCevent)
			{
				int L;
	            string prop7 = prop.Substring(7, L = prop.Length - 7);  // lop off 'MIDIio.'

				for (cc = 0; cc < CCname.Length; cc++)
					if (L == CCname[cc].Length && CCname[cc] == prop7)
					{
						Which[cc] |= SendEvent;
						CCmap[ct] = cc;
						break;
					}
				if (127 < cc)
					MIDIio.Log(0, $"IOproperties.SendAdd({prop}): not found in CCname[]");
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
					MIDIio.Info($"IOproperties.SendAdd(): unknown send type '{ABC}'");
					break;
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

		bool NoDup(string prop, ref List<string> plist)
		{
			if (null == prop)
				return false;

			for (int i = plist.Count - 1; i >= 0; i--)
				if (plist[i] == prop)
					return false;

			plist.Add(prop);
			return true;
		}

		// call CCprop to AttachDelegate() configured MIDIin properties
		internal void Attach(MIDIio I)
		{
			byte j, cc, st;

			if (MIDIio.Log(4, ""))
			{
				string s = "Attach() non-MIDI source properties:\n";
				List<string> nonMIDI = new List<string>();

				// search thru all non-MIDI source
				for (st = 0; st < 3; st++)
					for (j = 0; j < SourceList[st].Count; j++)
						if(NoDup(SourceList[st][j].Name, ref nonMIDI))
							s += "\t" + SourceList[st][j].Name + "\n";
				MIDIio.Info(s);
			}

			for (cc = 0; cc < 128; cc++)
				if (0 < (3 & Which[cc]))
				{
//					MIDIio.Log(4, $"CCprop({CCname[cc]})"); 
 					CCprop(I, cc);				// set property for configured input
					if (SendEvent == Which[cc])
						Which[cc] |= CC;
				}

			// MIDIin property configuration is now complete

			if (MIDIio.DoEcho)
			{
				for (j = cc = 0; cc < 128; cc++)
					if (0 < (Unc & Which[cc]))
					{
						CCprop(I, cc);			// restore previous received unconfigured CCs
						j++;
					}
					if (0 < j)
						MIDIio.Log(4, $"Attach():  {j} previous CC properties restored");
			}
		}	// Attach()

		internal void End(MIDIio I)
		{
			byte ct; 

			for (byte i = ct = 0; MIDIio.DoEcho && i < 128; i++)
				if (Unc == Which[i])
				{
					I.Settings.CCvalue[i] |= 0x80; // flag unconfigured CCs to restore
					ct++;
				}
			if (0 < ct)
				MIDIio.Log(4, $"IOProperties.End():  {ct} unconfigured CCs");
		}
	}
}
