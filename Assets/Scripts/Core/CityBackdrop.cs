using UnityEngine;

/// <summary>Cenário Vice City: céu pôr do sol, sol retro, prédios com janelas neon, palmeiras e chão com grade.</summary>
public static class CityBackdrop
{
    public static GameObject Build(bool gameScene)
    {
        var root = new GameObject("CenarioCidade");
        var rnd = new System.Random(2611);

        // céu (esfera de dentro para fora)
        var sky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sky.name = "Ceu";
        Object.Destroy(sky.GetComponent<Collider>());
        sky.transform.SetParent(root.transform, false);
        sky.transform.localScale = Vector3.one * 600f;
        var skyMat = Mats.UnlitTex(Tex.Sky(), Color.white);
        skyMat.SetFloat("_Cull", 0f);
        sky.GetComponent<Renderer>().sharedMaterial = skyMat;
        sky.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // sol retro
        var sun = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sun.name = "Sol";
        Object.Destroy(sun.GetComponent<Collider>());
        sun.transform.SetParent(root.transform, false);
        sun.transform.position = new Vector3(0f, gameScene ? 22f : 30f, 230f);
        sun.transform.localScale = Vector3.one * 95f;
        sun.GetComponent<Renderer>().sharedMaterial = Mats.SpriteMat(Tex.Sun(), Color.white);

        // chão com grade neon
        Texture2D gb, ge;
        Tex.Grid(128, 2, Palette.Violeta, Palette.H("07030f"), out gb, out ge);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Chao";
        Object.Destroy(floor.GetComponent<Collider>());
        floor.transform.SetParent(root.transform, false);
        floor.transform.position = new Vector3(0f, gameScene ? -0.72f : 0f, 0f);
        floor.transform.localScale = new Vector3(60f, 1f, 60f);
        var fm = Mats.LitTex(gb, ge, Color.white, 1.4f, 0.75f);
        fm.mainTextureScale = new Vector2(300f, 300f);
        fm.SetTextureScale("_BaseMap", new Vector2(300f, 300f));
        fm.SetTextureScale("_EmissionMap", new Vector2(300f, 300f));
        floor.GetComponent<Renderer>().sharedMaterial = fm;

        // prédios
        var wins = new Material[3];
        for (int i = 0; i < 3; i++)
        {
            Texture2D b, e;
            Tex.Windows(100 + i * 17, out b, out e);
            wins[i] = Mats.LitTex(b, e, Color.white, 2.2f, 0.2f);
        }
        int count = gameScene ? 46 : 60;
        for (int i = 0; i < count; i++)
        {
            float w = 8f + (float)rnd.NextDouble() * 12f;
            float h = 14f + (float)rnd.NextDouble() * 70f;
            float d = 8f + (float)rnd.NextDouble() * 10f;
            float x = -170f + (float)rnd.NextDouble() * 340f;
            float z = 70f + (float)rnd.NextDouble() * 80f;
            if (!gameScene && Mathf.Abs(x) < 18f) z += 40f;
            var bld = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bld.name = "Predio_" + i;
            Object.Destroy(bld.GetComponent<Collider>());
            bld.transform.SetParent(root.transform, false);
            bld.transform.position = new Vector3(x, h * 0.5f + (gameScene ? -0.72f : 0f), z);
            bld.transform.localScale = new Vector3(w, h, d);
            var m = new Material(wins[i % 3]);
            m.SetTextureScale("_BaseMap", new Vector2(w / 6f, h / 14f));
            m.SetTextureScale("_EmissionMap", new Vector2(w / 6f, h / 14f));
            bld.GetComponent<Renderer>().sharedMaterial = m;
        }

        // palmeiras (silhueta escura com brilho)
        int palms = gameScene ? 10 : 12;
        for (int i = 0; i < palms; i++)
        {
            float side = (i % 2 == 0) ? -1f : 1f;
            float x = side * (gameScene ? 7f + (float)rnd.NextDouble() * 12f : 10f + (float)rnd.NextDouble() * 24f);
            float z = gameScene ? 3f + (float)rnd.NextDouble() * 14f : 25f + (float)rnd.NextDouble() * 40f;
            Palm(root.transform, new Vector3(x, gameScene ? -0.72f : 0f, z), 0.9f + (float)rnd.NextDouble() * 0.6f, rnd);
        }

        return root;
    }

    static void Palm(Transform parent, Vector3 pos, float scale, System.Random rnd)
    {
        var p = new GameObject("Palmeira");
        p.transform.SetParent(parent, false);
        p.transform.position = pos;
        p.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
        p.transform.localScale = Vector3.one * scale;

        var dark = Mats.Lit(Palette.H("0d0620"), 0.2f);
        Mats.Emit(dark, Palette.Magenta, 0.15f);

        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(trunk.GetComponent<Collider>());
        trunk.transform.SetParent(p.transform, false);
        trunk.transform.localScale = new Vector3(0.35f, 4.5f, 0.35f);
        trunk.transform.localPosition = new Vector3(0f, 4.5f, 0f);
        trunk.GetComponent<Renderer>().sharedMaterial = dark;

        for (int i = 0; i < 8; i++)
        {
            var f = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(f.GetComponent<Collider>());
            f.transform.SetParent(p.transform, false);
            f.transform.localScale = new Vector3(0.55f, 0.06f, 4.2f);
            float yaw = i * 45f + (float)rnd.NextDouble() * 15f;
            f.transform.localRotation = Quaternion.Euler(28f, yaw, 0f);
            f.transform.localPosition = new Vector3(0f, 9.1f, 0f) + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, -0.9f, 1.8f);
            f.GetComponent<Renderer>().sharedMaterial = dark;
        }
    }
}
