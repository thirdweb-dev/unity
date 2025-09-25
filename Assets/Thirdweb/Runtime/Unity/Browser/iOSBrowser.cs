#if (UNITY_IOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AOT;

namespace Thirdweb.Unity
{
    public class ASWebAuthenticationSession : IDisposable
    {
        private static readonly Dictionary<IntPtr, ASWebAuthenticationSessionCompletionHandler> CompletionCallbacks = new();

        private IntPtr _sessionPtr;

        public bool prefersEphemeralWebBrowserSession
        {
            get => Thirdweb_ASWebAuthenticationSession_GetPrefersEphemeralWebBrowserSession(_sessionPtr) == 1;
            set => Thirdweb_ASWebAuthenticationSession_SetPrefersEphemeralWebBrowserSession(_sessionPtr, value ? 1 : 0);
        }

        public ASWebAuthenticationSession(string url, string callbackUrlScheme, ASWebAuthenticationSessionCompletionHandler completionHandler)
        {
            _sessionPtr = Thirdweb_ASWebAuthenticationSession_InitWithURL(url, callbackUrlScheme, OnAuthenticationSessionCompleted);

            if (_sessionPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to initialize ASWebAuthenticationSession.");
            }

            CompletionCallbacks[_sessionPtr] = completionHandler;
        }

        public bool Start()
        {
            return Thirdweb_ASWebAuthenticationSession_Start(_sessionPtr) == 1;
        }

        public void Cancel()
        {
            Thirdweb_ASWebAuthenticationSession_Cancel(_sessionPtr);
        }

        public void Dispose()
        {
            if (_sessionPtr != IntPtr.Zero)
            {
                CompletionCallbacks.Remove(_sessionPtr);
            }
            _sessionPtr = IntPtr.Zero;
        }

#if UNITY_STANDALONE_OSX
        private const string DllName = "MacBrowser";
#else
        private const string DllName = "__Internal";
#endif

        [DllImport(DllName)]
        private static extern IntPtr Thirdweb_ASWebAuthenticationSession_InitWithURL(string url, string callbackUrlScheme, AuthenticationSessionCompletedCallback completionHandler);

        [DllImport(DllName)]
        private static extern int Thirdweb_ASWebAuthenticationSession_Start(IntPtr session);

        [DllImport(DllName)]
        private static extern void Thirdweb_ASWebAuthenticationSession_Cancel(IntPtr session);

        [DllImport(DllName)]
        private static extern int Thirdweb_ASWebAuthenticationSession_GetPrefersEphemeralWebBrowserSession(IntPtr session);

        [DllImport(DllName)]
        private static extern void Thirdweb_ASWebAuthenticationSession_SetPrefersEphemeralWebBrowserSession(IntPtr session, int enable);

        public delegate void ASWebAuthenticationSessionCompletionHandler(string callbackUrl, ASWebAuthenticationSessionError error);

        private delegate void AuthenticationSessionCompletedCallback(IntPtr session, string callbackUrl, int errorCode, string errorMessage);

        [MonoPInvokeCallback(typeof(AuthenticationSessionCompletedCallback))]
        private static void OnAuthenticationSessionCompleted(IntPtr session, string callbackUrl, int errorCode, string errorMessage)
        {
            if (CompletionCallbacks.TryGetValue(session, out var callback))
            {
                callback?.Invoke(callbackUrl, new ASWebAuthenticationSessionError((ASWebAuthenticationSessionErrorCode)errorCode, errorMessage));
            }
        }
    }

#if UNITY_IOS
    public class IOSBrowser : IThirdwebBrowser
    {
        private TaskCompletionSource<BrowserResult> _taskCompletionSource;

        public bool prefersEphemeralWebBrowserSession { get; set; } = false;

        public async Task<BrowserResult> Login(ThirdwebClient client, string loginUrl, string redirectUrl, Action<string> browserOpenAction, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(loginUrl))
                throw new ArgumentNullException(nameof(loginUrl));

            if (string.IsNullOrEmpty(redirectUrl))
                throw new ArgumentNullException(nameof(redirectUrl));

            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();

