using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData
{
    public int Food { get; private set; }
    public int Water { get; private set; }
    public int Hull { get; private set; }
    public int Gold { get; private set; }

    public PlayerData(int startFood, int startWater, int startHull, int startGold)
    {
        Food = startFood;
        Water = startWater;
        Hull = startHull;
        Gold = startGold;
    }


    /// <param name="foodCost">消耗食物量</param>
    /// <param name="waterCost">消耗淡水量</param>

    public bool ConsumeResources(int foodCost, int waterCost)
    {
        if (Food >= foodCost && Water >= waterCost)
        {
            Food -= foodCost;
            Water -= waterCost;

            EventCenter.BroadcastResourceChanged(Food, Water, Hull, Gold);
            return true;
        }

        return false;
    }


    public void AddResources(int food, int water, int hull, int gold)
    {
        Food += food;
        Water += water;
        Hull += hull;
        Gold += gold;

        EventCenter.BroadcastResourceChanged(Food, Water, Hull, Gold);
    }
}