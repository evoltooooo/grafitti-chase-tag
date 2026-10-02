using UnityEngine;

public class AIPrediction
{
    public float CalculatePredictionTime(
        float distance,
        float selfSpeed,
        float minPredictionTime,
        float maxPredictionTime)
    {
        if (selfSpeed <= 0.01f)
            return minPredictionTime;

        float travelTime =
            distance / selfSpeed;

        return Mathf.Clamp(
            travelTime,
            minPredictionTime,
            maxPredictionTime
        );
    }


    public Vector3 PredictPosition(
        Vector3 position,
        Vector3 velocity,
        float predictionTime)
    {
        Vector3 horizontalVelocity =
            new Vector3(
                velocity.x,
                0f,
                velocity.z
            );

        return position +
            horizontalVelocity * predictionTime;
    }
}