﻿using HunterPie.Core.Architecture.Events;
using HunterPie.Core.Domain.Interfaces;
using HunterPie.Core.Extensions;
using HunterPie.Core.Game.Data;
using HunterPie.Core.Game.Data.Schemas;
using HunterPie.Core.Game.Entity.Player;
using HunterPie.Core.Game.Enums;
using HunterPie.Integrations.Datasources.MonsterHunterWorld.Definitions;

namespace HunterPie.Integrations.Datasources.MonsterHunterWorld.Entity.Player;

public class MHWAbnormality : IAbnormality, IEventDispatcher, IUpdatable<MHWAbnormalityStructure>
{
    private float _timer;

    public string Id { get; }

    public string Icon { get; }

    public AbnormalityType Type { get; }

    public float Timer
    {
        get => _timer;
        private set
        {
            if (value != _timer)
            {
                _timer = value;
                this.Dispatch(_onTimerUpdate, this);
            }
        }
    }

    public float MaxTimer { get; private set; }

    public bool IsInfinite { get; }

    public int Level { get; }

    public bool IsBuildup { get; set; }

    private readonly SmartEvent<IAbnormality> _onTimerUpdate = new();
    public event EventHandler<IAbnormality> OnTimerUpdate
    {
        add => _onTimerUpdate.Hook(value);
        remove => _onTimerUpdate.Unhook(value);
    }

    public MHWAbnormality(AbnormalitySchema schema)
    {
        Id = schema.Id;
        Icon = schema.Icon;
        Type = schema.Category switch
        {
            AbnormalityData.Consumables => AbnormalityType.Consumable,
            AbnormalityData.Songs => AbnormalityType.Song,
            AbnormalityData.Debuffs => AbnormalityType.Debuff,
            AbnormalityData.Skills => AbnormalityType.Skill,
            AbnormalityData.Orchestra => AbnormalityType.Orchestra,
            AbnormalityData.Gears => AbnormalityType.Gear,
            AbnormalityData.Foods => AbnormalityType.Food,
            _ => throw new NotImplementedException("unreachable")
        };
        IsBuildup = schema.IsBuildup;

        if (IsBuildup)
            MaxTimer = schema.MaxBuildup;
    }

    void IUpdatable<MHWAbnormalityStructure>.Update(MHWAbnormalityStructure data)
    {
        MaxTimer = Math.Max(MaxTimer, data.Timer);
        Timer = data.Timer;
    }
}
