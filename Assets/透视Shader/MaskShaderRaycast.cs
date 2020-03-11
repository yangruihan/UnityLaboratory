using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaskShaderRaycast : MonoBehaviour
{
    public Transform tf;
    public Transform tf2;

    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit raycastHit;
        var dis = (tf.position - _camera.transform.position).magnitude;
        if (Physics.Raycast(new Ray(_camera.transform.position, _camera.transform.forward), out raycastHit, dis))
        {
            if (raycastHit.transform == tf)
            {
                tf2.localScale = Vector3.zero;
            }
            else
            {
                tf2.localScale = new Vector3(2, 2, 2);
            }
        }
    }
}