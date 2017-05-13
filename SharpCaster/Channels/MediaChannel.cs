﻿using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpCaster.Models;
using SharpCaster.Models.Metadata;
using SharpCaster.Models.ChromecastRequests;
using SharpCaster.Models.MediaStatus;
using System.Threading;
using System;
using SharpCaster.Models.ChromecastStatus;
using SharpCaster.Exceptions;

namespace SharpCaster.Channels
{
    using static System.FormattableString;

    internal class MediaChannel : ChromecastChannelWithRequestTracking
    {
        public MediaChannel(ChromeCastClient client)
            : base(client, "urn:x-cast:com.google.cast.media")
        {
        }

        internal override void OnMessageReceived(CastMessage castMessage)
        {
            var json = castMessage.PayloadUtf8;
            var response = JsonConvert.DeserializeObject<MediaStatusResponse>(json);

            if (response.status != null)
            {
                Client.MediaStatus = response.status.FirstOrDefault();
            }
            System.Diagnostics.Debug.WriteLine("Load 00");

            if (response.requestId != 0)
            {
                if (TryRemoveRequestTracking(response.requestId, out var completed))
                {
                    if (response.status == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Load -1");
                        completed.SetException(new MediaLoadException(Client.DeviceUri.ToString(), response.type));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Load +1");
                        completed.SetResult(true);
                    }
                }
            }
        }

        public async Task GetMediaStatus(string transportId, CancellationToken token)
        {
            int requestId = RequestIdProvider.Next;

            var requestCompletedSource = await AddRequestTracking(requestId, token);
            await Write(MessageFactory.MediaStatus(transportId, requestId), token);
            await WaitOnRequestCompletion(requestCompletedSource.Task, token);
        }

        public async Task LoadMedia(
            ChromecastApplication application,
            Uri mediaUrl,
            string contentType /*= "application/vnd.ms-sstr+xml"*/,
            CancellationToken token,
            IMetadata metadata = null,
            string streamType = "BUFFERED",
            double duration = 0D,
            object customData = null,
            Track[] tracks = null,
            int[] activeTrackIds = null,
            bool autoPlay = true,
            double currentTime = 0D)
        {
            int requestId = RequestIdProvider.Next;
            var mediaObject = new MediaData(mediaUrl.ToString(), contentType, metadata, streamType, duration, customData, tracks);
            var req = new LoadRequest(requestId, application.SessionId, mediaObject, autoPlay, currentTime,
                                      customData, activeTrackIds);

            var reqJson = req.ToJson();
            var requestCompletedSource = await AddRequestTracking(requestId, token).ConfigureAwait(false);
            await Write(MessageFactory.Load(application.TransportId, reqJson), token).ConfigureAwait(false);
            await WaitOnRequestCompletion(requestCompletedSource.Task, token).ConfigureAwait(false);
            await requestCompletedSource.Task;
            System.Diagnostics.Debug.WriteLine("Load Finished");
        }
    }
}