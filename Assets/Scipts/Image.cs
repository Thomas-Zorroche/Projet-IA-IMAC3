using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Image
{
    //private List<Pixel> pixels;
    private List<Color> colors;

    private int size;

    public float fitness;

    Texture2D target;

    public Image(int _size, Texture2D _target, List<Color> palette)
    {
        size = _size;
        target = _target;
        colors = new List<Color>(size);
        SetRandomPixels(palette);
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
            colors.Add(palette[colorId]);
		}

        ComputeFitness();
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

	public void ComputeFitness()
    {
        fitness = 0.0f;
        Color[] targetColor = target.GetPixels();
		for (int i = 0; i < size; i++)
		{
            if (colors[i] == targetColor[i])
                fitness++;
		}

        fitness = fitness / size;

    }

    public void Crossover(Image parent1, Image parent2)
	{
        int middleIndex = Random.Range(0, size);
        for (int i = 0; i < size; i++)
            colors[i] = i > middleIndex ? parent2.colors[i] : parent1.colors[i];
        ComputeFitness();

    }


}
