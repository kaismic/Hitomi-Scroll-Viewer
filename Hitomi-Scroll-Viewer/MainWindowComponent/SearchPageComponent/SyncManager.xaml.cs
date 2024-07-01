using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using Google.Apis.Auth.OAuth2;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Threading;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Oauth2.v2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using static Hitomi_Scroll_Viewer.Resources;
using Google.Apis.Auth.OAuth2.Flows;
using Microsoft.UI.Windowing;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent {
    public sealed partial class SyncManager : Grid {
        private static readonly ResourceMap ResourceMap = MainResourceMap.GetSubtree("Credentials");

        private static readonly string CLIENT_ID = ResourceMap.GetValue("OAuthAppClientId").ValueAsString;
        private static readonly string CLIENT_SECRET = ResourceMap.GetValue("OAuthAppClientSecret").ValueAsString;
        private static readonly string[] SCOPES = ["email", DriveService.Scope.DriveAppdata];
        private static readonly FileDataStore FILE_DATA_STORE = new(GoogleWebAuthorizationBroker.Folder);
        private static readonly int AUTH_USER_ACTION_TIMEOUT = 120; // seconds

        private static bool _isSignedIn = false;
        private static UserCredential _userCredential;

        public SyncManager() {
            InitializeComponent();

            TokenResponse tokenResponse = FILE_DATA_STORE.GetAsync<TokenResponse>(Environment.UserName).Result;
            if (tokenResponse != null) {
                ToggleSignInState(true);

                _userCredential = new(
                    new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer {
                        ClientSecrets = new ClientSecrets {
                            ClientId = CLIENT_ID,
                            ClientSecret = CLIENT_SECRET
                        },
                        Scopes = SCOPES,
                        DataStore = FILE_DATA_STORE
                    }),
                    Environment.UserName,
                    tokenResponse
                );

                Trace.WriteLine($"credential.UserId = {_userCredential.UserId}");
                Trace.WriteLine($"token expiry UTC time = {_userCredential.Token.IssuedUtc.AddSeconds((double)_userCredential.Token.ExpiresInSeconds)}");
                Trace.WriteLine($"token expiry time = {_userCredential.Token.Issued.AddSeconds((double)_userCredential.Token.ExpiresInSeconds)}");
                if (tokenResponse.IsStale) {
                    Trace.WriteLine("Refreshing token...");
                    _userCredential.RefreshTokenAsync(CancellationToken.None);
                    Trace.WriteLine("Token refreshed");
                    Trace.WriteLine($"credential.UserId = {_userCredential.UserId}");
                    Trace.WriteLine($"new token expiry UTC time = {_userCredential.Token.IssuedUtc.AddSeconds((double)_userCredential.Token.ExpiresInSeconds)}");
                    Trace.WriteLine($"new token expiry time = {_userCredential.Token.Issued.AddSeconds((double)_userCredential.Token.ExpiresInSeconds)}");
                }

                Trace.WriteLine("Requesting the email address of the user from Google");
                var initializer = new BaseClientService.Initializer() {
                    HttpClientInitializer = _userCredential,
                    ApplicationName = APP_DISPLAY_NAME
                };
                var oauth2Service = new Oauth2Service(initializer);
                SignInBtnTextBlock.Text = "Signed in as " + oauth2Service.Userinfo.Get().ExecuteAsync().Result.Email;
            } else {
                ToggleSignInState(false);
                Trace.WriteLine("There are no previously stored user access token");
            }
        }

        private async void SignInBtn_Clicked(object _0, RoutedEventArgs _1) {
            SignInBtn.IsEnabled = false;
            try {
                if (_isSignedIn) {
                    ContentDialogResult cdr = await MainWindow.SearchPage.ShowConfirmDialogAsync("Sign out?", "Make sure you have synced your data.");
                    if (cdr != ContentDialogResult.Primary) {
                        return;
                    }
                    ToggleSignInState(false);

                    await FILE_DATA_STORE.DeleteAsync<TokenResponse>(Environment.UserName);
                    await _userCredential.RevokeTokenAsync(CancellationToken.None);

                    SignInBtnTextBlock.Text = "Sign in with Google to Sync Data";
                } else {
                    try {
                        Trace.WriteLine("before AuthorizeAsync");
                        CancellationTokenSource cts = new();
                        cts.CancelAfter(AUTH_USER_ACTION_TIMEOUT * 1000);
                        _userCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                            new ClientSecrets {
                                ClientId = CLIENT_ID,
                                ClientSecret = CLIENT_SECRET
                            },
                            SCOPES,
                            Environment.UserName,
                            cts.Token
                        );
                        Trace.WriteLine("after AuthorizeAsync");
                    } catch (TokenResponseException) {
                        Trace.WriteLine("TokenResponseException thrown");
                        return;
                    } catch (OperationCanceledException) {
                        Trace.WriteLine("OperationCanceledException thrown");
                        return;
                    } finally {
                        Trace.WriteLine("activating current window");
                        // MainWindow.Current.Activate() workaround ref https://github.com/microsoft/microsoft-ui-xaml/issues/7595#issuecomment-1909723229
                        OverlappedPresenter presenter = App.MainWindow.AppWindow.Presenter as OverlappedPresenter;
                        presenter.Minimize();
                        App.MainWindow.Activate();
                    }
                    ToggleSignInState(true);
                    Trace.WriteLine($"credential.UserId = {_userCredential.UserId}");
                    Trace.WriteLine($"credential.Token.ExpiresInSeconds = {_userCredential.Token.ExpiresInSeconds}");
                    Trace.WriteLine($"new token expiry UTC time = {_userCredential.Token.IssuedUtc.AddSeconds((double)_userCredential.Token.ExpiresInSeconds)}");
                    Trace.WriteLine($"new token expiry time = {_userCredential.Token.Issued.AddSeconds((double)_userCredential.Token.ExpiresInSeconds)}");

                    Trace.WriteLine("Requesting the e-mail address of the user from Google");

                    var initializer = new BaseClientService.Initializer() {
                        HttpClientInitializer = _userCredential,
                        ApplicationName = APP_DISPLAY_NAME
                    };
                    var oauth2Service = new Oauth2Service(initializer);
                    SignInBtnTextBlock.Text = "Signed in as " + oauth2Service.Userinfo.Get().ExecuteAsync().Result.Email;
                }
            } finally {
                SignInBtn.IsEnabled = true;
            }
        }

        private void SyncBtn_Clicked(object _0, RoutedEventArgs _1) {
            Trace.WriteLine("SyncBtn_Clicked");
            if (_userCredential.Token.IsStale) {
                Trace.WriteLine("The access token is stale, refreshing it");
                if (_userCredential.RefreshTokenAsync(CancellationToken.None).Result) {
                    Trace.WriteLine("The access token is now refreshed");
                    Trace.WriteLine($"new token expiry UTC time = {_userCredential.Token.IssuedUtc.AddSeconds((double)_userCredential.Token.ExpiresInSeconds)}");
                    Trace.WriteLine($"new token expiry time = {_userCredential.Token.Issued.AddSeconds((double)_userCredential.Token.ExpiresInSeconds)}");
                } else {
                    Trace.WriteLine("_userCredential.Token.RefreshToken is null and cannot be refreshed");
                    return;
                }
            }
            //var driveService = new DriveService(initializer);
        }

        private void ToggleSignInState(bool isSignedIn) {
            _isSignedIn = isSignedIn;
            SyncBtn.IsEnabled = isSignedIn;
        }
    }
}
