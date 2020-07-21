using System;

namespace Wolfgang
{
    public class BlackToken : IToken
    {
        public bool HasAspect(Type t)
        {
            throw new NotImplementedException();
        }

        public int Volume { get; }
    }
}