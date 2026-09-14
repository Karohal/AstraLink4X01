using System;

namespace Game.Core
{
    public interface ISimulationClock
    {
        bool IsPaused { get; }
        float SpeedMultiplier { get; }
        void Pause();
        void Resume();
        void SetSpeed(float multiplier);
        void Tick(float realDeltaTime);
        event Action<float> OnTick;
    }

    public sealed class SimulationClock : ISimulationClock
    {
        public bool IsPaused { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;

        public event Action<float> OnTick;

        public void Pause() => IsPaused = true;

        public void Resume() => IsPaused = false;

        public void SetSpeed(float multiplier)
        {
            SpeedMultiplier = Math.Max(0f, multiplier);
        }

        public void Tick(float realDeltaTime)
        {
            if (IsPaused || realDeltaTime <= 0f) return;

            var simDeltaTime = realDeltaTime * SpeedMultiplier;
            if (simDeltaTime > 0f)
                OnTick?.Invoke(simDeltaTime);
        }
    }
}
