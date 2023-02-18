using System;
using System.Linq;
using SimHub.Plugins;

namespace blekenbleu.MIDIspace
{
	// Dynamically add events and properties for CC buttons as pressed
	// Working around the SimHub limitation that AttachDelegate() fails for variables.
	internal class IOproperties
	{
		internal string[][][] Send;                   		// these properties may be sent at 60Hz
		internal string[]   SendType;                  		// configuration by NCalc script
		internal byte[,]    SendCt;                    		// SendType subtotals for non-CC configured outputs

		internal string[]   CCname, CCtype;               	// for AttachDelegate();  CCn names get replaced by configured CCtype's
		internal byte[]     CCsndCt;       			// counts for configured CC sends
		private  byte[]     CCinCt;				// counts for configured MIDIin CC
		internal byte[,]    CCmap, Map;                      	// configured CC numbers:  MIDIin, send
		internal byte[]     Which, Wflag, Unmap;                // CCtype;  reverse map CC numbers to configured CCmap[,] indices.
		internal readonly byte Unc = 4, CC = 1, Button = 2;	// flags for Wflag
		internal byte[,]    Remap;				// indexed by Unmap; indexes CCmap[,]
		private string[]    Ping;                      		// ping[0-7]

		private string Join_strings(string s, string[] vector, byte length)
		{
			string sout = vector[0];

			for (byte i = 1; i < length; i++)
				sout += s + vector[i];
			return sout;
		}

		private string Join_bytes(string s, byte[] vector, byte length)
		{
			string sout = vector[0].ToString();

			for (byte i = 1; i < length; i++)
				sout += s + vector[i].ToString();
			return sout;
		}

		// build Map to configured CCnames, return CC number
		private int WhichCC(byte dest, string prop)
		{
			byte j, k;
			string prop4 = prop.Substring(7, 4);        // lop off 'MIDIio.'

			for (k = 1; k < CCtype.Length; k++)
			{
				if (prop4 == CCtype[k].Substring(0, 4)) // MIDIin type match
					break;
			}

			if (k == CCtype.Length)
				return (MIDIio.Info($"WhichCC() unrecognized property:  {prop}")) ? -1 : -1;

			for (j = 0; j < CCinCt[k]; j++)            				// search Map[]ed CCname[]s for configured CC of this type
			{ MIDIio.Info($"WhichCC() {prop.Substring(7, prop.Length - 7)} vs {CCname[CCmap[k - 1, j]]}");
				if (CCname[CCmap[k - 1, j]] == prop.Substring(7, prop.Length - 7))
				{
					// prop is configured; is it already in Map[dest]?
					for (byte i = 0; i < SendCt[dest, 0]; i++)
						if (CCmap[k - 1, j] == Map[dest, i])
							return Map[dest, i];			// already in Map[ct]

					if (MIDIio.size > SendCt[dest, 0])
						Map[dest, SendCt[dest, 0]++] = CCmap[k - 1, j];
					else MIDIio.Info($"WhichCC():  more than {MIDIio.size} MIDIin properties configured for "
							 + SendType[dest] + "; ignoring:  " + CCname[CCmap[k - 1, j]]);
					return CCmap[k - 1, j];                // added
				}
			}

			MIDIio.Info($"WhichCC() unconfigured property:  {prop}");
			return -1;
		}	// WhichCC(byte ct, string prop)

		private byte Unique(byte count, string[] name, string snew)
		{
			for (byte i = 0; i < count; i++)
				if (name[i].Length == snew.Length && name[i] == snew)
					return i;
			return count;
		}

		private int UniqueSend(byte dest, byte s, string prop)			// returns index for unique Send[, ] property name
		{
			int z;

			if (SendCt[dest, s] <= (z = Unique(SendCt[dest, s], Send[dest][s], prop)))
			{
			    if (SendCt[dest, s] < MIDIio.size)				// perhaps a duplicate send, but a unique source 
				Send[dest][s][SendCt[dest, s]++] = prop;		// bump source count and save the unique property
			    else MIDIio.Info($"UniqueSend():  {SendType[dest]} count {SendCt[dest, s]} maxed; ignoring {prop}"); 
			}
			return z;
		}

