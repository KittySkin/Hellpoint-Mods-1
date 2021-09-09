using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;

namespace HellpointDataminer
{
    //public class TextureHelper
    //{
    //    /// <summary>
    //    /// Save an Icon as a png file.
    //    /// </summary>
    //    /// <param name="icon">The icon to save.</param>
    //    /// <param name="dir">The directory to save at.</param>
    //    /// <param name="name">The filename of the icon.</param>
    //    public static void SaveIconAsPNG(Sprite icon, string dir, string name = "icon")
    //    {
    //        SaveTextureAsPNG(icon.texture, dir, name, false);
    //    }

    //    /// <summary>
    //    /// Save a Texture2D as a png file.
    //    /// </summary>
    //    /// <param name="_tex">The texture to save.</param>
    //    /// <param name="dir">The directory to save at.</param>
    //    /// <param name="name">The filename to save as.</param>
    //    /// <param name="normal">Is this a Normal map (bump map)?</param>
    //    public static void SaveTextureAsPNG(Texture2D _tex, string dir, string name, bool normal)
    //    {
    //        if (!Directory.Exists(dir))
    //        {
    //            Directory.CreateDirectory(dir);
    //        }

    //        byte[] data;
    //        var savepath = dir + @"\" + name + ".png";

    //        try
    //        {
    //            if (normal)
    //            {
    //                _tex = DTXnmToRGBA(_tex);
    //                _tex.Apply(false, false);
    //            }

    //            data = _tex.EncodeToPNG();

    //            if (data == null)
    //            {
    //                throw new Exception();
    //            }
    //        }
    //        catch
    //        {
    //            var origFilter = _tex.filterMode;
    //            _tex.filterMode = FilterMode.Point;

    //            RenderTexture rt = RenderTexture.GetTemporary(_tex.width, _tex.height);
    //            rt.filterMode = FilterMode.Point;
    //            RenderTexture.active = rt;
    //            Graphics.Blit(_tex, rt);

    //            Texture2D _newTex = new Texture2D(_tex.width, _tex.height, TextureFormat.RGBA32, false);
    //            _newTex.ReadPixels(new Rect(0, 0, _tex.width, _tex.height), 0, 0);

    //            if (normal)
    //            {
    //                _newTex = DTXnmToRGBA(_newTex);
    //            }

    //            _newTex.Apply(false, false);

    //            RenderTexture.active = null;
    //            _tex.filterMode = origFilter;

    //            data = _newTex.EncodeToPNG();
    //            //data = _newTex.GetRawTextureData();
    //        }

    //        File.WriteAllBytes(savepath, data);
    //    }

    //    // Converts DTXnm-format Normal Map to RGBA-format Normal Map.
    //    private static Texture2D DTXnmToRGBA(Texture2D tex)
    //    {
    //        Color[] colors = tex.GetPixels();

    //        for (int i = 0; i < colors.Length; i++)
    //        {
    //            Color c = colors[i];

    //            c.r = c.a * 2 - 1;  // red <- alpha (x <- w)
    //            c.g = c.g * 2 - 1;  // green is always the same (y)

    //            Vector2 rg = new Vector2(c.r, c.g); //this is the xy vector
    //            c.b = Mathf.Sqrt(1 - Mathf.Clamp01(Vector2.Dot(rg, rg))); //recalculate the blue channel (z)

    //            colors[i] = new Color(
    //                (c.r * 0.5f) + 0.5f,
    //                (c.g * 0.5f) + 0.25f,
    //                (c.b * 0.5f) + 0.5f
    //            );
    //        }

    //        var newtex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
    //        newtex.SetPixels(colors); //apply pixels to the texture

    //        return newtex;
    //    }
    //}
}
