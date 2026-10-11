using System.Collections.Generic;
using UnityEngine;

namespace TowerDefensePort
{
    public enum TowerType
    {
        Arrow,
        Magic,
        Cannon,
        Poison,
        Tesla,
        Ice
    }

    public enum PowerType
    {
        Fireball,
        Freeze,
        Heal,
        Lightning
    }

    public enum MapType
    {
        Classic,
        Valley,
        Crossroads,
        Desert,
        Snow
    }

    public enum GameMode
    {
        Campaign,
        Endless,
        BossRush,
        DailyChallenge
    }

    public enum EnemyArchetype
    {
        Runner,
        Raider,
        Brute,
        Wisp,
        Boss,
        Goblin,
        Skeleton,
        Orc,
        Demon,
        Dragon,
        Shadow,
        Slime,
        Bat,
        Spider,
        GolemShard,
        FastSkeleton,
        ArmoredGolem,
        MiniOrc,
        MiniSkeleton,
        MiniDemon,
        MiniDragon
    }

    public sealed class TowerDefinition
    {
        public TowerType Type;
        public string Name = "";
        public int Cost;
        public float Damage;
        public float Range;
        public float FireRate;
        public float ProjectileSpeed;
        public float AbilityCooldown;
        public Color Body;
        public Color Accent;
        public string Description = "";
    }

    public sealed class PowerDefinition
    {
        public PowerType Type;
        public string Name = "";
        public int Cost;
        public float Cooldown;
        public Color Color;
        public string Description = "";
    }

    public sealed class EnemyDefinition
    {
        public EnemyArchetype Type;
        public string Name = "";
        public float Health;
        public float Speed;
        public float Damage;
        public int Reward;
        public Color Body;
        public Color Accent;
    }

    public sealed class MapDefinition
    {
        public MapType Type;
        public string Name = "";
        public string Label = "";
        public Color BackgroundTop;
        public Color BackgroundBottom;
        public Color Surface;
        public Color PathOuter;
        public Color PathInner;
        public Color Accent;
        public bool DualLane;
    }

    public sealed class CampaignLevel
    {
        public int Id;
        public string Title = "";
        public string Hint = "";
        public int StartingGold;
        public int TargetWave;
        public float EnemyHealthMultiplier;
        public float EnemyDamageMultiplier = 1f;
        public float EnemySpeedMultiplier;
        public float RewardMultiplier;
        public float GoldMultiplier = 1f;
        public float SpawnRateMultiplier = 1f;
        public int DiamondReward = 3;
        public bool UpgradesEnabled = true;
        public MapType Map;
        public List<TowerType> AllowedTowers = new();
        public List<PowerType> AllowedPowers = new();
    }

