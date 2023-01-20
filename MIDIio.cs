using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;

namespace blekenbleu.MIDIspace
{
    [PluginDescription("MIDI slider IO")]
    [PluginAuthor("blekenbleu")]
    [PluginName("MIDIio")]
    public class MIDIio : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        private double speed;
//      public MIDIioSettings Settings;

        /// <summary>
        /// Instance of the current plugin manager
        /// </summary>
        public PluginManager PluginManager { get; set; }

        /// <summary>
        /// Gets the left menu icon. Icon must be 24x24 and compatible with black and white display.
        /// </summary>
//      public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sliders);
        public ImageSource PictureIcon => null;

        /// <summary>
        /// Gets a short plugin title to show in left menu. Return null if you want to use the title as defined in PluginName attribute.
        /// </summary>
        public string LeftMenuTitle => "MIDIio";

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
            // Define the value of our property (declared in init)
            if (data.GameRunning)
            {
                if (data.OldData != null && data.NewData != null)
                {
                    if (data.OldData.SpeedKmh < speed && data.OldData.SpeedKmh >= speed)
                    {
                        // Trigger an event
                        this.TriggerEvent("MIDIioWarning");
                        speed = data.OldData.SpeedKmh;
                    }
                }
            }
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            // Save settings
//          this.SaveCommonSettings("GeneralSettings", Settings);
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
//          return new SettingsControl(this);
            return null;
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        private static int count = 0;
        public void Init(PluginManager pluginManager)
        {
            speed = 0;
            // Load settings
//          Settings = this.ReadCommonSettings<MIDIioSettings>("GeneralSettings", () => new MIDIioSettings());

            // Declare a property available in the property list; this gets evaluated "on demand" (when shown or used in formulas)
            object data = pluginManager.GetPropertyValue("DataCorePlugin.ExternalScript.MIDIin");
            String input = (null == data) ? "unassigned" : data.ToString();
            pluginManager.AddProperty("in", this.GetType(), input);

            SimHub.Logging.Current.Info("MIDIio plugin input: " + input);

            data = pluginManager.GetPropertyValue("DataCorePlugin.ExternalScript.MIDIout");
            pluginManager.AddProperty("out", this.GetType(), (null == data) ? "unassigned" : data.ToString());
            count += 1;		// increments for each Init(), provoked e.g. by game change or restart
            pluginManager.AddProperty("count", this.GetType(), count);
//          this.AttachDelegate("DateTime", () => DateTime.Now);
//          data = pluginManager.GetPropertyValue("DataCorePlugin.CustomExpression.MIDIsliders");
//          pluginManager.AddProperty("sliders", this.GetType(), (null == data) ? "unassigned" : data.ToString());

            // Declare an event
            this.AddEvent("MIDIioWarning");

            // Declare an action which can be called
//          this.AddAction("IncrementSpeedWarning",(a, b) =>
//          {
//              Settings.SpeedWarningLevel++;
//              SimHub.Logging.Current.Info("Speed warning changed");
//          });

            // Declare an action which can be called
 //         this.AddAction("DecrementSpeedWarning", (a, b) =>
 //         {
 //             Settings.SpeedWarningLevel--;
 //         });
        }
    }
}
