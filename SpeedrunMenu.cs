using KitchenLib.DevUI;
using KitchenLib.Utils;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace KitchenMoreSpeedrunInfo
{
    public class SpeedrunMenu : BaseUI
    {
        protected GUIStyle LabelLeftStyle { get; private set; }
        protected GUIStyle LabelCentreStyle { get; private set; }
        protected GUIStyle LeaderboardLabelLeftStyle { get; private set; }
        protected GUIStyle LeaderboardLabelCentreStyle { get; private set; }
        protected GUIStyle LeaderboardLabelHeaderStyle { get; private set; }
        protected GUIStyle LeaderboardTop1LeftStyle { get; private set; }
        protected GUIStyle LeaderboardTop1CentreStyle { get; private set; }

        protected Texture2D Background { get; private set; }

        public SpeedrunMenu()
        {
            ButtonName = "Speedrun";
        }

        public sealed override void OnInit()
        {
            Background = new Texture2D(64, 64);
            UnityEngine.Color grayWithAlpha = new UnityEngine.Color(0.2f, 0.2f, 0.2f, 0.6f);
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    Background.SetPixel(x, y, grayWithAlpha);
                }
            }
            Background.Apply();
            OnInitialise();
        }

        public sealed override void Setup()
        {
            if (LabelLeftStyle == null)
            {
                LabelLeftStyle = new GUIStyle(GUI.skin.label);
                LabelLeftStyle.alignment = TextAnchor.MiddleLeft;
                LabelLeftStyle.padding.left = 10;
                LabelLeftStyle.stretchWidth = true;
            }

            if (LabelCentreStyle == null)
            {
                LabelCentreStyle = new GUIStyle(GUI.skin.label);
                LabelCentreStyle.alignment = TextAnchor.MiddleCenter;
                LabelCentreStyle.stretchWidth = true;
            }

            if (LeaderboardLabelLeftStyle == null)
            {
                LeaderboardLabelLeftStyle = new GUIStyle(GUI.skin.label);
                LeaderboardLabelLeftStyle.alignment = TextAnchor.MiddleLeft;
                LeaderboardLabelLeftStyle.padding.left = 10;
                LeaderboardLabelLeftStyle.stretchWidth = true;
                LeaderboardLabelLeftStyle.fontSize = 14;
            }


            if (LeaderboardLabelCentreStyle == null)
            {
                LeaderboardLabelCentreStyle = new GUIStyle(GUI.skin.label);
                LeaderboardLabelCentreStyle.alignment = TextAnchor.MiddleCenter;
                LeaderboardLabelCentreStyle.stretchWidth = true;
                LeaderboardLabelCentreStyle.fontSize = 14;
            }


            if (LeaderboardLabelHeaderStyle == null)
            {
                LeaderboardLabelHeaderStyle = new GUIStyle(GUI.skin.label);
                LeaderboardLabelHeaderStyle.alignment = TextAnchor.MiddleCenter;
                LeaderboardLabelHeaderStyle.stretchWidth = true;
                LeaderboardLabelHeaderStyle.fontSize = 16;
                LeaderboardLabelHeaderStyle.fontStyle = FontStyle.Bold;
            }

            if (LeaderboardTop1LeftStyle == null)
            {
                LeaderboardTop1LeftStyle = new GUIStyle(GUI.skin.label);
                LeaderboardTop1LeftStyle.alignment = TextAnchor.MiddleLeft;
                LeaderboardTop1LeftStyle.padding.left = 10;
                LeaderboardTop1LeftStyle.stretchWidth = true;
                LeaderboardTop1LeftStyle.fontSize = 16;
            }


            if (LeaderboardTop1CentreStyle == null)
            {
                LeaderboardTop1CentreStyle = new GUIStyle(GUI.skin.label);
                LeaderboardTop1CentreStyle.alignment = TextAnchor.MiddleCenter;
                LeaderboardTop1CentreStyle.stretchWidth = true;
                LeaderboardTop1CentreStyle.fontSize = 16;
            }
            OnSetup();
        }


        float windowWidth = 770f;
        float windowHeight = 1050f;

        const string ALLOWED_SEED_CHARS = "abcdefghijklmnopqrstuvwxyz123456789";
        Regex containsNotAllowedCharacters;
        Regex isNumeric;

        string searchDay = "";
        string searchMonth = "";
        string searchYear = "";
        int yearNow;
        string searchSeed = "";

        bool requestedLeaderboard = false;

        readonly DateTime SPEEDRUN_INTRODUCTION_DATE = new DateTime(2023, 3, 14);

        Vector2 scrollPosition = default;

        protected const int MAX_ROWS_PER_PAGE = 300;
        private int currentPage = 0;

        protected virtual void OnInitialise()
        {
            yearNow = DateTime.UtcNow.Year;
            searchYear = DateTime.UtcNow.Year.ToString();
            searchMonth = DateTime.UtcNow.Month.ToString();
            searchDay = DateTime.UtcNow.Day.ToString();
            containsNotAllowedCharacters = new Regex($"[^{Regex.Escape(ALLOWED_SEED_CHARS)}]");
            isNumeric = new Regex(@"^\d+$");
        }

        protected virtual void OnSetup()
        {
            GUILayout.BeginArea(new Rect(10f, 10f, windowWidth, windowHeight));
            GUI.DrawTexture(new Rect(0f, 0f, windowWidth, windowHeight), Background);
            GUILayout.Label("Speedrun Stats", LabelCentreStyle);

            GUILayout.BeginHorizontal();

            GUILayout.Space(15f);

            float dateSearchWidth = (windowWidth - 60f) * 0.2f;
            float dateSearchBoxWidth = dateSearchWidth * 0.6f;
            GUILayout.BeginVertical(GUILayout.Width(dateSearchWidth));
            GUILayout.Label("Search by date", LabelCentreStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Year");
            searchYear = SanitiseIntInput(GUILayout.TextField(searchYear, GUILayout.Width(dateSearchBoxWidth)), searchYear, min: 2023, max: yearNow);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Month");
            searchMonth = SanitiseIntInput(GUILayout.TextField(searchMonth, GUILayout.Width(dateSearchBoxWidth)), searchMonth, min: 1, max: 12);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Day");
            searchDay = SanitiseIntInput(GUILayout.TextField(searchDay, GUILayout.Width(dateSearchBoxWidth)), searchDay, min: 1, max: 31);
            GUILayout.EndHorizontal();
            if (!searchDay.IsNullOrEmpty() && !searchMonth.IsNullOrEmpty() && !searchYear.IsNullOrEmpty() &&
                IsValidDate(int.Parse(searchDay), int.Parse(searchMonth), int.Parse(searchYear), out DateTime searchDate))
            {
                if (GUILayout.Button("Date Search"))
                {
                    searchDate = ClampDateTime(searchDate, SPEEDRUN_INTRODUCTION_DATE, DateTime.Now);
                    Main.RequestLeaderboard(searchDate);
                    scrollPosition = default;
                    requestedLeaderboard = true;
                }
            }
            else
            {
                GUILayout.Label("Date Search", LabelCentreStyle);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("");
            if (GUILayout.Button("Export CSV"))
            {
                GenerateCSV();
            }
            GUILayout.Label("");
            if (Main.IsLoading)
                GUILayout.Label("Waiting for data...", LabelCentreStyle);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Label("");
            GUILayout.Label($"Leaderboard (Week {Main.LoadedWeek}, Year {Main.LoadedYear})", LeaderboardLabelHeaderStyle);
            GUILayout.Label($"{Main.SpeedrunDish} - {Main.SpeedrunSeed}", LeaderboardLabelHeaderStyle);
            GUILayout.Label($"{Main.SpeedrunData.Count} Participants", LeaderboardTop1CentreStyle);
            GUILayout.Label("");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Previous"))
            {
                currentPage--;
                scrollPosition = default;
            }
            if (GUILayout.Button("Next"))
            {
                currentPage++;
                scrollPosition = default;
            }
            currentPage = Mathf.Clamp(currentPage, 0, Mathf.CeilToInt(Main.SpeedrunData.Count / (float)MAX_ROWS_PER_PAGE) - 1);

            if (requestedLeaderboard && !Main.IsLoading)
            {
                requestedLeaderboard = false;
                scrollPosition = default;
                currentPage = 0;
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            float tableWidth = windowWidth - 30f;
            float col1Width = tableWidth * 0.1f;
            float col2Width = tableWidth * 0.7f;
            float col3Width = tableWidth * 0.2f;
            GUILayout.Space(15f);
            GUILayout.Label("Rank", LeaderboardLabelHeaderStyle, GUILayout.Width(col1Width));
            GUILayout.Label("Username", LeaderboardLabelHeaderStyle, GUILayout.Width(col2Width));
            GUILayout.Label("Time", LeaderboardLabelHeaderStyle, GUILayout.Width(col3Width));
            GUILayout.Label("", GUILayout.Width(15f));
            GUILayout.EndHorizontal();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);

            for (int i = 0; i < Mathf.Min(Main.SpeedrunData.Count - currentPage * MAX_ROWS_PER_PAGE, MAX_ROWS_PER_PAGE); i++)
            {
                SpeedrunEntry speedrunEntry = Main.SpeedrunData[currentPage * MAX_ROWS_PER_PAGE + i];
                GUILayout.BeginHorizontal();
                GUILayout.Space(15f);
                bool isLeader = currentPage == 0 && i == 0;
                GUILayout.Label(speedrunEntry.Rank.ToString(), isLeader ? LeaderboardTop1CentreStyle : LeaderboardLabelCentreStyle, GUILayout.Width(col1Width));
                GUILayout.Label(speedrunEntry.PlayerName, isLeader ? LeaderboardTop1LeftStyle : LeaderboardLabelLeftStyle, GUILayout.Width(col2Width));
                GUILayout.Label(speedrunEntry.RunTimeString, isLeader ? LeaderboardTop1CentreStyle : LeaderboardLabelCentreStyle, GUILayout.Width(col3Width));
                GUILayout.Label("", GUILayout.Width(15f));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private string SanitiseIntInput(string input, string fallback = "", int min = int.MinValue, int max = int.MaxValue)
        {
            if (input.IsNullOrEmpty())
            {
                return string.Empty;
            }

            if (input == null ||
                !IsNumeric(input) ||
                !int.TryParse(input, out int num) ||
                num.ToString().Length > max.ToString().Length)
            {
                return fallback;
            }
            return Mathf.Clamp(num, min, max).ToString();
        }

        private bool IsNumeric(string input)
        {
            //DateTime.DaysInMonth()
            return isNumeric.IsMatch(input);
        }

        public static bool IsValidDate(int day, int month, int year, out DateTime date)
        {
            return DateTime.TryParse($"{month}/{day}/{year}", out date);
        }

        public static DateTime ClampDateTime(DateTime value, DateTime minValue, DateTime maxValue)
        {
            if (value < minValue)
            {
                return minValue;
            }
            else if (value > maxValue)
            {
                return maxValue;
            }
            else
            {
                return value;
            }
        }

        protected void GenerateCSV()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Leaderboard");
            sb.AppendLine($"Year,{Main.LoadedYear}");
            sb.AppendLine($"Week,{Main.LoadedWeek}");
            sb.AppendLine($"Seed,{Main.SpeedrunSeed}");
            sb.AppendLine($"Year,{Main.SpeedrunDish}");
            sb.AppendLine("Rank,Username,Time");
            for (int i = 0; i < Main.SpeedrunData.Count; i++)
            {
                SpeedrunEntry entry = Main.SpeedrunData[i];
                sb.AppendLine($"{entry.Rank},{entry.PlayerName},{entry.RunTimeString}");
            }
            DateTime dateTime = DateTime.UtcNow;
            SaveCSV($"{Main.RequestYear}_{Main.RequestWeek}", $"{dateTime.Hour}_{dateTime.Minute}_{dateTime.Second}_{dateTime.Millisecond}", sb);
        }

        protected void SaveCSV(string path, string name, StringBuilder sb)
        {
            string text = Path.Combine(Application.persistentDataPath, "Speedrun");
            if (!Directory.Exists(text))
            {
                Directory.CreateDirectory(text);
            }
            if (!Directory.Exists(Path.Combine(text, path)))
            {
                Directory.CreateDirectory(Path.Combine(text, path));
            }
            File.WriteAllText(Path.Combine(text, path, name + ".csv"), sb.ToString());
        }
    }
}
