using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pixel
{
    public Color color;

    private Color target;

    public float fitness;

    public Pixel(Color _color, Color _target)
    {
        color = _color;
        target = _target;
        ComputeFitness();
    }

    public void SetColor(Color newColor)
    {
        color = newColor;
        ComputeFitness();
    }

    public void Mutate()
    {
        color = Random.ColorHSV();
        ComputeFitness();
    }

    public void ComputeFitness()
    {
        ComputeEqualColors();
        //ComputeDiffColors(0.3f);
    }

    private void ComputeEqualColors()
    {
        if (color.r == target.r && color.g == target.g && color.b == target.b)
            fitness = 1.0f;
        else
            fitness = 0.0f;
    }

    private void ComputeDiffColors(float epsilon)
    {
        if (color.r - target.r < epsilon && color.g - target.g < epsilon && color.b - target.b < epsilon)
            fitness = 1.0f;
        else
            fitness = 0.0f;
    }


}
