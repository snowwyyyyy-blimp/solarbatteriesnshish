using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolarBatteries
{
    /// <summary>
    /// Makes the vanilla battery / power cell prefab read as yellow.
    ///
    /// Simply setting _Color is not enough: the stock models get their colour from
    /// _MainTex, and those textures are dark blue-grey, so a colour multiply comes
    /// out gold/olive rather than yellow. Instead the albedo is remapped to a yellow
    /// hue while keeping its original luminance, so panel lines, screws and bevels
    /// all survive and the item looks painted yellow rather than flat.
    ///
    /// The source texture object is never modified - a new Texture2D is derived and
    /// assigned to this renderer's own material copy. That matters because Subnautica
    /// materials often share an atlas; leaving the original alone is what stops this
    /// from recolouring unrelated props.
    ///
    /// Results are cached per source texture, so the GPU readback happens once per
    /// texture rather than once per battery spawned.
    /// </summary>
    internal static class SolarTint
    {
        private static readonly Color Yellow = new Color(1f, 0.82f, 0.06f, 1f);

        /// <summary>Upper bound on derived texture size, to bound one-off GPU cost.</summary>
        private const int MaxPixels = 4 * 1024 * 1024;

        private static readonly Dictionary<Texture, Texture2D> Cache = new Dictionary<Texture, Texture2D>();

        public static void Apply(GameObject obj)
        {
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true))
            {
                // .material, not .sharedMaterial: gives this renderer its own copy so
                // the shared vanilla battery material is never mutated.
                var material = renderer.material;
                if (material != null)
                {
                    TintMaterial(material);
                }
            }
        }

        private static void TintMaterial(Material material)
        {
            // Emission tint is safe on every material and makes the item read yellow
            // in the dark as well, which suits a solar device.
            material.SetColor("_EmissionColor", Yellow * 0.35f);

            var source = material.GetTexture("_MainTex") as Texture;

            // Anything unexpected (no albedo, non-readable, oversized, or an error
            // during the readback) falls back to a plain colour multiply rather than
            // risking a broken or missing item.
            if (source == null)
            {
                material.SetColor("_Color", Yellow);
                return;
            }

            try
            {
                var tinted = GetYellowed(source);
                if (tinted != null)
                {
                    material.SetTexture("_MainTex", tinted);
                    material.SetColor("_Color", Color.white);
                    return;
                }
            }
            catch (Exception)
            {
                // Intentionally swallowed - see above.
            }

            material.SetColor("_Color", Yellow);
        }

        private static Texture2D GetYellowed(Texture source)
        {
            if (Cache.TryGetValue(source, out var cached))
            {
                return cached;
            }

            var derived = Build(source);
            Cache[source] = derived;
            return derived;
        }

        private static Texture2D Build(Texture source)
        {
            var width = source.width;
            var height = source.height;
            if (width <= 0 || height <= 0 || width * height > MaxPixels)
            {
                return null;
            }

            // Blitting to a temporary RT and reading it back works even when the
            // source texture is not CPU-readable, which imported game textures are not.
            var pixels = ReadPixels(source, width, height);
            if (pixels == null)
            {
                return null;
            }

            for (var i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];

                // Rec.709 luminance, so shading detail is preserved rather than
                // flattened. Alpha is left alone to keep any cutouts intact.
                var luminance = (c.r * 0.2126f + c.g * 0.7152f + c.b * 0.0722f) / 255f;

                pixels[i] = new Color32(
                    (byte)Mathf.Clamp(luminance * 255f * 1.00f, 0f, 255f),
                    (byte)Mathf.Clamp(luminance * 255f * 0.82f, 0f, 255f),
                    (byte)Mathf.Clamp(luminance * 255f * 0.06f, 0f, 255f),
                    c.a);
            }

            var result = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = source.name + " (SolarYellow)",
                wrapMode = source.wrapMode,
                filterMode = source.filterMode,
            };

            result.SetPixels32(pixels);
            result.Apply(false, false);
            return result;
        }

        private static Color32[] ReadPixels(Texture source, int width, int height)
        {
            var previous = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);

            try
            {
                RenderTexture.active = rt;
                GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));
                Graphics.Blit(source, rt);

                var readable = new Texture2D(width, height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                readable.Apply(false, false);
                return readable.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
