using System;

namespace OpenUniverse.Runtime.OpenLoader
{
    public class OpenLoaderCache
    {
        public void LoadFromCache(string name, Action callback = null)
        {
            callback?.Invoke();
        }

        public void SaveToCache(string name, byte[] data, Action callback = null)
        {
            callback?.Invoke();
        }
    }
}
