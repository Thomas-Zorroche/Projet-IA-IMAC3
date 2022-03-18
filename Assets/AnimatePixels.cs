using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatePixels : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
		foreach (Transform pixel in transform)
		{
            float randomScale = Random.Range(1f, 1.3f);
            float randomScaleTime = Random.Range(2f, 4f);
            LeanTween.scale(pixel.gameObject, new Vector3(randomScale, randomScale, randomScale), randomScaleTime)
                .setEase(LeanTweenType.easeInOutSine)
                .setLoopType(LeanTweenType.pingPong);

            int[] sign = { -1, 1 };
            int randomSign = sign[Random.Range(0, sign.Length)];
            float randomRotateTime = Random.Range(4f, 6f);
            LeanTween.rotate(pixel.gameObject, new Vector3(0, 0, 90 * randomSign), randomRotateTime)
                .setEase(LeanTweenType.easeInOutSine)
                .setLoopType(LeanTweenType.pingPong);
        }
    }

}
