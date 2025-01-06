using UnityEngine;
using UnityEditor;
using Thirdweb.Unity;
using System.Reflection;

namespace Thirdweb.Editor
{
    [CustomEditor(typeof(ThirdwebManager))]
    public class ThirdwebManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty clientIdProp;
        private SerializedProperty bundleIdProp;
        private SerializedProperty initializeOnAwakeProp;
        private SerializedProperty showDebugLogsProp;
        private SerializedProperty optOutUsageAnalyticsProp;
        private SerializedProperty supportedChainsProp;
        private SerializedProperty includedWalletIdsProp;
        private SerializedProperty redirectPageHtmlOverrideProp;
        private SerializedProperty rpcOverridesProp;

        private int selectedTab = 0;
        private readonly string[] tabTitles = { "Client", "Preferences", "Misc", "Debug" };

        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;

        private Texture2D bannerImage;

        private void OnEnable()
        {
            clientIdProp = FindProperty("ClientId");
            bundleIdProp = FindProperty("BundleId");
            initializeOnAwakeProp = FindProperty("InitializeOnAwake");
            showDebugLogsProp = FindProperty("ShowDebugLogs");
            optOutUsageAnalyticsProp = FindProperty("OptOutUsageAnalytics");
            supportedChainsProp = FindProperty("SupportedChains");
            includedWalletIdsProp = FindProperty("IncludedWalletIds");
            redirectPageHtmlOverrideProp = FindProperty("RedirectPageHtmlOverride");
            rpcOverridesProp = FindProperty("RpcOverrides");

            bannerImage = Resources.Load<Texture2D>("EditorBanner");
        }

        private void InitializeStyles()
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 10, 10)
            };
        }

        private SerializedProperty FindProperty(string propertyName)
        {
            var targetType = target.GetType();
            var property = targetType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (property == null)
                return null;

            var backingFieldName = $"<{propertyName}>k__BackingField";
            return serializedObject.FindProperty(backingFieldName);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (headerStyle == null || buttonStyle == null)
            {
                InitializeStyles();
            }

            // Draw Banner and Title
            DrawBannerAndTitle();

            // Draw Tab Bar
            DrawTabs();

            // Draw Selected Tab Content
            GUILayout.Space(10);
            DrawSelectedTabContent();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBannerAndTitle()
        {
            GUILayout.BeginVertical();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            if (bannerImage != null)
            {
                GUILayout.Label(bannerImage, GUILayout.Width(64), GUILayout.Height(64));
            }

            GUILayout.Space(10);

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.Label("Thirdweb Configuration", EditorStyles.boldLabel);
            GUILayout.Label("Configure your settings and preferences.\nYou can access ThirdwebManager.Instance from anywhere.", EditorStyles.miniLabel);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.EndVertical();
        }

        private void DrawTabs()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabTitles, GUILayout.Height(25));
        }

        private void DrawSelectedTabContent()
        {
            switch (selectedTab)
            {
                case 0:
                    DrawClientTab();
                    break;
                case 1:
                    DrawPreferencesTab();
                    break;
                case 2:
                    DrawMiscTab();
                    break;
                case 3:
                    DrawDebugTab();
                    break;
                default:
                    GUILayout.Label("Unknown Tab", EditorStyles.boldLabel);
                    break;
            }
        }

        private void DrawClientTab()
        {
            EditorGUILayout.HelpBox("Configure your client settings here.", MessageType.Info);
            DrawProperty(clientIdProp, "Client ID", "Your Thirdweb Client ID. You can find this in the Thirdweb Dashboard by creating a project.");
            DrawProperty(bundleIdProp, "Bundle ID", "You may allowlist a bundle ID in the Thirdweb Dashboard to allow native apps to access our services, eg com.mycompany.myapp");
            DrawButton(
                "Create API Key",
                () =>
                {
                    Application.OpenURL("https://thirdweb.com/create-api-key");
                }
            );
        }

        private void DrawPreferencesTab()
        {
            EditorGUILayout.HelpBox("Set your preferences and initialization options here.", MessageType.Info);
            DrawProperty(initializeOnAwakeProp, "Initialize On Awake", "If enabled, Thirdweb will initialize on Awake. If disabled, you must call ThirdwebManager.Instance.Initialize() manually.");
            DrawProperty(showDebugLogsProp, "Show Debug Logs", "If enabled, Thirdweb will log debug messages to the console.");
            DrawProperty(optOutUsageAnalyticsProp, "Opt-Out of Usage Analytics", "If enabled, you may see inconsistent stats in your Thirdweb Dashboard.");
        }

        private void DrawMiscTab()
        {
            EditorGUILayout.HelpBox("Configure other settings here.", MessageType.Info);

            // RPC Overrides
            DrawProperty(rpcOverridesProp, "RPC Overrides", "We recommend using our default RPC, but if you encounter issues you may override them by chain id here.");

            GUILayout.Space(10);

            // Desktop OAuth Settings
            EditorGUILayout.LabelField(
                new GUIContent("OAuth Redirect Page HTML Override", "The HTML displayed after Social Login on Desktop, you may override it to match your branding."),
                EditorStyles.boldLabel
            );
            redirectPageHtmlOverrideProp.stringValue = EditorGUILayout.TextArea(redirectPageHtmlOverrideProp.stringValue, GUILayout.MinHeight(75));

            GUILayout.Space(10);

            // Wallet Connect Settings
            DrawProperty(
                supportedChainsProp,
                "WalletConnect Supported Chains",
                "You technically connect to multiple chains at once with WC, so try to include all chain ids your app wants to support."
            );

            DrawProperty(
                includedWalletIdsProp,
                "WalletConnect Included Wallet IDs",
                "If displaying less than 3 wallets look for ConnectView script in your hierarchy and edit WC Modal wallet count and other settings there."
            );
        }

        private void DrawDebugTab()
        {
            EditorGUILayout.HelpBox("Debug your settings here.", MessageType.Info);

            DrawButton(
                "Log Active Wallet Info",
                () =>
                {
                    if (Application.isPlaying)
                    {
                        var wallet = ((ThirdwebManager)target).GetActiveWallet();
                        if (wallet != null)
                        {
                            Debug.Log($"Active Wallet ({wallet.GetType().Name}) Address: {wallet.GetAddress().Result}");
                        }
                        else
                        {
                            Debug.LogWarning("No active wallet found.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Debugging can only be done in Play Mode.");
                    }
                }
            );

            DrawButton(
                "Open Documentation",
                () =>
                {
                    Application.OpenURL("http://portal.thirdweb.com/unity/v5");
                }
            );
        }

        private void DrawProperty(SerializedProperty property, string label, string tooltip = null)
        {
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
            }
            else
            {
                EditorGUILayout.HelpBox($"Property '{label}' not found.", MessageType.Error);
            }
        }

        private void DrawButton(string label, System.Action action)
        {
            GUILayout.FlexibleSpace();
            // center label
            if (GUILayout.Button(label, buttonStyle, GUILayout.Height(35), GUILayout.ExpandWidth(true)))
            {
                action.Invoke();
            }
            GUILayout.FlexibleSpace();
        }
    }
}
