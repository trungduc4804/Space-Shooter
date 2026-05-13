using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "SpaceShooter/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Stats")]
    public float maxHp = 100f;
    public float moveSpeed = 2.5f;
    
    [Header("Behavior")]
    [Tooltip("Thời gian dừng lại để xả đạn (giây)")]
    public float stopDuration = 8f;
    
    [Header("Combat")]
    [Tooltip("Thời gian giữa 2 lần bắn (giây)")]
    public float fireInterval = 5f;
    public GameObject bulletPrefab;
    [Tooltip("Tốc độ bay của đạn so với tốc độ di chuyển của tàu")]
    public float bulletSpeedMultiplier = 2f;
    
    [Header("Rewards")]
    public int scoreValue = 25;
    [Range(0f, 1f)] public float powerupDropRate = 0.07f; // 7% by default
}
