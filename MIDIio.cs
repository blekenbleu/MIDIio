using GameReaderCommon;
using SimHub.Plugins;

namespace blekenbleu
{
	[PluginDescription("MIDI button, knob, slider; Joystick button and axis I/O routing")]
	[PluginAuthor("blekenbleu")]
	[PluginName("MIDIio")]
	public partial class MIDIio : IPlugin, IDataPlugin
	{
		private static byte Level;
		internal static readonly string My = "MIDIio.";								// SimHub Plugin Name + '.'
		internal MIDIioSettings Settings;
		internal static IOproperties Properties;
		internal VJsend VJD;
		internal INdrywet Reader;
		internal OUTdrywet Outer;
		bool loop = false;
		byte start = 1;

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
			start = (byte)((data.GameRunning && data.OldData != null && data.NewData != null) ? 0 : 1);

//			SendIf(pluginManager, start);	// Scan for non-game property changes anytime (echo)
			SendIf(pluginManager, 0);		// scan for *any* property changes anytime
			if (loop)
				VJD.Loop();									// for testing: loops thru configured axes and buttons
		}

		/// <summary>
		/// Called at plugin manager stop, close/dispose anything needed here !
		/// Plugins are rebuilt at game change
		/// </summary>
		/// <param name="pluginManager"></param>
		public void End(PluginManager pluginManager)
		{
			Reader?.End();
			Outer?.End();
			VJD?.End();
			Properties.End(this);
			this.SaveCommonSettings("GeneralSettings", Settings);
		}
	}				// class MIDIio
}