    /// <summary>
    /// Static registry for canonical game data. 
    /// Matches the Kotlin reference implementation 1:1 to ensure cross-platform parity.
    /// </summary>
    public static class TDData
    {
        public static readonly Dictionary<TowerType, TowerDefinition> Towers = new()
        {
            [TowerType.Arrow] = new TowerDefinition
            {
                Type = TowerType.Arrow,
                Name = "Arrow",
                Cost = 30,
                Damage = 8f,
                Range = 2.8f,
                FireRate = 1.25f,
                ProjectileSpeed = 9.5f,
                AbilityCooldown = 14f,
                Body = new Color(0.28f, 0.22f, 0.16f),
                Accent = new Color(0.58f, 0.88f, 0.38f),
                Description = "Fast physical pressure and reliable single-target fire."
            },
            [TowerType.Magic] = new TowerDefinition
            {
                Type = TowerType.Magic,
                Name = "Magic",
                Cost = 60,
                Damage = 14f,
                Range = 3.1f,
                FireRate = 0.82f,
                ProjectileSpeed = 8f,
                AbilityCooldown = 16f,
                Body = new Color(0.20f, 0.14f, 0.34f),
                Accent = new Color(0.82f, 0.54f, 1.0f),
                Description = "Arcane bursts with high per-shot impact."
            },
            [TowerType.Cannon] = new TowerDefinition
            {
                Type = TowerType.Cannon,
                Name = "Cannon",
                Cost = 95,
                Damage = 26f,
                Range = 2.5f,
                FireRate = 0.52f,
                ProjectileSpeed = 6.4f,
                AbilityCooldown = 18f,
                Body = new Color(0.24f, 0.22f, 0.20f),
                Accent = new Color(0.98f, 0.52f, 0.22f),
                Description = "Splash damage built for packed lanes."
            },
            [TowerType.Poison] = new TowerDefinition
            {
                Type = TowerType.Poison,
                Name = "Poison",
                Cost = 80,
                Damage = 7f,
                Range = 2.9f,
                FireRate = 1.0f,
                ProjectileSpeed = 7.2f,
                AbilityCooldown = 15f,
                Body = new Color(0.12f, 0.24f, 0.14f),
                Accent = new Color(0.42f, 0.94f, 0.34f),
                Description = "Damage over time and soft area pressure."
            },
            [TowerType.Tesla] = new TowerDefinition
            {
                Type = TowerType.Tesla,
                Name = "Tesla",
                Cost = 120,
                Damage = 18f,
                Range = 3.25f,
                FireRate = 0.72f,
                ProjectileSpeed = 11f,
                AbilityCooldown = 18f,
                Body = new Color(0.20f, 0.22f, 0.28f),
                Accent = new Color(1.0f, 0.88f, 0.28f),
                Description = "Chain damage across clustered enemies."
            },
            [TowerType.Ice] = new TowerDefinition
            {
                Type = TowerType.Ice,
                Name = "Ice",
                Cost = 70,
                Damage = 3f,
                Range = 3.0f,
                FireRate = 0.55f,
                ProjectileSpeed = 7.2f,
                AbilityCooldown = 14f,
                Body = new Color(0.18f, 0.28f, 0.38f),
                Accent = new Color(0.66f, 0.92f, 1.0f),
                Description = "Slows enemies and stabilizes heavy lanes."
            }
        };

        public static readonly Dictionary<PowerType, PowerDefinition> Powers = new()
        {
            [PowerType.Fireball] = new PowerDefinition
            {
                Type = PowerType.Fireball,
                Name = "Fireball",
                Cost = 45,
                Cooldown = 9f,
                Color = new Color(1f, 0.42f, 0.18f),
                Description = "Blast a target area."
            },
            [PowerType.Freeze] = new PowerDefinition
            {
                Type = PowerType.Freeze,
                Name = "Freeze",
                Cost = 35,
                Cooldown = 13f,
                Color = new Color(0.58f, 0.86f, 1f),
                Description = "Lock down the whole wave."
            },
            [PowerType.Heal] = new PowerDefinition
            {
                Type = PowerType.Heal,
                Name = "Heal",
                Cost = 28,
                Cooldown = 15f,
                Color = new Color(0.42f, 0.96f, 0.58f),
                Description = "Restore the base."
            },
            [PowerType.Lightning] = new PowerDefinition
            {
                Type = PowerType.Lightning,
                Name = "Lightning",
                Cost = 52,
                Cooldown = 11f,
                Color = new Color(1f, 0.9f, 0.32f),
                Description = "Chain through nearby enemies."
            }
        };

