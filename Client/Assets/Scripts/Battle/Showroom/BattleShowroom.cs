#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MyDefense.Battle.Showroom
{
    public static class BattleShowroomScenes
    {
        public const string Hub = "Assets/Scenes/Tests/FeatureTestHub.unity";
        public const string Directory = "Assets/Scenes/Tests/Battle";
        public const string Character = Directory + "/BattleShowroomCharacter.unity";
        public const string Combat = Directory + "/BattleShowroomCombat.unity";
        public const string Content = Directory + "/BattleShowroomContent.unity";
        public const string CharacterId = "P0-11-SHOWROOM-CHARACTER";
        public const string CombatId = "P0-11-SHOWROOM-COMBAT";
        public const string ContentId = "P0-11-SHOWROOM-CONTENT";

        public static bool IsShowroomScene(string path)
            => string.Equals(path, Character, StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, Combat, StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, Content, StringComparison.OrdinalIgnoreCase);

        public static void OpenInPlayMode(string path)
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(path, LoadSceneMode.Single);
#endif
        }
    }

    public sealed class BattleShowroomLaunch
    {
        private static BattleShowroomLaunch _pending;
        public string Map { get; }
        public int InitialWave { get; }
        public string DailyContent { get; }
        public int DailyStage { get; }

        public BattleShowroomLaunch(string map, int initialWave, string dailyContent = null, int dailyStage = 0)
        {
            Map = map;
            InitialWave = initialWave;
            DailyContent = dailyContent;
            DailyStage = dailyStage;
        }

        public void OpenCombat()
        {
            _pending = this;
            BattleShowroomScenes.OpenInPlayMode(BattleShowroomScenes.Combat);
        }

        public static bool TryConsume(out BattleShowroomLaunch launch)
        {
            launch = _pending;
            _pending = null;
            return launch != null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearLaunch() => _pending = null;
    }

    public static class BattleShowroomPreferences
    {
        private const string Prefix = "MyDefense.BattleShowroom.v1.";

        public static T Load<T>(string sceneId) where T : class, new()
        {
            string json = PlayerPrefs.GetString(Prefix + sceneId);
            if (string.IsNullOrEmpty(json))
                return new T();

            try
            {
                return JsonUtility.FromJson<T>(json) ?? new T();
            }
            catch (ArgumentException)
            {
                return new T();
            }
        }

        public static void Save<T>(string sceneId, T value)
        {
            PlayerPrefs.SetString(Prefix + sceneId, JsonUtility.ToJson(value));
            PlayerPrefs.Save();
        }

        public static void Clear(string sceneId)
        {
            PlayerPrefs.DeleteKey(Prefix + sceneId);
            PlayerPrefs.Save();
        }
    }

    public abstract class BattleShowroomWindow : MonoBehaviour
    {
        private Vector2 _scroll;
        private static readonly Color PanelBackground = new(0.06f, 0.09f, 0.14f);

        private static float ControlsWidth => Mathf.Min(360f, Mathf.Max(120f, Screen.width * 0.49f - 24f));
        protected static float PreviewStart => Mathf.Clamp01((ControlsWidth + 24f) / Mathf.Max(1, Screen.width));

        protected abstract string Title { get; }
        protected abstract string SessionDescription { get; }
        protected abstract void DrawControls();
        protected abstract void ResetControls();
        protected virtual void ReturnToHub() => BattleShowroomScenes.OpenInPlayMode(BattleShowroomScenes.Hub);

        protected virtual void OnGUI()
        {
            Color previousColor = GUI.color;
            GUI.color = PanelBackground;
            GUI.DrawTexture(new Rect(0f, 0f, ControlsWidth + 24f, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
            bool previousLabelWrap = GUI.skin.label.wordWrap;
            bool previousBoxWrap = GUI.skin.box.wordWrap;
            GUI.skin.label.wordWrap = true;
            GUI.skin.box.wordWrap = true;
            GUILayout.BeginArea(new Rect(12f, 12f, ControlsWidth, Mathf.Max(80f, Screen.height - 24f)), GUI.skin.window);
            GUILayout.Label(Title, GUI.skin.box);
            GUILayout.Label(SessionDescription);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Feature Test Hub"))
                ReturnToHub();
            if (GUILayout.Button("Reset"))
                ResetControls();
            GUILayout.EndHorizontal();
            _scroll = GUILayout.BeginScrollView(_scroll, false, false);
            DrawControls();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.skin.label.wordWrap = previousLabelWrap;
            GUI.skin.box.wordWrap = previousBoxWrap;
        }
    }
}
#endif
