using System;

namespace Wolfgang
{
    public interface IToken
    {
        bool HasAspect(Type t);
        int Volume { get; }
    }
}