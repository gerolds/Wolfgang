using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NSubstitute;
using Wolfgang;

namespace Wolfgang.Test
{
    [TestFixture]
    public class SetupTests
    {
        [Test]
        [Timeout(100)]
        public void CanCreateNet()
        {
            INet net = new Net();
            Assert.That(net, Is.Not.Null);
        }

        [Test]
        [Timeout(100)]
        public void CanCreatePlaces()
        {
            INet net = new Net();
            net.CreatePlace(out Place someplace)
               .CreatePlace(out Place someplaceElse);
            Assert.That(net.PlaceCount, Is.EqualTo(2));
            Assert.That(net.Places, Does.Contain(someplace));
            Assert.That(net.Places, Does.Contain(someplaceElse));
            Assert.That(net.GetPlaces(), Does.Contain(someplace));
            Assert.That(net.GetPlaces(), Does.Contain(someplaceElse));
        }

        [Test]
        [Timeout(100)]
        public void CanRemovePlacesSafely()
        {
            INet net = new Net();
            IToken token = new BlackToken();

            net.CreatePlace(out Place somePlace)
               .CreatePlace(out Place somePlaceElse)
               .CreateTransition(out Transition someTransition);

            someTransition
               .AddConsumer(somePlace, token, 1)
               .AddProducer(somePlaceElse, token, 2);

            Assert.That(net.PlaceCount, Is.EqualTo(2));
            Assert.That(net.Places, Does.Contain(somePlace));
            var success = net.TryRemove(somePlace);

            Assert.That(success, Is.True);
            Assert.That(somePlace, Is.Empty);

            // somePlace is empty/disposed now, so we check if there are any now invalid structures left over
            // where previously we would have found references to somePlace.

            Assert.That(net.Contains(somePlace), Is.False);
            Assert.That(net.PlaceCount, Is.EqualTo(1));
            Assert.That(net.Places, Does.Contain(somePlaceElse));
            Assert.That(net.GetPlaces().Count, Is.EqualTo(1));
            Assert.That(net.GetPlaces(), Does.Contain(somePlaceElse));
            Assert.That(
                net.Transitions
                   .Any(
                        transition =>
                            transition.Producers.Any(arc => arc.Place == somePlace) ||
                            transition.Consumers.Any(arc => arc.Place == somePlace)
                    ),
                Is.False
            );
        }

        [Test]
        [Timeout(100)]
        public void CanRemoveTransitionsSafely()
        {
            INet net = new Net();
            IToken token = new BlackToken();

            net.CreatePlace(out Place someplace)
               .CreatePlace(out Place someplaceElse)
               .CreateTransition(out Transition someTransition);

            someTransition
               .AddConsumer(someplace, token, 1)
               .AddProducer(someplaceElse, token, 2);

            var success = net.TryRemove(someTransition);

            Assert.That(success, Is.True);
            Assert.That(net.TransitionCount, Is.EqualTo(0));
            Assert.That(net.Contains(someTransition), Is.False);
            Assert.That(net.Transitions, !Does.Contain(someplace));
            Assert.That(net.GetTransitions(), Does.Not.Contain(someplace));
        }

        [Test]
        [Timeout(100)]
        public void CanAddTokens()
        {
            INet net = new Net();
            IToken token = new BlackToken();

            net.CreatePlace(out Place someplace);
            someplace.AddToken(token, 2);
            Assert.That(someplace.GetTokenCount(token), Is.EqualTo(2));
            Assert.That(someplace[token], Is.Not.Null);
            Assert.That(someplace[token], Is.EqualTo(2));
            Assert.That(someplace.Count(pair => pair.Key == token && pair.Value == 2), Is.EqualTo(1));
        }

        [Test]
        [Timeout(100)]
        public void CanCreateTransitions()
        {
            INet net = new Net();
            net.CreateTransition(out Transition someTransition);
            Assert.That(net.Transitions, Does.Contain(someTransition));
        }

        [Test]
        [Timeout(100)]
        public void CanAddProducers()
        {
            INet net = new Net();
            IToken token = new BlackToken();

            net.CreatePlace(out Place someplace);
            net.CreateTransition(out Transition someTransition);
            someTransition.AddProducer(someplace, token, 2);
            Assert.That(
                someTransition.Producers.Count(
                    arc =>
                        arc.Place == someplace &&
                        arc.Transition == someTransition &&
                        arc.Quantity == 2
                ),
                Is.EqualTo(1)
            );
        }

        [Test]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        [Timeout(100)]
        public void ArcCreationFailsOnNegativeQuantity(int n)
        {
            INet net = new Net();
            IToken token = new BlackToken();

            net.CreatePlace(out Place someplace);
            net.CreateTransition(out Transition someTransition);
            Assert.Throws<ArgumentException>(() => someTransition.AddProducer(someplace, token, -1));
            Assert.Throws<ArgumentException>(() => someTransition.AddConsumer(someplace, token, -2));
        }

        [Test]
        [Timeout(100)]
        public void CanAddConsumers()
        {
            INet net = new Net();
            IToken token = new BlackToken();

            net.CreatePlace(out Place someplace);
            net.CreateTransition(out Transition someTransition);
            someTransition.AddConsumer(someplace, token, 1);

            Assert.That(
                someTransition.Consumers.Count(
                    arc =>
                        arc.Place == someplace &&
                        arc.Transition == someTransition &&
                        arc.Quantity == 1
                ),
                Is.EqualTo(1)
            );
        }

        [Test]
        [Timeout(100)]
        public async Task CanTickAsync()
        {
            INet net = new Net();
            IToken token = new BlackToken();

            net.CreatePlace(out Place somePlace)
               .CreatePlace(out Place somePlaceElse);

            somePlace
               .AddToken(token, 2)
                ;

            net
               .CreateTransition(out Transition someTransition);

            someTransition
               .AddConsumer(somePlace, token, 2, "")
               .AddProducer(somePlaceElse, token, 3, "")
                ;
            var ct = new CancellationTokenSource().Token;

            //net.Tick();
            await net.TickAsync(ct);
            Assert.That(somePlaceElse.GetTokenCount(token), Is.EqualTo(3));
        }
        
    }
}