﻿using System;
using System.Linq;
using SimHub.Plugins;

namespace blekenbleu
{
	// Dynamically add events and properties for CC buttons as pressed
	// Working around the SimHub limitation that AttachDelegate() fails for variables.
	internal class IOproperties
	{
		internal string[][] SourceName;				   		// these non-CC SourceType[] Property names may be sent at 60Hz to 3 DestType[]s
		internal byte[,,] 	SourceArray;					// Destination port,port index for 3 SourceType[]s
		internal byte[,]	CCarray;						// Destination port index for CC source type
		internal byte[]		SourceCt, Map;					// No skipping around;  Map CCnumber to CCarray[dt,] index

		internal string[]   DestType;				  		// configuration by NCalc script

		internal string[]   CCname, CCtype;			   		// for AttachDelegate();  CCn names get replaced by configured CCtype's
		internal byte[]	 	Which, Wflag;					// CCtype
		internal readonly byte Unc = 4, CC = 1, Button = 2;	// Wflag[] input type bits for Which[]
		internal readonly byte[] Route = {8, 16, 32};		// Which[] flags for near-real-time MIDIin forwarding
		private  string[]	Ping;					  		// ping[0-7]
		private  byte		size;
		private readonly string[] SourceType = {"game", "Joystick axis", "Joystick button"};	// for SourceName[][], SourceArray[,,]
/*
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
 */

        /// <summary>
        /// Called by Init(); adds source property data to CCname[], Which[]
        /// </summary>
		private int WhichCC(string prop, byte dt, byte DevAddr)
		{
			byte cc;
			int L;
			string prop7 = prop.Substring(7, L = prop.Length - 7);	// lop off 'MIDIio.'

			for (cc = 0; cc < CCname.Length; cc++)
				if (L == CCname[cc].Length && CCname[cc] == prop7)
				{
					Which[cc] &= (byte)(~Unc);						// no longer unconfigured
					Which[cc] |= (CC);								// perhaps already configured as a Button
					if (1 == dt)									// routed to a vJoy button?
						Which[cc] |= Button;						// make it so

					if (0 < (56 & Which[cc]))						// already a CCarray[,] for this cc?
					{
						CCarray[dt, Map[cc]] = DevAddr;
						Which[cc] |= Route[dt];						// Active() checks Which[] Route[]	flags
					}
					else if (SourceCt[3] < 3 * size)
					{
						Which[cc] |= Route[dt];
						Map[cc] = SourceCt[3];						// Map to CCarray[dt, SourceCt[3]]
						CCarray[dt, SourceCt[3]++] = DevAddr;
					}
					return cc;
				}

			MIDIio.Info($"WhichCC() unrecognized {DestType[dt]} property:  {prop}");
			return -1;
		}

        /// <summary>
        /// Called by Init(); adds source property data to SourceName[], SourceArray[,,]
        /// </summary>
		private void Source(byte dt, byte st, byte dev_address, string dp, string prop)
		{
			if (size > SourceCt[st])
			{
				SourceName[st][SourceCt[st]] = prop;
				SourceArray[st, 0, SourceCt[st]] = dt;
				SourceArray[st, 1, SourceCt[st]++] = dev_address;
			}
			else MIDIio.Info($"Properties.Source({SourceType[st]}): property size {size} exceeded;\n\t\t"
					+ $"ignoring {DestType[dt]} property {dp}:  {prop}");
			return;
		}

