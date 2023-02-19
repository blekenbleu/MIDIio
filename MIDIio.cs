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
	internal static byte size = 8;						// default configurable output count
	internal static byte[] Size;
	private bool[,,] Once;
	private byte[,] Sent;
	private byte[] DoSendCt;
	internal static readonly string Ini = "DataCorePlugin.ExternalScript.MIDI";	// configuration source
	internal static readonly string My = "MIDIio.";					// PluginName + '.'
        internal static string[] Real { get; } = { My, "JoystickPlugin.", "InputStatus." };
	internal MIDIioSettings Settings;
	internal static IOproperties Properties;
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
		DoSend(pluginManager, 1);

	    // Send my property messages anytime (echo)
	    DoSend(pluginManager, 0);
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

//	    Log(4, "Init()");
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
		VJD = new VJsend();			// vJoy
		VJDmaxval = VJD.Init(1);		// obtain joystick button and axis counts VJD.nButtons, VJD.nAxes
	    }
	    else VJDmaxval = 0;
	    if (0 == VJDmaxval)
		Size[1] = Size[2] = 0;
	    else
	    {
		if (size > VJD.nAxes)
		    Size[1] = VJD.nAxes;
		if (size > VJD.nButtons)
		    Size[2] = VJD.nButtons;
            }

	    // Launch Outer before Reader and Properties
	    prop = pluginManager.GetPropertyValue(Ini + "out")?.ToString();
	    if (null == prop || 0 == prop.Length)
	    {
		Info((null == prop) ? "Init(): missing " + Ini + "out entry!" : "Init(): " + Ini + "out is undefined");
		Size[0] = 0;
	    }
	    else
	    {
	    	pluginManager.AddProperty("out", this.GetType(), prop);
		Outer = new OUTdrywet();
		Outer.Init(prop);	// may zero Size[0]
	    }

	    Properties = new IOproperties();	// MIDI and vJoy property configuration
	    Properties.Init(this);		// send unconfigured DoEchoes, set SendCt[,], sort Send[, ]

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

	    Once = new bool[Properties.SendType.Length, 4, size];

	    for (byte j = 0; j < Properties.CCtype.Length - 1; j++)
		for (int i = 0; i < Properties.SendCt[j,0]; i++)
		    Settings.Sent[Properties.Map[j, i]] = 129;	// impossible first CC Send[i] values

	    for (s = 0; s < Properties.SendType.Length; s++)
		for (byte j = 0; j < 4; j++)			// send source count
		    for (int i = 0; i < size; i++)
			Once[s, j, i] = true;

	    DoSendCt = new byte[3];
	    Sent = new byte[Properties.SendType.Length, size];
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
//	    this.AddEvent(Properties.CCname[CCnumber]);			// Users may assign CCn events to e.g. Ping()
//	    this.TriggerEvent(Properties.CCname[CCnumber]);
	    Settings.Sent[CCnumber] = value;
	    return Properties.SetProp(this, CCnumber);
	}	// Active()

/*
 ; Properties.Send[,] is an array of property names for other than MIDIin
 ; My + CCname[cc = Properties.Map[,]] are MIDIin properties corresponding to Settings.Sent[cc]
 ;
 ; Accomodate device value range differences:
 ; 0 <= MIDI value <= 127
 ; 0 <= JoyStick property <= VJDmaxval
 ; 0 <= ShakeIt property <= 100.0
 */
	private static readonly double vMax = (double)VJDmaxval;
	private readonly double[,] scale ={{ 1.0,	127.0/vMax, 1.27	},
					   { vMax/127,	1.0,	    vMax/100.0	},
					   { 100.0/127,	100.0/vMax, 1.0		}};

	private void DoSend(PluginManager pluginManager, byte index)
	{
	    for (byte s = 0; s < Properties.SendType.Length; s++)			// destination index
	    {
		byte[,] table = {{0, 3}, {3, 4}};			// source indices [0-2]: real; 3: game

	    	for (byte i = table[index, 0]; i < table[index, 1]; i++)		// source index
	    	for (byte k = 0; k < Properties.SendCt[(byte)s, i]; k++)
		{
		    byte cc;

		    if (!Once[s, i, k])
		       continue;							// skip null properties

		    if (0 == i)
			prop = My + Properties.CCname[cc = Properties.Map[s, k]];	// MIDIout CC number
		    else prop = Properties.Send[s][i][cc = k];

		    string send = pluginManager.GetPropertyValue(prop)?.ToString();

		    if (null == send)
		    {
		        Once[s, i, k] = false;
                        Info($"DoSend({Properties.SendType[s]}): null {prop} for "
			   + ((0 == i) ? $"CCname[{cc}]" : $"Send[{s}][{i}][{k}]"));
                    }
                    else if (0 < send.Length)
                    {  
			double property = Convert.ToDouble(send);
			byte value = (byte)(0.5 + property * scale[0, i]);
			value &= 0x7F;

			if (k >= DoSendCt[s] || value == Sent[s, k])
			    continue;							// send only changed values

                	Sent[s, k] = value;
			switch (s)
			{
			    case 0:
				Outer.SendCCval((byte)cc, Settings.Sent[cc] = value);
				break;
			    case 1:							// rescale to vJoy
				VJD.Axis(cc, (int)(0.5 * property * scale[1, i]));
				break;
			    case 2:
				VJD.Button((byte)(1 + ), 0.5 < property);		// first VJoy button is 1, not 0
				break;
			    default:
				Info($"DoSend(): mystery type {s} ignored: Send[{s}][{i}][{k}] = {prop}");
				Once[s, i, k] = false;
				break;
			}
		    }
		    else Info($"DoSend(): 0 length {prop}"
			    + ((0 == i) ? $" from Map[{s}, {k}]" : ""));
		}									// 0 < send.Length
	    }
	}		// DoSend()
    }
}
