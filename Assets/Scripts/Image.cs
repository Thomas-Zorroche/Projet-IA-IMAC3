using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Image
{
    private Color[] colors;

    private int size;

    public float fitness;

    Texture2D target;

    public Image(int _size, Texture2D _target, List<Color> palette, Color[] targetColors, bool setRandomPixel = true, bool computeFitness = true)
    {
        size = _size;
        target = _target;
        colors = new Color[size];

        if (setRandomPixel)
        {
            SetRandomPixels(palette);
        }

        if (computeFitness)
        {
            ComputeFitness(targetColors);
        }
    }

    public Image(Image parent)
    {
        size = parent.size;
        target = parent.target;
        colors = new Color[size];
        parent.colors.CopyTo(colors, 0);
    }

    public void SetPixelColor(int index, Color color)
    {
        if (index < size)
        {
            colors[index] = color;
        }
    }

    public void SetRandomPixels(List<Color> palette)
    {
		for (int i = 0; i < size; i++)
		{
            int colorId = Random.Range(0, palette.Count);
            colors[i] = palette[colorId];
		}
    }

    public Color GetPixel(int index)
    {
        return colors[index];
    }

	public Color[] GetColors()
	{
		var colorArray = new Color[size];
		for (int i = 0; i < size; i++)
			colorArray[i] = colors[i];
		return colorArray;
	}

	public void ComputeFitness(Color[] targetColors)
    {
        fitness = 0.0f;
        Color targetColor;
        Color currentColor;
		for (int i = 0; i < size; i++)
		{
            targetColor = targetColors[i];
            currentColor = colors[i];
            float epsilon = 0.01f;
            if (Mathf.Abs(currentColor.r - targetColor.r) < epsilon
                || Mathf.Abs(currentColor.g - targetColor.g) < epsilon 
                || Mathf.Abs(currentColor.b - targetColor.b) < epsilon)
            {
                continue;
            }
            else
            {
                fitness++;
            }
		}

        fitness = fitness / size;
    }

    public void ComputeFitnessParallel()
    {

    }

    public void Crossover(Image parent)
	{
        int middleIndex = Random.Range(0, size);
        for (int i = middleIndex; i < size; i++)
        {
            colors[i] = parent.colors[i];
        }
    }

    // Kernels Ids
    //readonly int KERNEL_FITNESS;

    // Uniforms Ids
    //readonly int CS_ID_BUTTERFLY_TEXTURE = Shader.PropertyToID("ButterflyTexture");


}
