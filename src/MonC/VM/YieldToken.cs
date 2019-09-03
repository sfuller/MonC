using System;

namespace MonC.VM
{
    /// <summary>
    /// NOTE: This implementation of YieldToken is not thread safe.
    /// </summary>
    public class YieldToken : IYieldToken
    {
        private bool _ready;
        private bool _finished;
        private Action _callback;
        
        public void OnFinished(Action handler)
        {
            if (_finished && _ready) {
                handler();
                return;
            }
            
            if (_callback != null) {
                handler = (Action)Action.Combine(_callback, handler);
            }
            _callback = handler;
        }

        public void Start()
        {
            _ready = true;
            if (_finished && _callback != null) {
                _callback();
            }
        }

        public void Finish()
        {
            if (_finished) {
                return;
            }

            _finished = true;
            if (_callback != null) {
                _callback();
            }
        }
        
    }
}