        public static readonly Dictionary<EnemyArchetype, EnemyDefinition> Enemies = new()
        {
            [EnemyArchetype.Runner] = new EnemyDefinition
            {
                Type = EnemyArchetype.Runner,
                Name = "Runner",
                Health = 20f,
                Speed = 1.65f,
                Damage = 5f,
                Reward = 4,
                Body = new Color(0.34f, 0.80f, 0.40f),
                Accent = new Color(0.98f, 0.94f, 0.36f)
            },
            [EnemyArchetype.Raider] = new EnemyDefinition
            {
                Type = EnemyArchetype.Raider,
                Name = "Raider",
                Health = 42f,
                Speed = 1.25f,
                Damage = 8f,
                Reward = 7,
                Body = new Color(0.86f, 0.30f, 0.26f),
                Accent = new Color(1f, 0.82f, 0.34f)
            },
            [EnemyArchetype.Brute] = new EnemyDefinition
            {
                Type = EnemyArchetype.Brute,
                Name = "Brute",
                Health = 82f,
                Speed = 0.9f,
                Damage = 14f,
                Reward = 12,
                Body = new Color(0.44f, 0.42f, 0.52f),
                Accent = new Color(0.70f, 0.88f, 1f)
            },
            [EnemyArchetype.Wisp] = new EnemyDefinition
            {
                Type = EnemyArchetype.Wisp,
                Name = "Wisp",
                Health = 26f,
                Speed = 1.45f,
                Damage = 6f,
                Reward = 6,
                Body = new Color(0.62f, 0.84f, 1f),
                Accent = new Color(1f, 1f, 1f)
            },
            [EnemyArchetype.Boss] = new EnemyDefinition
            {
                Type = EnemyArchetype.Boss,
                Name = "Boss",
                Health = 320f,
                Speed = 0.78f,
                Damage = 28f,
                Reward = 48,
                Body = new Color(0.86f, 0.20f, 0.22f),
                Accent = new Color(1f, 0.84f, 0.34f)
            },
            [EnemyArchetype.Goblin] = new EnemyDefinition
            {
                Type = EnemyArchetype.Goblin,
                Name = "Goblin",
                Health = 20f,
                Speed = 1.55f,
                Damage = 5f,
                Reward = 4,
                Body = new Color(0.34f, 0.80f, 0.40f),
                Accent = new Color(0.98f, 0.94f, 0.36f)
            },
            [EnemyArchetype.Skeleton] = new EnemyDefinition
            {
                Type = EnemyArchetype.Skeleton,
                Name = "Skeleton",
                Health = 35f,
                Speed = 1.75f,
                Damage = 8f,
                Reward = 6,
                Body = new Color(0.78f, 0.78f, 0.78f),
                Accent = new Color(0.98f, 0.98f, 0.98f)
            },
            [EnemyArchetype.Orc] = new EnemyDefinition
            {
                Type = EnemyArchetype.Orc,
                Name = "Orc",
                Health = 60f,
                Speed = 1.15f,
                Damage = 12f,
                Reward = 9,
                Body = new Color(0.46f, 0.68f, 0.24f),
                Accent = new Color(0.80f, 0.96f, 0.34f)
            },
            [EnemyArchetype.Demon] = new EnemyDefinition
            {
                Type = EnemyArchetype.Demon,
                Name = "Demon",
                Health = 82f,
                Speed = 1.35f,
                Damage = 15f,
                Reward = 13,
                Body = new Color(0.90f, 0.32f, 0.24f),
                Accent = new Color(1f, 0.68f, 0.22f)
            },
            [EnemyArchetype.Dragon] = new EnemyDefinition
            {
                Type = EnemyArchetype.Dragon,
                Name = "Dragon",
                Health = 150f,
                Speed = 1.0f,
                Damage = 20f,
                Reward = 22,
                Body = new Color(0.84f, 0.40f, 0.18f),
                Accent = new Color(1f, 0.78f, 0.38f)
            },
            [EnemyArchetype.Shadow] = new EnemyDefinition
            {
                Type = EnemyArchetype.Shadow,
                Name = "Shadow",
                Health = 28f,
                Speed = 1.62f,
                Damage = 7f,
                Reward = 7,
                Body = new Color(0.22f, 0.28f, 0.34f),
                Accent = new Color(0.62f, 0.74f, 0.86f)
            },
            [EnemyArchetype.Slime] = new EnemyDefinition
            {
                Type = EnemyArchetype.Slime,
                Name = "Slime",
                Health = 36f,
                Speed = 1.05f,
                Damage = 6f,
                Reward = 6,
                Body = new Color(0.22f, 0.86f, 0.42f),
                Accent = new Color(0.86f, 1f, 0.78f)
            },
            [EnemyArchetype.Bat] = new EnemyDefinition
            {
                Type = EnemyArchetype.Bat,
                Name = "Bat",
                Health = 26f,
                Speed = 1.95f,
                Damage = 6f,
                Reward = 7,
                Body = new Color(0.30f, 0.18f, 0.44f),
                Accent = new Color(0.74f, 0.46f, 0.92f)
            },
            [EnemyArchetype.Spider] = new EnemyDefinition
            {
                Type = EnemyArchetype.Spider,
                Name = "Spider",
                Health = 44f,
                Speed = 1.22f,
                Damage = 10f,
                Reward = 8,
                Body = new Color(0.34f, 0.24f, 0.20f),
                Accent = new Color(0.62f, 0.50f, 0.42f)
            },
            [EnemyArchetype.GolemShard] = new EnemyDefinition
            {
                Type = EnemyArchetype.GolemShard,
                Name = "Golem Shard",
                Health = 95f,
                Speed = 0.92f,
                Damage = 14f,
                Reward = 12,
                Body = new Color(0.52f, 0.40f, 0.30f),
                Accent = new Color(0.80f, 0.68f, 0.54f)
            },
            [EnemyArchetype.FastSkeleton] = new EnemyDefinition
            {
                Type = EnemyArchetype.FastSkeleton,
                Name = "Fast Skeleton",
                Health = 25f,
                Speed = 2.15f,
                Damage = 6f,
                Reward = 7,
                Body = new Color(0.88f, 0.88f, 0.88f),
                Accent = new Color(1f, 1f, 1f)
            },
            [EnemyArchetype.ArmoredGolem] = new EnemyDefinition
            {
                Type = EnemyArchetype.ArmoredGolem,
                Name = "Armored Golem",
                Health = 128f,
                Speed = 0.82f,
                Damage = 18f,
                Reward = 16,
                Body = new Color(0.42f, 0.30f, 0.24f),
                Accent = new Color(0.76f, 0.60f, 0.46f)
            },
            [EnemyArchetype.MiniOrc] = new EnemyDefinition
            {
                Type = EnemyArchetype.MiniOrc,
                Name = "Mini Orc",
                Health = 26f,
                Speed = 1.30f,
                Damage = 7f,
                Reward = 4,
                Body = new Color(0.42f, 0.66f, 0.22f),
                Accent = new Color(0.76f, 0.92f, 0.34f)
            },
            [EnemyArchetype.MiniSkeleton] = new EnemyDefinition
            {
                Type = EnemyArchetype.MiniSkeleton,
                Name = "Mini Skeleton",
                Health = 20f,
                Speed = 1.72f,
                Damage = 5f,
                Reward = 4,
                Body = new Color(0.76f, 0.76f, 0.76f),
                Accent = new Color(0.94f, 0.94f, 0.94f)
            },
            [EnemyArchetype.MiniDemon] = new EnemyDefinition
            {
                Type = EnemyArchetype.MiniDemon,
                Name = "Mini Demon",
                Health = 30f,
                Speed = 1.42f,
                Damage = 8f,
                Reward = 5,
                Body = new Color(0.82f, 0.28f, 0.20f),
                Accent = new Color(0.98f, 0.62f, 0.26f)
            },
            [EnemyArchetype.MiniDragon] = new EnemyDefinition
            {
                Type = EnemyArchetype.MiniDragon,
                Name = "Mini Dragon",
                Health = 42f,
                Speed = 1.18f,
                Damage = 9f,
                Reward = 6,
                Body = new Color(0.78f, 0.38f, 0.16f),
                Accent = new Color(0.96f, 0.74f, 0.34f)
            }
        };

