using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Thirdweb.Unity.Helpers
{
    public class UnityThirdwebHttpClient : IThirdwebHttpClient
    {
        public Dictionary<string, string> Headers { get; private set; }

        private bool _disposed;

        public UnityThirdwebHttpClient()
        {
            Headers = new Dictionary<string, string>();
        }

        public void SetHeaders(Dictionary<string, string> headers)
        {
            Headers = new Dictionary<string, string>(headers);
        }

        public void ClearHeaders()
        {
            Headers.Clear();
        }

        public void AddHeader(string key, string value)
        {
            Headers.Add(key, value);
        }

        public void RemoveHeader(string key)
        {
            _ = Headers.Remove(key);
        }

        private void AddHeaders(UnityWebRequest request)
        {
            foreach (var header in Headers)
            {
                if (header.Value != null)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }
        }

        public async Task<ThirdwebHttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync(() => UnityWebRequest.Get(requestUri), cancellationToken).ConfigureAwait(false);
        }

        public async Task<ThirdwebHttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            var contentBytes = await content.ReadAsByteArrayAsync().ConfigureAwait(false);

            return await SendRequestAsync(
                    () =>
                    {
                        var webRequest = new UnityWebRequest(requestUri, UnityWebRequest.kHttpVerbPOST)
                        {
                            uploadHandler = new UploadHandlerRaw(contentBytes) { contentType = content.Headers.ContentType.ToString() },
                            downloadHandler = new DownloadHandlerBuffer(),
                        };
                        return webRequest;
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        public Task<ThirdwebHttpResponseMessage> PutAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ThirdwebHttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private Task<ThirdwebHttpResponseMessage> SendRequestAsync(Func<UnityWebRequest> createRequest, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<ThirdwebHttpResponseMessage>();

            _ = ThirdwebMainThreadExecutor.Instance.RunOnMainThread(async () =>
            {
                using var webRequest = createRequest();
                AddHeaders(webRequest);

                var operation = webRequest.SendWebRequest();

                while (!operation.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        webRequest.Abort();
                        tcs.SetCanceled();
                        return;
                    }
                    await Task.Yield();
                }

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.DataProcessingError)
                {
                    tcs.SetException(new Exception(webRequest.error));
                }
                else
                {
                    tcs.SetResult(
                        new ThirdwebHttpResponseMessage(
                            statusCode: webRequest.responseCode,
                            content: new ThirdwebHttpContent(webRequest.downloadHandler.data),
                            isSuccessStatusCode: webRequest.responseCode >= 200 && webRequest.responseCode < 300
                        )
                    );
                }
            });

            return tcs.Task;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // No need to dispose UnityWebRequest
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
