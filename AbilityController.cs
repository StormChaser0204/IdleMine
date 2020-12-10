using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AbilityController : SceneSingleton<AbilityController> {

    private SuperstitionConfig _superstitionConfig;
    private AbilityConfig _abilityConfig;
    private List<BaseAbilityData> _abilityDatas;

    private List<AbilityObject> _takedAbilities;
    private List<AbilityObject> _abilities;
    private Dictionary<AbilityCategory, int> _abilityCostModifier;
    private Dictionary<CountryFeatures, int> _featureCount;
    private Dictionary<CountryFeatures, float> _additionalImpose;
    private System.Random _random;
    private int _superstitionCount;
    private int _rollbackCostModifier;
    private int _currentSuperstitionDelay;
    private float _additionalSuperstition;
    private float _additionalRecoilChance;

    public List<AbilityObject> Abilities => _abilities;
    public List<AbilityObject> TakedAbilities => _takedAbilities;
    public int RollbackCostModifier => _rollbackCostModifier;

    protected override void Init() {
        _superstitionConfig = Player.Instance.CurrentArchetype.SuperstitionConfig;
        _abilityConfig = Player.Instance.CurrentArchetype.AbilityConfig;
        if (Player.Instance.SetAbilitiesFromTextFile)
            ParseAbilityConfig(_abilityConfig.AbilityDatas);
        else
            _abilityDatas = _abilityConfig.ParseAbilityData.Abilities;

        _takedAbilities = new List<AbilityObject>();
        _abilities = new List<AbilityObject>();
        _abilityCostModifier = new Dictionary<AbilityCategory, int>();
        _featureCount = new Dictionary<CountryFeatures, int>();
        _additionalImpose = new Dictionary<CountryFeatures, float>();
        _superstitionCount = _superstitionConfig.SuperstitionCount;
        _random = new System.Random();
        _currentSuperstitionDelay = _superstitionConfig.SuperstitionDelay;
        CreateGameAbilities();
        EventManager.Instance.AddEventListener<AbilityEvent>(OnAbilityEvent);
        EventManager.Instance.AddEventListener<TimeEvent>(OnTimeEvent);
        MakeVisibleAbilities();
    }

    private void ParseAbilityConfig(TextAsset abilities) {
        var fmt = new NumberFormatInfo {
            NegativeSign = "-",
            PositiveSign = "+",
            NumberDecimalSeparator = ","
        };

        var sr = new StreamReader(string.Format("/Assets/Resources/Data/AbilitiesText/{0}.txt", abilities));
        var parsedData = new List<BaseAbilityData>();
        while (!sr.EndOfStream) {
            var line = sr.ReadLine();
            var data = line.Split('/');
            if (data[1] == "0") continue;
            var ability = new BaseAbilityData();
            ability.GridIndex = int.Parse(data[0]);
            ability.AbilityID = int.Parse(data[1]);
            ability.BaseCost = int.Parse(data[2]);
            ability.AbilityName = data[3];
            ability.Description = data[4];
            ability.AbilityCategory = EnumExtension.ToEnum<AbilityCategory>(data[5]);
            ability.AbilityType = EnumExtension.ToEnum<AbilityType>(data[6]);
            ability.TypeModifiers = new List<TypeModifier>();
            ability.ImposeModifiers = new List<TypeModifier>();
            ability.GameplayEffects = new List<GameplayEffect>();

            var featuresString = data[7].Split('|');
            var imposeModifierString = data[8].Split('|');
            var gameplayEffectString = data[9].Split('|');
            var visibleAbilitiesString = data[10].Split('|').ToList();
            visibleAbilitiesString.RemoveAt(visibleAbilitiesString.Count - 1);
            var visibleAbilities = new List<int>();
            visibleAbilitiesString.ForEach(a => visibleAbilities.Add(int.Parse(a)));
            var abilityCondition = data[11].Split(':');

            foreach (var feature in featuresString) {
                if (string.IsNullOrEmpty(feature)) continue;
                var featureData = feature.Split(':');
                var featureID = int.Parse(featureData[0]);
                var featureValue = float.Parse(featureData[1], fmt);
                ability.TypeModifiers.Add(new TypeModifier((CountryFeatures)featureID, featureValue));
            }
            foreach (var feature in imposeModifierString) {
                if (string.IsNullOrEmpty(feature)) continue;
                var featureData = feature.Split(':');
                var featureID = int.Parse(featureData[0]);
                var featureValue = float.Parse(featureData[1], fmt);
                ability.ImposeModifiers.Add(new TypeModifier((CountryFeatures)featureID, featureValue));
            }

            foreach (var effect in gameplayEffectString) {
                if (string.IsNullOrEmpty(effect)) continue;
                var effectData = effect.Split(':');
                var effectID = int.Parse(effectData[0]);
                var effectValue = float.Parse(effectData[1], fmt);
                var abilityID = int.Parse(effectData[2]);
                ability.GameplayEffects.Add(new GameplayEffect((GameplayEffectType)effectID, new ValueSelector(ValueType.Fixed, effectValue), (ActiveAbilityType)abilityID));
            }

            var condition = new AbilityCondition();
            var conditionAbilitiesString = abilityCondition[1].Split('|').ToList();
            var conditionAbilities = new List<int>();
            conditionAbilitiesString.RemoveAt(conditionAbilitiesString.Count - 1);
            conditionAbilitiesString.ForEach(a => conditionAbilities.Add(int.Parse(a)));
            condition.AbilityConditionType = EnumExtension.ToEnum<AbilityConditionType>(abilityCondition[0]);
            condition.AbilitiesID = conditionAbilities;
            condition.BelieverCount = int.Parse(abilityCondition[2]);

            ability.MakeVisibleAbilities = visibleAbilities;
            ability.AbilityCondition = condition;
            parsedData.Add(ability);
        }
        _abilityDatas = parsedData;
    }

    protected override void OnDestroy() {
        EventManager.Instance?.RemoveEventListener<AbilityEvent>(OnAbilityEvent);
        EventManager.Instance?.RemoveEventListener<TimeEvent>(OnTimeEvent);
        _abilityDatas.ForEach(a => a.StopListen());
        base.OnDestroy();
    }

    public void Load(AbilityContainer abilityContainer) {
        if (abilityContainer == null) return;
      
        var abilities = abilityContainer;
        _superstitionCount = abilities.SuperstitionCount;
        foreach (var ability in abilities.AbilitySaveDatas) {
            var abilityById = _abilities.FirstOrDefault(a => a.AbilityData.AbilityID == int.Parse(ability.Id));
            if (abilityById == null) continue;

            abilityById.Purchased = ability.Purchased;
            if (ability.Purchased) {
                EventManager.Instance.DispatchEvent(new AbilityEvent(AbilityEventType.BuyAbility, abilityById));
            }
        }
        MakeVisibleAbilities();
    }

    private void CreateGameAbilities() {
        foreach (var ability in _abilityDatas) {
            _abilities.Add(new AbilityObject(ability));
        }
    }

    public AbilityContainer Save() {
        var activeAbilityController = ActiveAbilityController.Instance;
        var abilityContainer = new AbilityContainer(_superstitionCount, activeAbilityController.CountryWithHolyMassenger, activeAbilityController.CountryWithTemple, activeAbilityController.GetWorldTreeData(), activeAbilityController.CountryWithBifrost, activeAbilityController.ActiveAbilityUseCount);
        foreach (var ability in _abilities) {
            abilityContainer.AddAbilityData(ability);
        }
        return abilityContainer;
    }

    private void OnAbilityEvent(AbilityEvent abilityEvent) {
        var ability = abilityEvent.Target;
        if (ability == null) return;
        switch (abilityEvent.EventSubtype) {
            case AbilityEventType.BuyAbility:
                BuyAbility(ability);
                IncreaseFeatureCount(abilityEvent.Target.AbilityData);
                break;
            case AbilityEventType.Superstition:
                SuperstitionAbility(ability);
                break;
            case AbilityEventType.ReturnAbility:
                ReturnAbility(ability);
                DecreaseFeatureCount(abilityEvent.Target.AbilityData);
                break;
        }
    }

    private void IncreaseFeatureCount(BaseAbilityData abilityData) {
        foreach (var feature in abilityData.ImposeModifiers) {
            IncreaseImposeByFeature(feature.CountryModifierType, feature.Value);
        }
        foreach (var feature in abilityData.TypeModifiers) {
            IncreaseFeature(feature.CountryModifierType);
        }
        foreach (var gameplayEffect in abilityData.GameplayEffects) {
            switch (gameplayEffect.GameplayEffectType) {
                case GameplayEffectType.AdditionalSuperstitionChance:
                    _additionalSuperstition += gameplayEffect.ValueSelector.Value;
                    break;
                case GameplayEffectType.AdditionalRecoilChance:
                    _additionalRecoilChance += gameplayEffect.ValueSelector.Value;
                    break;
            }
        }
    }

    private void DecreaseFeatureCount(BaseAbilityData abilityData) {
        foreach (var feature in abilityData.ImposeModifiers) {
            DecreaseImposeFeature(feature.CountryModifierType, feature.Value);
        }
        foreach (var feature in abilityData.TypeModifiers) {
            DecreaseFeature(feature.CountryModifierType);
        }
        foreach (var gameplayEffect in abilityData.GameplayEffects) {
            switch (gameplayEffect.GameplayEffectType) {
                case GameplayEffectType.AdditionalSuperstitionChance:
                    _additionalSuperstition -= gameplayEffect.ValueSelector.Value;
                    break;
                case GameplayEffectType.AdditionalRecoilChance:
                    _additionalRecoilChance -= gameplayEffect.ValueSelector.Value;
                    break;
            }
        }
    }

    private void IncreaseFeature(CountryFeatures feature) {
        if (_featureCount.ContainsKey(feature))
            _featureCount[feature]++;
        else
            _featureCount.Add(feature, 1);
    }

    private void DecreaseFeature(CountryFeatures feature) {
        _featureCount[feature]--;
    }

    private void IncreaseImposeByFeature(CountryFeatures feature, float value) {
        if (_additionalImpose.ContainsKey(feature))
            _additionalImpose[feature] += value;
        else
            _additionalImpose.Add(feature, value);
    }

    private void DecreaseImposeFeature(CountryFeatures feature, float value) {
        _additionalImpose[feature] -= value;
    }

    public float GetActiveAbilitysBonus(IEnumerable<ActiveAbilityType> activeAbilitys) {
        var value = 1f;
        foreach (var activeAbility in activeAbilitys) {
            switch (activeAbility) {
                case ActiveAbilityType.Wrath: value += 0.001f; break;
            }
        }
        return value;
    }

    public float GetAdditionalImposeValue(IEnumerable<CountryFeatures> features) {
        var value = 0f;
        foreach (var feature in features) {
            value += GetFeatureImposeValue(feature);
        }
        return value;
    }

    public float GetFeatureImposeValue(CountryFeatures feature) {
        if (_additionalImpose.TryGetValue(feature, out var value))
            return value;
        return 0f;
    }

    private void BuyAbility(AbilityObject ability) {
        _takedAbilities.Add(ability);
        ability.Purchased = true;
        UpdateCostModifier(ability);
    }

    private void ReturnAbility(AbilityObject ability) {
        _takedAbilities.Remove(ability);
        ability.Purchased = false;
    }

    private void SuperstitionAbility(AbilityObject ability) {
        EventManager.Instance.DispatchEvent(new AbilityEvent(AbilityEventType.BuyAbility, ability));
    }

    public void MakeVisibleAbilities() {
        var availalbeAbilities = _abilities.Where(a => a.AvailableToPurchase);
        foreach (var ability in availalbeAbilities) {
            ability.Visible = true;
            var visibleAbilities = GetAbilities(ability.AbilityData.MakeVisibleAbilities);
            if (visibleAbilities.Count() == 0) continue;
            foreach (var va in visibleAbilities) {
                if (va.Visible) continue;
                va.Visible = true;
            }
        }
    }

    private void OnTimeEvent(TimeEvent timeEvent) {

        if (Player.Instance.ActivateTutorial)
        {
            var tutorialWindow = UIController.Instance.TutorialWindow;
            if (tutorialWindow.TutorialIsActive)
                return;
        }

        if (timeEvent.EventSubtype != TimeEventType.Tick) return;
        if (timeEvent.Target.CurrentYear < _superstitionConfig.StartTick) return;
        CheckRecoil();
        _currentSuperstitionDelay--;
        if (_currentSuperstitionDelay > 0) return;
        CheckSuperstition(timeEvent.Target.CurrentYear);
    }

    private void CheckRecoil() {
        if (_superstitionConfig.BaseRecoilChance == 0) return;
        var chance = _superstitionConfig.BaseRecoilChance + _additionalRecoilChance;
        var randomValue = UnityEngine.Random.Range(0f, 100f);
        if (randomValue <= chance) {
            ReturnRandomAbility();
        }
    }

    private void CheckSuperstition(int currentTick) {
        var chance = currentTick * (_superstitionConfig.BaseChance - (0.001f * _superstitionCount)) * (1 + _additionalSuperstition);
        var randomValue = UnityEngine.Random.Range(0f, 100f);
        if (randomValue <= chance) {
            LearnRandomAbility();
            _currentSuperstitionDelay = _superstitionConfig.SuperstitionDelay;
        }
    }

    private void LearnRandomAbility() {
        if (_abilities == null) return;

        var featuresAbilities = _abilities.Where(a => a.AbilityData.AbilityCategory == AbilityCategory.Features);
        if (featuresAbilities == null || featuresAbilities.Count() == 0) return;

        var notPurchasedAndAvailable = featuresAbilities.Where(a => !a.Purchased && a.AvailableToPurchase);
        if (notPurchasedAndAvailable == null || notPurchasedAndAvailable.Count() == 0) return;

        var randomAbility = notPurchasedAndAvailable.PickRandom();
        EventManager.Instance.DispatchEvent(new InGameEvent(InGameEventType.Setup, new EventData(true, true, "Eventdiscriptor_Superstition", "Eventheader_Superstition", randomAbility, EventSubtype.Superstition, "Character Traveler")));
        EventManager.Instance.DispatchEvent(new AbilityEvent(AbilityEventType.Superstition, randomAbility));
        _superstitionCount++;
    }

    private void ReturnRandomAbility() {
        if (_takedAbilities.Count == 0) return;
        var targetAbility = _takedAbilities.PickRandom();
        EventManager.Instance.DispatchEvent(new InGameEvent(InGameEventType.Setup, new EventData(true, true, "Eventdiscriptor_AntiSuperstition", "Eventheader_AntiSuperstition", targetAbility, EventSubtype.ReturnAbility, "Character Spirit")));
        EventManager.Instance.DispatchEvent(new AbilityEvent(AbilityEventType.ReturnAbility, targetAbility));
    }

    private void UpdateCostModifier(AbilityObject ability) {
        var abilityData = ability.AbilityData;
        if (_abilityCostModifier.ContainsKey(abilityData.AbilityCategory))
            _abilityCostModifier[abilityData.AbilityCategory] += 1;
        else
            _abilityCostModifier.Add(abilityData.AbilityCategory, 1);
        var gameplayEffects = ability.AbilityData.GameplayEffects;
        foreach (var effect in gameplayEffects) {
            if (effect.GameplayEffectType != GameplayEffectType.Rollback) continue;
            _rollbackCostModifier += (int)effect.ValueSelector.Value;
        }

    }

    public int GetCostModifier(AbilityCategory category) {
        if (!_abilityCostModifier.ContainsKey(category)) return 0;
        return _abilityCostModifier[category];
    }

    public AbilityObject GetAbility(int id) {
        foreach (var ability in _abilities) {
            if (ability.AbilityData.AbilityID == id)
                return ability;
        }
        return null;
    }

    public IEnumerable<AbilityObject> GetAbilities(IEnumerable<int> indexes) {
        var abilities = new List<AbilityObject>();
        foreach (var index in indexes) {
            var ability = GetAbility(index);
            if (ability == null) continue;
            abilities.Add(ability);
        }
        return abilities;
    }

    public IEnumerable<AbilityObject> GetAllAbilityByCategory(AbilityCategory abilityCategory) {
        var abilities = new List<AbilityObject>();
        foreach (var ability in _abilities) {
            if (ability.AbilityData.AbilityCategory != abilityCategory) continue;
            abilities.Add(ability);
        }
        return abilities;
    }

    public void ReturnAbility(AbilityType abilityType) {
        var abilitiesToReturn = new List<AbilityObject>();
        foreach (var ability in _takedAbilities) {
            if (ability.AbilityData.AbilityCategory != AbilityCategory.Features) continue;
            if (ability.AbilityData.AbilityType != abilityType) continue;
            abilitiesToReturn.Add(ability);
        }
        var returnAbilityCount = abilitiesToReturn.Count();
        foreach (var ability in abilitiesToReturn) {
            ability.Blocked = true;
            EventManager.Instance.DispatchEvent(new AbilityEvent(AbilityEventType.ReturnAbility, ability));
        }
        switch (abilityType) {
            case AbilityType.Military:
                LearnRandomAbilities(AbilityType.Peaceful, returnAbilityCount);
                break;
            case AbilityType.Peaceful:
                LearnRandomAbilities(AbilityType.Military, returnAbilityCount);
                break;
        }
    }

    private void LearnRandomAbilities(AbilityType abilityType, int count) {
        for (int i = 0; i < count; i++) {
            var randomAbility = _abilities.Where(a => a.AbilityData.AbilityType == abilityType && !a.Purchased).PickRandom();
            EventManager.Instance.DispatchEvent(new AbilityEvent(AbilityEventType.BuyAbility, randomAbility));
        }
    }
}