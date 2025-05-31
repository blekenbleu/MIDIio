using SimHub.Plugins;
using System.Collections.Generic;
using System.Linq;

namespace blekenbleu
{
	internal partial class IOproperties
	{
		private  string[]	Send;					  		// send[0-7] Actions
		internal byte[]		Map;							// Map CCnumber to ListCC[]
		// CCn names get replaced by configured CCtype's
		internal string[]   CCname, CCtype;			   		// for AttachDelegate()
		internal byte[]	 	Which, Wflag;					// CCtype
		internal readonly byte CC = 1, Button = 2, Unc = 4;	// Wflag[] input type bits for Which[]
		// CC source properties, sent in INdrywet.OnEventReceived() by ActionCC()
		internal List<byte[]> ListCC = new List<byte[]> { };	

		void InitCC(MIDIio I, byte CCSize)
		{
			byte ct, j;

			Send = new string[CCSize];
			for (j = 0; j < CCSize; j++)
				Send[j] = "send" + j;

			for (ct = j = 0; j < 128; j++)									// extract unconfigured CC flags
				CCname[j] = "CC" + j;

			if (MIDIio.DoEcho)
				for (j = 0; j < 128; j++)									// extract unconfigured CC flags
				{
					if (0 < (0x80 & I.Settings.CCvalue[j]))
					{
						Which[j] = Unc;
						ct++;
						I.Settings.CCvalue[j] &= 0x7F;
						I.Outer.SendCCval(j, I.Settings.CCvalue[j]); 		// reinitialize MIDIout device
					}
					else Which[j] = 0;
				}
			else for (j = 0; j < I.Settings.CCvalue.Length; j++)
				I.Settings.CCvalue[j] = 0;
			if (0 < ct)
				MIDIio.Log(4, $"IOProperties.InitCC(): {ct} CCs resent after restart");

			// Collect configured CCname[] and other Which[] properties from MIDIio.ini
			for (ct = 1; ct < CCtype.Length; ct++)				// skip CCtype[0]: Unconfigured
			{
				string type = MIDIio.Ini + CCtype[ct];			// "slider", "knob", "button"
				string property = I.PluginManager.GetPropertyValue(type + 's')?.ToString();
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
			}      // ct < CCtype.Length
        }	// all configured MIDIin properties are now in CCname[] and Which[]

		void Action(MIDIio I, byte bn, byte CCnumber)
		{
			I.AddEvent(CCname[CCnumber]);
			if (bn < Send.Length)
				switch (bn)				// configure button property and event
				{
					case 0:
						I.AddAction(Send[0],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)0));
						break;
					case 1:
						I.AddAction(Send[1],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)1));
						break;
					case 2:
						I.AddAction(Send[2],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)2));
						break;
					case 3:
						I.AddAction(Send[3],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)3));
						break;
					case 4:
						I.AddAction(Send[4],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)4));
						break;
					case 5:
						I.AddAction(Send[5],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)5));
						break;
					case 6:
						I.AddAction(Send[6],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)6));
						break;
					case 7:
						I.AddAction(Send[7],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)7));
						break;
					default:
						MIDIio.Info($"Action(): invalid Send[{bn}] for {CCnumber}");
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

			if (0 < MIDIio.CCSize && MIDIio.Log(4, ""))
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

			for (byte an = cc = 0; cc < 128; cc++)
				if (0 < (3 & Which[cc]))
				{
//					MIDIio.Log(4, $"CCprop({CCname[cc]})"); 
 					CCprop(I, cc);				// set property for configured input
					if (Button == Which[cc])
					{
						Which[cc] |= CC;
						Action(I, an++, cc);	// AddAction() for buttons
					}
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
