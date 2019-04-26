using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nito.AsyncEx;

namespace IDeliverable.ForceClient.Core.Tokens
{
    public class IsolatedStorageTokenStore : ITokenStore
    {
        public IsolatedStorageTokenStore()
        {
            mStore = IsolatedStorageFile.GetUserStoreForApplication();
            mFileLocks = new ConcurrentDictionary<string, AsyncLock>();
        }

        private readonly IsolatedStorageFile mStore;
        private readonly ConcurrentDictionary<string, AsyncLock> mFileLocks;

        public async Task<string> LoadTokenAsync(TokenKind kind, OrgType orgType, string username)
        {
            var fileName = GetTokenFileName(kind, orgType, username);

            if (!mStore.FileExists(fileName))
                return null;

            string tokenInfoJson;
            using (var stream = mStore.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    tokenInfoJson = await reader.ReadToEndAsync();
                }
            }

            var tokenInfo = JsonConvert.DeserializeObject<TokenInfo>(tokenInfoJson);

            if (tokenInfo.ExpiresAtUtc.HasValue && tokenInfo.ExpiresAtUtc.Value < DateTime.UtcNow)
            {
                await DeleteTokenAsync(kind, orgType, username);
                return null;
            }

            return tokenInfo.Token;
        }

        public async Task SaveTokenAsync(TokenKind kind, OrgType orgType, string username, string token, DateTime? expiresAtUtc)
        {
            var fileName = GetTokenFileName(kind, orgType, username);

            if (expiresAtUtc.HasValue && expiresAtUtc.Value < DateTime.UtcNow)
            {
                await DeleteTokenAsync(kind, orgType, username);
                return;
            }

            var tokenInfo = new TokenInfo(token, expiresAtUtc);
            var tokenInfoJson = JsonConvert.SerializeObject(tokenInfo);

            var fileLock = mFileLocks.GetOrAdd(fileName, new AsyncLock());
            using (await fileLock.LockAsync())
            {
                using (var stream = mStore.OpenFile(fileName, FileMode.Create))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        await writer.WriteAsync(tokenInfoJson);
                    }
                }
            }
        }

        public async Task DeleteTokenAsync(TokenKind kind, OrgType orgType, string username)
        {
            var fileName = GetTokenFileName(kind, orgType, username);

            var fileLock = mFileLocks.GetOrAdd(fileName, new AsyncLock());
            using (await fileLock.LockAsync())
            {
                mStore.DeleteFile(fileName);
            }
        }

        public async Task<IReadOnlyDictionary<string, string>> LoadUrlsAsync(OrgType orgType, string username)
        {
            var fileName = GetUrlsFileName(orgType, username);

            if (!mStore.FileExists(fileName))
                return null;

            string urlsJson;
            using (var stream = mStore.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    urlsJson = await reader.ReadToEndAsync();
                }
            }

            var urls = JsonConvert.DeserializeObject<Dictionary<string, string>>(urlsJson);

            return urls;
        }

        public async Task SaveUrlsAsync(OrgType orgType, string username, IReadOnlyDictionary<string, string> urls)
        {
            var fileName = GetUrlsFileName(orgType, username);

            var urlsJson = JsonConvert.SerializeObject(urls);

            var fileLock = mFileLocks.GetOrAdd(fileName, new AsyncLock());
            using (await fileLock.LockAsync())
            {
                using (var stream = mStore.OpenFile(fileName, FileMode.Create))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        await writer.WriteAsync(urlsJson);
                    }
                }
            }
        }

        private string GetTokenFileName(TokenKind kind, OrgType orgType, string username)
        {
            return String.Intern($"{kind}_{orgType}_{username}.json");
        }

        private string GetUrlsFileName(OrgType orgType, string username)
        {
            return String.Intern($"Urls_{orgType}_{username}.json");
        }

        private class TokenInfo
        {
            public TokenInfo(string token, DateTime? expiresAtUtc)
            {
                Token = token;
                ExpiresAtUtc = expiresAtUtc;
            }

            public string Token { get; }
            public DateTime? ExpiresAtUtc { get; }
        }
    }
}
