﻿using p3ppc.LazyTranslationFramework.Template.Configuration;
using System.ComponentModel;

namespace p3ppc.LazyTranslationFramework.Configuration;
public class Config : Configurable<Config>
{
    [DisplayName("Debug Mode")]
    [Description("Logs additional information to the console that is useful for debugging.")]
    [DefaultValue(false)]
    public bool DebugEnabled { get; set; } = false;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}
