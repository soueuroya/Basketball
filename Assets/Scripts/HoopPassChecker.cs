using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HoopPassChecker : MonoBehaviour
{
    private enum PassState
    {
        None,
        EnteredTop,
        EnteredBottom
    }

    private struct BallPass
    {
        public PassState State;
        public float StartedAt;
    }

    [System.Serializable]
    public class BallEvent : UnityEvent<Ball>
    {
    }

    [Header("Events")]
    [SerializeField] private BallEvent onScored;
    [SerializeField] private BallEvent onReversePass;

    [Header("Score Effect")]
    [SerializeField] private ParticleSystem scoreParticles;

    [Header("Debug")]
    [SerializeField] private bool logPasses = true;
    [SerializeField, Min(0.1f)] private float sequenceTimeout = 2f;

    private readonly Dictionary<Ball, BallPass> ballStates = new();

    public int ScoreCount { get; private set; }
    public int ReversePassCount { get; private set; }

    public void RegisterPass(Ball ball, HoopPassTrigger.Zone zone)
    {
        if (ball == null || ball.IsHeld)
        {
            return;
        }

        ballStates.TryGetValue(ball, out BallPass pass);

        if (Time.time - pass.StartedAt > sequenceTimeout)
        {
            pass = default;
        }

        if (zone == HoopPassTrigger.Zone.Top)
        {
            if (pass.State == PassState.EnteredBottom)
            {
                ReversePassCount++;
                onReversePass?.Invoke(ball);

                if (logPasses)
                {
                    Debug.Log($"Reverse hoop pass: {ball.name}", this);
                }

                ballStates.Remove(ball);
                return;
            }

            ballStates[ball] = new BallPass
            {
                State = PassState.EnteredTop,
                StartedAt = Time.time
            };
            return;
        }

        if (pass.State == PassState.EnteredTop)
        {
            ScoreCount++;
            PlayScoreEffect();
            onScored?.Invoke(ball);

            if (logPasses)
            {
                Debug.Log($"Basket scored: {ball.name}", this);
            }

            ballStates.Remove(ball);
            return;
        }

        ballStates[ball] = new BallPass
        {
            State = PassState.EnteredBottom,
            StartedAt = Time.time
        };
    }

    private void PlayScoreEffect()
    {
        if (scoreParticles == null)
        {
            return;
        }

        scoreParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        scoreParticles.Play(true);
    }

    public void ForgetBall(Ball ball)
    {
        if (ball != null)
        {
            ballStates.Remove(ball);
        }
    }
}
