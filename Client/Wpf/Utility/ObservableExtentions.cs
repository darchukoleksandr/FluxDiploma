using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Client.Wpf.Utility
{
    internal static class ObservableExtentions
    {
        public static void AddRange<T>(this ObservableCollection<T> source, IEnumerable<T> range)
        {
            foreach (var item in range)
            {
                source.Add(item);
            }
        }
    }
}
