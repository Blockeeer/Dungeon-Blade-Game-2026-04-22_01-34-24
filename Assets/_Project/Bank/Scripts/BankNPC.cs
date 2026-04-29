using DungeonBlade.Bank.UI;
using UnityEngine;

namespace DungeonBlade.Bank
{
    public class BankNPC : Interactable
    {
        public override void OnInteract(GameObject player)
        {
            if (BankController.Instance != null)
            {
                BankController.Instance.Open();
            }
            else
            {
                Debug.LogWarning("[BankNPC] No BankController in scene.");
            }
        }
    }
}