		// VJD.Init() has already run; now sort "my" CC properties first for sending, even when game is not running
        /// <summary>
        /// Calls Source() and WhichCC() to build MIDI and vJoy routing tables
        /// </summary>
		internal void Init(MIDIio I)
		{																	// CC configuration property types
			CCtype = new string[] { "unconfigured", "slider", "knob", "button" };
			Wflag = new byte[] { Unc, CC, CC, Button };						// Which type flag bits
			Which = new byte[128];				  							// OUTwetdry.Init() resends unconfigured CCs on restart
			// selectively replaced by configured slider0-n, knob0-n, button0-n:
			CCname = new string[128];				   						// Initialized to CC[0-128]

/* DoSend() may send 4 property value source types to each of 3 DestType[]s  (vJoy axes, vJoy buttons, MIDIout)
 ; DoSend() has 3 property SourceType[]s: (game, JoyStick axis or button) and CCtype
 ; DoSend() indexes SourceName[st,,]s up to SourceCt[st].
 : SourceCt[] and SourceArray[,,] first dimension is SourceType
 ; SourceName[st][] dimension is SourceType; last diminension is sequential configured indices < SourceCt[,]
 ; SourceCt[0] counts SourceName[0][] game entries, SourceCt[1] counts JoyStick axis entries,
 ; SourceCt[2] counts JoyStick button entries, SourceCt[3] counts CC entries
 ; DoSend(0) is called to index SourceName[0][] only when games are active.
 */
			DestType =	new string[] { "vJoyaxis", "vJoybutton", "CCsend" };	// destination prefixes to search
			size = MIDIio.size;
			SourceName = new string[SourceType.Length][];					// CCname instead of SourceName[3]
			SourceArray = new byte[SourceType.Length, 2, size];				// non-CC sources
			CCarray = 	new byte[DestType.Length, 3 * size];
			Map = new byte[128];
			SourceCt = 	new byte[] { 0, 0, 0, 0 };
			byte[][] Darray = new byte[DestType.Length][];					// configured destination indices
			byte dt, ct, j;

			for (dt = 0; dt < DestType.Length; dt++)
			{
				SourceName[dt] = new string[size];
				string pts;

				// configured destination indices
				string ds = I.PluginManager.GetPropertyValue(pts = MIDIio.Ini + DestType[dt] + 's')?.ToString();
				if (null == ds && MIDIio.Info($"Init(): {DestType[dt]} property '{pts}' not found"))
					continue;

				// bless the Internet: split comma separated integers
				Darray[dt] = ds.Split(',').Select(byte.Parse).ToArray();
			}

			if (null != Darray && null != I.VJD)
				for (j = 0; j < Darray[0].Length; j++)							// valid vJoy axes address?
					if (I.VJD.Usage.Length <= Darray[0][j])
						MIDIio.Info($"Properties.Init(): Invalid {DestType[0]} address {Darray[0][j]} > {I.VJD.Usage.Length}");

			Ping = new string[MIDIio.Size[2]];
            if (null != Darray && null != Darray[1])
                for (j = 0; j < Darray[1].Length; j++)							// valid vJoy button address?
				if (0 > Darray[1][j] || Darray[1][j] >= I.VJD.nButtons)
					MIDIio.Info($"Properties.Init(): Invalid {DestType[1]} address {Darray[1][j]}");

			Ping = new string[MIDIio.Size[2]];
			for (j = 0; j < MIDIio.Size[2]; j++)
				Ping[j] = "ping" + j;

// 1) initialize CC

			for (j = ct = 0; j < 128; j++)					// extract unconfigured CC flags
			{
				CCname[j] = "CC" + j;

				if (0 < (0x80 & I.Settings.CCvalue[j]))
				{
					ct++;
					I.Settings.CCvalue[j] &= 0x7F;
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
						I.Outer.SendCCval(i, I.Settings.CCvalue[i]);	// much time may have passed;  reinitialize MIDIout device
						j++;
					}
				}
				if (0 < j)
					MIDIio.Log(4, $"CCProperties.Init(): {j} CCs resent after restart");
			}

// 2) Collect configured MIDIin Which[] and CCname[] properties

			// Collect MIDIin properties from MIDIio.ini
			for (ct = 1; ct < CCtype.Length; ct++)				// skipping CCtype[0]: Unconfigured
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
//							MIDIio.Log(4, $"CCProperties.Init(): CCname[{cc}] = " + CCname[cc]);
						}
						else MIDIio.Info($"Init({type + j}):  {cc} already configured as {CCname[cc]}");
					}
					else MIDIio.Info($"Init({type + j}) invalid CC value: {cc}");
				}
			}	   // ct < CCtype.Length
   			// all configured MIDIin properties are now in CCname[] and Which[]

