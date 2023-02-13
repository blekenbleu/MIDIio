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
	private byte size = 8;						// default configurable output count
	internal static byte[] Size;
	private bool[,] Once;
	private byte[][] Sent;
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
	    count += 1;		// increments for each restart, provoked e.g. by game change or restart
	    pluginManager.AddProperty(My + "Init().count", this.GetType(), count);

	    // Log() level configuration
	    prop = pluginManager.GetPropertyValue(Ini + "log")?.ToString();
	    Level = (byte)((null != prop && 0 < prop.Length) ? Int32.Parse(prop) : 0);
            Log(8, $"log Level {Level}");

	    int s = size;
	    prop = pluginManager.GetPropertyValue(Ini + "size")?.ToString();
	    if (null != prop && 0 < prop.Length && (0 >= (s = Int32.Parse(prop)) || 128 < s))
		Info($"Init(): invalid {Ini + "size"} {prop}; defaulting to {size}");
	    else size = (byte)s;

	    VJD = new VJsend();			// vJoy
	    VJDmaxval = VJD.Init(1);		// obtain joystick button and axis counts VJD.nButtons, VJD.nAxes

	    Size = new byte[] {size, (size < VJD.nAxes) ? size : VJD.nAxes, (size < VJD.nButtons) ? size : VJD.nButtons};
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

	    Properties = new IOproperties();    // MIDI and vJoy property configuration
	    Properties.Init(this);	// set SendCt[], sort My Send[,] first and unconfigured before Outer.Init()

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
	private void DoSend(PluginManager pluginManager, byte index)
	{
	    byte j, b, value;
            string send;
	    byte[,] table = {{ 2, 5}, {5, 6}, {0, SendCt[0]}, {SendCt[1], SendCt[2]}, {SendCt[2], SendCt[3]}, {SendCt[3], SendCt[4]}};
/*
 ; At least 3 possibilities:
 ; 1) 0 <= MIDI value < 128
 ; 2) 0 <= ShakeIt property <= 100.0
 ; 3) 0 <= JoyStick property <= VJDmaxval
 */
            double[,] scale =  {{ 1.0,               	127.0 / VJDmaxval, 1.27 },
				{ VJDmaxval / 127.0, 	1.0,               VJDmaxval / 100.0},
				{ 100.0 / 127,		100.0 / VJDmaxval, 1.0}};

	    for (int i = table[index, 0]; i < table[index, 1]; i++)
                for (byte k = table[i, 0]; k < table[i, 0]; k++)
	    	   for (j = 0; j < Properties.Send.GetLength(0); j++)
	    	   {
		
		    ushort cc = Properties.Map[j][k];	// MIDIout CC number or vJoy button or axis
                    b = (byte)(cc / 1000);
                    cc %= 10000;
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
			double property = Convert.ToDouble(send);
			value = (byte)(0.5 + property * scale[0, j]);
			value &= 0x7F;
			if (value == Sent[j][cc])
			    continue;				// send only changed values
                        Sent[j][cc] = value;
		    switch (b)
		    {
			case 0:
				Outer.SendCCval((byte)cc, value);		// DoSendCC()
			    break;
			case 1:
				VJD.Axis((byte)cc, (int) (0.5 * property * scale[1, j]));	// rescale from MIDI to vJoy
			    break;
			case 2:
				VJD.Button((byte)(1 + cc), 0.5 < property);	// first VJoy button is 1, not 0
			    break;
			default:
			    Info($"DoSend(): mystery type {b} ignored");
			    break;
			}
		    }
		    else Info($"DoSend(): 0 length {prop} map[{j}, {b}]");
		}
	}		// DoSend()
    }
}
