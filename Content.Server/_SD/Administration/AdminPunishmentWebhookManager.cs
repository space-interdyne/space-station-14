using System.Text;
using Content.Server.Discord;
using Content.Server.GameTicking;
using Content.Shared._SD.CCVar;
using Content.Shared.Database;
using Robust.Server;
using Robust.Shared.Configuration;

namespace Content.Server._SD.Administration;

/// <summary>
/// Sends Discord webhook notifications for admin punishments:
/// server bans, role bans, and non-secret notes (not watchlists).
/// </summary>
public sealed partial class AdminPunishmentWebhookManager : IPostInjectInit
{
    [Dependency] private IBaseServer _baseServer = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private DiscordWebhook _discord = default!;
    [Dependency] private IEntitySystemManager _systems = default!;
    [Dependency] private ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    void IPostInjectInit.PostInject()
    {
        _sawmill = _logManager.GetSawmill("discord.admin_punishment");
    }

    public void SendServerBan(
        int? banId,
        string target,
        string admin,
        string reason,
        NoteSeverity severity,
        DateTimeOffset? expires)
    {
        SendPunishment(
            Loc.GetString("discord-admin-punishment-server-ban-title", ("banId", banId?.ToString() ?? "?")),
            target,
            admin,
            reason,
            severity,
            expires,
            GetSeverityColor(severity));
    }

    public void SendRoleBan(
        int? banId,
        string target,
        string admin,
        string reason,
        NoteSeverity severity,
        DateTimeOffset? expires,
        string roles)
    {
        SendPunishment(
            Loc.GetString("discord-admin-punishment-role-ban-title", ("banId", banId?.ToString() ?? "?")),
            target,
            admin,
            reason,
            severity,
            expires,
            GetSeverityColor(severity),
            roles);
    }

    /// <summary>
    /// Sends a webhook for a non-secret admin note. Callers must not invoke this for secret notes or watchlists.
    /// </summary>
    public void SendNote(
        int noteId,
        string target,
        string admin,
        string message,
        NoteSeverity severity,
        DateTimeOffset? expires)
    {
        SendPunishment(
            Loc.GetString("discord-admin-punishment-note-title", ("noteId", noteId)),
            target,
            admin,
            message,
            severity,
            expires,
            GetSeverityColor(severity));
    }

    private async void SendPunishment(
        string title,
        string target,
        string admin,
        string reason,
        NoteSeverity severity,
        DateTimeOffset? expires,
        int color,
        string? roles = null)
    {
        try
        {
            var webhookUrl = _cfg.GetCVar(SDCCVars.DiscordBanWebhook);
            if (string.IsNullOrWhiteSpace(webhookUrl))
                return;

            var webhookData = await _discord.GetWebhook(webhookUrl);
            if (webhookData == null)
                return;

            var description = BuildDescription(target, admin, reason, severity, expires, roles);
            var roundId = 0;
            if (_systems.TryGetEntitySystem<GameTicker>(out var ticker))
                roundId = ticker.RoundId;

            var payload = new WebhookPayload
            {
                Username = Loc.GetString("discord-admin-punishment-username"),
                Embeds =
                [
                    new WebhookEmbed
                    {
                        Title = title,
                        Description = description,
                        Color = color,
                        Footer = new WebhookEmbedFooter
                        {
                            Text = Loc.GetString(
                                "discord-admin-punishment-footer",
                                ("serverName", _baseServer.ServerName),
                                ("roundId", roundId)),
                        },
                    },
                ],
            };

            await _discord.CreateMessage(webhookData.Value.ToIdentifier(), payload);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error while sending admin punishment Discord webhook:\n{e}");
        }
    }

    private static string BuildDescription(
        string target,
        string admin,
        string reason,
        NoteSeverity severity,
        DateTimeOffset? expires,
        string? roles)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString("discord-admin-punishment-target", ("target", target)));
        sb.AppendLine(Loc.GetString("discord-admin-punishment-admin", ("admin", admin)));
        sb.AppendLine(Loc.GetString("discord-admin-punishment-reason", ("reason", reason)));
        sb.AppendLine(Loc.GetString(
            "discord-admin-punishment-severity",
            ("severity", Loc.GetString($"admin-note-editor-severity-{SeverityLocaleKey(severity)}"))));

        if (expires == null)
            sb.AppendLine(Loc.GetString("discord-admin-punishment-expires-permanent"));
        else
            sb.AppendLine(Loc.GetString("discord-admin-punishment-expires", ("expires", expires.Value.ToUnixTimeSeconds())));

        if (!string.IsNullOrWhiteSpace(roles))
            sb.AppendLine(Loc.GetString("discord-admin-punishment-roles", ("roles", roles)));

        return sb.ToString().TrimEnd();
    }

    private static string SeverityLocaleKey(NoteSeverity severity)
    {
        return severity switch
        {
            NoteSeverity.None => "none",
            NoteSeverity.Minor => "low",
            NoteSeverity.Medium => "medium",
            NoteSeverity.High => "high",
            _ => "none",
        };
    }

    /// <summary>
    /// Discord embed color without alpha channel.
    /// </summary>
    private static int GetSeverityColor(NoteSeverity severity)
    {
        var color = severity switch
        {
            NoteSeverity.None => Color.Gray,
            NoteSeverity.Minor => Color.Yellow,
            NoteSeverity.Medium => Color.Orange,
            NoteSeverity.High => Color.Red,
            _ => Color.Gray,
        };

        return color.ToArgb() & 0x00FFFFFF;
    }
}
