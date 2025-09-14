using GameReaderCommon;
using SimHub.Plugins;
using System.Collections.Generic;
using System.Windows.Media;

namespace blekenbleu
{
	[PluginDescription("MIDI button, knob, slider; Joystick button and axis I/O routing")]
	[PluginAuthor("blekenbleu")]
	[PluginName("MIDIio")]
	public partial class MIDIio : IPlugin, IDataPlugin, IWPFSettingsV2
    {
		public List<Values> simValues = new List<Values>();             // must be initialized before Init()
		internal MIDIioSettings Settings;

		internal static readonly string My = "MIDIio.";								// SimHub Plugin Name + '.'
		internal static IOproperties MidiProps;
		internal VJsend VJD;
		internal INdrywet Reader;
		internal OUTdrywet Outer;

		private static byte Level;
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
		/// Plugin manager instance
		/// </summary>
		public PluginManager PluginManager { get; set; }

		/// <summary>
		/// Gets the left menu icon. Icon must be 24x24 and compatible with black and white display.
		/// </summary>
		public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);

		/// <summary>
		/// Short plugin title to show in left menu. Return null to use the PluginName attribute.
		/// </summary>
		public string LeftMenuTitle => "MIDIio " + Control.version;

		/// <summary>
		/// Called one time per game data update, contains all normalized game data,
		/// raw data are intentionnally "hidden" under a generic object type (plugins SHOULD NOT USE)
		/// This method is on the critical path, must execute as fast as possible and avoid throwing any error
		/// </summary>
		/// <param name="pluginManager"></param>
		/// <param name="data">Current game data, including current and previous data frames.</param>
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
			MidiProps.End(this);
			this.SaveCommonSettings("GeneralSettings", Settings);
		}

		/// <summary>
		/// Returns settings control or null if not required
		/// </summary>
		/// <param name="pluginManager"></param>
		/// <returns>UserControl instance</returns>
		private Control View;	// instance of Control.xaml.cs Control()
		public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
		{
			View = new Control(this);		// invoked *after* Init()
//			SetSlider();
			return View;
		}


		/// <summary>
		/// Called once after plugins startup
		/// Plugins are rebuilt at game change
		/// </summary>
		/// <param name="pluginManager"></param>
//		public void Init(PluginManager pluginManager)	// see Init.cs
	}				// class MIDIio
}
