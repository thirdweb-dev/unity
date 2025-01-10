using System.Numerics;
using System.Threading.Tasks;
using UnityEngine;
using ZXing;
using ZXing.QrCode;

namespace Thirdweb.Unity
{
    public static class ThirdwebUnityExtensions
    {
        public static async Task<Sprite> GetNFTSprite(this NFT nft, ThirdwebClient client)
        {
            var bytes = await nft.GetNFTImageBytes(client);
            Texture2D texture = new(2, 2);

            bool isLoaded = texture.LoadImage(bytes);
            if (!isLoaded)
            {
                Debug.LogError("Failed to load image from bytes.");
                return null;
            }

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new UnityEngine.Vector2(0.5f, 0.5f));
        }

        public static async Task<SmartWallet> UpgradeToSmartWallet(this IThirdwebWallet personalWallet, BigInteger chainId, SmartWalletOptions smartWalletOptions)
        {
            return await ThirdwebManagerBase.Instance.UpgradeToSmartWallet(personalWallet, chainId, smartWalletOptions);
        }

        public static Texture2D ToQRTexture(this string textForEncoding, Color? fgColor = null, Color? bgColor = null, int width = 512, int height = 512)
        {
            fgColor ??= Color.black;
            bgColor ??= Color.white;

            var qrCodeEncodingOptions = new QrCodeEncodingOptions
            {
                Height = height,
                Width = width,
                Margin = 4,
                QrVersion = 11
            };

            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = qrCodeEncodingOptions,
                Renderer = new Color32Renderer { Foreground = fgColor.Value, Background = bgColor.Value }
            };

            var pixels = writer.Write(textForEncoding);

            var texture = new Texture2D(width, height);
            texture.SetPixels32(pixels);
            texture.filterMode = FilterMode.Point;
            texture.Apply();
            texture.Compress(true);

            return texture;
        }
    }
}
