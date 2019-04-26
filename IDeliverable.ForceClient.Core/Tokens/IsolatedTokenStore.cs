using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Core.OrgAccess;
using Newtonsoft.Json;
using Nito.AsyncEx;

namespace IDeliverable.ForceClient.Core.Tokens
{
    public class IsolatedTokenStore : ITokenStore
    {
        public IsolatedTokenStore()
        {
            mStore = IsolatedStorageFile.GetUserStoreForApplication();
            mFileLocks = new ConcurrentDictionary<string, AsyncReaderWriterLock>();
        }

        private readonly IsolatedStorageFile mStore;
        private readonly ConcurrentDictionary<string, AsyncReaderWriterLock> mFileLocks;

        public async Task<string> LoadTokenAsync(TokenKind kind, OrgType orgType, string username)
        {
            var fileName = GetTokenFileName(kind, orgType, username);

            var fileLock = mFileLocks.GetOrAdd(fileName, new AsyncReaderWriterLock());
            using (await fileLock.ReaderLockAsync())
            {
                if (!mStore.FileExists(fileName))
                    return null;

                using (var stream = mStore.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var token = await reader.ReadToEndAsync();
                        return token;
                    }
                }
            }
        }

        public async Task SaveTokenAsync(TokenKind kind, OrgType orgType, string username, string token)
        {
            var fileName = GetTokenFileName(kind, orgType, username);

            var fileLock = mFileLocks.GetOrAdd(fileName, new AsyncReaderWriterLock());
            using (await fileLock.WriterLockAsync())
            {
                using (var stream = mStore.OpenFile(fileName, FileMode.Create))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        await writer.WriteAsync(token);
                    }
                }
            }
        }

        public async Task DeleteTokenAsync(TokenKind kind, OrgType orgType, string username)
        {
            var fileName = GetTokenFileName(kind, orgType, username);

            var fileLock = mFileLocks.GetOrAdd(fileName, new AsyncReaderWriterLock());
            using (await fileLock.WriterLockAsync())
            {
                mStore.DeleteFile(fileName);
            }
        }

        public async Task<IReadOnlyDictionary<string, string>> LoadUrlsAsync(OrgType orgType, string username)
        {
            var fileName = GetUrlsFileName(orgType, username);

            string urlsJson;
            var fileLock = mFileLocks.GetOrAdd(fileName, new AsyncReaderWriterLock());
            using (await fileLock.ReaderLockAsync())
            {
                if (!mStore.FileExists(fileName))
                    return null;

                using (var stream = mStore.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        urlsJson = await reader.ReadToEndAsync();
                    }
                }
            }

            var urls = JsonConvert.DeserializeObject<Dictionary<string, string>>(urlsJson);

            return urls;
        }

        public async Task SaveUrlsAsync(OrgType orgType, string username, IReadOnlyDictionary<string, string> urls)
        {
            var fileName = GetUrlsFileName(orgType, username);

            var urlsJson = JsonConvert.SerializeObject(urls);

            var fileLock = mFileLocks.GetOrAdd(fileName, new AsyncReaderWriterLock());
            using (await fileLock.WriterLockAsync())
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
    }
}
