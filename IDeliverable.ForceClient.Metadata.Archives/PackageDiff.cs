using System.Collections.Generic;

namespace IDeliverable.ForceClient.Metadata.Archives
{
    public class PackageDiff
    {
        public PackageDiff(IEnumerable<Component> added, IEnumerable<Component> modified, IEnumerable<(string type, string name)> deleted)
        {
            Added = added;
            Modified = modified;
            Deleted = deleted;
        }

        public IEnumerable<Component> Added { get; }
        public IEnumerable<Component> Modified { get; }
        public IEnumerable<(string type, string name)> Deleted { get; }
    }
}
