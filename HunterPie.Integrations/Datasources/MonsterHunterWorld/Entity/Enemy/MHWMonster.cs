﻿using HunterPie.Core.Address.Map;
using HunterPie.Core.Architecture.Events;
using HunterPie.Core.Domain;
using HunterPie.Core.Domain.Interfaces;
using HunterPie.Core.Domain.Process;
using HunterPie.Core.Extensions;
using HunterPie.Core.Game.Data;
using HunterPie.Core.Game.Data.Schemas;
using HunterPie.Core.Game.Entity.Enemy;
using HunterPie.Core.Game.Enums;
using HunterPie.Core.Logger;
using HunterPie.Integrations.Datasources.MonsterHunterWorld.Definitions;

namespace HunterPie.Integrations.Datasources.MonsterHunterWorld.Entity.Enemy;

public class MHWMonster : Scannable, IMonster, IEventDispatcher
{
    #region Private
    private readonly long _address;
    private int _id = -1;
    private int _doubleLinkedListIndex;
    private float _health = -1;
    private bool _isTarget;
    private bool _isEnraged;
    private Target _target;
    private Crown _crown;
    private float _stamina;
    private float _captureThreshold;
    private readonly MHWMonsterAilment _enrage = new("STATUS_ENRAGE");
    private (long, MHWMonsterPart)[] _parts;
    private List<(long, MHWMonsterAilment)> _ailments;
    #endregion

    public int Id
    {
        get => _id;
        private set
        {
            if (value != _id)
            {
                _id = value;
                GetMonsterWeaknesses();
                GetMonsterCaptureThreshold();
                this.Dispatch(_onSpawn);
            }
        }
    }

    public string Em { get; private set; }

    public string Name => MHWContext.Strings.GetMonsterNameById(Id);

    public float Health
    {
        get => _health;
        private set
        {
            if (value != _health)
            {
                _health = value;
                this.Dispatch(_onHealthChange);

                if (Health <= 0)
                    this.Dispatch(_onDeath);
            }
        }
    }

    public float MaxHealth { get; private set; }

    public float Stamina
    {
        get => _stamina;
        private set
        {
            if (value != _stamina)
            {
                _stamina = value;
                this.Dispatch(_onStaminaChange);
            }
        }
    }

    public float MaxStamina { get; private set; }

    public bool IsTarget
    {
        get => _isTarget;
        private set
        {
            if (_isTarget != value)
            {
                _isTarget = value;
                this.Dispatch(_onTarget);
            }
        }
    }

    public IMonsterPart[] Parts => _parts?
                                    .Select(v => v.Item2)
                                    .ToArray<IMonsterPart>() ?? Array.Empty<IMonsterPart>();

    public IMonsterAilment[] Ailments => _ailments?
                                          .Select(a => a.Item2)
                                          .ToArray<IMonsterAilment>() ?? Array.Empty<IMonsterAilment>();
    public Target Target
    {
        get => _target;
        private set
        {
            if (_target != value)
            {
                _target = value;
                this.Dispatch(_onTargetChange);
            }
        }
    }

    public Crown Crown
    {
        get => _crown;
        private set
        {
            if (_crown != value)
            {
                _crown = value;
                this.Dispatch(_onCrownChange);
            }
        }
    }

    public bool IsEnraged
    {
        get => _isEnraged;
        private set
        {
            if (value != _isEnraged)
            {
                _isEnraged = value;
                this.Dispatch(_onEnrageStateChange);
            }
        }
    }

    public IMonsterAilment Enrage => _enrage;

    private readonly List<Element> _weaknesses = new();
    public Element[] Weaknesses => _weaknesses.ToArray();

    public float CaptureThreshold
    {
        get => _captureThreshold;
        private set
        {
            if (value != _captureThreshold)
            {
                _captureThreshold = value;
                this.Dispatch(_onCaptureThresholdChange, this);
            }
        }
    }

    private readonly SmartEvent<EventArgs> _onSpawn = new();
    public event EventHandler<EventArgs> OnSpawn
    {
        add => _onSpawn.Hook(value);
        remove => _onSpawn.Unhook(value);
    }

    private readonly SmartEvent<EventArgs> _onLoad = new();
    public event EventHandler<EventArgs> OnLoad
    {
        add => _onLoad.Hook(value);
        remove => _onLoad.Unhook(value);
    }

