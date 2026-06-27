namespace MonoGameLearning.Core.Combat;

public class OilDrumBehavior
{
    private bool _isHitStunned;
    private float _hitStunTimer;

    public bool IsHitStunned => _isHitStunned;

    public bool CanTakeDamage(bool isAlive) => isAlive && !_isHitStunned;

    public void ApplyStun()
    {
        _isHitStunned = true;
        _hitStunTimer = 0.3f;
    }

    public bool Update(float deltaSeconds)
    {
        if (!_isHitStunned) return false;
        _hitStunTimer -= deltaSeconds;
        if (_hitStunTimer <= 0)
        {
            _isHitStunned = false;
            return true;
        }
        return false;
    }

    public void Reset()
    {
        _isHitStunned = false;
        _hitStunTimer = 0;
    }
}