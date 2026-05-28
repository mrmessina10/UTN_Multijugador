using Unity.Netcode;
using UnityEngine;

public class ActiveFlightState : IProjectileState
{
    public static readonly ActiveFlightState Instance = new ActiveFlightState();

    public void EnterState(Projectile context)
    {
        if (context.trail != null)
        {
            context.trail.Clear();
            context.trail.emitting = true;
        }
    }

    public void UpdateState(Projectile context)
    {
        float moveDistance = context.speed * Time.deltaTime;

        context.totalDistanceTraveled += moveDistance;
        context.distanceSinceLastBounce += moveDistance;

        if (context.totalDistanceTraveled >= context.currentMaxDistance)
        {
            context.ChangeState(TransitionArcState.Instance);
            return;
        }

        CheckCollision(context, moveDistance);

        // El movimiento se aplica DESPUÉS de comprobar la colisión
        context.transform.position += context.direction * moveDistance;

        UpdateVisualArc(context);
    }

    private void CheckCollision(Projectile context, float distance)
    {
        if (Physics.Raycast(context.transform.position, context.direction, out RaycastHit hit, distance + 0.1f))
        {
            // Verificamos si el objeto golpeado tiene un NetworkObject (los jugadores lo tienen)
            if (hit.collider.TryGetComponent(out NetworkObject netObj))
            {
                // Si el ID del objeto golpeado es el mismo que el del tirador, ignoramos el impacto
                if (netObj.NetworkObjectId == context.shooterNetworkId) return;
            }

            // 1. CHOQUE CON ENTIDAD DAÑABLE
            if (hit.collider.TryGetComponent(out IDamageable damageableTarget))
            {
                damageableTarget.TakeDamage(context.damage, context.effectType, context.effectDuration);

                if (context.remainingPenetration > 0)
                {
                    context.remainingPenetration--;
                    context.transform.position = hit.point + context.direction * 0.1f;
                }
                else
                {
                    context.ChangeState(TransitionArcState.Instance);
                }
            }
            // 2. CHOQUE CON ENTORNO MATERIA INERTE (Paredes, Suelo, Obstáculos)
            else
            {
                if (context.remainingBounces > 0)
                {
                    // LÓGICA FÍSICA: Calculamos el ángulo de salida perfecto
                    context.direction = Vector3.Reflect(context.direction, hit.normal);

                    // Forzamos la Y a 0 para que el proyectil no se dispare hacia el cielo o el piso
                    context.direction.y = 0;

                    context.remainingBounces--;

                    // Evitamos quedarnos atrapados dentro de la pared
                    context.transform.position = hit.point + context.direction * 0.05f;

                    // LÓGICA DE GAMEPLAY: Damos la bonificación de distancia y reseteamos el arco
                    context.currentMaxDistance += context.distanceBonusPerBounce;
                    context.distanceSinceLastBounce = 0f;
                }
                else
                {
                    // Si chocó contra la pared y no le quedan rebotes, transiciona a inofensivo
                    context.ChangeState(TransitionArcState.Instance);
                }
            }
        }
    }

    private void UpdateVisualArc(Projectile context)
    {
        if (context.visualModel != null && context.arcHeight > 0f)
        {
            float progress = Mathf.Clamp01(context.distanceSinceLastBounce / context.visualArcDistance);
            float currentHeight = Mathf.Sin(progress * Mathf.PI) * context.arcHeight;
            context.visualModel.localPosition = context.startVisualLocalPos + new Vector3(0f, currentHeight, 0f);
        }
    }
}