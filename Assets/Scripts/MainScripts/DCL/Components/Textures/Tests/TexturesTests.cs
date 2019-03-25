using System.Collections;
using System.Collections.Generic;
using DCL;
using DCL.Components;
using DCL.Helpers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class TexturesTests : TestsBase
    {
        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator TextureCreateAndLoadTest()
        {
            yield return InitScene();

            DCLTexture dclTexture = TestHelpers.CreateDCLTexture(scene,
                TestHelpers.GetTestsAssetsPath() + "/Images/avatar.png",
                DCLTexture.BabylonWrapMode.CLAMP,
                FilterMode.Bilinear);

            yield return new WaitForSeconds(1);

            Assert.IsTrue(dclTexture.texture != null, "Texture didn't load correctly?");
            Assert.IsTrue(dclTexture.unityWrap == TextureWrapMode.Clamp, "Bad wrap mode!");
            Assert.IsTrue(dclTexture.unitySamplingMode == FilterMode.Bilinear, "Bad sampling mode!");

            dclTexture.Dispose();

            yield return null;
            Assert.IsTrue(dclTexture.texture == null, "Texture didn't dispose correctly?");
        }
    }
}
