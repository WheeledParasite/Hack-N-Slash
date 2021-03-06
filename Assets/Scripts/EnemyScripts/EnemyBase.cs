using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random=UnityEngine.Random;

public class EnemyBase : MonoBehaviour
{
    [SerializeField, Tooltip("Enemy Health (hitpoints)")]
    public int health = 15;
    [SerializeField, Tooltip("Knockback Multiplier")]
    float knockbackFactor = 0.2f;

    public bool isBlocking = false;

    [SerializeField]
    int hitQueueSize = 5;
    private int currentHits = 0;
    private Queue<int> hitQueue = new Queue<int>();
    Rigidbody2D rigidBody; 
    BoxCollider2D enemyWeapon;
    CapsuleCollider2D enemyCollider;
    Animator animator;
    public static event Action<GameObject> onEnemyDeath;
    public static event Action onEnemyBlocked;

    [SerializeField, Tooltip("Block after this many hits in a row, if enemy can block")] int numBlockHits = 2;
    private Transform playerTrans;

    private int randomNumber;

    // Start is called before the first frame update
    void Start()
    {
        enemyWeapon = transform.GetChild(0).GetComponent<BoxCollider2D>();
        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerTrans = GameObject.Find("PlayerV4").GetComponent<Transform>();
        enemyCollider = GetComponent<CapsuleCollider2D>();
        StartCoroutine(blockTimer());
    }
    private void OnEnable() { // Watches for when the enemy gets hit
        WeaponBase.onEnemyDamaged += onEnemyHit;
    } 
    private void OnDisable() {
        WeaponBase.onEnemyDamaged -= onEnemyHit;
    } 

    // when we receive the onEnemyHit event, we do knockback + damage.
    private void onEnemyHit(float damage, GameObject enemyObject) 
    {
        // check to see if the enemy that was hit is this enemy.
        if (this != null && this.gameObject == enemyObject) {
            if(isBlocking)
            {
                onEnemyBlocked?.Invoke();
                Debug.Log("Enemy blocked hit");
                animator.SetTrigger("riposted");
            }
            else
            {
                animator.ResetTrigger("riposted");
                health -= (int)Math.Round(damage);
                if(health <= 0) {
                    animator.SetTrigger("death");
                    onEnemyDeath?.Invoke(this.gameObject);
                    enemyCollider.enabled = false;
                    // the death animation should call the public death function
                } else {
                    animator.SetTrigger("hit");
                    currentHits += 1;
                    //Calculate knockback force
                    StartCoroutine(FakeAddForceMotion(damage*knockbackFactor));
                }
            }
        }
    }

    // This function adds a fake force to a Kinematic body
    public IEnumerator FakeAddForceMotion(float forceAmount)
    {
        float i = 0.01f;
        while (forceAmount > i)
        {
            if (playerTrans.localScale.x > 0)
            {
                rigidBody.velocity = new Vector2(forceAmount / i, rigidBody.velocity.y); // !! For X axis positive force
            }
            else
            {
                rigidBody.velocity = new Vector2(-forceAmount / i, rigidBody.velocity.y); // !! For X axis positive force
            }
            
            i = i + Time.deltaTime;
            yield return new WaitForEndOfFrame();      
        }
        rigidBody.velocity = Vector2.zero;
        yield return null;
    }

    public void callDeath() {
        
        Destroy(this.gameObject);
    }

    //This functions performs a block if the number of times the player has hit the enemy exceeds 2
    private IEnumerator blockTimer() { 
        while(health > 0) {
            yield return new WaitForSeconds(1f);     // wait designated time 
            hitQueue.Enqueue(currentHits);
            if(hitQueue.Count > hitQueueSize)
                hitQueue.Dequeue();
            int[] hitArray = hitQueue.ToArray();
            int sumHit = 0;
            foreach(int hit in hitArray){
                sumHit += hit;
            }  

            currentHits = 0;
            if (sumHit >= numBlockHits){
                animator.SetTrigger("block");
                Debug.Log("Enemy block");
                hitQueue.Clear();
            }
        }
    }    
}
