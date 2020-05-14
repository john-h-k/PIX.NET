using System;
using System.Diagnostics;
using TerraFX.Interop;

namespace PIX.NET
{
    public readonly unsafe partial struct ScopedEvent
    {
        private readonly void* _context;
        private readonly ContextType _type;

        private static readonly CtorDummy NoContext = default;

        private ScopedEvent(CtorDummy dummy)
        {
            _context = null;
            _type = ContextType.None;
        }

        private struct CtorDummy
        {
        }

        private ScopedEvent(ID3D12CommandQueue* context)
        {
            _context = context;
            _type = ContextType.Queue;
        }

        private ScopedEvent(ID3D12GraphicsCommandList* context)
        {
            _context = context;
            _type = ContextType.List;
        }

        private enum ContextType
        {
            None = 1, // by doing this we get defaulted or uninit'd structs to hit the default as that is likely a _bug
            List,
            Queue
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public void EndEvent()
        {
            Debug.Assert(_context != null || _type == ContextType.None);
            if (_type == ContextType.List)
            {
                PIXMethods.EndEvent((ID3D12GraphicsCommandList*) _context);
            }
            else if (_type == ContextType.Queue)
            {
                PIXMethods.EndEvent((ID3D12CommandQueue*) _context);
            }
            else if (_type != ContextType.None)
            {
                Debug.Fail("damn bro this ain't good");
            }
        }

        public void Dispose() => EndEvent();
    }
}