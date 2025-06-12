using System;
using Godot;

public partial class Health : Node
{
    public float health = 200f;
    public int maxHealth = 200;

    public float armor = 100f;
    public int maxArmor = 100;

    [Export] public HSlider healthSlider;
    [Export] public HSlider armorSlider;

    public float healthRegenRate = 2.5f;
    public float armorRegenRate = 15.5f;
    public float healthRegenDelay = 15f;
    public float armorRegenDelay = 3f;
    private float healthRegenTimer = 0f;
    private float armorRegenTimer = 0f;
    private bool takenDamage = false;

    private StyleBoxEmpty emptyGrabberStyle = new StyleBoxEmpty();
    private StyleBox armorGrabberStyle;
    private StyleBox healthGrabberStyle;
    private bool stylesInitialized = false;

    public override void _Ready()
    {
        health = maxHealth;
        armor = maxArmor;

        SaveGrabberStyle();
        UpdateGrabberVisibility();
    }

    private void SaveGrabberStyle()
    {
        if (healthSlider != null && armorSlider != null)
        {
            healthGrabberStyle = healthSlider.GetThemeStylebox("grabber_area");
            armorGrabberStyle = armorSlider.GetThemeStylebox("grabber_area");
            stylesInitialized = true;
        }
    }

    public override void _Process(double delta)
    {
        UpdateSliders();
        StartRegeneration(delta);
        RegeneTimer();

        if (Input.IsActionJustPressed("damage"))
        {
            TakeDamage(30f);
        }
    }

    private void UpdateSliders()
    {
        if (healthSlider != null)
        {
            healthSlider.MaxValue = maxHealth;
            healthSlider.Value = health;
        }

        if (armorSlider != null)
        {
            armorSlider.MaxValue = maxArmor;
            armorSlider.Value = armor;
        }
    }

    public void StartRegeneration(double delta)
    {
        if (health < maxHealth)
        {
            healthRegenTimer += (float)delta;
            if (healthRegenTimer >= healthRegenDelay)
            {
                health += healthRegenRate * (float)delta;
                if (health > maxHealth)
                    health = maxHealth;
            }
        }

        if (armor < maxArmor)
        {
            armorRegenTimer += (float)delta;
            if (armorRegenTimer >= armorRegenDelay)
            {
                armor += armorRegenRate * (float)delta;
                if (armor > maxArmor)
                    armor = maxArmor;
            }
        }

        UpdateGrabberVisibility();
    }

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
            int randomRange = (int)GD.RandRange(5, 10);
            float remainingDamage = damage - armor;
            armor -= damage;
            health -= damage / randomRange;

            if (armor < 0)
                armor = 0;

            if (remainingDamage > 0)
                health -= remainingDamage;
        }
        else
        {
            health -= damage;
        }

        if (health < 0)
            health = 0;

        UpdateGrabberVisibility();
    }

    private void UpdateGrabberVisibility()
    {
        if (!stylesInitialized)
        {
            SaveGrabberStyle();
            if (!stylesInitialized) return;
        }

        if (health <= 0)
        {
            healthSlider.AddThemeStyleboxOverride("grabber_area", emptyGrabberStyle);
        }
        else
        {
            healthSlider.AddThemeStyleboxOverride("grabber_area", healthGrabberStyle);
        }

        if (armor <= 0)
        {
            armorSlider.AddThemeStyleboxOverride("grabber_area", emptyGrabberStyle);
        }
        else
        {
            armorSlider.AddThemeStyleboxOverride("grabber_area", armorGrabberStyle);
        }
    }
}
