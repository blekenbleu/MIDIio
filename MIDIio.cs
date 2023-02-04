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
        internal byte size = 8;			// Maximum ping, etc to configure
        private bool[] Once;

        internal string Ini = "DataCorePlugin.ExternalScript.MIDI";	// configuration source
        internal string my = "MIDIio.";					// PluginName + '.'
        internal MIDIioSettings Settings;
        internal CCProperties Properties;
        internal bool DoEcho = false;

        internal INdrywet Reader;
        internal OUTdrywet Outer;
        internal byte Level = 0;

        internal void Info(string str)
        {
            SimHub.Logging.Current.Info(str);
        }

        internal bool Log(byte level, string str)
        {
            bool b = 0 < (level & Level);

            if (b)
                SimHub.Logging.Current.Info(str);
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
            if (data.GameRunning && data.OldData != null && data.NewData != null)
                DoSendCC(pluginManager, Properties.MySendCt, Properties.SendCt);

            // Send my property messages anytime (echo)
            DoSendCC(pluginManager, 0, Properties.MySendCt);
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            Reader.End();
            Outer.End();
            Properties.End(this);
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        /// <summary>
        /// Called at SimHub start then after game changes
        /// </summary>
        /// <param name="pluginManager"></param>
        private static int count = 0;
        public void Init(PluginManager pluginManager)
        {
            Log(4, my + "Init()");
            // Load settings
            Settings = this.ReadCommonSettings<MIDIioSettings>("GeneralSettings", () => new MIDIioSettings());
            Once = new bool[size];

            // Make properties available
            // these get evaluated "on demand" (when shown or used in formulas)
            Properties = new CCProperties();
            Properties.Init(this);		// set SendCt before Outer

            string output = pluginManager.GetPropertyValue(Ini + "log")?.ToString();
            // Log() level configuration
            if (null != output && 0 < output.Length)
                Level = (byte)(0.5 + Convert.ToDouble(output));

            // Launch Outer before Reader, which tries to send stored MIDI CC messages
            output = pluginManager.GetPropertyValue(Ini + "out")?.ToString();
            if (null == output || 0 == output.Length) {
                Info(my + ".out: unassigned");
                pluginManager.AddProperty("out", this.GetType(), "unassigned");
            }
            else pluginManager.AddProperty("out", this.GetType(), output);

            DoEcho = 0 < Int32.Parse(pluginManager.GetPropertyValue(Ini + "echo")?.ToString());
            Info(my + "Init(): unconfigured CCs will" + (DoEcho ? "" : " not") + " be echo'ed"); 

            Outer = new OUTdrywet();
            Outer.Init(this, output, Properties.SendCt);

            string input = pluginManager.GetPropertyValue(Ini + "in")?.ToString();
            if (0 < input.Length)
            {
                pluginManager.AddProperty("in", this.GetType(), input);
                Reader = new INdrywet();
                Reader.Init(input, this);
            }
            else Info(my + "Init(): " + Ini + "in is invalid: '" + input +"'" );

            count += 1;		// increments for each restart, provoked e.g. by game change or restart
            pluginManager.AddProperty(my + "Init().count", this.GetType(), count);

            for (int i = 0; i < size; i++)
            {
                Settings.Sent[Properties.Map[i]] = 129;	// impossible first Send[i] values
                Once[i] = true;
            }

//          data = pluginManager.GetPropertyValue("DataCorePlugin.CustomExpression.MIDIsliders");
//          pluginManager.AddProperty("sliders", this.GetType(), (null == data) ? "unassigned" : data.ToString());
        }

        // track active CCs and save values
        internal bool Active(byte CCnumber, byte value)		// returns true if first time
        {
            byte which = Properties.Which[CCnumber];
            if (5 == CCnumber)
                Log(8, Properties.CCname[CCnumber]);		// just a debugging breakpoint
            if (0 < which)	// Configured?
            {
                Properties.CCvalue[CCnumber] = value;
                if (3 <= which && 0 < value)			// button or CC?
                {
                    Outer.Latest = value;			// drop pass to Ping()
                    if (4 == which || 0 < value)
                        this.TriggerEvent(Properties.CCname[CCnumber]);
                    if (DoEcho && 4 == which)
                        return !Outer.SendCCval(CCnumber, value); // 4 may also be appropriated configured
                }   
                return false;
            }

            if (DoEcho)
                return !Outer.SendCCval(CCnumber, value);	// Activate(): do not SetProp()

            // First time CC number seen
            Properties.Which[CCnumber] = 4;			// dynamic CC configuration
            this.AddEvent(Properties.CCname[CCnumber]);	// Users may assign CCn events to e.g. Ping()
            this.TriggerEvent(Properties.CCname[CCnumber]);
            return Properties.SetProp(this, CCnumber, value);
        }

        private void DoSendCC(PluginManager pluginManager, byte b, byte to)
        {
            for ( ; b < to; b++)
            {
                byte cc = Properties.Map[b];	// MIDIout CC numbers

                if (!Once[b])
                   return;

                string prop = Properties.Send[b];
                string send = pluginManager.GetPropertyValue(prop)?.ToString();

                if (null == send)
                {
                     Once[b] = false;
                     Info(my + "DataUpdate(): null " + prop);
                }
                else if (0 < send.Length)
                {
                    byte value = (byte)(0.5 + Convert.ToDouble(send));

                    value &= 0x7F;
                    if (Settings.Sent[cc] != value)			// send only changed values
                        Outer.SendCCval(cc, Settings.Sent[cc] = value);	// DoSendCC()
                }
            }
        }
    }
}
