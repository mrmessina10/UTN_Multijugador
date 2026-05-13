using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Used for syncing a transform with client side changes. This includes host. Pure server as owner isn't supported by this. Please use NetworkTransform
/// for transforms that'll always be owned by the server.
/// </summary>
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    /// <summary>
    /// Used to determine who can write to this transform. Owner client only.
    /// </summary>
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}