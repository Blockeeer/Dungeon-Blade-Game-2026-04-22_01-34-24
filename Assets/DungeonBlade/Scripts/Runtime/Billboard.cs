using UnityEngine;

namespace DungeonBlade.Runtime
{
    /// <summary>
    /// Rotates the GameObject to always face the main camera. Used by zone labels
    /// so text is readable from any direction.
    ///
    /// TextMesh renders on one side only; without this component, text appears
    /// mirrored when viewed from behind.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        public bool lockY = false;
        Camera cam;

        void LateUpdate()
        {
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null) return;
            }

            Vector3 dir = transform.position - cam.transform.position;
            if (lockY) dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
