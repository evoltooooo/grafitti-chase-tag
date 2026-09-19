using System;
using UnityEngine;

[Serializable]
public class TicTacActionSettings
{
    [Min(0f)]
    public float minBounceSpeed = 4f;

    [Min(0f)]
    public float verticalLaunchSpeed = 5f;
}