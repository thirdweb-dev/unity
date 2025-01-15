![Thirdweb Unity SDK](https://github.com/thirdweb-dev/unity-sdk/assets/43042585/0eb16b66-317b-462b-9eb1-9425c0929c96)

<p align="center">
  <strong>All-In-One Cross-Platform Blockchain Unity SDK for Browser, Standalone, Mobile and Server Targets.</strong>
</p>

<p align="center">
  <a href="https://portal.thirdweb.com/unity/v5">
    <img alt="Unity Documentation" src="https://img.shields.io/badge/Unity-Documentation-blue?logo=unity&style=for-the-badge" height="30">
  </a>
  <a href="https://portal.thirdweb.com/dotnet">
    <img alt=".NET Documentation" src="https://img.shields.io/badge/.NET-Documentation-purple?logo=dotnet&style=for-the-badge" height="30">
  </a>
</p>

# Technical Demo

Experience our multichain game demo leveraging In-App Wallets and Account Abstraction built in three weeks - [Web3 Warriors](https://web3warriors.thirdweb.com/).

![image](https://github.com/thirdweb-dev/unity-sdk/assets/43042585/171198b2-83e7-4c8a-951b-79126dd47abb)

# Features

This SDK provides a Unity-first integration of all [thirdweb](https://thirdweb.com) functionality, including but not limited to:

- Support for all target platforms, Unity 2022 & Unity 6.
- First party support for [In-App Wallets](https://portal.thirdweb.com/connect/wallet/overview) (Guest, Email, Phone, Socials, Custom Auth+).
- First party support for [Account Abstraction](https://portal.thirdweb.com/connect/account-abstraction/overview) (Both EIP-4337 & zkSync Native AA).
- Instant connection to any chain with RPC Edge integration.
- Integrated IPFS upload/download.
- Easy to extend or wrap.
- High level contract extensions for interacting with common standards and thirdweb contract standards.
- Automatic ABI resolution.
- Build on top of thirdweb's [.NET SDK](https://portal.thirdweb.com/dotnet) - unity package updates are typically updates to a single DLL/a file or two.
- Get started in 5 minutes with a simple [ThirdwebManager](https://portal.thirdweb.com/unity/v5/thirdwebmanager) prefab.

# Supported Platforms & Wallets

**Build games for Web, Standalone, and Mobile using 2000+ supported chains, with various login options!**

| Wallet Provider                                              | Web | Standalone | Mobile |
| ------------------------------------------------------------ | :---: | :-----: | :----: |
| **In-App Wallet** (Guest, Email, Phone, Socials, Custom)     |  ✔️   |   ✔️    |   ✔️   |
| **Ecosystem Wallet** (IAW w/ partner permissions)            |  ✔️   |   ✔️    |   ✔️   |
| **Private Key Wallet** (Ephemereal, good for testing)        |  ✔️   |   ✔️    |   ✔️   |
| **Wallet Connect Wallet** (400+ Wallets)                     |  ✔️   |   ✔️    |   ✔️   |
| **MetaMask Wallet** (Browser Extension)                      |  ✔️   |    —    |   —    |
| **Smart Wallet** (Account Abstraction: 4337 & ZkSync Native) |  ✔️   |   ✔️    |   ✔️   |

<sub>✔️ Supported</sub> &nbsp; <sub>❌ Not Supported</sub> &nbsp; <sub>— Not Applicable</sub>

# Getting Started

1. **Download:** Head over to the [releases](https://github.com/thirdweb-dev/unity-sdk/releases) page and download the latest `.unitypackage` file.
2. **Explore:** Try out `Scene_Playground` to explore functionality and get onboarded.
3. **Learn:** Explore the [Unity v5 SDK Docs](https://portal.thirdweb.com/unity/v5) and the [.NET SDK Docs](https://portal.thirdweb.com/dotnet) to find a full API reference.

## Miscellaneous

- Recommended Unity Editor Version: 2022.3+ (LTS)
- Newtonsoft.Json and EDM4U are included utilities; deselect when importing if already installed to avoid conflicts.
- If using .NET Framework and encountering `HttpUtility` errors, create `csc.rsp` with `-r:System.Web.dll` under `Assets`.
- Use version control and test removing duplicate DLLs if conflicts arise. Our SDK generally works with most versions of the few dependencies we do include.
- To use your own WalletConnect Project ID, edit `Assets/Thirdweb/Plugins/WalletConnectUnity/Resources/WalletConnectProjectConfig.asset`.

## Additional Resources

- [Documentation](https://portal.thirdweb.com/unity/v5)
- [Templates](https://thirdweb.com/templates)
- [Website](https://thirdweb.com)

## Support

For help or feedback, please [visit our support site](https://thirdweb.com/support)

## Security

If you believe you have found a security vulnerability in any of our packages, we kindly ask you not to open a public issue; and to disclose this to us by emailing `security@thirdweb.com`.
