using Godot;
using System;

public partial class Health : Node
{
    public float health = 200f;
    public int maxHealth = 200;

    public float armor = 100f;
    public int maxArmor = 100;

    [Export] public HSlider healthSlider;
    [Export] public HSlider armorSlider;

    public float healthRegenRate = 0.5f;
    public float armorRegenRate = 15.5f;
    public float healthRegenDelay = 15f;
    public float armorRegenDelay = 3f;
    private float healthRegenTimer = 0f;
    private float armorRegenTimer = 0f;

    public override void _Ready()
    {
        health = maxHealth;
        armor = maxArmor;

        healthSlider = GetNode<HSlider>("/root/Main/Game UI/Panel/HealthSlider");
        armorSlider = GetNode<HSlider>("/root/Main/Game UI/Panel/ArmorSlider");
    }


    public override void _Process(double delta)
    {
        UpdateSliders();
        Regeneration(delta);
        RegeneTimer();

        if (Input.IsActionJustPressed("damage"))
        {
            TakeDamage(10f);
        }
    }

    public void Regeneration(double delta)
    {
        if (health < maxHealth)
        {
            healthRegenTimer += (float)delta;
            if (healthRegenTimer >= healthRegenDelay)
            {
                health += healthRegenRate * (float)delta;
                if (health > maxHealth)
                {
                    health = maxHealth;
                }
            }
        }

        if (armor < maxArmor)
        {
            armorRegenTimer += (float)delta;
            if (armorRegenTimer >= armorRegenDelay)
            {
                armor += armorRegenRate * (float)delta;
                if (armor > maxArmor)
                {
                    armor = maxArmor;
                }
            }
        }
    }

    private void UpdateSliders()
    {
        healthSlider.MaxValue = maxHealth;
        armorSlider.MaxValue = maxArmor;

        if (healthSlider != null)
        {
            healthSlider.Value = health;
        }
        if (armorSlider != null)
        {
            armorSlider.Value = armor;
        }
    }

    private bool takenDamage = false;

    private void RegeneTimer()
    {
        if (takenDamage)
        {
            healthRegenTimer = 0f;
            armorRegenTimer = 0f;
            takenDamage = false;
        }
    }

    private void TakeDamage(float damage)
    {
        takenDamage = true;

        if (armor > 0)
        {
            float remainingDamage = damage - armor;
            armor -= damage;
            health -= damage / 10f;

            if (armor < 0)
            {
                armor = 0;
            }

            if (remainingDamage > 0)
            {
                health -= remainingDamage;
            }
        }
        else
        {
            health -= damage;
        }

        if (health < 0)
        {
            health = 0;
        }
    }

}
