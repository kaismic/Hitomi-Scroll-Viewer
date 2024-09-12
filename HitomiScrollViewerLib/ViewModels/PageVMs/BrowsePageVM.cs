using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public class BrowsePageVM {
        private static BrowsePageVM _main;
        public static BrowsePageVM Main => _main ??= new();

        private readonly int[] _paginationNums = Enumerable.Range(1, 15).ToArray();

    }
}
