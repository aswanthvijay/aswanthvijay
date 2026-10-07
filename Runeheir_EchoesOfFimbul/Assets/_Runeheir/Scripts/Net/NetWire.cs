using Mirror;
using Runeheir.Combat;
using UnityEngine;

namespace Runeheir.Net
{
    /// <summary>Small helpers for packing combat data into message fields and finding entities by network id.</summary>
    internal static class NetWire
    {
        /// <summary>Largest single hit the realm accepts from a client (anything above is a forged packet).</summary>
        public const int MaxHit = 5_000_000;

        /// <summary>How far from a target a client may claim to act on it (meters; skills reach about 15).</summary>
        public const float MaxActionRange = 40f;

        public static byte Flags(in DamageResult result)
        {
            int flags = 0;
            if (result.IsCritical)
            {
                flags |= 1;
            }

            if (result.IsMiss)
            {
                flags |= 2;
            }

            if (result.IsBlocked)
            {
                flags |= 4;
            }

            if (result.IsDamageOverTime)
            {
                flags |= 8;
            }

            if (result.IsMagical)
            {
                flags |= 16;
            }

            return (byte)flags;
        }

        public static DamageResult Result(int amount, byte flags, float elementMultiplier, int absorbed = 0)
        {
            return new DamageResult
            {
                Amount = Mathf.Clamp(amount, 0, MaxHit),
                IsCritical = (flags & 1) != 0,
                IsMiss = (flags & 2) != 0,
                IsBlocked = (flags & 4) != 0,
                IsDamageOverTime = (flags & 8) != 0,
                IsMagical = (flags & 16) != 0,
                ElementMultiplier = float.IsNaN(elementMultiplier) ? 1f : Mathf.Clamp(elementMultiplier, 0f, 4f),
                Absorbed = Mathf.Max(0, absorbed),
            };
        }

        /// <summary>The network id of an entity (0 when it isn't networked).</summary>
        public static uint IdOf(CombatEntity entity)
        {
            if (entity == null)
            {
                return 0;
            }

            var identity = entity.GetComponent<NetworkIdentity>();
            return identity != null ? identity.netId : 0;
        }

        /// <summary>The entity with that network id, on whichever side we're on.</summary>
        public static CombatEntity Entity(uint netId)
        {
            if (netId == 0)
            {
                return null;
            }

            NetworkIdentity identity = null;
            if (NetworkServer.active)
            {
                NetworkServer.spawned.TryGetValue(netId, out identity);
            }

            if (identity == null && NetworkClient.active)
            {
                NetworkClient.spawned.TryGetValue(netId, out identity);
            }

            return identity != null ? identity.GetComponent<CombatEntity>() : null;
        }

        public static bool Finite(Vector3 value)
        {
            return !(float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z)
                     || float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z));
        }

        public static float Clamp(float value, float min, float max)
        {
            return float.IsNaN(value) ? min : Mathf.Clamp(value, min, max);
        }
    }
}
