using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SystemHeat
{
  /// <summary>Reports the live heat load of MKS USI_Converter modules without replacing them.</summary>
  public class ModuleSystemHeatUSIConverter : PartModule
  {
    [KSPField(isPersistant = false)] public string moduleID = "mksConverter";
    [KSPField(isPersistant = false)] public string systemHeatModuleID = "mks";
    [KSPField(isPersistant = false)] public float heatMultiplier = 0.9f;
    [KSPField(isPersistant = false)] public float systemOutletTemperature = 600f;
    private ModuleSystemHeat heatModule;
    private List<ModuleResourceConverter> converters = new();

    public void Start()
    {
      heatModule = ModuleUtils.FindHeatModule(part, systemHeatModuleID);
      converters = part.Modules.Cast<PartModule>()
        .Where(x => x.GetType().Name == "USI_Converter")
        .OfType<ModuleResourceConverter>().ToList();
    }

    public void FixedUpdate()
    {
      if (!HighLogic.LoadedSceneIsEditor)
        UpdateFlux(false);
    }

    public void UpdateFlux(bool editor)
    {
      if (heatModule == null) return;
      float total = 0f;
      foreach (var converter in converters)
      {
        if (!editor && !converter.ModuleIsActive()) continue;
        if (!editor && converter.lastTimeFactor == 0d) continue;
        if (converter.inputList == null) continue;
        total += (float)converter.inputList.Where(x => x.ResourceName == "ElectricCharge").Sum(x => x.Ratio) * heatMultiplier;
      }
      heatModule.AddFlux(moduleID, total > 0f ? systemOutletTemperature : 0f, total, true);
    }

    public void Update()
    {
      if (HighLogic.LoadedSceneIsEditor) UpdateFlux(true);
    }

    public void OnDisable() => heatModule?.AddFlux(moduleID, 0f, 0f, false);
  }

  /// <summary>Reports the live heat load of MKS USI_Harvester modules without replacing them.</summary>
  public class ModuleSystemHeatUSIHarvester : PartModule
  {
    [KSPField(isPersistant = false)] public string moduleID = "mksHarvester";
    [KSPField(isPersistant = false)] public string systemHeatModuleID = "mks";
    [KSPField(isPersistant = false)] public float heatMultiplier = 1f;
    [KSPField(isPersistant = false)] public float systemOutletTemperature = 350f;
    private ModuleSystemHeat heatModule;
    private List<ModuleResourceHarvester> harvesters = new();

    public void Start()
    {
      heatModule = ModuleUtils.FindHeatModule(part, systemHeatModuleID);
      harvesters = part.Modules.Cast<PartModule>()
        .Where(x => x.GetType().Name == "USI_Harvester")
        .OfType<ModuleResourceHarvester>().ToList();
    }

    public void FixedUpdate()
    {
      if (!HighLogic.LoadedSceneIsEditor)
        UpdateFlux(false);
    }

    public void UpdateFlux(bool editor)
    {
      if (heatModule == null) return;
      float total = 0f;
      foreach (var harvester in harvesters)
      {
        if (!editor && !harvester.ModuleIsActive()) continue;
        if (!editor && harvester.lastTimeFactor == 0d) continue;
        if (harvester.inputList == null) continue;
        total += (float)harvester.inputList.Where(x => x.ResourceName == "ElectricCharge").Sum(x => x.Ratio) * heatMultiplier;
      }
      heatModule.AddFlux(moduleID, total > 0f ? systemOutletTemperature : 0f, total, true);
    }

    public void Update()
    {
      if (HighLogic.LoadedSceneIsEditor) UpdateFlux(true);
    }

    public void OnDisable() => heatModule?.AddFlux(moduleID, 0f, 0f, false);
  }

  /// <summary>Bridges the MKS geothermal heat pump to a SystemHeat loop.</summary>
  public class ModuleSystemHeatUSIHeatPump : PartModule
  {
    [KSPField(isPersistant = false)] public string moduleID = "mksHeatPump";
    [KSPField(isPersistant = false)] public string systemHeatModuleID = "mks";
    [KSPField(isPersistant = false)] public float maxCooling = 0f;
    private ModuleSystemHeat heatModule;
    private ModuleActiveRadiator heatPump;

    public void Start()
    {
      heatModule = ModuleUtils.FindHeatModule(part, systemHeatModuleID);
      heatPump = part.Modules.Cast<PartModule>().FirstOrDefault(x => x.GetType().Name == "ModuleHeatPump") as ModuleActiveRadiator;
      if (heatPump != null && maxCooling <= 0f)
      {
        var field = heatPump.GetType().GetField("maxEnergyTransfer");
        if (field != null) maxCooling = Convert.ToSingle(field.GetValue(heatPump));
      }
    }

    public void FixedUpdate()
    {
      if (heatModule == null || heatPump == null) return;
      heatModule.AddFlux(moduleID, 0f, heatPump.IsCooling ? -maxCooling : 0f, false);
    }

    public void OnDisable() => heatModule?.AddFlux(moduleID, 0f, 0f, false);
  }
}
