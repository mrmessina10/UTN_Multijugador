using UnityEngine;
using System;

public class GroundedState : IProjectileState
{
    public static readonly GroundedState Instance = new GroundedState();

    public void EnterState(Projectile context)
    {
        // Forzamos la posición estricta al suelo
        context.transform.position = new Vector3(context.transform.position.x, 0f, context.transform.position.z);

        if (context.visualModel != null) context.visualModel.localPosition = context.startVisualLocalPos;
        if (context.trail != null) context.trail.emitting = false;

        context.groundTimer = 0f;
    }

    public void UpdateState(Projectile context)
    {
        // Derrape con inercia
        float moveDistance = context.currentGroundedSpeed * Time.deltaTime;
        context.currentGroundedSpeed *= (float)Math.Pow(context.groundedFriction, Time.deltaTime);
        context.transform.position += context.direction * moveDistance;

        // Temporizador de vida en el suelo
        context.groundTimer += Time.deltaTime;
        if (context.groundTimer >= context.groundDuration)
        {
            context.onRelease?.Invoke(context);
        }
    }
}