// 3) Now use Darray[] to collect SourceName[][], SourceArray[,,], CCarray[,], Map[]

			string dp;
			for (dt = 0; dt < DestType.Length; dt++)									// vJoy axis, vJoy button, CC
			{
				if (null == Darray[dt])
					continue;															// perhaps no properties for this destination

				for(byte i = 0; i < Darray[dt].Length; i++)
				{
					dp = MIDIio.Ini;

					if (1 == dt)
					{
						dp += "vJoyB";
						if (10 > Darray[dt][i])											// match JoyStick button naming style
							dp += "0";
					}
					else dp += DestType[dt];
					dp += (Darray[dt][i]).ToString();

					string prop = I.PluginManager.GetPropertyValue(dp)?.ToString();

			 		if (null == prop) 													// Configured properties should not be null
					{
						MIDIio.Info($"IOproperties.Init(): null Send {DestType[dt]} property {dp}");
						continue;
					}

					switch (prop.Substring(0, 7))
					{
						case "MIDIio.":
							WhichCC(prop, dt, Darray[dt][i]);							// CC property names are in CCname[]
							break;
						case "Joystic":													// JoyStick axis
							Source(dt, 1, Darray[dt][i], dp, prop);
							break;
						case "InputSt":													// JoyStick button
							Source(dt, 2, Darray[dt][i], dp, prop);
							break;
						default:														// "game"
							Source(dt, 0, Darray[dt][i], dp, prop);
							break;
					}
				}
			}

// 4) optionally log

			if (MIDIio.Log(4, ""))
			{
				string s = "";

				for (dt = 0; dt < DestType.Length; dt++)
				{
					if (0 < dt)
						s += "\n\t";
					s +=  $"Properties.SourceName[{DestType[dt]}]:  ";

					for (byte pt = 0; pt < 4; pt++)					// property type: game, Joy axis, Joy button, CC
					{
						string[] N = (3 > pt) ? SourceName[pt] : CCname;

						// unlike other sources, CC source address is the known at Send() time
						for (byte k = j = 0; j < ((3 > pt) ? SourceCt[pt] : 128); j++)
						{
							if ((3 > pt) ? (dt != SourceArray[pt, 0, j]) : (0 == (Route[dt] & Which[j])))
								continue;

							if (0 < k++)
								s += "\n\t\t\t\t";
							else if (0 < SourceCt[pt])
								s += "\n\t\t\t\t";
							if(null != N[j])
								s += "@ " + ((3 > pt) ? SourceArray[pt, 1, j] : CCarray[dt, Map[j]]) + ": " + N[j];
							else s += $"\nnull == SourceName[{dt}][{pt}][{j}]\n\t\t\t\t\t";
						}
					}
				}
				MIDIio.Info(s + "\n");

				s = "Properties.CCname[]:";
				for (dt = 0; dt < 128; dt++)
					if (0 < (3 & Which[dt]))
					{
						s += $"\n\t{CCname[dt]}\t@ {dt}";
						if (0 < (Button & Which[dt]))
							s += " (Button)";
						for (byte pt = 0; pt < Route.Length; pt++)
							if (0 < (Route[pt] & Which[dt]))
							{
								byte b = CCarray[pt, Map[dt]];

								if (1 == pt)	// replace vJoybutton with vJoyB0
								{
									s += $"  vJoyB";
									if (10 > b)
										s += "0";
								}
								else s += $"  {DestType[pt]}";
								s += $"{b}";
							}
					}
				if (17 < s.Length)
					MIDIio.Info(s + "\n");
			}								// if (MIDIio.Log(4, ""))
		}									// Init()


