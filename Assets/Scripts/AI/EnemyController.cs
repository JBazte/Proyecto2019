﻿using UnityEngine.AI;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent), typeof(CharacterStats))]
public class EnemyController : Enemy
{

    [SerializeField]
    private Vector3 movePoint;
    [SerializeField]
    private LayerMask detectMask;
    [SerializeField]
    private LayerMask attackMask;
    [SerializeField]
    private Transform shotSpawnPosition;
    [SerializeField]
    private Shot shotInstance;


    [SerializeField]
    private float detectRadious;
    [SerializeField]
    private float attackRadious;
    [SerializeField]
    private float shotForce;


    const float shotAccuaracy = 3;

    private NavMeshAgent agent;
    private CharacterStats stats;
    private CharacterStats target;
    private float lastAttack;
    private int currentAmmo;
    private bool isReloading;
    public bool PriorityMoving
    {
        set
        {
            if (value)
                SetTarget(null);
            value = priorityMoving;

        }
        get
        {
            return priorityMoving;
        }

    }

    private bool priorityMoving;

    bool ischasingEnemy;
    bool isMoving;
    [SerializeField]
    private Animator animatorController;
    private float lastLookAround;

    protected void Start()
    {
        if (movePoint != Vector3.zero)
        {
            SetPoint(movePoint);
        }
        else
        {
            agent.isStopped = true;
            movePoint = transform.position;
        }
        agent.speed = stats.Speed;
        agent.stoppingDistance = attackRadious;
        currentAmmo = stats.MaxAmmo;
        lastLookAround = 3f;
    }
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<CharacterStats>();
        animatorController = GetComponentInChildren<Animator>();

    }

    public void ReturnHome()
    {
        SetPoint(movePoint);
    }
    public virtual void SetPoint(Vector3 position)
    {
        this.movePoint = position;
        agent.SetDestination(position);
        target = null;
        ischasingEnemy = false;
        agent.stoppingDistance = 0;
        agent.isStopped = false;
    }

    public void SetTarget(CharacterStats target)
    {

        this.target = target;
        agent.stoppingDistance = attackRadious;
        if (target != null)
            agent.SetDestination(target.transform.position);
        ischasingEnemy = true;

    }
    private void Update()
    {
        lastLookAround -= Time.deltaTime;
        lastAttack -= Time.deltaTime;
        if (!PriorityMoving)
        {

            SearchTargets();
            if (ischasingEnemy)
                ChaseTargets();

        }
        else
        {
            if (agent.remainingDistance < .3f)
            {
                PriorityMoving = false;
            }
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            stats.TakeDamage(10f);
        }

        if (agent.velocity != Vector3.zero)
        {
            animatorController.SetBool("isRunning", true);
        }
        else
        {
            animatorController.SetBool("isRunning", false);


        }
        if (lastLookAround <= 0)
        {
            if (!ischasingEnemy)
                animatorController.SetTrigger("isLooking");
            lastLookAround = 3f;
        }



    }

    public void ChangeTarget(EnemyController other)
    {
        Vector3 point = other.movePoint;
        other.SetPoint(movePoint);
        SetPoint(point);
    }

    private void SearchTargets()
    {
        if (Physics.CheckSphere(transform.position, detectRadious, detectMask))
        {
            Collider[] enemies = Physics.OverlapSphere(transform.position, detectRadious, detectMask);
            if (enemies.Length != 0)
            {
                float closestDistance = int.MaxValue;
                int index = 0;
                for (int i = 0; i < enemies.Length; i++)
                {
                    float distance = Vector3.Distance(transform.position, enemies[i].transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        index = i;
                    }
                }
                if (closestDistance != int.MaxValue)
                {
                    CharacterStats enemy = enemies[index].GetComponentInParent<CharacterStats>();
                    SetTarget(enemy);

                }
            }
        }
        else
        {

            SetPoint(movePoint);
            ischasingEnemy = false;
        }
    }

    public void LookAtTarget()
    {
        Quaternion rotation = transform.rotation;
        transform.LookAt(target.transform);
        transform.rotation = new Quaternion(rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w);
    }

    public void ChaseTargets()
    {
        LookAtTarget();

        float distance = Vector3.Distance(transform.position, target.transform.position);

        if (distance <= attackRadious)
        {
            RaycastHit hit;
            if (Physics.SphereCast(shotSpawnPosition.position, 2f, shotSpawnPosition.forward, out hit, 25f))
            {
                Shoot();
                if (attackMask == (attackMask | (1 << hit.transform.gameObject.layer)))
                {
                    agent.isStopped = true;

                }
                else
                {
                    agent.isStopped = false;

                }
            }

        }
        else
        {
            agent.isStopped = false;
            animatorController.SetBool("isShooting", false);
        }
    }
    private void Shoot()
    {
        if (lastAttack <= 0)
        {
            if (currentAmmo > 0)
            {
                // Replace with Pool Later
                animatorController.SetBool("isShooting", true);
                currentAmmo--;
                lastAttack = 1 / stats.AttackSpeed;
                Shot instance = Instantiate(shotInstance, shotSpawnPosition.position, transform.rotation);
                instance.transform.rotation = transform.rotation;
                instance.SetShot(stats.Damage, attackMask);
                Vector3 offset = Random.insideUnitSphere * ((100 - stats.Accuracy) / 100) / shotAccuaracy;
                Rigidbody rb = instance.GetComponent<Rigidbody>();
                rb.AddForce((transform.forward + offset) * shotForce, ForceMode.Impulse);
            }
            else
            {
                if (!isReloading)
                    StartCoroutine(Reload());
            }
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(stats.ReloadTime);
        currentAmmo = stats.MaxAmmo;
        isReloading = false;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRadious);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadious);
    }

    public override void Save(GameDataWriter writer)
    {
        base.Save(writer);
        writer.Write(stats.CurrentHealth);
        writer.Write(currentAmmo);
        writer.Write(lastAttack);
        writer.Write(movePoint);
        writer.Write(gameObject.activeSelf);
    }

    public override void Load(GameDataReader reader)
    {
        base.Load(reader);
        stats.HealDamage(reader.ReadFloat() - stats.CurrentHealth);
        currentAmmo = reader.ReadInt();
        lastAttack = reader.ReadFloat();
        Vector3 position = reader.ReadVector3();
        Debug.Log(position + gameObject.name);
        SetPoint(position);
        if (currentAmmo == 0)
        {
            StartCoroutine(Reload());
        }
        gameObject.SetActive(reader.ReadBool());

    }

}
