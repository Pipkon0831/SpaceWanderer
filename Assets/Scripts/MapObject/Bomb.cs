using UnityEngine;
using System.Collections.Generic;

public class Bomb : MonoBehaviour
{
    [Header("炸弹属性")]
    [Tooltip("爆炸范围半径")]
    public float explosionRadius = 2f;

    [Tooltip("爆炸伤害值")]
    public float explosionDamage = 50f;
    
    // 可被伤害的标签列表
    private List<string> damageableTags = new List<string> { "Obstacle", "Collectible" };


    void OnTriggerEnter2D(Collider2D other)
    {
        // 任何碰撞都会触发爆炸，不做任何过滤
        Explode();
    }


    void Explode()
    {
        // 播放爆炸音效（避免空引用）
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySoundEffect(0);
        }
        
        DrawExplosionRange();
        DamageObjectsInRange();
        Destroy(gameObject);
    }


    private void DamageObjectsInRange()
    {
        Collider2D[] collidersInRange = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D collider in collidersInRange)
        {
            if (damageableTags.Contains(collider.tag))
            {
                CollectibleObject target = collider.GetComponent<CollectibleObject>();
                if (target != null && !target.isDestroyedState())
                {
                    target.TakeDamage(explosionDamage, transform.position);
                }
            }
        }
    }


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