		// VJD.Init() has already run; now sort "my" CC properties first for sending, even when game is not running
		internal void Init(MIDIio I)
		{                               // CC configuration property name prefixes
			CCtype = new string[] { "unconfigured", "slider", "knob", "button" };   // Which CC types
			Wflag = new byte[] { Unc, CC, CC, Button };				// Which type flag bits
			Which = new byte[128];                  				// OUTwetdry.Init() resends unconfigured CCs on restart
			Unmap = new byte[128];                  			// OnEventSent() warns of unexpected CC numbers sent
			// selectively replaced by configured slider0-n, knob0-n, button0-n:
			CCname = new string[128];                   			// Initialized to CC[0-128]
			CCinCt = new byte[] { 0, 0, 0, 0 };				// unused CCinCt[0], to work with CCtype[]
			CCsndCt = new byte[] { 0, 0, 0 };              			// count CC send entries.

/* For DoSend(), there are 4 source types for each of 3 destinations (MIDIout, vJoy axes, vJoy buttons)
 ; DoSend needs not distinguish among MIDIout types (slider vs knob vs button), since all have 0-127 range scaling.
 ; Each output may get input from 3 MIDIin types + 2 JoyStick types + game type properties
 ; For those 3 MIDIin types, DoSend() must index CCnames[] by Map[,],
 :    where the first Map[,] dimension is SendType and second is cumulative configured count.
 ; Second Send indices, whether or not via Map are constructed using cumulative SendCt[, ],
 ;    where the first 3 entries for each SendCt[] apply to Map[]
 ;    and SendCt[, 4] is counts for corresponding Send[][][] JoyStick entries,
 ;    and SendCt[, 5] is counts for corresponding Send[][][] game entries, which DoSend indexes only when games are active.
 */
			SendType = new string[] { "send", "vJDaxis", "vJDbutton" };	// prefixes to search
			SendCt =     new byte[SendType.Length, 4];        		// entries in SendType[], game order
			byte size = MIDIio.size;
			Send = new string[3][][];				 // non-CC source properties configured for output
			CCmap =      new byte[SendType.Length, MIDIio.size];   	// configured MIDIin CC numbers
			Map =        new byte[SendType.Length, MIDIio.size];   	// CC numbers configured to send
			byte ct, j;
			for (ct = 0; ct < 3; ct++)
			{
			    Send[ct] = new string[3][];
                            for (j = 0; j < 3; j++)
				Send[ct][j] = new string[MIDIio.size];
			    for (j = 0; j < MIDIio.size; j++)
				CCmap[ct, j] = Map[ct, j] = 222;   // impossible match
			}

			Ping = new string[MIDIio.Size[0]];
			for (j = 0; j < MIDIio.Size[0]; j++)
				Ping[j] = "ping" + j;

			// 1) restart CC restoration

			MIDIio.Log(8, "Properties.send.Length = " + SendType.Length);
			for (j = ct = 0; j < 128; j++)					// extract unconfigured CC flags
			{
				Unmap[j] = j;
				CCname[j] = "CC" + j;

				if (0 < (0x80 & I.Settings.Sent[j]))
				{
					ct++;
					I.Settings.Sent[j] &= 0x7F;
					Which[j] = Unc;
				}
				else Which[j] = 0;
			}

			if (MIDIio.DoEcho)
			{
				for (byte i = j = 0; j < ct && i < 128; i++)			// resend saved CCs
				{
					if (0 < (Unc & Which[i]))  // unconfigured CC number?
					{
						I.Outer.SendCCval(i, I.Settings.Sent[i]);	// much time may have passed;  reinitialize MIDIout device
						j++;
					}
				}
				MIDIio.Log(4, $"CCProperties.Init(): {j} CCs resent after restart");
			} else MIDIio.Log(4, $"CCProperties.Init():  {ct} unconfigured CCs restored");

			// 2) Collect properties to be sent: SendType.Length destinations, 4 == MIDIio.Real.Length + game sources

			// Collect MIDIin properties from MIDIio.ini
			for (ct = 1; ct < CCtype.Length; ct++)
			{
				string type = MIDIio.Ini + CCtype[ct];
				string property = I.PluginManager.GetPropertyValue(type + 's')?.ToString();
				if (null == property) {
					MIDIio.Info($"Init(): '{type + 's'}' not found");
					continue;
				}

				// bless the Internet
				byte[] array = property.Split(',').Select(byte.Parse).ToArray();
				MIDIio.Log(4, $"Init(): '{MIDIio.Ini + CCtype[ct]}' {string.Join(",", array.Select(p => p.ToString()).ToArray())}");

				j = 0;
				foreach (byte cc in array)          // array has cc numbers assigned for this type
				{
					byte k;

					if (CCinCt[ct] >= MIDIio.size)	// ct is 1-based
						continue;

					for (k = 0; k <= CCinCt[ct]; k++)
						if (cc == CCmap[ct - 1, k])
						{
							MIDIio.Info("Init():  {type + j} {cc} already configured as {CCname[cc]}");
							k = (byte)(2 + CCinCt[ct]);
						}

					if (k == (1 + CCinCt[ct]))           // not found?
					{
						CCmap[ct - 1, CCinCt[ct]++] = cc;
						CCname[cc] = CCtype[ct] + j++;      // replacing "CCcc"
						Which[cc] = Wflag[ct];
					}
				}
			}       // ct < CCtype.Length
					// all configured MIDIin properties are now in CCname[] and CCmap

			if (MIDIio.Log(8, $"Init.CCmap[]:"))
				for (ct = 0; ct < CCtype.Length - 1; ct++)
				    for (j = 0; j < CCinCt[ct + 1]; j++)
					MIDIio.Info(CCname[CCmap[ct, j]]);

// 3) Collect and Map input properties configured for output

			for (byte st = 0; st < SendType.Length; st++)
				for (j = 0; j < MIDIio.Real.Length; j++)
				{
					MIDIio.Log(8, $"{SendType[st]} for {MIDIio.Real[j]}");
					byte k = (2 > j) ? j : (byte)(j + 1);
					for (byte i = 0; i < MIDIio.size; i++)      // snag Real[j] configured sends
					{
						string prop = I.PluginManager.GetPropertyValue($"{MIDIio.Ini + SendType[j]}{k}")?.ToString();

						if (null != prop && 11 < prop.Length)
						{
							string prop7 = prop.Substring(0, 7);

							if ("MIDIio." == prop7)
								WhichCC(st, prop);	// Unique CC property names are already tabulated in CCname[]
							else if ("Joystic" == prop7)
								UniqueSend(st, 1, prop);
							else if ("InputSt" == prop7)
								UniqueSend(st, 2, prop);
							else UniqueSend(st, 3, prop);
						}
						else MIDIio.Info($"Init(): unrecognized Send property:  {prop}");
					}
				}

// 4) optionally log

			string s = "";
			for (byte st = 0; st < SendType.Length; st++)
			{
				if (0 < st)
					s += "\n\t\t\t";
				s += SendType[st] + ":  " + SendCt[st, 0].ToString();
				for (j = 1; j <= MIDIio.Real.Length; j++)
					s += "," + SendCt[st, j].ToString();
			}

			MIDIio.Log(4, $"Properties.SendCt " + s);

			s = "";
			for (byte st = 0; st < SendType.Length; st++)
			{
				if (0 < st)
					s += "\n\t\t\t\t";
				s += SendType[st] + ":  ";
				byte i;
				for (byte pt = 0; pt < 4; pt++)					// property type: CC, Joy axis, Joy button, game
				for (i = j = 0; j < SendCt[st,pt]; j++)
				{
					if (0 < j && 0 < pt)
					{
						if(null != Send[st][pt - 1][j])
							s += "\n\t\t\t\t\t";
						else s += $"\nnull == Send[{st}][{pt - 1}][{j}]";
					}
					if (0 < pt)
						s += Send[st][pt - 1][j];
												// unlike st > 0; 0 == pt wants CCname[Map[st,]]
					else for (i = 0; i < CCsndCt[st]; i++)
						s += (128 > Map[st, i]) ? CCname[Map[st, i]] : $"invalid Map[{st}, {i}]";

				}
			}
	    		MIDIio.Log(4, $"Properties.Send[][][]:  " + s);
		}									// Init()


// End of Init();  beginning of End()

