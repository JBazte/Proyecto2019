﻿
using UnityEngine;
using UnityEngine.AI;

public class MedicController : MonoBehaviour
{

    [SerializeField]
    protected Vector3 home;
    [SerializeField]
    private CharacterStats target;
    [SerializeField]
    protected float interactRadious;
    [SerializeField]
    private float healing;
    [SerializeField]
    private float healDelay;
    [SerializeField]
    private LayerMask healingMask;
    [SerializeField]
    private float healingDetectRadious;

    protected bool checkHealing = true;
    protected AllyController controller;
    protected NavMeshAgent agent;
    private float lastHeal;

    protected void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (target != null)
        {
            SetTarget(target);
        }
        agent.stoppingDistance = interactRadious;
        controller = GetComponent<AllyController>();
    }
    public void SetTarget(CharacterStats target)
    {

        if (healingMask == (healingMask | (1 << target.gameObject.layer)))
        {
            agent.SetDestination(target.transform.position);
            controller.SetPrioirityPoint(target.transform.position);
            this.target = target;
            controller.PriorityMoving = true;

        }
    }

    protected void Update()
    {
        lastHeal -= Time.deltaTime;
        if (target != null)
        {
            Heal();
        }
        if (checkHealing)
            CheckHealing();
    }

    private void Heal()
    {
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance <= interactRadious + 0.1f)
        {
            if (lastHeal < 0)
            {
                lastHeal = 1 / healDelay;
                CharacterStats targetStats = target.GetComponent<CharacterStats>();
                if (targetStats != null)
                {
                    bool hasFinished = targetStats.HealDamage(healing);
                    if (hasFinished)
                    {
                        ReturnHome();
                        controller.PriorityMoving = false;
                    }
                }
            }
        }
    }

    protected void ReturnHome()
    {
        agent.isStopped = false;

        controller.SetPoint(home);



    }
    private void CheckHealing()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, healingDetectRadious, healingMask);
        float closestDistance = int.MaxValue;
        CharacterStats cs = null;
        foreach (var enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                var characterstat = enemy.GetComponent<CharacterStats>();
                if (characterstat != null)
                {

                    if (characterstat.CurrentHealth < characterstat.MaxHealth)
                    {
                        closestDistance = distance;
                        cs = characterstat;
                    }
                }
            }
            if (closestDistance != int.MaxValue)
            {
                SetTarget(cs);
                home = transform.position;
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRadious);
        Gizmos.DrawWireSphere(transform.position, healingDetectRadious);
    }


}
