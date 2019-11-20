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
        private Action? _callback;

        public void Reset()
        {
            if (!_ready && !_finished && _callback == null) {
                return;
            }
            
            // Can only be reset when finished.
            if (!_ready || !_finished) {
                throw new InvalidOperationException("Cannot reset incomplete yield token.");
            }

            _ready = false;
            _finished = false;
            _callback = null;
        }
        
        public void OnFinished(Action handler)
        {
            if (_ready) {
                throw new InvalidOperationException("Cannot add finished handlers to YieldToken after it has been started.");
            }
            
            if (_callback != null) {
                // TODO: What are the performance characteristics of multicast delegates?
                // Is this a simple list? A hash set?
                handler = _callback + handler;
            }
            _callback = handler;
        }

        public void Start()
        {
            if (_ready) {
                throw new InvalidOperationException("Cannot start YieldToken after it has been started.");
            }
            
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
            if (_ready && _callback != null) {
                _callback();
            }
        }
        
    }
}