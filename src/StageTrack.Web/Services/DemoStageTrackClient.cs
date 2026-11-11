using StageTrack.Web.Models;

namespace StageTrack.Web.Services;

/// <summary>
/// Mode démonstration : données en mémoire, mêmes règles métier que l'API
/// (nom d'entreprise unique, date d'envoi fixée au passage à « Envoyée », suppression en cascade).
/// Sert pour la page publiée sur GitHub Pages et pour les tests.
/// </summary>
public class DemoStageTrackClient : IStageTrackClient
{
    private readonly List<EntrepriseInterne> _entreprises = [];
    private readonly List<CandidatureInterne> _candidatures = [];
    private readonly Func<DateOnly> _aujourdhui;
    private int _prochainId = 1;

    public DemoStageTrackClient(bool avecExemples = true, Func<DateOnly>? aujourdhui = null)
    {
        _aujourdhui = aujourdhui ?? (() => DateOnly.FromDateTime(DateTime.Today));
        if (avecExemples) AjouterExemples();
    }

    public bool EstDemo => true;

    public Task<IReadOnlyList<Entreprise>> ListerEntreprisesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Entreprise>>(_entreprises.OrderBy(e => e.Nom).Select(VersDto).ToList());

    public Task<Entreprise> CreerEntrepriseAsync(EntrepriseFormulaire f, CancellationToken ct = default)
    {
        var nom = f.Nom.Trim();
        if (nom.Length < 2)
            throw new StageTrackException("Le nom doit compter de 2 à 150 caractères.", 400);
        if (_entreprises.Any(e => string.Equals(e.Nom, nom, StringComparison.OrdinalIgnoreCase)))
            throw new StageTrackException($"L'entreprise « {nom} » existe déjà.", 409);

        var entreprise = new EntrepriseInterne(_prochainId++, nom, Vide(f.Ville), Vide(f.SiteWeb));
        _entreprises.Add(entreprise);
        return Task.FromResult(VersDto(entreprise));
    }

    public Task SupprimerEntrepriseAsync(int id, CancellationToken ct = default)
    {
        if (_entreprises.RemoveAll(e => e.Id == id) == 0)
            throw new StageTrackException("Élément introuvable (il a peut-être déjà été supprimé).", 404);
        _candidatures.RemoveAll(c => c.EntrepriseId == id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Candidature>> ListerCandidaturesAsync(
        StatutCandidature? statut = null, string? recherche = null, CancellationToken ct = default)
    {
        IEnumerable<CandidatureInterne> requete = _candidatures;
        if (statut is not null) requete = requete.Where(c => c.Statut == statut);
        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var q = recherche.Trim();
            requete = requete.Where(c =>
                c.Poste.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                NomEntreprise(c.EntrepriseId).Contains(q, StringComparison.OrdinalIgnoreCase));
        }
        return Task.FromResult<IReadOnlyList<Candidature>>(requete.OrderByDescending(c => c.Id).Select(VersDto).ToList());
    }

    public Task<Candidature> CreerCandidatureAsync(CandidatureFormulaire f, CancellationToken ct = default)
    {
        if (_entreprises.All(e => e.Id != f.EntrepriseId))
            throw new StageTrackException("Entreprise introuvable.", 400);

        var candidature = new CandidatureInterne(_prochainId++, f.Poste.Trim(), f.EntrepriseId)
        {
            Statut = f.Statut,
            Lien = Vide(f.Lien),
            Notes = Vide(f.Notes),
            DateEnvoi = f.Statut == StatutCandidature.AEnvoyer ? null : _aujourdhui()
        };
        _candidatures.Add(candidature);
        return Task.FromResult(VersDto(candidature));
    }

    public Task<Candidature> ChangerStatutAsync(int id, StatutCandidature statut, CancellationToken ct = default)
    {
        var c = _candidatures.FirstOrDefault(x => x.Id == id)
                ?? throw new StageTrackException("Élément introuvable (il a peut-être déjà été supprimé).", 404);
        c.Statut = statut;
        if (statut == StatutCandidature.Envoyee && c.DateEnvoi is null)
            c.DateEnvoi = _aujourdhui();
        return Task.FromResult(VersDto(c));
    }

    public Task SupprimerCandidatureAsync(int id, CancellationToken ct = default)
    {
        if (_candidatures.RemoveAll(c => c.Id == id) == 0)
            throw new StageTrackException("Élément introuvable (il a peut-être déjà été supprimé).", 404);
        return Task.CompletedTask;
    }

    /// <summary>Même calcul que l'API : (entrevues + offres) / candidatures envoyées, en %.</summary>
    public Task<Statistiques> StatistiquesAsync(CancellationToken ct = default)
    {
        var parStatut = Statuts.Tous.ToDictionary(s => s.ToString(), s => _candidatures.Count(c => c.Statut == s));
        var total = _candidatures.Count;
        var envoyees = total - parStatut[nameof(StatutCandidature.AEnvoyer)];
        var entrevues = parStatut[nameof(StatutCandidature.Entrevue)] + parStatut[nameof(StatutCandidature.Offre)];
        var taux = envoyees == 0 ? 0 : Math.Round(100.0 * entrevues / envoyees, 1);
        return Task.FromResult(new Statistiques(total, parStatut, taux));
    }

    private void AjouterExemples()
    {
        var desjardins = Ajouter("Desjardins", "Lévis", "https://www.desjardins.com");
        var ville = Ajouter("Ville de Lévis", "Lévis", null);
        var startup = Ajouter("Studio Fleuve (fictif)", "Québec", null);

        AjouterCandidature("Stagiaire développement .NET", desjardins, StatutCandidature.Entrevue, -12);
        AjouterCandidature("Technicien soutien TI", ville, StatutCandidature.Envoyee, -5);
        AjouterCandidature("Développeur web junior", startup, StatutCandidature.AEnvoyer, null);
        AjouterCandidature("Analyste sécurité (stage)", desjardins, StatutCandidature.Refusee, -30);
    }

    private int Ajouter(string nom, string? ville, string? site)
    {
        var e = new EntrepriseInterne(_prochainId++, nom, ville, site);
        _entreprises.Add(e);
        return e.Id;
    }

    private void AjouterCandidature(string poste, int entrepriseId, StatutCandidature statut, int? joursDepuisEnvoi) =>
        _candidatures.Add(new CandidatureInterne(_prochainId++, poste, entrepriseId)
        {
            Statut = statut,
            DateEnvoi = joursDepuisEnvoi is null ? null : _aujourdhui().AddDays(joursDepuisEnvoi.Value)
        });

    private string NomEntreprise(int id) => _entreprises.FirstOrDefault(e => e.Id == id)?.Nom ?? "";

    private Entreprise VersDto(EntrepriseInterne e) =>
        new(e.Id, e.Nom, e.Ville, e.SiteWeb, _candidatures.Count(c => c.EntrepriseId == e.Id));

    private Candidature VersDto(CandidatureInterne c) =>
        new(c.Id, c.Poste, c.Statut, c.DateEnvoi, c.Lien, c.Notes, c.EntrepriseId, NomEntreprise(c.EntrepriseId));

    private static string? Vide(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private sealed record EntrepriseInterne(int Id, string Nom, string? Ville, string? SiteWeb);

    private sealed class CandidatureInterne(int id, string poste, int entrepriseId)
    {
        public int Id { get; } = id;
        public string Poste { get; } = poste;
        public int EntrepriseId { get; } = entrepriseId;
        public StatutCandidature Statut { get; set; }
        public DateOnly? DateEnvoi { get; set; }
        public string? Lien { get; set; }
        public string? Notes { get; set; }
    }
}