        public static readonly Dictionary<MapType, MapDefinition> Maps = new()
        {
            [MapType.Classic] = new MapDefinition
            {
                Type = MapType.Classic,
                Name = "Classic",
                Label = "Steady lane with clear reads.",
                BackgroundTop = new Color(0.05f, 0.14f, 0.22f),
                BackgroundBottom = new Color(0.03f, 0.05f, 0.10f),
                Surface = new Color(0.08f, 0.18f, 0.14f),
                PathOuter = new Color(0.28f, 0.20f, 0.12f),
                PathInner = new Color(0.54f, 0.42f, 0.26f),
                Accent = new Color(0.52f, 0.82f, 0.96f),
                DualLane = false
            },
            [MapType.Valley] = new MapDefinition
            {
                Type = MapType.Valley,
                Name = "Valley",
                Label = "Long curves and safer early setup.",
                BackgroundTop = new Color(0.06f, 0.12f, 0.18f),
                BackgroundBottom = new Color(0.04f, 0.06f, 0.10f),
                Surface = new Color(0.10f, 0.16f, 0.12f),
                PathOuter = new Color(0.24f, 0.20f, 0.14f),
                PathInner = new Color(0.50f, 0.40f, 0.24f),
                Accent = new Color(0.58f, 0.84f, 0.72f),
                DualLane = false
            },
            [MapType.Crossroads] = new MapDefinition
            {
                Type = MapType.Crossroads,
                Name = "Crossroads",
                Label = "Split pressure from multiple entries.",
                BackgroundTop = new Color(0.06f, 0.10f, 0.18f),
                BackgroundBottom = new Color(0.02f, 0.04f, 0.08f),
                Surface = new Color(0.09f, 0.14f, 0.12f),
                PathOuter = new Color(0.22f, 0.18f, 0.14f),
                PathInner = new Color(0.52f, 0.40f, 0.26f),
                Accent = new Color(0.96f, 0.84f, 0.40f),
                DualLane = true
            },
            [MapType.Desert] = new MapDefinition
            {
                Type = MapType.Desert,
                Name = "Desert",
                Label = "Warmer palette and open build zones.",
                BackgroundTop = new Color(0.66f, 0.82f, 0.92f),
                BackgroundBottom = new Color(0.78f, 0.66f, 0.38f),
                Surface = new Color(0.80f, 0.69f, 0.42f),
                PathOuter = new Color(0.60f, 0.50f, 0.31f),
                PathInner = new Color(0.76f, 0.66f, 0.43f),
                Accent = new Color(0.24f, 0.60f, 0.36f),
                DualLane = false
            },
            [MapType.Snow] = new MapDefinition
            {
                Type = MapType.Snow,
                Name = "Snow",
                Label = "Cold reads and bright tactical contrast.",
                BackgroundTop = new Color(0.24f, 0.34f, 0.42f),
                BackgroundBottom = new Color(0.08f, 0.12f, 0.18f),
                Surface = new Color(0.72f, 0.78f, 0.82f),
                PathOuter = new Color(0.42f, 0.48f, 0.56f),
                PathInner = new Color(0.70f, 0.74f, 0.80f),
                Accent = new Color(0.70f, 0.92f, 1f),
                DualLane = false
            }
        };

