using System.Linq;
using System.Collections.Generic;

namespace blekenbleu
{
	// Dynamically add events and properties for CC buttons as pressed
	// Working around the SimHub limitation that AttachDelegate() fails for variables.

	internal class Source									// SourceList elements
	{
		internal byte Device;								// destination device
		internal byte Addr;									// destination device address
		internal string Name;								// source property name
	}

	internal partial class IOproperties
	{
		MIDIio M;
		internal static readonly string[] DestDev =			// destination devices
			{ "vJoyAxis", "vJoyButton", "CC" };
		private readonly static string[] SourceType =		// SourceList[] source property types
			{"game", "Joystick axis", "Joystick button"};

		// non-CC source properties, sent in SendIf() by DataUpdate()
		internal List<Source>[] SourceList = new List<Source>[SourceType.Length];

		// VJD.Init() has already run; now sort "my" CC properties first for sending, even when game is not running
        /// <summary>
		/// see InitCC.cs for MIDIin initialization
		/// other property initializations here
        /// </summary>
		internal void Init(MIDIio I)
		{																	// CC configuration property types
			M = I;
			byte[][] Darray = new byte[DestDev.Length][];					// destination addresses, extracted from .ini
			byte dt, j, first = (byte)((null == I.VJD) ? 2 : 0);

			InitCC();
		
/* SendIf() may send any of 3 property value source types to any of 3 DestDev[]s: (vJoy axes, vJoy buttons, MIDIout)
 ; SendIf() indexes SourceList[st] for those property SourceType[]s: (game, JoyStick axis or button)
 ; DataUpdate() calls SendIf(0) for SourceList[0] only when games are active.
 ; ActionCC() separately handles CC sends to all 3 DestDev[]s asynchronously in INdrywet.OnEventReceived()
 */
			for (dt = 0; dt < SourceList.Length; dt++)
				SourceList[dt] = new List<Source>();

			for (dt = 0; dt < DestDev.Length; dt++)
			{
				string pts;

				// configured destination indices
				string ds = I.PluginManager.GetPropertyValue(pts = MIDIio.Ini + DestDev[dt] + 's')?.ToString();
				if (null == ds && (dt < first || MIDIio.Info($"IOProperties.Init(): {DestDev[dt]} property '{pts}' not found")))
					continue;

				// bless the Internet: split comma separated integers
				Darray[dt] = ds.Split(',').Select(byte.Parse).ToArray();
			}

			if (null != Darray[0] && null != I.VJD)
				for (j = 0; j < Darray[0].Length; j++)						// valid vJoy axes address?
					if (I.VJD.Usage.Length <= Darray[0][j])
						MIDIio.Info($"IOProperties.Init(): Invalid {DestDev[0]} address {Darray[0][j]} > {I.VJD.Usage.Length}");

            if (null != Darray[1])
                for (j = 0; j < Darray[1].Length; j++)						// valid vJoy button address?
					if (0 > Darray[1][j] || Darray[1][j] >= I.VJD.nButtons)
						MIDIio.Info($"IOProperties.Init(): Invalid {DestDev[1]} address {Darray[1][j]}");

			// collect ListCC[][], Map[], SourceList[] from Darray[]
            string dp;
			for (dt = 0; dt < Darray.Length; dt++)										// vJoy axis, vJoy button, CC
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
					else dp += DestDev[dt];
					dp += (Darray[dt][i]).ToString();

					string prop = I.PluginManager.GetPropertyValue(dp)?.ToString();

			 		if (null == prop)													// Configured non-Button properties should not be null
					{
						if("vJoyButton" != DestDev[dt])
							MIDIio.Info($"IOproperties.Init(): null DestDev[{DestDev[dt]}] property {dp}");
						continue;
					}

					switch (prop.Substring(0, 7))
					{
						case "MIDIio.":
        					// build routing tables for CC sends to MIDI and vJoy
							WhichCC(dt, Darray[dt][i], prop);							// CC property names are in CCname[]
							break;
						case "Joystic":													// JoyStick
							SourceList[1].Add(new Source() { Name = prop, Device = dt, Addr = Darray[dt][i] });
							break;
						case "InputSt":													// any SimHub controller
							SourceList[2].Add(new Source() { Name = prop, Device = dt, Addr = Darray[dt][i] });
							break;
						default:														// "game"
							SourceList[0].Add(new Source() { Name = prop, Device = dt, Addr = Darray[dt][i] });
							break;
					}
				}
			}

			for (dt = 0; dt < SourceList.Length; dt++)
				M.stop[dt] = (byte)SourceList[dt].Count;										// SourceList[].Count >= stop[] are SimHub Events.

			// optionally log
			if (MIDIio.Log(4, ""))
			{
				string s = "";

				for (dt = 0; dt < DestDev.Length; dt++)
				{
					byte k = 0;
					string t = "";
					bool some = false;

					if (0 < dt)
						t += "\n\t";
					t +=  $"IOProperties.SourceList[{DestDev[dt]}].Name:  ";

					for (byte pt = 0; pt < 3; pt++)					// property type: game, Joy axis, Joy button, CC
					{
						for (j = 0; j < SourceList[pt].Count; j++)
						{
							// test for configured destination device
							if (dt != SourceList[pt][j].Device)
								continue;

							some = true;
							string N = SourceList[pt][j].Name;

							if (0 < k++)
								t += "\n\t\t\t\t";
							else if (0 < SourceList[pt].Count)
								t += "\n\t\t\t\t";
							if(null != N)
								t += $"@ {SourceList[pt][j].Addr}: " + N;
							else t += $"\nnull == SourceList[{pt}][{j}].Name\n\t\t\t\t\t";
						}
					}

					// unlike other sources, CC source address is the known at Send() time
					for (j = 0; j < 128; j++)
					{
						// test for configured destination device
						if (0 == ((8 << dt) & Which[j]))
							continue;

						some = true;
						string N = CCname[j];

						if (0 < k++)
							t += "\n\t\t\t\t";
						else t += "\n\t\t\t\t";
                        if (Map[j] < ListCC.Count)
						{
							if (null != N)
								t += "@ " + (ListCC[Map[j]][dt]) + ": " + N;
							else t += $"\nnull == CCname[{j}]!!\n\t\t\t\t\t";
						} else  t += $"\nMap[{j}] = {Map[j]} >= ListCC.Count {ListCC.Count}!!\n\t\t\t\t\t";
					}
					if (some)
						s += t;
				}
				MIDIio.Info(s + "\n");

				s = "Properties.CCname[]:";
				for (dt = 0; dt < 128; dt++)
					if (0 < (3 & Which[dt]))
					{
						s += $"\n\t{CCname[dt]}\t@ {dt}";
						if (0 < (SendEvent & Which[dt]))
							s += " (SendEvent)";
						for (byte pt = 0; pt < DestDev.Length; pt++)
							if (0 < ((8 << pt) & Which[dt]))	// 8, 16, 32
							{
								byte b = ListCC[Map[dt]][pt];

								if (1 == pt)	// replace vJoyButton with vJoyB0
								{
									s += $"  vJoyB";
									if (10 > b)
										s += "0";
								}
								else s += $"  {DestDev[pt]}";
								s += $"{b}";
							}
					}
				if (17 < s.Length)
					MIDIio.Info(s + "\n");
			}								// if (MIDIio.Log(4, ""))
		}									// Init()
	}
}
