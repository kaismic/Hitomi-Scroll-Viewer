﻿using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class GalleryService(HttpClient httpClient) {
        public async Task<int> GetCount() {
            return await httpClient.GetFromJsonAsync<int>("count");
        }

        public async Task<GalleryMinDTO?> GetGalleryMinDTO(int id) {
            try {
                HttpResponseMessage response = await httpClient.GetAsync($"min?id={id}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GalleryMinDTO>();
            } catch (HttpRequestException e) {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                }
                throw;
            }
        }

        public async Task<GalleryFullDTO?> GetGalleryFullDTO(int id) {
            try {
                HttpResponseMessage response = await httpClient.GetAsync($"full?id={id}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GalleryFullDTO>();
            } catch (HttpRequestException e) {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                }
                throw;
            }
        }

        /// <summary>
        /// <paramref name="pageIndex"/> is 0-based
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="itemsPerPage"></param>
        /// <returns></returns>
        public async Task<IEnumerable<GalleryFullDTO>> GetGalleryFullDTOs(int pageIndex, int itemsPerPage) {
            return (await httpClient.GetFromJsonAsync<IEnumerable<GalleryFullDTO>>($"galleries?pageIndex={pageIndex}&itemsPerPage={itemsPerPage}"))!;
        }
    }
}
