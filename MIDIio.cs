using GameReaderCommon;
using SimHub.Plugins;
using System;

namespace blekenbleu.MIDIspace
{
	[PluginDescription("MIDI slider I/O")]
	[PluginAuthor("blekenbleu")]
	[PluginName("MIDIio")]
	public class MIDIio : IPlugin, IDataPlugin
	{
		internal MIDIioSettings Settings;
		internal static IOproperties Properties;
		internal VJsend VJD;
		internal INdrywet Reader;
		internal OUTdrywet Outer;
		internal static readonly string Ini = "DataCorePlugin.ExternalScript.MIDI";	// configuration source
		internal static readonly string My = "MIDIio.";								// SimHub Plugin Name + '.'
		internal static string[] Real { get; } = { My, "JoystickPlugin.", "InputStatus." };
		internal static byte size = 8;												// default configurable output count
		internal static byte[] Size;
		internal static byte[,] Dest;												// which VJD axis or button
		internal static bool DoEcho;
		private byte[] DoSendCt;
		private static byte Level;
		private bool[,] Once;
		private int[,] Sent;														// remember and don't repeat
		private string prop;
		private  double[,] scale;

		internal static bool Info(string str)
		{
			SimHub.Logging.Current.Info(MIDIio.My + str);	// bool Info()
			return true;
		}

		internal static bool Log(byte level, string str)
		{
			bool b = 0 < (level & Level);

			if (b && 0 < str.Length)
			SimHub.Logging.Current.Info(MIDIio.My + str);	// bool Log()
			return b;
		}

		/// <summary>
		/// Instance of the current plugin manager
		/// </summary>
		public PluginManager PluginManager { get; set; }

		/// <summary>
		/// Called one time per game data update, contains all normalized game data,
		/// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
		///
		/// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
		///
		/// </summary>
		/// <param name="pluginManager"></param>
		/// <param name="data">Current game data, including current and previous data frame.</param>
		public void DataUpdate(PluginManager pluginManager, ref GameData data)
		{
			if (data.GameRunning && data.OldData != null && data.NewData != null)
				DoSend(pluginManager, 0);

			// Send my property messages anytime (echo)
			DoSend(pluginManager, 1);
//			VJD.Run();									// for testing: loops thru configured axes and buttons
		}

		/// <summary>
		/// Called at plugin manager stop, close/dispose anything needed here !
		/// Plugins are rebuilt at game change
		/// </summary>
		/// <param name="pluginManager"></param>
		public void End(PluginManager pluginManager)
		{
			if (null != Reader)
			Reader.End();
			if (null != Outer)
				Outer.End();
			if (null != VJD)
				VJD.End();
			Properties.End(this);
			this.SaveCommonSettings("GeneralSettings", Settings);
		}

