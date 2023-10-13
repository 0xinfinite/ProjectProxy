using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DopplerEffectSource : MonoBehaviour
{
    private Renderer render;

    private MaterialPropertyBlock block;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        render = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        SetPrevPosition();
        
        while (enabled)
        {
            yield return new WaitForEndOfFrame();
            SetPrevPosition();
        }
    }

    private void SetPrevPosition()
    {
        block.SetVector("_PrevPosition", transform.position);

        render.SetPropertyBlock(block);
    }

    private void OnDestroy()
    {
        render.SetPropertyBlock(null);
        block = null;
    }

    // Update is called once per frame

}