            redirectUrl = redirectUrl.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0];

            using var authenticationSession = new ASWebAuthenticationSession(loginUrl, redirectUrl, AuthenticationSessionCompletionHandler);
            authenticationSession.prefersEphemeralWebBrowserSession = prefersEphemeralWebBrowserSession;

            cancellationToken.Register(() =>
            {
                _taskCompletionSource?.TrySetCanceled();
            });

            try
            {
                if (!authenticationSession.Start())
                {
                    _taskCompletionSource.SetResult(new BrowserResult(BrowserStatus.UnknownError, "Browser could not be started."));
                }

                return await _taskCompletionSource.Task;
            }
            catch (TaskCanceledException)
            {
                authenticationSession?.Cancel();
                throw;
            }
        }

        private void AuthenticationSessionCompletionHandler(string callbackUrl, ASWebAuthenticationSessionError error)
        {
            if (error.code == ASWebAuthenticationSessionErrorCode.None)
            {
                _taskCompletionSource.SetResult(new BrowserResult(BrowserStatus.Success, callbackUrl));
            }
            else if (error.code == ASWebAuthenticationSessionErrorCode.CanceledLogin)
            {
                _taskCompletionSource.SetResult(new BrowserResult(BrowserStatus.UserCanceled, callbackUrl, error.message));
            }
            else
            {
                _taskCompletionSource.SetResult(new BrowserResult(BrowserStatus.UnknownError, callbackUrl, error.message));
            }
        }
    }
#endif

#if UNITY_STANDALONE_OSX
    public class MacBrowser : IThirdwebBrowser
    {
        private TaskCompletionSource<BrowserResult> _taskCompletionSource;

        public bool prefersEphemeralWebBrowserSession { get; set; } = false;

        public async Task<BrowserResult> Login(ThirdwebClient client, string loginUrl, string redirectUrl, Action<string> browserOpenAction, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(loginUrl))
                throw new ArgumentNullException(nameof(loginUrl));

            if (string.IsNullOrEmpty(redirectUrl))
                throw new ArgumentNullException(nameof(redirectUrl));

            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();

            redirectUrl = redirectUrl.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0];

            using var authenticationSession = new ASWebAuthenticationSession(loginUrl, redirectUrl, AuthenticationSessionCompletionHandler);
            authenticationSession.prefersEphemeralWebBrowserSession = prefersEphemeralWebBrowserSession;

            cancellationToken.Register(() =>
            {
                _taskCompletionSource?.TrySetCanceled();
            });

            try
            {
                if (!authenticationSession.Start())
                {
                    _taskCompletionSource.SetResult(new BrowserResult(BrowserStatus.UnknownError, "Browser could not be started."));
                }

                return await _taskCompletionSource.Task;
            }
            catch (TaskCanceledException)
            {
                authenticationSession?.Cancel();
                throw;
            }
        }

        private void AuthenticationSessionCompletionHandler(string callbackUrl, ASWebAuthenticationSessionError error)
        {
            if (error.code == ASWebAuthenticationSessionErrorCode.None)
            {
                _taskCompletionSource.SetResult(new BrowserResult(BrowserStatus.Success, callbackUrl));
            }
            else if (error.code == ASWebAuthenticationSessionErrorCode.CanceledLogin)
            {
                _taskCompletionSource.SetResult(new BrowserResult(BrowserStatus.UserCanceled, callbackUrl, error.message));
            }
            else
            {
                _taskCompletionSource.SetResult(new BrowserResult(BrowserStatus.UnknownError, callbackUrl, error.message));
            }
        }
    }
#endif

    public class ASWebAuthenticationSessionError
    {
        public ASWebAuthenticationSessionErrorCode code { get; }
        public string message { get; }

        public ASWebAuthenticationSessionError(ASWebAuthenticationSessionErrorCode code, string message)
        {
            this.code = code;
            this.message = message;
        }
    }

    public enum ASWebAuthenticationSessionErrorCode
    {
        None = 0,
        CanceledLogin = 1,
        PresentationContextNotProvided = 2,
        PresentationContextInvalid = 3
    }
}

#endif