	internal void End(MIDIio I)
	{
	    byte ct; 

	    for (byte i = ct = 0; i < 128; i++)
		if (Unc == Which[i])
		{
		    I.Settings.Sent[i] |= 0x80; // flag unconfigured CCs to restore
		    ct++;
		}

	    MIDIio.Log(4, $"Properties.End():  {ct} unconfigured CCs");
	}

	private void Action(MIDIio I, byte bn, byte CCnumber)
	{
	    I.AddEvent(CCname[CCnumber]);
			if (bn < Ping.Length)
	    switch (bn)				// configure button property and event
	    {
		case 0:
		    I.AddAction(Ping[0],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)0));
		    break;
		case 1:
		    I.AddAction(Ping[1],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)1));
		    break;
		case 2:
		    I.AddAction(Ping[2],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)2));
		    break;
		case 3:
		    I.AddAction(Ping[3],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)3));
		    break;
		case 4:
		    I.AddAction(Ping[4],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)4));
		    break;
		case 5:
		    I.AddAction(Ping[5],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)5));
		    break;
		case 6:
		    I.AddAction(Ping[6],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)6));
		    break;
		case 7:
		    I.AddAction(Ping[7],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)7));
		    break;
		default:
		    MIDIio.Info($"Action(): invalid Ping[{bn}] for {CCnumber}");
		    break;
	    }
	}

	internal bool SetProp(MIDIio I, int CCnumber)
	{
	    switch (CCnumber)		// configure CC property and event
	    {
		case 0:
		    I.AttachDelegate(CCname[0], () => I.Settings.Sent[0]);
		    break;
		case 1:
		    I.AttachDelegate(CCname[1], () => I.Settings.Sent[1]);
		    break;
		case 2:
		    I.AttachDelegate(CCname[2], () => I.Settings.Sent[2]);
		    break;
		case 3:
		    I.AttachDelegate(CCname[3], () => I.Settings.Sent[3]);
		    break;
		case 4:
		    I.AttachDelegate(CCname[4], () => I.Settings.Sent[4]);
		    break;
		case 5:
		    I.AttachDelegate(CCname[5], () => I.Settings.Sent[5]);
		    break;
		case 6:
		    I.AttachDelegate(CCname[6], () => I.Settings.Sent[6]);
		    break;
		case 7:
		    I.AttachDelegate(CCname[7], () => I.Settings.Sent[7]);
		    break;
		case 8:
		    I.AttachDelegate(CCname[8], () => I.Settings.Sent[8]);
		    break;
		case 9:
		    I.AttachDelegate(CCname[9], () => I.Settings.Sent[9]);
		    break;
		case 10:
		    I.AttachDelegate(CCname[10], () => I.Settings.Sent[10]);
		    break;
		case 11:
		    I.AttachDelegate(CCname[11], () => I.Settings.Sent[11]);
		    break;
		case 12:
		    I.AttachDelegate(CCname[12], () => I.Settings.Sent[12]);
		    break;
		case 13:
		    I.AttachDelegate(CCname[13], () => I.Settings.Sent[13]);
		    break;
		case 14:
		    I.AttachDelegate(CCname[14], () => I.Settings.Sent[14]);
		    break;
		case 15:
		    I.AttachDelegate(CCname[15], () => I.Settings.Sent[15]);
		    break;
		case 16:
		    I.AttachDelegate(CCname[16], () => I.Settings.Sent[16]);
		    break;
		case 17:
		    I.AttachDelegate(CCname[17], () => I.Settings.Sent[17]);
		    break;
		case 18:
		    I.AttachDelegate(CCname[18], () => I.Settings.Sent[18]);
		    break;
		case 19:
		    I.AttachDelegate(CCname[19], () => I.Settings.Sent[19]);
		    break;
		case 20:
		    I.AttachDelegate(CCname[20], () => I.Settings.Sent[20]);
		    break;
		case 21:
		    I.AttachDelegate(CCname[21], () => I.Settings.Sent[21]);
		    break;
		case 22:
		    I.AttachDelegate(CCname[22], () => I.Settings.Sent[22]);
		    break;
		case 23:
		    I.AttachDelegate(CCname[23], () => I.Settings.Sent[23]);
		    break;
		case 24:
		    I.AttachDelegate(CCname[24], () => I.Settings.Sent[24]);
		    break;
		case 25:
		    I.AttachDelegate(CCname[25], () => I.Settings.Sent[25]);
		    break;
		case 26:
		    I.AttachDelegate(CCname[26], () => I.Settings.Sent[26]);
		    break;
		case 27:
		    I.AttachDelegate(CCname[27], () => I.Settings.Sent[27]);
		    break;
		case 28:
		    I.AttachDelegate(CCname[28], () => I.Settings.Sent[28]);
		    break;
		case 29:
		    I.AttachDelegate(CCname[29], () => I.Settings.Sent[29]);
		    break;
		case 30:
		    I.AttachDelegate(CCname[30], () => I.Settings.Sent[30]);
		    break;
		case 31:
		    I.AttachDelegate(CCname[31], () => I.Settings.Sent[31]);
		    break;
		case 32:
		    I.AttachDelegate(CCname[32], () => I.Settings.Sent[32]);
		    break;
		case 33:
		    I.AttachDelegate(CCname[33], () => I.Settings.Sent[33]);
		    break;
		case 34:
		    I.AttachDelegate(CCname[34], () => I.Settings.Sent[34]);
		    break;
		case 35:
		    I.AttachDelegate(CCname[35], () => I.Settings.Sent[35]);
		    break;
		case 36:
		    I.AttachDelegate(CCname[36], () => I.Settings.Sent[36]);
		    break;
		case 37:
		    I.AttachDelegate(CCname[37], () => I.Settings.Sent[37]);
		    break;
		case 38:
		    I.AttachDelegate(CCname[38], () => I.Settings.Sent[38]);
		    break;
		case 39:
		    I.AttachDelegate(CCname[39], () => I.Settings.Sent[39]);
		    break;
		case 40:
		    I.AttachDelegate(CCname[40], () => I.Settings.Sent[40]);
		    break;
		case 41:
		    I.AttachDelegate(CCname[41], () => I.Settings.Sent[41]);
		    break;
		case 42:
		    I.AttachDelegate(CCname[42], () => I.Settings.Sent[42]);
		    break;
		case 43:
		    I.AttachDelegate(CCname[43], () => I.Settings.Sent[43]);
		    break;
		case 44:
		    I.AttachDelegate(CCname[44], () => I.Settings.Sent[44]);
		    break;
		case 45:
		    I.AttachDelegate(CCname[45], () => I.Settings.Sent[45]);
		    break;
		case 46:
		    I.AttachDelegate(CCname[46], () => I.Settings.Sent[46]);
		    break;
		case 47:
		    I.AttachDelegate(CCname[47], () => I.Settings.Sent[47]);
		    break;
		case 48:
		    I.AttachDelegate(CCname[48], () => I.Settings.Sent[48]);
		    break;
		case 49:
		    I.AttachDelegate(CCname[49], () => I.Settings.Sent[49]);
		    break;
		case 50:
		    I.AttachDelegate(CCname[50], () => I.Settings.Sent[50]);
		    break;
		case 51:
		    I.AttachDelegate(CCname[51], () => I.Settings.Sent[51]);
		    break;
		case 52:
		    I.AttachDelegate(CCname[52], () => I.Settings.Sent[52]);
		    break;
		case 53:
		    I.AttachDelegate(CCname[53], () => I.Settings.Sent[53]);
		    break;
		case 54:
		    I.AttachDelegate(CCname[54], () => I.Settings.Sent[54]);
		    break;
		case 55:
		    I.AttachDelegate(CCname[55], () => I.Settings.Sent[55]);
		    break;
		case 56:
		    I.AttachDelegate(CCname[56], () => I.Settings.Sent[56]);
		    break;
		case 57:
		    I.AttachDelegate(CCname[57], () => I.Settings.Sent[57]);
		    break;
		case 58:
		    I.AttachDelegate(CCname[58], () => I.Settings.Sent[58]);
		    break;
		case 59:
		    I.AttachDelegate(CCname[59], () => I.Settings.Sent[59]);
		    break;
		case 60:
		    I.AttachDelegate(CCname[60], () => I.Settings.Sent[60]);
		    break;
		case 61:
		    I.AttachDelegate(CCname[61], () => I.Settings.Sent[61]);
		    break;
		case 62:
		    I.AttachDelegate(CCname[62], () => I.Settings.Sent[62]);
		    break;
		case 63:
		    I.AttachDelegate(CCname[63], () => I.Settings.Sent[63]);
		    break;
		case 64:
		    I.AttachDelegate(CCname[64], () => I.Settings.Sent[64]);
		    break;
		case 65:
		    I.AttachDelegate(CCname[65], () => I.Settings.Sent[65]);
		    break;
		case 66:
		    I.AttachDelegate(CCname[66], () => I.Settings.Sent[66]);
		    break;
		case 67:
		    I.AttachDelegate(CCname[67], () => I.Settings.Sent[67]);
		    break;
		case 68:
		    I.AttachDelegate(CCname[68], () => I.Settings.Sent[68]);
		    break;
		case 69:
		    I.AttachDelegate(CCname[69], () => I.Settings.Sent[69]);
		    break;
		case 70:
		    I.AttachDelegate(CCname[70], () => I.Settings.Sent[70]);
		    break;
		case 71:
		    I.AttachDelegate(CCname[71], () => I.Settings.Sent[71]);
		    break;
		case 72:
		    I.AttachDelegate(CCname[72], () => I.Settings.Sent[72]);
		    break;
		case 73:
		    I.AttachDelegate(CCname[73], () => I.Settings.Sent[73]);
		    break;
		case 74:
		    I.AttachDelegate(CCname[74], () => I.Settings.Sent[74]);
		    break;
		case 75:
		    I.AttachDelegate(CCname[75], () => I.Settings.Sent[75]);
		    break;
		case 76:
		    I.AttachDelegate(CCname[76], () => I.Settings.Sent[76]);
		    break;
		case 77:
		    I.AttachDelegate(CCname[77], () => I.Settings.Sent[77]);
		    break;
		case 78:
		    I.AttachDelegate(CCname[78], () => I.Settings.Sent[78]);
		    break;
		case 79:
		    I.AttachDelegate(CCname[79], () => I.Settings.Sent[79]);
		    break;
		case 80:
		    I.AttachDelegate(CCname[80], () => I.Settings.Sent[80]);
		    break;
		case 81:
		    I.AttachDelegate(CCname[81], () => I.Settings.Sent[81]);
		    break;
		case 82:
		    I.AttachDelegate(CCname[82], () => I.Settings.Sent[82]);
		    break;
		case 83:
		    I.AttachDelegate(CCname[83], () => I.Settings.Sent[83]);
		    break;
		case 84:
		    I.AttachDelegate(CCname[84], () => I.Settings.Sent[84]);
		    break;
		case 85:
		    I.AttachDelegate(CCname[85], () => I.Settings.Sent[85]);
		    break;
		case 86:
		    I.AttachDelegate(CCname[86], () => I.Settings.Sent[86]);
		    break;
		case 87:
		    I.AttachDelegate(CCname[87], () => I.Settings.Sent[87]);
		    break;
		case 88:
		    I.AttachDelegate(CCname[88], () => I.Settings.Sent[88]);
		    break;
		case 89:
		    I.AttachDelegate(CCname[89], () => I.Settings.Sent[89]);
		    break;
		case 90:
		    I.AttachDelegate(CCname[90], () => I.Settings.Sent[90]);
		    break;
		case 91:
		    I.AttachDelegate(CCname[91], () => I.Settings.Sent[91]);
		    break;
		case 92:
		    I.AttachDelegate(CCname[92], () => I.Settings.Sent[92]);
		    break;
		case 93:
		    I.AttachDelegate(CCname[93], () => I.Settings.Sent[93]);
		    break;
		case 94:
		    I.AttachDelegate(CCname[94], () => I.Settings.Sent[94]);
		    break;
		case 95:
		    I.AttachDelegate(CCname[95], () => I.Settings.Sent[95]);
		    break;
		case 96:
		    I.AttachDelegate(CCname[96], () => I.Settings.Sent[96]);
		    break;
		case 97:
		    I.AttachDelegate(CCname[97], () => I.Settings.Sent[97]);
		    break;
		case 98:
		    I.AttachDelegate(CCname[98], () => I.Settings.Sent[98]);
		    break;
		case 99:
		    I.AttachDelegate(CCname[99], () => I.Settings.Sent[99]);
		    break;
		case 100:
		    I.AttachDelegate(CCname[100], () => I.Settings.Sent[100]);
		    break;
		case 101:
		    I.AttachDelegate(CCname[101], () => I.Settings.Sent[101]);
		    break;
		case 102:
		    I.AttachDelegate(CCname[102], () => I.Settings.Sent[102]);
		    break;
		case 103:
		    I.AttachDelegate(CCname[103], () => I.Settings.Sent[103]);
		    break;
		case 104:
		    I.AttachDelegate(CCname[104], () => I.Settings.Sent[104]);
		    break;
		case 105:
		    I.AttachDelegate(CCname[105], () => I.Settings.Sent[105]);
		    break;
		case 106:
		    I.AttachDelegate(CCname[106], () => I.Settings.Sent[106]);
		    break;
		case 107:
		    I.AttachDelegate(CCname[107], () => I.Settings.Sent[107]);
		    break;
		case 108:
		    I.AttachDelegate(CCname[108], () => I.Settings.Sent[108]);
		    break;
		case 109:
		    I.AttachDelegate(CCname[109], () => I.Settings.Sent[109]);
		    break;
		case 110:
		    I.AttachDelegate(CCname[110], () => I.Settings.Sent[110]);
		    break;
		case 111:
		    I.AttachDelegate(CCname[111], () => I.Settings.Sent[111]);
		    break;
		case 112:
		    I.AttachDelegate(CCname[112], () => I.Settings.Sent[112]);
		    break;
		case 113:
		    I.AttachDelegate(CCname[113], () => I.Settings.Sent[113]);
		    break;
		case 114:
		    I.AttachDelegate(CCname[114], () => I.Settings.Sent[114]);
		    break;
		case 115:
		    I.AttachDelegate(CCname[115], () => I.Settings.Sent[115]);
		    break;
		case 116:
		    I.AttachDelegate(CCname[116], () => I.Settings.Sent[116]);
		    break;
		case 117:
		    I.AttachDelegate(CCname[117], () => I.Settings.Sent[117]);
		    break;
		case 118:
		    I.AttachDelegate(CCname[118], () => I.Settings.Sent[118]);
		    break;
		case 119:
		    I.AttachDelegate(CCname[119], () => I.Settings.Sent[119]);
		    break;
		case 120:
		    I.AttachDelegate(CCname[120], () => I.Settings.Sent[120]);
		    break;
		case 121:
		    I.AttachDelegate(CCname[121], () => I.Settings.Sent[121]);
		    break;
		case 122:
		    I.AttachDelegate(CCname[122], () => I.Settings.Sent[122]);
		    break;
		case 123:
		    I.AttachDelegate(CCname[123], () => I.Settings.Sent[123]);
		    break;
		case 124:
		    I.AttachDelegate(CCname[124], () => I.Settings.Sent[124]);
		    break;
		case 125:
		    I.AttachDelegate(CCname[125], () => I.Settings.Sent[125]);
		    break;
		case 126:
		    I.AttachDelegate(CCname[126], () => I.Settings.Sent[126]);
		    break;
		case 127:
		    I.AttachDelegate(CCname[127], () => I.Settings.Sent[127]);
		    break;
		default:
		    MIDIio.Info($"SetProp() not set: CC{CCnumber}");
		    return false;
	    }
	    return true;
	}	// SetProp()

