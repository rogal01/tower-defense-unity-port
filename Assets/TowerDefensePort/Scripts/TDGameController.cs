using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#nullable enable

namespace TowerDefensePort
{
    public sealed class TDGameController : MonoBehaviour
    {
        private enum RuntimeState
        {
            Menu,
            Playing,
            Overlay
        }

        private const float WorldHalfHeight = 8.5f;
        private const float WorldHalfWidth = 4.7f;
        private const float TowerCritChance = 0.12f;
        private const float TowerCritMultiplier = 2f;
        private const string SavePrefix = "td_save_";

        private sealed class SkillNode
        {
            public readonly string Id;
            public readonly string Name;
            public readonly string Description;
            public readonly int MaxLevel;
            public readonly int BaseCost;
            public readonly int CostPerLevel;
            public readonly bool PrestigePage;

            public SkillNode(string id, string name, string description, int maxLevel, int baseCost, int costPerLevel, bool prestigePage)
            {
                Id = id;
                Name = name;
                Description = description;
                MaxLevel = maxLevel;
                BaseCost = baseCost;
                CostPerLevel = costPerLevel;
                PrestigePage = prestigePage;
            }
        }

        private static readonly List<SkillNode> SkillNodes = new()
        {
            new SkillNode("start_gold", "Golden Start", "+15 starting gold per level", 5, 3, 2, false),
            new SkillNode("base_hp", "Fortified Base", "+20 base HP per level", 5, 4, 3, false),
            new SkillNode("player_damage", "Sharp Blade", "+3 player damage per level", 5, 5, 3, false),
            new SkillNode("player_speed", "Swift Feet", "+20 move speed per level", 5, 3, 2, false),
            new SkillNode("player_hp", "Tough Skin", "+15 player HP per level", 5, 4, 3, false),
            new SkillNode("tower_damage", "Tower Mastery", "+8% tower damage per level", 5, 6, 4, false),
            new SkillNode("gold_bonus", "Treasure Hunter", "+10% gold from kills per level", 5, 5, 3, false),
            new SkillNode("diamond_luck", "Diamond Magnet", "+5% diamond chance per level", 3, 8, 6, false),
            new SkillNode("wave_bonus", "War Veteran", "+3 wave bonus gold per level", 5, 4, 3, false),
            new SkillNode("attack_range", "Eagle Eye", "+15 attack range per level", 4, 5, 4, false),
            new SkillNode("ice_power", "Frost Mastery", "+10% ice slow per level", 5, 6, 4, true),
            new SkillNode("ability_cd", "Quick Cast", "-5% tower ability cooldown per level", 5, 5, 3, true),
            new SkillNode("sell_bonus", "Haggler", "+10% tower sell value per level", 5, 4, 2, true),
            new SkillNode("resist_pierce", "Armor Break", "+5% resistance pierce per level", 5, 7, 5, true),
            new SkillNode("wave_modifier", "Lucky Waves", "Better wave modifier chance", 3, 8, 6, true),
            new SkillNode("prestige_gold", "Midas Touch", "+5% gold from kills per level", 5, 5, 4, true)
        };

        private Camera _gameCamera = null!;
        private Canvas _canvas = null!;
        private Transform _worldRoot = null!;

        private GameObject? _menuRoot;
        private GameObject? _hudRoot;
        private GameObject? _overlayRoot;
        private GameObject? _mapButtonRoot;
        private GameObject? _selectionPanel;

        private Text? _goldText;
        private Text? _waveText;
        private Text? _scoreText;
        private Text? _hpText;
        private Text? _bannerText;
        private Text? _hintText;
        private Text? _selectedTowerText;
        private Text? _selectedTowerStatsText;
        private Text? _playerHpText;
        private Button? _upgradeButton;
        private Button? _specializeAlphaButton;
        private Button? _specializeBetaButton;
        private Button? _sellButton;
        private Button? _speedButton;
        private Button? _dashButton;
        private Button? _campaignButton;
        private Button? _endlessButton;
        private Button? _continueButton;
        private Button? _skillTreeButton;
        private Button? _upgradePlayerDamageButton;
        private Button? _upgradePlayerSpeedButton;
        private Button? _upgradePlayerHpButton;
        private Button? _upgradeBaseHpButton;

        private readonly Dictionary<TowerType, Button> _towerButtons = new();
        private readonly Dictionary<PowerType, Button> _powerButtons = new();
        private readonly Dictionary<MapType, Button> _mapButtons = new();
        private readonly Dictionary<PowerType, float> _powerCooldowns = new();
        private readonly List<TowerActor> _towers = new();
        private readonly List<EnemyActor> _enemies = new();
        private readonly List<ProjectileVisual> _projectiles = new();
        private readonly List<FootprintDecal> _footprints = new();
        private readonly List<List<Vector2>> _paths = new();
        private readonly List<TDBossType> _bossPool = new();
        private readonly List<TDWaveModifier> _dailyModifierRotation = new();
        private readonly HashSet<PowerType> _powersUsedThisRun = new();
        private readonly HashSet<TowerType> _towerTypesPlacedThisRun = new();
        private System.Random _runRng = new();
        private int _runSeed;

        private RuntimeState _state = RuntimeState.Menu;
        private CampaignLevel? _campaignLevel;
        private GameMode _mode;
        private MapType _selectedMenuMap = MapType.Classic;
        private MapType _activeMap = MapType.Classic;
        private List<TowerType> _allowedTowers = new();
        private List<PowerType> _allowedPowers = new();
        private TowerType? _placingTowerType;
        private PowerType? _armedPowerType;
        private TowerActor? _selectedTower;
        private Queue<SpawnRequest> _spawnQueue = new();
        private Vector2 _basePosition = new(0f, -7.2f);
        private TDWaveModifier _currentWaveModifier = TDWaveModifier.None;
        private TDBossType? _currentWaveBoss;
        private bool _isNight;
        private bool _upgradesEnabled = true;
        private bool _baseDamagedThisWave;
        private bool _baseDamagedThisRun;
        private int _killsThisRun;
        private int _bossesKilledThisRun;
        private int _diamondsThisRun;
        private int _repairsThisRun;
        private int _goldEarnedThisRun;
        private int _comboCount;
        private int _bestCombo;
        private int _totalEnemiesThisWave;
        private int _enemiesSpawnedThisWave;
        private float _comboTimer;
        private float _comboMultiplier = 1f;

        private int _gold;
        private int _score;
        private int _wave;
        private int _baseHp;
        private int _maxBaseHp = 100;
        private float _gameSpeed = 1f;
        private float _intermissionTimer;
        private float _spawnTimer;
        private float _bannerTimer;
        private bool _overlayCanResume;
        private string _defaultHint = "Build your defense.";
        private int _skillTreePage;
        private bool _loadingSavedRun;

        private GameObject? _playerRoot;
        private SpriteRenderer? _playerBody;
        private SpriteRenderer? _playerCore;
        private Transform? _playerHpFill;
        private Transform? _playerDashFill;
        private Vector2 _playerPosition;
        private Vector2 _playerTarget;
        private float _playerSpeed = 3f;
        private float _playerAttackRange = 1.8f;
        private float _playerAttackDamage = 10f;
        private float _playerAttackTimer;
        private float _playerAttackCooldown = 0.50f;
        private float _playerHp = 100f;
        private float _playerMaxHp = 100f;
        private float _dashCooldown;
        private readonly float _dashCooldownMax = 8f;
        private readonly float _dashRange = 2.8f;
        private bool _dashArmed;
        private int _playerDamageLevel = 1;
        private int _playerSpeedLevel = 1;
        private int _playerHpLevel = 1;
        private int _baseHpLevel = 1;

        private int _prestigeLevel;
        private float _skillTowerDamageMult = 1f;
        private float _skillGoldBonusMult = 1f;
        private float _skillDiamondLuckBonus;
        private int _skillWaveGoldBonus;
        private float _skillIceSlowBonus;
        private float _skillAbilityCooldownReduction;
        private float _skillSellBonus;
        private float _skillResistancePierce;
        private float _skillLuckyWaveBonus;

        private void Awake()
        {
            Application.targetFrameRate = 120;
            TDVisuals.EnsureEventSystem();
            EnsureCameraAndCanvas();
            BuildMenu();
        }

        private void Update()
        {
            if (_state == RuntimeState.Playing)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ShowPauseOverlay();
                    return;
                }

