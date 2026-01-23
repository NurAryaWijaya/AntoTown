using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Economy")]
    public int money = 1000;
    public float tickInterval = 1f;

    [Header("Stats")]
    public int totalPopulation;
    [Range(0, 100)] public float happiness = 50f;

    [Header("Income")]
    public float incomeInterval = 5f;
    float incomeTimer = 0f;
    int lastIncome = 0;

    [Header("UI")]
    public TMP_Text moneyText;
    public TMP_Text populationText;
    public TMP_Text happinessText;
    public TMP_Text incomeText;

    List<Building> buildings = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        UpdateUI();
        StartCoroutine(TickLoop());
    }

    IEnumerator TickLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(tickInterval);
            Tick();
        }
    }

    void Tick()
    {
        incomeTimer += tickInterval;

        int pop = 0;
        float happinessSum = 0f;
        int residentialCount = 0;

        foreach (var b in buildings)
        {
            if (!b.IsOperational()) continue;

            if (b.buildingType == BuildingType.Residential)
            {
                pop += b.currentPopulation;

                float h = b.baseHappiness;

                foreach (var other in buildings)
                {
                    if (!other.hasArea) continue;

                    int r = other.areaRadiusInTiles;
                    Vector2 center = other.GetCenterGridPosition();
                    Vector2 pos = b.GetCenterGridPosition();

                    float dx = pos.x - center.x;
                    float dz = pos.y - center.y;

                    if (dx * dx + dz * dz <= r * r)
                    {
                        h += other.happinessEffect;
                    }
                }

                happinessSum += Mathf.Clamp(h, 0, 100);
                residentialCount++;
            }

            b.Upgrade(tickInterval);
        }

        totalPopulation = pop;

        happiness = residentialCount > 0
            ? happinessSum / residentialCount
            : 50f;

        if (incomeTimer >= incomeInterval)
        {
            incomeTimer = 0f;
            CalculateIncome();
        }

        UpdateUI();
    }

    void CalculateIncome()
    {
        int income = 0;

        foreach (var b in buildings)
        {
            if (!b.IsOperational()) continue;
            income += Mathf.RoundToInt(b.incomePerTick);
        }

        money += income;
        lastIncome = income;
    }

    void UpdateUI()
    {
        if (moneyText) moneyText.text = $"{money}";
        if (populationText) populationText.text = $"{totalPopulation}";
        if (happinessText) happinessText.text = $"{Mathf.RoundToInt(happiness)}%";
        if (incomeText) incomeText.text = $"(+{lastIncome}) / 5s";
    }


    // ===================== API =====================

    public bool CanAfford(int cost) => money >= cost;

    public void SpendMoney(int amount)
    {
        money -= amount;
        UpdateUI();
    }

    public void RegisterBuilding(Building b)
    {
        if (!buildings.Contains(b))
            buildings.Add(b);
    }

    public void UnregisterBuilding(Building b)
    {
        buildings.Remove(b);
    }
}
