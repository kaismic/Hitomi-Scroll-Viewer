using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class SyncManager : Grid {
        private static readonly string USER_EMAIL_FILE_PATH = Path.Combine(ROOT_DIR_V2, "user_email.txt");

        private static readonly ResourceMap ResourceMap = MainResourceMap.GetSubtree("SyncManager");

        private static readonly ResourceMap CredentialsResourceMap = MainResourceMap.GetSubtree("Credentials");
        private static readonly string CLIENT_ID = CredentialsResourceMap.GetValue("OAuthAppClientId").ValueAsString;
        private static readonly string CLIENT_SECRET = CredentialsResourceMap.GetValue("OAuthAppClientSecret").ValueAsString;

        private static readonly string BUTTON_TEXT_NOT_SIGNED_IN = ResourceMap.GetValue("ButtonText_NotSignedIn").ValueAsString;
        private static readonly string BUTTON_TEXT_SIGNED_IN = ResourceMap.GetValue("ButtonText_SignedIn").ValueAsString;
        private static readonly string NOTIFICATION_SIGN_IN_TITLE = ResourceMap.GetValue("Notification_SignIn_Title").ValueAsString;
        private static readonly string NOTIFICATION_SIGN_IN_CONTENT = ResourceMap.GetValue("Notification_SignIn_Content").ValueAsString;
        private static readonly string NOTIFICATION_SIGN_OUT_TITLE = ResourceMap.GetValue("Notification_SignOut_Title").ValueAsString;

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
                string userEmail = null;
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
                    userEmail = await File.ReadAllTextAsync(USER_EMAIL_FILE_PATH);
                }
                DispatcherQueue.TryEnqueue(() => {
                    SignInBtnTextBlock.Text = tokenExists && userEmail != null ? string.Format(BUTTON_TEXT_SIGNED_IN, userEmail) : BUTTON_TEXT_NOT_SIGNED_IN;
                    ToggleSignInState(tokenExists);
                    SignInBtn.IsEnabled = true;
                });
            });
        }

        private async void SignInBtn_Clicked(object _0, RoutedEventArgs _1) {
            SignInBtn.IsEnabled = false;
            try {
                if (_isSignedIn) {
                    ContentDialog contentDialog = new() {
                        Style = Resources["DefaultContentDialogStyle"] as Style,
                        Title = NOTIFICATION_SIGN_OUT_TITLE,
                        CloseButtonText = TEXT_CANCEL
                    };
                    ContentDialogResult cdr = await contentDialog.ShowAsync();
                    if (cdr != ContentDialogResult.Primary) {
                        return;
                    }
                    ToggleSignInState(false);

                    try {
                        await _userCredential.RevokeTokenAsync(CancellationToken.None);
                    } catch (TokenResponseException) {}
                    await FILE_DATA_STORE.DeleteAsync<TokenResponse>(Environment.UserName);
                    File.Delete(USER_EMAIL_FILE_PATH);

                    SignInBtnTextBlock.Text = BUTTON_TEXT_NOT_SIGNED_IN;
                } else {
                    bool isWindowFocused = false;
                    try {
                        CancellationTokenSource cts = new();
                        // ContentDialog is needed because it is currently not possible to detect when the user has closed the browser
                        // ref https://github.com/googleapis/google-api-dotnet-client/issues/508#issuecomment-290700919
                        ContentDialog manualCancelDialog = new() {
                            CloseButtonText = TEXT_CANCEL,
                            Title = new TextBlock() {
                                TextWrapping = TextWrapping.WrapWholeWords,
                                Text = NOTIFICATION_SIGN_IN_TITLE
                            },
                            Content = new TextBlock() {
                                TextWrapping = TextWrapping.WrapWholeWords,
                                Text = NOTIFICATION_SIGN_IN_CONTENT
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
                            _initializer = new() {
                                HttpClientInitializer = _userCredential,
                                ApplicationName = APP_DISPLAY_NAME
                            };
                            Userinfo userInfo = await new Oauth2Service(_initializer).Userinfo.Get().ExecuteAsync();
                            SignInBtnTextBlock.Text = string.Format(BUTTON_TEXT_SIGNED_IN, userInfo.Email);
                            await File.WriteAllTextAsync(USER_EMAIL_FILE_PATH, userInfo.Email);
                            ToggleSignInState(true);
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
                            (MainWindow.CurrentMainWindow.AppWindow.Presenter as OverlappedPresenter).Minimize();
                            MainWindow.CurrentMainWindow.Activate();
                        }
                    }
                }
            } finally {
                SignInBtn.IsEnabled = true;
            }
        }

        private async void SyncBtn_Clicked(object _0, RoutedEventArgs _1) {
            SyncBtn.IsEnabled = false;
            SyncContentDialog dialog = new(new(_initializer)) {
                XamlRoot = XamlRoot,
            };
            await dialog.ShowAsync();
            SyncBtn.IsEnabled = true;
        }

        private void ToggleSignInState(bool isSignedIn) {
            _isSignedIn = isSignedIn;
            SyncBtn.IsEnabled = isSignedIn;
        }
    }
}
