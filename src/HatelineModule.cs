﻿using System;
using System.Collections.Generic;
using System.Xml;
using Celeste.Mod.Hateline.CelesteNet;
using FMOD.Studio;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.Hateline;

public class HatelineModule : EverestModule
{
    public static HatelineModule Instance { get; private set; }

    public override Type SettingsType => typeof(HatelineModuleSettings);

    public static HatelineModuleSettings Settings => (HatelineModuleSettings)Instance._Settings;

    public override Type SessionType => typeof(HatelineModuleSession);

    public static HatelineModuleSession Session => (HatelineModuleSession)Instance._Session;

    public HatelineModule()
    {
        Instance = this; 
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(HatelineModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(KelperModule), LogLevel.Info);
#endif
    }
    
    public static readonly EverestModuleMetadata CelesteNetMetadata = new() { Name = "CelesteNet.Client", VersionString = "2.0.0"};

    public bool HasForcedHat => Settings.AllowMapChanges && Session?.MapForcedHat != null;
    public bool ShouldShowHat => HasForcedHat || Settings.Enabled;
    public bool IsCNetLoaded;

    public string? VisibleHat => HasForcedHat ? Session.MapForcedHat : Settings.SelectedHat;
    public string? CurrentHat => ShouldShowHat ? VisibleHat : HAT_NONE;

    public const string HAT_NONE = "none";

    public int CurrentX => HasForcedHat ? Session.mapsetX : Settings.CrownX;
    public int CurrentY => HasForcedHat ? Session.mapsetY : Settings.CrownY;

    public static HashSet<string> hats = new();

    public Dictionary<string, Dictionary<string, string>> HatAttributes = new();
    
    // Define custom hat attributes here
    private readonly Dictionary<string, string> _hatAttributeDefinitions = new()
    {
        // The name of the attribute and its default value
        { "scaling", "true" }, 
        { "flip", "true" },
        {"tint", "false"}
    };

    public override void Load()
    {
        typeof(GravityHelperImports).ModInterop();
        On.Celeste.Player.Added += HookPlayerAdded;
        On.Celeste.Level.LoadLevel += HookLoadLevel;
        On.Celeste.Player.ResetSprite += HookPlayerResetSprite;

        IsCNetLoaded = Everest.Loader.DependencyLoaded(CelesteNetMetadata);
        
        if (IsCNetLoaded) CelesteNetSupport.Load();
    }

    public override void Unload()
    {
        On.Celeste.Player.Added -= HookPlayerAdded;
        On.Celeste.Level.LoadLevel -= HookLoadLevel;
        On.Celeste.Player.ResetSprite -= HookPlayerResetSprite;

        if (IsCNetLoaded) CelesteNetSupport.Unload();
    }
    
    private static void HookLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerintro, bool isfromloader)
    {
        RegisterHats();
        orig(self, playerintro, isfromloader);
    }
    
    private static void HookPlayerResetSprite(On.Celeste.Player.orig_ResetSprite orig, Player self, PlayerSpriteMode mode)
    {
        orig(self, mode);
        ResetHat(self);
    }
    
    private static void HookPlayerAdded(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
    {
        orig(self, scene);
        ResetHat(self);
    }
    
    public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot)
    {
        base.CreateModMenuSection(menu, inGame, snapshot);
        HatelineSettingsUI.CreateMenu(menu, inGame);
    }
    
    public static void ResetHat(Player self)
    {
        self.Get<HatComponent>()?.RemoveSelf();
        self.Add(new HatComponent(Instance.CurrentHat, Instance.CurrentX, Instance.CurrentY));
    }
    
    public static void ReloadHat()
    {
        HatComponent playerHatComponent = Engine.Scene?.Tracker?.GetEntity<Player>()?.Get<HatComponent>();
        if (playerHatComponent is null) return;
        
        playerHatComponent.CreateHat(Instance.CurrentHat, true);
        playerHatComponent.SetPosition(Instance.CurrentX, Instance.CurrentY);
        
        CelesteNetSupport.CNetComponent?.SendPlayerHat();
    }
    
    public static void RegisterHats()
    {
        Instance.HatAttributes.Clear();
        hats.Clear();
        foreach ((string spriteName, SpriteData spriteData) in GFX.SpriteBank.SpriteData)
        {
            if (!spriteName.StartsWith("hateline_")) continue;
            
            // [9..] removes the hateline_ prefix
            string hatName = spriteName[9..];
            XmlNode node = spriteData.Sources[0].XML;
            Dictionary<string, string> attributesToAdd = new();
            
            foreach ((string attributeName, string defaultValue) in Instance._hatAttributeDefinitions)
                attributesToAdd[attributeName] = node.Attributes?[attributeName]?.Value ?? defaultValue;
            
            Instance.HatAttributes[hatName] = attributesToAdd;
            
            if (hatName != HAT_NONE)
                hats.Add(hatName);
        }
    }
}