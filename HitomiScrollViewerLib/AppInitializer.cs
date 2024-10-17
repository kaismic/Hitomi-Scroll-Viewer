using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.DTOs;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels;
using HitomiScrollViewerLib.ViewModels.PageVMs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Windows.ApplicationModel;
using static HitomiScrollViewerLib.Constants;

namespace HitomiScrollViewerLib {
    public static class AppInitializer {
        public static event Action<LoadProgressReporterVM> ShowLoadProgressReporter;
        public static event Action HideLoadProgressReporter;
        public static event Action Initialised;

        public static void Start() {
            LoadProgressReporterVM vm = new() {
                IsIndeterminate = true
            };
            ShowLoadProgressReporter.Invoke(vm);

            vm.SetText(LoadProgressReporterVM.LoadingStatus.InitialisingDatabase);

            bool dbCreatedFirstTime;
            using (HitomiContext context = new()) {
                //context.Database.EnsureDeleted(); // Uncomment to reset database @@@@@@@@@@@
                dbCreatedFirstTime = context.Database.EnsureCreated();
                if (dbCreatedFirstTime) {
                    vm.IsIndeterminate = false;
                    vm.Value = 0;
                    vm.Maximum = DATABASE_INIT_OP_COUNT;
                    DatabaseInitProgressChanged += (value) => vm.Value = value;
                    ChangeToIndeterminateEvent += () => vm.IsIndeterminate = true;
                    InitDatabase(context);
                }
            }

            bool v2TagFilterExists = File.Exists(TAG_FILTERS_FILE_PATH_V2);
            // User upgraded from v2 to v3
            if (v2TagFilterExists) {
                vm.IsIndeterminate = false;
                vm.Value = 0;
                vm.SetText(LoadProgressReporterVM.LoadingStatus.MigratingTFSs);
                Dictionary<string, LegacyTagFilter> legacyTagFilters = JsonSerializer.Deserialize<Dictionary<string, LegacyTagFilter>>(
                    File.ReadAllText(TAG_FILTERS_FILE_PATH_V2),
                    LegacyTagFilter.SERIALIZER_OPTIONS
                );
                vm.Maximum = legacyTagFilters.Count;
                using HitomiContext context = new();
                foreach (var pair in legacyTagFilters) {
                    context.TagFilters.AddRange(pair.Value.ToTagFilters(context, pair.Key));
                    vm.Value++;
                }
                context.SaveChanges();
                File.Delete(TAG_FILTERS_FILE_PATH_V2);
            }

            // The user installed this app for the first time (which means there is no previous tf)
            // AND is starting the app for the first time
            if (!v2TagFilterExists && dbCreatedFirstTime) {
                vm.IsIndeterminate = true;
                vm.SetText(LoadProgressReporterVM.LoadingStatus.AddingExampleTFSs);
                using HitomiContext context = new();
                AddExampleTagFilters(context);
                context.SaveChanges();
            }

            // migrate existing galleries (p.k.a. bookmarks) from v2
            if (File.Exists(BOOKMARKS_FILE_PATH_V2)) {
                vm.IsIndeterminate = false;
                vm.Value = 0;
                vm.SetText(LoadProgressReporterVM.LoadingStatus.MigratingGalleries);
                ICollection<OriginalGalleryInfoDTO> originalGalleryInfos = (ICollection<OriginalGalleryInfoDTO>)JsonSerializer.Deserialize(
                    File.ReadAllText(BOOKMARKS_FILE_PATH_V2),
                    typeof(List<OriginalGalleryInfoDTO>),
                    OriginalGalleryInfoDTO.SERIALIZER_OPTIONS
                );
                vm.Maximum = originalGalleryInfos.Count;
                using HitomiContext context = new();
                foreach (var ogi in originalGalleryInfos) {
                    context.Galleries.Add(ogi.ToGallery(context));
                    vm.Value++;
                }
                context.SaveChanges();
                File.Delete(BOOKMARKS_FILE_PATH_V2);
            }

            if (Directory.Exists(IMAGE_DIR_V2)) {
                // move images folder in roaming folder to local
                vm.IsIndeterminate = true;
                vm.SetText(LoadProgressReporterVM.LoadingStatus.MovingImageFolder);
                Directory.Move(IMAGE_DIR_V2, IMAGE_DIR_V3);

                // rename image files
                vm.SetText(LoadProgressReporterVM.LoadingStatus.RenamingImageFiles);
                vm.IsIndeterminate = false;
                IEnumerable<string> dirPaths = Directory.EnumerateDirectories(IMAGE_DIR_V3);

                using HitomiContext context = new();
                context.Galleries.Include(g => g.Files).Load();
                foreach (string dirPath in Directory.EnumerateDirectories(IMAGE_DIR_V3)) {
                    int id = int.Parse(Path.GetFileName(dirPath));
                    Gallery gallery = context.Galleries.Find(id);
                    ImageInfo[] imageInfos = [.. gallery.Files.OrderBy(f => f.Index)];
                    vm.Value = 0;
                    vm.Maximum = imageInfos.Length;
                    for (int i = imageInfos.Length - 1; i >= 0; i--) {
                        string oldFilePath = Path.Combine(dirPath, i.ToString() + '.' + imageInfos[i].FileExtension);
                        string newFilePath = Path.Combine(dirPath, imageInfos[i].FullFileName);
                        if (File.Exists(oldFilePath)) {
                            File.Move(oldFilePath, newFilePath);
                        }
                        vm.Value++;
                    }
                }
            }

            // Uncomment on production @@@@@@@@@@@@@@@@@@@@@@@@@@
            //if (Directory.Exists(ROOT_DIR_V2)) {
            //    Directory.Delete(ROOT_DIR_V2);
            //}

            vm.IsIndeterminate = true;
            vm.SetText(LoadProgressReporterVM.LoadingStatus.InitialisingApp);

            SearchPageVM.Init();
            BrowsePageVM.Init();
            ViewPageVM.Init();

            HideLoadProgressReporter.Invoke();
            Initialised?.Invoke();
        }


