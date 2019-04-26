using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IDeliverable.ForceClient.Core
{
    public interface ITokenStore
    {
        Task<string> LoadTokenAsync(TokenKind kind, OrgType orgType, string username);
        Task SaveTokenAsync(TokenKind kind, OrgType orgType, string username, string token, DateTime? expiresAtUtc);
        Task DeleteTokenAsync(TokenKind kind, OrgType orgType, string username);
        Task<IReadOnlyDictionary<string, string>> LoadUrlsAsync(OrgType orgType, string username);
        Task SaveUrlsAsync(OrgType orgType, string username, IReadOnlyDictionary<string, string> urls);
    }
}
