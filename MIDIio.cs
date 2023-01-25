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
        internal MIDIioSettings Settings;
        internal CCProperties CCProperties;

        internal INdrywet Reader;
        internal OUTdrywet Outer;

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
            for (byte b = 0; b < 8; b++)
                if (!Outer.SendProp(b))
                    break;
            if (data.GameRunning && data.OldData != null && data.NewData != null)
            {
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
            Outer.End();
            this.SaveCommonSettings("GeneralSettings", Reader.End());
        }

        /// <summary>
        /// Called at SimHub start then after game changes
        /// </summary>
        /// <param name="pluginManager"></param>
        private static int count = 0;
        public void Init(PluginManager pluginManager)
        {
            // Load settings
            Settings = this.ReadCommonSettings<MIDIioSettings>("GeneralSettings", () => new MIDIioSettings());

            // Make properties available in the property list; these get evaluated "on demand" (when shown or used in formulas)
            CCProperties = new CCProperties();
            // Init Outer before Reader, which tries to send stored MIDI CC messages
            object data = pluginManager.GetPropertyValue("DataCorePlugin.ExternalScript.MIDIout");
            String output = (null == data) ? "unassigned" : data.ToString();
            pluginManager.AddProperty("out", this.GetType(), (null == data) ? "unassigned" : output);
            SimHub.Logging.Current.Info("MIDIio output device: " + output);
            string MIDIsend = "DataCorePlugin.ExternalScript.MIDIsend";
            string SendName = null;
            data = pluginManager.GetPropertyValue(MIDIsend);
            if (null == data)
                SimHub.Logging.Current.Info("MIDIio unassigned output data source property: " + MIDIsend);
            else
                SimHub.Logging.Current.Info("MIDIio send property: " + (SendName = data.ToString()));
            Outer = new OUTdrywet();
            Outer.Init(output, SendName, Settings, this);

            data = pluginManager.GetPropertyValue("DataCorePlugin.ExternalScript.MIDIin");
            String input = (null == data) ? "unassigned" : data.ToString();
            pluginManager.AddProperty("in", this.GetType(), input);
            SimHub.Logging.Current.Info("MIDIio input device: " + input);
            Reader = new INdrywet();
            Reader.Init(input, Settings, this);

            count += 1;		// increments for each Init(), provoked e.g. by game change or restart
            pluginManager.AddProperty("Init() count", this.GetType(), count);

//          data = pluginManager.GetPropertyValue("DataCorePlugin.CustomExpression.MIDIsliders");
//          pluginManager.AddProperty("sliders", this.GetType(), (null == data) ? "unassigned" : data.ToString());
        }
    }
}
