using System.Windows.Controls;

namespace blekenbleu.MIDIspace
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public MIDIio Plugin { get; }

        public SettingsControl()
        {
            InitializeComponent();
        }

        public SettingsControl(MIDIio plugin) : this()
        {
            this.Plugin = plugin;
        }
    }
}
