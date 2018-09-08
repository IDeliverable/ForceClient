namespace IDeliverable.ForceClient.Metadata.Deploy
{
    public class DeployResult
    {
        public DeployResult(DeployStatus status, string state, int numberComponentsTotal, int numberComponentsDeployed, int numberComponentsFailed, int numberTestsTotal, int numberTestsCompleted, int numberTestsFailed)
        {
            Status = status;
            State = state;
            NumberComponentsTotal = numberComponentsTotal;
            NumberComponentsDeployed = numberComponentsDeployed;
            NumberComponentsFailed = numberComponentsFailed;
            NumberTestsTotal = numberTestsTotal;
            NumberTestsCompleted = numberTestsCompleted;
            NumberTestsFailed = numberTestsFailed;
        }

        public DeployStatus Status { get; }

        public string State { get; }

        public bool IsDone => Status == DeployStatus.Failed || Status == DeployStatus.Succeeded || Status == DeployStatus.SucceededPartial || Status == DeployStatus.Canceled;

        public int NumberComponentsTotal { get; }

        public int NumberComponentsDeployed { get; }

        public int NumberComponentsFailed { get; }

        public int NumberTestsTotal { get; }

        public int NumberTestsCompleted { get; }

        public int NumberTestsFailed { get; }
    }
}
