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

            doors = true;
            switches = true;
            chests = true;
            shrines = true;

            doorDistance = new RangeNode<int>(150, 0, 300);
            switchDistance = new RangeNode<int>(150, 0, 300);
            chestDistance = new RangeNode<int>(150, 0, 300);
            shrineDistance = new RangeNode<int>(150, 0, 300);

            chestWhitelistKey = new HotkeyNode(Keys.V);
        }

        [Menu("Speed")]
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

        [Menu("Whitelist Key", 3001, 3000)]
        public HotkeyNode chestWhitelistKey { get; set; }

        [Menu("Distance", 3002, 3000)]
        public RangeNode<int> chestDistance { get; set; }

        [Menu("Shrines", 4000)]
        public ToggleNode shrines { get; set; }

        [Menu("Distance", 4001, 4000)]
        public RangeNode<int> shrineDistance { get; set; }

    }
}