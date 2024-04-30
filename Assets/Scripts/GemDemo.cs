using RedGame.GemCreator;
using UnityEngine;

public class GemDemo : MonoBehaviour
{
    public int count = 3;
    public GemSpriteRenderer[] gemLines;
    
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < gemLines.Length; i++)
        {
            gemLines[i].Init();
            CreateLine(gemLines[i]);
        }
    }

    private void CreateLine(GemSpriteRenderer template)
    {
        float width = 3;
        float startX = -(count - 1) * width * 0.5f;
        Color color = template.GemMaterial.color;
        Color.RGBToHSV(color, out float h, out float s, out float v);
        float y = template.transform.position.y;
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(template.gameObject, null);
            GemSpriteRenderer gem = go.GetComponent<GemSpriteRenderer>();
            gem.transform.position = new Vector3(startX + i * width, y, 0);
            //gem.transform.rotation *= Quaternion.Euler(0, 0, 10.0f * i);
            gem.Init();
            gem.GemMaterial.color = Color.HSVToRGB(h, s, v);
            h = (h + 0.05f) % 1;
        }
        template.gameObject.SetActive(false);
    }
}
