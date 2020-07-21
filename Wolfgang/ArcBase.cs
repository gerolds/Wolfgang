using System;

namespace Wolfgang
{
    public abstract class ArcBase : IArc
    {
        private readonly INet _net;
        private readonly IToken _token;
        private readonly int _quantity;

        public IPlace Place { get; }

        public ITransition Transition { get; }

        public IToken Token => _token;

        public bool TryGetProduceRequest(out Place.ITokenRequest request)
            => Place.TryCreateTokenChangeRequest(in _token, Quantity, out request);

        public bool TryGetConsumeRequest(out Place.ITokenRequest request)
            => Place.TryCreateTokenChangeRequest(in _token, -Quantity, out request);

        public int Quantity => _quantity;

        public INet Net => _net;

        internal ArcBase(
            in INet net,
            in IToken token,
            in IPlace place,
            in ITransition transition,
            int quantity,
            string description
        )
        {
            if (quantity < 0)
                throw new ArgumentException($"Arcs must be constructed with a positive quantity.", nameof(quantity));

            _net = net;
            Place = place;
            Transition = transition;
            _token = token;
            _quantity = quantity;
        }
    }
}