using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
public class MinMaxSliderDrawer : PropertyDrawer
{
    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.Vector2)
        {
            EditorGUI.LabelField(
                position,
                label.text,
                "MinMaxSlider requires a Vector2.");
            return;
        }

        MinMaxSliderAttribute slider =
            (MinMaxSliderAttribute)attribute;
        Vector2 range = property.vector2Value;

        EditorGUI.BeginProperty(position, label, property);
        position = EditorGUI.PrefixLabel(position, label);

        const float fieldWidth = 48f;
        const float spacing = 4f;

        Rect minRect = new Rect(
            position.x,
            position.y,
            fieldWidth,
            position.height);
        Rect sliderRect = new Rect(
            minRect.xMax + spacing,
            position.y,
            position.width - fieldWidth * 2f - spacing * 2f,
            position.height);
        Rect maxRect = new Rect(
            sliderRect.xMax + spacing,
            position.y,
            fieldWidth,
            position.height);

        range.x = EditorGUI.FloatField(minRect, range.x);
        range.y = EditorGUI.FloatField(maxRect, range.y);
        EditorGUI.MinMaxSlider(
            sliderRect,
            ref range.x,
            ref range.y,
            slider.MinLimit,
            slider.MaxLimit);

        range.x = Mathf.Clamp(range.x, slider.MinLimit, slider.MaxLimit);
        range.y = Mathf.Clamp(range.y, slider.MinLimit, slider.MaxLimit);

        if (range.x > range.y)
        {
            (range.x, range.y) = (range.y, range.x);
        }

        property.vector2Value = range;

        EditorGUI.EndProperty();
    }
}
