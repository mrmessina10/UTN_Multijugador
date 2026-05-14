public interface IProjectileState
{
    void EnterState(Projectile context);
    void UpdateState(Projectile context);
}