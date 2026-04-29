using System.Collections.Generic;
using UnityEngine;

namespace DungeonBlade.Combat
{
    public static class MeleeHitbox
    {
        static readonly Collider[] s_buffer = new Collider[16];

        public static int SphereSweep(
            Vector3 center,
            float radius,
            LayerMask mask,
            HashSet<IDamageable> alreadyHit,
            in DamageInfo template,
            float damageOverride = -1f)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, s_buffer, mask, QueryTriggerInteraction.Ignore);
            int hits = 0;

            for (int i = 0; i < count; i++)
            {
                var col = s_buffer[i];
                if (col == null) continue;
                if (template.Source != null && col.transform.IsChildOf(template.Source.transform)) continue;

                var dmg = col.GetComponentInParent<IDamageable>();
                if (dmg == null || !dmg.IsAlive) continue;
                if (alreadyHit != null && !alreadyHit.Add(dmg)) continue;

                var info = template;
                if (damageOverride > 0f) info.Amount = damageOverride;
                info.HitPoint = col.ClosestPoint(center);
                info.HitDirection = (info.HitPoint - center).sqrMagnitude > 0.0001f
                    ? (info.HitPoint - center).normalized
                    : (template.Source != null ? template.Source.transform.forward : Vector3.forward);

                dmg.ApplyDamage(info);
                hits++;
            }

            return hits;
        }
    }
}
