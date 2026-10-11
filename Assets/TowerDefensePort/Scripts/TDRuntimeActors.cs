using System.Collections.Generic;
using UnityEngine;

namespace TowerDefensePort
{
    internal sealed class TowerActor
    {
        public TowerDefinition Definition = null!;
        public GameObject Root = null!;
        public SpriteRenderer Base = null!;
        public SpriteRenderer Top = null!;
        public Transform Ring = null!;
        public Transform Prism = null!;
        public GameObject Glow = null!;
        public float Damage;
        public float Range;
        public float FireRate;
        public float Cooldown;
        public float AbilityTimer;
        public int Level = 1;
        public float Seed;
        public int InvestedCost;
        public int Specialization;
        public float AbilityCooldownScale = 1f;
    }

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

    internal sealed class ProjectileVisual
    {
        public GameObject Root = null!;
        public SpriteRenderer Renderer = null!;
        public Vector2 Start;
        public Vector2 End;
        public float Progress;
        public float Speed;
        public float Distance;
        public float ArcHeight;
    }

    internal sealed class FootprintDecal
    {
        public GameObject Root = null!;
        public SpriteRenderer Renderer = null!;
        public float Life;
        public float MaxLife;
        public Color BaseColor;
        public Vector3 BaseScale;
    }

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
