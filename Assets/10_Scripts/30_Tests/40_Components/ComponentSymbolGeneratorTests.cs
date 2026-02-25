using System.Collections.Generic;
using NUnit.Framework;
using CircuitCraft.Components;
using CircuitCraft.Data;
using UnityEngine;

namespace CircuitCraft.Tests.Components
{
    [TestFixture]
    public class ComponentSymbolGeneratorTests
    {
        // Track all created Unity objects for cleanup
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            _createdObjects.Clear();
        }

        // ------------------------------------------------------------------
        // Helper
        // ------------------------------------------------------------------

        private static bool HasNonTransparentPixels(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            foreach (var pixel in pixels)
            {
                if (pixel.a > 0)
                {
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------------
        // GetOrCreateFallbackSprite — non-null checks per ComponentKind
        // ------------------------------------------------------------------

        [TestCase(ComponentKind.Resistor)]
        [TestCase(ComponentKind.Capacitor)]
        [TestCase(ComponentKind.Inductor)]
        [TestCase(ComponentKind.Diode)]
        [TestCase(ComponentKind.LED)]
        [TestCase(ComponentKind.BJT)]
        [TestCase(ComponentKind.MOSFET)]
        [TestCase(ComponentKind.VoltageSource)]
        [TestCase(ComponentKind.CurrentSource)]
        [TestCase(ComponentKind.Ground)]
        [TestCase(ComponentKind.Probe)]
        [TestCase(ComponentKind.ZenerDiode)]
        public void GetOrCreateFallbackSprite_AllKinds_ReturnsNonNull(ComponentKind kind)
        {
            var sprite = ComponentSymbolGenerator.GetOrCreateFallbackSprite(kind);

            Assert.IsNotNull(sprite, $"Expected non-null Sprite for {kind}");

            // Track the sprite's texture for cleanup (sprite itself is managed by static cache)
            if (sprite.texture != null)
            {
                _createdObjects.Add(sprite.texture);
            }
        }

        // ------------------------------------------------------------------
        // GetOrCreateFallbackSprite — sprite name format
        // ------------------------------------------------------------------

        [TestCase(ComponentKind.Resistor, "Resistor_FallbackSprite")]
        [TestCase(ComponentKind.Capacitor, "Capacitor_FallbackSprite")]
        [TestCase(ComponentKind.LED, "LED_FallbackSprite")]
        [TestCase(ComponentKind.VoltageSource, "VoltageSource_FallbackSprite")]
        [TestCase(ComponentKind.Ground, "Ground_FallbackSprite")]
        public void GetOrCreateFallbackSprite_ReturnsCorrectSpriteName(ComponentKind kind, string expectedName)
        {
            var sprite = ComponentSymbolGenerator.GetOrCreateFallbackSprite(kind);

            Assert.AreEqual(expectedName, sprite.name, $"Sprite name mismatch for {kind}");
        }

        // ------------------------------------------------------------------
        // GetOrCreateFallbackSprite — caching: second call returns SAME instance
        // ------------------------------------------------------------------

        [TestCase(ComponentKind.Resistor)]
        [TestCase(ComponentKind.Capacitor)]
        [TestCase(ComponentKind.Inductor)]
        [TestCase(ComponentKind.VoltageSource)]
        [TestCase(ComponentKind.Ground)]
        public void GetOrCreateFallbackSprite_CalledTwice_ReturnsSameInstance(ComponentKind kind)
        {
            var first = ComponentSymbolGenerator.GetOrCreateFallbackSprite(kind);
            var second = ComponentSymbolGenerator.GetOrCreateFallbackSprite(kind);

            Assert.AreSame(first, second, $"Expected same cached Sprite instance for {kind}");
        }

        // ------------------------------------------------------------------
        // GetOrCreateFallbackSprite — texture has actual drawn pixels (symbol drawn)
        // ------------------------------------------------------------------

        [TestCase(ComponentKind.Resistor)]
        [TestCase(ComponentKind.Capacitor)]
        [TestCase(ComponentKind.Inductor)]
        [TestCase(ComponentKind.Diode)]
        [TestCase(ComponentKind.LED)]
        [TestCase(ComponentKind.BJT)]
        [TestCase(ComponentKind.MOSFET)]
        [TestCase(ComponentKind.VoltageSource)]
        [TestCase(ComponentKind.CurrentSource)]
        [TestCase(ComponentKind.Ground)]
        [TestCase(ComponentKind.Probe)]
        [TestCase(ComponentKind.ZenerDiode)]
        public void GetOrCreateFallbackSprite_AllKinds_TextureHasNonTransparentPixels(ComponentKind kind)
        {
            var sprite = ComponentSymbolGenerator.GetOrCreateFallbackSprite(kind);

            Assert.IsNotNull(sprite.texture, $"Sprite.texture is null for {kind}");
            Assert.IsTrue(
                HasNonTransparentPixels(sprite.texture),
                $"Texture for {kind} has no non-transparent pixels — symbol was not drawn");
        }

        // ------------------------------------------------------------------
        // GetOrCreateFallbackSprite — texture is 64x64
        // ------------------------------------------------------------------

        [TestCase(ComponentKind.Resistor)]
        [TestCase(ComponentKind.VoltageSource)]
        [TestCase(ComponentKind.Ground)]
        public void GetOrCreateFallbackSprite_TextureIs64x64(ComponentKind kind)
        {
            var sprite = ComponentSymbolGenerator.GetOrCreateFallbackSprite(kind);

            Assert.AreEqual(64, sprite.texture.width, $"Texture width should be 64 for {kind}");
            Assert.AreEqual(64, sprite.texture.height, $"Texture height should be 64 for {kind}");
        }

        // ------------------------------------------------------------------
        // GetPinDotSprite — non-null, cached, correct texture size
        // ------------------------------------------------------------------

        [Test]
        public void GetPinDotSprite_ReturnsNonNull()
        {
            var sprite = ComponentSymbolGenerator.GetPinDotSprite();

            Assert.IsNotNull(sprite, "GetPinDotSprite returned null");
        }

        [Test]
        public void GetPinDotSprite_TextureIs32x32()
        {
            var sprite = ComponentSymbolGenerator.GetPinDotSprite();

            Assert.IsNotNull(sprite.texture, "GetPinDotSprite.texture is null");
            Assert.AreEqual(32, sprite.texture.width, "PinDot texture width should be 32");
            Assert.AreEqual(32, sprite.texture.height, "PinDot texture height should be 32");
        }

        [Test]
        public void GetPinDotSprite_CalledTwice_ReturnsSameInstance()
        {
            var first = ComponentSymbolGenerator.GetPinDotSprite();
            var second = ComponentSymbolGenerator.GetPinDotSprite();

            Assert.AreSame(first, second, "GetPinDotSprite should return the same cached instance");
        }

        [Test]
        public void GetPinDotSprite_TextureHasNonTransparentPixels()
        {
            var sprite = ComponentSymbolGenerator.GetPinDotSprite();

            Assert.IsTrue(
                HasNonTransparentPixels(sprite.texture),
                "PinDot texture has no non-transparent pixels");
        }

        // ------------------------------------------------------------------
        // GetLedGlowSprite — non-null, cached, correct texture size (64x64)
        // ------------------------------------------------------------------

        [Test]
        public void GetLedGlowSprite_ReturnsNonNull()
        {
            var sprite = ComponentSymbolGenerator.GetLedGlowSprite();

            Assert.IsNotNull(sprite, "GetLedGlowSprite returned null");
        }

        [Test]
        public void GetLedGlowSprite_TextureIs64x64()
        {
            var sprite = ComponentSymbolGenerator.GetLedGlowSprite();

            Assert.IsNotNull(sprite.texture, "GetLedGlowSprite.texture is null");
            Assert.AreEqual(64, sprite.texture.width, "LedGlow texture width should be 64");
            Assert.AreEqual(64, sprite.texture.height, "LedGlow texture height should be 64");
        }

        [Test]
        public void GetLedGlowSprite_CalledTwice_ReturnsSameInstance()
        {
            var first = ComponentSymbolGenerator.GetLedGlowSprite();
            var second = ComponentSymbolGenerator.GetLedGlowSprite();

            Assert.AreSame(first, second, "GetLedGlowSprite should return the same cached instance");
        }

        [Test]
        public void GetLedGlowSprite_TextureHasNonTransparentPixels()
        {
            var sprite = ComponentSymbolGenerator.GetLedGlowSprite();

            Assert.IsTrue(
                HasNonTransparentPixels(sprite.texture),
                "LedGlow texture has no non-transparent pixels");
        }

        // ------------------------------------------------------------------
        // GetHeatGlowSprite — non-null, cached, correct texture size (64x64)
        // ------------------------------------------------------------------

        [Test]
        public void GetHeatGlowSprite_ReturnsNonNull()
        {
            var sprite = ComponentSymbolGenerator.GetHeatGlowSprite();

            Assert.IsNotNull(sprite, "GetHeatGlowSprite returned null");
        }

        [Test]
        public void GetHeatGlowSprite_TextureIs64x64()
        {
            var sprite = ComponentSymbolGenerator.GetHeatGlowSprite();

            Assert.IsNotNull(sprite.texture, "GetHeatGlowSprite.texture is null");
            Assert.AreEqual(64, sprite.texture.width, "HeatGlow texture width should be 64");
            Assert.AreEqual(64, sprite.texture.height, "HeatGlow texture height should be 64");
        }

        [Test]
        public void GetHeatGlowSprite_CalledTwice_ReturnsSameInstance()
        {
            var first = ComponentSymbolGenerator.GetHeatGlowSprite();
            var second = ComponentSymbolGenerator.GetHeatGlowSprite();

            Assert.AreSame(first, second, "GetHeatGlowSprite should return the same cached instance");
        }

        [Test]
        public void GetHeatGlowSprite_TextureHasNonTransparentPixels()
        {
            var sprite = ComponentSymbolGenerator.GetHeatGlowSprite();

            Assert.IsTrue(
                HasNonTransparentPixels(sprite.texture),
                "HeatGlow texture has no non-transparent pixels");
        }

        // ------------------------------------------------------------------
        // PinDotRadius constant
        // ------------------------------------------------------------------

        [Test]
        public void PinDotRadius_IsExpectedValue()
        {
            Assert.AreEqual(0.18f, ComponentSymbolGenerator.PinDotRadius, 0.0001f,
                "PinDotRadius should be 0.18f");
        }

        // ------------------------------------------------------------------
        // Cross-sprite independence: different ComponentKinds return different instances
        // ------------------------------------------------------------------

        [Test]
        public void GetOrCreateFallbackSprite_DifferentKinds_ReturnDifferentInstances()
        {
            var resistor = ComponentSymbolGenerator.GetOrCreateFallbackSprite(ComponentKind.Resistor);
            var capacitor = ComponentSymbolGenerator.GetOrCreateFallbackSprite(ComponentKind.Capacitor);
            var inductor = ComponentSymbolGenerator.GetOrCreateFallbackSprite(ComponentKind.Inductor);

            Assert.AreNotSame(resistor, capacitor, "Resistor and Capacitor should be different Sprite instances");
            Assert.AreNotSame(resistor, inductor, "Resistor and Inductor should be different Sprite instances");
            Assert.AreNotSame(capacitor, inductor, "Capacitor and Inductor should be different Sprite instances");
        }

        // ------------------------------------------------------------------
        // Sprite rect covers the full texture (pivot at center, PPU = texture size)
        // ------------------------------------------------------------------

        [TestCase(ComponentKind.Resistor)]
        [TestCase(ComponentKind.BJT)]
        public void GetOrCreateFallbackSprite_SpriteRectCoversFullTexture(ComponentKind kind)
        {
            var sprite = ComponentSymbolGenerator.GetOrCreateFallbackSprite(kind);

            Assert.AreEqual(0f, sprite.rect.x, 0.01f, $"Sprite rect.x should be 0 for {kind}");
            Assert.AreEqual(0f, sprite.rect.y, 0.01f, $"Sprite rect.y should be 0 for {kind}");
            Assert.AreEqual(64f, sprite.rect.width, 0.01f, $"Sprite rect.width should be 64 for {kind}");
            Assert.AreEqual(64f, sprite.rect.height, 0.01f, $"Sprite rect.height should be 64 for {kind}");
        }
    }
}
