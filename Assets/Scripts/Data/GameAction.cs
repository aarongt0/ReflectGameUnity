using System;
using UnityEngine;

public sealed class GameAction
{
    // Class to store actions for PlayerController
    public string Name { get; }
    public float CooldownSeconds { get; set; }
    public float CurrentCooldownDuration { get; private set; }
    private float _nextReadyTime;
    public bool IsOnCooldown => Time.time < _nextReadyTime;


    private readonly Func<bool> _canUse;
    private readonly Action _execute; // Action is a method that returns void i.e. button-activated actions

    public event Action<float> CooldownStarted;
    public event Action CooldownEnded;

    public GameAction(
        string name,
        float cooldownSeconds,
        Action execute,
        Func<bool> canUse = null)
    {
        Name = name;
        CooldownSeconds = Mathf.Max(0f, cooldownSeconds); // Clamp between 0 and remaining seconds to avoid negative vals
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canUse = canUse; 
    }

    // Called from the buffer list if is a valid buffered action
    public bool TryInvoke()
    {
        if (IsOnCooldown) return false;
        if (_canUse != null && !_canUse()) return false;

        _execute.Invoke();

        if (CooldownSeconds > 0f)
        {
            CurrentCooldownDuration = CooldownSeconds;
            _nextReadyTime = Time.time + CurrentCooldownDuration;
            CooldownStarted?.Invoke(CurrentCooldownDuration);
        }

        return true;
    }

    public float CooldownRemaining()
        => Mathf.Max(0f, _nextReadyTime - Time.time);

    public void ResetCooldown()
    {
        _nextReadyTime = 0f;
        CurrentCooldownDuration = 0f;
        CooldownEnded?.Invoke();
    }

    public void ReduceCooldownRemaining(float factor)
    {
        if (!IsOnCooldown) return;

        factor = Mathf.Clamp01(factor);

        float remaining = CooldownRemaining();
        float newRemaining = remaining * factor;

        _nextReadyTime = Time.time + newRemaining;
    }
}
  

