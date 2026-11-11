using StageTrack.Web.Models;

namespace StageTrack.Web.Services;

/// <summary>
/// Accès aux données de StageTrack. Deux implémentations : l'API réelle (<see cref="HttpStageTrackClient"/>)
/// et un mode démonstration en mémoire (<see cref="DemoStageTrackClient"/>) qui applique les mêmes règles.
/// </summary>
public interface IStageTrackClient
{
    /// <summary>Vrai en mode démonstration (aucune API branchée).</summary>
    bool EstDemo { get; }

    Task<IReadOnlyList<Entreprise>> ListerEntreprisesAsync(CancellationToken ct = default);
    Task<Entreprise> CreerEntrepriseAsync(EntrepriseFormulaire formulaire, CancellationToken ct = default);
    Task SupprimerEntrepriseAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Candidature>> ListerCandidaturesAsync(StatutCandidature? statut = null, string? recherche = null, CancellationToken ct = default);
    Task<Candidature> CreerCandidatureAsync(CandidatureFormulaire formulaire, CancellationToken ct = default);
    Task<Candidature> ChangerStatutAsync(int id, StatutCandidature statut, CancellationToken ct = default);
    Task SupprimerCandidatureAsync(int id, CancellationToken ct = default);

    Task<Statistiques> StatistiquesAsync(CancellationToken ct = default);
}

/// <summary>Erreur renvoyée par l'API, avec un message lisible tiré du ProblemDetails.</summary>
public class StageTrackException(string message, int? codeHttp = null) : Exception(message)
{
    public int? CodeHttp { get; } = codeHttp;
}
