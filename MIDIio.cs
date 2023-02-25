using GameReaderCommon;
using SimHub.Plugins;
using System;

namespace blekenbleu.MIDIspace
{
	[PluginDescription("MIDI button, knob, slider; Joystick button and axis I/O routing")]
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
		private static byte Level;
		private bool[][] Once;
		private int[][] Sent;														// remember and don't repeat
		private string prop;
		private  double[,] scale;

		/// <summary>
		/// wraps SimHub.Logging.Current.Info(); prefixes MIDIio.My
		/// </summary>
		internal static bool Info(string str)
		{
			SimHub.Logging.Current.Info(MIDIio.My + str);	// bool Info()
			return true;
		}

		/// <summary>
		/// as Info(), with log level 1/2/4/8
		/// </summary>
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
//			VJD.Loop();									// for testing: loops thru configured axes and buttons
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

		private static int count = 0;
		private static long VJDmaxval;
		/// <summary>
		/// Called at SimHub start then after game changes
		/// </summary>
		/// <param name="pluginManager"></param>
		public void Init(PluginManager pluginManager)
		{
			// Log() level configuration
			prop = pluginManager.GetPropertyValue(Ini + "log")?.ToString();
			Level = (byte)((null != prop && 0 < prop.Length) ? Int32.Parse(prop) : 0);
			Log(8, $"log Level {Level}");

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
				VJDmaxval = VJD.Init(1);		// obtain vJoy parameters
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
			Dest = new byte[2 , size];				// for vJoy
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

			Once = new bool[Properties.SourceCt.Length][];		// null game properties
			Sent = new int[Properties.SourceCt.Length][];
			for (s = 0; s < Properties.SourceCt.Length; s++)
			{
				Once[s] = new bool[Properties.SourceCt[s]];
				Sent[s] = new int[Properties.SourceCt[s]];
			}
			for (s = 0; s < Properties.SourceCt.Length; s++)
				for (byte p = 0; p < Properties.SourceCt[s]; p++)
				{
					Once[s][p] = true;
					Sent[s][p] = 222;
				}

//			data = pluginManager.GetPropertyValue("DataCorePlugin.CustomExpression.MIDIsliders");
//			pluginManager.AddProperty("sliders", this.GetType(), (null == data) ? "unassigned" : data.ToString());
		}

		/// <summary>
		/// Called by OnEventReceived() for each MIDIin ControlChangeEvent
		/// track active CCs and save values
		/// </summary>
		internal bool Active(byte CCnumber, byte value)								// returns true if first time
		{
			byte which = Properties.Which[CCnumber];

			if (0 < which && Settings.Sent[CCnumber] == value)
				return false;														// ignore unchanged values

			Settings.Sent[CCnumber] = value;
/*
			if (5 == CCnumber)
				Log(8, "Active(): " + Properties.CCname[CCnumber]);					// just a debugging breakpoint

			Log(8, $"Active(): ControlNumber = {CCnumber}; ControlValue = {value}");
 */
			if (0 < which)															// Known and changed?
			{
				if (0 < (Properties.Button & which))
				{
					Outer.Latest = value;											// drop pass to Ping()
					this.TriggerEvent(Properties.CCname[CCnumber]);
				}
//*	Near-real-time routing					
				if (0 < (56 & which))												// call Send()?
					for (byte d = 0; d < Properties.Route.Length; d++)				// at most one Send() to each dt from each CCnumber
						if (0 < (Properties.Route[d] & which))
							Send((double)value, d, CCnumber, 0, 3, Properties.CCname[CCnumber]);
//*/
				return false;
			}

			if (DoEcho)
				return Outer.SendCCval(CCnumber, Settings.Sent[CCnumber] = value);	// do not CCprop()

			// First time CC number seen
			Properties.Which[CCnumber] = Properties.Unc;							// dynamic CC configuration
			return Properties.CCprop(this, CCnumber);
		}																			// Active()

/*
 ; Properties.SourceName[][] is an array of property names for other than MIDI
 ; My + CCname[cc] MIDI properties correspond to Settings.Sent[cc]
 ;
 ; Accomodate device value range differences:
 ; 0 <= MIDI cc and value < 128
 ; 0 <= JoyStick property <= VJDmaxval
 ; 0 <= ShakeIt property <= 100.0
 */
		/// <summary>
		/// Called by DoSend() and Active() for each property change sent;
		/// d: 0=VJD.Axis; 1=VJD.Button; 2=Outer.SendCCval;  i: d address
		/// t: 0=game; 1=Joy axis, 2=Joy button 3=MIDIin;    p: t address
		/// prop: source property name for error log
		/// </summary>
		internal void Send(double property, byte d, byte i, byte p, byte t, string prop)
		{
			int	a = (int)(0.5 + scale[d, t] * property);

			if ( 0 > a || (3 > d && Sent[t][p] == a))		// CC duplication is already handled
				return;										// send only changed values

			Sent[t][p] = a;
			switch (d)
			{
				case 0:
					if (VJD.Usage.Length > i)
						VJD.Axis(i, a);						// 0-based axes
					else Info($"DoSend({Properties.DestType[d]}): invalid axis {i} from {prop}");
					break;
				case 1:
					VJD.Button((byte)(1 + i), 0 < a);						// 1-based buttons
					break;
				case 2:
					Outer.SendCCval(i, Settings.Sent[i] = (byte)(0x7F & a));
					break;
				default:											// should be impossible
					Info($"DoSend(): mystery property {prop} send type {d}, source type {t}, index{p}");
					break;
			}
		}

		/// <summary>
		/// Called by DataUpdate(); calls Send()
		/// index: 0=game; 1=Joystick Source property types
		/// </summary>
		private void DoSend(PluginManager pluginManager, byte index)		// 0: game;	1: always
		{																	// handle 3: CC  in Active()
			byte[,] table = {{0, 1}, {1, 3}};                               // source indices 0: game, 1: axis, 2: button
			byte d, i;														// destination type, index
			string send;

			for (byte t = table[index, 0]; t < table[index, 1]; t++)		// source type index
				for (byte p = 0; p < Properties.SourceCt[t]; p++)			// index properties of a type
				{
					prop = Properties.SourceName[t][p];						// t: game, axis, button
					d = Properties.SourceArray[t, 0, p];
					i = Properties.SourceArray[t, 1, p];

					if ((0 == t) && !Once[t][p])
							continue;										// skip null (game) properties

					if (null == (send = pluginManager.GetPropertyValue(prop)?.ToString()))
					{
						if (2 == t)											// null Joystick button properties until pressed
							continue;

						if (Once[t][p])
							Info($"DoSend({Properties.DestType[d]}): null {prop} for SourceName[{t}][{p}]");

						Once[t][p] = false;									// property not configured
					}
					else if (0 == send.Length)
						Info($"DoSend({Properties.DestType[d]}): 0 length {prop}");
					else Send(Convert.ToDouble(send), d, i, p, t, prop);
				}
		}			// DoSend()
	}
}
