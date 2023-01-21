using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;

namespace blekenbleu.MIDIspace
{
    [PluginDescription("MIDI slider IO")]
    [PluginAuthor("blekenbleu")]
    [PluginName("MIDIio")]
    public class MIDIio : IPlugin, IDataPlugin
    {
        private double speed;

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
                    if (data.OldData.SpeedKmh > speed)
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
            // Declare a property available in the property list; this gets evaluated "on demand" (when shown or used in formulas)
            object data = pluginManager.GetPropertyValue("DataCorePlugin.ExternalScript.MIDIin");
            String input = (null == data) ? "unassigned" : data.ToString();
            pluginManager.AddProperty("in", this.GetType(), input);

            SimHub.Logging.Current.Info("MIDIio plugin input: " + input);

            data = pluginManager.GetPropertyValue("DataCorePlugin.ExternalScript.MIDIout");
            pluginManager.AddProperty("out", this.GetType(), (null == data) ? "unassigned" : data.ToString());
            count += 1;		// increments for each Init(), provoked e.g. by game change or restart
            pluginManager.AddProperty("count", this.GetType(), count);
            this.AttachDelegate("speed", () => speed);
//          data = pluginManager.GetPropertyValue("DataCorePlugin.CustomExpression.MIDIsliders");
//          pluginManager.AddProperty("sliders", this.GetType(), (null == data) ? "unassigned" : data.ToString());

            // Declare an event
            this.AddEvent("MIDIioWarning");

            // Declare an action which can be called
            this.AddAction("IncrementSpeed",(a, b) =>
            {
                speed+=10;
                SimHub.Logging.Current.Info("Speed warning incremented");
            });

            // Declare an action which can be called
            this.AddAction("DecrementSpeed", (a, b) =>
            {
                speed-=10;
                SimHub.Logging.Current.Info("Speed warning decremented");
            });
            this.AddAction("ReZeroSpeed", (a, b) =>
            {
                speed = 0;
                SimHub.Logging.Current.Info("Speed warning = 0");
            });
        }
    }
}
