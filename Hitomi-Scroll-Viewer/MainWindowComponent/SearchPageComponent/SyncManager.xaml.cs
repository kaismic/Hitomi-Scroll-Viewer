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
using static Hitomi_Scroll_Viewer.MainWindowComponent.SearchPage;
using Google.Apis.Auth.OAuth2.Flows;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent {
    public sealed partial class SyncManager : Grid {
        private static readonly ResourceMap ResourceMap = MainResourceMap.GetSubtree("Credentials");

        private static readonly string CLIENT_ID = ResourceMap.GetValue("OAuthAppClientId").ValueAsString;
        private static readonly string CLIENT_SECRET = ResourceMap.GetValue("OAuthAppClientSecret").ValueAsString;
        private static readonly string[] SCOPES = ["email", DriveService.Scope.DriveAppdata];

        private static bool _isSignedIn = false;

        public SyncManager() {
            InitializeComponent();

            Trace.WriteLine("SyncManager constructor called");
            FileDataStore fileDataStore = new(GoogleWebAuthorizationBroker.Folder);
            TokenResponse tokenResponse = fileDataStore.GetAsync<TokenResponse>(Environment.UserName).Result;

            if (tokenResponse != null) {
                if (tokenResponse.IsStale) {
                    UserCredential credential = new(
                        new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer {
                            ClientSecrets = new ClientSecrets {
                                ClientId = CLIENT_ID,
                                ClientSecret = CLIENT_SECRET
                            },
                            Scopes = SCOPES,
                            DataStore = fileDataStore
                        }),
                        Environment.UserName,
                        tokenResponse
                    );
                    Trace.WriteLine($"credential.UserId = {credential.UserId}");
                    Trace.WriteLine($"token expiry UTC time = {tokenResponse.IssuedUtc.AddSeconds((double)credential.Token.ExpiresInSeconds)}");
                    Trace.WriteLine($"token expiry time = {tokenResponse.Issued.AddSeconds((double)credential.Token.ExpiresInSeconds)}");
                }
            } else {
                Trace.WriteLine("tokenResponse is null");

            }

            //if (tokenResponse != null) {
            //    _isSignedIn = true;
            //    Trace.WriteLine("Requesting the e-mail address of the user from Google");
            //    GoogleCredential credential = GoogleCredential.FromAccessToken(tokenResponse.AccessToken);
            //    Trace.WriteLine("1111");

            //    var initializer = new BaseClientService.Initializer() {
            //        HttpClientInitializer = credential,
            //        ApplicationName = APP_DISPLAY_NAME
            //    };
            //    Trace.WriteLine("2222");
            //    var oauth2Service = new Oauth2Service(initializer);
            //    Trace.WriteLine("3333");
            //    SignInBtnTextBlock.Text = "Signed in as " + oauth2Service.Userinfo.Get().ExecuteAsync().Result.Email;
            //    Trace.WriteLine("4444");
            //} else {
            //    Trace.WriteLine("There are no previously stored user access token");
            //}

        }

        private async void SignInBtn_Clicked(object _0, RoutedEventArgs _1) {
            SignInBtn.IsEnabled = false;
            try {
                //if (_isSignedIn) {
                //    ContentDialogResult cdr = await MainWindow.SearchPage.ShowConfirmDialogAsync("Sign out?", "Make sure you have synced your data.");
                //    if (cdr != ContentDialogResult.Primary) {
                //        return;
                //    }
                //    FileDataStore fileDataStore = new(GoogleWebAuthorizationBroker.Folder);
                //    TokenResponse tokenResponse = fileDataStore.GetAsync<TokenResponse>(USER).Result;
                //    UserCredential credential = new(
                //        new GoogleAuthorizationCodeFlow.Initializer {
                //            ClientSecrets = new ClientSecrets {
                //                ClientId = CLIENT_ID,
                //                ClientSecret = CLIENT_SECRET
                //            },
                //            Scopes = SCOPES,
                //            DataStore = fileDataStore
                //        },

                //    );
                //    FileDataStore fileDataStore = new(GoogleWebAuthorizationBroker.Folder);
                //    await fileDataStore.DeleteAsync<TokenResponse>(USER);
                    
                //} else {
                    UserCredential credential;
                    try {
                        credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                            new ClientSecrets {
                                ClientId = CLIENT_ID,
                                ClientSecret = CLIENT_SECRET
                            },
                            SCOPES,
                            Environment.UserName,
                            CancellationToken.None
                        );
                    } catch (TokenResponseException) {
                        return;
                    } finally {
                        Window.Current.Activate();
                    }

                    Trace.WriteLine($"credential.UserId = {credential.UserId}");
                    Trace.WriteLine($"credential.Token.ExpiresInSeconds = {credential.Token.ExpiresInSeconds}");
                    Trace.WriteLine($"Environment.UserName = {Environment.UserName}");

                    if (credential.Token.IsStale) {
                        Trace.WriteLine("The access token is stale, refreshing it");
                        if (credential.RefreshTokenAsync(CancellationToken.None).Result) {
                            Trace.WriteLine("The access token is now refreshed");
                        } else {
                            Trace.WriteLine("credential.Token.RefreshToken is null and cannot be refreshed");
                            return;
                        }
                    } else {
                        Trace.WriteLine("The access token is OK, continue");
                    }

                    Trace.WriteLine("Requesting the e-mail address of the user from Google");

                    var initializer = new BaseClientService.Initializer() {
                        HttpClientInitializer = credential,
                        ApplicationName = APP_DISPLAY_NAME
                    };
                    var oauth2Service = new Oauth2Service(initializer);
                    SignInBtnTextBlock.Text = "Signed in as " + oauth2Service.Userinfo.Get().ExecuteAsync().Result.Email;

                    
                //}

                //var driveService = new DriveService(initializer);

            } finally {
                SignInBtn.IsEnabled = true;
            }
        }

        private async void SyncBtn_Clicked(object _0, RoutedEventArgs _1) { 
        
        }
    }
}
