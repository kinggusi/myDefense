using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace MyDefense.Clans
{
    public interface IClanPendingStore { string Load(); void Save(string json); void Clear(); }
    public sealed class ClanPendingRequestStore : IClanPendingStore
    {
        private readonly string key;
        public ClanPendingRequestStore(string endpoint,long userId){key=Scope(endpoint,userId);}
        public static string Scope(string endpoint,long userId)
        {
            using(var sha=SHA256.Create())return "MyDefense.Clan.Pending."+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(endpoint+"|"+userId))).Replace("-","");
        }
        public string Load(){if(!PlayerPrefs.HasKey(key))return null;string value=PlayerPrefs.GetString(key);ClanClientContract.ReadCommand(value);return value;}
        public void Save(string json){ClanClientContract.ReadCommand(json);PlayerPrefs.SetString(key,json);PlayerPrefs.Save();}
        public void Clear(){PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
    }
    public sealed class MemoryClanPendingStore : IClanPendingStore
    {
        private string value;
        public string Load()=>value;
        public void Save(string json){ClanClientContract.ReadCommand(json);value=json;}
        public void Clear(){value=null;}
    }
}
