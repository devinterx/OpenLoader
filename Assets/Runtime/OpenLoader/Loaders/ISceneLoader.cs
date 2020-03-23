using System;

namespace OpenUniverse.Runtime.OpenLoader.Loaders
{
    public interface IOpenSceneLoader
    {
        bool IsSceneLoaded(string sceneName);
        void LoadScene(string sceneName, Action callback = null);
        void LoadScene(Uri assetBundleUrl, string sceneName, Action callback = null, bool autoUnloadAssetBundle = false);
        void UnLoadScene(string sceneName);
    }
}
