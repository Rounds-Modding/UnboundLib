/*
MIT License

Copyright(c) 2021 JotunnLib Team

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.IO;
using UnityEngine;
using System.Reflection;
using System.Linq;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Util functions related to loading assets at runtime.
    /// </summary>
    public static class AssetUtils
    {
        /// <summary>
        ///     Path separator for AssetBundles
        /// </summary>
        public const char AssetBundlePathSeparator = '$';

        /// <summary>
        ///     Loads a <see cref="Texture2D"/> from file at runtime.
        /// </summary>
        /// <param name="texturePath">Texture path relative to "plugins" BepInEx folder</param>
        /// <param name="relativePath">Is the given path relative</param>
        /// <returns>Texture2D loaded, or null if invalid path</returns>
        public static Texture2D LoadTexture(string texturePath, bool relativePath = true)
        {
            string path = texturePath;

            if (relativePath)
            {
                path = Path.Combine(BepInEx.Paths.PluginPath, texturePath);
            }

            if (!File.Exists(path))
            {
                return null;
            }

            // Ensure it's a texture
            if (!path.EndsWith(".png") && !path.EndsWith(".jpg"))
            {
                throw new Exception("LoadTexture can only load png or jpg textures");
            }

            byte[] fileData = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadRawTextureData(fileData);
            return tex;
        }

        /// <summary>
        ///     Loads a <see cref="Sprite"/> from file at runtime.
        /// </summary>
        /// <param name="spritePath">Texture path relative to "plugins" BepInEx folder</param>
        /// <returns>Texture2D loaded, or null if invalid path</returns>
        public static Sprite LoadSpriteFromFile(string spritePath)
        {
            var tex = LoadTexture(spritePath);

            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(), 100);
            }

            return null;
        }
        
        /// <summary>
        ///     Loads an asset bundle at runtime.
        /// </summary>
        /// <param name="bundlePath">Asset bundle path relative to "plugins" BepInEx folder</param>
        /// <returns>AssetBundle loaded, or null if invalid path</returns>
        public static AssetBundle LoadAssetBundle(string bundlePath)
        {
            string path = Path.Combine(BepInEx.Paths.PluginPath, bundlePath);

            if (!File.Exists(path))
            {
                return null;
            }

            return AssetBundle.LoadFromFile(path);
        }

        /// <summary>
        ///     Load an assembly-embedded <see cref="AssetBundle" />
        /// </summary>
        /// <param name="bundleName">Name of the bundle</param>
        /// <param name="resourceAssembly">Executing assembly</param>
        /// <returns></returns>
        public static AssetBundle LoadAssetBundleFromResources(string bundleName, Assembly resourceAssembly)
        {
            if (resourceAssembly == null)
            {
                throw new ArgumentNullException("Parameter resourceAssembly can not be null.");
            }

            string resourceName = null;
            try
            {
                resourceName = resourceAssembly.GetManifestResourceNames().Single(str => str.EndsWith(bundleName));
            } catch (Exception) { }

            if (resourceName == null)
            {
                Debug.LogError($"AssetBundle {bundleName} not found in assembly manifest");
                return null;
            }

            AssetBundle ret;
            using (var stream = resourceAssembly.GetManifestResourceStream(resourceName))
            {
                ret = AssetBundle.LoadFromStream(stream);
            }

            return ret;
        }

        /// <summary>
        ///     Loads the contents of a file as a char string
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string LoadText(string path)
        {
            string absPath = Path.Combine(BepInEx.Paths.PluginPath, path);

            if (!File.Exists(absPath))
            {
                Debug.LogError($"Error, failed to load contents from non-existant path: ${absPath}");
                return null;
            }

            return File.ReadAllText(absPath);
        }
        
        /// <summary>
        ///     Loads a <see cref="Sprite"/> from a file path or an asset bundle (separated by <see cref="AssetBundlePathSeparator"/>)
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static Sprite LoadSprite(string assetPath)
        {
            string path = Path.Combine(BepInEx.Paths.PluginPath, assetPath);

            if (!File.Exists(path))
            {
                return null;
            }

            // Check if asset is from a bundle or from a path
            if (path.Contains(AssetBundlePathSeparator.ToString()))
            {
                string[] parts = path.Split(AssetBundlePathSeparator);
                string bundlePath = parts[0];
                string assetName = parts[1];

                // TODO: This is very likely going to need some caching for asset bundles
                AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
                Sprite ret = bundle.LoadAsset<Sprite>(assetName);
                bundle.Unload(false);
                return ret;
            }

            // Load texture and create sprite
            Texture2D texture = LoadTexture(path, false);
            
            if (!texture)
            {
                return null;
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
        }
    }
}
