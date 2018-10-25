using UnityEngine;
using System.Collections;

public class anm_tex_01 : MonoBehaviour
{
    public int uvAnimationTileX = 8;
    public int uvAnimationTileY = 8;
    public float framesPerSecond = 24.0f;

    private void Update()
    {
        int index = (int)(Time.time * framesPerSecond);

        index = index % (uvAnimationTileX * uvAnimationTileY);

        var size = new Vector2(1.0f / uvAnimationTileX, 1.0f / uvAnimationTileY);

        var uIndex = index % uvAnimationTileX;
        var vIndex = index / uvAnimationTileX;

        var offset = new Vector2(uIndex * size.x, 1.0f - size.y - vIndex * size.y);

        GetComponent<Renderer>().material.SetTextureOffset("_MainTex", offset);
        GetComponent<Renderer>().material.SetTextureScale("_MainTex", size);
    }
}