        public static readonly List<CampaignLevel> Campaign = new()
        {
            new CampaignLevel
            {
                Id = 1,
                Title = "The Basics",
                Hint = "Tap a tower button, then tap the field to place it.",
                StartingGold = 100,
                TargetWave = 3,
                EnemyHealthMultiplier = 0.60f,
                EnemyDamageMultiplier = 0.50f,
                EnemySpeedMultiplier = 0.80f,
                RewardMultiplier = 1.50f,
                GoldMultiplier = 1.50f,
                UpgradesEnabled = false,
                DiamondReward = 2,
                Map = MapType.Classic,
                AllowedTowers = new List<TowerType> { TowerType.Arrow },
                AllowedPowers = new List<PowerType>()
            },
            new CampaignLevel
            {
                Id = 2,
                Title = "Magic Touch",
                Hint = "Mix Arrow and Magic towers for best coverage.",
                StartingGold = 120,
                TargetWave = 4,
                EnemyHealthMultiplier = 0.70f,
                EnemyDamageMultiplier = 0.60f,
                EnemySpeedMultiplier = 0.85f,
                RewardMultiplier = 1.30f,
                GoldMultiplier = 1.30f,
                UpgradesEnabled = false,
                DiamondReward = 2,
                Map = MapType.Valley,
                AllowedTowers = new List<TowerType> { TowerType.Arrow, TowerType.Magic },
                AllowedPowers = new List<PowerType>()
            },
            new CampaignLevel
            {
                Id = 3,
                Title = "Heavy Artillery",
                Hint = "Cannons are expensive but powerful.",
                StartingGold = 150,
                TargetWave = 5,
                EnemyHealthMultiplier = 0.80f,
                EnemyDamageMultiplier = 0.70f,
                EnemySpeedMultiplier = 0.90f,
                RewardMultiplier = 1.20f,
                GoldMultiplier = 1.20f,
                UpgradesEnabled = false,
                DiamondReward = 3,
                Map = MapType.Crossroads,
                AllowedTowers = new List<TowerType> { TowerType.Arrow, TowerType.Magic, TowerType.Cannon },
                AllowedPowers = new List<PowerType>()
            },
            new CampaignLevel
            {
                Id = 4,
                Title = "Upgrades!",
                Hint = "Tap a tower to select it, then press upgrade.",
                StartingGold = 100,
                TargetWave = 5,
                EnemyHealthMultiplier = 0.90f,
                EnemyDamageMultiplier = 0.80f,
                EnemySpeedMultiplier = 0.90f,
                RewardMultiplier = 1.30f,
                GoldMultiplier = 1.30f,
                UpgradesEnabled = true,
                DiamondReward = 3,
                Map = MapType.Classic,
                AllowedTowers = new List<TowerType> { TowerType.Arrow, TowerType.Magic },
                AllowedPowers = new List<PowerType>()
            },
            new CampaignLevel
            {
                Id = 5,
                Title = "Boss Battle",
                Hint = "Every 5th wave is a boss wave. Prepare defenses.",
                StartingGold = 120,
                TargetWave = 5,
                EnemyHealthMultiplier = 0.80f,
                EnemyDamageMultiplier = 0.70f,
                EnemySpeedMultiplier = 0.90f,
                RewardMultiplier = 1.20f,
                GoldMultiplier = 1.20f,
                UpgradesEnabled = true,
                DiamondReward = 4,
                Map = MapType.Valley,
                AllowedTowers = new List<TowerType> { TowerType.Arrow, TowerType.Magic, TowerType.Cannon },
                AllowedPowers = new List<PowerType>()
            },
            new CampaignLevel
            {
                Id = 6,
                Title = "Toxic Strategy",
                Hint = "All tower types are available now, use slows for control.",
                StartingGold = 100,
                TargetWave = 7,
                EnemyHealthMultiplier = 0.90f,
                EnemyDamageMultiplier = 0.80f,
                EnemySpeedMultiplier = 1.00f,
                RewardMultiplier = 1.10f,
                GoldMultiplier = 1.10f,
                DiamondReward = 4,
                Map = MapType.Desert,
                AllowedTowers = new List<TowerType>(Towers.Keys),
                AllowedPowers = new List<PowerType>()
            },
            new CampaignLevel
            {
                Id = 7,
                Title = "Power Surge",
                Hint = "Fireball deals burst damage, Freeze buys time.",
                StartingGold = 120,
                TargetWave = 7,
                EnemyHealthMultiplier = 1.00f,
                EnemyDamageMultiplier = 0.90f,
                EnemySpeedMultiplier = 1.00f,
                RewardMultiplier = 1.20f,
                GoldMultiplier = 1.20f,
                DiamondReward = 4,
                Map = MapType.Valley,
                AllowedTowers = new List<TowerType> { TowerType.Arrow, TowerType.Magic, TowerType.Cannon },
                AllowedPowers = new List<PowerType> { PowerType.Fireball, PowerType.Freeze }
            },
            new CampaignLevel
            {
                Id = 8,
                Title = "Full Arsenal",
                Hint = "Lightning chains and Heal keeps the base alive.",
                StartingGold = 100,
                TargetWave = 8,
                EnemyHealthMultiplier = 1.00f,
                EnemyDamageMultiplier = 1.00f,
                EnemySpeedMultiplier = 1.00f,
                RewardMultiplier = 1.00f,
                GoldMultiplier = 1.00f,
                DiamondReward = 5,
                Map = MapType.Crossroads,
                AllowedTowers = new List<TowerType>(Towers.Keys),
                AllowedPowers = new List<PowerType>(Powers.Keys)
            },
            new CampaignLevel
            {
                Id = 9,
                Title = "The Horde",
                Hint = "Many enemies incoming. Build wide lane coverage.",
                StartingGold = 80,
                TargetWave = 10,
                EnemyHealthMultiplier = 0.80f,
                EnemyDamageMultiplier = 0.90f,
                EnemySpeedMultiplier = 1.00f,
                RewardMultiplier = 0.90f,
                GoldMultiplier = 0.90f,
                SpawnRateMultiplier = 1.50f,
                DiamondReward = 5,
                Map = MapType.Desert,
                AllowedTowers = new List<TowerType>(Towers.Keys),
                AllowedPowers = new List<PowerType>(Powers.Keys)
            },
            new CampaignLevel
            {
                Id = 10,
                Title = "Tower Budget",
                Hint = "Every coin matters. Upgrade when possible.",
                StartingGold = 30,
                TargetWave = 8,
                EnemyHealthMultiplier = 0.90f,
                EnemyDamageMultiplier = 0.90f,
                EnemySpeedMultiplier = 1.00f,
                RewardMultiplier = 0.60f,
                GoldMultiplier = 0.60f,
                DiamondReward = 5,
                Map = MapType.Classic,
                AllowedTowers = new List<TowerType>(Towers.Keys),
                AllowedPowers = new List<PowerType>(Powers.Keys)
            },
            new CampaignLevel
            {
                Id = 11,
                Title = "Speed Demons",
                Hint = "Use slows and freeze before enemies break through.",
                StartingGold = 100,
                TargetWave = 8,
                EnemyHealthMultiplier = 0.80f,
                EnemyDamageMultiplier = 0.80f,
                EnemySpeedMultiplier = 1.50f,
                RewardMultiplier = 1.10f,
                GoldMultiplier = 1.10f,
                DiamondReward = 6,
                Map = MapType.Snow,
                AllowedTowers = new List<TowerType>(Towers.Keys),
                AllowedPowers = new List<PowerType>(Powers.Keys)
            },
            new CampaignLevel
            {
                Id = 12,
                Title = "Iron Wall",
                Hint = "Armored enemies need concentrated damage.",
                StartingGold = 100,
                TargetWave = 10,
                EnemyHealthMultiplier = 1.50f,
                EnemyDamageMultiplier = 1.30f,
                EnemySpeedMultiplier = 0.90f,
                RewardMultiplier = 1.20f,
                GoldMultiplier = 1.20f,
                DiamondReward = 6,
                Map = MapType.Snow,
                AllowedTowers = new List<TowerType>(Towers.Keys),
                AllowedPowers = new List<PowerType>(Powers.Keys)
            },
            new CampaignLevel
            {
                Id = 13,
                Title = "Boss Rush",
                Hint = "Bosses arrive every 3 waves. Keep scaling damage.",
                StartingGold = 120,
                TargetWave = 9,
                EnemyHealthMultiplier = 1.10f,
                EnemyDamageMultiplier = 1.00f,
                EnemySpeedMultiplier = 1.00f,
                RewardMultiplier = 1.30f,
                GoldMultiplier = 1.30f,
                DiamondReward = 7,
                Map = MapType.Crossroads,
                AllowedTowers = new List<TowerType>(Towers.Keys),
                AllowedPowers = new List<PowerType>(Powers.Keys)
            },
            new CampaignLevel
            {
                Id = 14,
                Title = "Arrows Only",
                Hint = "Boost your arrow towers and time powers carefully.",
                StartingGold = 150,
                TargetWave = 10,
                EnemyHealthMultiplier = 0.90f,
                EnemyDamageMultiplier = 0.80f,
                EnemySpeedMultiplier = 1.00f,
                RewardMultiplier = 1.30f,
                GoldMultiplier = 1.30f,
                DiamondReward = 7,
                Map = MapType.Classic,
                AllowedTowers = new List<TowerType> { TowerType.Arrow },
                AllowedPowers = new List<PowerType>(Powers.Keys)
            },
            new CampaignLevel
            {
                Id = 15,
                Title = "Night Raid",
                Hint = "Night boosts enemies and shrinks tower range.",
                StartingGold = 100,
                TargetWave = 8,
                EnemyHealthMultiplier = 1.20f,
                EnemyDamageMultiplier = 1.10f,
                EnemySpeedMultiplier = 1.00f,
                RewardMultiplier = 1.20f,
                GoldMultiplier = 1.20f,
                DiamondReward = 6,
                Map = MapType.Snow,
                AllowedTowers = new List<TowerType>(Towers.Keys),
                AllowedPowers = new List<PowerType>(Powers.Keys)
            },
            new CampaignLevel
            {
                Id = 16,
                Title = "Swarm Tactics",
                Hint = "Huge counts of weaker enemies. Splash towers shine.",
                StartingGold = 100,
                TargetWave = 8,
                EnemyHealthMultiplier = 0.50f,
                EnemyDamageMultiplier = 0.70f,
                EnemySpeedMultiplier = 1.00f,
                RewardMultiplier = 1.10f,
                GoldMultiplier = 1.10f,
                SpawnRateMultiplier = 2.00f,
                DiamondReward = 6,
                Map = MapType.Valley,
                AllowedTowers = new List<TowerType>(Towers.Keys),
                AllowedPowers = new List<PowerType>(Powers.Keys)
            },
            new CampaignLevel
            {
                Id = 17,
                Title = "No Powers",
                Hint = "No active powers. Tower placement must carry you.",
                StartingGold = 120,
                TargetWave = 8,
                EnemyHealthMultiplier = 0.90f,
                EnemyDamageMultiplier = 0.90f,
                EnemySpeedMultiplier = 1.00f,
                RewardMultiplier = 1.20f,
                GoldMultiplier = 1.20f,
                DiamondReward = 7,
                Map = MapType.Desert,
                AllowedTowers = new List<TowerType>(Towers.Keys),
                AllowedPowers = new List<PowerType>()
            },
            new CampaignLevel
            {
                Id = 18,
                Title = "Elite Forces",
                Hint = "Focus elite targets first for better survival tempo.",
                StartingGold = 100,
                TargetWave = 10,
                EnemyHealthMultiplier = 1.10f,
                EnemyDamageMultiplier = 1.00f,
                EnemySpeedMultiplier = 1.00f,
                RewardMultiplier = 1.30f,
                GoldMultiplier = 1.30f,
                DiamondReward = 8,
                Map = MapType.Crossroads,
                AllowedTowers = new List<TowerType>(Towers.Keys),
                AllowedPowers = new List<PowerType>(Powers.Keys)
            },
            new CampaignLevel
            {
                Id = 19,
                Title = "Gauntlet",
                Hint = "Long mission: plan upgrades and economy in advance.",
                StartingGold = 80,
                TargetWave = 12,
                EnemyHealthMultiplier = 1.20f,
                EnemyDamageMultiplier = 1.10f,
                EnemySpeedMultiplier = 1.10f,
                RewardMultiplier = 1.00f,
                GoldMultiplier = 1.00f,
                SpawnRateMultiplier = 1.30f,
                DiamondReward = 9,
                Map = MapType.Crossroads,
                AllowedTowers = new List<TowerType>(Towers.Keys),
                AllowedPowers = new List<PowerType>(Powers.Keys)
            },
            new CampaignLevel
            {
                Id = 20,
                Title = "Final Stand",
                Hint = "Everything is cranked up. Bring your strongest build.",
                StartingGold = 80,
                TargetWave = 15,
                EnemyHealthMultiplier = 1.40f,
                EnemyDamageMultiplier = 1.30f,
                EnemySpeedMultiplier = 1.10f,
                RewardMultiplier = 0.90f,
                GoldMultiplier = 0.90f,
                SpawnRateMultiplier = 1.20f,
                DiamondReward = 10,
                Map = MapType.Snow,
                AllowedTowers = new List<TowerType>(Towers.Keys),
                AllowedPowers = new List<PowerType>(Powers.Keys)
            }
        };
    }
}
