using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using WalletConnectSharp.Common.Logging;
using WalletConnectSharp.Common.Model.Errors;
using WalletConnectSharp.Sign;
using WalletConnectSharp.Sign.Interfaces;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectSharp.Sign.Models.Engine.Events;
using WalletConnectSharp.Sign.Models.Engine.Methods;
using WalletConnectSharp.Storage;
using WalletConnectSharp.Storage.Interfaces;

namespace WalletConnectUnity.Core
{
    public class WalletConnect : IWalletConnect
    {
        private static readonly Lazy<IWalletConnect> LazyInstance = new(() => new WalletConnect());
        public static IWalletConnect Instance { get; } = LazyInstance.Value;

        public static SynchronizationContext UnitySyncContext { get; private set; }

        public ISignClient SignClient { get; private set; }

        public Linker Linker { get; private set; }

        public SessionStruct ActiveSession
        {
            get => SignClient.AddressProvider.DefaultSession;
        }

        public bool IsInitialized { get; private set; }

        public bool IsConnected
        {
            get => !string.IsNullOrWhiteSpace(ActiveSession.Topic);
        }

        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);

        [Obsolete("Use SessionConnected or SessionUpdated instead")]
        public event EventHandler<SessionStruct> ActiveSessionChanged;

        public event EventHandler<SessionStruct> SessionConnected;
        public event EventHandler<SessionStruct> SessionUpdated;
        public event EventHandler SessionDisconnected;

        public event EventHandler<string> ActiveChainIdChanged;

        protected bool disposed;

