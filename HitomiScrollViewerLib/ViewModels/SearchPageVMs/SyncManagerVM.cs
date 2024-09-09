using CommunityToolkit.Mvvm.ComponentModel;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using HitomiScrollViewerLib.Views.SearchPageViews;
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

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public partial class SyncManagerVM : ObservableObject {
        private const string USER_EMAIL_FILE_NAME = "user_email.txt";
        private static readonly string USER_EMAIL_FILE_PATH_V2 = Path.Combine(ROOT_DIR_V2, USER_EMAIL_FILE_NAME);
        private static readonly string USER_EMAIL_FILE_PATH_V3 = Path.Combine(ROAMING_DIR_V3, USER_EMAIL_FILE_NAME);
        // WARNING: Do not replace this with SharedResources.APP_DISPLAY_NAME because app name not in English will throw eror
        private const string USER_AGENT_HEADER_APP_NAME = "Hitomi Scroll Viewer";

        private static readonly string[] SCOPES = ["email", DriveService.Scope.DriveAppdata];
        private static readonly FileDataStore FILE_DATA_STORE = new(GoogleWebAuthorizationBroker.Folder);

        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(SyncManager).Name);
        private static readonly ResourceMap _credentialsResourceMap = MainResourceMap.GetSubtree("Credentials");

        private static UserCredential _userCredential;
        private static BaseClientService.Initializer _initializer;

        [ObservableProperty]
        private string _signInButtonText;
        [ObservableProperty]
        private bool _isSignInButtonEnabled = false;
        [ObservableProperty]
        private bool _isSignedIn = false;
        partial void OnIsSignedInChanged(bool value) {
            IsSyncButtonEnabled = value;
        }
        [ObservableProperty]
        private bool _isSyncButtonEnabled = false;

        private SyncContentDialog _syncContentDialog;
        private SyncContentDialog SyncContentDialog =>
            _syncContentDialog ??= new(new(new(_initializer))) { XamlRoot = MainWindow.SearchPageVM.XamlRoot };

        public SyncManagerVM() {
            _ = Task.Run(async () => {
                TokenResponse tokenResponse = await FILE_DATA_STORE.GetAsync<TokenResponse>(Environment.UserName);
                bool tokenExists = tokenResponse != null;
                string userEmail = null;
                if (tokenExists) {
                    _userCredential = new(
                        new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer {
                            ClientSecrets = new ClientSecrets {
                                ClientId = _credentialsResourceMap.GetValue("OAuthAppClientId").ValueAsString,
                                ClientSecret = _credentialsResourceMap.GetValue("OAuthAppClientSecret").ValueAsString
                            },
                            Scopes = SCOPES,
                            DataStore = FILE_DATA_STORE
                        }),
                        Environment.UserName,
                        tokenResponse
                    );
                    _initializer = new BaseClientService.Initializer() {
                        HttpClientInitializer = _userCredential,
                        ApplicationName = USER_AGENT_HEADER_APP_NAME
                    };
                    if (File.Exists(USER_EMAIL_FILE_PATH_V2)) {
                        File.Move(USER_EMAIL_FILE_PATH_V2, USER_EMAIL_FILE_PATH_V3);
                    }
                    userEmail = await File.ReadAllTextAsync(USER_EMAIL_FILE_PATH_V3);
                }
                IsSignInButtonEnabled = true;
                IsSignedIn = tokenExists && userEmail != null;
                SignInButtonText =
                    IsSignedIn ?
                    string.Format(_resourceMap.GetValue("ButtonText_SignedIn").ValueAsString, userEmail) :
                    _resourceMap.GetValue("ButtonText_NotSignedIn").ValueAsString;
            });
        }

        public async void SignInBtn_Clicked(object _0, RoutedEventArgs _1) {
            IsSignInButtonEnabled = false;
            try {
                if (IsSignedIn) {
                    ContentDialog contentDialog = new() {
                        DefaultButton = ContentDialogButton.Primary,
                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = _resourceMap.GetValue("Notification_SignOut_Title").ValueAsString,
                        PrimaryButtonText = TEXT_YES,
                        CloseButtonText = TEXT_CANCEL,
                        XamlRoot = MainWindow.SearchPageVM.XamlRoot
                    };
                    ContentDialogResult cdr = await contentDialog.ShowAsync();
                    if (cdr != ContentDialogResult.Primary) {
                        return;
                    }
                    IsSignedIn = false;
                    SignInButtonText = _resourceMap.GetValue("ButtonText_NotSignedIn").ValueAsString;

                    try {
                        await _userCredential.RevokeTokenAsync(CancellationToken.None);
                    } catch (TokenResponseException) { }
                    await FILE_DATA_STORE.DeleteAsync<TokenResponse>(Environment.UserName);
                    File.Delete(USER_EMAIL_FILE_PATH_V3);
                } else {
                    bool isWindowFocused = false;
                    CancellationTokenSource cts = new();
                    try {
                        // ContentDialog is needed because it is currently not possible to detect when the user has closed the browser
                        // ref https://github.com/googleapis/google-api-dotnet-client/issues/508#issuecomment-290700919
                        ContentDialog manualCancelDialog = new() {
                            CloseButtonText = TEXT_CANCEL,
                            Title = new TextBlock() {
                                TextWrapping = TextWrapping.WrapWholeWords,
                                Text = _resourceMap.GetValue("Notification_SignIn_Title").ValueAsString
                            },
                            Content = new TextBlock() {
                                TextWrapping = TextWrapping.WrapWholeWords,
                                Text = _resourceMap.GetValue("Notification_SignIn_Content").ValueAsString
                            },
                            XamlRoot = MainWindow.SearchPageVM.XamlRoot
                        };
                        Task manualCancelTask = manualCancelDialog.ShowAsync().AsTask();
                        Task<UserCredential> authTask = GoogleWebAuthorizationBroker.AuthorizeAsync(
                            new ClientSecrets {
                                ClientId = _credentialsResourceMap.GetValue("OAuthAppClientId").ValueAsString,
                                ClientSecret = _credentialsResourceMap.GetValue("OAuthAppClientSecret").ValueAsString
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
                                ApplicationName = USER_AGENT_HEADER_APP_NAME
                            };
                            Userinfo userInfo = await new Oauth2Service(_initializer).Userinfo.Get().ExecuteAsync();
                            await File.WriteAllTextAsync(USER_EMAIL_FILE_PATH_V2, userInfo.Email);
                            SignInButtonText = string.Format(_resourceMap.GetValue("ButtonText_SignedIn").ValueAsString, userInfo.Email);
                            IsSignedIn = true;
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
                            (MainWindow.CurrMW.AppWindow.Presenter as OverlappedPresenter).Minimize();
                            MainWindow.CurrMW.Activate();
                            if (!cts.IsCancellationRequested) {
                                cts.Dispose();
                            }
                        }
                    }
                }
            } finally {
                IsSignInButtonEnabled = true;
            }
        }

        public async void SyncBtn_Clicked(object _0, RoutedEventArgs _1) {
            IsSyncButtonEnabled = false;
            await SyncContentDialog.ShowAsync();
            IsSyncButtonEnabled = true;
        }
    }
}
