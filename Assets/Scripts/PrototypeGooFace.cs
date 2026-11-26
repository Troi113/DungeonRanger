using UnityEngine;

/// <summary>
/// Presentation only: an original cute-slime look for Goo built from primitives (a glossy
/// teardrop tip, big eyes, and a small mouth on the body's +Z side). When Goo is hit it
/// squashes and briefly pulls a random silly face: squinting (> <), shocked (O O with an
/// open mouth), or derpy (mismatched eyes, tongue out).
/// </summary>
[RequireComponent(typeof(PrototypeEnemy))]
public sealed class PrototypeGooFace : MonoBehaviour
{
    [SerializeField] private float faceSeconds = 0.8f;
    [SerializeField] private float squashSeconds = 0.35f;

    private PrototypeEnemy enemy;
    private Transform face;
    private GameObject normal;
    private GameObject[] silly;
    private Vector3 baseScale;
    private float faceTimer;
    private float squashTimer;
    private int sillyIndex;

    private void Start()
    {
        enemy = GetComponent<PrototypeEnemy>();
        baseScale = transform.localScale;
        enemy.Damaged += OnDamaged;

        // Children of a non-uniformly scaled body: a holder undoes the scale so parts keep their shape.
        face = new GameObject("Goo_Face").transform;
        face.SetParent(transform, false);
        face.localScale = new Vector3(1f / baseScale.x, 1f / baseScale.y, 1f / baseScale.z);
        var r = baseScale.x * 0.5f;
        var h = baseScale.y * 0.5f;
        var front = r * 0.93f;

        var white = PrototypeBootstrap.CreateMaterial("Goo Eye White", Color.white);
        var black = PrototypeBootstrap.CreateMaterial("Goo Eye Black", new Color(0.08f, 0.04f, 0.04f));
        var mouthColor = PrototypeBootstrap.CreateMaterial("Goo Mouth", new Color(0.12f, 0.3f, 0.12f));
        var tongue = PrototypeBootstrap.CreateMaterial("Goo Tongue", new Color(0.95f, 0.4f, 0.5f));
        var body = GetComponent<Renderer>() != null ? GetComponent<Renderer>().sharedMaterial : white;

        // Teardrop tip and a little droplet on top.
        Part("Tip", face, PrimitiveType.Sphere, new Vector3(0f, h * 0.85f, -r * 0.1f), new Vector3(r * 0.7f, h * 0.7f, r * 0.7f), body);
        Part("Droplet", face, PrimitiveType.Sphere, new Vector3(r * 0.25f, h * 1.35f, -r * 0.1f), Vector3.one * r * 0.18f, body);

        normal = new GameObject("Face_Normal");
        normal.transform.SetParent(face, false);
        Eye(normal.transform, -0.32f, r, h, front, 0.3f, white, black);
        Eye(normal.transform, 0.32f, r, h, front, 0.3f, white, black);
        Part("Mouth", normal.transform, PrimitiveType.Cube, new Vector3(0f, -h * 0.2f, front), new Vector3(r * 0.22f, h * 0.04f, 0.05f), mouthColor);

        silly = new GameObject[3];
        // Squint: > <
        silly[0] = new GameObject("Face_Squint");
        silly[0].transform.SetParent(face, false);
        foreach (var side in new[] { -1f, 1f })
            foreach (var tilt in new[] { 30f, -30f })
            {
                var bar = Part("Squint", silly[0].transform, PrimitiveType.Cube,
                    new Vector3(side * r * 0.32f, h * 0.12f + tilt / 30f * h * 0.05f, front), new Vector3(r * 0.24f, h * 0.05f, 0.05f), black);
                bar.localRotation = Quaternion.Euler(0f, 0f, side * tilt);
            }
        Part("Grit", silly[0].transform, PrimitiveType.Cube, new Vector3(0f, -h * 0.22f, front), new Vector3(r * 0.35f, h * 0.08f, 0.05f), mouthColor);
        // Shocked: O O with an open O mouth.
        silly[1] = new GameObject("Face_Shocked");
        silly[1].transform.SetParent(face, false);
        Eye(silly[1].transform, -0.32f, r, h, front, 0.38f, white, black, 0.35f);
        Eye(silly[1].transform, 0.32f, r, h, front, 0.38f, white, black, 0.35f);
        Part("O_Mouth", silly[1].transform, PrimitiveType.Sphere, new Vector3(0f, -h * 0.28f, front), new Vector3(r * 0.2f, h * 0.26f, 0.1f), mouthColor);
        // Derp: mismatched eyes looking different ways, tongue out.
        silly[2] = new GameObject("Face_Derp");
        silly[2].transform.SetParent(face, false);
        Eye(silly[2].transform, -0.3f, r, h, front, 0.3f, white, black, 0.4f, new Vector2(-0.4f, 0.3f));
        Eye(silly[2].transform, 0.34f, r, h, front, 0.16f, white, black, 0.5f, new Vector2(0.4f, -0.3f));
        Part("Grin", silly[2].transform, PrimitiveType.Cube, new Vector3(0f, -h * 0.2f, front), new Vector3(r * 0.4f, h * 0.05f, 0.05f), mouthColor);
        Part("Tongue", silly[2].transform, PrimitiveType.Sphere, new Vector3(r * 0.1f, -h * 0.32f, front), new Vector3(r * 0.14f, h * 0.2f, 0.12f), tongue);

        ShowFace(-1);
        enemy.AttachExtraRenderers(face.GetComponentsInChildren<Renderer>(true));
    }

