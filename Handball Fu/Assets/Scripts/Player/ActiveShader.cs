using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveShader : MonoBehaviour
{
    private Material mat;
    private void Start()
    {
        mat = GetComponent<Renderer>().material;
    }
    public void MakeTransparent()
    {
        mat.SetFloat("_Transparent", 1);
    }
    public void UndoTransparent()
    {
        mat.SetFloat("_Transparent", 0);
    }
}
