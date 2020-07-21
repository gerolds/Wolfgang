using System;

namespace Wolfgang
{
    public delegate bool Consumer(in IPlace place);

    public delegate IToken[] Producer(in ITransition place);

    public interface IArc
    {
        IPlace Place { get; }

        ITransition Transition { get; }

        bool TryGetProduceRequest(out Place.ITokenRequest request);

        bool TryGetConsumeRequest(out Place.ITokenRequest request);

        int Quantity { get; }

        INet Net { get; }
    }
}