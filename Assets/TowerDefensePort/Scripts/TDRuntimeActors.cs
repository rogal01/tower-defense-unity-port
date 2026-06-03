using System.Collections.Generic;
using UnityEngine;

namespace TowerDefensePort
{
    /// <summary>
    /// Runtime state for a single placed tower. The "actor" half of an actor +
    /// controller pattern: this struct-like class only owns data and scene refs.
    /// All gameplay decisions (when to fire, which target, upgrade rules) live in
    /// <see cref="TDGameController"/> so the actor stays trivially serializable
    /// and cheap to pool.
    /// </summary>
    internal sealed class TowerActor
    {
        /// <summary>Static archetype data (cost, base stats, sprites). Set once at spawn.</summary>
        public TowerDefinition Definition = null!;
        /// <summary>Root GameObject — used for despawn and selection ray hits.</summary>
        public GameObject Root = null!;
        public SpriteRenderer Base = null!;
        public SpriteRenderer Top = null!;
        public Transform Ring = null!;
        public Transform Prism = null!;
        public GameObject Glow = null!;
        public float Damage;
        public float Range;
        public float FireRate;
        /// <summary>Seconds remaining until this tower can fire its next shot.</summary>
        public float Cooldown;
        /// <summary>Seconds remaining until the special ability is off cooldown.</summary>
        public float AbilityTimer;
        public int Level = 1;
        /// <summary>Per-instance random seed — drives jitter on the idle/firing animations.</summary>
        public float Seed;
        /// <summary>Total gold spent on this tower (base cost + upgrades). Used to compute sell value.</summary>
        public int InvestedCost;
        /// <summary>0 = unspecialized, 1 = first branch, 2 = second branch. Unlocked at level 5.</summary>
        public int Specialization;
        /// <summary>Multiplier applied to ability cooldowns by global power-ups / skill tree.</summary>
        public float AbilityCooldownScale = 1f;
    }

    /// <summary>
    /// Runtime state for a single live enemy. Holds movement progress along the
    /// lane, current health, every active status effect, and boss-specific
    /// scratch state. Same actor/controller split as <see cref="TowerActor"/>.
    /// </summary>
    internal sealed class EnemyActor
    {
        public GameObject Root = null!;
        public SpriteRenderer Body = null!;
        public SpriteRenderer Core = null!;
        public Transform Shell = null!;
        public Transform Eye = null!;
        public Transform BarRoot = null!;
        public Transform BarFill = null!;
        public float BarWidth = 0.56f;
        public EnemyArchetype Type;
        public float Health;
        public float MaxHealth;
        public float Speed;
        public float Damage;
        public int Reward;
        public List<Vector2> Path = new();
        public int LaneIndex;
        public int PathIndex;
        public float Progress;
        public float BurnTimer;
        public float BurnDps;
        public float PoisonTimer;
        public float PoisonDps;
        public float SlowMultiplier = 1f;
        public float SlowTimer;
        public float Seed;
        public float FootstepTimer;
        public bool LeftStep;
        public float RegenRate;
        public bool ReachedBase;
        public bool IsElite;
        public float ShieldTimer;
        public TDBossType? BossType;
        public TDBossAbility BossAbility;
        public float BossAbilityTimer;
        public float BossAbilityCooldown = 5f;
        public bool IsCharging;
        public float ChargeTimer;
        public bool HasSplit;
        public float RoarSpeedBoost = 1f;
        public float RoarBoostTimer;
        public bool Removed;
    }

    /// <summary>
    /// Visual-only projectile (the actual damage is applied the moment a tower
    /// fires; this just flies a sprite from Start to End so the shot feels real).
    /// Kept separate from gameplay so we can drop projectile rendering on low-end
    /// devices without changing the simulation.
    /// </summary>
    internal sealed class ProjectileVisual
    {
        public GameObject Root = null!;
        public SpriteRenderer Renderer = null!;
        public Vector2 Start;
        public Vector2 End;
        /// <summary>0..1 along the path from Start to End.</summary>
        public float Progress;
        public float Speed;
        public float Distance;
        /// <summary>Vertical arc height in world units — 0 for straight bolts, &gt;0 for lobbed cannon shells.</summary>
        public float ArcHeight;
    }

    /// <summary>Footprint left behind by walking enemies — purely cosmetic, fades out over <see cref="MaxLife"/> seconds.</summary>
    internal sealed class FootprintDecal
    {
        public GameObject Root = null!;
        public SpriteRenderer Renderer = null!;
        public float Life;
        public float MaxLife;
        public Color BaseColor;
        public Vector3 BaseScale;
    }

    /// <summary>One pending enemy spawn coming out of the wave queue. Multipliers compose with the wave/elite modifiers.</summary>
    internal struct SpawnRequest
    {
        public EnemyArchetype Type;
        public int PathIndex;
        public float HealthMultiplier;
        public float SpeedMultiplier;
        public float RewardMultiplier;
        public TDWaveModifier Modifier;
        public TDBossType? BossType;
        public bool IsElite;
        public bool IsBossMinion;
        public float RegenRate;
    }
}
