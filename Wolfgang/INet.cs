using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Wolfgang
{
    public interface INet
    {
        [NotNull] IDictionary<IToken, int> GetAggregateTokenCounts();

        /// <summary>
        /// Moment-in-time snapshot of all producers (lock-free aggregation)
        /// </summary>
        [NotNull] ICollection<IArc> GetProducers();

        /// <summary>
        /// Moment-in-time snapshot of all consumers (lock-free aggregation)
        /// </summary>
        [NotNull] ICollection<IArc> GetConsumers();

        /// <summary>
        /// Moment-in-time snapshot of all places (lock-free aggregation)
        /// </summary>
        public ICollection<IPlace> GetPlaces();

        /// <summary>
        /// Moment-in-time snapshot of all transitions (lock-free aggregation)
        /// </summary>
        public ICollection<ITransition> GetTransitions();

        public int PlaceCount { get; }

        public int TransitionCount { get; }

        /// <summary>
        /// Concurrent (lock-free) enumerable of all places. May contain changes made after the internal call to GetEnumerator().
        /// </summary>
        IEnumerable<IPlace> Places { [NotNull] get; }

        /// <summary>
        /// Concurrent (lock-free) enumerable of all transitions. May contain changes made after the internal call to GetEnumerator().
        /// </summary>
        IEnumerable<ITransition> Transitions { [NotNull] get; }

        bool Contains([NotNull] IPlace place);

        bool Contains([NotNull] ITransition transition);

        INet CreatePlace<TPlace>([NotNull] out TPlace place, [CanBeNull] string description = null)
            where TPlace : IPlace, new();

        INet CreateTransition<TTransition>([NotNull] out TTransition transition, int period = 0, [CanBeNull] string description = null)
            where TTransition : ITransition, new();

        bool TryRemove([NotNull] IPlace place);

        bool TryRemove([NotNull] ITransition transition);

        bool CanRemove([NotNull] IPlace place);

        public Task TickAsync(CancellationToken cancellationToken);
        public void Tick();
    }
}