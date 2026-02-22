using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CircuitCraft.Editor
{
    /// <summary>
    /// Generates and imports main menu icon textures used by the UI.
    /// </summary>
    public static class MainMenuTextureGenerator
    {
        [Tooltip("Generated icon texture size in pixels.")]
        private const int k_IconSize = 128;

        [Tooltip("Output folder for generated main menu textures.")]
        private const string k_OutputPath = "Assets/60_UI/10_Sprites/10_MainMenu";

        /// <summary>
        /// Generates all main menu icon textures and configures them as sprites.
        /// </summary>
        [MenuItem("Tools/CircuitCraft/Generate MainMenu Textures")]
        public static void GenerateAll()
        {
            if (!Directory.Exists(k_OutputPath))
            {
                Directory.CreateDirectory(k_OutputPath);
            }

            GenerateIconPlay();
            GenerateIconSettings();
            GenerateIconQuit();

            AssetDatabase.Refresh();
            ConfigureAsSprite("IconPlay.png");
            ConfigureAsSprite("IconSettings.png");
            ConfigureAsSprite("IconQuit.png");
            AssetDatabase.SaveAssets();

            Debug.Log("[MainMenuTextureGenerator] Generated 3 icon textures.");
        }

        private static void GenerateIconPlay()
        {
            Vector2 a = new(-22f, 30f);
            Vector2 b = new(-22f, -30f);
            Vector2 c = new(30f, 0f);

            GenerateTexture("IconPlay.png", p =>
            {
                float sdf = SdfTriangle(p, a, b, c);
                return OutlineAlphaFromSignedDistance(sdf, 2f, 1.25f);
            });
        }

        private static void GenerateIconSettings()
        {
            const float holeRadius = 13f;
            const float baseOuterRadius = 33f;
            const float toothHeight = 5f;
            const int teeth = 8;

            GenerateTexture("IconSettings.png", p =>
            {
                float r = p.magnitude;
                float theta = Mathf.Atan2(p.y, p.x);
                float toothWave = Mathf.Max(0f, Mathf.Cos(theta * teeth));
                float outerRadius = baseOuterRadius + toothWave * toothHeight;

                float dOuter = r - outerRadius;
                float dHole = holeRadius - r;
                float filledSdf = Mathf.Max(dOuter, dHole);

                return OutlineAlphaFromSignedDistance(filledSdf, 1.7f, 1.2f);
            });
        }

        private static void GenerateIconQuit()
        {
            Vector2 a0 = new(-24f, -24f);
            Vector2 b0 = new(24f, 24f);
            Vector2 a1 = new(-24f, 24f);
            Vector2 b1 = new(24f, -24f);

            GenerateTexture("IconQuit.png", p =>
            {
                float d0 = SdfLineSegment(p, a0, b0);
                float d1 = SdfLineSegment(p, a1, b1);
                float d = Mathf.Min(d0, d1);
                return OutlineAlphaFromUnsignedDistance(d, 2f, 1.25f);
            });
        }

        private static void GenerateTexture(string fileName, Func<Vector2, float> alphaEvaluator)
        {
            var tex = new Texture2D(k_IconSize, k_IconSize, TextureFormat.RGBA32, false);
            var pixels = new Color32[k_IconSize * k_IconSize];
            Vector2 center = new((k_IconSize - 1) * 0.5f, (k_IconSize - 1) * 0.5f);

            for (int y = 0; y < k_IconSize; y++)
            {
                for (int x = 0; x < k_IconSize; x++)
                {
                    Vector2 p = new(x, y);
                    p -= center;
                    float alpha = Mathf.Clamp01(alphaEvaluator(p));
                    pixels[y * k_IconSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            File.WriteAllBytes(Path.Combine(k_OutputPath, fileName), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static float OutlineAlphaFromSignedDistance(float sdf, float halfLineWidth, float aaWidth)
        {
            float edge = Mathf.Abs(sdf) - halfLineWidth;
            return 1f - Smoothstep01(edge / aaWidth);
        }

        private static float OutlineAlphaFromUnsignedDistance(float distance, float halfLineWidth, float aaWidth)
        {
            float edge = distance - halfLineWidth;
            return 1f - Smoothstep01(edge / aaWidth);
        }

        private static float Smoothstep01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static float SdfLineSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        private static float SdfTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d0 = SdfLineSegment(p, a, b);
            float d1 = SdfLineSegment(p, b, c);
            float d2 = SdfLineSegment(p, c, a);
            float edgeDistance = Mathf.Min(d0, Mathf.Min(d1, d2));

            bool inside = IsPointInTriangle(p, a, b, c);
            return inside ? -edgeDistance : edgeDistance;
        }

        private static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s0 = CrossZ(b - a, p - a);
            float s1 = CrossZ(c - b, p - b);
            float s2 = CrossZ(a - c, p - c);

            bool hasNeg = (s0 < 0f) || (s1 < 0f) || (s2 < 0f);
            bool hasPos = (s0 > 0f) || (s1 > 0f) || (s2 > 0f);
            return !(hasNeg && hasPos);
        }

        private static float CrossZ(Vector2 a, Vector2 b)
        {
            return (a.x * b.y) - (a.y * b.x);
        }

        private static void ConfigureAsSprite(string fileName)
        {
            string assetPath = k_OutputPath + "/" + fileName;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
