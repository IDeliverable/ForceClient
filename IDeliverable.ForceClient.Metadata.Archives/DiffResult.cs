using System.Collections.Generic;

namespace IDeliverable.ForceClient.Metadata.Archives
{
    public class DiffResult<T>
    {
        public DiffResult(IEnumerable<T> added, IEnumerable<T> modified, IEnumerable<T> deleted)
        {
            Added = added;
            Modified = modified;
            Deleted = deleted;
        }

        public IEnumerable<T> Added { get; }
        public IEnumerable<T> Modified { get; }
        public IEnumerable<T> Deleted { get; }
    }
}
