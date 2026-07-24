using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityHealth : MonoBehaviour
{
    EntityStat stat;

    public float health, maxHealth;

    public bool isDeath;

    public struct Context
    {
        public float damage;
        public EntityHealth attacker;
        public bool canceled;
        public float karmaAmount;
    }

    List<Action<Context>> onDamageEv = new();
    List<Action<Context>> onGiveDamageEv = new();
    List<Action<Context>> onDeathEv = new();

    public delegate bool DamageInterceptor(float incomingDamage, EntityHealth attacker);
    List<DamageInterceptor> damageInterceptors = new();

    public void AddDamageInterceptor(DamageInterceptor interceptor)
    {
        damageInterceptors.Add(interceptor);
    }

    void Start()
    {
        stat = GetComponent<EntityStat>();
        ResetHealth();
    }

    public void ResetHealth()
    {
        health = maxHealth;
    }

    public void OnDamage(Action<Context> action)
    {
        onDamageEv.Add(action);
    }

    public void OnGiveDamage(Action<Context> action)
    {
        onGiveDamageEv.Add(action);
    }

    public void OnDeath(Action<Context> action)
    {
        onDeathEv.Add(action);
    }
    public void ReduceHealth(float amount)
    {
        if (isDeath)
            return;

        health -= amount;

        if (health <= 0)
        {
            health = 0;
            isDeath = true;

            Context ctx = new Context();
            foreach (var c in onDeathEv)
            {
                c.Invoke(ctx);
            }
        }
    }

    public void GetDamage(float damage, EntityHealth attacker = null, bool isKarma = false)
    {
        if (isDeath)
            return;

        foreach (var interceptor in damageInterceptors)
        {
            if (interceptor(damage, attacker))
                return;
        }

        float critPer = 0, critMul = 0, inc = 0;

        if (attacker != null)
        {
            critPer = attacker.stat.GetResultValue("critPer");
            critMul = attacker.stat.GetResultValue("critMul");
            inc = attacker.stat.GetResultValue("increaseDamage");
        }

        float dmg = damage * (1 + stat.GetResultValue("hurtDamage") / 100) * (1 + inc / 100);

        if (UnityEngine.Random.Range(0, 100) <= critPer)
            dmg *= 1 + critMul / 100;

        dmg -= stat.GetResultValue("defense");

        if (dmg < 0)
            dmg = 0;

        Context ctx = new Context();
        ctx.damage = dmg;
        ctx.attacker = attacker;
        ctx.karmaAmount = isKarma ? dmg : 0;

        foreach (var c in onDamageEv)
        {
            c.Invoke(ctx);
        }

        if (attacker != null)
        {
            foreach (var c in attacker.onGiveDamageEv)
            {
                c.Invoke(ctx);
            }
        }

        if (ctx.canceled)
        {
            return;
        }

        if (!isKarma)
            health -= dmg;

        if (health <= 0)
        {
            isDeath = true;

            foreach (var c in onDeathEv)
            {
                c.Invoke(ctx);
            }
        }
    }
}