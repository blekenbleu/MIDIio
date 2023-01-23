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
        public MIDIioSettings Settings;

        public MIDIdrywet Device = new MIDIdrywet();

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
            // Define the value of our property (declared in init)
            if (data.GameRunning)
            {
                if (data.OldData != null && data.NewData != null)
                {
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
            // Save settings returned by Device.End()
            this.SaveCommonSettings("GeneralSettings", Device.End());
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        private static int count = 0;
        public void Init(PluginManager pluginManager)
        {
            // Load settings
            Settings = this.ReadCommonSettings<MIDIioSettings>("GeneralSettings", () => new MIDIioSettings());

            // Declare a property available in the property list; this gets evaluated "on demand" (when shown or used in formulas)

            object data = pluginManager.GetPropertyValue("DataCorePlugin.ExternalScript.MIDIin");
            String input = (null == data) ? "unassigned" : data.ToString();
            pluginManager.AddProperty("in", this.GetType(), input);

            SimHub.Logging.Current.Info("MIDIio input device: " + input);
            Device.Init(input, Settings);

            data = pluginManager.GetPropertyValue("DataCorePlugin.ExternalScript.MIDIout");
            pluginManager.AddProperty("out", this.GetType(), (null == data) ? "unassigned" : data.ToString());
            count += 1;		// increments for each Init(), provoked e.g. by game change or restart
            pluginManager.AddProperty("count", this.GetType(), count);

            this.AttachDelegate($"slider0", () => Device.Settings.Slider[0]);
            this.AttachDelegate($"knob0", () => Device.Settings.Knob[0]);
            this.AttachDelegate($"slider1", () => Device.Settings.Slider[1]);
            this.AttachDelegate($"knob1", () => Device.Settings.Knob[1]);
            this.AttachDelegate($"slider2", () => Device.Settings.Slider[2]);
            this.AttachDelegate($"knob2", () => Device.Settings.Knob[2]);
            this.AttachDelegate($"slider3", () => Device.Settings.Slider[3]);
            this.AttachDelegate($"knob3", () => Device.Settings.Knob[3]);
            this.AttachDelegate($"slider4", () => Device.Settings.Slider[4]);
            this.AttachDelegate($"knob4", () => Device.Settings.Knob[4]);
            this.AttachDelegate($"slider5", () => Device.Settings.Slider[5]);
            this.AttachDelegate($"knob5", () => Device.Settings.Knob[5]);
            this.AttachDelegate($"slider6", () => Device.Settings.Slider[6]);
            this.AttachDelegate($"knob6", () => Device.Settings.Knob[6]);
            this.AttachDelegate($"slider7", () => Device.Settings.Slider[7]);
            this.AttachDelegate($"knob7", () => Device.Settings.Knob[7]);

//          data = pluginManager.GetPropertyValue("DataCorePlugin.CustomExpression.MIDIsliders");
//          pluginManager.AddProperty("sliders", this.GetType(), (null == data) ? "unassigned" : data.ToString());

            // Declare an event
            this.AddEvent("MIDIioWarning");

            // Declare actions which can be called
            this.AddAction("ping0",(a, b) =>
            {
                SimHub.Logging.Current.Info("MIDIout0 pinged");
            });
            this.AddAction("ping1", (a, b) =>
            {
                SimHub.Logging.Current.Info("MIDIout1 pinged");
            });
            this.AddAction("ping2", (a, b) =>
            {
                SimHub.Logging.Current.Info("MIDIout2 pinged");
            });
        }
    }
}
