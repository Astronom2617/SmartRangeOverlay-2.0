using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace SmartRangeOverlay
{
    public class ModEntry : Mod
    {
        private ModConfig Config = null!;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null) return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            configMenu.AddBoolOption(ModManifest, () => Config.IsEnabled, val => Config.IsEnabled = val,
                name: () => Helper.Translation.Get("config.enabled.name"), tooltip: () => Helper.Translation.Get("config.enabled.desc"));

            configMenu.AddKeybindList(ModManifest, () => Config.ToggleHotkey, val => Config.ToggleHotkey = val,
                name: () => Helper.Translation.Get("config.hotkey.name"), tooltip: () => Helper.Translation.Get("config.hotkey.desc"));

            configMenu.AddBoolOption(ModManifest, () => Config.ShowSprinklers, val => Config.ShowSprinklers = val,
                name: () => Helper.Translation.Get("config.sprinklers.name"));

            configMenu.AddBoolOption(ModManifest, () => Config.ShowScarecrows, val => Config.ShowScarecrows = val,
                name: () => Helper.Translation.Get("config.scarecrows.name"));

            configMenu.AddBoolOption(ModManifest, () => Config.ShowBeeHouses, val => Config.ShowBeeHouses = val,
                name: () => Helper.Translation.Get("config.beehouses.name"));

            configMenu.AddBoolOption(ModManifest, () => Config.ShowLightningRods, val => Config.ShowLightningRods = val,
                name: () => Helper.Translation.Get("config.lightningrods.name"));

            configMenu.AddBoolOption(ModManifest, () => Config.ShowBombs, val => Config.ShowBombs = val,
                name: () => Helper.Translation.Get("config.bombs.name"));

            configMenu.AddBoolOption(ModManifest, () => Config.ShowActiveBombs, val => Config.ShowActiveBombs = val,
                name: () => Helper.Translation.Get("config.activebombs.name"));

            configMenu.AddBoolOption(ModManifest, () => Config.ShowTools, val => Config.ShowTools = val,
                name: () => Helper.Translation.Get("config.tools.name"));
        }

        private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            if (Config.ToggleHotkey.JustPressed())
            {
                Config.IsEnabled = !Config.IsEnabled;
                Helper.WriteConfig(Config);

                string msg = Config.IsEnabled ? Helper.Translation.Get("hud.enabled") : Helper.Translation.Get("hud.disabled");
                Game1.addHUDMessage(new HUDMessage(msg, 2));
            }
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!Config.IsEnabled || !Context.IsWorldReady || Game1.activeClickableMenu != null)
                return;

            HashSet<Vector2> normalTilesToDraw = new HashSet<Vector2>();
            HashSet<Vector2> bombTilesToDraw = new HashSet<Vector2>();
            HashSet<Vector2> toolTilesToDraw = new HashSet<Vector2>();

            Vector2 cursorTile = Game1.currentCursorTile;

            if (Game1.currentLocation.Objects.TryGetValue(cursorTile, out StardewValley.Object obj))
            {
                AddTilesToProperSet(obj.QualifiedItemId, cursorTile, normalTilesToDraw, bombTilesToDraw);
            }

            Item heldItem = Game1.player.CurrentItem;
            if (heldItem != null)
            {
                AddTilesToProperSet(heldItem.QualifiedItemId, cursorTile, normalTilesToDraw, bombTilesToDraw);

                if ((heldItem is WateringCan || heldItem is Hoe) && Config.ShowTools)
                {
                    int upgradeLevel = (heldItem as Tool)!.UpgradeLevel;
                    var toolCoverage = GetToolCoverage(upgradeLevel, cursorTile, Game1.player.FacingDirection);
                    toolTilesToDraw.UnionWith(toolCoverage);
                }
            }

            // Ticking bombs are not standard objects, they are spawned as TemporaryAnimatedSprites
            if (Config.ShowActiveBombs)
            {
                foreach (var sprite in Game1.currentLocation.temporarySprites)
                {
                    if (sprite.bombRadius > 0)
                    {
                        Vector2 tilePos = new Vector2((int)(sprite.position.X / Game1.tileSize), (int)(sprite.position.Y / Game1.tileSize));
                        var coverage = GetCircularCoverage(tilePos, sprite.bombRadius + 0.5f);
                        bombTilesToDraw.UnionWith(coverage);
                    }
                }
            }

            if (normalTilesToDraw.Count > 0) DrawGrid(normalTilesToDraw, e.SpriteBatch, Color.White * 0.45f);
            if (bombTilesToDraw.Count > 0) DrawGrid(bombTilesToDraw, e.SpriteBatch, Color.Red * 0.55f);
            if (toolTilesToDraw.Count > 0) DrawGrid(toolTilesToDraw, e.SpriteBatch, Color.DeepSkyBlue * 0.55f);
        }

        /// <summary>
        /// Routes the calculated coverage tiles to the appropriate HashSet based on item type for color-coding.
        /// </summary>
        private void AddTilesToProperSet(string itemId, Vector2 origin, HashSet<Vector2> normalTiles, HashSet<Vector2> bombTiles)
        {
            var coverage = GetCoverage(itemId, origin);
            if (coverage == null) return;

            if (itemId == "(O)286" || itemId == "(O)287" || itemId == "(O)288")
            {
                bombTiles.UnionWith(coverage);
            }
            else
            {
                normalTiles.UnionWith(coverage);
            }
        }

        /// <summary>
        /// Draws a hollow square grid for the specified tiles to improve visibility.
        /// </summary>
        private void DrawGrid(HashSet<Vector2> tiles, SpriteBatch spriteBatch, Color overlayColor)
        {
            Texture2D rectTexture = Game1.staminaRect;
            int thickness = 3;

            foreach (Vector2 tile in tiles)
            {
                Vector2 localPos = Game1.GlobalToLocal(Game1.viewport, tile * Game1.tileSize);
                int x = (int)localPos.X;
                int y = (int)localPos.Y;

                spriteBatch.Draw(rectTexture, new Rectangle(x, y, Game1.tileSize, thickness), overlayColor);
                spriteBatch.Draw(rectTexture, new Rectangle(x, y + Game1.tileSize - thickness, Game1.tileSize, thickness), overlayColor);
                spriteBatch.Draw(rectTexture, new Rectangle(x, y, thickness, Game1.tileSize), overlayColor);
                spriteBatch.Draw(rectTexture, new Rectangle(x + Game1.tileSize - thickness, y, thickness, Game1.tileSize), overlayColor);
            }
        }

        /// <summary>
        /// Calculates directional coverage for upgradeable tools (Watering Can, Hoe).
        /// </summary>
        private IEnumerable<Vector2> GetToolCoverage(int upgradeLevel, Vector2 origin, int facingDirection)
        {
            List<Vector2> tiles = new List<Vector2>();
            Vector2 forward = Vector2.Zero;
            Vector2 right = Vector2.Zero;

            switch (facingDirection)
            {
                case 0: forward = new Vector2(0, -1); right = new Vector2(1, 0); break; // Up
                case 1: forward = new Vector2(1, 0); right = new Vector2(0, 1); break;  // Right
                case 2: forward = new Vector2(0, 1); right = new Vector2(-1, 0); break; // Down
                case 3: forward = new Vector2(-1, 0); right = new Vector2(0, -1); break; // Left
            }

            if (upgradeLevel == 0) tiles.Add(origin);
            else if (upgradeLevel == 1) for (int i = 0; i < 3; i++) tiles.Add(origin + forward * i);
            else if (upgradeLevel == 2) for (int i = 0; i < 5; i++) tiles.Add(origin + forward * i);
            else if (upgradeLevel == 3)
                for (int f = 0; f < 3; f++)
                    for (int r = -1; r <= 1; r++)
                        tiles.Add(origin + forward * f + right * r);
            else if (upgradeLevel >= 4)
                for (int f = 0; f < 6; f++)
                    for (int r = -1; r <= 1; r++)
                        tiles.Add(origin + forward * f + right * r);

            return tiles;
        }

        /// <summary>
        /// Calculates coverage for static objects and held items based on SDV 1.6 string IDs.
        /// </summary>
        private IEnumerable<Vector2>? GetCoverage(string itemId, Vector2 origin)
        {
            List<Vector2> tiles = new List<Vector2>();

            switch (itemId)
            {
                case "(O)599":
                    if (!Config.ShowSprinklers) return null;
                    tiles.Add(origin + new Vector2(0, -1));
                    tiles.Add(origin + new Vector2(0, 1));
                    tiles.Add(origin + new Vector2(-1, 0));
                    tiles.Add(origin + new Vector2(1, 0));
                    break;

                case "(O)621":
                    if (!Config.ShowSprinklers) return null;
                    for (int x = -1; x <= 1; x++)
                        for (int y = -1; y <= 1; y++)
                            tiles.Add(origin + new Vector2(x, y));
                    break;

                case "(O)645":
                    if (!Config.ShowSprinklers) return null;
                    for (int x = -2; x <= 2; x++)
                        for (int y = -2; y <= 2; y++)
                            tiles.Add(origin + new Vector2(x, y));
                    break;

                case "(BC)8":
                    if (!Config.ShowScarecrows) return null;
                    return GetCircularCoverage(origin, 8.9f);

                case "(BC)167":
                    if (!Config.ShowScarecrows) return null;
                    return GetCircularCoverage(origin, 16.9f);

                case "(BC)10":
                    if (!Config.ShowBeeHouses) return null;
                    for (int x = -5; x <= 5; x++)
                        for (int y = -5; y <= 5; y++)
                            if (Math.Abs(x) + Math.Abs(y) <= 5)
                                tiles.Add(origin + new Vector2(x, y));
                    break;

                case "(BC)9":
                    if (!Config.ShowLightningRods) return null;
                    tiles.Add(origin);
                    break;

                case "(O)286":
                    if (!Config.ShowBombs) return null;
                    return GetCircularCoverage(origin, 3.5f);

                case "(O)287":
                    if (!Config.ShowBombs) return null;
                    return GetCircularCoverage(origin, 5.5f);

                case "(O)288":
                    if (!Config.ShowBombs) return null;
                    return GetCircularCoverage(origin, 7.5f);

                default:
                    return null;
            }

            return tiles;
        }

        /// <summary>
        /// Returns a standard circular radius coverage, simulating vanilla distance checks.
        /// </summary>
        private IEnumerable<Vector2> GetCircularCoverage(Vector2 origin, float radius)
        {
            List<Vector2> tiles = new List<Vector2>();
            int r = (int)Math.Ceiling(radius);

            for (int x = -r; x <= r; x++)
            {
                for (int y = -r; y <= r; y++)
                {
                    Vector2 offset = new Vector2(x, y);
                    if (Vector2.Distance(Vector2.Zero, offset) < radius)
                        tiles.Add(origin + offset);
                }
            }
            return tiles;
        }
    }
}