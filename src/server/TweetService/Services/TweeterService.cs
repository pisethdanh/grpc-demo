using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TweetService
{
    public class TweeterService : Tweeter.TweeterBase
    {
        private readonly ILogger<TweeterService> _logger;
        public TweeterService(ILogger<TweeterService> logger)
        {
            _logger = logger;
        }

        public override async Task GetTweetStream(Empty _, IServerStreamWriter<TweetData> responseStream, ServerCallContext context)
        {
            await foreach (var tweet in GetTweetDataAsync(context.CancellationToken).ConfigureAwait(false))
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Sending TweetData response");
                await responseStream.WriteAsync(tweet).ConfigureAwait(false);
            }
        }

        private async IAsyncEnumerable<TweetData> GetTweetDataAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            int max = 500;
            int id = 1;

            var now = DateTime.UtcNow;

            while (id < max)
            {
                cancellationToken.ThrowIfCancellationRequested();

                id++;
                yield return new TweetData
                {
                    DateTimeStamp = Timestamp.FromDateTime(now.AddDays(id)),
                    Id = id,
                    Message = "test"
                };
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