                var dt = Time.deltaTime * _gameSpeed;
                UpdateRun(dt);
                HandleGameplayPointer();
                RefreshHud();
            }
            else if (_state == RuntimeState.Overlay && _overlayCanResume && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseOverlayToGame();
            }
        }

        private void EnsureCameraAndCanvas()
        {
            _gameCamera = Camera.main;
            if (_gameCamera == null)
            {
                var cameraGo = new GameObject("Main Camera", typeof(Camera));
                cameraGo.tag = "MainCamera";
                _gameCamera = cameraGo.GetComponent<Camera>();
            }

            _gameCamera.orthographic = true;
            _gameCamera.orthographicSize = WorldHalfHeight;
            _gameCamera.transform.position = new Vector3(0f, 0f, -10f);
            _gameCamera.clearFlags = CameraClearFlags.SolidColor;
            _gameCamera.backgroundColor = new Color(0.03f, 0.05f, 0.10f);

            _canvas = TDVisuals.CreateCanvas();
            _canvas.transform.SetParent(transform, false);

            _worldRoot = new GameObject("WorldRoot").transform;
            _worldRoot.SetParent(transform, false);
        }

        private void BuildMenu()
        {
            ClearRuntime();
            _state = RuntimeState.Menu;
            _placingTowerType = null;
            _armedPowerType = null;
            _selectedTower = null;

            var map = TDData.Maps[_selectedMenuMap];
            _gameCamera.backgroundColor = map.BackgroundBottom;

            _menuRoot = new GameObject("MenuRoot", typeof(RectTransform));
            _menuRoot.transform.SetParent(_canvas.transform, false);
            var menuRect = _menuRoot.GetComponent<RectTransform>();
            SetRect(menuRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var backdrop = TDVisuals.CreateUiBlock(
                _menuRoot.transform,
                "Backdrop",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                map.BackgroundBottom);
            var backdropImage = backdrop.GetComponent<Image>();
            backdropImage.color = map.BackgroundBottom;

            TDVisuals.CreateUiBlock(
                _menuRoot.transform,
                "GlowTop",
                new Vector2(0f, 0.62f),
                new Vector2(1f, 1f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Color(map.BackgroundTop.r, map.BackgroundTop.g, map.BackgroundTop.b, 0.95f));

            TDVisuals.CreateUiBlock(
                _menuRoot.transform,
                "AccentBar",
                new Vector2(0.08f, 0.93f),
                new Vector2(0.92f, 0.935f),
                Vector2.zero,
                Vector2.zero,
                map.Accent);

            var frame = TDVisuals.CreateUiBlock(
                _menuRoot.transform,
                "Frame",
                new Vector2(0.05f, 0.05f),
                new Vector2(0.95f, 0.92f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.04f, 0.08f, 0.14f, 0.84f));

            var frameBorder = TDVisuals.CreateUiBlock(
                frame.transform,
                "InnerGlow",
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(16f, 16f),
                new Vector2(-16f, -16f),
                new Color(map.Accent.r, map.Accent.g, map.Accent.b, 0.08f));

            var header = TDVisuals.CreateText(
                frame.transform,
                "Header",
                "Aegis Line",
                78,
                TextAnchor.UpperLeft,
                Color.white);
            SetRect(header.rectTransform, new Vector2(0.07f, 0.73f), new Vector2(0.58f, 0.92f), Vector2.zero, Vector2.zero);
            header.fontStyle = FontStyle.Bold;

            var subHeader = TDVisuals.CreateText(
                frame.transform,
                "SubHeader",
                "Stylized Unity runtime port of the Godot tower defense. Pick a battlefield, bring a loadout, and hold the line.",
                28,
                TextAnchor.UpperLeft,
                new Color(0.78f, 0.86f, 0.94f));
            SetRect(subHeader.rectTransform, new Vector2(0.07f, 0.63f), new Vector2(0.58f, 0.76f), Vector2.zero, Vector2.zero);

            BuildMenuStats(frame.GetComponent<RectTransform>());
            BuildModeCards(frame.GetComponent<RectTransform>());
            BuildMapSelector(frame.GetComponent<RectTransform>());
            BuildMenuMeta(frame.GetComponent<RectTransform>());

            var footer = TDVisuals.CreateText(
                frame.transform,
                "Footer",
                "Runtime-built UI, world, waves, and combat. Open any scene and press Play.",
                22,
                TextAnchor.LowerLeft,
                new Color(0.62f, 0.72f, 0.82f));
            SetRect(footer.rectTransform, new Vector2(0.07f, 0.04f), new Vector2(0.7f, 0.1f), Vector2.zero, Vector2.zero);

            _ = frameBorder;
        }

        private void BuildMenuMeta(RectTransform parent)
        {
            var metaPanel = TDVisuals.CreateUiBlock(
                parent,
                "MetaPanel",
                new Vector2(0.63f, 0.05f),
                new Vector2(0.93f, 0.14f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.07f, 0.12f, 0.18f, 0.94f));

            var diamonds = PlayerPrefs.GetInt("td_diamonds", 0);
            var prestige = PlayerPrefs.GetInt("td_prestige_level", 0);
            var status = TDVisuals.CreateText(metaPanel.transform, "Status", "Diamonds " + diamonds + "  |  Prestige " + prestige, 20, TextAnchor.UpperLeft, new Color(0.84f, 0.92f, 0.98f));
            SetRect(status.rectTransform, new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);

            _continueButton = TDVisuals.CreateButton(metaPanel.transform, "ContinueButton", "Continue", new Color(0.28f, 0.54f, 0.72f), Color.white);
            SetRect(_continueButton.GetComponent<RectTransform>(), new Vector2(0.04f, 0.08f), new Vector2(0.47f, 0.52f), Vector2.zero, Vector2.zero);
            _continueButton.onClick.AddListener(ContinueSavedRun);
            _continueButton.interactable = HasSavedRun();
            _continueButton.GetComponentInChildren<Text>().text = HasSavedRun() ? "Continue" : "No Save";

            _skillTreeButton = TDVisuals.CreateButton(metaPanel.transform, "SkillTreeButton", "Skill Tree", new Color(0.32f, 0.24f, 0.52f), Color.white);
            SetRect(_skillTreeButton.GetComponent<RectTransform>(), new Vector2(0.53f, 0.08f), new Vector2(0.96f, 0.52f), Vector2.zero, Vector2.zero);
            _skillTreeButton.onClick.AddListener(() => ShowSkillTreeOverlay(0));
        }

        private void BuildMenuStats(RectTransform parent)
        {
            var statsPanel = TDVisuals.CreateUiBlock(
                parent,
                "StatsPanel",
                new Vector2(0.07f, 0.47f),
                new Vector2(0.58f, 0.62f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.08f, 0.12f, 0.18f, 0.92f));

            var bestScore = PlayerPrefs.GetInt("td_best_score", 0).ToString();
            var bestWave = PlayerPrefs.GetInt("td_best_wave", 0).ToString();
            var unlocked = PlayerPrefs.GetInt("td_campaign_unlocked", 1);
            var unlockedAchievements = CountUnlockedAchievements();

            CreateStatCard(statsPanel.transform, "Best Score", bestScore, new Vector2(0.03f, 0.38f), new Vector2(0.24f, 0.90f), new Color(0.11f, 0.19f, 0.26f));
            CreateStatCard(statsPanel.transform, "Best Wave", bestWave, new Vector2(0.27f, 0.38f), new Vector2(0.48f, 0.90f), new Color(0.12f, 0.16f, 0.24f));
            CreateStatCard(statsPanel.transform, "Campaign", "L" + unlocked, new Vector2(0.51f, 0.38f), new Vector2(0.72f, 0.90f), new Color(0.20f, 0.18f, 0.10f));
            CreateStatCard(statsPanel.transform, "Achieve", unlockedAchievements + "/" + TDLegacyData.Achievements.Count, new Vector2(0.75f, 0.38f), new Vector2(0.96f, 0.90f), new Color(0.16f, 0.12f, 0.22f));

            var achievementsButton = TDVisuals.CreateButton(statsPanel.transform, "AchievementsButton", "Achievements", new Color(0.26f, 0.24f, 0.50f), Color.white);
            SetRect(achievementsButton.GetComponent<RectTransform>(), new Vector2(0.03f, 0.08f), new Vector2(0.48f, 0.30f), Vector2.zero, Vector2.zero);
            achievementsButton.onClick.AddListener(ShowAchievementsOverlay);

            var statsButton = TDVisuals.CreateButton(statsPanel.transform, "LifetimeStatsButton", "Lifetime Stats", new Color(0.18f, 0.30f, 0.42f), Color.white);
            SetRect(statsButton.GetComponent<RectTransform>(), new Vector2(0.51f, 0.08f), new Vector2(0.96f, 0.30f), Vector2.zero, Vector2.zero);
            statsButton.onClick.AddListener(ShowLifetimeStatsOverlay);
        }

        private GameObject CreateStatCard(Transform parent, string label, string value, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var card = TDVisuals.CreateUiBlock(parent, label.Replace(" ", "") + "Card", anchorMin, anchorMax, Vector2.zero, Vector2.zero, color);
            var labelText = TDVisuals.CreateText(card.transform, "Label", label, 22, TextAnchor.UpperLeft, new Color(0.68f, 0.78f, 0.88f));
            SetRect(labelText.rectTransform, new Vector2(0.08f, 0.55f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);
            var valueText = TDVisuals.CreateText(card.transform, "Value", value, 38, TextAnchor.LowerLeft, Color.white);
            valueText.fontStyle = FontStyle.Bold;
            SetRect(valueText.rectTransform, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.62f), Vector2.zero, Vector2.zero);
            return card;
        }

        private int CountUnlockedAchievements()
        {
            var unlocked = 0;
            foreach (var achievement in TDLegacyData.Achievements)
            {
                if (PlayerPrefs.GetInt("td_ach_" + achievement.Id, 0) == 1)
                {
                    unlocked += 1;
                }
            }

            return unlocked;
        }

        private void ShowAchievementsOverlay()
        {
            if (_state != RuntimeState.Menu)
            {
                return;
            }

            if (_overlayRoot != null)
            {
                Destroy(_overlayRoot);
                _overlayRoot = null;
            }

            _overlayRoot = new GameObject("AchievementsOverlay", typeof(RectTransform));
            _overlayRoot.transform.SetParent(_canvas.transform, false);
            var rootRect = _overlayRoot.GetComponent<RectTransform>();
            SetRect(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TDVisuals.CreateUiBlock(_overlayRoot.transform, "Scrim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.78f));
            var card = TDVisuals.CreateUiBlock(
                _overlayRoot.transform,
                "Card",
                new Vector2(0.08f, 0.07f),
                new Vector2(0.92f, 0.93f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.06f, 0.10f, 0.16f, 0.98f));

            var title = TDVisuals.CreateText(card.transform, "Title", "Achievements", 48, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.04f, 0.90f), new Vector2(0.60f, 0.98f), Vector2.zero, Vector2.zero);

            var unlocked = CountUnlockedAchievements();
            var status = TDVisuals.CreateText(
                card.transform,
                "Status",
                "Unlocked " + unlocked + " / " + TDLegacyData.Achievements.Count + "  |  Bosses " + TDLegacyData.Bosses.Count,
                24,
                TextAnchor.UpperRight,
                new Color(0.86f, 0.94f, 1f));
            SetRect(status.rectTransform, new Vector2(0.40f, 0.90f), new Vector2(0.84f, 0.98f), Vector2.zero, Vector2.zero);

            var close = TDVisuals.CreateButton(card.transform, "Close", "Close", new Color(0.22f, 0.24f, 0.30f), Color.white);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0.85f, 0.90f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
            close.onClick.AddListener(() =>
            {
                if (_overlayRoot != null)
                {
                    Destroy(_overlayRoot);
                    _overlayRoot = null;
                }
            });

            var listPanel = TDVisuals.CreateUiBlock(
                card.transform,
                "ListPanel",
                new Vector2(0.04f, 0.08f),
                new Vector2(0.96f, 0.86f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.04f, 0.08f, 0.12f, 0.95f));

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            viewport.transform.SetParent(listPanel.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            SetRect(viewportRect, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));

            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            var mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewport.transform, false);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            var rowHeight = 80f;
            var rowPadding = 10f;
            var totalHeight = rowPadding + TDLegacyData.Achievements.Count * rowHeight + rowPadding;
            contentRect.sizeDelta = new Vector2(0f, totalHeight);

            var scrollRect = viewport.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 38f;

            for (var i = 0; i < TDLegacyData.Achievements.Count; i++)
            {
                var achievement = TDLegacyData.Achievements[i];
                var isUnlocked = PlayerPrefs.GetInt("td_ach_" + achievement.Id, 0) == 1;

                var rowTop = -rowPadding - i * rowHeight;
                var rowBottom = rowTop - (rowHeight - 8f);
                var row = TDVisuals.CreateUiBlock(
                    contentRect,
                    "Achievement_" + achievement.Id,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(6f, rowBottom),
                    new Vector2(-6f, rowTop),
                    isUnlocked ? new Color(0.12f, 0.22f, 0.18f, 0.96f) : new Color(0.10f, 0.12f, 0.16f, 0.96f));

                var label = TDVisuals.CreateText(
                    row.transform,
                    "Name",
                    (isUnlocked ? "Unlocked  " : "Locked  ") + achievement.Title,
                    22,
                    TextAnchor.UpperLeft,
                    isUnlocked ? new Color(0.76f, 1f, 0.82f) : new Color(0.80f, 0.86f, 0.92f));
                label.fontStyle = FontStyle.Bold;
                SetRect(label.rectTransform, new Vector2(0.03f, 0.52f), new Vector2(0.96f, 0.90f), Vector2.zero, Vector2.zero);

                var description = TDVisuals.CreateText(row.transform, "Description", achievement.Description, 18, TextAnchor.UpperLeft, new Color(0.72f, 0.82f, 0.90f));
                SetRect(description.rectTransform, new Vector2(0.03f, 0.12f), new Vector2(0.96f, 0.54f), Vector2.zero, Vector2.zero);
            }
        }

        private void ShowLifetimeStatsOverlay()
        {
            if (_state != RuntimeState.Menu)
            {
                return;
            }

            if (_overlayRoot != null)
            {
                Destroy(_overlayRoot);
                _overlayRoot = null;
            }

            _overlayRoot = new GameObject("LifetimeStatsOverlay", typeof(RectTransform));
            _overlayRoot.transform.SetParent(_canvas.transform, false);
            var rootRect = _overlayRoot.GetComponent<RectTransform>();
            SetRect(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TDVisuals.CreateUiBlock(_overlayRoot.transform, "Scrim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.78f));
            var card = TDVisuals.CreateUiBlock(
                _overlayRoot.transform,
                "Card",
                new Vector2(0.16f, 0.16f),
                new Vector2(0.84f, 0.84f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.06f, 0.10f, 0.16f, 0.98f));

            var title = TDVisuals.CreateText(card.transform, "Title", "Lifetime Stats", 44, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.06f, 0.86f), new Vector2(0.70f, 0.96f), Vector2.zero, Vector2.zero);

            var close = TDVisuals.CreateButton(card.transform, "Close", "Close", new Color(0.22f, 0.24f, 0.30f), Color.white);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0.78f, 0.86f), new Vector2(0.94f, 0.95f), Vector2.zero, Vector2.zero);
            close.onClick.AddListener(() =>
            {
                if (_overlayRoot != null)
                {
                    Destroy(_overlayRoot);
                    _overlayRoot = null;
                }
            });

            var unlockedAchievements = CountUnlockedAchievements();
            var campaignUnlocked = PlayerPrefs.GetInt("td_campaign_unlocked", 1);
            var statsText =
                "Total Runs: " + PlayerPrefs.GetInt("td_lifetime_runs", 0) +
                "\nKills: " + PlayerPrefs.GetInt("td_lifetime_kills", 0) +
                "\nBosses Defeated: " + PlayerPrefs.GetInt("td_lifetime_bosses", 0) +
                "\nGold Collected: " + PlayerPrefs.GetInt("td_lifetime_gold", 0) +
                "\nBest Combo: " + PlayerPrefs.GetInt("td_lifetime_best_combo", 0) +
                "\nBest Score: " + PlayerPrefs.GetInt("td_best_score", 0) +
                "\nBest Wave: " + PlayerPrefs.GetInt("td_best_wave", 0) +
                "\nDiamonds Banked: " + GetDiamonds() +
                "\nAchievements: " + unlockedAchievements + " / " + TDLegacyData.Achievements.Count +
                "\nCampaign Progress: " + campaignUnlocked + " / " + TDData.Campaign.Count +
                "\nUnique Boss Types: " + TDLegacyData.Bosses.Count;

            var body = TDVisuals.CreateText(card.transform, "Body", statsText, 28, TextAnchor.UpperLeft, new Color(0.84f, 0.92f, 0.98f));
            SetRect(body.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.80f), Vector2.zero, Vector2.zero);
        }

        private void BuildModeCards(RectTransform parent)
        {
            BuildModeCard(
                parent,
                "Campaign",
                "Hand-authored progression with curated tower unlocks and themed battlegrounds.",
                "Start Campaign",
                new Color(0.08f, 0.14f, 0.22f),
                new Vector2(0.07f, 0.32f),
                new Vector2(0.31f, 0.46f),
                StartCampaign);

            BuildModeCard(
                parent,
                "Endless",
                "Use the full armory, survive scaling waves, and push for a new high score.",
                "Start Endless",
                new Color(0.16f, 0.10f, 0.08f),
                new Vector2(0.34f, 0.32f),
                new Vector2(0.58f, 0.46f),
                StartEndless);

            BuildModeCard(
                parent,
                "Boss Rush",
                "Frequent boss encounters with fast scaling. Prioritize burst and control towers.",
                "Start Boss Rush",
                new Color(0.16f, 0.09f, 0.17f),
                new Vector2(0.07f, 0.16f),
                new Vector2(0.31f, 0.30f),
                StartBossRush);

            BuildModeCard(
                parent,
                "Daily",
                "Seeded challenge with fixed map and rotating modifiers for everyone on the same day.",
                "Start Daily",
                new Color(0.10f, 0.15f, 0.10f),
                new Vector2(0.34f, 0.16f),
                new Vector2(0.58f, 0.30f),
                StartDailyChallenge);
        }

        private GameObject BuildModeCard(
            Transform parent,
            string title,
            string body,
            string buttonText,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            UnityEngine.Events.UnityAction action)
        {
            var card = TDVisuals.CreateUiBlock(
                parent,
                title + "Card",
                anchorMin,
                anchorMax,
                Vector2.zero,
                Vector2.zero,
                color);

            var titleText = TDVisuals.CreateText(card.transform, "Title", title, 30, TextAnchor.UpperLeft, Color.white);
            titleText.fontStyle = FontStyle.Bold;
            SetRect(titleText.rectTransform, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.90f), Vector2.zero, Vector2.zero);

            var bodyText = TDVisuals.CreateText(card.transform, "Body", body, 20, TextAnchor.UpperLeft, new Color(0.78f, 0.84f, 0.92f));
            SetRect(bodyText.rectTransform, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.68f), Vector2.zero, Vector2.zero);

            var button = TDVisuals.CreateButton(card.transform, "Action", buttonText, new Color(0.92f, 0.74f, 0.24f), new Color(0.14f, 0.10f, 0.06f));
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.24f), Vector2.zero, Vector2.zero);
            button.onClick.AddListener(action);

            if (title == "Campaign")
            {
                _campaignButton = button;
            }
            else if (title == "Endless")
            {
                _endlessButton = button;
            }

            return card;
        }

        private void BuildMapSelector(RectTransform parent)
        {
            var panel = TDVisuals.CreateUiBlock(
                parent,
                "MapPanel",
                new Vector2(0.63f, 0.16f),
                new Vector2(0.93f, 0.86f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.08f, 0.11f, 0.17f, 0.94f));

            var title = TDVisuals.CreateText(panel.transform, "Title", "Battlefields", 34, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.08f, 0.90f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);

            var body = TDVisuals.CreateText(panel.transform, "Body", "Each map shifts the tone and lane pattern. Campaign follows unlock order; endless lets you pick freely.", 22, TextAnchor.UpperLeft, new Color(0.74f, 0.82f, 0.90f));
            SetRect(body.rectTransform, new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero);

            _mapButtonRoot = new GameObject("MapButtons", typeof(RectTransform));
            _mapButtonRoot.transform.SetParent(panel.transform, false);
            var buttonsRect = _mapButtonRoot.GetComponent<RectTransform>();
            SetRect(buttonsRect, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.70f), Vector2.zero, Vector2.zero);

            var index = 0;
            foreach (var pair in TDData.Maps)
            {
                var map = pair.Value;
                var button = TDVisuals.CreateButton(_mapButtonRoot.transform, map.Name + "Button", map.Name, new Color(0.12f, 0.17f, 0.24f), Color.white);
                var row = 5 - index;
                SetRect(
                    button.GetComponent<RectTransform>(),
                    new Vector2(0f, row / 6f),
                    new Vector2(1f, (row + 1) / 6f),
                    new Vector2(0f, 6f),
                    new Vector2(0f, -6f));

                var label = button.GetComponentInChildren<Text>();
                label.text = map.Name + "\n<size=18>" + map.Label + "</size>";
                label.alignment = TextAnchor.MiddleLeft;
                label.resizeTextForBestFit = false;
                button.onClick.AddListener(() => SelectMenuMap(map.Type));
                _mapButtons[map.Type] = button;
                index += 1;
            }

            SelectMenuMap(_selectedMenuMap);
        }

        private void SelectMenuMap(MapType type)
        {
            _selectedMenuMap = type;
            foreach (var pair in _mapButtons)
            {
                var selected = pair.Key == type;
                var image = pair.Value.GetComponent<Image>();
                var map = TDData.Maps[pair.Key];
                image.color = selected ? map.Accent : new Color(0.12f, 0.17f, 0.24f);
                var text = pair.Value.GetComponentInChildren<Text>();
                text.color = selected ? new Color(0.10f, 0.08f, 0.06f) : Color.white;
            }

            if (_menuRoot != null)
            {
                var activeMap = TDData.Maps[type];
                var backdrop = _menuRoot.transform.Find("Backdrop");
                if (backdrop != null)
                {
                    backdrop.GetComponent<Image>().color = activeMap.BackgroundBottom;
                }
            }
        }

        private void StartCampaign()
        {
            ShowCampaignOverlay();
        }

        private void ShowCampaignOverlay()
        {
            if (_state != RuntimeState.Menu)
            {
                return;
            }

            if (_overlayRoot != null)
            {
                Destroy(_overlayRoot);
                _overlayRoot = null;
            }

            _overlayRoot = new GameObject("CampaignOverlay", typeof(RectTransform));
            _overlayRoot.transform.SetParent(_canvas.transform, false);
            var rootRect = _overlayRoot.GetComponent<RectTransform>();
            SetRect(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TDVisuals.CreateUiBlock(_overlayRoot.transform, "Backdrop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.06f, 0.08f, 0.16f, 0.98f));
            TDVisuals.CreateUiBlock(_overlayRoot.transform, "TopGlow", new Vector2(0f, 0.86f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(0.10f, 0.18f, 0.36f, 0.55f));

            var title = TDVisuals.CreateText(_overlayRoot.transform, "Title", "\ud83d\udcdc  Campaign", 62, TextAnchor.UpperCenter, new Color(1f, 0.88f, 0.18f));
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.10f, 0.93f), new Vector2(0.90f, 0.985f), Vector2.zero, Vector2.zero);

            var completed = CountCompletedCampaignLevels();
            var fullHp = CountFullHpCampaignLevels();
            var progress = TDVisuals.CreateText(
                _overlayRoot.transform,
                "Progress",
                completed + " / " + TDData.Campaign.Count + " Completed  |  \u2b50 " + fullHp + " flawless",
                30,
                TextAnchor.MiddleCenter,
                new Color(0.82f, 0.88f, 0.96f));
            SetRect(progress.rectTransform, new Vector2(0.10f, 0.885f), new Vector2(0.90f, 0.93f), Vector2.zero, Vector2.zero);

            var listPanel = TDVisuals.CreateUiBlock(
                _overlayRoot.transform,
                "ListPanel",
                new Vector2(0.04f, 0.10f),
                new Vector2(0.96f, 0.87f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.05f, 0.11f, 0.20f, 0.94f));

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            viewport.transform.SetParent(listPanel.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            SetRect(viewportRect, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewport.transform, false);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            var rowHeight = 116f;
            var rowPadding = 10f;
            contentRect.sizeDelta = new Vector2(0f, rowPadding + TDData.Campaign.Count * rowHeight + rowPadding);

            var scrollRect = viewport.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 45f;

            var unlocked = Mathf.Clamp(PlayerPrefs.GetInt("td_campaign_unlocked", 1), 1, TDData.Campaign.Count);
            for (var i = 0; i < TDData.Campaign.Count; i++)
            {
                var level = TDData.Campaign[i];
                var levelUnlocked = level.Id <= unlocked;
                var completedLevel = PlayerPrefs.GetInt("td_campaign_level_" + level.Id, 0) == 1;
                var flawless = PlayerPrefs.GetInt("td_campaign_level_fullhp_" + level.Id, 0) == 1;
                var starCount = flawless ? 3 : completedLevel ? 2 : 0;
                var rowTop = -rowPadding - i * rowHeight;
                var rowBottom = rowTop - (rowHeight - 8f);

                var cardButton = TDVisuals.CreateButton(
                    contentRect,
                    "CampaignCard_" + level.Id,
                    string.Empty,
                    levelUnlocked ? new Color(0.08f, 0.24f, 0.45f, 0.98f) : new Color(0.10f, 0.13f, 0.19f, 0.96f),
                    Color.white);
                SetRect(cardButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(6f, rowBottom), new Vector2(-6f, rowTop));
                cardButton.interactable = levelUnlocked;

                var icon = TDVisuals.CreateText(cardButton.transform, "Icon", GetCampaignLevelEmoji(level), 40, TextAnchor.MiddleCenter, Color.white);
                SetRect(icon.rectTransform, new Vector2(0.02f, 0.14f), new Vector2(0.16f, 0.86f), Vector2.zero, Vector2.zero);

                var titleText = TDVisuals.CreateText(
                    cardButton.transform,
                    "Title",
                    level.Id + ". " + level.Title,
                    34,
                    TextAnchor.UpperLeft,
                    levelUnlocked ? new Color(1f, 0.90f, 0.24f) : new Color(0.70f, 0.76f, 0.84f));
                titleText.fontStyle = FontStyle.Bold;
                SetRect(titleText.rectTransform, new Vector2(0.18f, 0.56f), new Vector2(0.78f, 0.93f), Vector2.zero, Vector2.zero);

                var body = TDVisuals.CreateText(
                    cardButton.transform,
                    "Body",
                    level.Hint + "\nSurvive " + level.TargetWave + " waves  |  " + GetMapEmoji(level.Map) + " " + TDData.Maps[level.Map].Name + "  |  \ud83d\udc8e " + level.DiamondReward,
                    22,
                    TextAnchor.UpperLeft,
                    new Color(0.84f, 0.90f, 0.96f));
                SetRect(body.rectTransform, new Vector2(0.18f, 0.10f), new Vector2(0.84f, 0.62f), Vector2.zero, Vector2.zero);

                var stars = TDVisuals.CreateText(cardButton.transform, "Stars", new string('\u2b50', starCount == 0 ? 1 : starCount), 34, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.24f, starCount == 0 ? 0.32f : 1f));
                SetRect(stars.rectTransform, new Vector2(0.84f, 0.16f), new Vector2(0.98f, 0.84f), Vector2.zero, Vector2.zero);

                if (!levelUnlocked)
                {
                    var lockText = TDVisuals.CreateText(cardButton.transform, "Lock", "\ud83d\udd12", 34, TextAnchor.MiddleCenter, new Color(0.88f, 0.90f, 0.96f));
                    SetRect(lockText.rectTransform, new Vector2(0.84f, 0.16f), new Vector2(0.98f, 0.84f), Vector2.zero, Vector2.zero);
                }
                else
                {
                    var selectedLevel = level;
                    cardButton.onClick.AddListener(() =>
                    {
                        if (_overlayRoot != null)
                        {
                            Destroy(_overlayRoot);
                            _overlayRoot = null;
                        }

                        StartRun(selectedLevel.Map, GameMode.Campaign, selectedLevel);
                    });
                }
            }

            var backButton = TDVisuals.CreateButton(_overlayRoot.transform, "BackButton", "BACK", new Color(0.10f, 0.14f, 0.24f), new Color(0.82f, 0.86f, 0.94f));
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.08f), Vector2.zero, Vector2.zero);
            backButton.onClick.AddListener(() =>
            {
                if (_overlayRoot != null)
                {
                    Destroy(_overlayRoot);
                    _overlayRoot = null;
                }
            });
        }

        private static string GetCampaignLevelEmoji(CampaignLevel level)
        {
            if (level.Id % 5 == 0)
            {
                return "\ud83d\udc80";
            }

            if (level.AllowedTowers.Contains(TowerType.Cannon))
            {
                return "\ud83d\udca3";
            }

            if (level.AllowedTowers.Contains(TowerType.Magic))
            {
                return "\ud83e\ude84";
            }

            if (level.AllowedPowers.Contains(PowerType.Heal))
            {
                return "\ud83d\udd27";
            }

            return "\ud83c\udff9";
        }

        private static string GetMapEmoji(MapType map)
        {
            switch (map)
            {
                case MapType.Classic:
                    return "\ud83c\udfde\ufe0f";
                case MapType.Valley:
                    return "\u26f0\ufe0f";
                case MapType.Crossroads:
                    return "\u2795";
                case MapType.Desert:
                    return "\ud83c\udfdc\ufe0f";
                case MapType.Snow:
                    return "\u2744\ufe0f";
                case MapType.Volcano:
                    return "\ud83c\udf0b";
                default:
                    return "\ud83d\uddfa\ufe0f";
            }
        }

        private static string GetTowerEmoji(TowerType type)
        {
            switch (type)
            {
                case TowerType.Arrow:
                    return "\ud83c\udff9";
                case TowerType.Magic:
                    return "\ud83e\ude84";
                case TowerType.Cannon:
                    return "\ud83d\udca3";
                case TowerType.Poison:
                    return "\ud83e\uddea";
                case TowerType.Tesla:
                    return "\u26a1";
                case TowerType.Ice:
                    return "\u2744\ufe0f";
                case TowerType.Flame:
                    return "\ud83d\udd25";
                default:
                    return "\ud83c\udff0";
            }
        }

        private static string GetPowerEmoji(PowerType type)
        {
            switch (type)
            {
                case PowerType.Fireball:
                    return "\ud83d\udd25";
                case PowerType.Freeze:
                    return "\u2744\ufe0f";
                case PowerType.Heal:
                    return "\ud83d\udc9a";
                case PowerType.Lightning:
                    return "\u26a1";
                default:
                    return "\u2734\ufe0f";
            }
        }

        private void StartEndless()
        {
            StartRun(_selectedMenuMap, GameMode.Endless, null);
        }

        private void StartBossRush()
        {
            StartRun(_selectedMenuMap, GameMode.BossRush, null);
        }

        private void StartDailyChallenge()
        {
            var maps = new List<MapType>(TDData.Maps.Keys);
            maps.Sort((left, right) => left.CompareTo(right));
            var key = GetDailyKey();
            var map = maps[Mathf.Abs(key) % maps.Count];
            StartRun(map, GameMode.DailyChallenge, null);
        }

        private void StartRun(MapType map, GameMode mode, CampaignLevel? campaignLevel)
        {
            var loadingSavedRun = _loadingSavedRun;
            ClearRuntime();
            _loadingSavedRun = loadingSavedRun;
            if (!_loadingSavedRun)
            {
                ClearSavedRun();
            }

            _state = RuntimeState.Playing;
            _mode = mode;
            _activeMap = map;
            _campaignLevel = campaignLevel;
            _allowedTowers = campaignLevel != null ? new List<TowerType>(campaignLevel.AllowedTowers) : new List<TowerType>(TDData.Towers.Keys);
            if (_allowedTowers.Count == 0)
            {
                _allowedTowers = new List<TowerType>(TDData.Towers.Keys);
            }
            _allowedPowers = campaignLevel != null ? new List<PowerType>(campaignLevel.AllowedPowers) : new List<PowerType>(TDData.Powers.Keys);
            _upgradesEnabled = campaignLevel?.UpgradesEnabled ?? true;

            _playerDamageLevel = 1;
            _playerSpeedLevel = 1;
            _playerHpLevel = 1;
            _baseHpLevel = 1;
            RefreshSkillRuntimeBonuses();

            _gold = (campaignLevel?.StartingGold ?? 150) + GetSkillLevel("start_gold") * 15;
            _score = 0;
            _wave = 0;
            _maxBaseHp = 100 + GetSkillLevel("base_hp") * 20;
            _baseHp = _maxBaseHp;
            _baseDamagedThisWave = false;
            _baseDamagedThisRun = false;
            _gameSpeed = 1f;
            _intermissionTimer = 3.2f;
            _spawnTimer = 0f;
            _bannerTimer = 0f;
            _placingTowerType = null;
            _armedPowerType = null;
            _selectedTower = null;
            _spawnQueue = new Queue<SpawnRequest>();
            _currentWaveModifier = TDWaveModifier.None;
            _currentWaveBoss = null;
            _isNight = false;
            _killsThisRun = 0;
            _bossesKilledThisRun = 0;
            _diamondsThisRun = 0;
            _repairsThisRun = 0;
            _goldEarnedThisRun = 0;
            _comboCount = 0;
            _comboTimer = 0f;
            _comboMultiplier = 1f;
            _bestCombo = 0;
            _totalEnemiesThisWave = 0;
            _enemiesSpawnedThisWave = 0;
            _powersUsedThisRun.Clear();
            _towerTypesPlacedThisRun.Clear();
            _bossPool.Clear();
            _powerCooldowns.Clear();
            foreach (var power in _allowedPowers)
            {
                _powerCooldowns[power] = 0f;
            }
            InitializeRunSeed();

            InitializePlayerStats();

            _defaultHint = campaignLevel != null
                ? campaignLevel.Hint
                : mode == GameMode.BossRush
                    ? "Boss waves arrive rapidly. Build for burst and control."
                    : mode == GameMode.DailyChallenge
                        ? "Daily modifiers are active. Adapt your build on the fly."
                        : "Endless mode scales every wave. Use powers to recover tempo.";
            BuildWorld();
            BuildHud();

            var title = campaignLevel != null
                ? "Campaign " + campaignLevel.Id + " - " + campaignLevel.Title
                : mode == GameMode.BossRush
                    ? map + " Boss Rush"
                    : mode == GameMode.DailyChallenge
                        ? map + " Daily Challenge"
                        : map + " Endless";
            ShowBanner(title, _defaultHint, 3.6f);
            RefreshHud();
        }

        private void InitializeRunSeed()
        {
            if (_mode == GameMode.DailyChallenge)
            {
                _runSeed = GetDailyKey();
            }
            else
            {
                _runSeed = Environment.TickCount ^ (int)(DateTime.Now.Ticks & int.MaxValue);
            }

            _runRng = new System.Random(_runSeed);
            _dailyModifierRotation.Clear();
            if (_mode == GameMode.DailyChallenge)
            {
                var pool = new List<TDWaveModifier>
                {
                    TDWaveModifier.Fast,
                    TDWaveModifier.Armored,
                    TDWaveModifier.Regen,
                    TDWaveModifier.Swarm,
                    TDWaveModifier.Invisible,
                    TDWaveModifier.Rich,
                    TDWaveModifier.BossRally
                };

                for (var i = 0; i < 3; i++)
                {
                    _dailyModifierRotation.Add(pool[_runRng.Next(pool.Count)]);
                }
            }
        }

        private static int GetDailyKey()
        {
            var now = DateTime.Now;
            return now.Year * 10000 + now.Month * 100 + now.Day;
        }

        private bool RollChance(float chance)
        {
            if (chance <= 0f)
            {
                return false;
            }

            if (chance >= 1f)
            {
                return true;
            }

            return _runRng.NextDouble() < chance;
        }

        private float NextRunRandom(float min, float max)
        {
            return (float)(_runRng.NextDouble() * (max - min) + min);
        }

        private void RefreshSkillRuntimeBonuses()
        {
            _prestigeLevel = PlayerPrefs.GetInt("td_prestige_level", 0);
            _skillTowerDamageMult = 1f + GetSkillLevel("tower_damage") * 0.08f;
            _skillGoldBonusMult =
                (1f + GetSkillLevel("gold_bonus") * 0.10f) *
                (1f + GetSkillLevel("prestige_gold") * 0.05f);
            _skillDiamondLuckBonus = GetSkillLevel("diamond_luck") * 0.05f;
            _skillWaveGoldBonus = GetSkillLevel("wave_bonus") * 3;
            _skillIceSlowBonus = GetSkillLevel("ice_power") * 0.10f;
            _skillAbilityCooldownReduction = Mathf.Clamp01(GetSkillLevel("ability_cd") * 0.05f);
            _skillSellBonus = GetSkillLevel("sell_bonus") * 0.10f;
            _skillResistancePierce = GetSkillLevel("resist_pierce") * 0.05f;
            _skillLuckyWaveBonus = GetSkillLevel("wave_modifier") * 0.10f;
        }

        private float GetPrestigeDamageMultiplier()
        {
            return 1f + _prestigeLevel * 0.05f;
        }

        private void InitializePlayerStats()
        {
            _playerSpeed = 3.0f + GetSkillLevel("player_speed") * 0.20f;
            _playerAttackDamage = 10f + GetSkillLevel("player_damage") * 3f;
            _playerAttackRange = 1.8f + GetSkillLevel("attack_range") * 0.15f;
            _playerMaxHp = 100f + GetSkillLevel("player_hp") * 15f;
            _playerHp = _playerMaxHp;
            _playerAttackTimer = 0f;
            _dashCooldown = 0f;
        }

        private void BuildWorld()
        {
            foreach (Transform child in _worldRoot)
            {
                Destroy(child.gameObject);
            }

            _paths.Clear();
            _paths.AddRange(GetPathsForMap(_activeMap));
            BuildMap();
            BuildBase();
            _playerRoot = null;
            _playerPosition = _basePosition;
            _playerTarget = _basePosition;
        }

        private void BuildMap()
        {
            var map = TDData.Maps[_activeMap];
            _gameCamera.backgroundColor = map.BackgroundBottom;

            TDVisuals.CreateWorldSprite(
                "BackdropBottom",
                _worldRoot,
                Vector2.zero,
                new Vector2(WorldHalfWidth * 2.8f, WorldHalfHeight * 2.4f),
                map.BackgroundBottom,
                -20);

            TDVisuals.CreateWorldSprite(
                "BackdropTop",
                _worldRoot,
                new Vector2(0f, 3.8f),
                new Vector2(WorldHalfWidth * 2.8f, WorldHalfHeight * 0.95f),
                map.BackgroundTop,
                -19);

            TDVisuals.CreateWorldSprite(
                "Surface",
                _worldRoot,
                new Vector2(0f, -0.35f),
                new Vector2(WorldHalfWidth * 1.95f, WorldHalfHeight * 1.86f),
                map.Surface,
                -18);

            TDVisuals.CreateWorldSprite(
                "HazeBand",
                _worldRoot,
                new Vector2(0f, 4.1f),
                new Vector2(WorldHalfWidth * 2.2f, 0.36f),
                new Color(0.86f, 0.92f, 0.98f, 0.22f),
                -17);

            if (_activeMap == MapType.Desert)
            {
                TDVisuals.CreateWorldSprite(
                    "DesertSkyBand",
                    _worldRoot,
                    new Vector2(0f, 6.9f),
                    new Vector2(WorldHalfWidth * 2.6f, 1.2f),
                    new Color(0.76f, 0.90f, 0.98f, 0.55f),
                    -18);
                TDVisuals.CreateWorldSprite(
                    "DesertHorizon",
                    _worldRoot,
                    new Vector2(0f, 5.9f),
                    new Vector2(WorldHalfWidth * 2.6f, 0.12f),
                    new Color(0.88f, 0.86f, 0.70f, 0.48f),
                    -17);
            }

            for (var i = 0; i < 11; i++)
            {
                var t = i / 10f;
                var y = 6.8f - i * 1.42f;
                var stripe = Color.Lerp(map.Surface, map.BackgroundBottom, 0.14f + t * 0.24f);
                stripe.a = 0.18f;
                TDVisuals.CreateWorldSprite(
                    "GroundBand" + i,
                    _worldRoot,
                    new Vector2(0f, y),
                    new Vector2(WorldHalfWidth * 2.3f, 1.08f),
                    stripe,
                    -16);
            }

            BuildSceneryProps(map);

            for (var i = 0; i < _paths.Count; i++)
            {
                BuildPathVisual(_paths[i], map.PathOuter, map.PathInner);
                var gatePosition = _paths[i][0];
                TDVisuals.CreateWorldSprite(
                    "SpawnGate" + i,
                    _worldRoot,
                    gatePosition + new Vector2(0f, 0.45f),
                    new Vector2(1.1f, 0.34f),
                    map.Accent,
                    -7);
            }

            for (var i = 0; i < 5; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                TDVisuals.CreateWorldSprite(
                    "Beacon" + i,
                    _worldRoot,
                    new Vector2(side * (2.5f + i * 0.2f), 5.4f - i * 2.4f),
                    new Vector2(0.18f, 0.18f),
                    new Color(map.Accent.r, map.Accent.g, map.Accent.b, 0.28f),
                    -8);
            }

            BuildMapAmbientEffects(map);
        }

        private void BuildPathVisual(List<Vector2> path, Color outer, Color inner)
        {
            var shadowColor = new Color(0f, 0f, 0f, 0.30f);
            var shoulderColor = Color.Lerp(outer, new Color(0.42f, 0.34f, 0.22f), 0.36f);
            var laneColor = Color.Lerp(inner, new Color(0.88f, 0.78f, 0.58f), 0.52f);
            var centerColor = new Color(0.94f, 0.88f, 0.68f, 0.26f);

            var shadowLine = TDVisuals.CreateLine("PathShadow", _worldRoot, shadowColor, 1.44f, -12);
            shadowLine.positionCount = path.Count;

            var shoulderLine = TDVisuals.CreateLine("PathShoulder", _worldRoot, new Color(shoulderColor.r, shoulderColor.g, shoulderColor.b, 0.95f), 1.18f, -11);
            shoulderLine.positionCount = path.Count;

            var laneLine = TDVisuals.CreateLine("PathLane", _worldRoot, laneColor, 0.86f, -10);
            laneLine.positionCount = path.Count;

            var centerLine = TDVisuals.CreateLine("PathCenter", _worldRoot, centerColor, 0.20f, -9);
            centerLine.positionCount = path.Count;

            for (var i = 0; i < path.Count; i++)
            {
                shadowLine.SetPosition(i, path[i] + new Vector2(0.03f, -0.05f));
                shoulderLine.SetPosition(i, path[i]);
                laneLine.SetPosition(i, path[i]);
                centerLine.SetPosition(i, path[i]);

                TDVisuals.CreateWorldSprite(
                    "PathNodeOuter" + i,
                    _worldRoot,
                    path[i],
                    new Vector2(0.40f, 0.40f),
                    new Color(outer.r, outer.g, outer.b, 0.32f),
                    -10);

                TDVisuals.CreateWorldSprite(
                    "PathNodeInner" + i,
                    _worldRoot,
                    path[i],
                    new Vector2(0.22f, 0.22f),
                    new Color(laneColor.r, laneColor.g, laneColor.b, 0.40f),
                    -9);
            }

            var tileIndex = 0;
            for (var i = 0; i < path.Count - 1; i++)
            {
                var from = path[i];
                var to = path[i + 1];
                var segment = to - from;
                var length = segment.magnitude;
                if (length <= 0.01f)
                {
                    continue;
                }

                var dir = segment / length;
                var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                var steps = Mathf.Max(1, Mathf.RoundToInt(length / 0.42f));
                for (var step = 0; step < steps; step++)
                {
                    var t = (step + 0.5f) / steps;
                    var center = Vector2.Lerp(from, to, t);
                    var jitter = Mathf.Sin((tileIndex + step) * 1.47f) * 0.035f;
                    var tile = TDVisuals.CreateWorldSprite(
                        "PathTile" + tileIndex,
                        _worldRoot,
                        center + new Vector2(-dir.y, dir.x) * jitter,
                        new Vector2(0.16f, 0.24f),
                        new Color(0.68f, 0.58f, 0.42f, 0.20f),
                        -9);
                    tile.transform.rotation = Quaternion.Euler(0f, 0f, angle + Mathf.Sin((tileIndex + step) * 0.8f) * 5f);
                    var decalRenderer = tile.GetComponent<SpriteRenderer>();
                    if (decalRenderer != null)
                    {
                        var decalSprite = TDArtLibrary.GetPathDecalSprite(tileIndex);
                        if (decalSprite != null)
                        {
                            decalRenderer.sprite = decalSprite;
                            decalRenderer.color = new Color(0.78f, 0.66f, 0.45f, 0.24f);
                        }
                    }

                    if ((tileIndex + step) % 3 == 0)
                    {
                        TDVisuals.CreateWorldSprite(
                            "PathPebble" + tileIndex,
                            _worldRoot,
                            center + new Vector2(-dir.y, dir.x) * (0.16f + Mathf.Sin(tileIndex * 0.67f) * 0.03f),
                            new Vector2(0.03f, 0.03f),
                            new Color(0.60f, 0.52f, 0.40f, 0.46f),
                            -8);
                    }

                    tileIndex += 1;
                }
            }
        }

        private void BuildSceneryProps(MapDefinition map)
        {
            var propCount = _activeMap == MapType.Crossroads ? 34 : 48;
            for (var i = 0; i < propCount; i++)
            {
                var a = Mathf.Abs(Mathf.Sin((i + 1) * 12.9898f + (int)_activeMap * 78.233f));
                var b = Mathf.Abs(Mathf.Sin((i + 1) * 41.3421f + (int)_activeMap * 19.173f));
                var x = Mathf.Lerp(-4.35f, 4.35f, a);
                var y = Mathf.Lerp(-6.2f, 7.2f, b);
                var position = new Vector2(x, y);
                if (Vector2.Distance(position, _basePosition) < 1.8f || IsNearAnyPath(position, 0.62f))
                {
                    continue;
                }

                var sprite = TDArtLibrary.GetPropSprite(_activeMap, i);
                if (sprite == null)
                {
                    continue;
                }

                var scale = 0.16f + Mathf.Abs(Mathf.Sin((i + 3) * 1.18f)) * 0.20f;
                var alpha = _activeMap == MapType.Snow ? 0.75f : 0.84f;
                var tint = Color.Lerp(Color.white, map.Surface, 0.08f);
                var prop = TDVisuals.CreateWorldSprite(
                    "Prop" + i,
                    _worldRoot,
                    position,
                    new Vector2(scale, scale),
                    new Color(tint.r, tint.g, tint.b, alpha),
                    -14);
                prop.transform.localRotation = Quaternion.Euler(0f, 0f, -16f + Mathf.Sin(i * 0.71f) * 22f);

                var renderer = prop.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sprite = sprite;
                }
            }
        }

        private bool IsNearAnyPath(Vector2 position, float maxDistance)
        {
            foreach (var path in _paths)
            {
                for (var i = 0; i < path.Count - 1; i++)
                {
                    if (DistanceToSegment(position, path[i], path[i + 1]) <= maxDistance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void BuildBase()
        {
            TDVisuals.CreateWorldSprite(
                "BaseShadow",
                _worldRoot,
                _basePosition + new Vector2(0f, -0.18f),
                new Vector2(2.2f, 0.72f),
                new Color(0f, 0f, 0f, 0.28f),
                -5);

            TDVisuals.CreateWorldSprite(
                "BasePlate",
                _worldRoot,
                _basePosition,
                new Vector2(1.8f, 1.15f),
                new Color(0.16f, 0.18f, 0.24f),
                -4);

            TDVisuals.CreateWorldSprite(
                "BaseCore",
                _worldRoot,
                _basePosition + new Vector2(0f, 0.08f),
                new Vector2(1.05f, 0.72f),
                new Color(0.96f, 0.80f, 0.24f),
                -3);

            TDVisuals.CreateWorldSprite(
                "BaseGlow",
                _worldRoot,
                _basePosition + new Vector2(0f, 0.05f),
                new Vector2(2.25f, 1.28f),
                new Color(0.96f, 0.80f, 0.24f, 0.18f),
                -6);

            TDVisuals.CreateWorldSprite(
                "BaseRing",
                _worldRoot,
                _basePosition + new Vector2(0f, 0.12f),
                new Vector2(2.0f, 0.24f),
                new Color(0.60f, 0.74f, 0.94f, 0.32f),
                -2);

            TDVisuals.CreateWorldSprite(
                "BasePylonLeft",
                _worldRoot,
                _basePosition + new Vector2(-0.56f, 0.20f),
                new Vector2(0.16f, 0.52f),
                new Color(0.58f, 0.78f, 1f, 0.62f),
                -2);

            TDVisuals.CreateWorldSprite(
                "BasePylonRight",
                _worldRoot,
                _basePosition + new Vector2(0.56f, 0.20f),
                new Vector2(0.16f, 0.52f),
                new Color(0.58f, 0.78f, 1f, 0.62f),
                -2);

            TDVisuals.CreateLoopParticles(
                "BaseAuraParticles",
                _worldRoot,
                _basePosition + new Vector2(0f, 0.20f),
                new Color(1f, 0.86f, 0.28f, 0.70f),
                new Color(1f, 0.98f, 0.82f, 0.35f),
                0.24f,
                12f,
                0.45f,
                1.3f,
                0.07f,
                0.14f,
                -1);
        }

        private void BuildPlayer()
        {
            if (_playerRoot != null)
            {
                Destroy(_playerRoot);
                _playerRoot = null;
            }

            _playerPosition = _basePosition + new Vector2(0f, -0.95f);
            _playerTarget = _playerPosition;

            _playerRoot = new GameObject("Player");
            _playerRoot.transform.SetParent(_worldRoot, false);
            _playerRoot.transform.position = _playerPosition;

            TDVisuals.CreateWorldSprite("PlayerShadow", _playerRoot.transform, new Vector2(0f, -0.08f), new Vector2(0.56f, 0.18f), new Color(0f, 0f, 0f, 0.34f), 20);

            var body = TDVisuals.CreateWorldSprite("PlayerBody", _playerRoot.transform, Vector2.zero, new Vector2(0.34f, 0.34f), new Color(0.28f, 0.56f, 0.84f), 21);
            var core = TDVisuals.CreateWorldSprite("PlayerCore", _playerRoot.transform, new Vector2(0f, 0.04f), new Vector2(0.16f, 0.16f), new Color(0.88f, 0.96f, 1f), 22);
            TDVisuals.CreateWorldSprite("PlayerBlade", _playerRoot.transform, new Vector2(0.13f, 0.16f), new Vector2(0.06f, 0.26f), new Color(0.82f, 0.92f, 1f), 22);

            _playerBody = body.GetComponent<SpriteRenderer>();
            _playerCore = core.GetComponent<SpriteRenderer>();

            var hpRoot = new GameObject("PlayerHpBar");
            hpRoot.transform.SetParent(_playerRoot.transform, false);
            hpRoot.transform.localPosition = new Vector3(0f, 0.44f, 0f);
            TDVisuals.CreateWorldSprite("HpBack", hpRoot.transform, Vector2.zero, new Vector2(0.52f, 0.07f), new Color(0f, 0f, 0f, 0.56f), 23);
            var hpFill = TDVisuals.CreateWorldSprite("HpFill", hpRoot.transform, Vector2.zero, new Vector2(0.52f, 0.07f), new Color(0.40f, 0.96f, 0.56f), 24);
            _playerHpFill = hpFill.transform;

            var dashRoot = new GameObject("PlayerDashBar");
            dashRoot.transform.SetParent(_playerRoot.transform, false);
            dashRoot.transform.localPosition = new Vector3(0f, 0.33f, 0f);
            TDVisuals.CreateWorldSprite("DashBack", dashRoot.transform, Vector2.zero, new Vector2(0.52f, 0.05f), new Color(0f, 0f, 0f, 0.56f), 23);
            var dashFill = TDVisuals.CreateWorldSprite("DashFill", dashRoot.transform, Vector2.zero, new Vector2(0.52f, 0.05f), new Color(0.00f, 0.88f, 0.92f), 24);
            _playerDashFill = dashFill.transform;

            TDVisuals.CreateLoopParticles(
                "PlayerAura",
                _playerRoot.transform,
                new Vector2(0f, 0.14f),
                new Color(0.46f, 0.72f, 1f, 0.45f),
                new Color(0.94f, 1f, 1f, 0.18f),
                0.09f,
                8f,
                0.20f,
                1.1f,
                0.03f,
                0.08f,
                20);

            UpdatePlayerBars();
        }

        private void BuildMapAmbientEffects(MapDefinition map)
        {
            var ambientRate = 16f;
            var ambientSpeed = 0.42f;
            var lifetime = 5.6f;
            var particleA = new Color(map.Accent.r, map.Accent.g, map.Accent.b, 0.22f);
            var particleB = new Color(1f, 1f, 1f, 0.10f);

            if (_activeMap == MapType.Volcano)
            {
                ambientRate = 28f;
                ambientSpeed = 0.95f;
                lifetime = 3.1f;
                particleA = new Color(1f, 0.46f, 0.22f, 0.45f);
                particleB = new Color(1f, 0.86f, 0.42f, 0.24f);
            }
            else if (_activeMap == MapType.Snow)
            {
                ambientRate = 22f;
                ambientSpeed = 0.20f;
                lifetime = 7.4f;
                particleA = new Color(0.80f, 0.92f, 1f, 0.40f);
                particleB = new Color(1f, 1f, 1f, 0.12f);
            }
            else if (_activeMap == MapType.Desert)
            {
                ambientRate = 18f;
                ambientSpeed = 0.56f;
                lifetime = 4.8f;
                particleA = new Color(0.96f, 0.76f, 0.42f, 0.28f);
                particleB = new Color(0.84f, 0.62f, 0.32f, 0.12f);
            }

            TDVisuals.CreateLoopParticles(
                "AmbientLeft",
                _worldRoot,
                new Vector2(-3.6f, 2.2f),
                particleA,
                particleB,
                0.90f,
                ambientRate,
                ambientSpeed,
                lifetime,
                0.06f,
                0.14f,
                -12);

            TDVisuals.CreateLoopParticles(
                "AmbientRight",
                _worldRoot,
                new Vector2(3.6f, 1.8f),
                particleA,
                particleB,
                0.90f,
                ambientRate,
                ambientSpeed,
                lifetime,
                0.06f,
                0.14f,
                -12);
        }

        private void BuildHud()
        {
            _hudRoot = new GameObject("HudRoot", typeof(RectTransform));
            _hudRoot.transform.SetParent(_canvas.transform, false);
            var hudRect = _hudRoot.GetComponent<RectTransform>();
            SetRect(hudRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var topBar = TDVisuals.CreateUiBlock(
                _hudRoot.transform,
                "TopBar",
                new Vector2(0.01f, 0.89f),
                new Vector2(0.99f, 0.99f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.09f, 0.16f, 0.26f, 0.96f));

            _goldText = CreateHudValue(topBar.transform, "\uD83D\uDCB0 Gold", new Vector2(0.01f, 0.14f), new Vector2(0.17f, 0.86f));
            _waveText = CreateHudValue(topBar.transform, "\u2694 Wave", new Vector2(0.175f, 0.14f), new Vector2(0.335f, 0.86f));
            _scoreText = CreateHudValue(topBar.transform, "\uD83D\uDC8E Stars", new Vector2(0.34f, 0.14f), new Vector2(0.50f, 0.86f));
            _hpText = CreateHudValue(topBar.transform, "\uD83C\uDFF0 Base", new Vector2(0.505f, 0.14f), new Vector2(0.665f, 0.86f));
            _playerHpText = null;
            _dashButton = null;

            _speedButton = TDVisuals.CreateButton(topBar.transform, "SpeedButton", "\u25B6 x1.0", new Color(0.02f, 0.66f, 0.56f), Color.white);
            SetRect(_speedButton.GetComponent<RectTransform>(), new Vector2(0.69f, 0.16f), new Vector2(0.84f, 0.84f), Vector2.zero, Vector2.zero);
            _speedButton.onClick.AddListener(CycleSpeed);

            var pauseButton = TDVisuals.CreateButton(topBar.transform, "PauseButton", "\u23F8", new Color(0.90f, 0.22f, 0.20f), Color.white);
            SetRect(pauseButton.GetComponent<RectTransform>(), new Vector2(0.85f, 0.16f), new Vector2(0.99f, 0.84f), Vector2.zero, Vector2.zero);
            pauseButton.onClick.AddListener(ShowPauseOverlay);

            var powersPanel = TDVisuals.CreateUiBlock(
                _hudRoot.transform,
                "PowersPanel",
                new Vector2(0.01f, 0.13f),
                new Vector2(0.99f, 0.19f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.10f, 0.18f, 0.30f, 0.94f));

            _powerButtons.Clear();
            if (_allowedPowers.Count == 0)
            {
                var lockedLabel = TDVisuals.CreateText(powersPanel.transform, "LockedLabel", "Powers locked in this mission.", 22, TextAnchor.MiddleCenter, new Color(0.82f, 0.86f, 0.92f));
                SetRect(lockedLabel.rectTransform, new Vector2(0.02f, 0.14f), new Vector2(0.98f, 0.86f), Vector2.zero, Vector2.zero);
            }
            else
            {
                var powerIndex = 0;
                var count = _allowedPowers.Count;
                foreach (var powerType in _allowedPowers)
                {
                    if (!TDData.Powers.TryGetValue(powerType, out var power))
                    {
                        continue;
                    }

                    var button = TDVisuals.CreateButton(
                        powersPanel.transform,
                        power.Name + "Button",
                        GetPowerEmoji(power.Type) + " " + power.Name.ToUpperInvariant(),
                        new Color(power.Color.r * 0.64f, power.Color.g * 0.64f, power.Color.b * 0.64f, 1f),
                        new Color(1f, 0.86f, 0.18f));
                    var start = powerIndex * (1f / count) + 0.006f;
                    var end = (powerIndex + 1) * (1f / count) - 0.006f;
                    SetRect(button.GetComponent<RectTransform>(), new Vector2(start, 0.08f), new Vector2(end, 0.92f), Vector2.zero, Vector2.zero);
                    button.onClick.AddListener(() => ArmPower(power.Type));
                    _powerButtons[power.Type] = button;
                    powerIndex += 1;
                }
            }

            var towerPanel = TDVisuals.CreateUiBlock(
                _hudRoot.transform,
                "TowerPanel",
                new Vector2(0.01f, 0.06f),
                new Vector2(0.99f, 0.12f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.10f, 0.18f, 0.30f, 0.94f));

            _towerButtons.Clear();
            var towerCount = _allowedTowers.Count;
            if (towerCount == 0)
            {
                var lockedLabel = TDVisuals.CreateText(towerPanel.transform, "LockedLabel", "No towers available in this mission.", 22, TextAnchor.MiddleCenter, new Color(0.82f, 0.86f, 0.92f));
                SetRect(lockedLabel.rectTransform, new Vector2(0.02f, 0.14f), new Vector2(0.98f, 0.86f), Vector2.zero, Vector2.zero);
            }
            else
            {
                for (var i = 0; i < towerCount; i++)
                {
                    var towerType = _allowedTowers[i];
                    var tower = TDData.Towers[towerType];
                    var button = TDVisuals.CreateButton(
                        towerPanel.transform,
                        tower.Name + "Button",
                        GetTowerEmoji(tower.Type) + " " + tower.Name.ToUpperInvariant(),
                        new Color(tower.Accent.r * 0.44f, tower.Accent.g * 0.44f, tower.Accent.b * 0.44f, 1f),
                        new Color(1f, 0.86f, 0.18f));
                    var start = i * (1f / towerCount) + 0.006f;
                    var end = (i + 1) * (1f / towerCount) - 0.006f;
                    SetRect(button.GetComponent<RectTransform>(), new Vector2(start, 0.08f), new Vector2(end, 0.92f), Vector2.zero, Vector2.zero);
                    button.GetComponentInChildren<Text>().text = GetTowerEmoji(tower.Type) + " " + tower.Name + "\n<size=18>" + tower.Cost + "g</size>";
                    button.onClick.AddListener(() => BeginPlacement(towerType));
                    _towerButtons[towerType] = button;
                }
            }

            var upgradesPanel = TDVisuals.CreateUiBlock(
                _hudRoot.transform,
                "UpgradesPanel",
                new Vector2(0.01f, 0.005f),
                new Vector2(0.80f, 0.055f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.08f, 0.20f, 0.38f, 0.94f));

            _upgradePlayerDamageButton = null;
            _upgradePlayerSpeedButton = null;
            _upgradePlayerHpButton = null;

            _upgradeBaseHpButton = TDVisuals.CreateButton(upgradesPanel.transform, "UpgradeBase", "\uD83C\uDFF0 BASE", new Color(0.25f, 0.60f, 0.92f), new Color(1f, 0.86f, 0.18f));
            SetRect(_upgradeBaseHpButton.GetComponent<RectTransform>(), new Vector2(0.01f, 0.08f), new Vector2(0.99f, 0.92f), Vector2.zero, Vector2.zero);
            _upgradeBaseHpButton.onClick.AddListener(UpgradeBaseHp);

            _selectionPanel = TDVisuals.CreateUiBlock(
                _hudRoot.transform,
                "SelectionPanel",
                new Vector2(0.78f, 0.20f),
                new Vector2(0.99f, 0.58f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.12f, 0.20f, 0.32f, 0.96f));

            _selectedTowerText = TDVisuals.CreateText(_selectionPanel.transform, "SelectedTitle", "No Tower Selected", 28, TextAnchor.UpperLeft, Color.white);
            _selectedTowerText.fontStyle = FontStyle.Bold;
            SetRect(_selectedTowerText.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.95f), Vector2.zero, Vector2.zero);

            _selectedTowerStatsText = TDVisuals.CreateText(_selectionPanel.transform, "SelectedStats", "Tap a tower to inspect and upgrade it.", 21, TextAnchor.UpperLeft, new Color(0.84f, 0.90f, 0.96f));
            SetRect(_selectedTowerStatsText.rectTransform, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.84f), Vector2.zero, Vector2.zero);

            _upgradeButton = TDVisuals.CreateButton(_selectionPanel.transform, "UpgradeButton", "UPGRADE", new Color(0.20f, 0.54f, 0.90f), new Color(1f, 0.86f, 0.18f));
            SetRect(_upgradeButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.46f), Vector2.zero, Vector2.zero);
            _upgradeButton.onClick.AddListener(UpgradeSelectedTower);

            _specializeAlphaButton = TDVisuals.CreateButton(_selectionPanel.transform, "SpecializeAlphaButton", "A PATH", new Color(0.20f, 0.42f, 0.72f), Color.white);
            SetRect(_specializeAlphaButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.34f), Vector2.zero, Vector2.zero);
            _specializeAlphaButton.onClick.AddListener(() => SpecializeSelectedTower(1));

            _specializeBetaButton = TDVisuals.CreateButton(_selectionPanel.transform, "SpecializeBetaButton", "B PATH", new Color(0.24f, 0.20f, 0.56f), Color.white);
            SetRect(_specializeBetaButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.22f), Vector2.zero, Vector2.zero);
            _specializeBetaButton.onClick.AddListener(() => SpecializeSelectedTower(2));

            _sellButton = TDVisuals.CreateButton(_selectionPanel.transform, "SellButton", "SELL", new Color(0.80f, 0.32f, 0.26f), Color.white);
            SetRect(_sellButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.10f), Vector2.zero, Vector2.zero);
            _sellButton.onClick.AddListener(SellSelectedTower);

            SetSpecializationButtonsVisible(false);

            _bannerText = TDVisuals.CreateText(_hudRoot.transform, "Banner", "", 42, TextAnchor.MiddleCenter, Color.white);
            _bannerText.fontStyle = FontStyle.Bold;
            SetRect(_bannerText.rectTransform, new Vector2(0.24f, 0.83f), new Vector2(0.76f, 0.88f), Vector2.zero, Vector2.zero);

            _hintText = TDVisuals.CreateText(_hudRoot.transform, "Hint", "", 22, TextAnchor.MiddleCenter, new Color(0.84f, 0.90f, 0.96f));
            SetRect(_hintText.rectTransform, new Vector2(0.22f, 0.80f), new Vector2(0.78f, 0.84f), Vector2.zero, Vector2.zero);
        }

        private Text CreateHudValue(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var block = TDVisuals.CreateUiBlock(parent, label + "Block", anchorMin, anchorMax, Vector2.zero, Vector2.zero, new Color(0.12f, 0.24f, 0.38f, 0.96f));
            var labelText = TDVisuals.CreateText(block.transform, "Label", label, 16, TextAnchor.UpperCenter, new Color(0.84f, 0.90f, 0.98f));
            SetRect(labelText.rectTransform, new Vector2(0f, 0.56f), new Vector2(1f, 0.96f), Vector2.zero, Vector2.zero);
            var valueText = TDVisuals.CreateText(block.transform, "Value", "0", 30, TextAnchor.LowerCenter, new Color(1f, 0.86f, 0.18f));
            valueText.fontStyle = FontStyle.Bold;
            SetRect(valueText.rectTransform, new Vector2(0f, 0.06f), new Vector2(1f, 0.68f), Vector2.zero, Vector2.zero);
            return valueText;
        }

        private void UpdateRun(float dt)
        {
            if (_bannerTimer > 0f)
            {
                _bannerTimer = Mathf.Max(0f, _bannerTimer - Time.deltaTime);
                var alpha = Mathf.Clamp01(_bannerTimer / 0.35f);
                if (_bannerText != null)
                {
                    _bannerText.color = new Color(1f, 1f, 1f, Mathf.Max(alpha, 0.35f));
                }
                if (_hintText != null)
                {
                    _hintText.color = new Color(0.84f, 0.90f, 0.96f, Mathf.Max(alpha, 0.25f));
                }
            }

            if (_comboTimer > 0f)
            {
                _comboTimer -= dt;
                if (_comboTimer <= 0f)
                {
                    if (_comboCount >= 5)
                    {
                        var comboBonus = _comboCount * 2;
                        GainGold(comboBonus);
                        _score += comboBonus;
                    }

                    _comboCount = 0;
                    _comboMultiplier = 1f;
                }
            }

            UpdatePowerCooldowns(dt);
            UpdateEnemies(dt);
            UpdateTowers(dt);
            UpdateProjectiles(dt);
            UpdateFootprints(dt);

            if (_state != RuntimeState.Playing)
            {
                return;
            }

            if (_baseHp <= 0)
            {
                OnGameOver();
                return;
            }

            if (_intermissionTimer > 0f)
            {
                _intermissionTimer -= dt;
                if (_intermissionTimer <= 0f)
                {
                    StartNextWave();
                }
            }
            else if (_spawnQueue.Count > 0)
            {
                _spawnTimer -= dt;
                while (_spawnTimer <= 0f && _spawnQueue.Count > 0)
                {
                    SpawnEnemy(_spawnQueue.Dequeue());
                    var interval = Mathf.Max(0.22f, 0.68f - _wave * 0.014f);
                    if (_currentWaveModifier == TDWaveModifier.Swarm)
                    {
                        interval *= 0.65f;
                    }

                    if (_mode == GameMode.BossRush)
                    {
                        interval *= 0.8f;
                    }

                    _spawnTimer += interval;
                }
            }
            else if (_enemies.Count == 0)
            {
                OnWaveCleared();
            }
        }

        private void UpdatePowerCooldowns(float dt)
        {
            var keys = new List<PowerType>(_powerCooldowns.Keys);
            foreach (var key in keys)
            {
                _powerCooldowns[key] = Mathf.Max(0f, _powerCooldowns[key] - dt);
            }
        }

        private void GainGold(int amount, bool trackLifetime = true)
        {
            if (amount <= 0)
            {
                return;
            }

            _gold += amount;
            if (trackLifetime)
            {
                _goldEarnedThisRun += amount;
            }
        }

        private void UpdateEnemies(float dt)
        {
            for (var i = _enemies.Count - 1; i >= 0; i--)
            {
                var enemy = _enemies[i];
                if (enemy.Removed || enemy.Root == null)
                {
                    _enemies.RemoveAt(i);
                    continue;
                }

                if (enemy.BurnTimer > 0f)
                {
                    enemy.BurnTimer = Mathf.Max(0f, enemy.BurnTimer - dt);
                    DamageEnemy(enemy, enemy.BurnDps * dt);
                }

                if (enemy.PoisonTimer > 0f)
                {
                    enemy.PoisonTimer = Mathf.Max(0f, enemy.PoisonTimer - dt);
                    DamageEnemy(enemy, enemy.PoisonDps * dt);
                }

                if (enemy.Removed || enemy.Root == null)
                {
                    continue;
                }

                if (enemy.RegenRate > 0f && enemy.Health < enemy.MaxHealth)
                {
                    enemy.Health = Mathf.Min(enemy.MaxHealth, enemy.Health + enemy.RegenRate * dt);
                }

                if (enemy.SlowTimer > 0f)
                {
                    enemy.SlowTimer = Mathf.Max(0f, enemy.SlowTimer - dt);
                }
                else
                {
                    enemy.SlowMultiplier = 1f;
                }

                if (enemy.ShieldTimer > 0f)
                {
                    enemy.ShieldTimer = Mathf.Max(0f, enemy.ShieldTimer - dt);
                }

                if (enemy.RoarBoostTimer > 0f)
                {
                    enemy.RoarBoostTimer = Mathf.Max(0f, enemy.RoarBoostTimer - dt);
                    if (enemy.RoarBoostTimer <= 0f)
                    {
                        enemy.RoarSpeedBoost = 1f;
                    }
                }

                if (enemy.BossType.HasValue)
                {
                    enemy.BossAbilityTimer -= dt;
                    if (enemy.IsCharging)
                    {
                        enemy.ChargeTimer -= dt;
                        if (enemy.ChargeTimer <= 0f)
                        {
                            enemy.IsCharging = false;
                        }
                    }

                    if (enemy.BossAbilityTimer <= 0f)
                    {
                        enemy.BossAbilityTimer = enemy.BossAbilityCooldown;
                        ExecuteBossAbility(enemy);
                    }
                }

                if (enemy.Removed || enemy.Root == null)
                {
                    continue;
                }

                var moveMultiplier = enemy.SlowMultiplier * enemy.RoarSpeedBoost * (enemy.IsCharging ? 3f : 1f);
                var moveDistance = enemy.Speed * moveMultiplier * dt;
                while (moveDistance > 0f && !enemy.Removed)
                {
                    if (enemy.PathIndex >= enemy.Path.Count - 1)
                    {
                        ReachBase(enemy);
                        break;
                    }

                    var start = enemy.Path[enemy.PathIndex];
                    var end = enemy.Path[enemy.PathIndex + 1];
                    var segmentLength = Vector2.Distance(start, end);
                    if (segmentLength <= 0.001f)
                    {
                        enemy.PathIndex += 1;
                        enemy.Progress = 0f;
                        continue;
                    }

                    var segmentRemaining = segmentLength * (1f - enemy.Progress);
                    if (moveDistance < segmentRemaining)
                    {
                        enemy.Progress += moveDistance / segmentLength;
                        moveDistance = 0f;
                    }
                    else
                    {
                        moveDistance -= segmentRemaining;
                        enemy.PathIndex += 1;
                        enemy.Progress = 0f;
                    }
                }

                if (enemy.Removed)
                {
                    continue;
                }

                var position = enemy.PathIndex >= enemy.Path.Count - 1
                    ? enemy.Path[enemy.Path.Count - 1]
                    : Vector2.Lerp(enemy.Path[enemy.PathIndex], enemy.Path[enemy.PathIndex + 1], enemy.Progress);
                enemy.Root.transform.position = position;

                var modelScale = enemy.BossType.HasValue
                    ? 1.50f
                    : enemy.IsElite
                        ? 1.26f
                        : enemy.Type == EnemyArchetype.Brute || enemy.Type == EnemyArchetype.ArmoredGolem
                            ? 1.20f
                            : 1.12f;
                var spriteBaseScale = TDArtLibrary.GetEnemySpriteScale(enemy.Type, enemy.BossType.HasValue);
                var bodyPulse = 1f + Mathf.Sin(Time.time * 4.1f + enemy.Seed) * 0.05f;
                var corePulse = 1f + Mathf.Sin(Time.time * 6.2f + enemy.Seed * 1.1f) * 0.08f;
                enemy.Body.transform.localScale = Vector3.one * spriteBaseScale * modelScale * bodyPulse;
                enemy.Core.transform.localScale = Vector3.one * modelScale * 0.24f * corePulse;
                if (enemy.Shell != null)
                {
                    enemy.Shell.localScale = new Vector3(0.66f, 0.30f, 1f) * modelScale * (1f + Mathf.Sin(Time.time * 5.4f + enemy.Seed * 0.8f) * 0.03f);
                    enemy.Shell.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.4f + enemy.Seed) * 6f);
                }
                if (enemy.Eye != null)
                {
                    enemy.Eye.localScale = new Vector3(0.08f, 0.08f, 1f) * modelScale * (1f + Mathf.Sin(Time.time * 7.6f + enemy.Seed) * 0.08f);
                }
                if (enemy.BarRoot != null)
                {
                    var bodyHeight = enemy.Body.sprite != null
                        ? enemy.Body.sprite.bounds.size.y * enemy.Body.transform.localScale.y
                        : 0.82f * modelScale;
                    var barYOffset = bodyHeight * 0.58f + (enemy.BossType.HasValue ? 0.28f : 0.22f);
                    enemy.BarRoot.localPosition = new Vector3(0f, barYOffset, 0f);
                }

                enemy.FootstepTimer -= dt;
                if (enemy.FootstepTimer <= 0f)
                {
                    var forward = GetEnemyForwardDirection(enemy);
                    SpawnFootprint(enemy, position, forward);
                    var cadence = Mathf.Clamp(0.24f - enemy.Speed * 0.04f, 0.08f, 0.24f);
                    enemy.FootstepTimer = cadence + UnityEngine.Random.Range(0f, 0.03f);
                }

                var fill = Mathf.Clamp01(enemy.Health / enemy.MaxHealth);
                var barWidth = Mathf.Max(0.01f, enemy.BarWidth);
                enemy.BarFill.localScale = new Vector3(barWidth * fill, 0.08f, 1f);
                enemy.BarFill.localPosition = new Vector3(-barWidth * 0.5f + (barWidth * fill * 0.5f), 0f, 0f);

                var fillRenderer = enemy.BarFill.GetComponent<SpriteRenderer>();
                if (fillRenderer != null)
                {
                    fillRenderer.color = enemy.ShieldTimer > 0f
                        ? new Color(0.46f, 0.82f, 1f)
                        : enemy.IsElite
                            ? new Color(1f, 0.88f, 0.42f)
                            : new Color(0.34f, 0.96f, 0.56f);
                }
            }
        }

        private Vector2 GetEnemyForwardDirection(EnemyActor enemy)
        {
            if (enemy.PathIndex < enemy.Path.Count - 1)
            {
                return (enemy.Path[enemy.PathIndex + 1] - enemy.Path[enemy.PathIndex]).normalized;
            }

            if (enemy.PathIndex > 0 && enemy.PathIndex < enemy.Path.Count)
            {
                return (enemy.Path[enemy.PathIndex] - enemy.Path[enemy.PathIndex - 1]).normalized;
            }

            return Vector2.up;
        }

        private void SpawnFootprint(EnemyActor enemy, Vector2 position, Vector2 forward)
        {
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var modelScale = enemy.BossType.HasValue ? 1.50f : enemy.IsElite ? 1.26f : enemy.Type == EnemyArchetype.Brute ? 1.20f : 1.12f;
            var side = new Vector2(-forward.y, forward.x);
            var offset = (enemy.LeftStep ? -0.10f : 0.10f) * modelScale;
            var footprintPosition = position + side * offset + forward * -0.04f;
            var footprintColor = new Color(0.10f, 0.08f, 0.06f, enemy.Type == EnemyArchetype.Wisp || enemy.Type == EnemyArchetype.Shadow ? 0.18f : 0.32f);
            var footprint = TDVisuals.CreateWorldSprite(
                "Footprint",
                _worldRoot,
                footprintPosition,
                new Vector2(0.07f, 0.16f) * modelScale,
                footprintColor,
                -8);
            footprint.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - 90f + UnityEngine.Random.Range(-9f, 9f));

            var renderer = footprint.GetComponent<SpriteRenderer>();
            var decal = new FootprintDecal
            {
                Root = footprint,
                Renderer = renderer,
                Life = 1.55f,
                MaxLife = 1.55f,
                BaseColor = footprintColor,
                BaseScale = footprint.transform.localScale
            };
            _footprints.Add(decal);
            enemy.LeftStep = !enemy.LeftStep;

            if (_footprints.Count > 420)
            {
                var oldest = _footprints[0];
                if (oldest.Root != null)
                {
                    Destroy(oldest.Root);
                }
                _footprints.RemoveAt(0);
            }
        }

        private void UpdateTowers(float dt)
        {
            foreach (var tower in _towers)
            {
                if (tower.Root == null || tower.Top == null || tower.Glow == null)
                {
                    continue;
                }

                tower.Cooldown = Mathf.Max(0f, tower.Cooldown - dt);
                tower.AbilityTimer = Mathf.Max(0f, tower.AbilityTimer - dt);
                var synergyMultiplier = GetTowerSynergyMultiplier(tower);
                var synergyStacks = Mathf.Clamp(Mathf.RoundToInt((synergyMultiplier - 1f) / 0.1f), 0, 3);

                var target = PickTarget(tower);
                if (target != null)
                {
                    if (TDArtLibrary.TowerUsesAimRotation(tower.Definition.Type))
                    {
                        var direction = target.Root.transform.position - tower.Root.transform.position;
                        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                        tower.Top.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    }
                }

                var glowScale = 1f + Mathf.Sin(Time.time * 4.2f + tower.Seed) * 0.08f;
                tower.Glow.transform.localScale = new Vector3(1.5f, 1.5f, 1f) * glowScale;
                var glowRenderer = tower.Glow.GetComponent<SpriteRenderer>();
                if (glowRenderer != null)
                {
                    var c = tower.Definition.Accent;
                    glowRenderer.color = new Color(c.r, c.g, c.b, 0.15f + synergyStacks * 0.06f);
                }
                if (tower.Ring != null)
                {
                    tower.Ring.localRotation = Quaternion.Euler(0f, 0f, Time.time * (tower.Specialization == 2 ? -76f : 48f) + tower.Seed * 10f);
                }
                if (tower.Prism != null)
                {
                    var prismPulse = 1f + Mathf.Sin(Time.time * 6.4f + tower.Seed) * 0.12f;
                    tower.Prism.localScale = new Vector3(0.22f, 0.30f, 1f) * prismPulse;
                }

                if (target != null && tower.Cooldown <= 0f)
                {
                    FireTower(tower, target, synergyMultiplier);
                    tower.Cooldown = 1f / Mathf.Max(0.15f, tower.FireRate);
                }

                if (_enemies.Count > 0 && tower.AbilityTimer <= 0f)
                {
                    TriggerTowerAbility(tower, synergyMultiplier);
                    tower.AbilityTimer = GetTowerAbilityCooldown(tower);
                }
            }
        }

        private EnemyActor? PickTarget(TowerActor tower)
        {
            if (tower.Root == null)
            {
                return null;
            }

            EnemyActor? best = null;
            var bestScore = float.MinValue;
            var towerPosition = (Vector2)tower.Root.transform.position;
            var effectiveRange = GetEffectiveTowerRange(tower);
            foreach (var enemy in _enemies)
            {
                if (enemy.Removed || enemy.Root == null)
                {
                    continue;
                }

                var distance = Vector2.Distance(towerPosition, enemy.Root.transform.position);
                if (distance > effectiveRange)
                {
                    continue;
                }

                var score = enemy.PathIndex * 10f + enemy.Progress - distance * 0.05f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }

            return best;
        }

        private void FireTower(TowerActor tower, EnemyActor enemy, float synergyMultiplier)
        {
            if (enemy.Removed || enemy.Root == null || tower.Root == null)
            {
                return;
            }

            var start = (Vector2)tower.Root.transform.position;
            var end = (Vector2)enemy.Root.transform.position;
            var definition = tower.Definition;
            SpawnProjectileVisual(start, end, definition.Accent, definition.ProjectileSpeed, definition.Type == TowerType.Cannon ? 0.55f : 0.22f);
            var direction = (end - start).normalized;
            TDVisuals.SpawnBurstParticles(_worldRoot, "TowerMuzzleBurst", start + direction * 0.18f, definition.Accent, 6, 1.2f, 0.35f, 0.09f, 16);
            TDVisuals.SpawnBurstParticles(_worldRoot, "TowerImpactBurst", end, definition.Accent, definition.Type == TowerType.Cannon ? 14 : 8, definition.Type == TowerType.Cannon ? 2.4f : 1.5f, 0.42f, definition.Type == TowerType.Cannon ? 0.14f : 0.10f, 16);

            switch (definition.Type)
            {
                case TowerType.Arrow:
                    var arrowDamage = tower.Damage;
                    if (tower.Specialization == 1)
                    {
                        arrowDamage *= 1.22f;
                    }
                    else if (tower.Specialization == 2)
                    {
                        arrowDamage *= 0.92f;
                    }
                    DamageEnemyWithTower(enemy, tower, arrowDamage, synergyMultiplier);
                    break;
                case TowerType.Magic:
                    var magicDamage = tower.Damage * 1.18f;
                    if (tower.Specialization == 1)
                    {
                        magicDamage *= 1.35f;
                    }
                    else if (tower.Specialization == 2)
                    {
                        magicDamage *= 0.95f;
                    }
                    DamageEnemyWithTower(enemy, tower, magicDamage, synergyMultiplier);
                    break;
                case TowerType.Cannon:
                    var cannonRadius = tower.Specialization == 1 ? 1.75f : 1.35f;
                    var cannonDamage = tower.Damage * (tower.Specialization == 1 ? 1.12f : 1f);
                    SplashDamage(end, cannonRadius, cannonDamage, tower, synergyMultiplier);
                    if (tower.Specialization == 2)
                    {
                        foreach (var candidate in new List<EnemyActor>(_enemies))
                        {
                            if (!candidate.Removed && candidate.Root != null && Vector2.Distance(end, candidate.Root.transform.position) <= cannonRadius)
                            {
                                ApplyBurn(candidate, tower.Damage * synergyMultiplier * 0.42f, 2.2f);
                            }
                        }
                    }
                    break;
                case TowerType.Poison:
                    DamageEnemyWithTower(enemy, tower, tower.Damage * (tower.Specialization == 1 ? 1.08f : 1f), synergyMultiplier);
                    enemy.PoisonTimer = Mathf.Max(enemy.PoisonTimer, tower.Specialization == 1 ? 6f : 4f);
                    enemy.PoisonDps = Mathf.Max(enemy.PoisonDps, tower.Damage * synergyMultiplier * (tower.Specialization == 1 ? 0.82f : 0.52f));
                    if (tower.Specialization == 2)
                    {
                        enemy.SlowMultiplier = Mathf.Min(enemy.SlowMultiplier, 0.88f);
                        enemy.SlowTimer = Mathf.Max(enemy.SlowTimer, 1.8f);
                    }
                    break;
                case TowerType.Tesla:
                    DamageEnemyWithTower(enemy, tower, tower.Damage, synergyMultiplier);
                    if (tower.Specialization == 2)
                    {
                        enemy.SlowMultiplier = Mathf.Min(enemy.SlowMultiplier, 0.66f);
                        enemy.SlowTimer = Mathf.Max(enemy.SlowTimer, 1.2f);
                    }
                    ChainDamage(
                        enemy,
                        tower.Damage * (tower.Specialization == 1 ? 0.72f : 0.65f),
                        tower.Specialization == 1 ? 2.4f : 1.9f,
                        tower.Specialization == 1 ? 4 : 2,
                        tower,
                        synergyMultiplier);
                    break;
                case TowerType.Ice:
                    var iceDamage = tower.Damage;
                    if (tower.Specialization == 2 && enemy.SlowTimer > 0f)
                    {
                        iceDamage *= 1.8f;
                    }
                    SplashSlow(
                        end,
                        tower.Specialization == 1 ? 1.45f : 1.25f,
                        tower.Specialization == 1 ? iceDamage * 0.82f : iceDamage,
                        ApplyIceSlowBonus(tower.Specialization == 1 ? 0.34f : 0.48f),
                        tower.Specialization == 1 ? 2.4f : 1.8f,
                        tower,
                        synergyMultiplier);
                    break;
                case TowerType.Flame:
                    var flameRadius = tower.Specialization == 2 ? 1.50f : 1.05f;
                    var flameImpact = tower.Damage * (tower.Specialization == 2 ? 0.68f : 0.72f);
                    SplashDamage(end, flameRadius, flameImpact, tower, synergyMultiplier);
                    foreach (var candidate in new List<EnemyActor>(_enemies))
                    {
                        if (!candidate.Removed && candidate.Root != null && Vector2.Distance(end, candidate.Root.transform.position) <= flameRadius)
                        {
                            ApplyBurn(
                                candidate,
                                tower.Damage * synergyMultiplier * (tower.Specialization == 1 ? 0.84f : 0.55f),
                                tower.Specialization == 1 ? 3.8f : 2.4f);
                        }
                    }
                    break;
            }
        }

        private void TriggerTowerAbility(TowerActor tower, float synergyMultiplier)
        {
            var target = PickTarget(tower);
            if (target == null)
            {
                return;
            }

            var origin = (Vector2)target.Root.transform.position;
            TDVisuals.SpawnBurstParticles(_worldRoot, "TowerAbilityBurst", origin, tower.Definition.Accent, 12, 2.0f, 0.58f, 0.15f, 17);
            switch (tower.Definition.Type)
            {
                case TowerType.Arrow:
                    var arrowTargets = new List<EnemyActor>();
                    var effectiveRange = GetEffectiveTowerRange(tower);
                    foreach (var enemy in _enemies)
                    {
                        if (!enemy.Removed && enemy.Root != null && Vector2.Distance(origin, enemy.Root.transform.position) <= effectiveRange + 1f)
                        {
                            arrowTargets.Add(enemy);
                        }
                    }
                    arrowTargets.Sort((left, right) => Vector2.Distance(origin, left.Root.transform.position).CompareTo(Vector2.Distance(origin, right.Root.transform.position)));
                    var volleyCount = tower.Specialization == 2 ? 5 : tower.Specialization == 1 ? 2 : 3;
                    var volleyDamage = tower.Specialization == 2 ? tower.Damage * 0.64f : tower.Specialization == 1 ? tower.Damage * 1.30f : tower.Damage * 0.86f;
                    for (var i = 0; i < Mathf.Min(volleyCount, arrowTargets.Count); i++)
                    {
                        SpawnProjectileVisual((Vector2)tower.Root.transform.position, arrowTargets[i].Root.transform.position, tower.Definition.Accent, tower.Definition.ProjectileSpeed * 1.2f, 0.12f);
                        DamageEnemyWithTower(arrowTargets[i], tower, volleyDamage, synergyMultiplier);
                    }
                    break;
                case TowerType.Magic:
                    SplashDamage(
                        origin,
                        tower.Specialization == 2 ? 2.05f : 1.45f,
                        tower.Damage * (tower.Specialization == 1 ? 1.75f : 1.35f),
                        tower,
                        synergyMultiplier);
                    break;
                case TowerType.Cannon:
                    var abilityRadius = tower.Specialization == 1 ? 2.35f : 1.95f;
                    var abilityDamage = tower.Damage * (tower.Specialization == 1 ? 1.95f : 1.55f);
                    SplashDamage(origin, abilityRadius, abilityDamage, tower, synergyMultiplier);
                    if (tower.Specialization == 2)
                    {
                        foreach (var enemy in new List<EnemyActor>(_enemies))
                        {
                            if (!enemy.Removed && enemy.Root != null && Vector2.Distance(origin, enemy.Root.transform.position) <= abilityRadius)
                            {
                                ApplyBurn(enemy, tower.Damage * synergyMultiplier * 0.52f, 3.1f);
                            }
                        }
                    }
                    break;
                case TowerType.Poison:
                    foreach (var enemy in new List<EnemyActor>(_enemies))
                    {
                        var poisonRadius = tower.Specialization == 2 ? 2.7f : 2.1f;
                        if (!enemy.Removed && enemy.Root != null && Vector2.Distance(origin, enemy.Root.transform.position) <= poisonRadius)
                        {
                            DamageEnemyWithTower(enemy, tower, tower.Damage * 0.42f, synergyMultiplier);
                            enemy.PoisonTimer = Mathf.Max(enemy.PoisonTimer, tower.Specialization == 1 ? 7f : 5f);
                            enemy.PoisonDps = Mathf.Max(enemy.PoisonDps, tower.Damage * synergyMultiplier * (tower.Specialization == 1 ? 0.95f : 0.7f));
                            if (tower.Specialization == 2)
                            {
                                enemy.SlowMultiplier = Mathf.Min(enemy.SlowMultiplier, 0.72f);
                                enemy.SlowTimer = Mathf.Max(enemy.SlowTimer, 2.6f);
                            }
                        }
                    }
                    break;
                case TowerType.Tesla:
                    ChainDamage(
                        target,
                        tower.Damage * (tower.Specialization == 1 ? 1.15f : 0.95f),
                        tower.Specialization == 1 ? 2.8f : 2.3f,
                        tower.Specialization == 1 ? 6 : 4,
                        tower,
                        synergyMultiplier);
                    if (tower.Specialization == 2)
                    {
                        foreach (var enemy in new List<EnemyActor>(_enemies))
                        {
                        if (!enemy.Removed && enemy.Root != null && Vector2.Distance(origin, enemy.Root.transform.position) <= 2.2f)
                            {
                                enemy.SlowMultiplier = Mathf.Min(enemy.SlowMultiplier, 0.54f);
                                enemy.SlowTimer = Mathf.Max(enemy.SlowTimer, 2.1f);
                            }
                        }
                    }
                    break;
                case TowerType.Ice:
                    SplashSlow(
                        origin,
                        tower.Specialization == 1 ? 2.6f : 2.25f,
                        tower.Damage * (tower.Specialization == 2 ? 0.62f : 0.35f),
                        ApplyIceSlowBonus(tower.Specialization == 1 ? 0.20f : 0.28f),
                        tower.Specialization == 1 ? 3.8f : 2.8f,
                        tower,
                        synergyMultiplier);
                    break;
                case TowerType.Flame:
                    var flameAbilityRadius = tower.Specialization == 2 ? 2.8f : 2f;
                    foreach (var enemy in new List<EnemyActor>(_enemies))
                    {
                        if (!enemy.Removed && enemy.Root != null && Vector2.Distance(tower.Root.transform.position, enemy.Root.transform.position) <= flameAbilityRadius)
                        {
                            DamageEnemyWithTower(enemy, tower, tower.Damage * (tower.Specialization == 2 ? 0.68f : 0.82f), synergyMultiplier);
                            ApplyBurn(enemy, tower.Damage * synergyMultiplier * (tower.Specialization == 1 ? 1.02f : 0.72f), tower.Specialization == 1 ? 4.6f : 3.4f);
                        }
                    }
                    break;
            }
        }

        private void SplashDamage(
            Vector2 origin,
            float radius,
            float damage,
            TowerActor? sourceTower = null,
            float synergyMultiplier = 1f,
            bool allowCrit = false)
        {
            foreach (var enemy in new List<EnemyActor>(_enemies))
            {
                if (!enemy.Removed && enemy.Root != null && Vector2.Distance(origin, enemy.Root.transform.position) <= radius)
                {
                    if (sourceTower != null)
                    {
                        DamageEnemyWithTower(enemy, sourceTower, damage, synergyMultiplier, allowCrit);
                    }
                    else
                    {
                        DamageEnemy(enemy, damage);
                    }
                }
            }
        }

        private void ChainDamage(
            EnemyActor source,
            float damage,
            float radius,
            int maxChains,
            TowerActor? sourceTower = null,
            float synergyMultiplier = 1f,
            bool allowCrit = false)
        {
            if (source.Removed || source.Root == null)
            {
                return;
            }

            var visited = new HashSet<EnemyActor> { source };
            var current = source;
            var currentDamage = damage;
            for (var chain = 0; chain < maxChains; chain++)
            {
                EnemyActor? next = null;
                var bestDistance = float.MaxValue;
                foreach (var enemy in _enemies)
                {
                    if (enemy.Removed || enemy.Root == null || visited.Contains(enemy))
                    {
                        continue;
                    }

                    var distance = Vector2.Distance(current.Root.transform.position, enemy.Root.transform.position);
                    if (distance <= radius && distance < bestDistance)
                    {
                        bestDistance = distance;
                        next = enemy;
                    }
                }

                if (next == null)
                {
                    break;
                }

                SpawnProjectileVisual(current.Root.transform.position, next.Root.transform.position, new Color(1f, 0.90f, 0.30f), 14f, 0.05f);
                if (sourceTower != null)
                {
                    DamageEnemyWithTower(next, sourceTower, currentDamage, synergyMultiplier, allowCrit);
                }
                else
                {
                    DamageEnemy(next, currentDamage);
                }
                visited.Add(next);
                current = next;
                currentDamage *= 0.82f;
            }
        }

        private void SplashSlow(
            Vector2 origin,
            float radius,
            float damage,
            float slowMultiplier,
            float duration,
            TowerActor? sourceTower = null,
            float synergyMultiplier = 1f,
            bool allowCrit = false)
        {
            foreach (var enemy in new List<EnemyActor>(_enemies))
            {
                if (enemy.Removed || enemy.Root == null || Vector2.Distance(origin, enemy.Root.transform.position) > radius)
                {
                    continue;
                }

                if (sourceTower != null)
                {
                    DamageEnemyWithTower(enemy, sourceTower, damage, synergyMultiplier, allowCrit);
                }
                else
                {
                    DamageEnemy(enemy, damage);
                }
                if (!enemy.Removed && enemy.Root != null)
                {
                    enemy.SlowMultiplier = Mathf.Min(enemy.SlowMultiplier, slowMultiplier);
                    enemy.SlowTimer = Mathf.Max(enemy.SlowTimer, duration);
                }
            }
        }

        private void ApplyBurn(EnemyActor enemy, float dps, float duration)
        {
            if (enemy.Removed || enemy.Root == null)
            {
                return;
            }

            enemy.BurnTimer = Mathf.Max(enemy.BurnTimer, duration);
            enemy.BurnDps = Mathf.Max(enemy.BurnDps, dps);
        }

        private void DamageEnemy(EnemyActor enemy, float damage)
        {
            if (enemy.Removed || enemy.Root == null || damage <= 0f)
            {
                return;
            }

            if (enemy.ShieldTimer > 0f)
            {
                damage *= 0.3f;
            }

            enemy.Health -= damage;
            if (enemy.Health <= 0f)
            {
                KillEnemy(enemy);
            }
        }

        private void KillEnemy(EnemyActor enemy)
        {
            if (enemy.Removed || enemy.Root == null)
            {
                return;
            }

            var deathColor = enemy.Core != null ? enemy.Core.color : new Color(1f, 0.62f, 0.34f);
            var isBoss = enemy.BossType.HasValue || enemy.Type == EnemyArchetype.Boss;
            TDVisuals.SpawnBurstParticles(_worldRoot, "EnemyDeathBurst", enemy.Root.transform.position, deathColor, isBoss ? 32 : 16, isBoss ? 2.8f : 2.0f, isBoss ? 0.95f : 0.65f, isBoss ? 0.18f : 0.12f, 18);

            enemy.Removed = true;
            var reward = Mathf.Max(1, Mathf.RoundToInt(enemy.Reward * _comboMultiplier * _skillGoldBonusMult));
            GainGold(reward);
            _score += reward * 3 + _wave;
            _killsThisRun += 1;
            _comboCount += 1;
            _comboTimer = 2f;
            _comboMultiplier = Mathf.Min(5f, 1f + _comboCount * 0.10f);
            _bestCombo = Mathf.Max(_bestCombo, _comboCount);

            if (enemy.BossType.HasValue)
            {
                _bossesKilledThisRun += 1;
                var diamondDrop = Mathf.Clamp(2 + _wave / 5, 2, 10);
                _diamondsThisRun += diamondDrop;
                UnlockAchievement("boss_kill");
                if (_bossesKilledThisRun >= 5)
                {
                    UnlockAchievement("5_bosses");
                }

                if (_bossesKilledThisRun >= 10)
                {
                    UnlockAchievement("10_bosses");
                }
            }
            else if (RollChance(0.03f + _skillDiamondLuckBonus))
            {
                _diamondsThisRun += 1;
            }

            if (_killsThisRun >= 50)
            {
                UnlockAchievement("kills_50");
            }
            if (_killsThisRun >= 200)
            {
                UnlockAchievement("kills_200");
            }
            if (_killsThisRun >= 500)
            {
                UnlockAchievement("kills_500");
            }
            if (_killsThisRun >= 1000)
            {
                UnlockAchievement("kills_1000");
            }

            if (_comboCount >= 10)
            {
                UnlockAchievement("combo_10");
            }
            if (_comboCount >= 20)
            {
                UnlockAchievement("combo_20");
            }
            if (_score >= 1000)
            {
                UnlockAchievement("score_1000");
            }
            if (_diamondsThisRun >= 10)
            {
                UnlockAchievement("diamond_10");
            }
            if (_diamondsThisRun >= 50)
            {
                UnlockAchievement("diamond_50");
            }

            UnlockAchievement("first_kill");
            Destroy(enemy.Root);
            _enemies.Remove(enemy);
        }

        private void ReachBase(EnemyActor enemy)
        {
            if (enemy.Removed)
            {
                return;
            }

            TDVisuals.SpawnBurstParticles(_worldRoot, "BaseHitBurst", _basePosition + new Vector2(0f, 0.22f), new Color(1f, 0.34f, 0.24f), 26, 2.6f, 0.75f, 0.15f, 18);

            enemy.Removed = true;
            enemy.ReachedBase = true;
            var baseDamage = Mathf.Max(0, Mathf.RoundToInt(enemy.Damage));
            if (baseDamage > 0)
            {
                _baseDamagedThisWave = true;
                _baseDamagedThisRun = true;
            }
            _baseHp -= baseDamage;
            _baseHp = Mathf.Max(0, _baseHp);
            _score = Mathf.Max(0, _score - 12);
            Destroy(enemy.Root);
            _enemies.Remove(enemy);
            if (_baseHp <= 0)
            {
                OnGameOver();
            }
        }

        private void UpdateProjectiles(float dt)
        {
            for (var i = _projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = _projectiles[i];
                projectile.Progress += dt * projectile.Speed / Mathf.Max(0.1f, projectile.Distance);
                var t = Mathf.Clamp01(projectile.Progress);
                var position = Vector2.Lerp(projectile.Start, projectile.End, t);
                position.y += Mathf.Sin(t * Mathf.PI) * projectile.ArcHeight;
                projectile.Root.transform.position = position;
                projectile.Root.transform.localScale = Vector3.one * Mathf.Lerp(0.16f, 0.26f, 1f - Mathf.Abs(0.5f - t) * 2f);

                if (projectile.Progress >= 1f)
                {
                    Destroy(projectile.Root);
                    _projectiles.RemoveAt(i);
                }
            }
        }

        private void UpdateFootprints(float dt)
        {
            for (var i = _footprints.Count - 1; i >= 0; i--)
            {
                var decal = _footprints[i];
                if (decal.Root == null || decal.Renderer == null)
                {
                    _footprints.RemoveAt(i);
                    continue;
                }

                decal.Life -= dt;
                if (decal.Life <= 0f)
                {
                    Destroy(decal.Root);
                    _footprints.RemoveAt(i);
                    continue;
                }

                var t = Mathf.Clamp01(decal.Life / decal.MaxLife);
                var alpha = decal.BaseColor.a * t * t;
                decal.Renderer.color = new Color(decal.BaseColor.r, decal.BaseColor.g, decal.BaseColor.b, alpha);
                decal.Root.transform.localScale = decal.BaseScale * Mathf.Lerp(0.82f, 1f, t);
            }
        }

        private void UpdatePlayer(float dt)
        {
            if (_playerRoot == null)
            {
                return;
            }

            _playerAttackTimer = Mathf.Max(0f, _playerAttackTimer - dt);
            _dashCooldown = Mathf.Max(0f, _dashCooldown - dt);

            var toTarget = _playerTarget - _playerPosition;
            var distance = toTarget.magnitude;
            if (distance > 0.02f)
            {
                var move = Mathf.Min(distance, _playerSpeed * dt);
                _playerPosition += toTarget.normalized * move;
            }

            _playerRoot.transform.position = _playerPosition;

            if (_playerBody != null)
            {
                var pulse = 1f + Mathf.Sin(Time.time * 6.2f) * 0.06f;
                _playerBody.transform.localScale = new Vector3(0.34f, 0.34f, 1f) * pulse;
                _playerBody.color = _dashArmed ? new Color(0.24f, 0.78f, 0.92f) : new Color(0.28f, 0.56f, 0.84f);
            }

            if (_playerCore != null)
            {
                var pulse = 1f + Mathf.Sin(Time.time * 9.4f + 0.7f) * 0.10f;
                _playerCore.transform.localScale = new Vector3(0.16f, 0.16f, 1f) * pulse;
            }

            var effectiveRange = _playerAttackRange * (_currentWaveModifier == TDWaveModifier.Invisible ? 0.7f : 1f);
            EnemyActor? nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var enemy in _enemies)
            {
                if (enemy.Removed || enemy.Root == null)
                {
                    continue;
                }

                var enemyDistance = Vector2.Distance(_playerPosition, enemy.Root.transform.position);
                if (enemyDistance <= effectiveRange && enemyDistance < nearestDistance)
                {
                    nearestDistance = enemyDistance;
                    nearest = enemy;
                }
            }

            if (nearest != null && _playerAttackTimer <= 0f)
            {
                var damage = _playerAttackDamage * GetPrestigeDamageMultiplier();
                DamageEnemy(nearest, damage);
                if (nearest.Root != null)
                {
                    SpawnProjectileVisual(_playerPosition, nearest.Root.transform.position, new Color(0.34f, 0.72f, 1f), 12f, 0.08f);
                    TDVisuals.SpawnBurstParticles(_worldRoot, "PlayerHitBurst", nearest.Root.transform.position, new Color(0.34f, 0.72f, 1f), 8, 1.6f, 0.34f, 0.08f, 19);
                }
                _playerAttackTimer = _playerAttackCooldown;
            }

            UpdatePlayerBars();
        }

        private void UpdatePlayerBars()
        {
            if (_playerHpFill != null)
            {
                var hpRatio = Mathf.Clamp01(_playerHp / Mathf.Max(1f, _playerMaxHp));
                _playerHpFill.localScale = new Vector3(0.52f * hpRatio, 0.07f, 1f);
                _playerHpFill.localPosition = new Vector3(-0.26f + 0.52f * hpRatio * 0.5f, 0f, 0f);
            }

            if (_playerDashFill != null)
            {
                var dashRatio = 1f - Mathf.Clamp01(_dashCooldown / _dashCooldownMax);
                _playerDashFill.localScale = new Vector3(0.52f * dashRatio, 0.05f, 1f);
                _playerDashFill.localPosition = new Vector3(-0.26f + 0.52f * dashRatio * 0.5f, 0f, 0f);
            }
        }

        private void HandleGameplayPointer()
        {
            if (Input.GetMouseButtonDown(1))
            {
                _placingTowerType = null;
                _armedPowerType = null;
                _dashArmed = false;
                ShowBanner("Orders Cleared", _defaultHint, 1.2f);
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (!TryGetPointerDown(out var worldPosition))
            {
                return;
            }

            if (_placingTowerType.HasValue)
            {
                TryPlaceTower(worldPosition, _placingTowerType.Value);
                return;
            }

            if (_armedPowerType.HasValue)
            {
                UseArmedPower(worldPosition);
                return;
            }

            if (SelectTowerAt(worldPosition))
            {
                return;
            }
        }

        private bool TryGetPointerDown(out Vector2 worldPosition)
        {
            worldPosition = default;
            if (!Input.GetMouseButtonDown(0))
            {
                return false;
            }

            var mouse = Input.mousePosition;
            var world = _gameCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -_gameCamera.transform.position.z));
            worldPosition = new Vector2(world.x, world.y);
            return true;
        }

        private void BeginPlacement(TowerType type)
        {
            _placingTowerType = type;
            _armedPowerType = null;
            _dashArmed = false;
            SelectTower(null);
            var definition = TDData.Towers[type];
            ShowBanner("Place " + definition.Name, definition.Description + " Right-click to cancel.", 2.0f);
        }

        private void MovePlayerTo(Vector2 worldPosition)
        {
            var clamped = new Vector2(
                Mathf.Clamp(worldPosition.x, -WorldHalfWidth + 0.4f, WorldHalfWidth - 0.4f),
                Mathf.Clamp(worldPosition.y, -7.4f, WorldHalfHeight - 0.6f));
            _playerTarget = clamped;
            ShowBanner("Move Order", "Player repositioning.", 0.8f);
        }

        private void ArmDash()
        {
            if (_dashCooldown > 0f)
            {
                ShowBanner("Dash Cooling", "Ready in " + _dashCooldown.ToString("0.0") + "s.", 1.2f);
                return;
            }

            _dashArmed = true;
            _placingTowerType = null;
            _armedPowerType = null;
            ShowBanner("Dash Armed", "Click a destination to dash and strike through enemies.", 1.6f);
        }

        private bool TryDashToPosition(Vector2 target)
        {
            if (_dashCooldown > 0f)
            {
                ShowBanner("Dash Cooling", "Ready in " + _dashCooldown.ToString("0.0") + "s.", 1.0f);
                return false;
            }

            var start = _playerPosition;
            var direction = target - start;
            var distance = direction.magnitude;
            if (distance < 0.08f)
            {
                return false;
            }

            var end = start + direction.normalized * Mathf.Min(distance, _dashRange);
            end = new Vector2(
                Mathf.Clamp(end.x, -WorldHalfWidth + 0.4f, WorldHalfWidth - 0.4f),
                Mathf.Clamp(end.y, -7.4f, WorldHalfHeight - 0.6f));

            var dashDamage = _playerAttackDamage * 2.5f * GetPrestigeDamageMultiplier();
            foreach (var enemy in new List<EnemyActor>(_enemies))
            {
                if (enemy.Removed || enemy.Root == null)
                {
                    continue;
                }

                var distanceToDash = DistanceToSegment(enemy.Root.transform.position, start, end);
                if (distanceToDash <= 0.62f)
                {
                    DamageEnemy(enemy, dashDamage);
                    TDVisuals.SpawnBurstParticles(_worldRoot, "DashHit", enemy.Root.transform.position, new Color(0.08f, 0.88f, 0.98f), 8, 2.2f, 0.36f, 0.10f, 19);
                }
            }

            for (var i = 0; i < 10; i++)
            {
                var t = i / 9f;
                var p = Vector2.Lerp(start, end, t);
                TDVisuals.SpawnBurstParticles(_worldRoot, "DashTrail", p, new Color(0.18f, 0.84f, 0.96f, 0.9f), 3, 0.9f, 0.22f, 0.06f, 18);
            }

            _playerPosition = end;
            _playerTarget = end;
            if (_playerRoot != null)
            {
                _playerRoot.transform.position = end;
            }
            _dashCooldown = _dashCooldownMax;
            _dashArmed = false;
            UpdatePlayerBars();
            ShowBanner("Dash", "Blink strike executed.", 0.9f);
            return true;
        }

        private void ArmPower(PowerType type)
        {
            var definition = TDData.Powers[type];
            var cooldown = _powerCooldowns.TryGetValue(type, out var currentCooldown) ? currentCooldown : 0f;
            if (cooldown > 0f)
            {
                ShowBanner(definition.Name + " Cooling", "Ready in " + cooldown.ToString("0.0") + "s.", 1.2f);
                return;
            }

            if (_gold < definition.Cost)
            {
                ShowBanner("Need More Gold", definition.Name + " costs $" + definition.Cost + ".", 1.2f);
                return;
            }

            _armedPowerType = type;
            _placingTowerType = null;
            _dashArmed = false;
            SelectTower(null);
            ShowBanner("Power Armed", definition.Description + " Click the battlefield to cast.", 2.0f);
        }

        private void UseArmedPower(Vector2 worldPosition)
        {
            if (!_armedPowerType.HasValue)
            {
                return;
            }

            var type = _armedPowerType.Value;
            var definition = TDData.Powers[type];
            var cooldown = _powerCooldowns.TryGetValue(type, out var currentCooldown) ? currentCooldown : 0f;
            if (_gold < definition.Cost || cooldown > 0f)
            {
                _armedPowerType = null;
                return;
            }

            var spent = false;
            switch (type)
            {
                case PowerType.Fireball:
                    SplashDamage(worldPosition, 1.75f, 42f + _wave * 1.5f);
                    SpawnProjectileVisual(worldPosition + new Vector2(0f, 2.8f), worldPosition, definition.Color, 12f, 0.32f);
                    TDVisuals.SpawnBurstParticles(_worldRoot, "PowerFireballBurst", worldPosition, definition.Color, 28, 2.8f, 0.9f, 0.18f, 19);
                    spent = true;
                    break;
                case PowerType.Freeze:
                    foreach (var enemy in new List<EnemyActor>(_enemies))
                    {
                        if (!enemy.Removed)
                        {
                            enemy.SlowMultiplier = Mathf.Min(enemy.SlowMultiplier, 0.18f);
                            enemy.SlowTimer = Mathf.Max(enemy.SlowTimer, 2.4f);
                            DamageEnemy(enemy, 4f);
                        }
                    }
                    TDVisuals.SpawnBurstParticles(_worldRoot, "PowerFreezeBurst", worldPosition, definition.Color, 34, 1.6f, 1.1f, 0.16f, 19);
                    spent = true;
                    break;
                case PowerType.Heal:
                    _baseHp = Mathf.Min(_maxBaseHp, _baseHp + 24);
                    TDVisuals.SpawnBurstParticles(_worldRoot, "PowerHealBurst", _basePosition + new Vector2(0f, 0.24f), definition.Color, 22, 1.4f, 1.0f, 0.14f, 19);
                    _repairsThisRun += 1;
                    if (_repairsThisRun >= 3)
                    {
                        UnlockAchievement("repaired_3");
                    }
                    spent = true;
                    break;
                case PowerType.Lightning:
                    EnemyActor? target = null;
                    var bestDistance = float.MaxValue;
                    foreach (var enemy in _enemies)
                    {
                        if (enemy.Removed || enemy.Root == null)
                        {
                            continue;
                        }

                        var distance = Vector2.Distance(worldPosition, enemy.Root.transform.position);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            target = enemy;
                        }
                    }

                    if (target != null)
                    {
                        DamageEnemy(target, 36f + _wave * 1.2f);
                        ChainDamage(target, 24f, 2.2f, 3);
                        TDVisuals.SpawnBurstParticles(_worldRoot, "PowerLightningBurst", target.Root.transform.position, definition.Color, 26, 2.4f, 0.8f, 0.16f, 19);
                        spent = true;
                    }
                    break;
            }

            if (!spent)
            {
                ShowBanner("No Target", "Pick a busier part of the lane.", 1.2f);
                return;
            }

            _gold -= definition.Cost;
            _powerCooldowns[type] = definition.Cooldown;
            _armedPowerType = null;
            _powersUsedThisRun.Add(type);
            UnlockAchievement("use_power");
            if (_powersUsedThisRun.Count >= 4)
            {
                UnlockAchievement("all_powers");
            }

            ShowBanner(definition.Name, "Power deployed.", 1.0f);
        }

        private bool TryPlaceTower(Vector2 position, TowerType type)
        {
            var definition = TDData.Towers[type];
            if (_gold < definition.Cost)
            {
                ShowBanner("Need More Gold", definition.Name + " costs $" + definition.Cost + ".", 1.1f);
                return false;
            }

            if (!CanPlaceTower(position))
            {
                ShowBanner("Invalid Position", "Keep towers off the road and away from each other.", 1.2f);
                return false;
            }

            var tower = CreateTower(position, definition);
            _towers.Add(tower);
            _gold -= definition.Cost;
            _towerTypesPlacedThisRun.Add(type);
            if (_towers.Count >= 5)
            {
                UnlockAchievement("5_towers");
            }
            if (_towers.Count >= 10)
            {
                UnlockAchievement("10_towers");
            }
            if (_towerTypesPlacedThisRun.Count >= TDData.Towers.Count)
            {
                UnlockAchievement("all_tower_types");
            }

            SelectTower(tower);
            ShowBanner(definition.Name + " Built", "Build again or right-click to cancel placement.", 1.0f);
            return true;
        }

        private bool CanPlaceTower(Vector2 position)
        {
            if (position.x < -WorldHalfWidth + 0.6f || position.x > WorldHalfWidth - 0.6f || position.y < -7f || position.y > WorldHalfHeight - 0.8f)
            {
                return false;
            }

            if (Vector2.Distance(position, _basePosition) < 1.35f)
            {
                return false;
            }

            foreach (var tower in _towers)
            {
                if (Vector2.Distance(position, tower.Root.transform.position) < 1.05f)
                {
                    return false;
                }
            }

            foreach (var path in _paths)
            {
                for (var i = 0; i < path.Count - 1; i++)
                {
                    if (DistanceToSegment(position, path[i], path[i + 1]) < 0.8f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private TowerActor CreateTower(Vector2 position, TowerDefinition definition)
        {
            var root = new GameObject(definition.Name + "Tower");
            root.transform.SetParent(_worldRoot, false);
            root.transform.position = position;

            var pedestal = TDVisuals.CreateWorldDisc("Pedestal", root.transform, new Vector2(0f, -0.10f), new Vector2(1.12f, 0.32f), new Color(0f, 0f, 0f, 0.28f), 2);
            pedestal.transform.localPosition = new Vector3(0f, -0.10f, 0f);

            var glow = TDVisuals.CreateWorldDisc("Glow", root.transform, new Vector2(0f, 0.05f), new Vector2(1.52f, 1.52f), new Color(definition.Accent.r, definition.Accent.g, definition.Accent.b, 0.22f), 3);
            glow.transform.localPosition = Vector3.zero;

            var ring = TDVisuals.CreateWorldDisc("Ring", root.transform, new Vector2(0f, 0.00f), new Vector2(1.16f, 0.26f), new Color(definition.Accent.r, definition.Accent.g, definition.Accent.b, 0.50f), 4);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            var baseGo = TDVisuals.CreateWorldDisc("Base", root.transform, Vector2.zero, new Vector2(0.96f, 0.96f), Color.Lerp(definition.Body, new Color(0.10f, 0.12f, 0.16f), 0.25f), 4);
            baseGo.transform.localPosition = Vector3.zero;

            var towerSprite = TDArtLibrary.GetTowerSprite(definition.Type);
            var towerSpriteScale = TDArtLibrary.GetTowerSpriteScale(definition.Type);
            var towerSpriteYOffset = TDArtLibrary.GetTowerSpriteYOffset(definition.Type);
            var coreGo = TDVisuals.CreateWorldSprite("Core", root.transform, new Vector2(0f, towerSpriteYOffset), new Vector2(towerSpriteScale, towerSpriteScale), Color.white, 8);
            coreGo.transform.localPosition = new Vector3(0f, towerSpriteYOffset, 0f);
            var coreRenderer = coreGo.GetComponent<SpriteRenderer>();
            if (coreRenderer != null)
            {
                if (towerSprite != null)
                {
                    coreRenderer.sprite = towerSprite;
                    coreRenderer.color = Color.Lerp(Color.white, definition.Accent, 0.12f);
                }
                else
                {
                    coreRenderer.sprite = TDVisuals.CircleSprite;
                    coreRenderer.color = Color.Lerp(definition.Body, definition.Accent, 0.55f);
                }
            }

            var prism = TDVisuals.CreateWorldDisc("Prism", coreGo.transform, new Vector2(0f, 0.10f), new Vector2(0.26f, 0.16f), new Color(1f, 1f, 1f, 0.42f), 9);
            prism.transform.localPosition = new Vector3(0f, 0.10f, 0f);

            switch (definition.Type)
            {
                case TowerType.Magic:
                case TowerType.Poison:
                case TowerType.Tesla:
                case TowerType.Ice:
                case TowerType.Flame:
                    TDVisuals.CreateLoopParticles(
                        "TowerSignature",
                        coreGo.transform,
                        new Vector2(0f, 0.16f),
                        new Color(definition.Accent.r, definition.Accent.g, definition.Accent.b, 0.58f),
                        new Color(1f, 1f, 1f, 0.24f),
                        0.10f,
                        8f,
                        0.18f,
                        1.2f,
                        0.03f,
                        0.08f,
                        10);
                    break;
            }

            TDVisuals.CreateWorldDisc(
                "TowerHalo",
                root.transform,
                new Vector2(0f, 0.24f),
                new Vector2(0.68f, 0.38f),
                new Color(definition.Accent.r, definition.Accent.g, definition.Accent.b, 0.18f),
                5);

            TDVisuals.CreateLoopParticles(
                "TowerAura",
                root.transform,
                new Vector2(0f, 0.30f),
                new Color(definition.Accent.r, definition.Accent.g, definition.Accent.b, 0.50f),
                new Color(1f, 1f, 1f, 0.28f),
                0.13f,
                9f,
                0.32f,
                1.15f,
                0.05f,
                0.11f,
                6);

            TDVisuals.SpawnBurstParticles(_worldRoot, "TowerPlaceBurst", position + new Vector2(0f, 0.20f), definition.Accent, 16, 1.8f, 0.55f, 0.15f, 7);

            var tower = new TowerActor
            {
                Definition = definition,
                Root = root,
                Glow = glow,
                Base = baseGo.GetComponent<SpriteRenderer>()!,
                Top = coreRenderer!,
                Ring = ring.transform,
                Prism = prism.transform,
                Damage = definition.Damage,
                Range = definition.Range,
                FireRate = definition.FireRate,
                Cooldown = 0.25f,
                AbilityTimer = definition.AbilityCooldown * 0.75f,
                Seed = UnityEngine.Random.value * 9f,
                InvestedCost = definition.Cost
            };

            return tower;
        }

        private bool SelectTowerAt(Vector2 position)
        {
            TowerActor? nearest = null;
            var bestDistance = 0.72f;
            foreach (var tower in _towers)
            {
                var distance = Vector2.Distance(position, tower.Root.transform.position);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    nearest = tower;
                }
            }

            SelectTower(nearest);
            return nearest != null;
        }

        private void SelectTower(TowerActor? tower)
        {
            _selectedTower = tower;
        }

        private void UpgradeSelectedTower()
        {
            if (_selectedTower == null)
            {
                return;
            }

            if (!_upgradesEnabled)
            {
                ShowBanner("Upgrades Locked", "This campaign mission disables upgrades.", 1.2f);
                return;
            }

            if (NeedsSpecializationChoice(_selectedTower))
            {
                ShowBanner("Choose Specialization", "Pick one of the specialization branches first.", 1.2f);
                return;
            }

            var cost = GetUpgradeCost(_selectedTower);
            if (cost <= 0)
            {
                ShowBanner("Max Level", "This tower is already fully upgraded.", 1.0f);
                return;
            }

            if (_gold < cost)
            {
                ShowBanner("Need More Gold", "Upgrade costs $" + cost + ".", 1.0f);
                return;
            }

            _gold -= cost;
            _selectedTower.Level += 1;
            _selectedTower.InvestedCost += cost;
            _selectedTower.Damage *= 1.28f;
            _selectedTower.Range += 0.22f;
            _selectedTower.FireRate *= 1.12f;
            _selectedTower.Base.color = Color.Lerp(_selectedTower.Base.color, Color.white, 0.10f);
            _selectedTower.Top.transform.localScale *= 1.08f;
            if (_selectedTower.Level >= 5)
            {
                UnlockAchievement("max_tower");
            }

            if (NeedsSpecializationChoice(_selectedTower))
            {
                ShowBanner(_selectedTower.Definition.Name + " Max Level", "Choose a specialization branch.", 1.4f);
            }
            else
            {
                ShowBanner(_selectedTower.Definition.Name + " Upgraded", "Level " + _selectedTower.Level + " online.", 1.0f);
            }
        }

        private void SpecializeSelectedTower(int branch)
        {
            if (_selectedTower == null || !NeedsSpecializationChoice(_selectedTower))
            {
                return;
            }

            if (branch != 1 && branch != 2)
            {
                return;
            }

            _selectedTower.Specialization = branch;
            var tower = _selectedTower;
            switch (tower.Definition.Type)
            {
                case TowerType.Arrow:
                    if (branch == 1)
                    {
                        tower.Damage *= 1.24f;
                        tower.Range += 0.65f;
                        tower.FireRate *= 0.82f;
                        tower.AbilityCooldownScale = 1.05f;
                    }
                    else
                    {
                        tower.Damage *= 0.93f;
                        tower.Range += 0.15f;
                        tower.FireRate *= 1.42f;
                        tower.AbilityCooldownScale = 0.78f;
                    }
                    break;
                case TowerType.Magic:
                    if (branch == 1)
                    {
                        tower.Damage *= 1.35f;
                        tower.Range += 0.20f;
                        tower.FireRate *= 0.95f;
                        tower.AbilityCooldownScale = 0.92f;
                    }
                    else
                    {
                        tower.Damage *= 1.05f;
                        tower.Range += 0.48f;
                        tower.FireRate *= 1.08f;
                        tower.AbilityCooldownScale = 0.78f;
                    }
                    break;
                case TowerType.Cannon:
                    if (branch == 1)
                    {
                        tower.Damage *= 1.30f;
                        tower.Range += 0.22f;
                        tower.FireRate *= 0.82f;
                        tower.AbilityCooldownScale = 0.95f;
                    }
                    else
                    {
                        tower.Damage *= 0.96f;
                        tower.Range += 0.40f;
                        tower.FireRate *= 1.12f;
                        tower.AbilityCooldownScale = 0.86f;
                    }
                    break;
                case TowerType.Poison:
                    if (branch == 1)
                    {
                        tower.Damage *= 1.12f;
                        tower.Range += 0.25f;
                        tower.FireRate *= 0.96f;
                        tower.AbilityCooldownScale = 0.82f;
                    }
                    else
                    {
                        tower.Damage *= 0.98f;
                        tower.Range += 0.55f;
                        tower.FireRate *= 1.08f;
                        tower.AbilityCooldownScale = 0.78f;
                    }
                    break;
                case TowerType.Tesla:
                    if (branch == 1)
                    {
                        tower.Damage *= 1.18f;
                        tower.Range += 0.34f;
                        tower.FireRate *= 0.96f;
                        tower.AbilityCooldownScale = 0.84f;
                    }
                    else
                    {
                        tower.Damage *= 0.95f;
                        tower.Range += 0.58f;
                        tower.FireRate *= 1.08f;
                        tower.AbilityCooldownScale = 0.80f;
                    }
                    break;
                case TowerType.Ice:
                    if (branch == 1)
                    {
                        tower.Damage *= 0.92f;
                        tower.Range += 0.56f;
                        tower.FireRate *= 1.04f;
                        tower.AbilityCooldownScale = 0.74f;
                    }
                    else
                    {
                        tower.Damage *= 1.26f;
                        tower.Range += 0.20f;
                        tower.FireRate *= 1.10f;
                        tower.AbilityCooldownScale = 0.86f;
                    }
                    break;
                case TowerType.Flame:
                    if (branch == 1)
                    {
                        tower.Damage *= 1.24f;
                        tower.Range += 0.18f;
                        tower.FireRate *= 0.92f;
                        tower.AbilityCooldownScale = 0.82f;
                    }
                    else
                    {
                        tower.Damage *= 0.96f;
                        tower.Range += 0.46f;
                        tower.FireRate *= 1.15f;
                        tower.AbilityCooldownScale = 0.76f;
                    }
                    break;
            }

            var specializationColor = branch == 1 ? new Color(1f, 0.92f, 0.62f) : new Color(0.70f, 0.88f, 1f);
            tower.Base.color = Color.Lerp(tower.Base.color, specializationColor, 0.30f);
            tower.Top.color = Color.Lerp(tower.Top.color, specializationColor, 0.20f);
            tower.Top.transform.localScale *= 1.12f;
            tower.AbilityTimer = Mathf.Min(tower.AbilityTimer, GetTowerAbilityCooldown(tower));

            ShowBanner(GetSpecializationName(tower, branch), GetSpecializationSummary(tower, branch), 1.4f);
        }

        private void SellSelectedTower()
        {
            if (_selectedTower == null)
            {
                return;
            }

            var sellValue = Mathf.RoundToInt(_selectedTower.InvestedCost * 0.65f * (1f + _skillSellBonus));
            GainGold(sellValue);
            _towers.Remove(_selectedTower);
            Destroy(_selectedTower.Root);
            ShowBanner("Tower Sold", "Resources recovered for a new layout.", 1.0f);
            SelectTower(null);
        }

        private int GetPlayerDamageUpgradeCost()
        {
            return _playerDamageLevel * 25;
        }

        private int GetPlayerSpeedUpgradeCost()
        {
            return _playerSpeedLevel * 20;
        }

        private int GetPlayerHpUpgradeCost()
        {
            return _playerHpLevel * 30;
        }

        private int GetBaseHpUpgradeCost()
        {
            return _baseHpLevel * 40;
        }

        private void UpgradePlayerDamage()
        {
            if (!_upgradesEnabled)
            {
                ShowBanner("Upgrades Locked", "This mission disables upgrades.", 1.0f);
                return;
            }

            var cost = GetPlayerDamageUpgradeCost();
            if (_gold < cost)
            {
                ShowBanner("Need More Gold", "Damage upgrade costs $" + cost + ".", 1.0f);
                return;
            }

            _gold -= cost;
            _playerDamageLevel += 1;
            _playerAttackDamage += 5f;
            CheckUpgradeAllAchievement();
            ShowBanner("Player Damage", "Damage upgraded to " + _playerAttackDamage.ToString("0") + ".", 1.0f);
        }

        private void UpgradePlayerSpeed()
        {
            if (!_upgradesEnabled)
            {
                ShowBanner("Upgrades Locked", "This mission disables upgrades.", 1.0f);
                return;
            }

            var cost = GetPlayerSpeedUpgradeCost();
            if (_gold < cost)
            {
                ShowBanner("Need More Gold", "Speed upgrade costs $" + cost + ".", 1.0f);
                return;
            }

            _gold -= cost;
            _playerSpeedLevel += 1;
            _playerSpeed += 0.45f;
            CheckUpgradeAllAchievement();
            ShowBanner("Player Speed", "Move speed upgraded.", 1.0f);
        }

        private void UpgradePlayerHp()
        {
            if (!_upgradesEnabled)
            {
                ShowBanner("Upgrades Locked", "This mission disables upgrades.", 1.0f);
                return;
            }

            var cost = GetPlayerHpUpgradeCost();
            if (_gold < cost)
            {
                ShowBanner("Need More Gold", "Health upgrade costs $" + cost + ".", 1.0f);
                return;
            }

            _gold -= cost;
            _playerHpLevel += 1;
            _playerMaxHp += 25f;
            _playerHp = _playerMaxHp;
            UpdatePlayerBars();
            CheckUpgradeAllAchievement();
            ShowBanner("Player Vitality", "Player max HP increased.", 1.0f);
        }

        private void UpgradeBaseHp()
        {
            if (!_upgradesEnabled)
            {
                ShowBanner("Upgrades Locked", "This mission disables upgrades.", 1.0f);
                return;
            }

            var cost = GetBaseHpUpgradeCost();
            if (_gold < cost)
            {
                ShowBanner("Need More Gold", "Base upgrade costs $" + cost + ".", 1.0f);
                return;
            }

            _gold -= cost;
            _baseHpLevel += 1;
            _maxBaseHp += 30;
            _baseHp = Mathf.Min(_maxBaseHp, _baseHp + 30);
            CheckUpgradeAllAchievement();
            ShowBanner("Base Reinforced", "Base capacity increased to " + _maxBaseHp + ".", 1.0f);
        }

        private void CheckUpgradeAllAchievement()
        {
            var hasTowerLevel2 = false;
            foreach (var tower in _towers)
            {
                if (tower.Level >= 2)
                {
                    hasTowerLevel2 = true;
                    break;
                }
            }

            if (hasTowerLevel2 && _baseHpLevel >= 2)
            {
                UnlockAchievement("upgrade_all");
            }
        }

        private int GetUpgradeCost(TowerActor tower)
        {
            if (tower.Level >= 5)
            {
                return 0;
            }

            return Mathf.RoundToInt(tower.Definition.Cost * (0.76f + tower.Level * 0.42f));
        }

        private bool NeedsSpecializationChoice(TowerActor tower)
        {
            return _upgradesEnabled && tower.Level >= 3 && tower.Specialization == 0;
        }

        private static string GetSpecializationName(TowerActor tower, int branch)
        {
            switch (tower.Definition.Type)
            {
                case TowerType.Arrow:
                    return branch == 1 ? "Deadeye" : "Volley Core";
                case TowerType.Magic:
                    return branch == 1 ? "Focus Crystal" : "Rift Core";
                case TowerType.Cannon:
                    return branch == 1 ? "Siege Barrel" : "Shrapnel Rounds";
                case TowerType.Poison:
                    return branch == 1 ? "Corrosion" : "Miasma";
                case TowerType.Tesla:
                    return branch == 1 ? "Overcharge" : "Static Net";
                case TowerType.Ice:
                    return branch == 1 ? "Glacier" : "Frostbite";
                case TowerType.Flame:
                    return branch == 1 ? "Inferno" : "Emberstorm";
                default:
                    return branch == 1 ? "Branch A" : "Branch B";
            }
        }

        private static string GetSpecializationSummary(TowerActor tower, int branch)
        {
            switch (tower.Definition.Type)
            {
                case TowerType.Arrow:
                    return branch == 1 ? "Long-range precision volleys." : "Fast attack cycles and rapid skills.";
                case TowerType.Magic:
                    return branch == 1 ? "Heavy single-target arcane bursts." : "Frequent wide-area detonations.";
                case TowerType.Cannon:
                    return branch == 1 ? "Huge shell blasts and siege damage." : "Explosions ignite nearby enemies.";
                case TowerType.Poison:
                    return branch == 1 ? "Stronger poison damage over time." : "Poison clouds also slow targets.";
                case TowerType.Tesla:
                    return branch == 1 ? "Longer, stronger chain strikes." : "Electrical hits entangle movement.";
                case TowerType.Ice:
                    return branch == 1 ? "Powerful control and freeze uptime." : "Bonus damage to slowed enemies.";
                case TowerType.Flame:
                    return branch == 1 ? "Longer burns with higher DPS." : "Larger flame zones and splash.";
                default:
                    return "Unlocks a distinct branch bonus.";
            }
        }

        private float GetTowerAbilityCooldown(TowerActor tower)
        {
            var reduction = Mathf.Clamp01(_skillAbilityCooldownReduction);
            return tower.Definition.AbilityCooldown * tower.AbilityCooldownScale * (1f - reduction);
        }

        private float GetEffectiveTowerRange(TowerActor tower)
        {
            var range = tower.Range;
            if (_currentWaveModifier == TDWaveModifier.Invisible)
            {
                range *= 0.7f;
            }
            if (_isNight)
            {
                range *= 0.9f;
            }

            return range;
        }

        private float ApplyIceSlowBonus(float baseSlowMultiplier)
        {
            return Mathf.Clamp(baseSlowMultiplier - _skillIceSlowBonus, 0.05f, 0.95f);
        }

        private float GetTowerSynergyMultiplier(TowerActor tower)
        {
            var nearbySameType = 0;
            var origin = (Vector2)tower.Root.transform.position;
            var synergyRange = tower.Range * 1.2f;
            foreach (var other in _towers)
            {
                if (other == tower || other.Definition.Type != tower.Definition.Type)
                {
                    continue;
                }

                if (Vector2.Distance(origin, other.Root.transform.position) <= synergyRange)
                {
                    nearbySameType += 1;
                }
            }

            return 1f + Mathf.Min(nearbySameType, 3) * 0.10f;
        }

        private static float GetResistanceMultiplier(EnemyArchetype enemyType, TowerType towerType)
        {
            switch (enemyType)
            {
                case EnemyArchetype.Skeleton:
                case EnemyArchetype.FastSkeleton:
                case EnemyArchetype.MiniSkeleton:
                    switch (towerType)
                    {
                        case TowerType.Arrow:
                            return 0.5f;
                        case TowerType.Magic:
                            return 1.5f;
                        case TowerType.Cannon:
                        case TowerType.Flame:
                            return 1.3f;
                        default:
                            return 1f;
                    }
                case EnemyArchetype.Orc:
                case EnemyArchetype.MiniOrc:
                    switch (towerType)
                    {
                        case TowerType.Arrow:
                            return 0.7f;
                        case TowerType.Cannon:
                        case TowerType.Flame:
                            return 1.3f;
                        case TowerType.Ice:
                            return 1.2f;
                        default:
                            return 1f;
                    }
                case EnemyArchetype.Demon:
                case EnemyArchetype.MiniDemon:
                    switch (towerType)
                    {
                        case TowerType.Ice:
                            return 1.5f;
                        case TowerType.Poison:
                            return 0.5f;
                        case TowerType.Magic:
                            return 0.8f;
                        default:
                            return 1f;
                    }
                case EnemyArchetype.Dragon:
                case EnemyArchetype.MiniDragon:
                    switch (towerType)
                    {
                        case TowerType.Arrow:
                            return 0.6f;
                        case TowerType.Ice:
                            return 1.4f;
                        case TowerType.Magic:
                            return 1.2f;
                        default:
                            return 1f;
                    }
                case EnemyArchetype.Shadow:
                    switch (towerType)
                    {
                        case TowerType.Arrow:
                            return 0.3f;
                        case TowerType.Magic:
                            return 1.5f;
                        case TowerType.Tesla:
                            return 1.3f;
                        default:
                            return 1f;
                    }
                case EnemyArchetype.GolemShard:
                    switch (towerType)
                    {
                        case TowerType.Arrow:
                            return 0.5f;
                        case TowerType.Cannon:
                        case TowerType.Flame:
                            return 1.5f;
                        case TowerType.Magic:
                            return 1.3f;
                        default:
                            return 1f;
                    }
                case EnemyArchetype.ArmoredGolem:
                    switch (towerType)
                    {
                        case TowerType.Arrow:
                            return 0.3f;
                        case TowerType.Cannon:
                        case TowerType.Flame:
                            return 1.5f;
                        case TowerType.Magic:
                            return 1.4f;
                        case TowerType.Tesla:
                            return 1.2f;
                        default:
                            return 1f;
                    }
                case EnemyArchetype.Wisp:
                    switch (towerType)
                    {
                        case TowerType.Arrow:
                            return 0.4f;
                        case TowerType.Ice:
                            return 1.4f;
                        case TowerType.Tesla:
                            return 0.5f;
                        default:
                            return 1f;
                    }
                default:
                    return 1f;
            }
        }

        private void DamageEnemyWithTower(EnemyActor enemy, TowerActor tower, float baseDamage, float synergyMultiplier, bool allowCrit = true)
        {
            if (enemy.Removed || enemy.Root == null || baseDamage <= 0f)
            {
                return;
            }

            var resistance = GetResistanceMultiplier(enemy.Type, tower.Definition.Type);
            if (resistance < 1f)
            {
                resistance += (1f - resistance) * _skillResistancePierce;
            }

            var crit = allowCrit && RollChance(TowerCritChance);
            var damage = baseDamage
                * _skillTowerDamageMult
                * GetPrestigeDamageMultiplier()
                * synergyMultiplier
                * resistance
                * (crit ? TowerCritMultiplier : 1f);
            var hitPosition = (Vector2)enemy.Root.transform.position;
            DamageEnemy(enemy, damage);
            if (crit)
            {
                TDVisuals.SpawnBurstParticles(_worldRoot, "CritBurst", hitPosition, new Color(1f, 0.18f, 0.22f), 10, 2.2f, 0.45f, 0.11f, 19);
            }
        }

        private void SetSpecializationButtonsVisible(bool visible)
        {
            if (_specializeAlphaButton != null)
            {
                _specializeAlphaButton.gameObject.SetActive(visible);
                _specializeAlphaButton.interactable = visible;
            }

            if (_specializeBetaButton != null)
            {
                _specializeBetaButton.gameObject.SetActive(visible);
                _specializeBetaButton.interactable = visible;
            }
        }

        private void StartNextWave()
        {
            _wave += 1;
            _baseDamagedThisWave = false;
            _isNight = ((_wave / 8) % 2) == 1;
            _currentWaveModifier = IsBossWave(_wave) ? TDWaveModifier.None : PickWaveModifier(_wave);
            _spawnQueue = BuildWave(_wave);
            _spawnTimer = 0.4f;
            var map = TDData.Maps[_activeMap];
            _gameCamera.backgroundColor = _isNight
                ? Color.Lerp(map.BackgroundBottom, new Color(0.02f, 0.02f, 0.06f), 0.55f)
                : map.BackgroundBottom;

            var body = _mode == GameMode.Campaign && _campaignLevel != null && _wave == _campaignLevel.TargetWave
                ? "Final wave for this sector. Hold the line."
                : "Enemy movement detected. Spend fast, then stabilize.";
            if (_isNight)
            {
                body += " Nightfall: enemies gain HP and tower range drops.";
            }
            if (_currentWaveModifier != TDWaveModifier.None)
            {
                body += " Modifier: " + GetModifierLabel(_currentWaveModifier) + ".";
            }

            if (_wave >= 5)
            {
                UnlockAchievement("wave_5");
            }
            if (_wave >= 10)
            {
                UnlockAchievement("wave_10");
            }
            if (_wave >= 20)
            {
                UnlockAchievement("wave_20");
            }
            if (_wave >= 30)
            {
                UnlockAchievement("wave_30");
            }
            if (_wave >= 50)
            {
                UnlockAchievement("wave_50");
            }
            if (_wave >= 100)
            {
                UnlockAchievement("wave_100");
            }
            if (_mode == GameMode.Endless && _wave >= 10)
            {
                UnlockAchievement("endless_10");
            }

            ShowBanner("Wave " + _wave + (_currentWaveModifier != TDWaveModifier.None ? " - " + GetModifierLabel(_currentWaveModifier) : ""), body, 1.7f);
        }

        private Queue<SpawnRequest> BuildWave(int wave)
        {
            var queue = new Queue<SpawnRequest>();
            var laneCount = Mathf.Max(1, _paths.Count);
            var healthScale = _campaignLevel?.EnemyHealthMultiplier ?? 1f;
            var speedScale = _campaignLevel?.EnemySpeedMultiplier ?? 1f;
            var rewardScale = _campaignLevel?.RewardMultiplier ?? 1f;
            var spawnScale = _campaignLevel?.SpawnRateMultiplier ?? 1f;

            if (_mode == GameMode.Endless && wave > 10)
            {
                var tier = Mathf.Clamp((wave - 10f) / 10f, 0f, 5f);
                healthScale *= 1f + tier * 0.15f;
                speedScale *= 1f + tier * 0.05f;
                rewardScale *= 1f + tier * 0.08f;
                spawnScale *= 1f + tier * 0.08f;
            }
            else if (_mode == GameMode.BossRush)
            {
                healthScale *= 1f + wave * 0.08f;
                speedScale *= 1f + wave * 0.03f;
                rewardScale *= 1f + wave * 0.06f;
            }
            else
            {
                healthScale *= 1f + wave * 0.06f;
                speedScale *= 1f + wave * 0.015f;
                rewardScale *= 1f + wave * 0.03f;
            }

            if (_isNight)
            {
                healthScale *= 1.2f;
            }

            var regenRate = _currentWaveModifier == TDWaveModifier.Regen ? 3f + wave * 0.5f : 0f;
            _currentWaveBoss = null;

            if (IsBossWave(wave))
            {
                var bossType = DrawNextBoss();
                var bossDef = TDLegacyData.Bosses[bossType];
                _currentWaveBoss = bossType;

                queue.Enqueue(new SpawnRequest
                {
                    Type = EnemyArchetype.Boss,
                    PathIndex = wave % laneCount,
                    HealthMultiplier = healthScale,
                    SpeedMultiplier = speedScale,
                    RewardMultiplier = rewardScale,
                    Modifier = _currentWaveModifier,
                    BossType = bossType,
                    RegenRate = regenRate
                });

                var minionScale = _mode == GameMode.BossRush ? 0.6f : 1f;
                var minionCount = Mathf.Max(_mode == GameMode.BossRush ? 1 : 2, Mathf.RoundToInt(bossDef.MinionCount * spawnScale * minionScale));
                for (var i = 0; i < minionCount; i++)
                {
                    queue.Enqueue(new SpawnRequest
                    {
                        Type = bossDef.MinionType,
                        PathIndex = i % laneCount,
                        HealthMultiplier = healthScale,
                        SpeedMultiplier = speedScale,
                        RewardMultiplier = rewardScale,
                        Modifier = _currentWaveModifier,
                        IsBossMinion = true,
                        RegenRate = regenRate
                    });
                }

                _totalEnemiesThisWave = 1 + minionCount;
                _enemiesSpawnedThisWave = 0;
                return queue;
            }

            var count = Mathf.Clamp(Mathf.RoundToInt((3 + wave * 2) * spawnScale), 4, 140);
            if (_currentWaveModifier == TDWaveModifier.Swarm)
            {
                count *= 2;
            }

            var eliteSpawned = false;
            for (var i = 0; i < count; i++)
            {
                var isElite = wave >= 5 && wave % 5 == 0 && !eliteSpawned;
                if (isElite)
                {
                    eliteSpawned = true;
                }

                queue.Enqueue(new SpawnRequest
                {
                    Type = PickRegularEnemyType(wave),
                    PathIndex = i % laneCount,
                    HealthMultiplier = healthScale,
                    SpeedMultiplier = speedScale,
                    RewardMultiplier = rewardScale,
                    Modifier = _currentWaveModifier,
                    IsElite = isElite,
                    RegenRate = regenRate
                });
            }

            _totalEnemiesThisWave = count;
            _enemiesSpawnedThisWave = 0;
            return queue;
        }

        private bool IsBossWave(int wave)
        {
            if (_mode == GameMode.BossRush)
            {
                return wave % 3 == 0;
            }

            if (_campaignLevel != null && _campaignLevel.Id == 13)
            {
                return wave % 3 == 0;
            }

            return wave % 5 == 0;
        }

        private TDBossType DrawNextBoss()
        {
            if (_bossPool.Count == 0)
            {
                var values = (TDBossType[])Enum.GetValues(typeof(TDBossType));
                _bossPool.AddRange(values);
                for (var i = _bossPool.Count - 1; i > 0; i--)
                {
                    var swapIndex = _runRng.Next(0, i + 1);
                    var cached = _bossPool[i];
                    _bossPool[i] = _bossPool[swapIndex];
                    _bossPool[swapIndex] = cached;
                }
            }

            var next = _bossPool[0];
            _bossPool.RemoveAt(0);
            return next;
        }

        private TDWaveModifier PickWaveModifier(int wave)
        {
            if (_mode == GameMode.DailyChallenge && _dailyModifierRotation.Count > 0)
            {
                return _dailyModifierRotation[wave % _dailyModifierRotation.Count];
            }

            if (wave < 3)
            {
                return TDWaveModifier.None;
            }

            var modifierChance = Mathf.Clamp01(0.40f + _skillLuckyWaveBonus);
            if (!RollChance(modifierChance))
            {
                return TDWaveModifier.None;
            }

            var modifiers = new List<TDWaveModifier>
            {
                TDWaveModifier.Fast,
                TDWaveModifier.Armored,
                TDWaveModifier.Regen,
                TDWaveModifier.Swarm,
                TDWaveModifier.Invisible,
                TDWaveModifier.Rich,
                TDWaveModifier.BossRally
            };

            return modifiers[_runRng.Next(modifiers.Count)];
        }

        private EnemyArchetype PickRegularEnemyType(int wave)
        {
            var pool = new List<EnemyArchetype>
            {
                EnemyArchetype.Goblin,
                EnemyArchetype.Goblin,
                EnemyArchetype.Skeleton
            };

            if (wave >= 4)
            {
                pool.Add(EnemyArchetype.Orc);
                pool.Add(EnemyArchetype.Skeleton);
            }
            if (wave >= 6)
            {
                pool.Add(EnemyArchetype.FastSkeleton);
                pool.Add(EnemyArchetype.Slime);
            }
            if (wave >= 8)
            {
                pool.Add(EnemyArchetype.Demon);
                pool.Add(EnemyArchetype.Shadow);
            }
            if (wave >= 10)
            {
                pool.Add(EnemyArchetype.Dragon);
                pool.Add(EnemyArchetype.Bat);
                pool.Add(EnemyArchetype.Spider);
            }
            if (wave >= 12)
            {
                pool.Add(EnemyArchetype.ArmoredGolem);
                pool.Add(EnemyArchetype.GolemShard);
                pool.Add(EnemyArchetype.Wisp);
            }

            var bossThemedWave = _mode == GameMode.BossRush || _currentWaveModifier == TDWaveModifier.BossRally || wave % 9 == 0;
            if (bossThemedWave && wave >= 8)
            {
                pool.Add(EnemyArchetype.MiniOrc);
                pool.Add(EnemyArchetype.MiniSkeleton);
            }
            if (bossThemedWave && wave >= 12)
            {
                pool.Add(EnemyArchetype.MiniDemon);
                pool.Add(EnemyArchetype.MiniDragon);
            }

            return pool[_runRng.Next(pool.Count)];
        }

        private static string GetModifierLabel(TDWaveModifier modifier)
        {
            switch (modifier)
            {
                case TDWaveModifier.Fast:
                    return "Fast";
                case TDWaveModifier.Armored:
                    return "Armored";
                case TDWaveModifier.Regen:
                    return "Regen";
                case TDWaveModifier.Swarm:
                    return "Swarm";
                case TDWaveModifier.Invisible:
                    return "Invisible";
                case TDWaveModifier.Rich:
                    return "Rich";
                case TDWaveModifier.BossRally:
                    return "Boss Rally";
                default:
                    return "Normal";
            }
        }

        private void SpawnEnemy(SpawnRequest request)
        {
            var path = _paths[Mathf.Clamp(request.PathIndex, 0, _paths.Count - 1)];
            var laneIndex = Mathf.Clamp(request.PathIndex, 0, _paths.Count - 1);
            var isBoss = request.BossType.HasValue;
            var goldScale = _campaignLevel?.GoldMultiplier ?? 1f;
            var damageScale = _campaignLevel?.EnemyDamageMultiplier ?? 1f;

            EnemyDefinition definition;
            var bodyColor = new Color(0.6f, 0.4f, 0.4f);
            var accentColor = new Color(0.9f, 0.7f, 0.3f);
            var baseHealth = 40f;
            var baseSpeed = 1f;
            var baseDamage = 8f;
            var baseReward = 6f;

            if (isBoss)
            {
                var bossDef = TDLegacyData.Bosses[request.BossType.Value];
                definition = TDData.Enemies[EnemyArchetype.Boss];
                bodyColor = bossDef.Color;
                accentColor = Color.Lerp(bossDef.Color, Color.white, 0.35f);
                var waveScale = 1f + (_wave - 1) * 0.15f;
                baseHealth = (bossDef.BaseHp + _wave * 40f) * waveScale;
                baseSpeed = bossDef.BaseSpeed;
                baseDamage = bossDef.BaseDamage * waveScale * damageScale;
                baseReward = (bossDef.BaseGold + _wave * 10f) * waveScale;
            }
            else
            {
                definition = TDData.Enemies[request.Type];
                bodyColor = definition.Body;
                accentColor = definition.Accent;
                baseHealth = definition.Health;
                baseSpeed = definition.Speed;
                baseDamage = definition.Damage * damageScale;
                baseReward = definition.Reward;
            }

            var hpMod = 1f;
            var speedMod = 1f;
            var rewardMod = 1f;
            var regen = request.RegenRate;
            switch (request.Modifier)
            {
                case TDWaveModifier.Fast:
                    speedMod = 2f;
                    break;
                case TDWaveModifier.Armored:
                    hpMod = 1.5f;
                    break;
                case TDWaveModifier.Regen:
                    regen = Mathf.Max(regen, 3f + _wave * 0.5f);
                    break;
                case TDWaveModifier.Swarm:
                    hpMod = 0.5f;
                    break;
                case TDWaveModifier.Rich:
                    rewardMod = 2f;
                    break;
                case TDWaveModifier.BossRally:
                    speedMod = 1.4f;
                    break;
            }

            if (request.IsElite)
            {
                hpMod *= 3f;
                rewardMod *= 2f;
                baseDamage *= 1.5f;
                accentColor = Color.Lerp(accentColor, new Color(1f, 0.86f, 0.34f), 0.45f);
            }

            var health = baseHealth * request.HealthMultiplier * hpMod;
            var speed = baseSpeed * request.SpeedMultiplier * speedMod;
            var reward = Mathf.Max(1, Mathf.RoundToInt(baseReward * request.RewardMultiplier * goldScale * rewardMod));

            var root = new GameObject(definition.Name);
            root.transform.SetParent(_worldRoot, false);
            root.transform.position = path[0];

            var scale = isBoss
                ? 1.50f
                : request.IsElite
                    ? 1.26f
                    : request.Type == EnemyArchetype.Brute || request.Type == EnemyArchetype.ArmoredGolem
                        ? 1.20f
                        : 1.12f;
            var enemySpriteScale = TDArtLibrary.GetEnemySpriteScale(request.Type, isBoss);
            var enemySpriteYOffset = TDArtLibrary.GetEnemySpriteYOffset(request.Type, isBoss);
            var shadowGo = TDVisuals.CreateWorldDisc("Shadow", root.transform, new Vector2(0f, -0.05f), new Vector2(0.82f, 0.24f) * scale, new Color(0f, 0f, 0f, 0.30f), 10);
            shadowGo.transform.localPosition = new Vector3(0f, -0.10f, 0f);

            var outlineGo = TDVisuals.CreateWorldDisc("Outline", root.transform, Vector2.zero, new Vector2(0.94f, 1.00f) * scale, new Color(0f, 0f, 0f, 0.20f), 10);
            outlineGo.transform.localPosition = new Vector3(0f, -0.02f, 0f);

            var enemySprite = TDArtLibrary.GetEnemySprite(request.Type, isBoss);
            var bodyGo = TDVisuals.CreateWorldSprite("Body", root.transform, new Vector2(0f, enemySpriteYOffset), new Vector2(enemySpriteScale, enemySpriteScale) * scale, Color.white, 11);
            bodyGo.transform.localPosition = new Vector3(0f, enemySpriteYOffset, 0f);
            var bodyRenderer = bodyGo.GetComponent<SpriteRenderer>();
            if (bodyRenderer != null)
            {
                if (enemySprite != null)
                {
                    bodyRenderer.sprite = enemySprite;
                    bodyRenderer.color = request.IsElite
                        ? Color.Lerp(Color.white, accentColor, 0.22f)
                        : Color.white;
                }
                else
                {
                    bodyRenderer.sprite = TDVisuals.CircleSprite;
                    bodyRenderer.color = bodyColor;
                }
            }

            var shellGo = TDVisuals.CreateWorldDisc("Shell", root.transform, new Vector2(0f, 0.00f), new Vector2(0.66f, 0.30f) * scale, new Color(0f, 0f, 0f, 0.15f), 10);
            shellGo.transform.localPosition = new Vector3(0f, 0.00f, 0f);

            var coreGo = TDVisuals.CreateWorldDisc("Core", root.transform, new Vector2(0f, 0.12f), new Vector2(0.24f, 0.24f) * scale, new Color(accentColor.r, accentColor.g, accentColor.b, 0.76f), 12);
            coreGo.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            var eyeGo = TDVisuals.CreateWorldDisc("Eye", root.transform, new Vector2(0f, 0.12f), new Vector2(0.08f, 0.08f) * scale, new Color(1f, 1f, 1f, 0.50f), 13);
            eyeGo.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            if (request.IsElite)
            {
                var crown = TDVisuals.CreateWorldSprite("EliteCrown", root.transform, new Vector2(0f, 0.40f), new Vector2(0.26f, 0.14f) * scale, new Color(1f, 0.88f, 0.34f), 15);
                crown.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-8f, 8f));
            }

            if (isBoss)
            {
                TDVisuals.CreateWorldDisc("BossAura", root.transform, Vector2.zero, new Vector2(1.12f, 1.12f) * scale, new Color(accentColor.r, accentColor.g, accentColor.b, 0.22f), 9);
            }

            var barRoot = new GameObject("HealthBar");
            barRoot.transform.SetParent(root.transform, false);
            var spriteHeight = bodyRenderer != null && bodyRenderer.sprite != null
                ? bodyRenderer.sprite.bounds.size.y * bodyGo.transform.localScale.y
                : 0.82f * scale;
            var barYOffset = spriteHeight * 0.58f + (isBoss ? 0.28f : 0.22f);
            var barWidth = isBoss ? 0.74f : 0.56f;
            barRoot.transform.localPosition = new Vector3(0f, barYOffset, 0f);
            var barBack = TDVisuals.CreateWorldSprite("BarBack", barRoot.transform, Vector2.zero, new Vector2(barWidth, 0.08f), new Color(0f, 0f, 0f, 0.45f), 13);
            barBack.transform.localPosition = Vector3.zero;
            var barFill = TDVisuals.CreateWorldSprite("BarFill", barRoot.transform, Vector2.zero, new Vector2(barWidth, 0.08f), new Color(0.34f, 0.96f, 0.56f), 14);
            barFill.transform.localPosition = Vector3.zero;

            TDVisuals.CreateLoopParticles(
                "EnemyTrail",
                root.transform,
                new Vector2(0f, -0.04f),
                new Color(accentColor.r, accentColor.g, accentColor.b, 0.32f),
                new Color(bodyColor.r, bodyColor.g, bodyColor.b, 0.10f),
                0.11f * scale,
                isBoss ? 10f : 6f,
                0.20f,
                1.2f,
                0.04f,
                0.09f,
                10);

            TDVisuals.SpawnBurstParticles(_worldRoot, "EnemySpawnBurst", path[0], accentColor, isBoss ? 22 : 12, 1.9f, 0.55f, 0.13f * scale, 15);

            var enemy = new EnemyActor
            {
                Root = root,
                Body = bodyRenderer!,
                Core = coreGo.GetComponent<SpriteRenderer>()!,
                Shell = shellGo.transform,
                Eye = eyeGo.transform,
                BarRoot = barRoot.transform,
                BarFill = barFill.transform,
                BarWidth = barWidth,
                Type = request.Type,
                Health = health,
                MaxHealth = health,
                Speed = speed,
                Damage = baseDamage * Mathf.Lerp(0.8f, 1f, Mathf.Clamp01(_wave / 20f)),
                Reward = reward,
                Path = new List<Vector2>(path),
                LaneIndex = laneIndex,
                PathIndex = 0,
                Progress = 0f,
                Seed = UnityEngine.Random.value * 12f,
                FootstepTimer = UnityEngine.Random.Range(0.04f, 0.20f),
                LeftStep = UnityEngine.Random.value > 0.5f,
                RegenRate = regen,
                IsElite = request.IsElite,
                BossType = request.BossType,
                BossAbility = request.BossType.HasValue ? TDLegacyData.Bosses[request.BossType.Value].Ability : TDBossAbility.Charge,
                BossAbilityCooldown = request.BossType.HasValue ? 5f : 0f,
                BossAbilityTimer = request.BossType.HasValue ? 5f + UnityEngine.Random.Range(0f, 1.0f) : 0f
            };

            _enemies.Add(enemy);
            _enemiesSpawnedThisWave += 1;
        }

        private void ExecuteBossAbility(EnemyActor boss)
        {
            if (boss.Removed || boss.Root == null || !boss.BossType.HasValue)
            {
                return;
            }

            var bossDef = TDLegacyData.Bosses[boss.BossType.Value];
            switch (boss.BossAbility)
            {
                case TDBossAbility.Charge:
                    boss.IsCharging = true;
                    boss.ChargeTimer = 2f;
                    TDVisuals.SpawnBurstParticles(_worldRoot, "BossCharge", boss.Root.transform.position, bossDef.Color, 18, 2f, 0.55f, 0.16f, 18);
                    break;
                case TDBossAbility.Summon:
                    for (var i = 0; i < 3; i++)
                    {
                        SpawnBossAbilityMinion(boss, bossDef.MinionType, 1f);
                    }

                    _totalEnemiesThisWave += 3;
                    TDVisuals.SpawnBurstParticles(_worldRoot, "BossSummon", boss.Root.transform.position, bossDef.Color, 20, 2.2f, 0.65f, 0.14f, 18);
                    break;
                case TDBossAbility.Heal:
                    boss.Health = Mathf.Min(boss.MaxHealth, boss.Health + boss.MaxHealth * 0.15f);
                    TDVisuals.SpawnBurstParticles(_worldRoot, "BossHeal", boss.Root.transform.position, new Color(0.40f, 0.88f, 0.52f), 14, 1.8f, 0.6f, 0.12f, 18);
                    break;
                case TDBossAbility.AoeDamage:
                    foreach (var tower in _towers)
                    {
                        if (tower.Root != null && Vector2.Distance(tower.Root.transform.position, boss.Root.transform.position) <= 2.2f)
                        {
                            tower.Cooldown += 2f;
                        }
                    }

                    if (Vector2.Distance(_basePosition, boss.Root.transform.position) <= 2.2f)
                    {
                        _baseHp = Mathf.Max(0, _baseHp - 15);
                    }
                    TDVisuals.SpawnBurstParticles(_worldRoot, "BossAoe", boss.Root.transform.position, new Color(1f, 0.42f, 0.24f), 22, 2.2f, 0.72f, 0.14f, 18);
                    break;
                case TDBossAbility.Shield:
                    boss.ShieldTimer = 5f;
                    TDVisuals.SpawnBurstParticles(_worldRoot, "BossShield", boss.Root.transform.position, new Color(0.32f, 0.78f, 1f), 16, 1.4f, 0.66f, 0.11f, 18);
                    break;
                case TDBossAbility.Roar:
                    foreach (var enemy in _enemies)
                    {
                        if (enemy == boss || enemy.Removed || enemy.Root == null)
                        {
                            continue;
                        }

                        if (Vector2.Distance(enemy.Root.transform.position, boss.Root.transform.position) <= 2.8f)
                        {
                            enemy.RoarSpeedBoost = 2f;
                            enemy.RoarBoostTimer = 3f;
                        }
                    }
                    TDVisuals.SpawnBurstParticles(_worldRoot, "BossRoar", boss.Root.transform.position, bossDef.Color, 18, 2.4f, 0.6f, 0.12f, 18);
                    break;
                case TDBossAbility.Teleport:
                    var nextSegment = Mathf.Min(boss.PathIndex + 2, boss.Path.Count - 2);
                    if (nextSegment > boss.PathIndex)
                    {
                        boss.PathIndex = nextSegment;
                        boss.Progress = 0f;
                        boss.Root.transform.position = boss.Path[nextSegment];
                        TDVisuals.SpawnBurstParticles(_worldRoot, "BossTeleport", boss.Root.transform.position, bossDef.Color, 18, 2.4f, 0.5f, 0.12f, 18);
                    }
                    break;
                case TDBossAbility.Drain:
                    var stolen = Mathf.Min(10 + _wave, _gold);
                    if (stolen > 0)
                    {
                        _gold -= stolen;
                        boss.Health = Mathf.Min(boss.MaxHealth * 1.2f, boss.Health + stolen * 2f);
                    }
                    TDVisuals.SpawnBurstParticles(_worldRoot, "BossDrain", boss.Root.transform.position, new Color(0.92f, 0.30f, 0.86f), 16, 1.6f, 0.62f, 0.12f, 18);
                    break;
                case TDBossAbility.Quake:
                    foreach (var tower in _towers)
                    {
                        tower.Cooldown += 1.5f;
                    }
                    TDVisuals.SpawnBurstParticles(_worldRoot, "BossQuake", boss.Root.transform.position, new Color(0.64f, 0.44f, 0.28f), 24, 2.8f, 0.8f, 0.14f, 18);
                    break;
                case TDBossAbility.Split:
                    if (!boss.HasSplit && boss.Health < boss.MaxHealth * 0.5f)
                    {
                        boss.HasSplit = true;
                        var cloneHp = boss.Health * 0.3f;
                        boss.Health *= 0.4f;
                        for (var i = 0; i < 2; i++)
                        {
                            SpawnBossAbilityMinion(boss, bossDef.MinionType, 1f);
                            var clone = _enemies[_enemies.Count - 1];
                            clone.Health = cloneHp;
                            clone.MaxHealth = cloneHp;
                            clone.Damage = boss.Damage * 0.5f;
                            clone.Reward = Mathf.Max(1, boss.Reward / 4);
                            clone.Root.transform.position = boss.Root.transform.position + new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(-0.25f, 0.25f), 0f);
                            clone.PathIndex = boss.PathIndex;
                            clone.Progress = boss.Progress;
                        }

                        _totalEnemiesThisWave += 2;
                        TDVisuals.SpawnBurstParticles(_worldRoot, "BossSplit", boss.Root.transform.position, bossDef.Color, 20, 2.2f, 0.62f, 0.14f, 18);
                    }
                    break;
            }
        }

        private void SpawnBossAbilityMinion(EnemyActor boss, EnemyArchetype type, float waveScale)
        {
            var lane = Mathf.Clamp(boss.LaneIndex, 0, Mathf.Max(0, _paths.Count - 1));
            SpawnEnemy(new SpawnRequest
            {
                Type = type,
                PathIndex = lane,
                HealthMultiplier = (1f + (_wave - 1) * 0.15f) * waveScale,
                SpeedMultiplier = 1f,
                RewardMultiplier = 1f,
                Modifier = _currentWaveModifier,
                IsBossMinion = true,
                RegenRate = _currentWaveModifier == TDWaveModifier.Regen ? 3f + _wave * 0.5f : 0f
            });
        }

        private void ShowPauseOverlay()
        {
            if (_state != RuntimeState.Playing)
            {
                return;
            }

            _overlayCanResume = true;
            _state = RuntimeState.Overlay;
            if (_overlayRoot != null)
            {
                Destroy(_overlayRoot);
            }

            _overlayRoot = new GameObject("OverlayRoot", typeof(RectTransform));
            _overlayRoot.transform.SetParent(_canvas.transform, false);
            var rect = _overlayRoot.GetComponent<RectTransform>();
            SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TDVisuals.CreateUiBlock(_overlayRoot.transform, "Scrim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.68f));
            var card = TDVisuals.CreateUiBlock(_overlayRoot.transform, "Card", new Vector2(0.18f, 0.32f), new Vector2(0.82f, 0.68f), Vector2.zero, Vector2.zero, new Color(0.05f, 0.09f, 0.15f, 0.96f));

            var title = TDVisuals.CreateText(card.transform, "Title", "Tactical Pause", 42, TextAnchor.UpperCenter, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.90f), Vector2.zero, Vector2.zero);

            var body = TDVisuals.CreateText(card.transform, "Body", "Reposition your plan, then jump back in.", 24, TextAnchor.UpperCenter, new Color(0.78f, 0.86f, 0.94f));
            SetRect(body.rectTransform, new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.70f), Vector2.zero, Vector2.zero);

            var resume = TDVisuals.CreateButton(card.transform, "Resume", "Resume", new Color(0.94f, 0.74f, 0.24f), new Color(0.14f, 0.10f, 0.06f));
            SetRect(resume.GetComponent<RectTransform>(), new Vector2(0.12f, 0.24f), new Vector2(0.88f, 0.38f), Vector2.zero, Vector2.zero);
            resume.onClick.AddListener(CloseOverlayToGame);

            var retry = TDVisuals.CreateButton(card.transform, "Retry", "Restart Run", new Color(0.18f, 0.24f, 0.34f), Color.white);
            SetRect(retry.GetComponent<RectTransform>(), new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.24f), Vector2.zero, Vector2.zero);
            retry.onClick.AddListener(() => StartRun(_activeMap, _mode, _campaignLevel));

            var saveExit = TDVisuals.CreateButton(card.transform, "SaveExit", "Save & Exit", new Color(0.10f, 0.30f, 0.34f), Color.white);
            SetRect(saveExit.GetComponent<RectTransform>(), new Vector2(0.12f, 0.04f), new Vector2(0.88f, 0.14f), Vector2.zero, Vector2.zero);
            saveExit.onClick.AddListener(() =>
            {
                if (SaveRunSnapshot(false, true))
                {
                    BuildMenu();
                }
            });

            var menu = TDVisuals.CreateButton(card.transform, "Menu", "Main Menu", new Color(0.24f, 0.14f, 0.14f), Color.white);
            SetRect(menu.GetComponent<RectTransform>(), new Vector2(0.12f, 0.26f), new Vector2(0.88f, 0.34f), Vector2.zero, Vector2.zero);
            menu.onClick.AddListener(BuildMenu);
        }

        private void CloseOverlayToGame()
        {
            if (!_overlayCanResume)
            {
                return;
            }

            if (_overlayRoot != null)
            {
                Destroy(_overlayRoot);
                _overlayRoot = null;
            }

            _overlayCanResume = false;
            _state = RuntimeState.Playing;
        }

        private void OnVictory()
        {
            if (_state == RuntimeState.Overlay)
            {
                return;
            }

            if (_campaignLevel != null)
            {
                PlayerPrefs.SetInt("td_campaign_level_" + _campaignLevel.Id, 1);
                var unlocked = Mathf.Max(PlayerPrefs.GetInt("td_campaign_unlocked", 1), _campaignLevel.Id + 1);
                PlayerPrefs.SetInt("td_campaign_unlocked", Mathf.Min(unlocked, TDData.Campaign.Count));

                _diamondsThisRun += _campaignLevel.DiamondReward;
                var completed = CountCompletedCampaignLevels();
                if (completed >= 5)
                {
                    UnlockAchievement("campaign_5");
                }
                if (completed >= 10)
                {
                    UnlockAchievement("campaign_10");
                }
                if (completed >= TDData.Campaign.Count)
                {
                    UnlockAchievement("campaign_all");
                }
                if (!_baseDamagedThisRun)
                {
                    PlayerPrefs.SetInt("td_campaign_level_fullhp_" + _campaignLevel.Id, 1);
                    UnlockAchievement("campaign_no_damage");
                    if (CountFullHpCampaignLevels() >= 5)
                    {
                        UnlockAchievement("campaign_3star");
                    }
                }
            }

            PersistRunStats();
            ClearSavedRun();

            var body = _campaignLevel != null
                ? "Wave " + _wave + " cleared. The next sector is now unlocked."
                : "New benchmark recorded for " + TDData.Maps[_activeMap].Name + ".";
            BuildOverlay(true, "Sector Secure", body);
        }

        private void OnGameOver()
        {
            if (_state == RuntimeState.Overlay)
            {
                return;
            }

            PersistRunStats();
            ClearSavedRun();
            BuildOverlay(false, "Base Lost", "The line collapsed at wave " + _wave + ". Rebuild with a tighter tower mix.");
        }

        private void OnWaveCleared()
        {
            if (_wave <= 0)
            {
                _intermissionTimer = 2.8f;
                return;
            }

            if (!_baseDamagedThisWave)
            {
                UnlockAchievement("no_damage");
            }
            if (_baseHp == 1)
            {
                UnlockAchievement("survivor_1hp");
            }
            if (_gameSpeed >= 3f && _wave >= 10)
            {
                UnlockAchievement("speed_demon");
            }

            var interest = Mathf.FloorToInt(_gold * 0.05f);
            if (interest > 0)
            {
                GainGold(interest);
                _score += interest;
            }

            var waveBonus = Mathf.Max(6, Mathf.RoundToInt((_wave * 5 + 3 + _skillWaveGoldBonus) * (_campaignLevel?.GoldMultiplier ?? 1f)));
            GainGold(waveBonus);
            _score += waveBonus;

            if (_mode == GameMode.Campaign && _campaignLevel != null && _wave >= _campaignLevel.TargetWave)
            {
                OnVictory();
                return;
            }

            _intermissionTimer = _mode == GameMode.BossRush ? 2f : 2.8f;
            ShowBanner("Sector Clear", "+$" + (interest + waveBonus) + " secured. Reposition before wave " + (_wave + 1) + ".", 1.6f);
            SaveRunSnapshot(true);
        }

        private void PersistRunStats()
        {
            PlayerPrefs.SetInt("td_best_score", Mathf.Max(PlayerPrefs.GetInt("td_best_score", 0), _score));
            PlayerPrefs.SetInt("td_best_wave", Mathf.Max(PlayerPrefs.GetInt("td_best_wave", 0), _wave));
            PlayerPrefs.SetInt("td_lifetime_kills", PlayerPrefs.GetInt("td_lifetime_kills", 0) + _killsThisRun);
            PlayerPrefs.SetInt("td_lifetime_runs", PlayerPrefs.GetInt("td_lifetime_runs", 0) + 1);
            PlayerPrefs.SetInt("td_lifetime_gold", PlayerPrefs.GetInt("td_lifetime_gold", 0) + _goldEarnedThisRun);
            PlayerPrefs.SetInt("td_lifetime_bosses", PlayerPrefs.GetInt("td_lifetime_bosses", 0) + _bossesKilledThisRun);
            PlayerPrefs.SetInt("td_lifetime_best_combo", Mathf.Max(PlayerPrefs.GetInt("td_lifetime_best_combo", 0), _bestCombo));
            if (_diamondsThisRun > 0)
            {
                PlayerPrefs.SetInt("td_diamonds", PlayerPrefs.GetInt("td_diamonds", 0) + _diamondsThisRun);
            }

            PlayerPrefs.Save();
        }

        private int CountCompletedCampaignLevels()
        {
            var completed = 0;
            foreach (var level in TDData.Campaign)
            {
                if (PlayerPrefs.GetInt("td_campaign_level_" + level.Id, 0) == 1)
                {
                    completed += 1;
                }
            }

            return completed;
        }

        private int CountFullHpCampaignLevels()
        {
            var completed = 0;
            foreach (var level in TDData.Campaign)
            {
                if (PlayerPrefs.GetInt("td_campaign_level_fullhp_" + level.Id, 0) == 1)
                {
                    completed += 1;
                }
            }

            return completed;
        }

        private int GetDiamonds()
        {
            return PlayerPrefs.GetInt("td_diamonds", 0);
        }

        private void SetDiamonds(int value)
        {
            PlayerPrefs.SetInt("td_diamonds", Mathf.Max(0, value));
            PlayerPrefs.Save();
        }

        private int GetSkillLevel(string id)
        {
            return PlayerPrefs.GetInt("td_skill_" + id, 0);
        }

        private void SetSkillLevel(string id, int level)
        {
            PlayerPrefs.SetInt("td_skill_" + id, Mathf.Max(0, level));
        }

        private SkillNode? GetSkillNode(string id)
        {
            foreach (var node in SkillNodes)
            {
                if (node.Id == id)
                {
                    return node;
                }
            }

            return null;
        }

        private int GetSkillUpgradeCost(SkillNode node)
        {
            var level = GetSkillLevel(node.Id);
            return node.BaseCost + level * node.CostPerLevel;
        }

        private bool TryUpgradeSkill(string id)
        {
            var node = GetSkillNode(id);
            if (node == null)
            {
                return false;
            }

            if (node.PrestigePage && PlayerPrefs.GetInt("td_prestige_level", 0) <= 0)
            {
                return false;
            }

            var level = GetSkillLevel(node.Id);
            if (level >= node.MaxLevel)
            {
                return false;
            }

            var diamonds = GetDiamonds();
            var cost = node.BaseCost + level * node.CostPerLevel;
            if (diamonds < cost)
            {
                return false;
            }

            SetDiamonds(diamonds - cost);
            SetSkillLevel(node.Id, level + 1);
            PlayerPrefs.Save();
            RefreshSkillRuntimeBonuses();
            return true;
        }

        private bool AllBaseSkillsMaxed()
        {
            foreach (var node in SkillNodes)
            {
                if (node.PrestigePage)
                {
                    continue;
                }

                if (GetSkillLevel(node.Id) < node.MaxLevel)
                {
                    return false;
                }
            }

            return true;
        }

        private int GetPrestigeCost()
        {
            var prestige = PlayerPrefs.GetInt("td_prestige_level", 0);
            return 50 + prestige * 30;
        }

        private bool TryPrestige()
        {
            if (!AllBaseSkillsMaxed())
            {
                return false;
            }

            var diamonds = GetDiamonds();
            var cost = GetPrestigeCost();
            if (diamonds < cost)
            {
                return false;
            }

            SetDiamonds(diamonds - cost);
            var prestige = PlayerPrefs.GetInt("td_prestige_level", 0) + 1;
            PlayerPrefs.SetInt("td_prestige_level", prestige);
            foreach (var node in SkillNodes)
            {
                if (!node.PrestigePage)
                {
                    SetSkillLevel(node.Id, 0);
                }
            }

            PlayerPrefs.Save();
            RefreshSkillRuntimeBonuses();
            return true;
        }

        private void ShowSkillTreeOverlay(int page)
        {
            if (_state != RuntimeState.Menu)
            {
                return;
            }

            _skillTreePage = Mathf.Clamp(page, 0, 1);

            if (_overlayRoot != null)
            {
                Destroy(_overlayRoot);
                _overlayRoot = null;
            }

            _overlayRoot = new GameObject("SkillTreeOverlay", typeof(RectTransform));
            _overlayRoot.transform.SetParent(_canvas.transform, false);
            var rootRect = _overlayRoot.GetComponent<RectTransform>();
            SetRect(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TDVisuals.CreateUiBlock(_overlayRoot.transform, "Scrim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.78f));

            var card = TDVisuals.CreateUiBlock(
                _overlayRoot.transform,
                "Card",
                new Vector2(0.06f, 0.07f),
                new Vector2(0.94f, 0.92f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.06f, 0.10f, 0.16f, 0.98f));

            var title = TDVisuals.CreateText(card.transform, "Title", "Skill Tree", 48, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.04f, 0.91f), new Vector2(0.46f, 0.98f), Vector2.zero, Vector2.zero);

            var diamonds = GetDiamonds();
            var prestige = PlayerPrefs.GetInt("td_prestige_level", 0);
            var status = TDVisuals.CreateText(card.transform, "Status", "Diamonds " + diamonds + "  |  Prestige " + prestige, 24, TextAnchor.UpperRight, new Color(0.88f, 0.96f, 1f));
            SetRect(status.rectTransform, new Vector2(0.46f, 0.91f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

            var close = TDVisuals.CreateButton(card.transform, "Close", "Close", new Color(0.22f, 0.24f, 0.30f), Color.white);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0.86f, 0.84f), new Vector2(0.96f, 0.90f), Vector2.zero, Vector2.zero);
            close.onClick.AddListener(() =>
            {
                if (_overlayRoot != null)
                {
                    Destroy(_overlayRoot);
                    _overlayRoot = null;
                }
            });

            var pageBase = TDVisuals.CreateButton(card.transform, "PageBase", "Base Skills", new Color(_skillTreePage == 0 ? 0.24f : 0.12f, 0.20f, 0.34f), Color.white);
            SetRect(pageBase.GetComponent<RectTransform>(), new Vector2(0.04f, 0.84f), new Vector2(0.24f, 0.90f), Vector2.zero, Vector2.zero);
            pageBase.onClick.AddListener(() => ShowSkillTreeOverlay(0));

            var pagePrestige = TDVisuals.CreateButton(card.transform, "PagePrestige", "Prestige", new Color(_skillTreePage == 1 ? 0.24f : 0.12f, 0.20f, 0.34f), Color.white);
            SetRect(pagePrestige.GetComponent<RectTransform>(), new Vector2(0.25f, 0.84f), new Vector2(0.45f, 0.90f), Vector2.zero, Vector2.zero);
            pagePrestige.onClick.AddListener(() => ShowSkillTreeOverlay(1));

            var activeSkills = new List<SkillNode>();
            foreach (var node in SkillNodes)
            {
                if (node.PrestigePage == (_skillTreePage == 1))
                {
                    activeSkills.Add(node);
                }
            }

            if (_skillTreePage == 1 && prestige <= 0)
            {
                var lockedText = TDVisuals.CreateText(card.transform, "Locked", "Prestige once to unlock this page.", 32, TextAnchor.MiddleCenter, new Color(0.84f, 0.90f, 0.96f));
                SetRect(lockedText.rectTransform, new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.74f), Vector2.zero, Vector2.zero);
                return;
            }

            var rows = Mathf.Max(1, Mathf.CeilToInt(activeSkills.Count / 2f));
            for (var i = 0; i < activeSkills.Count; i++)
            {
                var node = activeSkills[i];
                var column = i % 2;
                var row = i / 2;
                var top = 0.80f - row * (0.68f / rows);
                var bottom = top - (0.64f / rows);
                var left = column == 0 ? 0.04f : 0.51f;
                var right = column == 0 ? 0.49f : 0.96f;

                var block = TDVisuals.CreateUiBlock(
                    card.transform,
                    "Skill_" + node.Id,
                    new Vector2(left, bottom),
                    new Vector2(right, top),
                    Vector2.zero,
                    Vector2.zero,
                    new Color(0.08f, 0.14f, 0.20f, 0.94f));

                var level = GetSkillLevel(node.Id);
                var cost = GetSkillUpgradeCost(node);

                var header = TDVisuals.CreateText(block.transform, "Name", node.Name + "  Lv." + level + "/" + node.MaxLevel, 22, TextAnchor.UpperLeft, Color.white);
                header.fontStyle = FontStyle.Bold;
                SetRect(header.rectTransform, new Vector2(0.03f, 0.54f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);

                var desc = TDVisuals.CreateText(block.transform, "Desc", node.Description, 18, TextAnchor.UpperLeft, new Color(0.80f, 0.88f, 0.94f));
                SetRect(desc.rectTransform, new Vector2(0.03f, 0.24f), new Vector2(0.66f, 0.56f), Vector2.zero, Vector2.zero);

                var upgrade = TDVisuals.CreateButton(block.transform, "Upgrade", "Upgrade", new Color(0.24f, 0.42f, 0.56f), Color.white);
                SetRect(upgrade.GetComponent<RectTransform>(), new Vector2(0.68f, 0.20f), new Vector2(0.97f, 0.74f), Vector2.zero, Vector2.zero);
                var buttonText = upgrade.GetComponentInChildren<Text>();
                if (level >= node.MaxLevel)
                {
                    upgrade.interactable = false;
                    buttonText.text = "MAX";
                }
                else if (diamonds < cost)
                {
                    upgrade.interactable = false;
                    buttonText.text = "$" + cost;
                }
                else
                {
                    buttonText.text = "$" + cost;
                    var skillId = node.Id;
                    upgrade.onClick.AddListener(() =>
                    {
                        if (TryUpgradeSkill(skillId))
                        {
                            ShowSkillTreeOverlay(_skillTreePage);
                        }
                    });
                }
            }

            if (_skillTreePage == 0)
            {
                var prestigeCost = GetPrestigeCost();
                var prestigeButton = TDVisuals.CreateButton(card.transform, "PrestigeButton", "Prestige", new Color(0.44f, 0.30f, 0.14f), Color.white);
                SetRect(prestigeButton.GetComponent<RectTransform>(), new Vector2(0.72f, 0.04f), new Vector2(0.96f, 0.10f), Vector2.zero, Vector2.zero);
                prestigeButton.interactable = AllBaseSkillsMaxed() && diamonds >= prestigeCost;
                prestigeButton.GetComponentInChildren<Text>().text = "Prestige $" + prestigeCost;
                prestigeButton.onClick.AddListener(() =>
                {
                    if (TryPrestige())
                    {
                        ShowSkillTreeOverlay(0);
                    }
                });

                var prestigeHint = TDVisuals.CreateText(card.transform, "PrestigeHint", "Max all base skills to prestige and unlock page 2.", 18, TextAnchor.MiddleLeft, new Color(0.84f, 0.90f, 0.96f));
                SetRect(prestigeHint.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.70f, 0.10f), Vector2.zero, Vector2.zero);
            }
        }

        private bool HasSavedRun()
        {
            return PlayerPrefs.GetInt(SavePrefix + "has", 0) == 1;
        }

        private void ClearSavedRun()
        {
            PlayerPrefs.DeleteKey(SavePrefix + "has");
            PlayerPrefs.DeleteKey(SavePrefix + "map");
            PlayerPrefs.DeleteKey(SavePrefix + "mode");
            PlayerPrefs.DeleteKey(SavePrefix + "campaign");
            PlayerPrefs.DeleteKey(SavePrefix + "wave");
            PlayerPrefs.DeleteKey(SavePrefix + "gold");
            PlayerPrefs.DeleteKey(SavePrefix + "score");
            PlayerPrefs.DeleteKey(SavePrefix + "base_hp");
            PlayerPrefs.DeleteKey(SavePrefix + "base_hp_max");
            PlayerPrefs.DeleteKey(SavePrefix + "intermission");
            PlayerPrefs.DeleteKey(SavePrefix + "speed");
            PlayerPrefs.DeleteKey(SavePrefix + "player_x");
            PlayerPrefs.DeleteKey(SavePrefix + "player_y");
            PlayerPrefs.DeleteKey(SavePrefix + "target_x");
            PlayerPrefs.DeleteKey(SavePrefix + "target_y");
            PlayerPrefs.DeleteKey(SavePrefix + "player_speed");
            PlayerPrefs.DeleteKey(SavePrefix + "player_range");
            PlayerPrefs.DeleteKey(SavePrefix + "player_damage");
            PlayerPrefs.DeleteKey(SavePrefix + "player_hp");
            PlayerPrefs.DeleteKey(SavePrefix + "player_hp_max");
            PlayerPrefs.DeleteKey(SavePrefix + "player_dash");
            PlayerPrefs.DeleteKey(SavePrefix + "player_lvl_dmg");
            PlayerPrefs.DeleteKey(SavePrefix + "player_lvl_spd");
            PlayerPrefs.DeleteKey(SavePrefix + "player_lvl_hp");
            PlayerPrefs.DeleteKey(SavePrefix + "base_lvl_hp");
            PlayerPrefs.DeleteKey(SavePrefix + "run_seed");
            PlayerPrefs.DeleteKey(SavePrefix + "daily_mods");
            PlayerPrefs.DeleteKey(SavePrefix + "towers");
            PlayerPrefs.DeleteKey(SavePrefix + "kills_run");
            PlayerPrefs.DeleteKey(SavePrefix + "bosses_run");
            PlayerPrefs.DeleteKey(SavePrefix + "diamonds_run");
            PlayerPrefs.DeleteKey(SavePrefix + "repairs_run");
            PlayerPrefs.DeleteKey(SavePrefix + "gold_earned_run");
            PlayerPrefs.DeleteKey(SavePrefix + "combo_count");
            PlayerPrefs.DeleteKey(SavePrefix + "combo_best");
            PlayerPrefs.DeleteKey(SavePrefix + "combo_timer");
            PlayerPrefs.DeleteKey(SavePrefix + "combo_mult");
            PlayerPrefs.DeleteKey(SavePrefix + "base_dmg_wave");
            PlayerPrefs.DeleteKey(SavePrefix + "base_dmg_run");
            PlayerPrefs.DeleteKey(SavePrefix + "power_cds");
            PlayerPrefs.DeleteKey(SavePrefix + "powers_used");
            PlayerPrefs.DeleteKey(SavePrefix + "tower_types_used");
            PlayerPrefs.Save();
        }

        private static string FloatToSave(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static float FloatFromSave(string value, float fallback = 0f)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private bool SaveRunSnapshot(bool autoSave = false, bool allowOverlaySave = false)
        {
            var canSaveState = _state == RuntimeState.Playing || (allowOverlaySave && _state == RuntimeState.Overlay && _overlayCanResume);
            if (!canSaveState)
            {
                return false;
            }

            if (!autoSave && (_enemies.Count > 0 || _spawnQueue.Count > 0))
            {
                ShowBanner("Save Blocked", "You can only manual-save between waves.", 1.2f);
                return false;
            }

            PlayerPrefs.SetInt(SavePrefix + "has", 1);
            PlayerPrefs.SetInt(SavePrefix + "map", (int)_activeMap);
            PlayerPrefs.SetInt(SavePrefix + "mode", (int)_mode);
            PlayerPrefs.SetInt(SavePrefix + "campaign", _campaignLevel?.Id ?? 0);
            PlayerPrefs.SetInt(SavePrefix + "wave", _wave);
            PlayerPrefs.SetInt(SavePrefix + "gold", _gold);
            PlayerPrefs.SetInt(SavePrefix + "score", _score);
            PlayerPrefs.SetInt(SavePrefix + "base_hp", _baseHp);
            PlayerPrefs.SetInt(SavePrefix + "base_hp_max", _maxBaseHp);
            PlayerPrefs.SetFloat(SavePrefix + "intermission", Mathf.Max(0f, _intermissionTimer));
            PlayerPrefs.SetFloat(SavePrefix + "speed", _gameSpeed);
            PlayerPrefs.SetFloat(SavePrefix + "player_x", _playerPosition.x);
            PlayerPrefs.SetFloat(SavePrefix + "player_y", _playerPosition.y);
            PlayerPrefs.SetFloat(SavePrefix + "target_x", _playerTarget.x);
            PlayerPrefs.SetFloat(SavePrefix + "target_y", _playerTarget.y);
            PlayerPrefs.SetFloat(SavePrefix + "player_speed", _playerSpeed);
            PlayerPrefs.SetFloat(SavePrefix + "player_range", _playerAttackRange);
            PlayerPrefs.SetFloat(SavePrefix + "player_damage", _playerAttackDamage);
            PlayerPrefs.SetFloat(SavePrefix + "player_hp", _playerHp);
            PlayerPrefs.SetFloat(SavePrefix + "player_hp_max", _playerMaxHp);
            PlayerPrefs.SetFloat(SavePrefix + "player_dash", _dashCooldown);
            PlayerPrefs.SetInt(SavePrefix + "player_lvl_dmg", _playerDamageLevel);
            PlayerPrefs.SetInt(SavePrefix + "player_lvl_spd", _playerSpeedLevel);
            PlayerPrefs.SetInt(SavePrefix + "player_lvl_hp", _playerHpLevel);
            PlayerPrefs.SetInt(SavePrefix + "base_lvl_hp", _baseHpLevel);
            PlayerPrefs.SetInt(SavePrefix + "kills_run", _killsThisRun);
            PlayerPrefs.SetInt(SavePrefix + "bosses_run", _bossesKilledThisRun);
            PlayerPrefs.SetInt(SavePrefix + "diamonds_run", _diamondsThisRun);
            PlayerPrefs.SetInt(SavePrefix + "repairs_run", _repairsThisRun);
            PlayerPrefs.SetInt(SavePrefix + "gold_earned_run", _goldEarnedThisRun);
            PlayerPrefs.SetInt(SavePrefix + "combo_count", _comboCount);
            PlayerPrefs.SetInt(SavePrefix + "combo_best", _bestCombo);
            PlayerPrefs.SetFloat(SavePrefix + "combo_timer", _comboTimer);
            PlayerPrefs.SetFloat(SavePrefix + "combo_mult", _comboMultiplier);
            PlayerPrefs.SetInt(SavePrefix + "base_dmg_wave", _baseDamagedThisWave ? 1 : 0);
            PlayerPrefs.SetInt(SavePrefix + "base_dmg_run", _baseDamagedThisRun ? 1 : 0);
            PlayerPrefs.SetInt(SavePrefix + "run_seed", _runSeed);

            var modifierBuilder = new StringBuilder();
            for (var i = 0; i < _dailyModifierRotation.Count; i++)
            {
                if (i > 0)
                {
                    modifierBuilder.Append(",");
                }
                modifierBuilder.Append((int)_dailyModifierRotation[i]);
            }
            PlayerPrefs.SetString(SavePrefix + "daily_mods", modifierBuilder.ToString());

            var cooldownBuilder = new StringBuilder();
            foreach (var cooldown in _powerCooldowns)
            {
                if (cooldownBuilder.Length > 0)
                {
                    cooldownBuilder.Append(",");
                }

                cooldownBuilder
                    .Append((int)cooldown.Key)
                    .Append(":")
                    .Append(FloatToSave(cooldown.Value));
            }
            PlayerPrefs.SetString(SavePrefix + "power_cds", cooldownBuilder.ToString());

            var powersUsedBuilder = new StringBuilder();
            foreach (var power in _powersUsedThisRun)
            {
                if (powersUsedBuilder.Length > 0)
                {
                    powersUsedBuilder.Append(",");
                }

                powersUsedBuilder.Append((int)power);
            }
            PlayerPrefs.SetString(SavePrefix + "powers_used", powersUsedBuilder.ToString());

            var towerTypesBuilder = new StringBuilder();
            foreach (var towerType in _towerTypesPlacedThisRun)
            {
                if (towerTypesBuilder.Length > 0)
                {
                    towerTypesBuilder.Append(",");
                }

                towerTypesBuilder.Append((int)towerType);
            }
            PlayerPrefs.SetString(SavePrefix + "tower_types_used", towerTypesBuilder.ToString());

            var towerBuilder = new StringBuilder();
            foreach (var tower in _towers)
            {
                if (towerBuilder.Length > 0)
                {
                    towerBuilder.Append("|");
                }

                var position = (Vector2)tower.Root.transform.position;
                towerBuilder
                    .Append((int)tower.Definition.Type).Append(";")
                    .Append(FloatToSave(position.x)).Append(";")
                    .Append(FloatToSave(position.y)).Append(";")
                    .Append(tower.Level).Append(";")
                    .Append(FloatToSave(tower.Damage)).Append(";")
                    .Append(FloatToSave(tower.Range)).Append(";")
                    .Append(FloatToSave(tower.FireRate)).Append(";")
                    .Append(FloatToSave(tower.Cooldown)).Append(";")
                    .Append(FloatToSave(tower.AbilityTimer)).Append(";")
                    .Append(tower.InvestedCost).Append(";")
                    .Append(tower.Specialization).Append(";")
                    .Append(FloatToSave(tower.AbilityCooldownScale));
            }
            PlayerPrefs.SetString(SavePrefix + "towers", towerBuilder.ToString());

            PlayerPrefs.Save();
            if (!autoSave)
            {
                ShowBanner("Run Saved", "Use Continue on the main menu to resume.", 1.4f);
            }

            return true;
        }

        private void ContinueSavedRun()
        {
            if (!HasSavedRun())
            {
                return;
            }

            var map = (MapType)Mathf.Clamp(PlayerPrefs.GetInt(SavePrefix + "map", (int)MapType.Classic), 0, Enum.GetValues(typeof(MapType)).Length - 1);
            var mode = (GameMode)Mathf.Clamp(PlayerPrefs.GetInt(SavePrefix + "mode", (int)GameMode.Endless), 0, Enum.GetValues(typeof(GameMode)).Length - 1);
            var campaignId = PlayerPrefs.GetInt(SavePrefix + "campaign", 0);

            CampaignLevel? campaign = null;
            if (campaignId > 0)
            {
                foreach (var level in TDData.Campaign)
                {
                    if (level.Id == campaignId)
                    {
                        campaign = level;
                        break;
                    }
                }
            }

            _loadingSavedRun = true;
            StartRun(map, mode, campaign);
            _loadingSavedRun = false;

            _wave = PlayerPrefs.GetInt(SavePrefix + "wave", _wave);
            _gold = PlayerPrefs.GetInt(SavePrefix + "gold", _gold);
            _score = PlayerPrefs.GetInt(SavePrefix + "score", _score);
            _baseHp = Mathf.Clamp(PlayerPrefs.GetInt(SavePrefix + "base_hp", _baseHp), 0, 99999);
            _maxBaseHp = Mathf.Max(1, PlayerPrefs.GetInt(SavePrefix + "base_hp_max", _maxBaseHp));
            _intermissionTimer = Mathf.Max(0f, PlayerPrefs.GetFloat(SavePrefix + "intermission", 1.2f));
            _gameSpeed = Mathf.Clamp(PlayerPrefs.GetFloat(SavePrefix + "speed", 1f), 1f, 3f);

            _playerPosition = new Vector2(
                PlayerPrefs.GetFloat(SavePrefix + "player_x", _playerPosition.x),
                PlayerPrefs.GetFloat(SavePrefix + "player_y", _playerPosition.y));
            _playerTarget = new Vector2(
                PlayerPrefs.GetFloat(SavePrefix + "target_x", _playerTarget.x),
                PlayerPrefs.GetFloat(SavePrefix + "target_y", _playerTarget.y));
            _playerSpeed = PlayerPrefs.GetFloat(SavePrefix + "player_speed", _playerSpeed);
            _playerAttackRange = PlayerPrefs.GetFloat(SavePrefix + "player_range", _playerAttackRange);
            _playerAttackDamage = PlayerPrefs.GetFloat(SavePrefix + "player_damage", _playerAttackDamage);
            _playerHp = PlayerPrefs.GetFloat(SavePrefix + "player_hp", _playerHp);
            _playerMaxHp = PlayerPrefs.GetFloat(SavePrefix + "player_hp_max", _playerMaxHp);
            _playerHp = Mathf.Clamp(_playerHp, 1f, _playerMaxHp);
            _dashCooldown = PlayerPrefs.GetFloat(SavePrefix + "player_dash", _dashCooldown);
            _playerDamageLevel = Mathf.Max(1, PlayerPrefs.GetInt(SavePrefix + "player_lvl_dmg", _playerDamageLevel));
            _playerSpeedLevel = Mathf.Max(1, PlayerPrefs.GetInt(SavePrefix + "player_lvl_spd", _playerSpeedLevel));
            _playerHpLevel = Mathf.Max(1, PlayerPrefs.GetInt(SavePrefix + "player_lvl_hp", _playerHpLevel));
            _baseHpLevel = Mathf.Max(1, PlayerPrefs.GetInt(SavePrefix + "base_lvl_hp", _baseHpLevel));
            _killsThisRun = Mathf.Max(0, PlayerPrefs.GetInt(SavePrefix + "kills_run", _killsThisRun));
            _bossesKilledThisRun = Mathf.Max(0, PlayerPrefs.GetInt(SavePrefix + "bosses_run", _bossesKilledThisRun));
            _diamondsThisRun = Mathf.Max(0, PlayerPrefs.GetInt(SavePrefix + "diamonds_run", _diamondsThisRun));
            _repairsThisRun = Mathf.Max(0, PlayerPrefs.GetInt(SavePrefix + "repairs_run", _repairsThisRun));
            _goldEarnedThisRun = Mathf.Max(0, PlayerPrefs.GetInt(SavePrefix + "gold_earned_run", _goldEarnedThisRun));
            _comboCount = Mathf.Max(0, PlayerPrefs.GetInt(SavePrefix + "combo_count", _comboCount));
            _bestCombo = Mathf.Max(0, PlayerPrefs.GetInt(SavePrefix + "combo_best", _bestCombo));
            _comboTimer = Mathf.Max(0f, PlayerPrefs.GetFloat(SavePrefix + "combo_timer", _comboTimer));
            _comboMultiplier = Mathf.Max(1f, PlayerPrefs.GetFloat(SavePrefix + "combo_mult", _comboMultiplier));
            _baseDamagedThisWave = PlayerPrefs.GetInt(SavePrefix + "base_dmg_wave", 0) == 1;
            _baseDamagedThisRun = PlayerPrefs.GetInt(SavePrefix + "base_dmg_run", 0) == 1;

            if (_playerRoot != null)
            {
                _playerRoot.transform.position = _playerPosition;
            }
            UpdatePlayerBars();

            _runSeed = PlayerPrefs.GetInt(SavePrefix + "run_seed", _runSeed);
            _runRng = new System.Random(_runSeed == 0 ? Environment.TickCount : _runSeed);
            _dailyModifierRotation.Clear();
            var mods = PlayerPrefs.GetString(SavePrefix + "daily_mods", string.Empty);
            if (!string.IsNullOrWhiteSpace(mods))
            {
                var modTokens = mods.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in modTokens)
                {
                    if (int.TryParse(token, out var modValue) && Enum.IsDefined(typeof(TDWaveModifier), modValue))
                    {
                        _dailyModifierRotation.Add((TDWaveModifier)modValue);
                    }
                }
            }

            var cooldowns = PlayerPrefs.GetString(SavePrefix + "power_cds", string.Empty);
            if (!string.IsNullOrWhiteSpace(cooldowns))
            {
                var cooldownTokens = cooldowns.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in cooldownTokens)
                {
                    var parts = token.Split(':');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out var powerValue) || !Enum.IsDefined(typeof(PowerType), powerValue))
                    {
                        continue;
                    }

                    var powerType = (PowerType)powerValue;
                    if (_powerCooldowns.ContainsKey(powerType))
                    {
                        _powerCooldowns[powerType] = Mathf.Max(0f, FloatFromSave(parts[1]));
                    }
                }
            }

            _powersUsedThisRun.Clear();
            var powersUsed = PlayerPrefs.GetString(SavePrefix + "powers_used", string.Empty);
            if (!string.IsNullOrWhiteSpace(powersUsed))
            {
                var powerTokens = powersUsed.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in powerTokens)
                {
                    if (int.TryParse(token, out var powerValue) && Enum.IsDefined(typeof(PowerType), powerValue))
                    {
                        _powersUsedThisRun.Add((PowerType)powerValue);
                    }
                }
            }

            _towerTypesPlacedThisRun.Clear();
            var towerTypes = PlayerPrefs.GetString(SavePrefix + "tower_types_used", string.Empty);
            if (!string.IsNullOrWhiteSpace(towerTypes))
            {
                var towerTokens = towerTypes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in towerTokens)
                {
                    if (int.TryParse(token, out var towerTypeValue) && Enum.IsDefined(typeof(TowerType), towerTypeValue))
                    {
                        _towerTypesPlacedThisRun.Add((TowerType)towerTypeValue);
                    }
                }
            }

            foreach (var tower in _towers)
            {
                if (tower.Root != null)
                {
                    Destroy(tower.Root);
                }
            }
            _towers.Clear();

            var serializedTowers = PlayerPrefs.GetString(SavePrefix + "towers", string.Empty);
            if (!string.IsNullOrWhiteSpace(serializedTowers))
            {
                var towerEntries = serializedTowers.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var entry in towerEntries)
                {
                    var parts = entry.Split(';');
                    if (parts.Length < 12)
                    {
                        continue;
                    }

                    if (!int.TryParse(parts[0], out var towerTypeValue))
                    {
                        continue;
                    }

                    var towerType = (TowerType)towerTypeValue;
                    if (!TDData.Towers.TryGetValue(towerType, out var definition))
                    {
                        continue;
                    }

                    var position = new Vector2(FloatFromSave(parts[1]), FloatFromSave(parts[2]));
                    var tower = CreateTower(position, definition);
                    tower.Level = Mathf.Max(1, int.TryParse(parts[3], out var parsedLevel) ? parsedLevel : 1);
                    tower.Damage = FloatFromSave(parts[4], tower.Damage);
                    tower.Range = FloatFromSave(parts[5], tower.Range);
                    tower.FireRate = FloatFromSave(parts[6], tower.FireRate);
                    tower.Cooldown = FloatFromSave(parts[7], tower.Cooldown);
                    tower.AbilityTimer = FloatFromSave(parts[8], tower.AbilityTimer);
                    tower.InvestedCost = int.TryParse(parts[9], out var parsedCost) ? parsedCost : tower.InvestedCost;
                    tower.Specialization = int.TryParse(parts[10], out var parsedSpec) ? parsedSpec : 0;
                    tower.AbilityCooldownScale = FloatFromSave(parts[11], tower.AbilityCooldownScale);
                    if (tower.Specialization > 0)
                    {
                        var specializationColor = tower.Specialization == 1 ? new Color(1f, 0.92f, 0.62f) : new Color(0.70f, 0.88f, 1f);
                        tower.Base.color = Color.Lerp(tower.Base.color, specializationColor, 0.30f);
                        tower.Top.color = Color.Lerp(tower.Top.color, specializationColor, 0.20f);
                    }

                    _towers.Add(tower);
                }
            }

            _spawnQueue = new Queue<SpawnRequest>();
            _spawnTimer = 0f;
            _currentWaveModifier = TDWaveModifier.None;
            _currentWaveBoss = null;
            _isNight = ((_wave / 8) % 2) == 1;
            ShowBanner("Run Restored", "Resumed at wave " + _wave + ".", 1.8f);
        }

        private void UnlockAchievement(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            var key = "td_ach_" + id;
            if (PlayerPrefs.GetInt(key, 0) == 1)
            {
                return;
            }

            PlayerPrefs.SetInt(key, 1);
            var title = "Achievement Unlocked";
            var body = id;
            foreach (var achievement in TDLegacyData.Achievements)
            {
                if (achievement.Id == id)
                {
                    title = achievement.Title;
                    body = achievement.Description;
                    break;
                }
            }

            ShowBanner(title, body, 2.0f);
            PlayerPrefs.Save();
        }

        private void BuildOverlay(bool victory, string title, string body)
        {
            _overlayCanResume = false;
            _state = RuntimeState.Overlay;
            if (_overlayRoot != null)
            {
                Destroy(_overlayRoot);
            }

            _overlayRoot = new GameObject("OverlayRoot", typeof(RectTransform));
            _overlayRoot.transform.SetParent(_canvas.transform, false);
            var rect = _overlayRoot.GetComponent<RectTransform>();
            SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TDVisuals.CreateUiBlock(_overlayRoot.transform, "Scrim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.74f));
            var card = TDVisuals.CreateUiBlock(
                _overlayRoot.transform,
                "Card",
                new Vector2(0.14f, 0.24f),
                new Vector2(0.86f, 0.72f),
                Vector2.zero,
                Vector2.zero,
                victory ? new Color(0.08f, 0.14f, 0.18f, 0.98f) : new Color(0.18f, 0.08f, 0.08f, 0.98f));

            TDVisuals.CreateUiBlock(
                card.transform,
                "Accent",
                new Vector2(0.06f, 0.88f),
                new Vector2(0.94f, 0.91f),
                Vector2.zero,
                Vector2.zero,
                victory ? new Color(0.52f, 0.92f, 0.86f) : new Color(1f, 0.48f, 0.32f));

            var titleText = TDVisuals.CreateText(card.transform, "Title", title, 48, TextAnchor.UpperCenter, Color.white);
            titleText.fontStyle = FontStyle.Bold;
            SetRect(titleText.rectTransform, new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);

            var bodyText = TDVisuals.CreateText(card.transform, "Body", body, 26, TextAnchor.UpperCenter, new Color(0.82f, 0.90f, 0.96f));
            SetRect(bodyText.rectTransform, new Vector2(0.10f, 0.50f), new Vector2(0.90f, 0.68f), Vector2.zero, Vector2.zero);

            var scoreText = TDVisuals.CreateText(card.transform, "Score", "Score " + _score + "  |  Wave " + _wave, 24, TextAnchor.MiddleCenter, victory ? new Color(0.92f, 0.86f, 0.38f) : new Color(1f, 0.72f, 0.42f));
            SetRect(scoreText.rectTransform, new Vector2(0.18f, 0.38f), new Vector2(0.82f, 0.46f), Vector2.zero, Vector2.zero);

            var retry = TDVisuals.CreateButton(card.transform, "Retry", "Run Again", new Color(0.94f, 0.74f, 0.24f), new Color(0.14f, 0.10f, 0.06f));
            SetRect(retry.GetComponent<RectTransform>(), new Vector2(0.14f, 0.20f), new Vector2(0.86f, 0.30f), Vector2.zero, Vector2.zero);
            retry.onClick.AddListener(() => StartRun(_activeMap, _mode, _campaignLevel));

            var menu = TDVisuals.CreateButton(card.transform, "Menu", "Main Menu", new Color(0.18f, 0.24f, 0.34f), Color.white);
            SetRect(menu.GetComponent<RectTransform>(), new Vector2(0.14f, 0.08f), new Vector2(0.48f, 0.16f), Vector2.zero, Vector2.zero);
            menu.onClick.AddListener(BuildMenu);

            if (victory && _campaignLevel != null && _campaignLevel.Id < TDData.Campaign.Count)
            {
                var next = TDVisuals.CreateButton(card.transform, "Next", "Next Sector", new Color(0.10f, 0.20f, 0.26f), Color.white);
                SetRect(next.GetComponent<RectTransform>(), new Vector2(0.52f, 0.08f), new Vector2(0.86f, 0.16f), Vector2.zero, Vector2.zero);
                var nextLevel = TDData.Campaign[Mathf.Min(_campaignLevel.Id, TDData.Campaign.Count - 1)];
                next.onClick.AddListener(() => StartRun(nextLevel.Map, GameMode.Campaign, nextLevel));
            }
            else
            {
                var endless = TDVisuals.CreateButton(card.transform, "Endless", "Try Endless", new Color(0.10f, 0.20f, 0.26f), Color.white);
                SetRect(endless.GetComponent<RectTransform>(), new Vector2(0.52f, 0.08f), new Vector2(0.86f, 0.16f), Vector2.zero, Vector2.zero);
                endless.onClick.AddListener(() => StartRun(_activeMap, GameMode.Endless, null));
            }
        }

        private void CycleSpeed()
        {
            if (_gameSpeed < 1.5f)
            {
                _gameSpeed = 2f;
            }
            else if (_gameSpeed < 2.5f)
            {
                _gameSpeed = 3f;
            }
            else
            {
                _gameSpeed = 1f;
            }
        }

        private void RefreshHud()
        {
            if (_gold >= 500)
            {
                UnlockAchievement("rich");
            }
            if (_gold >= 1000)
            {
                UnlockAchievement("rich_1000");
            }
            if (_gold >= 2000)
            {
                UnlockAchievement("gold_hoarder");
            }

            if (_goldText != null)
            {
                _goldText.text = "$" + _gold;
            }
            if (_waveText != null)
            {
                _waveText.text = _wave == 0 ? "Prep" : _wave.ToString();
            }
            if (_scoreText != null)
            {
                _scoreText.text = _score.ToString();
            }
            if (_hpText != null)
            {
                _hpText.text = _baseHp + "/" + _maxBaseHp;
                _hpText.color = _baseHp > 40 ? Color.white : new Color(1f, 0.66f, 0.52f);
            }
            if (_playerHpText != null)
            {
                _playerHpText.text = Mathf.CeilToInt(_playerHp) + "/" + Mathf.CeilToInt(_playerMaxHp);
                _playerHpText.color = _playerHp > _playerMaxHp * 0.4f ? Color.white : new Color(1f, 0.66f, 0.52f);
            }
            if (_dashButton != null)
            {
                var dashReady = _dashCooldown <= 0f;
                _dashButton.interactable = dashReady;
                _dashButton.GetComponent<Image>().color = _dashArmed
                    ? new Color(0.16f, 0.74f, 0.82f)
                    : dashReady
                        ? new Color(0.16f, 0.54f, 0.62f)
                        : new Color(0.12f, 0.24f, 0.28f);
                _dashButton.GetComponentInChildren<Text>().text = dashReady
                    ? (_dashArmed ? "\uD83D\uDCA8 DASH!" : "\uD83D\uDCA8 DASH")
                    : "\uD83D\uDCA8 DASH\n" + _dashCooldown.ToString("0.0") + "s";
            }
            if (_speedButton != null)
            {
                _speedButton.GetComponentInChildren<Text>().text = "\u25B6 x" + _gameSpeed.ToString("0.0");
            }

            if (_upgradePlayerDamageButton != null)
            {
                var cost = GetPlayerDamageUpgradeCost();
                _upgradePlayerDamageButton.interactable = _upgradesEnabled && _gold >= cost;
                _upgradePlayerDamageButton.GetComponentInChildren<Text>().text = _upgradesEnabled ? "ATK $" + cost + "\nL" + _playerDamageLevel : "ATK\nLocked";
            }
            if (_upgradePlayerSpeedButton != null)
            {
                var cost = GetPlayerSpeedUpgradeCost();
                _upgradePlayerSpeedButton.interactable = _upgradesEnabled && _gold >= cost;
                _upgradePlayerSpeedButton.GetComponentInChildren<Text>().text = _upgradesEnabled ? "SPD $" + cost + "\nL" + _playerSpeedLevel : "SPD\nLocked";
            }
            if (_upgradePlayerHpButton != null)
            {
                var cost = GetPlayerHpUpgradeCost();
                _upgradePlayerHpButton.interactable = _upgradesEnabled && _gold >= cost;
                _upgradePlayerHpButton.GetComponentInChildren<Text>().text = _upgradesEnabled ? "HP $" + cost + "\nL" + _playerHpLevel : "HP\nLocked";
            }
            if (_upgradeBaseHpButton != null)
            {
                var cost = GetBaseHpUpgradeCost();
                _upgradeBaseHpButton.interactable = _upgradesEnabled && _gold >= cost;
                _upgradeBaseHpButton.GetComponentInChildren<Text>().text = _upgradesEnabled ? "BASE $" + cost + "\nL" + _baseHpLevel : "BASE\nLocked";
            }

            foreach (var pair in _towerButtons)
            {
                var tower = TDData.Towers[pair.Key];
                var button = pair.Value;
                var selected = _placingTowerType == pair.Key;
                var affordable = _gold >= tower.Cost;
                button.interactable = affordable;
                button.GetComponent<Image>().color = selected ? tower.Accent : new Color(tower.Accent.r * 0.4f, tower.Accent.g * 0.4f, tower.Accent.b * 0.4f, affordable ? 1f : 0.55f);
                button.GetComponentInChildren<Text>().text = tower.Name + "\n<size=18>$" + tower.Cost + "</size>";
            }

            foreach (var pair in _powerButtons)
            {
                var power = TDData.Powers[pair.Key];
                var button = pair.Value;
                var cooldown = _powerCooldowns.ContainsKey(pair.Key) ? _powerCooldowns[pair.Key] : 0f;
                var active = _armedPowerType == pair.Key;
                var ready = cooldown <= 0f && _gold >= power.Cost;
                button.interactable = cooldown <= 0f;
                button.GetComponent<Image>().color = active ? power.Color : new Color(power.Color.r * 0.55f, power.Color.g * 0.55f, power.Color.b * 0.55f, ready ? 1f : 0.55f);
                button.GetComponentInChildren<Text>().text = cooldown > 0f
                    ? power.Name + "\n<size=16>" + cooldown.ToString("0.0") + "s</size>"
                    : power.Name + "\n<size=16>$" + power.Cost + "</size>";
            }

            if (_selectedTowerText != null && _selectedTowerStatsText != null && _upgradeButton != null && _sellButton != null)
            {
                if (_selectedTower == null)
                {
                    _selectedTowerText.text = "No Tower Selected";
                    _selectedTowerStatsText.text = "Tap a tower to inspect and upgrade it.";
                    _upgradeButton.interactable = false;
                    _upgradeButton.GetComponentInChildren<Text>().text = "Upgrade";
                    _sellButton.interactable = false;
                    SetSpecializationButtonsVisible(false);
                }
                else
                {
                    var specializationName = _selectedTower.Specialization > 0 ? "  " + GetSpecializationName(_selectedTower, _selectedTower.Specialization) : "";
                    _selectedTowerText.text = _selectedTower.Definition.Name + "  L" + _selectedTower.Level + specializationName;
                    _selectedTowerStatsText.text =
                        "Damage " + _selectedTower.Damage.ToString("0") +
                        "\nRange " + _selectedTower.Range.ToString("0.0") +
                        "\nRate " + _selectedTower.FireRate.ToString("0.00") +
                        "\nAbility " + GetTowerAbilityCooldown(_selectedTower).ToString("0.0") + "s";
                    _sellButton.interactable = true;
                    _sellButton.GetComponentInChildren<Text>().text = "Sell $" + Mathf.RoundToInt(_selectedTower.InvestedCost * 0.65f);

                    if (!_upgradesEnabled)
                    {
                        _upgradeButton.interactable = false;
                        _upgradeButton.GetComponentInChildren<Text>().text = "Upgrades Locked";
                        SetSpecializationButtonsVisible(false);
                    }
                    else if (NeedsSpecializationChoice(_selectedTower))
                    {
                        _upgradeButton.interactable = false;
                        _upgradeButton.GetComponentInChildren<Text>().text = "Choose Branch";
                        _selectedTowerStatsText.text += "\n\nChoose a specialization:";
                        if (_specializeAlphaButton != null && _specializeBetaButton != null)
                        {
                            _specializeAlphaButton.GetComponentInChildren<Text>().text = GetSpecializationName(_selectedTower, 1) + "\n<size=14>" + GetSpecializationSummary(_selectedTower, 1) + "</size>";
                            _specializeBetaButton.GetComponentInChildren<Text>().text = GetSpecializationName(_selectedTower, 2) + "\n<size=14>" + GetSpecializationSummary(_selectedTower, 2) + "</size>";
                        }
                        SetSpecializationButtonsVisible(true);
                    }
                    else
                    {
                        var upgradeCost = GetUpgradeCost(_selectedTower);
                        _upgradeButton.interactable = upgradeCost > 0 && _gold >= upgradeCost;
                        _upgradeButton.GetComponentInChildren<Text>().text = upgradeCost > 0 ? "Upgrade $" + upgradeCost : "Upgrade Max";
                        SetSpecializationButtonsVisible(false);
                    }
                }
            }

            if (_bannerTimer <= 0f)
            {
                if (_placingTowerType.HasValue)
                {
                    var tower = TDData.Towers[_placingTowerType.Value];
                    if (_bannerText != null)
                    {
                        _bannerText.text = "Placing " + tower.Name;
                        _bannerText.color = Color.white;
                    }
                    if (_hintText != null)
                    {
                        _hintText.text = tower.Description + " Click the battlefield to place. Right-click cancels.";
                        _hintText.color = new Color(0.84f, 0.90f, 0.96f);
                    }
                }
                else if (_armedPowerType.HasValue)
                {
                    var power = TDData.Powers[_armedPowerType.Value];
                    if (_bannerText != null)
                    {
                        _bannerText.text = power.Name + " Armed";
                        _bannerText.color = Color.white;
                    }
                    if (_hintText != null)
                    {
                        _hintText.text = power.Description + " Click to deploy.";
                        _hintText.color = new Color(0.84f, 0.90f, 0.96f);
                    }
                }
                else if (_dashArmed)
                {
                    if (_bannerText != null)
                    {
                        _bannerText.text = "Dash Armed";
                        _bannerText.color = Color.white;
                    }
                    if (_hintText != null)
                    {
                        _hintText.text = "Click destination to blink forward and hit enemies in the path.";
                        _hintText.color = new Color(0.84f, 0.90f, 0.96f);
                    }
                }
                else
                {
                    if (_bannerText != null)
                    {
                        _bannerText.text = _intermissionTimer > 0f ? "Prepare" : (_isNight ? "Night Watch" : "Hold the Line");
                        _bannerText.color = Color.white;
                    }
                    if (_hintText != null)
                    {
                        if (_intermissionTimer > 0f)
                        {
                            _hintText.text = "Wave " + (_wave + 1) + " incoming. Spend your gold.";
                        }
                        else
                        {
                            var status = _defaultHint;
                            if (_isNight)
                            {
                                status = "Night active: enemy HP up, tower range down.";
                            }
                            if (_currentWaveModifier != TDWaveModifier.None)
                            {
                                status += " Modifier: " + GetModifierLabel(_currentWaveModifier) + ".";
                            }

                            _hintText.text = status;
                        }
                        _hintText.color = new Color(0.84f, 0.90f, 0.96f);
                    }
                }
            }
        }

        private void ShowBanner(string title, string body, float duration)
        {
            _bannerTimer = duration;
            if (_bannerText != null)
            {
                _bannerText.text = title;
                _bannerText.color = Color.white;
            }
            if (_hintText != null)
            {
                _hintText.text = body;
                _hintText.color = new Color(0.84f, 0.90f, 0.96f);
            }
        }

        private void SpawnProjectileVisual(Vector2 start, Vector2 end, Color color, float speed, float arcHeight)
        {
            var go = TDVisuals.CreateWorldSprite("Projectile", _worldRoot, start, new Vector2(0.18f, 0.18f), color, 18);
            var projectile = new ProjectileVisual
            {
                Root = go,
                Renderer = go.GetComponent<SpriteRenderer>(),
                Start = start,
                End = end,
                Progress = 0f,
                Speed = speed,
                Distance = Vector2.Distance(start, end),
                ArcHeight = arcHeight
            };

            _projectiles.Add(projectile);
        }

        private List<List<Vector2>> GetPathsForMap(MapType type)
        {
            List<List<Vector2>> basePaths;
            switch (type)
            {
                case MapType.Classic:
                    basePaths = new List<List<Vector2>>
                    {
                        new()
                        {
                            new Vector2(-4.0f, 7.6f),
                            new Vector2(-4.0f, 5.3f),
                            new Vector2(-2.6f, 3.6f),
                            new Vector2(-1.9f, 1.4f),
                            new Vector2(-2.4f, -1.2f),
                            new Vector2(-1.0f, -3.9f),
                            _basePosition
                        },
                        new()
                        {
                            new Vector2(0f, 7.6f),
                            new Vector2(0.2f, 5.1f),
                            new Vector2(1.8f, 3.2f),
                            new Vector2(0.8f, 1.0f),
                            new Vector2(1.8f, -1.4f),
                            new Vector2(0.8f, -4.1f),
                            _basePosition
                        },
                        new()
                        {
                            new Vector2(4.0f, 7.6f),
                            new Vector2(3.6f, 5.2f),
                            new Vector2(2.4f, 3.5f),
                            new Vector2(1.6f, 1.2f),
                            new Vector2(2.6f, -1.6f),
                            new Vector2(1.2f, -4.0f),
                            _basePosition
                        }
                    };
                    break;
                case MapType.Valley:
                    basePaths = new List<List<Vector2>>
                    {
                        new()
                        {
                            new Vector2(-3.4f, 7.6f),
                            new Vector2(-4.2f, 5.3f),
                            new Vector2(-2.2f, 3.2f),
                            new Vector2(-0.4f, 1.6f),
                            new Vector2(-1.0f, -1.0f),
                            new Vector2(-0.2f, -4.2f),
                            _basePosition
                        },
                        new()
                        {
                            new Vector2(3.4f, 7.6f),
                            new Vector2(4.2f, 5.4f),
                            new Vector2(2.4f, 3.2f),
                            new Vector2(0.8f, 1.4f),
                            new Vector2(1.2f, -1.2f),
                            new Vector2(0.4f, -4.0f),
                            _basePosition
                        }
                    };
                    break;
                case MapType.Crossroads:
                    basePaths = new List<List<Vector2>>
                    {
                        new()
                        {
                            new Vector2(-4.0f, 7.6f),
                            new Vector2(-2.6f, 5.0f),
                            new Vector2(-1.4f, 2.8f),
                            new Vector2(-0.4f, 0.4f),
                            _basePosition
                        },
                        new()
                        {
                            new Vector2(4.0f, 7.6f),
                            new Vector2(2.6f, 5.0f),
                            new Vector2(1.4f, 2.8f),
                            new Vector2(0.4f, 0.4f),
                            _basePosition
                        },
                        new()
                        {
                            new Vector2(-1.2f, 7.6f),
                            new Vector2(-0.6f, 5.2f),
                            new Vector2(-1.8f, 3.2f),
                            new Vector2(-0.8f, 1.2f),
                            _basePosition
                        },
                        new()
                        {
                            new Vector2(1.2f, 7.6f),
                            new Vector2(0.6f, 5.2f),
                            new Vector2(1.8f, 3.2f),
                            new Vector2(0.8f, 1.2f),
                            _basePosition
                        }
                    };
                    break;
                case MapType.Desert:
                    basePaths = new List<List<Vector2>>
                    {
                        new()
                        {
                            new Vector2(4.0f, 7.6f),
                            new Vector2(2.0f, 6.2f),
                            new Vector2(-1.6f, 5.0f),
                            new Vector2(-2.8f, 2.2f),
                            new Vector2(-0.8f, 0.2f),
                            new Vector2(1.8f, -2.0f),
                            new Vector2(0.2f, -4.8f),
                            _basePosition
                        },
                        new()
                        {
                            new Vector2(-4.0f, 7.6f),
                            new Vector2(-2.2f, 6.0f),
                            new Vector2(1.4f, 4.8f),
                            new Vector2(2.8f, 2.0f),
                            new Vector2(0.9f, 0.0f),
                            new Vector2(-1.8f, -2.2f),
                            new Vector2(-0.2f, -4.6f),
                            _basePosition
                        }
                    };
                    break;
                case MapType.Snow:
                    basePaths = new List<List<Vector2>>
                    {
                        new()
                        {
                            new Vector2(-4.0f, 7.6f),
                            new Vector2(-2.6f, 5.8f),
                            new Vector2(-0.4f, 4.2f),
                            new Vector2(1.0f, 2.0f),
                            new Vector2(-0.6f, -0.6f),
                            new Vector2(0.4f, -3.6f),
                            _basePosition
                        },
                        new()
                        {
                            new Vector2(0f, 7.6f),
                            new Vector2(1.2f, 6.0f),
                            new Vector2(2.8f, 4.2f),
                            new Vector2(1.6f, 2.0f),
                            new Vector2(-1.0f, 0.4f),
                            new Vector2(-0.4f, -3.8f),
                            _basePosition
                        },
                        new()
                        {
                            new Vector2(4.0f, 7.6f),
                            new Vector2(2.8f, 5.8f),
                            new Vector2(0.6f, 4.0f),
                            new Vector2(-1.0f, 2.0f),
                            new Vector2(0.4f, -0.8f),
                            new Vector2(-0.2f, -3.9f),
                            _basePosition
                        }
                    };
                    break;
                case MapType.Volcano:
                    basePaths = new List<List<Vector2>>
                    {
                        new()
                        {
                            new Vector2(-4.0f, 7.6f),
                            new Vector2(-3.2f, 5.1f),
                            new Vector2(-1.8f, 3.0f),
                            new Vector2(-0.4f, 0.7f),
                            _basePosition
                        },
                        new()
                        {
                            new Vector2(4.0f, 7.6f),
                            new Vector2(3.2f, 5.3f),
                            new Vector2(1.8f, 3.2f),
                            new Vector2(0.4f, 0.9f),
                            _basePosition
                        }
                    };
                    break;
                default:
                    basePaths = new List<List<Vector2>>();
                    break;
            }

            return ApplyPathJitter(basePaths, type);
        }

        private List<List<Vector2>> ApplyPathJitter(List<List<Vector2>> basePaths, MapType mapType)
        {
            var jitterX = 0.22f;
            var jitterY = 0.30f;
            switch (mapType)
            {
                case MapType.Crossroads:
                    jitterX = 0.17f;
                    jitterY = 0.22f;
                    break;
                case MapType.Desert:
                    jitterX = 0.30f;
                    jitterY = 0.34f;
                    break;
                case MapType.Volcano:
                    jitterX = 0.14f;
                    jitterY = 0.20f;
                    break;
                case MapType.Snow:
                    jitterX = 0.24f;
                    jitterY = 0.28f;
                    break;
            }

            var jitteredPaths = new List<List<Vector2>>(basePaths.Count);
            foreach (var path in basePaths)
            {
                var jittered = new List<Vector2>(path.Count);
                for (var i = 0; i < path.Count; i++)
                {
                    var point = path[i];
                    if (i == 0 || i == path.Count - 1)
                    {
                        jittered.Add(point);
                        continue;
                    }

                    var x = Mathf.Clamp(point.x + NextRunRandom(-jitterX, jitterX), -4.2f, 4.2f);
                    var y = Mathf.Clamp(point.y + NextRunRandom(-jitterY, jitterY), -6.2f, 7.8f);
                    jittered.Add(new Vector2(x, y));
                }

                jitteredPaths.Add(jittered);
            }

            return jitteredPaths;
        }

        private float DistanceToSegment(Vector2 point, Vector2 segmentA, Vector2 segmentB)
        {
            var segment = segmentB - segmentA;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0.0001f)
            {
                return Vector2.Distance(point, segmentA);
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - segmentA, segment) / lengthSquared);
            var projection = segmentA + segment * t;
            return Vector2.Distance(point, projection);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void ClearRuntime()
        {
            _towerButtons.Clear();
            _powerButtons.Clear();
            _mapButtons.Clear();

            foreach (var tower in _towers)
            {
                if (tower.Root != null)
                {
                    Destroy(tower.Root);
                }
            }
            _towers.Clear();

            foreach (var enemy in _enemies)
            {
                if (enemy.Root != null)
                {
                    Destroy(enemy.Root);
                }
            }
            _enemies.Clear();

            foreach (var projectile in _projectiles)
            {
                if (projectile.Root != null)
                {
                    Destroy(projectile.Root);
                }
            }
            _projectiles.Clear();

            foreach (var decal in _footprints)
            {
                if (decal.Root != null)
                {
                    Destroy(decal.Root);
                }
            }
            _footprints.Clear();
            _paths.Clear();
            _bossPool.Clear();
            _dailyModifierRotation.Clear();
            _powersUsedThisRun.Clear();
            _towerTypesPlacedThisRun.Clear();
            _runSeed = 0;
            _runRng = new System.Random();
            _loadingSavedRun = false;
            _skillTreePage = 0;

            foreach (Transform child in _worldRoot)
            {
                Destroy(child.gameObject);
            }

            if (_menuRoot != null)
            {
                Destroy(_menuRoot);
                _menuRoot = null;
            }
            if (_hudRoot != null)
            {
                Destroy(_hudRoot);
                _hudRoot = null;
            }
            if (_overlayRoot != null)
            {
                Destroy(_overlayRoot);
                _overlayRoot = null;
            }

            _selectionPanel = null;
            _goldText = null;
            _waveText = null;
            _scoreText = null;
            _hpText = null;
            _bannerText = null;
            _hintText = null;
            _selectedTowerText = null;
            _selectedTowerStatsText = null;
            _playerHpText = null;
            _upgradeButton = null;
            _specializeAlphaButton = null;
            _specializeBetaButton = null;
            _sellButton = null;
            _dashButton = null;
            _speedButton = null;
            _campaignButton = null;
            _endlessButton = null;
            _continueButton = null;
            _skillTreeButton = null;
            _upgradePlayerDamageButton = null;
            _upgradePlayerSpeedButton = null;
            _upgradePlayerHpButton = null;
            _upgradeBaseHpButton = null;
            _playerRoot = null;
            _playerBody = null;
            _playerCore = null;
            _playerHpFill = null;
            _playerDashFill = null;
            _playerPosition = Vector2.zero;
            _playerTarget = Vector2.zero;
            _playerAttackTimer = 0f;
            _dashCooldown = 0f;
            _dashArmed = false;
            _overlayCanResume = false;
            _currentWaveModifier = TDWaveModifier.None;
            _currentWaveBoss = null;
            _isNight = false;
            _goldEarnedThisRun = 0;
            _baseDamagedThisWave = false;
            _baseDamagedThisRun = false;
            _totalEnemiesThisWave = 0;
            _enemiesSpawnedThisWave = 0;
        }
    }
}
