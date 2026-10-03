using UnityEngine;

public class AIChaserDestination
{
    private readonly AIPrediction prediction;
    private readonly AIIntercept intercept;

    private readonly float minPredictionTime;
    private readonly float maxPredictionTime;

    private readonly float minInterceptLeadTime;
    private readonly float maxInterceptLeadTime;

    public AIChaserDestination(
        AIPrediction prediction,
        AIIntercept intercept,
        float minPredictionTime,
        float maxPredictionTime,
        float minInterceptLeadTime,
        float maxInterceptLeadTime)
    {
        this.prediction = prediction;
        this.intercept = intercept;

        this.minPredictionTime = minPredictionTime;
        this.maxPredictionTime = maxPredictionTime;

        this.minInterceptLeadTime = minInterceptLeadTime;
        this.maxInterceptLeadTime = maxInterceptLeadTime;
    }

    public Vector3 Calculate(
        AIContext context,
        AIChaserState state)
    {
        switch (state)
        {
            case AIChaserState.Pursue:
                return CalculatePursue(context);

            case AIChaserState.Intercept:
                return CalculateIntercept(context);

            case AIChaserState.Search:
                return context.LastKnownOpponentPosition;

            default:
                return context.Position;
        }
    }

    private Vector3 CalculatePursue(
        AIContext context)
    {
        float distance =
            Vector3.Distance(
                context.Position,
                context.OpponentPosition
            );

        float predictionTime =
            prediction.CalculatePredictionTime(
                distance,
                context.HorizontalSpeed,
                minPredictionTime,
                maxPredictionTime
            );

        Vector3 destination =
            prediction.PredictPosition(
                context.OpponentPosition,
                context.OpponentVelocity,
                predictionTime
            );

        return destination;
    }

    private Vector3 CalculateIntercept(
        AIContext context)
    {
        Vector3 destination =
            intercept.CalculateInterceptPosition(
                context.Position,
                context.HorizontalSpeed,
                context.OpponentPosition,
                context.OpponentVelocity,
                minInterceptLeadTime,
                maxInterceptLeadTime
            );

        return destination;
    }
}