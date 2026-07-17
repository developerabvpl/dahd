using Dahd.Domain.Enums;

namespace Dahd.Domain.Entities;

/// <summary>
/// ITIL-style incident prioritisation: Impact × Urgency → Priority, and the SLA
/// resolution deadline per priority. Mirrors the Work121 "Raise Incident" model.
/// </summary>
public static class IncidentPolicy
{
    /// <summary>Impact × Urgency → Priority (3×3 grid).</summary>
    public static IncidentPriority Prioritise(IncidentImpact impact, IncidentUrgency urgency) =>
        (impact, urgency) switch
        {
            (IncidentImpact.High, IncidentUrgency.High) => IncidentPriority.Critical,
            (IncidentImpact.High, IncidentUrgency.Medium) => IncidentPriority.High,
            (IncidentImpact.Medium, IncidentUrgency.High) => IncidentPriority.High,
            (IncidentImpact.High, IncidentUrgency.Low) => IncidentPriority.Medium,
            (IncidentImpact.Medium, IncidentUrgency.Medium) => IncidentPriority.Medium,
            (IncidentImpact.Low, IncidentUrgency.High) => IncidentPriority.Medium,
            (IncidentImpact.Medium, IncidentUrgency.Low) => IncidentPriority.Medium,
            (IncidentImpact.Low, IncidentUrgency.Medium) => IncidentPriority.Medium,
            _ => IncidentPriority.Low
        };

    /// <summary>SLA resolution window (hours) per priority.</summary>
    public static int SlaHours(IncidentPriority priority) => priority switch
    {
        IncidentPriority.Critical => 4,
        IncidentPriority.High => 24,
        IncidentPriority.Medium => 72,
        _ => 168
    };

    /// <summary>Resolution deadline from the report time.</summary>
    public static DateTime Deadline(DateTime reportedAtUtc, IncidentPriority priority) =>
        reportedAtUtc.AddHours(SlaHours(priority));
}
