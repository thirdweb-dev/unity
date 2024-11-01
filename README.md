![Thirdweb Unity SDK](https://github.com/thirdweb-dev/unity-sdk/assets/43042585/0eb16b66-317b-462b-9eb1-9425c0929c96)

[<img alt="Unity Documentation" src="https://img.shields.io/badge/Unity-Documentation-blue?logo=unity&style=for-the-badge" height="30">](https://portal.thirdweb.com/unity/v5)
[<img alt=".NET Documentation" src="https://img.shields.io/badge/.NET-Documentation-purple?logo=dotnet&style=for-the-badge" height="30">](https://portal.thirdweb.com/dotnet)

# Technical Demo

Experience our multichain game demo leveraging In-App Wallets and Account Abstraction built in three weeks - [Web3 Warriors](https://web3warriors.thirdweb.com/).

![image](https://github.com/thirdweb-dev/unity-sdk/assets/43042585/171198b2-83e7-4c8a-951b-79126dd47abb)

# Supported Platforms & Wallets

**Build games for WebGL, Desktop, and Mobile using 1000+ supported chains, with various login options!**

| Wallet Provider                           | WebGL | Desktop | Mobile |
| ----------------------------------------- | :---: | :-----: | :----: |
| **In-App Wallet** (Email, Phone, Socials, Custom) |  ✔️   |   ✔️    |   ✔️   |
| **Ecosystem Wallet** (IAW w/ partner permissions) |  ✔️   |   ✔️    |   ✔️   |
| **Private Key Wallet** (Guest Mode)       |  ✔️   |   ✔️    |   ✔️   |
| **Wallet Connect Wallet** (400+ Wallets)  |  ✔️   |   ✔️    |   ✔️   |
| **MetaMask Wallet** (Browser Extension)   |  ✔️   |    —    |   —    |
| **Smart Wallet** (Account Abstraction)    |  ✔️   |   ✔️    |   ✔️   |

<sub>✔️ Supported</sub> &nbsp; <sub>❌ Not Supported</sub> &nbsp; <sub>— Not Applicable</sub>

# Getting Started

1. **Download:** Head over to the [releases](https://github.com/thirdweb-dev/unity-sdk/releases) page and download the latest `.unitypackage` file.
2. **Explore:** Try out `Scene_Playground` to explore functionality and get onboarded.
3. **Learn:** Explore the [Unity v5 SDK Docs](https://portal.thirdweb.com/unity/v5) and the [.NET SDK Docs](https://portal.thirdweb.com/dotnet) to find a full API reference.

**Notes:**

- Tested on Unity 2021.3+, 2022.3+, Unity 6 Preview. We recommend using 2022 LTS.
- Newtonsoft and EDM4U are included utilities; deselect if already installed to avoid conflicts.
- If using .NET Framework and encountering `HttpUtility` errors, create `csc.rsp` with `-r:System.Web.dll` under `Assets`.
- Use version control and test removing duplicate DLLs if conflicts arise.

# Build Instructions

## General

- **Build Settings:** Use `Smaller (faster) Builds` / `Shorter Build Time`.
- **Player Settings:** Use IL2CPP over Mono when available.
- **Stripping Level:** Set `Managed Stripping Level` to `Minimal` (`Player Settings` > `Other Settings` > `Optimization`). (Generally not a hard requirement unless using WalletConnect as a wallet provider option.)
- **Strip Engine Code:** Make sure this is turned off.

## WebGL

- **WebGL Template:** None enforced, feel free to customize!
- **Compression Format:** Set to `Disabled` (`Player Settings` > `Publishing Settings`) for final builds.
- **Testing WebGL Social Login Locally:** Host the build or run it locally with `Cross-Origin-Opener-Policy` set to `same-origin-allow-popups`.

Example setup for testing In-App or Ecosystem Wallet Social Login locally (no longer required with Unity 6 Web, can use Build & Run):

```javascript
// YourWebGLOutputFolder/server.js
const express = require("express");
const app = express();
const port = 8000;

app.use((req, res, next) => {
  res.header("Cross-Origin-Opener-Policy", "same-origin-allow-popups");
  next();
});

app.use(express.static("."));
app.listen(port, () =>
  console.log(`Server running on http://localhost:${port}`)
);

// run it with `node server.js`
```

No action needed for hosted builds.

## Mobile

- **EDM4U:** Comes with the package, resolves dependencies at runtime. Use `Force Resolve` from `Assets` > `External Dependency Manager` > `Android Resolver`.
- **Redirect Schemes:** Set custom schemes matching your bundle ID in `Plugins/AndroidManifest.xml` or equivalent to ensure OAuth redirects.

# Migration from v4

See https://portal.thirdweb.com/unity/v5/migration-guide

# Need Help?

For any questions or support, visit our [Support Portal](https://thirdweb.com/support).

Thank you for trying out the Thirdweb Unity SDK!
