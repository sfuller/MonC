using System;

namespace MonC.VM
{
    public interface IYieldToken
    {
        void OnFinished(Action handler);
        void Start();
    }
}