using StardewModdingAPI.Utilities;

namespace SmartRangeOverlay
{
    public class ModConfig
    {
        public KeybindList ToggleHotkey { get; set; } = KeybindList.Parse("LeftShift + R, RightShift + R");
        public bool IsEnabled { get; set; } = true;

        public bool ShowSprinklers { get; set; } = true;
        public bool ShowScarecrows { get; set; } = true;
        public bool ShowBeeHouses { get; set; } = true;
        public bool ShowLightningRods { get; set; } = true;
        public bool ShowBombs { get; set; } = true; 
        public bool ShowActiveBombs { get; set; } = true; 
        public bool ShowTools { get; set; } = true;
    }
}