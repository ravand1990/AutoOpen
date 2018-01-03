using System.Reflection;
using System.Windows.Forms;
using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;

namespace AutoOpen
{
    internal class Settings : SettingsBase
    {
        public Settings()
        {
            Enable = true;
            Speed = new RangeNode<int>(5, 0, 100);

            doors = true;
            switches = true;
            doorDistance = new RangeNode<int>(100,0,300);
            switchDistance = new RangeNode<int>(100, 0, 300);
        }

        [Menu("Speed")]
        public RangeNode<int> Speed { get; set; }

        [Menu("Doors",1000)]
        public ToggleNode doors { get; set; }

        [Menu("Distance",1001,1000)]
        public RangeNode<int> doorDistance { get; set; }

        [Menu("Switches/Levers", 2000)]
        public ToggleNode switches { get; set; }

        [Menu("Distance", 2001, 2000)]
        public RangeNode<int> switchDistance { get; set; }


    }
}