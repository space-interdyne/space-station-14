using Content.Server.GameTicking;
using Content.Shared._SD.CCVar;
using Content.Shared.Antag;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;

namespace Content.Server.Antag;

public sealed partial class ServerAntagSelectionSystem
{
    [Dependency] private IConfigurationManager _sdCfg = default!;

    protected override int GetSoftCommandAntagMinPlayers()
        => _sdCfg.GetCVar(SDCCVars.SoftCommandAntagMinPlayers);

    protected override int GetSoftCommandAntagPlayerCount()
    {
        if (GameTicker.RunLevel != GameRunLevel.InRound)
            return GameTicker.ReadyPlayerCount();

        return GetActivePlayerCount();
    }
}
