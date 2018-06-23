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
            Speed = new RangeNode<int>(1, 0, 100);
            BlockInput = true;

            doors = true;
            switches = true;
            chests = true;
            shrines = true;

            doorDistance = new RangeNode<int>(150, 0, 300);
            switchDistance = new RangeNode<int>(150, 0, 300);
            chestDistance = new RangeNode<int>(150, 0, 300);
            toggleEntityKey = new HotkeyNode(Keys.V);
            shrineDistance = new RangeNode<int>(150, 0, 300);
        }

        [Menu("Blacklist|Whitelist Key")]
        public HotkeyNode toggleEntityKey { get; set; }

        [Menu("Block User Input")]
        public ToggleNode BlockInput { get; set; }

        [Menu("Click Delay")]
        public RangeNode<int> Speed { get; set; }

        [Menu("Doors", 1000)]
        public ToggleNode doors { get; set; }

        [Menu("Distance", 1001, 1000)]
        public RangeNode<int> doorDistance { get; set; }

        [Menu("Switches/Levers", 2000)]
        public ToggleNode switches { get; set; }

        [Menu("Distance", 2001, 2000)]
        public RangeNode<int> switchDistance { get; set; }

        [Menu("Chests", 3000)]
        public ToggleNode chests { get; set; }

        [Menu("Distance", 3002, 3000)]
        public RangeNode<int> chestDistance { get; set; }

        [Menu("Shrines", 4000)]
        public ToggleNode shrines { get; set; }

        [Menu("Distance", 4001, 4000)]
        public RangeNode<int> shrineDistance { get; set; }
    }
}