using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jamper_Financial.Shared.Services
{
    public class SearchService
    {
        private string _searchQuery = string.Empty;

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                NotifySearchQueryChanged();
            }
        }

        public event Action OnSearchQueryChanged;

        private void NotifySearchQueryChanged() => OnSearchQueryChanged?.Invoke();
    }
}
