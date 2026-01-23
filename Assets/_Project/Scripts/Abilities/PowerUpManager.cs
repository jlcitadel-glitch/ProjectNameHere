using UnityEngine;
using System.Collections.Generic;

public class PowerUpManager : MonoBehaviour
{
    private HashSet<PowerUpType> unlockedPowerUps = new HashSet<PowerUpType>();

    public void UnlockPowerUp(PowerUpType type)
    {
        unlockedPowerUps.Add(type);
    }

    public bool HasPowerUp(PowerUpType type)
    {
        return unlockedPowerUps.Contains(type);
    }

    public HashSet<PowerUpType> GetAllUnlockedPowerUps()
    {
        return new HashSet<PowerUpType>(unlockedPowerUps);
    }
}