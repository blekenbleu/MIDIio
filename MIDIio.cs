﻿using GameReaderCommon;
using SimHub.Plugins;
using System;

namespace blekenbleu.MIDIspace
{
    [PluginDescription("MIDI slider I/O")]
    [PluginAuthor("blekenbleu")]
    [PluginName("MIDIio")]
    public class MIDIio : IPlugin, IDataPlugin
    {
	private byte size = 8;						// default configurable output count
	internal byte[] Size;
	private bool[,] Once;
	private byte[][] Sent;
	internal static string Ini = "DataCorePlugin.ExternalScript.MIDI";	// configuration source
	internal static string My = "MIDIio.";					// PluginName + '.'
        internal string[] Real { get; } = { My, "JoystickPlugin." };
	internal MIDIioSettings Settings;
	internal static CCProperties Properties;
	internal VJsend VJD;
	internal INdrywet Reader;
	internal OUTdrywet Outer;
	private static byte Level;
	internal static bool DoEcho;
        private string prop;

	internal static bool Info(string str)
	{
	    SimHub.Logging.Current.Info(MIDIio.My + str);	// bool Info()
	    return true;
	}

	internal static bool Log(byte level, string str)
	{
	    bool b = 0 < (level & Level);

	    if (b)
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
		DoSend(pluginManager, false);

	    // Send my property messages anytime (echo)
	    DoSend(pluginManager, true);
//	    VJD.Run();				// for testing: loops thru configured axes and buttons
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
	    VJD.End();
	    Properties.End(this);
	    this.SaveCommonSettings("GeneralSettings", Settings);
	}

	/// <summary>
	/// Called at SimHub start then after game changes
	/// </summary>
	/// <param name="pluginManager"></param>
	private static int count = 0;
        private long VJDmaxval;
	public void Init(PluginManager pluginManager)
	{
	    Log(4, "Init()");
	    // Load settings
	    Settings = this.ReadCommonSettings<MIDIioSettings>("GeneralSettings", () => new MIDIioSettings());

	    prop = pluginManager.GetPropertyValue(Ini + "echo")?.ToString();
	    DoEcho = (null != prop && 0 < Int32.Parse(prop));
	    Info("Init(): unconfigured CCs will" + (DoEcho ? "" : " not") + " be echo'ed");

	    int s = size;
	    prop = pluginManager.GetPropertyValue(Ini + "size")?.ToString();
	    if (null != prop && 0 < prop.Length && (0 >= (s = Int32.Parse(prop)) || 128 < s))
		Info($"Init(): invalid {Ini + "size"} {prop}; defaulting to {size}");
	    else size = (byte)s;

	    // Log() level configuration
	    prop = pluginManager.GetPropertyValue(Ini + "log")?.ToString();
	    Level = (byte)((null != prop && 0 < prop.Length) ? Int32.Parse(prop) : 0);
            Log(8, $"log Level {Level}");

	    VJD = new VJsend();			// vJoy
	    VJDmaxval = VJD.Init(1);		// obtain joystick button and axis counts VJD.nButtons, VJD.nAxes

	    Size = new byte[] {size, (size < VJD.nAxes) ? size : VJD.nAxes, (size < VJD.nButtons) ? size : VJD.nButtons};
	    Properties = new CCProperties();    // MIDI and vJoy property configuration
	    Properties.Init(this, Size);	// set SendCt[], sort My Send[,] first and unconfigured before Outer.Init()

	    // Launch Outer before Reader, which tries to send stored MIDI CC messages
	    prop = pluginManager.GetPropertyValue(Ini + "out")?.ToString();
	    if (null == prop)
		Info("Init(): missing " + Ini + "out entry!");
	    else if (0 < prop.Length)
	    {
	    	pluginManager.AddProperty("out", this.GetType(), prop);
		Outer = new OUTdrywet();
		Outer.Init(this, prop, Properties.SendCt[0]);
	    }
	    else Info("Init(): " + Ini + "out is undefined" );

	    prop = pluginManager.GetPropertyValue(Ini + "in")?.ToString();
	    if (null == prop)
		Info("Init(): missing " + Ini + "in entry!");
	    else if (0 < prop.Length)
	    {
		pluginManager.AddProperty("in", this.GetType(), prop);
		Reader = new INdrywet();
		if(Reader.Init(prop, this))
		    Properties.Attach(this);	// AttachDelegate buttons, sliders and knobs
	    }
	    else Info("Init(): " + Ini + "in is undefined" );

	    count += 1;		// increments for each restart, provoked e.g. by game change or restart
	    pluginManager.AddProperty(My + "Init().count", this.GetType(), count);
	    Once = new bool[Properties.Send.GetLength(0), size];

	    for (int i = 0; i < size; i++)
		Settings.Sent[Properties.Map[0][i]] = 129;	// impossible first Send[i] values
	    for (byte j = 0; j < Properties.Send.GetLength(0); j++)
		for (int i = 0; i < Size[j]; i++)
		    Once[j, i] = true;

	    Sent = new byte[][] {Settings.Sent, new byte[Size[1]], new byte[Size[2]]};
	    for (byte j = 1; j < Properties.Send.GetLength(0); j++)
		for (byte i = 0; i < Size[j]; i++)
		    Sent[j][i] = 0;

//	    data = pluginManager.GetPropertyValue("DataCorePlugin.CustomExpression.MIDIsliders");
//	    pluginManager.AddProperty("sliders", this.GetType(), (null == data) ? "unassigned" : data.ToString());
	}