		/// <summary>
		/// Called at SimHub start then after game changes
		/// </summary>
		/// <param name="pluginManager"></param>
		private static int count = 0;
		private static long VJDmaxval;
		public void Init(PluginManager pluginManager)
		{
			// Log() level configuration
			prop = pluginManager.GetPropertyValue(Ini + "log")?.ToString();
			Level = (byte)((null != prop && 0 < prop.Length) ? Int32.Parse(prop) : 0);
			Log(8, $"log Level {Level}");

//			Log(4, "Init()");

// Load settings
			Settings = this.ReadCommonSettings<MIDIioSettings>("GeneralSettings", () => new MIDIioSettings());

			prop = pluginManager.GetPropertyValue(Ini + "echo")?.ToString();
			DoEcho = (null != prop && 0 < Int32.Parse(prop));
			Info("Init(): unconfigured MIDIin CCs will" + (DoEcho ? "" : " not") + " be forwarded to MIDIout");
			count += 1;		// increments for each restart, provoked e.g. by game change or restart
			pluginManager.AddProperty(My + "Init().count", this.GetType(), count);

			int s = size;
			prop = pluginManager.GetPropertyValue(Ini + "size")?.ToString();
			if (null != prop && 0 < prop.Length && (0 >= (s = Int32.Parse(prop)) || 128 < s))
				Info($"Init(): invalid {Ini + "size"} {prop}; defaulting to {size}");
			else size = (byte)s;

			Size = new byte[] { size, size, size };
			prop = pluginManager.GetPropertyValue(Ini + "vJoy")?.ToString();
			if (null != prop && 1 == prop.Length && ("0" != prop))
			{
				VJD = new VJsend();				// vJoy
				VJDmaxval = VJD.Init(1);		// obtain joystick button and axis counts VJD.nButtons, VJD.nAxes
				if (size > VJD.nAxes)
					Size[0] = VJD.nAxes;
				if (size > VJD.nButtons)
					Size[1] = VJD.nButtons;
			}
			else
			{
				VJDmaxval = 65535;
				Size[0] = Size[1] = 0;
			}
			double vMax = (double)VJDmaxval;
								   // game			Joystick axis	button	MIDIin
			scale = new double[,] {	{ vMax/100.0,	vMax/65535.0,	vMax,	vMax/127},	// vJoy axis
						 			{ 0.02,			2.0/65535.0,	1,		2.0/127 },	// vJoy button
									{ 1.27,			127.0/65535.0,  127,	1.0		}};	// MIDIout

			// Launch Outer before Reader and Properties
			prop = pluginManager.GetPropertyValue(Ini + "out")?.ToString();
			if (null == prop || 0 == prop.Length)
			{
				Info((null == prop) ? "Init(): missing " + Ini + "out entry!" : "Init(): " + Ini + "out is undefined");
				Size[2] = 0;
			}
			else
			{
				pluginManager.AddProperty("out", this.GetType(), prop);
				Outer = new OUTdrywet();
				Outer.Init(prop);	// may zero Size[2]
			}

			Properties = new IOproperties();		// MIDI and vJoy property configuration
			Dest = new byte[2 , size];				// for vJoy;  CC are indexed by Map[,]
			Properties.Init(this);					// send unconfigured DoEchoes, set Dest[,] SendCt[,], sort Send[, ]

			prop = pluginManager.GetPropertyValue(Ini + "in")?.ToString();
			if (null == prop)
				Info("Init(): missing " + Ini + "in entry!");
			else if (0 < prop.Length)
			{
				pluginManager.AddProperty("in", this.GetType(), prop);
				Reader = new INdrywet();
				if(Reader.Init(prop, this))
					Properties.Attach(this);		// AttachDelegate buttons, sliders and knobs
			}
			else Info("Init(): " + Ini + "in is undefined" );

			Once = new bool[Properties.SendType.Length, size];		// null game properties

			for (byte j = 0; j < Properties.SendType.Length - 1; j++)
				for (int i = 0; i < Properties.SendCt[j, 3]; i++)	// CC counts
					Settings.Sent[Properties.Map[j, i]] = 129;		// impossible first CC Send[i] values

			for (s = 0; s < Properties.SendType.Length; s++)
				for (byte i = 0; i < size; i++)
					Once[s, i] = true;

			DoSendCt = new byte[3];
			Sent = new int[Properties.SendType.Length, size];
			for (byte j = 0; j < Properties.SendType.Length; j++)
			{
				for (byte i = 0; i < size; i++)
				Sent[j, i] = 222;

				DoSendCt[j] = 0;
				for (byte i = 0; i < 4; i++)
					DoSendCt[j] += Properties.SendCt[j, i];

				if (DoSendCt[j] > Size[j])
					DoSendCt[j] = Size[j];
			}

//			data = pluginManager.GetPropertyValue("DataCorePlugin.CustomExpression.MIDIsliders");
//			pluginManager.AddProperty("sliders", this.GetType(), (null == data) ? "unassigned" : data.ToString());
		}

