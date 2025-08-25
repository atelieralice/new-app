using System;

namespace meph {
    public static class EventBus {
        public static event Action OnAttackerTurn;
        public static event Action OnDefenderTurn;
        public static event Action OnActionLock;

        public static void RaiseAttackerTurn() => OnAttackerTurn?.Invoke();
        public static void RaiseDefenderTurn() => OnDefenderTurn?.Invoke();
        public static void RaiseActionLock() => OnActionLock?.Invoke();
    }
}