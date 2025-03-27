﻿using HitomiScrollViewerData;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Layout {
    public partial class MainLayout : LayoutComponentBase, IAsyncDisposable {
        private MudThemeProvider _mudThemeProvider = null!;
        private readonly MudTheme _theme = new();
        private bool _isDarkMode;
        private bool _drawerOpen = true;

        private HubConnection? _hubConnection;
        private bool _isInitialized = false;
        private bool _connectionError = false;
        private string _status = "";

        private void DrawerToggle() => _drawerOpen = !_drawerOpen;

        protected override async Task OnInitializedAsync() {
            try {
                _status = "Connecting to local server...";
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(ApiUrlService.BaseUrl + "api/initialize")
                    .Build();
                _hubConnection.On<InitStatus, int>("ReceiveStatus", UpdateStatus);
                await _hubConnection.StartAsync();
            } catch (HttpRequestException) {
                _connectionError = true;
                _status = "Connection error. Please reload after starting the local server.";
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                _isDarkMode = await _mudThemeProvider.GetSystemPreference();
            }
        }

        private async Task UpdateStatus(InitStatus status, int progress) {
            switch (status) {
                case InitStatus.InProgress:
                    _status = progress switch {
                        0 => "Adding tags...",
                        1 => "Adding gallery language and types...",
                        2 => "Adding query configurations...",
                        3 => "Adding gallery sorts...",
                        4 => "Adding example tag filters...",
                        _ => "Unknown status"
                    };
                    await InvokeAsync(StateHasChanged);
                    break;
                case InitStatus.Complete:
                    _status = "Fetching data from database...";
                    await InvokeAsync(StateHasChanged);
                    PageConfigurationService.Languages = [.. await LanguageTypeService.GetLanguagesAsync()];
                    PageConfigurationService.Types = [.. await LanguageTypeService.GetTypesAsync()];
                    _status = "Initialization complete";
                    await InvokeAsync(StateHasChanged);
                    _isInitialized = true;
                    if (_hubConnection is not null) {
                        await _hubConnection.DisposeAsync();
                    }
                    await InvokeAsync(StateHasChanged);
                    //NavigationManager.NavigateTo("/");
                    break;
            }
        }

        public async ValueTask DisposeAsync() {
            if (_hubConnection is not null) {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
