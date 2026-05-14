using UnityEngine;
using System;

public class TransitionArcState : IProjectileState
{
    public static readonly TransitionArcState Instance = new TransitionArcState();

    public void EnterState(Projectile context)
    {
        if (context.trail != null) context.trail.emitting = false;

        context.apexHeight = context.visualModel.localPosition.y - context.startVisualLocalPos.y;

        context.transitionDistance = 0f;
    }

    public void UpdateState(Projectile context)
    {
        float moveDistance = context.currentGroundedSpeed * Time.deltaTime;
        context.currentGroundedSpeed *= (float)Math.Pow(context.groundedFriction, Time.deltaTime);

        context.transitionDistance += moveDistance;
        float progress = Mathf.Clamp01(context.transitionDistance / context.transitionArcDistance);

        float currentHeight = Mathf.Sin((progress + 1f) * Mathf.PI / 2f) * context.apexHeight;

        context.transform.position += context.direction * moveDistance;
        context.visualModel.localPosition = context.startVisualLocalPos + new Vector3(0f, currentHeight, 0f);

        if (progress >= 1.0f)
        {
            context.ChangeState(GroundedState.Instance);
        }
    }
}