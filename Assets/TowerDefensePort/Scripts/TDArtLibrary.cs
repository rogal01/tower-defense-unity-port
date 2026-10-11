using System.Collections.Generic;
using UnityEngine;

namespace TowerDefensePort
{
    internal static class TDArtLibrary
    {
        private static readonly Dictionary<string, Sprite?> Cache = new();

        private static string TileName(int id)
        {
            return "towerDefense_tile" + id.ToString("D3");
        }

        private static Sprite? LoadSprite(string resourcesPath)
        {
            if (Cache.TryGetValue(resourcesPath, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>(resourcesPath);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(resourcesPath);
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        64f);
                }
            }

            Cache[resourcesPath] = sprite;
            return sprite;
        }

        public static Sprite? GetTowerSprite(TowerType type)
        {
            var id = 291;
            switch (type)
            {
                case TowerType.Arrow:
                    id = 291;
                    break;
                case TowerType.Magic:
                    id = 292;
                    break;
                case TowerType.Cannon:
                    id = 250;
                    break;
                case TowerType.Poison:
                    id = 249;
                    break;
                case TowerType.Tesla:
                    id = 203;
                    break;
                case TowerType.Ice:
                    id = 226;
                    break;
                case TowerType.Flame:
                    id = 204;
                    break;
            }

            return LoadSprite("TDArt/Towers/" + TileName(id));
        }

        public static float GetTowerSpriteScale(TowerType type)
        {
            switch (type)
            {
                case TowerType.Arrow:
                    return 0.72f;
                case TowerType.Magic:
                    return 0.74f;
                case TowerType.Cannon:
                    return 0.90f;
                case TowerType.Poison:
                    return 0.82f;
                case TowerType.Tesla:
                    return 0.82f;
                case TowerType.Ice:
                    return 0.82f;
                case TowerType.Flame:
                    return 0.82f;
                default:
                    return 0.80f;
            }
        }

        public static float GetTowerSpriteYOffset(TowerType type)
        {
            switch (type)
            {
                case TowerType.Cannon:
                    return 0.14f;
                default:
                    return 0.18f;
            }
        }

        public static bool TowerUsesAimRotation(TowerType type)
        {
            return type == TowerType.Arrow || type == TowerType.Cannon || type == TowerType.Tesla || type == TowerType.Flame;
        }

        public static Sprite? GetEnemySprite(EnemyArchetype type, bool isBoss)
        {
            if (isBoss)
            {
                return LoadSprite("TDArt/Enemies/" + TileName(250));
            }

            var id = 245;
            switch (type)
            {
                case EnemyArchetype.Goblin:
                    id = 245;
                    break;
                case EnemyArchetype.Skeleton:
                case EnemyArchetype.FastSkeleton:
                case EnemyArchetype.MiniSkeleton:
                    id = 248;
                    break;
                case EnemyArchetype.Orc:
                case EnemyArchetype.MiniOrc:
                case EnemyArchetype.Brute:
                    id = 247;
                    break;
                case EnemyArchetype.Demon:
                case EnemyArchetype.MiniDemon:
                    id = 246;
                    break;
                case EnemyArchetype.Dragon:
                case EnemyArchetype.MiniDragon:
                case EnemyArchetype.Bat:
                    id = 270;
                    break;
                case EnemyArchetype.Shadow:
                case EnemyArchetype.Spider:
                case EnemyArchetype.Wisp:
                    id = 271;
                    break;
                case EnemyArchetype.Slime:
                    id = 130;
                    break;
                case EnemyArchetype.GolemShard:
                case EnemyArchetype.ArmoredGolem:
                    id = 181;
                    break;
            }

            return LoadSprite("TDArt/Enemies/" + TileName(id));
        }

        public static float GetEnemySpriteScale(EnemyArchetype type, bool isBoss)
        {
            if (isBoss)
            {
                return 0.86f;
            }

            switch (type)
            {
                case EnemyArchetype.Slime:
                    return 0.70f;
                case EnemyArchetype.Bat:
                case EnemyArchetype.Dragon:
                case EnemyArchetype.MiniDragon:
                    return 0.74f;
                case EnemyArchetype.Brute:
                case EnemyArchetype.ArmoredGolem:
                case EnemyArchetype.GolemShard:
                    return 0.88f;
                default:
                    return 0.78f;
            }
        }

        public static float GetEnemySpriteYOffset(EnemyArchetype type, bool isBoss)
        {
            if (isBoss)
            {
                return 0.10f;
            }

            switch (type)
            {
                case EnemyArchetype.Slime:
                    return 0.02f;
                case EnemyArchetype.Bat:
                case EnemyArchetype.Dragon:
                case EnemyArchetype.MiniDragon:
                    return 0.08f;
                default:
                    return 0.06f;
            }
        }

        public static Sprite? GetPropSprite(MapType map, int index)
        {
            int[] ids;
            switch (map)
            {
                case MapType.Desert:
                    ids = new[] { 132, 135, 136, 137, 192, 266 };
                    break;
                case MapType.Snow:
                    ids = new[] { 135, 136, 137, 266, 268, 269 };
                    break;
                case MapType.Volcano:
                    ids = new[] { 134, 135, 136, 268, 269, 192 };
                    break;
                default:
                    ids = new[] { 131, 132, 134, 135, 136, 137 };
                    break;
            }

            var id = ids[Mathf.Abs(index) % ids.Length];
            return LoadSprite("TDArt/Props/" + TileName(id));
        }

        public static Sprite? GetPathDecalSprite(int index)
        {
            var ids = new[] { 192, 266 };
            var id = ids[Mathf.Abs(index) % ids.Length];
            return LoadSprite("TDArt/Props/" + TileName(id));
        }
    }
}
