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
            var now = DateTime.UtcNow;

            await foreach (var tweet in GetTweetDataAsync(context.CancellationToken, now))
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Sending TweetData response");
                await responseStream.WriteAsync(tweet);
            }
        }

        private async IAsyncEnumerable<TweetData> GetTweetDataAsync([EnumeratorCancellation] CancellationToken cancellationToken, DateTime now)
        {
            int days = 0;
            int max = 500;
            int id = 1;

            while (id < max)
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return new TweetData
                {
                    DateTimeStamp = Timestamp.FromDateTime(now.AddDays(days++)),
                    Id = id++,
                    Message = "test"
                };
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
