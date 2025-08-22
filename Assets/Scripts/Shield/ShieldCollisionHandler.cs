using System.Collections.Generic;
using UnityEngine;

public class ShieldCollisionHandler : MonoBehaviour
{
    [Tooltip("需要与护盾发生交互的物体标签")]
    public List<string> targetTags = new List<string> { "Obstacle", "Collectible" };

    [Header("护盾属性")]
    [Tooltip("护盾最大生命值")]
    public float maxShieldHealth = 100f;
    [Tooltip("护盾当前生命值（运行时自动更新）")]
    [SerializeField] public float currentShieldHealth;
    [Tooltip("护盾伤害系数（影响最终受到的伤害值）")]
    public float shieldDamageCoefficient = 0.8f;

    [Header("反弹配置")]
    [Tooltip("反弹速度倍率：1=保持原速度大小反向，>1=增强反向速度，<1=减弱反向速度")]
    public float bounceSpeedMultiplier = 5f;

    [HideInInspector] public GameObject hitEffectPrefab;
    private GameObject bounceEffectPrefab;
    private bool showDebugInfo = true;

    // 事件声明
    public System.Action<float> onShieldHealthChanged;
    public System.Action onShieldDestroyed;

    private HookSystem hookSystem;
    private Rigidbody2D shieldRigidbody;
    private float shieldMass; // 护盾（或飞船）的总质量

    private void Start()
    {
        currentShieldHealth = maxShieldHealth;
        hookSystem = HookSystem.Instance;
        shieldRigidbody = GetComponentInParent<Rigidbody2D>();
        
        // 初始化护盾质量
        if (hookSystem != null)
            shieldMass = hookSystem.spaceShipMass;
        else if (shieldRigidbody != null)
            shieldMass = shieldRigidbody.mass;
        else
            shieldMass = 100f; // 默认质量（避免计算错误）

        if (shieldRigidbody == null)
        {
            Debug.LogWarning("护盾未找到Rigidbody2D组件，碰撞物理计算将受影响");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 过滤不相关标签的物体
        if (!targetTags.Contains(collision.gameObject.tag)) return;

        // 获取碰撞物的CollectibleObject组件
        CollectibleObject collideObject = collision.gameObject.GetComponent<CollectibleObject>();
        if (collideObject == null)
        {
            if (showDebugInfo)
                Debug.LogWarning($"碰撞物 {collision.gameObject.name} 缺少CollectibleObject组件，忽略碰撞");
            return;
        }

        // 忽略已销毁状态的物体
        if (collideObject.isDestroyedState()) return;

        // 执行碰撞逻辑（伤害计算 + 反向反弹）
        HandleShieldCollision(collideObject, collision);
    }

    private void HandleShieldCollision(CollectibleObject collideObject, Collision2D collision)
    {
        if (shieldRigidbody == null) return;

        // 原有逻辑：伤害计算
        float objectMass = collideObject.mass;
        float reducedMass = (shieldMass * objectMass) / (shieldMass + objectMass);
        Vector2 relativeVelocity = collision.relativeVelocity;
        float relativeSpeed = relativeVelocity.magnitude;
        float effectiveMomentum = reducedMass * relativeSpeed;
        float shieldDamage = effectiveMomentum * collideObject.damageCoefficient * shieldDamageCoefficient;

        if (showDebugInfo)
        {
            Debug.Log($"=== 护盾碰撞计算 ===");
            Debug.Log($"碰撞物: {collideObject.name} (质量: {objectMass})");
            Debug.Log($"护盾质量: {shieldMass}, 约化质量: {reducedMass:F2}");
            Debug.Log($"相对速度: {relativeSpeed:F2}, 护盾伤害: {shieldDamage:F2}");
        }

        // 反向反弹逻辑（核心修改）
        ReverseDirectionBounce(collideObject);

        // 应用伤害到护盾
        TakeDamage(shieldDamage, collision.GetContact(0).point);
    }

    // 核心：将物体反弹方向改为原速度的反向
    private void ReverseDirectionBounce(CollectibleObject collideObject)
    {
        // 获取碰撞物的刚体
        Rigidbody2D objRb = collideObject.GetComponent<Rigidbody2D>();
        if (objRb == null)
        {
            if (showDebugInfo)
                Debug.LogWarning($"碰撞物 {collideObject.name} 缺少Rigidbody2D组件，无法反弹！");
            return;
        }

        // 保存原速度用于调试
        Vector2 originalVelocity = objRb.velocity;

        // 计算反向速度（原速度 × -1 × 倍率）
        Vector2 reversedVelocity = originalVelocity * -1f * bounceSpeedMultiplier;

        // 应用反向速度
        objRb.velocity = reversedVelocity;

        // 调试信息
        if (showDebugInfo)
        {
            Debug.Log($"=== 反向反弹信息 ===");
            Debug.Log($"物体: {collideObject.name}");
            Debug.Log($"原速度: {originalVelocity:F2} → 反向后速度: {reversedVelocity:F2}");
            Debug.Log($"速度大小变化: {originalVelocity.magnitude:F2} → {reversedVelocity.magnitude:F2}");
        }
    }

    private void TakeDamage(float damage, Vector2 hitPoint)
    {
        // 减少护盾生命值
        currentShieldHealth = Mathf.Max(0, currentShieldHealth - damage);
        
        // 触发生命值变化事件
        onShieldHealthChanged?.Invoke(currentShieldHealth);

        // 播放击中特效
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
            Destroy(effect, 1f);
        }

        // 护盾失效逻辑
        if (currentShieldHealth <= 0)
        {
            onShieldDestroyed?.Invoke();
        }
    }
}
