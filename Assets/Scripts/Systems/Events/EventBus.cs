using System;

namespace AINPC.Systems.Events
{
    public static class EventBus
    {
        public static event Action<PlayerEvent> OnPlayerEvent;

        public static void Publish(PlayerEvent e)
        {
            OnPlayerEvent?.Invoke(e);
        }
    }
}
