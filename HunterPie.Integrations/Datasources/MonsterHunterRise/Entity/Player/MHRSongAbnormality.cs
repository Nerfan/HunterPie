﻿using HunterPie.Core.Architecture.Events;
using HunterPie.Core.Domain.Interfaces;
using HunterPie.Core.Extensions;
using HunterPie.Core.Game.Data.Schemas;
using HunterPie.Core.Game.Entity.Player;
using HunterPie.Core.Game.Enums;
using HunterPie.Integrations.Datasources.MonsterHunterRise.Definitions;

namespace HunterPie.Integrations.Datasources.MonsterHunterRise.Entity.Player;

public class MHRSongAbnormality : IAbnormality, IUpdatable<MHRHHAbnormality>, IEventDispatcher
{

    private float _timer;

    public string Id { get; private set; }
    public string Name { get; private set; }
    public string Icon { get; private set; }
    public AbnormalityType Type => AbnormalityType.Song;
    public float Timer
    {
        get => _timer;
        private set
        {
            if (_timer != value)
            {
                _timer = value;
                this.Dispatch(_onTimerUpdate, this);
            }
        }
    }
    public float MaxTimer { get; private set; }
    public bool IsInfinite { get; private set; }
    public int Level { get; private set; }

    public bool IsBuildup { get; private set; }

    private readonly SmartEvent<IAbnormality> _onTimerUpdate = new();
    public event EventHandler<IAbnormality> OnTimerUpdate
    {
        add => _onTimerUpdate.Hook(value);
        remove => _onTimerUpdate.Unhook(value);
    }

    public MHRSongAbnormality(AbnormalitySchema schema)
    {
        Id = schema.Id;
        Name = schema.Name;
        Icon = schema.Icon;
        IsBuildup = schema.IsBuildup;
    }

    void IUpdatable<MHRHHAbnormality>.Update(MHRHHAbnormality data)
    {
        MaxTimer = Math.Max(MaxTimer, data.Timer);
        Timer = data.Timer;
    }
}
