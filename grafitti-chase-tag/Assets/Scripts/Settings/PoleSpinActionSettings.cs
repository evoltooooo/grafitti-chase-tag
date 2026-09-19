using System;
using UnityEngine;

[Serializable]
public class PoleSpinActionSettings
{
    [Min(0f)]
    public float staminaPerRevolution = 0f;

    [Min(0f)]
    public float angularSpeed = 2.5f;

    [Min(0f)]
    public float rotationSpeed = 10f;
}