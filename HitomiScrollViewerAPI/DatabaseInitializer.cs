using ConsoleUtilities;
using HitomiScrollViewerData;
using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerAPI {
    public static class DatabaseInitializer {
        private const string INITIALIZED_FLAG_FILE_PATH = "db-initialized.txt";
        private static readonly string[] ALPHABETS_WITH_123 =
            ["123", .. Enumerable.Range('a', 26).Select(intValue => Convert.ToChar(intValue).ToString())];

        private const string DB_RES_ROOT_DIR = "DatabaseResources";
        private static readonly string DELIMITER_FILE_PATH = Path.Combine(
            DB_RES_ROOT_DIR,
            "delimiter.txt"
        );
        private static readonly string LANGUAGES_FILE_PATH = Path.Combine(
            DB_RES_ROOT_DIR,
            "languages.txt"
        );
        private static readonly string TYPES_FILE_PATH = Path.Combine(
            DB_RES_ROOT_DIR,
            "types.txt"
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

        public static event Action<InitStatus, InitProgress?>? StatusChanged;
        public static bool IsInitialized { get; private set; } = false;
        public static void Start() {
            using HitomiContext context = new();
            bool initializedFlagFileExists = File.Exists(INITIALIZED_FLAG_FILE_PATH);
            if (initializedFlagFileExists) {
                bool isInitialized = bool.Parse(File.ReadAllText(INITIALIZED_FLAG_FILE_PATH));
                if (isInitialized) {
                    IsInitialized = true;
                    StatusChanged?.Invoke(InitStatus.Complete, null);
                    return;
                } else {
                    context.Database.EnsureDeleted();
                }
            } else {
                File.WriteAllText(INITIALIZED_FLAG_FILE_PATH, false.ToString());
            }
            context.Database.EnsureCreated();
            AddDefaultDataAsync(context);
            AddExampleTagFilters(context);
            IsInitialized = true;
            StatusChanged?.Invoke(InitStatus.Complete, null);
            StatusChanged = null; // clear event handlers
        }

        private static void AddDefaultDataAsync(HitomiContext context) {
            StatusChanged?.Invoke(InitStatus.InProgress, InitProgress.AddingTags);
            Console.WriteLine("Adding default data to the database...");
            string delimiter = File.ReadAllText(DELIMITER_FILE_PATH);
            foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                Console.Write($"Adding {category} tags... ");
                using var pb = new ProgressBar();
                int progress = 0;
                string categoryStr = CATEGORY_DIR_DICT[category];
                string dir = Path.Combine(
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
                    pb.Report((double)++progress / ALPHABETS_WITH_123.Length);
                }
                Console.WriteLine("Complete.");
            }

            // add gallery languages and its local names
            StatusChanged?.Invoke(InitStatus.InProgress, InitProgress.AddingGalleryLanguagesAndTypes);
            Console.Write("Adding gallery languages and types... ");
            string[][] languages = File.ReadAllLines(LANGUAGES_FILE_PATH).Select(pair => pair.Split(delimiter)).ToArray();
            context.GalleryLanguages.Add(new GalleryLanguage() {
                IsAll = true,
                EnglishName = "All",
                LocalName = null
            });
            context.GalleryLanguages.AddRange(languages.Select(
                pair => {
                    return new GalleryLanguage() {
                        IsAll = false,
                        EnglishName = pair[0],
                        LocalName = pair[1]
                    };
                }
            ));
            // add gallery types
            string[] types = [.. File.ReadAllLines(TYPES_FILE_PATH)];
            context.GalleryTypes.Add(new GalleryType() {
                IsAll = true,
                Value = null
            });
            context.GalleryTypes.AddRange(types.Select(t => new GalleryType() { IsAll = false, Value = t }));
            context.SaveChanges();
            Console.WriteLine("Complete.");


            // add query configurations
            Console.Write("Adding query configurations... ");
            StatusChanged?.Invoke(InitStatus.InProgress, InitProgress.AddingQueryConfigurations);
            context.QueryConfigurations.AddRange(
                new QueryConfiguration() {
                    PageKind = PageKind.SearchPage,
                    GalleryLanguage = context.GalleryLanguages.First(gl => gl.IsAll),
                    GalleryType = context.GalleryTypes.First(gt => gt.IsAll)
                },
                new QueryConfiguration() {
                    PageKind = PageKind.SearchPage,
                    GalleryLanguage = context.GalleryLanguages.First(gl => gl.IsAll),
                    GalleryType = context.GalleryTypes.First(gt => gt.IsAll)
                }
            );
            context.SaveChanges();
            Console.WriteLine("Complete.");

            // add gallery sorts
            Console.Write("Adding gallery sorts... ");
            StatusChanged?.Invoke(InitStatus.InProgress, InitProgress.AddingGallerySorts);
            GallerySort[] sorts =
                [.. Enumerable.Range(0, Enum.GetNames<GalleryProperty>().Length)
                    .Select(i => new GallerySort() {
                        Property = (GalleryProperty)i,
                        SortDirection = SortDirection.Ascending,
                        IsActive = false
                    })
                ];
            // default GallerySort as LastDownloadTime Descending
            GallerySort lastDownloadTimeSort = sorts.First(s => s.Property == GalleryProperty.LastDownloadTime);
            lastDownloadTimeSort.IsActive = true;
            lastDownloadTimeSort.SortDirection = SortDirection.Descending;
            context.GallerySorts.AddRange(sorts);
            context.SaveChanges();
            Console.WriteLine("Complete.");
        }

        private static void AddExampleTagFilters(HitomiContext context) {
            StatusChanged?.Invoke(InitStatus.InProgress, InitProgress.AddingExampleTagFilters);
            Console.Write("Adding example tag filters... ");
            IQueryable<Tag> tags = context.Tags;
            context.TagFilters.AddRange(
                new() {
                    Name = Resources.ExampleTagFilterNames.ExampleTagFilterName_1,
                    Tags = [
                        Utils.GetTag(tags, "full color", TagCategory.Tag),
                        Utils.GetTag(tags, "very long hair", TagCategory.Female),
                    ]
                },
                new() {
                    Name = Resources.ExampleTagFilterNames.ExampleTagFilterName_2,
                    Tags = [
                        Utils.GetTag(tags, "glasses", TagCategory.Female),
                        Utils.GetTag(tags, "sole male", TagCategory.Male),
                    ]
                },
                new() {
                    Name = Resources.ExampleTagFilterNames.ExampleTagFilterName_3,
                    Tags = [
                        Utils.GetTag(tags, "naruto", TagCategory.Series),
                        Utils.GetTag(tags, "big breasts", TagCategory.Female),
                    ]
                },
                new() {
                    Name = Resources.ExampleTagFilterNames.ExampleTagFilterName_4,
                    Tags = [
                        Utils.GetTag(tags, "non-h imageset", TagCategory.Tag)
                    ]
                }
            );
            context.SaveChanges();
            Console.WriteLine("Complete.");
        }
    }
}
