﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using HitomiScrollViewerLib.DAOs;
using HitomiScrollViewerLib.Models;
using HitomiScrollViewerLib.Views.SearchPageViews;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static HitomiScrollViewerLib.Constants;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public partial class SyncManagerVM : DQObservableObject {
        private const string USER_EMAIL_FILE_NAME = "user_email.txt";
        private static readonly string USER_EMAIL_FILE_PATH_V2 = Path.Combine(ROOT_DIR_V2, USER_EMAIL_FILE_NAME);
        private static readonly string USER_EMAIL_FILE_PATH_V3 = Path.Combine(ROAMING_DIR_V3, USER_EMAIL_FILE_NAME);
        // WARNING: Do not replace this with SharedResources.APP_DISPLAY_NAME because app name not in English will throw eror
        private const string USER_AGENT_HEADER_APP_NAME = "Hitomi Scroll Viewer";

        private static readonly string SUBTREE_NAME = typeof(SyncManager).Name;

        private static readonly ClientSecrets CLIENT_SECRETS = new() {
            ClientId = "OAuthAppClientId".GetLocalized("Credentials"),
            ClientSecret = "OAuthAppClientSecret".GetLocalized("Credentials")
        };

        private static readonly string[] SCOPES = ["email", DriveService.Scope.DriveAppdata];
        private static readonly FileDataStore FILE_DATA_STORE = new(GoogleWebAuthorizationBroker.Folder);

        private static UserCredential _userCredential;
        private static BaseClientService.Initializer Initializer { get; set; }

        [ObservableProperty]
        private bool _isSignedIn = false;
        partial void OnIsSignedInChanged(bool value) {
            IsSyncButtonEnabled = value;
        }

        [ObservableProperty]
        private string _signInButtonText;
        [ObservableProperty]
        private bool _isSignInButtonEnabled;
        [ObservableProperty]
        private bool _isSyncButtonEnabled;

        private readonly TagFilterDAO _tagFilterDAO;
        public SyncManagerVM(TagFilterDAO tagFilterDAO) {
            _tagFilterDAO = tagFilterDAO;
            _ = Task.Run(async () => {
                TokenResponse tokenResponse = await FILE_DATA_STORE.GetAsync<TokenResponse>(Environment.UserName);
                bool tokenExists = tokenResponse != null;
                string userEmail = null;
                if (tokenExists) {
                    _userCredential = new(
                        new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer {
                            ClientSecrets = CLIENT_SECRETS,
                            Scopes = SCOPES,
                            DataStore = FILE_DATA_STORE
                        }),
                        Environment.UserName,
                        tokenResponse
                    );
                    Initializer = new BaseClientService.Initializer() {
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
                    string.Format("ButtonText_SignedIn".GetLocalized(SUBTREE_NAME), userEmail) :
                    "ButtonText_NotSignedIn".GetLocalized(SUBTREE_NAME);
            });
        }


        [RelayCommand(CanExecute = nameof(IsSignInButtonEnabled))]
        private async Task HandleSignInButtonClick() {
            IsSignInButtonEnabled = false;
            try {
                if (IsSignedIn) {
                    ContentDialogModel signOutDialogModel = new() {
                        DefaultButton = ContentDialogButton.Primary,
                        Title = "Notification_SignOut_Title".GetLocalized(SUBTREE_NAME),
                        PrimaryButtonText = TEXT_YES
                    };
                    
                    ContentDialogResult cdr = await MainWindowVM.NotifyUser(signOutDialogModel);
                    if (cdr != ContentDialogResult.Primary) {
                        return;
                    }
                    IsSignedIn = false;
                    SignInButtonText = "ButtonText_NotSignedIn".GetLocalized(SUBTREE_NAME);

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
                        ContentDialogModel manualCancelDialogModel = new() {
                            Title = "Notification_SignIn_Title".GetLocalized(SUBTREE_NAME),
                            Message = "Notification_SignIn_Content".GetLocalized(SUBTREE_NAME)
                        };
                        Task manualCancelTask = MainWindowVM.NotifyUser(manualCancelDialogModel).AsTask();
                        Task<UserCredential> authTask = GoogleWebAuthorizationBroker.AuthorizeAsync(
                            CLIENT_SECRETS,
                            SCOPES,
                            Environment.UserName,
                            cts.Token
                        );
                        if (await Task.WhenAny(manualCancelTask, authTask) == authTask) {
                            MainWindowVM.HideCurrentNotification();
                            _userCredential = await authTask;
                            Initializer = new() {
                                HttpClientInitializer = _userCredential,
                                ApplicationName = USER_AGENT_HEADER_APP_NAME
                            };
                            Userinfo userInfo = await new Oauth2Service(Initializer).Userinfo.Get().ExecuteAsync();
                            await File.WriteAllTextAsync(USER_EMAIL_FILE_PATH_V2, userInfo.Email);
                            SignInButtonText = string.Format("ButtonText_SignedIn".GetLocalized(SUBTREE_NAME), userInfo.Email);
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
                            MainWindowVM.MinimizeWindow();
                            MainWindowVM.ActivateWindow();
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

        public event Func<SyncContentDialogVM, Task> ShowDialogRequested;

        [RelayCommand(CanExecute = nameof(IsSyncButtonEnabled))]
        public async Task HandleSyncButtonClick() {
            IsSyncButtonEnabled = false;
            SyncContentDialogVM vm = new(new(Initializer), _tagFilterDAO);
            await ShowDialogRequested?.Invoke(vm);
            IsSyncButtonEnabled = true;
        }
    }
}