        private static readonly string[] ALPHABETS_WITH_123 =
            ["123", .. Enumerable.Range('a', 26).Select(intValue => Convert.ToChar(intValue).ToString())];

        private const string DB_RES_ROOT_DIR = "DatabaseResources";
        private static readonly string DELIMITER_FILE_PATH = Path.Combine(
            Package.Current.InstalledPath,
            DB_RES_ROOT_DIR,
            "delimiter.txt"
        );
        private static readonly string LANGUAGES_FILE_PATH = Path.Combine(
            Package.Current.InstalledPath,
            DB_RES_ROOT_DIR,
            "languages.txt"
        );
        private static readonly Dictionary<TagCategory, string> CATEGORY_DIR_DICT = new() {
            { TagCategory.Tag, "Tags" },
            { TagCategory.Male, "Males" },
            { TagCategory.Female, "Females" },
            { TagCategory.Artist, "Artists" },
            { TagCategory.Group, "Groups" },
            { TagCategory.Character, "Characters" },
            { TagCategory.Series, "Series" }
        };

        public static readonly int DATABASE_INIT_OP_COUNT = Tag.TAG_CATEGORIES.Length * ALPHABETS_WITH_123.Length;
        public static event Action<int> DatabaseInitProgressChanged;
        public static event Action ChangeToIndeterminateEvent;
        private static void InitDatabase(HitomiContext context) {
            string delimiter = File.ReadAllText(DELIMITER_FILE_PATH);
            int progressValue = 0;
            // add tags
            foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                string categoryStr = CATEGORY_DIR_DICT[category];
                string dir = Path.Combine(
                    Package.Current.InstalledPath,
                    DB_RES_ROOT_DIR,
                    categoryStr
                );
                foreach (string alphanumStr in ALPHABETS_WITH_123) {
                    string path = Path.Combine(dir, $"{categoryStr.ToLower()}-{alphanumStr}.txt");
                    string[] tagInfoStrs = File.ReadAllLines(path);
                    context.Tags.AddRange(tagInfoStrs.Select(
                        tagInfoStr => {
                            string[] tagInfoArr = tagInfoStr.Split(delimiter);
                            return new Tag() {
                                Category = category,
                                Value = tagInfoArr[0],
                                GalleryCount = int.Parse(tagInfoArr[1])
                            };
                        }
                    ));
                    DatabaseInitProgressChanged?.Invoke(++progressValue);
                }
            }
            // change to indeterminate because SaveChanges() takes a long time and doesn't provide progress values
            ChangeToIndeterminateEvent?.Invoke();

