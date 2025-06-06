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
		internal static readonly string Ini = "DataCorePlugin.ExternalScript.MIDI"; // configuration source
        static readonly string wts = "watch this space";
		internal static string CCsent = wts, CCin = wts, Trigger = wts;
		internal static string VJsent = wts, Prop = wts,  Action = wts;
		internal static string oops = wts;	// AttachDelegate() always

		internal void Oops()
		{
			oops = "Oops():  " + Trigger;
		}

		/// <summary>
		/// Called at SimHub start and restarts
		/// </summary>
		/// <param name="pluginManager"></param>
		public void Init(PluginManager pluginManager)
		{
			// Log() level configuration
			Prop = pluginManager.GetPropertyValue(Ini + "log")?.ToString();
			Level = (byte)((null != Prop && 0 < Prop.Length) ? Int32.Parse(Prop) : 0);
			Log(4, $"log Level {Level}");

			// Load settings
			Settings = this.ReadCommonSettings("GeneralSettings", () => new MIDIioSettings());

			Prop = pluginManager.GetPropertyValue(Ini + "echo")?.ToString();
			DoEcho = null != Prop && 0 < int.Parse(Prop);
			MIDIout = pluginManager.GetPropertyValue(Ini + "out")?.ToString();
			if (null == MIDIout || 0 == MIDIout.Length)
			{
				MIDIout = "";
				Info((null == Prop) ? "Init(): missing " + Ini + "out entry!" : "Init(): " + Ini + "out is undefined");
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
			Prop = pluginManager.GetPropertyValue(Ini + "vJoy")?.ToString();
			if (null != Prop && 1 == Prop.Length && ("0" != Prop))
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
			Properties.Init(this, pluginManager);
			this.AddAction("Oops",(a, b) => Oops());

			this.AttachDelegate("oops", () => oops);
			if (3 < Level)
			{
				this.AttachDelegate("CCin",		() => CCin);
				this.AttachDelegate("CCsent",	() => CCsent);
				this.AttachDelegate("Prop",		() => Prop);
				this.AttachDelegate("Action",	() => Action);
				this.AttachDelegate("Trigger",	() => Trigger);
				this.AttachDelegate("VJsent",	() => VJsent);
			}

			if (0 < MIDIin.Length)
			{
				pluginManager.AddProperty("in", this.GetType(), MIDIin);
				Reader = new INdrywet();
				if(Reader.Init(MIDIin, this))
					Properties.Attach();						// AttachDelegate source CCs
			}
			else Info("Init(): '" + Ini + "in' is undefined" );

			Once = new bool[1 + Properties.SourceList.Length][];    // null game properties for SourceList + CC
			int s;
			for (s = 0; s < Properties.SourceList.Length; s++)
				Once[s] = new bool[Properties.SourceList[s].Count];
			Once[s] = new bool[Properties.ListCC.Count];
		
			Sent = new ushort[Once.Length][];                       // most recent Send() values
			for (s = 0; s < Properties.SourceList.Length; s++)
			{
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
