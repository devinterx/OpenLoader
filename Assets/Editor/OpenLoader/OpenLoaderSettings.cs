using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace OpenUniverse.Editor.OpenLoader
{
    [Serializable]
    public class OpenLoaderSettings
    {
        [NonSerialized]
        private static readonly string DirectorySettingPath = Application.persistentDataPath + "/OpenLoader";

        [NonSerialized]
        private static readonly string SettingPath = DirectorySettingPath + "/open-loader.accounts.dat";

        [NonSerialized]
        public readonly int SavedAccountsLimit = 10;

        [SerializeField]
        public List<OpenLoaderAccount> savedAccounts = new List<OpenLoaderAccount>();

        public List<OpenLoaderAccount> SavedAccounts
        {
            get => savedAccounts;
            private set => savedAccounts = value;
        }

        public static OpenLoaderSettings Load()
        {
            try
            {
                using (var streamReader = new StreamReader(SettingPath))
                {
                    return JsonUtility.FromJson<OpenLoaderSettings>(streamReader.ReadToEnd());
                }
            }
            catch (DirectoryNotFoundException)
            {
                Directory.CreateDirectory(DirectorySettingPath);
                return new OpenLoaderSettings();
            }
            catch (FileNotFoundException)
            {
                return new OpenLoaderSettings();
            }
        }

        public void Save()
        {
            var jsonData = JsonUtility.ToJson(this);

            using (var streamWriter = new StreamWriter(SettingPath, false, Encoding.UTF8))
            {
                streamWriter.Write(jsonData);
            }
        }

        public OpenLoaderSettings AddAccount(OpenLoaderAccount account)
        {
            if (savedAccounts.Count == SavedAccountsLimit) return this;

            if (savedAccounts.FindIndex(a => a.host == account.host && a.login == account.login) == -1)
            {
                savedAccounts.Add(account);
            }

            return this;
        }
    }

    public enum OpenLoaderSettingsMode
    {
        None,
        Account,
        Builder,
        Others
    }

    [Serializable]
    public class OpenLoaderAccount
    {
        public string host;
        public string login;
        public string password;

        public override string ToString()
        {
            return $"{login}@{host}";
        }
    }
}