            // add gallery languages and its local names
            string[][] languages = File.ReadAllLines(LANGUAGES_FILE_PATH).Select(pair => pair.Split(delimiter)).ToArray();
            context.GalleryLanguages.Add(new GalleryLanguage() {
                IsAll = true,
                LocalName = null,
                SearchParamValue = null
            });
            context.GalleryLanguages.AddRange(languages.Select(
                pair => {
                    return new GalleryLanguage() {
                        IsAll = false,
                        SearchParamValue = pair[0],
                        LocalName = pair[1]
                    };
                }
            ));
            // add gallery types
            context.GalleryTypes.AddRange(
                Enumerable.Range(0, Enum.GetNames(typeof(GalleryType)).Length)
                .Select(i => new GalleryTypeEntity() { GalleryType = (GalleryType)i })
            );
            context.SaveChanges();

            // add query configurations
            context.QueryConfigurations.AddRange(
                new QueryConfiguration() {
                    PageKind = PageKind.SearchPage,
                    SelectedLanguage = context.GalleryLanguages.First(gl => gl.IsAll),
                    SelectedType = context.GalleryTypes.Find(GalleryType.All)
                },
                new QueryConfiguration() {
                    PageKind = PageKind.BrowsePage,
                    SelectedLanguage = context.GalleryLanguages.First(gl => gl.IsAll),
                    SelectedType = context.GalleryTypes.Find(GalleryType.All)
                }
            );

            // add sort directions
            context.SortDirections.AddRange(
                Enumerable.Range(0, Enum.GetNames(typeof(SortDirection)).Length)
                .Select(i => new SortDirectionEntity() { SortDirection = (SortDirection)i })
            );

            // add gallery sorts
            context.SaveChanges();
            context.GallerySorts.AddRange(
                Enumerable.Range(0, Enum.GetNames(typeof(GallerySortProperty)).Length)
                .Select(i => new GallerySortEntity() {
                    GallerySortProperty = (GallerySortProperty)i,
                    SortDirectionEntity = context.SortDirections.First()
                })
            );
            // default DownloadTime sort
            context.GallerySorts.Find(GallerySortProperty.LastDownloadTime).IsActive = true;
            context.GallerySorts.Find(GallerySortProperty.LastDownloadTime).SortDirectionEntity = context.SortDirections.Find(SortDirection.Descending);

            context.SaveChanges();
            ClearInvocationList();
        }

        private static void ClearInvocationList() {
            var invocList = DatabaseInitProgressChanged?.GetInvocationList();
            if (invocList != null) {
                foreach (Delegate d in invocList) {
                    DatabaseInitProgressChanged -= (Action<int>)d;
                }
            }
            invocList = ChangeToIndeterminateEvent?.GetInvocationList();
            if (invocList != null) {
                foreach (Delegate d in invocList) {
                    ChangeToIndeterminateEvent -= (Action)d;
                }
            }
        }

        private static void AddExampleTagFilters(HitomiContext context) {
            IQueryable<Tag> tags = context.Tags;
            context.TagFilters.AddRange(
                new() {
                    Name = "ExampleTagFilterName_1".GetLocalized("ExampleTagFilterNames"),
                    Tags = [
                        Tag.GetTag(tags, "full color", TagCategory.Tag),
                        Tag.GetTag(tags, "very long hair", TagCategory.Female),
                    ]
                },
                new() {
                    Name = "ExampleTagFilterName_2".GetLocalized("ExampleTagFilterNames"),
                    Tags = [
                        Tag.GetTag(tags, "glasses", TagCategory.Female),
                        Tag.GetTag(tags, "sole male", TagCategory.Male),
                    ]
                },
                new() {
                    Name = "ExampleTagFilterName_3".GetLocalized("ExampleTagFilterNames"),
                    Tags = [
                        Tag.GetTag(tags, "naruto", TagCategory.Series),
                        Tag.GetTag(tags, "big breasts", TagCategory.Female),
                    ]
                },
                new() {
                    Name = "ExampleTagFilterName_4".GetLocalized("ExampleTagFilterNames"),
                    Tags = [
                        Tag.GetTag(tags, "non-h imageset", TagCategory.Tag)
                    ]
                }
            );
        }
    }
}