// call SetProp to AttachDelegate() configured MIDIin properties

	internal void Attach(MIDIio I)
	{
	    byte j, cc, cn = 0;

	    if (0 < MIDIio.Size[0] && MIDIio.Log(8, "Attach() MIDIio.in sends:"))
	    {
		byte L = (byte)MIDIio.Ini.Length;

		for (byte st = 0; st < 3; st++)
			for (j = 0; j < CCsndCt[st]; j++)
				MIDIio.Info("\t" + CCname[Map[st,j]]);

		MIDIio.Info("Attach() other sends:");
		for (byte i = 0; i < SendType.Length; i++)
		{
		    MIDIio.Info(SendType[i]);
		    for (cn = 0; cn < 4; cn++)
		    for (j = 1; j < SendCt[i, cn]; j++)
			MIDIio.Info("\t" + Send[i][cn][j] + " AKA " + Send[i][cn][j].Substring(L, Send[j][cn][j].Length - L));
		}
	    }

	    for (byte s = 0; s < 3; s++)
	    for (byte ct = 0; ct < 3; ct++)
	    for (j = 0; j < CCinCt[ct]; j++)
	    {
		cc = CCmap[ct, j];
 		SetProp(I, cc);				// set property for configured input
		if (Button == Which[cc])
		{
		    Which[cc] |= CC;
		    Action(I, cn++, cc);
		}
	    }

            // MIDIin property configuration is now complete

	    if (!MIDIio.DoEcho)
	    {
		for (j = cn = 0; cn < 128; cn++)
		    if (0 < (Unc & Which[cn]))
		    {
			SetProp(I, cn);			// restore previous received unconfigured CCs
			j++;
		    }
		if (0 < j)
		    MIDIio.Log(4, $"Attach():  {j} previous CC properties restored");
	    }
	}	// Attach()
    }
}
