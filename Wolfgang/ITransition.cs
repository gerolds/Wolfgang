using System.Collections.Generic;
using JetBrains.Annotations;

namespace Wolfgang
{
    public interface ITransition
    {
        IReadOnlyCollection<IArc> Consumers { [NotNull] get; }

        IReadOnlyCollection<IArc> Producers { [NotNull] get; }

        INet Net { [NotNull] get; [NotNull] set; }
        int Period { get; set; }

        ITransition AddConsumer(
            [NotNull] in IPlace place,
            [NotNull] in IToken token,
            int quantity,
            string description = null
        );

        ITransition AddProducer(
            [NotNull] in IPlace place,
            [NotNull] in IToken token,
            int quantity,
            string description = null
        );

        void RemoveAllArcs([NotNull] in IPlace place);

        string Description { [CanBeNull] get; set; }

        public void Tick();
    }
}