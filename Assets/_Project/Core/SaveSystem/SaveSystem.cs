using System.IO;
using UnityEngine;

namespace DungeonBlade.Core
{
    public class SaveSystem
    {
        const string ProfileFileName = "profile.json";
        const string BankFileName = "bank.json";

        public PlayerProfile Profile { get; private set; } = new PlayerProfile();
        public BankData Bank { get; private set; } = new BankData();
        public bool ProfileLoaded { get; private set; }
        public bool BankLoaded { get; private set; }

        static string Root => Path.Combine(Application.persistentDataPath, "DungeonBlade");

        public void Load()
        {
            Directory.CreateDirectory(Root);

            string profilePath = Path.Combine(Root, ProfileFileName);
            if (File.Exists(profilePath))
            {
                Profile = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(profilePath)) ?? new PlayerProfile();
                ProfileLoaded = true;
            }

            string bankPath = Path.Combine(Root, BankFileName);
            if (File.Exists(bankPath))
            {
                Bank = JsonUtility.FromJson<BankData>(File.ReadAllText(bankPath)) ?? new BankData();
                BankLoaded = true;
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(Root);
            File.WriteAllText(Path.Combine(Root, ProfileFileName), JsonUtility.ToJson(Profile, true));
            File.WriteAllText(Path.Combine(Root, BankFileName), JsonUtility.ToJson(Bank, true));
        }
    }
}
