using System;
using ColossalFramework.UI;
using UnityEngine;
using ChangeRoadHeight.Enums;

namespace ChangeRoadHeight.UI
{
    class AtlasCreator
    {
        public static UITextureAtlas CreateTextureAtlas(string textureFile, string atlasName, Material baseMaterial, int spriteWidth, int spriteHeight)
        {
            string[] spriteNames = Enum.GetNames(typeof(SpriteName));
            Texture2D tex = new Texture2D(spriteWidth * spriteNames.Length, spriteHeight, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;

            { // LoadTexture
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                System.IO.Stream textureStream = assembly.GetManifestResourceStream("ChangeRoadHeight." + textureFile);

                byte[] buf = new byte[textureStream.Length];  //declare arraysize
                textureStream.Read(buf, 0, buf.Length); // read from stream to byte array

                tex.LoadImage(buf);
                tex.Apply(true, true);
            }

            UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas>();

            { // Setup atlas
                Material material = (Material)Material.Instantiate(baseMaterial);
                material.mainTexture = tex;

                atlas.material = material;
                atlas.name = atlasName;
            }

            // Add sprites
            for (int i = 0; i < spriteNames.Length; ++i)
            {
                float uw = 1.0f / spriteNames.Length;

                var spriteInfo = new UITextureAtlas.SpriteInfo()
                {
                    name = spriteNames[i],
                    texture = tex,
                    region = new Rect(i * uw, 0, uw, 1),
                };

                atlas.AddSprite(spriteInfo);
            }

            return atlas;
        }
    }
}
