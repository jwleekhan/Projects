using System;
using System.Collections.Generic;
using UnityEngine;

public class AdditionalEffectManager : MonoBehaviour
{
    public struct AdditionalAbility
    {
        public string additionalAbility { get; }
        public int additionalTarget { get; }
        public float additionalAbilityAmount { get; }
        public float additionalAbilityTime { get; }

        public AdditionalAbility(
            string AdditionalAbility,
            int AdditionalTarget,
            float AdditionalAbilityAmount,
            float AdditionalAbilityTime)
        {
            additionalAbility = AdditionalAbility;
            additionalTarget = AdditionalTarget;
            additionalAbilityAmount = AdditionalAbilityAmount;
            additionalAbilityTime = AdditionalAbilityTime;
        }
    }

    PlayerStat playerStat;

    public void LevelUp(Dictionary<string, object> curLevelData, Dictionary<string, object> nextLevelData)
    {
        if (nextLevelData == null)
            return;

        for (int i = 1; i <= 3; i++)
        {
            AdditionalAbility curAdditionalAbility = ToAdditionalAbilityStruct(curLevelData, i);
            AdditionalAbility nextAdditionalAbility = ToAdditionalAbilityStruct(nextLevelData, i);
            if (string.IsNullOrEmpty(curAdditionalAbility.additionalAbility) || curAdditionalAbility.additionalAbility == nextAdditionalAbility.additionalAbility)
            {
                ApplyAdditionalAbility(curAdditionalAbility, nextAdditionalAbility);
            }
        }
    }

    AdditionalAbility ToAdditionalAbilityStruct(Dictionary<string, object> dict, int i)
    {
        string abilityKey = $"AdditionalAbility{i}";
        string targetKey = $"AdditionalTarget{i}";
        string amountKey = $"AdditionalAbility{i}Amount";
        string timeKey = $"AdditionalAbility{i}Time";

        if (dict == null || !dict.ContainsKey(abilityKey) || !dict.ContainsKey(targetKey)
            || !dict.ContainsKey(amountKey) || !dict.ContainsKey(timeKey))
        {
            return default;
        }

        string abilityString = dict[abilityKey].ToString();
        if (!string.IsNullOrEmpty(abilityString))
        {
            int additionalTarget = int.TryParse(dict[targetKey].ToString(), out int target) ? target : 0;
            float additionalAbilityAmount = float.TryParse(dict[amountKey].ToString(), out float amount) ? amount : 0f;
            float additionalAbilityTime = float.TryParse(dict[timeKey].ToString(), out float time) ? time : 0f;

            AdditionalAbility additionalAbility = new AdditionalAbility(
                abilityString,
                additionalTarget,
                additionalAbilityAmount,
                additionalAbilityTime);

            return additionalAbility;
        }
        return default;
    }

    void ApplyAdditionalAbility(AdditionalAbility curAdditionalAbility, AdditionalAbility nextAdditionalAbility)
    {
        switch (nextAdditionalAbility.additionalAbility)
        {
            case "BUFF_FINALDAMAGE":
                BuffFinalDamage(curAdditionalAbility, nextAdditionalAbility);
                break;
            case "BUFF_SUBCOOLTIME":
                BuffSubCoolTime(curAdditionalAbility, nextAdditionalAbility);
                break;
            case "BUFF_BULLETSIZE":
                BuffBulletSize(curAdditionalAbility, nextAdditionalAbility);
                break;
            case "BUFF_CRIRATE":
                BuffCriRate(curAdditionalAbility, nextAdditionalAbility);
                break;
            case "HEAL_VALUE":
                HealValue(curAdditionalAbility, nextAdditionalAbility);
                break;
            default:
                break;
        }
    }

    void BuffFinalDamage(AdditionalAbility curAdditionalAbility, AdditionalAbility nextAdditionalAbility)
    {
        GameObject player = PlayerManager.Instance.GetPlayer();
        if (player == null || player.GetComponent<PlayerStat>() == null)
            return;
        playerStat = player.GetComponent<PlayerStat>();

        float buffAmount = nextAdditionalAbility.additionalAbilityAmount - curAdditionalAbility.additionalAbilityAmount;
        Debug.Log(buffAmount);

        playerStat.buffFinalDamageRate += buffAmount / 100f;
    }

    void BuffSubCoolTime(AdditionalAbility curAdditionalAbility, AdditionalAbility nextAdditionalAbility)
    {
        GameObject player = PlayerManager.Instance.GetPlayer();
        if (player == null || player.GetComponent<PlayerStat>() == null)
            return;
        playerStat = player.GetComponent<PlayerStat>();

        float buffAmount = nextAdditionalAbility.additionalAbilityAmount - curAdditionalAbility.additionalAbilityAmount;
        Debug.Log(buffAmount);

        playerStat.buffCooldownRateModifier += buffAmount / 100f;
    }

    void BuffBulletSize(AdditionalAbility curAdditionalAbility, AdditionalAbility nextAdditionalAbility)
    {
        GameObject player = PlayerManager.Instance.GetPlayer();
        if (player == null || player.GetComponent<PlayerStat>() == null)
            return;
        playerStat = player.GetComponent<PlayerStat>();

        float buffAmount = nextAdditionalAbility.additionalAbilityAmount - curAdditionalAbility.additionalAbilityAmount;
        Debug.Log(buffAmount);

        playerStat.buffSizeMag += buffAmount / 100f;
    }

    void BuffCriRate(AdditionalAbility curAdditionalAbility, AdditionalAbility nextAdditionalAbility)
    {
        GameObject player = PlayerManager.Instance.GetPlayer();
        if (player == null || player.GetComponent<PlayerStat>() == null)
            return;
        playerStat = player.GetComponent<PlayerStat>();

        float buffAmount = nextAdditionalAbility.additionalAbilityAmount - curAdditionalAbility.additionalAbilityAmount;
        Debug.Log(buffAmount);

        playerStat.buffCriticalChance += buffAmount / 100f;
    }

    void HealValue(AdditionalAbility curAdditionalAbility, AdditionalAbility nextAdditionalAbility)
    {
        GameObject player = PlayerManager.Instance.GetPlayer();
        if (player == null || player.GetComponent<PlayerStat>() == null)
            return;
        playerStat = player.GetComponent<PlayerStat>();

        float healAmount = nextAdditionalAbility.additionalAbilityAmount;
        Debug.Log(healAmount);

        playerStat.heal(healAmount);
    }
}
