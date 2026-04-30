using DungeonBlade.Core;
using DungeonBlade.Inventory;
using UnityEngine;

namespace DungeonBlade.Bank
{
    public class ScenePortal : Interactable
    {
        [SerializeField] string targetScene = SceneLoader.Dungeon1;
        [SerializeField] InventoryPersistence persistence;
        [SerializeField] bool saveBeforeTransition = true;

        public override void OnInteract(GameObject player)
        {
            if (saveBeforeTransition && persistence != null) persistence.SaveNow();

            if (FadeLoader.Instance != null) FadeLoader.Instance.LoadScene(targetScene);
            else SceneLoader.Load(targetScene);
        }
    }
}
