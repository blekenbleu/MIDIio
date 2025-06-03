using SimHub.Plugins;
using System;
using System.Diagnostics;
using System.Reflection;

namespace blekenbleu
{
	public partial class MIDIio
	{
		internal static bool DoEcho;
		private  string MIDIin, MIDIout;
		private  static long VJDmaxval = 65535;
		private  bool[][] Once;
		private  ushort[][] Sent;													// remember and don't repeat
		private  double[,] scale;
		internal static readonly string Ini = "DataCorePlugin.ExternalScript.MIDI";	// configuration source
		internal static string CCsent = "watch this space", CCin = "watch this space", Ping = "watch this space";
		internal static string VJsent = "watch this space", oops = "watch this space", prop = "watch this space";

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

			// vJoy axes, vJoy buttons, CC sends
			prop = pluginManager.GetPropertyValue(Ini + "vJoy")?.ToString();
			if (null != prop && 1 == prop.Length && ("0" != prop))
			{
				VJD = new VJsend();				// vJoy
				VJDmaxval = VJD.Init(1);		// obtain vJoy parameters
			}

			double vMax100 = VJDmaxval/100.0, vMaxJa = VJDmaxval / 65535.0, vMaxCC = VJDmaxval / 127.0;
								   // game		Joystick axis	button		MIDIin
			scale = new double[,] {	{ vMax100,	vMaxJa,			VJDmaxval,	vMaxCC },	// vJoy axis
						 			{ vMax100,	vMaxJa,			VJDmaxval,	vMaxCC },	// vJoy button
									{ 1.27,		127.0/65535,	127.0,		1.0	   }};	// MIDIout

			// Launch Outer before Reader and Properties
			if (0 < MIDIout.Length)
			{
				Outer = new OUTdrywet();
				if (Outer.Init(MIDIout))
					pluginManager.AddProperty("out", this.GetType(), MIDIout);
			}

			// send unconfigured DoEchoes, set VJdest[,] SendCt[,], sort Send[, ]
			Properties = new IOproperties();						// MIDI and vJoy property configuration
			Properties.Init(this);

			this.AttachDelegate("oops", () => oops);
			if (3 < Level)
			{
				this.AttachDelegate("CCin",		() => CCin);
				this.AttachDelegate("CCsent",	() => CCsent);
				this.AttachDelegate("VJsent",	() => VJsent);
				this.AttachDelegate("Ping",		() => Ping);
				this.AttachDelegate("prop",		() => prop);
			}

			if (0 < MIDIin.Length)
			{
				pluginManager.AddProperty("in", this.GetType(), MIDIin);
				Reader = new INdrywet();
				if(Reader.Init(MIDIin, this))
					Properties.Attach(this);						// AttachDelegate source CCs
			}
			else Info("Init(): '" + Ini + "in' is undefined" );

			// set up Events and Actions
			prop = pluginManager.GetPropertyValue(Ini + "sends")?.ToString();
			if (null != prop && 1 < prop.Length)
				Properties.EnumActions(this, pluginManager, prop.Split(',')); 	// add MIDIsends to Properties.SourceList[]

			Once = new bool[1 + Properties.SourceList.Length][];	// null game properties for SourceList + CC
			Sent = new ushort[Once.Length][];						// duplicated send values
			for (int s = 0; s < Once.Length; s++)
			{
				if (s < Properties.SourceList.Length)
					Once[s] = new bool[Properties.SourceList[s].Count];
				else Once[s] = new bool[Properties.ListCC.Count];
				Sent[s] = new ushort[Once[s].Length];
				for (byte p = 0; p < Once[s].Length; p++)
				{
					Once[s][p] = true;
					Sent[s][p] = 222;
				}
			}
		}															// Init()
	}
}
