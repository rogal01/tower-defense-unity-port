using System.Collections.Generic;
using UnityEngine;

namespace TowerDefensePort
{
    public enum TDWaveModifier
    {
        None,
        Fast,
        Armored,
        Regen,
        Swarm,
        Invisible,
        Rich,
        BossRally
    }

    public enum TDBossAbility
    {
        Charge,
        Summon,
        Heal,
        AoeDamage,
        Shield,
        Roar,
        Teleport,
        Drain,
        Quake,
        Split
    }

    public enum TDBossType
    {
        OrcKing,
        LichLord,
        DemonPrince,
        DragonQueen,
        ShadowWraith,
        SlimeKing,
        VampireLord,
        SpiderQueen,
        FrostTitan,
        StoneGolem
    }

    public sealed class TDBossDefinition
    {
        public TDBossType Type;
        public string Name = "";
        public string Emoji = "";
        public Color Color;
        public EnemyArchetype MinionType;
        public int MinionCount;
        public float BaseHp;
        public float BaseSpeed;
        public float BaseGold;
        public float BaseDamage;
        public TDBossAbility Ability;
    }

    public sealed class TDAchievementDefinition
    {
        public string Id = "";
        public string Title = "";
        public string Description = "";

        public TDAchievementDefinition()
        {
        }

        public TDAchievementDefinition(string id, string title, string description)
        {
            Id = id;
            Title = title;
            Description = description;
        }
    }

    public static class TDLegacyData
    {
        public static readonly Dictionary<TDBossType, TDBossDefinition> Bosses = new()
        {
            [TDBossType.OrcKing] = new TDBossDefinition
            {
                Type = TDBossType.OrcKing,
                Name = "Orc King",
                Emoji = "\uD83D\uDC79",
                Color = new Color(0.20f, 0.42f, 0.12f),
                MinionType = EnemyArchetype.MiniOrc,
                MinionCount = 6,
                BaseHp = 700f,
                BaseSpeed = 1.05f,
                BaseGold = 120f,
                BaseDamage = 45f,
                Ability = TDBossAbility.Charge
            },
            [TDBossType.LichLord] = new TDBossDefinition
            {
                Type = TDBossType.LichLord,
                Name = "Lich Lord",
                Emoji = "\uD83D\uDC80",
                Color = new Color(0.42f, 0.10f, 0.60f),
                MinionType = EnemyArchetype.MiniSkeleton,
                MinionCount = 8,
                BaseHp = 600f,
                BaseSpeed = 1.30f,
                BaseGold = 100f,
                BaseDamage = 35f,
                Ability = TDBossAbility.Summon
            },
            [TDBossType.DemonPrince] = new TDBossDefinition
            {
                Type = TDBossType.DemonPrince,
                Name = "Demon Prince",
                Emoji = "\uD83D\uDE08",
                Color = new Color(0.70f, 0.12f, 0.12f),
                MinionType = EnemyArchetype.MiniDemon,
                MinionCount = 5,
                BaseHp = 900f,
                BaseSpeed = 1.15f,
                BaseGold = 150f,
                BaseDamage = 55f,
                Ability = TDBossAbility.AoeDamage
            },
            [TDBossType.DragonQueen] = new TDBossDefinition
            {
                Type = TDBossType.DragonQueen,
                Name = "Dragon Queen",
                Emoji = "\uD83D\uDC32",
                Color = new Color(0.90f, 0.40f, 0.04f),
                MinionType = EnemyArchetype.MiniDragon,
                MinionCount = 4,
                BaseHp = 1100f,
                BaseSpeed = 0.95f,
                BaseGold = 200f,
                BaseDamage = 60f,
                Ability = TDBossAbility.Roar
            },
            [TDBossType.ShadowWraith] = new TDBossDefinition
            {
                Type = TDBossType.ShadowWraith,
                Name = "Shadow Wraith",
                Emoji = "\uD83D\uDC7B",
                Color = new Color(0.16f, 0.22f, 0.26f),
                MinionType = EnemyArchetype.Shadow,
                MinionCount = 10,
                BaseHp = 550f,
                BaseSpeed = 1.52f,
                BaseGold = 90f,
                BaseDamage = 30f,
                Ability = TDBossAbility.Teleport
            },
            [TDBossType.SlimeKing] = new TDBossDefinition
            {
                Type = TDBossType.SlimeKing,
                Name = "Slime King",
                Emoji = "\uD83E\uDDA0",
                Color = new Color(0.00f, 0.90f, 0.40f),
                MinionType = EnemyArchetype.Slime,
                MinionCount = 12,
                BaseHp = 800f,
                BaseSpeed = 0.82f,
                BaseGold = 110f,
                BaseDamage = 25f,
                Ability = TDBossAbility.Split
            },
            [TDBossType.VampireLord] = new TDBossDefinition
            {
                Type = TDBossType.VampireLord,
                Name = "Vampire Lord",
                Emoji = "\uD83E\uDDDB",
                Color = new Color(0.52f, 0.06f, 0.30f),
                MinionType = EnemyArchetype.Bat,
                MinionCount = 8,
                BaseHp = 750f,
                BaseSpeed = 1.42f,
                BaseGold = 130f,
                BaseDamage = 40f,
                Ability = TDBossAbility.Drain
            },
            [TDBossType.SpiderQueen] = new TDBossDefinition
            {
                Type = TDBossType.SpiderQueen,
                Name = "Spider Queen",
                Emoji = "\uD83D\uDD77",
                Color = new Color(0.22f, 0.16f, 0.12f),
                MinionType = EnemyArchetype.Spider,
                MinionCount = 7,
                BaseHp = 650f,
                BaseSpeed = 1.22f,
                BaseGold = 100f,
                BaseDamage = 35f,
                Ability = TDBossAbility.Summon
            },
            [TDBossType.FrostTitan] = new TDBossDefinition
            {
                Type = TDBossType.FrostTitan,
                Name = "Frost Titan",
                Emoji = "\u2744",
                Color = new Color(0.04f, 0.56f, 0.82f),
                MinionType = EnemyArchetype.Wisp,
                MinionCount = 6,
                BaseHp = 1200f,
                BaseSpeed = 0.72f,
                BaseGold = 180f,
                BaseDamage = 50f,
                Ability = TDBossAbility.Quake
            },
            [TDBossType.StoneGolem] = new TDBossDefinition
            {
                Type = TDBossType.StoneGolem,
                Name = "Stone Golem",
                Emoji = "\uD83E\uDEA8",
                Color = new Color(0.36f, 0.25f, 0.22f),
                MinionType = EnemyArchetype.GolemShard,
                MinionCount = 5,
                BaseHp = 1400f,
                BaseSpeed = 0.60f,
                BaseGold = 220f,
                BaseDamage = 65f,
                Ability = TDBossAbility.Shield
            }
        };

