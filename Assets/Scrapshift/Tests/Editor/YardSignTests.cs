using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Scrapshift.Tests
{
    public sealed class YardSignTests
    {
        [SetUp]
        public void RequireFontGraphicsDevice()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("World-sign glyph/atlas tests require a graphics device. Run in the Unity Editor or batch without -nographics.");
        }
        [TestCase(0)][TestCase(90)][TestCase(180)]
        public void BoltedPlateKeepsDirectLastChildAndLegibleFrontAcrossRotations(float yaw)
        {
            var root = new GameObject("Rotated sign test");
            try
            {
                root.transform.localScale = new Vector3(2, 1.4f, .8f);
                Vector3 position = new Vector3(1, 2, 3);
                var label = YardSignText.Plate(root.transform, "RECEIVING / SCRAP", position, 2.4f, .4f, yaw);
                Assert.AreSame(label.transform, root.transform.GetChild(root.transform.childCount - 1));
                Assert.AreSame(root.transform, label.transform.parent);
                var expected = Quaternion.Euler(0, yaw, 0);
                Assert.Less(Quaternion.Angle(expected, label.transform.localRotation), .01f);
                Assert.Less(Vector3.Distance(position + expected * new Vector3(0, 0, -.039f), label.transform.localPosition), .0001f);
                var bounds = label.GetComponent<MeshRenderer>().localBounds.size;
                Assert.Greater(bounds.x, 0); Assert.Greater(bounds.y, 0);
                Assert.LessOrEqual(bounds.x, 2.4f * .80f + .001f);
                Assert.LessOrEqual(bounds.y, .4f * .67f + .001f);
                Assert.IsEmpty(root.GetComponentsInChildren<Collider>(), "decorative plates must not add collider components");
                var paints = root.GetComponentsInChildren<MeshRenderer>().Where(r => r.GetComponent<TextMesh>() == null).ToArray();
                Assert.AreEqual(6, paints.Length, "backing, ivory paint, four small corner fasteners");
                Assert.AreEqual(YardGeometry.Charcoal, label.color);
                Assert.IsFalse(label.richText);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void FitUsesGlyphWidthAndRefitsChangedTextInLocalSpace()
        {
            var root = new GameObject("Glyph metrics test");
            try
            {
                var wide = YardSignText.CreateLabel(root.transform, "WWWWWW", Vector3.zero, 90);
                var thin = YardSignText.CreateLabel(root.transform, "IIIIII", Vector3.zero, 180);
                float wideWidth = wide.GetComponent<MeshRenderer>().localBounds.size.x;
                float thinWidth = thin.GetComponent<MeshRenderer>().localBounds.size.x;
                Assert.Greater(wideWidth, thinWidth, "equal character count is not equal physical width");
                float available = (wideWidth + thinWidth) * .5f;
                root.transform.localScale = new Vector3(.5f, 2, 3);
                YardSignText.Fit(wide, available, 2); YardSignText.Fit(thin, available, 2);
                Assert.Less(wide.characterSize, thin.characterSize);
                Assert.LessOrEqual(wide.GetComponent<MeshRenderer>().localBounds.size.x, available + .001f);
                thin.text = "POWERED WIRE STRIPPER\nOUTPUT COLLECTION";
                YardSignText.Fit(thin, .9f, .3f);
                var size = thin.GetComponent<MeshRenderer>().localBounds.size;
                Assert.LessOrEqual(size.x, .901f); Assert.LessOrEqual(size.y, .301f);
                thin.text = ""; YardSignText.Fit(thin, 1, 1);
                Assert.Greater(thin.characterSize, 0);
                Assert.Throws<ArgumentOutOfRangeException>(() => YardSignText.Fit(thin, float.NaN, 1));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void MaterialsShareWithinWorldRefreshAtlasAndReleaseSubscriptionsOnDestroy()
        {
            var root = new GameObject("Font lifecycle test");
            var another = new GameObject("Independent font world");
            var temporary = new Texture2D(2, 2);
            Material first = null;
            try
            {
                var one = YardSignText.CreateLabel(root.transform, "DELIVERY", Vector3.zero);
                var two = YardSignText.CreateLabel(root.transform, "SALES", Vector3.zero);
                var other = YardSignText.CreateLabel(another.transform, "OFFICE", Vector3.zero);
                first = one.GetComponent<MeshRenderer>().sharedMaterial;
                Assert.AreSame(first, two.GetComponent<MeshRenderer>().sharedMaterial);
                Assert.AreNotSame(first, other.GetComponent<MeshRenderer>().sharedMaterial);
                Assert.AreEqual("Scrapshift/World Sign Text", first.shader.name);
                var owner = root.GetComponent<YardSignMaterials>();
                first.SetTexture("_MainTex", temporary);
                owner.RefreshAtlas(one.font);
                Assert.AreSame(one.font.material.mainTexture, first.GetTexture("_MainTex"));
                // This observes the public event's backing delegates to catch a globally rooted dead world.
                var backing = typeof(Font).GetField("textureRebuilt", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.NotNull(backing);
                Func<int> subscribed = () => ((Delegate)backing.GetValue(null))?.GetInvocationList().Count(d => ReferenceEquals(d.Target, owner)) ?? 0;
                Assert.AreEqual(1, subscribed());
                owner.enabled = false; Assert.AreEqual(0, subscribed()); Assert.IsFalse(first == null);
                first.SetTexture("_MainTex", temporary);
                owner.enabled = true; Assert.AreEqual(1, subscribed());
                Assert.AreSame(one.font.material.mainTexture, first.GetTexture("_MainTex"));
                owner.Dispose(); owner.Dispose();
                Assert.IsTrue(first == null); Assert.AreEqual(0, subscribed());
                var resumed = YardSignText.CreateLabel(root.transform, "CONTINUED", Vector3.zero).GetComponent<MeshRenderer>().sharedMaterial;
                Assert.AreEqual(1, subscribed());
                UnityEngine.Object.DestroyImmediate(root); root = null;
                Assert.IsTrue(resumed == null); Assert.AreEqual(0, subscribed());
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(another); UnityEngine.Object.DestroyImmediate(temporary);
            }
        }
    }
}