	// track active CCs and save values
	internal bool Active(byte CCnumber, byte value)			// returns true if first time
	{
	    byte which = Properties.Which[CCnumber];
	    if (5 == CCnumber)
		Log(8, "Active(): " + Properties.CCname[CCnumber]);			// just a debugging breakpoint

	    Log(8, $"Active(): ControlNumber = {CCnumber}; ControlValue = {value}");
	    if (0 < which)						// Known?
	    {
		if (Properties.CCvalue[CCnumber] != value)
		{
		    Properties.CCvalue[CCnumber] = value;
		    if (0 < (Properties.Button & which))
		    {
			Outer.Latest = value;				// drop pass to Ping()
			this.TriggerEvent(Properties.CCname[CCnumber]);
		    }
		    if (DoEcho && 0 < (Properties.unconfigured & which))
			return !Outer.SendCCval(CCnumber, value); 	// unconfigured may also be appropriated configured
		}
		return false;
	    }

	    if (DoEcho)
	    {
		if (value != Properties.CCvalue[CCnumber])				// send only changes
		    Outer.SendCCval(CCnumber, Properties.CCvalue[CCnumber] = value);	// do not SetProp()
		return true;
	    }

	    // First time CC number seen
	    Properties.Which[CCnumber] = Properties.unconfigured;	// dynamic CC configuration
//	    this.AddEvent(Properties.CCname[CCnumber]);			// Users may assign CCn events to e.g. Ping()
//	    this.TriggerEvent(Properties.CCname[CCnumber]);
	    return Properties.SetProp(this, CCnumber, Properties.CCvalue[CCnumber] = value);
	}	// Active()

	// Properties.Send[,] is an array of property names
	// Properties.Map[][] a prioritized array of destination indices
	// Properties.Map[0] are MIDIout CCn corresponding to Settings.Sent[]
	// Properties.Map[1-2] are vJoy axis and button indices, requiring their own Sent[,] array.
	private void DoSend(PluginManager pluginManager, bool always)
	{
	    byte end, j, b, cc, value;
            string send;

	    for (j = 0; j < Properties.Send.GetLength(0); j++)
	    {
		end = (always) ? Properties.MySendCt[j] : Properties.SendCt[j];

		for (b = (byte)((always) ? 0 : Properties.MySendCt[j]); b < end; b++)
		{
		    cc = Properties.Map[j][b];	// MIDIout CC number or vJoy button or axis
                        
		    if (!Once[j, b])
		       continue;

		    prop = Properties.Send[j, b];
		    send = pluginManager.GetPropertyValue(prop)?.ToString();

		    if (null == send)
		    {
			Once[j, b] = false;
			Info("DoSend(): null " + prop);
		    }
		    else if (0 < send.Length)
		    {
/*
 ; At least 3 possibilities:
 ; 1) 0 <= MIDI value < 128
 ; 2) 0 <= ShakeIt property <= 100.0
 ; 3) 0 <= JoyStick property <= VJDmaxval
 */
			double property = Convert.ToDouble(send);
			if (Properties.MySendCt[j] <= b)
			    property *= 1.27;				// ShakeIt properties <= 100
			value = (byte)(0.5 * property);

			value &= 0x7F;
			if (Sent[j][cc] != value) {			// send only changed values
			    Sent[j][cc] = value;
			    if (1 == j)
				VJD.Axis(cc, (int) (63.5 + property * VJDmaxval) / 127);	// rescale from MIDI to vJoy
			    else if (2 == j)
				VJD.Button( ++cc, 0.5 < property);	// first VJoy button is 1, not 0
			    else if (Properties.CC == (Properties.CC & Properties.Which[b]))
				Outer.SendCCval(cc, value);		// DoSendCC()
			}
		    }
		    else Info($"DoSend(): 0 length {prop} map[{j}, {b}]");
		}
	    }
	}		// DoSend()
    }
}
