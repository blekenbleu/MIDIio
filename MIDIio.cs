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

        public INdrywet Reader;
        public OUTdrywet Outer;

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
                Outer.SendProp(b);
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

            // Declare a property available in the property list; this gets evaluated "on demand" (when shown or used in formulas)
            object data = pluginManager.GetPropertyValue("DataCorePlugin.ExternalScript.MIDIin");
            String input = (null == data) ? "unassigned" : data.ToString();
            pluginManager.AddProperty("in", this.GetType(), input);
            SimHub.Logging.Current.Info("MIDIio input device: " + input);

            data = pluginManager.GetPropertyValue("DataCorePlugin.ExternalScript.MIDIin");
            SimHub.Logging.Current.Info("MIDIio output device: " + input);
            Reader = new INdrywet();
            Reader.Init(input, Settings, this);

            data = pluginManager.GetPropertyValue("DataCorePlugin.ExternalScript.MIDIout");
            String output = (null == data) ? "unassigned" : data.ToString();
            pluginManager.AddProperty("out", this.GetType(), (null == data) ? "unassigned" : output);
            Outer = new OUTdrywet();
            data = pluginManager.GetPropertyValue("DataCorePlugin.ExternalScript.MIDIsend");
            if (null == data)
            {
                SimHub.Logging.Current.Info("MIDIio unassigned output data source property");
                Outer.SendName = null;
                Outer.Init(output, Settings, this);
            }
            else
            {
                Outer.SendName = data.ToString();
                SimHub.Logging.Current.Info("MIDIio output device: " + output + " sending CC0-7 from: " + Outer.SendName + "0-7");
                Outer.Init(output, Settings, this);
            }

            count += 1;		// increments for each Init(), provoked e.g. by game change or restart
            pluginManager.AddProperty("Init() count", this.GetType(), count);

//          data = pluginManager.GetPropertyValue("DataCorePlugin.CustomExpression.MIDIsliders");
//          pluginManager.AddProperty("sliders", this.GetType(), (null == data) ? "unassigned" : data.ToString());

            // Declare an event
            this.AddEvent("MIDIioWarning");

            // Declare actions which can be called
            this.AddAction("ping0",(a, b) =>
            {
                SimHub.Logging.Current.Info("MIDIout0 pinged");
                Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)0);
            });

            this.AddAction("ping1", (a, b) =>
            {
                SimHub.Logging.Current.Info("MIDIout1 pinged");
                Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)1);
            });

            this.AddAction("ping2", (a, b) =>
            {
                SimHub.Logging.Current.Info("MIDIout2 pinged");
                Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)2);
            });

            this.AddAction("ping3", (a, b) =>
            {
                SimHub.Logging.Current.Info("MIDIout3 pinged");
                Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)3);
            });
        }
    }
}
