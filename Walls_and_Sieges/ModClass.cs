using System.Collections.Generic;
using HarmonyLib;
using NeoModLoader.api;
using ai.behaviours;

namespace HelloWorld
{
    public class ModClass : BasicMod<ModClass>
    {
        public static void HeartbeatPostfix() { Ram.Heartbeat(); Traits.Tick(); Siege.CraterTick(); Siege.OrphanTick(); Wf3D.Tick(); ManualGun.Tick(); TowerLadder.Tick(); TentEnter.Tick(); Scroll.Tick(); GunFlip.Tick(); }

        protected override void OnModLoad()
        {
            var harmony = new Harmony("hello.world.walls");
            harmony.Patch(
                AccessTools.Method(typeof(CityBehBuild), nameof(CityBehBuild.buildTick)),
                postfix: new HarmonyMethod(typeof(WallBuilder), nameof(WallBuilder.Postfix)));

            // Башни слабее (в игре 3000 хп): армии смогут их ломать
            try
            {
                foreach (string tid in new string[] { "watch_tower_human", "watch_tower_orc", "watch_tower_elf", "watch_tower_dwarf" })
                {
                    BuildingAsset tower = AssetManager.buildings.get(tid);
                    if (tower != null) tower.base_stats["health"] = WallBuilder.TOWER_HEALTH;
                }
            }
            catch (System.Exception e)
            {
                LogInfo("Tower health patch failed: " + e.Message);
            }

            try
            {
                WallBuilder.CreateCheapTower();
                LogInfo("Cheap tower created");
            }
            catch (System.Exception e)
            {
                LogInfo("Cheap tower failed: " + e);
            }

            try
            {
                WallBuilder.CreateCoastTowers();
                LogInfo("Coastal towers created");
            }
            catch (System.Exception e)
            {
                LogInfo("Coastal towers failed: " + e);
            }

            try { CampGuard.Install(harmony); } catch (System.Exception e) { LogInfo("Camp guard failed: " + e); }

            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(BuildingAsset), nameof(BuildingAsset.loadBuildingSpriteList)),
                    postfix: new HarmonyMethod(typeof(WallBuilder), nameof(WallBuilder.SpriteListPostfix)));
            }
            catch (System.Exception e)
            {
                LogInfo("Sprite patch failed: " + e);
            }

            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(City), "addCapturePoints", new System.Type[] { typeof(Kingdom), typeof(int) }),
                    prefix: new HarmonyMethod(typeof(WallBuilder), nameof(WallBuilder.CapturePrefix)));
            }
            catch (System.Exception e)
            {
                LogInfo("Capture patch failed: " + e);
            }

            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(CityZoneAbandon), "abandonSmallAreas"),
                    prefix: new HarmonyMethod(typeof(WallBuilder), nameof(WallBuilder.AbandonSmallAreasPrefix)));
            }
            catch (System.Exception e)
            {
                LogInfo("Abandon patch failed: " + e);
            }

            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(CityZoneAbandon), "abandonLeftoverZones"),
                    prefix: new HarmonyMethod(typeof(WallBuilder), nameof(WallBuilder.AbandonLeftoverPrefix)));
            }
            catch (System.Exception e)
            {
                LogInfo("Abandon-leftover patch failed: " + e);
            }

            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(MusicBox), "playSound", new System.Type[] { typeof(string), typeof(float), typeof(float), typeof(bool), typeof(bool) }),
                    prefix: new HarmonyMethod(typeof(Sfx), nameof(Sfx.PlaySoundPrefix)));
            }
            catch (System.Exception e)
            {
                LogInfo("impact sound patch failed: " + e);
            }

            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(Building), "startDestroyBuilding"),
                    prefix: new HarmonyMethod(typeof(WallBuilder), nameof(WallBuilder.DestroyPrefix)));
            }
            catch (System.Exception e)
            {
                LogInfo("destroy effect patch failed: " + e);
            }

            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(MapBox), "Update"),
                    postfix: new HarmonyMethod(typeof(ModClass), nameof(ModClass.HeartbeatPostfix)));
            }
            catch (System.Exception e)
            {
                LogInfo("heartbeat patch failed: " + e);
            }

            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(MapBox), "checkCityZone"),
                    prefix: new HarmonyMethod(typeof(WallBuilder), nameof(WallBuilder.CheckCityZonePrefix)));
            }
            catch (System.Exception e)
            {
                LogInfo("checkCityZone patch failed: " + e);
            }

            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(CityBehBuild), "isGoodTileForBuilding"),
                    prefix: new HarmonyMethod(typeof(WallBuilder), nameof(WallBuilder.GateFrontPrefix)));
            }
            catch (System.Exception e)
            {
                LogInfo("gate front patch failed: " + e);
            }

            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(City), "removeZone"),
                    prefix: new HarmonyMethod(typeof(WallBuilder), nameof(WallBuilder.RemoveZonePrefix)));
            }
            catch (System.Exception e)
            {
                LogInfo("removeZone diagnostic patch failed: " + e);
            }

            try
            {
                Siege.CreateAssets();
                harmony.Patch(
                    AccessTools.Method(typeof(BuildingManager), "precalculateRenderDataParallel"),
                    prefix: new HarmonyMethod(typeof(Siege), nameof(Siege.RenderGuardPrefix)),
                    finalizer: new HarmonyMethod(typeof(Siege), nameof(Siege.RenderGuardFinalizer)));
                harmony.Patch(
                    AccessTools.Method(typeof(City), nameof(City.joinAnotherKingdom)),
                    prefix: new HarmonyMethod(typeof(Siege), nameof(Siege.JoinPrefix)),
                    postfix: new HarmonyMethod(typeof(Siege), nameof(Siege.JoinPostfix)));
                harmony.Patch(
                    AccessTools.Method(typeof(Actor), "setAttackTarget"),
                    prefix: new HarmonyMethod(typeof(Ram), nameof(Ram.NoTargetPrefix)));
                harmony.Patch(
                    AccessTools.Method(typeof(Actor), "checkCurrentEnemyTarget"),
                    prefix: new HarmonyMethod(typeof(Ram), nameof(Ram.EnemyCheckPrefix)),
                    finalizer: new HarmonyMethod(typeof(Ram), nameof(Ram.EnemyCheckFinalizer)));
                harmony.Patch(
                    AccessTools.Method(typeof(Actor), "updateStats"),
                    prefix: new HarmonyMethod(typeof(Ram), nameof(Ram.StatsPre)),
                    postfix: new HarmonyMethod(typeof(Ram), nameof(Ram.StatsPost)));
                harmony.Patch(
                    AccessTools.Method(typeof(BuildingTower), "findTarget"),
                    postfix: new HarmonyMethod(typeof(Siege), nameof(Siege.CatapultTargetPostfix)));
                harmony.Patch(
                    AccessTools.Method(typeof(BuildingTower), "projectileStarted"),
                    postfix: new HarmonyMethod(typeof(Siege), nameof(Siege.ShakePostfix)));
                harmony.Patch(
                    AccessTools.Method(typeof(BuildingTower), "resetTimeout"),
                    postfix: new HarmonyMethod(typeof(Siege), nameof(Siege.ReloadPostfix)));
                LogInfo("Siege assets created");
                Ram.Init();
                // армия остаётся у закрытого города, пока идёт обстрел (см. Siege.HoldFor)
                harmony.Patch(
                    AccessTools.Method(typeof(CityBehCheckAttackZone), "findTargetCity"),
                    postfix: new HarmonyMethod(typeof(Siege), nameof(Siege.FindTargetPostfix)));
                harmony.Patch(
                    AccessTools.Method(typeof(CityBehCheckAttackZone), "execute"),
                    prefix: new HarmonyMethod(typeof(Siege), nameof(Siege.HoldPrefix)),
                    postfix: new HarmonyMethod(typeof(Siege), nameof(Siege.HoldPostfix)));
            }
            catch (System.Exception e)
            {
                LogInfo("Siege setup failed: " + e);
            }

            try
            {
                // игра рисует на миникарте здание без королевства и падает с NullReferenceException (на работу не влияет, но консоль всплывает)
                System.Type iconType = AccessTools.TypeByName("BuildingMapIcon");
                if (iconType != null)
                    harmony.Patch(
                        AccessTools.Method(iconType, "getColor"),
                        prefix: new HarmonyMethod(typeof(WallBuilder), nameof(WallBuilder.MapIconPrefix)));
            }
            catch (System.Exception e)
            {
                LogInfo("Minimap icon patch failed: " + e);
            }

            try
            {
                Traits.Register();
                harmony.Patch(
                    AccessTools.Method(typeof(CultureManager), nameof(CultureManager.newCulture)),
                    postfix: new HarmonyMethod(typeof(Traits), nameof(Traits.NewCulturePostfix)));
                LogInfo("Culture traits registered");
            }
            catch (System.Exception e)
            {
                LogInfo("Culture traits failed: " + e);
            }

            LogInfo("Wall builder installed");
            LogInfo("Settings file: " + Cfg.Save());
        }
    }

    public enum PlanKind { Wall, Tower }

    public class PlanItem
    {
        public WorldTile tile;
        public PlanKind kind;
        public float order;
        public int ring;
        public int side;        // 0 низ, 1 верх, 2 лево, 3 право
        public bool detoured;   // для этой клетки обход уже строили (или это сама клетка обхода)
        public bool iron;       // косяк ворот: строится из железной стены, чтобы ворота были заметны
        public bool spur;       // выступ у ворот (наружу от кольца)
        public bool layer2;     // второй слой внутренней стены (черта «Двойная стена»)
    }

    // Стиль укреплений по расе
    public class WallStyle
    {
        public TopTileType base_tile;
        public string base_id;
        public bool base_uses_stone;
        public TopTileType upgrade_tile;   // null = не улучшается
        public bool moat;                  // строит ли ров (только столица)
        public string arch = "human";      // архитектура основателя
    }

    public class CityState
    {
        public float outer_integrity = 1f;
        public double next_integrity = -1;
        public WorldTile center;
        public int stage = -1;
        public List<List<PlanItem>> rings = new List<List<PlanItem>>();
        public List<int> ring_radius = new List<int>();
        public List<PlanItem> plan = new List<PlanItem>();
        public HashSet<WorldTile> plan_tiles = new HashSet<WorldTile>();
        public int ticks_since_tower = 1000;
        public int ticks_total;
        public int wall_credit;
        public int insert_at;
        public int upgrade_credit;
        public List<GateTile> gates = new List<GateTile>();
        public int gates_rings;
        public bool gates_closed;
        public bool patrol;             // уровень 1: заперты все ворота, кроме одной стороны (патруль у города)
        public int patrol_open_side = -1;
        public double last_level2;      // когда последний раз была тревога второго уровня (армия или осада)
        public WorldTile garrison_pos;  // где ожидается враг: к этой стороне встаёт гарнизон
        public bool spurs;              // у этого города стены с выступами у ворот
        public double closed_since;     // когда ворота закрылись
        public double pressure;         // давление врага на закрытые ворота (воинов у стены * секунды)
        public int near_count;          // сколько вражеских воинов стоит прямо у стен
        public float enemy_cx, enemy_cy;// где в среднем стоят вражеские воины
        public bool decaying;           // стены города разрушаются (заброшен или мал)
        public double low_since = -1;   // с какого момента жителей меньше порога
        public int last_enemy_count;
        public double next_block_log;
        public double last_threat;
        public double next_gate_check;
        public int plaza_variant = -1;
        public bool is_capital;
        public double next_diag;
        public Dictionary<long, float> wall_hp = new Dictionary<long, float>();   // прочность участков внешней стены
        public Dictionary<int, float> gate_hp = new Dictionary<int, float>();   // прочность сторон ворот: ключ кольцо*4+сторона
        public Building[] barricades = new Building[16];     // 0-7 баррикады (кольцо*4+сторона), 8-15 знамёна рядом с ними
        public double next_smoke;
        public double next_barricade;
        public double next_barricade_log;
        public WorldTile pending_center;
        public int pending_ticks;
        public int last_outer_lim = -1000;
        public double next_outer_log;                // когда в следующий раз писать в лог, почему нет внешнего кольца
        public double next_foreign_check;            // когда в следующий раз искать свои стены на чужой земле
        public int foreign_streak;                   // сколько проверок подряд нашлись стены на чужой земле
        public HashSet<WorldTile> known_blocked = new HashSet<WorldTile>();   // клетки плана, недоступные уже при составлении плана (не считаем их «новыми дырами»)
        public List<PlanItem> keep_items;            // стены обхода зданий, которые надо сохранить при перепланировке
        public double next_open_log;
        public List<WorldTile> breaches = new List<WorldTile>();     // места пробоев стены и ворот (сюда стягиваются защитники)
        public double next_defend, next_def_log;
        public int replan_rings;                     // сколько колец было до перепланировки (внешнее надо вернуть сразу)
        public List<WorldTile> prune_old;            // старые клетки стен, которые надо снести после перепланировки кольца
        public bool warned_nogate;
        public double next_siege_log;
        public double last_breach;
        public double next_inside_log;
        public int moat_r = -1;          // радиус рва (расстояние от центра до воды рва)
        public int shape;               // форма стен: 0 квадрат, 1 срезанные углы, 2 бастионы на углах
        public int nogate = -1;         // сторона (0 низ, 1 верх, 2 лево, 3 право), где у города нет ворот; -1 - ворота со всех сторон
        public int coast_scanned;       // сколько клеток просмотрено с последней находки (для лога)
        public double next_coast_log;
        public int coast_cur = -1;      // где остановился поиск места для береговой башни
        public int stake_cur = -1;      // и для береговых кольев
        public int plaza_seed;
        public Building[] decor = new Building[4];
        public int[] decor_builds = new int[4];
        public bool[] ring_news = new bool[4];
        public bool[] ring_seen_low = new bool[4];

        // решение "нужны ли городу укрепления"
        public bool fortified;
        public int fort_checked = -1000;

        // дороги к воротам
        public bool roads_planned;
        public List<WorldTile> road_tiles = new List<WorldTile>();

        // ремонт
        public HashSet<WorldTile> built_walls = new HashSet<WorldTile>();

        // площадь столицы
        public WorldTile plaza_center;
        public int plaza_planned;
        public List<WorldTile> plaza_queue = new List<WorldTile>();

        // ров
        public bool moat_planned;
        public List<WorldTile> moat_tiles = new List<WorldTile>();
        public HashSet<WorldTile> moat_set = new HashSet<WorldTile>();   // то же для быстрого поиска (не путать ров с морем)
        public int moat_set_n = -1;

        // двойная стена
        public bool layer2_planned;
        public bool layer2_ok;
    }

    public class StuckInfo
    {
        public WorldTile tile;
        public int count;
    }

    public static class WallBuilder
    {
        // ================= Настройки =================
        public static float TOWER_HEALTH = Cfg.Float("tower_health", 1000f, "хп обычной башни (в игре 3000)");      // хп башни (в игре 3000)

        private static readonly int[] CAPITAL_RADII = new int[] { 14, 35 };   // радиусы колец столицы
        private const int OTHER_RADIUS = 20;                                  // радиус кольца обычных городов
        // Население для старта колец: столица [внутреннее, внешнее], остальные города [внутреннее]
        private static readonly int[] CAPITAL_RING_POP = new int[] { 100, 180 };
        private const int OTHER_RING_POP = 70;
        // Население для улучшения стены
        private static readonly int[] CAPITAL_UPGRADE_POP = new int[] { 140, 250 };
        private const int OTHER_UPGRADE_POP = 110;
        // Во время войны пороги ниже, но не меньше WAR_MIN_POP
        private const float WAR_POP_FACTOR = 0.7f;
        private const int WAR_MIN_POP = 30;

        // Влияние башен на захват города (в игре каждая башня даёт защитнику +10 очков захвата)
        private static int CAPTURE_MIN_POP = Cfg.Int("capture_min_pop", 20, "меньше жителей - башни не защищают город от захвата");           // меньше жителей - башни не защищают от захвата
        private static int CAPTURE_TOWERS_BASE = Cfg.Int("capture_towers_base", 2, "сколько башен всегда считается при защите от захвата");        // сколько башен считается всегда...
        private static int CAPTURE_TOWERS_PER_POP = Cfg.Int("capture_towers_per_pop", 60, "плюс одна башня на столько жителей");    // ...плюс по одной на столько жителей
        private static float CAPTURE_CHEAP_WEIGHT = Cfg.Float("capture_cheap_weight", 0.5f, "вес дешёвой башни при защите от захвата (1 = как обычная)");  // дешёвая башня считается за половину

        // Ремонт башен: руины убираются (место освобождается), повреждённые башни лечатся за камень
        private static bool REPAIR_TOWERS = Cfg.Bool("repair_towers", true, "ремонт и расчистка башен");
        private static float REPAIR_HEAL_PERCENT = Cfg.Float("repair_heal_percent", 0.1f, "доля хп башни, которую восстанавливает один ремонт");   // доля хп за один ремонт
        private const int REPAIR_STONE_RESERVE = 20;

        // Мощёная площадь в центре столицы (только люди и гномы)
        private static bool PLAZA_ENABLED = Cfg.Bool("plaza_enabled", true, "мощёная площадь в центре столицы");
        private static readonly int[] PLAZA_POP = new int[] { 100, 150, 250, 400 };
        private static readonly int[] PLAZA_RADII = new int[] { 4, 6, 9, 12 };
        private const int PLAZA_MARGIN = 3;       // не ближе стольких клеток к внутреннему кольцу стен
        private const int PLAZA_PER_TICK = 4;     // сколько клеток мостят за тик
        private const bool PLAZA_CLEAR = true;    // расчищать деревья и камни на площади

        // Дешёвая башня: только камень, одна стрела за выстрел, потом улучшается до обычной
        public const string CHEAP_TOWER_ID = "watch_tower_cheap";
        private static float CHEAP_FIRE_RATE = Cfg.Float("cheap_fire_rate", 1.5f, "во сколько раз чаще стреляет дешёвая башня");     // во сколько раз чаще стреляет дешёвая башня
        private static int CHEAP_STONE = Cfg.Int("cheap_stone", 12, "сколько камня стоит дешёвая башня");
        private static float CHEAP_HEALTH = Cfg.Float("cheap_health", 600f, "хп дешёвой башни");
        private static readonly int[] TOWER_STEPS = new int[] { 12, 24 };     // башня каждые N клеток
        private static readonly int[] TOWERS_BASE = new int[] { 1000, 2 };    // лимит башен = BASE + население / PER_POP (внутреннее кольцо: все позиции)
        private static readonly int[] TOWERS_PER_POP = new int[] { 8, 20 };

        private const int MIN_BUILDINGS = 15;
        private const float STAGE_DONE = 0.9f;
        private const float TOWER_AFTER_WALLS = 0.9f;

        private const int WOOD_RESERVE = 10;
        private const int STONE_BUILD_RESERVE = 20;   // запас камня для гномов (стена сразу каменная)
        private const int WALLS_PER_TICK = 5;
        private const int WALLS_PER_WOOD = 2;         // клеток стены за 1 ресурс
        private const int TOWER_COOLDOWN = 12;

        // Улучшение стены (человек, эльф): базовая -> каменная
        private static bool UPGRADE_WALLS = Cfg.Bool("upgrade_walls", true, "улучшение стен до каменных (люди, эльфы)");
        private const float UPGRADE_AFTER_WALLS = 0.95f;
        private const int STONE_RESERVE = 60;
        private const int UPGRADES_PER_TICK = 3;
        private const int UPGRADES_PER_STONE = 2;

        private static bool WALL_NEWS = Cfg.Bool("wall_news", true, "новости о постройке стен");
        private static bool GATES_ENABLED = Cfg.Bool("gates_enabled", true, "ворота закрываются, когда рядом вражеское войско или идёт осада");
        private static int GATE_THREAT_MIN = Cfg.Int("gate_enemies_min", 3, "сколько вражеских воинов рядом закрывают ворота");
        private static float GATE_OPEN_SECONDS = Cfg.Float("gate_open_seconds", 20f, "через сколько секунд после ухода врага ворота открываются");
        private static int GATE_RADIUS_EXTRA = Cfg.Int("gate_radius_extra", 25, "на сколько клеток дальше внешней стены ищем врага");
        private static int GATE_PER_TICK = Cfg.Int("gate_tiles_per_tick", 16, "сколько клеток ворот закрывается за один шаг города (чем больше, тем быстрее запираются ворота столицы)");
        private static float GATE_BREACH_SECONDS = Cfg.Float("gate_hold_seconds", 150f, "через сколько секунд осады (при 3+ воинах у стены) запертая сторона ворот поддаётся; у столиц вдвое дольше");
        private static float GATE_BREACH_PRESSURE = Cfg.Float("gate_hold_pressure", 1500f, "или набирают столько давления: каждые 2 секунды добавляется число воинов у стены (не больше gate_hold_crowd) * gate_hold_per_warrior; у столиц вдвое больше");
        private static int GATE_CROWD_CAP = Cfg.Int("gate_hold_crowd", 25, "сколько воинов у стены учитывается в давлении на ворота (толпа больше не ускоряет)");
        private static float GATE_PER_WARRIOR = Cfg.Float("gate_hold_per_warrior", 1.15f, "давление на ворота от одного воина у стены за 2 секунды");
        private static float GATE_FORCE_SECONDS = Cfg.Float("gate_force_seconds", 3f, "сколько секунд юниты могут стоять в проёме ворот, прежде чем ворота закроют принудительно");
        private static float GATE_BREACH_GAP = Cfg.Float("gate_breach_gap", 40f, "минимум секунд между двумя выбитыми сторонами ворот");
        private static bool WALL_DECAY = Cfg.Bool("wall_decay", true, "стены заброшенных и малых городов постепенно разрушаются");
        private static int DECAY_POP = Cfg.Int("wall_decay_pop", 25, "город, где жителей меньше, теряет стены");
        private static float DECAY_SECONDS = Cfg.Float("wall_decay_seconds", 360f, "за сколько секунд игрового времени исчезают все стены такого города");
        private static float DECAY_DELAY = Cfg.Float("wall_decay_delay", 60f, "сколько секунд город должен оставаться малым, прежде чем стены начнут рушиться");
        private static int SPUR_PERCENT = Cfg.Int("wall_spur_percent", 50, "у скольких процентов городов стены с выступами у ворот (0-100)");
        private const int SPUR_LEN = 2;
        private static int WALL_GAP = Cfg.Int("wall_gap", 4, "минимальный просвет между стенами соседних городов (клеток)");
        private static int WALL_MIN_RADIUS = Cfg.Int("wall_min_radius", 10, "если для стены остаётся меньший радиус, город стену не строит");
        private const int CAPITAL_MIN_RADIUS = 8;
        private const int CENTER_DRIFT = 20;        // на столько клеток центр города может сместиться без перестройки стен
        private static bool WALL_SWEEP = Cfg.Bool("wall_sweep", true, "убирать «бесхозные» стены (без города, с малым городом, от старых центров), в том числе из прошлых игр");
        private const int SWEEP_PER_TICK = 4000;
        private static int _sweep_idx;
        private static double _sweep_ready = -1;
        private static double _next_sweep_log;
        private static readonly Dictionary<WorldTile, double> _decay = new Dictionary<WorldTile, double>();
        private static double _next_decay;
        private static readonly System.Random _decay_rnd = new System.Random();
        private static bool PLAZA_RANDOM = Cfg.Bool("plaza_random_pattern", true, "случайный узор площади для каждой столицы (иначе кирпичики)");
        private static bool CITY_DECOR = Cfg.Bool("city_decor_v2", false, "знамёна (у орков бивни) по углам площади на последнем уровне");
        private const bool WALL_OVER_ROADS = true;
        private const double PRUNE_SECONDS = 40.0;   // за сколько секунд исчезают лишние стены после перепланировки (быстро, чтобы старый и новый контур не торчали вместе)
        private static bool WALL_REPLAN = Cfg.Bool("wall_replan", true, "перепланировать стены, если границы сдвинулись и стены оказались на чужой земле (экспериментально)");
        private static bool BORDER_BUFFER_ON = Cfg.Bool("wall_border_buffer", false, "оставлять 2 клетки запаса от чужих королевств на ничейной земле (экспериментально)");
        private const int FOREIGN_REPLAN_MIN = 8;   // с скольких стен на чужой земле перепланируем контур (меньше - сносим только эти клетки)
        private static bool AVOID_MOUNTAINS = Cfg.Bool("wall_avoid_mountains", true, "не строить стены, башни, ров и дороги на горах");
        // Гора: блокирующий тип клетки (горы и вершины)
        internal static bool OnMountain(WorldTile t) { return t != null && (t.Type.block || t.Type.mountains); }

        // Нужны ли городу укрепления: не нужны, если рядом (в пределах FORT_NEAR_DIST клеток)
        // есть столица своего королевства или другой укреплённый город своего королевства.
        private const bool FORT_BY_NEIGHBORS = true;
        private static int FORT_NEAR_DIST = Cfg.Int("fort_near_dist", 70, "рядом со столицей/укреплённым городом своего королевства стены не строятся (клеток)");
        private const int FORT_RECHECK = 30;          // как часто пересматривать решение (такты)

        // Война / опасность
        private static int WAR_WALL_MULT = Cfg.Int("war_wall_mult", 3, "во сколько раз быстрее строятся стены во время войны");          // во сколько раз быстрее стены
        private const float WAR_TOWER_AFTER_WALLS = 0.5f;

        // Дороги к воротам
        private const bool ROADS_TO_GATES = true;
        private const float ROADS_AFTER_WALLS = 0.6f;
        private const int ROADS_PER_TICK = 6;
        private const int ROAD_SEARCH_LIMIT = 9000;   // максимум клеток поиска пути

        // Ров вокруг внутреннего кольца столицы (люди, гномы, эльфы)
        private static bool MOAT_ENABLED = Cfg.Bool("moat_enabled", true, "ров вокруг внутренней стены столицы");
        private const int MOAT_OFFSET = 2;            // на сколько клеток дальше стены
        private static int MOAT_BRIDGE_STEP = Cfg.Int("moat_bridge_step", 8, "через сколько клеток во рву сухой проход шириной 2 (чтобы вода не делила карту для поиска пути); 0 - сплошной ров");
        private const int MOAT_PER_TICK = 4;
        private const float MOAT_AFTER_WALLS = 0.95f;

        // Борьба с застрявшими юнитами
        private const int UNSTUCK_EVERY = 3;
        private const int STUCK_CHECKS = 3;
        // =============================================

        private static readonly Dictionary<City, CityState> _states = new Dictionary<City, CityState>();
        private static readonly Dictionary<Actor, StuckInfo> _stuck = new Dictionary<Actor, StuckInfo>();

        // Радиус внешней стены города (0, если города с укреплениями нет)
        internal static int OuterRadius(City c)
        {
            CityState st;
            if (c == null || !_states.TryGetValue(c, out st)) return 0;
            int m = 0;
            for (int i = 0; i < st.ring_radius.Count; i++) if (st.ring_radius[i] > m) m = st.ring_radius[i];
            return m;
        }

        internal static void UnstuckAround(WorldTile center, int chunkRadius)
        {
            try { Unstuck(center, chunkRadius); } catch (System.Exception) { }
        }

        // Укреплён ли город настолько, чтобы его стоило осаждать лагерем
        internal static bool HasWalls(City c)
        {
            CityState st;
            return c != null && _states.TryGetValue(c, out st) && (st.rings.Count > 0 || st.built_walls.Count > 0);
        }

        // Клетка снаружи внешней стены города со стороны "from": если она на одном острове с "from", город отрезан от армии только нашими стенами
        // (закрытые ворота, ров, двойная стена), а не морем. Игра в таком случае считает город недостижимым и армия стоит дома
        internal static WorldTile OutsideTile(City target, WorldTile from)
        {
            CityState st;
            if (target == null || from == null || !_states.TryGetValue(target, out st) || st.rings.Count == 0 || st.center == null || st.ring_radius.Count == 0) return null;
            int R = 0;
            foreach (int r in st.ring_radius) R = System.Math.Max(R, r);
            float dx = from.x - st.center.x, dy = from.y - st.center.y;
            float len = (float)System.Math.Sqrt(dx * dx + dy * dy);
            if (len < 1f) return null;
            dx /= len; dy /= len;
            for (int extra = R + 3; extra <= R + 16; extra += 2)
            {
                WorldTile t = World.world.GetTile(st.center.x + (int)System.Math.Round(dx * extra), st.center.y + (int)System.Math.Round(dy * extra));
                if (t == null || t.is_liquid || t.Type.wall || t.Type.block) continue;
                if (t.isSameIsland(from)) return t;
            }
            return null;
        }

        internal static bool SeparatedByWalls(City attacker, City target)
        {
            try
            {
                if (attacker == null || target == null || !IsFortified(target) || target.reachableFrom(attacker)) return false;
                return OutsideTile(target, attacker.getTile()) != null;
            }
            catch (System.Exception) { return false; }
        }

        internal static bool IsFortified(City c)
        {
            CityState st;
            if (c == null || !_states.TryGetValue(c, out st) || st.rings.Count == 0) return false;
            if (RingProgress(c, st.rings[0]) >= 0.5f) return true;
            return BuiltTowers(st.rings[0]) >= 3;
        }

        internal static void Log(string s)
        {
            UnityEngine.Debug.Log("[WallBuilder] " + s);
        }

        private static bool LOG_VERBOSE = Cfg.Bool("log_verbose", false, "писать в лог подробности работы мода (осады, лагеря, ворота, тараны, армии и т.д.); для обычной игры не нужно, в лог идут только ошибки");
        internal static void Dbg(string s)
        {
            if (LOG_VERBOSE) UnityEngine.Debug.Log("[WallBuilder] " + s);
        }

        public static void Postfix(City pCity)
        {
            long t0 = Perf.Begin();
            try { CheckWorldChanged(); DecayTick(); ArmyTick(); Siege.FleetTick(); Siege.BeachTick(); GlobalTick(); PostfixInner(pCity); }
            finally { Perf.End(t0); }
        }

        // Мир сменился (загрузили другое сохранение или создали новый мир в той же сессии игры):
        // всё, что мод помнил про старый мир (города, клетки, таймеры по игровому времени), больше не верно.
        // Стены, ворота и лагеря, уже стоящие на карте, подхватываются заново: кольца планируются снова,
        // закрытые железные ворота "усыновляются" (см. HandleGates), бесхозное убирают подметальщики.
        private static WorldTile _world_mark;
        private static double _world_time;
        private static double _world_loaded_at = -1000.0;   // когда мир загрузили: сразу после этого население ещё не досчитано

        private static void CheckWorldChanged()
        {
            WorldTile mark = World.world.GetTile(0, 0);
            double wt = World.world.getCurWorldTime();
            bool timeBack = _world_time > 0 && wt < _world_time - 0.5;   // загрузили более раннее сохранение
            _world_time = wt;
            if (mark == _world_mark && !timeBack) return;
            bool had = _world_mark != null;
            _world_mark = mark;
            _world_loaded_at = wt;
            if (!had) return;
            _states.Clear();
            _decay.Clear();
            _stuck.Clear();
            _caps.Clear();
            split_logged.Clear();
            _sweep_idx = 0;
            _sweep_ready = -1;
            _next_sweep_log = 0;
            _next_decay = 0;
            _next_army_check = 0;
            Siege.ResetAll();
            Dbg("world changed: mod state reset");
        }

        internal static string StateInfo()
        {
            return "walled cities " + _states.Count + ", sieges/camps " + Siege.CountInfo();
        }

        // Забываем погибшие города и юниты (иначе записи копятся всю игру)
        internal static void CleanDead()
        {
            try
            {
                List<City> dead = null;
                foreach (KeyValuePair<City, CityState> kv in _states)
                    if (kv.Key == null || !kv.Key.isAlive()) { if (dead == null) dead = new List<City>(); dead.Add(kv.Key); }
                if (dead != null)
                {
                    foreach (City c in dead)
                    {
                        if (c == null) continue;
                        // город погиб: его стены не остаются навсегда, а рушатся так же, как у заброшенного
                        CityState dst;
                        if (WALL_DECAY && _states.TryGetValue(c, out dst))
                        {
                            dst.decaying = true;
                            int n = ScheduleDecay(dst);
                            if (n > 0) Dbg("city fell, its walls are decaying: " + n + " tiles");
                        }
                    }
                    foreach (City c in dead) if (c != null) _states.Remove(c);
                }

                List<Actor> gone = null;
                foreach (KeyValuePair<Actor, StuckInfo> kv in _stuck)
                    if (kv.Key == null || !kv.Key.isAlive()) { if (gone == null) gone = new List<Actor>(); gone.Add(kv.Key); }
                if (gone != null) foreach (Actor a in gone) if (a != null) _stuck.Remove(a);

                Siege.CleanDead();
            }
            catch (System.Exception e) { Log("clean failed: " + e.Message); }
        }

        // Игра не вызывает buildTick у города, пока он в опасности или его захватывают (CityBehBuild.execute),
        // а именно тогда и нужны ворота и осадные тики. Поэтому ворота и осады обновляем и отдельно, раз в полторы секунды.
        private static double _next_global;

        internal static void GlobalTick()
        {
            double now = World.world.getCurWorldTime();
            if (now < _next_global) return;
            _next_global = now + 1.5;
            List<City> list = null;
            foreach (KeyValuePair<City, CityState> kv in _states)
            {
                City c = kv.Key;
                CityState st = kv.Value;
                if (c == null || !c.isAlive() || st.decaying || st.rings.Count == 0 || st.center == null) continue;
                if (list == null) list = new List<City>();
                list.Add(c);
            }
            if (list == null) return;
            foreach (City c in list)
            {
                CityState st;
                if (!_states.TryGetValue(c, out st)) continue;
                try
                {
                    if (GATE_DIAG && ticksNow(st) && Siege.UnderSiege(c))
                        Dbg("siege target " + c.name + ": gates enabled " + GATES_ENABLED + ", rings " + st.rings.Count + ", gate tiles " + st.gates.Count
                            + ", flag " + (st.gates_closed ? "closed" : "open") + ", danger " + c.isInDanger());
                    if (GATES_ENABLED) HandleGates(c, st, null);
                    if (c.isInDanger() || c.isGettingCaptured()) Siege.Tick(c);
                }
                catch (System.Exception e) { Dbg("global tick failed: " + e.Message); }
            }
        }

        private static bool ticksNow(CityState st)
        {
            double now = World.world.getCurWorldTime();
            if (now < st.next_siege_log) return false;
            st.next_siege_log = now + 20.0;
            return true;
        }

        private static void PostfixInner(City pCity)
        {
            try
            {
                if (pCity == null) return;
                Siege.Tick(pCity);
                bool capital = pCity.isCapitalCity();

                WorldTile center = pCity.getTile();
                if (center == null) return;

                CityState st;
                if (!_states.TryGetValue(pCity, out st))
                {
                    st = new CityState();
                    _states[pCity] = st;
                }
                st.is_capital = capital;
                if (capital && st.nogate >= 0 && !st.warned_nogate) { st.warned_nogate = true; Dbg("capital " + pCity.name + " has a side without gates (city became capital after its walls were planned)"); }
                st.ticks_since_tower++;
                st.ticks_total++;

                // Заброшенный или совсем маленький город больше не держит стены: они постепенно рушатся
                if (CheckDecay(pCity, st)) return;

                // Ворота обрабатываем до проверок населения: город, где почти всех убили, как раз и надо запереть
                if (GATE_DIAG && ticksNow(st) && Siege.UnderSiege(pCity))
                    Dbg("siege target " + pCity.name + ": gates enabled " + GATES_ENABLED + ", rings " + st.rings.Count + ", gate tiles " + st.gates.Count
                        + ", flag " + (st.gates_closed ? "closed" : "open") + ", next check in " + (int)(st.next_gate_check - World.world.getCurWorldTime()) + "s");
                if (GATES_ENABLED && st.rings.Count > 0) HandleGates(pCity, st, null);

                bool war = IsWar(pCity);

                int minPop = RingPop(capital, 0, war);
                int minBuildings = war ? MIN_BUILDINGS / 2 : MIN_BUILDINGS;
                if (pCity.status.population < minPop) return;
                if (pCity.buildings.Count < minBuildings) return;

                // Площадь столицы не зависит от стен
                if (PLAZA_ENABLED && capital) HandlePlaza(pCity, st, center);
                if (CITY_DECOR && PLAZA_ENABLED && capital && st.ticks_total % 5 == 0) HandleDecor(pCity, st, center);

                // Черта культуры «Стеностроители»: без неё новые стены, башни и ремонт не делаются
                if (!Traits.Allows(pCity, Traits.WALLS, true))
                {
                    if (st.rings.Count == 0) return;
                    st.layer2_ok = false;
                    WallStyle style0 = GetStyle(pCity);
                    if (st.ticks_total % 6 == 1) HandleCoast(pCity, st, style0, capital);
                    if (MOAT_ENABLED && capital && Traits.Allows(pCity, Traits.MOAT, style0.moat)) HandleMoat(pCity, st);
                    return;
                }

                // Нужны ли укрепления этому городу
                if (st.rings.Count == 0)
                {
                    if (st.ticks_total - st.fort_checked >= FORT_RECHECK)
                    {
                        st.fort_checked = st.ticks_total;
                        st.fortified = !FORT_BY_NEIGHBORS || FortRule(pCity, 0);
                        // слишком тесно рядом с чужими стенами: не строим, чтобы не получалась сетка
                        if (st.fortified && !capital && SpaceLimit(pCity, center) < WALL_MIN_RADIUS) st.fortified = false;
                    }
                    if (!st.fortified) return;
                }

                // Центр города чуть сместился (перестроили главное здание): стены остаются на старом месте,
                // иначе рядом вырастет второе кольцо. Дальний перенос - старые стены рушатся, строим заново.
                bool keepCenter = st.center != null && st.rings.Count > 0
                    && System.Math.Max(System.Math.Abs(st.center.x - center.x), System.Math.Abs(st.center.y - center.y)) <= CENTER_DRIFT;
                if (st.center != center && !keepCenter && st.center != null && st.rings.Count > 0)
                {
                    // центр могло «качнуть» (после загрузки сохранения): переносим стены, только если новый центр держится несколько проверок подряд
                    if (st.pending_center != center) { st.pending_center = center; st.pending_ticks = 0; }
                    st.pending_ticks++;
                    if (st.pending_ticks < CENTER_CONFIRM_TICKS) keepCenter = true;
                    else Dbg("city center of " + pCity.name + " moved: " + st.center.x + "," + st.center.y + " -> " + center.x + "," + center.y);
                }
                else { st.pending_center = null; st.pending_ticks = 0; }

                if (st.center != center && !keepCenter)
                {
                    if (st.center != null && WALL_DECAY)
                    {
                        int oldWalls = ScheduleDecay(st);
                        if (oldWalls > 0) Dbg("city center of " + pCity.name + " moved far: old walls decaying, " + oldWalls + " tiles");
                    }
                    st.center = center;
                    st.stage = -1;
                    st.rings.Clear();
                    st.ring_radius.Clear();
                    st.plan.Clear();
                    st.plan_tiles.Clear();
                    st.roads_planned = false;
                    st.road_tiles.Clear();
                    st.moat_planned = false; st.moat_r = -1;
                    st.moat_tiles.Clear();
                    st.layer2_planned = false;
                    st.built_walls.Clear();
                    st.ring_news = new bool[4];
                    st.ring_seen_low = new bool[4];
                    st.gates.Clear();
                    st.gates_rings = 0;
                    st.gates_closed = false;
                }

                // Границы сдвинулись, и часть наших стен оказалась на земле другого королевства:
                // перепланируем кольцо вдоль новой границы, лишние стены потом снесёт распад
                if (WALL_REPLAN && st.rings.Count > 0 && !st.decaying)
                {
                    double fnow = World.world.getCurWorldTime();
                    if (fnow >= st.next_foreign_check)
                    {
                        st.next_foreign_check = fnow + 20.0;
                        List<WorldTile> badTiles = new List<WorldTile>();
                        foreach (WorldTile bt in st.built_walls)
                            if (bt != null && Foreign(bt, pCity)) badTiles.Add(bt);
                        int bad = badTiles.Count;
                        // клетки плана, куда стену уже нельзя поставить (сосед подошёл): в контуре будет дыра
                        int gaps = 0;
                        foreach (PlanItem pi in st.plan)
                        {
                            if (pi.kind != PlanKind.Wall) continue;
                            WorldTile gt = pi.tile;
                            if (pi.detoured || gt.Type.wall || gt.is_liquid || OnMountain(gt)) continue;
                            if (st.known_blocked.Contains(gt)) continue;
                            if (Blocked(gt, pCity)) gaps++;
                        }
                        int need = bad + gaps;
                        // границы часто «дрожат»: реагируем, только если проблема держится несколько проверок подряд
                        if (need == 0) st.foreign_streak = 0; else st.foreign_streak++;

                        if (bad > 0 && bad < FOREIGN_REPLAN_MIN && st.foreign_streak >= 2)
                        {
                            // единичные клетки: сносим только их, весь контур не трогаем
                            foreach (WorldTile bt in badTiles)
                            {
                                st.built_walls.Remove(bt);
                                if (bt.Type.wall && !_decay.ContainsKey(bt)) _decay[bt] = fnow + _decay_rnd.NextDouble() * PRUNE_SECONDS;
                            }
                            st.foreign_streak = 0;
                            Dbg("walls of " + pCity.name + ": " + bad + " stray wall tiles on foreign land will be removed");
                        }
                        else if (need >= FOREIGN_REPLAN_MIN && st.foreign_streak >= 3)
                        {
                            st.foreign_streak = 0;
                            st.next_foreign_check = fnow + 300.0;   // не чаще раза в пять минут
                            Dbg("walls of " + pCity.name + ": " + bad + " tiles on foreign land + " + gaps + " blocked planned tiles, re-planning along the new border (old radius " + (st.ring_radius.Count > 0 ? st.ring_radius[0] : -1) + ")");
                            List<WorldTile> old = new List<WorldTile>(st.built_walls);
                            foreach (GateTile g in st.gates) if (g.tile != null) old.Add(g.tile);
                            st.prune_old = old;
                            st.replan_rings = st.rings.Count;
                            // обходы зданий уже построены: их не сносим и не теряем
                            st.keep_items = new List<PlanItem>();
                            foreach (PlanItem ki in st.plan)
                                if (ki.kind == PlanKind.Wall && ki.detoured && ki.tile.Type.wall) st.keep_items.Add(ki);
                            st.known_blocked.Clear();
                            st.stage = -1;
                            st.rings.Clear();
                            st.ring_radius.Clear();
                            st.plan.Clear();
                            st.plan_tiles.Clear();
                            st.roads_planned = false;
                            st.road_tiles.Clear();
                            st.moat_planned = false; st.moat_r = -1;
                            st.moat_tiles.Clear();
                            st.layer2_planned = false;
                            st.ring_news = new bool[4];
                            st.ring_seen_low = new bool[4];
                            st.gates.Clear();
                            st.gates_rings = 0;
                            st.gates_closed = false;
                        }
                    }
                }

                int maxRings = capital ? 2 : 1;

                if (st.stage == -1)
                {
                    int minR = capital ? CAPITAL_MIN_RADIUS : WALL_MIN_RADIUS;
                    int maxR = System.Math.Max(SpaceLimit(pCity, center), minR);
                    int baseR = System.Math.Min(capital ? CAPITAL_RADII[0] : OTHER_RADIUS, maxR);
                    long hid = pCity.getID();
                    st.spurs = (int)(((hid * 2654435761L) >> 11) & 0x7fffffff) % 100 < SPUR_PERCENT;
                    InitShape(pCity, st, capital);   // форма стен выбирается здесь: статус столицы к этому моменту уже известен
                    AddRing(st, 0, baseR, maxR, st.spurs, pCity);
                    st.stage = 0;
                    NoteBlocked(pCity, st);

                    // внешнее кольцо столицы после перепланировки строим сразу по новой границе,
                    // иначе его уцелевшие стены сносятся, а новые появляются нескоро
                    if (st.replan_rings >= 2 && maxRings >= 2)
                    {
                        int lim2 = SpaceLimit(pCity, center);
                        int prevR2 = st.ring_radius[0];
                        if (lim2 >= prevR2 + 8)
                        {
                            AddRing(st, 1, System.Math.Min(CAPITAL_RADII[1], lim2), lim2, st.spurs, pCity);
                            st.stage = 1;
                            NoteBlocked(pCity, st);
                        }
                    }
                    st.replan_rings = 0;

                    if (st.keep_items != null)
                    {
                        foreach (PlanItem ki in st.keep_items)
                        {
                            if (st.plan_tiles.Contains(ki.tile) || Blocked(ki.tile, pCity) || ki.ring >= st.rings.Count) continue;
                            st.plan_tiles.Add(ki.tile);
                            st.plan.Add(ki);
                            st.rings[ki.ring].Add(ki);
                        }
                        st.keep_items = null;
                    }

                    // старые стены, не попавшие в новый план, сносим (те, что совпали, остаются)
                    if (st.prune_old != null)
                    {
                        double pnow = World.world.getCurWorldTime();
                        int pruned = 0, keptOld = 0, farOld = 0, foreignOld = 0;
                        int rr = st.ring_radius.Count > 0 ? st.ring_radius[0] : 0;
                        foreach (WorldTile pt in st.prune_old)
                        {
                            if (pt == null) continue;
                            if (st.plan_tiles.Contains(pt)) { keptOld++; continue; }
                            st.built_walls.Remove(pt);
                            if (pt.Type.wall && !_decay.ContainsKey(pt))
                            {
                                _decay[pt] = pnow + _decay_rnd.NextDouble() * PRUNE_SECONDS;
                                pruned++;
                                if (Foreign(pt, pCity)) foreignOld++;
                                if (System.Math.Max(System.Math.Abs(pt.x - st.center.x), System.Math.Abs(pt.y - st.center.y)) > rr + 3) farOld++;
                            }
                        }
                        st.prune_old = null;
                        Dbg("re-plan of " + pCity.name + ": kept " + keptOld + ", " + pruned + " old wall tiles will be removed (" + foreignOld + " on foreign land, " + farOld + " far from the ring), planned " + st.plan_tiles.Count + ", new radius " + rr);
                    }
                }
                else if (st.stage + 1 < maxRings
                         && pCity.status.population >= RingPop(capital, st.stage + 1, war)
                         && RingProgress(pCity, st.rings[st.stage]) >= STAGE_DONE)
                {
                    // внешнее кольцо столицы подстраивается под соседей; если места нет - его не будет
                    int lim = SpaceLimit(pCity, st.center);
                    int prevR = st.ring_radius[st.stage];
                    if (lim >= prevR + 8)
                    {
                        AddRing(st, st.stage + 1, System.Math.Min(CAPITAL_RADII[st.stage + 1], lim), lim, st.spurs, pCity);
                        st.stage++;
                        NoteBlocked(pCity, st);
                    }
                    else
                    {
                        maxRings = st.stage + 1;
                        double dnow = World.world.getCurWorldTime();
                        if (dnow >= st.next_outer_log || lim != st.last_outer_lim)
                        {
                            st.next_outer_log = dnow + 1200.0;
                            st.last_outer_lim = lim;
                            Dbg("outer ring of " + pCity.name + " skipped: free radius " + lim + " < needed " + (prevR + 8) + " (limited by " + SpaceLimiter(pCity, st.center) + ")");
                        }
                    }
                }

                WallStyle style = GetStyle(pCity);

                st.layer2_ok = Traits.Allows(pCity, Traits.DOUBLE, false);
                if (st.layer2_ok && !st.layer2_planned && st.rings.Count > 0 && RingProgress(pCity, st.rings[0]) >= 0.9f) PlanLayer2(pCity, st);

                PlaceWalls(pCity, st, style, war);
                if (WALL_NEWS && st.ticks_total % 10 == 3) WallNews(pCity, st);
                PlaceTower(pCity, st, war);
                if (st.ticks_total % 6 == 1) HandleCoast(pCity, st, style, capital);
                if (st.ticks_total % 6 == 4 && Traits.Allows(pCity, Traits.COAST, true)) HandleStakes(pCity, st);
                if (UPGRADE_WALLS && style.upgrade_tile != null) UpgradeWalls(pCity, st, style);
                if (st.ticks_total % 10 == 0) UpgradeTowers(pCity, st);
                if (ROADS_TO_GATES) HandleRoads(pCity, st);
                if (REPAIR_TOWERS && st.ticks_total % 10 == 5) RepairTowers(pCity, st);
                if (MOAT_ENABLED && capital && Traits.Allows(pCity, Traits.MOAT, style.moat)) HandleMoat(pCity, st);

                if (st.ticks_total % UNSTUCK_EVERY == 0) Unstuck(center, capital ? 3 : 2);
            }
            catch (System.Exception e)
            {
                Log("EXCEPTION: " + e);
            }
        }

        // ---------- Пороги населения ----------

        private static int ApplyWar(int pop, bool war)
        {
            if (!war) return pop;
            int v = (int)(pop * WAR_POP_FACTOR);
            return v < WAR_MIN_POP ? WAR_MIN_POP : v;
        }

        private static int RingPop(bool capital, int ringIndex, bool war)
        {
            int pop = capital ? CAPITAL_RING_POP[ringIndex] : OTHER_RING_POP;
            return ApplyWar(pop, war);
        }

        private static int UpgradePop(bool capital, int ringIndex, bool war)
        {
            int pop = capital ? CAPITAL_UPGRADE_POP[ringIndex] : OTHER_UPGRADE_POP;
            return ApplyWar(pop, war);
        }

        // ---------- Дешёвая башня ----------

        // Башня по расе города: watch_tower_<архитектура>, а если такой нет - человеческая
        internal static BuildingAsset RealTower(City city)
        {
            string arch = "human";
            try
            {
                ActorAsset a = city.getFounderSpecies();
                if (a != null && !string.IsNullOrEmpty(a.architecture_id)) arch = a.architecture_id;
            }
            catch (System.Exception) { }
            BuildingAsset t = AssetManager.buildings.get("watch_tower_" + arch);
            return t != null ? t : AssetManager.buildings.get("watch_tower_human");
        }

        public static void CreateCheapTower()
        {
            BuildingAsset real = AssetManager.buildings.get("watch_tower_human");
            if (real == null) return;
            if (AssetManager.buildings.get(CHEAP_TOWER_ID) != null) return;

            BuildingAsset cheap = AssetManager.buildings.clone(CHEAP_TOWER_ID, "watch_tower_human");
            // Пока тот же внешний вид, что у обычной башни (позже можно нарисовать свой спрайт)
            cheap.sprite_path = string.IsNullOrEmpty(real.sprite_path) ? real.main_path + real.id : real.sprite_path;
            cheap.cost = new ConstructionCost(0, CHEAP_STONE, 0, 0);
            cheap.base_stats["health"] = CHEAP_HEALTH;
            cheap.tower_projectile_amount = 1;
            cheap.tower_projectile_reload = real.tower_projectile_reload / CHEAP_FIRE_RATE;
            cheap.can_be_upgraded = true;
            cheap.upgrade_to = "watch_tower_human";
            cheap.buildings.Clear();
            if (real.atlas_asset != null) cheap.atlas_asset = real.atlas_asset;
        }

        // Свой спрайт дешёвой башни: деревянная вышка, нарисованная в коде.
        // Размер 11x25, pivot 0.5/0.1, ppu 1, Point (как у оригинала).
        // Флаг нарисован служебными розовыми цветами: игра сама красит их в цвет королевства.
        static UnityEngine.Sprite[] cheap_sprites;

        // Три варианта башни (как варианты домов): обычная, без крыши, со щитом и длинным вымпелом.
        // Y = огонёк (ночью светится), F/f/n = цвет королевства.
        internal static readonly string[][] GRIDS_MAIN = new string[][] {
            new string[] {
            ".....PFFFF.",
            ".....PFFff.",
            ".....Pfnn..",
            ".....P.....",
            "...kkkkk...",
            "..LMMLMML..",
            ".LMmMMmMMmL",
            "dDDDDDDDDDd",
            "..Dd.Y.dD..",
            "..DLLLLLD..",
            "..DLMLMLD..",
            "..DMmMmMD..",
            "dLLLLLLLLLd",
            ".dDMDMDMDd.",
            "...Dm.mD...",
            "...DM.MD...",
            "...D.M.D...",
            "...DM.MD...",
            "..dDDmDDd..",
            "..Dm.M.mD..",
            "..D.M.M.D..",
            "..Dm.M.mD..",
            "..DmeSemD..",
            ".sSsEeEsSs.",
            "eEsSeEeSsEe"
            },
            new string[] {
            ".....PFFFF.",
            ".....PFFff.",
            ".....Pfnn..",
            ".....P.....",
            "..D..P..D..",
            "..D..P..D..",
            ".dDddPddDd.",
            "..D..Y..D..",
            "..Dd...dD..",
            "..DLLLLLD..",
            "..DLMLMLD..",
            "..DMmMmMD..",
            "dLLLLLLLLLd",
            ".dDMDMDMDd.",
            "...Dm.mD...",
            "...DM.MD...",
            "...D.M.D...",
            "...DM.MD...",
            "..dDDmDDd..",
            "..Dm.M.mD..",
            "..D.M.M.D..",
            "..Dm.M.mD..",
            "..DmeSemD..",
            ".sSsEeEsSs.",
            "eEsSeEeSsEe"
            },
            new string[] {
            ".....PFFFFF",
            ".....PFFFff",
            ".....Pfnn..",
            ".....P.....",
            "...kkkkk...",
            "..LMMLMML..",
            ".LMmMMmMMmL",
            "dDDDDDDDDDd",
            "..Dd.Y.dD..",
            "..DLkFkLD..",
            "..DLfFfLD..",
            "..DMmfmMD..",
            "dLLLLLLLLLd",
            ".dDMDMDMDd.",
            "...Dm.mD...",
            "...DM.MD...",
            "...D.M.D...",
            "...DM.MD...",
            "..dDDmDDd..",
            "..Dm.M.mD..",
            "..D.M.M.D..",
            "..Dm.M.mD..",
            "..DmeSemD..",
            ".sSsEeEsSs.",
            "eEsSeEeSsEe"
            }
        };
        internal static readonly string[] GRID_CONSTRUCTION = {
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "dLLLLLLLLLd",
            ".dDMDMDMDd.",
            "...Dm.mD...",
            "...DM.MD...",
            "...D.M.D...",
            "...DM.MD...",
            "..dDDmDDd..",
            "..Dm.M.mD..",
            "..D.M.M.D..",
            "..Dm.M.mD..",
            "..DmeSemD..",
            ".sSsEeEsSs.",
            "eEsSeEeSsEe"
        };
        internal static readonly string[] GRID_RUIN = {
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...D.......",
            "..Dm...M...",
            "..D.M.mD...",
            "..DmeSemD..",
            ".sSsEeEsSs.",
            "eEsSeEeSsEe"
        };

        static UnityEngine.Color32 C(int r, int g, int b) { return new UnityEngine.Color32((byte)r, (byte)g, (byte)b, 255); }

        // Палитра игры: свои постройки в стиле игровых (ограниченный набор насыщенных коричневых и серо-зелёных камней)
        internal static bool native_pal;
        static UnityEngine.Color32 NativeColor(char c, out bool found)
        {
            found = true;
            switch (c)
            {
                    case 'P': return C(75, 46, 27);
                    case 'D': return C(75, 46, 27);
                    case 'd': return C(42, 24, 13);
                    case 'm': return C(122, 76, 45);
                    case 'M': return C(160, 101, 59);
                    case 'L': return C(198, 125, 73);
                    case 'l': return C(242, 176, 81);
                    case 'r': return C(160, 101, 59);
                    case 'R': return C(198, 125, 73);
                    case 'k': return C(62, 58, 55);
                    case 's': return C(184, 194, 173);
                    case 't': return C(92, 93, 94);
                    case 'p': return C(214, 190, 150);   // песчаник (береговые башни), свет
                    case 'U': return C(170, 146, 104);
                    case 'X': return C(124, 104, 76);
                    case '0': return C(88, 72, 56);
                    case '@': return C(158, 168, 184);   // сланец (береговые башни), свет
                    case '#': return C(112, 122, 142);
                    case '$': return C(80, 90, 108);
                    case '%': return C(54, 60, 74);
                    case 'F': return C(255, 0, 255);
                    case 'f': return C(222, 0, 222);
                    case 'n': return C(167, 0, 167);
                    case 'x': return C(127, 0, 127);
                    case 'z': return C(88, 0, 88);
                    case 'T': return C(211, 217, 197);
                    case 'I': return C(184, 194, 173);
                    case 'i': return C(134, 145, 123);
                    case 'J': return C(92, 93, 94);
                    case 'Q': return C(62, 58, 55);
                    case 'N': return C(92, 93, 94);
            }
            found = false;
            return new UnityEngine.Color32(0, 0, 0, 0);
        }

        static UnityEngine.Color32 GridColor(char c)
        {
            if (native_pal) { bool nf; UnityEngine.Color32 nc = NativeColor(c, out nf); if (nf) return nc; }
            switch (c)
            {
                case 'P': return C(58, 42, 30);      // древко
                case 'D': return C(75, 58, 47);      // тёмное дерево
                case 'd': return C(52, 40, 34);      // самое тёмное
                case 'm': return C(104, 66, 36);     // дерево, тень
                case 'M': return C(138, 90, 46);     // дерево, середина
                case 'L': return C(192, 138, 78);    // дерево, свет
                case 'l': return C(222, 170, 104);   // дерево, блик
                case 'r': return C(150, 96, 52);     // крыша, тень
                case 'R': return C(190, 126, 70);    // крыша
                case 'k': return C(52, 52, 60);      // конёк крыши
                case 's': return C(160, 158, 150);   // камень, свет
                case 'S': return C(122, 120, 118);   // камень, тень
                case 'e': return C(122, 90, 58);     // земля, свет
                case 'E': return C(92, 68, 44);      // земля, тень
                case 'F': return C(255, 0, 255);     // цвет королевства 0
                case 'f': return C(222, 0, 222);     // цвет королевства 1
                case 'n': return C(167, 0, 167);     // цвет королевства 2
                case 'Y': return C(255, 216, 0);     // огонь (игра сама делает из него ночной свет)
                case 'y': return C(255, 232, 120);   // пламя, ядро (не жёлтый #FFD800: игра днём делает его чёрным)
                case 'W': return C(110, 74, 40);     // колесо, тёмное
                case 'w': return C(150, 104, 60);    // колесо, светлое
                case 'c': return C(214, 196, 150);   // верёвка, светлая ткань
                case 'C': return C(170, 150, 108);   // верёвка, тень
                case 'b': return C(234, 226, 200);   // кость, свет
                case 'B': return C(196, 184, 150);   // кость, середина
                case 'u': return C(140, 128, 100);   // кость, тень
                case 't': return C(98, 92, 86);      // камень, самый тёмный
                case 'q': return C(170, 52, 28);     // угли
                case 'O': return C(226, 98, 32);     // пламя, тёмное
                case 'o': return C(250, 160, 44);    // пламя, светлое
                case 'Q': return C(46, 42, 40);      // зола
                case 'h': return C(236, 200, 90);    // сено, свет
                case 'H': return C(196, 156, 58);    // сено, середина
                case 'j': return C(150, 112, 40);    // сено, тень
                case 'x': return C(127, 0, 127);     // цвет королевства 3
                case 'z': return C(88, 0, 88);       // цвет королевства 4
                case 'A': return C(238, 238, 232);   // побелка, свет
                case 'a': return C(206, 208, 208);   // побелка, середина
                case 'G': return C(150, 154, 162);   // побелка, тень
                case 'V': return C(196, 56, 48);     // красная полоса
                case 'v': return C(146, 40, 38);     // красная полоса, тень
                case 'K': return C(120, 176, 206);   // стекло фонаря
                case 'I': return C(236, 230, 212);   // светлый камень
                case 'i': return C(208, 200, 176);   // светлый камень, середина
                case 'J': return C(172, 164, 142);   // светлый камень, тень
                case 'T': return C(222, 214, 190);   // светлый камень, блик
                case 'g': return C(52, 98, 50);      // прибрежные заросли, тёмные
                case 'Z': return C(96, 150, 70);     // прибрежные заросли, светлые
                case 'N': return C(120, 112, 98);    // цоколь, тёмный камень
                case '1': return C(142, 192, 98);    // флаг тревоги: зелёный, свет
                case '2': return C(86, 142, 62);
                case '3': return C(48, 92, 46);
                case '4': return C(250, 220, 112);   // жёлтый
                case '5': return C(216, 172, 58);
                case '6': return C(156, 112, 36);
                case '7': return C(218, 84, 66);     // красный
                case '8': return C(170, 46, 42);
                case '9': return C(108, 28, 32);
                default: return new UnityEngine.Color32(0, 0, 0, 0);
            }
        }

        internal static UnityEngine.Sprite FromGrid(string[] grid, string name)
        {
            int h = grid.Length, w = grid[0].Length;
            UnityEngine.Texture2D tex = new UnityEngine.Texture2D(w, h, UnityEngine.TextureFormat.RGBA32, false);
            tex.filterMode = UnityEngine.FilterMode.Point;
            tex.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            UnityEngine.Color32[] px = new UnityEngine.Color32[w * h];
            for (int row = 0; row < h; row++)
            {
                int y = h - 1 - row; // первая строка сетки - верх спрайта
                for (int x = 0; x < w; x++)
                    px[y * w + x] = GridColor(grid[row][x]);
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            UnityEngine.Sprite sp = UnityEngine.Sprite.Create(tex, new UnityEngine.Rect(0, 0, w, h),
                new UnityEngine.Vector2(0.5f, 0.1f), 1f);
            sp.name = name;
            return sp;
        }

        // Какие постройки мода рисуются в палитре игры (палатки, орудия и лагерный скарб остаются как были)
        static bool NativeArtId(string id)
        {
            if (id == null) return false;
            return id == CHEAP_TOWER_ID || Coast.IsCoastId(id) || Fx.IsFxId(id) || id == Siege.FLAG_ID || id == Siege.TUSK_ID || id == Siege.TUSK2_ID || id == Siege.TUSK3_ID
                || id == Siege.BANNER_ID || id == Siege.BANNER_S_ID || id == Siege.TOWER_ID || id == Siege.BARRICADE_ID || id == Siege.BARRICADE_L_ID || id == Siege.BARRICADE_R_ID || id == Siege.STAKE_ID || id == Siege.STAKE_V_ID;
        }

        public static void SpriteListPostfix(BuildingAsset __instance, ref UnityEngine.Sprite[] __result)
        {
            if (__instance == null) return;
            native_pal = NativeArtId(__instance.id);
            if (Siege.IsSiegeId(__instance.id))
            {
                try { __result = Siege.GetSprites(__instance.id); }
                catch (System.Exception e) { UnityEngine.Debug.Log("[Mymod] siege sprite failed: " + e); }
                return;
            }
            if (Coast.IsCoastId(__instance.id))
            {
                try { __result = Coast.GetSprites(__instance.id); }
                catch (System.Exception e) { UnityEngine.Debug.Log("[Mymod] coast sprite failed: " + e); }
                return;
            }
            if (__instance.id != CHEAP_TOWER_ID) return;
            try
            {
                if (cheap_sprites == null)
                {
                    // mini_0 (значок на карте) берём у обычной башни
                    UnityEngine.Sprite mini = null;
                    BuildingAsset real = AssetManager.buildings.get("watch_tower_human");
                    foreach (UnityEngine.Sprite sp in real.loadBuildingSpriteList())
                        if (sp.name.StartsWith("mini")) mini = sp;

                    List<UnityEngine.Sprite> res = new List<UnityEngine.Sprite>();
                    for (int v = 0; v < GRIDS_MAIN.Length; v++)
                    {
                        res.Add(FromGrid(GRID_CONSTRUCTION, "construction_" + v));
                        res.Add(FromGrid(GRIDS_MAIN[v], "main_" + v));
                        if (mini != null)
                        {
                            UnityEngine.Sprite m = UnityEngine.Sprite.Create(mini.texture, mini.rect,
                                new UnityEngine.Vector2(mini.pivot.x / mini.rect.width, mini.pivot.y / mini.rect.height), mini.pixelsPerUnit);
                            m.name = "mini_" + v;
                            res.Add(m);
                        }
                        res.Add(FromGrid(GRID_RUIN, "ruin_" + v));
                    }
                    cheap_sprites = res.ToArray();
                }
                __result = cheap_sprites;
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.Log("[Mymod] cheap sprite failed: " + e);
            }
        }

        // Дешёвые башни по возможности улучшаем до обычных (когда хватает ресурсов и камня)
        private static void UpgradeTowers(City city, CityState st)
        {
            BuildingAsset real = RealTower(city);
            if (real == null || !city.hasEnoughResourcesFor(real.cost)) return;
            if (city.amount_stone <= STONE_RESERVE) return;
            BuildingAsset cheapAsset = AssetManager.buildings.get(CHEAP_TOWER_ID);
            if (cheapAsset != null) cheapAsset.upgrade_to = real.id;      // улучшаем до башни своей расы

            foreach (PlanItem it in st.plan)
            {
                if (it.kind != PlanKind.Tower) continue;
                WorldTile t = it.tile;
                if (!t.hasBuilding()) continue;
                Building b = t.building;
                if (b.asset.id != CHEAP_TOWER_ID || b.isUnderConstruction()) continue;
                if (CityBehBuild.upgradeBuilding(b, city))
                {
                    Dbg("tower upgraded at " + t.x + "," + t.y);
                    return;
                }
            }
        }

        // ---------- Разрушение стен заброшенных городов ----------

        // Планируем снос всех стен города: у каждой клетки случайный момент в пределах DECAY_SECONDS
        private static int ScheduleDecay(CityState st)
        {
            double now = World.world.getCurWorldTime();
            int n = 0;
            foreach (WorldTile t in st.plan_tiles)
                if (t != null && t.Type.wall && !_decay.ContainsKey(t)) { _decay[t] = now + _decay_rnd.NextDouble() * DECAY_SECONDS; n++; }
            foreach (WorldTile t in st.built_walls)
                if (t != null && t.Type.wall && !_decay.ContainsKey(t)) { _decay[t] = now + _decay_rnd.NextDouble() * DECAY_SECONDS; n++; }
            foreach (GateTile g in st.gates)
                if (g.tile != null && g.tile.Type.wall && !_decay.ContainsKey(g.tile)) { _decay[g.tile] = now + _decay_rnd.NextDouble() * DECAY_SECONDS; n++; }
            return n;
        }

        private static void CancelDecay(CityState st)
        {
            foreach (WorldTile t in st.plan_tiles) _decay.Remove(t);
            foreach (WorldTile t in st.built_walls) _decay.Remove(t);
            foreach (GateTile g in st.gates) if (g.tile != null) _decay.Remove(g.tile);
        }

        // true - город сейчас разрушает стены, строить ничего не нужно
        private static bool CheckDecay(City city, CityState st)
        {
            if (!WALL_DECAY) return false;
            if (st.built_walls.Count == 0 && !st.decaying) { st.low_since = -1; return false; }
            double now = World.world.getCurWorldTime();
            int pop = city.status.population;

            if (st.decaying)
            {
                if (pop < DECAY_POP + 10) return true;
                // город отстроился: стены больше не рушим, он сам их достроит
                st.decaying = false;
                st.low_since = -1;
                CancelDecay(st);
                st.built_walls.Clear();
                st.gates_closed = false;
                foreach (GateTile g in st.gates) if (g.closed && (g.tile == null || !g.tile.Type.wall)) g.closed = false;
                Dbg("wall decay cancelled at " + city.name + " (pop " + pop + ")");
                return false;
            }

            // под осадой или недавно под угрозой город стен не теряет, даже если почти всех убили
            // сразу после загрузки население городов может быть занижено: стены в это время не рушим
            if (now - _world_loaded_at < 120.0) { st.low_since = -1; return false; }
            if (pop >= DECAY_POP || Siege.UnderSiege(city) || now - st.last_threat < 180.0) { st.low_since = -1; return false; }
            if (st.low_since < 0) { st.low_since = now; return false; }
            if (now - st.low_since < DECAY_DELAY) return false;

            st.decaying = true;
            int n = ScheduleDecay(st);
            Dbg("walls of " + city.name + " are decaying (pop " + pop + "): " + n + " tiles over " + (int)DECAY_SECONDS + " s");
            return true;
        }

        private static bool ProtectedByLiving(WorldTile t)
        {
            foreach (KeyValuePair<City, CityState> kv in _states)
            {
                CityState s = kv.Value;
                if (s.decaying || kv.Key == null || !kv.Key.isAlive()) continue;
                if (s.plan_tiles.Contains(t) && !Foreign(t, kv.Key)) return true;   // чужая земля стену города не защищает
            }
            return false;
        }

        // Раз в игровую секунду сносим клетки, у которых подошёл срок
        private static bool IsOurWallTop(TopTileType tt)
        {
            if (tt == null) return false;
            string id = tt.id;
            return id == "wall_wild" || id == "wall_order" || id == "wall_green" || id == "wall_iron";
        }

        // Стена без хозяина: нет живого города с планом на эту клетку, земля ничья либо город погиб или слишком мал
        private static bool IsOrphanWall(WorldTile t)
        {
            if (ProtectedByLiving(t) || Siege.IsCampTile(t)) return false;
            City c = t.zone != null ? t.zone.city : null;
            if (c == null || !c.isAlive()) return true;
            return c.status.population < DECAY_POP;
        }

        // Медленно (по кусочку карты за секунду) ищем бесхозные стены. Так уходят и стены, о которых мод не помнит:
        // из прошлых игр, после сохранений и после переноса центра города.
        private static void SweepWalls(double now)
        {
            if (_sweep_ready < 0) _sweep_ready = now + 60.0;     // сначала пусть города заново распланируют кольца
            if (now < _sweep_ready) return;
            int total = MapBox.width * MapBox.height;
            if (total <= 0) return;
            int found = 0;
            for (int k = 0; k < SWEEP_PER_TICK; k++)
            {
                int idx = _sweep_idx++;
                if (_sweep_idx >= total) _sweep_idx = 0;
                WorldTile t = World.world.GetTile(idx % MapBox.width, idx / MapBox.width);
                if (t == null || !IsOurWallTop(t.top_type)) continue;
                if (_decay.ContainsKey(t) || !IsOrphanWall(t)) continue;
                _decay[t] = now + _decay_rnd.NextDouble() * DECAY_SECONDS;
                found++;
            }
            if (found > 0 && now >= _next_sweep_log)
            {
                _next_sweep_log = now + 30.0;
                Dbg("orphan walls found: " + found + " (scheduled to decay over " + (int)DECAY_SECONDS + " s)");
            }
        }

        internal static void DecayTick()
        {
            if (!WALL_DECAY) return;
            double now = World.world.getCurWorldTime();
            if (now < _next_decay) return;
            _next_decay = now + 1.0;
            if (WALL_SWEEP) SweepWalls(now);
            if (_decay.Count == 0) return;

            List<WorldTile> due = null;
            foreach (KeyValuePair<WorldTile, double> kv in _decay)
                if (kv.Value <= now) { if (due == null) due = new List<WorldTile>(); due.Add(kv.Key); }
            if (due == null) return;

            int removed = 0;
            foreach (WorldTile t in due)
            {
                _decay.Remove(t);
                if (t == null || !t.Type.wall || ProtectedByLiving(t)) continue;
                // на месте стены снова трава (если под ней была обычная земля), а не голая коричневая полоса
                TopTileType after = null;
                try
                {
                    string mt = t.main_type != null ? t.main_type.id : "";
                    if (mt == "soil_low") after = TopTileLibrary.grass_low;
                    else if (mt == "soil_high") after = TopTileLibrary.grass_high;
                }
                catch (System.Exception) { }
                MapAction.terraformTop(t, after);
                removed++;
            }
            if (_decay.Count == 0) Dbg("wall decay finished (last batch: " + removed + " tiles)");
        }

        // ---------- Стиль по расе ----------

        private static WallStyle GetStyle(City city)
        {
            string arch = "human";
            try
            {
                ActorAsset a = city.getFounderSpecies();
                if (a != null && !string.IsNullOrEmpty(a.architecture_id)) arch = a.architecture_id;
            }
            catch (System.Exception) { }

            WallStyle s = new WallStyle();
            s.moat = true;
            s.arch = arch;
            if (arch == "orc")
            {
                s.base_tile = TopTileLibrary.wall_wild;
                s.base_id = "wall_wild";
                s.moat = false;                       // орки: только дерево, без рва
            }
            else if (arch == "dwarf")
            {
                s.base_tile = TopTileLibrary.wall_order;   // гномы сразу из камня
                s.base_id = "wall_order";
                s.base_uses_stone = true;
            }
            else if (arch == "elf")
            {
                s.base_tile = TopTileLibrary.wall_green;   // живая изгородь, потом камень
                s.base_id = "wall_green";
                s.upgrade_tile = TopTileLibrary.wall_order;
            }
            else
            {
                s.base_tile = TopTileLibrary.wall_wild;
                s.base_id = "wall_wild";
                s.upgrade_tile = TopTileLibrary.wall_order;
            }
            return s;
        }

        // ---------- Война и соседи ----------

        private static bool IsWar(City city)
        {
            if (city.isInDanger()) return true;
            return city.kingdom != null && city.kingdom.hasEnemies();
        }

        // Нужны ли укрепления: нет, если рядом столица своего королевства или укреплённый (по этому же правилу)
        // город своего королевства с меньшим id. Так решение стабильно и не зависит от порядка загрузки.
        private static bool FortRule(City city, int depth)
        {
            if (city.isCapitalCity()) return true;
            WorldTile ct = city.getTile();
            if (ct == null || city.kingdom == null) return true;

            foreach (City o in World.world.cities)
            {
                if (o == city || !o.isAlive() || o.kingdom != city.kingdom) continue;
                WorldTile ot = o.getTile();
                if (ot == null) continue;
                int dx = ot.x - ct.x;
                int dy = ot.y - ct.y;
                if (dx * dx + dy * dy > FORT_NEAR_DIST * FORT_NEAR_DIST) continue;

                if (o.isCapitalCity()) return false;
                if (depth < 6 && o.status.population >= OTHER_RING_POP
                    && o.data.id < city.data.id && FortRule(o, depth + 1)) return false;
            }
            return true;
        }

        // Клетка внутри кольца другого города того же королевства: эта грань лишняя,
        // иначе при пересечении колец получаются пустые прямоугольники.
        private static bool InsideOtherRing(WorldTile t, City city)
        {
            if (city.kingdom == null) return false;
            foreach (KeyValuePair<City, CityState> kv in _states)
            {
                if (kv.Key == city) continue;
                CityState s = kv.Value;
                if (s.center == null || s.ring_radius.Count == 0) continue;
                if (!kv.Key.isAlive() || kv.Key.kingdom != city.kingdom) continue;

                int dx = t.x - s.center.x;
                if (dx < 0) dx = -dx;
                int dy = t.y - s.center.y;
                if (dy < 0) dy = -dy;
                for (int i = 0; i < s.ring_radius.Count; i++)
                {
                    int R = s.ring_radius[i];
                    if (dx < R && dy < R) return true;
                }
            }
            return false;
        }

        // ---------- Планирование колец ----------

        // Запоминаем клетки плана, которые недоступны уже сейчас: они не считаются новыми дырами при проверке границ
        private static void NoteBlocked(City city, CityState st)
        {
            foreach (PlanItem pi in st.plan)
            {
                if (pi.kind != PlanKind.Wall) continue;
                WorldTile t = pi.tile;
                if (t.Type.wall || t.is_liquid) continue;
                if (OnMountain(t) || Blocked(t, city)) st.known_blocked.Add(t);
            }
        }

        private static void AddRing(CityState st, int ringIndex, int baseRadius, int maxRadius, bool spurs, City city)
        {
            int radius = ChooseRadius(st.center, baseRadius, maxRadius);
            // после загрузки сохранения стены уже стоят: берём их радиус, а не строим новое кольцо внутри старого
            List<int> found = DetectRings(st.center, maxRadius);
            int adopt = -1;
            foreach (int fr in found)
            {
                if (ringIndex > 0 && st.ring_radius.Count > 0 && fr <= st.ring_radius[st.ring_radius.Count - 1] + 4) continue;
                if (System.Math.Abs(fr - baseRadius) > 9 || fr > maxRadius) continue;
                if (adopt < 0 || System.Math.Abs(fr - baseRadius) < System.Math.Abs(adopt - baseRadius)) adopt = fr;
            }
            string fs = "";
            foreach (int fr in found) fs += fr + " ";
            if (adopt > 0 && adopt != radius)
            {
                Dbg("ring " + ringIndex + " of " + (city != null ? city.name : "?") + ": adopted existing walls, radius " + adopt + " (chosen " + radius + ", found: " + fs + ")");
                radius = adopt;
            }
            else if (found.Count > 0) Dbg("ring " + ringIndex + " of " + (city != null ? city.name : "?") + ": radius " + radius + " (found existing: " + fs + ")");
            List<PlanItem> items = BuildRing(st.center, radius, TOWER_STEPS[ringIndex], ringIndex, spurs, city);
            st.rings.Add(items);
            st.ring_radius.Add(radius);
            foreach (PlanItem it in items)
            {
                st.plan.Add(it);
                if (it.kind == PlanKind.Wall) st.plan_tiles.Add(it.tile);
            }
            Dbg("ring " + ringIndex + ": radius=" + radius + ", items=" + items.Count);
        }

        // Радиусы колец, которые уже стоят вокруг центра (стены есть минимум с трёх сторон и занимают не меньше 28% клеток)
        private static List<int> DetectRings(WorldTile c, int maxR)
        {
            List<int> res = new List<int>();
            int top = System.Math.Min(maxR + 6, 60);
            int bestR = -1;
            float bestFrac = 0f;
            for (int R = 8; R <= top + 1; R++)
            {
                float frac = 0f;
                if (R <= top)
                {
                    int total = 0, walls = 0;
                    int okSides = 0;
                    for (int side = 0; side < 4; side++)
                    {
                        int sideTotal = 0, sideWalls = 0;
                        int from = (side < 2) ? -R : -R + 1;
                        int to = (side < 2) ? R : R - 1;
                        for (int sq = from; sq <= to; sq++)
                        {
                            int x, y;
                            RingPoint(c, R, side, sq, out x, out y);
                            WorldTile t = World.world.GetTile(x, y);
                            if (t == null) continue;
                            total++;
                            sideTotal++;
                            if (t.Type.wall) { walls++; sideWalls++; }
                        }
                        if (sideTotal > 0 && sideWalls >= sideTotal * 0.12f) okSides++;
                    }
                    // недостроенное кольцо тоже считается, если стены есть минимум с трёх сторон (чужая прямая стена даёт одну сторону)
                    if (total > 0 && okSides >= 3) frac = (float)walls / total;
                }
                if (frac >= 0.28f)
                {
                    if (frac > bestFrac) { bestFrac = frac; bestR = R; }
                }
                else if (bestR > 0)
                {
                    res.Add(bestR);
                    bestR = -1;
                    bestFrac = 0f;
                }
            }
            return res;
        }

        private static void RingPoint(WorldTile c, int R, int side, int s, out int x, out int y)
        {
            if (side == 0) { x = c.x + s; y = c.y - R; }
            else if (side == 1) { x = c.x + s; y = c.y + R; }
            else if (side == 2) { x = c.x - R; y = c.y + s; }
            else { x = c.x + R; y = c.y + s; }
        }

        // ---------- Форма стен ----------

        private static bool WALL_SHAPES = Cfg.Bool("wall_shapes", true, "разнообразить форму стен: срезанные углы, бастионы, города без одних ворот");
        private static int CHAMFER_PCT = Cfg.Int("wall_chamfer_percent", 30, "у скольких процентов городов (не столиц) углы стен срезаны");
        private static int NOGATE_PCT = Cfg.Int("wall_nogate_percent", 50, "у скольких процентов городов (не столиц) с одной из сторон нет ворот");

        // Форма города зависит только от его номера, поэтому после загрузки сохранения план стен тот же
        private static void InitShape(City city, CityState st, bool capital)
        {
            st.shape = 0;
            st.nogate = -1;
            if (!WALL_SHAPES || capital || city == null) return;
            long id = city.getID();
            int h = (int)(((id * 2654435761L) >> 5) & 0x7fffffff);
            int r = h % 100;
            if (r < CHAMFER_PCT) st.shape = 1;
            
            if ((h / 100) % 100 < NOGATE_PCT) st.nogate = (h / 10000) % 4;
        }

        private static void AddShapeWall(List<PlanItem> items, HashSet<WorldTile> used, WorldTile t, int ringIndex, int side)
        {
            if (t == null || !used.Add(t)) return;
            PlanItem w = new PlanItem();
            w.tile = t;
            w.kind = PlanKind.Wall;
            w.ring = ringIndex;
            w.side = side;
            items.Add(w);
        }

        private static void AddShapeTower(List<PlanItem> items, HashSet<WorldTile> used, WorldTile t, int ringIndex, int side)
        {
            if (t == null || !used.Add(t)) return;
            PlanItem w = new PlanItem();
            w.tile = t;
            w.kind = PlanKind.Tower;
            w.ring = ringIndex;
            w.side = side;
            items.Add(w);
        }

        // Углы кольца: срез (диагональная лесенка толщиной в две клетки, без щелей) или бастион (выступ с башней внутри).
        // Если угол задевает чужую землю или воду, он остаётся прямым.
        private static void ApplyShape(List<PlanItem> items, HashSet<WorldTile> used, WorldTile c, int R, int ringIndex, City city, int shape)
        {
            bool chamfer = shape == 1 && ringIndex == 0 && R >= 10;
            bool bastion = ringIndex >= 1 && R >= 14;   // бастионы только на внешних кольцах столиц
            if (!chamfer && !bastion) return;

            int[] sxs = new int[] { -1, 1, -1, 1 };
            int[] sys = new int[] { -1, -1, 1, 1 };
            for (int ci = 0; ci < 4; ci++)
            {
                int sx = sxs[ci], sy = sys[ci];
                int cx0 = c.x + sx * R, cy0 = c.y + sy * R;
                int side = sy < 0 ? 0 : 1;

                if (chamfer)
                {
                    int k = R >= 14 ? 3 : 2;
                    bool ok = true;
                    for (int a = 0; a <= k + 1 && ok; a++)
                        for (int b = 0; b <= k + 1; b++)
                        {
                            WorldTile t = World.world.GetTile(cx0 - sx * a, cy0 - sy * b);
                            if (t == null || Blocked(t, city)) { ok = false; break; }
                        }
                    if (!ok) continue;
                    int cxx = cx0, cyy = cy0, ssx = sx, ssy = sy, kk = k;
                    items.RemoveAll(delegate (PlanItem it)
                    {
                        int dxl = (it.tile.x - cxx) * (-ssx);
                        int dyl = (it.tile.y - cyy) * (-ssy);
                        if (dxl < 0 || dyl < 0 || dxl + dyl > kk + 1) return false;
                        used.Remove(it.tile);
                        return true;
                    });
                    for (int a = 0; a <= k + 1; a++)
                        for (int b = 0; b <= k + 1; b++)
                        {
                            int sum = a + b;
                            if (sum != k && sum != k + 1) continue;
                            AddShapeWall(items, used, World.world.GetTile(cx0 - sx * a, cy0 - sy * b), ringIndex, side);
                        }
                    int tw = (k + 3) / 2;
                    AddShapeTower(items, used, World.world.GetTile(cx0 - sx * tw, cy0 - sy * tw), ringIndex, side);
                }
                else
                {
                    int B = (ringIndex >= 1 && R >= 22) ? 4 : 3;
                    bool ok = true;
                    for (int u = 0; u < B && ok; u++)
                        for (int v = 0; v < B; v++)
                        {
                            WorldTile t = World.world.GetTile(cx0 + sx * u, cy0 + sy * v);
                            if (t == null || t.is_liquid || Blocked(t, city)) { ok = false; break; }
                        }
                    if (!ok) continue;
                    for (int u = 0; u < B; u++)
                        for (int v = 0; v < B; v++)
                        {
                            if (u != 0 && u != B - 1 && v != 0 && v != B - 1) continue;
                            AddShapeWall(items, used, World.world.GetTile(cx0 + sx * u, cy0 + sy * v), ringIndex, side);
                        }
                    AddShapeTower(items, used, World.world.GetTile(cx0 + sx, cy0 + sy), ringIndex, side);
                }
            }
        }

        internal static bool IsCiv(WorldTile t)
        {
            return t.hasBuilding() && t.building.asset.city_building;
        }

        private static int ScoreRing(WorldTile c, int R)
        {
            int score = 0;
            for (int side = 0; side < 4; side++)
            {
                int from = (side < 2) ? -R : -R + 1;
                int to = (side < 2) ? R : R - 1;
                for (int s = from; s <= to; s++)
                {
                    int x, y;
                    RingPoint(c, R, side, s, out x, out y);
                    WorldTile t = World.world.GetTile(x, y);
                    if (t == null) continue;
                    if (t.Type.wall) score += 4;
                    else if (IsCiv(t)) score -= 8;
                    else if (t.Type.road) score -= 1;
                    else if (t.is_liquid) score -= 1;
                }
            }
            return score;
        }

        // Максимальный радиус кольца, при котором стена не подползает к стенам соседних городов ближе WALL_GAP клеток
        private static int SpaceLimit(City city, WorldTile c)
        {
            int lim = int.MaxValue;
            foreach (KeyValuePair<City, CityState> kv in _states)
            {
                if (kv.Key == city || kv.Key == null || !kv.Key.isAlive()) continue;
                CityState s = kv.Value;
                if (s.center == null || s.ring_radius.Count == 0 || s.decaying) continue;
                int dx = System.Math.Abs(c.x - s.center.x);
                int dy = System.Math.Abs(c.y - s.center.y);
                int outer = 0;
                for (int i = 0; i < s.ring_radius.Count; i++) if (s.ring_radius[i] > outer) outer = s.ring_radius[i];
                int allow = System.Math.Max(dx, dy) - outer - SPUR_LEN - WALL_GAP;
                if (allow < lim) lim = allow;
            }
            return lim;
        }

        // Какой соседний город сильнее всего ограничивает радиус (для лога)
        private static string SpaceLimiter(City city, WorldTile c)
        {
            int lim = int.MaxValue;
            string who = "nobody";
            foreach (KeyValuePair<City, CityState> kv in _states)
            {
                if (kv.Key == city || kv.Key == null || !kv.Key.isAlive()) continue;
                CityState s = kv.Value;
                if (s.center == null || s.ring_radius.Count == 0 || s.decaying) continue;
                int dx = System.Math.Abs(c.x - s.center.x);
                int dy = System.Math.Abs(c.y - s.center.y);
                int outer = 0;
                for (int i = 0; i < s.ring_radius.Count; i++) if (s.ring_radius[i] > outer) outer = s.ring_radius[i];
                int allow = System.Math.Max(dx, dy) - outer - SPUR_LEN - WALL_GAP;
                if (allow < lim)
                {
                    lim = allow;
                    who = kv.Key.name + " (distance " + System.Math.Max(dx, dy) + ", its ring " + outer + (kv.Key.kingdom == city.kingdom ? ", same kingdom" : ", other kingdom") + ")";
                }
            }
            return who;
        }

        private static int ChooseRadius(WorldTile c, int baseR, int maxR)
        {
            int[] offsets = new int[] { 0, -1, 1, -2, 2, -3, 3, -4, 4, -5, 5, -6, 6 };
            int best = baseR;
            int bestScore = int.MinValue;
            foreach (int o in offsets)
            {
                int r = baseR + o;
                if (r < 6 || r > maxR) continue;
                int sc = ScoreRing(c, r) - System.Math.Abs(o);
                if (sc > bestScore)
                {
                    bestScore = sc;
                    best = r;
                }
            }
            return best;
        }

        // Клетка чужой земли: зона принадлежит городу другого королевства
        private static bool Foreign(WorldTile t, City city)
        {
            if (t == null || t.zone == null || city == null) return false;
            City o = t.zone.city;
            if (o == city) return false;
            if (o == null) return false;   // ничейная земля: строить можно
            return o.kingdom == null || o.kingdom != city.kingdom;
        }

        private const int BORDER_BUFFER = 2;   // запас от чужих королевств при стройке на ничейной земле (клеток)

        // Рядом (в пределах BORDER_BUFFER клеток) есть земля другого королевства
        private static bool NearForeign(WorldTile t, City city)
        {
            if (!BORDER_BUFFER_ON) return false;
            int B = BORDER_BUFFER;
            int[] dx = new int[] { -B, B, 0, 0, -B, B, -B, B };
            int[] dy = new int[] { 0, 0, -B, B, -B, -B, B, B };
            for (int i = 0; i < 8; i++)
            {
                WorldTile n = World.world.GetTile(t.x + dx[i], t.y + dy[i]);
                if (n != null && n.zone != null && n.zone.city != null && Foreign(n, city)) return true;
            }
            return false;
        }

        // Нельзя ли ставить сюда стену по плану: чужая земля, а на ничейной земле ещё и запас у чужой границы.
        // Земля своего города и уже стоящие стены запаса не требуют: когда границы устоялись, стена идёт вплотную.
        private static bool Blocked(WorldTile t, City city)
        {
            if (Foreign(t, city)) return true;
            if (t == null || t.zone == null || t.zone.city != null || t.Type.wall) return false;
            return NearForeign(t, city);
        }

        // На сколько клеток внутрь от кольца надо отступить, чтобы выйти из чужой зоны (-1 - некуда)
        private static int InwardOffset(WorldTile c, int R, int side, int s, City city)
        {
            for (int k = 1; k <= R - 3; k++)
            {
                int x, y;
                RingPoint(c, R - k, side, s, out x, out y);
                WorldTile t = World.world.GetTile(x, y);
                if (t == null || t.is_liquid) continue;
                if (!Blocked(t, city)) return k;
            }
            return -1;
        }

        // Столбик стены поперёк кольца: клетки со смещением внутрь от lo до hi (чужие клетки пропускаем)
        private static void AddWallColumn(List<PlanItem> items, HashSet<WorldTile> used, WorldTile c, int R, int side, int s, int lo, int hi, int ringIndex, City city)
        {
            for (int k = lo; k <= hi; k++)
            {
                if (k >= R - 2) break;
                int x, y;
                RingPoint(c, R - k, side, s, out x, out y);
                WorldTile t = World.world.GetTile(x, y);
                if (t == null || Blocked(t, city) || !used.Add(t)) continue;
                PlanItem w = new PlanItem();
                w.tile = t;
                w.kind = PlanKind.Wall;
                w.ring = ringIndex;
                w.side = side;
                items.Add(w);
            }
        }

        private static List<PlanItem> BuildRing(WorldTile c, int R, int towerStep, int ringIndex, bool spurs, City city)
        {
            List<PlanItem> items = new List<PlanItem>();
            HashSet<WorldTile> used = new HashSet<WorldTile>();
            CityState cst;
            int nogate = -1;
            int shape = 0;
            if (city != null && _states.TryGetValue(city, out cst)) { nogate = cst.nogate; shape = cst.shape; }
            for (int side = 0; side < 4; side++)
            {
                int from = (side < 2) ? -R : -R + 1;
                int to = (side < 2) ? R : R - 1;
                int prevOff = 0;
                bool gated = side != nogate;     // на стороне без ворот проход заложен обычной стеной
                for (int s = from; s <= to; s++)
                {
                    if (gated && (s == 0 || s == 1)) { prevOff = 0; continue; } // ворота: проход шириной 2

                    int x, y;
                    RingPoint(c, R, side, s, out x, out y);
                    WorldTile t = World.world.GetTile(x, y);
                    if (t == null) continue;

                    // кольцо заходит на землю соседа: стена идёт вдоль его границы, чуть глубже внутрь, а не обрывается
                    int off = 0;
                    if (Blocked(t, city))
                    {
                        off = InwardOffset(c, R, side, s, city);
                        if (off < 0) { prevOff = 0; continue; }
                    }
                    if (off != prevOff)
                    {
                        int lo = System.Math.Min(off, prevOff), hi = System.Math.Max(off, prevOff);
                        if (s - 1 != 0 && s - 1 != 1 && s - 1 >= from)
                            AddWallColumn(items, used, c, R, side, s - 1, prevOff == 0 ? 1 : lo, hi, ringIndex, city);
                        AddWallColumn(items, used, c, R, side, s, off == 0 ? 1 : lo, hi, ringIndex, city);
                    }
                    else if (off > 0) AddWallColumn(items, used, c, R, side, s, off, off, ringIndex, city);
                    prevOff = off;
                    if (off > 0) continue;

                    PlanItem wall = new PlanItem();
                    wall.tile = t;
                    wall.kind = PlanKind.Wall;
                    wall.ring = ringIndex;
                    wall.side = side;
                    if (gated && (s == -1 || s == 2)) wall.iron = true;   // косяки ворот из железа
                    if (used.Add(t)) items.Add(wall);

                    // выступы у ворот: два коротких крыла наружу, между ними проход шириной 2
                    if (spurs && gated && (s == -1 || s == 2))
                    {
                        for (int k = 1; k <= SPUR_LEN; k++)
                        {
                            int sx = x, sy = y;
                            if (side == 0) sy = y - k;
                            else if (side == 1) sy = y + k;
                            else if (side == 2) sx = x - k;
                            else sx = x + k;
                            WorldTile spt = World.world.GetTile(sx, sy);
                            if (spt == null || Blocked(spt, city) || !used.Add(spt)) continue;
                            PlanItem sp = new PlanItem();
                            sp.tile = spt;
                            sp.kind = PlanKind.Wall;
                            sp.ring = ringIndex;
                            sp.side = side;
                            sp.spur = true;
                            if (k == SPUR_LEN) sp.iron = true;   // торец выступа тоже железный
                            items.Add(sp);
                        }
                    }

                    bool wantTower = false;
                    if (gated && (s == -1 || s == 2)) wantTower = true;
                    else if (side < 2 && (s == -R || s == R)) wantTower = true;
                    else if (s % towerStep == 0) wantTower = true;
                    if (!wantTower) continue;

                    int ix = x, iy = y;
                    if (side == 0) iy = y + 1;
                    else if (side == 1) iy = y - 1;
                    else if (side == 2) ix = x + 1;
                    else ix = x - 1;
                    if (side < 2 && s == -R) ix = x + 1;
                    if (side < 2 && s == R) ix = x - 1;
                    // у башни площадь 3x2 клетки: у косяков ворот сдвигаем её вдоль стены, чтобы она не накрывала клетки проёма
                    // (иначе в проёме навсегда остаётся постройка, и ворота не закрываются)
                    if (gated && (s == -1 || s == 2))
                    {
                        if (side < 2) ix = (s == -1) ? x - 1 : x + 1;
                        else if (s == -1) iy = y - 1;
                    }

                    WorldTile it2 = World.world.GetTile(ix, iy);
                    if (it2 == null) continue;
                    PlanItem tower = new PlanItem();
                    tower.tile = it2;
                    tower.kind = PlanKind.Tower;
                    tower.ring = ringIndex;
                    tower.side = side;
                    items.Add(tower);
                }
            }

            ApplyShape(items, used, c, R, ringIndex, city, shape);

            int[] gx = new int[] { c.x, c.x, c.x - R, c.x + R };
            int[] gy = new int[] { c.y - R, c.y + R, c.y, c.y };
            foreach (PlanItem it in items)
            {
                float best = float.MaxValue;
                for (int i = 0; i < 4; i++)
                {
                    float dx = it.tile.x - gx[i];
                    float dy = it.tile.y - gy[i];
                    float d = dx * dx + dy * dy;
                    if (d < best) best = d;
                }
                it.order = best;
            }
            items.Sort((a, b) => a.order.CompareTo(b.order));
            return items;
        }

        private static bool Allowed(WorldTile t, City city)
        {
            if (t.zone == null) return false;
            if (AVOID_MOUNTAINS && OnMountain(t)) return false;
            City owner = t.zone.city;
            if (owner == city) return true;
            if (owner == null) return !NearForeign(t, city);
            return owner.kingdom != null && owner.kingdom == city.kingdom;
        }

        private static float RingProgress(City city, List<PlanItem> ring)
        {
            int eligible = 0;
            int done = 0;
            foreach (PlanItem it in ring)
            {
                if (it.kind != PlanKind.Wall || it.layer2) continue;
                WorldTile t = it.tile;
                if (t.Type.wall) { eligible++; done++; continue; }
                if ((!WALL_OVER_ROADS && t.Type.road) || t.is_liquid || IsCiv(t) || !Allowed(t, city)) continue;
                if (InsideOtherRing(t, city)) continue;
                eligible++;
            }
            if (eligible == 0) return 1f;
            return (float)done / eligible;
        }

        // Новости: внутреннее кольцо замкнуто / внешнее достроено (только для колец, которые мы видели недостроенными)
        private static void WallNews(City city, CityState st)
        {
            for (int r = 0; r < st.rings.Count && r < st.ring_news.Length; r++)
            {
                if (st.ring_news[r]) continue;
                float p = RingProgress(city, st.rings[r]);
                if (p < 0.9f) st.ring_seen_low[r] = true;
                if (p >= 0.98f && st.ring_seen_low[r])
                {
                    st.ring_news[r] = true;
                    Dbg("ring " + r + " of " + city.name + " completed");
                    Siege.AnnounceWall(city, r > 0);
                }
            }
        }

        // ---------- Двойная стена (черта «Двойная стена») ----------

        private static void AddLayer2Tile(CityState st, PlanItem src, int x, int y, int side)
        {
            WorldTile t = World.world.GetTile(x, y);
            if (t == null || st.plan_tiles.Contains(t)) return;
            st.plan_tiles.Add(t);
            PlanItem n = new PlanItem();
            n.tile = t;
            n.kind = PlanKind.Wall;
            n.ring = 0;
            n.side = side;
            n.order = src != null ? src.order + 0.5f : 1e9f;
            n.layer2 = true;
            st.plan.Add(n);
            st.rings[0].Add(n);
        }

        // Второй слой внутренней стены: снаружи вплотную к первому, в промежутке между стеной и рвом. У ворот слой прерывается
        private static void PlanLayer2(City city, CityState st)
        {
            st.layer2_planned = true;
            if (st.rings.Count == 0 || st.ring_radius.Count == 0 || st.center == null) return;
            WorldTile c = st.center;
            int R = st.ring_radius[0];
            int before = st.plan.Count;
            List<PlanItem> src = new List<PlanItem>(st.rings[0]);
            foreach (PlanItem it in src)
            {
                if (it.kind != PlanKind.Wall || it.spur || it.detoured || it.layer2 || it.tile == null) continue;
                int dx = 0, dy = 0;
                if (it.side == 0) dy = -1; else if (it.side == 1) dy = 1; else if (it.side == 2) dx = -1; else dx = 1;
                int sOff = it.side < 2 ? it.tile.x - c.x : it.tile.y - c.y;
                if (it.side != st.nogate && sOff >= -4 && sOff <= 5) continue;   // проход у ворот и выступы не трогаем
                AddLayer2Tile(st, it, it.tile.x + dx, it.tile.y + dy, it.side);
            }
            if (st.shape == 0)
            {
                // углы квадрата, которые не покрыты смещением сторон
                for (int ox = -(R + 1); ox <= R + 1; ox++)
                    for (int oy = -(R + 1); oy <= R + 1; oy++)
                    {
                        if (System.Math.Max(System.Math.Abs(ox), System.Math.Abs(oy)) != R + 1) continue;
                        if (System.Math.Abs(ox) < R || System.Math.Abs(oy) < R) continue;
                        AddLayer2Tile(st, null, c.x + ox, c.y + oy, System.Math.Abs(ox) == R + 1 ? (ox < 0 ? 2 : 3) : (oy < 0 ? 0 : 1));
                    }
            }
            Dbg("double wall planned for " + city.name + ": " + (st.plan.Count - before) + " tiles");
        }

        // ---------- Обход зданий ----------

        private static void AddDetourTile(CityState st, PlanItem src, int x, int y)
        {
            WorldTile t = World.world.GetTile(x, y);
            if (t == null) return;
            if (st.plan_tiles.Contains(t)) return;
            st.plan_tiles.Add(t);

            PlanItem n = new PlanItem();
            n.tile = t;
            n.kind = PlanKind.Wall;
            n.ring = src.ring;
            n.side = src.side;
            n.order = src.order;
            n.detoured = true;
            st.plan.Insert(st.insert_at, n);
            st.insert_at++;
            st.rings[src.ring].Add(n);
        }

        private static void BuildDetour(CityState st, PlanItem it, Building b)
        {
            it.detoured = true;
            if (b.tiles.Count == 0) return;

            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
            foreach (WorldTile bt in b.tiles)
            {
                if (bt.x < x0) x0 = bt.x;
                if (bt.x > x1) x1 = bt.x;
                if (bt.y < y0) y0 = bt.y;
                if (bt.y > y1) y1 = bt.y;
            }

            int rx = it.tile.x, ry = it.tile.y;
            if (it.side < 2)
            {
                int yo = (it.side == 0) ? y0 - 1 : y1 + 1;
                int ya = System.Math.Min(yo, ry), yb = System.Math.Max(yo, ry);
                for (int y = ya; y <= yb; y++)
                {
                    AddDetourTile(st, it, x0 - 1, y);
                    AddDetourTile(st, it, x1 + 1, y);
                }
                for (int x = x0 - 1; x <= x1 + 1; x++) AddDetourTile(st, it, x, yo);
            }
            else
            {
                int xo = (it.side == 2) ? x0 - 1 : x1 + 1;
                int xa = System.Math.Min(xo, rx), xb = System.Math.Max(xo, rx);
                for (int x = xa; x <= xb; x++)
                {
                    AddDetourTile(st, it, x, y0 - 1);
                    AddDetourTile(st, it, x, y1 + 1);
                }
                for (int y = y0 - 1; y <= y1 + 1; y++) AddDetourTile(st, it, xo, y);
            }
        }

        // ---------- Строительство ----------

        private static void PlaceWalls(City city, CityState st, WallStyle style, bool war)
        {
            int perTick = war ? WALLS_PER_TICK * WAR_WALL_MULT : WALLS_PER_TICK;
            int placed = 0;
            for (int i = 0; i < st.plan.Count; i++)
            {
                if (placed >= perTick) break;
                PlanItem it = st.plan[i];
                if (it.kind != PlanKind.Wall) continue;

                WorldTile t = it.tile;
                if (t.Type.wall || t.is_liquid) continue;
                if (!WALL_OVER_ROADS && t.Type.road) continue;
                if (!Allowed(t, city)) continue;

                if (it.layer2 && !st.layer2_ok) continue;
                if (IsCiv(t))
                {
                    if (it.spur || it.layer2) continue;   // выступ упёрся в здание - просто пропускаем, обход не строим
                    if (!it.detoured)
                    {
                        st.insert_at = i + 1;
                        BuildDetour(st, it, t.building);
                    }
                    continue;
                }
                if (t.hasUnits()) continue;
                if (InsideOtherRing(t, city)) continue;

                if (st.wall_credit <= 0)
                {
                    if (style.base_uses_stone)
                    {
                        int reserve = war ? 0 : STONE_BUILD_RESERVE;
                        if (city.amount_stone <= reserve) break;
                        city.takeResource("stone", 1);
                    }
                    else
                    {
                        int reserve = war ? 0 : WOOD_RESERVE;
                        if (city.amount_wood <= reserve) break;
                        city.takeResource("wood", 1);
                    }
                    st.wall_credit = WALLS_PER_WOOD;
                }
                st.wall_credit--;

                if (!st.built_walls.Add(t)) Dbg("wall repaired at " + t.x + "," + t.y);
                MapAction.terraformTop(t, it.iron ? TopTileLibrary.wall_iron : style.base_tile);
                placed++;
            }
        }

        // Все ли башни внутреннего кольца достроены (позиции, где башню поставить нельзя, не считаются)
        private static bool InnerTowersDone(City city, CityState st)
        {
            if (st.rings.Count == 0) return false;
            foreach (PlanItem it in st.rings[0])
            {
                if (it.kind != PlanKind.Tower) continue;
                WorldTile t = it.tile;
                if (t.hasBuilding())
                {
                    Building b = t.building;
                    if (b.asset.tower && b.isUnderConstruction()) return false;
                    continue; // башня готова, либо место занято другим
                }
                if (t.is_liquid || t.Type.wall || t.Type.road || !Allowed(t, city) || InsideOtherRing(t, city)) continue;
                return false; // свободное место, где башню ещё можно построить
            }
            return true;
        }

        private static void UpgradeWalls(City city, CityState st, WallStyle style)
        {
            bool capital = city.isCapitalCity();
            bool war = IsWar(city);
            int done = 0;
            for (int r = 0; r < st.rings.Count; r++)
            {
                int needPop = UpgradePop(capital, r, war);
                List<PlanItem> ring = st.rings[r];
                float progress = RingProgress(city, ring);
                bool towersOk = r == 0 || InnerTowersDone(city, st);

                if (st.ticks_total % 40 == 0)
                    Dbg("upgrade check ring " + r + ": pop=" + city.status.population + "/" + needPop +
                        ", progress=" + progress.ToString("0.00") + ", stone=" + city.amount_stone + "/" + STONE_RESERVE +
                        ", inner towers done=" + towersOk);

                if (city.status.population < needPop) continue;
                if (progress < UPGRADE_AFTER_WALLS) continue;
                if (!towersOk) continue;

                for (int i = 0; i < ring.Count; i++)
                {
                    if (done >= UPGRADES_PER_TICK) return;
                    PlanItem it = ring[i];
                    if (it.kind != PlanKind.Wall) continue;
                    WorldTile t = it.tile;
                    if (t.Type.id != style.base_id) continue;

                    if (st.upgrade_credit <= 0)
                    {
                        if (city.amount_stone <= STONE_RESERVE) return;
                        city.takeResource("stone", 1);
                        st.upgrade_credit = UPGRADES_PER_STONE;
                    }
                    st.upgrade_credit--;

                    MapAction.terraformTop(t, style.upgrade_tile);
                    done++;
                }
            }
        }

        private static int BuiltTowers(List<PlanItem> ring)
        {
            int n = 0;
            foreach (PlanItem it in ring)
            {
                if (it.kind != PlanKind.Tower) continue;
                WorldTile t = it.tile;
                if (t.hasBuilding() && t.building.asset.tower) n++;
            }
            return n;
        }

        private static void PlaceTower(City city, CityState st, bool war)
        {
            if (city.under_construction_building != null) return;
            int cooldown = war ? TOWER_COOLDOWN / 2 : TOWER_COOLDOWN;
            if (st.ticks_since_tower < cooldown) return;

            // Обычная башня, если хватает ресурсов (металл и золото), иначе дешёвая (только камень)
            BuildingAsset real = RealTower(city);
            BuildingAsset cheap = AssetManager.buildings.get(CHEAP_TOWER_ID);
            BuildingAsset tower = null;
            if (real != null && city.hasEnoughResourcesFor(real.cost)) tower = real;
            else if (cheap != null && city.hasEnoughResourcesFor(cheap.cost)) tower = cheap;
            if (tower == null) return;

            float need = war ? WAR_TOWER_AFTER_WALLS : TOWER_AFTER_WALLS;

            for (int r = 0; r < st.rings.Count; r++)
            {
                List<PlanItem> ring = st.rings[r];
                if (RingProgress(city, ring) < need) continue;

                int maxTowers = TOWERS_BASE[r] + city.status.population / TOWERS_PER_POP[r];
                if (BuiltTowers(ring) >= maxTowers) continue;

                foreach (PlanItem it in ring)
                {
                    if (it.kind != PlanKind.Tower) continue;
                    WorldTile t = it.tile;
                    if (t.hasBuilding() || t.is_liquid || t.Type.wall || t.Type.road) continue;
                    if (!Allowed(t, city)) continue;
                    if (InsideOtherRing(t, city)) continue;

                    Building b = World.world.buildings.addBuilding(tower, t);
                    if (b == null) continue;
                    b.setUnderConstruction();
                    city.under_construction_building = b;
                    city.spendResourcesForBuildingAsset(tower.cost);
                    st.ticks_since_tower = 0;
                    Dbg("tower placed (" + tower.id + ", ring " + r + ") at " + t.x + "," + t.y);
                    return;
                }
            }
        }

        // ---------- Береговые башни и колья ----------

        private static bool COAST_TOWERS = Cfg.Bool("coast_towers", true, "береговые башни (маяк / форт) у воды в укреплённых городах");
        private static int COAST_STONE = Cfg.Int("coast_tower_stone", 20, "сколько камня (у орков дерева) стоит береговая башня");
        private static int COAST_MAX_CAPITAL = Cfg.Int("coast_towers_capital", 3, "береговых башен в столице (максимум)");
        private static int COAST_MAX_CITY = Cfg.Int("coast_towers_city", 1, "береговых башен в обычном городе (максимум)");
        private static bool COAST_STAKES = Cfg.Bool("coast_stakes", true, "небольшие колья-частокол на берегу у укреплённых городов (для вида)");
        private static int COAST_STAKE_PERCENT = Cfg.Int("coast_stake_percent", 14, "какой процент береговых клеток становится началом группы кольев");
        private static float COAST_DAMAGE = Cfg.Float("coast_tower_damage", 60f, "урон ядра береговой башни");
        private static float COAST_RELOAD = Cfg.Float("coast_tower_reload", 3.5f, "перезарядка береговой башни, секунды");
        private const int COAST_SCAN = 700;           // сколько клеток просматривается за один шаг поиска

        public static void CreateCoastTowers()
        {
            BuildingAsset real = AssetManager.buildings.get("watch_tower_human");
            if (real == null) return;
            for (int i = 0; i < Coast.IDS.Length; i++)
            {
                string id = Coast.IDS[i];
                if (AssetManager.buildings.get(id) != null) continue;
                BuildingAsset a = AssetManager.buildings.clone(id, "watch_tower_human");
                a.sprite_path = string.IsNullOrEmpty(real.sprite_path) ? real.main_path + real.id : real.sprite_path;
                a.cost = id == Coast.ORC_ID ? new ConstructionCost(COAST_STONE, 0, 0, 0) : new ConstructionCost(0, COAST_STONE, 0, 0);   // орочий форт деревянный
                a.base_stats["health"] = CHEAP_HEALTH;
                // береговая башня стреляет редко, но тяжело: одно ядро по кораблю (дальность - в Siege.CoastTarget)
                a.tower = true;
                a.tower_attack_buildings = false;
                a.tower_projectile = Siege.CoastShotId();
                a.tower_projectile_offset = 6f;
                a.tower_projectile_amount = 1;
                a.tower_projectile_reload = COAST_RELOAD;
                a.base_stats["damage"] = COAST_DAMAGE;
                a.can_be_upgraded = false;
                a.upgrade_to = string.Empty;
                a.buildings.Clear();
                if (real.atlas_asset != null) a.atlas_asset = real.atlas_asset;
            }
        }

        private static bool IsOceanTile(WorldTile t)
        {
            return t != null && t.Type.layer_type == TileLayerType.Ocean;
        }

        // Настоящее море рядом: ров города тоже сделан из воды, но берегом не считается
        private static bool SeaNear(WorldTile t, int r, CityState st)
        {
            if (st.moat_set_n != st.moat_tiles.Count)
            {
                st.moat_set.Clear();
                for (int i = 0; i < st.moat_tiles.Count; i++) if (st.moat_tiles[i] != null) st.moat_set.Add(st.moat_tiles[i]);
                st.moat_set_n = st.moat_tiles.Count;
            }
            // готовые клетки рва уже убраны из списка, поэтому ров определяем по геометрии: полоса вокруг внешней стены
            int moatLo = 0, moatHi = -1;
            if (st.is_capital && st.center != null && st.ring_radius.Count > 0)
            {
                int mr = st.moat_r > 0 ? st.moat_r : st.ring_radius[0] + MOAT_OFFSET;
                moatLo = mr - 1;
                moatHi = mr + 1;
            }
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    WorldTile n = World.world.GetTile(t.x + dx, t.y + dy);
                    if (!IsOceanTile(n) || st.moat_set.Contains(n)) continue;
                    if (moatHi >= moatLo)
                    {
                        int dd = System.Math.Max(System.Math.Abs(n.x - st.center.x), System.Math.Abs(n.y - st.center.y));
                        if (dd >= moatLo && dd <= moatHi) continue;
                    }
                    return true;
                }
            return false;
        }

        private static bool BuildingNear(WorldTile t, int r)
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    WorldTile n = World.world.GetTile(t.x + dx, t.y + dy);
                    if (n != null && n.hasBuilding()) return true;
                }
            return false;
        }

        // Свободная сухопутная клетка города, на которую можно что-то поставить (не дорога, не поле, не стена, не план стен)
        private static bool FreeLand(WorldTile t, City city, CityState st)
        {
            if (t == null || t.is_liquid || t.Type.wall || t.Type.road || t.Type.id == "field") return false;
            if (t.hasBuilding() || t.zone == null || t.zone.city != city) return false;
            if (AVOID_MOUNTAINS && OnMountain(t)) return false;
            if (st.plan_tiles.Contains(t)) return false;
            return true;
        }

        private static int ScanRadius(CityState st)
        {
            int r = st.ring_radius.Count > 0 ? st.ring_radius[st.ring_radius.Count - 1] : 20;
            return System.Math.Max(14, System.Math.Min(r + 8, 56));
        }

        // Береговая башня: маяк (люди, эльфы), светлый форт (гномы), форт из дерева и кости (орки).
        // У столицы до трёх, у города одна. Место ищем понемногу, по COAST_SCAN клеток за шаг.
        // Почему береговая башня не строится: пишем причину не чаще раза в 5 минут на город
        private static void NoCoast(City city, CityState st, string why)
        {
            double now = World.world.getCurWorldTime();
            if (now < st.next_coast_log) return;
            st.next_coast_log = now + 300.0;
            Dbg("no coastal tower at " + city.name + ": " + why);
        }

        private static void HandleCoast(City city, CityState st, WallStyle style, bool capital)
        {
            if (!COAST_TOWERS || st.rings.Count == 0) return;
            if (!Traits.Allows(city, Traits.COAST, true)) return;
            if (RingProgress(city, st.rings[0]) < 0.5f) return;
            if (city.under_construction_building != null) return;
            int max = capital ? COAST_MAX_CAPITAL : COAST_MAX_CITY;
            if (max <= 0) return;
            BuildingAsset asset = AssetManager.buildings.get(Coast.IdFor(style.arch));
            if (asset == null) return;
            if (!city.hasEnoughResourcesFor(asset.cost)) { NoCoast(city, st, "not enough resources (need " + (asset.cost.stone > 0 ? asset.cost.stone + " stone" : asset.cost.wood + " wood") + ")"); return; }

            List<WorldTile> mine = new List<WorldTile>();
            foreach (Building b in city.buildings)
                if (b != null && b.isAlive() && !b.isRuin() && b.asset != null && Coast.IsCoastId(b.asset.id) && b.current_tile != null) mine.Add(b.current_tile);
            if (mine.Count >= max) return;

            WorldTile c = st.center;
            if (c == null) return;
            int R = ScanRadius(st);
            int side = 2 * R + 1;
            int total = side * side;
            if (st.coast_cur < 0 || st.coast_cur >= total) st.coast_cur = Randy.randomInt(0, total);
            for (int k = 0; k < COAST_SCAN; k++)
            {
                int idx = (st.coast_cur + k) % total;
                WorldTile t = World.world.GetTile(c.x - R + idx % side, c.y - R + idx / side);
                if (!FreeLand(t, city, st)) continue;
                if (!SeaNear(t, 2, st)) continue;
                bool bad = false;
                for (int i = 0; i < mine.Count && !bad; i++)
                    if (System.Math.Abs(mine[i].x - t.x) < 12 && System.Math.Abs(mine[i].y - t.y) < 12) bad = true;
                if (bad) continue;
                for (int dx = -1; dx <= 1 && !bad; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        WorldTile n = World.world.GetTile(t.x + dx, t.y + dy);
                        if (n != null && n.Type.wall) { bad = true; break; }
                    }
                if (bad) continue;

                Building b2 = World.world.buildings.addBuilding(asset, t);
                if (b2 == null) continue;
                b2.setUnderConstruction();
                city.under_construction_building = b2;
                city.spendResourcesForBuildingAsset(asset.cost);
                st.coast_cur = (idx + 1) % total;
                st.coast_scanned = 0;
                Dbg("coastal tower (" + asset.id + ") placed at " + city.name + " " + t.x + "," + t.y);
                return;
            }
            st.coast_cur = (st.coast_cur + COAST_SCAN) % total;
            st.coast_scanned += COAST_SCAN;
            if (st.coast_scanned >= total)
            {
                st.coast_scanned = 0;
                NoCoast(city, st, "no suitable shore site in radius " + R + " (a full scan found nothing)");
            }
        }

        // Береговой частокол: редкие группы из 1-3 кольев у кромки воды. Места выбираются по хэшу клетки,
        // так что после загрузки сохранения город ставит те же колья и ничего не дублирует.
        private static void HandleStakes(City city, CityState st)
        {
            if (!COAST_STAKES || st.rings.Count == 0) return;
            if (RingProgress(city, st.rings[0]) < 0.5f) return;
            WorldTile c = st.center;
            if (c == null) return;
            int R = ScanRadius(st);
            int side = 2 * R + 1;
            int total = side * side;
            if (st.stake_cur < 0 || st.stake_cur >= total) st.stake_cur = 0;
            int seed = (int)(city.getID() & 0x7fffffff);
            int start = st.stake_cur;
            for (int k = 0; k < COAST_SCAN; k++)
            {
                int idx = (start + k) % total;
                int x = c.x - R + idx % side;
                int y = c.y - R + idx / side;
                int h = ((x * 73856093) ^ (y * 19349663) ^ (seed * 83492791)) & 0x7fffffff;
                if (h % 100 >= COAST_STAKE_PERCENT) continue;
                WorldTile t = World.world.GetTile(x, y);
                if (!FreeLand(t, city, st) || t.top_type == TopTileLibrary.wall_wild) continue;
                if (!SeaNear(t, 1, st) || BuildingNear(t, 1) || IsGateZone(t)) continue;

                // группа из 2-4 кольев идёт вдоль берега: каждый следующий - соседняя береговая клетка
                int len = 2 + (h / 100) % 3;
                WorldTile cur = t;
                int placed = 0;
                int[] ddx = new int[] { 1, 0, -1, 0 };
                int[] ddy = new int[] { 0, 1, 0, -1 };
                for (int i = 0; i < len && cur != null; i++)
                {
                    MapAction.terraformTop(cur, TopTileLibrary.wall_wild);
                    placed++;
                    WorldTile next = null;
                    int rot = (h / 7 + i) & 3;
                    for (int k2 = 0; k2 < 4 && next == null; k2++)
                    {
                        int dd = (rot + k2) & 3;
                        WorldTile n = World.world.GetTile(cur.x + ddx[dd], cur.y + ddy[dd]);
                        if (n != null && n.top_type != TopTileLibrary.wall_wild && FreeLand(n, city, st) && SeaNear(n, 1, st) && !BuildingNear(n, 1) && !IsGateZone(n)) next = n;
                    }
                    cur = next;
                }
                if (placed > 0)
                {
                    st.stake_cur = (idx + 1) % total;
                    Dbg("coast stakes: " + placed + " at " + x + "," + y);
                    return;
                }
            }
            st.stake_cur = (start + COAST_SCAN) % total;
        }

        // ---------- Дороги к воротам ----------

        private static bool RoadPassable(WorldTile n, WorldTile c, int box, bool allowNature)
        {
            if (n == null) return false;
            if (System.Math.Abs(n.x - c.x) > box || System.Math.Abs(n.y - c.y) > box) return false;
            if (n.is_liquid || n.Type.wall) return false;
            if (n.Type.id == "field") return false;
            if (IsCiv(n)) return false;
            if (!allowNature && n.hasBuilding()) return false;
            return true;
        }

        // Поиск пути от ворот к центру в обход строений (BFS по 4 соседям)
        private static List<WorldTile> FindRoad(WorldTile from, WorldTile c, int box, bool allowNature)
        {
            Queue<WorldTile> queue = new Queue<WorldTile>();
            Dictionary<WorldTile, WorldTile> parent = new Dictionary<WorldTile, WorldTile>();
            queue.Enqueue(from);
            parent[from] = null;
            int visited = 0;
            WorldTile goal = null;

            while (queue.Count > 0 && visited < ROAD_SEARCH_LIMIT)
            {
                WorldTile cur = queue.Dequeue();
                visited++;
                if (cur != from && System.Math.Abs(cur.x - c.x) <= 2 && System.Math.Abs(cur.y - c.y) <= 2)
                {
                    goal = cur;
                    break;
                }
                WorldTile[] around = cur.neighbours;
                if (around == null) continue;
                for (int i = 0; i < around.Length; i++)
                {
                    WorldTile n = around[i];
                    if (n == null || parent.ContainsKey(n)) continue;
                    if (!RoadPassable(n, c, box, allowNature)) continue;
                    parent[n] = cur;
                    queue.Enqueue(n);
                }
            }
            if (goal == null) return null;

            List<WorldTile> path = new List<WorldTile>();
            WorldTile p = goal;
            while (p != null)
            {
                path.Add(p);
                p = parent[p];
            }
            path.Reverse();
            return path;
        }

        private static void HandleRoads(City city, CityState st)
        {
            if (st.rings.Count == 0 || st.ring_radius.Count == 0) return;

            if (!st.roads_planned)
            {
                if (RingProgress(city, st.rings[0]) < ROADS_AFTER_WALLS) return;
                st.roads_planned = true;

                int R = st.ring_radius[0];
                WorldTile c = st.center;
                WorldTile[] gates = new WorldTile[]
                {
                    World.world.GetTile(c.x, c.y - R),
                    World.world.GetTile(c.x, c.y + R),
                    World.world.GetTile(c.x - R, c.y),
                    World.world.GetTile(c.x + R, c.y)
                };
                HashSet<WorldTile> seen = new HashSet<WorldTile>();
                for (int gi = 0; gi < gates.Length; gi++)
                {
                    WorldTile g = gates[gi];
                    if (g == null || gi == st.nogate) continue;
                    List<WorldTile> path = FindRoad(g, c, R + 3, false);
                    if (path == null) path = FindRoad(g, c, R + 3, true);
                    if (path == null) continue;
                    foreach (WorldTile t in path)
                        if (seen.Add(t)) st.road_tiles.Add(t);
                }
                Dbg("roads planned: " + st.road_tiles.Count + " tiles");
                return;
            }

            int placed = 0;
            while (st.road_tiles.Count > 0 && placed < ROADS_PER_TICK)
            {
                WorldTile t = st.road_tiles[0];
                st.road_tiles.RemoveAt(0);
                if (t.Type.road || t.Type.wall || t.is_liquid || IsCiv(t) || t.Type.id == "field") continue;
                if (AVOID_MOUNTAINS && OnMountain(t)) continue;
                MapAction.terraformTop(t, TopTileLibrary.road);
                placed++;
            }
        }

        // ---------- Захват города ----------

        // Башни добавляют защитнику 10 очков захвата за штуку. Здесь число считаемых башен ограничивается
        // населением, дешёвые башни весят меньше, повреждённые считаются пропорционально хп.
        public static void CapturePrefix(City __instance, Kingdom pKingdom, ref int pValue)
        {
            try
            {
                if (pValue < 10 || pKingdom != __instance.kingdom) return;   // только вызов от башен

                int pop = __instance.status.population;
                if (pop < CAPTURE_MIN_POP) { pValue = 0; return; }

                List<Building> list = __instance.getBuildingListOfType("type_watch_tower");
                if (list == null) return;

                float sum = 0f;
                foreach (Building b in list)
                {
                    if (b == null || b.isUnderConstruction() || b.isRuin()) continue;
                    float w = (b.asset != null && (b.asset.id == CHEAP_TOWER_ID || Coast.IsCoastId(b.asset.id))) ? CAPTURE_CHEAP_WEIGHT : 1f;
                    int max = b.getMaxHealth();
                    float hp = max > 0 ? UnityEngine.Mathf.Clamp01(b.getHealth() / (float)max) : 1f;
                    sum += w * hp;
                }
                float cap = CAPTURE_TOWERS_BASE + pop / (float)CAPTURE_TOWERS_PER_POP;
                pValue = (int)(10f * UnityEngine.Mathf.Min(sum, cap));
            }
            catch (System.Exception) { }
        }

        // ---------- Заброшенные здания ----------

        private static readonly Dictionary<City, double> split_logged = new Dictionary<City, double>();

        // Игра делит город на несвязные куски (стена считается препятствием) и бросает все, кроме самого большого.
        // У укреплённых городов не трогаем мелкие куски: территория и здания остаются за городом.
        // Что поражено снарядом у клетки: 2 - ворота закрытого города, 1 - стена, 0 - земля или здание
        internal static int ImpactKind(WorldTile t)
        {
            foreach (KeyValuePair<City, CityState> kv in _states)
            {
                CityState st = kv.Value;
                if (!st.gates_closed || st.center == null) continue;
                if (System.Math.Abs(t.x - st.center.x) > 80 || System.Math.Abs(t.y - st.center.y) > 80) continue;
                foreach (GateTile g in st.gates)
                    if (g.closed && g.tile != null && System.Math.Abs(g.tile.x - t.x) <= 3 && System.Math.Abs(g.tile.y - t.y) <= 3) return 2;
            }
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                {
                    WorldTile n = World.world.GetTile(t.x + dx, t.y + dy);
                    if (n != null && n.Type.wall) return 1;
                }
            return 0;
        }

        // Игра отбирает у города зоны, которые стены отрезали от зданий. Для города со стенами не даём этого делать
        // (иначе стены сами делают землю ничьей, соседи её занимают, и начинается круговорот). Пишем в лог, что произошло.
        private static bool KEEP_WALLED_ZONES = Cfg.Bool("wall_keep_zones", true, "не отбирать у города со стенами зоны, которые стены отрезали от зданий");
        private static readonly Dictionary<City, double> leftover_logged = new Dictionary<City, double>();

        // Игра отбирает зону у города, если после смены клетки в зоне нет зданий этого города. Клетка стены меняет тип
        // клетки и запускает эту проверку, поэтому каждая стена в пустой зоне города делала землю ничьей.
        // Для клеток стен проверку пропускаем.
        public static bool CheckCityZonePrefix(WorldTile pTile)
        {
            try
            {
                if (!KEEP_WALLED_ZONES || pTile == null || pTile.zone == null || pTile.zone.city == null) return true;
                CityState st;
                if (!_states.TryGetValue(pTile.zone.city, out st)) return true;
                if (pTile.Type.wall) return false;
                City cc = pTile.zone.city;
                if (st.rings.Count > 0 && cc.getTile() != null)
                {
                    int ro = OuterRadius(cc) + 5;
                    if (System.Math.Max(System.Math.Abs(pTile.x - cc.getTile().x), System.Math.Abs(pTile.y - cc.getTile().y)) <= ro) return false;
                }
                return true;
            }
            catch (System.Exception) { return true; }
        }

        // 1.3: Worldfall (3D) даёт домам широкую коллизию, дома перед воротами перекрывают проход.
        // Не даём городу ставить новые здания в коридоре перед и за воротами.
        private static bool GATE_CLEAR = Cfg.Bool("gate_clear_front", true, "не строить дома в коридоре перед воротами и за ними (иначе в 3D-режиме Worldfall юниты не проходят)");
        private static int GATE_CLEAR_LEN = Cfg.Int("gate_clear_length", 11, "длина свободного коридора у ворот (клеток в каждую сторону)");
        private static int GATE_CLEAR_HALF = Cfg.Int("gate_clear_half_width", 7, "полуширина свободного коридора у ворот (клеток)");
        private static HashSet<int> _gate_front = new HashSet<int>();
        private static float _gate_front_t = -100f;

        private static void RebuildGateFront()
        {
            HashSet<int> set = new HashSet<int>();
            foreach (KeyValuePair<City, CityState> kv in _states)
                foreach (GateTile g in kv.Value.gates)
                {
                    if (g.tile == null) continue;
                    bool axisY = g.side == 0 || g.side == 1;
                    for (int a = -GATE_CLEAR_LEN; a <= GATE_CLEAR_LEN; a++)
                        for (int b = -GATE_CLEAR_HALF; b <= GATE_CLEAR_HALF; b++)
                        {
                            int x = g.tile.x + (axisY ? b : a);
                            int y = g.tile.y + (axisY ? a : b);
                            set.Add(x + (y << 16));
                        }
                }
            _gate_front = set;
            _gate_front_t = UnityEngine.Time.realtimeSinceStartup;
        }

        public static bool GateFrontPrefix(WorldTile pTile, BuildingAsset pAsset, ref bool __result)
        {
            try
            {
                if (!GATE_CLEAR || pTile == null || pAsset == null) return true;
                if (UnityEngine.Time.realtimeSinceStartup - _gate_front_t > 3f) RebuildGateFront();
                if (_gate_front.Count == 0) return true;
                if (!_gate_front.Contains(pTile.x + (pTile.y << 16))) return true;
                __result = false;
                return false;
            }
            catch (System.Exception) { return true; }
        }

        public static void DestroyPrefix(Building __instance) { Fx.TowerWreck(__instance); }

        // Диагностика: кто отбирает зоны у города со стенами (метод-источник пишем в лог, не чаще раза в 30 с на пару город+метод)
        private static readonly Dictionary<string, double> rz_last = new Dictionary<string, double>();
        private static readonly Dictionary<string, int> rz_count = new Dictionary<string, int>();
        private static int rz_logged, rz_logged2;

        public static bool RemoveZonePrefix(City __instance, TileZone pZone)
        {
            try
            {
                CityState st;
                if (__instance == null || pZone == null || !_states.TryGetValue(__instance, out st) || st.built_walls.Count == 0) return true;
                string who = "?";
                System.Diagnostics.StackFrame fr = new System.Diagnostics.StackTrace(2, false).GetFrame(0);
                if (fr != null && fr.GetMethod() != null) who = (fr.GetMethod().DeclaringType != null ? fr.GetMethod().DeclaringType.Name : "") + "." + fr.GetMethod().Name;
                // зону, где стоит стена города, не отбираем ни проверкой клетки, ни сжатием границы, ни очисткой отрезанных зон
                bool passive = who == "MapBox.checkCityZone" || who == "CityBehBorderShrink.execute" || who.StartsWith("CityZoneAbandon.");
                bool hasWall = false;
                // зоны внутри кольца стен (и рядом с ним) тоже не отбираем: без зданий в них игра считала бы их лишними
                if (passive && KEEP_WALLED_ZONES && st.rings.Count > 0 && pZone.centerTile != null && __instance.getTile() != null)
                {
                    int ro = OuterRadius(__instance) + 5;
                    if (System.Math.Max(System.Math.Abs(pZone.centerTile.x - __instance.getTile().x), System.Math.Abs(pZone.centerTile.y - __instance.getTile().y)) <= ro) hasWall = true;
                }
                if (!hasWall && passive && KEEP_WALLED_ZONES && pZone.tiles != null)
                    for (int i = 0; i < pZone.tiles.Length; i++)
                    {
                        WorldTile t = pZone.tiles[i];
                        if (t != null && (st.built_walls.Contains(t) || t.Type.wall)) { hasWall = true; break; }
                    }
                if (passive && hasWall && rz_logged++ < 8) Log("[zones] removal of a zone at " + __instance.name + " blocked (by " + who + "), zones left " + __instance.zones.Count);
                else if (!hasWall && rz_logged2++ < 8) Log("[zones] zone of " + __instance.name + " removed by " + who + " (not protected), zones left " + __instance.zones.Count);
                string key = __instance.name + "|" + who;
                int c; rz_count.TryGetValue(key, out c); rz_count[key] = ++c;
                double now = World.world.getCurWorldTime(), last;
                if (!(rz_last.TryGetValue(key, out last) && now - last < 30.0))
                {
                    rz_last[key] = now; rz_count[key] = 0;
                    Dbg("zone removal at walled city " + __instance.name + " by " + who + (hasWall ? " blocked (wall in the zone)" : "") + " (x" + c + " since last note), zones left " + __instance.zones.Count);
                }
                return !hasWall;
            }
            catch (System.Exception) { return true; }
        }

        public static bool AbandonLeftoverPrefix(CityZoneAbandon __instance, City pCity)
        {
            try
            {
                CityState st;
                if (pCity == null || !_states.TryGetValue(pCity, out st) || st.built_walls.Count == 0) return true;
                int n = __instance._zones_to_check.Count;
                if (n == 0) return true;

                double now = World.world.getCurWorldTime();
                double last;
                if (!leftover_logged.TryGetValue(pCity, out last) || now - last > 30.0)
                {
                    leftover_logged[pCity] = now;
                    Dbg("zones cut off by walls at " + pCity.name + ": " + n + " of " + pCity.zones.Count + (KEEP_WALLED_ZONES ? " (kept)" : " (abandoned by the game)"));
                }
                return !KEEP_WALLED_ZONES;
            }
            catch (System.Exception) { return true; }
        }

        public static bool AbandonSmallAreasPrefix(CityZoneAbandon __instance, City pCity)
        {
            try
            {
                CityState st;
                if (pCity == null || !_states.TryGetValue(pCity, out st) || st.rings.Count == 0) return true;

                double now = World.world.getCurWorldTime();
                double last;
                if (!split_logged.TryGetValue(pCity, out last) || now - last > 60.0)
                {
                    split_logged[pCity] = now;
                    Dbg("city split into separate areas, abandon skipped (walled city), areas=" + __instance._split_areas.Count);
                }
                return false;
            }
            catch (System.Exception) { return true; }
        }

        // Здание без королевства на миникарте: рисуем прозрачным пикселем вместо ошибки
        public static bool MapIconPrefix(Building pBuilding, ref UnityEngine.Color32 __result)
        {
            if (pBuilding == null || pBuilding.kingdom == null)
            {
                __result = new UnityEngine.Color32(0, 0, 0, 0);
                return false;
            }
            return true;
        }

        // ---------- Ремонт башен ----------

        private static void RepairTowers(City city, CityState st)
        {
            foreach (PlanItem it in st.plan)
            {
                if (it.kind != PlanKind.Tower) continue;
                WorldTile t = it.tile;
                if (!t.hasBuilding()) continue;
                Building b = t.building;
                if (b.asset == null || !b.asset.tower) continue;

                if (b.isRuin())
                {
                    b.startDestroyBuilding();   // убираем руины, на их месте построят новую башню
                    Dbg("tower ruin cleared at " + t.x + "," + t.y);
                    continue;
                }
                if (b.isUnderConstruction()) continue;
                int max = b.getMaxHealth();
                int hp = b.getHealth();
                if (hp >= max) continue;
                if (city.amount_stone <= REPAIR_STONE_RESERVE) continue;
                city.takeResource("stone", 1);
                b.setHealth(hp + System.Math.Max(1, (int)(max * REPAIR_HEAL_PERCENT)));
            }
        }

        // ---------- Площадь ----------

        internal static bool Clearable(Building b)
        {
            if (b == null || b.asset == null || b.asset.city_building) return false;
            switch (b.asset.building_type)
            {
                case BuildingType.Building_Tree:
                case BuildingType.Building_Plant:
                case BuildingType.Building_Mineral:
                case BuildingType.Building_Nature:
                case BuildingType.Building_Fruits:
                case BuildingType.Building_Poops:
                    return true;
            }
            return false;
        }

        // Узор: сплошные оси и граница радиуса, остальное шахматкой
        internal static bool PlazaPattern(int dx, int dy, int d, int target)
        {
            if (dx == 0 || dy == 0) return true;
            if (d == target) return true;
            return ((dx + dy) & 1) == 0;
        }

        // Узоры площади: 0 кирпичики, 1 шахматная доска, 2 роза ветров, 3 крест с рамкой, 4 хаотичная мозаика
        internal static bool PlazaPatternV(int variant, int seed, int dx, int dy, int d, int target)
        {
            int ax = System.Math.Abs(dx), ay = System.Math.Abs(dy);
            switch (variant)
            {
                case 1:
                    return d == target || ((dx + dy) & 1) == 0;
                case 2:
                    return dx == 0 || dy == 0 || ax == ay || d == target || (target >= 6 && d == target / 2);
                case 3:
                    return ax <= 1 || ay <= 1 || d == target || (target >= 8 && d == target / 2 && ((dx + dy) & 1) == 0);
                case 4:
                    {
                        if (d == target || d <= 1) return true;
                        int h = (dx * 73856093) ^ (dy * 19349663) ^ (seed * 83492791);
                        h = (h & 0x7fffffff) % 100;
                        return h < 58;
                    }
                default:
                    return PlazaPattern(dx, dy, d, target);
            }
        }

        // Знамёна по углам площади (у орков - костяные бивни). Только на последнем уровне площади.
        private static void HandleDecor(City city, CityState st, WorldTile center)
        {
            int pop = city.status.population;
            if (pop < PLAZA_POP[PLAZA_POP.Length - 1]) return;
            int inner = st.ring_radius.Count > 0 ? st.ring_radius[0] : CAPITAL_RADII[0];
            int R = System.Math.Min(PLAZA_RADII[PLAZA_RADII.Length - 1], inner - PLAZA_MARGIN);
            if (R < 4) return;

            WallStyle style = GetStyle(city);
            bool paved = Traits.Allows(city, Traits.PAVING, style.arch == "human" || style.arch == "dwarf");
            if (paved && (st.plaza_planned < R || st.plaza_queue.Count > 0)) return;   // сначала площадь

            // один вид украшения на город: у орков одна из трёх костяных фигур, у остальных чаще висячий баннер, реже флаг
            int seed = (int)(city.getID() & 0x7fffffff);
            string id;
            if (style.arch == "orc") { int k = seed % 3; id = k == 0 ? Siege.TUSK_ID : (k == 1 ? Siege.TUSK2_ID : Siege.TUSK3_ID); }
            else id = ((seed / 7) % 3 == 0) ? Siege.FLAG_ID : Siege.BANNER_ID;
            int[] cx = new int[] { -R, R, -R, R };
            int[] cy = new int[] { -R, -R, R, R };
            for (int i = 0; i < 4; i++)
            {
                Building b = st.decor[i];
                if (b != null && b.isAlive())
                {
                    if (b.kingdom != city.kingdom) b.setKingdom(city.kingdom);
                    continue;
                }
                if (st.decor_builds[i] >= 3) continue;                 // не больше трёх раз на угол
                WorldTile corner = World.world.GetTile(center.x + cx[i], center.y + cy[i]);
                WorldTile t = Siege.FreeSpot(corner, 2);
                if (t == null || IsCiv(t)) continue;
                Building nb = Siege.Put(id, t, city.kingdom);
                if (nb != null)
                {
                    st.decor[i] = nb;
                    st.decor_builds[i]++;
                    Dbg("decor " + id + " placed at " + t.x + "," + t.y);
                    return;                                              // по одному за раз
                }
            }
        }

        internal static void CollectDecor(HashSet<Building> set)
        {
            foreach (CityState bs in _states.Values)
                for (int i = 0; i < bs.barricades.Length; i++)
                    if (bs.barricades[i] != null) set.Add(bs.barricades[i]);
            if (!CITY_DECOR) return;     // украшения выключены: всё, что осталось, считается бесхозным и убирается
            foreach (CityState st in _states.Values)
                for (int i = 0; i < st.decor.Length; i++)
                    if (st.decor[i] != null) set.Add(st.decor[i]);
        }

        // ---------- Ворота ----------

        private static int CENTER_CONFIRM_TICKS = Cfg.Int("center_confirm_ticks", 5, "сколько проверок подряд новый центр города должен держаться, прежде чем стены перенесут");
        private static bool GATE_DIAG = Cfg.Bool("gate_diag", false, "писать в лог, почему ворота не закрыты, когда враг рядом или уже внутри стен");

        // Диагностика ворот: враги рядом, но ворота открыты; или враг уже внутри стен
        private static void GateDiag(City city, CityState st, double now)
        {
            if (!GATE_DIAG || now < st.next_diag) return;
            st.next_diag = now + 4.0;
            if (city.kingdom == null || !city.kingdom.hasEnemies() || st.center == null) return;
            int outer = 0;
            for (int i = 0; i < st.ring_radius.Count; i++) if (st.ring_radius[i] > outer) outer = st.ring_radius[i];
            WorldTile c = st.center;
            int range = outer + GATE_RADIUS_EXTRA;
            int n = 0, inside = 0, nearWall = 0, ix = 0, iy = 0;
            string insideTask = "";
            foreach (Actor a in Finder.getUnitsFromChunk(c, (range + 15) / 16))
            {
                if (a == null || !a.isAlive() || a.current_tile == null || a.kingdom == null) continue;
                if (!a.kingdom.isCiv() || !a.kingdom.isEnemy(city.kingdom) || a.is_inside_boat || !a.isWarrior()) continue;
                int cheb = System.Math.Max(System.Math.Abs(a.current_tile.x - c.x), System.Math.Abs(a.current_tile.y - c.y));
                if (cheb > range) continue;
                n++;
                if (cheb >= outer - 2 && cheb <= outer + SPUR_LEN + 12) nearWall++;
                if (cheb < outer - 1)
                {
                    if (inside == 0)
                    {
                        ix = a.current_tile.x; iy = a.current_tile.y;
                        try { if (a.hasTask()) insideTask = a.getTaskText(); } catch (System.Exception) { }
                    }
                    inside++;
                }
            }
            if (n == 0) return;

            int total = 0, closed = 0, broken = 0;
            // клетка, которая и так стена (остаток старой стены на месте ворот), считается закрытой
            foreach (GateTile g in st.gates) { total++; if (g.closed || (g.tile.Type.wall && !g.broken)) closed++; if (g.broken) broken++; }
            string state = "gates " + closed + "/" + total + " closed, broken " + broken + ", flag " + (st.gates_closed ? "closed" : "open")
                + ", siege=" + Siege.UnderSiege(city) + ", danger=" + city.isInDanger()
                + ", enemies " + n + " (min " + GATE_THREAT_MIN + "), at wall " + nearWall;

            if (inside > 0)
            {
                if (now < st.next_inside_log) return;
                st.next_inside_log = now + 15.0;
                // через какие ворота вошли: ближайшие ворота к врагу
                string nearest = "none";
                float bd = float.MaxValue;
                foreach (GateTile g in st.gates)
                {
                    float d = Toolbox.Dist(g.tile.x, g.tile.y, ix, iy);
                    if (d < bd) { bd = d; nearest = "side " + g.side + " ring " + g.ring + " " + (g.closed ? "closed" : (g.broken ? "broken" : "open")) + " dist " + (int)d; }
                }
                Dbg("ENEMY INSIDE walls of " + city.name + ": " + inside + " warriors (first task: " + insideTask + "); nearest gate: " + nearest + "; " + state);
            }
            else if (!st.gates_closed && closed == 0 && nearWall > 1)
            {
                Dbg("gates open at " + city.name + " while enemies at the wall: " + state);
            }
            else if (st.gates_closed && closed < total)
            {
                int blockedNow = 0;
                foreach (GateTile g in st.gates) if (!g.closed && !g.broken && (g.tile.hasUnits() || g.tile.hasBuilding())) blockedNow++;
                string why = "";
                int listed = 0;
                foreach (GateTile g in st.gates)
                {
                    if (g.closed || listed >= 4 || (g.tile.Type.wall && !g.broken)) continue;
                    WorldTile gt = g.tile;
                    string topId = gt.top_type != null ? gt.top_type.id : "-";
                    why += " [ring " + g.ring + " side " + g.side + " at " + gt.x + "," + gt.y + ": tile=" + gt.Type.id + " top=" + topId
                        + " wall=" + gt.Type.wall + " broken=" + g.broken + " units=" + gt.countUnits() + " building=" + gt.hasBuilding()
                        + " plan=" + st.plan_tiles.Contains(gt) + "]";
                    listed++;
                }
                Dbg("gates not fully closed at " + city.name + ": blocked by units/buildings " + blockedNow + ", " + state + "; open tiles:" + why);
            }
        }

        private static int GATE_ARMY_RANGE = Cfg.Int("gate_army_range", 30, "ворота закрываются, когда вражеская армия, идущая на город, ближе стольких клеток от его стен");
        private static int GATE_ARMY_MIN = Cfg.Int("gate_army_min", 10, "минимум бойцов, чтобы считать группу армией (меньше - патруль: ворота запираются, кроме одной стороны)");
        private static int GATE_PATROL_MIN = 3;
        private static float GATE_STEP_SECONDS = Cfg.Float("gate_step_seconds", 30f, "через сколько секунд после ухода армии ворота перестают быть заперты полностью (тревога снижается по ступеням)");

        // Вражеская армия идёт именно на этот город и уже недалеко: запираемся, не дожидаясь, пока она встанет у стен
        private static bool ApproachingArmy(City city, CityState st, int outer, int minCount)
        {
            foreach (City oc in World.world.cities)
            {
                if (oc == null || oc == city || !oc.isAlive() || oc.target_attack_city != city) continue;
                Army army = oc.army;
                if (army == null || !army.hasCaptain() || army.countMelee() + army.countRange() < minCount) continue;
                Actor cap = army.getCaptain();
                if (cap == null || !cap.isAlive() || cap.current_tile == null) continue;
                int cheb = System.Math.Max(System.Math.Abs(cap.current_tile.x - st.center.x), System.Math.Abs(cap.current_tile.y - st.center.y));
                if (cheb <= outer + GATE_ARMY_RANGE)
                {
                    st.garrison_pos = cap.current_tile;
                    if (st.last_enemy_count == 0) { st.enemy_cx = cap.current_tile.x; st.enemy_cy = cap.current_tile.y; }
                    return true;
                }
            }
            return false;
        }

        // Уровень тревоги: 0 - тихо, 1 - патруль (мелкая группа), 2 - армия идёт на город или осада
        public static bool ALERT_ON = Cfg.Bool("alert_levels", true, "уровни тревоги: патруль, армия, осада (выключено - ворота запираются только при армии)");
        private static int ThreatLevel(City city, CityState st)
        {
            int lv = ThreatLevelRaw(city, st);
            return (!ALERT_ON && lv == 1) ? 0 : lv;
        }

        private static int ThreatLevelRaw(City city, CityState st)
        {
            st.near_count = 0;
            st.last_enemy_count = 0;
            if (Siege.UnderSiege(city)) return 2;
            int outer = 0;
            for (int i = 0; i < st.ring_radius.Count; i++) if (st.ring_radius[i] > outer) outer = st.ring_radius[i];
            if (city.kingdom == null || !city.kingdom.hasEnemies()) { st.garrison_pos = null; return 0; }
            if (ApproachingArmy(city, st, outer, GATE_ARMY_MIN)) return 2;
            int small = ApproachingArmy(city, st, outer, GATE_PATROL_MIN) ? 1 : 0;
            WorldTile c = st.center;
            int range = outer + GATE_RADIUS_EXTRA;
            int n = 0, near = 0;
            float sx = 0f, sy = 0f;
            foreach (Actor a in Finder.getUnitsFromChunk(c, (range + 15) / 16))
            {
                if (a == null || !a.isAlive() || a.current_tile == null || a.kingdom == null) continue;
                if (!a.kingdom.isCiv() || !a.kingdom.isEnemy(city.kingdom) || a.is_inside_boat || !a.isWarrior()) continue;
                if (Toolbox.Dist(a.current_tile.x, a.current_tile.y, c.x, c.y) > range) continue;
                n++;
                sx += a.current_tile.x;
                sy += a.current_tile.y;
                int cheb = System.Math.Max(System.Math.Abs(a.current_tile.x - c.x), System.Math.Abs(a.current_tile.y - c.y));
                if (cheb >= outer - 2 && cheb <= outer + SPUR_LEN + 12) near++;
            }
            st.last_enemy_count = n;
            st.near_count = near;
            if (n > 0) { st.enemy_cx = sx / n; st.enemy_cy = sy / n; }
            // город в опасности (его бьют) - достаточно и одного-двух врагов
            bool hostile = n >= GATE_THREAT_MIN || (n >= 2 && near >= 2) || (n >= 1 && city.isInDanger());
            if (!hostile) { if (small == 0) st.garrison_pos = null; return small; }
            if (n >= GATE_ARMY_MIN || city.isInDanger())
            {
                st.garrison_pos = World.world.GetTile((int)st.enemy_cx, (int)st.enemy_cy);
                return 2;
            }
            return 1;
        }

        private static bool Threat(City city, CityState st) { return ThreatLevel(city, st) >= 2; }

        // Какая сторона остаётся открытой при патруле: самая далёкая от врага
        private static int PickOpenSide(CityState st)
        {
            int best = -1; float bestD = -1f;
            for (int side = 0; side < 4; side++)
            {
                float sx = 0f, sy = 0f; int n = 0;
                foreach (GateTile g in st.gates) if (g.side == side && g.ring == 0) { sx += g.tile.x; sy += g.tile.y; n++; }
                if (n == 0) continue;
                float dx = sx / n - st.enemy_cx, dy = sy / n - st.enemy_cy;
                float d = dx * dx + dy * dy;
                if (d > bestD) { bestD = d; best = side; }
            }
            return best;
        }

        // Враг долго стоит у запертых ворот: самая близкая к нему сторона внешнего кольца поддаётся
        private static void BreachGate(City city, CityState st, double now)
        {
            int ring = -1;
            foreach (GateTile g in st.gates) if (g.closed && g.ring > ring) ring = g.ring;
            if (ring < 0) return;

            int bestSide = -1;
            float bestD = float.MaxValue;
            for (int side = 0; side < 4; side++)
            {
                float sx = 0f, sy = 0f; int n = 0;
                foreach (GateTile g in st.gates)
                    if (g.closed && g.ring == ring && g.side == side) { sx += g.tile.x; sy += g.tile.y; n++; }
                if (n == 0) continue;
                float dx = sx / n - st.enemy_cx, dy = sy / n - st.enemy_cy;
                float d = st.last_enemy_count > 0 ? dx * dx + dy * dy : Randy.randomFloat(0f, 1f);   // нет врагов в поле зрения - сторона наугад
                if (d < bestD) { bestD = d; bestSide = side; }
            }
            if (bestSide < 0) return;

            BreakSide(city, st, ring, bestSide, now, false);
        }

        // Сторона ворот разбита: ворота открыты до конца осады, баррикада за ними ломается
        private static void BreakSide(City city, CityState st, int ring, int side, double now, bool byGuns)
        {
            WorldTile first = null;
            foreach (GateTile g in st.gates)
            {
                if (!g.closed || g.ring != ring || g.side != side) continue;
                if (g.tile.Type.wall) MapAction.terraformTop(g.tile, g.prev);
                g.closed = false;
                g.broken = true;
                if (first == null) first = g.tile;
            }
            st.pressure = 0;
            st.closed_since = now;
            st.gate_hp.Remove(ring * 4 + side);
            Scroll.Note(city, "gate");
            Dbg("gate breached at " + city.name + ", ring " + ring + ", side " + side + (byGuns ? " by siege guns" : " by the crowd")
                + " (enemy warriors: " + st.last_enemy_count + ", at the wall: " + st.near_count + ")");
            if (first != null)
            {
                try
                {
                    EffectsLibrary.spawnAtTileRandomScale("fx_explosion_small", first, byGuns ? 0.18f : 0.1f, byGuns ? 0.26f : 0.15f);
                    Sfx.PlayLater("gate_destroyed", first, byGuns ? 1.0f : 0.1f);
                    Fx.GateWreck(first, city.kingdom, city);
                }
                catch (System.Exception) { }
            }
            try { Fx.GateFireOut(city, ring * 4 + side); } catch (System.Exception) { }
            if (first != null) { st.breaches.Add(first); if (st.breaches.Count > 8) st.breaches.RemoveAt(0); }
            DropBarricade(st, ring, side);
        }

        private static void DropBarricade(CityState st, int ring, int side)
        {
            int k = ring * 4 + side;
            if (k < 0 || k >= 8) return;
            Building b = st.barricades[k];
            if (b != null && b.isAlive()) b.startDestroyBuilding();
            st.barricades[k] = null;
            Building fl = st.barricades[k + 8];
            if (fl != null && fl.isAlive()) fl.startDestroyBuilding();
            st.barricades[k + 8] = null;
        }

        private static void ClearBarricades(CityState st)
        {
            for (int i = 0; i < st.barricades.Length; i++)
            {
                Building b = st.barricades[i];
                if (b != null && b.isAlive()) b.startDestroyBuilding();
                st.barricades[i] = null;
            }
        }

        // ---------- Обстрел ворот ----------

        internal const float IRON_MULT = 1.5f;        // черта «Укреплённые ворота»: ворота на 50% прочнее
        private static float GATE_HP = Cfg.Float("gate_hp", 900f, "прочность одной стороны ворот при обстреле орудиями (у столиц в полтора раза больше, у деревянных в 0.6 раза)");

        internal static bool GatesSealed(City city)
        {
            CityState st;
            if (city == null || !_states.TryGetValue(city, out st) || !st.gates_closed) return false;
            foreach (GateTile g in st.gates) if (g.closed) return true;
            return false;
        }

        // Для шкалы прочности ворот при наведении мыши (2D): доля целых ворот у ближайшего к курсору закрытого проёма
        internal static bool GateHover(WorldTile mouse, out float frac, out WorldTile at)
        {
            frac = 1f; at = null;
            if (mouse == null) return false;
            foreach (KeyValuePair<City, CityState> kv in _states)
            {
                CityState st = kv.Value;
                if (!st.gates_closed || kv.Key == null || !kv.Key.isAlive()) continue;
                foreach (GateTile g in st.gates)
                {
                    if (g.tile == null || g.broken || !g.closed) continue;
                    if (System.Math.Abs(g.tile.x - mouse.x) > 1 || System.Math.Abs(g.tile.y - mouse.y) > 1) continue;
                    float hp;
                    float max = GateMaxHp(kv.Key, st);
                    if (!st.gate_hp.TryGetValue(g.ring * 4 + g.side, out hp)) hp = max;
                    frac = UnityEngine.Mathf.Clamp01(hp / max); at = g.tile;
                    return true;
                }
            }
            return false;
        }

        private static float GateMaxHp(City city, CityState st)
        {
            float hp = GATE_HP;
            if (st.is_capital) hp *= 1.5f;
            try { if (GetStyle(city).arch == "orc") hp *= 0.6f; } catch (System.Exception) { }
            if (Traits.HasCity(city, Traits.IRON)) hp *= IRON_MULT;
            return hp;
        }

        // Какую сторону ворот бьют орудия: если у внешнего кольца сторона уже разбита, то внутреннее кольцо на той же стороне;
        // иначе ближайшая к орудию сторона внешнего кольца с закрытыми воротами
        private static float OUTER_BREACHED = Cfg.Float("siege_outer_breached", 0.9f, "если от внешней стены осталось меньше этой доли, таран и орудия переходят к внутренним воротам");
        // Какая доля внешней стены цела (считается не чаще раза в 3 секунды)
        private static float OuterIntegrity(CityState st, int ringIdx)
        {
            double now = World.world.getCurWorldTime();
            if (now < st.next_integrity) return st.outer_integrity;
            st.next_integrity = now + 3.0;
            float f = 1f;
            try
            {
                if (ringIdx >= 0 && ringIdx < st.rings.Count)
                {
                    int total = 0, alive = 0;
                    foreach (PlanItem it in st.rings[ringIdx])
                    {
                        if (it.kind != PlanKind.Wall || it.tile == null) continue;
                        if (it.tile.Type.wall) { total++; alive++; continue; }
                        if (it.tile.is_liquid || st.known_blocked.Contains(it.tile)) continue;
                        total++;
                    }
                    if (total >= 20) f = (float)alive / total;
                }
            }
            catch (System.Exception) { }
            st.outer_integrity = f;
            return f;
        }

        private static bool FocusSide(CityState st, WorldTile from, out int ring, out int side, out WorldTile at)
        {
            ring = -1; side = -1; at = null;
            int maxRing = -1;
            foreach (GateTile g in st.gates) if (g.ring > maxRing) maxRing = g.ring;
            if (maxRing < 0) return false;

            if (maxRing > 0 && OuterIntegrity(st, maxRing) < OUTER_BREACHED)
            {
                // внешняя стена уже разрушена: все прошли внутрь, ломаем ближайшие внутренние ворота
                float bi = float.MaxValue;
                foreach (GateTile g in st.gates)
                {
                    if (g.ring >= maxRing || !g.closed || g.broken) continue;
                    float d = Toolbox.Dist(g.tile.x, g.tile.y, from.x, from.y);
                    if (d < bi) { bi = d; ring = g.ring; side = g.side; at = g.tile; }
                }
                if (side >= 0) return true;
            }

            if (maxRing > 0)
            {
                for (int sd = 0; sd < 4; sd++)
                {
                    bool outerBroken = false;
                    foreach (GateTile g in st.gates) if (g.ring == maxRing && g.side == sd && g.broken) { outerBroken = true; break; }
                    if (!outerBroken) continue;
                    foreach (GateTile g in st.gates)
                        if (g.ring < maxRing && g.side == sd && g.closed && !g.broken)
                        {
                            ring = g.ring; side = sd; at = g.tile;
                            return true;
                        }
                }
            }

            int top = -1;
            foreach (GateTile g in st.gates) if (g.closed && !g.broken && g.ring > top) top = g.ring;
            if (top < 0) return false;
            float bd = float.MaxValue;
            foreach (GateTile g in st.gates)
            {
                if (!g.closed || g.broken || g.ring != top) continue;
                float d = Toolbox.Dist(g.tile.x, g.tile.y, from.x, from.y);
                if (d < bd) { bd = d; ring = g.ring; side = g.side; at = g.tile; }
            }
            return side >= 0;
        }

        internal static bool GateFocus(City city, WorldTile from, out WorldTile at)
        {
            at = null;
            CityState st;
            if (city == null || from == null || !_states.TryGetValue(city, out st) || !st.gates_closed) return false;
            int r, sd;
            return FocusSide(st, from, out r, out sd, out at);
        }

        // Попадание орудия по воротам
        internal static void GateDamage(City city, WorldTile gun, float dmg)
        {
            CityState st;
            if (city == null || gun == null || !_states.TryGetValue(city, out st) || !st.gates_closed) return;
            int ring, side;
            WorldTile at;
            if (!FocusSide(st, gun, out ring, out side, out at)) return;
            if (Toolbox.Dist(gun.x, gun.y, at.x, at.y) > Siege.GunRange() + 6f) return;
            int key = ring * 4 + side;
            float max = GateMaxHp(city, st);
            float hp;
            if (!st.gate_hp.TryGetValue(key, out hp)) hp = max;
            hp -= dmg;
            st.gate_hp[key] = hp;
            if (hp <= 0f) BreakSide(city, st, ring, side, World.world.getCurWorldTime(), true);
        }

        private static float WALL_HP_PCT = Cfg.Float("siege_wall_hp_pct", 70f, "прочность участка стены в процентах от прочности ворот");

        // Попадание орудия по внешней стене. Возвращает false, если подходящего участка нет
        internal static bool WallDamage(City city, WorldTile gun, float dmg, int rad = 2)
        {
            CityState st;
            if (city == null || gun == null || !_states.TryGetValue(city, out st) || !st.gates_closed || st.rings.Count == 0) return false;
            int outer = 0;
            for (int i = 1; i < st.ring_radius.Count && i < st.rings.Count; i++) if (st.ring_radius[i] > st.ring_radius[outer]) outer = i;
            HashSet<WorldTile> gateTiles = new HashSet<WorldTile>();
            foreach (GateTile g in st.gates) gateTiles.Add(g.tile);
            PlanItem best = null;
            float bd = Siege.GunRange() + 6f;
            List<PlanItem> ringItems = st.rings[outer];
            for (int i = 0; i < ringItems.Count; i++)
            {
                PlanItem p = ringItems[i];
                if (p.kind != PlanKind.Wall || p.iron || p.tile == null || !p.tile.Type.wall || gateTiles.Contains(p.tile)) continue;
                float d = Toolbox.Dist(gun.x, gun.y, p.tile.x, p.tile.y);
                if (d < bd) { bd = d; best = p; }
            }
            if (best == null) return false;
            long key = (best.tile.x / 4) * 100000L + (best.tile.y / 4);
            float max = GateMaxHp(city, st) * WALL_HP_PCT / 100f;
            float hp;
            if (!st.wall_hp.TryGetValue(key, out hp)) hp = max;
            hp -= dmg;
            st.wall_hp[key] = hp;
            try
            {
                
                EffectsLibrary.spawnAtTile("fx_fire_smoke", best.tile, Randy.randomFloat(0.12f, 0.2f));
            }
            catch (System.Exception) { }
            if (hp > 0f) return true;
            st.wall_hp.Remove(key);
            int removed = 0;
            bool stoneWall = false;
            for (int dx = -rad; dx <= rad; dx++)
                for (int dy = -rad; dy <= rad; dy++)
                {
                    WorldTile n = World.world.GetTile(best.tile.x + dx, best.tile.y + dy);
                    if (n == null || !n.Type.wall || gateTiles.Contains(n) || !st.plan_tiles.Contains(n)) continue;
                    if (n.Type.id == "wall_order" || n.Type.id == "wall_iron") stoneWall = true;
                    TopTileType after = null;
                    try
                    {
                        string mt = n.main_type != null ? n.main_type.id : "";
                        if (mt == "soil_low") after = TopTileLibrary.grass_low;
                        else if (mt == "soil_high") after = TopTileLibrary.grass_high;
                    }
                    catch (System.Exception) { }
                    MapAction.terraformTop(n, after);
                    removed++;
                }
            Scroll.Note(city, "wall");
            Dbg("wall breached at " + city.name + " near " + best.tile.x + "," + best.tile.y + " by siege guns (" + removed + " tiles)");
            st.breaches.Add(best.tile);
            if (st.breaches.Count > 8) st.breaches.RemoveAt(0);
            try
            {
                EffectsLibrary.spawnAtTileRandomScale("fx_explosion_small", best.tile, 0.18f, 0.26f);
                Sfx.PlayLater("wall_collapse", best.tile, 1.0f);
                Fx.Rubble(best.tile, city.kingdom, city, stoneWall);
            }
            catch (System.Exception) { }
            return true;
        }

        // Снаряд мортиры упал рядом со стеной: стена в радиусе взрыва выбивается (ворота не трогаем: их бьёт счётчик прочности)
        internal static void BlastWalls(WorldTile at, int rad)
        {
            if (at == null) return;
            foreach (KeyValuePair<City, CityState> kv in _states)
            {
                CityState st = kv.Value;
                if (st.plan_tiles.Count == 0) continue;
                HashSet<WorldTile> gateTiles = null;
                for (int dx = -rad; dx <= rad; dx++)
                    for (int dy = -rad; dy <= rad; dy++)
                    {
                        if (dx * dx + dy * dy > rad * rad + 1) continue;
                        WorldTile n = World.world.GetTile(at.x + dx, at.y + dy);
                        if (n == null || !n.Type.wall || !st.plan_tiles.Contains(n)) continue;
                        if (gateTiles == null) { gateTiles = new HashSet<WorldTile>(); foreach (GateTile g in st.gates) gateTiles.Add(g.tile); }
                        if (gateTiles.Contains(n)) continue;
                        TopTileType after = null;
                        try
                        {
                            string mt = n.main_type != null ? n.main_type.id : "";
                            if (mt == "soil_low") after = TopTileLibrary.grass_low;
                            else if (mt == "soil_high") after = TopTileLibrary.grass_high;
                        }
                        catch (System.Exception) { }
                        MapAction.terraformTop(n, after);
                    }
            }
        }

        // Чем больше побиты ворота, тем гуще дым над ними
        private static void GateSmoke(City city, CityState st, double now)
        {
            if (st.gate_hp.Count == 0 || now < st.next_smoke) return;
            st.next_smoke = now + 1.2;
            float max = GateMaxHp(city, st);
            foreach (KeyValuePair<int, float> kv in st.gate_hp)
            {
                float f = kv.Value / max;
                if (f > 0.8f) continue;
                int ring = kv.Key / 4, side = kv.Key % 4;
                List<WorldTile> tiles = new List<WorldTile>();
                foreach (GateTile g in st.gates) if (g.ring == ring && g.side == side && g.closed) tiles.Add(g.tile);
                if (tiles.Count == 0) continue;
                Fx.GateFire(city, kv.Key, f, tiles);
                int puffs = f < 0.3f ? 3 : (f < 0.55f ? 2 : 1);
                for (int i = 0; i < puffs; i++)
                {
                    WorldTile t0 = tiles[Randy.randomInt(0, tiles.Count)];
                    WorldTile t1 = World.world.GetTile(t0.x + Randy.randomInt(-1, 2), t0.y + Randy.randomInt(-1, 2));
                    if (t1 == null) continue;
                    try { EffectsLibrary.spawnAtTile("fx_fire_smoke", t1, Randy.randomFloat(0.1f, f < 0.3f ? 0.22f : 0.16f)); } catch (System.Exception) { }
                }
            }
        }

        // Баррикада из заострённых брёвен за закрытыми воротами, пока город под осадой
        // ---------- Защитники у ворот и пробоин ----------

        private static bool DEF_ON = Cfg.Bool("defenders_gather", true, "во время осады часть воинов города встаёт у обстреливаемых ворот и у пробоин в стене");
        private static int DEF_PCT = Cfg.Int("defenders_percent", 70, "какая доля воинов города получает приказ занять позицию (0-100)");
        private static int DEF_MAX_ORDERS = 60;

        // Точка в нескольких клетках от t в сторону центра города (внутрь стены)
        private static WorldTile InwardTile(CityState st, WorldTile t, int k)
        {
            int dx = st.center.x - t.x, dy = st.center.y - t.y;
            int sx = 0, sy = 0;
            if (System.Math.Abs(dx) >= System.Math.Abs(dy)) sx = dx > 0 ? k : -k; else sy = dy > 0 ? k : -k;
            return World.world.GetTile(t.x + sx, t.y + sy);
        }

        internal static void DefendTick(City city, CityState st, double now)
        {
            if (!DEF_ON || now < st.next_defend || st.center == null) return;
            st.next_defend = now + 5.0;
            try
            {
                List<WorldTile> gatePts = new List<WorldTile>();
                List<WorldTile> breachPts = new List<WorldTile>();
                HashSet<int> sides = new HashSet<int>();
                foreach (KeyValuePair<int, float> kv in st.gate_hp) sides.Add(kv.Key);
                WorldTile camp = Siege.CampCenterFor(city) ?? st.garrison_pos;      // до появления лагеря гарнизон встаёт у стороны, откуда идёт враг
                int fr, fs; WorldTile fat;
                if (camp != null && FocusSide(st, camp, out fr, out fs, out fat)) sides.Add(fr * 4 + fs);
                foreach (int key in sides)
                {
                    int ring = key / 4, side = key % 4;
                    long sx = 0, sy = 0; int n = 0;
                    foreach (GateTile g in st.gates) if (g.ring == ring && g.side == side) { sx += g.tile.x; sy += g.tile.y; n++; }
                    if (n == 0) continue;
                    WorldTile mid = World.world.GetTile((int)(sx / n), (int)(sy / n));
                    WorldTile pt = mid != null ? InwardTile(st, mid, 3) : null;
                    if (pt != null) gatePts.Add(pt);
                }
                // выбитые ворота и пробои в стене
                HashSet<int> brokenSides = new HashSet<int>();
                foreach (GateTile g in st.gates)
                {
                    if (!g.broken || !brokenSides.Add(g.ring * 4 + g.side)) continue;
                    WorldTile pt = InwardTile(st, g.tile, 3);
                    if (pt != null) breachPts.Add(pt);
                }
                for (int i = 0; i < st.breaches.Count; i++)
                {
                    WorldTile pt = InwardTile(st, st.breaches[i], 3);
                    if (pt != null) breachPts.Add(pt);
                }
                if (gatePts.Count + breachPts.Count == 0) return;

                int outer = 0;
                for (int i = 0; i < st.ring_radius.Count; i++) if (st.ring_radius[i] > outer) outer = st.ring_radius[i];
                int sentGate = 0, sentBreach = 0, orders = 0;
                foreach (Actor a in city.getUnits())
                {
                    if (orders >= DEF_MAX_ORDERS) break;
                    if (a == null || !a.isAlive() || !a.isWarrior() || a.isKing() || a.current_tile == null || a.isInsideSomething()) continue;
                    if (a.isFighting()) continue;      // кто уже дерётся, того не отвлекаем
                    WorldTile at = a.current_tile;
                    if (!at.isSameIsland(st.center)) continue;
                    if (System.Math.Max(System.Math.Abs(at.x - st.center.x), System.Math.Abs(at.y - st.center.y)) > outer + 30) continue;   // армия, ушедшая в поход
                    int h = (int)(((a.getID() * 2654435761L) >> 9) & 0x7fffffff);
                    if (h % 100 >= DEF_PCT) continue;
                    // часть воинов к воротам, часть к пробоям (если есть и то и другое)
                    bool toBreach = breachPts.Count > 0 && (gatePts.Count == 0 || ((h >> 7) & 1) == 1);
                    List<WorldTile> pts = toBreach ? breachPts : gatePts;
                    WorldTile pt = pts[(h >> 3) % pts.Count];
                    if (Toolbox.Dist(at.x, at.y, pt.x, pt.y) <= 5f) continue;
                    WorldTile dest = World.world.GetTile(pt.x + Randy.randomInt(-2, 3), pt.y + Randy.randomInt(-2, 3));
                    if (dest == null || dest.Type.block || dest.is_liquid || dest.Type.wall || !dest.isSameIsland(at)) continue;
                    a.cancelAllBeh();
                    a.goTo(dest);
                    orders++;
                    if (toBreach) sentBreach++; else sentGate++;
                }
                if (orders > 0 && now >= st.next_def_log)
                {
                    st.next_def_log = now + 30.0;
                    Dbg("defenders of " + city.name + ": " + sentGate + " sent to the gates, " + sentBreach + " to the breaches");
                }
            }
            catch (System.Exception e) { Dbg("defend failed: " + e.Message); }
        }

        // Для тарана: закрытые ворота, ближайшие к точке from, и клетка снаружи у них
        internal static bool GateApproach(City city, WorldTile from, out WorldTile gate, out WorldTile stand)
        {
            gate = null; stand = null;
            CityState st;
            if (city == null || from == null || !_states.TryGetValue(city, out st) || !st.gates_closed || st.center == null) return false;
            int ring, side;
            if (!FocusSide(st, from, out ring, out side, out gate)) return false;
            int dx = gate.x - st.center.x, dy = gate.y - st.center.y;
            int sx = 0, sy = 0;
            if (System.Math.Abs(dx) >= System.Math.Abs(dy)) sx = dx > 0 ? 2 : -2; else sy = dy > 0 ? 2 : -2;
            stand = World.world.GetTile(gate.x + sx, gate.y + sy);
            // внешние ворота уже выбиты, а таран ещё снаружи: сначала проходим через пролом, потом идём к внутренним воротам
            int maxRing = -1;
            foreach (GateTile g0 in st.gates) if (g0.ring > maxRing) maxRing = g0.ring;
            if (ring >= 0 && ring < maxRing && maxRing < st.ring_radius.Count)
            {
                int cheb = System.Math.Max(System.Math.Abs(from.x - st.center.x), System.Math.Abs(from.y - st.center.y));
                if (cheb > st.ring_radius[maxRing])
                {
                    float bd = float.MaxValue; WorldTile bt = null;
                    foreach (GateTile g1 in st.gates)
                    {
                        if (g1.ring != maxRing || !g1.broken || g1.tile == null) continue;
                        float d = Toolbox.Dist(g1.tile.x, g1.tile.y, from.x, from.y);
                        if (d < bd) { bd = d; bt = g1.tile; }
                    }
                    if (bt != null) stand = bt;
                }
            }
            return stand != null;
        }

        // Удар тарана по воротам: часть прочности ворот (fraction от максимума)
        internal static void GateRamHit(City city, WorldTile ram, float fraction)
        {
            Scroll.Note(city, "ram");
            CityState st;
            if (city == null || ram == null || !_states.TryGetValue(city, out st) || !st.gates_closed) return;
            GateDamage(city, ram, GateMaxHp(city, st) * fraction / (Traits.HasCity(city, Traits.IRON) ? IRON_MULT : 1f));   // урон тарана считается от прочности без черты, так что укреплённые ворота он выбивает в полтора раза дольше
        }

        internal static bool GatesClosed(City city)
        {
            CityState st;
            return city != null && _states.TryGetValue(city, out st) && st.gates_closed;
        }

        private static void HandleBarricades(City city, CityState st, double now)
        {
            if (now < st.next_barricade) return;
            st.next_barricade = now + 3.0;
            if (Scroll.Is3D())                                  // в 3D знамёна у баррикад не нужны
                for (int bi = 8; bi < 16 && bi < st.barricades.Length; bi++)
                    if (st.barricades[bi] != null) { try { if (st.barricades[bi].isAlive()) st.barricades[bi].startDestroyBuilding(); } catch (System.Exception) { } st.barricades[bi] = null; }
            if (!st.gates_closed || !Siege.UnderSiege(city)) return;
            for (int ring = 0; ring < st.ring_radius.Count && ring < 2; ring++)
            {
                int R = st.ring_radius[ring];
                for (int side = 0; side < 4; side++)
                {
                    int k = ring * 4 + side;
                    Building cur = st.barricades[k];
                    if (cur != null && cur.isAlive()) continue;
                    bool isSealed = false;
                    foreach (GateTile g in st.gates) if (g.ring == ring && g.side == side && g.closed && !g.broken) { isSealed = true; break; }
                    if (!isSealed) continue;
                    int x = st.center.x, y = st.center.y;
                    if (side == 0) y = st.center.y - R + 2;
                    else if (side == 1) y = st.center.y + R - 2;
                    else if (side == 2) x = st.center.x - R + 2;
                    else x = st.center.x + R - 2;
                    WorldTile t = null;
                    int[] shifts = new int[] { 0, 1, -1, 2, -2, 3, -3, 4, -4 };
                    for (int si = 0; si < shifts.Length && t == null; si++)
                    {
                        WorldTile cand = Siege.FreeSpot(World.world.GetTile(side < 2 ? x + shifts[si] : x, side < 2 ? y : y + shifts[si]), 1);
                        if (cand != null && !Siege.TowerNear(cand, 1)) t = cand;
                    }
                    if (t == null)
                    {
                        if (now >= st.next_barricade_log) { st.next_barricade_log = now + 60.0; Dbg("no free spot for a gate barricade at " + city.name + ", side " + side); }
                        continue;
                    }
                    Building b = Siege.Put(side == 2 ? Siege.BARRICADE_L_ID : side == 3 ? Siege.BARRICADE_R_ID : Siege.BARRICADE_ID, t, city.kingdom);
                    st.barricades[k] = b;
                    // знамя города рядом с баррикадой: сразу видно, какая сторона держит оборону
                    WorldTile ft = Siege.FreeSpot(World.world.GetTile(side < 2 ? t.x + 4 : t.x, side < 2 ? t.y : t.y + 4), 1);
                    if (ft != null && !Scroll.Is3D() && (st.barricades[k + 8] == null || !st.barricades[k + 8].isAlive())) st.barricades[k + 8] = Siege.Put(Siege.BANNER_S_ID, ft, city.kingdom);
                    return;      // не больше одной за раз
                }
            }
        }

        // Ворота закрываются стеной, пока рядом вражеское войско или идёт осада, и открываются через GATE_OPEN_SECONDS
        private static int gate_revert_logged, gate_block_logged;
        private static void OpenAllGates(City city, CityState st)
        {
            Dbg("gates opening at " + city.name);
            Sfx.Play("gate_open", st.gates.Count > 0 && st.gates[0].tile != null ? st.gates[0].tile : st.center);
            st.gates_closed = false;
            foreach (GateTile g in st.gates) g.broken = false;
            st.gate_hp.Clear();
            st.wall_hp.Clear();
            st.breaches.Clear();
            ClearBarricades(st);
        }

        private static void HandleGates(City city, CityState st, WallStyle style)
        {
            if (st.rings.Count == 0 || st.ring_radius.Count == 0) return;

            while (st.gates_rings < st.rings.Count && st.gates_rings < st.ring_radius.Count)
            {
                int R = st.ring_radius[st.gates_rings];
                for (int side = 0; side < 4; side++)
                {
                    if (side == st.nogate) continue;      // здесь стена без проёма
                    for (int s = 0; s <= 1; s++)
                    {
                        int x, y;
                        RingPoint(st.center, R, side, s, out x, out y);
                        WorldTile t = World.world.GetTile(x, y);
                        if (t == null || t.is_liquid) continue;
                        if (st.plan_tiles.Contains(t)) continue;   // здесь по плану обычная стена (сторона без ворот), это не проход
                        GateTile g = new GateTile();
                        g.tile = t;
                        g.ring = st.gates_rings;
                        g.side = side;
                        // после загрузки сохранения ворота могли остаться закрытыми (железная стена вне плана колец):
                        // принимаем их как закрытые, тогда при открытии они вернутся в проход
                        if (t.top_type == TopTileLibrary.wall_iron && !st.plan_tiles.Contains(t))
                        {
                            g.closed = true;
                            g.prev = null;
                        }
                        st.gates.Add(g);
                    }
                }
                st.gates_rings++;
            }

            double now = World.world.getCurWorldTime();
            if (now >= st.next_gate_check)
            {
                st.next_gate_check = now + 2.0;
                int level = ThreatLevel(city, st);
                if (level >= 2)
                {
                    if (!st.gates_closed)
                    {
                        Scroll.Note(city, "closed");
                        Dbg("alert level 2 at " + city.name + ": gates closing (enemy warriors: " + st.last_enemy_count + (st.patrol ? ", was patrol alert" : "") + ")");
                        st.closed_since = now;
                        st.pressure = 0;
                        WorldTile sfxAt = st.gates.Count > 0 && st.gates[0].tile != null ? st.gates[0].tile : st.center;
                        Sfx.Play("gate_close", sfxAt);
                    }
                    st.patrol = false;
                    st.last_threat = now;
                    st.last_level2 = now;
                    st.gates_closed = true;
                    // враг у стен давит на ворота; если он не уходит, ворота в итоге выбивают, чтобы осада не «зависала»
                    st.pressure += System.Math.Min(st.near_count, GATE_CROWD_CAP) * GATE_PER_WARRIOR;
                    float mult = st.is_capital ? 2f : 1f;
                    if (st.last_enemy_count > 0 && st.near_count >= 3 && now - st.last_breach >= GATE_BREACH_GAP
                        && (st.pressure >= GATE_BREACH_PRESSURE * mult || now - st.closed_since >= GATE_BREACH_SECONDS * mult))
                    {
                        BreachGate(city, st, now);
                        st.last_breach = now;
                    }
                }
                else
                {
                    if (level == 1) st.last_threat = now;
                    if (st.gates_closed)
                    {
                        // тревога снижается по ступеням: сначала полное запирание сменяется патрульным, потом ворота открываются
                        if (now - st.last_level2 > GATE_STEP_SECONDS)
                        {
                            OpenAllGates(city, st);
                            st.garrison_pos = null;
                            if (level == 1 || now - st.last_level2 <= GATE_STEP_SECONDS + GATE_OPEN_SECONDS * 2)
                            {
                                st.patrol = true;
                                st.patrol_open_side = PickOpenSide(st);
                                st.last_threat = now;
                                Dbg("alert level 1 at " + city.name + ": gates open again except the side " + st.patrol_open_side + " stays free");
                            }
                        }
                    }
                    else if (level == 1)
                    {
                        if (!st.patrol)
                        {
                            st.patrol = true;
                            st.patrol_open_side = PickOpenSide(st);
                            Dbg("alert level 1 at " + city.name + " (enemy warriors: " + st.last_enemy_count + "): all gates closed except side " + st.patrol_open_side);
                            Sfx.Play("gate_close", st.gates.Count > 0 && st.gates[0].tile != null ? st.gates[0].tile : st.center);
                        }
                    }
                    else if (st.patrol && now - st.last_threat > GATE_OPEN_SECONDS)
                    {
                        st.patrol = false;
                        st.patrol_open_side = -1;
                        Dbg("alert level 0 at " + city.name + ": all gates open");
                        Sfx.Play("gate_open", st.gates.Count > 0 && st.gates[0].tile != null ? st.gates[0].tile : st.center);
                    }
                }
            }

            try { Fx.AlertFlag(city, st.center, ALERT_ON ? (st.gates_closed ? 2 : (st.patrol ? 1 : 0)) : -1, now); } catch (System.Exception) { }
            GateDiag(city, st, now);
            GateSmoke(city, st, now);
            HandleBarricades(city, st, now);
            if (st.gates_closed && (Siege.UnderSiege(city) || st.garrison_pos != null)) DefendTick(city, st, now);

            int changed = 0;
            int blocked = 0;
            foreach (GateTile g in st.gates)
            {
                if (changed >= GATE_PER_TICK) break;
                WorldTile t = g.tile;
                if (st.gates_closed || (st.patrol && g.side != st.patrol_open_side))
                {
                    if (g.closed)
                    {
                        if (!t.Type.wall)
                        {
                            g.closed = false; g.broken = true;   // ворота выбили - пока угроза, заново не ставим
                            if (gate_revert_logged++ < 25 && !st.gate_hp.ContainsKey(g.ring * 4 + g.side))
                                Dbg("gate tile " + t.x + "," + t.y + " of " + city.name + " stopped being a wall without damage (now " + (t.Type != null ? t.Type.id : "?") + ")");
                        }
                        continue;
                    }
                    if (t.hasUnits() || t.hasBuilding()) blocked++;
                    if (g.broken || t.Type.wall) continue;
                    if (t.hasBuilding())
                    {
                        bool oldTower = t.building.asset != null && t.building.asset.id.StartsWith("watch_tower") && !Siege.IsSiegeId(t.building.asset.id);   // башня из старого плана, накрывающая проём
                        if (Clearable(t.building) || t.building.asset.building_type == BuildingType.Building_Wheat || (t.building.asset != null && !t.building.asset.city_building) || oldTower)
                        {
                            if (gate_block_logged++ < 25) Dbg("gate tile " + t.x + "," + t.y + " of " + city.name + ": removing " + t.building.asset.id + " that was in the way");
                            t.building.startDestroyBuilding();   // постройку в проёме (бочка, забор, обломки) убираем, ворота закроются на следующем шаге
                        }
                        continue;
                    }
                    if (t.hasUnits())
                    {
                        // в проёме идёт поток юнитов: ждём пару секунд, потом закрываем всё равно, а стоящих в проёме отталкиваем
                        if (g.blocked_since <= 0) { g.blocked_since = now; continue; }
                        if (now - g.blocked_since < GATE_FORCE_SECONDS) continue;
                        g.prev = t.top_type;
                        MapAction.terraformTop(t, TopTileLibrary.wall_iron);
                        g.closed = true;
                        g.blocked_since = 0;
                        changed++;
                        try
                        {
                            t.doUnits(new System.Action<Actor>(delegate (Actor a) { a.cancelAllBeh(); a.applyRandomForce(); }));
                        }
                        catch (System.Exception) { }
                        continue;
                    }
                    g.blocked_since = 0;
                    g.prev = t.top_type;
                    MapAction.terraformTop(t, TopTileLibrary.wall_iron);   // закрытые ворота - железные, их видно среди обычной стены
                    g.closed = true;
                    changed++;
                }
                else if (g.closed)
                {
                    if (t.Type.wall) { MapAction.terraformTop(t, g.prev); changed++; }
                    g.closed = false;
                }
            }
            if (st.gates_closed && now - st.closed_since > 20.0 && now >= st.next_open_log)
            {
                st.next_open_log = now + 40.0;
                foreach (GateTile g in st.gates)
                {
                    if (g.closed || g.broken || g.tile.Type.wall) continue;
                    WorldTile gt = g.tile;
                    Dbg("gate tile " + gt.x + "," + gt.y + " of " + city.name + " is still open " + (int)(now - st.closed_since) + " s after closing: type " + (gt.Type != null ? gt.Type.id : "?")
                        + ", units " + gt.hasUnits() + ", building " + (gt.hasBuilding() ? gt.building.asset.id : "none"));
                }
            }
            if (blocked > 0 && now >= st.next_block_log)
            {
                st.next_block_log = now + 15.0;
                Dbg("gates at " + city.name + ": " + blocked + " tiles blocked by units/buildings (enemies near: " + st.last_enemy_count + ")");
            }
        }

        private static void HandlePlaza(City city, CityState st, WorldTile center)
        {
            WallStyle style = GetStyle(city);
            if (!Traits.Allows(city, Traits.PAVING, style.arch == "human" || style.arch == "dwarf")) return;

            if (st.plaza_center != center)
            {
                st.plaza_center = center;
                st.plaza_planned = 0;
                st.plaza_queue.Clear();
            }

            if (st.plaza_variant < 0)
            {
                long id = city.getID();
                st.plaza_seed = (int)(id & 0x7fffffff);
                int pick = (int)((id * 2654435761L >> 7) % 100);
                if (pick < 0) pick = -pick;
                int roomR = (st.ring_radius.Count > 0 ? st.ring_radius[0] : CAPITAL_RADII[0]) - PLAZA_MARGIN;
                int v;
                // крупная площадь: роза ветров или крест с рамкой, редко мозаика; кирпичики и шахматка только для тесных столиц
                if (roomR <= 6) v = pick % 4;
                else v = pick < 45 ? 2 : (pick < 90 ? 3 : 4);
                st.plaza_variant = PLAZA_RANDOM ? v : 0;
                Dbg("plaza pattern " + st.plaza_variant + " for " + city.name);
            }

            int pop = city.status.population;
            int target = 0;
            for (int i = 0; i < PLAZA_POP.Length; i++)
                if (pop >= PLAZA_POP[i]) target = PLAZA_RADII[i];
            int inner = st.ring_radius.Count > 0 ? st.ring_radius[0] : CAPITAL_RADII[0];
            target = System.Math.Min(target, inner - PLAZA_MARGIN);

            if (target > st.plaza_planned)
            {
                // новые клетки по расстоянию от центра, изнутри наружу
                for (int d = st.plaza_planned + 1; d <= target; d++)
                {
                    for (int dx = -d; dx <= d; dx++)
                    {
                        for (int dy = -d; dy <= d; dy++)
                        {
                            if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != d) continue;
                            if (!PlazaPatternV(st.plaza_variant, st.plaza_seed, dx, dy, d, target)) continue;
                            WorldTile t = World.world.GetTile(center.x + dx, center.y + dy);
                            if (t != null) st.plaza_queue.Add(t);
                        }
                    }
                }
                st.plaza_planned = target;
                Dbg("plaza planned: radius " + target + ", queue " + st.plaza_queue.Count);
            }

            int placed = 0;
            while (st.plaza_queue.Count > 0 && placed < PLAZA_PER_TICK)
            {
                WorldTile t = st.plaza_queue[0];
                st.plaza_queue.RemoveAt(0);
                if (t.is_liquid || t.Type.wall || t.Type.road || t.Type.id == "field") continue;
                if (!Allowed(t, city)) continue;
                if (t.hasBuilding())
                {
                    if (IsCiv(t)) continue;
                    if (PLAZA_CLEAR && Clearable(t.building)) t.building.startDestroyBuilding();
                    else continue;
                }
                MapAction.terraformTop(t, TopTileLibrary.road);
                placed++;
            }
        }

        // ---------- Ров ----------

        // Ров, который уже вырыт (например, до загрузки сохранения): кольцо воды вокруг стены в 1-10 клетках от неё
        private static int DetectMoat(WorldTile c, int wallR)
        {
            int best = -1;
            float bestFrac = 0f;
            for (int R = wallR + 1; R <= wallR + 10; R++)
            {
                int total = 0, water = 0, okSides = 0;
                for (int side = 0; side < 4; side++)
                {
                    int from = (side < 2) ? -R : -R + 1;
                    int to = (side < 2) ? R : R - 1;
                    int sideTotal = 0, sideWater = 0;
                    for (int sq = from; sq <= to; sq++)
                    {
                        if (sq >= -2 && sq <= 3) continue;   // у ворот ров прерывается
                        int x, y;
                        RingPoint(c, R, side, sq, out x, out y);
                        WorldTile t = World.world.GetTile(x, y);
                        if (t == null) continue;
                        total++; sideTotal++;
                        if (IsOceanTile(t)) { water++; sideWater++; }
                    }
                    if (sideTotal > 0 && sideWater >= sideTotal * 0.08f) okSides++;
                }
                if (total == 0 || okSides < 3) continue;
                float frac = (float)water / total;
                if (frac >= 0.2f && frac > bestFrac) { bestFrac = frac; best = R; }
            }
            return best;
        }

        private static void HandleMoat(City city, CityState st)
        {
            if (st.rings.Count == 0 || st.ring_radius.Count == 0) return;

            if (!st.moat_planned)
            {
                if (RingProgress(city, st.rings[0]) < MOAT_AFTER_WALLS) return;
                st.moat_planned = true;

                int Rm = st.ring_radius[0] + MOAT_OFFSET;
                WorldTile c = st.center;
                int det = DetectMoat(c, st.ring_radius[0]);
                if (det > 0 && det != Rm)
                {
                    Dbg("moat of " + city.name + ": adopted existing moat, radius " + det + " (planned " + Rm + ")");
                    Rm = det;
                }
                st.moat_r = Rm;
                for (int side = 0; side < 4; side++)
                {
                    int from = (side < 2) ? -Rm : -Rm + 1;
                    int to = (side < 2) ? Rm : Rm - 1;
                    for (int s = from; s <= to; s++)
                    {
                        if (s >= -2 && s <= 3) continue; // у ворот ров прерывается (мост)
                        if (MOAT_BRIDGE_STEP > 0 && System.Math.Abs(s) < Rm - 1 && (((s % MOAT_BRIDGE_STEP) + MOAT_BRIDGE_STEP) % MOAT_BRIDGE_STEP) < 2) continue;   // частые сухие проходы
                        int x, y;
                        RingPoint(c, Rm, side, s, out x, out y);
                        WorldTile t = World.world.GetTile(x, y);
                        if (t != null) st.moat_tiles.Add(t);
                    }
                }
                Dbg("moat planned: " + st.moat_tiles.Count + " tiles");
                return;
            }

            int placed = 0;
            for (int i = 0; i < st.moat_tiles.Count && placed < MOAT_PER_TICK; i++)
            {
                WorldTile t = st.moat_tiles[i];
                if (t.is_liquid || t.Type.wall || t.Type.road || IsCiv(t) || !Allowed(t, city))
                {
                    st.moat_tiles.RemoveAt(i);
                    i--;
                    continue;
                }
                if (t.hasUnits()) continue; // не топим юнитов, вернёмся позже

                MapAction.terraformMain(t, TileLibrary.shallow_waters);
                st.moat_tiles.RemoveAt(i);
                i--;
                placed++;
            }
        }

        // ---------- Застрявшие капитаны армий ----------

        internal static void SetUnstick(bool v) { ARMY_UNSTICK = v; }
        private static bool ARMY_UNSTICK = Cfg.Bool("army_unstick", true, "подталкивать капитанов армий, застрявших у стен");
        private static float ARMY_STUCK_SECONDS = Cfg.Float("army_stuck_seconds", 30f, "сколько секунд капитан стоит у стены на месте, прежде чем его подтолкнут");

        private class CapInfo
        {
            public WorldTile tile;
            public double since;
            public double last_kick;
        }
        private static readonly Dictionary<Actor, CapInfo> _caps = new Dictionary<Actor, CapInfo>();
        private static double _next_army_check;

        private static bool WallNear(WorldTile t, int r)
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    WorldTile n = World.world.GetTile(t.x + dx, t.y + dy);
                    if (n != null && n.Type.wall) return true;
                }
            return false;
        }

        private static bool ARMY_STUCK_LOG = Cfg.Bool("army_stuck_log", false, "подробно писать в лог, почему застрял капитан (задание, цель, расстояние до стены)");

        private static int WallDist(WorldTile t, int r)
        {
            int best = int.MaxValue;
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    WorldTile n = World.world.GetTile(t.x + dx, t.y + dy);
                    if (n == null || !n.Type.wall) continue;
                    int d = System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy));
                    if (d < best) best = d;
                }
            return best == int.MaxValue ? -1 : best;
        }

        // Диагностика зависшего капитана: что он делает, куда идёт и что вокруг
        private static void LogStuckCaptain(Army army, Actor cap, WorldTile t, string reason)
        {
            try
            {
                string task = "none";
                try { if (cap.hasTask()) task = cap.getTaskText(); } catch (System.Exception) { }
                City home = army.getCity();
                City target = home != null ? home.target_attack_city : null;
                string tgt = "none";
                if (target != null && target.getTile() != null)
                    tgt = target.name + " dist " + (int)Toolbox.Dist(t.x, t.y, target.getTile().x, target.getTile().y);
                int idle = 0, total = 0;
                List<Actor> list = army.units;
                for (int i = 0; i < list.Count; i++)
                {
                    Actor a = list[i];
                    if (a == null || !a.isAlive()) continue;
                    total++;
                    if (!a.is_moving && !a.isFighting()) idle++;
                }
                bool inLand = t.Type.block || t.is_liquid;
                Dbg("army stuck (" + reason + "): " + army.name + " captain at " + t.x + "," + t.y + ", task=" + task + ", home=" + (home != null ? home.name : "none") +
                    ", target=" + tgt + ", wall dist=" + WallDist(t, 8) + ", on blocked/liquid tile=" + inLand +
                    ", idle units " + idle + "/" + total + ", tile on gate zone=" + (IsGateZone(t) ? "yes" : "no"));
            }
            catch (System.Exception e) { Log("stuck log failed: " + e.Message); }
        }

        // Стоит ли клетка в проходе ворот или выступа (вблизи любой клетки ворот любого укреплённого города)
        private static bool IsGateZone(WorldTile t)
        {
            foreach (KeyValuePair<City, CityState> kv in _states)
                foreach (GateTile g in kv.Value.gates)
                    if (g.tile != null && System.Math.Abs(g.tile.x - t.x) <= 4 && System.Math.Abs(g.tile.y - t.y) <= 4) return true;
            return false;
        }

        // Сколько секунд длится текущая задача юнита: из текста статуса вида "Поёт <color=#7D7D7D>18m</color>"
        private static float ARMY_MEMBER_SECONDS = Cfg.Float("army_member_seconds", 90f, "сколько секунд боец армии стоит на одном месте, прежде чем его подтолкнут");

        // Рядовые бойцы армии: стоят на месте (атакуют стену, ждут у рва), хотя армия идёт дальше
        private static int MembersTick(Army army, Actor cap, double now)
        {
            City home = army.getCity();
            bool attacking = home != null && home.target_attack_city != null;
            if (!attacking) return 0;
            if (Siege.IsHolding(home)) return 0;
            int kicked = 0;
            List<Actor> list = army.units;
            for (int i = 0; i < list.Count && kicked < 8; i++)
            {
                Actor a = list[i];
                if (a == null || a == cap || !a.isAlive() || a.current_tile == null) continue;
                CapInfo ci;
                if (!_caps.TryGetValue(a, out ci)) { ci = new CapInfo(); _caps[a] = ci; }
                WorldTile t = a.current_tile;
                bool sleeping = false;
                try { sleeping = a.isLying(); } catch (System.Exception) { }
                if (ci.tile != t || a.is_moving || sleeping || a.is_inside_boat)
                {
                    ci.tile = t;
                    ci.since = now;
                    continue;
                }
                double still = now - ci.since;
                if (still < ARMY_MEMBER_SECONDS || now - ci.last_kick < ARMY_MEMBER_SECONDS) continue;
                string task = null;
                double taskSec = 0;
                try { if (a.hasTask()) { task = a.getTaskText(); taskSec = TaskSeconds(task); } } catch (System.Exception) { }
                bool nearWall = WallNear(t, 6);
                if (!(nearWall || taskSec >= ARMY_TASK_SECONDS)) continue;
                if (ARMY_STUCK_LOG) Dbg("army unit kicked: " + (task ?? "?") + " still " + (int)still + "s at wall=" + nearWall);
                a.cancelAllBeh();
                a.applyRandomForce();
                ci.last_kick = now;
                ci.since = now;
                kicked++;
            }
            return kicked;
        }

        private static double TaskSeconds(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int end = text.LastIndexOf("</color>");
            if (end < 2) return 0;
            char unit = text[end - 1];
            int i = end - 2;
            while (i >= 0 && char.IsDigit(text[i])) i--;
            string num = text.Substring(i + 1, end - 2 - i);
            int v;
            if (num.Length == 0 || !int.TryParse(num, out v)) return 0;
            if (unit == 's') return v;
            if (unit == 'm') return v * 60.0;
            if (unit == 'h') return v * 3600.0;
            return 0;
        }

        private static float HUNG_TASK_SECONDS = Cfg.Float("hung_task_seconds", 60f, "сколько секунд юнит у стены может висеть на одной и той же задаче, прежде чем его подтолкнут");
        private static float ARMY_TASK_SECONDS = Cfg.Float("army_task_seconds", 60f, "сколько секунд капитан может повисеть на одной и той же мирной задаче (поёт, плачет...), прежде чем её сбросят");

        // Капитаны армий иногда «зависают» двумя способами:
        //  1) армия идёт в атаку, а капитан стоит у стены (путь не находится, стены дробят карту на области);
        //  2) капитан застрял в мирной задаче (поёт, плачет, ругается...): он выбрал целью соседа за стеной или рвом
        //     и никогда не дойдёт. Армия ходит за капитаном, поэтому стоит вся.
        // Первое лечим, если у города армии есть цель атаки и рядом стена. Второе - по длительности задачи.
        internal static void ArmyTick()
        {
            if (!ARMY_UNSTICK) return;
            double now = World.world.getCurWorldTime();
            if (now < _next_army_check) return;
            _next_army_check = now + 5.0;

            int kicked = 0;
            foreach (Army army in World.world.armies)
            {
                if (army == null || !army.isAlive()) continue;
                Actor cap = army.getCaptain();
                if (cap == null || !cap.isAlive() || cap.current_tile == null) continue;

                MembersTick(army, cap, now);

                CapInfo ci;
                if (!_caps.TryGetValue(cap, out ci)) { ci = new CapInfo(); _caps[cap] = ci; }
                WorldTile t = cap.current_tile;
                City homeC = army.getCity();
                if (homeC != null && Siege.IsHolding(homeC)) { ci.tile = t; ci.since = now; continue; }   // осада: армия стоит у стен намеренно
                if (ci.tile != t || cap.is_moving || cap.isFighting() || cap.is_inside_boat)
                {
                    ci.tile = t;
                    ci.since = now;
                    continue;
                }
                double still = now - ci.since;
                if (still < ARMY_STUCK_SECONDS || now - ci.last_kick < ARMY_STUCK_SECONDS) continue;

                City home = army.getCity();
                bool attacking = home != null && home.target_attack_city != null;
                string task = null;
                double taskSec = 0;
                try { if (cap.hasTask()) { task = cap.getTaskText(); taskSec = TaskSeconds(task); } } catch (System.Exception) { }
                bool sleeping = false;
                try { sleeping = cap.isLying(); } catch (System.Exception) { }

                string reason = null;
                if (attacking && WallNear(t, 6)) reason = "blocked on the way to the target";
                else if (!sleeping && taskSec >= ARMY_TASK_SECONDS && still >= ARMY_TASK_SECONDS) reason = "hung on one task";
                if (reason == null) continue;

                if (ARMY_STUCK_LOG) LogStuckCaptain(army, cap, t, reason);
                cap.cancelAllBeh();
                cap.applyRandomForce();
                kicked++;
                // и тех бойцов армии, что стоят рядом с ним (только когда армия идёт в атаку)
                if (attacking)
                {
                    List<Actor> list = army.units;
                    int mates = 0;
                    for (int i = 0; i < list.Count && mates < 12; i++)
                    {
                        Actor a = list[i];
                        if (a == null || a == cap || !a.isAlive() || a.current_tile == null || a.is_moving || a.isFighting()) continue;
                        if (System.Math.Abs(a.current_tile.x - t.x) > 5 || System.Math.Abs(a.current_tile.y - t.y) > 5) continue;
                        a.cancelAllBeh();
                        a.applyRandomForce();
                        mates++;
                    }
                }
                ci.last_kick = now;
                ci.since = now;
            }
            if (kicked > 0) Dbg("army captains kicked: " + kicked);

            if (_caps.Count > 3000)
            {
                List<Actor> gone = new List<Actor>();
                foreach (KeyValuePair<Actor, CapInfo> kv in _caps) if (kv.Key == null || !kv.Key.isAlive()) gone.Add(kv.Key);
                foreach (Actor a in gone) if (a != null) _caps.Remove(a);
            }
        }

        // ---------- Застрявшие юниты ----------

        private static void Unstuck(WorldTile center, int chunkRadius)
        {
            List<Actor> units = new List<Actor>();
            foreach (Actor a in Finder.getUnitsFromChunk(center, chunkRadius)) units.Add(a);

            int kicked = 0;
            foreach (Actor a in units)
            {
                if (a == null || !a.isAlive()) continue;
                WorldTile t = a.current_tile;
                if (t == null) continue;

                bool nearWall = t.Type.block || t.hasWallsAround();
                // юнит чуть дальше от стены (до 5 клеток) тоже считается застрявшим, если очень долго занят одной и той же задачей на месте
                bool far = false;
                if (!nearWall && !a.is_moving && !a.is_inside_boat && WallNear(t, 5)) far = true;
                if ((!nearWall && !far) || a.is_moving)
                {
                    _stuck.Remove(a);
                    continue;
                }
                if (far)
                {
                    bool lying = false; double ts = 0;
                    try { lying = a.isLying(); if (a.hasTask()) ts = TaskSeconds(a.getTaskText()); } catch (System.Exception) { }
                    if (lying || ts < HUNG_TASK_SECONDS) { _stuck.Remove(a); continue; }
                }

                StuckInfo info;
                if (!_stuck.TryGetValue(a, out info))
                {
                    info = new StuckInfo();
                    _stuck[a] = info;
                }
                if (info.tile == t) info.count++;
                else { info.tile = t; info.count = 0; }

                if (info.count >= STUCK_CHECKS)
                {
                    a.cancelAllBeh();
                    a.applyRandomForce();
                    info.count = 0;
                    kicked++;
                }
            }

            if (kicked > 0) Dbg("unstuck: " + kicked + " units");

            if (_stuck.Count > 2000)
            {
                List<Actor> dead = new List<Actor>();
                foreach (KeyValuePair<Actor, StuckInfo> kv in _stuck)
                {
                    if (kv.Key == null || !kv.Key.isAlive()) dead.Add(kv.Key);
                }
                foreach (Actor d in dead) _stuck.Remove(d);
            }
        }
    }

    public class TileChange
    {
        public WorldTile tile;
        public TopTileType prev;
        public TopTileType placed;
    }

    // Настройки мода: текстовый файл "ключ = значение" в папке данных игры (создаётся при первом запуске)
    internal static class Cfg
    {
        private static Dictionary<string, string> loaded;
        private static readonly List<string[]> used = new List<string[]>();   // ключ, значение, описание

        private static string FilePath()
        {
            return System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "Mymod_settings.txt");
        }

        private static void Load()
        {
            if (loaded != null) return;
            loaded = new Dictionary<string, string>();
            try
            {
                string path = FilePath();
                if (!System.IO.File.Exists(path)) return;
                foreach (string raw in System.IO.File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#') continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    loaded[line.Substring(0, eq).Trim().ToLowerInvariant()] = line.Substring(eq + 1).Trim();
                }
            }
            catch (System.Exception) { }
            // значения, сохранённые прежними версиями как значение по умолчанию и признанные неудачными: берём новое по умолчанию
            string old;
            if (loaded.TryGetValue("gate_army_range", out old) && old == "80") loaded.Remove("gate_army_range");
            if (loaded.TryGetValue("gate_hold_per_warrior", out old) && (old == "1" || old == "1.0")) loaded.Remove("gate_hold_per_warrior");
            if (loaded.TryGetValue("gate_clear_length", out old) && old == "8") loaded.Remove("gate_clear_length");
            if (loaded.TryGetValue("gate_clear_half_width", out old) && old == "4") loaded.Remove("gate_clear_half_width");
            if (loaded.TryGetValue("gate_diag", out old) && old == "true") loaded.Remove("gate_diag");      // диагностика ворот и замер времени теперь выключены по умолчанию
            if (loaded.TryGetValue("perf_log", out old) && old == "true") loaded.Remove("perf_log");
            if (loaded.TryGetValue("siege_shot_log", out old) && old == "true") loaded.Remove("siege_shot_log");   // выстрелы орудий в лог теперь только по желанию
            if (loaded.TryGetValue("army_stuck_log", out old) && old == "true") loaded.Remove("army_stuck_log");
            if (loaded.TryGetValue("army_task_seconds", out old) && old == "120") loaded.Remove("army_task_seconds");     // новые умолчания 1.2.0
            if (loaded.TryGetValue("army_stuck_seconds", out old) && old == "45") loaded.Remove("army_stuck_seconds");
            if (loaded.TryGetValue("siege_gun_max_range", out old) && old == "45") loaded.Remove("siege_gun_max_range");   // дальность орудий увеличена на 25%
            // значения тестовых сборок 1.2, сохранённые как умолчания
            if (loaded.TryGetValue("mortar_blast", out old) && old == "900") loaded.Remove("mortar_blast");
            if (loaded.TryGetValue("mortar_reload", out old) && old == "16") loaded.Remove("mortar_reload");
            if (loaded.TryGetValue("mortar_crater_radius", out old) && (old == "3" || old == "2")) loaded.Remove("mortar_crater_radius");
            if (loaded.TryGetValue("siege_ram_chance", out old) && old == "70") loaded.Remove("siege_ram_chance");
            if (loaded.TryGetValue("siege_ram_regen", out old) && old == "0.02") loaded.Remove("siege_ram_regen");
        }

        private static string Get(string key, string def, string desc)
        {
            Load();
            string v;
            if (!loaded.TryGetValue(key, out v)) v = def;
            used.Add(new string[] { key, v, desc });
            return v;
        }

        public static int Int(string key, int def, string desc)
        {
            int r;
            return int.TryParse(Get(key, def.ToString(), desc), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out r) ? r : def;
        }

        public static float Float(string key, float def, string desc)
        {
            float r;
            return float.TryParse(Get(key, def.ToString(System.Globalization.CultureInfo.InvariantCulture), desc),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out r) ? r : def;
        }

        public static bool Bool(string key, bool def, string desc)
        {
            string v = Get(key, def ? "true" : "false", desc).ToLowerInvariant();
            if (v == "true" || v == "1" || v == "yes" || v == "да") return true;
            if (v == "false" || v == "0" || v == "no" || v == "нет") return false;
            return def;
        }

        // Значение из окна настроек игры: записываем в файл, действует со следующего запуска
        public static void Set(string key, string value)
        {
            Load();
            loaded[key] = value;
            bool found = false;
            foreach (string[] u in used)
                if (u[0] == key) { u[1] = value; found = true; }
            if (!found) used.Add(new string[] { key, value, "(из окна настроек игры)" });
            Save();
        }

        // Переписываем файл со всеми настройками (так новые пункты появляются сами, а ваши значения сохраняются)
        public static string Save()
        {
            string path = FilePath();
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("# Mymod: настройки. Формат: ключ = значение. Изменения применяются при следующем запуске игры.");
                sb.AppendLine("# Чтобы вернуть значения по умолчанию, удалите этот файл.");
                sb.AppendLine();
                foreach (string[] u in used)
                {
                    sb.AppendLine("# " + u[2]);
                    sb.AppendLine(u[0] + " = " + u[1]);
                }
                System.IO.File.WriteAllText(path, sb.ToString());
            }
            catch (System.Exception) { }
            return path;
        }
    }


    // Культурные черты «Укрепления»: что умеет строить и чем стрелять королевство, зависит от культуры
    // Переводы: файлы Locales/<язык>.json рядом с модом (en, ru, es, cz - китайский упрощённый, ch - китайский традиционный).
    // Меню настроек подхватывает NML, остальные ключи (черты, сообщения мира, задачи юнитов) добавляем в игру сами.
    // Нет перевода на язык игры - английский
    internal static class Loc
    {
        private static string applied_lang;
        private static readonly Dictionary<string, Dictionary<string, string>> cache = new Dictionary<string, Dictionary<string, string>>();

        private static Dictionary<string, string> Load(string code)
        {
            Dictionary<string, string> d;
            if (cache.TryGetValue(code, out d)) return d;
            d = new Dictionary<string, string>();
            try
            {
                string f = System.IO.Path.Combine(System.IO.Path.Combine(ModClass.Instance.GetDeclaration().FolderPath, "Locales"), code + ".json");
                if (System.IO.File.Exists(f))
                    d = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(f, System.Text.Encoding.UTF8)) ?? d;
            }
            catch (System.Exception e) { WallBuilder.Log("locale " + code + " failed: " + e.Message); }
            cache[code] = d;
            return d;
        }

        internal static void Apply(bool force = false)
        {
            try
            {
                if (LocalizedTextManager.instance == null) return;
                string lang = LocalizedTextManager.instance.language;
                if (!force && lang == applied_lang) return;
                Dictionary<string, string> en = Load("en"), cur = lang == "en" ? en : Load(lang);
                foreach (KeyValuePair<string, string> kv in en)
                {
                    string v;
                    if (!cur.TryGetValue(kv.Key, out v) || string.IsNullOrEmpty(v)) v = kv.Value;
                    LocalizedTextManager.add(kv.Key, v, true, "", false);
                }
                applied_lang = lang;
            }
            catch (System.Exception) { }
        }
    }

    public static class Traits
    {
        public const string WALLS = "ws_walls";
        public const string MOAT = "ws_moat";
        public const string COAST = "ws_coast";
        public const string DOUBLE = "ws_double_wall";
        public const string MORTAR = "ws_mortar";
        public const string PAVING = "ws_paving";
        public const string RAMMERS = "ws_rammers";
        public const string IRON = "ws_iron_gates";
        private const string FLAG2 = "ws_traits_v2";     // метка: новые черты (таранщики, укреплённые ворота) уже разыграны
        private const string GROUP = "fortifications";
        private const string FLAG = "ws_traits_v1";      // скрытая метка: у культуры черты уже разданы (старые культуры без метки получают черты один раз)
        public static readonly string[] ALL = new string[] { WALLS, MOAT, COAST, DOUBLE, MORTAR, PAVING, RAMMERS, IRON };
        public static bool REQUIRED = Cfg.Bool("traits_required", true, "укрепления и мощение требуют культурных черт (выключено - как в версиях до 1.2)");
        private static float LATE_CHANCE = Cfg.Float("traits_late_chance", 0.004f, "шанс за проверку (раз в 30 с), что культура сама научится рву, береговой обороне, двойной стене или мортире");
        private static readonly System.Random rnd = new System.Random();

        // свои иконки лежат в папке Icons мода (PNG 32x32), в игру их добавляем через SpriteTextureLoader; запасной вариант - значок игры
        private static readonly Dictionary<string, string> FALLBACK = new Dictionary<string, string>
        {
            { WALLS, "ui/Icons/culture_traits/culture_trait_tower_lovers" },
            { MOAT, "ui/Icons/culture_traits/culture_trait_statue_lovers" },
            { COAST, "ui/Icons/culture_traits/culture_trait_animal_whisperers" },
            { DOUBLE, "ui/Icons/culture_traits/culture_trait_training_potential" },
            { MORTAR, "ui/Icons/culture_traits/culture_trait_expansionists" },
            { PAVING, "ui/Icons/culture_traits/culture_trait_roads" },
            { RAMMERS, "ui/Icons/culture_traits/culture_trait_expansionists" },
            { IRON, "ui/Icons/culture_traits/culture_trait_tower_lovers" },
        };
        private static bool icons_loaded;

        private static string IconFor(string id)
        {
            string key = "ws_icons/" + id;
            try
            {
                if (!icons_loaded)
                {
                    icons_loaded = true;
                    string dir = System.IO.Path.Combine(ModClass.Instance.GetDeclaration().FolderPath, "Icons");
                    foreach (string tid in ALL)
                    {
                        string f = System.IO.Path.Combine(dir, tid + ".png");
                        if (!System.IO.File.Exists(f)) continue;
                        try { SpriteTextureLoader.addSprite("ws_icons/" + tid, System.IO.File.ReadAllBytes(f)); icon_ok.Add(tid); } catch (System.Exception) { }
                    }
                }
            }
            catch (System.Exception e) { WallBuilder.Log("trait icons failed: " + e.Message); }
            return icon_ok.Contains(id) ? key : FALLBACK[id];
        }
        private static readonly HashSet<string> icon_ok = new HashSet<string>();

        private static string last_lang;
        private static double next_late;

        public static void Register()
        {
            if (AssetManager.culture_trait_groups.get(GROUP) == null)
            {
                CultureTraitGroupAsset g = new CultureTraitGroupAsset();
                g.id = GROUP;
                g.name = "trait_group_fortifications";
                g.color = "#D9C28A";
                AssetManager.culture_trait_groups.add(g);
            }
            foreach (string id in ALL)
            {
                if (AssetManager.culture_traits.get(id) != null) continue;
                CultureTrait t = new CultureTrait();
                t.id = id;
                t.group_id = GROUP;
                t.path_icon = IconFor(id);
                t.spawn_random_trait_allowed = false;   // раздаём сами
                t.needs_to_be_explored = false;
                if (id == MORTAR || id == RAMMERS) t.can_be_in_book = false;   // мортира и таранщики не расходятся по книгам
                t.rarity = id == MORTAR ? Rarity.R2_Epic : (id == WALLS || id == PAVING || id == RAMMERS || id == IRON ? Rarity.R0_Normal : Rarity.R1_Rare);
                AssetManager.culture_traits.add(t);
            }
            AddTexts();
        }

        private static void AddTexts()
        {
            last_lang = LocalizedTextManager.instance != null ? LocalizedTextManager.instance.language : null;
            Loc.Apply(true);
        }

        // Раз в секунду-другую: подменили язык - вернуть свои тексты; изредка культура сама осваивает новые черты
        public static void Tick()
        {
            try
            {
                if (LocalizedTextManager.instance == null) return;
                if (LocalizedTextManager.instance.language != last_lang) AddTexts();
                if (World.world == null || World.world.cultures == null) return;
                double now = World.world.getCurWorldTime();
                if (now < next_late) return;
                next_late = now + 30.0;
                if (LATE_CHANCE <= 0f) return;
                foreach (Culture c in World.world.cultures)
                {
                    if (c == null || !c.isAlive()) continue;
                    if (!c.data.hasFlag(FLAG2))
                    {
                        // культуры из прежних сборок один раз разыгрывают новые черты
                        c.data.addFlag(FLAG2);
                        string arch = ArchOf(c);
                        foreach (string id in new string[] { RAMMERS, IRON })
                            if (!c.hasTrait(id) && rnd.NextDouble() < Chance(id, arch)) { c.addTrait(id); WallBuilder.Dbg("culture " + c.name + " got " + id + " (old culture)"); }
                    }
                    if (!c.data.hasFlag(FLAG) || c.countCities() == 0) continue;
                    if (!c.hasTrait(RAMMERS) && rnd.NextDouble() < LATE_CHANCE * 0.5) { c.addTrait(RAMMERS); WallBuilder.Dbg("culture " + c.name + " learned " + RAMMERS); }
                    if (!c.hasTrait(WALLS)) continue;
                    foreach (string id in new string[] { MOAT, COAST, DOUBLE, MORTAR, IRON })
                    {
                        if (c.hasTrait(id) || rnd.NextDouble() >= (id == MORTAR ? LATE_CHANCE * 0.05 : (id == DOUBLE ? LATE_CHANCE * 0.25 : LATE_CHANCE))) continue;
                        c.addTrait(id);
                        WallBuilder.Dbg("culture " + c.name + " learned " + id);
                    }
                }
            }
            catch (System.Exception) { }
        }

        private static string ArchOf(Culture c)
        {
            try
            {
                ActorAsset a = AssetManager.actor_library.get(c.data.original_actor_asset);
                if (a != null && !string.IsNullOrEmpty(a.architecture_id)) return a.architecture_id;
            }
            catch (System.Exception) { }
            return "human";
        }

        private static float Chance(string id, string arch)
        {
            bool core = arch == "human" || arch == "orc" || arch == "dwarf";
            switch (id)
            {
                case WALLS: return arch == "elf" ? 0.5f : (core ? 0.75f : 0.35f);
                case MOAT: return 0.5f;
                case COAST: return arch == "dwarf" ? 1f : 0.6f;
                case DOUBLE: return 0.08f;
                case MORTAR: return arch == "dwarf" ? 1f : 0f;      // на старте мортира только у гномов, остальные осваивают её редко по ходу игры
                case PAVING: return (arch == "human" || arch == "dwarf") ? 1f : 0.15f;
                case RAMMERS: return arch == "orc" ? 0.25f : 0.10f;
                case IRON: return 0.10f;
            }
            return 0f;
        }

        // Новая культура: черты разыгрываются сразу (культура, созданная копированием, получит черты родителя)
        public static void NewCulturePostfix(Culture __result, bool pAddDefaultTraits)
        {
            try
            {
                Culture c = __result;
                if (c == null) return;
                c.data.addFlag(FLAG);
                c.data.addFlag(FLAG2);
                if (!pAddDefaultTraits) return;
                string arch = ArchOf(c);
                foreach (string id in ALL)
                    if (rnd.NextDouble() < Chance(id, arch)) c.addTrait(id);
            }
            catch (System.Exception e) { WallBuilder.Log("culture trait roll failed: " + e.Message); }
        }

        // Культура из старого сохранения (без метки): получает стены, ров, берег и мощение как раньше, один раз
        private static void Ensure(Culture c)
        {
            if (c.data.hasFlag(FLAG)) return;
            c.data.addFlag(FLAG);
            string arch = ArchOf(c);
            c.addTrait(WALLS);
            if (arch != "orc") c.addTrait(MOAT);
            c.addTrait(COAST);
            if (arch == "human" || arch == "dwarf") c.addTrait(PAVING);
            WallBuilder.Dbg("old culture " + c.name + " got the fortification traits");
        }

        private static bool Has(Culture c, string id, bool legacy)
        {
            if (c == null) return legacy;
            Ensure(c);
            return c.hasTrait(id);
        }

        // legacy: что было бы по прежним правилам (если культуры нет или проверка черт выключена)
        public static bool Allows(City city, string id, bool legacy)
        {
            try
            {
                if (!REQUIRED && id != DOUBLE && id != MORTAR) return legacy;
                if (city == null) return legacy;
                Culture c = city.culture != null ? city.culture : (city.kingdom != null ? city.kingdom.culture : null);
                if (c == null) return id == DOUBLE || id == MORTAR ? false : legacy;
                return Has(c, id, legacy);
            }
            catch (System.Exception) { return legacy; }
        }

        // у культуры есть черта (без запасных правил): для таранщиков и укреплённых ворот
        public static bool HasKingdom(Kingdom k, string id)
        {
            try { return k != null && k.culture != null && k.culture.hasTrait(id); } catch (System.Exception) { return false; }
        }

        public static bool HasCity(City city, string id)
        {
            try
            {
                if (city == null) return false;
                Culture c = city.culture != null ? city.culture : (city.kingdom != null ? city.kingdom.culture : null);
                return c != null && c.hasTrait(id);
            }
            catch (System.Exception) { return false; }
        }

        public static bool AllowsKingdom(Kingdom k, string id, bool legacy)
        {
            try
            {
                if (!REQUIRED && id != DOUBLE && id != MORTAR) return legacy;
                if (k == null) return legacy;
                Culture c = k.culture;
                if (c == null) return id == DOUBLE || id == MORTAR ? false : legacy;
                return Has(c, id, legacy);
            }
            catch (System.Exception) { return legacy; }
        }
    }

    // Обработчики окна Mod Configure (имена указаны в default_config.json)
    public static class ConfigCallbacks
    {
        private static string B(bool v) { return v ? "true" : "false"; }
        public static void OnCamps(bool v) { Cfg.Set("camp_enabled", B(v)); }
        public static void OnCoast(bool v) { Cfg.Set("coast_towers", B(v)); }
        public static void OnGates(bool v) { Cfg.Set("gates_enabled", B(v)); }
        public static void OnMoat(bool v) { Cfg.Set("moat_enabled", B(v)); }
        public static void OnDecay(bool v) { Cfg.Set("wall_decay", B(v)); }
        public static void OnNews(bool v) { Cfg.Set("wall_news", B(v)); }
        public static void OnReplan(bool v) { Cfg.Set("wall_replan", B(v)); }
        public static void OnBuffer(bool v) { Cfg.Set("wall_border_buffer", B(v)); }
        public static void OnMountains(bool v) { Cfg.Set("wall_avoid_mountains", B(v)); }
        public static void OnFx(bool v) { Cfg.Set("fx_effects", B(v)); }
        public static void OnMuster(bool v) { Cfg.Set("muster_civilians", B(v)); }
        public static void OnDefenders(bool v) { Cfg.Set("defenders_gather", B(v)); }
        public static void OnRam(bool v) { Cfg.Set("siege_ram", B(v)); }
        public static void OnTraits(bool v) { Cfg.Set("traits_required", B(v)); Traits.REQUIRED = v; }
        public static void OnAlert(bool v) { Cfg.Set("alert_levels", B(v)); WallBuilder.ALERT_ON = v; }
        public static void OnGunRange(float v) { int r = (int)System.Math.Round(v); Cfg.Set("siege_gun_max_range", r.ToString()); Siege.SetGunRange(r); }
        public static void OnWallShare(float v) { Cfg.Set("siege_wall_share", ((int)System.Math.Round(v)).ToString()); }
        public static void OnGateHp(float v) { Cfg.Set("gate_hp", ((int)System.Math.Round(v)).ToString() ); }
        public static void OnSounds(bool v) { Cfg.Set("custom_sounds", B(v)); Sfx.SetOn(v); }
        public static void OnSoundVol(float v) { float f = System.Math.Max(0f, System.Math.Min(100f, v)) / 100f; Cfg.Set("custom_sounds_volume", f.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)); Sfx.SetVol(f); }
        public static void OnMortarMin(float v) { int r = (int)System.Math.Round(v); Cfg.Set("mortar_min_range", r.ToString()); Siege.SetMortarMin(r); }
        public static void OnUnstick(bool v) { Cfg.Set("army_unstick", B(v)); WallBuilder.SetUnstick(v); }
        public static void OnReload(float v) { Cfg.Set("siege_gun_reload", v.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)); }
    }

    // Свои звуки: WAV-файлы из папки Sounds рядом с модом. Соответствие событий и файлов задано ниже,
    // его можно поменять файлом Sounds/sounds.txt (строки вида "hit_wall = a.wav, b.wav"). Нет файла - играет звук игры
    internal static class Sfx
    {
        private static bool ON = Cfg.Bool("custom_sounds", true, "играть свои звуки из папки Sounds");
        internal static void SetOn(bool v) { ON = v; }
        internal static void SetVol(float v) { VOL = UnityEngine.Mathf.Clamp01(v); }
        private static float VOL = Cfg.Float("custom_sounds_volume", 0.6f, "громкость своих звуков (0-1)");
        private static readonly Dictionary<string, List<object>> clips = new Dictionary<string, List<object>>();
        private static readonly Dictionary<string, List<float>> lens = new Dictionary<string, List<float>>();
        private static readonly Dictionary<string, float> last_play = new Dictionary<string, float>();
        private static bool loaded, failed;
        private static System.Type tClip, tSrc;
        private static bool SFX_LOG = Cfg.Bool("sound_log", false, "писать в лог, какие свои звуки сыграли");
        public const string IMPACT_ROCK = "mymod:impact_rock";
        public const string IMPACT_BALL = "mymod:impact_ball";
        public const string IMPACT_MORTAR = "mymod:impact_mortar";

        // Снаряд осадного орудия падает: звук зависит от того, во что он попал (ворота, стена или земля)
        public static bool PlaySoundPrefix(string pSoundPath, float pX, float pY, bool pGameViewOnly)
        {
            try
            {
                if (pSoundPath == null || !pSoundPath.StartsWith("mymod:")) return true;
                WorldTile t = World.world.GetTile((int)pX, (int)pY);
                string evt = pSoundPath == IMPACT_BALL ? "impact_ball" : (pSoundPath == IMPACT_MORTAR ? "impact_mortar" : "impact_rock");
                int kind = t != null ? WallBuilder.ImpactKind(t) : 0;
                string hit = kind == 2 ? "hit_gate" : kind == 1 ? "hit_wall" : null;
                bool played = false;
                if (hit != null) played = Play(hit, t);
                if (!played) played = Play(evt, t);
                if (!played && t != null && MusicBox.sounds_on)
                    MusicBox.playSound("event:/SFX/EXPLOSIONS/ExplosionSmall", pX, pY, pGameViewOnly);
            }
            catch (System.Exception) { }
            return false;
        }

        private static readonly List<object[]> later = new List<object[]>();

        // Звук с задержкой (снаряд ещё летит, когда считается пробитие)
        public static void PlayLater(string evt, WorldTile at, float delay)
        {
            if (!ON || at == null) return;
            later.Add(new object[] { evt, at, UnityEngine.Time.realtimeSinceStartup + delay });
        }

        public static void Pump()
        {
            if (later.Count == 0) return;
            float now = UnityEngine.Time.realtimeSinceStartup;
            for (int i = later.Count - 1; i >= 0; i--)
            {
                if ((float)later[i][2] > now) continue;
                string e = (string)later[i][0]; WorldTile w = (WorldTile)later[i][1];
                later.RemoveAt(i);
                Play(e, w);
            }
        }

        private static readonly string[][] DEFAULTS = new string[][]
        {
            new string[] { "shot_catapult", "catapult.wav", "catapult2.wav" },
            new string[] { "shot_trebuchet", "trebuchet_shot.wav" },
            new string[] { "hit_wall", "wall_hit_1.wav", "stone_hit.wav" },
            new string[] { "hit_gate", "stone_hit.wav" },
            new string[] { "impact_rock", "boom5.wav" },
            new string[] { "impact_ball", "boom1.wav" },
            new string[] { "shot_mortar", "boom3.wav" },
            new string[] { "impact_mortar", "boom4.wav", "boom2.wav" },
            new string[] { "wall_collapse", "wall_collapse_1.wav", "boom2.wav" },
            new string[] { "gate_destroyed", "gatesfall.wav", "boom3.wav" },
            new string[] { "gate_close", "gate_close_1.wav", "gate_close_2.wav" },
            new string[] { "gate_open", "gate_open_1.wav" },
            new string[] { "alarm", "belltoll.wav" },
            new string[] { "tower_collapse", "boom4.wav" },
            new string[] { "ram_creak", "ram_creak_1.wav", "ram_creak_2.wav" },
            new string[] { "ram_bash", "ram_bash_1.wav", "ram_bash_2.wav", "ram_bash_3.wav" },
        };

        private static string Folder()
        {
            try
            {
                string f = ModClass.Instance.GetDeclaration().FolderPath;
                return System.IO.Path.Combine(f, "Sounds");
            }
            catch (System.Exception) { return null; }
        }

        private static void Load()
        {
            loaded = true;
            try
            {
                tClip = System.Type.GetType("UnityEngine.AudioClip, UnityEngine.AudioModule");
                tSrc = System.Type.GetType("UnityEngine.AudioSource, UnityEngine.AudioModule");
                string dir = Folder();
                if (tClip == null || tSrc == null || dir == null || !System.IO.Directory.Exists(dir)) { failed = true; return; }

                Dictionary<string, string> files = new Dictionary<string, string>();
                foreach (string f in System.IO.Directory.GetFiles(dir, "*.wav", System.IO.SearchOption.TopDirectoryOnly))
                    files[System.IO.Path.GetFileName(f).ToLowerInvariant()] = f;

                List<string[]> map = new List<string[]>(DEFAULTS);
                string cfg = System.IO.Path.Combine(dir, "sounds.txt");
                if (System.IO.File.Exists(cfg))
                {
                    map.Clear();
                    foreach (string line in System.IO.File.ReadAllLines(cfg))
                    {
                        string l = line.Trim();
                        if (l.Length == 0 || l.StartsWith("#")) continue;
                        int eq = l.IndexOf('=');
                        if (eq < 1) continue;
                        List<string> parts = new List<string>();
                        parts.Add(l.Substring(0, eq).Trim());
                        foreach (string nm in l.Substring(eq + 1).Split(',')) if (nm.Trim().Length > 0) parts.Add(nm.Trim());
                        map.Add(parts.ToArray());
                    }
                }

                int count = 0;
                Dictionary<string, object> cache = new Dictionary<string, object>();
                Dictionary<string, float> cacheLen = new Dictionary<string, float>();
                foreach (string[] row in map)
                {
                    List<object> cl = new List<object>(); List<float> ll = new List<float>();
                    for (int i = 1; i < row.Length; i++)
                    {
                        string key = row[i].ToLowerInvariant();
                        string path;
                        if (!files.TryGetValue(key, out path)) continue;
                        object clip; float len;
                        if (!cache.TryGetValue(path, out clip))
                        {
                            float[] data; int ch, rate;
                            if (!ReadWav(path, out data, out ch, out rate)) { WallBuilder.Log("sound skipped (unsupported wav): " + row[i]); cache[path] = null; continue; }
                            float pk = 0f;
                            for (int di = 0; di < data.Length; di++) { float av = data[di] < 0f ? -data[di] : data[di]; if (av > pk) pk = av; }
                            if (pk > 0.7f) { float gain = 0.7f / pk; for (int di = 0; di < data.Length; di++) data[di] *= gain; }   // слишком громкие файлы приглушаем, чтобы не перегружать звук игры
                            int frames = data.Length / ch;
                            clip = tClip.GetMethod("Create", new System.Type[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(bool) })
                                .Invoke(null, new object[] { "mymod_" + row[i], frames, ch, rate, false });
                            tClip.GetMethod("SetData", new System.Type[] { typeof(float[]), typeof(int) }).Invoke(clip, new object[] { data, 0 });
                            cache[path] = clip; cacheLen[path] = (float)frames / rate;
                            count++;
                        }
                        if (clip == null) continue;
                        len = cacheLen[path];
                        cl.Add(clip); ll.Add(len);
                    }
                    if (cl.Count > 0) { clips[row[0]] = cl; lens[row[0]] = ll; }
                }
                WallBuilder.Log("custom sounds loaded: " + count + " files, " + clips.Count + " events (" + dir + ")");
                if (clips.Count == 0) failed = true;
            }
            catch (System.Exception e) { failed = true; WallBuilder.Log("custom sounds failed: " + e.Message); }
        }

        // PCM 8/16/24 бит и float 32, любое число каналов
        private static bool ReadWav(string path, out float[] data, out int channels, out int rate)
        {
            data = null; channels = 0; rate = 0;
            byte[] b = System.IO.File.ReadAllBytes(path);
            if (b.Length < 44 || b[0] != 'R' || b[1] != 'I' || b[2] != 'F' || b[3] != 'F' || b[8] != 'W' || b[9] != 'A' || b[10] != 'V' || b[11] != 'E') return false;
            int pos = 12, fmt = 0, bits = 0, dpos = -1, dlen = 0;
            while (pos + 8 <= b.Length)
            {
                string id = "" + (char)b[pos] + (char)b[pos + 1] + (char)b[pos + 2] + (char)b[pos + 3];
                int len = System.BitConverter.ToInt32(b, pos + 4);
                if (len < 0) return false;
                if (id == "fmt ")
                {
                    fmt = System.BitConverter.ToUInt16(b, pos + 8);
                    channels = System.BitConverter.ToUInt16(b, pos + 10);
                    rate = System.BitConverter.ToInt32(b, pos + 12);
                    bits = System.BitConverter.ToUInt16(b, pos + 22);
                    if (fmt == 0xFFFE && len >= 26) fmt = System.BitConverter.ToUInt16(b, pos + 32);
                }
                else if (id == "data") { dpos = pos + 8; dlen = System.Math.Min(len, b.Length - dpos); break; }
                pos += 8 + len + (len & 1);
            }
            if (dpos < 0 || channels < 1 || channels > 8 || rate < 4000 || rate > 192000) return false;
            int bytes = bits / 8;
            if (!((fmt == 1 && (bits == 8 || bits == 16 || bits == 24)) || (fmt == 3 && bits == 32))) return false;
            int n = dlen / bytes / channels * channels;
            data = new float[n];
            for (int i = 0; i < n; i++)
            {
                int o = dpos + i * bytes;
                if (fmt == 3) data[i] = System.BitConverter.ToSingle(b, o);
                else if (bits == 8) data[i] = (b[o] - 128) / 128f;
                else if (bits == 16) data[i] = System.BitConverter.ToInt16(b, o) / 32768f;
                else data[i] = (((b[o + 2] << 24) | (b[o + 1] << 16) | (b[o] << 8)) >> 8) / 8388608f;
            }
            return n > 0;
        }

        // true - свой звук сыгран (или игроку его не слышно), стандартный звук игры не нужен
        public static bool Play(string evt, WorldTile at)
        {
            if (!ON || at == null) return false;
            try
            {
                if (!loaded) Load();
                if (failed) return false;
                List<object> cl;
                if (!clips.TryGetValue(evt, out cl) || cl.Count == 0) return false;
                if (!MusicBox.sounds_on) return true;
                if (at.zone != null && !at.zone.visible) return true;
                float now = UnityEngine.Time.realtimeSinceStartup, lp;
                if (last_play.TryGetValue(evt, out lp) && now - lp < 0.12f) return true;
                last_play[evt] = now;
                int k = UnityEngine.Random.Range(0, cl.Count);
                if (SFX_LOG) WallBuilder.Dbg("sound: " + evt + " #" + k + " at " + at.x + "," + at.y);
                UnityEngine.GameObject go = new UnityEngine.GameObject("MymodSfx");
                object src = go.AddComponent(tSrc);
                tSrc.GetProperty("clip").SetValue(src, cl[k], null);
                tSrc.GetProperty("volume").SetValue(src, UnityEngine.Mathf.Clamp01(VOL), null);
                tSrc.GetProperty("spatialBlend").SetValue(src, 0f, null);
                tSrc.GetMethod("Play", new System.Type[0]).Invoke(src, null);
                UnityEngine.Object.Destroy(go, lens[evt][k] + 0.5f);
                return true;
            }
            catch (System.Exception e) { failed = true; WallBuilder.Log("custom sound failed: " + e.Message); return false; }
        }
    }

    // Визуальные эффекты осады: обломки, тлеющие балки, огонь на разбитых воротах, флажки на башнях.
    // Это обычные нарисованные здания-декорации с таймером жизни; настройки: fx_effects, fx_pennants, fx_max
    internal static class Fx
    {
        public static bool ON = Cfg.Bool("fx_effects", true, "визуальные эффекты осады: обломки, тлеющие балки, огонь на воротах");
        private static bool PENNANTS = Cfg.Bool("fx_pennants", true, "маленькие флажки на башнях осаждённого города");
        private static int MAX = Cfg.Int("fx_max", 150, "не больше стольких зданий-эффектов на карте одновременно");

        public const string DEBRIS = "fx_debris";
        public const string EMBER = "fx_ember";
        public const string GFIRE = "fx_gate_fire";
        public const string PENNANT = "fx_pennant";
        public const string BRICK = "fx_brick";
        public const string ALERT0 = "fx_alert_0";
        public const string ALERT1 = "fx_alert_1";
        public const string ALERT2 = "fx_alert_2";
        public static readonly string[] IDS = new string[] { DEBRIS, EMBER, GFIRE, PENNANT, BRICK, ALERT0, ALERT1, ALERT2 };
        public static bool IsFxId(string id) { return id == DEBRIS || id == EMBER || id == GFIRE || id == PENNANT || id == BRICK || id == ALERT0 || id == ALERT1 || id == ALERT2; }

        private class Item
        {
            public Building b;
            public double expire;
            public City owner;
            public string id;
            public Building tower;
            public long key;
            public double gone_since = -1;
        }
        private static readonly List<Item> items = new List<Item>();
        private static int last_frame = -1;
        private static readonly Dictionary<long, double> next_pennants = new Dictionary<long, double>();

        public static bool Has(Building b)
        {
            for (int i = 0; i < items.Count; i++) if (items[i].b == b) return true;
            return false;
        }

        public static void AddActive(HashSet<Building> set)
        {
            for (int i = 0; i < items.Count; i++) if (items[i].b != null) set.Add(items[i].b);
        }

        // ---------- Рисунки ----------
        // Обломки: крупные, средние и мелкие кучки (каждое здание выбирает случайный вариант)
        static readonly string[][] GRIDS_DEBRIS = new string[][] {
            new string[] { "......s......", "..L..sSL.....", ".Ll.LSsLlt.s.", "tLlsSLlsStLsS", "SsLlLSsLlSStS", ".tSsSLtsSltS." },
            new string[] { "....L.......", ".L..lL.s....", ".lLmLLl.Ls..", "LlLMLmLlLSs.", "mLMlLSsLMmLt", "tmSsMLtLmStS" },
            new string[] { "....s....", "..L.sSL..", ".LlsSLlt.", "tLsSlLSsS", ".StsLtSt." },
            new string[] { "...L......", ".L.lL.s...", "LlLMLlSsL.", "mLlLSsLmLt", ".tSmLtSStt" },
            new string[] { "..sS..", ".LsSL.", "tLSstS" },
            new string[] { "...L...", ".LlLs..", "mLlSsLt" },
            new string[] { ".sS..", "LsSL.", "tSLtS" }
        };
        // Отдельные кирпичи из каменных стен
        static readonly string[][] GRIDS_BRICK = new string[][] {
            new string[] { "ssss", "SSSt" },
            new string[] { "ss.", "SSt" },
            new string[] { ".ss....", "sSSt.ss", "ttSt.St" },
            new string[] { "ss.s.", "SStSt" },
            new string[] { ".s.", "SSt" }
        };
        static readonly string[] GRID_BRICK_BUILD = new string[] { "ss", "SS" };
        static readonly string[] GRID_BRICK_RUIN = new string[] { "tt" };
        static readonly string[] GRID_DEBRIS_BUILD = new string[] { "...", ".L.", "LsL" };
        static readonly string[] GRID_DEBRIS_RUIN = new string[] { "...", ".Q.", "QtQ" };

        static readonly string[][] GRIDS_EMBER = new string[][] {
            new string[] { "...........", "..Q..Q.....", ".QtQQtQ.Q..", "QtqQtQoQtQ.", ".QQqQQQqQQ." },
            new string[] { "...........", "..Q..Q..Q..", ".QtQoQtQQ..", "QtQQtqQqtQ.", ".QQoQQQQqQ." },
            new string[] { "...........", "..Q..Q.....", ".QtQQtQ.Q..", "QtoQtQqQtQ.", ".QQQQqQQoQ." }
        };

        static readonly string[][] GRIDS_PENNANT = new string[][] {
            new string[] { "LL.....", "PDFFFf.", "PDFFff.", "PDfffn.", "PDnn...", "PD.....", "PD.....", "PD.....", "PD.....", "sPDs...", "SSsS..." },
            new string[] { "LL.....", "PDFFf..", "PDFFFff", "PDfffnn", "PDnn...", "PD.....", "PD.....", "PD.....", "PD.....", "sPDs...", "SSsS..." },
            new string[] { "LL.....", "PDFFFFf", "PDFfff.", "PDffn..", "PDn....", "PD.....", "PD.....", "PD.....", "PD.....", "sPDs...", "SSsS..." }
        };
        static readonly string[] GRID_PENNANT_BUILD = new string[] { "PD..", "PD..", "sPDs" };
        static readonly string[] GRID_PENNANT_RUIN = new string[] { ".t.", "sQs" };

        // Языки пламени трёх размеров, по 4 кадра (нарисованы вручную: сгенерированные выходили похожими на лампы)
        static readonly string[][] FLAMES_A = new string[][] {
            new string[] { "..O..", ".OoO.", ".OoO.", "OoyoO", "OoyoO", ".OoO." },
            new string[] { ".O...", ".Oo..", ".OoO.", "OoyoO", "OoyoO", ".OoO." },
            new string[] { "...O.", "..oO.", ".OoO.", "OoyoO", "OoyoO", ".OoO." },
            new string[] { "..O..", "..oO.", ".OoO.", "OoyoO", "OoyoO", ".OoO." } };
        static readonly string[][] FLAMES_B = new string[][] {
            new string[] { "..O..", "..O..", "..oO.", ".OoO.", ".OooO", "OoyoO", "OoyoO", "OoyyO", ".OoO." },
            new string[] { ".O...", ".Oo..", ".OoO.", ".OooO", "OoooO", "OoyoO", "OoyoO", "OoyoO", ".OoO." },
            new string[] { "...O.", "..Oo.", ".OoO.", "OooO.", "OoooO", "OoyoO", "OoyoO", "OyyoO", ".OoO." },
            new string[] { "..O..", ".Oo..", ".OoO.", ".OoOO", "OooO.", "OoyoO", "OoyoO", "OoyoO", ".OoO." } };
        static readonly string[][] FLAMES_C = new string[][] {
            new string[] { "...O...", "..OoO..", ".OoooO.", ".OoyoO.", "OoyyyoO", "OoyyyoO", "OooyooO", ".OOooO." },
            new string[] { "..O....", "..OoO..", ".OoooO.", ".OoyoO.", "OoyyyoO", "OoyyyoO", "OooyooO", ".OOooO." },
            new string[] { "....O..", "..OoO..", ".OoooO.", ".OoyoO.", "OoyyyoO", "OoyyyoO", "OooyooO", ".OOooO." },
            new string[] { "...O...", "..oOO..", ".OoooO.", ".OoyoO.", "OoyyyoO", "OoyyyoO", "OooyooO", ".OOooO." } };

        // Флаг тревоги на площади: цвет и значок (зелёный - точка, жёлтый - полоса, красный - крест), чтобы различался и без цвета
        static string[] AlertGrid(int level, int wave)
        {
            const int W = 12, H = 17;
            char[,] g = new char[H, W];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) g[y, x] = '.';
            char hi = level == 0 ? '1' : (level == 1 ? '4' : '7');
            char mid = level == 0 ? '2' : (level == 1 ? '5' : '8');
            char dk = level == 0 ? '3' : (level == 1 ? '6' : '9');
            // древко с навершием и каменное основание
            g[0, 0] = 'h'; g[0, 1] = 'H';
            for (int y = 1; y < 15; y++) { g[y, 0] = 'P'; g[y, 1] = (y % 5 == 0) ? 'd' : 'D'; }
            g[15, 0] = 's'; g[15, 1] = 'P'; g[15, 2] = 'D'; g[15, 3] = 's';
            for (int x = 0; x < 5; x++) g[16, x] = x % 2 == 0 ? 'S' : 't';
            // полотнище 2..11 x 1..7: без каймы, свет сверху, тень снизу, мягкие складки (свет - в верхней половине, тень - в нижней)
            for (int y = 1; y <= 7; y++)
            {
                int tail = (y >= 3 && y <= 5) ? (y == 4 ? 3 : 1) : 0;       // ласточкин хвост на свободном краю
                for (int x = 2; x < W - tail; x++)
                {
                    char c = y == 1 ? hi : (y == 7 ? dk : mid);
                    int fold = (x + wave * 2) % 5;
                    if (y >= 2 && y <= 3 && fold == 0) c = hi;
                    else if (y >= 5 && y <= 6 && fold == 2) c = dk;
                    g[y, x] = c;
                }
            }
            // значок: зелёный - кружок, жёлтый - полоса, красный - крест
            if (level == 0)
            {
                g[3, 5] = 'A'; g[3, 6] = 'A'; g[4, 4] = 'A'; g[4, 5] = 'A'; g[4, 6] = 'A'; g[4, 7] = 'a'; g[5, 5] = 'a'; g[5, 6] = 'a';
            }
            else if (level == 1)
            {
                for (int x = 3; x <= 9; x++) { g[3, x] = 'A'; g[4, x] = 'A'; g[5, x] = 'a'; }
            }
            else
            {
                int[][] cr = new int[][] { new int[] { 2, 4 }, new int[] { 2, 8 }, new int[] { 3, 5 }, new int[] { 3, 7 }, new int[] { 4, 6 }, new int[] { 5, 5 }, new int[] { 5, 7 }, new int[] { 6, 4 }, new int[] { 6, 8 } };
                foreach (int[] c in cr) { g[c[0], c[1]] = 'A'; if (c[1] + 1 < W && g[c[0], c[1] + 1] != 'A') g[c[0], c[1] + 1] = 'a'; }
            }
            string[] rows = new string[H];
            for (int y = 0; y < H; y++) { char[] r = new char[W]; for (int x = 0; x < W; x++) r[x] = g[y, x]; rows[y] = new string(r); }
            return rows;
        }

        // Флаг тревоги в центре города: один на город, цвет по уровню (level -1 - убрать)
        private static readonly Dictionary<long, Item> alert_flags = new Dictionary<long, Item>();
        public static void AlertFlag(City city, WorldTile center, int level, double now)
        {
            if (city == null || !city.isAlive()) return;
            long ck = city.getID();
            Item cur;
            bool have = alert_flags.TryGetValue(ck, out cur);
            if (have && (cur.b == null || !cur.b.isAlive() || !items.Contains(cur))) { alert_flags.Remove(ck); have = false; cur = null; }
            if (!ON || level < 0)
            {
                if (have) { try { cur.b.startDestroyBuilding(); } catch (System.Exception) { } items.Remove(cur); alert_flags.Remove(ck); }
                return;
            }
            string want = level == 0 ? ALERT0 : (level == 1 ? ALERT1 : ALERT2);
            if (have && cur.id == want) return;
            WorldTile at = have && cur.b.current_tile != null ? cur.b.current_tile : center;
            if (have) { try { cur.b.startDestroyBuilding(); } catch (System.Exception) { } items.Remove(cur); alert_flags.Remove(ck); }
            if (at == null || city.kingdom == null) return;
            Building nb = Spawn(want, at, 0, city.kingdom, 1.0e9, city, 0);
            if (nb != null) alert_flags[ck] = items[items.Count - 1];
        }

        public static UnityEngine.Sprite[] GetSprites(string id, UnityEngine.Sprite mini)
        {
            List<UnityEngine.Sprite> res = new List<UnityEngine.Sprite>();
            if (id == DEBRIS)
            {
                res.Add(WallBuilder.FromGrid(GRID_DEBRIS_BUILD, "construction_0"));
                for (int v = 0; v < GRIDS_DEBRIS.Length; v++) res.Add(WallBuilder.FromGrid(GRIDS_DEBRIS[v], "main_" + v));
                res.Add(WallBuilder.FromGrid(GRID_DEBRIS_RUIN, "ruin_0"));
            }
            else if (id == BRICK)
            {
                res.Add(WallBuilder.FromGrid(GRID_BRICK_BUILD, "construction_0"));
                for (int v = 0; v < GRIDS_BRICK.Length; v++) res.Add(WallBuilder.FromGrid(GRIDS_BRICK[v], "main_" + v));
                res.Add(WallBuilder.FromGrid(GRID_BRICK_RUIN, "ruin_0"));
            }
            else if (id == EMBER)
            {
                res.Add(WallBuilder.FromGrid(GRIDS_EMBER[0], "construction_0"));
                for (int f = 0; f < GRIDS_EMBER.Length; f++) res.Add(WallBuilder.FromGrid(GRIDS_EMBER[f], "main_0"));
                res.Add(WallBuilder.FromGrid(GRIDS_EMBER[0], "ruin_0"));
            }
            else if (id == GFIRE)
            {
                res.Add(WallBuilder.FromGrid(FLAMES_A[0], "construction_0"));
                string[][][] sets = new string[][][] { FLAMES_A, FLAMES_B, FLAMES_C };
                for (int v = 0; v < sets.Length; v++)
                    for (int f = 0; f < sets[v].Length; f++) res.Add(WallBuilder.FromGrid(sets[v][f], "main_" + v));
                res.Add(WallBuilder.FromGrid(FLAMES_A[1], "ruin_0"));
            }
            else if (id == ALERT0 || id == ALERT1 || id == ALERT2)
            {
                int lv = id == ALERT0 ? 0 : (id == ALERT1 ? 1 : 2);
                res.Add(WallBuilder.FromGrid(AlertGrid(lv, 0), "construction_0"));
                res.Add(WallBuilder.FromGrid(AlertGrid(lv, 0), "main_0"));
                res.Add(WallBuilder.FromGrid(AlertGrid(lv, 1), "main_0"));
                res.Add(WallBuilder.FromGrid(GRID_PENNANT_RUIN, "ruin_0"));
            }
            else
            {
                res.Add(WallBuilder.FromGrid(GRID_PENNANT_BUILD, "construction_0"));
                for (int f = 0; f < GRIDS_PENNANT.Length; f++) res.Add(WallBuilder.FromGrid(GRIDS_PENNANT[f], "main_0"));
                res.Add(WallBuilder.FromGrid(GRID_PENNANT_RUIN, "ruin_0"));
            }
            if (mini != null)
            {
                UnityEngine.Sprite m = UnityEngine.Sprite.Create(mini.texture, mini.rect,
                    new UnityEngine.Vector2(mini.pivot.x / mini.rect.width, mini.pivot.y / mini.rect.height), mini.pixelsPerUnit);
                m.name = "mini_0";
                res.Add(m);
            }
            return res.ToArray();
        }

        public static void CreateAssets(BuildingAsset real)
        {
            for (int i = 0; i < IDS.Length; i++)
            {
                Siege.MakeAssetPublic(real, IDS[i], IDS[i] == PENNANT ? 6f : 50000f);   // флажок хрупкий: атакующие сбивают его сразу и не застревают на нём
                // у башни, с которой скопирован ассет, площадь 3x2 клетки, а игра сносит всё, что стоит на занятых клетках: эффекту нужна одна
                BuildingAsset fa = AssetManager.buildings.get(IDS[i]);
                if (fa != null) fa.fundament = new BuildingFundament(0, 0, 0, 0);
            }
            int made = 0; for (int i = 0; i < IDS.Length; i++) if (AssetManager.buildings.get(IDS[i]) != null) made++;
            WallBuilder.Log("fx assets ready: " + made + " of " + IDS.Length + (ON ? "" : " (effects off)"));
            BuildingAsset e = AssetManager.buildings.get(EMBER); if (e != null) e.animation_speed = 2.2f;
            BuildingAsset g = AssetManager.buildings.get(GFIRE); if (g != null) g.animation_speed = 8f;
            BuildingAsset p = AssetManager.buildings.get(PENNANT); if (p != null) p.animation_speed = 3f;
            foreach (string aid in new string[] { ALERT0, ALERT1, ALERT2 }) { BuildingAsset ab = AssetManager.buildings.get(aid); if (ab != null) ab.animation_speed = 2.5f; }
        }

        // ---------- Создание и жизнь ----------

        private static int fx_logged;
        private static void FxLog(string m) { if (fx_logged++ < 60) WallBuilder.Log("fx: " + m); }

        private static Building Spawn(string id, WorldTile near, int scatter, Kingdom k, double life, City owner, long key)
        {
            try
            {
                if (!ON) { FxLog("effects are off"); return null; }
                if (near == null || k == null || !k.isAlive()) { FxLog(id + ": no tile or kingdom"); return null; }
                if (items.Count >= MAX) { FxLog(id + ": limit reached"); return null; }
                WorldTile c = scatter > 0
                    ? World.world.GetTile(near.x + Randy.randomInt(-scatter, scatter + 1), near.y + Randy.randomInt(-scatter, scatter + 1))
                    : near;
                WorldTile t = Siege.FreeSpot(c, 1);
                if (t == null) { FxLog(id + ": no free tile near " + near.x + "," + near.y); return null; }
                Kingdom nk = null;
                try { nk = World.world.kingdoms_wild.get("ruins"); } catch (System.Exception) { }
                Building b = Siege.Put(id, t, nk != null ? nk : k);      // обломки и флажки ничьи: атакующие не принимают их за цель
                if (b == null) { FxLog(id + ": building was not created at " + t.x + "," + t.y); return null; }
                Item it = new Item();
                it.b = b; it.id = id; it.owner = owner; it.key = key;
                it.expire = World.world.getCurWorldTime() + life;
                items.Add(it);
                FxLog(id + " placed at " + t.x + "," + t.y);
                return b;
            }
            catch (System.Exception e) { FxLog(id + " failed: " + e.Message); return null; }
        }

        // Участок стены обрушен: куски дерева и камня вокруг, у каменной стены ещё и отдельные кирпичи
        public static void Rubble(WorldTile at, Kingdom k, City owner, bool stone)
        {
            if (!ON || at == null) return;
            int n = Randy.randomInt(4, 8);
            for (int i = 0; i < n; i++) Spawn(DEBRIS, at, 4, k, 150.0 + Randy.randomFloat(0f, 60f), owner, 0);
            int bricks = stone ? Randy.randomInt(5, 10) : Randy.randomInt(0, 2);
            for (int i = 0; i < bricks; i++) Spawn(BRICK, at, 4, k, 150.0 + Randy.randomFloat(0f, 60f), owner, 0);
        }

        // Ворота выбиты: тлеющие балки и обломки
        public static void GateWreck(WorldTile at, Kingdom k, City owner)
        {
            if (!ON || at == null) return;
            int n = Randy.randomInt(4, 7);
            for (int i = 0; i < n; i++) Spawn(DEBRIS, at, 4, k, 180.0 + Randy.randomFloat(0f, 60f), owner, 0);
            int bricks = Randy.randomInt(2, 5);
            for (int i = 0; i < bricks; i++) Spawn(BRICK, at, 4, k, 180.0 + Randy.randomFloat(0f, 60f), owner, 0);
        }

        // Ворота пали: огонь на них гаснет совсем, остаются только обломки
        public static void GateFireOut(City city, int key)
        {
            long k = city.getID() * 64L + key;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                Item it = items[i];
                if (it.id != GFIRE || it.key != k) continue;
                try { if (it.b != null && it.b.isAlive()) it.b.startDestroyBuilding(); } catch (System.Exception) { }
                items.RemoveAt(i);
            }
        }

        // Башня разрушена во время осады
        public static void TowerWreck(Building tower)
        {
            try
            {
                if (!ON || tower == null || tower.asset == null || tower.current_tile == null || tower.kingdom == null) return;
                string id = tower.asset.id;
                if (!id.StartsWith("watch_tower")) return;
                City c = tower.city;
                if (c == null || !Siege.UnderSiege(c)) return;
                if (tower.isOnRemove()) return;
                WorldTile at = tower.current_tile;
                Kingdom k = tower.kingdom;
                Spawn(EMBER, at, 1, k, 220.0, c, 0);
                Spawn(EMBER, at, 2, k, 200.0, c, 0);
                int n = Randy.randomInt(5, 9);
                for (int i = 0; i < n; i++) Spawn(DEBRIS, at, 4, k, 180.0 + Randy.randomFloat(0f, 60f), c, 0);
                int bricks = Randy.randomInt(3, 7);
                for (int i = 0; i < bricks; i++) Spawn(BRICK, at, 4, k, 180.0 + Randy.randomFloat(0f, 60f), c, 0);
            }
            catch (System.Exception) { }
        }

        // Сильно побитые ворота горят: чем меньше прочность, тем больше огней
        public static void GateFire(City city, int key, float frac, List<WorldTile> gateTiles)
        {
            Scroll.Note(city, "guns");
            if (!ON || city == null || gateTiles == null || gateTiles.Count == 0 || frac >= 0.3f) return;
            int want = frac < 0.15f ? 9 : (frac < 0.22f ? 6 : 4);      // много небольших языков, а не один большой
            long k = city.getID() * 64L + key;
            int have = 0;
            for (int i = 0; i < items.Count; i++) if (items[i].id == GFIRE && items[i].key == k && items[i].b != null && items[i].b.isAlive()) have++;
            for (; have < want; have++)
            {
                WorldTile t0 = gateTiles[Randy.randomInt(0, gateTiles.Count)];
                if (Spawn(GFIRE, t0, 1, city.kingdom, 30.0 + Randy.randomFloat(0f, 30f), city, k) == null) break;
            }
        }

        // Флажки на башнях осаждённого города (небольшие, не больше нескольких штук)
        private static readonly HashSet<long> ever_flagged = new HashSet<long>();
        public static void Pennants(City city, double now)
        {
            if (!ON || !PENNANTS || city == null || !city.isAlive() || city.kingdom == null) return;
            if (!Siege.UnderSiege(city)) ever_flagged.Clear();
            long ck = city.getID();
            double nx;
            if (next_pennants.TryGetValue(ck, out nx) && now < nx) return;
            next_pennants[ck] = now + 12.0;
            int count = 0;
            HashSet<Building> flagged = new HashSet<Building>();
            for (int i = 0; i < items.Count; i++)
                if (items[i].id == PENNANT && items[i].owner == city && items[i].b != null && items[i].b.isAlive()) { count++; if (items[i].tower != null) flagged.Add(items[i].tower); }
            if (count >= 8) return;
            foreach (Building tb in city.buildings)
            {
                if (count >= 8) break;
                if (tb == null || tb.asset == null || !tb.isUsable() || tb.current_tile == null || !tb.asset.id.StartsWith("watch_tower")) continue;
                if (flagged.Contains(tb) || ever_flagged.Contains(tb.getID())) continue;      // сбитый флажок второй раз не ставим
                Building b = Spawn(PENNANT, World.world.GetTile(tb.current_tile.x + 1, tb.current_tile.y), 0, city.kingdom, 1800.0, city, 0);
                if (b == null) continue;
                items[items.Count - 1].tower = tb;
                ever_flagged.Add(tb.getID());
                count++;
            }
        }

        // Убираем просроченные, лишние и ненужные эффекты (по кадрам, не чаще раза за кадр)
        public static void Pump()
        {
            int frame = UnityEngine.Time.frameCount;
            if (frame == last_frame || items.Count == 0) return;
            if (frame - last_frame < 20 && last_frame >= 0) return;
            last_frame = frame;
            double now = World.world.getCurWorldTime();
            for (int i = items.Count - 1; i >= 0; i--)
            {
                Item it = items[i];
                bool drop = it.b == null || !it.b.isAlive() || now >= it.expire;
                if (!drop && it.id == PENNANT)
                {
                    bool over = it.owner == null || !it.owner.isAlive() || !Siege.UnderSiege(it.owner);
                    if (over) { if (it.gone_since < 0) it.gone_since = now; else if (now - it.gone_since > 20.0) drop = true; }
                    else it.gone_since = -1;
                    if (!drop && it.tower != null && !it.tower.isAlive()) drop = true;
                }
                if (!drop) continue;
                try { if (it.b != null && it.b.isAlive()) it.b.startDestroyBuilding(); } catch (System.Exception) { }
                items.RemoveAt(i);
            }
        }
    }

    // Отладка: выгрузка спрайтов построек (игровых и наших) в PNG для сравнения стиля. Включается настройкой sprite_dump
    internal static class SpriteDump
    {
        private static bool ON = Cfg.Bool("sprite_dump", false, "выгрузить спрайты построек в папку ws_sprites (для сравнения стиля, один раз за запуск)");
        private static bool done;
        private static int start_tick = System.Environment.TickCount;

        public static void Tick()
        {
            if (!ON || done || World.world == null) return;
            if (System.Environment.TickCount - start_tick < 25000) return;
            done = true;
            try { Run(); } catch (System.Exception e) { WallBuilder.Log("sprite dump failed: " + e.Message); }
        }

        private static UnityEngine.Texture2D ReadCopy(UnityEngine.Sprite sp)
        {
            UnityEngine.Texture src = sp.texture;
            UnityEngine.RenderTexture rt = UnityEngine.RenderTexture.GetTemporary(src.width, src.height, 0, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.Linear);
            UnityEngine.Graphics.Blit(src, rt);
            UnityEngine.RenderTexture prev = UnityEngine.RenderTexture.active;
            UnityEngine.RenderTexture.active = rt;
            UnityEngine.Rect r = sp.textureRect;
            UnityEngine.Texture2D tex = new UnityEngine.Texture2D((int)r.width, (int)r.height, UnityEngine.TextureFormat.RGBA32, false);
            tex.ReadPixels(new UnityEngine.Rect(r.x, r.y, r.width, r.height), 0, 0);
            tex.Apply();
            UnityEngine.RenderTexture.active = prev;
            UnityEngine.RenderTexture.ReleaseTemporary(rt);
            return tex;
        }

        private static void Run()
        {
            string dir = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "ws_sprites");
            System.IO.Directory.CreateDirectory(dir);
            int n = 0, bad = 0;
            foreach (BuildingAsset b in AssetManager.buildings.list)
            {
                if (b == null || b.id == null || b.id.StartsWith("$")) continue;
                bool ours = Siege.IsSiegeId(b.id) || b.id == WallBuilder.CHEAP_TOWER_ID || b.id.StartsWith("coast_tower") || b.id.StartsWith("fx_");
                if (!ours && !b.city_building && !b.id.StartsWith("watch_tower") && !b.id.StartsWith("tent") && !b.id.StartsWith("wall") && !b.id.Contains("fire")) continue;
                try
                {
                    UnityEngine.Sprite[] list = b.loadBuildingSpriteList();
                    if (list == null) continue;
                    int taken = 0;
                    foreach (UnityEngine.Sprite sp in list)
                    {
                        if (sp == null || sp.texture == null || taken >= 3) continue;
                        if (!sp.name.StartsWith("main")) continue;
                        taken++;
                        UnityEngine.Texture2D t = ReadCopy(sp);
                        string fn = (ours ? "OURS__" : "GAME__") + b.id + "__" + sp.name + ".png";
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, fn), UnityEngine.ImageConversion.EncodeToPNG(t));
                        UnityEngine.Object.Destroy(t);
                        n++;
                    }
                }
                catch (System.Exception) { bad++; }
            }
            WallBuilder.Log("sprite dump: " + n + " sprites written to " + dir + " (failed assets: " + bad + ")");
        }
    }

    // Своё действие: над головой появляется облачко с иконкой (игра рисует его, пока выполняется действие-разговор)
    internal class BehWsAlarm : ai.behaviours.BehaviourActionActor
    {
        public BehWsAlarm() { socialize = true; }
        public override ai.behaviours.BehResult execute(Actor pActor)
        {
            try { pActor.forceSocializeTopic("ui/Icons/iconWarning"); } catch (System.Exception) { }
            pActor.timer_action = 1.6f;
            return ai.behaviours.BehResult.Continue;
        }
    }

    // Свои задания юнитов с подписями: их видно в окне юнита
    internal static class WsTasks
    {
        private static bool done;

        private static void Add(string id, string key, bool alarm, float speed, float waitMin, float waitMax)
        {
            if (AssetManager.tasks_actor.get(id) != null) return;
            ai.behaviours.BehaviourTaskActor t = new ai.behaviours.BehaviourTaskActor();
            t.id = id;
            t.locale_key = key;
            t.speed_multiplier = speed;
            t.setIcon("ui/Icons/iconWarning");
            if (alarm) t.addBeh(new BehWsAlarm());
            t.addBeh(new ai.behaviours.BehGoToTileTarget());
            t.addBeh(new ai.behaviours.BehRandomWait(waitMin, waitMax));
            AssetManager.tasks_actor.add(t);
        }

        public static void Ensure()
        {
            if (done) return;
            done = true;
            try
            {
                Add("ws_muster", "ws_task_muster", false, 1.15f, 1f, 3f);
                Add("ws_refugee", "ws_task_refugee", true, 1.25f, 1f, 3f);
                Add("ws_refugee_go", "ws_task_refugee", false, 1.25f, 1f, 3f);
                Add("ws_ram", "ws_task_ram", false, 1f, 3f, 4f);
            }
            catch (System.Exception e) { WallBuilder.Log("custom tasks failed: " + e.Message); }
        }

        // Отдать приказ идти в точку под своим заданием (прежнее задание отменяется)
        public static void Order(Actor a, WorldTile dest, string taskId)
        {
            Ensure();
            a.cancelAllBeh();
            if (AssetManager.tasks_actor.get(taskId) == null) { a.goTo(dest); return; }
            a.beh_tile_target = dest;
            a.setTask(taskId, false, false, false);
        }
    }

    // Когда армия врага выступила к городу, часть жителей (половина взрослых и все дети) собирается в центре:
    // слух о войне. К началу осады они уже внутри стен, а нападающим есть кого искать
    internal static class Muster
    {
        private static bool CIV_ON = Cfg.Bool("muster_civilians", true, "когда армия врага выступила к городу, половина жителей и все дети собираются в центре");
        private static int CIV_PCT = Cfg.Int("muster_civilians_percent", 50, "какая доля взрослых жителей идёт в центр (0-100)");
        private static bool KIDS = Cfg.Bool("muster_children", true, "дети тоже идут в центр");
        private static int ARMY_MIN = Cfg.Int("muster_army_min", 8, "сколько бойцов врага должно идти к городу, чтобы жители собрались");
        private static int DIST = Cfg.Int("muster_distance", 110, "жители собираются, когда войско врага ближе стольких клеток к городу");
        private static int RADIUS = Cfg.Int("muster_radius", 7, "радиус сбора вокруг центра города");
        private const int MAX_PER_PULSE = 80;

        private static readonly Dictionary<City, double> next_warn = new Dictionary<City, double>();
        private static readonly Dictionary<City, double> last_call = new Dictionary<City, double>();

        public static void Warn(City target, City attacker, Army army, double now)
        {
            if (!CIV_ON || target == null || army == null) return;
            WsTasks.Ensure();
            double nx;
            if (next_warn.TryGetValue(target, out nx) && now < nx) return;
            next_warn[target] = now + 5.0;
            try
            {
                WorldTile tc = target.getTile();
                if (tc == null) return;
                WorldTile mass; List<WorldTile> crowd;
                int n = Siege.Gather(army, tc, DIST, out mass, out crowd);
                if (n < ARMY_MIN) return;
                double last;
                bool fresh = !last_call.TryGetValue(target, out last) || now - last > 60.0;
                last_call[target] = now;
                int sent = Call(target, tc);
                SendRefugees(target, attacker, tc, mass, now);
                if (fresh && sent > 0)
                    WallBuilder.Dbg("civilians of " + target.name + " called to the center: " + sent + " (enemy army of " + n + " from " + attacker.name + " is coming)");
            }
            catch (System.Exception e) { WallBuilder.Dbg("muster failed: " + e.Message); }
        }

        private static int Call(City target, WorldTile cc)
        {
            int sent = 0;
            foreach (Actor a in target.getUnits())
            {
                if (sent >= MAX_PER_PULSE) break;
                if (a == null || !a.isAlive() || a.isWarrior() || a.isKing() || a.current_tile == null || a.isInsideSomething()) continue;
                bool kid = KIDS && a.isBaby();
                if (!kid)
                {
                    int h = (int)(((a.getID() * 2654435761L) >> 11) & 0x7fffffff);
                    if (h % 100 >= CIV_PCT) continue;
                }
                WorldTile at = a.current_tile;
                if (!at.isSameIsland(cc)) continue;
                if (Toolbox.Dist(at.x, at.y, cc.x, cc.y) <= RADIUS + 2) continue;     // уже в центре
                WorldTile dest = World.world.GetTile(cc.x + Randy.randomInt(-RADIUS, RADIUS + 1), cc.y + Randy.randomInt(-RADIUS, RADIUS + 1));
                if (dest == null || dest.Type.block || dest.Type.wall || dest.is_liquid || !dest.isSameIsland(at)) continue;
                WsTasks.Order(a, dest, "ws_muster");
                sent++;
            }
            return sent;
        }

        // ---------- Беженцы ----------
        // Жители малых деревень того же королевства, что лежат между наступающей армией и крепостью, бегут в крепость:
        // половина взрослых и все дети. Деревня подходит, если в ней не больше REF_MAX_POP жителей, нет стен и её центр
        // не дальше REF_DIST от центра крепости

        private static bool REF_ON = Cfg.Bool("muster_refugees", true, "жители малых деревень между армией врага и крепостью бегут в крепость");
        private static int REF_MAX_POP = Cfg.Int("muster_refugee_max_pop", 90, "деревня, где не больше стольких жителей, считается малой");
        private static int REF_DIST = Cfg.Int("muster_refugee_distance", 70, "деревня не дальше стольких клеток от центра крепости отправляет беженцев");
        private static readonly HashSet<long> refugees = new HashSet<long>();
        private static readonly Dictionary<City, double> ref_logged = new Dictionary<City, double>();

        private static void SendRefugees(City fortress, City attacker, WorldTile fc, WorldTile mass, double now)
        {
            if (!REF_ON || mass == null || !WallBuilder.IsFortified(fortress)) return;
            float fax = mass.x - fc.x, fay = mass.y - fc.y;
            float fa = UnityEngine.Mathf.Sqrt(fax * fax + fay * fay);
            if (fa < 10f) return;
            foreach (City v in World.world.cities)
            {
                if (v == null || v == fortress || !v.isAlive() || v.kingdom == null || v.kingdom != fortress.kingdom) continue;
                if (v.status.population <= 0 || v.status.population > REF_MAX_POP) continue;
                if (WallBuilder.IsFortified(v) || WallBuilder.HasWalls(v)) continue;
                WorldTile vc = v.getTile();
                if (vc == null || !vc.isSameIsland(fc)) continue;
                float dvf = Toolbox.Dist(vc.x, vc.y, fc.x, fc.y);
                if (dvf > REF_DIST || dvf < 6f) continue;
                // деревня на стороне армии и ближе к ней, чем крепость
                float vx = vc.x - fc.x, vy = vc.y - fc.y;
                if (vx * fax + vy * fay <= 0f) continue;
                if (Toolbox.Dist(vc.x, vc.y, mass.x, mass.y) >= fa) continue;
                int sent = 0;
                foreach (Actor a in v.getUnits())
                {
                    if (sent >= MAX_PER_PULSE) break;
                    if (a == null || !a.isAlive() || a.isWarrior() || a.isKing() || a.current_tile == null || a.isInsideSomething()) continue;
                    bool kid = KIDS && a.isBaby();
                    if (!kid)
                    {
                        int h = (int)(((a.getID() * 2654435761L) >> 11) & 0x7fffffff);
                        if (h % 100 >= CIV_PCT) continue;
                    }
                    WorldTile at = a.current_tile;
                    if (!at.isSameIsland(fc)) continue;
                    if (Toolbox.Dist(at.x, at.y, fc.x, fc.y) <= RADIUS + 2) continue;
                    WorldTile dest = World.world.GetTile(fc.x + Randy.randomInt(-RADIUS, RADIUS + 1), fc.y + Randy.randomInt(-RADIUS, RADIUS + 1));
                    if (dest == null || dest.Type.block || dest.Type.wall || dest.is_liquid || !dest.isSameIsland(at)) continue;
                    // облачко показываем один раз, потом просто приказ с той же подписью
                    WsTasks.Order(a, dest, refugees.Add(a.getID()) ? "ws_refugee" : "ws_refugee_go");
                    sent++;
                }
                double last;
                if (sent > 0 && (!ref_logged.TryGetValue(v, out last) || now - last > 60.0))
                {
                    ref_logged[v] = now;
                    WallBuilder.Dbg("refugees from " + v.name + " (" + v.status.population + " people) to " + fortress.name + ": " + sent);
                }
            }
            if (refugees.Count > 4000) refugees.Clear();
        }
    }

    // Таран: изредка в лагере появляется тяжёлый зверь (пока вместо спрайта тарана берём готового носорога).
    // Он игнорирует всех, идёт к закрытым воротам и бьёт по ним, пока его не убьют защитники
    // Спрайты тарана: маленькая воксельная модель, нарисованная с 8 сторон по 2 кадра (по аналогии с лодкой в игре).
    // Спрайты создаются один раз при загрузке мода (текстуры можно делать только в главном потоке), а выбор кадра
    // для отрисовки идёт из потока рендера, поэтому там ничего не создаётся и не меняется.
    internal static class RamArt
    {
        private const int W = 64, H = 44, OX = 32, OY = 8, S = 2;
        public static UnityEngine.Sprite[][] sprites;       // [направление][кадр]
        private static readonly float[] HEADS = new float[] { 0f, -45f, -90f, -135f, 180f, 135f, 90f, 45f };

        private static UnityEngine.Color32 Pal(char c)
        {
            UnityEngine.Color32 r = PalRaw(c);
            if (r.r == 255 && r.g == 0 && r.b == 255) return r;
            const float K = 0.74f;      // таран темнее: иначе выбивается из стиля остальных построек
            return new UnityEngine.Color32((byte)(r.r * K), (byte)(r.g * K), (byte)(r.b * K), r.a);
        }

        private static UnityEngine.Color32 PalRaw(char c)
        {
            switch (c)
            {
                case 'L': return new UnityEngine.Color32(149, 104, 57, 255);
                case 'l': return new UnityEngine.Color32(173, 129, 76, 255);
                case 'm': return new UnityEngine.Color32(104, 66, 36, 255);
                case 'M': return new UnityEngine.Color32(107, 68, 34, 255);
                case 'D': return new UnityEngine.Color32(75, 58, 47, 255);
                case 'r': return new UnityEngine.Color32(117, 72, 38, 255);
                case 'R': return new UnityEngine.Color32(148, 95, 51, 255);
                case 's': return new UnityEngine.Color32(176, 174, 166, 255);
                case 'S': return new UnityEngine.Color32(128, 126, 124, 255);
                case 'w': return new UnityEngine.Color32(174, 148, 103, 255);
                case 'W': return new UnityEngine.Color32(146, 114, 71, 255);
                case 'F': return new UnityEngine.Color32(120, 170, 60, 255);
                case 'f': return new UnityEngine.Color32(92, 138, 44, 255);
                case 'b': return new UnityEngine.Color32(234, 226, 200, 255);
                case 'h': return new UnityEngine.Color32(195, 179, 125, 255);
                default: return new UnityEngine.Color32(255, 0, 255, 255);
            }
        }

        private struct Vox { public int x, y, z; public char c; }

        private static List<Vox> Model(int frame)
        {
            Dictionary<long, Vox> d = new Dictionary<long, Vox>();
            System.Action<int, int, int, char> put = (x, y, z, c) => { d[((long)(x + 8) * 100 + (y + 8)) * 100 + z] = new Vox { x = x, y = y, z = z, c = c }; };
            int Wd = 8;
            int sw = frame % 2;
            for (int x = 2; x < 14; x++) for (int y = 0; y < Wd; y++) { put(x, y, 2, 'm'); put(x, y, 3, 'M'); }
            foreach (int x in new int[] { 2, 13 }) foreach (int y in new int[] { 0, Wd - 1 }) for (int z = 4; z < 8; z++) put(x, y, z, 'D');
            for (int x = 3; x < 13; x++) foreach (int y in new int[] { 0, Wd - 1 }) for (int z = 4; z <= 5; z++) put(x, y, z, z == 4 ? 'm' : 'M');
            foreach (int x0 in new int[] { 4, 9 })
            {
                for (int x = x0; x < x0 + 3; x++) foreach (int y in new int[] { 0, Wd - 1 }) for (int z = 4; z <= 5; z++) put(x, y, z, z == 5 ? 'F' : 'f');
                put(x0 + 1, 0, 5, 'b'); put(x0 + 1, Wd - 1, 5, 'b');
            }
            for (int y = 0; y < Wd; y++) for (int z = 4; z < 8; z++) put(2, y, z, ((y + z) % 2 != 0) ? 'm' : 'M');
            int[][] layers = new int[][] { new int[] { 8, -1, Wd }, new int[] { 9, 0, Wd - 1 }, new int[] { 10, 1, Wd - 2 }, new int[] { 11, 2, Wd - 3 }, new int[] { 12, 3, Wd - 4 } };
            foreach (int[] ly in layers)
                for (int x = 1; x < 15; x++)
                    for (int y = ly[1]; y <= ly[2]; y++)
                        put(x, y, ly[0], ly[0] == 12 ? 'l' : (((x + ly[0]) % 2 == 0) ? 'R' : 'r'));
            foreach (int wx in new int[] { 4, 11 })
                foreach (int wy in new int[] { -1, Wd })
                    for (int dx = -2; dx <= 2; dx++)
                        for (int dz = -2; dz <= 2; dz++)
                        {
                            int dd = dx * dx + dz * dz;
                            if (dd > 5) continue;
                            int z = 2 + dz; if (z < 0) continue;
                            char c = dd == 0 ? 'h' : (dd <= 2 ? 'w' : 'W');
                            if (frame % 2 == 1 && (dd == 1 || dd == 2) && dx != 0 && dz != 0) c = 'W';
                            put(wx + dx, wy, z, c);
                        }
            int off = sw;
            for (int x = 0 + off; x < 20 + off; x++) for (int z = 5; z <= 6; z++) put(x, 3, z, z == 6 ? 'L' : 'M');
            for (int x = 19 + off; x < 22 + off; x++)
                for (int y = 2; y < 5; y++)
                    for (int z = 4; z < 8; z++)
                    {
                        if (x == 21 + off && (y == 2 || y == 4 || z == 4 || z == 7)) continue;
                        put(x, y, z, (z == 7 || x == 21 + off) ? 's' : 'S');
                    }
            return new List<Vox>(d.Values);
        }

        private static UnityEngine.Sprite Render(List<Vox> model, float headingDeg)
        {
            double h = headingDeg * System.Math.PI / 180.0;
            double cph = System.Math.Cos(38.0 * System.Math.PI / 180.0), sph = System.Math.Sin(38.0 * System.Math.PI / 180.0);
            double cx = 7.5, cy = 3.5;
            int n = model.Count;
            double[] dys = new double[n]; double[] sxs = new double[n]; double[] sys = new double[n];
            int[] idx = new int[n];
            for (int i = 0; i < n; i++)
            {
                double u = model[i].x - cx, v = model[i].y - cy;
                double dx = u * System.Math.Cos(h) - v * System.Math.Sin(h);
                double dy = u * System.Math.Sin(h) + v * System.Math.Cos(h);
                dys[i] = dy; sxs[i] = dx; sys[i] = model[i].z * cph + dy * sph; idx[i] = i;
            }
            System.Array.Sort(idx, (a, b) => { int c = (-dys[a]).CompareTo(-dys[b]); return c != 0 ? c : model[a].z.CompareTo(model[b].z); });
            UnityEngine.Color32[] px = new UnityEngine.Color32[W * H];
            for (int k = 0; k < n; k++)
            {
                int i = idx[k];
                int pxx = (int)System.Math.Round(OX + sxs[i] * S) - 1;
                int pyy = (int)System.Math.Round(sys[i] * S) - 1 + OY;       // снизу вверх
                UnityEngine.Color32 col = Pal(model[i].c);
                double sh = 0.86 + 0.02 * model[i].z;
                col = new UnityEngine.Color32((byte)System.Math.Min(255, col.r * sh), (byte)System.Math.Min(255, col.g * sh), (byte)System.Math.Min(255, col.b * sh), 255);
                for (int ax = 0; ax <= S; ax++)
                    for (int ay = 0; ay <= S; ay++)
                    {
                        int qx = pxx + ax, qy = pyy + ay;
                        if (qx < 0 || qy < 0 || qx >= W || qy >= H) continue;
                        px[qy * W + qx] = col;
                    }
            }
            UnityEngine.Texture2D tex = new UnityEngine.Texture2D(W, H, UnityEngine.TextureFormat.RGBA32, false);
            tex.filterMode = UnityEngine.FilterMode.Point;
            tex.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return UnityEngine.Sprite.Create(tex, new UnityEngine.Rect(0, 0, W, H), new UnityEngine.Vector2(0.5f, OY / (float)H), 1f);
        }

        public static bool Build()
        {
            if (sprites != null) return true;
            try
            {
                UnityEngine.Sprite[][] res = new UnityEngine.Sprite[8][];
                List<Vox>[] frames = new List<Vox>[] { Model(0), Model(1) };
                for (int d = 0; d < 8; d++)
                {
                    res[d] = new UnityEngine.Sprite[2];
                    for (int f = 0; f < 2; f++) { res[d][f] = Render(frames[f], HEADS[d]); res[d][f].name = "ws_ram_" + d + "_" + f; }
                }
                sprites = res;
                return true;
            }
            catch (System.Exception e) { WallBuilder.Log("ram sprites failed: " + e.Message); return false; }
        }

        public static int Sector(float angleDeg)
        {
            // направление движения (0 = восток, 90 = север) в ближайший из восьми ракурсов
            float a = angleDeg;
            int best = 0; float bd = 999f;
            for (int i = 0; i < 8; i++)
            {
                float d = System.Math.Abs(((a - HEADS[i]) % 360f + 540f) % 360f - 180f);
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }
    }

    internal static class Ram
    {
        private static bool ON = Cfg.Bool("siege_ram", true, "в осадном лагере иногда появляется таран, который идёт к закрытым воротам");
        private static int CHANCE = Cfg.Int("siege_ram_attempt_chance", 50, "шанс появления тарана (в процентах) при каждой из двух попыток (через 10 и 20 секунд после появления лагеря)");
        private static int CHANCE_SEA = Cfg.Int("siege_ram_attempt_chance_sea", 70, "то же для лагеря высадки с моря (плацдарма)");
        private static double ATTEMPT_DELAY = Cfg.Float("siege_ram_attempt_delay", 10f, "через сколько секунд после появления лагеря (и между попытками) пробуется появление тарана");
        private static float GATE_SECONDS = Cfg.Float("siege_ram_gate_seconds", 25f, "за сколько секунд таран выбивает целые ворота");
        private static int MAX_PER_CAMP = Cfg.Int("siege_ram_max_per_camp", 3, "сколько таранов может появиться у одного лагеря за осаду");
        private static float REGEN = Cfg.Float("siege_ram_regen", 0.005f, "доля здоровья тарана, которую он восстанавливает каждые 0,8 секунды (0 - не лечится)");

        private class Info
        {
            public Actor a;
            public SiegeCamp camp;
            public double born, next_pulse, next_move, next_sound, open_since = -1;
            public bool hit_logged;
            public float angle;                 // куда смотрит (градусы, 0 = восток, 90 = север)
            public UnityEngine.Vector2 last_pos;
            public int next_creak;
            public int moving_until;            // Environment.TickCount, до которого считаем, что таран едет
            public bool scaled;
            public bool crew;                   // у культуры черта «Таранщики»: здоровье +20%
            public int last_hp = -1, max_hp;
            public WorldTile last_stand;
        }
        public const string ID = "ws_ram_unit";
        private static float RAM_SPEED = Cfg.Float("siege_ram_speed_mult", 1.2f, "во сколько раз таран быстрее носорога");
        private static float RAM_HP_BONUS = Cfg.Float("siege_ram_hp_bonus", 1350f, "сколько здоровья добавляется тарану к здоровью носорога (для прочности)");
        private static ActorAsset asset;
        private static bool hp_boosted;
        private static float RAM_SCALE_K = Cfg.Float("siege_ram_size", 2.86f, "во сколько раз спрайт тарана длиннее носорога (подбор размера)");
        private static volatile Info[] snapshot = new Info[0];
        private static void Publish() { snapshot = rams.ToArray(); }

        // Свой тип юнита (копия носорога) с подменой спрайта; создаётся при загрузке мода, чтобы сохранения с тараном грузились
        public static void Init()
        {
            try
            {
                if (asset != null || AssetManager.actor_library.get(ID) != null) return;
                if (AssetManager.actor_library.get("rhino") == null) { WallBuilder.Log("ram: rhino asset not found, sprite not attached"); return; }
                if (!RamArt.Build()) return;
                ActorAsset t = AssetManager.actor_library.clone(ID, "rhino");
                t.can_flip = false;
                t.check_flip = (BaseSimObject _, WorldTile __) => false;
                t.need_colored_sprite = false;
                t.can_evolve_into_new_species = false;
                t.needs_to_be_explored = false;
                t.get_override_sprite = RamSprite;
                t.has_override_sprite = true;
                t.get_override_avatar_frames = (Actor _) => new UnityEngine.Sprite[] { RamArt.sprites[0][0], RamArt.sprites[0][1] };
                t.has_override_avatar_frames = true;
                // вместо звуков носорога таран скрипит (см. Sfx "ram_creak")
                t.sound_idle = null; t.sound_idle_loop = null; t.sound_spawn = null; t.sound_death = null; t.sound_attack = null;
                asset = t;
                WallBuilder.Dbg("ram: unit asset " + ID + " created (sprite attached)");
            }
            catch (System.Exception e) { WallBuilder.Log("ram: unit asset failed: " + e.Message); asset = null; }
        }

        // Вызывается отрисовкой (в том числе из параллельных потоков): только чтение готовых данных
        private static UnityEngine.Sprite RamSprite(Actor a)
        {
            UnityEngine.Sprite[][] sp = RamArt.sprites;
            if (sp == null) return null;
            Info[] snap = snapshot;
            for (int i = 0; i < snap.Length; i++)
            {
                if (snap[i].a != a) continue;
                int sector = RamArt.Sector(snap[i].angle);
                bool moving = System.Environment.TickCount - snap[i].moving_until < 0;
                int frame = moving ? ((System.Environment.TickCount / 170) & 1) : 0;
                return sp[sector][frame];
            }
            return sp[0][0];
        }
        private static readonly List<Info> rams = new List<Info>();
        private static double next_pump;
        private const double PUMP_STEP = 0.4;

        // Вызывается каждый кадр (патч MapBox.Update): иначе приказы тарану шли бы только вместе с тиками осады
        public static void Heartbeat()
        {
            if (World.world == null) return;
            SpriteDump.Tick();
            int tc = System.Environment.TickCount;
            if (tc - sweep_tick >= 5000) { sweep_tick = tc; try { SweepOrphans(); } catch (System.Exception) { } }      // тараны из сохранения убираем сразу после загрузки
            if (rams.Count == 0) return;
            try { Track(); Pump(World.world.getCurWorldTime()); } catch (System.Exception) { }
        }

        // Куда смотрит таран: по смещению за последние кадры
        private static void Track()
        {
            for (int i = 0; i < rams.Count; i++)
            {
                Info it = rams[i];
                if (it.a == null || !it.a.isAlive()) continue;
                UnityEngine.Vector2 p = it.a.current_position;
                UnityEngine.Vector2 d = p - it.last_pos;
                if (d.sqrMagnitude < 0.0225f) continue;
                it.angle = (float)(System.Math.Atan2(d.y, d.x) * 180.0 / System.Math.PI);
                it.last_pos = p;
                it.moving_until = System.Environment.TickCount + 350;
                if (System.Environment.TickCount - it.next_creak >= 0)
                {
                    it.next_creak = System.Environment.TickCount + Randy.randomInt(1400, 2600);
                    Sfx.Play("ram_creak", it.a.current_tile);
                }
            }
        }

        private static double next_sweep;
        private static int sweep_tick = System.Environment.TickCount - 4000;
        // Тараны из загруженного сохранения (осады уже нет): убираем
        private static void SweepOrphans()
        {
            if (asset == null || World.world == null) return;
            List<Actor> units = World.world.units.getSimpleList();
            int removed = 0;
            for (int i = units.Count - 1; i >= 0; i--)
            {
                Actor u = units[i];
                if (u == null || !u.isAlive() || u.asset == null || u.asset.id != ID) continue;
                bool known = false;
                for (int k = 0; k < rams.Count; k++) if (rams[k].a == u) { known = true; break; }
                if (!known) { removed++; try { u.clearAttackTarget(); } catch (System.Exception) { } try { u.dieAndDestroy(AttackType.Other); } catch (System.Exception) { } }
            }
            if (removed > 0) WallBuilder.Dbg("siege ram: " + removed + " leftover ram(s) without a camp removed");
        }

        private static bool WallWait(SiegeCamp camp) { return WallBuilder.GatesClosed(camp.target); }

        private static Info AliveFor(SiegeCamp camp)
        {
            for (int i = 0; i < rams.Count; i++) if (rams[i].camp == camp) return rams[i];
            return null;
        }

        public static void Tick(SiegeCamp camp, double now)
        {
            if (!ON) return;
            try
            {
                Pump(now);
                if (now >= next_sweep) { next_sweep = now + 30.0; SweepOrphans(); }
                if (camp == null || camp.target == null) return;
                if (camp.ram_state == 0) { camp.ram_state = 1; camp.next_ram_roll = now + ATTEMPT_DELAY; return; }
                if (camp.ram_attempts >= 2 || camp.rams_made >= MAX_PER_CAMP || now < camp.next_ram_roll) return;
                if (!WallWait(camp)) return;
                if (AliveFor(camp) != null) return;
                bool crew = Traits.HasKingdom(camp.kingdom, Traits.RAMMERS);
                int chance = crew ? (camp.rams_made == 0 ? 100 : 50) : (camp.beach ? CHANCE_SEA : CHANCE);
                camp.ram_attempts++;
                camp.next_ram_roll = now + ATTEMPT_DELAY;
                if (Randy.randomInt(0, 100) >= chance) { WallBuilder.Log("siege ram: attempt " + camp.ram_attempts + " failed"); return; }
                camp.ram_attempts = 2;      // таран появился: новых попыток нет (у таранщиков их вернёт потеря тарана)
                Spawn(camp, now);
            }
            catch (System.Exception e) { WallBuilder.Log("ram tick failed: " + e.Message); }
        }

        private static void Spawn(SiegeCamp camp, double now)
        {
            string id = null;
            Init();
            if (asset != null) id = ID;
            string[] cand = new string[] { "rhino", "buffalo", "bear" };
            for (int i = 0; i < cand.Length && id == null; i++) if (AssetManager.actor_library.get(cand[i]) != null) id = cand[i];
            if (id == null) { WallBuilder.Log("ram: no suitable unit asset"); camp.rams_made = MAX_PER_CAMP; return; }
            WorldTile c = camp.center;
            WorldTile t = Siege.FreeSpot(World.world.GetTile(c.x + Randy.randomInt(-3, 4), c.y + Randy.randomInt(-3, 4)), 2);
            if (t == null) return;
            Actor a = World.world.units.spawnNewUnit(id, t, true);
            if (a == null) { WallBuilder.Log("ram: spawn failed"); return; }
            WsTasks.Ensure();
            a.setKingdom(camp.kingdom);
            a.restoreHealthPercent(1f);
            Info it = new Info();
            it.a = a; it.camp = camp; it.born = now; it.next_move = 0;
            it.last_pos = a.current_position;
            it.crew = Traits.HasKingdom(camp.kingdom, Traits.RAMMERS);
            WorldTile ct = camp.target.getTile();
            if (ct != null) it.angle = (float)(System.Math.Atan2(ct.y - t.y, ct.x - t.x) * 180.0 / System.Math.PI);
            rams.Add(it);
            Publish();
            if (id == ID) FitScale(a);
            camp.rams_made++;
            WallBuilder.Dbg("siege ram (" + id + ") appeared at " + t.x + "," + t.y + " for " + camp.attacker.name + " -> " + camp.target.name + " (kingdom of the unit: " + (a.kingdom != null ? a.kingdom.name : "none") + ")");
        }

        // Размер спрайта подбираем по носорогу: берём его кадр ходьбы и растягиваем модель до нужной длины
        private static void FitScale(Actor a)
        {
            try
            {
                a.checkAnimationContainer();
                UnityEngine.Sprite fr = a.animation_container != null && a.animation_container.walking != null && a.animation_container.walking.frames.Length > 0 ? a.animation_container.walking.frames[0] : null;
                ActorAsset rh = AssetManager.actor_library.get("rhino");
                float rs = rh != null && rh.base_stats["scale"] > 0f ? rh.base_stats["scale"] : 0.25f;
                float k = 1f;
                if (fr != null) k = RAM_SCALE_K * fr.rect.width / 44f;      // 44 - ширина длинной стороны нашего спрайта в пикселях
                asset.base_stats["scale"] = rs * k;
                a.setStatsDirty();
                try { a.updateStats(); } catch (System.Exception) { }
                a.restoreHealthPercent(1f);
                WallBuilder.Dbg("ram: sprite scale " + (rs * k) + " (rhino " + rs + ", frame width " + (fr != null ? fr.rect.width.ToString() : "?") + "), health " + a.getHealth() + "/" + a.getMaxHealth());
            }
            catch (System.Exception e) { WallBuilder.Log("ram: scale fit failed: " + e.Message); }
        }

        private static void Pump(double now)
        {
            if (rams.Count == 0 || now < next_pump) return;
            next_pump = now + PUMP_STEP;
            for (int i = rams.Count - 1; i >= 0; i--)
            {
                Info it = rams[i];
                try
                {
                    if (it.a == null || !it.a.isAlive())
                    {
                        string by = "?";
                        try { if (it.a != null && it.a.attackedBy != null) by = it.a.attackedBy.name + " (" + (it.a.attackedBy.kingdom != null ? it.a.attackedBy.kingdom.name : "no kingdom") + ")"; else by = "nobody"; } catch (System.Exception) { }
                        WallBuilder.Dbg("siege ram destroyed after " + (int)(now - it.born) + " s, last hit by " + by + ", last health " + it.last_hp + ", max " + it.max_hp);
                        if (it.camp != null)
                        {
                            it.camp.next_ram_roll = now + ATTEMPT_DELAY;
                            if (Traits.HasKingdom(it.camp.kingdom, Traits.RAMMERS)) it.camp.ram_attempts = 0;   // таранщики присылают новый таран с шансом 50%
                        }
                        rams.RemoveAt(i);
                        continue;
                    }
                    City target = it.camp != null ? it.camp.target : null;
                    bool closed = target != null && target.isAlive() && WallBuilder.GatesClosed(target);
                    if (closed) it.open_since = -1; else if (it.open_since < 0) it.open_since = now;
                    if (target == null || !target.isAlive() || (it.open_since >= 0 && now - it.open_since > 15.0))
                    {
                        WallBuilder.Dbg("siege ram leaves (gates are open or the city is gone)");
                        Kill(it);
                        rams.RemoveAt(i);
                        continue;
                    }
                    if (!closed) continue;
                    it.max_hp = it.a.getMaxHealth();
                    int hp = it.a.getHealth();
                    if (it.last_hp >= 0 && hp < it.last_hp && it.a.current_tile != null)
                        MusicBox.playSound("event:/SFX/HIT/HitWood", it.a.current_tile, pGameViewOnly: true, pVisibleOnly: true);
                    it.last_hp = hp;
                    it.a.clearAttackTarget();
                    if (REGEN > 0f) it.a.restoreHealthPercent(REGEN * 0.5f);
                    WorldTile at = it.a.current_tile;
                    WorldTile gate, stand;
                    if (at == null || !WallBuilder.GateApproach(target, at, out gate, out stand)) continue;
                    float d = Toolbox.Dist(at.x, at.y, gate.x, gate.y);
                    // держим таран у ворот: если отошёл от места стоянки, приказ повторяется (ИИ юнита иначе уводит его в сторону)
                    float ds = Toolbox.Dist(at.x, at.y, stand.x, stand.y);
                    if (it.last_stand != stand) { it.last_stand = stand; it.next_move = 0; }
                    if (ds > 1.4f && now >= it.next_move)
                    {
                        it.next_move = now + (it.a.is_moving ? 1.5 : 0.4);      // в пути не дёргаем, остановился - сразу повторяем
                        WsTasks.Order(it.a, stand, "ws_ram");
                    }
                    if (d <= 3.6f)
                    {
                        if (System.Environment.TickCount - it.moving_until >= 0) it.angle = (float)(System.Math.Atan2(gate.y - at.y, gate.x - at.x) * 180.0 / System.Math.PI);
                        WallBuilder.GateRamHit(target, at, (float)PUMP_STEP / GATE_SECONDS);
                        if (now >= it.next_sound) { it.next_sound = now + 1.5; if (!Sfx.Play("ram_bash", gate)) Sfx.Play("hit_gate", gate); }
                        if (!it.hit_logged) { it.hit_logged = true; WallBuilder.Dbg("siege ram is hitting the gates of " + target.name); }
                    }
                }
                catch (System.Exception e) { WallBuilder.Log("ram pump failed: " + e.Message); rams.RemoveAt(i); }
            }
            Publish();
        }

        // таран не реагирует на врагов: ему нельзя назначить цель атаки, значит он не бросается в драку и не убегает
        // характеристики пересчитываются из подвида, а не из ассета юнита, поэтому прочность таран получает здесь
        public static void StatsPre(Actor __instance, ref bool __state)
        {
            try { __state = __instance != null && __instance.asset != null && __instance.asset.id == ID && __instance.isStatsDirty(); } catch (System.Exception) { __state = false; }
        }
        public static void StatsPost(Actor __instance, bool __state)
        {
            if (!__state) return;
            try
            {
                __instance.stats["health"] += RAM_HP_BONUS;
                __instance.stats["armor"] += 15f;
                __instance.stats["speed"] *= RAM_SPEED;
                Info[] sn = snapshot;
                for (int i = 0; i < sn.Length; i++)
                    if (sn[i].a == __instance) { if (sn[i].crew) __instance.stats["health"] *= 1.2f; break; }
            }
            catch (System.Exception) { }
        }

        // защита от NullReferenceException в Actor.checkCurrentEnemyTarget (цель пропала, пока флаг цели ещё стоит)
        public static bool EnemyCheckPrefix(Actor __instance, ref bool __result)
        {
            try
            {
                if (__instance == null) { __result = false; return false; }
                if (__instance.has_attack_target && __instance.attack_target == null) { __instance.clearAttackTarget(); __result = false; return false; }
                if (__instance.asset != null && __instance.asset.id == ID) { __result = false; return false; }
            }
            catch (System.Exception) { }
            return true;
        }

        public static System.Exception EnemyCheckFinalizer(Actor __instance, System.Exception __exception, ref bool __result)
        {
            if (__exception == null) return null;
            try { __result = false; if (__instance != null) __instance.clearAttackTarget(); } catch (System.Exception) { }
            double now = 0; try { now = World.world.getCurWorldTime(); } catch (System.Exception) { }
            if (enemy_err_count < 12 || now - last_enemy_err > 30.0)
            {
                enemy_err_count++; last_enemy_err = now;
                string det = "";
                try
                {
                    det = " has_target=" + __instance.has_attack_target + " target_null=" + (__instance.attack_target == null);
                    if (__instance.attack_target != null) det += " target=" + (__instance.attack_target.isActor() ? "actor" : "building") + " alive=" + __instance.attack_target.isAlive();
                    det += " hp=" + __instance.data.health + "/" + __instance.getMaxHealth() + " kingdom=" + (__instance.kingdom != null ? __instance.kingdom.name : "null");
                }
                catch (System.Exception) { det += " (details failed)"; }
                string[] st = (__exception.StackTrace ?? "").Split('\n');
                WallBuilder.Log("checkCurrentEnemyTarget error swallowed #" + enemy_err_count + ": " + __exception.GetType().Name + " at " + st[0].Trim() + (st.Length > 1 ? " <- " + st[1].Trim() : "") + (__instance != null && __instance.asset != null ? " actor=" + __instance.asset.id : "") + det);
            }
            return null;
        }
        private static double last_enemy_err = -100;
        private static int enemy_err_count;

        public static bool NoTargetPrefix(Actor __instance)
        {
            try { if (__instance != null && __instance.asset != null && __instance.asset.id == ID) return false; } catch (System.Exception) { }
            return true;
        }

        private static void Kill(Info it)
        {
            try { if (it.a != null && it.a.isAlive()) it.a.dieAndDestroy(AttackType.Other); } catch (System.Exception) { }
        }

        public static void Remove(SiegeCamp camp)
        {
            for (int i = rams.Count - 1; i >= 0; i--)
                if (rams[i].camp == camp) { Kill(rams[i]); rams.RemoveAt(i); }
            Publish();
        }
    }


    // Замер: сколько времени за кадр тратит мод
    internal static class Perf
    {
        private static int frame = -1;
        private static double frame_ms;
        private static double total_ms;
        private static double worst_ms;
        private static int frames;
        private static int calls;
        private static float next_report;
        private static int last_clean_frame;

        public static bool Enabled = Cfg.Bool("perf_log", false, "писать в лог, сколько времени за кадр тратит мод (раз в минуту)");

        public static long Begin() { return System.Diagnostics.Stopwatch.GetTimestamp(); }

        public static void End(long t0)
        {
            double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            int f = UnityEngine.Time.frameCount;
            if (f != frame)
            {
                if (frame >= 0) { total_ms += frame_ms; frames++; if (frame_ms > worst_ms) worst_ms = frame_ms; }
                frame_ms = 0;
                frame = f;
            }
            frame_ms += ms;
            calls++;

            if (f - last_clean_frame > 900)
            {
                last_clean_frame = f;
                WallBuilder.CleanDead();
            }
            if (Enabled && UnityEngine.Time.realtimeSinceStartup >= next_report)
            {
                next_report = UnityEngine.Time.realtimeSinceStartup + 60f;
                if (frames > 0)
                    WallBuilder.Dbg("perf: avg " + (total_ms / frames).ToString("0.00") + " ms/frame, worst " + worst_ms.ToString("0.0")
                        + " ms, frames " + frames + ", city ticks " + calls + ", " + WallBuilder.StateInfo());
                total_ms = 0; worst_ms = 0; frames = 0; calls = 0;
            }
        }
    }

    public class SiegeCamp
    {
        public double next_assault, next_assault_log;
        public City attacker;
        public City target;
        public Kingdom kingdom;
        public WorldTile center;
        public List<WorldTile> pave = new List<WorldTile>();
        public List<WorldTile> palisade = new List<WorldTile>();
        public int pave_total;
        public List<TileChange> changed = new List<TileChange>();
        public List<Building> pal_b = new List<Building>();        // секции частокола (здания)
        public HashSet<WorldTile> pal_v = new HashSet<WorldTile>();     // секции вдоль z (боковые стороны)
        public Building[] tents = new Building[6];
        public Building catapult;
        public Building[] extra_guns = new Building[2];      // второе и третье орудие больших армий
        public int[] extra_builds = new int[2];
        public int ticks;
        public int rams_made;
        public int ram_state, ram_attempts;
        public double next_ram_roll;
        public int low_ticks;
        public int build_cooldown;
        public int built_total;
        public int[] tent_builds = new int[6];
        public int shooter_builds;
        public string shooter_id;
        public Building fire;
        public bool fire_done;
        public Building tower;
        public int tower_builds;
        public Building banner;
        public bool banner_done;
        public Building[] props = new Building[3];
        public int[] prop_builds = new int[3];
        public int prop_start = -1;
        public bool placed_any;
        public bool beach;              // плацдарм: лагерь высадившейся армии (с другого острова)
        public double empty_since = -1;
        public double next_recall;
        public double next_unstuck;
        public double next_side_check;
        public int side_miss;
    }

    // Клетка ворот: что было под ней (дорога/земля) и закрыта ли сейчас
    // Береговые башни: три вида спрайтов по расе города
    public static class Coast
    {
        public const string LIGHT_ID = "coast_tower_light";     // каменная башня с сигнальным огнём: люди, эльфы
        public const string DWARF_ID = "coast_tower_dwarf";     // светлый каменный форт: гномы
        public const string ORC_ID = "coast_tower_orc";         // форт из дерева и кости: орки
        public static readonly string[] IDS = new string[] { LIGHT_ID, DWARF_ID, ORC_ID };

        public static bool IsCoastId(string id) { return id == LIGHT_ID || id == DWARF_ID || id == ORC_ID; }

        public static string IdFor(string arch)
        {
            if (arch == "dwarf") return DWARF_ID;
            if (arch == "orc") return ORC_ID;
            return LIGHT_ID;
        }

        // GRID_LIGHT - каменная сторожевая башня с сигнальным огнём (люди, эльфы)
        // Y = огонёк (ночью светится), F/f/n = цвет королевства
        static readonly string[] GRID_LIGHT = new string[] {
            ".....y.....",
            "....oYo....",
            ".sSsoYosSs.",
            "sS@@oYo@StS",
            "S@@@kYk@@St",
            "sSsSsSsSsSt",
            ".@@@$##$%%.",
            ".@@FFf@##$.",
            ".##FF@d%%%.",
            ".@#FF@d%%%.",
            ".#@FFn#$$%.",
            ".##nnn$%%%.",
            ".@@nnx#$%$.",
            ".###@@@#$%.",
            ".#@d$$$%%%.",
            ".#@d##$##$.",
            ".@@@##@##$.",
            ".###$$@d%%.",
            ".@@@$@@d%%.",
            ".###$$$%$%.",
            ".#@d$$$%%%.",
            ".@@d##$$$%.",
            ".@@@@@#$$%.",
            ".###$$$%%%.",
            "@@@%DDd$$%%",
            "@@ggDmd$$Zg",
            "###%DmZ%g%Z",
            ".g.ggZ.ZZZg"
        };
        static readonly string[] GRID_DWARF = new string[] {
            ".........Pz..",
            ".........PFf.",
            ".........Pn..",
            "pUpU.....pUpU",
            "pppppUpUpppXX",
            "pUUUUpppppUXX",
            "UpppUpppXXXX0",
            "ppdppFFfUpdXX",
            "UpdUUFfnXpd00",
            "ppUppFfnUX0XX",
            "pppppFnnUXXXX",
            "UpdUUnnxX0000",
            "UpdpUpnpXXXX0",
            "pppppppppXXXX",
            "pUUUUDDDUUUXX",
            "pUUUDDmDDUUXX",
            "pUUUDDmDDUUXX",
            "gX0XDDmDDZ0X0",
            "gg0XDDmDgX0X0",
            "..Zg.gZ.Z.ZZZ"
        };
        static readonly string[] GRID_ORC = new string[] {
            "......dz.......",
            ".b...zFFd...b..",
            ".Bb.zFFFfn.bB..",
            ".uBbzFFffnnbBu.",
            "..bBzFffffnBb..",
            "...bFFFfffnnb..",
            "..bdbMdDDMdBb..",
            ".bBdbMmDDMmdBb.",
            "bBu.DduubuDMuB.",
            ".uD.DmuuBuDmDub",
            "BdDFddDuuuDmMu.",
            "bmFFDmMdDdDmMdB",
            ".mDFDmMmDmDmMmb",
            ".mfFDbbbbbDmMm.",
            ".CdFdbdbdbdCdC.",
            "uMmfMDbubDMDmd.",
            ".MmnMDmMmDMDmmu",
            ".mmDMmmMmmMDmM.",
            ".dCdCdddddCdCd.",
            ".dMdDMdddmDMDm.",
            ".mMmDMQQQdDMDd.",
            "ZmMmDMQQQmDMDmg",
            "gmmmDMQQQgDMDmg",
            "ZmMgDmQQQmdZDmg",
            "Z.g...Z..g.gZgg",
            "ggZZZ..g.g..Z.g"
        };

        static readonly Dictionary<string, UnityEngine.Sprite[]> cache = new Dictionary<string, UnityEngine.Sprite[]>();

        public static UnityEngine.Sprite[] GetSprites(string id)
        {
            UnityEngine.Sprite[] cached;
            if (cache.TryGetValue(id, out cached)) return cached;

            UnityEngine.Sprite mini = null;
            BuildingAsset real = AssetManager.buildings.get("watch_tower_human");
            foreach (UnityEngine.Sprite sp in real.loadBuildingSpriteList())
                if (sp.name.StartsWith("mini")) mini = sp;

            string[] grid = id == DWARF_ID ? GRID_DWARF : (id == ORC_ID ? GRID_ORC : GRID_LIGHT);
            List<UnityEngine.Sprite> res = new List<UnityEngine.Sprite>();
            res.Add(WallBuilder.FromGrid(WallBuilder.GRID_CONSTRUCTION, "construction_0"));
            res.Add(WallBuilder.FromGrid(grid, "main_0"));
            if (mini != null)
            {
                UnityEngine.Sprite m = UnityEngine.Sprite.Create(mini.texture, mini.rect,
                    new UnityEngine.Vector2(mini.pivot.x / mini.rect.width, mini.pivot.y / mini.rect.height), mini.pixelsPerUnit);
                m.name = "mini_0";
                res.Add(m);
            }
            res.Add(WallBuilder.FromGrid(WallBuilder.GRID_RUIN, "ruin_0"));
            UnityEngine.Sprite[] arr = res.ToArray();
            cache[id] = arr;
            return arr;
        }
    }

    public class GateTile
    {
        public WorldTile tile;
        public TopTileType prev;
        public bool closed;
        public bool broken;
        public double blocked_since;   // с какого момента в проёме стоят юниты и мешают закрыть ворота
        public int ring;
        public int side;
    }

    // Одна осада: живёт, пока армия рядом с городом, даже если сам лагерь уже сломали
    public class SiegeInfo
    {
        public City target;
        public Kingdom kingdom;
        public int camps_made;
        public double low_since = -1;
        public double hold_since = -1;   // с какого момента армию удерживают у закрытого города
        public bool hold_done;
    }

    // Осадный лагерь: палатки, мощёная площадка, частокол и катапульта. Появляется у вражеского города, когда
    // армия достаточно большая, и убирается вместе с концом осады.
    public static class Siege
    {
        // ================= Настройки =================
        private static bool CAMP_ENABLED = Cfg.Bool("camp_enabled", true, "осадные лагеря");
        private static int CAMP_MIN_ARMY = Cfg.Int("camp_min_army", 10, "с какого числа бойцов рядом с городом появляется лагерь");          // лагерь появляется с такой армии
        private static int CAMP_TENT_STEP = Cfg.Int("camp_tent_step", 20, "каждые столько бойцов добавляют ещё одну палатку");         // каждые столько солдат - ещё одна палатка
        private static int CAMP_MAX_TENTS = Cfg.Int("camp_max_tents", 6, "максимум палаток (не больше 6)");
        private static int CATAPULT_MIN_ARMY = Cfg.Int("siege_gun_min_army", 46, "с какого числа бойцов в лагере появляется катапульта или требушет");      // катапульта, если в армии больше 45
        private static int CAMP_KEEP_ARMY = Cfg.Int("camp_keep_army", 8, "если рядом с лагерем бойцов меньше - осада считается снятой...");
        private static float CAMP_LOSE_SECONDS = Cfg.Float("camp_lose_seconds", 25f, "...если так продолжается столько секунд (игровое время)");
        private const int CAMP_TRIGGER_DIST = 80;          // бойцы армии не дальше этого от осаждаемого города
        private const int CAMP_ARMY_RADIUS = 60;           // и не дальше этого от лагеря, чтобы он держался
        private const int CAMP_MAX_BUILDS = 24;            // сколько построек можно поставить заново после разрушения
        private const float CAMP_RETRY_SECONDS = 20f;  // пауза после сворачивания или неудачного поиска места

        private const int SITE_MIN_DIST = 24;          // поиск места: от вражеского центра в сторону атакующих
        private const int SITE_MAX_DIST = 70;
        private const int SITE_STEP = 3;
        private const int PAVE_RADIUS = 7;
        private const int PALISADE_RADIUS = 11;
        private const int PALISADE_GAP = 3;            // проём в частоколе: |s| <= GAP
        private const int PAVE_PER_TICK = 40;
        private const int PALISADE_PER_TICK = 24;
        private const int DISMANTLE_PER_TICK = 12;
        private const int BUILD_COOLDOWN = 0;

        // Катапульта
        public const string TENT_ID = "siege_tent";
        public const string CATAPULT_ID = "siege_catapult";
        public const string TREBUCHET_ID = "siege_trebuchet";
        public const string MORTAR_ID = "siege_mortar";
        public const string FIRE_ID = "siege_campfire";
        public const string BARRICADE_ID = "gate_barricade";
        public const string STAKE_ID = "camp_stake";          // секция лагерного частокола вдоль x
        public const string STAKE_V_ID = "camp_stake_v";      // то же вдоль z
        public const string BARRICADE_L_ID = "gate_barricade_l";     // ворота на левой стене: колья смотрят влево
        public const string BARRICADE_R_ID = "gate_barricade_r";     // ворота на правой стене: колья смотрят вправо
        public const string FLAG_ID = "city_flag";
        public const string TUSK_ID = "city_tusks";
        public const string BANNER_ID = "city_banner";
        public const string BANNER_S_ID = "city_banner_small";     // уменьшенное знамя защитников у баррикад
        public const string TUSK2_ID = "city_tusks2";
        public const string TUSK3_ID = "city_tusks3";
        public const string TOWER_ID = "siege_tower";
        public const string PROP_BARREL = "camp_barrel";
        public const string PROP_CART = "camp_cart";
        public const string PROP_AMMO = "camp_ammo";
        private static readonly string[] PROP_IDS = new string[] { PROP_BARREL, PROP_CART, PROP_AMMO };
        public const string PROP_BOAT = "camp_boat";         // вытащенная на берег шлюпка (плацдарм)
        public const string PROP_ANCHOR = "camp_anchor";
        public const string PROP_NETS = "camp_nets";
        private static readonly string[] SEA_PROP_IDS = new string[] { PROP_BOAT, PROP_ANCHOR, PROP_NETS };
        private static int TOWER_MIN_ARMY = Cfg.Int("camp_tower_min_army", 50, "с какого числа бойцов в лагере появляется охранная башня (0 - никогда)");
        private static int CAMP_PROPS = Cfg.Int("camp_props", 3, "максимум декоративных построек в лагере (бочка, телега с сеном, снаряды), 0 - без них");
        private static readonly int[,] PROP_SLOTS = new int[,] { { -4, 3 }, { 4, 4 }, { -3, -6 } };
        private static int CAMP_MAX_PER_SIEGE = Cfg.Int("camp_max_per_siege", 2, "сколько раз за одну осаду можно ставить лагерь заново");      // сколько раз за одну осаду можно ставить лагерь заново
        private static float CAMP_EMPTY_SECONDS = Cfg.Float("camp_empty_seconds", 20f, "лагерь без единой целой постройки убирается через столько секунд");
        private static int CAMP_MIN_CITY_DIST = Cfg.Int("camp_min_city_dist", 60, "лагерь не ставится, если города на одном острове ближе стольких клеток (0 - без ограничения)");
        private static bool CAMP_NEED_WALLS = Cfg.Bool("camp_need_walls", true, "лагерь только у укреплённых городов (есть стена или 3+ башни)");
        private static int CAMP_LANDING_MODE = Cfg.Int("camp_landing_mode", 1, "лагерь при высадке с кораблей: 0 никогда, 1 только если армия сошла далеко и дошла пешком, 2 всегда");
        private static int CAMP_APPROACH_DIST = Cfg.Int("camp_approach_dist", 45, "на сколько клеток от города армия должна быть замечена, чтобы считаться дошедшей пешком");
        private static int GUN_MIN_RANGE = Cfg.Int("siege_gun_min_range", 14, "орудия не стреляют по целям ближе стольких клеток (чтобы не задеть своих)");
        private static bool HOLD_ENABLED = Cfg.Bool("siege_hold", true, "армия остаётся у запертого города, пока орудия бьют по воротам");
        private static float HOLD_SECONDS = Cfg.Float("siege_hold_seconds", 300f, "сколько секунд армию удерживают у запертого города, прежде чем она уйдёт");
        private static float GATE_DMG_CATAPULT = Cfg.Float("siege_gate_damage_catapult", 45f, "урон катапульты по воротам за выстрел");
        private static float GATE_DMG_TREBUCHET = Cfg.Float("siege_gate_damage_trebuchet", 90f, "урон требушета по воротам за выстрел");
        internal static float GunRange() { return GUN_MAX_RANGE; }
        private static int GUN_MAX_RANGE = Cfg.Int("siege_gun_max_range", 56, "дальность стрельбы орудий, клетки");
        internal static void SetGunRange(int r) { GUN_MAX_RANGE = System.Math.Max(GUN_MIN_RANGE + 6, System.Math.Min(160, r)); RefreshMortarSpeed(); }
        private static float GUN_RELOAD_SPREAD = Cfg.Float("siege_gun_reload_spread", 0.25f, "случайная добавка к перезарядке (0.25 = до +25 процентов)");
        private static bool SHOT_LOG = Cfg.Bool("siege_shot_log", false, "писать в лог выстрелы орудий и отсутствие целей");
        private const int CROWD_RADIUS = 8;            // лагерь не ставят там, где стоят свои бойцы
        private const int CROWD_MAX = 3;
        private const float RECALL_SECONDS = 4f;
        private const int REBUILDS = 1;                // сколько раз можно восстановить разрушенную палатку/орудие
        private static float TREBUCHET_DAMAGE = Cfg.Float("trebuchet_damage", 150f, "урон ядра требушета");   // ядро (взрыв радиусом 2)
        private static float FLAG_ANIM_SPEED = Cfg.Float("flag_anim_speed", 4f, "скорость колыхания знамён на площади (кадров в секунду)");
        private const float SHAKE_TIME = 0.5f;
        private const float SHAKE_POWER = 0.12f;
        private const float TENT_HEALTH = 400f;
        private const float CATAPULT_HEALTH = 800f;
        private static float CATAPULT_DAMAGE = Cfg.Float("catapult_damage", 250f, "урон камня катапульты");     // камень
        private static float CATAPULT_RELOAD = Cfg.Float("siege_gun_reload", 4f, "перезарядка орудий, секунды");
        private static float MORTAR_RELOAD = Cfg.Float("mortar_reload", 12f, "перезарядка мортиры, секунды (у обычных орудий 4)");
        private static float MORTAR_HEALTH = Cfg.Float("mortar_health", 1000f, "хп мортиры");
        private static float MORTAR_DAMAGE = Cfg.Float("mortar_damage", 600f, "урон ядра мортиры");
        internal static void SetMortarMin(int r) { MORTAR_MIN_RANGE = System.Math.Max(8, System.Math.Min(60, r)); }
        private static int MORTAR_MIN_RANGE = Cfg.Int("mortar_min_range", 20, "мортира не стреляет по целям ближе стольких клеток (взрыв и кратер большие, чтобы не задеть свой лагерь)");
        private static float MORTAR_RANGE_K = Cfg.Float("mortar_range_mult", 1.2f, "во сколько раз мортира стреляет дальше обычных орудий");
        private static float MORTAR_BLAST = Cfg.Float("mortar_blast", 800f, "урон взрыва мортиры по зданиям и воинам (радиус 2)");
        private static float GATE_DMG_MORTAR = Cfg.Float("siege_gate_damage_mortar", 420f, "урон мортиры по воротам и стене за выстрел");
        private static int MORTAR_CRATER = Cfg.Int("mortar_crater_radius", 3, "радиус кратера от мортиры, клетки");
        private static float MORTAR_SMOKE_SECONDS = Cfg.Float("mortar_crater_smoke_seconds", 45f, "сколько секунд дымится кратер мортиры");

        // Мирные жители осаждённого города возвращаются в центр
        private static bool RECALL_CIVILIANS = Cfg.Bool("recall_civilians", true, "мирные жители осаждённого города бегут к центру");
        private const int RECALL_EVERY = 8;
        private const float RECALL_INSIDE = 16f;       // дальше стольких клеток от центра города гражданских зовут домой
        private const int RECALL_MAX = 25;             // за один проход

        private static readonly int[,] TENT_SLOTS = new int[,] { { -7, -3 }, { 7, -3 }, { -7, 5 }, { 7, 5 }, { 0, -8 }, { 0, 8 } };

        private static readonly Dictionary<City, SiegeCamp> camps = new Dictionary<City, SiegeCamp>();
        // Лагерь, который сейчас осаждает этот город (null - город не в осаде)
        internal static SiegeCamp CampTargeting(City c)
        {
            if (c == null) return null;
            foreach (KeyValuePair<City, SiegeCamp> kv in camps) if (kv.Value != null && kv.Value.target == c) return kv.Value;
            return null;
        }
        private static readonly Dictionary<City, double> cooldown = new Dictionary<City, double>();
        private static readonly Dictionary<City, City> approached = new Dictionary<City, City>();
        private static double next_no_target_log;
        private static int WALL_SHARE = Cfg.Int("siege_wall_share", 30, "какая доля выстрелов (в процентах) идёт по стене, а не по воротам (0 - только ворота)");
        private static string last_shooter;
        private static int GUN2_ARMY = Cfg.Int("siege_gun2_min_army", 90, "с какого числа бойцов у лагеря появляется второе орудие (0 - выключить)");
        private static int GUN3_ARMY = Cfg.Int("siege_gun3_min_army", 140, "с какого числа бойцов у лагеря появляется третье орудие (0 - выключить)");
        private static readonly Dictionary<City, SiegeInfo> sieges = new Dictionary<City, SiegeInfo>();
        private static readonly List<SiegeCamp> dying = new List<SiegeCamp>();
        private static int last_dying_frame = -1;
        private static int last_sweep_frame = 0;

        internal static void ResetAll()
        {
            camps.Clear();
            cooldown.Clear();
            approached.Clear();
            sieges.Clear();
            dying.Clear();
            last_shooter = null;
            craters.Clear();
            why_text.Clear();
            why_time.Clear();
            fleet_seen.Clear();
            _next_fleet = 0;
            beaches.Clear();
            boat_info.Clear();
            _next_beach = 0;
            next_no_target_log = 0;
            last_sweep_frame = UnityEngine.Time.frameCount - 1500;   // скоро подметём оставшиеся от прошлой сессии палатки
        }

        // ---------- Спрайты ----------

        // Три варианта палатки: большая конусная, малая конусная с вымпелом, шатёр с оборкой
        static readonly string[][] GRIDS_TENT = new string[][] {
            new string[] {
            ".......PFFFF...",
            ".......PFFfn...",
            ".......Pffn....",
            ".......L.......",
            "......ncz......",
            ".....nFCnz.....",
            "....nfFcnnz....",
            "...nnffCnxzz...",
            "..nffDdddDnxz..",
            ".nffFFdddnnnxz.",
            "nxnnffdYdxxxzzz",
            "MffFFFdddfnnnxm",
            "mDddDDddDDddDDD",
            ".eSEsseseeseSs.",
            "SeeSeSEeSESessE"
            },
            new string[] {
            ".....PFFf..",
            ".....Pfn...",
            ".....P.....",
            ".....L.....",
            "....ncz....",
            "...nFCnz...",
            "..nfFcnnz..",
            ".nnfdddxzz.",
            "nffFdYdnnxz",
            "DDddDDddDDd",
            ".SEEeESEeS.",
            "EEeSSEEeSee"
            },
            new string[] {
            "......PFFf...",
            "......Pfn....",
            "......P......",
            "......L......",
            ".....ncz.....",
            "....nFcnz....",
            "...nFFcnnz...",
            "..nfFFcnnxz..",
            ".nffFFcnnxxz.",
            "cCcCcCcCcCcCc",
            "nFFFFffnnnxxz",
            ".FFffdddxnnx.",
            ".FFffdYdxnnx.",
            "DDddDDddDDddD",
            ".eSSSeeeESee."
            }
        };
        static readonly string[] GRID_CATAPULT = {
            ".DSsSD...............",
            ".DsL.............PFFF",
            "..DDDl.lL...LM...PFFf",
            "....cMLlM...Mm...Pff.",
            ".....cMLLDDDMm...Pn..",
            "......cMMLLdMm...P...",
            "......cLMMMkkkk..P...",
            "......DLM..SsSs..P...",
            ".DlLlDLllLllLllDllLD.",
            ".dmMmMmMmMmMmMmMmMmd.",
            "...wwDwW.....wwDwW...",
            "..WwDDDwW...WwDDDwW..",
            "..WDDlDDdddddDDlDDW..",
            "..WwDDDwW...WwDDDwW..",
            "...WwDwW.....WwDwW...",
            "...EWWWeeSSeEeWWWe...",
            ".eSEssSEeessEseESESS.",
            "SeeESeEeEeEesEeSsSesE"
        };
        // Мортира: тяжёлое орудие культуры с чертой «Мортира» (кадры: заряжена, выстрел, дым)
        static readonly string[] GRID_MORTAR_F0 = new string[] {
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............QQQQ............",
            "...........QaaaaQQ..........",
            "...........QaQttaGQ.........",
            "...........QaQQQQQGQ........",
            "...........QtaQQQQQGQ.......",
            "..........QttaQQQQQQSQ.FFFP.",
            ".........QtttGGQQQQQSQ.FffP.",
            "........QttStkSGGQQQSQ.fnnP.",
            ".......QttSaGSkttSSSSQ....P.",
            ".......QttaGSStkkkQQQ.....P.",
            "....dDDDDakkSttDDDDd......P.",
            "....DLMMmGSkkttmLLMD......P.",
            "....DMmmmGSSkkkmMMMD......P.",
            "....dkMmDSSttkkDmMkd......P.",
            "..dDDDDDDkQtkkQDDDDDDDDDDdP.",
            ".dDLlLLlLLlQQQLLlLLlLLlLLDP.",
            ".dkMmMMmMmkMMmMMmkMmMMmMkDd.",
            ".DmMmMMmMmMMmMmMMmMmMMmMmMD.",
            ".dkDDddDDkddDDddDDkddDDddkd.",
            "..dddddddddddddddddddddddd.."
        };
        static readonly string[] GRID_MORTAR_F1 = new string[] {
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            ".................y..........",
            ".............QQ.oyo.........",
            "............QaaoyyyO........",
            "...........QaataoOO.........",
            "...........QaQtQQGQ.........",
            "...........QaQQQQQGQ........",
            "..........QttaQQQQQSQ..FFFP.",
            ".........QtttGGQQQQQSQ.FffP.",
            ".........QtttSGGQQQQSQ.fnnP.",
            ".......QQtSaGkktGSSSSQ....P.",
            ".......QttaGSSkkkkSSQ.....P.",
            "....dDDDDtkSSttDDDDd......P.",
            "....DLMMmGkktttmLLMD......P.",
            "....DMmmmGSkktkmMMMD......P.",
            "....dkMmDSStkkkDmMkd......P.",
            "..dDDDDDkkttkkQDDDDDDDDDDdP.",
            ".dDLlLLlLLQkkQLLlLLlLLlLLDP.",
            ".dkMmMMmMmkQQmMMmkMmMMmMkDd.",
            ".DmMmMMmMmMMmMmMMmMmMMmMmMD.",
            ".dkDDddDDkddDDddDDkddDDddkd.",
            "..dddddddddddddddddddddddd.."
        };
        static readonly string[] GRID_MORTAR_F2 = new string[] {
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "................y...........",
            "...............oyo..........",
            "..............oyyyo.........",
            ".............oyyyyyO........",
            "............QoyyyyyO........",
            "...........QaaoyyyO.........",
            "..........QaataoOO..........",
            "..........QaQQQQGGQ.........",
            "..........QaQQQQQGGQ...FFFP.",
            ".........QttaQQQQQSQ...FffP.",
            ".........QttGGQQQQQSQ..fnnP.",
            "........QtStktGQQQQSQ.....P.",
            "......QQtSaGkktGSSSSQ.....P.",
            "....dDDDDaGSSkkDDDDd......P.",
            "....DLMMmkSSttkmLLMD......P.",
            "....DMmmmkktttkmMMMD......P.",
            "....dkMmDSSkkkkDmMkd......P.",
            "..dDDDDkSSttkkQDDDDDDDDDDdP.",
            ".dDLlLLlkttkkQLLlLLlLLlLLDP.",
            ".dkMmMMmMQkkQmMMmkMmMMmMkDd.",
            ".DmMmMMmMmQQmMmMMmMmMMmMmMD.",
            ".dkDDddDDkddDDddDDkddDDddkd.",
            "..dddddddddddddddddddddddd.."
        };
        static readonly string[] GRID_MORTAR_F3 = new string[] {
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            ".................aA.........",
            "................aAAa........",
            ".................Ga.........",
            "............QQQ.............",
            "...........QaaaQQ...........",
            "..........QaattaGQ..........",
            "..........QaQQQQQGQ.........",
            "..........QtaQQQQQGQ...FFFP.",
            ".........QttaQQQQQQSQ..FffP.",
            ".........QttGGQQQQQSQ..fnnP.",
            "........QtStkSGGQQQSQ.....P.",
            ".......QtSaGSkttSSSSQ.....P.",
            "....dDDDDaGSStkDDDDd......P.",
            "....DLMMmkkStttmLLMD......P.",
            "....DMmmmGkkttkmMMMD......P.",
            "....dkMmDSSkkkkDmMkd......P.",
            "..dDDDDkSSttkkQDDDDDDDDDDdP.",
            ".dDLlLLlkQtkkQQLlLLlLLlLLDP.",
            ".dkMmMMmMmQkQmMMmkMmMMmMkDd.",
            ".DmMmMMmMmMQmMmMMmMmMMmMmMD.",
            ".dkDDddDDkddDDddDDkddDDddkd.",
            "..dddddddddddddddddddddddd.."
        };
        static readonly string[] GRID_MORTAR_F4 = new string[] {
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "...................a.A.a....",
            "..................aAaAaAa...",
            "...................GaAaG....",
            "............................",
            "............................",
            "............................",
            "............QQQQ............",
            "...........QaaaaQQ..........",
            "...........QaQtQGGQ.........",
            "...........QaQQQQQGQ........",
            "..........QtaQQQQQQSQ..FFFP.",
            ".........QtttGQQQQQSQ..FffP.",
            ".........QtttSGQQQQSSQ.fnnP.",
            ".......QQtSakktGGQQSSQ....P.",
            ".......QtSaGSkktkSSSQ.....P.",
            "....dDDDDtGSStkDDDDd......P.",
            "....DLMMmGkStttmLLMD......P.",
            "....DMmmmGSkttkmMMMD......P.",
            "....dkMmDSStkkkDmMkd......P.",
            "..dDDDDDkSttkkQDDDDDDDDDDdP.",
            ".dDLlLLlLQtkkQLLlLLlLLlLLDP.",
            ".dkMmMMmMmQQQmMMmkMmMMmMkDd.",
            ".DmMmMMmMmMMmMmMMmMmMMmMmMD.",
            ".dkDDddDDkddDDddDDkddDDddkd.",
            "..dddddddddddddddddddddddd.."
        };
        static readonly string[] GRID_MORTAR_F5 = new string[] {
            "............................",
            "............................",
            "............................",
            "...................G.a..G...",
            "....................a..A....",
            "...................G........",
            "............................",
            "............................",
            "............................",
            "............................",
            ".............QQ.............",
            "............QaaQQ...........",
            "...........QaataaQ..........",
            "...........QaQtQQGQ.........",
            "...........QaQQQQQGQ........",
            "..........QttaQQQQQSQ..FFFP.",
            ".........QtttGGQQQQQSQ.FffP.",
            ".........QtttSGGQQQQSQ.fnnP.",
            ".......QQtSaGkktGSSSSQ....P.",
            ".......QttaGSSkkkkSSQ.....P.",
            "....dDDDDtkSSttDDDDd......P.",
            "....DLMMmGkktttmLLMD......P.",
            "....DMmmmGSkktkmMMMD......P.",
            "....dkMmDSStkkkDmMkd......P.",
            "..dDDDDDkkttkkQDDDDDDDDDDdP.",
            ".dDLlLLlLLQkkQLLlLLlLLlLLDP.",
            ".dkMmMMmMmkQQmMMmkMmMMmMkDd.",
            ".DmMmMMmMmMMmMmMMmMmMMmMmMD.",
            ".dkDDddDDkddDDddDDkddDDddkd.",
            "..dddddddddddddddddddddddd.."
        };
        static readonly string[] GRID_MORTAR_F6 = new string[] {
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            ".............QQQ............",
            "............QaaaQ...........",
            "...........QaQttaQQ.........",
            "...........QaQQQQGGQ........",
            "...........QaQQQQQQGQ.......",
            "..........QttaQQQQQSQ..FFFP.",
            ".........QtttGGQQQQQSQ.FffP.",
            ".........QtStktGQQQQSQ.fnnP.",
            ".......QQtSaGkktGSSSSQ....P.",
            ".......QttaGSSkkkkQQQ.....P.",
            "....dDDDDtkSSttDDDDd......P.",
            "....DLMMmGkktttmLLMD......P.",
            "....DMmmmGSSkkkmMMMD......P.",
            "....dkMmDSSttkkDmMkd......P.",
            "..dDDDDDDkttkkQDDDDDDDDDDdP.",
            ".dDLlLLlLLQkkQLLlLLlLLlLLDP.",
            ".dkMmMMmMmkQQmMMmkMmMMmMkDd.",
            ".DmMmMMmMmMMmMmMMmMmMMmMmMD.",
            ".dkDDddDDkddDDddDDkddDDddkd.",
            "..dddddddddddddddddddddddd.."
        };
        static readonly string[] GRID_MORTAR_F7 = new string[] {
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............................",
            "............QQQQ............",
            "...........QaaaaQQ..........",
            "...........QaQttaGQ.........",
            "...........QaQQQQQGQ........",
            "...........QtaQQQQQGQ.......",
            "..........QttaQQQQQQSQ.FFFP.",
            ".........QtttGGQQQQQSQ.FffP.",
            "........QttStkSGGQQQSQ.fnnP.",
            ".......QttSaGSkttSSSSQ....P.",
            ".......QttaGSStkkkQQQ.....P.",
            "....dDDDDakkSttDDDDd......P.",
            "....DLMMmGSkkttmLLMD......P.",
            "....DMmmmGSSkkkmMMMD......P.",
            "....dkMmDSSttkkDmMkd......P.",
            "..dDDDDDDkQtkkQDDDDDDDDDDdP.",
            ".dDLlLLlLLlQQQLLlLLlLLlLLDP.",
            ".dkMmMMmMmkMMmMMmkMmMMmMkDd.",
            ".DmMmMMmMmMMmMmMMmMmMMmMmMD.",
            ".dkDDddDDkddDDddDDkddDDddkd.",
            "..dddddddddddddddddddddddd.."
        };
        static readonly string[] GRID_MORTAR = GRID_MORTAR_F0;
        // Требушет: второй вариант осадного орудия (выше, без колёс)
        static readonly string[] GRID_TREBUCHET = {
            ".L..........PFFFF..",
            "cML.........PFFf...",
            "CcMLl.......Pff....",
            "cC.MLLl.....Pn.....",
            "SsS.MML.....P......",
            "SSk...MLlkDDP......",
            ".......MLkmM.......",
            ".......LML.mM......",
            ".......LMMLmM......",
            ".......LM.MLL......",
            "......LMDDDMML.....",
            "......LM....mML....",
            "......LM....mMML...",
            ".....LM......mMM...",
            ".....LMDdddDDmMc...",
            "....LM.d......mC...",
            "....LM.......sSsSk.",
            "....LMd......SDDDk.",
            "...LM........sSsSk.",
            "DllLMdLllLllLkkkkkD",
            "dMmMmMmMmMmMmMmMmMd",
            "..eeeesESsSseESEe..",
            "SEeSseeEeeeSeEEeeeS",
            "SeeeeEEeEsseeseeESE"
        };
        // Костёр: три кадра пламени (спрайты main_0 с одним номером - игра сама крутит их как анимацию)
        static readonly string[][] GRIDS_FIRE = new string[][] {
            new string[] { "....o......", "...oOo.o...", "..oOyOoO...", "..OoyyoOo..", "..OOyyyOOq.", "..qOoyoOq..", ".sSqOOOqSs.", "SsDLqOqLDsS", "StmLDDDLmtS", ".SmMLLLMmS.", "..SsSSSsS.." },
            new string[] { "......o....", ".o...oOo...", ".oOo.OyOo..", "..OoOyyoO..", "..OOyyyOOq.", "..qOoyoOq..", ".sSqOOOqSs.", "SsDLqOqLDsS", "StmLDDDLmtS", ".SmMLLLMmS.", "..SsSSSsS.." },
            new string[] { ".....o.....", "....oOo.o..", "...oOyOoO..", "..oOyyyoO..", "..OOyyyOOq.", "..qOoyoOq..", ".sSqOOOqSs.", "SsDLqOqLDsS", "StmLDDDLmtS", ".SmMLLLMmS.", "..SsSSSsS.." },
            new string[] { "...o.......", "..oOo..o...", "..OyOoOo...", "..OyyyoOo..", "..OOyyyOOq.", "..qOoyoOq..", ".sSqOOOqSs.", "SsDLqOqLDsS", "StmLDDDLmtS", ".SmMLLLMmS.", "..SsSSSsS.." },
            new string[] { "........o..", "...o..oOo..", "..oOoOoyO..", "..OoyyyOo..", "..OOyyyOOq.", "..qOoyoOq..", ".sSqOOOqSs.", "SsDLqOqLDsS", "StmLDDDLmtS", ".SmMLLLMmS.", "..SsSSSsS.." },
            new string[] { "....o.o....", "...oOoOo...", "..oOyOyOo..", "..OoyyyoO..", "..OOyyyOOq.", "..qOoyoOq..", ".sSqOOOqSs.", "SsDLqOqLDsS", "StmLDDDLmtS", ".SmMLLLMmS.", "..SsSSSsS.." }
        };
        static readonly string[] GRID_FIRE_BUILD = new string[] { "...........", "...DmD.....", ".SmDLDmDS..", "SsDmLLDmDsS", ".SsSSSSSsS." };
        static readonly string[] GRID_FIRE_RUIN = new string[] { "...........", "..Q.Q......", ".sQQtQQs...", "SsQQQQQQsS.", ".SsSQSQSsS." };

        // Знамя на флагштоке: три кадра полотнища (цвет королевства), и костяные бивни для орков (одна картинка)
        static readonly string[][] GRIDS_FLAG = new string[][] {
            new string[] { "..LL..........", "..LL..........", "..PD..........", "..PDFFFFFFFFf.", "..PDFFFFFFFff.", "..PDFFFFFFffn.", "..PDffffffnnn.", "..PDfffffnnn..", "..PDffnnnnn...", "..PDnnnnn.....", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", ".sPDs.........", "SsSSsS........", ".SsSSs........" },
            new string[] { "..LL..........", "..LL..........", "..PD..........", "..PDFFFFFf....", "..PDFFFFFFFff.", "..PDFFFFFffnn.", "..PDffffnnnn..", "..PDfffnnnn...", "..PDnnnnn.....", "..PDnn........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", ".sPDs.........", "SsSSsS........", ".SsSSs........" },
            new string[] { "..LL..........", "..LL..........", "..PD..........", "..PDFFFFf.....", "..PDFFFFFFfn..", "..PDffffffnnn.", "..PDffffnnnnn.", "..PDnnnnnnnn..", "..PDnnnnnnf...", "..PDnnn.......", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", "..PD..........", ".sPDs.........", "SsSSsS........", ".SsSSs........" }
        };
        static readonly string[] GRID_FLAG_BUILD = new string[] { "..PD..........", "..PD..........", "..PD..........", ".sPDs.........", "SsSSsS........" };
        static readonly string[] GRID_FLAG_RUIN = new string[] { ".....t......", ".sQtS.Q.....", "SsSQSsQS...." };
        static readonly string[] GRID_TUSK = new string[] { "...............", ".......b.......", "......bbb......", "...b..LL...b...", "...b...P...b...", "...b...P...b...", "..b...DP....b..", "..Bb..DP...bB..", ".Bb...DPFFf.bB.", ".Bb...DPffn.bB.", ".Bb...DPnn..bB.", ".Bb...DP....bB.", "..Bb..DP...bB..", "..uBb.DP..bBu..", "...uBbDP.bBu...", "....uBDPbBu....", ".....uDPBu.....", "......DPu......", "..SsSsPDsSsSs..", ".SsSSsSSSsSSsS.", "SsSSsSSsSSsSSsS", ".eSEsSEeSsEeSe." };
        static readonly string[] GRID_TUSK_BUILD = new string[] { "......PD.......", "....sSPDSs.....", "..SsSSsSSsSs...", ".SsSSsSSsSSsS.." };
        static readonly string[][] GRIDS_BANNER = new string[][] {
            new string[] { "......LL......", "......LL......", "......PD......", "lLDDDDDDDDDDLl", "...zzzzzzzz...", "...FFFFFFfn...", "...FFFccFfn...", "...FFcCCcfn...", "...FFFccFfn...", "...FFFFFfnn...", "...FFFFFfnn...", "...FFFFffnn...", "...FFFffnnn...", "...FFffnnnx...", "...Ffffnnxx...", "...fff..nnx...", "...ff....nx...", "...f......x...", "......PD......", "......PD......", "......PD......", "......PD......", "......PD......", "....sSPDsS....", "...SsSsSsSs...", ".............." },
            new string[] { "......LL......", "......LL......", "......PD......", "lLDDDDDDDDDDLl", "...zzzzzzzz...", "...FFFFFFfn...", "...FFFccFfn...", "...FFcCCcfn...", "...FFFccFfn...", "....FFFFFfnn..", "....FFFFFfnn..", "....FFFFffnn..", "....FFFffnnn..", "....FFffnnnx..", "....Ffffnnxx..", "....fff..nnx..", "....ff....nx..", "....f......x..", "......PD......", "......PD......", "......PD......", "......PD......", "......PD......", "....sSPDsS....", "...SsSsSsSs...", ".............." },
            new string[] { "......LL......", "......LL......", "......PD......", "lLDDDDDDDDDDLl", "...zzzzzzzz...", "...FFFFFFfn...", "...FFFccFfn...", "...FFcCCcfn...", "...FFFccFfn...", "...FFFFFfnn...", "...FFFFFfnn...", "...FFFFffnn...", "...FFFffnnn...", "...FFffnnnx...", "...Ffffnnxx...", "...fff..nnx...", "...ff....nx...", "...f......x...", "......PD......", "......PD......", "......PD......", "......PD......", "......PD......", "....sSPDsS....", "...SsSsSsSs...", ".............." },
            new string[] { "......LL......", "......LL......", "......PD......", "lLDDDDDDDDDDLl", "...zzzzzzzz...", "...FFFFFFfn...", "...FFFccFfn...", "...FFcCCcfn...", "...FFFccFfn...", "..FFFFFfnn....", "..FFFFFfnn....", "..FFFFffnn....", "..FFFffnnn....", "..FFffnnnx....", "..Ffffnnxx....", "..fff..nnx....", "..ff....nx....", "..f......x....", "......PD......", "......PD......", "......PD......", "......PD......", "......PD......", "....sSPDsS....", "...SsSsSsSs...", ".............." }
        };
        // Уменьшенное знамя: убираем по четыре строки из полотна и из древка
        static string[] ShrinkBanner(string[] g)
        {
            List<string> o = new List<string>();
            for (int r = 0; r < g.Length; r++)
            {
                if ((r >= 9 && r <= 12) || (r >= 19 && r <= 22)) continue;
                o.Add(g[r]);
            }
            return o.ToArray();
        }
        static readonly string[] GRID_BANNER_BUILD = new string[] { "......PD......", ".LDDDDDDDDDDL.", "......PD......", "....sSPDSs....", "...SsSSsSSs..." };
        static readonly string[] GRID_BANNER_RUIN = new string[] { "......t.......", "....cC.QcC....", "..sQtSsQSsS..." };
        static readonly string[] GRID_TUSK2 = new string[] { ".................", ".................", ".................", ".......bbbbbbb...", ".....bbbbBBBuu...", "....bbbBBuu......", "...bbbBuu........", "..bbbBuu.........", ".bbbBuu..........", ".bbuuu...........", "bbbBuu...........", "bbuuu............", "bbBuu............", "bbuuuu...........", "bbBBuu...........", "bbBuuu...........", "bbbBuuu..........", "bbbBBuuuu........", ".bbbuBuuuu.......", ".bbbFFfnuuu......", "..bbffnxzuu......", "...bbbbBzBB......", "...SsSSsSSsSsS...", "..SsSSsSSSsSSsS..", ".SsSSsSSsSSsSSsS.", "eSEsSEeSsEeSeSEse" };
        static readonly string[] GRID_TUSK2_BUILD = new string[] { "........PD.......", "......sSPDSs.....", "....SsSSsSSsSs...", "..SsSSsSSSsSSsS.." };
        static readonly string[] GRID_TUSK2_RUIN = new string[] { "........b.u......", "......bBQuB......", "...SsQbQSQsSs...." };
        static readonly string[] GRID_TUSK3 = new string[] { "..b...........b..", ".bBb.........bBb.", ".bB...........Bb.", ".bBb..bbbbb..bBb.", "..bBb.bbBbb.bBb..", "...bBbbbBbbbBb...", "...bbbQbbbQbbb...", "....bbQQbQQbb....", "....bbbbQbbbb....", ".....bbBbBbb.....", "......uBuBu......", ".....FFFFFFf.....", "....fnnPDnnnn....", ".....nnPDnnx.....", "......zPD........", "......PDD........", "......PDD........", "......PDD........", "......PDD........", "....sSPDDSs......", "..SsSSsSSsSSs....", ".SsSSsSSSsSSsS...", "SsSSsSSsSSsSSsS..", ".eSEsSEeSsEeSe..." };
        static readonly string[] GRID_TUSK3_BUILD = new string[] { "........PD.......", "......sSPDSs.....", "....SsSSsSSsS....", "...SsSSsSSsSSsS.." };
        static readonly string[] GRID_TUSK3_RUIN = new string[] { "........b.u......", ".......bBQuB.....", "...SsQbQSQuSs....", "..SsSQSSQsSsS...." };
        // Декор лагеря: бочка, телега с сеном, куча снарядов
        static readonly string[] GRID_BARRICADE = new string[] { "l..ll..ll..l", ".Mm..Mm..Mm.", ".mM..mM..mM.", "M..MM..MM..M", "DmDmDmDmDmDm", "CcCcCcCcCcCc", ".dddddddddd." };
        static readonly string[] GRID_BARRICADE_VL = new string[] { "lMMm.DCc", ".lMm.DcC", "lMMm.DCc", ".lMm.DcC", "lMMm.DCc", ".lMm.DcC", "lMMm.DCc", ".lMm.DcC", "lMMm.DCc", ".lMm.DcC", "lMMm.DCc", ".lMm.DcC" };
        static readonly string[] GRID_BARRICADE_VR = new string[] { "cCD.mMMl", "CcD.mMl.", "cCD.mMMl", "CcD.mMl.", "cCD.mMMl", "CcD.mMl.", "cCD.mMMl", "CcD.mMl.", "cCD.mMMl", "CcD.mMl.", "cCD.mMMl", "CcD.mMl." };
        static readonly string[] GRID_BARRICADE_V_BUILD = new string[] { "........", ".....DD.", ".....Dd.", "....DDd.", ".....Dd.", ".....DD.", ".....Dd.", "....DDd." };
        static readonly string[] GRID_BARRICADE_V_RUIN = new string[] { "........", "....Q.Q.", "..Q.sQtQ", ".sQtQQsQ" };
        static readonly string[] GRID_STAKE = new string[] { "l.l.l.l.l.l.", "MmMmMmMmMmMm", "mMmMmMmMmMmM", "DCDCDCDCDCDC", "dDdDdDdDdDdD" };
        static readonly string[] GRID_STAKE_V = new string[] { "lMMm", "MmmD", "lMMm", "MmmD", "lMMm", "MmmD", "lMMm", "MmmD", "lMMm", "MmmD" };
        static readonly string[] GRID_STAKE_BUILD = new string[] { "............", "DMMMMMMMMMMD", ".dD......Dd." };
        static readonly string[] GRID_BARRICADE_BUILD = new string[] { "............", "DMMMMMMMMMMD", ".dD......Dd." };
        static readonly string[] GRID_BARRICADE_RUIN = new string[] { "............", "..Q.Q..Q.Q..", ".sQtQQsQtQs." };
        static readonly string[] GRID_BARREL = new string[] { "..llLL..", ".LLLMMm.", ".kkkkkk.", "DLLMMmmD", "DLMMMmmD", "DLMMMmmD", ".kkkkkk.", ".DLMMmD.", "..dddd.." };
        static readonly string[] GRID_BOAT = new string[] { "...mmmmm...", "..mMMMMMm..", ".mMLLkLLMm.", "mMMMMkMMMMm", "DDDDDDDDDDD", ".d.......d." };
        static readonly string[] GRID_BOAT_BUILD = new string[] { "...........", "..mmmmmm...", "DDDDDDDDDDD" };
        static readonly string[] GRID_ANCHOR = new string[] { "..tSS..", "..S.t..", "..tSS..", "SSSSSSt", "...St..", "...St..", "S..St.S", ".SSStS.", "..SSS.." };
        static readonly string[] GRID_ANCHOR_BUILD = new string[] { ".......", "...S...", "..SSS.." };
        static readonly string[] GRID_NETS = new string[] { "P.......P", "PcCcCcCcP", "PCcCcCcCP", "PcCcCcCcP", "PCcCcCcCP", "P.c.C.c.P", "P.......P", "P.V...h.P", "eE.....Ee" };
        static readonly string[] GRID_NETS_BUILD = new string[] { "P.......P", "P.......P", "eE.....Ee" };
        static readonly string[] GRID_BARREL_BUILD = new string[] { "........", "..LLLL..", ".kkkkkk.", "DMMMMMMD" };
        static readonly string[] GRID_BARREL_RUIN = new string[] { "........", "..Q.Q...", ".sQtQs.." };
        static readonly string[] GRID_CART = new string[] { "....h.hh......", "..hhHhhHhh....", ".hhHhhhHhhHh..", ".hHhjhHhhjHhh.", "DDDDDDDDDDDDDD", "DMMLMMMMLMMMmD", "DmMMmMMmMMMmMD", ".dDDDDDDDDDDd.", "..wWDw..wDWw..", "..WDDW..WDDW.." };
        static readonly string[] GRID_CART_BUILD = new string[] { "..............", "..............", "DDDDDDDDDDDDDD", ".dDDDDDDDDDDd." };
        static readonly string[] GRID_CART_RUIN = new string[] { "..............", "...Q.Q..Q.....", ".sQQtQQsQ.sQ.." };
        static readonly string[] GRID_AMMO = new string[] { "......kk......", ".....kssk.....", ".....kSSk.....", "....kkkkkk....", "...ksskkssk...", "...kSSkkSSk...", "..kkkkkkkkkk..", ".ksskksskkssk.", ".kSSkkSSkkSSk.", "..kk..kk..kk.." };
        static readonly string[] GRID_AMMO_BUILD = new string[] { "..............", "..............", "..kk..kk..kk..", ".kssk.kssk.kss" };
        static readonly string[] GRID_AMMO_RUIN = new string[] { "..............", "....Q..Q......", "..sQtQQsQtS..." };
        static readonly string[] GRID_TUSK_RUIN = new string[] { "...............", ".....b.u.b.....", "..bBuQQQuBb....", ".SsQSQQSQsS...." };

        static readonly Dictionary<string, UnityEngine.Sprite[]> sprite_cache = new Dictionary<string, UnityEngine.Sprite[]>();

        public static bool IsShooterId(string id) { return id == CATAPULT_ID || id == TREBUCHET_ID || id == MORTAR_ID; }
        public static bool IsPropId(string id) { return id == PROP_BARREL || id == PROP_CART || id == PROP_AMMO || id == PROP_BOAT || id == PROP_ANCHOR || id == PROP_NETS; }
        public static bool IsSiegeId(string id) { return id == STAKE_ID || id == STAKE_V_ID || id == TENT_ID || id == FIRE_ID || id == TOWER_ID || id == BARRICADE_ID || id == BARRICADE_L_ID || id == BARRICADE_R_ID || IsPropId(id) || IsShooterId(id) || IsDecorId(id) || Fx.IsFxId(id); }
        public static bool IsDecorId(string id) { return id == FLAG_ID || id == TUSK_ID || id == BANNER_ID || id == BANNER_S_ID || id == TUSK2_ID || id == TUSK3_ID; }

        public static UnityEngine.Sprite[] GetSprites(string id)
        {
            UnityEngine.Sprite[] cached;
            if (sprite_cache.TryGetValue(id, out cached)) return cached;

            UnityEngine.Sprite mini = null;
            BuildingAsset real = AssetManager.buildings.get("watch_tower_human");
            foreach (UnityEngine.Sprite sp in real.loadBuildingSpriteList())
                if (sp.name.StartsWith("mini")) mini = sp;

            if (Fx.IsFxId(id))
            {
                UnityEngine.Sprite[] fxs = Fx.GetSprites(id, mini);
                sprite_cache[id] = fxs;
                return fxs;
            }
            List<UnityEngine.Sprite> res = new List<UnityEngine.Sprite>();
            if (id == FLAG_ID)
            {
                res.Add(WallBuilder.FromGrid(GRID_FLAG_BUILD, "construction_0"));
                for (int f = 0; f < GRIDS_FLAG.Length; f++) res.Add(WallBuilder.FromGrid(GRIDS_FLAG[f], "main_0"));
                res.Add(WallBuilder.FromGrid(GRID_FLAG_RUIN, "ruin_0"));
                if (mini != null) res.Add(MiniOf(mini, 0));
            }
            else if (id == BANNER_ID || id == BANNER_S_ID)
            {
                bool small = id == BANNER_S_ID;
                res.Add(WallBuilder.FromGrid(GRID_BANNER_BUILD, "construction_0"));
                for (int f = 0; f < GRIDS_BANNER.Length; f++) res.Add(WallBuilder.FromGrid(small ? ShrinkBanner(GRIDS_BANNER[f]) : GRIDS_BANNER[f], "main_0"));
                res.Add(WallBuilder.FromGrid(GRID_BANNER_RUIN, "ruin_0"));
                if (mini != null) res.Add(MiniOf(mini, 0));
            }
            else if (id == TUSK2_ID)
            {
                res.Add(WallBuilder.FromGrid(GRID_TUSK2_BUILD, "construction_0"));
                res.Add(WallBuilder.FromGrid(GRID_TUSK2, "main_0"));
                res.Add(WallBuilder.FromGrid(GRID_TUSK2_RUIN, "ruin_0"));
                if (mini != null) res.Add(MiniOf(mini, 0));
            }
            else if (id == TUSK3_ID)
            {
                res.Add(WallBuilder.FromGrid(GRID_TUSK3_BUILD, "construction_0"));
                res.Add(WallBuilder.FromGrid(GRID_TUSK3, "main_0"));
                res.Add(WallBuilder.FromGrid(GRID_TUSK3_RUIN, "ruin_0"));
                if (mini != null) res.Add(MiniOf(mini, 0));
            }
            else if (id == TUSK_ID)
            {
                res.Add(WallBuilder.FromGrid(GRID_TUSK_BUILD, "construction_0"));
                res.Add(WallBuilder.FromGrid(GRID_TUSK, "main_0"));
                res.Add(WallBuilder.FromGrid(GRID_TUSK_RUIN, "ruin_0"));
                if (mini != null) res.Add(MiniOf(mini, 0));
            }
            else if (id == TOWER_ID)
            {
                // охранная башня лагеря: та же деревянная вышка, что и у городов, но без ночного огонька (Y днём стал бы чёрным)
                string[] main = (string[])WallBuilder.GRIDS_MAIN[0].Clone();
                for (int r = 0; r < main.Length; r++) main[r] = main[r].Replace('Y', 'y');
                res.Add(WallBuilder.FromGrid(WallBuilder.GRID_CONSTRUCTION, "construction_0"));
                res.Add(WallBuilder.FromGrid(main, "main_0"));
                if (mini != null) res.Add(MiniOf(mini, 0));
                res.Add(WallBuilder.FromGrid(WallBuilder.GRID_RUIN, "ruin_0"));
            }
            else if (id == BARRICADE_L_ID || id == BARRICADE_R_ID)
            {
                res.Add(WallBuilder.FromGrid(GRID_BARRICADE_V_BUILD, "construction_0"));
                res.Add(WallBuilder.FromGrid(id == BARRICADE_L_ID ? GRID_BARRICADE_VL : GRID_BARRICADE_VR, "main_0"));
                res.Add(WallBuilder.FromGrid(GRID_BARRICADE_V_RUIN, "ruin_0"));
                if (mini != null) res.Add(MiniOf(mini, 0));
            }
            else if (id == STAKE_ID || id == STAKE_V_ID)
            {
                res.Add(WallBuilder.FromGrid(GRID_STAKE_BUILD, "construction_0"));
                res.Add(WallBuilder.FromGrid(id == STAKE_ID ? GRID_STAKE : GRID_STAKE_V, "main_0"));
                res.Add(WallBuilder.FromGrid(GRID_BARRICADE_RUIN, "ruin_0"));
                if (mini != null) res.Add(MiniOf(mini, 0));
            }
            else if (id == BARRICADE_ID)
            {
                res.Add(WallBuilder.FromGrid(GRID_BARRICADE_BUILD, "construction_0"));
                res.Add(WallBuilder.FromGrid(GRID_BARRICADE, "main_0"));
                res.Add(WallBuilder.FromGrid(GRID_BARRICADE_RUIN, "ruin_0"));
                if (mini != null) res.Add(MiniOf(mini, 0));
            }
            else if (IsPropId(id))
            {
                string[] gm = id == PROP_BARREL ? GRID_BARREL : id == PROP_CART ? GRID_CART : id == PROP_BOAT ? GRID_BOAT : id == PROP_ANCHOR ? GRID_ANCHOR : id == PROP_NETS ? GRID_NETS : GRID_AMMO;
                string[] gb = id == PROP_BARREL ? GRID_BARREL_BUILD : id == PROP_CART ? GRID_CART_BUILD : id == PROP_BOAT ? GRID_BOAT_BUILD : id == PROP_ANCHOR ? GRID_ANCHOR_BUILD : id == PROP_NETS ? GRID_NETS_BUILD : GRID_AMMO_BUILD;
                string[] gr = id == PROP_BARREL ? GRID_BARREL_RUIN : id == PROP_CART ? GRID_CART_RUIN : id == PROP_AMMO ? GRID_AMMO_RUIN : GRID_BARREL_RUIN;
                res.Add(WallBuilder.FromGrid(gb, "construction_0"));
                res.Add(WallBuilder.FromGrid(gm, "main_0"));
                res.Add(WallBuilder.FromGrid(gr, "ruin_0"));
                if (mini != null) res.Add(MiniOf(mini, 0));
            }
            else if (id == FIRE_ID)
            {
                res.Add(WallBuilder.FromGrid(GRID_FIRE_BUILD, "construction_0"));
                for (int f = 0; f < GRIDS_FIRE.Length; f++) res.Add(WallBuilder.FromGrid(GRIDS_FIRE[f], "main_0"));
                res.Add(WallBuilder.FromGrid(GRID_FIRE_RUIN, "ruin_0"));
                if (mini != null) res.Add(MiniOf(mini, 0));
            }
            else
            {
                string[][] variants = id == TENT_ID ? GRIDS_TENT
                    : new string[][] { id == TREBUCHET_ID ? GRID_TREBUCHET : (id == MORTAR_ID ? GRID_MORTAR : GRID_CATAPULT) };
                for (int v = 0; v < variants.Length; v++)
                {
                    res.Add(WallBuilder.FromGrid(variants[v], "construction_" + v));
                    res.Add(WallBuilder.FromGrid(variants[v], "main_" + v));
                    if (mini != null) res.Add(MiniOf(mini, v));
                    res.Add(WallBuilder.FromGrid(variants[v], "ruin_" + v));
                }
            }
            UnityEngine.Sprite[] arr = res.ToArray();
            sprite_cache[id] = arr;
            return arr;
        }

        private static UnityEngine.Sprite MiniOf(UnityEngine.Sprite mini, int v)
        {
            UnityEngine.Sprite m = UnityEngine.Sprite.Create(mini.texture, mini.rect,
                new UnityEngine.Vector2(mini.pivot.x / mini.rect.width, mini.pivot.y / mini.rect.height), mini.pixelsPerUnit);
            m.name = "mini_" + v;
            return m;
        }

        // ---------- Здания ----------

        public static void CreateAssets()
        {
            BuildingAsset real = AssetManager.buildings.get("watch_tower_human");
            if (real == null) return;
            MakeAsset(real, TENT_ID, TENT_HEALTH, 0f, null);
            MakeAsset(real, FIRE_ID, 150f, 0f, null);
            // снаряды орудий: свои копии камня и ядра со шлейфом дыма и взрывом при попадании
            string rockId = MakeProjectile("siege_rock", "rock", "fx_explosion_tiny", 0.22f, 0.12f, Sfx.IMPACT_ROCK);
            string ballId = MakeProjectile("siege_cannonball", "cannonball", null, 0.32f, 0.09f, Sfx.IMPACT_BALL);
            SetBlast(rockId, "siege_blast_rock", (int)CATAPULT_BLAST, 1);
            SetBlast(ballId, "siege_blast_ball", (int)TREBUCHET_BLAST, 2);
            MakeAsset(real, CATAPULT_ID, CATAPULT_HEALTH, CATAPULT_DAMAGE, rockId);
            MakeAsset(real, TREBUCHET_ID, CATAPULT_HEALTH, TREBUCHET_DAMAGE, ballId);
            string shellId = MakeProjectile("siege_mortar_shell", "cannonball", "fx_explosion_middle", 0.4f, 0.07f, Sfx.IMPACT_MORTAR);
            SetBlast(shellId, "siege_blast_mortar", (int)MORTAR_BLAST, 2);
            try
            {
                ProjectileAsset sp = AssetManager.projectiles.get(shellId);
                if (sp != null && shellId == "siege_mortar_shell")
                {
                    sp.scale_start = 0.17f; sp.scale_target = 0.17f;
                    sp.speed = MortarSpeed(sp);
                    sp.end_effect_scale = 0.38f;
                    sp.world_actions = MortarImpact;
                }
                TerraformOptions mo = AssetManager.terraform.get("siege_blast_mortar");
                if (mo != null) { mo.force_power = 3.5f; mo.shake = true; mo.shake_duration = 0.35f; mo.shake_intensity = 1.4f; }
            }
            catch (System.Exception e) { WallBuilder.Log("mortar shell setup failed: " + e.Message); }
            MakeAsset(real, MORTAR_ID, MORTAR_HEALTH, MORTAR_DAMAGE, shellId);
            BuildingAsset mortarAsset = AssetManager.buildings.get(MORTAR_ID);
            if (mortarAsset != null) { mortarAsset.tower_projectile_reload = MORTAR_RELOAD; mortarAsset.scale_base = new UnityEngine.Vector3(0.25f / 1.5f, 0.25f / 1.5f, 0.25f / 1.5f); }
            AttachGunAnim(CATAPULT_ID, ANIM_CATAPULT);
            AttachGunAnim(TREBUCHET_ID, ANIM_TREBUCHET);
            AttachGunAnim(MORTAR_ID, ANIM_MORTAR);
            MakeAsset(real, TOWER_ID, 600f, 0f, null);
            BuildingAsset tw = AssetManager.buildings.get(TOWER_ID);
            if (tw != null)
            {
                // охранная башня стреляет по воинам (как дешёвая городская), здания не бьёт
                tw.tower = true;
                tw.tower_projectile = real.tower_projectile;
                tw.tower_projectile_amount = 1;
                tw.tower_projectile_reload = real.tower_projectile_reload / 1.5f;
                tw.tower_attack_buildings = false;
            }
            for (int pi = 0; pi < PROP_IDS.Length; pi++) MakeAsset(real, PROP_IDS[pi], 120f, 0f, null);
            for (int pi = 0; pi < SEA_PROP_IDS.Length; pi++) MakeAsset(real, SEA_PROP_IDS[pi], 120f, 0f, null);
            MakeAsset(real, BARRICADE_ID, 300f, 0f, null);
            MakeAsset(real, BARRICADE_L_ID, 300f, 0f, null);
            MakeAsset(real, BARRICADE_R_ID, 300f, 0f, null);
            MakeAsset(real, FLAG_ID, 300f, 0f, null);
            MakeAsset(real, TUSK_ID, 300f, 0f, null);
            MakeAsset(real, BANNER_ID, 300f, 0f, null);
            MakeAsset(real, BANNER_S_ID, 300f, 0f, null);
            MakeAsset(real, TUSK2_ID, 300f, 0f, null);
            MakeAsset(real, TUSK3_ID, 300f, 0f, null);
            BuildingAsset fire = AssetManager.buildings.get(FIRE_ID);
            if (fire != null) fire.animation_speed = 3.5f;
            BuildingAsset flag = AssetManager.buildings.get(FLAG_ID);
            if (flag != null) flag.animation_speed = FLAG_ANIM_SPEED;
            BuildingAsset banner = AssetManager.buildings.get(BANNER_ID);
            if (banner != null) banner.animation_speed = FLAG_ANIM_SPEED * 0.75f;
            BuildingAsset bannerS = AssetManager.buildings.get(BANNER_S_ID);
            if (bannerS != null) bannerS.animation_speed = FLAG_ANIM_SPEED * 0.75f;
            Fx.CreateAssets(real);
            MakeLogAssets();
        }

        internal static void MakeAssetPublic(BuildingAsset real, string id, float health) { MakeAsset(real, id, health, 0f, null); }

        // ---------- Плацдарм ----------
        // Когда с корабля на берег сходит отряд, у места высадки появляется маленький лагерь: костёр, палатки,
        // шлюпка, якорь и сети. Он стоит, пока корабль рядом, и исчезает, когда корабль уплыл, утонул или разрушен.
        // Орудий и стен тут нет: это только вид (отряд сам ведёт обычную осаду).

        private static bool BEACH_ENABLED = Cfg.Bool("beachhead", true, "плацдарм: маленький лагерь у места высадки, пока рядом стоит корабль");
        private static int BEACH_MIN = Cfg.Int("beachhead_min_warriors", 5, "сколько воинов должно сойти с корабля, чтобы появился плацдарм");
        private static float BEACH_CITY_DIST = Cfg.Float("beachhead_city_dist", 90f, "плацдарм ставится, если вражеский город ближе стольких клеток");
        private static float BEACH_LEAVE_DIST = Cfg.Float("beachhead_leave_dist", 20f, "корабль дальше стольких клеток от плацдарма считается уплывшим");
        private const double BEACH_LEAVE_SECONDS = 20.0;
        private const double BEACH_MAX_SECONDS = 900.0;
        private const int BEACH_MAX = 8;

        private class Beach
        {
            public Actor boat;
            public Kingdom kingdom;
            public WorldTile center;
            public List<Building> blds = new List<Building>();
            public double born;
            public double away_since = -1;
        }

        private class BoatInfo
        {
            public int peak;
            public double peak_time;
            public Kingdom owner;
        }

        private static readonly List<Beach> beaches = new List<Beach>();
        private static readonly Dictionary<Actor, BoatInfo> boat_info = new Dictionary<Actor, BoatInfo>();
        private static double _next_beach;

        internal static void BeachTick()
        {
            if (!BEACH_ENABLED) return;
            double now = World.world.getCurWorldTime();
            if (now < _next_beach) return;
            _next_beach = now + 5.0;
            try
            {
                // 1. живые плацдармы: корабль уплыл, потонул или лагерь разрушен
                for (int i = beaches.Count - 1; i >= 0; i--)
                {
                    Beach bc = beaches[i];
                    string why = null;
                    if (bc.boat == null || !bc.boat.isAlive() || bc.boat.current_tile == null) why = "boat destroyed";
                    else
                    {
                        float d = Toolbox.Dist(bc.boat.current_tile.x, bc.boat.current_tile.y, bc.center.x, bc.center.y);
                        if (d > BEACH_LEAVE_DIST)
                        {
                            if (bc.away_since < 0) bc.away_since = now;
                            else if (now - bc.away_since > BEACH_LEAVE_SECONDS) why = "boat left";
                        }
                        else bc.away_since = -1;
                    }
                    if (why == null && now - bc.born > BEACH_MAX_SECONDS) why = "expired";
                    if (why == null)
                    {
                        bool any = false;
                        for (int k = 0; k < bc.blds.Count; k++) if (bc.blds[k] != null && bc.blds[k].isAlive()) { any = true; break; }
                        if (!any) why = "camp destroyed";
                    }
                    if (why != null)
                    {
                        for (int k = 0; k < bc.blds.Count; k++) if (bc.blds[k] != null && bc.blds[k].isAlive()) bc.blds[k].startDestroyBuilding();
                        WallBuilder.Dbg("beachhead removed (" + why + ")");
                        beaches.RemoveAt(i);
                    }
                }

                // 2. высадки: на борту было много воинов, а теперь их стало вдвое меньше
                List<Actor> units = World.world.units.getSimpleList();
                for (int i = 0; i < units.Count; i++)
                {
                    Actor a = units[i];
                    if (a == null || !a.isAlive() || a.asset == null || !a.asset.is_boat || a.current_tile == null) continue;
                    Boat boat = a.getSimpleComponent<Boat>();
                    if (boat == null) continue;
                    int wc = 0;
                    Kingdom owner = null;
                    foreach (Actor p in boat.getPassengers())
                    {
                        if (p == null || !p.isAlive() || p.kingdom == null || !p.isWarrior()) continue;
                        wc++;
                        if (owner == null) owner = p.kingdom;
                    }
                    BoatInfo info;
                    if (!boat_info.TryGetValue(a, out info))
                    {
                        if (wc == 0) continue;
                        info = new BoatInfo();
                        boat_info[a] = info;
                    }
                    if (wc >= info.peak)
                    {
                        info.peak = wc;
                        info.peak_time = now;
                        if (owner != null) info.owner = owner;
                    }
                    else if (info.peak >= BEACH_MIN && wc <= info.peak / 2 && now - info.peak_time < 60.0)
                    {
                        if (beaches.Count < BEACH_MAX) TryBeach(a, info, now);
                        info.peak = wc;
                        info.peak_time = now;
                    }
                    else if (now - info.peak_time >= 60.0)
                    {
                        info.peak = wc;
                        info.peak_time = now;
                    }
                }

                if (boat_info.Count > 300)
                {
                    List<Actor> gone = new List<Actor>();
                    foreach (KeyValuePair<Actor, BoatInfo> kv in boat_info) if (kv.Key == null || !kv.Key.isAlive()) gone.Add(kv.Key);
                    foreach (Actor g in gone) if (g != null) boat_info.Remove(g);
                }
            }
            catch (System.Exception e) { WallBuilder.Log("beachhead tick failed: " + e.Message); }
        }

        private static void TryBeach(Actor boat, BoatInfo info, double now)
        {
            WorldTile bt = boat.current_tile;
            Kingdom owner = info.owner;
            if (bt == null || owner == null) return;

            // ближайшая суша к кораблю
            WorldTile land = null;
            int bestd = int.MaxValue;
            for (int dx = -5; dx <= 5; dx++)
                for (int dy = -5; dy <= 5; dy++)
                {
                    WorldTile n = World.world.GetTile(bt.x + dx, bt.y + dy);
                    if (n == null || n.is_liquid || n.Type.block || n.Type.wall) continue;
                    int d = dx * dx + dy * dy;
                    if (d < bestd) { bestd = d; land = n; }
                }
            if (land == null) return;

            // не у себя дома: высадка в чужие земли
            if (land.zone != null && land.zone.city != null && land.zone.city.kingdom == owner) return;

            // рядом должен быть вражеский город
            City target = null;
            float td = BEACH_CITY_DIST;
            foreach (City c in World.world.cities)
            {
                if (c == null || !c.isAlive() || c.kingdom == null || c.getTile() == null) continue;
                if (!c.kingdom.isEnemy(owner)) continue;
                float d = Toolbox.Dist(land.x, land.y, c.getTile().x, c.getTile().y);
                if (d < td) { td = d; target = c; }
            }
            if (target == null) return;

            for (int i = 0; i < beaches.Count; i++)
                if (beaches[i].kingdom == owner && Toolbox.Dist(beaches[i].center.x, beaches[i].center.y, land.x, land.y) < 25f) return;

            // раскладка: от берега вглубь суши. шлюпка у воды, якорь рядом, дальше костёр и палатки по бокам
            int ix = 0, iy = 0;
            int ax = land.x - bt.x, ay = land.y - bt.y;
            if (System.Math.Abs(ax) >= System.Math.Abs(ay)) ix = ax >= 0 ? 1 : -1; else iy = ay >= 0 ? 1 : -1;
            int px = -iy, py = ix;
            Beach bc = new Beach();
            bc.boat = boat;
            bc.kingdom = owner;
            bc.born = now;
            bc.center = World.world.GetTile(land.x + ix * 3, land.y + iy * 3) ?? land;

            AddBeach(bc, PROP_BOAT, World.world.GetTile(land.x, land.y), 1);
            AddBeach(bc, PROP_ANCHOR, World.world.GetTile(land.x + px * 2, land.y + py * 2), 2);
            AddBeach(bc, FIRE_ID, bc.center, 2);
            AddBeach(bc, TENT_ID, World.world.GetTile(bc.center.x + px * 3 + ix, bc.center.y + py * 3 + iy), 2);
            if (info.peak >= 8) AddBeach(bc, TENT_ID, World.world.GetTile(bc.center.x - px * 3 + ix, bc.center.y - py * 3 + iy), 2);
            AddBeach(bc, PROP_NETS, World.world.GetTile(bc.center.x - px * 2 + ix * 2, bc.center.y - py * 2 + iy * 2), 2);

            if (bc.blds.Count == 0) return;
            beaches.Add(bc);
            WallBuilder.Dbg("beachhead landed: " + owner.name + " near " + target.name + " at " + land.x + "," + land.y + " (warriors landed: " + info.peak + ")");
        }

        private static void AddBeach(Beach bc, string id, WorldTile at, int radius)
        {
            WorldTile t = FreeSpot(at, radius);
            if (t == null) return;
            Building b = Put(id, t, bc.kingdom);
            if (b != null) bc.blds.Add(b);
        }

        // ---------- Вражеский флот ----------

        private static bool FLEET_NEWS = Cfg.Bool("fleet_news", true, "новость, когда вражеские корабли с воинами подходят к городу");
        private static float FLEET_DIST = Cfg.Float("fleet_news_dist", 45f, "на каком расстоянии от города (клетки) вражеские корабли считаются подошедшими");
        private static int FLEET_MIN = Cfg.Int("fleet_news_min", 4, "минимум воинов на кораблях рядом с городом для новости");
        private static double _next_fleet;
        private static readonly Dictionary<City, double> fleet_seen = new Dictionary<City, double>();

        private class FleetHit
        {
            public City city;
            public Kingdom kingdom;
            public int warriors;
            public WorldTile where;
        }

        // Раз в 10 игровых секунд смотрим, не подошли ли к чужому городу корабли с воинами враждебного королевства
        internal static void FleetTick()
        {
            if (!FLEET_NEWS) return;
            double now = World.world.getCurWorldTime();
            if (now < _next_fleet) return;
            _next_fleet = now + 10.0;
            try
            {
                List<FleetHit> hits = null;
                List<Actor> units = World.world.units.getSimpleList();
                for (int i = 0; i < units.Count; i++)
                {
                    Actor a = units[i];
                    if (a == null || !a.isAlive() || a.asset == null || !a.asset.is_boat || a.current_tile == null) continue;
                    Boat boat = a.getSimpleComponent<Boat>();
                    if (boat == null) continue;
                    int warriors = 0;
                    Kingdom owner = null;
                    foreach (Actor p in boat.getPassengers())
                    {
                        if (p == null || !p.isAlive() || p.kingdom == null || !p.isWarrior()) continue;
                        warriors++;
                        if (owner == null) owner = p.kingdom;
                    }
                    if (warriors == 0 || owner == null) continue;

                    City best = null;
                    float bestDist = FLEET_DIST;
                    foreach (City c in World.world.cities)
                    {
                        if (c == null || !c.isAlive() || c.kingdom == null || c.getTile() == null) continue;
                        if (!c.kingdom.isEnemy(owner)) continue;
                        float d = (float)Toolbox.Dist(a.current_tile.x, a.current_tile.y, c.getTile().x, c.getTile().y);
                        if (d < bestDist) { bestDist = d; best = c; }
                    }
                    if (best == null) continue;

                    if (hits == null) hits = new List<FleetHit>();
                    FleetHit hit = null;
                    for (int h = 0; h < hits.Count; h++) if (hits[h].city == best && hits[h].kingdom == owner) { hit = hits[h]; break; }
                    if (hit == null) { hit = new FleetHit(); hit.city = best; hit.kingdom = owner; hit.where = a.current_tile; hits.Add(hit); }
                    hit.warriors += warriors;
                }
                if (hits != null)
                    foreach (FleetHit hit in hits)
                    {
                        if (hit.warriors < FLEET_MIN) continue;
                        double last;
                        if (fleet_seen.TryGetValue(hit.city, out last) && now - last < 300.0) continue;
                        fleet_seen[hit.city] = now;
                        Announce(LOG_FLEET, hit.city, hit.kingdom, hit.where);
                        WallBuilder.Dbg("fleet sighted: " + hit.kingdom.name + " -> " + hit.city.name + " (warriors on boats: " + hit.warriors + ")");
                    }
            }
            catch (System.Exception e) { WallBuilder.Log("fleet news failed: " + e.Message); }
        }

        // ---------- Новости ----------

        private const string LOG_START = "mod_siege_started";
        private const string LOG_LIFTED = "mod_siege_lifted";
        private const string LOG_FELL = "mod_siege_fell";
        private const string LOG_RUINED = "mod_siege_ruined";
        private const string LOG_FLEET = "mod_fleet_sighted";
        private const string LOG_WALL_INNER = "mod_wall_inner";
        private const string LOG_WALL_OUTER = "mod_wall_outer";

        internal static bool Ru()
        {
            return LocalizedTextManager.instance != null && LocalizedTextManager.instance.language == "ru";
        }

        private static void MakeLogAssets()
        {
            AddLog(LOG_START, "ui/Icons/iconWar", Toolbox.color_log_warning);
            AddLog(LOG_LIFTED, "ui/Icons/actor_traits/iconPacifist", Toolbox.color_log_good);
            AddLog(LOG_RUINED, "ui/Icons/iconDead", Toolbox.color_log_warning);
            AddLog(LOG_FLEET, "ui/Icons/iconWar", Toolbox.color_log_warning);
            AddLog(LOG_WALL_INNER, "ui/Icons/iconCitySelect", Toolbox.color_log_neutral);
            AddLog(LOG_WALL_OUTER, "ui/Icons/iconCitySelect", Toolbox.color_log_neutral);
            AddLog(LOG_FELL, "ui/Icons/iconDead", Toolbox.color_log_warning);
        }

        private static void AddLog(string id, string icon, UnityEngine.Color color)
        {
            if (AssetManager.world_log_library.get(id) != null) return;
            WorldLogAsset a = new WorldLogAsset();
            a.id = id;
            a.group = "wars";
            a.path_icon = icon;
            a.color = color;
            a.text_replacer = delegate (WorldLogMessage m, ref string t)
            {
                t = LocalizedTextManager.getText(id).Replace("$1", m.getSpecial(1)).Replace("$2", m.getSpecial(2));
            };
            AssetManager.world_log_library.add(a);
        }

        internal static void AnnounceWall(City city, bool outer)
        {
            if (city == null || city.kingdom == null) return;
            Announce(outer ? LOG_WALL_OUTER : LOG_WALL_INNER, city, city.kingdom, city.getTile());
        }

        private static void Announce(string id, City city, Kingdom other, WorldTile where)
        {
            try
            {
                if (city == null || city.kingdom == null || other == null) return;
                Loc.Apply();     // игра ищет текст события в своих переводах: убеждаемся, что наши тексты на языке игры уже добавлены
                WorldLogMessage m = new WorldLogMessage(AssetManager.world_log_library.get(id), city.name, other.name);
                m.color_special1 = city.kingdom.getColor().getColorText();
                m.color_special2 = other.getColor().getColorText();
                if (where != null) m.location = new UnityEngine.Vector2(where.x, where.y);
                m.add();
                WfLinks.News(id, LocalizedTextManager.getText(id).Replace("$1", city.name).Replace("$2", other.name), where);
            }
            catch (System.Exception e) { WallBuilder.Log("news failed: " + e.Message); }
        }

        // Копия снаряда с дымным шлейфом (и своим эффектом попадания); если не вышло - остаётся обычный снаряд
        internal static string CoastShotId()
        {
            return MakeProjectile("coast_shot", "cannonball", "fx_explosion_tiny", 0.28f, 0.09f, "event:/SFX/EXPLOSIONS/ExplosionSmall");
        }

        private static float CATAPULT_BLAST = Cfg.Float("catapult_blast", 160f, "урон взрыва камня катапульты по зданиям и воинам (радиус 1)");
        private static float TREBUCHET_BLAST = Cfg.Float("trebuchet_blast", 320f, "урон взрыва ядра требушета по зданиям и воинам (радиус 2)");

        // Снаряду орудия даём свой взрыв: у ядра игры урон по зданиям нулевой, поэтому дома не разрушались
        private static void SetBlast(string projectileId, string optionId, int damage, int range)
        {
            try
            {
                ProjectileAsset p = AssetManager.projectiles.get(projectileId);
                if (p == null || !projectileId.StartsWith("siege_")) return;
                if (AssetManager.terraform.get(optionId) == null)
                {
                    TerraformOptions o = AssetManager.terraform.clone(optionId, "cannonball");
                    o.damage_buildings = true;
                    o.damage = damage;
                    o.apply_force = true;
                    o.force_power = 2f;
                    o.shake = false;
                    o.explode_tile = false;                  // взрыв не должен разрушать клетки: иначе выбивало стену ворот и жгло землю
                    o.explode_and_set_random_fire = false;
                }
                p.terraform_option = optionId;
                p.terraform_range = range;
            }
            catch (System.Exception e) { WallBuilder.Log("blast " + optionId + " failed: " + e.Message); }
        }

        private static string MakeProjectile(string id, string src, string endEffect, float trailScale, float trailTimer, string impactSound)
        {
            try
            {
                if (AssetManager.projectiles.get(id) == null)
                {
                    ProjectileAsset p = AssetManager.projectiles.clone(id, src);
                    if (!string.IsNullOrEmpty(endEffect)) p.end_effect = endEffect;
                    p.trail_effect_enabled = true;
                    p.trail_effect_id = "fx_fire_smoke";
                    p.trail_effect_scale = trailScale;
                    p.trail_effect_timer = trailTimer;
                    if (!string.IsNullOrEmpty(impactSound)) p.sound_impact = impactSound;   // слышно, где упал снаряд
                }
                return id;
            }
            catch (System.Exception e)
            {
                WallBuilder.Log("projectile " + id + " failed, using " + src + ": " + e.Message);
                return src;
            }
        }

        private static void MakeAsset(BuildingAsset real, string id, float health, float damage, string projectile)
        {
            if (AssetManager.buildings.get(id) != null) return;
            BuildingAsset a = AssetManager.buildings.clone(id, "watch_tower_human");
            a.sprite_path = string.IsNullOrEmpty(real.sprite_path) ? real.main_path + real.id : real.sprite_path;
            a.cost = new ConstructionCost(0, 0, 0, 0);
            a.can_be_upgraded = false;
            a.upgrade_to = string.Empty;
            a.buildings.Clear();
            a.type = "type_siege_camp";
            a.city_building = false;        // иначе игра сочтёт постройку заброшенной и снесёт её
            // При загрузке сохранения у не-городских построек королевство берётся только из asset.kingdom.
            // Без него здание остаётся без королевства, и игра падает в WildKingdomsManager.updateDirtyBuildings
            // (мир загружается пустым). Лагерь после загрузки всё равно сносится подметальщиком и строится заново.
            a.kingdom = "ruins";
            a.has_ruins_graphics = false;
            // у башни площадь 3x2 клетки, а игра сносит любое здание на занятых клетках: у копий одна клетка
            a.fundament = new BuildingFundament(0, 0, 0, 0);
            if (real.atlas_asset != null) a.atlas_asset = real.atlas_asset;
            a.base_stats["health"] = health;
            if (projectile != null)
            {
                a.base_stats["damage"] = damage;
                a.tower = true;
                a.tower_projectile = projectile;
                a.tower_projectile_reload = CATAPULT_RELOAD;
                a.tower_projectile_amount = 1;
                a.tower_attack_buildings = true;
                a.tower_projectile_offset = 6f;
            }
            else
            {
                a.tower = false;
                a.tower_projectile = string.Empty;
            }
        }

        // ---------- Взрыв мортиры: кратер, который дымится ----------
        private class Crater { public WorldTile tile; public double until, next; }
        private static readonly List<Crater> craters = new List<Crater>();

        private static bool MortarImpact(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile t)
        {
            try
            {
                if (t == null) return true;
                int R = System.Math.Max(1, MORTAR_CRATER);
                for (int dx = -R; dx <= R; dx++)
                    for (int dy = -R; dy <= R; dy++)
                    {
                        float d = (float)System.Math.Sqrt(dx * dx + dy * dy);
                        if (d > R + 0.4f) continue;
                        WorldTile n = World.world.GetTile(t.x + dx, t.y + dy);
                        if (n == null || n.is_liquid || n.Type.wall) continue;
                        if (Randy.randomFloat(0f, 1f) < 0.95f) n.setBurned();
                        if (d <= R * 0.9f && !n.hasBuilding()) MapAction.decreaseTile(n, false, "flash");   // сносим траву и дёрн: воронка
                        if (d <= R * 0.5f && !n.hasBuilding() && !n.is_liquid) MapAction.decreaseTile(n, false, "flash");   // в центре копаем глубже
                    }
                EffectsLibrary.spawnAtTileRandomScale("fx_explosion_middle", t, 0.35f, 0.45f);
                EffectsLibrary.spawnAtTileRandomScale("fx_explosion_small", t, 0.2f, 0.28f);
                for (int i = 0; i < 5; i++)
                {
                    WorldTile pt = World.world.GetTile(t.x + Randy.randomInt(-R, R + 1), t.y + Randy.randomInt(-R, R + 1));
                    if (pt != null) EffectsLibrary.spawnAtTile("fx_fire_smoke", pt, Randy.randomFloat(0.2f, 0.36f));
                }
                WallBuilder.BlastWalls(t, 2);
                double now = World.world.getCurWorldTime();
                if (craters.Count > 24) craters.RemoveAt(0);
                craters.Add(new Crater { tile = t, until = now + MORTAR_SMOKE_SECONDS, next = now + 0.4 });
            }
            catch (System.Exception e) { WallBuilder.Log("mortar impact failed: " + e.Message); }
            return true;
        }

        // Здание без королевства (владелец исчез: город опустел, королевство распалось) роняет отрисовку мира в BuildingManager.
        // Раз в секунду отдаём такие здания ничьему королевству «руины»; пишем в лог, чьё это здание
        private static float next_orphan;
        private static int orphan_logged;
        internal static void OrphanTick()
        {
            try
            {
                float rt = UnityEngine.Time.realtimeSinceStartup;
                if (rt < next_orphan) return;
                next_orphan = rt + 1f;
                if (World.world == null || World.world.buildings == null) return;
                Kingdom ruins = World.world.kingdoms_wild.get("ruins");
                if (ruins == null) return;
                List<Building> all = World.world.buildings.getSimpleList();
                for (int i = all.Count - 1; i >= 0; i--)
                {
                    Building b = all[i];
                    if (b == null || b.asset == null) continue;
                    if (b.kingdom != null && b.kingdom.asset != null && (b.kingdom.wild || b.asset.city_building || b.kingdom.isAlive())) continue;
                    if (orphan_logged++ < 30) WallBuilder.Dbg("building without kingdom: " + b.asset.id + " at " + (b.current_tile != null ? b.current_tile.x + "," + b.current_tile.y : "?") + ", handed to ruins");
                    try { b.setKingdom(ruins); } catch (System.Exception) { }
                }
            }
            catch (System.Exception) { }
        }

        // Город сменил королевство (захват, восстание): знамёна, баррикады и прочие постройки мода должны перейти к новому владельцу вместе с городом.
        // Иначе они остаются у прежнего королевства, а когда оно гибнет, ломают отрисовку (OrphanTick остаётся как страховка)
        public static void JoinPrefix(City __instance, out Kingdom __state)
        {
            __state = null;
            try { __state = __instance != null ? __instance.kingdom : null; } catch (System.Exception) { }
        }

        public static void JoinPostfix(City __instance, Kingdom __state)
        {
            try
            {
                if (__instance == null || __state == null || __instance.kingdom == null || __instance.kingdom == __state) return;
                List<Building> all = World.world.buildings.getSimpleList();
                int moved = 0;
                for (int i = all.Count - 1; i >= 0; i--)
                {
                    Building b = all[i];
                    if (b == null || b.asset == null || b.kingdom != __state || b.asset.city_building) continue;
                    string id = b.asset.id;
                    if (!ModBuilding(id)) continue;
                    WorldTile t = b.current_tile;
                    if (t == null || t.zone == null || t.zone.city != __instance) continue;
                    try { b.setKingdom(__instance.kingdom); moved++; } catch (System.Exception) { }
                }
                if (moved > 0) WallBuilder.Log(__instance.name + " changed hands: " + moved + " banner/barricade building(s) passed to " + __instance.kingdom.name);
            }
            catch (System.Exception) { }
        }

        private static bool ModBuilding(string id)
        {
            return id == STAKE_ID || id == STAKE_V_ID || id == BARRICADE_ID || id == BARRICADE_L_ID || id == BARRICADE_R_ID || id == FLAG_ID || id == TUSK_ID || id == BANNER_ID || id == BANNER_S_ID;
        }

        // Отрисовка зданий падает на здании без королевства (после захвата города). Исключение вылетает из MapBox.Update каждый кадр, и наши
        // тики (в том числе OrphanTick) после него не выполняются. Поэтому чиним прямо перед отрисовкой, а саму ошибку гасим на случай другой причины
        private static int render_fixed_logged, render_err_logged;
        public static void RenderGuardPrefix(BuildingManager __instance)
        {
            try
            {
                Building[] arr = __instance.getVisibleBuildings();
                int n = __instance.countVisibleBuildings();
                if (arr == null) return;
                Kingdom ruins = null;
                int w = 0; bool compact = false;
                for (int i = 0; i < n; i++)
                {
                    Building b = arr[i];
                    if (b == null || b.asset == null) { compact = true; continue; }
                    if (b.kingdom == null || b.kingdom.asset == null)
                    {
                        if (ruins == null) ruins = World.world.kingdoms_wild.get("ruins");
                        if (render_fixed_logged++ < 20) WallBuilder.Log("render guard: building " + b.asset.id + " without kingdom (kingdom " + (b.kingdom == null ? "null" : "cleared") + "), handed to ruins");
                        try { if (ruins != null) b.setKingdom(ruins); } catch (System.Exception) { }
                        if (b.kingdom == null || b.kingdom.asset == null) { compact = true; continue; }
                    }
                    arr[w++] = b;
                }
                if (compact)
                {
                    for (int i = w; i < n; i++) arr[i] = null;
                    HarmonyLib.Traverse.Create(__instance).Field("_visible_buildings_count").SetValue(w);
                    if (render_fixed_logged++ < 20) WallBuilder.Log("render guard: dropped " + (n - w) + " unrenderable building(s) from the frame");
                }
            }
            catch (System.Exception) { }
        }

        public static System.Exception RenderGuardFinalizer(System.Exception __exception)
        {
            if (__exception == null) return null;
            try { if (render_err_logged++ < 5) WallBuilder.Log("render error swallowed: " + __exception.GetType().Name + ": " + __exception.Message); } catch (System.Exception) { }
            return null;
        }

        // Кратеры от мортиры тлеют и дымят какое-то время
        internal static void CraterTick()
        {
            if (craters.Count == 0) return;
            try
            {
                double now = World.world.getCurWorldTime();
                for (int i = craters.Count - 1; i >= 0; i--)
                {
                    Crater c = craters[i];
                    if (now >= c.until) { craters.RemoveAt(i); continue; }
                    if (now < c.next) continue;
                    c.next = now + Randy.randomFloat(0.5f, 1.1f);
                    float left = (float)((c.until - now) / MORTAR_SMOKE_SECONDS);
                    WorldTile pt = World.world.GetTile(c.tile.x + Randy.randomInt(-2, 3), c.tile.y + Randy.randomInt(-2, 3));
                    if (pt != null) EffectsLibrary.spawnAtTile("fx_fire_smoke", pt, Randy.randomFloat(0.12f, 0.12f + 0.2f * left));
                }
            }
            catch (System.Exception) { }
        }

        // Выстрел: облако дыма и пыли вокруг орудия, у требушета ещё хлопок, у катапульты скрип дерева
        private static void MuzzleEffects(Building b)
        {
            try
            {
                WorldTile mt = b.current_tile;
                if (mt == null) return;
                if (b.asset.id == MORTAR_ID)
                {
                    // мортира: густое облако дыма и вспышка над стволом
                    for (int i = 0; i < 6; i++)
                    {
                        WorldTile pt = World.world.GetTile(mt.x + Randy.randomInt(-3, 2), mt.y + Randy.randomInt(0, 5));
                        if (pt == null) continue;
                        EffectsLibrary.spawnAtTile("fx_fire_smoke", pt, Randy.randomFloat(0.18f, 0.34f));
                    }
                    WorldTile ft = World.world.GetTile(mt.x - 1, mt.y + 4);
                    if (ft != null) EffectsLibrary.spawnAtTileRandomScale("fx_explosion_small", ft, 0.12f, 0.18f);
                    return;
                }
                bool treb = b.asset.id == TREBUCHET_ID;
                // лёгкое облачко дыма у орудия (дым в игре тёмный, поэтому мелкий и немного)
                int puffs = treb ? 2 : 1;
                for (int i = 0; i < puffs; i++)
                {
                    WorldTile pt = World.world.GetTile(mt.x + Randy.randomInt(-1, 2), mt.y + Randy.randomInt(0, 2));
                    if (pt == null) continue;
                    EffectsLibrary.spawnAtTile("fx_fire_smoke", pt, Randy.randomFloat(0.12f, 0.22f));
                }
                if (!treb) MusicBox.playSound("event:/SFX/BUILDINGS/DestroyBuildingWood", mt, pGameViewOnly: true, pVisibleOnly: true);
            }
            catch (System.Exception) { }
        }

        // ---------- Анимация выстрела орудий ----------
        // Кадры: 0 - заряжено, 1-3 - бросок рычага, 4 - рычаг внизу (перезарядка), 5 - рычаг поднимается. Сетки с запасом сверху (рычаг уходит вверх)
        static readonly string[][] ANIM_CATAPULT = new string[][] {
            new string[] {
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".DSsSD...............",
                ".DsL.............PFFF",
                "..DDDl.lL...LM...PFFf",
                "....cMLlM...Mm...Pff.",
                ".....cMLLDDDMm...Pn..",
                "......cMMLLdMm...P...",
                "......cLMMMkkkk..P...",
                "......DLM..SsSs..P...",
                ".DlLlDLllLllLllDllLD.",
                ".dmMmMmMmMmMmMmMmMmd.",
                "...wwDwW.....wwDwW...",
                "..WwDDDwW...WwDDDwW..",
                "..WDDlDDdddddDDlDDW..",
                "..WwDDDwW...WwDDDwW..",
                "...WwDwW.....WwDwW...",
                "...EWWWeeSSeEeWWWe...",
                ".eSEssSEeessEseESESS.",
                "SeeESeEeEeEesEeSsSesE"
            },
            new string[] {
                ".....................",
                ".........s...........",
                "........SsS..........",
                "........DDD..........",
                "........DdD..........",
                "........LMm..........",
                "........LMm..........",
                "........LMmm.....PFFF",
                ".........LMmLM...PFFf",
                ".........LMmMm...Pff.",
                ".........DDDMm...Pn..",
                ".........DdDMm...P...",
                ".........DDDkkk..P...",
                "...........SsSs..P...",
                ".DlLlDLllLllLllDllLD.",
                ".dmMmMmMmMmMmMmMmMmd.",
                "...wwDwW.....wwDwW...",
                "..WwDDDwW...WwDDDwW..",
                "..WDDlDDdddddDDlDDW..",
                "..WwDDDwW...WwDDDwW..",
                "...WwDwW.....WwDwW...",
                "...EWWWeeSSeEeWWWe...",
                ".eSEssSEeessEseESESS.",
                "SeeESeEeEeEesEeSsSesE"
            },
            new string[] {
                ".....................",
                ".....................",
                ".....................",
                "...............L.....",
                "..............LDDD...",
                "..............LDdD...",
                ".............LM.m....",
                "............LMmm.PFFF",
                "...........LMMm..PFFf",
                "..........L.Mm...Pff.",
                ".........DDDmm...Pn..",
                ".........DdDmm...P...",
                ".........DDDkkk..P...",
                "...........SsSs..P...",
                ".DlLlDLllLllLllDllLD.",
                ".dmMmMmMmMmMmMmMmMmd.",
                "...wwDwW.....wwDwW...",
                "..WwDDDwW...WwDDDwW..",
                "..WDDlDDdddddDDlDDW..",
                "..WwDDDwW...WwDDDwW..",
                "...WwDwW.....WwDwW...",
                "...EWWWeeSSeEeWWWe...",
                ".eSEssSEeessEseESESS.",
                "SeeESeEeEeEesEeSsSesE"
            },
            new string[] {
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".................PFFF",
                "............LM...PFFf",
                "............Mm.LLLLL.",
                ".........DDDLLLMMMDDD",
                ".........DdDMMMmmmDdD",
                ".........DDDmmm..P...",
                "...........SsSs..P...",
                ".DlLlDLllLllLllDllLD.",
                ".dmMmMmMmMmMmMmMmMmd.",
                "...wwDwW.....wwDwW...",
                "..WwDDDwW...WwDDDwW..",
                "..WDDlDDdddddDDlDDW..",
                "..WwDDDwW...WwDDDwW..",
                "...WwDwW.....WwDwW...",
                "...EWWWeeSSeEeWWWe...",
                ".eSEssSEeessEseESESS.",
                "SeeESeEeEeEesEeSsSesE"
            },
            new string[] {
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".................PFFF",
                "............LM...PFFf",
                "............Mm...Pff.",
                ".........DDDMm...Pn..",
                ".........DdDLLL..P...",
                ".........DDDMMMLLP...",
                "...........SmmMMMLL..",
                ".DlLlDLllLllLlmmmDDD.",
                ".dmMmMmMmMmMmMmMmDdD.",
                "...wwDwW.....wwDwW...",
                "..WwDDDwW...WwDDDwW..",
                "..WDDlDDdddddDDlDDW..",
                "..WwDDDwW...WwDDDwW..",
                "...WwDwW.....WwDwW...",
                "...EWWWeeSSeEeWWWe...",
                ".eSEssSEeessEseESESS.",
                "SeeESeEeEeEesEeSsSesE"
            },
            new string[] {
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                "........m............",
                "......DDD........PFFF",
                "......DdDm..LM...PFFf",
                ".......LMmm.Mm...Pff.",
                "........LDDDMm...Pn..",
                ".........DdDMm...P...",
                ".........DDDkkk..P...",
                "...........SsSs..P...",
                ".DlLlDLllLllLllDllLD.",
                ".dmMmMmMmMmMmMmMmMmd.",
                "...wwDwW.....wwDwW...",
                "..WwDDDwW...WwDDDwW..",
                "..WDDlDDdddddDDlDDW..",
                "..WwDDDwW...WwDDDwW..",
                "...WwDwW.....WwDwW...",
                "...EWWWeeSSeEeWWWe...",
                ".eSEssSEeessEseESESS.",
                "SeeESeEeEeEesEeSsSesE"
            },
        };
        static readonly string[][] ANIM_TREBUCHET = new string[][] {
            new string[] {
                "...................",
                "...................",
                "...................",
                "...................",
                "...................",
                "...................",
                ".L..........PFFFF..",
                "cML.........PFFf...",
                "CcMLl.......Pff....",
                "cC.MLLl.....Pn.....",
                "SsS.MML.....P......",
                "SSk...MLlkDDP......",
                ".......MLkmM.......",
                ".......LML.mM......",
                ".......LMMLmM......",
                ".......LM.MLL......",
                "......LMDDDMML.....",
                "......LM....mML....",
                "......LM....mMML...",
                ".....LM......mMM...",
                ".....LMDdddDDmMc...",
                "....LM.d......mC...",
                "....LM.......sSsSk.",
                "....LMd......SDDDk.",
                "...LM........sSsSk.",
                "DllLMdLllLllLkkkkkD",
                "dMmMmMmMmMmMmMmMmMd",
                "..eeeesESsSseESEe..",
                "SEeSseeEeeeSeEEeeeS",
                "SeeeeEEeEsseeseeESE"
            },
            new string[] {
                "...................",
                "........LMm........",
                "........Lcm........",
                "........LCm........",
                "........SsS........",
                "........LMm........",
                "........LMm.PFFFF..",
                "........LMm.PFFf...",
                "........LMm.Pff....",
                "........LMm.Pn.....",
                "........LMm.P......",
                "........DDDDP......",
                "........DDDM.......",
                "........DDDmM......",
                "..........LmM......",
                ".......LM.MLL......",
                "......LMDDDMML.....",
                "......LM....mML....",
                "......LM....mMML...",
                ".....LM......mMM...",
                ".....LMDdddDDmMc...",
                "....LM.d......mC...",
                "....LM.......sSsSk.",
                "....LMd......SDDDk.",
                "...LM........sSsSk.",
                "DllLMdLllLllLkkkkkD",
                "dMmMmMmMmMmMmMmMmMd",
                "..eeeesESsSseESEe..",
                "SEeSseeEeeeSeEEeeeS",
                "SeeeeEEeEsseeseeESE"
            },
            new string[] {
                "...................",
                "...................",
                "...................",
                "................L..",
                "...............LLM.",
                "..............LLMmm",
                "............PLLMmm.",
                "............PLMmm..",
                "...........LLMmm...",
                "..........LLMm.....",
                ".........LLMmm.....",
                "........DDDmm......",
                "........DDDm.......",
                "........DDDmM......",
                "..........LmM......",
                ".......LM.MLL......",
                "......LMDDDMML.....",
                "......LM....mML....",
                "......LM....mMML...",
                ".....LM......mMM...",
                ".....LMDdddDDmMc...",
                "....LM.d......mC...",
                "....LM.......sSsSk.",
                "....LMd......SDDDk.",
                "...LM........sSsSk.",
                "DllLMdLllLllLkkkkkD",
                "dMmMmMmMmMmMmMmMmMd",
                "..eeeesESsSseESEe..",
                "SEeSseeEeeeSeEEeeeS",
                "SeeeeEEeEsseeseeESE"
            },
            new string[] {
                "...................",
                "...................",
                "...................",
                "...................",
                "...................",
                "...................",
                "............PFFFF..",
                "............PFFf...",
                "............Pff....",
                "............Pn...LL",
                "............LLLLLLM",
                "........DDDLMMMMMMm",
                "........DDDMmmmmmm.",
                "........DDDmm......",
                "..........LmM......",
                ".......LM.MLL......",
                "......LMDDDMML.....",
                "......LM....mML....",
                "......LM....mMML...",
                ".....LM......mMM...",
                ".....LMDdddDDmMc...",
                "....LM.d......mC...",
                "....LM.......sSsSk.",
                "....LMd......SDDDk.",
                "...LM........sSsSk.",
                "DllLMdLllLllLkkkkkD",
                "dMmMmMmMmMmMmMmMmMd",
                "..eeeesESsSseESEe..",
                "SEeSseeEeeeSeEEeeeS",
                "SeeeeEEeEsseeseeESE"
            },
            new string[] {
                "...................",
                "...................",
                "...................",
                "...................",
                "...................",
                "...................",
                "............PFFFF..",
                "............PFFf...",
                "............Pff....",
                "............Pn.....",
                "............P......",
                "........DDDDP......",
                "........DDDLLLL....",
                "........DDDMMMLLL..",
                "..........LmmmMMMLL",
                ".......LM.MLLmmmmMM",
                "......LMDDDMML...mm",
                "......LM....mML....",
                "......LM....mMML...",
                ".....LM......mMM...",
                ".....LMDdddDDmMc...",
                "....LM.d......mC...",
                "....LM.......sSsSk.",
                "....LMd......SDDDk.",
                "...LM........sSsSk.",
                "DllLMdLllLllLkkkkkD",
                "dMmMmMmMmMmMmMmMmMd",
                "..eeeesESsSseESEe..",
                "SEeSseeEeeeSeEEeeeS",
                "SeeeeEEeEsseeseeESE"
            },
            new string[] {
                "...................",
                "...................",
                "...................",
                "...................",
                "...................",
                ".....m.............",
                "....Mmm.....PFFFF..",
                "...LcMm.....PFFf...",
                "....CLMm....Pff....",
                "...SsSMmm...Pn.....",
                "......LMmm..P......",
                ".......LDDDDP......",
                ".......LDDDM.......",
                "........DDDmM......",
                "..........LmM......",
                ".......LM.MLL......",
                "......LMDDDMML.....",
                "......LM....mML....",
                "......LM....mMML...",
                ".....LM......mMM...",
                ".....LMDdddDDmMc...",
                "....LM.d......mC...",
                "....LM.......sSsSk.",
                "....LMd......SDDDk.",
                "...LM........sSsSk.",
                "DllLMdLllLllLkkkkkD",
                "dMmMmMmMmMmMmMmMmMd",
                "..eeeesESsSseESEe..",
                "SEeSseeEeeeSeEEeeeS",
                "SeeeeEEeEsseeseeESE"
            },
        };

        private static readonly Dictionary<string, UnityEngine.Sprite[][]> anim_sprites = new Dictionary<string, UnityEngine.Sprite[][]>();
        static readonly string[][] ANIM_MORTAR = new string[][] { GRID_MORTAR_F0, GRID_MORTAR_F1, GRID_MORTAR_F2, GRID_MORTAR_F3, GRID_MORTAR_F4, GRID_MORTAR_F5, GRID_MORTAR_F6, GRID_MORTAR_F7 };

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<long, double> gun_shot = new System.Collections.Concurrent.ConcurrentDictionary<long, double>();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<long, double> gun_reload = new System.Collections.Concurrent.ConcurrentDictionary<long, double>();
        private static bool GUN_ANIM = Cfg.Bool("siege_gun_anim", true, "анимация выстрела у требушета и катапульты (рычаг делает бросок)");

        private static void AttachGunAnim(string id, string[][] frames)
        {
            if (!GUN_ANIM) return;
            BuildingAsset a = AssetManager.buildings.get(id);
            if (a == null) return;
            UnityEngine.Sprite[][] arr = new UnityEngine.Sprite[frames.Length][];
            for (int i = 0; i < frames.Length; i++) arr[i] = new UnityEngine.Sprite[] { WallBuilder.FromGrid(frames[i], "main_0") };
            anim_sprites[id] = arr;
            a.get_override_sprites_main = GunFrame;
        }

        // Вызывается при отрисовке (в том числе из параллельных потоков): только чтение
        private static UnityEngine.Sprite[] GunFrame(Building b)
        {
            UnityEngine.Sprite[][] arr;
            if (b == null || b.asset == null || !anim_sprites.TryGetValue(b.asset.id, out arr)) return null;
            int f = 0;
            double shot;
            if (gun_shot.TryGetValue(b.getID(), out shot))
            {
                if (arr.Length == 8)
                {
                    // мортира: 8 кадров (вспышка, откат, три стадии дыма, возврат)
                    double me = World.world.getCurWorldTime() - shot;
                    int mf = me < 0 ? 0 : me < 0.10 ? 1 : me < 0.22 ? 2 : me < 0.40 ? 3 : me < 0.80 ? 4 : me < 1.40 ? 5 : me < 2.00 ? 6 : me < 2.60 ? 7 : 0;
                    return arr[mf];
                }
                double dur; if (!gun_reload.TryGetValue(b.getID(), out dur)) dur = CATAPULT_RELOAD;
                if (dur < 2.0) dur = 2.0;
                double e = World.world.getCurWorldTime() - shot;
                if (e < 0) f = 0;
                else if (e < 0.12) f = 1;
                else if (e < 0.24) f = 2;
                else if (e < 0.40) f = 3;
                else if (e < dur - 0.9) f = 4;
                else if (e < dur - 0.4) f = 5;
                else f = 0;
            }
            return arr[f];
        }

        // Тряска орудия при выстреле
        public static void ShakePostfix(BuildingTower __instance)
        {
            try
            {
                Building b = __instance.building;
                if (b == null || b.asset == null || !IsShooterId(b.asset.id)) return;
                FireEffects(b);
            }
            catch (System.Exception) { }
        }

        // Тряска, дым, звук и урон по воротам при выстреле (общее для автоматического и ручного огня)
        internal static void FireEffects(Building b, bool gates = true)
        {
            if (b.asset.id == MORTAR_ID) b.startShake(0.8f, SHAKE_POWER * 2.5f, SHAKE_POWER * 1.2f);
            else b.startShake(SHAKE_TIME, SHAKE_POWER, SHAKE_POWER * 0.5f);
            if (GUN_ANIM) gun_shot[b.getID()] = World.world.getCurWorldTime();
            MuzzleEffects(b);
            if (gates) GateShot(b);
            Sfx.Play(b.asset.id == CATAPULT_ID ? "shot_catapult" : (b.asset.id == MORTAR_ID ? "shot_mortar" : "shot_trebuchet"), b.current_tile);
            if (SHOT_LOG) WallBuilder.Dbg("siege gun fired: " + b.asset.id + " at " + b.current_tile.x + "," + b.current_tile.y);
        }

        // ---- ручное управление орудием (клавиши, см. ManualGun) ----
        private static float AIM_COMP = Cfg.Float("manual_gun_aim_comp", 1.1f, "поправка дальности ручной стрельбы: во сколько раз дальше цели летит снаряд (снаряды ложились на 3-5 клеток ближе)");
        internal static Building manual_gun;
        private static readonly Dictionary<long, double> manual_ready = new Dictionary<long, double>();
        // при ручной стрельбе мортира может бить ближе, чем в автоматическом режиме (там запрет защищает свой лагерь, а игрок сам выбирает точку)
        private static int MANUAL_MORTAR_MIN = Cfg.Int("manual_mortar_min_range", 8, "ближайшая точка, в которую можно целиться из мортиры вручную, клетки (взрыв большой: не цельтесь рядом с собой)");
        internal static float GunMinRange(Building b) { return b.asset.id == MORTAR_ID ? System.Math.Max(4, MANUAL_MORTAR_MIN) : GUN_MIN_RANGE; }
        // Сколько секунд прошло с последнего выстрела и сколько длится перезарядка (для анимации в 3D); false - орудие ещё не стреляло
        internal static bool GunClock(Building b, out double since, out double reload)
        {
            since = 999; reload = b.asset.id == MORTAR_ID ? MORTAR_RELOAD : CATAPULT_RELOAD;
            double shot;
            if (!gun_shot.TryGetValue(b.getID(), out shot)) return false;
            since = World.world.getCurWorldTime() - shot;
            double r; if (gun_reload.TryGetValue(b.getID(), out r) && r > 0.5) reload = r;
            return true;
        }
        // Дальность полёта снаряда в игре ограничена скоростью: v*v/g. Раньше у мортиры было 17 (около 29 клеток), и все дальние выстрелы падали на одном расстоянии.
        internal static float MortarSpeed(ProjectileAsset sp)
        {
            float v = 17f;
            try
            {
                float g = SimGlobals.m.gravity;
                float need = GUN_MAX_RANGE * MORTAR_RANGE_K * AIM_COMP * 1.15f;
                float mass = sp != null && sp.mass > 0.01f ? sp.mass : 1f;
                if (g > 0.01f) v = (float)System.Math.Sqrt(g * need) * mass;
                WallBuilder.Log("mortar shell speed " + v.ToString("F1") + " (gravity " + g.ToString("F2") + ", mass " + mass.ToString("F2") + ", reaches about " + (g > 0.01f ? ((v / mass) * (v / mass) / g).ToString("F0") : "?") + " tiles)");
            }
            catch (System.Exception e) { WallBuilder.Log("mortar speed: " + e.Message); }
            return v;
        }
        internal static void RefreshMortarSpeed()
        {
            try { ProjectileAsset sp = AssetManager.projectiles.get("siege_mortar_shell"); if (sp != null) sp.speed = MortarSpeed(sp); } catch (System.Exception) { }
        }
        // Куда смотрит орудие (для 3D-модели): последняя точка прицеливания игрока, иначе осаждаемый город
        internal static readonly Dictionary<long, UnityEngine.Vector3> gun_aim = new Dictionary<long, UnityEngine.Vector3>();     // x,y - точка, z - время (игровое)
        internal static bool GunFacing(Building b, out UnityEngine.Vector2 at)
        {
            at = UnityEngine.Vector2.zero;
            UnityEngine.Vector3 a;
            if (gun_aim.TryGetValue(b.getID(), out a) && (manual_gun == b || World.world.getCurWorldTime() - a.z < 25.0)) { at = new UnityEngine.Vector2(a.x, a.y); return true; }
            SiegeCamp gc = CampOf(b);
            if (gc != null && gc.target != null)
            {
                WorldTile t = gc.target.getTile();
                if (t != null) { at = new UnityEngine.Vector2(t.x, t.y); return true; }
            }
            return false;
        }
        internal static Projectile last_shell;
        internal static bool CityPoint(Building b, out UnityEngine.Vector2 at)
        {
            at = UnityEngine.Vector2.zero;
            SiegeCamp gc = CampOf(b);
            if (gc == null || gc.target == null) return false;
            WorldTile t = gc.target.getTile();
            if (t == null) return false;
            at = new UnityEngine.Vector2(t.x, t.y); return true;
        }
        internal static float GunMaxRange(Building b) { return b.asset.id == MORTAR_ID ? GUN_MAX_RANGE * MORTAR_RANGE_K : GUN_MAX_RANGE; }
        internal static float GunReload(Building b) { return b.asset.id == MORTAR_ID ? MORTAR_RELOAD : CATAPULT_RELOAD; }
        internal static double GunReadyIn(Building b)
        {
            double r;
            if (!manual_ready.TryGetValue(b.getID(), out r)) return 0;
            return System.Math.Max(0, r - World.world.getCurWorldTime());
        }
        // Выстрел по клетке. Дальность уже приведена к допустимой вызывающим.
        internal static bool ManualFire(Building b, WorldTile aim)
        {
            if (b == null || aim == null || b.current_tile == null || !b.isUsable()) return false;
            if (GunReadyIn(b) > 0) return false;
            double now = World.world.getCurWorldTime();
            float rl = GunReload(b);
            manual_ready[b.getID()] = now + rl;
            if (GUN_ANIM) gun_reload[b.getID()] = rl;
            UnityEngine.Vector3 launch = new UnityEngine.Vector3(b.current_tile.posV3.x, b.current_tile.posV3.y);
            launch.y += b.asset.tower_projectile_offset;
            UnityEngine.Vector3 tp = aim.posV3;
            if (AIM_COMP != 1f) { tp.x = launch.x + (tp.x - launch.x) * AIM_COMP; tp.y = (launch.y - b.asset.tower_projectile_offset) + (tp.y - (launch.y - b.asset.tower_projectile_offset)) * AIM_COMP; }
            tp.x += Randy.randomFloat(-0.8f, 0.8f); tp.y += Randy.randomFloat(-0.8f, 0.8f);
            last_shell = World.world.projectiles.spawn(b, null, b.asset.tower_projectile, launch, tp, 0f);
            gun_aim[b.getID()] = new UnityEngine.Vector3(aim.x, aim.y, (float)now);
            // ворота и стена получают урон, только если выстрел пришёлся рядом с воротами осаждаемого города
            bool nearGate = false;
            SiegeCamp gc = CampOf(b);
            WorldTile gateAt;
            if (gc != null && gc.target != null && WallBuilder.GateFocus(gc.target, b.current_tile, out gateAt) && gateAt != null
                && Toolbox.Dist(gateAt.x, gateAt.y, aim.x, aim.y) <= 7f) nearGate = true;
            FireEffects(b, nearGate);
            return true;
        }

        // Береговая башня выбирает вражеский корабль (с воинами в приоритете) в пределах своей дальности;
        // если кораблей нет, остаётся обычный выбор цели игры
        private static float COAST_RANGE = Cfg.Float("coast_tower_range", 40f, "дальность стрельбы береговых башен по кораблям, клетки (у обычных башен около 16-32)");

        private static void CoastTarget(Building b, ref BaseSimObject result)
        {
            WorldTile bt = b.current_tile;
            if (bt == null || b.kingdom == null) return;
            Actor best = null;
            float bestD = 0f;
            bool bestLoaded = false;
            foreach (Actor a in Finder.getUnitsFromChunk(bt, 3))
            {
                if (a == null || !a.isAlive() || a.asset == null || !a.asset.is_boat || a.current_tile == null) continue;
                float d = Toolbox.Dist(bt.x, bt.y, a.current_tile.x, a.current_tile.y);
                if (d > COAST_RANGE) continue;
                bool enemy = a.kingdom != null && a.kingdom.isEnemy(b.kingdom);
                bool loaded = false;
                Boat boat = a.getSimpleComponent<Boat>();
                if (boat != null)
                    foreach (Actor p in boat.getPassengers())
                        if (p != null && p.isAlive() && p.kingdom != null && p.kingdom.isEnemy(b.kingdom)) { enemy = true; loaded = true; break; }
                if (!enemy) continue;
                if (best == null || (loaded && !bestLoaded) || (loaded == bestLoaded && d < bestD)) { best = a; bestD = d; bestLoaded = loaded; }
            }
            if (best != null) result = best;
        }

        // Катапульта стреляет только по зданиям врага
        // Здание враждебно орудию: оно в осаждаемом городе или принадлежит вражескому королевству
        private static bool HostileTo(Building tb, Building gun, SiegeCamp gc)
        {
            if (gc != null && gc.target != null)
            {
                try { if (tb.city == gc.target) return true; } catch (System.Exception) { }
            }
            return tb.kingdom != null && gun.kingdom != null && tb.kingdom.isEnemy(gun.kingdom);
        }

        public static void CatapultTargetPostfix(BuildingTower __instance, ref BaseSimObject __result)
        {
            try
            {
                Building b = __instance.building;
                if (b != null && b.asset != null && Coast.IsCoastId(b.asset.id)) { CoastTarget(b, ref __result); return; }
                if (b == null || b.asset == null || !IsShooterId(b.asset.id)) return;
                if (manual_gun == b) { __result = null; return; }      // орудием управляет игрок
                float maxR = b.asset.id == MORTAR_ID ? GUN_MAX_RANGE * MORTAR_RANGE_K : GUN_MAX_RANGE;
                float minR = b.asset.id == MORTAR_ID ? System.Math.Max(GUN_MIN_RANGE, MORTAR_MIN_RANGE) : GUN_MIN_RANGE;
                BaseSimObject pick = null;
                int seen = 0, outside = 0, total_seen = 0, not_hostile = 0;
                // запертый город: целимся в здания у ворот, по которым идёт обстрел
                SiegeCamp gc = CampOf(b);
                WorldTile gateAt = null;
                if (gc != null && gc.target != null && WallBuilder.GateFocus(gc.target, b.current_tile, out gateAt) && gateAt != null)
                {
                    int gseen = 0;
                    foreach (Building tb in World.world.buildings.getBuildingFromZones(gateAt, 10))
                    {
                        if (tb == null || tb.asset == null || !tb.asset.city_building || !tb.isUsable() || tb.current_tile == null) continue;
                        if (!HostileTo(tb, b, gc)) continue;
                        float gd = Toolbox.Dist(b.current_tile.x, b.current_tile.y, tb.current_tile.x, tb.current_tile.y);
                        if (gd < minR || gd > maxR) continue;
                        if (Toolbox.Dist(gateAt.x, gateAt.y, tb.current_tile.x, tb.current_tile.y) > 10f) continue;
                        gseen++;
                        if (Randy.randomInt(0, gseen) == 0) pick = tb;
                    }
                    if (pick != null) { __result = pick; return; }
                }
                // здания перебираем по зонам карты: поиск врагов в игре останавливается на ближайшем кольце чанков и зданий мог не найти
                foreach (Building tb in World.world.buildings.getBuildingFromZones(b.current_tile, maxR))
                {
                    if (tb == null || tb.asset == null || !tb.asset.city_building || !tb.isUsable() || tb.current_tile == null) continue;
                    total_seen++;
                    if (!HostileTo(tb, b, gc)) { not_hostile++; continue; }
                    float d = Toolbox.Dist(b.current_tile.x, b.current_tile.y, tb.current_tile.x, tb.current_tile.y);
                    if (d < minR || d > maxR) { outside++; continue; }
                    seen++;
                    if (Randy.randomInt(0, seen) == 0) pick = tb;
                }
                // запасной путь: перебираем здания осаждаемого города напрямую (поиск по зонам может не пройти проверку острова)
                if (pick == null && gc != null && gc.target != null)
                {
                    int cseen = 0;
                    try
                    {
                        foreach (Building tb in gc.target.buildings)
                        {
                            if (tb == null || tb.asset == null || !tb.asset.city_building || !tb.isUsable() || tb.current_tile == null) continue;
                            total_seen++;
                            float d = Toolbox.Dist(b.current_tile.x, b.current_tile.y, tb.current_tile.x, tb.current_tile.y);
                            if (d < minR || d > maxR) { outside++; continue; }
                            cseen++;
                            if (Randy.randomInt(0, cseen) == 0) pick = tb;
                        }
                    }
                    catch (System.Exception) { }
                }
                // домов в зоне обстрела нет: бьём по баррикаде у ворот, иначе орудие молчит, хотя ворота и стены можно разбить
                if (pick == null && gc != null && gateAt != null)
                {
                    int bseen = 0;
                    foreach (Building tb in World.world.buildings.getBuildingFromZones(gateAt, 14))
                    {
                        if (tb == null || tb.asset == null || !tb.isUsable() || tb.current_tile == null) continue;
                        string tid = tb.asset.id;
                        if (tid != BARRICADE_ID && tid != BARRICADE_L_ID && tid != BARRICADE_R_ID) continue;
                        if (!HostileTo(tb, b, gc)) continue;
                        float bd = Toolbox.Dist(b.current_tile.x, b.current_tile.y, tb.current_tile.x, tb.current_tile.y);
                        if (bd < minR || bd > maxR) continue;
                        bseen++;
                        if (Randy.randomInt(0, bseen) == 0) pick = tb;
                    }
                }
                __result = pick;
                if (pick == null && SHOT_LOG)
                {
                    double now = World.world.getCurWorldTime();
                    if (now >= next_no_target_log)
                    {
                        next_no_target_log = now + 20.0;
                        WallBuilder.Dbg("siege gun at " + b.current_tile.x + "," + b.current_tile.y + ": no target in range (outside range: " + outside + ", buildings seen " + total_seen + ", not hostile " + not_hostile
                            + ", gun kingdom " + (b.kingdom != null ? b.kingdom.name : "none") + ", camp target " + (gc != null && gc.target != null ? gc.target.name : "none") + ")");
                    }
                }
            }
            catch (System.Exception) { }
        }

        // Случайная добавка к перезарядке орудия
        public static void ReloadPostfix(BuildingTower __instance)
        {
            try
            {
                Building b = __instance.building;
                if (b == null || b.asset == null || !IsShooterId(b.asset.id)) return;
                float t = (b.asset.id == MORTAR_ID ? MORTAR_RELOAD : CATAPULT_RELOAD) * (1f + Randy.randomFloat(0f, GUN_RELOAD_SPREAD));
                Traverse.Create(__instance).Field("_check_targets_timeout").SetValue(t);
                if (GUN_ANIM) gun_reload[b.getID()] = t;
            }
            catch (System.Exception) { }
        }

        // Клетка принадлежит палисаду действующего (или разбираемого) лагеря - это не бесхозная стена
        internal static bool IsCampTile(WorldTile t)
        {
            foreach (SiegeCamp c in camps.Values)
                for (int i = 0; i < c.changed.Count; i++) if (c.changed[i].tile == t) return true;
            for (int j = 0; j < dying.Count; j++)
                for (int i = 0; i < dying[j].changed.Count; i++) if (dying[j].changed[i].tile == t) return true;
            return false;
        }

        internal static SiegeCamp CampOf(Building gun)
        {
            foreach (SiegeCamp c in camps.Values) if (c.catapult == gun || c.extra_guns[0] == gun || c.extra_guns[1] == gun) return c;
            return null;
        }

        // Армия остаётся у города, пока орудия лагеря бьют по запертым воротам (но не дольше HOLD_SECONDS).
        // Без этого игра сама сбрасывает цель: за закрытыми воротами город «недостижим», и армия уходит домой.
        internal static bool HoldFor(City attacker, City target)
        {
            if (!HOLD_ENABLED || attacker == null || target == null) return false;
            SiegeInfo info;
            bool has_info = sieges.TryGetValue(attacker, out info) && info.target == target;
            SiegeCamp camp = null;
            if (!has_info || !camps.TryGetValue(attacker, out camp) || camp.catapult == null || !camp.catapult.isUsable())
            {
                // лагеря с орудием ещё нет (армия только подходит или лагерь строится): удерживаем по раннему правилу
                if (has_info && info.hold_done) return false;
                return EarlyHold(attacker, target);
            }
            if (info.hold_done) return false;
            if (!target.isAlive() || target.kingdom == null || attacker.kingdom == null || !target.kingdom.isEnemy(attacker.kingdom)) return false;
            if (!attacker.hasAnyWarriors() || !(WallBuilder.GatesClosed(target) || WallBuilder.SeparatedByWalls(attacker, target))) return false;
            double now = World.world.getCurWorldTime();
            if (info.hold_since < 0)
            {
                info.hold_since = now;
                WallBuilder.Dbg("siege hold: " + attacker.name + " keeps the army at " + target.name + " (max " + (int)HOLD_SECONDS + " s)");
            }
            if (now - info.hold_since > HOLD_SECONDS)
            {
                info.hold_done = true;
                WallBuilder.Dbg("siege hold over at " + target.name + " (timeout)");
                return false;
            }
            return true;
        }

        // Ранняя фаза: армия идёт к городу, ворота закрылись, и игра считает город недостижимым и сбрасывает цель.
        // Раз поход начат, пусть армия дойдёт до города (там появится лагерь).
        private class EarlyHoldInfo { public City target; public double since; public bool done; }
        private static readonly Dictionary<City, EarlyHoldInfo> early_hold = new Dictionary<City, EarlyHoldInfo>();
        private static float EARLY_HOLD_SECONDS = Cfg.Float("siege_early_hold_seconds", 420f, "сколько секунд армия идёт к закрытому городу, не меняя цель, пока не встал лагерь");

        private static bool EarlyHold(City attacker, City target)
        {
            if (!target.isAlive() || target.kingdom == null || attacker.kingdom == null || !target.kingdom.isEnemy(attacker.kingdom)) return false;
            if (!attacker.hasAnyWarriors()) return false;
            double now = World.world.getCurWorldTime();
            EarlyHoldInfo e;
            if (!(WallBuilder.GatesClosed(target) || WallBuilder.SeparatedByWalls(attacker, target))) { early_hold.Remove(attacker); return false; }
            if (!early_hold.TryGetValue(attacker, out e) || e.target != target)
            {
                e = new EarlyHoldInfo { target = target, since = now };
                early_hold[attacker] = e;
                WallBuilder.Dbg("early hold: " + attacker.name + " keeps heading for the closed " + target.name + " (max " + (int)EARLY_HOLD_SECONDS + " s)");
            }
            if (e.done) return false;
            if (now - e.since > EARLY_HOLD_SECONDS)
            {
                e.done = true;
                WallBuilder.Dbg("early hold over for " + attacker.name + " -> " + target.name + " (timeout, no camp)");
                return false;
            }
            return true;
        }

        internal static bool IsHolding(City attacker)
        {
            SiegeInfo info;
            if (attacker != null && sieges.TryGetValue(attacker, out info) && info.hold_since >= 0 && !info.hold_done) return true;
            EarlyHoldInfo e;
            return attacker != null && early_hold.TryGetValue(attacker, out e) && !e.done;
        }

        private static readonly Dictionary<City, double> turn_log = new Dictionary<City, double>();

        public class HoldState
        {
            public City target;
            public TileZone zone;
        }

        public static void HoldPrefix(City pCity, out HoldState __state)
        {
            __state = null;
            try
            {
                if (pCity != null && pCity.target_attack_city != null)
                {
                    __state = new HoldState();
                    __state.target = pCity.target_attack_city;
                    __state.zone = pCity.target_attack_zone;
                }
            }
            catch (System.Exception) { }
        }

        public static void HoldPostfix(City pCity, HoldState __state)
        {
            try
            {
                if (pCity == null) return;
                if (__state != null && pCity.target_attack_city == null)
                {
                    if (!HoldFor(pCity, __state.target))
                    {
                        double nw = World.world.getCurWorldTime(); double nl;
                        if (!turn_log.TryGetValue(pCity, out nl) || nw >= nl)
                        {
                            turn_log[pCity] = nw + 60.0;
                            City tg = __state.target;
                            WallBuilder.Dbg("army of " + pCity.name + " dropped its target " + (tg != null ? tg.name : "?") + ": alive " + (tg != null && tg.isAlive())
                                + ", enemy " + (tg != null && tg.kingdom != null && pCity.kingdom != null && tg.kingdom.isEnemy(pCity.kingdom))
                                + ", warriors " + pCity.hasAnyWarriors() + ", sealed " + WallBuilder.GatesSealed(tg) + ", siege " + sieges.ContainsKey(pCity)
                                + ", cut by walls " + WallBuilder.SeparatedByWalls(pCity, tg) + ", reachable " + (tg != null && tg.isAlive() && tg.reachableFrom(pCity)));
                        }
                    }
                    else
                    {
                        pCity.target_attack_city = __state.target;
                        pCity.target_attack_zone = __state.zone;
                    }
                }
                if (pCity.target_attack_city != null) FixZone(pCity);
            }
            catch (System.Exception) { }
        }

        // Игра выбирает целью атаки случайную зону с домом - у укреплённого города это зона внутри стен, которая при закрытых воротах
        // на другом "острове" (BehCityActorCheckAttack отказывается идти туда, и армия стоит дома). Берём ближайшую к атакующим зону города, до которой можно дойти
        private static float PREFER_FORT = Cfg.Float("siege_prefer_fortified", 0.3f, "во сколько раз укреплённые города кажутся армии ближе при выборе цели (1 - как в игре: ближайший город)");
        private class ZonePick { public City target; public TileZone zone; }
        private static readonly Dictionary<City, ZonePick> zone_pick = new Dictionary<City, ZonePick>();

        private static void FixZone(City pCity)
        {
            City t = pCity.target_attack_city;
            if (t == null || !WallBuilder.IsFortified(t)) return;
            WorldTile at = pCity.getTile();
            if (at == null) return;
            ZonePick zp;
            if (zone_pick.TryGetValue(pCity, out zp) && zp.target == t && zp.zone != null && zp.zone.city == t && zp.zone.centerTile != null && zp.zone.centerTile.isSameIsland(at))
            {
                pCity.target_attack_zone = zp.zone;
                return;
            }
            TileZone best = null; float bd = float.MaxValue;
            for (int i = 0; i < t.zones.Count; i++)
            {
                TileZone z = t.zones[i];
                if (z == null || z.centerTile == null || !z.centerTile.isSameIsland(at)) continue;
                float d = Toolbox.SquaredDistVec2Float(z.centerTile.pos, at.pos);
                if (d < bd) { bd = d; best = z; }
            }
            if (best == null) return;
            if (zp == null) { zp = new ZonePick(); zone_pick[pCity] = zp; }
            zp.target = t; zp.zone = best;
            pCity.target_attack_zone = best;
            WallBuilder.Dbg("army of " + pCity.name + " heads for the nearest reachable zone of " + t.name + " at " + best.centerTile.x + "," + best.centerTile.y);
        }

        // Выбор цели: игра берёт ближайший город в войне, до которого можно дойти. Из-за этого крупные армии бьют мелкие города без стен,
        // а укреплённые (недостижимые при закрытых воротах) пропускают. Укреплённым даём приоритет и считаем их достижимыми, если их отрезали только стены
        public static void FindTargetPostfix(City pOurCity, ref City __result)
        {
            try
            {
                if (pOurCity == null || pOurCity.kingdom == null) return;
                WorldTile at = pOurCity.getTile();
                if (at == null) return;
                float k2 = PREFER_FORT * PREFER_FORT;
                City best = null; float bs = float.MaxValue;
                int wc = 0, rc = 0, cut = 0;
                using (ListPool<City> pool = new ListPool<City>(World.world.cities.Count))
                {
                    World.world.wars.getWarCities(pOurCity.kingdom, pool);
                    for (int i = 0; i < pool.Count; i++)
                    {
                        City c = pool[i];
                        if (c == null || !c.isAlive() || c.kingdom == null) continue;
                        bool fort = WallBuilder.IsFortified(c);
                        wc++;
                        bool reach = c.reachableFrom(pOurCity);
                        if (reach) rc++;
                        if (!reach && !(fort && WallBuilder.OutsideTile(c, at) != null)) continue;
                        if (!reach) cut++;
                        float d = Toolbox.SquaredDistVec2Float(c.city_center, pOurCity.city_center);
                        float score = fort ? d * k2 : d;
                        if (score < bs) { bs = score; best = c; }
                    }
                }
                if (best != null)
                {
                    if (best != __result)
                    {
                        double nw = World.world.getCurWorldTime(); double nl;
                        if (!pick_log.TryGetValue(pOurCity, out nl) || nw >= nl)
                        {
                            pick_log[pOurCity] = nw + 90.0;
                            WallBuilder.Dbg("army of " + pOurCity.name + ": target " + best.name + (WallBuilder.IsFortified(best) ? " (fortified)" : "") + " instead of " + (__result != null ? __result.name + (WallBuilder.IsFortified(__result) ? " (fortified)" : "") : "none")
                                + ", war cities " + wc + ", reachable " + rc + ", cut off by walls only " + cut);
                        }
                    }
                    __result = best;
                }
            }
            catch (System.Exception) { }
        }
        private static readonly Dictionary<City, double> pick_log = new Dictionary<City, double>();

        // Выстрел орудия: если его цель - запертый город, ворота теряют прочность
        private static void GateShot(Building gun)
        {
            SiegeCamp camp = CampOf(gun);
            if (camp == null || camp.target == null || gun.current_tile == null) return;
            if (!WallBuilder.GatesSealed(camp.target)) return;
            bool mortar = gun.asset.id == MORTAR_ID;
            float dmg = mortar ? GATE_DMG_MORTAR : (gun.asset.id == TREBUCHET_ID ? GATE_DMG_TREBUCHET : GATE_DMG_CATAPULT);
            // мортира чаще бьёт по стене, а пролом у неё шире
            int share = mortar ? System.Math.Max(WALL_SHARE, 60) : WALL_SHARE;
            if (share > 0 && Randy.randomInt(0, 100) < share && WallBuilder.WallDamage(camp.target, gun.current_tile, dmg, mortar ? 3 : 2)) return;
            WallBuilder.GateDamage(camp.target, gun.current_tile, dmg);
        }

        internal static WorldTile CampCenterFor(City target)
        {
            foreach (SiegeCamp c in camps.Values) if (c.target == target && c.center != null) return c.center;
            return null;
        }

        internal static bool UnderSiege(City c)
        {
            foreach (SiegeInfo i in sieges.Values) if (i.target == c) return true;
            return false;
        }

        internal static string CountInfo()
        {
            return sieges.Count + "/" + camps.Count;
        }

        // Забываем осады и лагеря погибших городов
        internal static void CleanDead()
        {
            List<City> dead = new List<City>();
            foreach (City c in camps.Keys) if (c == null || !c.isAlive()) dead.Add(c);
            foreach (City c in dead) { SiegeCamp sc; if (c != null && camps.TryGetValue(c, out sc)) StartRemoval(sc, "attacker gone"); }
            dead.Clear();
            foreach (City c in sieges.Keys) if (c == null || !c.isAlive()) dead.Add(c);
            foreach (City c in dead) if (c != null) sieges.Remove(c);
            dead.Clear();
            foreach (City c in cooldown.Keys) if (c == null || !c.isAlive()) dead.Add(c);
            foreach (City c in approached.Keys) if (c == null || !c.isAlive()) dead.Add(c);
            foreach (City c in dead) if (c != null) { cooldown.Remove(c); approached.Remove(c); }
            dead.Clear();
            foreach (City c in fleet_seen.Keys) if (c == null || !c.isAlive()) dead.Add(c);
            foreach (City c in dead) if (c != null) fleet_seen.Remove(c);
            dead.Clear();
            foreach (City c in why_text.Keys) if (c == null || !c.isAlive()) dead.Add(c);
            foreach (City c in dead) if (c != null) { why_text.Remove(c); why_time.Remove(c); }
        }

        // Сколько бойцов армии сейчас рядом с точкой (а не просто числится в армии)
        private static int NearCount(Army army, WorldTile at, int radius)
        {
            WorldTile mass;
            List<WorldTile> crowd;
            return Gather(army, at, radius, out mass, out crowd);
        }

        // Бойцы армии вокруг точки: сколько их, где в среднем стоят и где именно.
        // Не считаем тех, кто ещё в лодке или на другом острове.
        internal static int Gather(Army army, WorldTile at, int radius, out WorldTile mass, out List<WorldTile> crowd)
        {
            mass = null;
            crowd = new List<WorldTile>();
            if (army == null || at == null) return 0;
            int n = 0;
            long sx = 0, sy = 0;
            float r2 = (float)radius * radius;
            List<Actor> list = army.units;
            // центр укреплённого города с закрытыми воротами - "другой остров" для тех, кто стоит у его стен: считаем и тех, кто на одной суше со стороной снаружи стен
            City owner = at.zone != null ? at.zone.city : null;
            WorldTile oref = null; bool oref_done = false;
            for (int i = 0; i < list.Count; i++)
            {
                Actor a = list[i];
                if (a == null || !a.isAlive() || a.current_tile == null || a.is_inside_boat) continue;
                float dx = a.current_tile.x - at.x, dy = a.current_tile.y - at.y;
                if (dx * dx + dy * dy > r2) continue;
                if (!a.current_tile.isSameIsland(at))
                {
                    if (!oref_done) { oref_done = true; if (owner != null) oref = WallBuilder.OutsideTile(owner, a.current_tile); }
                    if (oref == null || !a.current_tile.isSameIsland(oref)) continue;
                }
                n++; sx += a.current_tile.x; sy += a.current_tile.y;
                crowd.Add(a.current_tile);
            }
            if (n > 0) mass = World.world.GetTile((int)(sx / n), (int)(sy / n));
            return n;
        }

        // Армия с другого острова: запоминаем, что она была замечена далеко от города (значит, дошла пешком)
        private static void TrackApproach(City attacker, City target, Army army, WorldTile tc)
        {
            City seen;
            if (approached.TryGetValue(attacker, out seen) && seen == target) return;
            WorldTile mass;
            List<WorldTile> crowd;
            int n = Gather(army, tc, 130, out mass, out crowd);
            if (n < 3 || mass == null) return;
            if (Toolbox.Dist(mass.x, mass.y, tc.x, tc.y) >= CAMP_APPROACH_DIST) approached[attacker] = target;
        }

        // ---------- Жизненный цикл осады и лагеря ----------

        private static int AliveCount(SiegeCamp camp)
        {
            int alive = 0;
            for (int i = 0; i < camp.tents.Length; i++)
                if (camp.tents[i] != null && camp.tents[i].isUsable()) alive++;
            if (camp.catapult != null && camp.catapult.isUsable()) alive++;
            for (int i = 0; i < camp.extra_guns.Length; i++) if (camp.extra_guns[i] != null && camp.extra_guns[i].isUsable()) alive++;
            return alive;
        }

        public static void Tick(City city)
        {
            if (!CAMP_ENABLED || city == null) return;
            Sfx.Pump();
            Fx.Pump();
            ProcessDying();
            double now = World.world.getCurWorldTime();

            SiegeCamp camp;
            camps.TryGetValue(city, out camp);
            SiegeInfo info;
            sieges.TryGetValue(city, out info);
            City target = city.target_attack_city;
            Army army = city.army;
            if (target != null && army != null && target.isAlive() && target.kingdom != null && city.kingdom != null && target.kingdom.isEnemy(city.kingdom))
                Muster.Warn(target, city, army, now);

            if (info != null)
            {
                City t = info.target;
                if (t == null || !t.isAlive() || t.kingdom == null)
                {
                    EndSiege(city, camp, info, null, "target gone");
                    return;
                }
                if (t.kingdom == info.kingdom) { EndSiege(city, camp, info, LOG_FELL, "city captured"); return; }
                // город стёрт до нуля, но формально ещё существует (пустая территория): это не «выстоял»
                if (t.status.population <= 0) { EndSiege(city, camp, info, LOG_RUINED, "city emptied"); return; }
                if (!t.kingdom.isEnemy(info.kingdom)) { EndSiege(city, camp, info, LOG_LIFTED, "peace"); return; }

                Fx.Pennants(t, now);
                WorldTile at = camp != null ? camp.center : t.getTile();
                int near = NearCount(army, at, CAMP_ARMY_RADIUS);
                if (near < CAMP_KEEP_ARMY) near = System.Math.Max(near, NearCount(army, t.getTile(), CAMP_TRIGGER_DIST));     // бой идёт уже в городе, далеко от лагеря: армия на месте
                bool weak = near < CAMP_KEEP_ARMY || target != t;
                if (weak) { if (info.low_since < 0) info.low_since = now; }
                else info.low_since = -1;
                if (info.low_since >= 0 && now - info.low_since > CAMP_LOSE_SECONDS)
                {
                    EndSiege(city, camp, info, t.status.population <= 0 || t.buildings.Count == 0 ? LOG_RUINED : LOG_LIFTED, "army gone");
                    return;
                }

                if (camp != null)
                {
                    camp.ticks++;
                    Ram.Tick(camp, now);
                    Assault(camp, army, now);
                    if (now >= camp.next_side_check && camp.ticks > 4)
                    {
                        camp.next_side_check = now + 15.0;
                        WorldTile stc = t.getTile();
                        WorldTile smass = null; List<WorldTile> scrowd = null;
                        int sn = stc != null ? Gather(army, stc, 130, out smass, out scrowd) : 0;
                        if (stc != null && sn >= 6 && smass != null)
                        {
                            float ax = camp.center.x - stc.x, ay = camp.center.y - stc.y;
                            float bx = smass.x - stc.x, by = smass.y - stc.y;
                            float la = UnityEngine.Mathf.Sqrt(ax * ax + ay * ay), lb = UnityEngine.Mathf.Sqrt(bx * bx + by * by);
                            if (SHOT_LOG) WallBuilder.Dbg("camp side check " + city.name + ": camp " + camp.center.x + "," + camp.center.y + ", army " + sn + " around " + smass.x + "," + smass.y + ", target " + stc.x + "," + stc.y);
                            bool opposite = la > 1f && lb > 1f && (ax * bx + ay * by) / (la * lb) < -0.2f;      // угол больше примерно 100 градусов
                            if (opposite) camp.side_miss++; else camp.side_miss = 0;
                            if (camp.side_miss >= 3 && info.camps_made < CAMP_MAX_PER_SIEGE)
                            {
                                StartRemoval(camp, "army is on the other side of the city, moving the camp");
                                return;
                            }
                        }
                    }
                    int total_now = AliveTotal(army);
                    Build(camp, (near >= CAMP_VANGUARD_MIN && near * 5 >= total_now) ? System.Math.Max(near, total_now) : near);
                    if (RECALL_CIVILIANS && now >= camp.next_recall)
                    {
                        camp.next_recall = now + RECALL_SECONDS;
                        RecallCivilians(camp);
                    }
                    if (now >= camp.next_unstuck)
                    {
                        camp.next_unstuck = now + 3.0;
                        WallBuilder.UnstuckAround(camp.center, 1);
                    }

                    // лагерь разрушен и восстанавливать уже нечего - убираем, осада при этом продолжается
                    if (camp.placed_any && AliveCount(camp) == 0)
                    {
                        if (camp.empty_since < 0) camp.empty_since = now;
                        else if (now - camp.empty_since > CAMP_EMPTY_SECONDS) StartRemoval(camp, "camp destroyed");
                    }
                    else camp.empty_since = -1;
                    return;
                }
            }

            double until;
            if (cooldown.TryGetValue(city, out until) && now < until) return;
            if (target == null || !target.isAlive() || army == null) return;
            if (target.kingdom == null || !target.kingdom.isEnemy(city.kingdom)) return;
            if (info != null && (info.target != target || info.camps_made >= CAMP_MAX_PER_SIEGE)) { NoCamp(city, target, "camp limit for this siege reached or target changed"); return; }
            foreach (SiegeCamp other in camps.Values)
                if (other.target == target && other.kingdom == city.kingdom) return;     // у этого города уже стоит лагерь этого королевства

            WorldTile tc = target.getTile();
            WorldTile ac = city.getTile();
            if (tc == null || ac == null) return;
            bool sameIsland = tc.isSameIsland(ac) || WallBuilder.SeparatedByWalls(city, target);    // отрезан только стенами - это по-прежнему суша

            // города стоят вплотную - осада в виде лагеря выглядела бы странно
            if (sameIsland && CAMP_MIN_CITY_DIST > 0 && Toolbox.Dist(tc.x, tc.y, ac.x, ac.y) < CAMP_MIN_CITY_DIST) { NoCamp(city, target, "cities too close"); return; }
            if (!sameIsland)
            {
                if (CAMP_LANDING_MODE == 0) { NoCamp(city, target, "target on another island (landing mode off)"); return; }
                if (CAMP_LANDING_MODE == 1) TrackApproach(city, target, army, tc);
            }

            WorldTile mass;
            List<WorldTile> crowd;
            int size = Gather(army, tc, CAMP_TRIGGER_DIST, out mass, out crowd);
            // армия растянулась по дороге: если у города уже хотя бы пятая часть всех бойцов, лагерь считаем по всей армии
            int total = AliveTotal(army);
            if (size < CAMP_MIN_ARMY && size >= CAMP_VANGUARD_MIN && size * 5 >= total && total >= CAMP_MIN_ARMY)
            {
                WallBuilder.Dbg("siege camp: " + size + " of " + total + " fighters are at " + target.name + " already, sizing the camp by the whole army");
                size = total;
            }
            if (size < CAMP_MIN_ARMY)
            {
                if (army.units.Count >= CAMP_MIN_ARMY)
                {
                    WorldTile wm; List<WorldTile> wc;
                    int wide = Gather(army, tc, 200, out wm, out wc);
                    int d = wm == null ? -1 : (int)Toolbox.Dist(wm.x, wm.y, tc.x, tc.y);
                    NoCamp(city, target, "army near target too small (" + size + " within " + CAMP_TRIGGER_DIST + " < " + CAMP_MIN_ARMY + "; army " + army.units.Count + ", " + wide + " within 200, main group " + d + " tiles away)");
                }
                return;
            }

            if (!sameIsland && CAMP_LANDING_MODE == 1)
            {
                City seen;
                if (!approached.TryGetValue(city, out seen) || seen != target) { NoCamp(city, target, "landing right at the city, no camp"); return; }   // десант прямо у города: без лагеря
            }
            if (CAMP_NEED_WALLS && !WallBuilder.IsFortified(target)) { NoCamp(city, target, "target city has no fortifications"); return; }

            WorldTile site = FindSite(city, target, mass, crowd);
            if (site == null)
            {
                cooldown[city] = now + CAMP_RETRY_SECONDS;
                NoCamp(city, target, "no site found near " + tc.x + "," + tc.y + " (rejected: other island " + site_rej[0] + ", too close to walls " + site_rej[1] + ", crowded " + site_rej[2] + ", water/rock " + site_rej[3] + ", buildings " + site_rej[4] + ", foreign land core " + site_rej[5] + ", foreign land edge " + site_rej[6] + ")");
                return;
            }

            camp = new SiegeCamp();
            camp.attacker = city;
            camp.target = target;
            camp.kingdom = city.kingdom;
            camp.center = site;
            camp.beach = false;     // плацдарм - отдельная механика (BeachTick), обычный лагерь после высадки остаётся лагерем
            PlanCamp(camp);
            camps[city] = camp;

            if (info == null)
            {
                info = new SiegeInfo();
                info.target = target;
                info.kingdom = city.kingdom;
                sieges[city] = info;
                Announce(LOG_START, target, city.kingdom, site);
                Scroll.Begin(city, target, size, site);
                Sfx.Play("alarm", target.getTile());      // в осаждённом городе бьют в колокол (ворота закрываются тогда же)
            }
            info.camps_made++;
            info.low_since = -1;
            WallBuilder.Dbg("siege camp planned at " + site.x + "," + site.y + " for " + city.name + " -> " + target.name + " (target center " + tc.x + "," + tc.y + ", army " + size + " around " + (mass != null ? mass.x + "," + mass.y : "?") + ", pave " + camp.pave.Count + ")");
        }

        // Почему лагерь не строится: пишем причину не чаще раза в 2 минуты на город (и только когда она сменилась)
        private static bool CAMP_WHY_LOG = Cfg.Bool("camp_why_log", true, "писать в лог причину, почему осадный лагерь не построен");
        private static readonly Dictionary<City, string> why_text = new Dictionary<City, string>();
        private static readonly Dictionary<City, double> why_time = new Dictionary<City, double>();
        private static void NoCamp(City city, City target, string why)
        {
            if (!CAMP_WHY_LOG) return;
            double now = World.world.getCurWorldTime();
            string last; double t;
            double gap = why.StartsWith("target city has no") ? 600.0 : 240.0;
            int par = why.IndexOf(" (");
            string key = (par > 0 ? why.Substring(0, par) : why) + ">" + (target != null ? target.name : "");
            if (why_text.TryGetValue(city, out last) && last == key && why_time.TryGetValue(city, out t) && now - t < gap) return;
            why_text[city] = key;
            why_time[city] = now;
            WallBuilder.Dbg("no siege camp " + city.name + " -> " + target.name + ": " + why);
        }

        // Конец осады: убираем лагерь и, если нужно, сообщаем в новостях
        private static void EndSiege(City attacker, SiegeCamp camp, SiegeInfo info, string news, string why)
        {
            if (camp != null) StartRemoval(camp, why);
            else WallBuilder.Dbg("siege ended (" + why + ")");
            sieges.Remove(attacker);
            approached.Remove(attacker);
            cooldown[attacker] = World.world.getCurWorldTime() + CAMP_RETRY_SECONDS;
            if (info != null && info.target != null) Scroll.End(attacker, info.target, news);
            if (news != null && info.target != null) Announce(news, info.target, info.kingdom, info.target.getTile());
        }

        private static void StartRemoval(SiegeCamp camp, string why)
        {
            WallBuilder.Dbg("siege camp removed (" + why + ")");
            Ram.Remove(camp);
            for (int i = 0; i < camp.tents.Length; i++)
            {
                Building b = camp.tents[i];
                if (b != null && b.isAlive()) b.startDestroyBuilding();
                camp.tents[i] = null;
            }
            if (camp.catapult != null && camp.catapult.isAlive()) camp.catapult.startDestroyBuilding();
            camp.catapult = null;
            for (int i = 0; i < camp.extra_guns.Length; i++)
            {
                if (camp.extra_guns[i] != null && camp.extra_guns[i].isAlive()) camp.extra_guns[i].startDestroyBuilding();
                camp.extra_guns[i] = null;
            }
            if (camp.fire != null && camp.fire.isAlive()) camp.fire.startDestroyBuilding();
            camp.fire = null;
            if (camp.banner != null && camp.banner.isAlive()) camp.banner.startDestroyBuilding();
            camp.banner = null;
            if (camp.tower != null && camp.tower.isAlive()) camp.tower.startDestroyBuilding();
            camp.tower = null;
            for (int i = 0; i < camp.props.Length; i++)
            {
                if (camp.props[i] != null && camp.props[i].isAlive()) camp.props[i].startDestroyBuilding();
                camp.props[i] = null;
            }
            for (int i = 0; i < camp.pal_b.Count; i++) if (camp.pal_b[i] != null && camp.pal_b[i].isAlive()) camp.pal_b[i].startDestroyBuilding();
            camp.pal_b.Clear();
            camps.Remove(camp.attacker);
            cooldown[camp.attacker] = World.world.getCurWorldTime() + CAMP_RETRY_SECONDS;
            dying.Add(camp);
        }

        // Палатки и катапульты, не принадлежащие ни одному живому лагерю (после захвата города они
        // становятся заброшенными и иначе стояли бы вечно), сносим
        private static void SweepOrphans()
        {
            try
            {
                HashSet<Building> active = new HashSet<Building>();
                Fx.AddActive(active);
                foreach (SiegeCamp c in camps.Values)
                {
                    for (int i = 0; i < c.tents.Length; i++) if (c.tents[i] != null) active.Add(c.tents[i]);
                    if (c.catapult != null) active.Add(c.catapult);
                    for (int i = 0; i < c.extra_guns.Length; i++) if (c.extra_guns[i] != null) active.Add(c.extra_guns[i]);
                    if (c.fire != null) active.Add(c.fire);
                    if (c.banner != null) active.Add(c.banner);
                    if (c.tower != null) active.Add(c.tower);
                    for (int i = 0; i < c.props.Length; i++) if (c.props[i] != null) active.Add(c.props[i]);
                    for (int i = 0; i < c.pal_b.Count; i++) if (c.pal_b[i] != null) active.Add(c.pal_b[i]);
                }
                for (int bi = 0; bi < beaches.Count; bi++)
                    for (int k = 0; k < beaches[bi].blds.Count; k++) if (beaches[bi].blds[k] != null) active.Add(beaches[bi].blds[k]);
                WallBuilder.CollectDecor(active);
                List<Building> all = World.world.buildings.getSimpleList();
                int removed = 0;
                for (int i = 0; i < all.Count; i++)
                {
                    Building b = all[i];
                    if (b == null || b.asset == null || !b.isAlive()) continue;
                    if (!IsSiegeId(b.asset.id)) continue;
                    if (active.Contains(b)) continue;
                    b.startDestroyBuilding();
                    removed++;
                }
                if (removed > 0) WallBuilder.Dbg("siege: leftover camp buildings removed: " + removed);
            }
            catch (System.Exception e) { WallBuilder.Log("sweep failed: " + e.Message); }
        }

        // Постепенно возвращаем землю под лагерем; заодно убираем лагеря, чьи города исчезли
        private static void ProcessDying()
        {
            int frame = UnityEngine.Time.frameCount;
            if (frame == last_dying_frame) return;
            last_dying_frame = frame;

            if (frame - last_sweep_frame > 1800) { last_sweep_frame = frame; SweepOrphans(); }

            if (camps.Count > 0)
            {
                List<SiegeCamp> orphans = null;
                foreach (SiegeCamp c in camps.Values)
                {
                    if (c.attacker == null || !c.attacker.isAlive())
                    {
                        if (orphans == null) orphans = new List<SiegeCamp>();
                        orphans.Add(c);
                    }
                }
                if (orphans != null)
                    foreach (SiegeCamp c in orphans) StartRemoval(c, "attacker city gone");
            }

            for (int i = dying.Count - 1; i >= 0; i--)
            {
                SiegeCamp c = dying[i];
                int n = 0;
                while (c.changed.Count > 0 && n < DISMANTLE_PER_TICK)
                {
                    TileChange ch = c.changed[c.changed.Count - 1];
                    c.changed.RemoveAt(c.changed.Count - 1);
                    if (ch.tile.top_type == ch.placed)
                    {
                        MapAction.terraformTop(ch.tile, ch.prev);
                        n++;
                    }
                }
                if (c.changed.Count == 0) dying.RemoveAt(i);
            }
        }

        // ---------- Выбор места ----------

        // Место лагеря: со стороны, где стоят основные силы, не ближе безопасной дистанции от города
        private static WorldTile FindSite(City attacker, City target, WorldTile mass, List<WorldTile> crowd)
        {
            System.Array.Clear(site_rej, 0, site_rej.Length);
            WorldTile tc = target.getTile();
            WorldTile ac = attacker.getTile();
            if (tc == null || ac == null) return null;
            WorldTile dirTile = mass != null ? mass : ac;
            site_ref = tc.isSameIsland(dirTile) ? null : WallBuilder.OutsideTile(target, dirTile);      // стены отрезали центр: площадку ищем на суше снаружи
            float vx = dirTile.x - tc.x, vy = dirTile.y - tc.y;
            float len = UnityEngine.Mathf.Sqrt(vx * vx + vy * vy);
            if (len < 1f)
            {
                vx = ac.x - tc.x; vy = ac.y - tc.y;
                len = UnityEngine.Mathf.Sqrt(vx * vx + vy * vy);
                if (len < 1f) return null;
            }
            float massDist = len;
            vx /= len; vy /= len;

            float[] angles = new float[] { 0f, 25f, -25f, 50f, -50f, 80f, -80f, 110f, -110f };
            int minDist = System.Math.Max(SITE_MIN_DIST, WallBuilder.OuterRadius(target) + PALISADE_RADIUS + 8);
            int maxDist = minDist + (SITE_MAX_DIST - SITE_MIN_DIST);
            int d0 = System.Math.Min(System.Math.Max((int)massDist, minDist), maxDist);
            d0 = System.Math.Min(d0, minDist + 10);      // бойцы могут быть ещё далеко: лагерь ставим у стен, а не там, где они сейчас идут

            // начинаем с расстояния, на котором стоят бойцы, и расходимся в обе стороны
            for (int k = 0; k <= (maxDist - minDist) / SITE_STEP + 1; k++)
            {
                for (int sign = 1; sign >= -1; sign -= 2)
                {
                    if (k == 0 && sign == -1) continue;
                    int dist = d0 + sign * k * SITE_STEP;
                    if (dist < minDist || dist > maxDist) continue;
                    for (int i = 0; i < angles.Length; i++)
                    {
                        float rad = angles[i] * UnityEngine.Mathf.Deg2Rad;
                        float cs = UnityEngine.Mathf.Cos(rad), sn = UnityEngine.Mathf.Sin(rad);
                        float rx = vx * cs - vy * sn, ry = vx * sn + vy * cs;
                        WorldTile t = World.world.GetTile(tc.x + (int)System.Math.Round(rx * dist), tc.y + (int)System.Math.Round(ry * dist));
                        if (SiteOk(t, attacker, target, tc, crowd)) return t;
                    }
                }
            }
            return null;
        }

        // Клетка чужая для лагеря: земля третьего королевства или другого города осаждаемого (не самой цели)
        private static bool ZoneBlocked(WorldTile n, Kingdom a, Kingdom b, City target)
        {
            City zc = (n != null && n.zone != null) ? n.zone.city : null;
            if (zc == null || zc.kingdom == a) return false;
            if (zc.kingdom == b) return target != null && zc != target;
            return true;
        }

        private static readonly int[] site_rej = new int[7];
        private static WorldTile site_ref;
        private static bool SiteOk(WorldTile t, City attacker, City target, WorldTile targetTile, List<WorldTile> crowd)
        {
            if (t == null || (!t.isSameIsland(targetTile) && (site_ref == null || !t.isSameIsland(site_ref)))) { site_rej[0]++; return false; }
            // стены квадратные: по диагонали расстояние по прямой обманывает, поэтому считаем по большей из осей
            if (System.Math.Max(System.Math.Abs(t.x - targetTile.x), System.Math.Abs(t.y - targetTile.y)) < WallBuilder.OuterRadius(target) + PALISADE_RADIUS + 6) { site_rej[1]++; return false; }
            if (crowd != null)
            {
                int near = 0;
                for (int i = 0; i < crowd.Count; i++)
                {
                    WorldTile w = crowd[i];
                    if (System.Math.Abs(w.x - t.x) <= CROWD_RADIUS && System.Math.Abs(w.y - t.y) <= CROWD_RADIUS && ++near > CROWD_MAX) { site_rej[2]++; return false; }
                }
            }
            int civ_hits = 0;
            int[] core = new int[] { 0, 0, 8, 0, -8, 0, 0, 8, 0, -8, 8, 8, -8, 8, 8, -8, -8, -8 };
            for (int i = 0; i < core.Length; i += 2)
            {
                WorldTile n = World.world.GetTile(t.x + core[i], t.y + core[i + 1]);
                if (n == null || n.is_liquid || n.Type.block || n.Type.wall) { site_rej[3]++; return false; }
                if (WallBuilder.IsCiv(n) && ++civ_hits > 3) { site_rej[4]++; return false; }
                if (ZoneBlocked(n, attacker.kingdom, target.kingdom, target)) { site_rej[5]++; return false; }
            }
            int R = PALISADE_RADIUS;
            int[] ring = new int[] { R, 0, -R, 0, 0, R, 0, -R, R, R, -R, R, R, -R, -R, -R };
            for (int i = 0; i < ring.Length; i += 2)
            {
                WorldTile n = World.world.GetTile(t.x + ring[i], t.y + ring[i + 1]);
                if (n == null) { site_rej[6]++; return false; }
                if (ZoneBlocked(n, attacker.kingdom, target.kingdom, target)) { site_rej[6]++; return false; }
            }
            return true;
        }

        // ---------- План и строительство ----------

        private static void PlanCamp(SiegeCamp camp)
        {
            WorldTile c = camp.center;
            for (int d = 0; d <= PAVE_RADIUS; d++)
            {
                for (int dx = -d; dx <= d; dx++)
                {
                    for (int dy = -d; dy <= d; dy++)
                    {
                        if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != d) continue;
                        if (d > 1)
                        {
                            // неровное мощение: пятна и проплешины вместо ровного узора, к краю всё реже
                            int h = (dx * 73856093) ^ (dy * 19349663) ^ (c.x * 83492791) ^ (c.y * 2654435);
                            h = (h & 0x7fffffff) % 100;
                            int chance = d >= PAVE_RADIUS - 1 ? 38 : 66;
                            if (h >= chance) continue;
                        }
                        WorldTile t = World.world.GetTile(c.x + dx, c.y + dy);
                        if (t != null) camp.pave.Add(t);
                    }
                }
            }
            camp.pave_total = camp.pave.Count;

            int R = PALISADE_RADIUS;
            // частокол только со стороны осаждаемого города
            WorldTile tt = camp.target != null ? camp.target.getTile() : null;
            float fx = 0f, fy = 0f;
            if (tt != null)
            {
                fx = tt.x - c.x; fy = tt.y - c.y;
                float fl = UnityEngine.Mathf.Sqrt(fx * fx + fy * fy);
                if (fl > 0.5f) { fx /= fl; fy /= fl; }
            }
            for (int dx = -R; dx <= R; dx++)
            {
                for (int dy = -R; dy <= R; dy++)
                {
                    if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != R) continue;
                    if (dx * fx + dy * fy < R * 0.35f) continue;
                    int s = (System.Math.Abs(dy) == R) ? dx : dy;      // положение вдоль стороны
                    bool corner = System.Math.Abs(dx) == R && System.Math.Abs(dy) == R;
                    if (System.Math.Abs(s) <= PALISADE_GAP && !corner) continue;
                    if (!corner && (((s % 3) + 3) % 3) == 0) continue;      // колья: два через один, а не сплошная стена
                    WorldTile t = World.world.GetTile(c.x + dx, c.y + dy);
                    if (t != null) { camp.palisade.Add(t); if (!corner && System.Math.Abs(dx) == R) camp.pal_v.Add(t); }
                }
            }
        }

        private static bool Usable(WorldTile t, SiegeCamp camp)
        {
            if (t == null || t.is_liquid || t.Type.block || t.Type.wall || t.Type.id == "field") return false;
            return !ZoneBlocked(t, camp.kingdom, camp.target != null ? camp.target.kingdom : null, camp.target);
        }

        // Клетка свободна для строения; мелкую природу на ней сносим и пробуем в следующий раз
        // Рядом с клеткой есть настоящее здание (башня, дом): баррикаду туда не ставим, чтобы не снести его
        internal static bool TowerNear(WorldTile t, int r)
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    WorldTile n = World.world.GetTile(t.x + dx, t.y + dy);
                    if (n != null && n.hasBuilding() && n.building.asset != null && n.building.asset.id.StartsWith("watch_tower")) return true;
                }
            return false;
        }

        internal static bool BuildingNear(WorldTile t, int r)
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    WorldTile n = World.world.GetTile(t.x + dx, t.y + dy);
                    if (n != null && n.hasBuilding() && !WallBuilder.Clearable(n.building) && !IsSiegeId(n.building.asset.id)) return true;
                }
            return false;
        }

        internal static WorldTile FreeSpot(WorldTile t, int radius)
        {
            if (t == null) return null;
            for (int d = 0; d <= radius; d++)
            {
                for (int dx = -d; dx <= d; dx++)
                {
                    for (int dy = -d; dy <= d; dy++)
                    {
                        if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != d) continue;
                        WorldTile n = World.world.GetTile(t.x + dx, t.y + dy);
                        if (n == null || n.is_liquid || n.Type.block || n.Type.wall) continue;
                        if (n.hasBuilding())
                        {
                            if (WallBuilder.Clearable(n.building)) n.building.startDestroyBuilding();
                            continue;
                        }
                        if (n.hasUnits()) continue;
                        return n;
                    }
                }
            }
            return null;
        }

        private static int put_blocked_logged;
        internal static Building Put(string id, WorldTile t, Kingdom k)
        {
            BuildingAsset a = AssetManager.buildings.get(id);
            if (a == null || t == null) return null;
            // здание без королевства ломает WildKingdomsManager.updateDirtyBuildings (NRE каждый кадр), поэтому такое не создаём
            if (k == null || !k.isAlive())
            {
                WallBuilder.Dbg("Put skipped (" + id + "): kingdom is " + (k == null ? "null" : "dead"));
                return null;
            }
            // игра сносит любое здание на клетках площади нового (у копий башни она 3x2), поэтому поверх чужих построек не ставим
            try
            {
                BuildingFundament fd = a.fundament;
                if (fd != null)
                    for (int fx = -fd.left; fx <= fd.right; fx++)
                        for (int fy = -fd.bottom; fy <= fd.top; fy++)
                        {
                            WorldTile ft = World.world.GetTile(t.x + fx, t.y + fy);
                            if (ft == null || !ft.hasBuilding()) continue;
                            Building ob = ft.building;
                            if (ob.asset == null || WallBuilder.Clearable(ob) || IsSiegeId(ob.asset.id)) continue;
                            if (put_blocked_logged++ < 40) WallBuilder.Dbg("Put skipped (" + id + ") at " + t.x + "," + t.y + ": " + ob.asset.id + " is in the way");
                            return null;
                        }
            }
            catch (System.Exception) { }
            Building b = World.world.buildings.addBuilding(a, t);
            if (b == null) return null;
            b.setKingdom(k);
            if (b.kingdom == null)
            {
                WallBuilder.Dbg("Put: building " + id + " ended up without a kingdom, removing it");
                try { b.startDestroyBuilding(); } catch (System.Exception) { }
                return null;
            }
            return b;
        }

        // Штурм: пока ворота заперты, большая часть армии идёт к ним и давит на них (набирается давление на ворота, см. WallBuilder.HandleGates)
        private static bool ASSAULT_ON = Cfg.Bool("siege_assault", true, "осаждающие идут к запертым воротам и штурмуют их");
        private static int ASSAULT_PCT = Cfg.Int("siege_assault_percent", 70, "какая часть армии идёт к воротам");

        private static void Assault(SiegeCamp camp, Army army, double now)
        {
            if (!ASSAULT_ON || army == null || camp == null || camp.target == null || now < camp.next_assault) return;
            camp.next_assault = now + 4.0;
            try
            {
                if (!WallBuilder.GatesClosed(camp.target)) return;
                WorldTile gate, stand;
                WorldTile from = camp.center;
                if (!WallBuilder.GateApproach(camp.target, from, out gate, out stand) || stand == null) return;
                WorldTile tc = camp.target.getTile();
                int sent = 0;
                List<Actor> list = army.units;
                for (int i = 0; i < list.Count && sent < 50; i++)
                {
                    Actor a = list[i];
                    if (a == null || !a.isAlive() || !a.isWarrior() || a.current_tile == null || a.is_inside_boat || a.isFighting()) continue;
                    if (!a.current_tile.isSameIsland(stand)) continue;
                    if (Toolbox.Dist(a.current_tile.x, a.current_tile.y, stand.x, stand.y) > 110f) continue;
                    int h = (int)(((a.getID() * 2654435761L) >> 9) & 0x7fffffff);
                    if (h % 100 >= ASSAULT_PCT) continue;
                    if (Toolbox.Dist(a.current_tile.x, a.current_tile.y, stand.x, stand.y) <= 4f) continue;
                    WorldTile dest = World.world.GetTile(stand.x + Randy.randomInt(-3, 4), stand.y + Randy.randomInt(-3, 4));
                    if (dest == null || dest.Type.block || dest.is_liquid || dest.Type.wall || !dest.isSameIsland(a.current_tile)) continue;
                    a.cancelAllBeh();
                    a.goTo(dest);
                    sent++;
                }
                if (sent > 0) Scroll.Note(camp.target, "assault");
                if (sent > 0 && now >= camp.next_assault_log)
                {
                    camp.next_assault_log = now + 30.0;
                    WallBuilder.Dbg("assault: " + sent + " fighters of " + camp.attacker.name + " sent to the gate of " + camp.target.name + " at " + stand.x + "," + stand.y);
                }
            }
            catch (System.Exception) { }
        }

        private const int CAMP_VANGUARD_MIN = 5;
        private static int AliveTotal(Army army)
        {
            if (army == null) return 0;
            int n = 0;
            List<Actor> l = army.units;
            for (int i = 0; i < l.Count; i++) if (l[i] != null && l[i].isAlive()) n++;
            return n;
        }

        private static void Build(SiegeCamp camp, int size)
        {
            Pave(camp);
            Palisade(camp);
            PlaceStructures(camp, size);
        }

        private static void Pave(SiegeCamp camp)
        {
            int placed = 0;
            while (camp.pave.Count > 0 && placed < PAVE_PER_TICK)
            {
                WorldTile t = camp.pave[0];
                camp.pave.RemoveAt(0);
                if (!Usable(t, camp) || t.Type.road) continue;
                if (t.hasBuilding())
                {
                    if (WallBuilder.IsCiv(t)) continue;
                    if (WallBuilder.Clearable(t.building)) t.building.startDestroyBuilding();
                    else continue;
                }
                TileChange ch = new TileChange();
                ch.tile = t; ch.prev = t.top_type; ch.placed = TopTileLibrary.road;
                camp.changed.Add(ch);
                MapAction.terraformTop(t, TopTileLibrary.road);
                placed++;
            }
        }

        private static void Palisade(SiegeCamp camp)
        {
            int placed = 0;
            while (camp.palisade.Count > 0 && placed < PALISADE_PER_TICK)
            {
                WorldTile t = camp.palisade[0];
                camp.palisade.RemoveAt(0);
                if (!Usable(t, camp) || t.hasUnits()) continue;
                if (t.hasBuilding())
                {
                    if (WallBuilder.IsCiv(t)) continue;
                    if (WallBuilder.Clearable(t.building)) t.building.startDestroyBuilding();
                    else continue;
                }
                TileChange ch = new TileChange();
                ch.tile = t; ch.prev = t.top_type; ch.placed = TopTileLibrary.wall_wild;
                camp.changed.Add(ch);
                MapAction.terraformTop(t, TopTileLibrary.wall_wild);
                placed++;
            }
        }

        // Есть ли в лагере воин осаждённого города
        private static bool DefenderNear(SiegeCamp camp, ref int cache)
        {
            if (cache >= 0) return cache == 1;
            cache = 0;
            foreach (Actor a in Finder.getUnitsFromChunk(camp.center, 1))
            {
                if (a == null || !a.isAlive() || a.current_tile == null || a.kingdom == null) continue;
                if (!a.kingdom.isEnemy(camp.kingdom) || !a.isWarrior()) continue;
                int dx = System.Math.Abs(a.current_tile.x - camp.center.x), dy = System.Math.Abs(a.current_tile.y - camp.center.y);
                if (System.Math.Max(dx, dy) <= PALISADE_RADIUS + 2) { cache = 1; return true; }
            }
            return false;
        }

        private static void PlaceStructures(SiegeCamp camp, int size)
        {
            if (camp.build_cooldown > 0) { camp.build_cooldown--; return; }
            if (camp.built_total >= CAMP_MAX_BUILDS) return;

            // костёр в центре лагеря (один раз)
            if (!camp.fire_done)
            {
                camp.fire_done = true;
                WorldTile ft = FreeSpot(World.world.GetTile(camp.center.x, camp.center.y - 4), 2);
                if (ft != null) camp.fire = Put(FIRE_ID, ft, camp.kingdom);
            }
            // знамя осаждающих (цвета их королевства) рядом с костром
            if (!camp.banner_done && !camp.beach)
            {
                camp.banner_done = true;
                WorldTile bt = FreeSpot(World.world.GetTile(camp.center.x - 3, camp.center.y - 3), 2);
                if (bt != null) camp.banner = Put(BANNER_ID, bt, camp.kingdom);
            }

            int wantTents = System.Math.Min(System.Math.Min(CAMP_MAX_TENTS, 6), 1 + System.Math.Max(0, size - CAMP_MIN_ARMY) / CAMP_TENT_STEP);
            if (camp.beach) wantTents = System.Math.Min(wantTents, 3);
            bool wantShooter = size >= CATAPULT_MIN_ARMY && !camp.beach;     // орудия на плацдарм не затаскивают

            int defender = -1;     // -1 не проверяли, 0 нет, 1 есть защитник в лагере (рассчитываем лениво)
            if (wantShooter && (camp.catapult == null || !camp.catapult.isUsable()) && camp.shooter_builds < 1 + REBUILDS
                && !(camp.shooter_builds > 0 && DefenderNear(camp, ref defender)))
            {
                if (camp.shooter_id == null && Traits.AllowsKingdom(camp.kingdom, Traits.MORTAR, false))
                {
                    camp.shooter_id = MORTAR_ID;      // культура с чертой «Мортира»: мортира всегда ставится первой, одна, дальше обычные орудия
                }
                if (camp.shooter_id == null)
                {
                    // орудия чередуются от лагеря к лагерю (первое выбираем случайно), чтобы видеть оба
                    camp.shooter_id = last_shooter == null ? (Randy.randomInt(0, 2) == 0 ? CATAPULT_ID : TREBUCHET_ID)
                        : (last_shooter == CATAPULT_ID ? TREBUCHET_ID : CATAPULT_ID);
                    last_shooter = camp.shooter_id;
                }
                WorldTile t = FreeSpot(camp.center, 1);
                if (t != null)
                {
                    Building b = Put(camp.shooter_id, t, camp.kingdom);
                    if (b != null)
                    {
                        camp.catapult = b;
                        camp.shooter_builds++;
                        camp.built_total++;
                        camp.placed_any = true;
                        camp.build_cooldown = BUILD_COOLDOWN;
                        WallBuilder.Dbg("siege " + (camp.shooter_id == TREBUCHET_ID ? "trebuchet" : (camp.shooter_id == MORTAR_ID ? "mortar" : "catapult")) + " placed at " + t.x + "," + t.y + " (army " + size + ")");
                        return;
                    }
                }
            }

            // второе и третье орудие для больших армий (тип чередуется с первым)
            if (wantShooter && camp.catapult != null && camp.catapult.isUsable())
            {
                int[] need = { GUN2_ARMY, GUN3_ARMY };
                int[] dx = { -5, 4 };
                int[] dy = { 2, 4 };
                for (int k = 0; k < 2; k++)
                {
                    if (need[k] <= 0 || size < need[k]) break;
                    if (camp.extra_guns[k] != null && camp.extra_guns[k].isUsable()) continue;
                    if (camp.extra_builds[k] >= 1 + REBUILDS) continue;
                    if (camp.extra_builds[k] > 0 && DefenderNear(camp, ref defender)) continue;
                    string gid = camp.shooter_id == MORTAR_ID ? (k == 0 ? CATAPULT_ID : TREBUCHET_ID) : ((k == 0) == (camp.shooter_id == CATAPULT_ID) ? TREBUCHET_ID : CATAPULT_ID);
                    WorldTile gt = FreeSpot(World.world.GetTile(camp.center.x + dx[k], camp.center.y + dy[k]), 2);
                    if (gt == null) continue;
                    Building gb = Put(gid, gt, camp.kingdom);
                    if (gb == null) continue;
                    camp.extra_guns[k] = gb;
                    camp.extra_builds[k]++;
                    camp.built_total++;
                    camp.build_cooldown = BUILD_COOLDOWN;
                    WallBuilder.Dbg("siege extra gun " + gid + " placed at " + gt.x + "," + gt.y + " (army " + size + ")");
                    return;
                }
            }

            // охранная башня рядом с орудием (большая армия)
            if (TOWER_MIN_ARMY > 0 && !camp.beach && size >= TOWER_MIN_ARMY && (camp.tower == null || !camp.tower.isUsable())
                && camp.tower_builds < 1 + REBUILDS && !(camp.tower_builds > 0 && DefenderNear(camp, ref defender)))
            {
                WorldTile tt2 = FreeSpot(World.world.GetTile(camp.center.x + 4, camp.center.y - 1), 2);
                if (tt2 != null)
                {
                    Building b = Put(TOWER_ID, tt2, camp.kingdom);
                    if (b != null)
                    {
                        camp.tower = b;
                        camp.tower_builds++;
                        camp.built_total++;
                        camp.placed_any = true;
                        camp.build_cooldown = BUILD_COOLDOWN;
                        WallBuilder.Dbg("siege camp tower placed at " + tt2.x + "," + tt2.y + " (army " + size + ")");
                        return;
                    }
                }
            }

            int alive = 0;
            for (int i = 0; i < camp.tents.Length; i++)
                if (camp.tents[i] != null && camp.tents[i].isUsable()) alive++;

            // немного мелких деталей: бочка, телега с сеном, куча снарядов (после первой палатки)
            int wantProps = System.Math.Min(System.Math.Min(CAMP_PROPS, 3), (camp.beach ? 2 : 1) + System.Math.Max(0, size - CAMP_MIN_ARMY) / 20);
            if (wantProps > 0 && alive > 0)
            {
                string[] pool = camp.beach ? SEA_PROP_IDS : PROP_IDS;
                if (camp.prop_start < 0) camp.prop_start = Randy.randomInt(0, pool.Length);
                for (int i = 0; i < wantProps; i++)
                {
                    if (camp.props[i] != null && camp.props[i].isAlive()) continue;
                    if (camp.prop_builds[i] >= 1) continue;                      // декор не восстанавливаем
                    WorldTile ps = FreeSpot(World.world.GetTile(camp.center.x + PROP_SLOTS[i, 0], camp.center.y + PROP_SLOTS[i, 1]), 2);
                    if (ps == null) { camp.prop_builds[i] = 1; continue; }
                    Building pb = Put(pool[(camp.prop_start + i) % pool.Length], ps, camp.kingdom);
                    camp.prop_builds[i] = 1;
                    if (pb != null)
                    {
                        camp.props[i] = pb;
                        camp.built_total++;
                        return;
                    }
                }
            }

            if (alive >= wantTents) return;

            for (int i = 0; i < camp.tents.Length; i++)
            {
                if (camp.tents[i] != null && camp.tents[i].isUsable()) continue;
                if (camp.tent_builds[i] >= 1 + REBUILDS) continue;      // эту палатку уже восстанавливали
                if (camp.tent_builds[i] > 0 && DefenderNear(camp, ref defender)) return;   // пока в лагере защитник, ничего не восстанавливаем
                WorldTile slot = World.world.GetTile(camp.center.x + TENT_SLOTS[i, 0], camp.center.y + TENT_SLOTS[i, 1]);
                WorldTile t = FreeSpot(slot, 2);
                if (t == null) return;
                Building b = Put(TENT_ID, t, camp.kingdom);
                if (b != null)
                {
                    camp.tents[i] = b;
                    camp.tent_builds[i]++;
                    camp.built_total++;
                    camp.placed_any = true;
                    camp.build_cooldown = BUILD_COOLDOWN;
                    WallBuilder.Dbg("siege tent " + (alive + 1) + "/" + wantTents + " placed at " + t.x + "," + t.y);
                }
                return;
            }
        }

        // ---------- Мирные жители ----------

        private static void RecallCivilians(SiegeCamp camp)
        {
            City def = camp.target;
            WorldTile cc = def.getTile();
            if (cc == null) return;
            int sent = 0;
            foreach (Actor a in def.getUnits())
            {
                if (sent >= RECALL_MAX) break;
                if (a == null || !a.isAlive() || a.isWarrior()) continue;
                WorldTile at = a.current_tile;
                if (at == null) continue;
                if (Toolbox.Dist(at.x, at.y, cc.x, cc.y) <= RECALL_INSIDE) continue;   // уже внутри
                WorldTile home = World.world.GetTile(cc.x + Randy.randomInt(-6, 7), cc.y + Randy.randomInt(-6, 7));
                if (home == null || home.Type.block || home.is_liquid || !home.isSameIsland(at)) continue;
                a.goTo(home);
                sent++;
            }
            if (sent > 0 && camp.ticks % (RECALL_EVERY * 5) == 0)
                WallBuilder.Dbg("siege: civilians recalled to the city: " + sent);
        }
    }

    // ---------- Мост к Worldfall (3D-режим) ----------
    // Worldfall не знает наших построек и рисует их плоскими спрайтами. Здесь мы сами строим из спрайта объёмную
    // модель (воксели: каждый пиксель - кубик), собираем из неё .glb и отдаём Worldfall через его же загрузчик.
    // Всё через рефлексию: если Worldfall не установлен или поменялся, мост тихо выключается.
    internal static class Wf3D
    {
        private static bool ON = Cfg.Bool("worldfall_models", true, "в 3D-режиме Worldfall рисовать орудия, палатки, знамёна и башни мода объёмными моделями");
        private static bool _done, _failed;
        private static int _attempts;
        private static float _next;
        private static readonly Dictionary<string, string> Family = new Dictionary<string, string>();
        private static System.Collections.IDictionary _library;

        private static string[] Ids()
        {
            List<string> l = new List<string>();
            l.Add(WallBuilder.CHEAP_TOWER_ID);
            l.AddRange(Coast.IDS);
            l.Add(Siege.TENT_ID); l.Add(Siege.FIRE_ID); l.Add(Siege.CATAPULT_ID); l.Add(Siege.TREBUCHET_ID); l.Add(Siege.MORTAR_ID);
            l.Add(Siege.BARRICADE_ID); l.Add(Siege.BARRICADE_L_ID); l.Add(Siege.BARRICADE_R_ID);
            l.Add(Siege.FLAG_ID);
            l.Add(Siege.BANNER_ID); l.Add(Siege.BANNER_S_ID); l.Add(Siege.TOWER_ID);
            l.Add(Siege.PROP_BARREL); l.Add(Siege.PROP_CART); l.Add(Siege.PROP_AMMO);
            l.Add(Siege.PROP_BOAT); l.Add(Siege.PROP_ANCHOR); l.Add(Siege.PROP_NETS);
            l.Add(Fx.DEBRIS); l.Add(Fx.BRICK); l.Add(Fx.EMBER); l.Add(Fx.GFIRE); l.Add(Fx.ALERT0); l.Add(Fx.ALERT1); l.Add(Fx.ALERT2);
            return l.ToArray();
        }

        public static void Tick()
        {
            if (!ON || _done || _failed) return;
            float rt = UnityEngine.Time.realtimeSinceStartup;
            if (rt < _next) return;
            _next = rt + 5f;
            if (++_attempts > 60) { _failed = true; return; }   // Worldfall не нашёлся за ~5 минут: его нет
            try { Init(); }
            catch (System.Exception e) { _failed = true; WallBuilder.Log("[3D] bridge to Worldfall failed: " + e); }
        }

        private static void Init()
        {
            System.Type models = WfT.Get("FirstPerson.Models");
            if (models == null) return;
            System.Reflection.PropertyInfo ready = AccessTools.Property(models, "Ready");
            if (ready != null && !(bool)ready.GetValue(null, null)) return;
            System.Reflection.FieldInfo lib = AccessTools.Field(models, "_library");
            System.Type glb = WfT.Get("FirstPerson.Core.Glb");
            System.Type lim = WfT.Get("FirstPerson.Core.GlbLimits");
            System.Reflection.MethodInfo read = glb == null ? null : AccessTools.Method(glb, "Read");
            System.Reflection.MethodInfo fam = AccessTools.Method(models, "BuildingFamily", new System.Type[] { typeof(BuildingAsset) });
            if (lib == null || read == null || lim == null || fam == null)
            {
                _failed = true;
                WallBuilder.Log("[3D] Worldfall found, but its API is different (library " + (lib != null) + ", Glb.Read " + (read != null) + ", limits " + (lim != null) + ", family " + (fam != null) + "): 3D models are off");
                return;
            }
            _library = lib.GetValue(null) as System.Collections.IDictionary;
            if (_library == null || _library.Count == 0) return;   // библиотека ещё не загружена
            object limits = System.Activator.CreateInstance(lim);

            int made = 0, failed = 0, own = 0, extras = 0;
            string[] ids = Ids();
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                try
                {
                    BuildingAsset a = AssetManager.buildings.get(id);
                    if (a == null) continue;
                    string name = "ws_" + id;
                    byte[] bytes = null;
                    // готовая 3D-модель из папки Models мода (если есть), иначе выдавливаем спрайт
                    try
                    {
                        string mf = System.IO.Path.Combine(System.IO.Path.Combine(ModClass.Instance.GetDeclaration().FolderPath, "Models"), name + ".glb");
                        if (System.IO.File.Exists(mf)) { bytes = System.IO.File.ReadAllBytes(mf); own++; }
                    }
                    catch (System.Exception) { bytes = null; }
                    if (bytes == null)
                    {
                        UnityEngine.Sprite[] list = a.loadBuildingSpriteList();
                        if (list == null || list.Length == 0 || list[0] == null) continue;
                        bytes = SpriteToGlb(list[0], DepthFraction(id), name);
                    }
                    if (bytes == null) { failed++; continue; }
                    List<string> problems = new List<string>();
                    object[] args = new object[] { bytes, name, limits, problems, null };
                    object model = read.Invoke(null, args);
                    if (model == null)
                    {
                        failed++;
                        WallBuilder.Log("[3D] " + id + ": Worldfall rejected the model: " + (problems.Count > 0 ? problems[0] : "no reason given"));
                        continue;
                    }
                    _library[name] = model;
                    Family[id] = name;
                    made++;
                }
                catch (System.Exception e) { failed++; WallBuilder.Log("[3D] " + id + " failed: " + e.Message); }
            }
            // остальные файлы папки Models: обломки (ws_ruin_X -> ruin_ws_X, так их ищет Worldfall) и варианты (ws_siege_tent_2)
            try
            {
                string mdir = System.IO.Path.Combine(ModClass.Instance.GetDeclaration().FolderPath, "Models");
                if (System.IO.Directory.Exists(mdir))
                {
                    foreach (string f in System.IO.Directory.GetFiles(mdir, "*.glb"))
                    {
                        string bn = System.IO.Path.GetFileNameWithoutExtension(f);
                        string nm = bn.StartsWith("ws_ruin_") ? "ruin_ws_" + bn.Substring(8) : bn;
                        if (_library.Contains(nm)) continue;
                        try
                        {
                            List<string> pr = new List<string>();
                            object md = read.Invoke(null, new object[] { System.IO.File.ReadAllBytes(f), nm, limits, pr, null });
                            if (md == null) { failed++; WallBuilder.Log("[3D] " + bn + ": Worldfall rejected the model: " + (pr.Count > 0 ? pr[0] : "no reason given")); continue; }
                            _library[nm] = md; extras++;
                        }
                        catch (System.Exception e) { failed++; WallBuilder.Log("[3D] " + bn + " failed: " + e.Message); }
                    }
                }
            }
            catch (System.Exception e) { WallBuilder.Log("[3D] extra models failed: " + e.Message); }
            try
            {
                int turn = Cfg.Int("ram_model_turn", 1, "поворот 3D-модели тарана: 0 - как нарисована, 1 - на 90 градусов, 2 - на 180, 3 - на 270 (если таран идёт боком или задом)");
                string rk = "ws_ram_unit_t" + System.Math.Max(0, System.Math.Min(3, turn));
                if (_library.Contains(rk))
                {
                    ram_model = _library[rk];
                    foreach (string k in new string[] { "ws_ram_unit", "ws_ram_unit_f", "ws_ram_unit_m" }) _library[k] = ram_model;
                    System.Reflection.MethodInfo cf = AccessTools.Method(models, "CreatureFamily", new System.Type[] { typeof(Actor) });
                    if (cf != null) new Harmony("hello.world.walls.ram").Patch(cf, postfix: new HarmonyMethod(typeof(Wf3D), nameof(RamFamilyPost)));
                    WallBuilder.Log("[3D] own ram model attached (" + rk + ")");
                }
            }
            catch (System.Exception e) { WallBuilder.Log("[3D] ram model failed: " + e.Message); }
            if (made == 0) { _failed = true; WallBuilder.Log("[3D] no models were made (" + failed + " failed): 3D models are off"); return; }

            WallBuilder.Log("[3D] " + made + " models accepted by Worldfall (own models from files: " + own + ", extra (ruins, variants): " + extras + ")" + (failed > 0 ? ", " + failed + " failed" : ""));
            Harmony h = new Harmony("hello.world.walls.worldfall");
            h.Patch(fam, postfix: new HarmonyMethod(typeof(Wf3D), nameof(FamilyPostfix)));
            // разные палатки: Worldfall спрашивает модель для конкретного здания через TownModels.PickForTest
            try
            {
                System.Type tmt = WfT.Get("FirstPerson.Towns.TownModels");
                System.Reflection.FieldInfo pf = tmt == null ? null : AccessTools.Field(tmt, "PickForTest");
                if (pf != null && (_library.Contains("ws_siege_tent_2") || _library.Contains("ws_fx_debris_2") || _library.Contains("ws_fx_brick_2")))
                {
                    _prevPick = pf.GetValue(null) as System.Func<Building, string, string>;
                    pf.SetValue(null, new System.Func<Building, string, string>(TentPick));
                }
            }
            catch (System.Exception e) { WallBuilder.Log("[3D] tent variants failed: " + e.Message); }
            // Worldfall запоминает «чем рисовать» для каждого здания; сбрасываем, чтобы уже стоящие здания перерисовались
            try
            {
                System.Type tm = WfT.Get("FirstPerson.Towns.TownModels");
                System.Reflection.MethodInfo bump = tm == null ? null : AccessTools.Method(tm, "Bump");
                if (bump != null) bump.Invoke(null, null);
            }
            catch (System.Exception e) { WallBuilder.Dbg("[3D] cache bump failed: " + e.Message); }
            try { WfLinks.Install(h); } catch (System.Exception e) { WallBuilder.Log("[links] install failed: " + e.Message); }
            try { GunAnim3D.Install(h); } catch (System.Exception e) { WallBuilder.Log("[anim3d] install failed: " + e.Message); }
            try { TentEnter.Install(h); } catch (System.Exception e) { WallBuilder.Log("[tent] install failed: " + e.Message); }
            try { NoSolidFx.Install(h); ShellModels.Install(h); TowerLadder.InstallPatch(h); ManualGun.FpInstall(h); } catch (System.Exception e) { WallBuilder.Log("[gunner] install failed: " + e.Message); }
            _done = true;
            WallBuilder.Log("[3D] Worldfall found: " + made + " models of the mod's buildings added" + (failed > 0 ? ", " + failed + " failed" : ""));
        }

        private static readonly HashSet<string> _seen = new HashSet<string>();
        private static System.Func<Building, string, string> _prevPick;
        private static string TentPick(Building b, string fam)
        {
            try
            {
                if (fam == "ws_siege_tent" && b != null && (b.getID() & 1) == 1) return "ws_siege_tent_2";
                if (b != null && (fam == "ws_fx_debris" || fam == "ws_fx_brick"))
                {
                    int k = (int)(b.getID() % 3);
                    if (k > 0 && _library.Contains(fam + "_" + (k + 1))) return fam + "_" + (k + 1);
                }
                if (_prevPick != null) return _prevPick(b, fam);
            }
            catch (System.Exception) { }
            return null;
        }
        private static object ram_model; private static bool ram_logged;
        public static void RamFamilyPost(Actor __0, ref string __result)
        {
            try
            {
                if (__0 == null || __0.asset == null || __0.asset.id != Ram.ID || ram_model == null) return;
                if (!ram_logged) { ram_logged = true; WallBuilder.Log("[3D] the ram is looked up by family '" + __result + "'"); }
                if (_library == null) return;
                __result = "ws_ram_unit";                      // у тарана своего семейства нет: подставляем наше
            }
            catch (System.Exception) { }
        }
        public static void FamilyPostfix(BuildingAsset __0, ref string __result)
        {
            try
            {
                string f;
                if (__0 != null && __0.id != null && Family.TryGetValue(__0.id, out f))
                {
                    if (_seen.Add(__0.id)) WallBuilder.Log("[3D] drawn as " + f + ": " + __0.id + " (Worldfall wanted " + __result + ")");
                    __result = f;
                }
            }
            catch (System.Exception) { }
        }

        // Какую часть ширины спрайта занимает глубина модели (сам спрайт плоский, глубину придумываем по виду постройки)
        private static float DepthFraction(string id)
        {
            if (id.Contains("flag") || id.Contains("banner") || id.Contains("tusk")) return 0.12f;
            if (id.Contains("barricade")) return 0.25f;
            if (id.Contains("tent")) return 0.9f;
            if (id == Siege.CATAPULT_ID || id == Siege.TREBUCHET_ID || id == Siege.MORTAR_ID) return 0.55f;
            if (id.Contains("tower")) return 0.9f;
            if (id.StartsWith("camp_")) return 0.6f;
            return 0.5f;
        }

        private class Group { public List<float> pos = new List<float>(); public List<float> nrm = new List<float>(); public List<int> idx = new List<int>(); }

        // Спрайт -> .glb: каждый непрозрачный пиксель - кубик, лишние грани между кубиками не строим
        private static byte[] SpriteToGlb(UnityEngine.Sprite sp, float depthFraction, string name)
        {
            UnityEngine.Texture2D tex = sp.texture;
            UnityEngine.Rect r = sp.rect;
            UnityEngine.Color32[] px = tex.GetPixels32();          // бросит исключение, если текстура не читаема: ловим снаружи
            int tw = tex.width;
            int x0 = (int)r.x, y0 = (int)r.y, w = (int)r.width, h = (int)r.height;
            int minx = w, maxx = -1, miny = h, maxy = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[(y0 + y) * tw + x0 + x].a > 40) { if (x < minx) minx = x; if (x > maxx) maxx = x; if (y < miny) miny = y; if (y > maxy) maxy = y; }
            if (maxx < 0) return null;
            int W = maxx - minx + 1, H = maxy - miny + 1;
            int D = System.Math.Max(2, System.Math.Min(40, (int)System.Math.Round(W * depthFraction)));

            // палитра: сжимаем цвета до 4 бит на канал, оставляем самые частые
            Dictionary<int, int> count = new Dictionary<int, int>();
            int[,] key = new int[W, H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    UnityEngine.Color32 c = px[(y0 + miny + y) * tw + x0 + minx + x];
                    if (c.a <= 40) { key[x, y] = -1; continue; }
                    int k = ((c.r >> 3) << 10) | ((c.g >> 3) << 5) | (c.b >> 3);
                    key[x, y] = k;
                    int n; count.TryGetValue(k, out n); count[k] = n + 1;
                }
            List<KeyValuePair<int, int>> top = new List<KeyValuePair<int, int>>(count);
            top.Sort(delegate (KeyValuePair<int, int> a, KeyValuePair<int, int> b) { return b.Value.CompareTo(a.Value); });
            int pal = System.Math.Min(60, top.Count);
            int[] pr = new int[pal], pg = new int[pal], pb = new int[pal];
            for (int i = 0; i < pal; i++) { int k = top[i].Key; pr[i] = ((k >> 10) & 31) * 8 + 4; pg[i] = ((k >> 5) & 31) * 8 + 4; pb[i] = (k & 31) * 8 + 4; }
            Dictionary<int, int> toPal = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> kv in top)
            {
                int k = kv.Key, r0 = ((k >> 10) & 31) * 8 + 4, g0 = ((k >> 5) & 31) * 8 + 4, b0 = (k & 31) * 8 + 4, best = 0, bd = int.MaxValue;
                for (int i = 0; i < pal; i++)
                {
                    int d = (pr[i] - r0) * (pr[i] - r0) + (pg[i] - g0) * (pg[i] - g0) + (pb[i] - b0) * (pb[i] - b0);
                    if (d < bd) { bd = d; best = i; }
                }
                toPal[k] = best;
            }
            int[,] mat = new int[W, H];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) mat[x, y] = key[x, y] < 0 ? -1 : toPal[key[x, y]];

            Group[] groups = new Group[pal];
            for (int i = 0; i < pal; i++) groups[i] = new Group();
            const float U = 0.1f;
            float cx = W / 2f, cz = D / 2f;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int m = mat[x, y];
                    if (m < 0) continue;
                    Group g = groups[m];
                    for (int z = 0; z < D; z++)
                    {
                        float fx = (x - cx) * U, fy = y * U, fz = (z - cz) * U;
                        // соседи: влево, вправо, вниз, вверх (в плоскости спрайта), назад и вперёд (z)
                        if (x == 0 || mat[x - 1, y] < 0) Quad(g, fx, fy, fz, U, 0);
                        if (x == W - 1 || mat[x + 1, y] < 0) Quad(g, fx, fy, fz, U, 1);
                        if (y == 0 || mat[x, y - 1] < 0) Quad(g, fx, fy, fz, U, 2);
                        if (y == H - 1 || mat[x, y + 1] < 0) Quad(g, fx, fy, fz, U, 3);
                        if (z == 0) Quad(g, fx, fy, fz, U, 4);
                        if (z == D - 1) Quad(g, fx, fy, fz, U, 5);
                    }
                }
            string[] kinds = new string[pal];
            for (int i = 0; i < pal; i++) kinds[i] = KindOf(ref pr[i], ref pg[i], ref pb[i]) + i;
            return WriteGlb(groups, pr, pg, pb, kinds, name);
        }

        // Одна грань кубика с левым-нижним-задним углом (x,y,z): 0 -X, 1 +X, 2 -Y, 3 +Y, 4 -Z (сзади), 5 +Z (спереди, лицо спрайта)
        private static void Quad(Group g, float x, float y, float z, float u, int side)
        {
            float[][] c = new float[4][];
            float nx = 0, ny = 0, nz = 0;
            switch (side)
            {
                case 0: nx = -1; c[0] = new float[] { x, y, z + u }; c[1] = new float[] { x, y + u, z + u }; c[2] = new float[] { x, y + u, z }; c[3] = new float[] { x, y, z }; break;
                case 1: nx = 1; c[0] = new float[] { x + u, y, z }; c[1] = new float[] { x + u, y + u, z }; c[2] = new float[] { x + u, y + u, z + u }; c[3] = new float[] { x + u, y, z + u }; break;
                case 2: ny = -1; c[0] = new float[] { x, y, z }; c[1] = new float[] { x + u, y, z }; c[2] = new float[] { x + u, y, z + u }; c[3] = new float[] { x, y, z + u }; break;
                case 3: ny = 1; c[0] = new float[] { x, y + u, z + u }; c[1] = new float[] { x + u, y + u, z + u }; c[2] = new float[] { x + u, y + u, z }; c[3] = new float[] { x, y + u, z }; break;
                case 4: nz = -1; c[0] = new float[] { x + u, y, z }; c[1] = new float[] { x, y, z }; c[2] = new float[] { x, y + u, z }; c[3] = new float[] { x + u, y + u, z }; break;
                default: nz = 1; c[0] = new float[] { x, y, z + u }; c[1] = new float[] { x + u, y, z + u }; c[2] = new float[] { x + u, y + u, z + u }; c[3] = new float[] { x, y + u, z + u }; break;
            }
            int b = g.pos.Count / 3;
            for (int i = 0; i < 4; i++)
            {
                g.pos.Add(c[i][0]); g.pos.Add(c[i][1]); g.pos.Add(c[i][2]);
                g.nrm.Add(nx); g.nrm.Add(ny); g.nrm.Add(nz);
            }
            g.idx.Add(b); g.idx.Add(b + 1); g.idx.Add(b + 2);
            g.idx.Add(b); g.idx.Add(b + 2); g.idx.Add(b + 3);
        }

        private static string F(float v) { return v.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture); }

        // Тип материала Worldfall (он сам рисует для него фактуру) по цвету пикселя спрайта; цвет-маска королевства (маджента) заменяем красной тканью
        private static string KindOf(ref int r, ref int g, ref int b)
        {
            if (r > 50 && b > 50 && g < 45 && System.Math.Abs(r - b) < 30)
            {
                float k = System.Math.Max(r, b) / 255f;
                r = (int)(205 * k + 20); g = (int)(48 * k + 8); b = (int)(44 * k + 8);
                return "cloth";
            }
            int mx = System.Math.Max(r, System.Math.Max(g, b)), mn = System.Math.Min(r, System.Math.Min(g, b));
            if (r >= 220 && g >= 80 && g <= 175 && b <= 70) return "fire";
            if (mx - mn <= 22) return mx < 75 ? "metal" : (mx > 205 ? "plaster" : "stone");
            if (r >= g && g >= b && r - b >= 28)
            {
                if (r > 200 && g > 160) return "cloth";      // светлая ткань, верёвка, сено
                return "wood";
            }
            return "plain";
        }

        private static byte[] WriteGlb(Group[] groups, int[] pr, int[] pg, int[] pb, string[] kinds, string name)
        {
            System.IO.MemoryStream bin = new System.IO.MemoryStream();
            System.IO.BinaryWriter bw = new System.IO.BinaryWriter(bin);
            System.Text.StringBuilder views = new System.Text.StringBuilder(), acc = new System.Text.StringBuilder(), prims = new System.Text.StringBuilder(), mats = new System.Text.StringBuilder();
            int viewN = 0, accN = 0, matN = 0;
            for (int gi = 0; gi < groups.Length; gi++)
            {
                Group g = groups[gi];
                if (g.idx.Count == 0) continue;
                int nv = g.pos.Count / 3;
                float mnx = float.MaxValue, mny = float.MaxValue, mnz = float.MaxValue, mxx = float.MinValue, mxy = float.MinValue, mxz = float.MinValue;
                for (int i = 0; i < nv; i++)
                {
                    float x = g.pos[i * 3], y = g.pos[i * 3 + 1], z = g.pos[i * 3 + 2];
                    if (x < mnx) mnx = x; if (x > mxx) mxx = x; if (y < mny) mny = y; if (y > mxy) mxy = y; if (z < mnz) mnz = z; if (z > mxz) mxz = z;
                }
                int posView = viewN++, nrmView = viewN++, idxView = viewN++;
                int off = (int)bin.Position;
                for (int i = 0; i < g.pos.Count; i++) bw.Write(g.pos[i]);
                views.Append(viewN > 3 ? "," : "").Append("{\"buffer\":0,\"byteOffset\":" + off + ",\"byteLength\":" + (g.pos.Count * 4) + ",\"target\":34962}");
                off = (int)bin.Position;
                for (int i = 0; i < g.nrm.Count; i++) bw.Write(g.nrm[i]);
                views.Append(",{\"buffer\":0,\"byteOffset\":" + off + ",\"byteLength\":" + (g.nrm.Count * 4) + ",\"target\":34962}");
                off = (int)bin.Position;
                bool wide = nv > 65535;
                for (int i = 0; i < g.idx.Count; i++) { if (wide) bw.Write((uint)g.idx[i]); else bw.Write((ushort)g.idx[i]); }
                while (bin.Position % 4 != 0) bw.Write((byte)0);
                views.Append(",{\"buffer\":0,\"byteOffset\":" + off + ",\"byteLength\":" + (g.idx.Count * (wide ? 4 : 2)) + ",\"target\":34963}");
                int aPos = accN++, aNrm = accN++, aIdx = accN++;
                if (aPos > 0) acc.Append(",");
                acc.Append("{\"bufferView\":" + posView + ",\"componentType\":5126,\"count\":" + nv + ",\"type\":\"VEC3\",\"min\":[" + F(mnx) + "," + F(mny) + "," + F(mnz) + "],\"max\":[" + F(mxx) + "," + F(mxy) + "," + F(mxz) + "]}");
                acc.Append(",{\"bufferView\":" + nrmView + ",\"componentType\":5126,\"count\":" + nv + ",\"type\":\"VEC3\"}");
                acc.Append(",{\"bufferView\":" + idxView + ",\"componentType\":" + (wide ? 5125 : 5123) + ",\"count\":" + g.idx.Count + ",\"type\":\"SCALAR\"}");
                if (matN > 0) { prims.Append(","); mats.Append(","); }
                prims.Append("{\"attributes\":{\"POSITION\":" + aPos + ",\"NORMAL\":" + aNrm + "},\"indices\":" + aIdx + ",\"material\":" + matN + ",\"mode\":4}");
                mats.Append("{\"name\":\"" + kinds[gi] + "\",\"pbrMetallicRoughness\":{\"baseColorFactor\":[" + F(pr[gi] / 255f) + "," + F(pg[gi] / 255f) + "," + F(pb[gi] / 255f) + ",1]}}");
                matN++;
            }
            if (matN == 0) return null;
            string json = "{\"asset\":{\"version\":\"2.0\",\"generator\":\"Walls and Sieges\"},\"scene\":0,\"scenes\":[{\"nodes\":[0],\"extras\":{\"info\":\"\",\"units\":\"library\"}}],"
                + "\"nodes\":[{\"name\":\"" + name + "\",\"mesh\":0}],\"meshes\":[{\"name\":\"" + name + "\",\"primitives\":[" + prims + "]}],"
                + "\"materials\":[" + mats + "],\"accessors\":[" + acc + "],\"bufferViews\":[" + views + "],\"buffers\":[{\"byteLength\":" + bin.Length + "}]}";
            byte[] jb = System.Text.Encoding.UTF8.GetBytes(json);
            int jpad = (4 - jb.Length % 4) % 4;
            int total = 12 + 8 + jb.Length + jpad + 8 + (int)bin.Length;
            System.IO.MemoryStream o = new System.IO.MemoryStream();
            System.IO.BinaryWriter ow = new System.IO.BinaryWriter(o);
            ow.Write((uint)0x46546C67); ow.Write((uint)2); ow.Write((uint)total);
            ow.Write((uint)(jb.Length + jpad)); ow.Write((uint)0x4E4F534A); ow.Write(jb);
            for (int i = 0; i < jpad; i++) ow.Write((byte)0x20);
            ow.Write((uint)bin.Length); ow.Write((uint)0x004E4942); ow.Write(bin.ToArray());
            return o.ToArray();
        }
    }
}

namespace HelloWorld
{
    // Ручное управление осадным орудием (2D): K - взять ближайшее к курсору орудие / отпустить, L - выстрел в точку под курсором.
    // Орудие остаётся обычной постройкой (вид, анимация, снаряды прежние); пока оно под управлением, автоматический огонь у него выключен.
    internal static partial class ManualGun
    {
        private static bool ON = Cfg.Bool("manual_gun", true, "ручное управление орудиями: K - взять орудие у курсора, L - выстрел по точке под курсором (пунктирная дуга показывает полёт)");
        private static UnityEngine.LineRenderer arc, ring;
        private static UnityEngine.Material dotted, solid;
        private static bool tipped_reload;
        private static float probe_at = -1f;
        private static bool probed;

        public static void Tick()
        {
            if (!ON) return;
            try
            {
                if (!probed) Probe();
                if (World.world == null) return;
                if (TypingNow()) return;
                if (FpTick()) { Hide(); return; }                 // вид от первого лица (Worldfall): целимся взглядом
                if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.K))
                {
                    if (Siege.manual_gun != null) Release(true); else Take();
                }
                Building g = Siege.manual_gun;
                if (g == null) return;
                if (!g.isAlive() || !g.isUsable() || g.current_tile == null) { Release(true); return; }
                WorldTile mt = World.world.getMouseTilePos();
                if (mt == null) { Hide(); return; }
                float dx = mt.x - g.current_tile.x, dy = mt.y - g.current_tile.y;
                float dist = (float)System.Math.Sqrt(dx * dx + dy * dy);
                float minR = Siege.GunMinRange(g), maxR = Siege.GunMaxRange(g);
                WorldTile aim = mt;
                bool clamped = false;
                if (dist > maxR || dist < minR)
                {
                    float k = (dist < 0.01f ? 1f : (dist > maxR ? maxR : minR) / dist);
                    if (dist < 0.01f) { dx = 1; dy = 0; }
                    aim = World.world.GetTile((int)System.Math.Round(g.current_tile.x + dx * k), (int)System.Math.Round(g.current_tile.y + dy * k));
                    clamped = true;
                    if (aim == null) { Hide(); return; }
                }
                double wait = Siege.GunReadyIn(g);
                Draw(g, aim, wait <= 0 ? (clamped ? new UnityEngine.Color(1f, 0.9f, 0.3f, 0.95f) : new UnityEngine.Color(0.45f, 1f, 0.45f, 0.95f)) : new UnityEngine.Color(1f, 0.45f, 0.35f, 0.9f));
                if (wait > 0) tipped_reload = true;
                else if (tipped_reload) { tipped_reload = false; Tip(GunName(g).Substring(0, 1).ToUpper() + GunName(g).Substring(1) + (g.asset.id == Siege.TREBUCHET_ID ? L10n.T(" заряжен", " is loaded") : L10n.T(" заряжена", " is loaded"))); }
                if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.L))
                {
                    if (wait > 0) Tip(L10n.T("Перезарядка: ", "Reloading: ") + (int)System.Math.Ceiling(wait) + L10n.T(" с", " s"));
                    else Siege.ManualFire(g, aim);
                }
            }
            catch (System.Exception e) { WallBuilder.Log("[manual gun] error: " + e.Message); Release(false); ON = false; }
        }

        private static bool TypingNow()
        {
            try
            {
                var es = UnityEngine.EventSystems.EventSystem.current;
                if (es == null || es.currentSelectedGameObject == null) return false;
                return es.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>() != null;
            }
            catch (System.Exception) { return false; }
        }

        private static void Take()
        {
            WorldTile mt = World.world.getMouseTilePos();
            if (mt == null) return;
            Building best = null; float bd = 1e9f;
            foreach (Building b in World.world.buildings.getBuildingFromZones(mt, 10))
            {
                if (b == null || b.asset == null || !Siege.IsShooterId(b.asset.id) || !b.isUsable() || b.current_tile == null) continue;
                float d = Toolbox.Dist(mt.x, mt.y, b.current_tile.x, b.current_tile.y);
                if (d < bd) { bd = d; best = b; }
            }
            if (best == null || bd > 9f) { Tip(L10n.T("Наведите курсор на осадное орудие и нажмите K", "Point at a siege gun and press K")); return; }
            Siege.manual_gun = best;
            tipped_reload = false;
            Tip(L10n.T("Вы управляете: ", "You are manning: ") + GunName(best) + L10n.T(". L - выстрел, K - отпустить", ". L - fire, K - release"));
        }

        private static string GunName(Building b)
        {
            string id = b.asset.id;
            return id == Siege.MORTAR_ID ? L10n.T("мортира", "mortar") : (id == Siege.TREBUCHET_ID ? L10n.T("требушет", "trebuchet") : L10n.T("катапульта", "catapult"));
        }

        private static void Release(bool tip)
        {
            Siege.manual_gun = null;
            Hide();
            if (tip) Tip(L10n.T("Орудие снова стреляет само", "The gun fires on its own again"));
        }

        private static void Tip(string s)
        {
            try { WorldTip.showNow(s, false, "top", 2.5f); } catch (System.Exception) { }
        }

        private static void Hide()
        {
            if (arc != null) arc.enabled = false;
            if (ring != null) ring.enabled = false;
        }

        private static UnityEngine.Material Mat(bool dash)
        {
            UnityEngine.Texture2D t = new UnityEngine.Texture2D(dash ? 4 : 1, 1, UnityEngine.TextureFormat.RGBA32, false);
            t.wrapMode = UnityEngine.TextureWrapMode.Repeat; t.filterMode = UnityEngine.FilterMode.Point;
            if (dash) { t.SetPixel(0, 0, UnityEngine.Color.white); t.SetPixel(1, 0, UnityEngine.Color.white); t.SetPixel(2, 0, UnityEngine.Color.clear); t.SetPixel(3, 0, UnityEngine.Color.clear); }
            else t.SetPixel(0, 0, UnityEngine.Color.white);
            t.Apply();
            UnityEngine.Material m = new UnityEngine.Material(UnityEngine.Shader.Find("Sprites/Default"));
            m.mainTexture = t;
            return m;
        }

        private static UnityEngine.LineRenderer MakeLine(string name, UnityEngine.Material m, float w)
        {
            UnityEngine.GameObject go = new UnityEngine.GameObject(name);
            UnityEngine.Object.DontDestroyOnLoad(go);
            UnityEngine.LineRenderer lr = go.AddComponent<UnityEngine.LineRenderer>();
            lr.material = m; lr.useWorldSpace = true; lr.startWidth = w; lr.endWidth = w;
            lr.textureMode = UnityEngine.LineTextureMode.Tile; lr.sortingOrder = 30000;
            try { lr.sortingLayerName = "EffectsTop"; } catch (System.Exception) { }
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false;
            return lr;
        }

        private static void Draw(Building g, WorldTile aim, UnityEngine.Color c)
        {
            if (dotted == null) { dotted = Mat(true); solid = Mat(false); }
            if (arc == null) arc = MakeLine("ws_gun_arc", dotted, 0.5f);
            if (ring == null) { ring = MakeLine("ws_gun_ring", solid, 0.25f); ring.loop = true; }
            UnityEngine.Vector3 a = new UnityEngine.Vector3(g.current_tile.posV3.x, g.current_tile.posV3.y + g.asset.tower_projectile_offset, -1f);
            UnityEngine.Vector3 b = new UnityEngine.Vector3(aim.posV3.x, aim.posV3.y, -1f);
            float len = UnityEngine.Vector3.Distance(a, b);
            float h = len * 0.28f;
            int n = 48;
            arc.positionCount = n + 1;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                UnityEngine.Vector3 p = UnityEngine.Vector3.Lerp(a, b, t);
                p.y += h * 4f * t * (1f - t);
                arc.SetPosition(i, p);
            }
            arc.startColor = c; arc.endColor = c; arc.enabled = true;
            int rn = 28; float rad = 2.2f;
            ring.positionCount = rn;
            for (int i = 0; i < rn; i++)
            {
                float ang = i * 6.2831853f / rn;
                ring.SetPosition(i, new UnityEngine.Vector3(b.x + (float)System.Math.Cos(ang) * rad, b.y + (float)System.Math.Sin(ang) * rad * 0.8f, -1f));
            }
            ring.startColor = c; ring.endColor = c; ring.enabled = true;
        }

        // Проба: один раз пишет в лог, что есть у Worldfall для вселения/ввода (только чтение, ничего не меняет)
        private static void Probe()
        {
            if (probe_at < 0f) probe_at = UnityEngine.Time.realtimeSinceStartup + 40f;
            if (UnityEngine.Time.realtimeSinceStartup < probe_at) return;
            probed = true;
            try
            {
                string[] names = new string[] { "FirstPerson.Possess", "FirstPerson.PossessionHooks", "FirstPerson.PlayInput", "FirstPerson.Creatures", "FirstPerson.Binoculars" };
                for (int i = 0; i < names.Length; i++)
                {
                    System.Type t = AccessTools.TypeByName(names[i]);
                    if (t == null) { WallBuilder.Log("[probe] " + names[i] + ": not found"); continue; }
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    foreach (System.Reflection.MethodInfo m in t.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
                    {
                        if (m.IsSpecialName) continue;
                        sb.Append(m.IsStatic ? "static " : "").Append(m.ReturnType.Name).Append(' ').Append(m.Name).Append('(');
                        System.Reflection.ParameterInfo[] ps = m.GetParameters();
                        for (int k = 0; k < ps.Length; k++) { if (k > 0) sb.Append(", "); sb.Append(ps[k].ParameterType.Name).Append(' ').Append(ps[k].Name); }
                        sb.Append("); ");
                    }
                    WallBuilder.Log("[probe] " + names[i] + ": " + sb);
                }
                System.Type wb = WfT.Get("FirstPerson.WorldBoxMod");
                WallBuilder.Log("[probe] WorldBoxMod " + (wb != null ? "found" : "not found"));
            }
            catch (System.Exception e) { WallBuilder.Log("[probe] failed: " + e.Message); }
        }
    }
}

namespace HelloWorld
{
    // Лестница и люк на дешёвой башне (только с моделями Worldfall): если центр экрана направлен на лестницу, появляется подсказка,
    // клавиша E переносит игрока на площадку; на площадке такая же подсказка у люка в полу переносит обратно вниз.
    internal static class TowerLadder
    {
        private static bool ON = Cfg.Bool("tower_ladder", true, "подъём на башню по лестнице и спуск через люк в 3D (клавиша E, нужен Worldfall)");
        // точки на модели башни (координаты .glb: x, высота, z; z>0 - к лестнице); значения те же, что в models/ws3d.py
        private const float LAD_X = 0f, LAD_Z = 0.55f;          // низ лестницы
        private const float LAD_OUT_Z = 0.98f;                  // куда ставим игрока при спуске
        private const float TOP_Y = 1.60f;                      // верх площадки
        private const float TOP_Z = 0.22f;                      // куда ставим игрока наверху
        private const float HATCH_Z = -0.05f;                   // люк в полу
        private const float REACH = 0.62f;                      // радиус срабатывания, в единицах модели

        private static bool failed, ready;
        internal static Building top_b; internal static Actor top_actor;
        private static System.Reflection.FieldInfo fFrozenL;
        private static void Freeze(bool on)
        {
            try
            {
                if (fFrozenL == null) fFrozenL = AccessTools.Field(WfT.Get("FirstPerson.PossessionHooks"), "Frozen");
                if (fFrozenL != null && ManualGun.gunner == null) fFrozenL.SetValue(null, on);
            }
            catch (System.Exception) { }
        }
        public static bool PushPre(UnityEngine.Vector2 p, ref UnityEngine.Vector2 __result)
        {
            if (top_actor == null) return true;
            __result = p; return false;                       // на площадке башни Worldfall не выталкивает игрока от стен и стоек
        }
        public static void InstallPatch(Harmony h)
        {
            if (!ON) return;
            try
            {
                System.Type ct = WfT.Get("FirstPerson.Colliders");
                System.Reflection.MethodInfo m = ct == null ? null : AccessTools.Method(ct, "PushOut");
                if (m == null) { WallBuilder.Log("[ladder] Colliders.PushOut not found"); return; }
                h.Patch(m, prefix: new HarmonyMethod(typeof(TowerLadder), nameof(PushPre)));
                WallBuilder.Log("[ladder] push-out patch ready");
            }
            catch (System.Exception e) { WallBuilder.Log("[ladder] patch failed: " + e.Message); }
        }
        private static System.Reflection.FieldInfo fActive, fYaw, fUnit;
        private static System.Reflection.PropertyInfo pInst;
        private static System.Reflection.MethodInfo mWorldOf, mPlaceBody, mStop;
        private static System.Reflection.FieldInfo fLanded, fFloorZ, fTakeoff, fLift;
        private static HudBehaviour hud;
        private static float next_scan;
        private static Building cand;
        private static bool cand_up;
        private static string text;
        private static Actor last_actor;
        private static float watch_until, next_watch;
        private static float verify_at; private static float verify_z; private static string verify_note;
        private static int logged;

        private static bool Init()
        {
            if (ready) return true;
            if (failed) return false;
            try
            {
                System.Type ph = WfT.Get("FirstPerson.PossessionHooks");
                System.Type wb = WfT.Get("FirstPerson.WorldBoxMod");
                System.Type bf = WfT.Get("FirstPerson.BuildingFloors");
                if (ph == null || wb == null || bf == null) return false;       // Worldfall ещё не загружен
                fActive = AccessTools.Field(ph, "Active"); fYaw = AccessTools.Field(ph, "Yaw");
                mStop = AccessTools.Method(ph, "Stop");
                pInst = AccessTools.Property(wb, "Instance");
                mPlaceBody = AccessTools.Method(wb, "PlaceBody");
                mWorldOf = AccessTools.Method(bf, "WorldOf");
                fLanded = AccessTools.Field(wb, "_landedOn"); fFloorZ = AccessTools.Field(wb, "_floorZ"); fTakeoff = AccessTools.Field(wb, "_takeoffZ"); fLift = AccessTools.Field(wb, "_standLift");
                fUnit = AccessTools.Field(typeof(ControllableUnit), "_unit_main");
                if (fActive == null || fYaw == null || pInst == null || mPlaceBody == null || mWorldOf == null || fUnit == null)
                {
                    failed = true;
                    WallBuilder.Log("[ladder] Worldfall API is different (Active " + (fActive != null) + ", Yaw " + (fYaw != null) + ", Instance " + (pInst != null) + ", PlaceBody " + (mPlaceBody != null) + ", WorldOf " + (mWorldOf != null) + ", unit " + (fUnit != null) + "): ladder is off");
                    return false;
                }
                UnityEngine.GameObject go = new UnityEngine.GameObject("ws_ladder_hud");
                UnityEngine.Object.DontDestroyOnLoad(go);
                hud = go.AddComponent<HudBehaviour>();
                ready = true;
                WallBuilder.Log("[ladder] ready");
                return true;
            }
            catch (System.Exception e) { failed = true; WallBuilder.Log("[ladder] init failed: " + e.Message); return false; }
        }

        // Точка модели -> мир: позиция, нижний уровень и масштаб (единиц мира на единицу модели). Модель Worldfall: y = -z файла .glb
        private static bool WorldOf(Building b, float gx, float gz, out UnityEngine.Vector2 at, out float baseZ, out float scale)
        {
            at = UnityEngine.Vector2.zero; baseZ = 0f; scale = 1f;
            object[] a = new object[] { b, gx, -gz, null, 0f, 0f };
            bool ok = (bool)mWorldOf.Invoke(null, a);
            if (!ok) return false;
            at = (UnityEngine.Vector2)a[3]; baseZ = (float)a[4]; scale = (float)a[5];
            return true;
        }

        public static void Tick()
        {
            if (!ON) return;
            try
            {
                if (World.world == null) return;
                if (verify_at > 0f && UnityEngine.Time.realtimeSinceStartup >= verify_at)
                {
                    verify_at = 0f;
                    if (last_actor != null) WallBuilder.Log("[ladder] " + verify_note + ": height " + last_actor.position_height.ToString("F2") + " (was set to " + verify_z.ToString("F2") + "), at " + last_actor.current_position.x.ToString("F2") + "," + last_actor.current_position.y.ToString("F2"));
                }
                if (!Init()) { text = null; return; }
                if (!(bool)fActive.GetValue(null)) { text = null; cand = null; perch_actor = null; if (top_actor != null) { top_actor = null; top_b = null; Freeze(false); } return; }
                if (watch_until > 0f)
                {
                    float wn = UnityEngine.Time.realtimeSinceStartup;
                    if (wn > watch_until) watch_until = 0f;
                    else if (wn >= next_watch && last_actor != null)
                    {
                        next_watch = wn + 0.1f;
                        object wi = pInst.GetValue(null, null);
                        string st = "";
                        try { if (wi != null) st = " landed " + fLanded.GetValue(wi) + ", floorZ " + ((float)fFloorZ.GetValue(wi)).ToString("F2") + ", takeoffZ " + ((float)fTakeoff.GetValue(wi)).ToString("F2"); } catch (System.Exception) { }
                        WallBuilder.Log("[ladder] watch: height " + last_actor.position_height.ToString("F2") + " at " + last_actor.current_position.x.ToString("F2") + "," + last_actor.current_position.y.ToString("F2") + st);
                    }
                }
                Actor me = fUnit.GetValue(null) as Actor;
                if (me == null || !me.isAlive()) { text = null; cand = null; return; }
                if (perch_actor != null) PerchTick(me);
                if (top_actor != null)
                {
                    UnityEngine.Vector2 tc; float tbz, tsc;
                    if (me != top_actor || top_b == null || !top_b.isAlive() || !WorldOf(top_b, 0f, 0f, out tc, out tbz, out tsc)) { top_actor = null; top_b = null; Freeze(false); }
                    else
                    {
                        UnityEngine.Vector2 td = me.current_position - tc;
                        if (System.Math.Abs(td.x) > 2.5f * tsc || System.Math.Abs(td.y) > 2.5f * tsc) { top_actor = null; top_b = null; Freeze(false); WallBuilder.Log("[ladder] left the platform"); }
                    }
                }
                float now = UnityEngine.Time.realtimeSinceStartup;
                if (now >= next_scan) { next_scan = now + 0.1f; Scan(me); }
                else if (cand != null) Check(me, cand);
                if (text != null && cand != null && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.E) && !Typing()) Go(me, cand, cand_up);
            }
            catch (System.Exception e) { WallBuilder.Log("[ladder] error: " + e.Message); ON = false; text = null; }
        }

        private static bool Typing()
        {
            try
            {
                var es = UnityEngine.EventSystems.EventSystem.current;
                return es != null && es.currentSelectedGameObject != null && es.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>() != null;
            }
            catch (System.Exception) { return false; }
        }

        private static void Scan(Actor me)
        {
            WorldTile t = me.current_tile;
            cand = null; text = null;
            if (t == null) return;
            foreach (Building b in World.world.buildings.getBuildingFromZones(t, 5))
            {
                if (b == null || b.asset == null || !b.isAlive()) continue;
                string id = b.asset.id;
                if (id != WallBuilder.CHEAP_TOWER_ID && id != Siege.TOWER_ID) continue;
                if (Check(me, b)) { cand = b; return; }
            }
        }

        // Смотрит ли игрок на лестницу (внизу) или на люк (наверху) этой башни
        private static bool Check(Actor me, Building b)
        {
            text = null;
            UnityEngine.Vector2 ladder, hatch; float baseZ, sc, bz2, sc2; UnityEngine.Vector2 tmp;
            if (!WorldOf(b, LAD_X, LAD_Z, out ladder, out baseZ, out sc)) return false;
            if (!WorldOf(b, 0f, HATCH_Z, out hatch, out bz2, out sc2)) return false;
            float topZ = baseZ + TOP_Y * sc;
            float h = me.position_height;
            bool up = h > (TOP_Y * sc) * 0.5f;                         // игрок наверху (position_height - высота над землёй)
            UnityEngine.Vector2 target = up ? hatch : ladder;
            UnityEngine.Vector2 p = me.current_position;
            UnityEngine.Vector2 d = target - p;
            float dist = d.magnitude;
            float reach = REACH * sc;
            if (dist > reach * (up ? 1.0f : 1.7f)) return false;
            float yaw = (float)fYaw.GetValue(null);
            UnityEngine.Vector2 fwd = new UnityEngine.Vector2(UnityEngine.Mathf.Cos(yaw), UnityEngine.Mathf.Sin(yaw));
            float cos = dist < 0.001f ? 1f : UnityEngine.Vector2.Dot(fwd, d / dist);
            if (dist > reach * 0.35f && cos < 0.8f) return false;       // центр экрана не на цели (около 35 градусов)
            cand_up = up;
            text = up ? L10n.T("[E] Спуститься через люк", "[E] Go down the hatch") : L10n.T("[E] Подняться по лестнице", "[E] Climb the ladder");
            if (logged < 6 && !_seenTower.Contains(b.getID()))
            {
                _seenTower.Add(b.getID()); logged++;
                WallBuilder.Log("[ladder] tower " + b.asset.id + " at " + b.current_tile.x + "," + b.current_tile.y + ": ladder world " + ladder.x.ToString("F2") + "," + ladder.y.ToString("F2") + ", hatch " + hatch.x.ToString("F2") + "," + hatch.y.ToString("F2") + ", scale " + sc.ToString("F3") + ", base z " + baseZ.ToString("F2") + ", top z " + topZ.ToString("F2") + ", player " + p.x.ToString("F2") + "," + p.y.ToString("F2") + " h " + h.ToString("F2"));
            }
            return true;
        }
        private static readonly HashSet<long> _seenTower = new HashSet<long>();

        // ---- пол площадки: просим у Worldfall настоящую высоту поверхности под ногами (его пол строится по модели) ----
        private static System.Reflection.MethodInfo mStandOn, mGroundAt, mDescribe;
        private static void InitFloor()
        {
            if (mStandOn != null || floor_tried) return;
            floor_tried = true;
            System.Type bf = WfT.Get("FirstPerson.BuildingFloors");
            System.Type wb = WfT.Get("FirstPerson.WorldBoxMod");
            if (bf != null) mStandOn = AccessTools.Method(bf, "StandOn", new System.Type[] { typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float).MakeByRefType(), typeof(byte).MakeByRefType() });
            if (bf != null) mDescribe = AccessTools.Method(bf, "DescribeForTest");
            if (wb != null) mGroundAt = AccessTools.Method(wb, "GroundAt");
        }
        private static bool floor_tried;

        // Высота пола (мировая z) в точке (x,y) не выше maxZ; false - Worldfall пола там не знает
        private static bool FloorAt(float x, float y, float maxZ, out float z)
        {
            z = 0f;
            InitFloor();
            if (mStandOn == null) return false;
            object[] a = new object[] { x, y, BodyR(), maxZ, float.NaN, 0f, (byte)0 };
            bool ok = (bool)mStandOn.Invoke(null, a);
            if (!ok) return false;
            z = (float)a[5];
            return true;
        }

        private static System.Reflection.PropertyInfo pRadius;
        private static float BodyR()
        {
            try
            {
                if (pRadius == null) pRadius = AccessTools.Property(WfT.Get("FirstPerson.PossessionHooks"), "BodyRadius");
                if (pRadius != null) return (float)pRadius.GetValue(null, null);
            }
            catch (System.Exception) { }
            return 0.12f;
        }

        private static float GroundZ(UnityEngine.Vector2 p)
        {
            InitFloor();
            object inst = pInst.GetValue(null, null);
            if (mGroundAt == null || inst == null) return 0f;
            return (float)mGroundAt.Invoke(inst, new object[] { p });
        }

        // ---- запасной вариант: если Worldfall не нашёл пол, держим игрока на площадке сами, пока он не сойдёт с неё ----
        private static Actor perch_actor; private static Building perch_b; private static float perch_h; private static float perch_floor;
        private static bool perch_pinned; private static UnityEngine.Vector2 perch_pin;
        private static void PerchTick(Actor me)
        {
            if (perch_actor == null) return;
            if (me != perch_actor || !me.isAlive() || perch_b == null || !perch_b.isAlive()) { perch_actor = null; return; }
            UnityEngine.Vector2 c; float bz, sc;
            if (!WorldOf(perch_b, 0f, 0f, out c, out bz, out sc)) { perch_actor = null; return; }
            UnityEngine.Vector2 d = me.current_position - c;
            if (!perch_pinned) { perch_pin = me.current_position - c; perch_pinned = true; }
            Freeze(true);
            float lim = 0.04f * sc;
            d -= perch_pin;
            if (System.Math.Abs(d.x) > lim || System.Math.Abs(d.y) > lim)
            {
                UnityEngine.Vector2 cl = c + perch_pin + new UnityEngine.Vector2(UnityEngine.Mathf.Clamp(d.x, -lim, lim), UnityEngine.Mathf.Clamp(d.y, -lim, lim));
                try { object pi = pInst.GetValue(null, null); if (pi != null) mPlaceBody.Invoke(pi, new object[] { me, cl }); } catch (System.Exception) { }       // с площадки не сойти: вниз только через люк (E)
            }
            me.position_height = perch_h;
            object inst = pInst.GetValue(null, null);
            try { if (inst != null) { if (fLanded != null) fLanded.SetValue(inst, true); if (fFloorZ != null) fFloorZ.SetValue(inst, perch_floor); if (fTakeoff != null) fTakeoff.SetValue(inst, perch_floor); if (fLift != null) fLift.SetValue(inst, 0f); } } catch (System.Exception) { }
        }

        private static void Go(Actor me, Building b, bool wasUp)
        {
            UnityEngine.Vector2 at; float baseZ, sc;
            float z;
            object inst = pInst.GetValue(null, null);
            if (inst == null) return;
            if (!wasUp)
            {
                if (!WorldOf(b, 0f, TOP_Z, out at, out baseZ, out sc)) return;
                // высота пола площадки: сначала спрашиваем Worldfall (его пол строится по самой модели), иначе берём высоту из модели
                float floorZ = baseZ + TOP_Y * sc; bool found = false;
                // перебираем точки площадки с настоящим радиусом тела игрока: у люка пол с дыркой, и в узком месте тело не помещается
                string trail = "";
                float bestScore = 1e9f; float bgx = 0f, bgz = TOP_Z;
                float[] gxs = new float[] { 0f, -0.2f, 0.2f, -0.35f, 0.35f };
                float[] gzs = new float[] { 0.30f, 0.38f, 0.22f, 0.46f, 0.55f, 0.1f, -0.2f, -0.35f, -0.5f };
                for (int ix = 0; ix < gxs.Length; ix++)
                    for (int iz = 0; iz < gzs.Length; iz++)
                    {
                        UnityEngine.Vector2 q; float bz2, sc2;
                        if (!WorldOf(b, gxs[ix], gzs[iz], out q, out bz2, out sc2)) continue;
                        float fz;
                        if (!FloorAt(q.x, q.y, baseZ + (TOP_Y + 0.4f) * sc, out fz) || fz <= baseZ + 0.8f * sc) continue;
                        float score = System.Math.Abs(gzs[iz] - 0.30f) + System.Math.Abs(gxs[ix]) * 0.5f;
                        if (score < bestScore) { bestScore = score; bgx = gxs[ix]; bgz = gzs[iz]; floorZ = fz; found = true; }
                    }
                if (found) { WorldOf(b, bgx, bgz, out at, out baseZ, out sc); trail = " at model " + bgx.ToString("F2") + "," + bgz.ToString("F2") + " body radius " + BodyR().ToString("F2"); }
                else trail = " (no point of the platform fits the body, radius " + BodyR().ToString("F2") + ")";
                WallBuilder.Log("[ladder] Worldfall floor on the platform: " + (found ? "found, " + ((floorZ - baseZ) / sc).ToString("F2") + " model units" : "NOT found (model height " + TOP_Y.ToString("F2") + " used)") + trail);
                mPlaceBody.Invoke(inst, new object[] { me, at });
                float gz = GroundZ(at);
                me.position_height = System.Math.Max(0.05f, floorZ - gz + 0.02f);
                z = me.position_height;
                float worldFloor = gz + z - 0.02f;
                try { if (fLanded != null) fLanded.SetValue(inst, true); if (fFloorZ != null) fFloorZ.SetValue(inst, worldFloor); if (fTakeoff != null) fTakeoff.SetValue(inst, worldFloor); if (fLift != null) fLift.SetValue(inst, 0f); } catch (System.Exception) { }
                top_actor = me; top_b = b; Freeze(true);
                perch_pinned = false; perch_actor = me; perch_b = b; perch_h = z; perch_floor = worldFloor;       // держим игрока на площадке сами: Worldfall его оттуда роняет
                try { if (mDescribe != null) WallBuilder.Log("[ladder] Worldfall's view of the platform: " + mDescribe.Invoke(null, new object[] { b, at })); } catch (System.Exception) { }
            }
            else
            {
                if (!WorldOf(b, 0f, LAD_OUT_Z, out at, out baseZ, out sc)) return;
                perch_actor = null; top_actor = null; top_b = null; Freeze(false);
                mPlaceBody.Invoke(inst, new object[] { me, at });
                me.position_height = 0f;
                z = 0f;
                try { if (fLanded != null) fLanded.SetValue(inst, false); if (fFloorZ != null) fFloorZ.SetValue(inst, float.NaN); if (fTakeoff != null) fTakeoff.SetValue(inst, float.NaN); if (fLift != null) fLift.SetValue(inst, 0f); } catch (System.Exception) { }
            }
            if (mStop != null) { try { mStop.Invoke(null, null); } catch (System.Exception) { } }
            last_actor = me; verify_z = z; verify_at = UnityEngine.Time.realtimeSinceStartup + 0.6f;
            watch_until = UnityEngine.Time.realtimeSinceStartup + 1.5f; next_watch = 0f;
            try { System.Reflection.FieldInfo pt = AccessTools.Field(WfT.Get("FirstPerson.WorldBoxMod"), "PullTrace"); if (pt != null) pt.SetValue(null, true); } catch (System.Exception) { }
            verify_note = wasUp ? "after going down" : "after going up";
            WallBuilder.Log("[ladder] " + (wasUp ? "down" : "up") + ": to " + at.x.ToString("F2") + "," + at.y.ToString("F2") + " height " + z.ToString("F2"));
            text = null; cand = null; next_scan = UnityEngine.Time.realtimeSinceStartup + 0.4f;
        }

        // Подсказка под центром экрана
        private class HudBehaviour : UnityEngine.MonoBehaviour
        {
            private UnityEngine.GUIStyle st;
            void OnGUI()
            {
                string s = TowerLadder.text;
                if (s == null) return;
                Prompt.Draw(s);
            }
        }
    }
}

namespace HelloWorld
{
    // Связь с Worldfall: события осады в его ленте новостей и реплики жителей осаждённого города
    internal static class WfLinks
    {
        private static bool ON = Cfg.Bool("worldfall_links", true, "связь с Worldfall: события осады в его новостях и разговоры жителей об осаде");
        private static System.Reflection.MethodInfo mAnnounce;
        private static System.Type toneType;
        private static bool tried;
        private static readonly Dictionary<string, double> last = new Dictionary<string, double>();

        public static void News(string id, string text, WorldTile at)
        {
            if (!ON) return;
            try
            {
                if (!tried)
                {
                    tried = true;
                    System.Type wn = WfT.Get("FirstPerson.WorldNews");
                    toneType = WfT.Get("FirstPerson.NewsTone");
                    mAnnounce = wn == null ? null : AccessTools.Method(wn, "Announce");
                }
                if (mAnnounce == null || toneType == null) return;
                double now = UnityEngine.Time.realtimeSinceStartup; double t;
                string key = id + "|" + text;
                if (last.TryGetValue(key, out t) && now - t < 30.0) return;
                last[key] = now;
                string tone = id.EndsWith("lifted") ? "Good" : (id.EndsWith("fell") || id.EndsWith("ruined") ? "Bad" : "Warn");
                object tv = System.Enum.Parse(toneType, tone);
                UnityEngine.Vector2 pos = at != null ? new UnityEngine.Vector2(at.x, at.y) : UnityEngine.Vector2.zero;
                mAnnounce.Invoke(null, new object[] { id, text, tv, pos });
                WallBuilder.Dbg("[links] news to Worldfall: " + text);
            }
            catch (System.Exception e) { WallBuilder.Log("[links] news failed: " + e.Message); ON = false; }
        }

        public static void Install(Harmony h)
        {
            if (!ON) return;
            System.Type dl = WfT.Get("FirstPerson.Dialogue");
            System.Reflection.MethodInfo g = dl == null ? null : AccessTools.Method(dl, "Gossip", new System.Type[] { typeof(Actor), typeof(Actor) });
            if (g == null) { WallBuilder.Log("[links] Dialogue.Gossip not found: villagers will not talk about sieges"); return; }
            h.Patch(g, postfix: new HarmonyMethod(typeof(WfLinks), nameof(GossipPostfix)));
            WallBuilder.Log("[links] ready (news and villagers' talk)");
        }

        public static void GossipPostfix(Actor __0, Actor __1, ref string __result)
        {
            try
            {
                if (!ON || __0 == null || __0.city == null) return;
                SiegeCamp c = Siege.CampTargeting(__0.city);
                if (c == null || Randy.randomFloat(0f, 1f) > 0.7f) return;
                Loc.Apply();
                string key;
                if (c.shooter_id == Siege.MORTAR_ID) key = "ws_gossip_mortar";
                else key = "ws_gossip_siege_" + Randy.randomInt(1, 4);
                string txt = LocalizedTextManager.getText(key);
                if (string.IsNullOrEmpty(txt) || txt == key) return;
                __result = txt.Replace("$1", c.kingdom != null ? c.kingdom.name : "?");
            }
            catch (System.Exception) { }
        }
    }
}

namespace HelloWorld
{
    // Осадные постройки лагеря (палатки, орудия, частокол, костёр, знамя) не ломают мирные жители: их громят только воины
    internal static class CampGuard
    {
        private static readonly bool ON = false;   // отложено до 1.4: мирные жители бьют постройки лагеря, как в 1.2
        public static void Install(Harmony h)
        {
            if (!ON) return;
            h.Patch(AccessTools.Method(typeof(Building), nameof(Building.getHit)), prefix: new HarmonyMethod(typeof(CampGuard), nameof(Pre)));
        }
        private static bool Protected(string id)
        {
            return id == Siege.TENT_ID || id == Siege.FIRE_ID || id == Siege.STAKE_ID || id == Siege.STAKE_V_ID || id == Siege.BANNER_ID || id == Siege.BANNER_S_ID || id == Siege.TOWER_ID
                || Siege.IsShooterId(id) || Siege.IsPropId(id);
        }
        public static bool Pre(Building __instance, BaseSimObject pAttacker)
        {
            try
            {
                if (__instance == null || __instance.asset == null || !Protected(__instance.asset.id)) return true;
                Actor a = pAttacker as Actor;
                if (a == null) return true;
                return a.isWarrior();                     // мирный житель урона не наносит
            }
            catch (System.Exception) { return true; }
        }
    }
}

namespace HelloWorld
{
    // Огонь, угли и обломки в 3D не твёрдые: сквозь них можно пройти (иначе огонь в пролёте павших ворот не пускает игрока)
    internal static class NoSolidFx
    {
        private static System.Reflection.FieldInfo fName;
        private static int logged;
        public static void Install(Harmony h)
        {
            System.Type sc = WfT.Get("FirstPerson.SceneCollector");
            System.Reflection.MethodInfo m = sc == null ? null : AccessTools.Method(sc, "SolidModel");
            System.Type mm = WfT.Get("FirstPerson.Core.MeshModel");
            if (m == null || mm == null) { WallBuilder.Log("[3D] SolidModel not found: fire stays solid"); return; }
            fName = AccessTools.Field(mm, "Name");
            if (fName == null) return;
            h.Patch(m, prefix: new HarmonyMethod(typeof(NoSolidFx), nameof(Pre)));
            // второй путь: DrawAsModel строит коллайдер сам (Colliders.FromModel -> SceneCollector.Solid)
            try
            {
                System.Type cs = WfT.Get("FirstPerson.Colliders");
                System.Reflection.MethodInfo fm = cs == null ? null : AccessTools.Method(cs, "FromModel");
                System.Reflection.MethodInfo so = AccessTools.Method(sc, "Solid");
                if (fm != null && so != null)
                {
                    h.Patch(fm, prefix: new HarmonyMethod(typeof(NoSolidFx), nameof(FromPre)));
                    h.Patch(so, prefix: new HarmonyMethod(typeof(NoSolidFx), nameof(SolidPre)));
                }
            }
            catch (System.Exception e) { WallBuilder.Log("[3D] second collider path not patched: " + e.Message); }
            WallBuilder.Log("[3D] fire and debris are not solid");
        }
        private static bool skip_next;
        public static void FromPre(object __0)
        {
            try
            {
                string n = __0 == null || fName == null ? null : fName.GetValue(__0) as string;
                skip_next = n != null && n.StartsWith("ws_fx_") && !n.Contains("alert");
            }
            catch (System.Exception) { skip_next = false; }
        }
        public static bool SolidPre()
        {
            if (!skip_next) return true;
            skip_next = false; return false;
        }
        public static bool Pre(object __1)
        {
            try
            {
                string n = __1 == null ? null : fName.GetValue(__1) as string;
                if (n == null || !n.StartsWith("ws_fx_") || n.Contains("alert")) return true;
                if (logged++ < 2) WallBuilder.Log("[3D] no collider for " + n);
                return false;
            }
            catch (System.Exception) { return true; }
        }
    }
}

namespace HelloWorld
{
    // В 2D спрайты орудий умеют смотреть в одну сторону: отражаем их по горизонтали, когда цель слева
    internal static class GunFlip
    {
        private static readonly bool ON = Cfg.Bool("gun_sprite_flip", true, "в 2D осадные орудия разворачиваются спрайтом лицом к цели (влево или вправо)");
        private static readonly bool INVERT = Cfg.Bool("gun_sprite_flip_invert", false, "если орудия в 2D смотрят от цели - поставьте true");
        private static float next;
        private static int logged;
        public static void Tick()
        {
            if (!ON || World.world == null) return;
            float rt = UnityEngine.Time.realtimeSinceStartup;
            if (rt < next) return;
            next = rt + 0.4f;
            try
            {
                List<Building> all = World.world.buildings.getSimpleList();
                for (int i = 0; i < all.Count; i++)
                {
                    Building b = all[i];
                    if (b == null || b.asset == null || !Siege.IsShooterId(b.asset.id) || !b.isAlive() || b.current_tile == null) continue;
                    UnityEngine.Vector2 at;
                    if (!Siege.GunFacing(b, out at)) continue;
                    float dx = at.x - b.current_tile.x;
                    if (System.Math.Abs(dx) < 1.5f) continue;
                    bool want = (dx < 0f) != INVERT;
                    if (b.flip_x == want) continue;
                    b.flip_x = want;
                    if (logged++ < 4) WallBuilder.Log("[flip] " + b.asset.id + " now looks " + (dx < 0f ? "left" : "right"));
                }
            }
            catch (System.Exception) { }
        }
    }
}

namespace HelloWorld
{
    // В 2D: при наведении мыши на закрытые ворота показываем лёгкую шкалу их прочности (как шкала здоровья у выделенного юнита)
    internal class GateBarGui : UnityEngine.MonoBehaviour
    {
        private float alpha, frac = 1f;
        private UnityEngine.Vector3 pos;
        private float lost;
        void OnGUI()
        {
            try
            {
                if (World.world == null || Scroll.Is3D()) { alpha = 0f; return; }
                WorldTile mt = World.world.getMouseTilePos();
                float f; WorldTile at;
                bool hov = mt != null && WallBuilder.GateHover(mt, out f, out at);
                if (hov)
                {
                    WallBuilder.GateHover(mt, out f, out at);
                    frac = f; pos = new UnityEngine.Vector3(at.posV3.x, at.posV3.y, 0f); lost = 0f;
                    alpha = UnityEngine.Mathf.MoveTowards(alpha, 1f, UnityEngine.Time.unscaledDeltaTime * 8f);
                }
                else alpha = UnityEngine.Mathf.MoveTowards(alpha, 0f, UnityEngine.Time.unscaledDeltaTime * 5f);
                if (alpha <= 0.01f || UnityEngine.Camera.main == null) return;
                UnityEngine.Vector3 sp = UnityEngine.Camera.main.WorldToScreenPoint(pos);
                if (sp.z < 0f) return;
                float zoom = UnityEngine.Mathf.Clamp(UnityEngine.Screen.height / 1000f, 0.8f, 1.6f);
                float w = 46f * zoom, h = 4.8f * zoom, x = sp.x - w / 2f, y = UnityEngine.Screen.height - sp.y - 20f * zoom;
                UnityEngine.GUI.depth = -32000;
                UnityEngine.Color old = UnityEngine.GUI.color;
                UnityEngine.GUI.color = new UnityEngine.Color(0.05f, 0.04f, 0.04f, 0.8f * alpha);
                UnityEngine.GUI.DrawTexture(new UnityEngine.Rect(x - 1.5f, y - 1.5f, w + 3f, h + 3f), UnityEngine.Texture2D.whiteTexture);
                UnityEngine.GUI.color = new UnityEngine.Color(0.28f, 0.14f, 0.12f, 0.9f * alpha);
                UnityEngine.GUI.DrawTexture(new UnityEngine.Rect(x, y, w, h), UnityEngine.Texture2D.whiteTexture);
                UnityEngine.Color fill = UnityEngine.Color.Lerp(new UnityEngine.Color(0.85f, 0.25f, 0.2f), new UnityEngine.Color(0.45f, 0.82f, 0.3f), frac);
                UnityEngine.GUI.color = new UnityEngine.Color(fill.r, fill.g, fill.b, alpha);
                UnityEngine.GUI.DrawTexture(new UnityEngine.Rect(x, y, w * frac, h), UnityEngine.Texture2D.whiteTexture);
                UnityEngine.GUI.color = old;
            }
            catch (System.Exception) { }
        }
    }
}

namespace HelloWorld
{
    // Свиток осады: итог осады в виде свитка. После конца осады, которую видел игрок (город в кадре или тело игрока рядом),
    // появляется подсказка «Посмотреть свиток осады: J»; хранится только последний свиток.
    internal static class Scroll
    {
        private static bool ON = Cfg.Bool("siege_scroll", true, "после осады, которую видел игрок, предлагать посмотреть свиток осады (клавиша J)");
        private static readonly UnityEngine.KeyCode KEY = UnityEngine.KeyCode.J;

        private class Rec
        {
            public string attacker, defender, outcome;
            public double t0, t1, seen_at;
            public int army0, army1, def0, def1, guns;
            public bool ram, gate, wall, watched;
            public List<string> lines = new List<string>();
            public HashSet<string> keys = new HashSet<string>();
            public int year0, mon_n; public string mon0; public WorldTile site;
            public City city; public Kingdom att_k;
        }
        private static readonly Dictionary<City, Rec> active = new Dictionary<City, Rec>();
        private static Rec last;
        private static bool open;
        private static double hint_until;
        private static float next_watch, open_t0;
        private static UnityEngine.GameObject ui;

        private static string T(string key) { try { Loc.Apply(); } catch (System.Exception) { } return LocalizedTextManager.getText(key); }
        private static double DayLen() { try { return Date.getMonthTime() / 30.0; } catch (System.Exception) { return 2.0; } }
        private static int DayOf(Rec r, double now) { return 1 + (int)((now - r.t0) / System.Math.Max(0.2, DayLen())); }

        private static string MonthName(double t)
        {
            try { int[] raw = Date.getRawDate(t); return Date.formatMonth(raw[1]); } catch (System.Exception) { return ""; }
        }

        public static void Begin(City attacker, City target, int army, WorldTile site = null)
        {
            if (!ON || attacker == null || target == null) return;
            try
            {
                double now = World.world.getCurWorldTime();
                Rec r = new Rec();
                r.attacker = attacker.kingdom != null ? attacker.kingdom.name : attacker.name; r.defender = target.name;
                r.site = site; r.t0 = now; r.army0 = army; r.def0 = target.countWarriors(); r.city = target; r.att_k = attacker.kingdom;
                r.year0 = Date.getYear(now); r.mon0 = MonthName(now);
                active[target] = r;
                Line(r, "start", T("ws_sc_start").Replace("$1", r.attacker).Replace("$2", r.defender), now);
            }
            catch (System.Exception e) { WallBuilder.Log("scroll begin failed: " + e.Message); }
        }

        private static void Line(Rec r, string key, string text, double now)
        {
            if (!r.keys.Add(key) || r.lines.Count >= 8) return;
            r.lines.Add(MonthName(now) + ". " + text);
        }

        public static void Note(City target, string key)
        {
            if (!ON || target == null) return;
            Rec r;
            if (!active.TryGetValue(target, out r)) return;
            double now = World.world.getCurWorldTime();
            switch (key)
            {
                case "closed": Line(r, key, T("ws_sc_closed"), now); break;
                case "guns": r.guns = System.Math.Max(r.guns, 1); Line(r, key, T("ws_sc_guns"), now); break;
                case "ram": r.ram = true; Line(r, key, T("ws_sc_ram"), now); break;
                case "assault": Line(r, key, T("ws_sc_assault"), now); break;
                case "gate": r.gate = true; Line(r, key, T("ws_sc_gate"), now); break;
                case "wall": r.wall = true; Line(r, key, T("ws_sc_wall"), now); break;
            }
        }

        public static void End(City attacker, City target, string outcomeId)
        {
            if (!ON || target == null) return;
            try
            {
                Rec r;
                if (!active.TryGetValue(target, out r)) return;
                active.Remove(target);
                double now = World.world.getCurWorldTime();
                r.t1 = now;
                try { r.mon_n = System.Math.Max(0, Date.getMonthsSince(r.t0) - Date.getMonthsSince(now)); } catch (System.Exception) { }
                r.army1 = attacker != null && attacker.army != null ? attacker.army.units.Count : 0;
                r.def1 = target.isAlive() ? target.countWarriors() : 0;
                string key = outcomeId == "mod_siege_fell" ? "ws_sc_fell" : outcomeId == "mod_siege_ruined" ? "ws_sc_ruined" : "ws_sc_repelled";
                r.outcome = T(key);
                string endLine = outcomeId == "mod_siege_fell" ? "ws_sc_end_fell" : outcomeId == "mod_siege_ruined" ? "ws_sc_end_ruined" : "ws_sc_end_repelled";
                r.keys.Remove("end"); if (r.lines.Count >= 8) r.lines.RemoveAt(r.lines.Count - 2);
                Line(r, "end", T(endLine).Replace("$1", r.attacker).Replace("$2", r.defender), now);
                if (!r.watched) return;
                last = r; open = false; hint_until = UnityEngine.Time.realtimeSinceStartup + 40.0;
            }
            catch (System.Exception e) { WallBuilder.Log("scroll end failed: " + e.Message); }
        }

        private static System.Reflection.PropertyInfo pWfInst, pWfFp;
        private static System.Reflection.FieldInfo fWfView, fVwX, fVwY;
        private static bool wf_probe;
        // Включён ли 3D-режим Worldfall (его камера - вид от первого лица или полёта)
        internal static bool Is3D()
        {
            try
            {
                if (!wf_probe)
                {
                    wf_probe = true;
                    System.Type wb = WfT.Get("FirstPerson.WorldBoxMod");
                    if (wb != null)
                    {
                        pWfInst = AccessTools.Property(wb, "Instance"); pWfFp = AccessTools.Property(wb, "IsFirstPerson");
                        fWfView = AccessTools.Field(wb, "_view");
                        if (fWfView != null) { fVwX = AccessTools.Field(fWfView.FieldType, "X"); fVwY = AccessTools.Field(fWfView.FieldType, "Y"); }
                    }
                }
                if (pWfInst == null || pWfFp == null) return false;
                object inst = pWfInst.GetValue(null, null);
                return inst != null && (bool)pWfFp.GetValue(inst, null);
            }
            catch (System.Exception) { return false; }
        }

        private static bool CamNear(WorldTile at)
        {
            try
            {
                if (at == null || fWfView == null || fVwX == null || fVwY == null) return false;
                object inst = pWfInst.GetValue(null, null);
                object v = fWfView.GetValue(inst);
                float x = System.Convert.ToSingle(fVwX.GetValue(v)), y = System.Convert.ToSingle(fVwY.GetValue(v));
                return Toolbox.Dist(x, y, at.x, at.y) < NEAR;
            }
            catch (System.Exception) { return false; }
        }
        private const float NEAR = 90f;

        private static System.Reflection.FieldInfo fUnitMain;
        private static bool PlayerNear(WorldTile at)
        {
            if (at == null) return false;
            try
            {
                if (CamNear(at)) return true;
                if (fUnitMain == null) fUnitMain = AccessTools.Field(typeof(ControllableUnit), "_unit_main");
                Actor me = fUnitMain == null ? null : fUnitMain.GetValue(null) as Actor;
                if (me != null && me.isAlive() && Toolbox.Dist(me.current_position.x, me.current_position.y, at.x, at.y) < NEAR) return true;
            }
            catch (System.Exception) { }
            return false;
        }

        public static void Tick()
        {
            if (!ON || World.world == null) return;
            try
            {
                if (ui == null) { ui = new UnityEngine.GameObject("ws_scroll_ui"); UnityEngine.Object.DontDestroyOnLoad(ui); ui.AddComponent<Gui>(); ui.AddComponent<GateBarGui>(); ui.AddComponent<GunHintGui>(); }
                float rt = UnityEngine.Time.realtimeSinceStartup;
                if (rt >= next_watch && active.Count > 0)
                {
                    next_watch = rt + 1.5f;
                    if (Is3D())
                        foreach (KeyValuePair<City, Rec> kv in active)
                            if (!kv.Value.watched && kv.Key != null && kv.Key.isAlive() && (PlayerNear(kv.Key.getTile()) || PlayerNear(kv.Value.site))) kv.Value.watched = true;
                }
                if (open && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Escape)) open = false;
                else if (open && UnityEngine.Input.GetKeyDown(KEY)) open = false;
                else if (!open && last != null && rt < hint_until && Is3D() && UnityEngine.Input.GetKeyDown(KEY)) { open = true; open_t0 = rt; hint_until = 0; }
            }
            catch (System.Exception) { }
        }

        private class Gui : UnityEngine.MonoBehaviour
        {
            private UnityEngine.Texture2D paper, seal;
            private UnityEngine.GUIStyle st;

            private void Make()
            {
                paper = new UnityEngine.Texture2D(128, 128, UnityEngine.TextureFormat.RGBA32, false);
                System.Random rnd = new System.Random(7);
                for (int y = 0; y < 128; y++)
                    for (int x = 0; x < 128; x++)
                    {
                        float n = (float)rnd.NextDouble() * 0.06f + 0.03f * UnityEngine.Mathf.Sin(x * 0.13f + y * 0.07f);
                        paper.SetPixel(x, y, new UnityEngine.Color(0.89f - n, 0.80f - n, 0.63f - n, 1f));
                    }
                paper.Apply(); paper.filterMode = UnityEngine.FilterMode.Bilinear; paper.wrapMode = UnityEngine.TextureWrapMode.Repeat;
                seal = new UnityEngine.Texture2D(64, 64, UnityEngine.TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        float d = UnityEngine.Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f));
                        float a = d < 30f ? 1f : 0f;
                        float ring = (d > 22f && d < 24f) ? 0.25f : 0f;
                        seal.SetPixel(x, y, new UnityEngine.Color(0.56f + ring, 0.13f + ring * 0.5f, 0.11f + ring * 0.5f, a));
                    }
                seal.Apply(); seal.filterMode = UnityEngine.FilterMode.Bilinear;
            }

            private void Box(UnityEngine.Rect r, UnityEngine.Color c) { UnityEngine.GUI.color = c; UnityEngine.GUI.DrawTexture(r, UnityEngine.Texture2D.whiteTexture); UnityEngine.GUI.color = UnityEngine.Color.white; }

            // Замок на печати: корпус, дужка и замочная скважина
            private void Lock(float cx, float cy, float sz, UnityEngine.Color c, UnityEngine.Color dark)
            {
                float bw = sz * 0.78f, bh = sz * 0.56f, t = sz * 0.13f;
                Box(new UnityEngine.Rect(cx - bw / 2f, cy - bh * 0.15f, bw, bh), c);
                Box(new UnityEngine.Rect(cx - bw * 0.33f, cy - bh * 0.15f - sz * 0.38f, t, sz * 0.38f), c);
                Box(new UnityEngine.Rect(cx + bw * 0.33f - t, cy - bh * 0.15f - sz * 0.38f, t, sz * 0.38f), c);
                Box(new UnityEngine.Rect(cx - bw * 0.33f, cy - bh * 0.15f - sz * 0.38f, bw * 0.66f, t), c);
                Box(new UnityEngine.Rect(cx - t * 0.6f, cy + bh * 0.1f, t * 1.2f, t * 1.2f), dark);
                Box(new UnityEngine.Rect(cx - t * 0.35f, cy + bh * 0.1f + t, t * 0.7f, bh * 0.28f), dark);
            }

            private void Txt(UnityEngine.Rect r, string s, int size, UnityEngine.Color c, UnityEngine.TextAnchor a, bool bold = false, bool wrap = false)
            {
                if (st == null) st = new UnityEngine.GUIStyle(UnityEngine.GUI.skin.label);
                st.fontSize = size; st.alignment = a; st.fontStyle = bold ? UnityEngine.FontStyle.Bold : UnityEngine.FontStyle.Normal; st.wordWrap = wrap; st.normal.textColor = c;
                UnityEngine.GUI.Label(r, s, st);
            }

            void OnGUI()
            {
                if (!ON) return;
                try
                {
                    float W = UnityEngine.Screen.width, H = UnityEngine.Screen.height;
                    UnityEngine.GUI.depth = -32001;
                    if (last == null) return;
                    float rt = UnityEngine.Time.realtimeSinceStartup;
                    if (!open)
                    {
                        if (rt >= hint_until || !Is3D()) return;
                        if (paper == null) Make();
                        string h = T("ws_sc_hint").Replace("$1", "").Trim();
                        float k0 = UnityEngine.Mathf.Clamp(H / 900f, 0.7f, 1.5f);
                        float age = (float)(hint_until - rt);
                        float al = UnityEngine.Mathf.Clamp01(UnityEngine.Mathf.Min((40f - age) * 2.5f, age * 1.0f));
                        float slide = (1f - UnityEngine.Mathf.Clamp01((40f - age) * 2.5f)) * 30f * k0;
                        float bw = 520f * k0, bh = 92f * k0, bx = (W - bw) / 2f, by = H - 170f * k0 + slide;
                        UnityEngine.GUI.color = new UnityEngine.Color(1, 1, 1, al);
                        UnityEngine.GUI.DrawTextureWithTexCoords(new UnityEngine.Rect(bx, by, bw, bh), paper, new UnityEngine.Rect(0, 0, 3, 0.6f));
                        UnityEngine.Color ink2 = new UnityEngine.Color(0.23f, 0.13f, 0.08f, al), wd = new UnityEngine.Color(0.30f, 0.18f, 0.09f, al);
                        Box(new UnityEngine.Rect(bx, by, bw, 3f), ink2); Box(new UnityEngine.Rect(bx, by + bh - 3f, bw, 3f), ink2);
                        Box(new UnityEngine.Rect(bx - 14f * k0, by - 6f * k0, 16f * k0, bh + 12f * k0), wd); Box(new UnityEngine.Rect(bx + bw - 2f * k0, by - 6f * k0, 16f * k0, bh + 12f * k0), wd);
                        UnityEngine.GUI.color = new UnityEngine.Color(1, 1, 1, al);
                        UnityEngine.GUI.DrawTexture(new UnityEngine.Rect(bx + 22f * k0, by + 14f * k0, 64f * k0, 64f * k0), seal);
                        UnityEngine.GUI.color = UnityEngine.Color.white;
                        Lock(bx + 54f * k0, by + 46f * k0, 34f * k0, new UnityEngine.Color(0.96f, 0.84f, 0.72f, al), new UnityEngine.Color(0.45f, 0.10f, 0.09f, al));
                        Txt(new UnityEngine.Rect(bx + 100f * k0, by + 12f * k0, bw - 110f * k0, 30f * k0), T("ws_sc_title"), (int)(15 * k0), new UnityEngine.Color(0.55f, 0.14f, 0.12f, al), UnityEngine.TextAnchor.MiddleLeft, true);
                        Txt(new UnityEngine.Rect(bx + 100f * k0, by + 38f * k0, bw - 110f * k0, 44f * k0), h, (int)(22 * k0), ink2, UnityEngine.TextAnchor.MiddleLeft, true);
                        float tw = st.CalcSize(new UnityEngine.GUIContent(h)).x;
                        float kx = bx + 100f * k0 + tw + 14f * k0, ky = by + 44f * k0, ks = 32f * k0;
                        Box(new UnityEngine.Rect(kx, ky, ks, ks), new UnityEngine.Color(0.17f, 0.10f, 0.06f, al));
                        Box(new UnityEngine.Rect(kx + 2f * k0, ky + 2f * k0, ks - 4f * k0, ks - 5f * k0), new UnityEngine.Color(0.94f, 0.85f, 0.66f, al));
                        Txt(new UnityEngine.Rect(kx, ky - 1f * k0, ks, ks), KEY.ToString(), (int)(20 * k0), ink2, UnityEngine.TextAnchor.MiddleCenter, true);
                        return;
                    }
                    if (paper == null) Make();
                    Rec r = last;
                    float k = UnityEngine.Mathf.Clamp(H / 900f, 0.6f, 1.6f);
                    float pw = 780f * k, ph = 740f * k, px = (W - pw) / 2f, py = (H - ph) / 2f;
                    float ap = UnityEngine.Mathf.Clamp01((rt - open_t0) / 0.55f); ap = ap * ap * (3f - 2f * ap);
                    Box(new UnityEngine.Rect(0, 0, W, H), new UnityEngine.Color(0, 0, 0, 0.45f * ap));
                    if (ap < 1f)
                    {
                        // свиток раскрывается: валики расходятся от середины
                        float phf = ph * ap + 4f, pyf = (H - phf) / 2f;
                        UnityEngine.GUI.DrawTextureWithTexCoords(new UnityEngine.Rect(px, pyf, pw, phf), paper, new UnityEngine.Rect(0, 0, 4, 4f * ap));
                        UnityEngine.Color wo = new UnityEngine.Color(0.47f, 0.30f, 0.16f, 1f), wd2 = new UnityEngine.Color(0.24f, 0.14f, 0.07f, 1f);
                        foreach (float yy in new float[] { pyf - 22f * k, pyf + phf - 8f * k })
                        {
                            Box(new UnityEngine.Rect(px - 36f * k, yy, pw + 72f * k, 30f * k), wd2);
                            Box(new UnityEngine.Rect(px - 34f * k, yy + 3f * k, pw + 68f * k, 24f * k), wo);
                            Box(new UnityEngine.Rect(px - 50f * k, yy - 3f * k, 22f * k, 36f * k), wd2); Box(new UnityEngine.Rect(px + pw + 28f * k, yy - 3f * k, 22f * k, 36f * k), wd2);
                        }
                        return;
                    }
                    UnityEngine.GUI.contentColor = new UnityEngine.Color(1, 1, 1, UnityEngine.Mathf.Clamp01((rt - open_t0 - 0.55f) / 0.3f));
                    UnityEngine.GUI.DrawTextureWithTexCoords(new UnityEngine.Rect(px, py, pw, ph), paper, new UnityEngine.Rect(0, 0, 4, 4));
                    UnityEngine.Color edge = new UnityEngine.Color(0.38f, 0.23f, 0.10f, 0.35f);
                    for (int i = 0; i < 5; i++) { float d = i * 5f * k; Box(new UnityEngine.Rect(px, py + d, pw, 5f * k), new UnityEngine.Color(edge.r, edge.g, edge.b, 0.3f - i * 0.05f)); Box(new UnityEngine.Rect(px, py + ph - d - 5f * k, pw, 5f * k), new UnityEngine.Color(edge.r, edge.g, edge.b, 0.3f - i * 0.05f)); Box(new UnityEngine.Rect(px + d, py, 5f * k, ph), new UnityEngine.Color(edge.r, edge.g, edge.b, 0.3f - i * 0.05f)); Box(new UnityEngine.Rect(px + pw - d - 5f * k, py, 5f * k, ph), new UnityEngine.Color(edge.r, edge.g, edge.b, 0.3f - i * 0.05f)); }
                    UnityEngine.Color wood = new UnityEngine.Color(0.47f, 0.30f, 0.16f, 1f), woodD = new UnityEngine.Color(0.24f, 0.14f, 0.07f, 1f);
                    foreach (float yy in new float[] { py - 22f * k, py + ph - 8f * k })
                    {
                        Box(new UnityEngine.Rect(px - 36f * k, yy, pw + 72f * k, 30f * k), woodD);
                        Box(new UnityEngine.Rect(px - 34f * k, yy + 3f * k, pw + 68f * k, 24f * k), wood);
                        Box(new UnityEngine.Rect(px - 50f * k, yy - 3f * k, 22f * k, 36f * k), woodD); Box(new UnityEngine.Rect(px + pw + 28f * k, yy - 3f * k, 22f * k, 36f * k), woodD);
                    }
                    UnityEngine.Color ink = new UnityEngine.Color(0.23f, 0.13f, 0.08f, 1f), red = new UnityEngine.Color(0.55f, 0.14f, 0.12f, 1f);
                    float m = 40f * k;
                    Box(new UnityEngine.Rect(px + m, py + 52f * k, pw - 2 * m, 2f), ink); Box(new UnityEngine.Rect(px + m, py + ph - 52f * k, pw - 2 * m, 2f), ink);
                    Box(new UnityEngine.Rect(px + m, py + 52f * k, 2f, ph - 104f * k), ink); Box(new UnityEngine.Rect(px + pw - m, py + 52f * k, 2f, ph - 104f * k), ink);
                    int fs(float f) { return (int)(f * k); }
                    Txt(new UnityEngine.Rect(px, py + 70f * k, pw, 28f * k), T("ws_sc_title"), fs(19), red, UnityEngine.TextAnchor.MiddleCenter);
                    Txt(new UnityEngine.Rect(px, py + 98f * k, pw, 50f * k), T("ws_sc_head").Replace("$1", r.defender), fs(34), ink, UnityEngine.TextAnchor.MiddleCenter, true);
                    int days = System.Math.Max(1, (int)((r.t1 - r.t0) / System.Math.Max(0.2, DayLen())));
                    Txt(new UnityEngine.Rect(px, py + 150f * k, pw, 26f * k), T("ws_sc_when").Replace("$1", r.mon0 ?? "").Replace("$2", r.year0.ToString()).Replace("$3", r.mon_n < 1 ? T("ws_sc_lt_month") : T("ws_sc_months").Replace("$1", r.mon_n.ToString())), fs(17), ink, UnityEngine.TextAnchor.MiddleCenter);
                    Box(new UnityEngine.Rect(px + 120f * k, py + 186f * k, pw - 240f * k, 2f), ink);
                    float cx1 = px + m + 20f * k, cx2 = px + pw / 2f + 20f * k, cw = pw / 2f - m - 30f * k, y = py + 200f * k;
                    Txt(new UnityEngine.Rect(cx1, y, cw, 26f * k), T("ws_sc_att"), fs(18), ink, UnityEngine.TextAnchor.MiddleLeft, true);
                    Txt(new UnityEngine.Rect(cx2, y, cw, 26f * k), T("ws_sc_def"), fs(18), ink, UnityEngine.TextAnchor.MiddleLeft, true);
                    y += 30f * k;
                    string[] a = new string[] { r.attacker, T("ws_sc_army").Replace("$1", r.army0.ToString()).Replace("$2", r.army1.ToString()), T("ws_sc_guns_n").Replace("$1", r.guns.ToString()), T("ws_sc_ram_n").Replace("$1", r.ram ? T("ws_sc_yes") : T("ws_sc_no")) };
                    string[] b = new string[] { r.defender, T("ws_sc_army").Replace("$1", r.def0.ToString()).Replace("$2", r.def1.ToString()), r.wall ? T("ws_sc_wall_n") : T("ws_sc_wall_ok"), r.gate ? T("ws_sc_gate_n") : T("ws_sc_gate_ok") };
                    for (int i = 0; i < 4; i++)
                    {
                        Txt(new UnityEngine.Rect(cx1, y, cw, 24f * k), a[i], fs(16), ink, UnityEngine.TextAnchor.MiddleLeft);
                        Txt(new UnityEngine.Rect(cx2, y, cw, 24f * k), b[i], fs(16), ink, UnityEngine.TextAnchor.MiddleLeft);
                        y += 27f * k;
                    }
                    y += 8f * k; Box(new UnityEngine.Rect(px + m + 20f * k, y, pw - 2 * m - 40f * k, 1f), ink); y += 12f * k;
                    Txt(new UnityEngine.Rect(cx1, y, cw * 2, 26f * k), T("ws_sc_chron"), fs(18), ink, UnityEngine.TextAnchor.MiddleLeft, true);
                    y += 32f * k;
                    for (int i = 0; i < r.lines.Count; i++)
                    {
                        Txt(new UnityEngine.Rect(cx1, y, pw - 2 * m - 190f * k, 26f * k), r.lines[i], fs(15), ink, UnityEngine.TextAnchor.MiddleLeft);
                        y += 26f * k;
                    }
                    float sx = px + pw - m - 120f * k, sy = py + ph - 52f * k - 130f * k;
                    UnityEngine.GUI.DrawTexture(new UnityEngine.Rect(sx - 52f * k, sy - 52f * k, 104f * k, 104f * k), seal);
                    Txt(new UnityEngine.Rect(sx - 52f * k, sy - 14f * k, 104f * k, 28f * k), r.outcome ?? "", fs(17), new UnityEngine.Color(0.95f, 0.8f, 0.7f, 1f), UnityEngine.TextAnchor.MiddleCenter, true);
                    Txt(new UnityEngine.Rect(px + m + 20f * k, py + ph - 52f * k - 34f * k, 300f * k, 24f * k), T("ws_sc_close").Replace("$1", KEY.ToString()), fs(14), new UnityEngine.Color(0.4f, 0.27f, 0.15f, 1f), UnityEngine.TextAnchor.MiddleLeft);
                    UnityEngine.GUI.contentColor = UnityEngine.Color.white;
                }
                catch (System.Exception) { }
            }
        }
    }
}

namespace HelloWorld
{
    // Снаряды орудий в 3D: Worldfall рисует настоящие 3D-ядра и камни только для снарядов с id cannonball и rock.
    // На время отрисовки подменяем снаряду ассет на соответствующий ванильный, потом возвращаем обратно.
    internal static class ShellModels
    {
        private static bool ON = Cfg.Bool("shell_models", true, "снаряды орудий в 3D-режиме рисуются настоящими 3D-моделями Worldfall (ядро, камень), а не плоской картинкой");
        private static ProjectileAsset ball, rock;
        private static int logged;

        public static void Install(Harmony h)
        {
            if (!ON) return;
            System.Type sc = WfT.Get("FirstPerson.SceneCollector");
            if (sc == null) return;
            int n = 0;
            foreach (string name in new string[] { "DrawCannonShot", "DrawThrown" })
            {
                System.Reflection.MethodInfo m = AccessTools.Method(sc, name);
                if (m == null) continue;
                h.Patch(m, prefix: new HarmonyMethod(typeof(ShellModels), nameof(Pre)), finalizer: new HarmonyMethod(typeof(ShellModels), nameof(Fin)));
                n++;
            }
            WallBuilder.Log("[shells] 3D shell models: " + n + " hook(s)");
        }

        public static void Pre(Projectile __0, ref ProjectileAsset __state)
        {
            __state = null;
            try
            {
                if (__0 == null || __0.asset == null) return;
                string id = __0.asset.id;
                ProjectileAsset to = null;
                if (id == "siege_cannonball" || id == "siege_mortar_shell") { if (ball == null) ball = AssetManager.projectiles.get("cannonball"); to = ball; }
                else if (id == "siege_rock") { if (rock == null) rock = AssetManager.projectiles.get("rock"); to = rock; }
                if (to == null) return;
                __state = __0.asset; __0.asset = to;
                if (logged++ < 4) WallBuilder.Log("[shells] " + id + " drawn as " + to.id);
            }
            catch (System.Exception) { __state = null; }
        }

        public static System.Exception Fin(Projectile __0, ProjectileAsset __state, System.Exception __exception)
        {
            try { if (__state != null && __0 != null) __0.asset = __state; } catch (System.Exception) { }
            return __exception;
        }
    }
}

namespace HelloWorld
{
    // Цвет королевства на флагах, знамёнах, палатках и орудиях: красные тканевые слоты модели заменяются цветом королевства-владельца
    internal static class KingdomTint
    {
        private static bool ON = Cfg.Bool("kingdom_colors", true, "флаги, знамёна, палатки и орудия в 3D окрашиваются в цвет своего королевства (Worldfall)");
        private static System.Reflection.FieldInfo fPal, fSlotColor, fSlotKind, fSlotNames;
        private static readonly Dictionary<long, uint[]> cache = new Dictionary<long, uint[]>();
        private static readonly Dictionary<long, uint> cache_col = new Dictionary<long, uint>();
        private static int logged;

        private static System.Reflection.FieldInfo fSway, fSwayTop;
        private static readonly float WAVE = Cfg.Float("flag_wave", 0.5f, "колыхание флагов и знамён на ветру в 3D (0 — выключить)");
        private static void Wave(object inst, string id)
        {
            if (WAVE <= 0f || (id != "city_flag" && id != "city_banner" && id != "city_banner_small" && id != "tent")) return;
            if (fSway == null) { fSway = AccessTools.Field(inst.GetType(), "Sway"); fSwayTop = AccessTools.Field(inst.GetType(), "SwayInvTop"); }
            if (fSway == null) return;
            float cur = (float)fSway.GetValue(inst);
            if (cur > 0.0001f) return;
            fSway.SetValue(inst, WAVE * 0.1f);
            if (fSwayTop != null) fSwayTop.SetValue(inst, 0.25f);
        }

        public static void Install(Harmony h, System.Reflection.MethodInfo fx, System.Type mi, System.Type mm)
        {
            if (!ON) return;
            fPal = AccessTools.Field(mi, "Palette"); fSlotColor = AccessTools.Field(mm, "SlotColor"); fSlotKind = AccessTools.Field(mm, "SlotKind"); fSlotNames = AccessTools.Field(mm, "SlotNames");
            if (fPal == null || fSlotColor == null) { WallBuilder.Log("[tint] palette API not found: kingdom colours are off"); return; }
            h.Patch(fx, postfix: new HarmonyMethod(typeof(KingdomTint), nameof(Post)));
            WallBuilder.Log("[tint] ready");
        }

        private static bool Wanted(string id)
        {
            return id == Siege.TENT_ID || id == Siege.CATAPULT_ID || id == Siege.TREBUCHET_ID || id == Siege.MORTAR_ID
                || id == Siege.FLAG_ID || id == Siege.BANNER_ID || id == Siege.BANNER_S_ID || id == WallBuilder.CHEAP_TOWER_ID;
        }

        public static void Post(Building __1, object __2, object __3)
        {
            try
            {
                if (__1 == null || __2 == null || __3 == null || __1.asset == null || !Wanted(__1.asset.id) || __1.kingdom == null) return;
                Wave(__2, __1.asset.id);
                long key = __1.getID();
                UnityEngine.Color32 kc = __1.kingdom.getColor().getColorMain32();
                uint packed = (uint)(kc.r | (kc.g << 8) | (kc.b << 16));
                uint[] pal;
                uint was;
                if (cache.TryGetValue(key, out pal) && cache_col.TryGetValue(key, out was) && was == packed) { fPal.SetValue(__2, pal); return; }
                uint[] src = (uint[])fSlotColor.GetValue(__3);
                byte[] kind = fSlotKind == null ? null : (byte[])fSlotKind.GetValue(__3);
                if (src == null) return;
                pal = (uint[])src.Clone();
                string[] nm0 = fSlotNames == null ? null : (string[])fSlotNames.GetValue(__3);
                int n = 0;
                for (int i = 0; i < pal.Length; i++)
                {
                    bool named = nm0 != null && i < nm0.Length && nm0[i] != null && nm0[i].StartsWith("cloth");
                    if (!named && kind != null && i < kind.Length && kind[i] != 7) continue;          // только ткань
                    uint c = pal[i];
                    int b0 = (int)(c & 255), b1 = (int)((c >> 8) & 255), b2 = (int)((c >> 16) & 255);
                    bool rgba = b0 > 90 && b0 > b1 * 2.2f && b0 > b2 * 2.2f;                // красный в младшем байте
                    bool bgra = b2 > 90 && b2 > b1 * 2.2f && b2 > b0 * 2.2f;
                    if (!rgba && !bgra) continue;
                    float k = UnityEngine.Mathf.Clamp((rgba ? b0 : b2) / 196f, 0.55f, 1f);
                    int r = (int)(kc.r * k), g = (int)(kc.g * k), b = (int)(kc.b * k);
                    uint alpha = c & 0xFF000000u;
                    pal[i] = rgba ? (alpha | (uint)(r | (g << 8) | (b << 16))) : (alpha | (uint)(b | (g << 8) | (r << 16)));
                    n++;
                }
                cache[key] = pal; cache_col[key] = packed;
                if (n == 0 && logged < 12)
                {
                    string[] nm = fSlotNames == null ? null : (string[])fSlotNames.GetValue(__3);
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int i = 0; i < pal.Length; i++) sb.Append(i).Append(':').Append(nm != null && i < nm.Length ? nm[i] : "?").Append(" kind ").Append(kind != null && i < kind.Length ? kind[i].ToString() : "?").Append(" #").Append(pal[i].ToString("X8")).Append("; ");
                    WallBuilder.Log("[tint] slots of " + __1.asset.id + ": " + sb);
                }
                if (logged++ < 10) WallBuilder.Log("[tint] " + __1.asset.id + ": " + n + " cloth slot(s) of " + pal.Length + " recoloured to " + kc.r + "," + kc.g + "," + kc.b);
                fPal.SetValue(__2, pal);
            }
            catch (System.Exception) { }
        }
    }
}

namespace HelloWorld
{
    // Анимация осадных орудий в 3D-режиме Worldfall: плечо катапульты и требушета, противовес и праща, откат ствола мортиры.
    // Позы пишутся поверх модели в SceneCollector.BuildingEffects (модель делится на части frame/arm/weight/sling/barrel)
    internal static class GunAnim3D
    {
        private static bool ON = Cfg.Bool("gun_animation", true, "анимация орудий в 3D (Worldfall): взмах плеча, противовес, праща, откат мортиры");
        private static System.Reflection.MethodInfo mEnsure, mPartIndex, mRot;
        private static System.Reflection.FieldInfo fRot, fStatic, fFade;
        private static System.Type matT;
        private static System.Reflection.FieldInfo fPos, fBasis, fVx, fVy, fC0;
        private static System.Reflection.MethodInfo mRotZ, mMul;
        private static int face_logged;
        private static readonly Dictionary<object, int[]> parts = new Dictionary<object, int[]>();
        private static int logged;

        public static void Install(Harmony h)
        {
            if (!ON) return;
            System.Type sc = WfT.Get("FirstPerson.SceneCollector");
            System.Type mi = WfT.Get("FirstPerson.Core.MeshInstance");
            System.Type mm = WfT.Get("FirstPerson.Core.MeshModel");
            matT = WfT.Get("FirstPerson.Core.Mat3");
            System.Reflection.MethodInfo fx = sc == null ? null : AccessTools.Method(sc, "BuildingEffects");
            if (fx == null || mi == null || mm == null || matT == null) { WallBuilder.Log("[anim3d] Worldfall API is different (BuildingEffects " + (fx != null) + "): gun animation is off"); return; }
            mEnsure = AccessTools.Method(mi, "EnsurePose"); fRot = AccessTools.Field(mi, "PartRotation"); fStatic = AccessTools.Field(mi, "Static"); fFade = AccessTools.Field(mi, "Fade");
            mPartIndex = AccessTools.Method(mm, "PartIndex"); mRot = AccessTools.Method(matT, "RotY", new System.Type[] { typeof(float) });
            try
            {
                System.Type v3 = WfT.Get("FirstPerson.Core.Vec3");
                fPos = AccessTools.Field(mi, "Position"); fBasis = AccessTools.Field(mi, "Basis");
                fVx = AccessTools.Field(v3, "X"); fVy = AccessTools.Field(v3, "Y"); fC0 = AccessTools.Field(matT, "C0");
                mRotZ = AccessTools.Method(matT, "RotZ", new System.Type[] { typeof(float) });
                mMul = AccessTools.Method(matT, "op_Multiply", new System.Type[] { matT.MakeByRefType(), matT.MakeByRefType() });
            }
            catch (System.Exception) { }
            if (mEnsure == null || fRot == null || mPartIndex == null || mRot == null) { WallBuilder.Log("[anim3d] Worldfall pose API not found: gun animation is off"); return; }
            h.Patch(fx, postfix: new HarmonyMethod(typeof(GunAnim3D), nameof(Post)));
            try { KingdomTint.Install(h, fx, mi, mm); } catch (System.Exception e) { WallBuilder.Log("[tint] install failed: " + e.Message); }
            WallBuilder.Log("[anim3d] ready");
        }

        // поворот вокруг оси файла .glb (Z) на угол deg градусов против часовой; во внутренней системе Worldfall это поворот вокруг Y с обратным знаком
        private static object Rot(float deg)
        {
            object self = mRot.IsStatic ? null : System.Activator.CreateInstance(matT);
            return mRot.Invoke(self, new object[] { -deg * 0.017453292f });
        }

        // Поворачиваем модель орудия к цели (спереди у моделей - плюс X файла .glb); без этого все орудия смотрят в одну сторону света
        private static void Face(Building b, object inst)
        {
            if (fPos == null || fBasis == null || fVx == null || fVy == null || fC0 == null || mRotZ == null || mMul == null) return;
            UnityEngine.Vector2 at;
            if (!Siege.GunFacing(b, out at)) return;
            object pos = fPos.GetValue(inst);
            float px = (float)fVx.GetValue(pos), py = (float)fVy.GetValue(pos);
            object basis = fBasis.GetValue(inst);
            object c0 = fC0.GetValue(basis);
            float cx = (float)fVx.GetValue(c0), cy = (float)fVy.GetValue(c0);
            if (cx * cx + cy * cy < 1e-6f) return;
            float cur = UnityEngine.Mathf.Atan2(cy, cx);
            if (b.asset.id == Siege.MORTAR_ID) cur += 3.14159265f;      // у мортиры дуло смотрит в обратную сторону модели: разворачиваем на 180
            float want = UnityEngine.Mathf.Atan2(at.y - py, at.x - px);
            float d = want - cur;
            while (d > 3.14159265f) d -= 6.2831853f;
            while (d < -3.14159265f) d += 6.2831853f;
            if (face_logged < 8) { face_logged++; WallBuilder.Log("[anim3d] " + b.asset.id + " at " + px.ToString("F1") + "," + py.ToString("F1") + " faces " + (cur * 57.2958f).ToString("F0") + " deg, target " + at.x.ToString("F0") + "," + at.y.ToString("F0") + " -> " + (want * 57.2958f).ToString("F0") + " deg"); }
            if (System.Math.Abs(d) < 0.02f) return;
            object rz = mRotZ.Invoke(System.Activator.CreateInstance(matT), new object[] { d });
            object[] a = new object[] { rz, basis };
            object nb = mMul.Invoke(null, a);
            fBasis.SetValue(inst, nb);
        }

        private static float Smooth(float t) { t = UnityEngine.Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        public static void Post(Building __1, object __2, object __3)
        {
            if (!ON) return;
            try
            {
                if (__1 == null || __2 == null || __3 == null || __1.asset == null) return;
                string id = __1.asset.id;
                int kind = id == Siege.CATAPULT_ID ? 1 : (id == Siege.TREBUCHET_ID ? 2 : (id == Siege.MORTAR_ID ? 3 : 0));
                if (kind == 0) return;
                int[] ix;
                if (!parts.TryGetValue(__3, out ix))
                {
                    ix = new int[4];
                    string[] nm = new string[] { "arm", "weight", "sling", "barrel" };
                    for (int i = 0; i < 4; i++) ix[i] = (int)mPartIndex.Invoke(__3, new object[] { nm[i] });
                    parts[__3] = ix;
                    if (logged++ < 6) WallBuilder.Log("[anim3d] " + id + ": parts arm " + ix[0] + ", weight " + ix[1] + ", sling " + ix[2] + ", barrel " + ix[3]);
                }
                Face(__1, __2);
                if (ManualGun.gunner == __1 && ManualGun.g_view == 0 && !ManualGun.chase_active && fFade != null) { try { fFade.SetValue(__2, 0.3f); } catch (System.Exception) { } }
                double since, reload;
                bool shot = Siege.GunClock(__1, out since, out reload);
                float arm = 0f, swing = 0f, kick = 0f;
                float cocked = kind == 1 ? 75f : 90f;                 // угол заряженного положения плеча
                if (kind == 3)
                {
                    if (shot && since < 0.9) kick = -9f * (float)System.Math.Exp(-since * 5.0) * (float)System.Math.Cos(since * 9.0);
                }
                else
                {
                    float throwT = kind == 1 ? 0.45f : 0.9f;          // время взмаха
                    if (!shot) arm = cocked;
                    else if (since < throwT) { float p = Smooth((float)since / throwT); arm = cocked * (1f - p); swing = (1f - p) * 25f; }
                    else
                    {
                        double rest = reload - throwT - 0.5;           // потом плечо взводится обратно
                        float w = rest <= 0.5 ? 1f : Smooth((float)((since - throwT - 0.3) / rest));
                        arm = cocked * w;
                        if (since < throwT + 1.2) swing = -10f * (float)System.Math.Exp(-(since - throwT) * 3.0) * (float)System.Math.Sin((since - throwT) * 9.0);
                    }
                }
                mEnsure.Invoke(__2, null);
                System.Array rot = fRot.GetValue(__2) as System.Array;
                if (rot == null) return;
                if (fStatic != null) fStatic.SetValue(__2, false);
                if (kind == 3) { if (ix[3] >= 0 && ix[3] < rot.Length) rot.SetValue(Rot(kick), ix[3]); }
                else
                {
                    if (ix[0] >= 0 && ix[0] < rot.Length) rot.SetValue(Rot(arm), ix[0]);
                    if (kind == 2)
                    {
                        if (ix[1] >= 0 && ix[1] < rot.Length) rot.SetValue(Rot(-arm + swing * 0.6f), ix[1]);     // противовес висит вертикально
                        if (ix[2] >= 0 && ix[2] < rot.Length) rot.SetValue(Rot(-arm - swing * 1.4f), ix[2]);     // праща отстаёт от плеча
                    }
                }
            }
            catch (System.Exception e) { WallBuilder.Log("[anim3d] error: " + e.Message); ON = false; }
        }
    }
}

namespace HelloWorld
{
    // Вход в палатки осады через "дома" Worldfall: палатка получает вид "tent" (kind=tent в её модели), комната - как у палаток самого Worldfall
    internal static class TentEnter
    {
        private static bool ON = Cfg.Bool("tent_enter", true, "в палатки осады можно заходить в 3D-режиме Worldfall (клавиша E у входа)");
        private static bool logged_plan;
        private static float next_log;

        public static void Install(Harmony h)
        {
            if (!ON) return;
            System.Type hi = WfT.Get("FirstPerson.HouseInterior");
            if (hi == null) { WallBuilder.Log("[tent] HouseInterior not found: tents cannot be entered"); return; }
            System.Reflection.MethodInfo en = AccessTools.Method(hi, "IsEnterable", new System.Type[] { typeof(Building) });
            System.Reflection.MethodInfo rn = AccessTools.Method(hi, "RoomName", new System.Type[] { typeof(BuildingAsset) });
            if (en == null || rn == null) { WallBuilder.Log("[tent] HouseInterior API is different: tents cannot be entered"); return; }
            h.Patch(en, postfix: new HarmonyMethod(typeof(TentEnter), nameof(EnterablePost)));
            h.Patch(rn, postfix: new HarmonyMethod(typeof(TentEnter), nameof(RoomPost)));
            WallBuilder.Log("[tent] ready");
        }

        public static void EnterablePost(Building __0, ref bool __result)
        {
            try
            {
                if (__result || !ON || __0 == null || __0.asset == null || __0.asset.id != Siege.TENT_ID) return;
                if (!__0.isAlive() || __0.isUnderConstruction() || __0.isRuin()) return;
                __result = true;
            }
            catch (System.Exception) { }
        }

        public static void RoomPost(BuildingAsset __0, ref string __result)
        {
            try { if (ON && __0 != null && __0.id == Siege.TENT_ID) __result = "tent"; } catch (System.Exception) { }
        }

        // Раз в несколько секунд, пока Worldfall рисует палатку, пишем в лог, как он её разметил (вход, размеры)
        public static void Tick()
        {
            if (!ON || logged_plan) return;
            float now = UnityEngine.Time.realtimeSinceStartup;
            if (now < next_log) return;
            next_log = now + 5f;
            try
            {
                System.Type hw = WfT.Get("FirstPerson.HouseWorld");
                if (hw == null || World.world == null) return;
                System.Reflection.MethodInfo near = AccessTools.Method(hw, "Near");
                System.Reflection.MethodInfo planOf = AccessTools.Method(hw, "PlanOf");
                if (near == null || planOf == null) { logged_plan = true; return; }
                System.Collections.IEnumerable list = near.Invoke(null, null) as System.Collections.IEnumerable;
                if (list == null) return;
                foreach (object o in list)
                {
                    Building b = o as Building;
                    if (b == null)
                    {
                        System.Reflection.FieldInfo fb = o.GetType().GetField("Building") ?? o.GetType().GetField("B");
                        if (fb != null) b = fb.GetValue(o) as Building;
                    }
                    if (b == null || b.asset == null || b.asset.id != Siege.TENT_ID) continue;
                    object plan = planOf.Invoke(null, new object[] { b });
                    WallBuilder.Log("[tent] Worldfall sees a siege tent at " + b.current_tile.x + "," + b.current_tile.y + ": plan " + (plan == null ? "none" : plan.ToString()) + ", enterable " + HouseEnterable(b));
                    logged_plan = true;
                    return;
                }
            }
            catch (System.Exception e) { WallBuilder.Log("[tent] diagnostic failed: " + e.Message); logged_plan = true; }
        }

        private static string HouseEnterable(Building b)
        {
            try
            {
                System.Type hi = WfT.Get("FirstPerson.HouseInterior");
                return AccessTools.Method(hi, "IsEnterable", new System.Type[] { typeof(Building) }).Invoke(null, new object[] { b }).ToString();
            }
            catch (System.Exception) { return "?"; }
        }
    }
}

namespace HelloWorld
{
    // Режим наводчика (Worldfall, вид от первого лица): K у орудия - встать к нему; камера встаёт позади орудия,
    // A/D поворачивают орудие влево-вправо, W/S меняют дальность, пунктирная дуга и кольцо показывают, куда упадёт снаряд.
    internal static partial class ManualGun
    {
        private static float GUNNER_BACK = Cfg.Float("gunner_back", 3.2f, "режим наводчика (3D): на сколько клеток камера стоит позади орудия");
        private static float GUNNER_SIDE = Cfg.Float("gunner_side", 0.8f, "режим наводчика (3D): сдвиг камеры вправо от оси ствола, клетки (чтобы дуга выстрела была видна сбоку)");
        private static float GUNNER_PITCH = Cfg.Float("gunner_pitch", -4f, "режим наводчика (3D): наклон камеры, градусы (минус - вниз)");
        private static float GUNNER_FOV_K = Cfg.Float("gunner_fov_scale", 1f, "режим наводчика (3D): поправка масштаба пунктирной дуги на экране (если дуга не совпадает с картинкой)");

        private static System.Reflection.FieldInfo fpActive, fpYaw, fpUnit, fpFrozen, fpBlock, fvView, fvPitchDeg;
        private static System.Reflection.FieldInfo vwX, vwY, vwZ, vwYaw, vwPitch, vwFov;
        private static System.Reflection.PropertyInfo fpInst;
        private static System.Reflection.MethodInfo fpGround, fpPlace;
        private static bool fpTried, fpOk;
        internal static string fp_text;
        internal static Building gunner;
        private static float g_az, g_az0, g_range, g_next_scan;
        private static UnityEngine.Vector2 g_exit;
        private static bool g_block_was;
        private static UnityEngine.GameObject fpHud;
        internal static int g_view;                       // 0 - казуальный, 1 - средний
        internal static bool chase_on, chase_active;
        private static Projectile chase_shell, shot_shell;
        private static UnityEngine.Vector2 shot_from, shot_aim, shot_last; private static float shot_range, shot_t0;
        private static void ShotWatch()
        {
            if (shot_shell == null) return;
            try
            {
                bool fin = shot_shell.isFinished() || shot_shell.isDeadAnimation() || UnityEngine.Time.realtimeSinceStartup > shot_t0 + 20f;
                if (!fin) { UnityEngine.Vector2 p = shot_shell.getCurrentPosition(); if (p.sqrMagnitude > 1f) shot_last = p; return; }
                float fl = UnityEngine.Vector2.Distance(shot_from, shot_last), ex = UnityEngine.Vector2.Distance(shot_from, shot_aim);
                WallBuilder.Log("[gunner] landed at " + shot_last.x.ToString("F1") + "," + shot_last.y.ToString("F1") + ": flew " + fl.ToString("F1") + " tiles, set " + shot_range.ToString("F1") + ", aim distance " + ex.ToString("F1") + ", flight " + (UnityEngine.Time.realtimeSinceStartup - shot_t0).ToString("F1") + " s");
            }
            catch (System.Exception) { }
            shot_shell = null;
        }
        private static UnityEngine.Vector3 chase_pos, chase_prev;
        private static float chase_yaw, chase_start, chase_end;
        private static System.Reflection.FieldInfo fgLanded, fgFloor, fgTake, fgLift;
        private static void Lift(Actor me, float h, UnityEngine.Vector2 at)
        {
            try
            {
                object inst = fpInst.GetValue(null, null);
                if (fgLanded == null)
                {
                    System.Type wb = WfT.Get("FirstPerson.WorldBoxMod");
                    fgLanded = AccessTools.Field(wb, "_landedOn"); fgFloor = AccessTools.Field(wb, "_floorZ"); fgTake = AccessTools.Field(wb, "_takeoffZ"); fgLift = AccessTools.Field(wb, "_standLift");
                }
                if (h <= 0f)
                {
                    me.position_height = 0f;
                    if (fgLanded != null) fgLanded.SetValue(inst, false); if (fgFloor != null) fgFloor.SetValue(inst, float.NaN); if (fgTake != null) fgTake.SetValue(inst, float.NaN); if (fgLift != null) fgLift.SetValue(inst, 0f);
                    return;
                }
                float fz = GroundAtF(at.x, at.y) + h;
                me.position_height = h;
                if (fgLanded != null) fgLanded.SetValue(inst, true); if (fgFloor != null) fgFloor.SetValue(inst, fz); if (fgTake != null) fgTake.SetValue(inst, fz); if (fgLift != null) fgLift.SetValue(inst, 0f);
            }
            catch (System.Exception) { }
        }
        private static System.Reflection.FieldInfo feSet, feXY, feH;
        private static object eyeInst;
        // Камеру переносим через отладочную точку глаза Worldfall: DebugEye - место (x, y), DebugEyeHeight - высота над землёй
        private static void SetEye(bool on, UnityEngine.Vector2 at, float h)
        {
            try
            {
                object inst = eyeInst ?? fpInst.GetValue(null, null);
                if (inst == null) return;
                if (feSet == null)
                {
                    System.Type wb = WfT.Get("FirstPerson.WorldBoxMod");
                    feSet = AccessTools.Field(wb, "DebugEyeSet"); feXY = AccessTools.Field(wb, "DebugEye"); feH = AccessTools.Field(wb, "DebugEyeHeight");
                    if (feSet == null || feXY == null || feH == null) { WallBuilder.Log("[gunner] DebugEye fields not found: raised camera is off"); feSet = null; return; }
                }
                if (on) { feXY.SetValue(inst, at); feH.SetValue(inst, h); }
                feSet.SetValue(inst, on);
            }
            catch (System.Exception) { }
        }
        private static float ViewBack() { return g_view == 0 ? GUNNER_BACK : GUNNER_BACK + 1.6f; }
        private static float ViewRise() { return g_view == 0 ? 1.9f : 5.5f; }
        private static float ViewPitch() { return g_view == 0 ? GUNNER_PITCH - 6f : -20f; }

        public static void FpInstall(Harmony h)
        {
            if (!ON) return;
            System.Type wb = WfT.Get("FirstPerson.WorldBoxMod");
            System.Reflection.MethodInfo um = wb == null ? null : AccessTools.Method(wb, "UpdateMod");
            if (um == null) { WallBuilder.Log("[gunner] WorldBoxMod.UpdateMod not found: gunner camera is off"); return; }
            h.Patch(um, postfix: new HarmonyMethod(typeof(ManualGun), nameof(CamPost)));
            WallBuilder.Log("[gunner] camera hook ready");
            try
            {
                System.Reflection.MethodInfo bh = AccessTools.Method(wb, "BuildHands");
                if (bh != null) { h.Patch(bh, prefix: new HarmonyMethod(typeof(ManualGun), nameof(HandsPre))); WallBuilder.Log("[gunner] hands hook ready"); }
            }
            catch (System.Exception e) { WallBuilder.Log("[gunner] hands hook failed: " + e.Message); }
        }
        public static bool HandsPre() { return !(chase_active || (gunner != null && g_view == 1)); }       // нет рук и тела: в полёте за снарядом и в среднем виде над землёй       // камера летит за снарядом: рук и тела от первого лица нет

        // После обычного обновления Worldfall держим камеру по оси орудия (мышь не крутит вид, пока игрок у орудия)
        public static void CamPost(object __instance)
        {
            if (gunner == null || !fpOk) return;
            try
            {
                object v = fvView.GetValue(__instance);
                if (chase_active)
                {
                    float now = UnityEngine.Time.realtimeSinceStartup;
                    bool fin = true;
                    if (chase_shell != null) { try { fin = chase_shell.isFinished() || chase_shell.isDeadAnimation(); } catch (System.Exception) { fin = true; } }
                    if (!fin)
                    {
                        UnityEngine.Vector2 p = chase_shell.getCurrentPosition();
                        float h = chase_shell.getCurrentHeight();
                        chase_prev = chase_pos; chase_pos = new UnityEngine.Vector3(p.x, p.y, h);
                        UnityEngine.Vector2 dv = new UnityEngine.Vector2(chase_pos.x - chase_prev.x, chase_pos.y - chase_prev.y);
                        if (dv.sqrMagnitude > 1e-6f) chase_yaw = UnityEngine.Mathf.LerpAngle(chase_yaw * 57.29578f, UnityEngine.Mathf.Atan2(dv.y, dv.x) * 57.29578f, 0.15f) * 0.017453292f;
                        chase_end = now + 1.0f;
                    }
                    if (now > chase_end || now > chase_start + 14f) chase_active = false;
                    else
                    {
                        float gz = GroundAtF(chase_pos.x, chase_pos.y);
                        float cb = 3.2f;
                        vwX.SetValue(v, chase_pos.x - UnityEngine.Mathf.Cos(chase_yaw) * cb);
                        vwY.SetValue(v, chase_pos.y - UnityEngine.Mathf.Sin(chase_yaw) * cb);
                        SetEye(true, new UnityEngine.Vector2(chase_pos.x - UnityEngine.Mathf.Cos(chase_yaw) * cb, chase_pos.y - UnityEngine.Mathf.Sin(chase_yaw) * cb), UnityEngine.Mathf.Max(0.4f, chase_pos.z + 1.4f));
                        vwYaw.SetValue(v, chase_yaw);
                        vwPitch.SetValue(v, UnityEngine.Mathf.Sin(-12f * 0.017453292f));
                        fvView.SetValue(__instance, v);
                        return;
                    }
                }
                float pd = ViewPitch();
                { UnityEngine.Vector2 bp = UnityEngine.Vector2.zero; Actor um = fpUnit.GetValue(null) as Actor; if (um != null) bp = um.current_position; SetEye(true, bp, ViewRise()); }
                vwYaw.SetValue(v, g_az);
                vwPitch.SetValue(v, UnityEngine.Mathf.Sin(pd * 0.017453292f));
                fvView.SetValue(__instance, v);
                if (fvPitchDeg != null) fvPitchDeg.SetValue(__instance, pd);
                fpYaw.SetValue(null, g_az);
            }
            catch (System.Exception) { }
        }

        private static bool FpInit()
        {
            if (fpTried) return fpOk;
            System.Type ph = WfT.Get("FirstPerson.PossessionHooks");
            System.Type wb = WfT.Get("FirstPerson.WorldBoxMod");
            if (ph == null || wb == null) return false;             // Worldfall ещё не загружен
            fpTried = true;
            try
            {
                fpActive = AccessTools.Field(ph, "Active"); fpYaw = AccessTools.Field(ph, "Yaw");
                fpFrozen = AccessTools.Field(ph, "Frozen"); fpBlock = AccessTools.Field(ph, "BlockAttack");
                fpUnit = AccessTools.Field(typeof(ControllableUnit), "_unit_main");
                fpInst = AccessTools.Property(wb, "Instance");
                fpGround = AccessTools.Method(wb, "GroundAt"); fpPlace = AccessTools.Method(wb, "PlaceBody");
                fvView = AccessTools.Field(wb, "_view"); fvPitchDeg = AccessTools.Field(wb, "_pitchDegrees");
                System.Type vt = fvView == null ? null : fvView.FieldType;
                if (vt != null)
                {
                    vwX = AccessTools.Field(vt, "X"); vwY = AccessTools.Field(vt, "Y"); vwZ = AccessTools.Field(vt, "Z");
                    vwYaw = AccessTools.Field(vt, "Yaw"); vwPitch = AccessTools.Field(vt, "Pitch"); vwFov = AccessTools.Field(vt, "Fov");
                }
                fpOk = fpActive != null && fpYaw != null && fpFrozen != null && fpUnit != null && fpInst != null && fpGround != null && fpPlace != null && fvView != null && vwX != null && vwY != null && vwZ != null && vwYaw != null && vwPitch != null && vwFov != null;
            }
            catch (System.Exception e) { WallBuilder.Log("[gunner] init failed: " + e.Message); fpOk = false; }
            if (!fpOk) WallBuilder.Log("[gunner] Worldfall API is different (Active " + (fpActive != null) + ", Yaw " + (fpYaw != null) + ", Frozen " + (fpFrozen != null) + ", Instance " + (fpInst != null) + ", GroundAt " + (fpGround != null) + ", PlaceBody " + (fpPlace != null) + ", _view " + (fvView != null) + "): first-person fire is off");
            else
            {
                fpHud = new UnityEngine.GameObject("ws_gunner_hud");
                UnityEngine.Object.DontDestroyOnLoad(fpHud);
                fpHud.AddComponent<GunnerHud>();
                WallBuilder.Log("[gunner] ready");
            }
            return fpOk;
        }

        private static float WrapPi(float a) { while (a > 3.14159265f) a -= 6.2831853f; while (a < -3.14159265f) a += 6.2831853f; return a; }

        private static string Compass(float az)
        {
            float bearing = 90f - az * 57.29578f;                      // север - вверх по карте (+Y), восток - +X
            bearing = ((bearing % 360f) + 360f) % 360f;
            string[] n = L10n.Dirs();
            return ((int)System.Math.Round(bearing)) + "° " + n[((int)System.Math.Round(bearing / 45f)) % 8];
        }

        private static float GroundAtF(float x, float y)
        {
            object inst = fpInst.GetValue(null, null);
            if (inst == null) return 0f;
            return (float)fpGround.Invoke(inst, new object[] { new UnityEngine.Vector2(x, y) });
        }

        private static void Place(Actor me, UnityEngine.Vector2 at)
        {
            object inst = fpInst.GetValue(null, null);
            if (inst != null) fpPlace.Invoke(inst, new object[] { me, at });
        }

        private static UnityEngine.Vector2 Seat(Building g, float az)
        {
            UnityEngine.Vector2 gp = new UnityEngine.Vector2(g.current_tile.x, g.current_tile.y);
            UnityEngine.Vector2 dir = new UnityEngine.Vector2(UnityEngine.Mathf.Cos(az), UnityEngine.Mathf.Sin(az));
            UnityEngine.Vector2 right = new UnityEngine.Vector2(dir.y, -dir.x);
            return gp - dir * ViewBack() + right * GUNNER_SIDE;
        }

        internal static UnityEngine.Vector2 GunnerTarget(Building g)
        {
            return new UnityEngine.Vector2(g.current_tile.x + UnityEngine.Mathf.Cos(g_az) * g_range, g.current_tile.y + UnityEngine.Mathf.Sin(g_az) * g_range);
        }

        private static void BeginGunner(Actor me, Building g)
        {
            gunner = g; Siege.manual_gun = g;
            g_exit = me.current_position;
            UnityEngine.Vector2 at;
            if (Siege.GunFacing(g, out at)) g_az0 = UnityEngine.Mathf.Atan2(at.y - g.current_tile.y, at.x - g.current_tile.x);
            else g_az0 = (float)fpYaw.GetValue(null);
            g_az = g_az0; chase_active = false;
            float mn = Siege.GunMinRange(g), mx = Siege.GunMaxRange(g);
            g_range = UnityEngine.Mathf.Clamp(mn + (mx - mn) * 0.5f, mn, mx);
            fpFrozen.SetValue(null, true);
            if (fpBlock != null) { g_block_was = (bool)fpBlock.GetValue(null); fpBlock.SetValue(null, true); }
            Place(me, Seat(g, g_az));
            fp_text = null;
            Tip(L10n.T("Вы у орудия. A/D - влево-вправо, W/S - дальность, ЛКМ - выстрел, R - вид, U - камера за снарядом, K - уйти", "You are at the gun. A/D - turn, W/S - range, LMB - fire, R - view, U - shell camera, K - leave"));
            WallBuilder.Log("[gunner] manned " + g.asset.id + " at " + g.current_tile.x + "," + g.current_tile.y + ", azimuth " + (g_az * 57.2958f).ToString("F0") + ", range " + g_range.ToString("F0"));
        }

        private static void EndGunner(Actor me, bool restore, string msg)
        {
            gunner = null; Siege.manual_gun = null; chase_active = false;
            try { fpFrozen.SetValue(null, false); if (fpBlock != null) fpBlock.SetValue(null, g_block_was); } catch (System.Exception) { }
            SetEye(false, UnityEngine.Vector2.zero, 0f);
            if (restore && me != null && me.isAlive()) Place(me, g_exit);
            if (msg != null) Tip(msg);
        }

        // true - игрок в 3D-режиме, и обычное 2D-управление курсором отключено
        private static bool FpTick()
        {
            if (!FpInit()) return false;
            Actor me = fpUnit.GetValue(null) as Actor;
            if (!(bool)fpActive.GetValue(null))
            {
                fp_text = null;
                if (gunner != null) EndGunner(me, false, null);
                return false;
            }
            if (me == null || !me.isAlive()) { fp_text = null; if (gunner != null) EndGunner(me, false, null); return true; }
            float now = UnityEngine.Time.realtimeSinceStartup;
            if (gunner == null)
            {
                // подсказка у орудия
                if (now >= g_next_scan)
                {
                    g_next_scan = now + 0.25f;
                    fp_text = null;
                    if (me.current_tile != null)
                    {
                        Building best = null; float bd = 1e9f;
                        foreach (Building b in World.world.buildings.getBuildingFromZones(me.current_tile, 8))
                        {
                            if (b == null || b.asset == null || !Siege.IsShooterId(b.asset.id) || !b.isUsable() || b.current_tile == null) continue;
                            float d = Toolbox.Dist(me.current_position.x, me.current_position.y, b.current_tile.x, b.current_tile.y);
                            if (d < bd) { bd = d; best = b; }
                        }
                        if (best != null && bd <= 6f) { string nm = GunName(best); fp_text = L10n.T("[K] Встать к орудию: ", "[K] Man the gun: ") + nm; }
                    }
                }
                if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.K))
                {
                    Building best = null; float bd = 1e9f;
                    foreach (Building b in World.world.buildings.getBuildingFromZones(me.current_tile, 8))
                    {
                        if (b == null || b.asset == null || !Siege.IsShooterId(b.asset.id) || !b.isUsable() || b.current_tile == null) continue;
                        float d = Toolbox.Dist(me.current_position.x, me.current_position.y, b.current_tile.x, b.current_tile.y);
                        if (d < bd) { bd = d; best = b; }
                    }
                    if (best == null || bd > 6f) Tip(L10n.T("Подойдите к орудию (до 6 клеток) и нажмите K", "Go up to a gun (within 6 tiles) and press K"));
                    else BeginGunner(me, best);
                }
                return true;
            }
            ShotWatch();
            Building g = gunner;
            if (!g.isAlive() || !g.isUsable() || g.current_tile == null) { EndGunner(me, true, L10n.T("Орудие разрушено", "The gun is destroyed")); return true; }
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.K) || UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Escape)) { EndGunner(me, true, L10n.T("Орудие снова стреляет само", "The gun fires on its own again")); return true; }
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.R)) { g_view = 1 - g_view; Tip(g_view == 0 ? L10n.T("Вид: казуальный", "View: casual") : L10n.T("Вид: средний (сверху)", "View: overhead")); }
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.U))
            {
                if (chase_active) chase_active = false;
                else { chase_on = !chase_on; Tip(chase_on ? L10n.T("Камера за снарядом: вкл (экспериментально)", "Shell camera: on (experimental)") : L10n.T("Камера за снарядом: выкл", "Shell camera: off")); }
            }
            if (chase_active) return true;
            float dt = UnityEngine.Mathf.Min(UnityEngine.Time.unscaledDeltaTime, 0.1f);
            float fine = (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftShift) || UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightShift)) ? 0.25f : 1f;
            float turn = 0f, dr = 0f;
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.A) || UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftArrow)) turn += 1f;
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.D) || UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightArrow)) turn -= 1f;
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.W) || UnityEngine.Input.GetKey(UnityEngine.KeyCode.UpArrow)) dr += 1f;
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.S) || UnityEngine.Input.GetKey(UnityEngine.KeyCode.DownArrow)) dr -= 1f;
            dr += UnityEngine.Input.mouseScrollDelta.y * 1.2f;
            float minR = Siege.GunMinRange(g), maxR = Siege.GunMaxRange(g);
            g_az += turn * 0.5f * fine * dt;
            g_range = UnityEngine.Mathf.Clamp(g_range + dr * (maxR - minR) * 0.3f * fine * dt, minR, maxR);
            UnityEngine.Vector2 seat = Seat(g, g_az);
            if ((me.current_position - seat).sqrMagnitude > 0.0025f) Place(me, seat);
            UnityEngine.Vector2 tp = GunnerTarget(g);
            double now2 = World.world.getCurWorldTime();
            Siege.gun_aim[g.getID()] = new UnityEngine.Vector3(tp.x, tp.y, (float)now2);
            double wait = Siege.GunReadyIn(g);
            string name = GunName(g); name = name.Substring(0, 1).ToUpper() + name.Substring(1);
            fp_text = name + (wait > 0 ? L10n.T(": перезарядка ", ": reloading ") + (int)System.Math.Ceiling(wait) + L10n.T(" с", " s") : L10n.T(": готово к выстрелу", ": ready to fire"));
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.L) || UnityEngine.Input.GetMouseButtonDown(0))
            {
                if (wait > 0) Tip(L10n.T("Перезарядка: ", "Reloading: ") + (int)System.Math.Ceiling(wait) + L10n.T(" с", " s"));
                else
                {
                    WorldTile aim = World.world.GetTile((int)System.Math.Round(tp.x), (int)System.Math.Round(tp.y));
                    if (aim != null && Siege.ManualFire(g, aim))
                    {
                        shot_shell = Siege.last_shell; shot_from = new UnityEngine.Vector2(g.current_tile.x, g.current_tile.y); shot_aim = tp; shot_range = g_range; shot_last = shot_from; shot_t0 = UnityEngine.Time.realtimeSinceStartup;
                        WallBuilder.Log("[gunner] shot: " + g.asset.id + " azimuth " + (g_az * 57.2958f).ToString("F0") + ", set range " + g_range.ToString("F1") + ", aim " + tp.x.ToString("F1") + "," + tp.y.ToString("F1") + ", gun " + shot_from.x.ToString("F0") + "," + shot_from.y.ToString("F0"));
                        if (chase_on && Siege.last_shell != null)
                        {
                            chase_shell = Siege.last_shell; chase_active = true; chase_start = UnityEngine.Time.realtimeSinceStartup; chase_end = chase_start + 3f;
                            chase_pos = new UnityEngine.Vector3(g.current_tile.x, g.current_tile.y, 1f); chase_prev = chase_pos; chase_yaw = g_az;
                        }
                    }
                }
            }
            return true;
        }

        private class GunnerHud : UnityEngine.MonoBehaviour
        {
            private UnityEngine.GUIStyle st;
            private UnityEngine.Texture2D px;

            private void Label(UnityEngine.Rect r, string s, int size, UnityEngine.Color c)
            {
                if (st == null) { st = new UnityEngine.GUIStyle(UnityEngine.GUI.skin.label); st.alignment = UnityEngine.TextAnchor.MiddleCenter; st.fontStyle = UnityEngine.FontStyle.Bold; }
                st.fontSize = size;
                st.normal.textColor = new UnityEngine.Color(0f, 0f, 0f, 0.85f);
                UnityEngine.GUI.Label(new UnityEngine.Rect(r.x + 2, r.y + 2, r.width, r.height), s, st);
                st.normal.textColor = c;
                UnityEngine.GUI.Label(r, s, st);
            }

            private void Box(float x, float y, float w, float h, UnityEngine.Color c)
            {
                UnityEngine.GUI.color = c;
                UnityEngine.GUI.DrawTexture(new UnityEngine.Rect(x, y, w, h), UnityEngine.Texture2D.whiteTexture);
                UnityEngine.GUI.color = UnityEngine.Color.white;
            }

            void OnGUI()
            {
                string s = ManualGun.fp_text;
                Building g = ManualGun.gunner;
                if (s == null && g == null) return;
                if (ManualGun.chase_active) return;                  // камера летит за снарядом: интерфейс убираем
                UnityEngine.GUI.depth = -32000;
                float W = UnityEngine.Screen.width, H = UnityEngine.Screen.height;
                if (g == null)
                {
                    Prompt.Draw(s);
                    return;
                }
                try { DrawGunner(g, W, H); } catch (System.Exception) { }
            }

            private void DrawGunner(Building g, float W, float H)
            {
                float minR = Siege.GunMinRange(g), maxR = Siege.GunMaxRange(g);
                // --- проекция точек мира на экран по камере Worldfall ---
                object inst = ManualGun.fpInst.GetValue(null, null);
                object v = ManualGun.fvView.GetValue(inst);
                float ex = (float)ManualGun.vwX.GetValue(v), ey = (float)ManualGun.vwY.GetValue(v), ez = (float)ManualGun.vwZ.GetValue(v);
                float yaw = (float)ManualGun.vwYaw.GetValue(v), sp = UnityEngine.Mathf.Clamp((float)ManualGun.vwPitch.GetValue(v), -0.99f, 0.99f), fov = (float)ManualGun.vwFov.GetValue(v);
                if (fov < 0.05f || fov > 3f) fov = 1.2f;
                float cp = UnityEngine.Mathf.Sqrt(1f - sp * sp);
                UnityEngine.Vector3 fwd = new UnityEngine.Vector3(UnityEngine.Mathf.Cos(yaw) * cp, UnityEngine.Mathf.Sin(yaw) * cp, sp);
                UnityEngine.Vector3 right = UnityEngine.Vector3.Cross(fwd, UnityEngine.Vector3.forward);      // мир: X вправо, Y вдаль, Z вверх (Unity-Vector3.forward = +Z)
                right.Normalize();
                UnityEngine.Vector3 up = UnityEngine.Vector3.Cross(right, fwd);
                float f = (H * 0.5f) / UnityEngine.Mathf.Tan(fov * 0.5f) * ManualGun.GUNNER_FOV_K;
                UnityEngine.Vector3 eye = new UnityEngine.Vector3(ex, ey, ez);

                UnityEngine.Vector2 tp = ManualGun.GunnerTarget(g);
                float tz = ManualGun.GroundAtF(tp.x, tp.y);
                float gz = ManualGun.GroundAtF(g.current_tile.x, g.current_tile.y) + 1.0f;
                UnityEngine.Vector3 p0 = new UnityEngine.Vector3(g.current_tile.x, g.current_tile.y, gz);
                UnityEngine.Vector3 p1 = new UnityEngine.Vector3(tp.x, tp.y, tz);
                float len = UnityEngine.Vector2.Distance(new UnityEngine.Vector2(p0.x, p0.y), tp);
                float apex = len * (g.asset.id == Siege.MORTAR_ID ? 0.45f : 0.28f);
                double wait = Siege.GunReadyIn(g);
                bool atMax = ManualGun.g_range >= maxR - 0.01f;
                UnityEngine.Color col = wait <= 0 ? new UnityEngine.Color(1f, 0.96f, 0.8f, 0.95f) : new UnityEngine.Color(1f, 0.55f, 0.45f, 0.9f);
                if (atMax) col = new UnityEngine.Color(1f, 0.45f, 0.38f, 0.95f);

                if (!ManualGun.chase_active)
                {
                    // траектория: чистый штрих (длинные штрихи, короткие просветы), без вершины
                    System.Collections.Generic.List<UnityEngine.Vector2> pl = new System.Collections.Generic.List<UnityEngine.Vector2>();
                    int n = 90;
                    for (int i = 0; i <= n; i++)
                    {
                        float t = i / (float)n;
                        UnityEngine.Vector3 pt = new UnityEngine.Vector3(p0.x + (p1.x - p0.x) * t, p0.y + (p1.y - p0.y) * t, p0.z + (p1.z - p0.z) * t + apex * 4f * t * (1f - t));
                        UnityEngine.Vector2 sc;
                        if (Project(pt, eye, fwd, right, up, f, W, H, out sc)) pl.Add(sc);
                        else { Dashes(pl, col); pl.Clear(); }
                    }
                    Dashes(pl, col);
                    // метки дальности по земле каждые 10 клеток и столб над точкой падения
                    for (float dd = 10f; dd < len - 3f; dd += 10f)
                    {
                        float tt = dd / len;
                        UnityEngine.Vector3 gp = new UnityEngine.Vector3(p0.x + (p1.x - p0.x) * tt, p0.y + (p1.y - p0.y) * tt, ManualGun.GroundAtF(p0.x + (p1.x - p0.x) * tt, p0.y + (p1.y - p0.y) * tt) + 0.1f);
                        UnityEngine.Vector2 sg;
                        if (Project(gp, eye, fwd, right, up, f, W, H, out sg)) { Box(sg.x - 1f, sg.y - 6f, 2f, 12f, col); Lab(new UnityEngine.Rect(sg.x + 4f, sg.y - 10f, 40f, 18f), ((int)dd).ToString(), 13, col); }
                    }
                    {
                        UnityEngine.Vector2 s0, s1;
                        if (Project(new UnityEngine.Vector3(p1.x, p1.y, tz), eye, fwd, right, up, f, W, H, out s0) && Project(new UnityEngine.Vector3(p1.x, p1.y, tz + 8f), eye, fwd, right, up, f, W, H, out s1))
                        {
                            Line(s0, s1, 2f, col);
                            Lab(new UnityEngine.Rect(s1.x - 40f, s1.y - 22f, 80f, 20f), ((int)System.Math.Round(ManualGun.g_range)).ToString(), 16, col);
                        }
                    }
                    // кольцо прицела
                    UnityEngine.Vector2 prev = UnityEngine.Vector2.zero; bool hp = false;
                    for (int i = 0; i <= 32; i++)
                    {
                        float a = i * 6.2831853f / 32f;
                        UnityEngine.Vector3 pt = new UnityEngine.Vector3(p1.x + UnityEngine.Mathf.Cos(a) * 2.2f, p1.y + UnityEngine.Mathf.Sin(a) * 2.2f, tz + 0.05f);
                        UnityEngine.Vector2 sc;
                        bool ok = Project(pt, eye, fwd, right, up, f, W, H, out sc);
                        if (ok && hp && (i & 1) == 0) Line(prev, sc, 2f, col);
                        prev = sc; hp = ok;
                    }
                }

                // --- панель в стиле Worldfall ---
                float pw = 640f, ph = 150f, px0 = (W - pw) / 2f, py0 = H - ph - 170f;
                Stone(new UnityEngine.Rect(px0, py0, pw, ph), 0.92f);
                float cx0 = px0 + 24f, cw = pw - 48f;
                // шкала азимута (как компас): +-50 градусов вокруг направления орудия
                float az = ManualGun.g_az;
                float cy0 = py0 + 18f;
                Box(cx0, cy0 + 8f, cw, 22f, new UnityEngine.Color(0f, 0f, 0f, 0.35f));
                float span = 100f;                                                 // градусов в ширине шкалы
                float bearing = ((90f - az * 57.29578f) % 360f + 360f) % 360f;
                for (int d = -60; d <= 60; d += 5)
                {
                    float deg = System.Math.Abs(bearing + d - 0.0001f);
                    float bb = (((bearing + d) % 360f) + 360f) % 360f;
                    float x = cx0 + cw * (0.5f + d / span);
                    if (x < cx0 || x > cx0 + cw) continue;
                    bool big = ((int)System.Math.Round(bb)) % 15 == 0;
                    Box(x - 1f, cy0 + (big ? 14f : 20f), 2f, big ? 16f : 10f, new UnityEngine.Color(1f, 0.96f, 0.8f, big ? 0.85f : 0.4f));
                    if (((int)System.Math.Round(bb)) % 45 == 0)
                    {
                        string[] nn = L10n.Dirs();
                        Lab(new UnityEngine.Rect(x - 20f, cy0 - 14f, 40f, 18f), nn[((int)System.Math.Round(bb / 45f)) % 8], 15, new UnityEngine.Color(1f, 0.96f, 0.8f, 1f));
                    }
                }
                Box(cx0 + cw * 0.5f - 2f, cy0 + 4f, 4f, 32f, col);
                UnityEngine.Vector2 city; bool hasCity = Siege.CityPoint(g, out city);
                if (hasCity)
                {
                    float cb = UnityEngine.Mathf.Atan2(city.y - g.current_tile.y, city.x - g.current_tile.x);
                    float rel = ManualGun.WrapPi(cb - az) * 57.29578f;               // + влево (угол растёт против часовой)
                    float dd = -rel;
                    float cd = UnityEngine.Vector2.Distance(new UnityEngine.Vector2(g.current_tile.x, g.current_tile.y), city);
                    bool far = cd > maxR;
                    UnityEngine.Color cc = far ? new UnityEngine.Color(1f, 0.45f, 0.38f, 1f) : new UnityEngine.Color(0.55f, 0.9f, 1f, 1f);
                    float dx = UnityEngine.Mathf.Clamp(dd, -span * 0.5f + 3f, span * 0.5f - 3f);
                    float x = cx0 + cw * (0.5f + dx / span);
                    Lab(new UnityEngine.Rect(x - 14f, cy0 + 4f, 28f, 28f), (dd < -span * 0.5f ? "◀" : (dd > span * 0.5f ? "▶" : "◆")), 22, cc);
                    Lab(new UnityEngine.Rect(x - 40f, cy0 + 30f, 80f, 16f), far ? L10n.T("город (далеко)", "city (far)") : L10n.T("город", "city"), 13, cc);
                }
                // дальность
                float ry = py0 + 66f;
                float rr = UnityEngine.Mathf.Clamp01((ManualGun.g_range - minR) / System.Math.Max(1f, maxR - minR));
                BarW(new UnityEngine.Rect(cx0, ry, cw, 20f), rr, atMax ? new UnityEngine.Color(0.9f, 0.35f, 0.3f, 1f) : new UnityEngine.Color(0.85f, 0.7f, 0.3f, 1f), L10n.T("Дальность ", "Range ") + (int)System.Math.Round(ManualGun.g_range) + "   (" + (int)minR + " – " + (int)maxR + ")");
                // перезарядка
                float rl = Siege.GunReload(g);
                float rf = wait <= 0 ? 1f : UnityEngine.Mathf.Clamp01(1f - (float)(wait / System.Math.Max(0.1f, rl)));
                BarW(new UnityEngine.Rect(cx0, ry + 26f, cw, 16f), rf, wait <= 0 ? new UnityEngine.Color(0.5f, 0.8f, 0.4f, 1f) : new UnityEngine.Color(0.8f, 0.45f, 0.3f, 1f), wait <= 0 ? L10n.T("Готово", "Ready") : L10n.T("Перезарядка ", "Reloading ") + (int)System.Math.Ceiling(wait) + L10n.T(" с", " s"));
                // клавиши
                float kx = cx0, ky = py0 + ph - 20f;
                kx += Key(kx, ky, "A D", L10n.T("поворот", "turn")); kx += Key(kx, ky, "W S", L10n.T("дальность", "range")); kx += Key(kx, ky, L10n.T("ЛКМ", "LMB"), L10n.T("выстрел", "fire"));
                kx += Key(kx, ky, "R", ManualGun.g_view == 0 ? L10n.T("вид: обычный", "view: normal") : L10n.T("вид: сверху", "view: overhead")); kx += Key(kx, ky, "U", ManualGun.chase_on ? L10n.T("за снарядом: вкл*", "shell cam: on*") : L10n.T("за снарядом*", "shell cam*")); Key(kx, ky, "K", L10n.T("уйти", "leave"));
                if (hasCity)
                {
                    float cd2 = UnityEngine.Vector2.Distance(new UnityEngine.Vector2(g.current_tile.x, g.current_tile.y), city);
                    if (cd2 > maxR) Lab(new UnityEngine.Rect(px0, py0 - 26f, pw, 24f), L10n.T("Город вне дальности орудия (", "City is out of gun range (") + (int)cd2 + " > " + (int)maxR + ")", 17, new UnityEngine.Color(1f, 0.5f, 0.4f, 1f));
                }
            }

            private static System.Reflection.MethodInfo gaStone, gaBar, gaKey, gaLabel;
            private static bool gaTried, gaOk;
            private static void GaInit()
            {
                if (gaTried) return; gaTried = true;
                try
                {
                    System.Type t = WfT.Get("FirstPerson.GameArt");
                    if (t == null) return;
                    gaStone = AccessTools.Method(t, "StonePanel"); gaBar = AccessTools.Method(t, "Bar"); gaKey = AccessTools.Method(t, "KeyCap"); gaLabel = AccessTools.Method(t, "Label");
                    gaOk = gaStone != null && gaBar != null && gaKey != null && gaLabel != null;
                }
                catch (System.Exception) { gaOk = false; }
            }

            private void Stone(UnityEngine.Rect r, float a)
            {
                GaInit();
                if (gaOk) { try { gaStone.Invoke(null, new object[] { r, a }); return; } catch (System.Exception) { gaOk = false; } }
                Box(r.x, r.y, r.width, r.height, new UnityEngine.Color(0.12f, 0.12f, 0.13f, 0.85f));
            }

            private void Lab(UnityEngine.Rect r, string s, int size, UnityEngine.Color c)
            {
                Label(r, s, size, c);
            }

            private void BarW(UnityEngine.Rect r, float frac, UnityEngine.Color fill, string text)
            {
                Box(r.x, r.y, r.width, r.height, new UnityEngine.Color(0f, 0f, 0f, 0.55f));
                Box(r.x, r.y, r.width * frac, r.height, fill);
                Label(r, text, (int)(r.height * 0.7f), UnityEngine.Color.white);
            }

            // клавиша + подпись; возвращает занятую ширину
            private float Key(float x, float cy, string k, string label)
            {
                float w = 0f;
                GaInit();
                if (gaOk) { try { w = (float)gaKey.Invoke(null, new object[] { x, cy, k, 9f, 1f }); } catch (System.Exception) { gaOk = false; } }
                if (!gaOk || w <= 0f) { w = 20f + k.Length * 9f; Box(x, cy - 11f, w, 22f, new UnityEngine.Color(0.9f, 0.9f, 0.85f, 0.9f)); Label(new UnityEngine.Rect(x, cy - 11f, w, 22f), k, 14, UnityEngine.Color.black); }
                float lw = 14f + label.Length * 7.5f;
                Lab(new UnityEngine.Rect(x + w + 2f, cy - 11f, lw, 22f), label, 14, new UnityEngine.Color(1f, 0.96f, 0.8f, 1f));
                return w + lw + 8f;
            }

            private void Line(UnityEngine.Vector2 a, UnityEngine.Vector2 b, float w, UnityEngine.Color c)
            {
                UnityEngine.Vector2 d = b - a; float L = d.magnitude; if (L < 0.5f) return;
                UnityEngine.Matrix4x4 m = UnityEngine.GUI.matrix;
                UnityEngine.GUIUtility.RotateAroundPivot(UnityEngine.Mathf.Atan2(d.y, d.x) * 57.29578f, a);
                UnityEngine.GUI.color = c;
                UnityEngine.GUI.DrawTexture(new UnityEngine.Rect(a.x, a.y - w * 0.5f, L, w), UnityEngine.Texture2D.whiteTexture);
                UnityEngine.GUI.color = UnityEngine.Color.white;
                UnityEngine.GUI.matrix = m;
            }

            private void Dashes(System.Collections.Generic.List<UnityEngine.Vector2> pl, UnityEngine.Color c)
            {
                float on = 16f, off = 8f, acc = 0f; bool draw = true;
                for (int i = 0; i + 1 < pl.Count; i++)
                {
                    UnityEngine.Vector2 a = pl[i], b = pl[i + 1]; float L = (b - a).magnitude; float pos = 0f;
                    while (pos < L)
                    {
                        float lim = draw ? on : off;
                        float step = UnityEngine.Mathf.Min(lim - acc, L - pos);
                        if (draw) Line(a + (b - a) * (pos / L), a + (b - a) * ((pos + step) / L), 3f, c);
                        pos += step; acc += step;
                        if (acc >= lim - 0.001f) { acc = 0f; draw = !draw; }
                    }
                }
            }

            private static bool Project(UnityEngine.Vector3 p, UnityEngine.Vector3 eye, UnityEngine.Vector3 fwd, UnityEngine.Vector3 right, UnityEngine.Vector3 up, float f, float W, float H, out UnityEngine.Vector2 sc)
            {
                UnityEngine.Vector3 d = p - eye;
                float z = UnityEngine.Vector3.Dot(d, fwd);
                sc = UnityEngine.Vector2.zero;
                if (z < 0.2f) return false;
                sc = new UnityEngine.Vector2(W * 0.5f + f * UnityEngine.Vector3.Dot(d, right) / z, H * 0.5f - f * UnityEngine.Vector3.Dot(d, up) / z);
                return sc.x > -50f && sc.x < W + 50f && sc.y > -50f && sc.y < H + 50f;
            }
        }
    }
}

namespace HelloWorld
{
    // Подсказка действия в 3D: на той же высоте, что подсказки Worldfall («E Обыскать тело»), в небольшой полупрозрачной рамке в цветах мода
    internal static class Prompt
    {
        private static UnityEngine.GUIStyle st, kst;
        public static void Draw(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return;
            string key = null, txt = raw;
            if (raw.StartsWith("[")) { int e = raw.IndexOf(']'); if (e > 1) { key = raw.Substring(1, e - 1); txt = raw.Substring(e + 1).TrimStart(); } }
            float W = UnityEngine.Screen.width, H = UnityEngine.Screen.height;
            float h = UnityEngine.Mathf.Max(30f, H * 0.043f);
            if (st == null)
            {
                st = new UnityEngine.GUIStyle(UnityEngine.GUI.skin.label) { alignment = UnityEngine.TextAnchor.MiddleLeft, fontStyle = UnityEngine.FontStyle.Bold, wordWrap = false };
                kst = new UnityEngine.GUIStyle(UnityEngine.GUI.skin.label) { alignment = UnityEngine.TextAnchor.MiddleCenter, fontStyle = UnityEngine.FontStyle.Bold, wordWrap = false };
            }
            int fs = (int)(h * 0.40f); st.fontSize = fs; kst.fontSize = (int)(h * 0.36f);
            float tw = st.CalcSize(new UnityEngine.GUIContent(txt)).x;
            float kw = key == null ? 0f : UnityEngine.Mathf.Max(h * 0.62f, kst.CalcSize(new UnityEngine.GUIContent(key)).x + h * 0.3f);
            float pad = h * 0.30f, gap = key == null ? 0f : h * 0.25f;
            float w = pad + kw + gap + tw + pad;
            float x = (W - w) / 2f, y = H * 0.671f - h / 2f;
            UnityEngine.Texture2D t = UnityEngine.Texture2D.whiteTexture;
            UnityEngine.GUI.depth = -32000;
            Box(t, x - 2f, y - 2f, w + 4f, h + 4f, new UnityEngine.Color(0.80f, 0.62f, 0.30f, 0.55f));       // тонкая золотая окантовка
            Box(t, x, y, w, h, new UnityEngine.Color(0.09f, 0.06f, 0.05f, 0.62f));
            float cx = x + pad;
            if (key != null)
            {
                float kh = h * 0.62f, ky = y + (h - kh) / 2f;
                Box(t, cx, ky, kw, kh, new UnityEngine.Color(0.58f, 0.14f, 0.15f, 0.92f));
                Box(t, cx, ky, kw, 2f, new UnityEngine.Color(0.90f, 0.75f, 0.42f, 0.9f));
                kst.normal.textColor = new UnityEngine.Color(1f, 0.93f, 0.75f, 1f);
                UnityEngine.GUI.Label(new UnityEngine.Rect(cx, ky, kw, kh), key, kst);
                cx += kw + gap;
            }
            Shadow(new UnityEngine.Rect(cx, y, tw + 4f, h), txt);
        }
        private static void Shadow(UnityEngine.Rect r, string s)
        {
            st.normal.textColor = new UnityEngine.Color(0f, 0f, 0f, 0.8f);
            UnityEngine.GUI.Label(new UnityEngine.Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), s, st);
            st.normal.textColor = new UnityEngine.Color(0.98f, 0.94f, 0.82f, 1f);
            UnityEngine.GUI.Label(r, s, st);
        }
        private static void Box(UnityEngine.Texture2D t, float x, float y, float w, float h, UnityEngine.Color c)
        {
            UnityEngine.GUI.color = c; UnityEngine.GUI.DrawTexture(new UnityEngine.Rect(x, y, w, h), t); UnityEngine.GUI.color = UnityEngine.Color.white;
        }
    }
}

namespace HelloWorld
{
    // Тексты 3D-интерфейса: русский при русском языке игры, иначе английский
    internal static class L10n
    {
        public static bool Ru() { try { return LocalizedTextManager.instance != null && LocalizedTextManager.instance.language == "ru"; } catch (System.Exception) { return false; } }
        public static string T(string ru, string en) { return Ru() ? ru : en; }
        public static string[] Dirs() { return Ru() ? new string[] { "С", "СВ", "В", "ЮВ", "Ю", "ЮЗ", "З", "СЗ" } : new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }; }
    }
}

namespace HelloWorld
{
    // 2D: при наведении на осадное орудие над ним появляется небольшая плашка «K - управление орудием»
    internal class GunHintGui : UnityEngine.MonoBehaviour
    {
        private float alpha; private UnityEngine.Vector3 pos; private string name = "";
        private UnityEngine.GUIStyle st, kst;
        void OnGUI()
        {
            try
            {
                if (World.world == null || Scroll.Is3D() || Siege.manual_gun != null) { alpha = 0f; return; }
                WorldTile mt = World.world.getMouseTilePos();
                Building best = null;
                if (mt != null)
                {
                    float bd = 1e9f;
                    foreach (Building b in World.world.buildings.getBuildingFromZones(mt, 4))
                    {
                        if (b == null || b.asset == null || !Siege.IsShooterId(b.asset.id) || !b.isUsable() || b.current_tile == null) continue;
                        float d = Toolbox.Dist(mt.x, mt.y, b.current_tile.x, b.current_tile.y);
                        if (d < bd) { bd = d; best = b; }
                    }
                    if (best != null && bd > 3.2f) best = null;
                }
                if (best != null)
                {
                    pos = new UnityEngine.Vector3(best.current_tile.posV3.x, best.current_tile.posV3.y, 0f);
                    string id = best.asset.id;
                    name = id == Siege.MORTAR_ID ? L10n.T("мортира", "mortar") : (id == Siege.TREBUCHET_ID ? L10n.T("требушет", "trebuchet") : L10n.T("катапульта", "catapult"));
                    alpha = UnityEngine.Mathf.MoveTowards(alpha, 1f, UnityEngine.Time.unscaledDeltaTime * 8f);
                }
                else alpha = UnityEngine.Mathf.MoveTowards(alpha, 0f, UnityEngine.Time.unscaledDeltaTime * 5f);
                if (alpha <= 0.01f || UnityEngine.Camera.main == null) return;
                UnityEngine.Vector3 sp = UnityEngine.Camera.main.WorldToScreenPoint(pos);
                if (sp.z < 0f) return;
                float zoom = UnityEngine.Mathf.Clamp(UnityEngine.Screen.height / 1000f, 0.8f, 1.6f);
                float h = 22f * zoom;
                if (st == null)
                {
                    st = new UnityEngine.GUIStyle(UnityEngine.GUI.skin.label) { alignment = UnityEngine.TextAnchor.MiddleLeft, fontStyle = UnityEngine.FontStyle.Bold, wordWrap = false };
                    kst = new UnityEngine.GUIStyle(UnityEngine.GUI.skin.label) { alignment = UnityEngine.TextAnchor.MiddleCenter, fontStyle = UnityEngine.FontStyle.Bold, wordWrap = false };
                }
                st.fontSize = (int)(h * 0.50f); kst.fontSize = (int)(h * 0.46f);
                string txt = L10n.T("Управление: ", "Control: ") + name;
                float tw = st.CalcSize(new UnityEngine.GUIContent(txt)).x;
                float kw = h * 0.78f, pad = h * 0.32f, gap = h * 0.28f;
                float w = pad + kw + gap + tw + pad;
                float x = sp.x - w / 2f, y = UnityEngine.Screen.height - sp.y - 38f * zoom - h;
                UnityEngine.GUI.depth = -32000;
                UnityEngine.Texture2D t = UnityEngine.Texture2D.whiteTexture;
                UnityEngine.Color old = UnityEngine.GUI.color;
                UnityEngine.GUI.color = new UnityEngine.Color(0.80f, 0.62f, 0.30f, 0.6f * alpha); UnityEngine.GUI.DrawTexture(new UnityEngine.Rect(x - 1.5f, y - 1.5f, w + 3f, h + 3f), t);
                UnityEngine.GUI.color = new UnityEngine.Color(0.09f, 0.06f, 0.05f, 0.78f * alpha); UnityEngine.GUI.DrawTexture(new UnityEngine.Rect(x, y, w, h), t);
                float kh = h * 0.70f, ky = y + (h - kh) / 2f;
                UnityEngine.GUI.color = new UnityEngine.Color(0.58f, 0.14f, 0.15f, 0.95f * alpha); UnityEngine.GUI.DrawTexture(new UnityEngine.Rect(x + pad, ky, kw, kh), t);
                UnityEngine.GUI.color = new UnityEngine.Color(0.90f, 0.75f, 0.42f, 0.9f * alpha); UnityEngine.GUI.DrawTexture(new UnityEngine.Rect(x + pad, ky, kw, 1.5f), t);
                kst.normal.textColor = new UnityEngine.Color(1f, 0.93f, 0.75f, alpha);
                UnityEngine.GUI.color = UnityEngine.Color.white;
                UnityEngine.GUI.Label(new UnityEngine.Rect(x + pad, ky, kw, kh), "K", kst);
                st.normal.textColor = new UnityEngine.Color(0f, 0f, 0f, 0.8f * alpha);
                UnityEngine.GUI.Label(new UnityEngine.Rect(x + pad + kw + gap + 1f, y + 1f, tw + 4f, h), txt, st);
                st.normal.textColor = new UnityEngine.Color(0.98f, 0.94f, 0.82f, alpha);
                UnityEngine.GUI.Label(new UnityEngine.Rect(x + pad + kw + gap, y, tw + 4f, h), txt, st);
                UnityEngine.GUI.color = old;
            }
            catch (System.Exception) { }
        }
    }
}

namespace HelloWorld
{
    // Поиск типов Worldfall с кэшем: AccessTools.TypeByName при отсутствии типа перебирает ВСЕ типы всех сборок,
    // а без Worldfall его звали каждый кадр - загрузка игры тянулась минутами. Промах помним 10 секунд.
    internal static class WfT
    {
        private static readonly System.Collections.Generic.Dictionary<string, System.Type> hit = new System.Collections.Generic.Dictionary<string, System.Type>();
        private static readonly System.Collections.Generic.Dictionary<string, float> miss = new System.Collections.Generic.Dictionary<string, float>();
        public static System.Type Get(string name)
        {
            System.Type t;
            if (hit.TryGetValue(name, out t)) return t;
            float now = UnityEngine.Time.realtimeSinceStartup, until;
            if (miss.TryGetValue(name, out until) && now < until) return null;
            t = HarmonyLib.AccessTools.TypeByName(name);
            if (t != null) hit[name] = t; else miss[name] = now + 10f;
            return t;
        }
    }
}