// End of Init();  beginning of End()

		internal void End(MIDIio I)
		{
			byte ct; 

			for (byte i = ct = 0; i < 128; i++)
				if (Unc == Which[i])
				{
					I.Settings.CCvalue[i] |= 0x80; // flag unconfigured CCs to restore
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

		internal bool CCprop(MIDIio I, int CCnumber)
		{
			switch (CCnumber)		// configure CC property and event
			{
				case 0:
					I.AttachDelegate(CCname[0], () => I.Settings.CCvalue[0]);
					break;
				case 1:
					I.AttachDelegate(CCname[1], () => I.Settings.CCvalue[1]);
					break;
				case 2:
					I.AttachDelegate(CCname[2], () => I.Settings.CCvalue[2]);
					break;
				case 3:
					I.AttachDelegate(CCname[3], () => I.Settings.CCvalue[3]);
					break;
				case 4:
					I.AttachDelegate(CCname[4], () => I.Settings.CCvalue[4]);
					break;
				case 5:
					I.AttachDelegate(CCname[5], () => I.Settings.CCvalue[5]);
					break;
				case 6:
					I.AttachDelegate(CCname[6], () => I.Settings.CCvalue[6]);
					break;
				case 7:
					I.AttachDelegate(CCname[7], () => I.Settings.CCvalue[7]);
					break;
				case 8:
					I.AttachDelegate(CCname[8], () => I.Settings.CCvalue[8]);
					break;
				case 9:
					I.AttachDelegate(CCname[9], () => I.Settings.CCvalue[9]);
					break;
				case 10:
					I.AttachDelegate(CCname[10], () => I.Settings.CCvalue[10]);
					break;
				case 11:
					I.AttachDelegate(CCname[11], () => I.Settings.CCvalue[11]);
					break;
				case 12:
					I.AttachDelegate(CCname[12], () => I.Settings.CCvalue[12]);
					break;
				case 13:
					I.AttachDelegate(CCname[13], () => I.Settings.CCvalue[13]);
					break;
				case 14:
					I.AttachDelegate(CCname[14], () => I.Settings.CCvalue[14]);
					break;
				case 15:
					I.AttachDelegate(CCname[15], () => I.Settings.CCvalue[15]);
					break;
				case 16:
					I.AttachDelegate(CCname[16], () => I.Settings.CCvalue[16]);
					break;
				case 17:
					I.AttachDelegate(CCname[17], () => I.Settings.CCvalue[17]);
					break;
				case 18:
					I.AttachDelegate(CCname[18], () => I.Settings.CCvalue[18]);
					break;
				case 19:
					I.AttachDelegate(CCname[19], () => I.Settings.CCvalue[19]);
					break;
				case 20:
					I.AttachDelegate(CCname[20], () => I.Settings.CCvalue[20]);
					break;
				case 21:
					I.AttachDelegate(CCname[21], () => I.Settings.CCvalue[21]);
					break;
				case 22:
					I.AttachDelegate(CCname[22], () => I.Settings.CCvalue[22]);
					break;
				case 23:
					I.AttachDelegate(CCname[23], () => I.Settings.CCvalue[23]);
					break;
				case 24:
					I.AttachDelegate(CCname[24], () => I.Settings.CCvalue[24]);
					break;
				case 25:
					I.AttachDelegate(CCname[25], () => I.Settings.CCvalue[25]);
					break;
				case 26:
					I.AttachDelegate(CCname[26], () => I.Settings.CCvalue[26]);
					break;
				case 27:
					I.AttachDelegate(CCname[27], () => I.Settings.CCvalue[27]);
					break;
				case 28:
					I.AttachDelegate(CCname[28], () => I.Settings.CCvalue[28]);
					break;
				case 29:
					I.AttachDelegate(CCname[29], () => I.Settings.CCvalue[29]);
					break;
				case 30:
					I.AttachDelegate(CCname[30], () => I.Settings.CCvalue[30]);
					break;
				case 31:
					I.AttachDelegate(CCname[31], () => I.Settings.CCvalue[31]);
					break;
				case 32:
					I.AttachDelegate(CCname[32], () => I.Settings.CCvalue[32]);
					break;
				case 33:
					I.AttachDelegate(CCname[33], () => I.Settings.CCvalue[33]);
					break;
				case 34:
					I.AttachDelegate(CCname[34], () => I.Settings.CCvalue[34]);
					break;
				case 35:
					I.AttachDelegate(CCname[35], () => I.Settings.CCvalue[35]);
					break;
				case 36:
					I.AttachDelegate(CCname[36], () => I.Settings.CCvalue[36]);
					break;
				case 37:
					I.AttachDelegate(CCname[37], () => I.Settings.CCvalue[37]);
					break;
				case 38:
					I.AttachDelegate(CCname[38], () => I.Settings.CCvalue[38]);
					break;
				case 39:
					I.AttachDelegate(CCname[39], () => I.Settings.CCvalue[39]);
					break;
				case 40:
					I.AttachDelegate(CCname[40], () => I.Settings.CCvalue[40]);
					break;
				case 41:
					I.AttachDelegate(CCname[41], () => I.Settings.CCvalue[41]);
					break;
				case 42:
					I.AttachDelegate(CCname[42], () => I.Settings.CCvalue[42]);
					break;
				case 43:
					I.AttachDelegate(CCname[43], () => I.Settings.CCvalue[43]);
					break;
				case 44:
					I.AttachDelegate(CCname[44], () => I.Settings.CCvalue[44]);
					break;
				case 45:
					I.AttachDelegate(CCname[45], () => I.Settings.CCvalue[45]);
					break;
				case 46:
					I.AttachDelegate(CCname[46], () => I.Settings.CCvalue[46]);
					break;
				case 47:
					I.AttachDelegate(CCname[47], () => I.Settings.CCvalue[47]);
					break;
				case 48:
					I.AttachDelegate(CCname[48], () => I.Settings.CCvalue[48]);
					break;
				case 49:
					I.AttachDelegate(CCname[49], () => I.Settings.CCvalue[49]);
					break;
				case 50:
					I.AttachDelegate(CCname[50], () => I.Settings.CCvalue[50]);
					break;
				case 51:
					I.AttachDelegate(CCname[51], () => I.Settings.CCvalue[51]);
					break;
				case 52:
					I.AttachDelegate(CCname[52], () => I.Settings.CCvalue[52]);
					break;
				case 53:
					I.AttachDelegate(CCname[53], () => I.Settings.CCvalue[53]);
					break;
				case 54:
					I.AttachDelegate(CCname[54], () => I.Settings.CCvalue[54]);
					break;
				case 55:
					I.AttachDelegate(CCname[55], () => I.Settings.CCvalue[55]);
					break;
				case 56:
					I.AttachDelegate(CCname[56], () => I.Settings.CCvalue[56]);
					break;
				case 57:
					I.AttachDelegate(CCname[57], () => I.Settings.CCvalue[57]);
					break;
				case 58:
					I.AttachDelegate(CCname[58], () => I.Settings.CCvalue[58]);
					break;
				case 59:
					I.AttachDelegate(CCname[59], () => I.Settings.CCvalue[59]);
					break;
				case 60:
					I.AttachDelegate(CCname[60], () => I.Settings.CCvalue[60]);
					break;
				case 61:
					I.AttachDelegate(CCname[61], () => I.Settings.CCvalue[61]);
					break;
				case 62:
					I.AttachDelegate(CCname[62], () => I.Settings.CCvalue[62]);
					break;
				case 63:
					I.AttachDelegate(CCname[63], () => I.Settings.CCvalue[63]);
					break;
				case 64:
					I.AttachDelegate(CCname[64], () => I.Settings.CCvalue[64]);
					break;
				case 65:
					I.AttachDelegate(CCname[65], () => I.Settings.CCvalue[65]);
					break;
				case 66:
					I.AttachDelegate(CCname[66], () => I.Settings.CCvalue[66]);
					break;
				case 67:
					I.AttachDelegate(CCname[67], () => I.Settings.CCvalue[67]);
					break;
				case 68:
					I.AttachDelegate(CCname[68], () => I.Settings.CCvalue[68]);
					break;
				case 69:
					I.AttachDelegate(CCname[69], () => I.Settings.CCvalue[69]);
					break;
				case 70:
					I.AttachDelegate(CCname[70], () => I.Settings.CCvalue[70]);
					break;
				case 71:
					I.AttachDelegate(CCname[71], () => I.Settings.CCvalue[71]);
					break;
				case 72:
					I.AttachDelegate(CCname[72], () => I.Settings.CCvalue[72]);
					break;
				case 73:
					I.AttachDelegate(CCname[73], () => I.Settings.CCvalue[73]);
					break;
				case 74:
					I.AttachDelegate(CCname[74], () => I.Settings.CCvalue[74]);
					break;
				case 75:
					I.AttachDelegate(CCname[75], () => I.Settings.CCvalue[75]);
					break;
				case 76:
					I.AttachDelegate(CCname[76], () => I.Settings.CCvalue[76]);
					break;
				case 77:
					I.AttachDelegate(CCname[77], () => I.Settings.CCvalue[77]);
					break;
				case 78:
					I.AttachDelegate(CCname[78], () => I.Settings.CCvalue[78]);
					break;
				case 79:
					I.AttachDelegate(CCname[79], () => I.Settings.CCvalue[79]);
					break;
				case 80:
					I.AttachDelegate(CCname[80], () => I.Settings.CCvalue[80]);
					break;
				case 81:
					I.AttachDelegate(CCname[81], () => I.Settings.CCvalue[81]);
					break;
				case 82:
					I.AttachDelegate(CCname[82], () => I.Settings.CCvalue[82]);
					break;
				case 83:
					I.AttachDelegate(CCname[83], () => I.Settings.CCvalue[83]);
					break;
				case 84:
					I.AttachDelegate(CCname[84], () => I.Settings.CCvalue[84]);
					break;
				case 85:
					I.AttachDelegate(CCname[85], () => I.Settings.CCvalue[85]);
					break;
				case 86:
					I.AttachDelegate(CCname[86], () => I.Settings.CCvalue[86]);
					break;
				case 87:
					I.AttachDelegate(CCname[87], () => I.Settings.CCvalue[87]);
					break;
				case 88:
					I.AttachDelegate(CCname[88], () => I.Settings.CCvalue[88]);
					break;
				case 89:
					I.AttachDelegate(CCname[89], () => I.Settings.CCvalue[89]);
					break;
				case 90:
					I.AttachDelegate(CCname[90], () => I.Settings.CCvalue[90]);
					break;
				case 91:
					I.AttachDelegate(CCname[91], () => I.Settings.CCvalue[91]);
					break;
				case 92:
					I.AttachDelegate(CCname[92], () => I.Settings.CCvalue[92]);
					break;
				case 93:
					I.AttachDelegate(CCname[93], () => I.Settings.CCvalue[93]);
					break;
				case 94:
					I.AttachDelegate(CCname[94], () => I.Settings.CCvalue[94]);
					break;
				case 95:
					I.AttachDelegate(CCname[95], () => I.Settings.CCvalue[95]);
					break;
				case 96:
					I.AttachDelegate(CCname[96], () => I.Settings.CCvalue[96]);
					break;
				case 97:
					I.AttachDelegate(CCname[97], () => I.Settings.CCvalue[97]);
					break;
				case 98:
					I.AttachDelegate(CCname[98], () => I.Settings.CCvalue[98]);
					break;
				case 99:
					I.AttachDelegate(CCname[99], () => I.Settings.CCvalue[99]);
					break;
				case 100:
					I.AttachDelegate(CCname[100], () => I.Settings.CCvalue[100]);
					break;
				case 101:
					I.AttachDelegate(CCname[101], () => I.Settings.CCvalue[101]);
					break;
				case 102:
					I.AttachDelegate(CCname[102], () => I.Settings.CCvalue[102]);
					break;
				case 103:
					I.AttachDelegate(CCname[103], () => I.Settings.CCvalue[103]);
					break;
				case 104:
					I.AttachDelegate(CCname[104], () => I.Settings.CCvalue[104]);
					break;
				case 105:
					I.AttachDelegate(CCname[105], () => I.Settings.CCvalue[105]);
					break;
				case 106:
					I.AttachDelegate(CCname[106], () => I.Settings.CCvalue[106]);
					break;
				case 107:
					I.AttachDelegate(CCname[107], () => I.Settings.CCvalue[107]);
					break;
				case 108:
					I.AttachDelegate(CCname[108], () => I.Settings.CCvalue[108]);
					break;
				case 109:
					I.AttachDelegate(CCname[109], () => I.Settings.CCvalue[109]);
					break;
				case 110:
					I.AttachDelegate(CCname[110], () => I.Settings.CCvalue[110]);
					break;
				case 111:
					I.AttachDelegate(CCname[111], () => I.Settings.CCvalue[111]);
					break;
				case 112:
					I.AttachDelegate(CCname[112], () => I.Settings.CCvalue[112]);
					break;
				case 113:
					I.AttachDelegate(CCname[113], () => I.Settings.CCvalue[113]);
					break;
				case 114:
					I.AttachDelegate(CCname[114], () => I.Settings.CCvalue[114]);
					break;
				case 115:
					I.AttachDelegate(CCname[115], () => I.Settings.CCvalue[115]);
					break;
				case 116:
					I.AttachDelegate(CCname[116], () => I.Settings.CCvalue[116]);
					break;
				case 117:
					I.AttachDelegate(CCname[117], () => I.Settings.CCvalue[117]);
					break;
				case 118:
					I.AttachDelegate(CCname[118], () => I.Settings.CCvalue[118]);
					break;
				case 119:
					I.AttachDelegate(CCname[119], () => I.Settings.CCvalue[119]);
					break;
				case 120:
					I.AttachDelegate(CCname[120], () => I.Settings.CCvalue[120]);
					break;
				case 121:
					I.AttachDelegate(CCname[121], () => I.Settings.CCvalue[121]);
					break;
				case 122:
					I.AttachDelegate(CCname[122], () => I.Settings.CCvalue[122]);
					break;
				case 123:
					I.AttachDelegate(CCname[123], () => I.Settings.CCvalue[123]);
					break;
				case 124:
					I.AttachDelegate(CCname[124], () => I.Settings.CCvalue[124]);
					break;
				case 125:
					I.AttachDelegate(CCname[125], () => I.Settings.CCvalue[125]);
					break;
				case 126:
					I.AttachDelegate(CCname[126], () => I.Settings.CCvalue[126]);
					break;
				case 127:
					I.AttachDelegate(CCname[127], () => I.Settings.CCvalue[127]);
					break;
				default:
					MIDIio.Info($"CCprop() not set: CC{CCnumber}");
					return false;
			}
			I.Settings.CCvalue[CCnumber] = I.Settings.CCvalue[CCnumber];		// SimHub otherwise shows 0
			return true;
		}	// CCprop()

// call CCprop to AttachDelegate() configured MIDIin properties

		internal void Attach(MIDIio I)
		{
			byte j, cc, pt;

			if (0 < MIDIio.Size[2] && MIDIio.Log(4, ""))
			{
				string s = "Attach() non-MIDI source properties:\n";

				for (pt = 0; pt < 3; pt++)
					for (j = 1; j < SourceCt[pt]; j++)
						if(null != SourceName[pt][j])
							s += "\t\t" + SourceName[pt][j] +"\n";
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
						Action(I, an++, cc);
					}
				}

			// MIDIin property configuration is now complete

			if (!MIDIio.DoEcho)
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
	}
}
