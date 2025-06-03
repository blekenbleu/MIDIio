using SimHub.Plugins;
using System.Collections.Generic;
using System.Linq;

namespace blekenbleu
{
	internal partial class IOproperties
	{
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

		void InitCC()
		{
			byte ct, j;

			Send = new List<string> {};
			CCmap = new List<byte> {};
			CCtype = new string[] { "unconfigured", "slider", "knob", "button" };
			Wflag = new byte[] { Unc, CC, CC, SendEvent };						// Which type flag bits
			Which = new byte[128];				  							// OUTwetdry.Init() resends unconfigured CCs on restart
			// selectively replaced by configured slider0-n, knob0-n, button0-n:
			CCname = new string[128];				   						// Initialized to CC[0-128]
			Map = new byte[128];
 
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

        /// <summary>
        /// Called by Init(); adds source property data to CCname[], Which[]
        /// </summary>
		private int WhichCC(byte dt, byte DevAddr, string prop)
		{
			byte cc;
			int L;
			string prop7 = prop.Substring(7, L = prop.Length - 7);	// lop off 'MIDIio.'

			for (cc = 0; cc < CCname.Length; cc++)
				if (L == CCname[cc].Length && CCname[cc] == prop7)
				{
					Which[cc] &= (byte)(~Unc);						// no longer unconfigured
					Which[cc] |= CC;								// perhaps already configured as a SendEvent

					if (0 < (56 & Which[cc]))						// already a ListCC[] for this cc?
					{
						try {
                        	ListCC[dt][Map[cc]] = DevAddr;
						}
						catch {};
					}
					else
					{
						Map[cc] = (byte)ListCC.Count;
						byte[] devs = new byte[DestDev.Length];
						devs[dt] = DevAddr;
						ListCC.Add(devs);
					}
					Which[cc] |= (byte)(8 << dt);					// ActionCC() checks Which[] flags
					return cc;
				}

			MIDIio.Info($"WhichCC() unrecognized {DestDev[dt]} property:  {prop}");
			return -1;
		}
	}
}
