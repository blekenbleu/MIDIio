using SimHub.Plugins;
using System;
using System.Diagnostics;
using System.Reflection;

namespace blekenbleu
{
	public partial class MIDIio
	{
		internal static byte CCSize = 8;											// hard-coded CC send Action count
		internal static byte size = 8;												// default configurable array size
		internal static bool DoEcho;
		private  string prop, MIDIin, MIDIout;
		private  static long VJDmaxval = 65535;
		private  bool[][] Once;
		private  int[][] Sent;														// remember and don't repeat
		private  double[,] scale;
		internal static readonly string Ini = "DataCorePlugin.ExternalScript.MIDI";	// configuration source
		internal static string SentEvent = "watch this space", CCin = "watch this space", Ping = "watch this space";

		/// <summary>
		/// Called at SimHub start and restarts
		/// </summary>
		/// <param name="pluginManager"></param>
		public void Init(PluginManager pluginManager)
		{
			// Log() level configuration
			prop = pluginManager.GetPropertyValue(Ini + "log")?.ToString();
			Level = (byte)((null != prop && 0 < prop.Length) ? Int32.Parse(prop) : 0);
			Log(4, $"log Level {Level}");

// Load settings
			Settings = this.ReadCommonSettings("GeneralSettings", () => new MIDIioSettings());

			prop = pluginManager.GetPropertyValue(Ini + "echo")?.ToString();
			DoEcho = null != prop && 0 < int.Parse(prop);
			if (!DoEcho)
				for (byte i = 0; i < Settings.CCvalue.Length; i++)
					Settings.CCvalue[i] = 0;
			MIDIout = pluginManager.GetPropertyValue(Ini + "out")?.ToString();
			if (null == MIDIout || 0 == MIDIout.Length)
			{
				MIDIout = "";
				Info((null == prop) ? "Init(): missing " + Ini + "out entry!" : "Init(): " + Ini + "out is undefined");
			}
			MIDIin = pluginManager.GetPropertyValue(Ini + "in")?.ToString();
			if (null == MIDIin || 0 == MIDIin.Length)
			{
				Info("Init(): missing " + Ini + "in entry!");
				MIDIin = "";
			}
			string version = "version "
				+ FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion.ToString();
			if (0 < MIDIin.Length && 0 < MIDIout.Length)
				Info(version + (DoEcho ? ": " : ":  not") + " forwarding unconfigured " + MIDIin + " CCs to " + MIDIout);
			else Info(version);

			int s = size;
			prop = pluginManager.GetPropertyValue(Ini + "size")?.ToString();
			if (null == prop || 1 > prop.Length)
				Info($"Init(): missing {Ini + "size"}; defaulting to {size}");
			else if (0 >= (s = Int32.Parse(prop)) || 128 < s)
				Info($"Init(): invalid {Ini + "size"} {prop} = {s}; defaulting to {size}");
			else size = (byte)s;

			// vJoy axes, vJoy buttons, CC sends
			prop = pluginManager.GetPropertyValue(Ini + "vJoy")?.ToString();
			if (null != prop && 1 == prop.Length && ("0" != prop))
			{
				VJD = new VJsend();				// vJoy
				VJDmaxval = VJD.Init(1);		// obtain vJoy parameters
			}

			double vMax100 = VJDmaxval/100, vMaxJa = VJDmaxval / 65535, vMaxCC = VJDmaxval / 127;
								   // game		Joystick axis	button		MIDIin
			scale = new double[,] {	{ vMax100,	vMaxJa,			VJDmaxval,	vMaxCC },	// vJoy axis
						 			{ vMax100,	vMaxJa,			VJDmaxval,	vMaxCC },	// vJoy button
									{ 1.27,		127.0/65535,	127.0,		1.0	   }};	// MIDIout

			// Launch Outer before Reader and Properties
			if (null != MIDIout && 0 < MIDIout.Length)
			{
				Outer = new OUTdrywet();
				if (Outer.Init(MIDIout))
					pluginManager.AddProperty("out", this.GetType(), MIDIout);
				else CCSize = 0;
			}	else CCSize = 0;

			Properties = new IOproperties();		// MIDI and vJoy property configuration
			Properties.Init(this, CCSize);			// send unconfigured DoEchoes, set VJdest[,] SendCt[,], sort Send[, ]
			this.AttachDelegate("CCin", () => CCin);
			this.AttachDelegate("Ping", () => Ping);
			this.AttachDelegate("SentEvent", () => SentEvent);

			if (0 < MIDIin.Length)
			{
				pluginManager.AddProperty("in", this.GetType(), MIDIin);
				Reader = new INdrywet();
				if(Reader.Init(MIDIin, this))
					Properties.Attach(this);									// AttachDelegate buttons, sliders and knobs
			}
			else Info("Init(): " + Ini + "in is undefined" );

			Once = new bool[Properties.SourceCt.Length][];						// null game properties
			Sent = new int[Properties.SourceCt.Length][];
			for (s = 0; s < Properties.SourceCt.Length; s++)
			{
				Once[s] = new bool[Properties.SourceCt[s]];
				Sent[s] = new int[Properties.SourceCt[s]];
				for (byte p = 0; p < Properties.SourceCt[s]; p++)
				{
					Once[s][p] = true;
					Sent[s][p] = 222;
				}
			}
		}																		// Init()
		
	}
}
