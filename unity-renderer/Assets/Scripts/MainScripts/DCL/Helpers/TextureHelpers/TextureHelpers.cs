using UnityEngine;

public static class TextureHelpers
{
    public static void EnsureTexture2DMaxSize(ref Texture2D texture, int maxTextureSize)
    {
        if (texture == null)
            return;

        if (texture.width == 0 || texture.height == 0)
            return;

        if (Mathf.Max(texture.height, texture.width) <= maxTextureSize)
            return;

        int w, h;
        if (texture.height > texture.width)
        {
            h = maxTextureSize;
            w = (int) ((texture.width / (float) texture.height) * h);
        }
        else
        {
            w = maxTextureSize;
            h = (int) ((texture.height / (float) texture.width) * w);
        }

        var newTexture = Resize(texture, w, h);
        var oldTexture = texture;
        texture = newTexture;
        Object.Destroy(oldTexture);
    }

    public static Texture2D Resize(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);

        source.filterMode = FilterMode.Point;
        rt.filterMode = FilterMode.Point;

        Graphics.Blit(source, rt);

        Texture2D nTex = new Texture2D(newWidth, newHeight, TextureFormat.ARGB32, false);

        Graphics.CopyTexture(rt, nTex);

        RenderTexture.ReleaseTemporary(rt);
        return nTex;
    }

    public static Texture2D CopyTexture(Texture2D sourceTexture)
    {
        Texture2D texture = new Texture2D(sourceTexture.width, sourceTexture.height, sourceTexture.format, false);
        Graphics.CopyTexture(sourceTexture, texture);
        return texture;
    }
}