    private readonly SmartEvent<EventArgs> _onDespawn = new();
    public event EventHandler<EventArgs> OnDespawn
    {
        add => _onDespawn.Hook(value);
        remove => _onDespawn.Unhook(value);
    }

    private readonly SmartEvent<EventArgs> _onDeath = new();
    public event EventHandler<EventArgs> OnDeath
    {
        add => _onDeath.Hook(value);
        remove => _onDeath.Unhook(value);
    }

    private readonly SmartEvent<EventArgs> _onCapture = new();
    public event EventHandler<EventArgs> OnCapture
    {
        add => _onCapture.Hook(value);
        remove => _onCapture.Unhook(value);
    }

    private readonly SmartEvent<EventArgs> _onTarget = new();
    public event EventHandler<EventArgs> OnTarget
    {
        add => _onTarget.Hook(value);
        remove => _onTarget.Unhook(value);
    }

    private readonly SmartEvent<EventArgs> _onCrownChange = new();
    public event EventHandler<EventArgs> OnCrownChange
    {
        add => _onCrownChange.Hook(value);
        remove => _onCrownChange.Unhook(value);
    }

    private readonly SmartEvent<EventArgs> _onHealthChange = new();
    public event EventHandler<EventArgs> OnHealthChange
    {
        add => _onHealthChange.Hook(value);
        remove => _onHealthChange.Unhook(value);
    }

    private readonly SmartEvent<EventArgs> _onStaminaChange = new();
    public event EventHandler<EventArgs> OnStaminaChange
    {
        add => _onStaminaChange.Hook(value);
        remove => _onStaminaChange.Unhook(value);
    }

    private readonly SmartEvent<EventArgs> _onActionChange = new();
    public event EventHandler<EventArgs> OnActionChange
    {
        add => _onActionChange.Hook(value);
        remove => _onActionChange.Unhook(value);
    }

    private readonly SmartEvent<EventArgs> _onEnrageStateChange = new();
    public event EventHandler<EventArgs> OnEnrageStateChange
    {
        add => _onEnrageStateChange.Hook(value);
        remove => _onEnrageStateChange.Unhook(value);
    }

    private readonly SmartEvent<EventArgs> _onTargetChange = new();
    public event EventHandler<EventArgs> OnTargetChange
    {
        add => _onTargetChange.Hook(value);
        remove => _onTargetChange.Unhook(value);
    }

    private readonly SmartEvent<IMonsterPart> _onNewPartFound = new();
    public event EventHandler<IMonsterPart> OnNewPartFound
    {
        add => _onNewPartFound.Hook(value);
        remove => _onNewPartFound.Unhook(value);
    }

    private readonly SmartEvent<IMonsterAilment> _onNewAilmentFound = new();
    public event EventHandler<IMonsterAilment> OnNewAilmentFound
    {
        add => _onNewAilmentFound.Hook(value);
        remove => _onNewAilmentFound.Unhook(value);
    }

    private readonly SmartEvent<Element[]> _onWeaknessesChange = new();
    public event EventHandler<Element[]> OnWeaknessesChange
    {
        add => _onWeaknessesChange.Hook(value);
        remove => _onWeaknessesChange.Unhook(value);
    }

    private readonly SmartEvent<IMonster> _onCaptureThresholdChange = new();
    public event EventHandler<IMonster> OnCaptureThresholdChange
    {
        add => _onCaptureThresholdChange.Hook(value);
        remove => _onCaptureThresholdChange.Unhook(value);
    }

    public MHWMonster(IProcessManager process, long address, string em) : base(process)
    {
        _address = address;
        Em = em;

        Log.Debug($"Initialized monster at address {address:X}");
    }

    private void GetMonsterCaptureThreshold()
    {
        MonsterDataSchema? data = MonsterData.GetMonsterData(Id);

        if (!data.HasValue)
            return;

        CaptureThreshold = MonsterData.GetMonsterData(Id)?.Capture / 100f ?? 0;
    }

    private void GetMonsterWeaknesses()
    {
        MonsterDataSchema? data = MonsterData.GetMonsterData(Id);

        if (!data.HasValue)
            return;

        _weaknesses.AddRange(data.Value.Weaknesses);
        this.Dispatch(_onWeaknessesChange, Weaknesses);
    }

