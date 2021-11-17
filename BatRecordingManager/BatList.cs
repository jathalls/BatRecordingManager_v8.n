using Microsoft.VisualStudio.Language.Intellisense;
using System.Collections.Generic;

namespace BatRecordingManager
{
    /// <summary>
    /// class to contain a pair of lists of Bat, one for manually identified bats
    /// and the second for automatically identified bats
    /// </summary>
    public class BatList
    {
        public BatList()
        {
        }

        public List<Bat> autoBats { get; set; } = new List<Bat>();
        public List<Bat> bats { get; set; } = new List<Bat>();

        public BulkObservableCollection<Bat> GetBatList(bool byAutoID)
        {
            var result = new BulkObservableCollection<Bat>();
            if (byAutoID)
            {
                result.AddRange(autoBats ?? new List<Bat>());
            }
            else
            {
                result.AddRange(bats ?? new List<Bat>());
            }

            return (result);
        }
    }
}