    private void OnDestroy()
    {
        if (enemy != null) enemy.Damaged -= OnDamaged;
    }

    private void OnDamaged()
    {
        if (silly == null) return;
        sillyIndex = (sillyIndex + 1 + Random.Range(0, silly.Length - 1)) % silly.Length;
        ShowFace(sillyIndex);
        faceTimer = faceSeconds;
        squashTimer = squashSeconds;
    }

    private void Update()
    {
        if (silly == null || PrototypeGameUI.IsGamePaused) return;
        if (faceTimer > 0f)
        {
            faceTimer -= Time.deltaTime;
            if (faceTimer <= 0f) ShowFace(-1);
        }

        // Squash-and-stretch wobble after a hit.
        var squash = 0f;
        if (squashTimer > 0f)
        {
            squashTimer -= Time.deltaTime;
            squash = Mathf.Sin((1f - squashTimer / squashSeconds) * Mathf.PI * 2f) * 0.18f * (squashTimer / squashSeconds);
        }
        transform.localScale = new Vector3(baseScale.x * (1f + squash), baseScale.y * (1f - squash), baseScale.z * (1f + squash));
    }

    private void ShowFace(int index)
    {
        normal.SetActive(index < 0);
        for (var i = 0; i < silly.Length; i++) silly[i].SetActive(i == index);
    }

    private static void Eye(Transform parent, float x, float r, float h, float front, float size, Material white, Material black,
        float pupil = 0.55f, Vector2 look = default)
    {
        var center = new Vector3(x * r, h * 0.12f, front);
        Part("Eye", parent, PrimitiveType.Sphere, center, new Vector3(r * size, r * size * 1.1f, r * size * 0.5f), white);
        var offset = new Vector3(look.x, look.y, 0f) * r * size * 0.25f;
        Part("Pupil", parent, PrimitiveType.Sphere, center + offset + Vector3.forward * r * size * 0.2f,
            new Vector3(r * size * pupil, r * size * pupil * 1.1f, r * size * 0.3f), black);
        Part("Shine", parent, PrimitiveType.Sphere, center + offset + new Vector3(r * size * 0.12f, r * size * 0.15f, r * size * 0.32f),
            Vector3.one * r * size * 0.15f, white);
    }

    private static Transform Part(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Object.Destroy(part.GetComponent<Collider>());
        return part.transform;
    }
}
