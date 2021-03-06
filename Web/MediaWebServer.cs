﻿using NullGuard;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using static System.FormattableString;

namespace Hspi.Web
{
    [NullGuard(ValidationFlags.Arguments | ValidationFlags.NonPublic)]
    internal sealed class MediaWebServer : IDisposable
    {
        public MediaWebServer(IPAddress ipAddress, ushort port)
        {
            UrlPrefix = Invariant($"http://{ipAddress}:{port}/");
            server = new WebServer(UrlPrefix, RoutingStrategy.Wildcard);
            server.RegisterModule(inMemoryFileSystem);
        }

        public async Task StartListening(CancellationToken token)
        {
            await server.RunAsync(token).ConfigureAwait(false);
        }

        public void Add(byte[] buffer, DateTimeOffset lastModified, string path, DateTimeOffset expiry)
        {
            inMemoryFileSystem.AddCacheFile(buffer, lastModified, path, expiry);
        }

        public string UrlPrefix { get; }
        public bool IsListening => server.Listener.IsListening;

        private readonly WebServer server;
        private readonly InMemoryFileSystemModule inMemoryFileSystem = new InMemoryFileSystemModule();

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        public void Dispose()
        {
            if (!disposedValue)
            {
                if (server != null)
                {
                    server.Dispose();
                }

                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}