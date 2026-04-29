using System;

namespace DungeonBlade.Core
{
    public static class MenuState
    {
        static int _openCount;

        public static bool IsAnyOpen => _openCount > 0;

        public static event Action<bool> OnAnyMenuChanged;

        public static void Push()
        {
            _openCount++;
            if (_openCount == 1) OnAnyMenuChanged?.Invoke(true);
        }

        public static void Pop()
        {
            if (_openCount <= 0) return;
            _openCount--;
            if (_openCount == 0) OnAnyMenuChanged?.Invoke(false);
        }

        public static void Reset()
        {
            _openCount = 0;
            OnAnyMenuChanged?.Invoke(false);
        }
    }
}
