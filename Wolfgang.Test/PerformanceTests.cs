using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Wolfgang.Test
{
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [TestFixture]
    public class PerformanceTests
    {
        [Test]
        public void RunBench()
        {
            BenchmarkRunner.Run<PerformanceTests>();
        }

        [Params(1000)] public  int Ticks;
        [Params(250)] public  int MaxPeriod;
        [Params(10000)] public  int TransitionCount;
        [Params(100, 10000)] public int PlaceCount;
        [Params(10, 100)] public  int TokenCount;
        Random rng;

        [GlobalSetup]
        public void Setup()
        {
            rng = new Random(137);
        }

        [Benchmark]
        public async Task CanTickManyAsync(
        )
        {
            INet net = new Net();
            List<IToken> tokenPool = new List<IToken>();
            for (int i = 0; i < TokenCount; i++)
                tokenPool.Add(new BlackToken());

            // add N places
            for (int i = 0; i < PlaceCount; i++)
            {
                net.CreatePlace(out Place somePlace);
                // add random tokens
                for (var j = 0; j < rng.Next(TokenCount); j++)
                {
                    IToken randomToken = tokenPool[rng.Next(tokenPool.Count)];
                    somePlace.AddToken(randomToken, rng.Next(1000));
                }
            }

            var places = net.GetPlaces().ToArray();
            // add N transitions
            for (int i = 0; i < TransitionCount; i++)
            {
                var randomPlace = places[rng.Next(PlaceCount)];
                var randomPlaceTokens = randomPlace.Tokens.Keys.ToList();
                net.CreateTransition(out Transition transition, rng.Next(MaxPeriod));

                // consumers
                for (int j = 0; j < rng.Next(randomPlaceTokens.Count); j++)
                {
                    transition.AddConsumer(randomPlace, randomPlaceTokens[j], 1);
                }

                var anotherRandomPlace = places[rng.Next(PlaceCount)];
                if (anotherRandomPlace == randomPlace)
                    continue;

                // producers
                for (int j = 0; j < rng.Next(TokenCount); j++)
                {
                    transition.AddProducer(anotherRandomPlace, tokenPool[rng.Next(TokenCount)], 1);
                }
            }

            var ct = new CancellationTokenSource().Token;
            for (int i = 0; i < Ticks; i++)
            {
                await net.TickAsync(ct);
            }
        }
    }
}