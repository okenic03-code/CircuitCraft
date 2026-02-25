using System;
using System.Reflection;
using CircuitCraft.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CircuitCraft.Tests.Components
{
    [TestFixture]
    public sealed class ComponentViewNullHandlingTests
    {
        private const string ComponentViewPrefabPath = "Assets/20_Prefabs/50_Components/ComponentView.prefab";

        [Test]
        public void Init_WhenSpriteRendererReferenceIsDestroyed_RebindsSpriteRendererWithoutException()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ComponentViewPrefabPath);
            Assert.IsNotNull(prefab, $"Missing prefab at '{ComponentViewPrefabPath}'");

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Assert.IsNotNull(instance, "PrefabUtility.InstantiatePrefab returned null");

            GameObject tempRendererGo = null;
            try
            {
                ComponentView view = instance.GetComponent<ComponentView>();
                Assert.IsNotNull(view, "ComponentView component missing on prefab instance");

                var instanceRenderer = instance.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(instanceRenderer, "SpriteRenderer component missing on prefab instance");

                tempRendererGo = new GameObject("TempSpriteRenderer");
                var tempRenderer = tempRendererGo.AddComponent<SpriteRenderer>();

                SetPrivateField(view, "_spriteRenderer", tempRenderer);
                UnityEngine.Object.DestroyImmediate(tempRendererGo);

                Assert.IsTrue(tempRenderer == null, "Expected destroyed renderer to compare equal to null via Unity operator==");
                Assert.IsFalse(ReferenceEquals(tempRenderer, null), "Expected destroyed renderer reference to be non-null at CLR level");

                Assert.DoesNotThrow(() => InvokePrivateMethod(view, "Init"));

                var reboundRenderer = (SpriteRenderer)GetPrivateField(view, "_spriteRenderer");
                Assert.IsNotNull(reboundRenderer, "_spriteRenderer should be rebound during Init()");
                Assert.AreSame(instanceRenderer, reboundRenderer, "_spriteRenderer should be rebound to the instance SpriteRenderer");
            }
            finally
            {
                if (tempRendererGo != null)
                {
                    UnityEngine.Object.DestroyImmediate(tempRendererGo);
                }

                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}");
            return field.GetValue(target);
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"Method '{methodName}' not found on {target.GetType().Name}");
            try
            {
                method.Invoke(target, null);
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                throw new Exception($"{target.GetType().Name}.{methodName} threw: {e.InnerException.GetType().Name}", e.InnerException);
            }
        }
    }
}
