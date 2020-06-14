using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tweetinvi;

namespace TweetService
{
    public class TweeterService : Tweeter.TweeterBase, IDisposable
    {
        private readonly ILogger<TweeterService> _logger;
        private readonly ITwitterClient _twitterClient;
        private readonly BlockingCollection<TweetData> _tweets = new BlockingCollection<TweetData>();

        public TweeterService(ILogger<TweeterService> logger, ITwitterClient twitterClient)
        {
            _logger = logger;
            _twitterClient = twitterClient;
        }

        public override async Task GetTweetStream(Empty _, IServerStreamWriter<TweetData> responseStream, ServerCallContext context)
        {
            var producer = ProduceTweets();
            var consumer = ConsumeTweets(responseStream);

            await Task.WhenAll(producer, consumer);
        }

        private Task ProduceTweets()
        {
            return Task.Run(async () =>
            {
                int i = 0;

                var sampleStream = _twitterClient.Streams.CreateSampleStream();
                sampleStream.TweetReceived += (sender, eventArgs) =>
                {
                    var tweet = new TweetData { Id = i, Message = eventArgs.Tweet.Text };
                    _tweets.Add(tweet);

                    if (++i == 20)
                    {
                        _tweets.CompleteAdding();
                        sampleStream.Stop();
                    }
                };

                await sampleStream.StartAsync().ConfigureAwait(false);
            });
        }

        private Task ConsumeTweets(IServerStreamWriter<TweetData> responseStream)
        {
            return Task.Run(async () =>
            {
                try
                {
                    await foreach (var tweet in GetTweetAsync().ConfigureAwait(false))
                    {
                        await responseStream.WriteAsync(tweet).ConfigureAwait(false);
                    }
                }
                catch (InvalidOperationException)
                {
                    // An InvalidOperationException means that Take() was called on a completed collection
                    Console.WriteLine("No more tweets!");
                }
            });
        }

        private async IAsyncEnumerable<TweetData> GetTweetAsync()
        {
            await Task.CompletedTask.ConfigureAwait(false);

            while (true)
                yield return _tweets.Take();
        }


        #region IDisposable

        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _tweets.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TweeterService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable
    }
}