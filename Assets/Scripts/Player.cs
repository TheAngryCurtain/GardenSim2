using UnityEngine;
using System.Collections;

[System.Serializable]
public class Player
{
	public System.Action<int> OnWalletValueChanged;
	public System.Action<int, int> OnStaminaValueChanged;
	public System.Action<int, int, int> OnTotalXPChanged;

	private int _wallet;
	private int _level;
	private int _totalXP;
	private int _maxStamina;
	private int _currentStamina;

	public Player(int maxStamina)
	{
		_wallet = 0;
		_level = 1;
		_totalXP = 0;
		_maxStamina = maxStamina;
		_currentStamina = _maxStamina;

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

	public void ModifyStamina(int amount)
	{
		_currentStamina += amount;
		if (OnStaminaValueChanged != null)
		{
			OnStaminaValueChanged(_currentStamina, _maxStamina);
		}
	}

	public void ModifyWallet(int amount)
	{
		_wallet += amount;
		if (OnWalletValueChanged != null)
		{
			OnWalletValueChanged(_wallet);
		}
	}

	public void ModifyTotalXP(int amount)
	{
		int xpToNextLevel = GetXPForLevel(_level + 1);
		_totalXP += amount;
		if (_totalXP >= xpToNextLevel)
		{
			_level += 1;
		}

		if (OnTotalXPChanged != null)
		{
			OnTotalXPChanged(_level, _totalXP, xpToNextLevel);
		}
	}

	private int GetXPForLevel(int level)
	{
		return Mathf.RoundToInt(100 * Mathf.Pow(level, 1.5f));
	}
}
