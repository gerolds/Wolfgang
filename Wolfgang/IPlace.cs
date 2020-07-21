using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Wolfgang
{
    public interface IPlace : IDisposable
    {
        IReadOnlyDictionary<IToken, int> Tokens { [NotNull] get; }

        //TODO: this should be an internal call by ITokenRequest to keep updates thread-safe
        //bool TryUpdate([NotNull] in IToken token, int delta);
        
        int GetTokenCount([NotNull] in IToken token);

        bool TryCreateTokenChangeRequest(in IToken token, int delta, [CanBeNull] out Place.ITokenRequest request);

        bool IsEmpty { get; }

        INet Net { [NotNull] get; [NotNull] set; }

        string Description { [CanBeNull] get; set; }
    }
}