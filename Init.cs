using SimHub.Plugins;
using System;
using System.Diagnostics;
using System.Reflection;

namespace blekenbleu
{
	public partial class MIDIio
	{
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
			Log(4, $"log Level {Level}");

// Load settings

			Settings = this.ReadCommonSettings<MIDIioSettings>("GeneralSettings", () => new MIDIioSettings());

			prop = pluginManager.GetPropertyValue(Ini + "echo")?.ToString();
			DoEcho = (null != prop && 0 < Int32.Parse(prop));
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
			count += 1;		// increments for each restart, provoked e.g. by game change or restart
			pluginManager.AddProperty(My + "Init().count", this.GetType(), count);

			int s = size;
			prop = pluginManager.GetPropertyValue(Ini + "size")?.ToString();
			if (null == prop || 1 > prop.Length)
				Info($"Init(): missing {Ini + "size"}; defaulting to {size}");
			else if (0 >= (s = Int32.Parse(prop)) || 128 < s)
				Info($"Init(): invalid {Ini + "size"} {prop} = {s}; defaulting to {size}");
			else size = (byte)s;

			Size = new byte[] { size, size, size };	// vJoy axes, vJoy buttons, CC sends
			if (null == MIDIout || 0 == MIDIout.Length)
				Size[2] = 0;

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
			scale = new double[,] {	{ vMax/100.0,	vMax/65535.0,	vMax,	vMax/127.0 },	// vJoy axis
						 			{ vMax/100.0,	vMax/65535.0,	vMax,	vMax/127.0 },	// vJoy button
									{ 1.27,			127.0/65535.0,  127,	1.0		   }};	// MIDIout

			// Launch Outer before Reader and Properties
			if (0 < MIDIout.Length)
			{
				pluginManager.AddProperty("out", this.GetType(), MIDIout);
				Outer = new OUTdrywet();
				Outer.Init(MIDIout);	// may zero Size[2]
			}

			Properties = new IOproperties();		// MIDI and vJoy property configuration
			Dest = new byte[2 , size];				// for vJoy
			Properties.Init(this);					// send unconfigured DoEchoes, set Dest[,] SendCt[,], sort Send[, ]
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
