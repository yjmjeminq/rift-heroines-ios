using System;
using System.IO;
using UnityEngine;

namespace RiftHeroines
{
    public static class RiftStorage
    {
        public static string OverridePath;
        public static string RecoveryNotice {get;private set;}="";
        public static bool LastSaveSucceeded {get;private set;}=true;
        static string protectBackup;
        public static string Path { get { return OverridePath??System.IO.Path.Combine(Application.persistentDataPath,"rift-profile-v1.json"); } }
        public static PlayerProfile Load()
        {
            RecoveryNotice="";PlayerProfile profile;
            if(TryRead(Path,out profile))return profile;
            bool damaged=File.Exists(Path);if(damaged)PreserveDamaged(Path);
            if(TryRead(Path+".bak",out profile))
            {protectBackup=Path;RecoveryNotice="백업에서 진행 기록을 복구했습니다.";return profile;}
            if(File.Exists(Path+".bak"))PreserveDamaged(Path+".bak");
            if(damaged)RecoveryNotice="읽을 수 없는 진행 기록을 별도로 보관했습니다.";
            var fresh=new PlayerProfile();fresh.Normalize();return fresh;
        }
        static bool TryRead(string file,out PlayerProfile profile)
        {
            profile=null;if(!File.Exists(file))return false;
            try
            {
                profile=JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(file));
                if(profile==null || profile.schema!=1 || profile.shards<0)throw new InvalidDataException("Invalid profile");
                profile.healthUpgrade=Mathf.Clamp(profile.healthUpgrade,0,5);profile.manaUpgrade=Mathf.Clamp(profile.manaUpgrade,0,5);
                profile.Normalize();
                if(!RiftBattle.IsValid(profile.checkpoint))profile.checkpoint=null;
                return true;
            }
            catch(Exception){profile=null;return false;}
        }
        static void PreserveDamaged(string file)
        {
            try {File.Move(file,file+".damaged-"+DateTime.UtcNow.Ticks);}
            catch(Exception error){Debug.LogWarning("Could not preserve unreadable profile: "+error.Message);}
        }
        public static bool Save(PlayerProfile profile)
        {
            try
            {
                if(profile==null)return false;profile.Normalize();
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                string temporary=Path+".tmp";File.WriteAllText(temporary,JsonUtility.ToJson(profile,true));
                if(File.Exists(Path))File.Replace(temporary,Path,protectBackup==Path?null:Path+".bak");else File.Move(temporary,Path);
                if(protectBackup==Path)protectBackup=null;LastSaveSucceeded=true;return true;
            }
            catch(Exception error){LastSaveSucceeded=false;Debug.LogWarning("Could not save Rift profile: "+error.Message);return false;}
        }
    }
}
