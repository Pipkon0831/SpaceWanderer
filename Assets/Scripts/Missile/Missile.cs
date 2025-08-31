using UnityEngine;
using System.Collections.Generic;

public class Missile : MonoBehaviour
{
    [Header("飞弹属性")]
    [Tooltip("爆炸范围半径")]
    public float explosionRadius = 2f;

    [Tooltip("最大飞行距离")]
    public float maxRange = 20f;

    [Tooltip("爆炸伤害值")]
    public float explosionDamage = 50f;  // 新增：爆炸伤害

    [Header("导弹大小")]
    [Tooltip("导弹长度")]
    public float capsuleLength = 1f;

    [Tooltip("导弹半径")]
    public float capsuleRadius = 0.3f;

    // 可触发爆炸的目标标签列表（可根据需求添加更多标签，如"Enemy"）
    private List<string> damageableTags = new List<string> { "Obstacle", "Collectible" };

    // 胶囊体方向（水平或垂直）
    private CapsuleDirection2D capsuleDirection = CapsuleDirection2D.Horizontal;

    // 组件引用
    private Rigidbody2D rb;
    private CapsuleCollider2D missileCollider;

    // 运行时参数
    private float speed;
    private Vector2 spawnPosition;
    private Vector2 fireDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        missileCollider = GetComponent<CapsuleCollider2D>();
        spawnPosition = transform.position;

        if (missileCollider == null)
        {
            missileCollider = gameObject.AddComponent<CapsuleCollider2D>();
        }

        missileCollider.size = new Vector2(capsuleLength, capsuleRadius * 2);
        missileCollider.direction = capsuleDirection;
        missileCollider.isTrigger = true;
    }

    public void Initialize(float missileSpeed, Vector2 direction)
    {
        speed = missileSpeed;
        fireDirection = direction;
        rb.velocity = fireDirection * speed;
    }

    void Update()
    {
        if (Vector2.Distance(transform.position, spawnPosition) > maxRange)
        {
            Explode();
        }
    }

    // 核心修改：优化碰撞检测逻辑，确保触发爆炸，增加调试日志
    void OnTriggerEnter2D(Collider2D other)
    {
        // 打印碰撞信息，方便排查标签/物体匹配问题
        Debug.Log($"导弹碰撞到：{other.gameObject.name} | 标签：{other.tag}", other.gameObject);

        // 1. 碰撞到可伤害目标，直接爆炸
        if (damageableTags.Contains(other.tag))
        {
            Explode();
        }
        // 2. 碰撞到非目标物体（如地形、墙壁）也强制爆炸（避免导弹穿透）
        else
        {
            // 排除不需要触发爆炸的标签（如玩家，可根据需求调整）
            if (!other.CompareTag("Player"))
            {
                Explode();
            }
        }
    }

    void Explode()
    {
        AudioManager.Instance.PlaySoundEffect(0);
        DrawExplosionRange();
        DamageObjectsInRange();
        Destroy(gameObject);
    }

    // 从直接销毁改为造成伤害
    private void DamageObjectsInRange()
    {
        Collider2D[] collidersInRange = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D collider in collidersInRange)
        {
            if (damageableTags.Contains(collider.tag))
            {
                // 尝试获取CollectibleObject组件
                CollectibleObject target = collider.GetComponent<CollectibleObject>();
                if (target != null)
                {
                    // 对目标造成伤害，伤害值为设定的爆炸伤害
                    target.TakeDamage(explosionDamage, transform.position);
                }
            }
        }
    }

    // 以下方法保持不变
    void DrawExplosionRange()
    {
        GameObject indicator = new GameObject("ExplosionRange");
        indicator.transform.position = transform.position;

        MeshRenderer renderer = indicator.AddComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.color = new Color(1f, 0.3f, 0.3f, 0.3f);

        MeshFilter meshFilter = indicator.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateCircleMesh(explosionRadius);

        int effectsLayer = LayerMask.NameToLayer("Effects");
        indicator.layer = effectsLayer != -1 ? effectsLayer : 0;

        Destroy(indicator, 1f);
    }

    private Mesh CreateCircleMesh(float radius)
    {
        Mesh mesh = new Mesh();
        const int segments = 32;
        Vector3[] vertices = new Vector3[segments + 1];
        vertices[0] = Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * 2 * Mathf.PI;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
        }

        int[] triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 2 > segments) ? 1 : i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }
}