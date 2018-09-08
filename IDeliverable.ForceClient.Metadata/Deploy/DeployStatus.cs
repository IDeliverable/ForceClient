namespace IDeliverable.ForceClient.Metadata.Deploy
{
    public enum DeployStatus
    {
        Pending,
        InProgress,
        Succeeded,
        SucceededPartial,
        Failed,
        Canceling,
        Canceled,
    }
}
