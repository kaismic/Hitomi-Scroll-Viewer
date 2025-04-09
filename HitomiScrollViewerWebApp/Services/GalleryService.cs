using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class GalleryService(HttpClient httpClient) {
        public async Task<GalleryDownloadDTO?> GetGalleryDownloadDTO(int id) {
            return await httpClient.GetFromJsonAsync<GalleryDownloadDTO>($"api/gallery/get-download-dto?id={id}");
        }

        public async Task<int> GetCount() {
            return await httpClient.GetFromJsonAsync<int>("api/gallery/count");
        }

        /// <summary>
        /// <paramref name="pageIndex"/> is 0-based
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="itemsPerPage"></param>
        /// <returns></returns>
        public async Task<IEnumerable<GalleryFullDTO>> GetGalleryFullDTOs(int pageIndex, int itemsPerPage) {
            return (await httpClient.GetFromJsonAsync<IEnumerable<GalleryFullDTO>>($"api/gallery/galleries?pageIndex={pageIndex}&itemsPerPage={itemsPerPage}"))!;
        }
    }
}
