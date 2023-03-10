﻿using HunterPie.Core.Game.Enums;
using HunterPie.UI.Architecture;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.ObjectModel;

namespace HunterPie.GUI.Parts.Statistics.Details.ViewModels;

public class MonsterDetailsViewModel : ViewModel
{
    private string _name;
    public string Name { get => _name; set => SetValue(ref _name, value); }

    private string _icon;
    public string Icon { get => _icon; set => SetValue(ref _icon, value); }

    private DateTime _huntedAt;
    public DateTime HuntedAt { get => _huntedAt; set => SetValue(ref _huntedAt, value); }

    private TimeSpan? _timeElapsed;
    public TimeSpan? TimeElapsed { get => _timeElapsed; set => SetValue(ref _timeElapsed, value); }

    private Crown _crown;
    public Crown Crown { get => _crown; set => SetValue(ref _crown, value); }

    private double _maxHealth;
    public double MaxHealth { get => _maxHealth; set => SetValue(ref _maxHealth, value); }

    public ObservableCollection<PartyMemberDetailsViewModel> Players { get; init; } = new();

    public ObservableCollection<StatusDetailsViewModel> Statuses { get; init; } = new();

    public SeriesCollection DamageSeries { get; } = new();

    public SectionsCollection Sections { get; } = new();

    public Func<double, string> TimeFormatter => new((value) => TimeSpan.FromSeconds(value).ToString("mm\\:ss"));

    public Func<double, string> DamageFormatter => new((damage) => $"{damage:0.00}/s");


}