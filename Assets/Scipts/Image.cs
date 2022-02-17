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

    Color[] targetColors;


    public Image(int _size, Texture2D _target, List<Color> palette, bool setRandomPixel = true, bool computeFitness = true)
    {
        size = _size;
        target = _target;
        colors = new List<Color>(size);
        targetColors = target.GetPixels();

        if (setRandomPixel)
        {
            SetRandomPixels(palette);
        }

        if (computeFitness)
        {
            ComputeFitness();
        }
    }

    public Image(Image parent)
    {
        size = parent.size;
        target = parent.target;
        targetColors = target.GetPixels();
        colors = new List<Color>(parent.colors);
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
        Color targetColor;
        Color currentColor;
		for (int i = 0; i < size; i++)
		{
            //if (colors[i] == targetColor[i])
            targetColor = targetColors[i];
            currentColor = colors[i];
            //if (currentColor.r != targetColor.r || currentColor.g != targetColor.g || currentColor.b != targetColor.b)
            if (currentColor.r != targetColor.r || currentColor.g != targetColor.g || currentColor.b != targetColor.b)
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

    public void Crossover(Image parent)
	{
        int middleIndex = Random.Range(0, size);
        for (int i = middleIndex; i < size; i++)
        {
            colors[i] = parent.colors[i];
        }
    }


}
