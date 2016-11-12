﻿using UnityEngine;
using System.Collections;

[System.Serializable]
public class Player
{
	public System.Action<int, int> OnWalletValueChanged;
	public System.Action<int, int, int> OnStaminaValueChanged;
	public System.Action<object> OnTotalXPChanged;

	private int _wallet;
	private int _level;
	private int _totalXP;
	private int _maxStamina;
	private int _currentStamina;

	public Player(int maxStamina)
	{
		_wallet = 100;
		_level = 1;
		_totalXP = 0;
		_maxStamina = maxStamina;
		_currentStamina = _maxStamina;
	}

    public void Init()
    {
        OnStaminaValueChanged += UIController.Instance.OnPlayerStaminaUpdated;
        OnWalletValueChanged += UIController.Instance.OnPlayerWalletUpdated;
        OnTotalXPChanged += UIController.Instance.OnPlayerXpUpdated;

        RefreshValues();
    }

	public void RefreshValues()
	{
		ModifyStamina(0);
		ModifyWallet(0);
		ModifyTotalXP(0);
	}

    public bool CanAffordAction(int cost)
    {
        return cost <= _wallet;
    }

	public void ModifyStamina(int amount)
	{
		_currentStamina += amount;
		if (OnStaminaValueChanged != null)
		{
			OnStaminaValueChanged(amount, _currentStamina, _maxStamina);
		}
	}

	public void ModifyWallet(int amount)
	{
		_wallet += amount;
		if (OnWalletValueChanged != null)
		{
			OnWalletValueChanged(amount, _wallet);
		}
	}

	public void ModifyTotalXP(int amount)
	{
		int xpToNextLevel = GetXPForLevel(_level);
        int delta = 0;
		_totalXP += amount;
		if (_totalXP >= xpToNextLevel)
		{
			_level += 1;
            delta = _totalXP - xpToNextLevel;
            xpToNextLevel = GetXPForLevel(_level);
		}

		if (OnTotalXPChanged != null)
		{
            object data = new object[] { amount, _level, _totalXP, xpToNextLevel, delta };
			OnTotalXPChanged(data);
		}
	}

	private int GetXPForLevel(int level)
	{
		return Mathf.RoundToInt(100 * Mathf.Pow(level, 1.5f));
	}
}
