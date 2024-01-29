using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MoreCompany.Utils;

public class BundleUtilities
{
    public static byte[] GetResourceBytes(String filename, Assembly assembly)
    {
        foreach (var resource in assembly.GetManifestResourceNames())
        {
            if (resource.Contains(filename))
            {
                using (Stream resFilestream = assembly.GetManifestResourceStream(resource))
                {
                    if (resFilestream == null) return null;
                    byte[] ba = new byte[resFilestream.Length];
                    resFilestream.Read(ba, 0, ba.Length);
                    return ba;
                }
            }
        }
        return null;
    }

    public static AssetBundle LoadBundleFromInternalAssembly(string filename, Assembly assembly)
    {
        AssetBundle bundle = AssetBundle.LoadFromMemory(GetResourceBytes(filename, assembly));
        return bundle;
    }
}
    
public static class AssetBundleExtension
{
    public static T LoadPersistentAsset<T>(this AssetBundle bundle, string name) where T : UnityEngine.Object {
        var asset = bundle.LoadAsset(name);

        if (asset != null) {
            asset.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return (T) asset;
        }

        return null;
    }
}