using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTexturePreview : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        var GM = GameObject.FindObjectOfType<GameManager>();
        if (GM)
        {
            Graphics.Blit(GM.GetGridImage(), destination);
        }
    }
}
