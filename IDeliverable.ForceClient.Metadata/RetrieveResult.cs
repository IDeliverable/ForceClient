﻿namespace IDeliverable.ForceClient.Metadata
{
    public class RetrieveResult
    {
        public RetrieveResult(RetrieveStatus status, byte[] zipFile)
        {
            Status = status;
            ZipFile = zipFile;
        }

        public RetrieveStatus Status { get; }

        public bool IsDone => Status == RetrieveStatus.Failed || Status == RetrieveStatus.Succeeded;

        public byte[] ZipFile { get; }

        public MetadataArchive ToMetadataArchive()
        {
            return MetadataArchive.FromZipFile(ZipFile);
        }
    }
}
