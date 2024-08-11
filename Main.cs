using Kitchen;
using KitchenData;
using KitchenLib;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMods;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace KitchenMoreSpeedrunInfo
{
    internal struct SpeedrunEntry
    {
        public int Rank;
        public SteamId PlayerID;
        public string PlayerName;
        public TimeSpan RunTime;
        public string RunTimeString;
    }

    public class Main : BaseMod, IModSystem
    {
        // GUID must be unique and is recommended to be in reverse domain name notation
        // Mod Name is displayed to the player and listed in the mods menu
        // Mod Version must follow semver notation e.g. "1.2.3"
        public const string MOD_GUID = "IcedMilo.PlateUp.MoreSpeedrunInfo";
        public const string MOD_NAME = "MoreSpeedrunInfo";
        public const string MOD_VERSION = "0.2.6";
        public const string MOD_AUTHOR = "IcedMilo";
        public const string MOD_GAMEVERSION = ">=1.1.5";
        // Game version this mod is designed for in semver
        // e.g. ">=1.1.3" current and all future
        // e.g. ">=1.1.3 <=1.2.3" for all from/until

        // Boolean constant whose value depends on whether you built with DEBUG or RELEASE mode, useful for testing
#if DEBUG
        public const bool DEBUG_MODE = true;
#else
        public const bool DEBUG_MODE = false;
#endif

        public static AssetBundle Bundle;

        static bool refreshLeaderboard = true;
        internal static int RequestWeek;
        internal static int RequestYear;

        internal static bool IsLoading { get; private set; } = true;
        internal static List<SpeedrunEntry> SpeedrunData = new List<SpeedrunEntry>();
        internal static string SpeedrunDish = "";
        internal static string SpeedrunSetting = "";
        internal static string SpeedrunSeed = "";
        internal static int LoadedWeek;
        internal static int LoadedYear;

        public Main() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, MOD_VERSION, MOD_GAMEVERSION, Assembly.GetExecutingAssembly()) { }

        protected override void OnInitialise()
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
            RegisterMenu<SpeedrunMenu>();
            (RequestYear, RequestWeek) = SpeedrunHelpers.CurrentLeaderboardYearAndWeek();
            LoadedWeek = RequestWeek;
            LoadedYear = RequestYear;
        }

        protected override async void OnUpdate()
        {
            if (refreshLeaderboard)
            {
                refreshLeaderboard = false;

                Leaderboard? lb = await SpeedrunHelpers.GetLeaderboard(RequestYear, RequestWeek);
                if (lb.HasValue)
                {
                    List<SpeedrunEntry> newData = new List<SpeedrunEntry>();
                    for (int i = 0; i < 1000000; i++)
                    {
                        Leaderboard leaderboard = lb.Value;
                        LeaderboardEntry[] page = await leaderboard.GetScoresAsync(99, i * 100 + 1);
                        if (page == null || page.Length == 0)
                        {
                            break;
                        }
                        for (int j = 0; j < page.Length; j++)
                        {
                            TimeSpan timeSpan = new TimeSpan(page[j].Score * 10000L);
                            string runTimeString = String.Format("{0}:{1:00}.{2:000}", (int)timeSpan.TotalMinutes, timeSpan.Seconds, timeSpan.Milliseconds);
                            newData.Add(new SpeedrunEntry
                            {
                                PlayerID = page[j].User.Id,
                                PlayerName = page[j].User.Name,
                                Rank = page[j].GlobalRank,
                                RunTime = timeSpan,
                                RunTimeString = runTimeString
                            });
                        }
                    }
                    SpeedrunData = newData;
                }

                LoadedWeek = RequestWeek;
                LoadedYear = RequestYear;

                (SpeedrunSeed, SpeedrunSetting, SpeedrunDish) = GetSpeedrunDetails(LoadedYear, LoadedWeek);
                IsLoading = false;
            }
        }


        internal static (string seed, string setting, string dish) GetSpeedrunDetails(int year, int week)
        {
            int source = year * 200 + week;
            LayoutSeed layoutSeed = new LayoutSeed(source);
            string seed = layoutSeed.FixedSeed.Value.Value.ToUpper();
            using FixedSeedContext fixedSeedContext = new FixedSeedContext(layoutSeed.FixedSeed, 8853129);
            string setting;
            string dish;
            using (fixedSeedContext.UseSubcontext(0))
            {
                int settingID = Kitchen.RandomExtensions.Random(AssetReference.FixedRunSetting);
                setting = (GDOUtils.GetExistingGDO(settingID) as RestaurantSetting).Info.Get().Name;
            }
            using (fixedSeedContext.UseSubcontext(1))
            {
                Dish dishGDO;
                if (year <= 2023 && week < 27)
                {
                    int dishID = Kitchen.RandomExtensions.Random(new int[]
                    {
                        DishReferences.SteakBase,
                        DishReferences.FishBase,
                        DishReferences.StirFryBase,
                        DishReferences.PieBase
                    });
                    dishGDO = GDOUtils.GetExistingGDO(dishID) as Dish;
                }
                else
                {
                    dishGDO = Kitchen.RandomExtensions.Random((from x in GameData.Main.Get<Dish>()
                                                                    where x.IsSpeedrunDish
                                                                    select x).ToList());
                }
                dish = dishGDO.Info.Get().Name;
            }
            return (seed, setting, dish);
        }

        internal static void RequestLeaderboard(DateTime dateTime)
        {
            (RequestYear, RequestWeek) = GetSpeedrunYearAndWeek(dateTime);
            if (!IsLoading && !(LoadedWeek == RequestWeek && LoadedYear == RequestYear))
            {
                refreshLeaderboard = true;
                IsLoading = true;
            }
        }

        private static (int, int) GetSpeedrunYearAndWeek(DateTime dateTime)
        {
            GregorianCalendar gregorianCalendar = new GregorianCalendar();
            int weekOfYear = gregorianCalendar.GetWeekOfYear(dateTime, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            int year = gregorianCalendar.GetYear(dateTime);
            return (year, weekOfYear);
        }

        private static void LogLookAhead(int toYear, int fromYear = 2023)
        {
            LogInfo("Speedrun lookahead");
            for (int i = fromYear; i < toYear + 1; i++)
            {
                for (int j = 1; j < GetWeeksInYear(i) + 1; j++)
                {
                    (string seed, string setting, string dish) speedrunDetails = GetSpeedrunDetails(i, j);
                    LogInfo($"{i},{j},{speedrunDetails.seed},{speedrunDetails.setting},{speedrunDetails.dish}");
                }
            }
        }

        internal static int GetWeeksInYear(int year)
        {
            GregorianCalendar gregorianCalendar = new GregorianCalendar();
            DateTime date1 = new DateTime(year, 12, 31);
            return gregorianCalendar.GetWeekOfYear(date1, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }

        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
        }
        #region Logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }
}
