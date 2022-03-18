using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

public class Image
{
    public Color[] colors;
    //NativeArray<Color> colors;

    JobHandle fitnessJobHandle;
    ComputeFitnessJob fitnessJob; 


    private int size;

    public float fitness;

    public Image(int _size)
    {
        size = _size;
        //colors = new NativeArray<Color>(size, Allocator.Persistent);
        colors = new Color[size];
    }

    private void OnDestroy()
    {
        //colors.Dispose();
    }

    /*    public Image(Image parent)
        {
            size = parent.size;
            colors = new Color[size];
            CopyColors(parent);
        }*/

    public void CopyColors(Image parent)
    {
        parent.colors.CopyTo(colors, 0);
        //NativeArray<Color>.Copy(parent.colors, colors, size);
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

    private struct ComputeFitnessJob : IJobParallelFor
    {
        public float _fitness;
        private Color _targetColor;
        private Color _currentColor;

        [ReadOnly]
        public NativeArray<Color> _targetColors;
        [ReadOnly]
        public NativeArray<Color> _colors;

        public void Execute(int i)
        {
            _targetColor = _targetColors[i];
            _currentColor = _colors[i];
            
            if (_currentColor.r == _targetColor.r && _currentColor.g == _targetColor.g && _currentColor.b == _targetColor.b)
            {
                _fitness++;
            }
        }
    }

    public void ComputeFitnessParallel(Color[] targetColors)
    {
/*        fitnessJob = new ComputeFitnessJob()
        {
            _fitness = fitness,
            _targetColors = targetColors,
            _colors = colors,
        };*/

        fitnessJobHandle = fitnessJob.Schedule(size, 64);


        // Finish job
        fitnessJobHandle.Complete();
        fitness = fitnessJob._fitness / size;
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




}
