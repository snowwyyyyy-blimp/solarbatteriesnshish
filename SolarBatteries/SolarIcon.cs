using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

// BepInEx 5's plugin Logger is a ManualLogSource; it has no ILogger in this
// version, and UnityEngine's own ILogger would otherwise shadow the name anyway.
using BepInLog = BepInEx.Logging.ManualLogSource;

namespace SolarBatteries
{
    /// <summary>
    /// Loads item icons from PNG files that sit next to the plugin DLL and wraps
    /// them in a Sprite for CbBattery/CbPowerCell.CustomIcon.
    ///
    /// CustomBatteries hands the sprite to the item's sprite atlas while Patch()
    /// runs and keeps no managed reference to it afterwards, so every sprite made
    /// here is parked in a static list. Without that hold, the Texture2D behind
    /// the icon stays collectable and the PDA ends up showing a blank square.
    /// </summary>
    internal static class SolarIcon
    {
        private static readonly List<Sprite> Retained = new List<Sprite>();

        /// <summary>
        /// Reads a PNG from the plugin folder. Any failure returns null so the
        /// item is still patched and simply keeps the stock battery icon, which
        /// is a far better outcome than the mod failing to load entirely.
        /// </summary>
        public static Sprite Load(string fileName, BepInLog logger)
        {
            try
            {
                var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(folder))
                {
                    logger.LogWarning($"Solar Batteries: could not locate the plugin folder for {fileName}.");
                    return null;
                }

                var path = Path.Combine(folder, fileName);
                if (!File.Exists(path))
                {
                    logger.LogWarning($"Solar Batteries: icon missing at {path}; the item keeps its default icon.");
                    return null;
                }

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(File.ReadAllBytes(path)))
                {
                    UnityEngine.Object.Destroy(texture);
                    logger.LogWarning($"Solar Batteries: {fileName} could not be decoded as an image.");
                    return null;
                }

                // Pixels-per-unit 100 matches what the wider Subnautica modding
                // scene produces for runtime icons, so the PDA scales it the same
                // way it scales the stock battery icon.
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);

                Retained.Add(sprite);
                return sprite;
            }
            catch (Exception e)
            {
                logger.LogError($"Solar Batteries: failed to load {fileName}: {e}");
                return null;
            }
        }
    }
}
