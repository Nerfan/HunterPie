﻿using HunterPie.Core.Architecture.Events;
using HunterPie.Core.Domain.Interfaces;
using HunterPie.Core.Extensions;
using HunterPie.Core.Game.Entity.Enemy;
using HunterPie.Integrations.Datasources.MonsterHunterWorld.Definitions;

namespace HunterPie.Integrations.Datasources.MonsterHunterWorld.Entity.Enemy;

public class MHWMonsterAilment : IMonsterAilment, IEventDispatcher, IUpdatable<MHWMonsterStatusStructure>, IUpdatable<MHWMonsterAilmentStructure>
{
    private int _counter;
    private float _timer;
    private float _buildup;

    public string Id { get; }
    public int Counter
    {
        get => _counter;
        private set
        {
            if (value != _counter)
            {
                _counter = value;
                this.Dispatch(_onCounterUpdate, this);
            }
        }
    }
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
    public float BuildUp
    {
        get => _buildup;
        private set
        {
            if (value != _buildup)
            {
                _buildup = value;
                this.Dispatch(_onBuildUpUpdate, this);
            }
        }
    }
    public float MaxBuildUp { get; private set; }

    private readonly SmartEvent<IMonsterAilment> _onTimerUpdate = new();
    public event EventHandler<IMonsterAilment> OnTimerUpdate
    {
        add => _onTimerUpdate.Hook(value);
        remove => _onTimerUpdate.Unhook(value);
    }

    private readonly SmartEvent<IMonsterAilment> _onBuildUpUpdate = new();
    public event EventHandler<IMonsterAilment> OnBuildUpUpdate
    {
        add => _onBuildUpUpdate.Hook(value);
        remove => _onBuildUpUpdate.Unhook(value);
    }

    private readonly SmartEvent<IMonsterAilment> _onCounterUpdate = new();
    public event EventHandler<IMonsterAilment> OnCounterUpdate
    {
        add => _onCounterUpdate.Hook(value);
        remove => _onCounterUpdate.Unhook(value);
    }

    public MHWMonsterAilment(string ailmentId)
    {
        Id = ailmentId;
    }

    void IUpdatable<MHWMonsterStatusStructure>.Update(MHWMonsterStatusStructure data)
    {
        MaxTimer = data.MaxDuration;
        Timer = data.Duration > 0
            ? data.MaxDuration - data.Duration
            : 0;
        MaxBuildUp = data.MaxBuildup;
        BuildUp = data.Buildup;
        Counter = data.Counter;
    }

    void IUpdatable<MHWMonsterAilmentStructure>.Update(MHWMonsterAilmentStructure data)
    {
        MaxTimer = data.MaxDuration;
        Timer = data.Duration;
        MaxBuildUp = data.MaxBuildup;
        BuildUp = data.Buildup;
        Counter = data.Counter;
    }
}
