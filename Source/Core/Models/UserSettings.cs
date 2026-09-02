using System;
using System.Collections.Generic;
using System.Text;
using Rampastring.Tools;
using Rampastring.Tools.Ini;

namespace WWSTE.Core.Models;

public class UserSettings
{
    private readonly IniDeserializationOptions _options = new()
    {
        SectionName = nameof(Settings),
        SkipEmptyKeys = true,
        SkipUnableToParseTypes = true,
    };

    private readonly IniSerializer srz = new(new Conversions());
    public Settings Data;

    UserSettings()
    {
        Data = srz.Deserialize<Settings>(new IniFile($"Resources/{nameof(Settings)}.ini"));
    }
}
