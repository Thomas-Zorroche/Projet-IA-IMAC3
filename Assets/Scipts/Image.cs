using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Image
{
    private List<Pixel> pixels;

    private int size;

    public float fitness;
    public Image(int _size, Texture2D target)
    {
        size = _size;
        pixels = new List<Pixel>(size);
        var targetColors = target.GetPixels();
        for (int i = 0; i < size; i++)
            pixels.Add(new Pixel(Random.ColorHSV(), targetColors[i]));
        
        ComputeFitness();
    }

    public void SetPixelColor(int index, Color color)
    {
        if (index < size)
        {
            pixels[index].SetColor(color);
        }
    }

    public void SetRandomPixels()
    {
        foreach (var pixel in pixels)
        {
            pixel.SetColor(Random.ColorHSV());
        }
        ComputeFitness();
    }

    public Color GetPixel(int index)
    {
        return pixels[index].color;
    }

    public Color[] GetColors()
    {
        var colors = new Color[size];
        for (int i = 0; i < size; i++)
            colors[i] = pixels[i].color;
        return colors;
    }

    public void ComputeFitness()
    {
        fitness = 0.0f;
        foreach (var pixel in pixels)
            fitness += pixel.fitness;

        fitness /= size;
    }


}
