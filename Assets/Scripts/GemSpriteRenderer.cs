using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GemSpriteRenderer : MonoBehaviour
{
    private SpriteRenderer _sprite;
    private Light2D _light;
    private Material _material;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start GemSpriteRenderer " + name);
        _light = FindObjectOfType<Light2D>();
        _sprite = GetComponent<SpriteRenderer>();
        
        if (_sprite)
        {
            Debug.Log("Found SpriteRenderer " + name);
            _material = new Material(_sprite.material);
            _material.name = "GemMaterial " + name;
            _sprite.material = _material;
        }
        
    }

    private void Update()
    {
        if (_material)
        {
            Vector3 lightDirection = -_light.transform.forward;
            Vector3 lightDirLocal = transform.InverseTransformDirection(lightDirection);
            lightDirLocal.z = -lightDirLocal.z;

            Camera curCamera = Camera.main;
            
            Vector3 cameraPosLocal = transform.InverseTransformPoint(curCamera.transform.position);
            float spriteSize = _sprite.size.y;
            cameraPosLocal.x /= spriteSize;
            cameraPosLocal.y /= spriteSize;
            cameraPosLocal.z /= -spriteSize;

            Vector4 lightDirParam = lightDirLocal;
            lightDirParam.w = _light.intensity;
            _material.SetVector("_MainLightDir", lightDirParam);
            _material.SetVector("_EyePos", cameraPosLocal);
        }
    }

}
