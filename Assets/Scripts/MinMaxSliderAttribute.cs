using UnityEngine;

public class MinMaxSliderAttribute : PropertyAttribute
{
    public float MinLimit { get; }
    public float MaxLimit { get; }

    public MinMaxSliderAttribute(float minLimit, float maxLimit)
    {
        MinLimit = minLimit;
        MaxLimit = maxLimit;
    }
}
