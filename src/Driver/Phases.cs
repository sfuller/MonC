using System.Collections;
using System.Collections.Generic;

namespace Driver
{
    public enum Phase
    {
        Null,
        Lex,
        Parse,
        CodeGen,
        Backend,
        Link,
        VM,
        MaxPhase
    }

    public readonly struct PhaseSet : IEnumerable<Phase>
    {
        private readonly int _phaseMask;
        public int NumPhases { get; }
        public Phase FirstPhase { get; }
        public Phase LastPhase { get; }

        private static int BuildPhaseMask(Phase[] phases)
        {
            int phaseMask = 0;
            foreach (Phase phase in phases) {
                phaseMask |= 1 << (int) phase;
            }

            return phaseMask;
        }

        private PhaseSet(int phaseMask)
        {
            _phaseMask = phaseMask & (1 << (int) Phase.MaxPhase) - 2;

            NumPhases = 0;
            FirstPhase = Phase.Null;
            LastPhase = Phase.Null;
            for (Phase i = Phase.Null, iend = Phase.MaxPhase; i < iend; ++i) {
                if ((_phaseMask & (1 << (int) i)) != 0) {
                    NumPhases += 1;
                    if (FirstPhase == Phase.Null)
                        FirstPhase = i;
                    LastPhase = i;
                }
            }
        }

        public PhaseSet(params Phase[] phases) : this(BuildPhaseMask(phases))
        {
        }

        public static PhaseSet NoPhases => new PhaseSet(0);
        public static PhaseSet AllPhases => new PhaseSet(~0);
        public static PhaseSet AllPhasesTo(Phase lastPhase) => new PhaseSet((1 << (int) (lastPhase + 1)) - 2);
        
        public static PhaseSet operator |(PhaseSet a, PhaseSet b) => new PhaseSet(a._phaseMask | b._phaseMask);
        public static PhaseSet operator &(PhaseSet a, PhaseSet b) => new PhaseSet(a._phaseMask & b._phaseMask);
        public static PhaseSet operator ~(PhaseSet a) => new PhaseSet(~a._phaseMask);

        private struct Enumerator : IEnumerator<Phase>
        {
            private int _phaseMask;
            public Phase Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(int phaseMask)
            {
                _phaseMask = phaseMask;
                Current = Phase.Null;
            }

            public bool MoveNext()
            {
                for (Phase i = Current + 1, iend = Phase.MaxPhase; i < iend; ++i) {
                    if ((_phaseMask & (1 << (int) i)) != 0) {
                        Current = i;
                        return true;
                    }
                }

                return false;
            }

            public void Reset() => Current = Phase.Null;

            public void Dispose()
            {
            }
        }

        public IEnumerator<Phase> GetEnumerator() => new Enumerator(_phaseMask);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}