    [ScannableMethod]
    private void GetMonsterBasicInformation()
    {
        int monsterId = Process.Memory.Read<int>(_address + 0x12280);
        int doubleLinkedListIndex = Process.Memory.Read<int>(_address + 0x1228C);

        Id = monsterId;
        _doubleLinkedListIndex = doubleLinkedListIndex;
    }

    [ScannableMethod]
    private void GetMonsterHealthData()
    {
        long monsterHealthPtr = Process.Memory.Read<long>(_address + 0x7670);
        float[] healthValues = Process.Memory.Read<float>(monsterHealthPtr + 0x60, 2);

        MaxHealth = healthValues[0];
        Health = healthValues[1];
    }

    [ScannableMethod]
    private void GetMonsterStaminaData()
    {
        float[] staminaValues = Process.Memory.Read<float>(_address + 0x1C0F0, 2);

        MaxStamina = staminaValues[1];
        Stamina = staminaValues[0];
    }

    [ScannableMethod]
    private void GetMonsterCrownData()
    {
        float sizeModifier = Process.Memory.Read<float>(_address + 0x7730);
        float sizeMultiplier = Process.Memory.Read<float>(_address + 0x188);

        if (sizeModifier is <= 0 or >= 2)
            sizeModifier = 1;

        float monsterSizeMultiplier = sizeMultiplier / sizeModifier;

        MonsterSizeSchema? crownData = MonsterData.GetMonsterData(Id)?.Size;

        if (crownData is null)
            return;

        MonsterSizeSchema crown = crownData.Value;

        Crown = monsterSizeMultiplier >= crown.Gold
            ? Crown.Gold
            : monsterSizeMultiplier >= crown.Silver ? Crown.Silver : monsterSizeMultiplier <= crown.Mini ? Crown.Mini : Crown.None;
    }

    [ScannableMethod]
    private void GetMonsterEnrage()
    {
        MHWMonsterStatusStructure enrageStructure = Process.Memory.Read<MHWMonsterStatusStructure>(_address + 0x1BE30);
        IUpdatable<MHWMonsterStatusStructure> enrage = _enrage;

        IsEnraged = enrageStructure.Duration > 0;

        enrage.Update(enrageStructure);
    }

    [ScannableMethod]
    private void GetLockedOnMonster()
    {
        int lockedOnDoubleLinkedListIndex = Process.Memory.Deref<int>(
            AddressMap.GetAbsolute("LOCKON_ADDRESS"),
            AddressMap.Get<int[]>("LOCKEDON_MONSTER_INDEX_OFFSETS")
        );

        IsTarget = lockedOnDoubleLinkedListIndex == _doubleLinkedListIndex;

        Target = IsTarget ? Target.Self : lockedOnDoubleLinkedListIndex != -1 ? Target.Another : Target.None;
    }

