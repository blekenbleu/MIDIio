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
	internal byte[] Size;
	private bool[,] Once;
	private byte[][] Sent;
	internal string Ini = "DataCorePlugin.ExternalScript.MIDI";	// configuration source
	internal static string My = "MIDIio.";					// PluginName + '.'
        internal string[] Real { get; } = { My, "JoystickPlugin." };
	internal MIDIioSettings Settings;
	internal CCProperties Properties;
	internal VJsend VJD;
	internal INdrywet Reader;
	internal OUTdrywet Outer;
	private byte Level = 0;
	internal bool DoEcho = false;

	internal bool Info(string str)
	{
	    SimHub.Logging.Current.Info(MIDIio.My + str);	// bool Info()
	    return true;
	}

	internal bool Log(byte level, string str)
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
//	    VJD.Run();
	}

	/// <summary>
	/// Called at plugin manager stop, close/dispose anything needed here !
	/// Plugins are rebuilt at game change
	/// </summary>
	/// <param name="pluginManager"></param>
	public void End(PluginManager pluginManager)
	{
	    Reader.End();
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
	public void Init(PluginManager pluginManager)
	{
	    Log(4, "Init()");
	    // Load settings
	    Settings = this.ReadCommonSettings<MIDIioSettings>("GeneralSettings", () => new MIDIioSettings());

	    VJD = new VJsend();			// vJoy
	    VJD.Init(this, 1);			// obtain joystick button and axis counts VJD.nButtons, VJD.nAxes

	    int s = size;
	    string input = pluginManager.GetPropertyValue(Ini + "size")?.ToString();
	    if (null != input && 0 < input.Length && (0 >= (s = Int32.Parse(input)) || 128 < s))
		Info($"Init(): invalid {Ini + "size"} {input}; defaulting to {size}");
	    else size = (byte)s;
	    pluginManager.AddProperty("size", this.GetType(), size);

	    Size = new byte[] {size, (size < VJD.nAxes) ? size : VJD.nAxes, (size < VJD.nButtons) ? size : VJD.nButtons};
	    Properties = new CCProperties();    // MIDI and vJoy property configuration

	    string output = pluginManager.GetPropertyValue(Ini + "log")?.ToString();
	    // Log() level configuration
	    if (null != output && 0 < output.Length)
		Level = (byte)(0.5 + Convert.ToDouble(output));
            Log(8, $"log Level {Level}");
	    Properties.Init(this, Size);	// set SendCt[], sort My Send[,] first and unconfigured before Outer.Init()

	    // Launch Outer before Reader, which tries to send stored MIDI CC messages
	    output = pluginManager.GetPropertyValue(Ini + "out")?.ToString();
	    if (null == output || 0 == output.Length) {
		output = "unassigned";
		Info("out: " + output);
	    }
	    pluginManager.AddProperty("out", this.GetType(), output);

	    DoEcho = 0 < Int32.Parse(pluginManager.GetPropertyValue(Ini + "echo")?.ToString());
	    Info("Init(): unconfigured CCs will" + (DoEcho ? "" : " not") + " be echo'ed");

	    Outer = new OUTdrywet();
	    Outer.Init(this, output, Properties.SendCt[0]);

	    input = pluginManager.GetPropertyValue(Ini + "in")?.ToString();
	    if (null == input)
		Info("Init(): missing " + Ini + "in entry!");
	    else if (0 < input.Length)
	    {
		pluginManager.AddProperty("in", this.GetType(), input);
		Reader = new INdrywet();
		if(Reader.Init(input, this))
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
            string prop, send;
            double property;

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
			property = Convert.ToDouble(send);
			if (Properties.MySendCt[j] <= b)
			    property *= 1.27;				// ShakeIt properties <= 100
			value = (byte)(0.5 * property);

			value &= 0x7F;
			if (Sent[j][cc] != value) {			// send only changed values
			    Sent[j][cc] = value;
			    if (1 == j)
				VJD.Axis(cc, property);
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
