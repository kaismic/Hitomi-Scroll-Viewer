﻿using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class SearchConfigurationService(HttpClient httpClient) {
        private bool _isLoaded = false;
        public bool IsInitTagFilterNull { get; private set; } = false;
        public SearchConfigurationDTO Config { get; private set; } = new() {
            SearchFilters = [],
            TitleSearchKeyword = "",
            SelectedExcludeTagFilterIds = [],
            SelectedIncludeTagFilterIds = [],
            SelectedTagFilterId = 0,
            SelectedLanguage = new() { EnglishName = "", Id = 0, IsAll = true, LocalName = "" },
            SelectedType = new() { Id = 0, IsAll = true, Value = "" },
            TagFilters = []
        };

        public async Task Load() {
            if (_isLoaded) {
                return;
            }
            Config = (await httpClient.GetFromJsonAsync<SearchConfigurationDTO>(""))!;
            if (Config.SelectedTagFilterId == default) {
                IsInitTagFilterNull = true;
            }
            _isLoaded = true;
        }

        public async Task<bool> UpdateAutoSaveAsync(bool enable) {
            var response = await httpClient.PatchAsync($"enable-auto-save?configId={Config.Id}&enable={enable}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSelectedTagFilterCollectionAsync(bool isInclude, IEnumerable<int> tagFilterIds) {
            var response = await httpClient.PatchAsync($"tag-filter-collection?configId={Config.Id}&isInclude={isInclude}", JsonContent.Create(tagFilterIds));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSelectedTagFilterAsync(int tagFilterId) {
            var response = await httpClient.PatchAsync($"selected-tag-filter?configId={Config.Id}&tagFilterId={tagFilterId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTitleSearchKeywordAsync(string titleSearchKeyword) {
            var response = await httpClient.PatchAsync($"title-search-keyword?configId={Config.Id}", JsonContent.Create(titleSearchKeyword));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateLanguageAsync(int languageId) {
            var response = await httpClient.PatchAsync($"language?configId={Config.Id}&languageId={languageId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTypeAsync(int typeId) {
            var response = await httpClient.PatchAsync($"type?configId={Config.Id}&typeId={typeId}", null);
            return response.IsSuccessStatusCode;
        }
    }
}