    [ScannableMethod]
    private void GetMonsterParts()
    {
        long monsterPartPtr = Process.Memory.Read<long>(_address + 0x1D058);

        if (monsterPartPtr == 0)
            return;

        long monsterPartAddress = monsterPartPtr + 0x40;
        long monsterSeverableAddress = monsterPartPtr + 0x1FC8;

        MonsterDataSchema? monsterSchema = MonsterData.GetMonsterData(Id);

        if (!monsterSchema.HasValue)
            return;

        MonsterDataSchema monsterInfo = monsterSchema.Value;

        if (_parts is null)
        {
            _parts = new (long, MHWMonsterPart)[monsterInfo.Parts.Length];
            for (int i = 0; i < _parts.Length; i++)
                _parts[i] = (0, null);
        }

        int normalPartIndex = 0;

        for (int pIndex = 0; pIndex < monsterInfo.Parts.Length; pIndex++)
        {
            (long cachedAddress, MHWMonsterPart part) = _parts[pIndex];
            IUpdatable<MHWMonsterPartStructure> updatable = _parts[pIndex].Item2;
            MonsterPartSchema partSchema = monsterInfo.Parts[pIndex];
            MHWMonsterPartStructure partStructure = new();

            // If the part address has been cached already, we can just read them
            if (cachedAddress > 0)
            {
                partStructure = Process.Memory.Read<MHWMonsterPartStructure>(cachedAddress);

                // Alatreon elemental explosion level
                if (Id == 87 && partStructure.Index == 3)
                    partStructure.Counter = Process.Memory.Read<int>(_address + 0x20920);

                updatable.Update(partStructure);
                continue;
            }

            if (partSchema.IsSeverable)
                while (monsterSeverableAddress < (monsterSeverableAddress + (0x120 * 32)))
                {
                    if (Process.Memory.Read<int>(monsterSeverableAddress) <= 0xA0)
                        monsterSeverableAddress += 0x8;

                    partStructure = Process.Memory.Read<MHWMonsterPartStructure>(monsterSeverableAddress);

                    if (partStructure.Index == partSchema.Id && partStructure.MaxHealth > 0)
                    {
                        MHWMonsterPart newPart = new(
                            partSchema.String,
                            partSchema.IsSeverable,
                            partSchema.TenderizeIds
                        );
                        _parts[pIndex] = (monsterSeverableAddress, newPart);

                        this.Dispatch(_onNewPartFound, newPart);

                        do
                            monsterSeverableAddress += 0x78;
                        while (partStructure.Equals(Process.Memory.Read<MHWMonsterPartStructure>(monsterSeverableAddress)));

                        break;
                    }

                    monsterSeverableAddress += 0x78;
                }
            else
            {
                long address = monsterPartAddress + (normalPartIndex * 0x1F8);
                partStructure = Process.Memory.Read<MHWMonsterPartStructure>(address);

                MHWMonsterPart newPart = new(
                    partSchema.String,
                    partSchema.IsSeverable,
                    partSchema.TenderizeIds
                );

                _parts[pIndex] = (address, newPart);

                this.Dispatch(_onNewPartFound, newPart);

                normalPartIndex++;
            }

            updatable = _parts[pIndex].Item2;
            updatable.Update(partStructure);
        }
    }

    [ScannableMethod]
    private void GetMonsterPartTenderizes()
    {
        MHWTenderizeInfoStructure[] tenderizeInfos = Process.Memory.Read<MHWTenderizeInfoStructure>(
            _address + 0x1C458,
            10
        );

        foreach (MHWTenderizeInfoStructure tenderizeInfo in tenderizeInfos)
        {
            if (tenderizeInfo.PartId == uint.MaxValue)
                continue;

            MHWMonsterPart[] parts = _parts.Select(p => p.Item2)
                              .Where(p => p.HasTenderizeId(tenderizeInfo.PartId))
                              .ToArray();

            foreach (IUpdatable<MHWTenderizeInfoStructure> part in parts)
                part.Update(tenderizeInfo);
        }
    }

    [ScannableMethod]
    private void GetMonsterAilments()
    {
        if (_ailments is null)
        {
            _ailments = new(32);
            long monsterAilmentArrayElement = _address + 0x1BC40;
            long monsterAilmentPtr = Process.Memory.Read<long>(monsterAilmentArrayElement);

            while (monsterAilmentPtr > 1)
            {
                long currentMonsterAilmentPtr = monsterAilmentPtr;
                // Comment from V1 so I don't forget: There's a gap between the monsterAilmentPtr and the actual ailment data
                MHWMonsterAilmentStructure structure = Process.Memory.Read<MHWMonsterAilmentStructure>(currentMonsterAilmentPtr + 0x148);

                monsterAilmentArrayElement += sizeof(long);
                monsterAilmentPtr = Process.Memory.Read<long>(monsterAilmentArrayElement);

                if (structure.Owner != _address)
                    break;

                AilmentDataSchema ailmentSchema = MonsterData.GetAilmentData(structure.Id);
                if (ailmentSchema.IsUnknown)
                    continue;

                var ailment = new MHWMonsterAilment(ailmentSchema.String);

                _ailments.Add((currentMonsterAilmentPtr, ailment));
                this.Dispatch(_onNewAilmentFound, ailment);

                IUpdatable<MHWMonsterAilmentStructure> updatable = ailment;
                updatable.Update(structure);
            }

            return;
        }

        for (int i = 0; i < _ailments.Count; i++)
        {
            (long address, MHWMonsterAilment ailment) = _ailments[i];

            MHWMonsterAilmentStructure structure = Process.Memory.Read<MHWMonsterAilmentStructure>(address + 0x148);
            IUpdatable<MHWMonsterAilmentStructure> updatable = ailment;
            updatable.Update(structure);
        }
    }
}
