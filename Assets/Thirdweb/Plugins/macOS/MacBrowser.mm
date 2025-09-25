#import <Cocoa/Cocoa.h>
#import <AuthenticationServices/AuthenticationServices.h>

#include "../iOS/Common.h"

typedef void (*ASWebAuthenticationSessionCompletionCallback)(void* sessionPtr, const char* callbackUrl, int errorCode, const char* errorMessage);

@interface Thirdweb_ASWebAuthenticationSession : NSObject<ASWebAuthenticationPresentationContextProviding>

@property (readonly, nonatomic) ASWebAuthenticationSession* session;

@end

@implementation Thirdweb_ASWebAuthenticationSession

- (instancetype)initWithURL:(NSURL *)URL callbackURLScheme:(nullable NSString *)callbackURLScheme completionCallback:(ASWebAuthenticationSessionCompletionCallback)completionCallback
{
    self = [super init];
    if (self)
    {
        _session = [[ASWebAuthenticationSession alloc] initWithURL:URL
                                                callbackURLScheme:callbackURLScheme
                                                completionHandler:^(NSURL * _Nullable callbackURL, NSError * _Nullable error)
        {
            if (error != nil)
            {
                NSLog(@"[ASWebAuthenticationSession:CompletionHandler] %@", error.description);
            }

            completionCallback((__bridge void*)self, toString(callbackURL.absoluteString), (int)error.code, toString(error.localizedDescription));
        }];

        if (@available(macOS 10.15, *))
        {
            _session.presentationContextProvider = self;
        }
    }
    return self;
}

- (ASPresentationAnchor)presentationAnchorForWebAuthenticationSession:(ASWebAuthenticationSession *)session
{
    if (@available(macOS 10.15, *))
    {
        NSWindow* anchor = [NSApplication sharedApplication].keyWindow;
        if (anchor == nil)
        {
            anchor = [NSApplication sharedApplication].mainWindow;
        }
        if (anchor == nil)
        {
            anchor = [NSApplication sharedApplication].windows.firstObject;
        }
        return anchor;
    }
    return nil;
}

@end

extern "C"
{
    Thirdweb_ASWebAuthenticationSession* Thirdweb_ASWebAuthenticationSession_InitWithURL(
        const char* urlStr, const char* urlSchemeStr, ASWebAuthenticationSessionCompletionCallback completionCallback)
    {
        NSURL* url = [NSURL URLWithString: toString(urlStr)];
        NSString* urlScheme = toString(urlSchemeStr);

        Thirdweb_ASWebAuthenticationSession* session = [[Thirdweb_ASWebAuthenticationSession alloc] initWithURL:url
                                                                            callbackURLScheme:urlScheme
                                                                            completionCallback:completionCallback];
        return session;
    }

    int Thirdweb_ASWebAuthenticationSession_Start(void* sessionPtr)
    {
        Thirdweb_ASWebAuthenticationSession* session = (__bridge Thirdweb_ASWebAuthenticationSession*) sessionPtr;
        BOOL started = [[session session] start];
        return toBool(started);
    }

    void Thirdweb_ASWebAuthenticationSession_Cancel(void* sessionPtr)
    {
        Thirdweb_ASWebAuthenticationSession* session = (__bridge Thirdweb_ASWebAuthenticationSession*) sessionPtr;
        [[session session] cancel];
    }

    int Thirdweb_ASWebAuthenticationSession_GetPrefersEphemeralWebBrowserSession(void* sessionPtr)
    {
        Thirdweb_ASWebAuthenticationSession* session = (__bridge Thirdweb_ASWebAuthenticationSession*) sessionPtr;
        if (@available(macOS 10.15, *))
        {
            return toBool([[session session] prefersEphemeralWebBrowserSession]);
        }
        return 0;
    }

    void Thirdweb_ASWebAuthenticationSession_SetPrefersEphemeralWebBrowserSession(void* sessionPtr, int enable)
    {
        Thirdweb_ASWebAuthenticationSession* session = (__bridge Thirdweb_ASWebAuthenticationSession*) sessionPtr;
        if (@available(macOS 10.15, *))
        {
            [[session session] setPrefersEphemeralWebBrowserSession:toBool(enable)];
        }
    }
}
