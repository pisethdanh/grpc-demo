using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using TweetService;

namespace console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // The port number(5001) must match the port of the gRPC server.
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");

            var client = new Tweeter.TweeterClient(channel);
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            using var streamingCall = client.GetTweetStream(new Empty(), cancellationToken: cts.Token);

            try
            {
                await foreach (var tweet in streamingCall.ResponseStream.ReadAllAsync(cancellationToken: cts.Token).ConfigureAwait(false))
                {
                    Console.WriteLine($"{tweet.DateTimeStamp.ToDateTime():s} | {tweet.Id} | {tweet.Message}");
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                Console.WriteLine("Stream cancelled.");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            cts.Cancel();
        }
    }
}