        public async Task<IWalletConnect> InitializeAsync()
        {
            try
            {
                await _initializationSemaphore.WaitAsync();

                if (IsInitialized)
                {
                    Debug.LogError("[WalletConnectUnity] Already initialized");
                    return this;
                }

                var currentSyncContext = SynchronizationContext.Current;
                bool isSkipSyncCtxCheck = AppContext.TryGetSwitch("IsSkipWalletConnectSyncCtxCheck", out var isEnabled) && isEnabled;
                if (!isSkipSyncCtxCheck && currentSyncContext.GetType().FullName != "UnityEngine.UnitySynchronizationContext")
                    throw new Exception(
                        $"[WalletConnectUnity] SynchronizationContext is not of type UnityEngine.UnitySynchronizationContext. Current type is <i>{currentSyncContext.GetType().FullName}</i>. Make sure to initialize WalletConnect from the main thread."
                    );
                UnitySyncContext = currentSyncContext;

                var projectConfig = ProjectConfiguration.Load();

                Assert.IsNotNull(projectConfig, $"Project configuration not found. Expected to find it at <i>{ProjectConfiguration.ConfigPath}</i>");
                Assert.IsFalse(string.IsNullOrWhiteSpace(projectConfig.Id), $"Project ID is not set in the project configuration asset ( <i>{ProjectConfiguration.ConfigPath}</i> ).");
                Assert.IsFalse(
                    projectConfig.Metadata == null || string.IsNullOrWhiteSpace(projectConfig.Metadata.Name),
                    $"Project name is not set in the project configuration asset ( <i>{ProjectConfiguration.ConfigPath}</i> )."
                );

                if (projectConfig.LoggingEnabled)
                    WCLogger.Logger = new Logger();

                var storage = await BuildStorage();

                SignClient = await WalletConnectSignClient.Init(
                    new SignClientOptions
                    {
                        Metadata = projectConfig.Metadata,
                        Name = projectConfig.Metadata.Name,
                        ProjectId = projectConfig.Id,
                        Storage = storage,
                        RelayUrl = projectConfig.RelayUrl,
                        RelayUrlBuilder = new UnityRelayUrlBuilder(),
                        ConnectionBuilder = new NativeWebSocketConnectionBuilder(),
                        ConnectionTimeout = TimeSpan.FromSeconds(180)
                    }
                );

                SignClient.SessionConnected += OnSessionConnected;
                SignClient.SessionUpdateRequest += OnSessionUpdated;
                SignClient.SessionDeleted += OnSessionDeleted;

                SignClient.SubscribeToSessionEvent("chainChanged", OnChainChanged);

                Linker = new Linker(this);

                UnityEventsDispatcher.Instance.ApplicationQuit += ApplicationQuitHandler;

                IsInitialized = true;

                return this;
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        public async Task<bool> TryResumeSessionAsync()
        {
            await SignClient.AddressProvider.LoadDefaultsAsync();

            var sessionTopic = SignClient.AddressProvider.DefaultSession.Topic;

            if (string.IsNullOrWhiteSpace(sessionTopic))
                return false;

            try
            {
                await SignClient.Extend(sessionTopic);
            }
            catch (KeyNotFoundException)
            {
                SignClient.AddressProvider.DefaultSession = default;
                return false;
            }
            catch (WalletConnectException)
            {
                SignClient.AddressProvider.DefaultSession = default;
                return false;
            }

            return true;
        }

        public Task<ConnectedData> ConnectAsync(ConnectOptions options)
        {
            return SignClient.Connect(options);
        }

        public Task<TResponse> RequestAsync<TRequestData, TResponse>(TRequestData data, string chainId = null)
        {
            ThrowIfNoActiveSession();

            var activeSessionTopic = ActiveSession.Topic;

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            Linker.OpenSessionRequestDeepLinkAfterMessageFromSession(activeSessionTopic);
#endif

            return SignClient.Request<TRequestData, TResponse>(activeSessionTopic, data, chainId);
        }

        public Task DisconnectAsync()
        {
            ThrowIfNoActiveSession();
            return SignClient.Disconnect(ActiveSession.Topic, new SessionDelete());
        }

        private async void OnChainChanged(object sender, SessionEvent<JToken> sessionEvent)
        {
            if (sessionEvent.ChainId == "eip155:0")
                return;

            try
            {
                await Instance.SignClient.AddressProvider.SetDefaultChainIdAsync(sessionEvent.ChainId);
                ActiveChainIdChanged?.Invoke(this, sessionEvent.ChainId);
            }
            catch (ArgumentException e)
            {
                WCLogger.LogError(e);
            }
        }

        private void OnSessionConnected(object sender, SessionStruct session)
        {
            UnitySyncContext.Post(
                _ =>
                {
                    SessionConnected?.Invoke(this, session);
                    ActiveSessionChanged?.Invoke(this, session);
                },
                null
            );
        }

        private void OnSessionUpdated(object sender, SessionEvent sessionEvent)
        {
            var sessionStruct = SignClient.Session.Values.First(s => s.Topic == sessionEvent.Topic);
            UnitySyncContext.Post(
                _ =>
                {
                    SessionUpdated?.Invoke(this, sessionStruct);
                    ActiveSessionChanged?.Invoke(this, sessionStruct);
                },
                null
            );
        }

        private void OnSessionDeleted(object sender, SessionEvent _)
        {
            UnitySyncContext.Post(
                _ =>
                {
                    SessionDisconnected?.Invoke(this, EventArgs.Empty);
                    ActiveSessionChanged?.Invoke(this, default);
                },
                null
            );
        }

        private static async Task<IKeyValueStorage> BuildStorage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var currentSyncContext = SynchronizationContext.Current;
            if (currentSyncContext.GetType().FullName != "UnityEngine.UnitySynchronizationContext")
                throw new Exception(
                    $"[WalletConnectUnity] SynchronizationContext is not of type UnityEngine.UnitySynchronizationContext. Current type is <i>{currentSyncContext.GetType().FullName}</i>. When targeting WebGL, Make sure to initialize WalletConnect from the main thread.");

            var playerPrefsStorage = new PlayerPrefsStorage(currentSyncContext);
            await playerPrefsStorage.Init();

            return playerPrefsStorage;
#else

            var path = $"{Application.persistentDataPath}/WalletConnect/storage.json";
            WCLogger.Log($"[WalletConnectUnity] Using storage path <i>{path}</i>");

            var storage = new FileSystemStorage(path);

            try
            {
                await storage.Init();
            }
            catch (JsonException)
            {
                Debug.LogError($"[WalletConnectUnity] Failed to deserialize storage. Deleting it and creating a new one at <i>{path}</i>");
                await storage.Clear();
                await storage.Init();
            }

            return storage;
#endif
        }

        private void ThrowIfNoActiveSession()
        {
            if (!IsInitialized || string.IsNullOrWhiteSpace(ActiveSession.Topic))
                throw new Exception("No active session");
        }

        private void ApplicationQuitHandler()
        {
            if (IsInitialized)
                Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                SignClient?.Dispose();
                Linker?.Dispose();

                _initializationSemaphore?.Dispose();
            }

            disposed = true;
        }
    }
}
