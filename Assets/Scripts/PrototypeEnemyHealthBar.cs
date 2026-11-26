using UnityEngine;

/// <summary>Screen-facing health bar positioned above a damaged enemy.</summary>
[RequireComponent(typeof(PrototypeEnemy), typeof(Renderer))]
public sealed class PrototypeEnemyHealthBar : MonoBehaviour
{
    [SerializeField] private float widthPixels = 56f;
    [SerializeField] private float heightPixels = 8f;
    [SerializeField] private float heightOffset = 0.4f;

    private PrototypeEnemy enemy;
    private Renderer enemyRenderer;
    private Camera viewCamera;

    private void Awake()
    {
        enemy = GetComponent<PrototypeEnemy>();
        enemyRenderer = GetComponent<Renderer>();
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || PrototypeGameUI.IsGamePaused ||
            !enemy.IsAlive || !enemy.HasTakenDamage || !enemyRenderer.enabled) return;

        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera == null) return;

        var bounds = enemyRenderer.bounds;
        var worldPosition = new Vector3(bounds.center.x, bounds.max.y + heightOffset, bounds.center.z);
        var position = viewCamera.WorldToScreenPoint(worldPosition);
        if (position.z <= 0f || position.x < 0f || position.x > Screen.width || position.y < 0f || position.y > Screen.height) return;

        var x = position.x - widthPixels * 0.5f;
        var y = Screen.height - position.y - heightPixels;
        var oldColor = GUI.color;
        GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
        GUI.DrawTexture(new Rect(x, y, widthPixels, heightPixels), Texture2D.whiteTexture);
        var fraction = Mathf.Clamp01((float)enemy.CurrentHealth / enemy.MaxHealth);
        GUI.color = new Color(0.88f, 0.16f, 0.12f);
        GUI.DrawTexture(new Rect(x + 1f, y + 1f, (widthPixels - 2f) * fraction, heightPixels - 2f), Texture2D.whiteTexture);
        GUI.color = oldColor;
    }
}
