using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent.SyncManagerComponent;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Hitomi_Scroll_Viewer.Resources;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent {
    public sealed partial class SyncManager : Grid {
        private static readonly string USER_EMAIL_FILE_PATH = Path.Combine(ROOT_DIR, "user_email.txt");

        private static readonly ResourceMap ResourceMap = MainResourceMap.GetSubtree("Credentials");

        private static readonly string CLIENT_ID = ResourceMap.GetValue("OAuthAppClientId").ValueAsString;
        private static readonly string CLIENT_SECRET = ResourceMap.GetValue("OAuthAppClientSecret").ValueAsString;
        private static readonly string[] SCOPES = ["email", DriveService.Scope.DriveAppdata];
        private static readonly FileDataStore FILE_DATA_STORE = new(GoogleWebAuthorizationBroker.Folder);

        private static bool _isSignedIn = false;
        private static UserCredential _userCredential;
        private static BaseClientService.Initializer _initializer;

        public SyncManager() {
            InitializeComponent();
            Task.Run(async () => {
                TokenResponse tokenResponse = await FILE_DATA_STORE.GetAsync<TokenResponse>(Environment.UserName);
                bool tokenExists = tokenResponse != null;
                try {
                    if (tokenExists) {
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
                        _initializer = new BaseClientService.Initializer() {
                            HttpClientInitializer = _userCredential,
                            ApplicationName = APP_DISPLAY_NAME
                        };
                        string userEmail = await File.ReadAllTextAsync(USER_EMAIL_FILE_PATH);
                        DispatcherQueue.TryEnqueue(() => SignInBtnTextBlock.Text = "Signed in as " + userEmail);
                    }
                } finally {
                    DispatcherQueue.TryEnqueue(() => {
                        ToggleSignInState(tokenExists);
                        SignInBtn.IsEnabled = true;
                    });
                }
            });
        }

        private async void SignInBtn_Clicked(object _0, RoutedEventArgs _1) {
            SignInBtn.IsEnabled = false;
            try {
                if (_isSignedIn) {
                    ContentDialogResult cdr = await MainWindow.SearchPage.ShowConfirmDialogAsync("Sign out?", "");
                    if (cdr != ContentDialogResult.Primary) {
                        return;
                    }
                    ToggleSignInState(false);

                    await _userCredential.RevokeTokenAsync(CancellationToken.None);
                    await FILE_DATA_STORE.DeleteAsync<TokenResponse>(Environment.UserName);
                    File.Delete(USER_EMAIL_FILE_PATH);

                    SignInBtnTextBlock.Text = "Sign in with Google to Sync Data";
                } else {
                    bool isWindowFocused = false;
                    try {
                        CancellationTokenSource cts = new();
                        // ContentDialog is needed because it is currently not possible to detect when the user has closed the browser
                        // ref https://github.com/googleapis/google-api-dotnet-client/issues/508#issuecomment-290700919
                        ContentDialog manualCancelDialog = new() {
                            CloseButtonText = DIALOG_BUTTON_TEXT_CANCEL,
                            Title = new TextBlock() {
                                TextWrapping = TextWrapping.WrapWholeWords,
                                Text = "Waiting to Sign in on external browser..."
                            },
                            Content = new TextBlock() {
                                TextWrapping = TextWrapping.WrapWholeWords,
                                Text = "To cancel sign in, click the cancel button."
                            },
                            XamlRoot = XamlRoot
                        };
                        Task manualCancelTask = manualCancelDialog.ShowAsync().AsTask();
                        Task<UserCredential> authTask = GoogleWebAuthorizationBroker.AuthorizeAsync(
                            new ClientSecrets {
                                ClientId = CLIENT_ID,
                                ClientSecret = CLIENT_SECRET
                            },
                            SCOPES,
                            Environment.UserName,
                            cts.Token
                        );
                        if (await Task.WhenAny(manualCancelTask, authTask) == authTask) {
                            manualCancelDialog.Hide();
                            _userCredential = await authTask;
                        } else {
                            isWindowFocused = true;
                            cts.Cancel();
                            return;
                        }
                    } catch (TokenResponseException) {
                        return;
                    } catch (OperationCanceledException) {
                        return;
                    } finally {
                        // App.MainWindow.Activate(); alone doesn't work and instead we need to
                        // minimize then activate the window because of this bug https://github.com/microsoft/microsoft-ui-xaml/issues/7595
                        if (!isWindowFocused) {
                            (App.MainWindow.AppWindow.Presenter as OverlappedPresenter).Minimize();
                            App.MainWindow.Activate();
                        }
                    }
                    ToggleSignInState(true);

                    _initializer = new() {
                        HttpClientInitializer = _userCredential,
                        ApplicationName = APP_DISPLAY_NAME
                    };
                    Userinfo userInfo = await new Oauth2Service(_initializer).Userinfo.Get().ExecuteAsync();
                    SignInBtnTextBlock.Text = "Signed in as " + userInfo.Email;
                    await File.WriteAllTextAsync(USER_EMAIL_FILE_PATH, userInfo.Email);
                }
            } finally {
                SignInBtn.IsEnabled = true;
            }
        }

        private async void SyncBtn_Clicked(object _0, RoutedEventArgs _1) {
            SyncContentDialog dialog = new(new(_initializer)) {
                XamlRoot = XamlRoot,
            };
            await dialog.ShowAsync();
        }

        private void ToggleSignInState(bool isSignedIn) {
            _isSignedIn = isSignedIn;
            SyncBtn.IsEnabled = isSignedIn;
        }
    }
}
