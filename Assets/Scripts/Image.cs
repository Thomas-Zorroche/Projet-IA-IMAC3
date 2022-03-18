using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Image
{
    public Color[] colors;

    private int size;

    public float fitness;

    public Image(int _size)
    {
        size = _size;
        colors = new Color[size];
    }

    public Image(Image parent)
    {
        size = parent.size;
        colors = new Color[size];
        parent.colors.CopyTo(colors, 0);
        //CopyColors(parent);
    }

    public void CopyColors(Image parent)
    {
        //System.Array.Copy(parent.colors, colors, size);
        //colors = new Color[size];
        parent.colors.CopyTo(colors, 0);
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

    // Kernels Ids
    //readonly int KERNEL_FITNESS;

    // Uniforms Ids
    //readonly int CS_ID_BUTTERFLY_TEXTURE = Shader.PropertyToID("ButterflyTexture");


}