        public static readonly List<TDAchievementDefinition> Achievements = new()
        {
            new TDAchievementDefinition("first_kill", "First Blood", "Kill your first enemy"),
            new TDAchievementDefinition("wave_5", "Survivor", "Reach wave 5"),
            new TDAchievementDefinition("wave_10", "Veteran", "Reach wave 10"),
            new TDAchievementDefinition("wave_20", "Legend", "Reach wave 20"),
            new TDAchievementDefinition("wave_30", "Immortal", "Reach wave 30"),
            new TDAchievementDefinition("wave_50", "Mythic", "Reach wave 50"),
            new TDAchievementDefinition("kills_50", "Slayer", "Kill 50 enemies"),
            new TDAchievementDefinition("kills_200", "Destroyer", "Kill 200 enemies"),
            new TDAchievementDefinition("kills_500", "Annihilator", "Kill 500 enemies"),
            new TDAchievementDefinition("combo_10", "Combo King", "Get a 10x combo"),
            new TDAchievementDefinition("combo_20", "Combo God", "Get a 20x combo"),
            new TDAchievementDefinition("boss_kill", "Boss Slayer", "Kill your first boss"),
            new TDAchievementDefinition("5_bosses", "Boss Hunter", "Kill 5 bosses in one run"),
            new TDAchievementDefinition("10_bosses", "Boss Legend", "Kill 10 bosses in one run"),
            new TDAchievementDefinition("5_towers", "Architect", "Place 5 towers"),
            new TDAchievementDefinition("10_towers", "Fortress", "Place 10 towers"),
            new TDAchievementDefinition("all_tower_types", "Arsenal", "Place all tower types"),
            new TDAchievementDefinition("use_power", "Sorcerer", "Use a power for the first time"),
            new TDAchievementDefinition("max_tower", "Master Builder", "Upgrade a tower to level 5"),
            new TDAchievementDefinition("rich", "Rich", "Have 500 gold at once"),
            new TDAchievementDefinition("rich_1000", "Millionaire", "Have 1000 gold at once"),
            new TDAchievementDefinition("gold_hoarder", "Gold Hoarder", "Have 2000 gold at once"),
            new TDAchievementDefinition("diamond_10", "Diamond Hoarder", "Earn 10 diamonds in a run"),
            new TDAchievementDefinition("diamond_50", "Diamond Mine", "Earn 50 diamonds in a run"),
            new TDAchievementDefinition("score_1000", "Score Chaser", "Reach 1000 score"),
            new TDAchievementDefinition("repaired_3", "Mechanic", "Repair the base 3 times in a run"),
            new TDAchievementDefinition("upgrade_all", "Well Rounded", "Buy all 4 player upgrades"),
            new TDAchievementDefinition("endless_10", "Endurance", "Reach wave 10 in endless mode"),
            new TDAchievementDefinition("kills_1000", "Genocide", "Kill 1000 enemies in one run"),
            new TDAchievementDefinition("wave_100", "Centurion", "Reach wave 100"),
            new TDAchievementDefinition("no_damage", "Untouchable", "Complete a wave without base damage"),
            new TDAchievementDefinition("speed_demon", "Speed Demon", "Reach wave 10 at 3x speed"),
            new TDAchievementDefinition("all_powers", "Elementalist", "Use all 4 powers in one run"),
            new TDAchievementDefinition("survivor_1hp", "Last Stand", "Win a wave with base at 1 HP"),
            new TDAchievementDefinition("campaign_5", "Campaigner", "Complete 5 campaign levels"),
            new TDAchievementDefinition("campaign_10", "Strategist", "Complete 10 campaign levels"),
            new TDAchievementDefinition("campaign_all", "Conqueror", "Complete all campaign levels"),
            new TDAchievementDefinition("campaign_no_damage", "Flawless", "Beat a campaign level without base damage"),
            new TDAchievementDefinition("campaign_3star", "Perfectionist", "Beat 5 campaign levels at full base HP")
        };

        public static readonly List<EnemyArchetype> RegularSpawnPool = new()
        {
            EnemyArchetype.Goblin,
            EnemyArchetype.Skeleton,
            EnemyArchetype.Orc,
            EnemyArchetype.Demon,
            EnemyArchetype.Dragon,
            EnemyArchetype.FastSkeleton,
            EnemyArchetype.ArmoredGolem
        };
    }
}
