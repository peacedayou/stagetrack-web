using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StageTrack.Web.Models;

namespace StageTrack.Web.Services;

/// <summary>Client typé de l'API StageTrack (voir le dépôt stagetrack-api).</summary>
public class HttpStageTrackClient(HttpClient http) : IStageTrackClient
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public bool EstDemo => false;

    public async Task<IReadOnlyList<Entreprise>> ListerEntreprisesAsync(CancellationToken ct = default) =>
        await LireAsync<List<Entreprise>>(await http.GetAsync("api/entreprises", ct), ct);

    public async Task<Entreprise> CreerEntrepriseAsync(EntrepriseFormulaire f, CancellationToken ct = default) =>
        await LireAsync<Entreprise>(await http.PostAsJsonAsync("api/entreprises",
            new { nom = f.Nom.Trim(), ville = Vide(f.Ville), siteWeb = Vide(f.SiteWeb) }, Json, ct), ct);

    public async Task SupprimerEntrepriseAsync(int id, CancellationToken ct = default) =>
        await VerifierAsync(await http.DeleteAsync($"api/entreprises/{id}", ct), ct);

    public async Task<IReadOnlyList<Candidature>> ListerCandidaturesAsync(
        StatutCandidature? statut = null, string? recherche = null, CancellationToken ct = default) =>
        await LireAsync<List<Candidature>>(await http.GetAsync(UrlCandidatures(statut, recherche), ct), ct);

    public async Task<Candidature> CreerCandidatureAsync(CandidatureFormulaire f, CancellationToken ct = default) =>
        await LireAsync<Candidature>(await http.PostAsJsonAsync("api/candidatures", new
        {
            poste = f.Poste.Trim(),
            entrepriseId = f.EntrepriseId,
            statut = f.Statut,
            // Comme en mode démo : une candidature créée déjà envoyée reçoit la date du jour.
            dateEnvoi = f.Statut == StatutCandidature.AEnvoyer ? (DateOnly?)null : DateOnly.FromDateTime(DateTime.Today),
            lien = Vide(f.Lien),
            notes = Vide(f.Notes)
        }, Json, ct), ct);

    public async Task<Candidature> ChangerStatutAsync(int id, StatutCandidature statut, CancellationToken ct = default) =>
        await LireAsync<Candidature>(await http.PatchAsJsonAsync($"api/candidatures/{id}/statut", new { statut }, Json, ct), ct);

    public async Task SupprimerCandidatureAsync(int id, CancellationToken ct = default) =>
        await VerifierAsync(await http.DeleteAsync($"api/candidatures/{id}", ct), ct);

    public async Task<Statistiques> StatistiquesAsync(CancellationToken ct = default) =>
        await LireAsync<Statistiques>(await http.GetAsync("api/statistiques", ct), ct);

    /// <summary>Construit l'URL de liste avec les filtres, encodés.</summary>
    internal static string UrlCandidatures(StatutCandidature? statut, string? recherche)
    {
        var parametres = new List<string>();
        if (statut is not null) parametres.Add("statut=" + statut);
        if (!string.IsNullOrWhiteSpace(recherche)) parametres.Add("q=" + Uri.EscapeDataString(recherche.Trim()));
        return parametres.Count == 0 ? "api/candidatures" : "api/candidatures?" + string.Join("&", parametres);
    }

    private static string? Vide(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static async Task<T> LireAsync<T>(HttpResponseMessage reponse, CancellationToken ct)
    {
        await VerifierAsync(reponse, ct);
        return await reponse.Content.ReadFromJsonAsync<T>(Json, ct)
               ?? throw new StageTrackException("Réponse vide de l'API.");
    }

    /// <summary>Transforme une réponse d'erreur (ProblemDetails) en message clair pour l'interface.</summary>
    internal static async Task VerifierAsync(HttpResponseMessage reponse, CancellationToken ct)
    {
        if (reponse.IsSuccessStatusCode) return;

        var code = (int)reponse.StatusCode;
        string? message = null;
        try
        {
            using var doc = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync(ct));
            var racine = doc.RootElement;
            if (racine.TryGetProperty("errors", out var erreurs) && erreurs.ValueKind == JsonValueKind.Object)
            {
                message = string.Join(" ", erreurs.EnumerateObject()
                    .SelectMany(e => e.Value.EnumerateArray())
                    .Select(v => v.GetString())
                    .Where(v => !string.IsNullOrWhiteSpace(v)));
            }
            if (string.IsNullOrWhiteSpace(message) && racine.TryGetProperty("title", out var titre))
                message = titre.GetString();
        }
        catch (JsonException)
        {
            // Corps vide ou non JSON : message par défaut ci-dessous.
        }

        message = string.IsNullOrWhiteSpace(message)
            ? reponse.StatusCode switch
            {
                HttpStatusCode.NotFound => "Élément introuvable (il a peut-être déjà été supprimé).",
                _ => $"L'API a répondu avec une erreur ({code})."
            }
            : message;
        throw new StageTrackException(message, code);
    }
}
