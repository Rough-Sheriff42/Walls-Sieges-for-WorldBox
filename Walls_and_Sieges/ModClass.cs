using System.Collections.Generic;
using HarmonyLib;
using NeoModLoader.api;
using ai.behaviours;

namespace HelloWorld
{
    public class ModClass : BasicMod<ModClass>
    {
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
                Siege.CreateAssets();
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
                // армия остаётся у закрытого города, пока идёт обстрел (см. Siege.HoldFor)
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
        public WorldTile pending_center;
        public int pending_ticks;
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
        private static float GATE_PER_WARRIOR = Cfg.Float("gate_hold_per_warrior", 1f, "давление на ворота от одного воина у стены за 2 секунды");
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

        private static bool LOG_VERBOSE = Cfg.Bool("log_verbose", false, "писать в лог подробности (планы колец, постройка башен, ремонт, дороги и т.п.); для обычной игры не нужно");
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
            Log("world changed: mod state reset");
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
                            if (n > 0) Log("city fell, its walls are decaying: " + n + " tiles");
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
            Horn.DumpEventsOnce();
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
                if (capital && st.nogate >= 0 && !st.warned_nogate) { st.warned_nogate = true; Log("capital " + pCity.name + " has a side without gates (city became capital after its walls were planned)"); }
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
                    else Log("city center of " + pCity.name + " moved: " + st.center.x + "," + st.center.y + " -> " + center.x + "," + center.y);
                }
                else { st.pending_center = null; st.pending_ticks = 0; }

                if (st.center != center && !keepCenter)
                {
                    if (st.center != null && WALL_DECAY)
                    {
                        int oldWalls = ScheduleDecay(st);
                        if (oldWalls > 0) Log("city center of " + pCity.name + " moved far: old walls decaying, " + oldWalls + " tiles");
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
                    st.built_walls.Clear();
                    st.ring_news = new bool[4];
                    st.ring_seen_low = new bool[4];
                    st.gates.Clear();
                    st.gates_rings = 0;
                    st.gates_closed = false;
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
                    }
                    else maxRings = st.stage + 1;
                }

                WallStyle style = GetStyle(pCity);

                PlaceWalls(pCity, st, style, war);
                if (WALL_NEWS && st.ticks_total % 10 == 3) WallNews(pCity, st);
                PlaceTower(pCity, st, war);
                if (st.ticks_total % 6 == 1) HandleCoast(pCity, st, style, capital);
                if (st.ticks_total % 6 == 4) HandleStakes(pCity, st);
                if (UPGRADE_WALLS && style.upgrade_tile != null) UpgradeWalls(pCity, st, style);
                if (st.ticks_total % 10 == 0) UpgradeTowers(pCity, st);
                if (ROADS_TO_GATES) HandleRoads(pCity, st);
                if (REPAIR_TOWERS && st.ticks_total % 10 == 5) RepairTowers(pCity, st);
                if (MOAT_ENABLED && capital && style.moat) HandleMoat(pCity, st);

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
            "..lRRlRRl..",
            ".lRrRRrRRrl",
            "dDDDDDDDDDd",
            "..Dd.Y.dD..",
            "..DlLLLlD..",
            "..DLMLMLD..",
            "..DMmMmMD..",
            "dLLlLLlLLLd",
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
            "..DlLLLlD..",
            "..DLMLMLD..",
            "..DMmMmMD..",
            "dLLlLLlLLLd",
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
            "..lRRlRRl..",
            ".lRrRRrRRrl",
            "dDDDDDDDDDd",
            "..Dd.Y.dD..",
            "..DlkFklD..",
            "..DLfFfLD..",
            "..DMmfmMD..",
            "dLLlLLlLLLd",
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
            "dLLlLLlLLLd",
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

