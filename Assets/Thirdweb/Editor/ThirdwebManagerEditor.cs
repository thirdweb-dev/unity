using UnityEngine;
using UnityEditor;
using Thirdweb.Unity;
using System.Reflection;

namespace Thirdweb.Editor
{
    public abstract class ThirdwebManagerBaseEditor<T> : UnityEditor.Editor
        where T : MonoBehaviour
    {
        protected SerializedProperty initializeOnAwakeProp;
        protected SerializedProperty showDebugLogsProp;
        protected SerializedProperty autoConnectLastWalletProp;
        protected SerializedProperty supportedChainsProp;
        protected SerializedProperty includedWalletIdsProp;
        protected SerializedProperty redirectPageHtmlOverrideProp;
        protected SerializedProperty rpcOverridesProp;

        protected int selectedTab;
        protected GUIStyle buttonStyle;
        protected Texture2D bannerImage;

        protected virtual string[] TabTitles => new string[] { "Client/Server", "Preferences", "Misc", "Debug" };

        protected virtual void OnEnable()
        {
            initializeOnAwakeProp = FindProp("InitializeOnAwake");
            showDebugLogsProp = FindProp("ShowDebugLogs");
            autoConnectLastWalletProp = FindProp("AutoConnectLastWallet");
            supportedChainsProp = FindProp("SupportedChains");
            includedWalletIdsProp = FindProp("IncludedWalletIds");
            redirectPageHtmlOverrideProp = FindProp("RedirectPageHtmlOverride");
            rpcOverridesProp = FindProp("RpcOverrides");

            bannerImage = Resources.Load<Texture2D>("EditorBanner");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (buttonStyle == null)
            {
                InitializeStyles();
            }

            DrawBannerAndTitle();
            DrawTabs();
            GUILayout.Space(10);
            DrawSelectedTabContent();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawSelectedTabContent()
        {
            switch (selectedTab)
            {
                case 0:
                    DrawClientOrServerTab();
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

        protected abstract void DrawClientOrServerTab();

        protected virtual void DrawPreferencesTab()
        {
            EditorGUILayout.HelpBox("Set your preferences and initialization options here.", MessageType.Info);
            DrawProperty(initializeOnAwakeProp, "Initialize On Awake");
            DrawProperty(showDebugLogsProp, "Show Debug Logs");
            DrawProperty(autoConnectLastWalletProp, "Auto-Connect Last Wallet");
        }

        protected virtual void DrawMiscTab()
        {
            EditorGUILayout.HelpBox("Configure other settings here.", MessageType.Info);
            DrawProperty(rpcOverridesProp, "RPC Overrides");
            GUILayout.Space(10);
            EditorGUILayout.LabelField("OAuth Redirect Page HTML Override", EditorStyles.boldLabel);
            redirectPageHtmlOverrideProp.stringValue = EditorGUILayout.TextArea(redirectPageHtmlOverrideProp.stringValue, GUILayout.MinHeight(75));
            GUILayout.Space(10);
            DrawProperty(supportedChainsProp, "WalletConnect Supported Chains");
            DrawProperty(includedWalletIdsProp, "WalletConnect Included Wallet IDs");
        }

        protected virtual void DrawDebugTab()
        {
            EditorGUILayout.HelpBox("Debug your settings here.", MessageType.Info);
            DrawButton(
                "Log Active Wallet Info",
                () =>
                {
                    if (Application.isPlaying)
                    {
                        var mgr = target as T;
                        var method = mgr.GetType().GetMethod("GetActiveWallet", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null)
                        {
                            var wallet = method.Invoke(mgr, null) as IThirdwebWallet;
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
                            Debug.LogWarning("GetActiveWallet() not found.");
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

        protected void DrawBannerAndTitle()
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

        protected void DrawTabs()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, TabTitles, GUILayout.Height(25));
        }

        protected void InitializeStyles()
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 10, 10)
            };
        }

        protected void DrawProperty(SerializedProperty property, string label)
        {
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
            }
            else
            {
                EditorGUILayout.HelpBox($"Property '{label}' not found.", MessageType.Error);
            }
        }

        protected void DrawButton(string label, System.Action action)
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(label, buttonStyle, GUILayout.Height(35), GUILayout.ExpandWidth(true)))
            {
                action.Invoke();
            }
            GUILayout.FlexibleSpace();
        }

        protected SerializedProperty FindProp(string propName)
        {
            var targetType = target.GetType();
            var property = targetType.GetProperty(propName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (property == null)
                return null;
            var backingFieldName = $"<{propName}>k__BackingField";
            return serializedObject.FindProperty(backingFieldName);
        }
    }

    [CustomEditor(typeof(ThirdwebManager))]
    public class ThirdwebManagerEditor : ThirdwebManagerBaseEditor<ThirdwebManager>
    {
        SerializedProperty clientIdProp;
        SerializedProperty bundleIdProp;

        protected override void OnEnable()
        {
            base.OnEnable();
            clientIdProp = FindProp("ClientId");
            bundleIdProp = FindProp("BundleId");
        }

        protected override string[] TabTitles => new string[] { "Client", "Preferences", "Misc", "Debug" };

        protected override void DrawClientOrServerTab()
        {
            EditorGUILayout.HelpBox("Configure your client settings here.", MessageType.Info);
            DrawProperty(clientIdProp, "Client ID");
            DrawProperty(bundleIdProp, "Bundle ID");
            DrawButton(
                "Create API Key",
                () =>
                {
                    Application.OpenURL("https://thirdweb.com/create-api-key");
                }
            );
        }
    }

    [CustomEditor(typeof(ThirdwebManagerServer))]
    public class ThirdwebManagerServerEditor : ThirdwebManagerBaseEditor<ThirdwebManagerServer>
    {
        SerializedProperty secretKeyProp;

        protected override void OnEnable()
        {
            base.OnEnable();
            secretKeyProp = FindProp("SecretKey");
        }

        protected override string[] TabTitles => new string[] { "Client", "Preferences", "Misc", "Debug" };

        protected override void DrawClientOrServerTab()
        {
            EditorGUILayout.HelpBox("Configure your client settings here.", MessageType.Info);
            DrawProperty(secretKeyProp, "Secret Key");
            DrawButton(
                "Create API Key",
                () =>
                {
                    Application.OpenURL("https://thirdweb.com/create-api-key");
                }
            );
        }
    }
}