		// track active CCs and save values
		internal bool Active(byte CCnumber, byte value)					// returns true if first time
		{
			byte which = Properties.Which[CCnumber];
			if (5 == CCnumber)
			Log(8, "Active(): " + Properties.CCname[CCnumber]);			// just a debugging breakpoint

			Log(8, $"Active(): ControlNumber = {CCnumber}; ControlValue = {value}");
			if (0 < which)						// Known?
			{
				if (Settings.Sent[CCnumber] != value)
				{
					Settings.Sent[CCnumber] = value;
					if (0 < (Properties.Button & which))
					{
						Outer.Latest = value;				// drop pass to Ping()
						this.TriggerEvent(Properties.CCname[CCnumber]);
					}
					if (DoEcho && 0 < (Properties.Unc & which))
						return !Outer.SendCCval(CCnumber, value); 	// unconfigured may also be appropriated configured
				}
				return false;
			}

			if (DoEcho)
			{
				if (value != Settings.Sent[CCnumber])				// send only changes
					Outer.SendCCval(CCnumber, Settings.Sent[CCnumber] = value);	// do not SetProp()
				return true;
			}

			// First time CC number seen
			Properties.Which[CCnumber] = Properties.Unc;		// dynamic CC configuration
//			this.AddEvent(Properties.CCname[CCnumber]);			// Users may assign CCn events to e.g. Ping()
//			this.TriggerEvent(Properties.CCname[CCnumber]);
			Settings.Sent[CCnumber] = value;
			return Properties.SetProp(this, CCnumber);
		}	// Active()

/*
 ; Properties.Send[,] is an array of property names for other than MIDIin
 ; My + CCname[cc = Properties.Map[,]] are MIDIin properties corresponding to Settings.Sent[0, cc]
 ;
 ; Accomodate device value range differences:
 ; 0 <= MIDI cc and value < 128
 ; 0 <= JoyStick property <= VJDmaxval
 ; 0 <= ShakeIt property <= 100.0
 */
		private void DoSend(PluginManager pluginManager, byte index)					// 0: game;	1: always
		{
			int a;
			byte[,] table = {{0, 1}, {1, 4}};			// source indices 0: game, 1: axis, 2: button, 3: CC
			byte cc = 0;																// for MIDIout
			string send;

			for (byte s = 0; s < Properties.SendType.Length; s++)						// destination index
			{
				int d = -1;									// Dest index

				for (byte t = table[index, 0]; t < table[index, 1]; t++)				// source type index
					for (byte p = 0; p < Properties.SendCt[s, t]; p++)					// index properties of a type
					{
						if (!Once[s, ++d] && 0 == t)
							continue;													// skip null (game) properties

						if (3 > t)
							prop = Properties.SendName[s][t][p];						// t: game, axis, button
						else prop = My + Properties.CCname[cc = Properties.Map[s, p]];	// MIDIout CC number to name

						if (null == (send = pluginManager.GetPropertyValue(prop)?.ToString()))
						{
							if (1 == t)								// Joystick button properties do not appear until pressed
								continue;

							if (Once[s, d])
								Info($"DoSend({Properties.SendType[s]}): null {prop} for Send[{s}][{t}][{p}]");
							Once[s, d] = false;						// property not configured
						}
						else if (0 < send.Length)
						{
							double property = Convert.ToDouble(send);

							a = (int)(0.5 + scale[s, t] * property);
							if (d >= DoSendCt[s] || 0 > a || Sent[s, d] == a)
								continue;										// send only changed values

							Sent[s, d] = a;
							switch (s)
							{
								case 0:												// rescale to vJoy
									VJD.Axis(Dest[0, d], a);						// 0-based axes
									break;
								case 1:
									VJD.Button(Dest[1, d], 0 < a);					// 1-based buttons
									break;
								case 2:
									Outer.SendCCval(cc, Settings.Sent[cc] = (byte)(0x7F & a));
									break;
								default:											// should be impossible
									Info($"DoSend(): mystery property {prop} send type {s}, source type {t}, index{p}");
									break;
							}
						}
						else Info($"DoSend({Properties.SendType[s]}): 0 length {prop}");
					}
			}
		}		// DoSend()
	}
}