        static UnityEngine.Color32 GridColor(char c)
        {
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

        public static void SpriteListPostfix(BuildingAsset __instance, ref UnityEngine.Sprite[] __result)
        {
            if (__instance == null) return;
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
                Log("wall decay cancelled at " + city.name + " (pop " + pop + ")");
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
            Log("walls of " + city.name + " are decaying (pop " + pop + "): " + n + " tiles over " + (int)DECAY_SECONDS + " s");
            return true;
        }

        private static bool ProtectedByLiving(WorldTile t)
        {
            foreach (KeyValuePair<City, CityState> kv in _states)
            {
                CityState s = kv.Value;
                if (s.decaying || kv.Key == null || !kv.Key.isAlive()) continue;
                if (s.plan_tiles.Contains(t)) return true;
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
            if (_decay.Count == 0) Log("wall decay finished (last batch: " + removed + " tiles)");
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
                            if (t == null || Foreign(t, city)) { ok = false; break; }
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
                            if (t == null || t.is_liquid || Foreign(t, city)) { ok = false; break; }
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
            if (o == null || o == city) return false;
            return o.kingdom == null || o.kingdom != city.kingdom;
        }

        // На сколько клеток внутрь от кольца надо отступить, чтобы выйти из чужой зоны (-1 - некуда)
        private static int InwardOffset(WorldTile c, int R, int side, int s, City city)
        {
            for (int k = 1; k <= R - 4; k++)
            {
                int x, y;
                RingPoint(c, R - k, side, s, out x, out y);
                WorldTile t = World.world.GetTile(x, y);
                if (t == null || t.is_liquid) continue;
                if (!Foreign(t, city)) return k;
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
                if (t == null || Foreign(t, city) || !used.Add(t)) continue;
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
                    if (Foreign(t, city))
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
                            if (spt == null || Foreign(spt, city) || !used.Add(spt)) continue;
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
            City owner = t.zone.city;
            if (owner == null || owner == city) return true;
            return owner.kingdom != null && owner.kingdom == city.kingdom;
        }

        private static float RingProgress(City city, List<PlanItem> ring)
        {
            int eligible = 0;
            int done = 0;
            foreach (PlanItem it in ring)
            {
                if (it.kind != PlanKind.Wall) continue;
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
                    Siege.AnnounceWall(city, r > 0);
                }
            }
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

                if (IsCiv(t))
                {
                    if (it.spur) continue;   // выступ упёрся в здание - просто пропускаем, обход не строим
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
            bool paved = style.arch == "human" || style.arch == "dwarf";
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
        private static int GATE_ARMY_MIN = Cfg.Int("gate_army_min", 5, "минимум бойцов в идущей на город армии, чтобы закрыть ворота заранее");

        // Вражеская армия идёт именно на этот город и уже недалеко: запираемся, не дожидаясь, пока она встанет у стен
        private static bool ApproachingArmy(City city, CityState st, int outer)
        {
            foreach (City oc in World.world.cities)
            {
                if (oc == null || oc == city || !oc.isAlive() || oc.target_attack_city != city) continue;
                Army army = oc.army;
                if (army == null || !army.hasCaptain() || army.countMelee() + army.countRange() < GATE_ARMY_MIN) continue;
                Actor cap = army.getCaptain();
                if (cap == null || !cap.isAlive() || cap.current_tile == null) continue;
                int cheb = System.Math.Max(System.Math.Abs(cap.current_tile.x - st.center.x), System.Math.Abs(cap.current_tile.y - st.center.y));
                if (cheb <= outer + GATE_ARMY_RANGE) return true;
            }
            return false;
        }

        private static bool Threat(City city, CityState st)
        {
            st.near_count = 0;
            st.last_enemy_count = 0;
            if (Siege.UnderSiege(city)) return true;
            int outer = 0;
            for (int i = 0; i < st.ring_radius.Count; i++) if (st.ring_radius[i] > outer) outer = st.ring_radius[i];
            if (city.kingdom == null || !city.kingdom.hasEnemies()) return false;
            if (ApproachingArmy(city, st, outer)) return true;
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
            return n >= GATE_THREAT_MIN || (n >= 2 && near >= 2) || (n >= 1 && city.isInDanger());
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
            Log("gate breached at " + city.name + ", ring " + ring + ", side " + side + (byGuns ? " by siege guns" : " by the crowd")
                + " (enemy warriors: " + st.last_enemy_count + ", at the wall: " + st.near_count + ")");
            if (first != null)
            {
                try
                {
                    EffectsLibrary.spawnAtTileRandomScale("fx_explosion_small", first, byGuns ? 0.18f : 0.1f, byGuns ? 0.26f : 0.15f);
                    if (byGuns) MusicBox.playSound("event:/SFX/EXPLOSIONS/ExplosionMiddle", first, pGameViewOnly: true, pVisibleOnly: true);
                }
                catch (System.Exception) { }
            }
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

        private static float GATE_HP = Cfg.Float("gate_hp", 900f, "прочность одной стороны ворот при обстреле орудиями (у столиц в полтора раза больше, у деревянных в 0.6 раза)");

        internal static bool GatesSealed(City city)
        {
            CityState st;
            if (city == null || !_states.TryGetValue(city, out st) || !st.gates_closed) return false;
            foreach (GateTile g in st.gates) if (g.closed) return true;
            return false;
        }

        private static float GateMaxHp(City city, CityState st)
        {
            float hp = GATE_HP;
            if (st.is_capital) hp *= 1.5f;
            try { if (GetStyle(city).arch == "orc") hp *= 0.6f; } catch (System.Exception) { }
            return hp;
        }

        // Какую сторону ворот бьют орудия: если у внешнего кольца сторона уже разбита, то внутреннее кольцо на той же стороне;
        // иначе ближайшая к орудию сторона внешнего кольца с закрытыми воротами
        private static bool FocusSide(CityState st, WorldTile from, out int ring, out int side, out WorldTile at)
        {
            ring = -1; side = -1; at = null;
            int maxRing = -1;
            foreach (GateTile g in st.gates) if (g.ring > maxRing) maxRing = g.ring;
            if (maxRing < 0) return false;

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
            try { MusicBox.playSound("event:/SFX/HIT/HitStone", at, pGameViewOnly: true, pVisibleOnly: true); }
            catch (System.Exception) { }
            if (hp <= 0f) BreakSide(city, st, ring, side, World.world.getCurWorldTime(), true);
        }

        private static float WALL_HP_PCT = Cfg.Float("siege_wall_hp_pct", 70f, "прочность участка стены в процентах от прочности ворот");

        // Попадание орудия по внешней стене. Возвращает false, если подходящего участка нет
        internal static bool WallDamage(City city, WorldTile gun, float dmg)
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
                MusicBox.playSound("event:/SFX/HIT/HitStone", best.tile, pGameViewOnly: true, pVisibleOnly: true);
                EffectsLibrary.spawnAtTile("fx_fire_smoke", best.tile, Randy.randomFloat(0.12f, 0.2f));
            }
            catch (System.Exception) { }
            if (hp > 0f) return true;
            st.wall_hp.Remove(key);
            int removed = 0;
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                {
                    WorldTile n = World.world.GetTile(best.tile.x + dx, best.tile.y + dy);
                    if (n == null || !n.Type.wall || gateTiles.Contains(n) || !st.plan_tiles.Contains(n)) continue;
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
            Log("wall breached at " + city.name + " near " + best.tile.x + "," + best.tile.y + " by siege guns (" + removed + " tiles)");
            try
            {
                EffectsLibrary.spawnAtTileRandomScale("fx_explosion_small", best.tile, 0.18f, 0.26f);
                MusicBox.playSound("event:/SFX/EXPLOSIONS/ExplosionMiddle", best.tile, pGameViewOnly: true, pVisibleOnly: true);
            }
            catch (System.Exception) { }
            return true;
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
        private static void HandleBarricades(City city, CityState st, double now)
        {
            if (now < st.next_barricade) return;
            st.next_barricade = now + 3.0;
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
                    WorldTile t = Siege.FreeSpot(World.world.GetTile(x, y), 1);
                    if (t == null) continue;
                    Building b = Siege.Put(side == 2 ? Siege.BARRICADE_L_ID : side == 3 ? Siege.BARRICADE_R_ID : Siege.BARRICADE_ID, t, city.kingdom);
                    st.barricades[k] = b;
                    // знамя города рядом с баррикадой: сразу видно, какая сторона держит оборону
                    WorldTile ft = Siege.FreeSpot(World.world.GetTile(side < 2 ? t.x + 4 : t.x, side < 2 ? t.y : t.y + 4), 1);
                    if (ft != null && (st.barricades[k + 8] == null || !st.barricades[k + 8].isAlive())) st.barricades[k + 8] = Siege.Put(Siege.BANNER_S_ID, ft, city.kingdom);
                    return;      // не больше одной за раз
                }
            }
        }

        // Ворота закрываются стеной, пока рядом вражеское войско или идёт осада, и открываются через GATE_OPEN_SECONDS
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
                if (Threat(city, st))
                {
                    if (!st.gates_closed)
                    {
                        Dbg("gates closing at " + city.name + " (enemy warriors: " + st.last_enemy_count + ")");
                        st.closed_since = now;
                        st.pressure = 0;
                    }
                    st.last_threat = now;
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
                else if (st.gates_closed && now - st.last_threat > GATE_OPEN_SECONDS)
                {
                    Dbg("gates opening at " + city.name);
                    st.gates_closed = false;
                    foreach (GateTile g in st.gates) g.broken = false;
                    st.gate_hp.Clear();
                    st.wall_hp.Clear();
                    ClearBarricades(st);
                }
            }

            GateDiag(city, st, now);
            GateSmoke(city, st, now);
            HandleBarricades(city, st, now);

            int changed = 0;
            int blocked = 0;
            foreach (GateTile g in st.gates)
            {
                if (changed >= GATE_PER_TICK) break;
                WorldTile t = g.tile;
                if (st.gates_closed)
                {
                    if (g.closed)
                    {
                        if (!t.Type.wall) { g.closed = false; g.broken = true; }   // ворота выбили - пока угроза, заново не ставим
                        continue;
                    }
                    if (t.hasUnits() || t.hasBuilding()) blocked++;
                    if (g.broken || t.Type.wall) continue;
                    if (t.hasBuilding())
                    {
                        if (Clearable(t.building) || t.building.asset.building_type == BuildingType.Building_Wheat) t.building.startDestroyBuilding();   // постройку в проёме (бочка, забор) убираем, ворота закроются на следующем шаге
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
            if (blocked > 0 && now >= st.next_block_log)
            {
                st.next_block_log = now + 15.0;
                Dbg("gates at " + city.name + ": " + blocked + " tiles blocked by units/buildings (enemies near: " + st.last_enemy_count + ")");
            }
        }

        private static void HandlePlaza(City city, CityState st, WorldTile center)
        {
            WallStyle style = GetStyle(city);
            if (style.arch != "human" && style.arch != "dwarf") return;

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

        private static bool ARMY_UNSTICK = Cfg.Bool("army_unstick", true, "подталкивать капитанов армий, застрявших у стен");
        private static float ARMY_STUCK_SECONDS = Cfg.Float("army_stuck_seconds", 45f, "сколько секунд капитан стоит у стены на месте, прежде чем его подтолкнут");

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

        private static bool ARMY_STUCK_LOG = Cfg.Bool("army_stuck_log", true, "подробно писать в лог, почему застрял капитан (задание, цель, расстояние до стены)");

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

        private static float ARMY_TASK_SECONDS = Cfg.Float("army_task_seconds", 120f, "сколько секунд капитан может повисеть на одной и той же мирной задаче (поёт, плачет...), прежде чем её сбросят");

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
                if (!nearWall || a.is_moving)
                {
                    _stuck.Remove(a);
                    continue;
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
            if (loaded.TryGetValue("gate_diag", out old) && old == "true") loaded.Remove("gate_diag");      // диагностика ворот и замер времени теперь выключены по умолчанию
            if (loaded.TryGetValue("perf_log", out old) && old == "true") loaded.Remove("perf_log");
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
        public static void OnHorn(bool v) { Cfg.Set("siege_horn", B(v)); }
        public static void OnWallShare(float v) { Cfg.Set("siege_wall_share", ((int)System.Math.Round(v)).ToString()); }
        public static void OnGateHp(float v) { Cfg.Set("gate_hp", ((int)System.Math.Round(v)).ToString() ); }
        public static void OnReload(float v) { Cfg.Set("siege_gun_reload", v.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)); }
    }

    // Осадный рог: короткий сигнал из двух нот, синтезируется при первом вызове (готового звука рога в игре нет).
    // Unity-звук берём через рефлексию: если модуль звука недоступен, мод просто молчит
    internal static class Horn
    {
        private static bool HORN_ENABLED = Cfg.Bool("siege_horn", true, "играть звук рога, когда у осаждённого города встаёт вражеский лагерь");
        private static float HORN_VOLUME = Cfg.Float("siege_horn_volume", 0.6f, "громкость рога (0-1)");
        private static object clip;
        private static float clip_len;
        private static bool failed;

        private static void Build(System.Type tClip)
        {
            int rate = 22050;
            float[] notes = { 98f, 131f };
            float[] lens = { 1.3f, 1.9f };
            int total = 0;
            for (int n = 0; n < notes.Length; n++) total += (int)(lens[n] * rate) + rate / 10;
            float[] data = new float[total];
            int pos = 0;
            for (int n = 0; n < notes.Length; n++)
            {
                int len = (int)(lens[n] * rate);
                double phase = 0;
                for (int i = 0; i < len; i++)
                {
                    float t = (float)i / rate;
                    float f = notes[n] * (n == 1 ? 0.94f + 0.06f * UnityEngine.Mathf.Min(1f, t / 0.35f) : 1f) * (1f + 0.006f * UnityEngine.Mathf.Sin(t * 38f));
                    phase += 2.0 * System.Math.PI * f / rate;
                    float v = 0f;
                    for (int h = 1; h <= 7; h++) v += UnityEngine.Mathf.Sin((float)(phase * h)) / (h * (h > 4 ? 1.8f : 1f));
                    float env = UnityEngine.Mathf.Min(1f, t / 0.18f) * UnityEngine.Mathf.Min(1f, (lens[n] - t) / 0.45f);
                    data[pos + i] = v * env * 0.35f;
                }
                pos += len + rate / 10;
            }
            clip_len = (float)total / rate;
            clip = tClip.GetMethod("Create", new System.Type[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(bool) })
                .Invoke(null, new object[] { "mymod_horn", total, 1, rate, false });
            tClip.GetMethod("SetData", new System.Type[] { typeof(float[]), typeof(int) }).Invoke(clip, new object[] { data, 0 });
        }

        private static bool dumped;

        // Один раз выписываем в файл Mymod_sounds.txt названия всех звуков игры: так можно подобрать готовый звук для рога
        public static void DumpEventsOnce()
        {
            if (dumped) return;
            dumped = true;
            try
            {
                string path = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "Mymod_sounds.txt");
                if (System.IO.File.Exists(path)) return;
                System.Type rm = null;
                foreach (System.Reflection.Assembly a in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    rm = a.GetType("FMODUnity.RuntimeManager");
                    if (rm != null) break;
                }
                if (rm == null) return;
                object studio = rm.GetProperty("StudioSystem").GetValue(null, null);
                System.Reflection.MethodInfo getBanks = null;
                foreach (System.Reflection.MethodInfo m in studio.GetType().GetMethods())
                    if (m.Name == "getBankList" && m.GetParameters().Length == 1) { getBanks = m; break; }
                if (getBanks == null) return;
                object[] ba = new object[1];
                getBanks.Invoke(studio, ba);
                List<string> lines = new List<string>();
                foreach (object bank in (System.Array)ba[0])
                {
                    System.Reflection.MethodInfo getEvents = null;
                    foreach (System.Reflection.MethodInfo m in bank.GetType().GetMethods())
                        if (m.Name == "getEventList" && m.GetParameters().Length == 1) { getEvents = m; break; }
                    if (getEvents == null) continue;
                    object[] ea = new object[1];
                    getEvents.Invoke(bank, ea);
                    if (ea[0] == null) continue;
                    foreach (object ev in (System.Array)ea[0])
                    {
                        System.Reflection.MethodInfo getPath = null;
                        foreach (System.Reflection.MethodInfo m in ev.GetType().GetMethods())
                            if (m.Name == "getPath" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.GetElementType() == typeof(string)) { getPath = m; break; }
                        if (getPath == null) continue;
                        object[] pa = new object[1];
                        getPath.Invoke(ev, pa);
                        if (pa[0] != null) lines.Add((string)pa[0]);
                    }
                }
                lines.Sort();
                System.IO.File.WriteAllLines(path, lines.ToArray());
                WallBuilder.Log("sound list written: " + path + " (" + lines.Count + " events)");
            }
            catch (System.Exception) { }
        }

        public static void Play(WorldTile at)
        {
            if (!HORN_ENABLED || failed || at == null) return;
            try
            {
                if (!MusicBox.sounds_on) return;
                if (at.zone != null && !at.zone.visible) return;
                System.Type tClip = System.Type.GetType("UnityEngine.AudioClip, UnityEngine.AudioModule");
                System.Type tSrc = System.Type.GetType("UnityEngine.AudioSource, UnityEngine.AudioModule");
                if (tClip == null || tSrc == null) { failed = true; return; }
                if (clip == null) Build(tClip);
                UnityEngine.GameObject go = new UnityEngine.GameObject("MymodHorn");
                object src = go.AddComponent(tSrc);
                tSrc.GetProperty("clip").SetValue(src, clip, null);
                tSrc.GetProperty("volume").SetValue(src, UnityEngine.Mathf.Clamp01(HORN_VOLUME), null);
                tSrc.GetProperty("spatialBlend").SetValue(src, 0f, null);
                tSrc.GetMethod("Play", new System.Type[0]).Invoke(src, null);
                UnityEngine.Object.Destroy(go, clip_len + 0.5f);
                WallBuilder.Log("horn played at " + at.x + "," + at.y + " (volume " + HORN_VOLUME + ")");
            }
            catch (System.Exception e) { failed = true; WallBuilder.Log("horn failed: " + e.Message); }
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
                    WallBuilder.Log("perf: avg " + (total_ms / frames).ToString("0.00") + " ms/frame, worst " + worst_ms.ToString("0.0")
                        + " ms, frames " + frames + ", city ticks " + calls + ", " + WallBuilder.StateInfo());
                total_ms = 0; worst_ms = 0; frames = 0; calls = 0;
            }
        }
    }

    public class SiegeCamp
    {
        public City attacker;
        public City target;
        public Kingdom kingdom;
        public WorldTile center;
        public List<WorldTile> pave = new List<WorldTile>();
        public List<WorldTile> palisade = new List<WorldTile>();
        public int pave_total;
        public List<TileChange> changed = new List<TileChange>();
        public Building[] tents = new Building[6];
        public Building catapult;
        public Building[] extra_guns = new Building[2];      // второе и третье орудие больших армий
        public int[] extra_builds = new int[2];
        public int ticks;
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
            "....oyo....",
            "...oYYYo...",
            "..tSSSSSt..",
            "Ss.sS.Ss.sS",
            "SssSssSssSs",
            "tStStStStSt",
            ".ssssssStt.",
            ".sstFFftSt.",
            ".sdSFFfsdt.",
            ".sdSFFfSdt.",
            ".sssFFfttt.",
            ".ssSFFftSt.",
            ".sssfnnStt.",
            ".ssssnssSt.",
            ".sdtSsssdt.",
            ".sdSsdSSdt.",
            ".sSstdSstt.",
            ".sSstsssSt.",
            ".sssSSsStt.",
            ".sstSsssSt.",
            ".ssssssStt.",
            "SsssDmDssSt",
            "StstDDDSStS",
            "SSStDmDSSsS",
            "SgssDgDtSgS",
            "gggZ..Z.ZgZ"
        };
        static readonly string[] GRID_DWARF = new string[] {
            "......PFFFf..",
            "......PFfn...",
            "......P......",
            "Ti.TT.I.Ii.IT",
            "IcicciiiIIiIc",
            "JJJJJJJJJJJJJ",
            "TiBBIIiiIIBJJ",
            "TudiTJdIiudiJ",
            "TcdIIcdTTcdiJ",
            "TuTJIJTIuBuiJ",
            "CcCcCcCcCcCcC",
            "TIuJJcJJJJIJJ",
            "TIIiiTTcciiJJ",
            "TcuiJDDDJiJiJ",
            "TiBBDDmDDTTiJ",
            "TBcJDDmDDTJiJ",
            "TIIcDDmDDcciJ",
            "JJgZDDgDDZuNN",
            ".g...Z..Zgggg"
        };
        static readonly string[] GRID_ORC = new string[] {
            "...............",
            "...............",
            "...............",
            ".......y.......",
            "..b..boyo..b...",
            "..B..BbY..bB...",
            ".bu.buBb.bBub..",
            "bBu.BuuB.BudBb.",
            "buu.DduubuDMuB.",
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
            "Z.g...Z..g.gZgg"
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
        private const int CAMP_TRIGGER_DIST = 60;          // бойцы армии не дальше этого от осаждаемого города
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
        public const string FIRE_ID = "siege_campfire";
        public const string BARRICADE_ID = "gate_barricade";
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
        private static int GUN_MAX_RANGE = Cfg.Int("siege_gun_max_range", 45, "дальность стрельбы орудий, клетки");
        private static float GUN_RELOAD_SPREAD = Cfg.Float("siege_gun_reload_spread", 0.25f, "случайная добавка к перезарядке (0.25 = до +25 процентов)");
        private static bool SHOT_LOG = Cfg.Bool("siege_shot_log", false, "писать в лог выстрелы орудий и отсутствие целей");
        private const int CROWD_RADIUS = 8;            // лагерь не ставят там, где стоят свои бойцы
        private const int CROWD_MAX = 3;
        private const float RECALL_SECONDS = 4f;
        private const int REBUILDS = 1;                // сколько раз можно восстановить разрушенную палатку/орудие
        private static float TREBUCHET_DAMAGE = Cfg.Float("trebuchet_damage", 150f, "урон ядра требушета");   // ядро (взрыв радиусом 2)
        private static float FLAG_ANIM_SPEED = Cfg.Float("flag_anim_speed", 4f, "скорость колыхания знамён на площади (кадров в секунду)");
        private const float SHAKE_TIME = 0.5f;
        private const float SHAKE_POWER = 0.22f;
        private const float TENT_HEALTH = 400f;
        private const float CATAPULT_HEALTH = 800f;
        private static float CATAPULT_DAMAGE = Cfg.Float("catapult_damage", 250f, "урон камня катапульты");     // камень
        private static float CATAPULT_RELOAD = Cfg.Float("siege_gun_reload", 4f, "перезарядка орудий, секунды");

        // Мирные жители осаждённого города возвращаются в центр
        private static bool RECALL_CIVILIANS = Cfg.Bool("recall_civilians", true, "мирные жители осаждённого города бегут к центру");
        private const int RECALL_EVERY = 8;
        private const float RECALL_INSIDE = 16f;       // дальше стольких клеток от центра города гражданских зовут домой
        private const int RECALL_MAX = 25;             // за один проход

        private static readonly int[,] TENT_SLOTS = new int[,] { { -7, -3 }, { 7, -3 }, { -7, 5 }, { 7, 5 }, { 0, -8 }, { 0, 8 } };

        private static readonly Dictionary<City, SiegeCamp> camps = new Dictionary<City, SiegeCamp>();
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

        public static bool IsShooterId(string id) { return id == CATAPULT_ID || id == TREBUCHET_ID; }
        public static bool IsPropId(string id) { return id == PROP_BARREL || id == PROP_CART || id == PROP_AMMO || id == PROP_BOAT || id == PROP_ANCHOR || id == PROP_NETS; }
        public static bool IsSiegeId(string id) { return id == TENT_ID || id == FIRE_ID || id == TOWER_ID || id == BARRICADE_ID || id == BARRICADE_L_ID || id == BARRICADE_R_ID || IsPropId(id) || IsShooterId(id) || IsDecorId(id); }
        public static bool IsDecorId(string id) { return id == FLAG_ID || id == TUSK_ID || id == BANNER_ID || id == BANNER_S_ID || id == TUSK2_ID || id == TUSK3_ID; }

        public static UnityEngine.Sprite[] GetSprites(string id)
        {
            UnityEngine.Sprite[] cached;
            if (sprite_cache.TryGetValue(id, out cached)) return cached;

            UnityEngine.Sprite mini = null;
            BuildingAsset real = AssetManager.buildings.get("watch_tower_human");
            foreach (UnityEngine.Sprite sp in real.loadBuildingSpriteList())
                if (sp.name.StartsWith("mini")) mini = sp;

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
                    : new string[][] { id == TREBUCHET_ID ? GRID_TREBUCHET : GRID_CATAPULT };
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
            string rockId = MakeProjectile("siege_rock", "rock", "fx_explosion_tiny", 0.22f, 0.12f, "event:/SFX/EXPLOSIONS/ExplosionSmall");
            string ballId = MakeProjectile("siege_cannonball", "cannonball", null, 0.32f, 0.09f, "event:/SFX/EXPLOSIONS/ExplosionSmall");
            SetBlast(rockId, "siege_blast_rock", (int)CATAPULT_BLAST, 1);
            SetBlast(ballId, "siege_blast_ball", (int)TREBUCHET_BLAST, 2);
            MakeAsset(real, CATAPULT_ID, CATAPULT_HEALTH, CATAPULT_DAMAGE, rockId);
            MakeAsset(real, TREBUCHET_ID, CATAPULT_HEALTH, TREBUCHET_DAMAGE, ballId);
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
            MakeLogAssets();
        }

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
                        WallBuilder.Log("beachhead removed (" + why + ")");
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
            WallBuilder.Log("beachhead landed: " + owner.name + " near " + target.name + " at " + land.x + "," + land.y + " (warriors landed: " + info.peak + ")");
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
                        WallBuilder.Log("fleet sighted: " + hit.kingdom.name + " -> " + hit.city.name + " (warriors on boats: " + hit.warriors + ")");
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

        private static bool Ru()
        {
            return LocalizedTextManager.instance != null && LocalizedTextManager.instance.language == "ru";
        }

        private static void MakeLogAssets()
        {
            AddLog(LOG_START, "ui/Icons/iconWar", Toolbox.color_log_warning,
                "Началась осада города $1: армия королевства $2 разбила лагерь у стен",
                "The siege of $1 has begun: the army of $2 has set up camp");
            AddLog(LOG_LIFTED, "ui/Icons/actor_traits/iconPacifist", Toolbox.color_log_good,
                "Осада города $1 снята, город отстоял свои стены",
                "The siege of $1 is lifted, the city has held");
            AddLog(LOG_RUINED, "ui/Icons/iconDead", Toolbox.color_log_warning,
                "Город $1 разгромлен и опустел, осада закончена",
                "$1 has been wiped out and left empty, the siege is over");
            AddLog(LOG_FLEET, "ui/Icons/iconWar", Toolbox.color_log_warning,
                "Флот королевства $2 замечен у берегов города $1",
                "A fleet of $2 has been sighted off the coast of $1");
            AddLog(LOG_WALL_INNER, "ui/Icons/iconCitySelect", Toolbox.color_log_neutral,
                "Город $1 обнёс себя крепостной стеной",
                "$1 has surrounded itself with a defensive wall");
            AddLog(LOG_WALL_OUTER, "ui/Icons/iconCitySelect", Toolbox.color_log_neutral,
                "Город $1 достроил внешние стены",
                "$1 has completed its outer walls");
            AddLog(LOG_FELL, "ui/Icons/iconDead", Toolbox.color_log_warning,
                "Город $1 пал, выжившие защитники сдались королевству $2",
                "$1 has fallen, the surviving defenders surrendered to $2");
        }

        private static readonly Dictionary<string, string[]> log_texts = new Dictionary<string, string[]>();

        private static void AddLog(string id, string icon, UnityEngine.Color color, string ru, string en)
        {
            log_texts[id] = new string[] { ru, en };
            if (AssetManager.world_log_library.get(id) != null) return;
            WorldLogAsset a = new WorldLogAsset();
            a.id = id;
            a.group = "wars";
            a.path_icon = icon;
            a.color = color;
            a.text_replacer = delegate (WorldLogMessage m, ref string t)
            {
                t = (Ru() ? ru : en).Replace("$1", m.getSpecial(1)).Replace("$2", m.getSpecial(2));
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
                string[] tx;
                if (log_texts.TryGetValue(id, out tx))      // игра ищет текст события в своих переводах: регистрируем свой
                    LocalizedTextManager.add(id, Ru() ? tx[0] : tx[1], true, "", false);
                WorldLogMessage m = new WorldLogMessage(AssetManager.world_log_library.get(id), city.name, other.name);
                m.color_special1 = city.kingdom.getColor().getColorText();
                m.color_special2 = other.getColor().getColorText();
                if (where != null) m.location = new UnityEngine.Vector2(where.x, where.y);
                m.add();
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

        // Выстрел: облако дыма и пыли вокруг орудия, у требушета ещё хлопок, у катапульты скрип дерева
        private static void MuzzleEffects(Building b)
        {
            try
            {
                WorldTile mt = b.current_tile;
                if (mt == null) return;
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

        // Тряска орудия при выстреле
        public static void ShakePostfix(BuildingTower __instance)
        {
            try
            {
                Building b = __instance.building;
                if (b == null || b.asset == null || !IsShooterId(b.asset.id)) return;
                b.startShake(SHAKE_TIME, SHAKE_POWER, SHAKE_POWER * 0.5f);
                MuzzleEffects(b);
                GateShot(b);
                if (SHOT_LOG) WallBuilder.Log("siege gun fired: " + b.asset.id + " at " + b.current_tile.x + "," + b.current_tile.y);
            }
            catch (System.Exception) { }
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
        public static void CatapultTargetPostfix(BuildingTower __instance, ref BaseSimObject __result)
        {
            try
            {
                Building b = __instance.building;
                if (b != null && b.asset != null && Coast.IsCoastId(b.asset.id)) { CoastTarget(b, ref __result); return; }
                if (b == null || b.asset == null || !IsShooterId(b.asset.id)) return;
                BaseSimObject pick = null;
                int seen = 0, outside = 0;
                // запертый город: целимся в здания у ворот, по которым идёт обстрел
                SiegeCamp gc = CampOf(b);
                WorldTile gateAt = null;
                if (gc != null && gc.target != null && WallBuilder.GateFocus(gc.target, b.current_tile, out gateAt) && gateAt != null)
                {
                    int gseen = 0;
                    foreach (Building tb in World.world.buildings.getBuildingFromZones(gateAt, 10))
                    {
                        if (tb == null || tb.asset == null || !tb.asset.city_building || !tb.isUsable() || tb.current_tile == null) continue;
                        if (tb.kingdom == null || !tb.kingdom.isEnemy(b.kingdom)) continue;
                        float gd = Toolbox.Dist(b.current_tile.x, b.current_tile.y, tb.current_tile.x, tb.current_tile.y);
                        if (gd < GUN_MIN_RANGE || gd > GUN_MAX_RANGE) continue;
                        if (Toolbox.Dist(gateAt.x, gateAt.y, tb.current_tile.x, tb.current_tile.y) > 10f) continue;
                        gseen++;
                        if (Randy.randomInt(0, gseen) == 0) pick = tb;
                    }
                    if (pick != null) { __result = pick; return; }
                }
                // здания перебираем по зонам карты: поиск врагов в игре останавливается на ближайшем кольце чанков и зданий мог не найти
                foreach (Building tb in World.world.buildings.getBuildingFromZones(b.current_tile, GUN_MAX_RANGE))
                {
                    if (tb == null || tb.asset == null || !tb.asset.city_building || !tb.isUsable() || tb.current_tile == null) continue;
                    if (tb.kingdom == null || !tb.kingdom.isEnemy(b.kingdom)) continue;
                    float d = Toolbox.Dist(b.current_tile.x, b.current_tile.y, tb.current_tile.x, tb.current_tile.y);
                    if (d < GUN_MIN_RANGE || d > GUN_MAX_RANGE) { outside++; continue; }
                    seen++;
                    if (Randy.randomInt(0, seen) == 0) pick = tb;
                }
                __result = pick;
                if (pick == null && SHOT_LOG)
                {
                    double now = World.world.getCurWorldTime();
                    if (now >= next_no_target_log)
                    {
                        next_no_target_log = now + 20.0;
                        WallBuilder.Dbg("siege gun at " + b.current_tile.x + "," + b.current_tile.y + ": no target in range (outside range: " + outside + ")");
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
                float t = CATAPULT_RELOAD * (1f + Randy.randomFloat(0f, GUN_RELOAD_SPREAD));
                Traverse.Create(__instance).Field("_check_targets_timeout").SetValue(t);
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
            if (!sieges.TryGetValue(attacker, out info) || info.target != target || info.hold_done) return false;
            SiegeCamp camp;
            if (!camps.TryGetValue(attacker, out camp) || camp.catapult == null || !camp.catapult.isUsable()) return false;
            if (!target.isAlive() || target.kingdom == null || attacker.kingdom == null || !target.kingdom.isEnemy(attacker.kingdom)) return false;
            if (!attacker.hasAnyWarriors() || !WallBuilder.GatesSealed(target)) return false;
            double now = World.world.getCurWorldTime();
            if (info.hold_since < 0)
            {
                info.hold_since = now;
                WallBuilder.Log("siege hold: " + attacker.name + " keeps the army at " + target.name + " (max " + (int)HOLD_SECONDS + " s)");
            }
            if (now - info.hold_since > HOLD_SECONDS)
            {
                info.hold_done = true;
                WallBuilder.Log("siege hold over at " + target.name + " (timeout)");
                return false;
            }
            return true;
        }

        internal static bool IsHolding(City attacker)
        {
            SiegeInfo info;
            return attacker != null && sieges.TryGetValue(attacker, out info) && info.hold_since >= 0 && !info.hold_done;
        }

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
                if (__state == null || pCity == null || pCity.target_attack_city != null) return;
                if (!HoldFor(pCity, __state.target)) return;
                pCity.target_attack_city = __state.target;
                pCity.target_attack_zone = __state.zone;
            }
            catch (System.Exception) { }
        }

        // Выстрел орудия: если его цель - запертый город, ворота теряют прочность
        private static void GateShot(Building gun)
        {
            SiegeCamp camp = CampOf(gun);
            if (camp == null || camp.target == null || gun.current_tile == null) return;
            if (!WallBuilder.GatesSealed(camp.target)) return;
            float dmg = gun.asset.id == TREBUCHET_ID ? GATE_DMG_TREBUCHET : GATE_DMG_CATAPULT;
            if (WALL_SHARE > 0 && Randy.randomInt(0, 100) < WALL_SHARE && WallBuilder.WallDamage(camp.target, gun.current_tile, dmg)) return;
            WallBuilder.GateDamage(camp.target, gun.current_tile, dmg);
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
        private static int Gather(Army army, WorldTile at, int radius, out WorldTile mass, out List<WorldTile> crowd)
        {
            mass = null;
            crowd = new List<WorldTile>();
            if (army == null || at == null) return 0;
            int n = 0;
            long sx = 0, sy = 0;
            float r2 = (float)radius * radius;
            List<Actor> list = army.units;
            for (int i = 0; i < list.Count; i++)
            {
                Actor a = list[i];
                if (a == null || !a.isAlive() || a.current_tile == null || a.is_inside_boat) continue;
                float dx = a.current_tile.x - at.x, dy = a.current_tile.y - at.y;
                if (dx * dx + dy * dy > r2) continue;
                if (!a.current_tile.isSameIsland(at)) continue;
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
            ProcessDying();
            double now = World.world.getCurWorldTime();

            SiegeCamp camp;
            camps.TryGetValue(city, out camp);
            SiegeInfo info;
            sieges.TryGetValue(city, out info);
            City target = city.target_attack_city;
            Army army = city.army;

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

                WorldTile at = camp != null ? camp.center : t.getTile();
                int near = NearCount(army, at, CAMP_ARMY_RADIUS);
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
                    Build(camp, near);
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
                if (other.target == target) return;     // у этого города уже стоит лагерь

            WorldTile tc = target.getTile();
            WorldTile ac = city.getTile();
            if (tc == null || ac == null) return;
            bool sameIsland = tc.isSameIsland(ac);

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
            if (size < CAMP_MIN_ARMY) { if (size * 3 >= CAMP_MIN_ARMY) NoCamp(city, target, "army near target too small (" + size + " < " + CAMP_MIN_ARMY + ")"); return; }

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
                WallBuilder.Log("siege camp: no site found near " + tc.x + "," + tc.y);
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
                Horn.Play(site);
            }
            info.camps_made++;
            info.low_since = -1;
            WallBuilder.Log("siege camp planned at " + site.x + "," + site.y + " (army " + size + ", pave " + camp.pave.Count + ")");
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
            double gap = why.StartsWith("target city has no") ? 600.0 : 120.0;
            if (why_text.TryGetValue(city, out last) && last == why && why_time.TryGetValue(city, out t) && now - t < gap) return;
            why_text[city] = why;
            why_time[city] = now;
            WallBuilder.Dbg("no siege camp " + city.name + " -> " + target.name + ": " + why);
        }

        // Конец осады: убираем лагерь и, если нужно, сообщаем в новостях
        private static void EndSiege(City attacker, SiegeCamp camp, SiegeInfo info, string news, string why)
        {
            if (camp != null) StartRemoval(camp, why);
            else WallBuilder.Log("siege ended (" + why + ")");
            sieges.Remove(attacker);
            approached.Remove(attacker);
            cooldown[attacker] = World.world.getCurWorldTime() + CAMP_RETRY_SECONDS;
            if (news != null && info.target != null) Announce(news, info.target, info.kingdom, info.target.getTile());
        }

        private static void StartRemoval(SiegeCamp camp, string why)
        {
            WallBuilder.Log("siege camp removed (" + why + ")");
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
                foreach (SiegeCamp c in camps.Values)
                {
                    for (int i = 0; i < c.tents.Length; i++) if (c.tents[i] != null) active.Add(c.tents[i]);
                    if (c.catapult != null) active.Add(c.catapult);
                    for (int i = 0; i < c.extra_guns.Length; i++) if (c.extra_guns[i] != null) active.Add(c.extra_guns[i]);
                    if (c.fire != null) active.Add(c.fire);
                    if (c.banner != null) active.Add(c.banner);
                    if (c.tower != null) active.Add(c.tower);
                    for (int i = 0; i < c.props.Length; i++) if (c.props[i] != null) active.Add(c.props[i]);
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
                if (removed > 0) WallBuilder.Log("siege: leftover camp buildings removed: " + removed);
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
            WorldTile tc = target.getTile();
            WorldTile ac = attacker.getTile();
            if (tc == null || ac == null) return null;
            WorldTile dirTile = mass != null ? mass : ac;
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

        private static bool SiteOk(WorldTile t, City attacker, City target, WorldTile targetTile, List<WorldTile> crowd)
        {
            if (t == null || !t.isSameIsland(targetTile)) return false;
            // стены квадратные: по диагонали расстояние по прямой обманывает, поэтому считаем по большей из осей
            if (System.Math.Max(System.Math.Abs(t.x - targetTile.x), System.Math.Abs(t.y - targetTile.y)) < WallBuilder.OuterRadius(target) + PALISADE_RADIUS + 6) return false;
            if (crowd != null)
            {
                int near = 0;
                for (int i = 0; i < crowd.Count; i++)
                {
                    WorldTile w = crowd[i];
                    if (System.Math.Abs(w.x - t.x) <= CROWD_RADIUS && System.Math.Abs(w.y - t.y) <= CROWD_RADIUS && ++near > CROWD_MAX) return false;
                }
            }
            int civ_hits = 0;
            int[] core = new int[] { 0, 0, 8, 0, -8, 0, 0, 8, 0, -8, 8, 8, -8, 8, 8, -8, -8, -8 };
            for (int i = 0; i < core.Length; i += 2)
            {
                WorldTile n = World.world.GetTile(t.x + core[i], t.y + core[i + 1]);
                if (n == null || n.is_liquid || n.Type.block || n.Type.wall) return false;
                if (WallBuilder.IsCiv(n) && ++civ_hits > 3) return false;
                if (ZoneBlocked(n, attacker.kingdom, target.kingdom, target)) return false;
            }
            int R = PALISADE_RADIUS;
            int[] ring = new int[] { R, 0, -R, 0, 0, R, 0, -R, R, R, -R, R, R, -R, -R, -R };
            for (int i = 0; i < ring.Length; i += 2)
            {
                WorldTile n = World.world.GetTile(t.x + ring[i], t.y + ring[i + 1]);
                if (n == null) return false;
                if (ZoneBlocked(n, attacker.kingdom, target.kingdom, target)) return false;
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
                    if (t != null) camp.palisade.Add(t);
                }
            }
        }

        private static bool Usable(WorldTile t, SiegeCamp camp)
        {
            if (t == null || t.is_liquid || t.Type.block || t.Type.wall || t.Type.id == "field") return false;
            return !ZoneBlocked(t, camp.kingdom, camp.target != null ? camp.target.kingdom : null, camp.target);
        }

        // Клетка свободна для строения; мелкую природу на ней сносим и пробуем в следующий раз
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

        internal static Building Put(string id, WorldTile t, Kingdom k)
        {
            BuildingAsset a = AssetManager.buildings.get(id);
            if (a == null || t == null) return null;
            Building b = World.world.buildings.addBuilding(a, t);
            if (b == null) return null;
            b.setKingdom(k);
            return b;
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
                        WallBuilder.Dbg("siege " + (camp.shooter_id == TREBUCHET_ID ? "trebuchet" : "catapult") + " placed at " + t.x + "," + t.y + " (army " + size + ")");
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
                    string gid = (k == 0) == (camp.shooter_id == CATAPULT_ID) ? TREBUCHET_ID : CATAPULT_ID;
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
}
