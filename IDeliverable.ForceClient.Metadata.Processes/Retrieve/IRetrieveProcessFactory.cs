using IDeliverable.ForceClient.Core.OrgAccess;

namespace IDeliverable.ForceClient.Metadata.Processes.Retrieve
{
	public interface IRetrieveProcessFactory
	{
		IRetrieveProcess CreateRetrieveProcess(IOrgAccessProvider orgAccessProvider);
	}
}
