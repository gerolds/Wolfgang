namespace Wolfgang
{
    internal class Arc : ArcBase
    {
        internal Arc(
            in INet net,
            in IPlace place,
            in ITransition transition,
            in IToken token,
            int quantity,
            string description
        ) : base(in net, in token, in place, in transition, quantity, description)
        {
        }
    }
}