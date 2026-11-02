using System;
using MyDefense.Auth;
using UnityEditor;
using UnityEngine;

public sealed class DevelopmentLoginWindow : EditorWindow
{
    private static readonly string[] Accounts = { "jjangash", "kingusi" };
    [NonSerialized] private string password = "";
    [NonSerialized] private string feedback;
    [SerializeField] private int selected;

    [MenuItem("Tools/MyDefense/Auth/Development Login")]
    public static void Open() => GetWindow<DevelopmentLoginWindow>("Development Login");
    private void OnEnable()
    {
        password = "";
        EditorApplication.playModeStateChanged += PlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload += ClearPassword;
    }
    private void OnDisable()
    {
        ClearPassword();
        EditorApplication.playModeStateChanged -= PlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= ClearPassword;
    }
    private void ClearPassword() { password = ""; }
    private void PlayModeChanged(PlayModeStateChange state) { ClearPassword(); feedback = null; Repaint(); }
    private void OnInspectorUpdate() { Repaint(); }
    private void OnGUI()
    {
        EditorGUILayout.LabelField("로컬 개발 계정 로그인", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("1. Play 전에 계정을 선택하고 사용 버튼을 누르세요.\n2. SampleScene을 Play한 뒤 이 창에서 비밀번호를 입력하세요.\n게스트 저장 파일은 그대로 유지됩니다. 비밀번호는 저장하지 않습니다.", MessageType.Info);
        string endpoint = NetworkManager.Instance != null ? NetworkManager.Instance.BaseUrl : RuntimeEnvironmentConfig.ApiBaseUrl;
        EditorGUILayout.LabelField("API", endpoint);
        EditorGUILayout.LabelField("선택된 모드", DevelopmentLoginPolicy.IsRequested ? DevelopmentLoginPolicy.SelectedAccount : "기존 게스트 / 운영 로그인");
        bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
        using (new EditorGUI.DisabledScope(playing))
        {
            selected = EditorGUILayout.Popup("개발 계정", selected, Accounts);
            bool allowed = DevelopmentLoginPolicy.IsAllowed(endpoint, Accounts[selected]);
            if (!allowed) EditorGUILayout.HelpBox("MYDEFENSE_ENV=local 또는 dev를 명시하고 loopback API를 사용해야 합니다. 운영·원격 서버에서는 선택할 수 없습니다.", MessageType.Warning);
            using (new EditorGUI.DisabledScope(!allowed))
            {
                if (GUILayout.Button("선택한 개발 계정 사용"))
                {
                    SessionState.SetString(DevelopmentLoginPolicy.SelectionKey, Accounts[selected]);
                    ClearPassword(); feedback = "SampleScene을 Play한 뒤 비밀번호를 입력하세요.";
                }
            }
            if (GUILayout.Button("기존 게스트 / 운영 로그인으로 돌아가기"))
            {
                SessionState.EraseString(DevelopmentLoginPolicy.SelectionKey);
                ClearPassword(); feedback = "다음 Play에서 기존 게스트 저장소를 사용합니다.";
            }
        }
        if (EditorApplication.isPlaying && DevelopmentLoginPolicy.IsRequested)
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<AuthStartupController>();
            var session = AuthSession.Instance;
            if (session != null && session.IsAuthenticated && controller == null)
                EditorGUILayout.HelpBox(session.User.username + " 로그인 완료. 계정 변경은 Play 종료 후 가능합니다.", MessageType.Info);
            else
            {
                password = EditorGUILayout.PasswordField("비밀번호", password ?? "");
                bool ready = controller != null && controller.CanSubmitDevelopmentLogin;
                using (new EditorGUI.DisabledScope(!ready || (string.IsNullOrEmpty(password) && (session == null || !session.IsAuthenticated))))
                {
                    if (GUILayout.Button("로그인 / 로비 다시 시도"))
                    {
                        string submitted = password; ClearPassword();
                        feedback = controller.SubmitDevelopmentLogin(submitted) ? "요청 중…" : "로그인 화면 준비를 기다려 주세요.";
                        submitted = null; GUI.FocusControl(null);
                    }
                }
                if (!string.IsNullOrEmpty(session?.LastError)) EditorGUILayout.HelpBox(session.LastError, MessageType.Warning);
            }
        }
        if (!string.IsNullOrEmpty(feedback)) EditorGUILayout.HelpBox(feedback, MessageType.Info);
    }
}
