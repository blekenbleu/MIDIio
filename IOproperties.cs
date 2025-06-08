using System.Linq;
using System.Collections.Generic;
using SimHub.Plugins;

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
		internal readonly static string[] DestDev =			// destination devices
			{ "vJoyAxis", "vJoyButton", "CC" };
		// unlike a MIDIin event's known CCnumber, search SourceList for other input source property value changes
		private readonly static string[] SourceType =		// (non-CC) SourceList[] source property types
			{"game", "Joystick axis", "Joystick button"};

		// called by DataUpdate(), SendIf() searchs SourceList for non-CC source property changed values
		internal List<Source>[] SourceList = new List<Source>[SourceType.Length];

		// VJD.Init() has already run; now sort "my" CC properties first for sending, even when game is not running
        /// <summary>
		/// see InitCC.cs for MIDIin initialization
		/// other property initializations here
        /// </summary>
		internal void Init(MIDIio I, PluginManager pluginManager)
		{																	// CC configuration property types
			M = I;
			byte[][] Darray = new byte[DestDev.Length][];					// destination addresses, extracted from .ini
			byte dt, j, first = (byte)((null == I.VJD) ? 2 : 0);

			InitCC();
		
/* SendIf() may send any of 3 property value source types to any of 3 DestDev[]s: (vJoy axes, vJoy buttons, MIDIout)
 ; SendIf() indexes SourceList[st] for those property SourceType[]s: (game, JoyStick axis or button)
 ; DataUpdate() calls SendIf(0) for SourceList[0] only when games are active.
 ; ReceivedCC() separately handles CC sends to all 3 DestDev[]s asynchronously in INdrywet.OnEventReceived()
 */
			for (int src = 0; src < SourceList.Length; src++)
				SourceList[src] = new List<Source>();

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
					dp = MIDIio.Ini + DestDev[dt];
					if (1 == dt && 10 > Darray[1][i])
						dp += "0";
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

				for (dt = 0; dt < DestDev.Length; dt++)							// sort by destination device
				{
					byte k = 0;
					string t = "";
					bool some = false;

					if (0 < dt)
						t += "\n\t";
					t +=  $"IOProperties.SourceList[{DestDev[dt]}].Name:  ";

					for (byte src = 0; src < 3; src++)							// nonCC source properties: game, Joy axis, Joy button
					{
						for (ushort sd = 0; sd < SourceList[src].Count; sd++)   // source DestDev iteration
                        {
							// test for configured destination device
							if (dt != SourceList[src][sd].Device)
								continue;

							some = true;
							string N = SourceList[src][sd].Name;

							if (0 < k++)
								t += "\n\t\t\t\t";
							else if (0 < SourceList[src].Count)
								t += "\n\t\t\t\t";
							if(null != N)
								t += $"@ {SourceList[src][sd].Addr}: " + N;
							else t += $"\nnull == SourceList[{src}][{sd}].Name\n\t\t\t\t\t";
						}
					}

					// unlike other sources, CC source address is the known at Send() time
					for (byte cc = 0; cc < 128; cc++)
					{
						// test for configured destination device
						if (0 == ((2 << dt) & Which[cc]))
							continue;

						some = true;
						string N = CCname[cc];

						if (0 < k++)
							t += "\n\t\t\t\t";
						else t += "\n\t\t\t\t";
                        if (Map[cc] < ListCC.Count)
						{
							if (null != N)
								t += "@ " + (ListCC[Map[cc]][dt]) + ": " + N;
							else t += $"\nnull == CCname[{cc}]!!\n\t\t\t\t\t";
						} else  t += $"\nMap[{cc}] = {Map[cc]} >= ListCC.Count {ListCC.Count}!!\n\t\t\t\t\t";
					}
					if (some)	// no joystick button sources may be detected
						s += t;
				}
				if (0 < s.Length)
					MIDIio.Info(s + "\n");

				string props = pluginManager.GetPropertyValue(MIDIio.Ini + "sends")?.ToString();
				if (null != props && 1 < props.Length)				// set up Events and Actions
					EnumActions(pluginManager, props.Split(',')); 	// add MIDIsends to Properties.SourceList[]

				s = "Properties.CCname[]:";
				for (byte cc = 0; cc < 128; cc++)
					if (0 < (CC & Which[cc]))
					{
						s += $"\n\t{CCname[cc]}\t@ {cc}";
						if (0 < (SendEvent & Which[cc]))
							s += " (SendEvent)";
						for (dt = 0; dt < DestDev.Length; dt++)
							if (0 < ((2 << dt) & Which[cc]))	// 8, 16, 32
							{
								byte b = ListCC[Map[cc]][dt];

								if (1 == dt)	// replace vJoyButton with vJoyB0
								{
									s += $"  vJoyB";
									if (10 > b)
										s += "0";
								}
								else s += $"  {DestDev[dt]}";
								s += $"{b}";
							}
					}
				if (17 < s.Length)
					MIDIio.Info(s + "\n");
			}								// if (MIDIio.Log(4, ""))
		}									// Init()
	}
}
