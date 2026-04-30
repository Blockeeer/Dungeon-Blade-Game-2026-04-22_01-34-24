using UnityEngine;

namespace DungeonBlade.Core
{
    public class Billboard : MonoBehaviour
    {
        [SerializeField] bool lockYAxis = true;

        Transform _cam;

        void LateUpdate()
        {
            if (_cam == null)
            {
                if (Camera.main == null) return;
                _cam = Camera.main.transform;
            }

            Vector3 dir = _cam.position - transform.position;
            if (lockYAxis) dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(-dir);
        }
    }
}
