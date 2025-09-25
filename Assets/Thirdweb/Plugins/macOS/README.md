# macOS OAuth Plugin

The native bridge in this folder (`libMacBrowser.dylib`) is a universal binary (arm64 + x86_64) built from `MacBrowser.mm`.

## Rebuild steps

Run these commands from the repository root on macOS:

```bash
cd Assets/Thirdweb/Plugins/macOS
clang++ -std=c++17 -ObjC++ -fmodules -Wall -Werror -arch arm64 -framework Cocoa -framework AuthenticationServices -dynamiclib MacBrowser.mm -o /tmp/libMacBrowser_arm64.dylib
clang++ -std=c++17 -ObjC++ -fmodules -Wall -Werror -arch x86_64 -framework Cocoa -framework AuthenticationServices -dynamiclib MacBrowser.mm -o /tmp/libMacBrowser_x86_64.dylib
lipo -create /tmp/libMacBrowser_arm64.dylib /tmp/libMacBrowser_x86_64.dylib -output libMacBrowser.dylib
rm /tmp/libMacBrowser_arm64.dylib /tmp/libMacBrowser_x86_64.dylib
```

After rebuilding, re-run your Unity macOS build to make sure the new binary is picked up.
