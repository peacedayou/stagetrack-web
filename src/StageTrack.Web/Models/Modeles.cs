using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StageTrack.Web.Models;

/// <summary>Même énumération que l'API ; elle circule en texte (« Entrevue », « Offre »…).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<StatutCandidature>))]
public enum StatutCandidature
{
    AEnvoyer,
    Envoyee,
    Entrevue,
    Offre,
    Refusee
}

public static class Statuts
{
    public static IReadOnlyList<StatutCandidature> Tous { get; } = Enum.GetValues<StatutCandidature>();

    public static string Libelle(StatutCandidature statut) => statut switch
    {
        StatutCandidature.AEnvoyer => "À envoyer",
        StatutCandidature.Envoyee => "Envoyée",
        StatutCandidature.Entrevue => "Entrevue",
        StatutCandidature.Offre => "Offre",
        StatutCandidature.Refusee => "Refusée",
        _ => statut.ToString()
    };

    /// <summary>Classe CSS de la pastille de statut.</summary>
    public static string Classe(StatutCandidature statut) => "statut statut-" + statut.ToString().ToLowerInvariant();
}

public record Entreprise(int Id, string Nom, string? Ville, string? SiteWeb, int NombreCandidatures);

public record Candidature(
    int Id,
    string Poste,
    StatutCandidature Statut,
    DateOnly? DateEnvoi,
    string? Lien,
    string? Notes,
    int EntrepriseId,
    string Entreprise);

public record Statistiques(int Total, Dictionary<string, int> ParStatut, double TauxEntrevue);

/// <summary>Formulaire de création d'entreprise ; mêmes règles que l'API, vérifiées avant l'envoi.</summary>
public class EntrepriseFormulaire
{
    [Required(ErrorMessage = "Le nom est obligatoire.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Le nom doit compter de 2 à 150 caractères.")]
    public string Nom { get; set; } = "";

    [StringLength(100, ErrorMessage = "La ville compte au plus 100 caractères.")]
    public string? Ville { get; set; }

    [Url(ErrorMessage = "Le site web doit être une adresse complète (https://…).")]
    public string? SiteWeb { get; set; }
}

public class CandidatureFormulaire
{
    [Required(ErrorMessage = "Le poste est obligatoire.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Le poste doit compter de 2 à 200 caractères.")]
    public string Poste { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Choisis une entreprise.")]
    public int EntrepriseId { get; set; }

    public StatutCandidature Statut { get; set; } = StatutCandidature.AEnvoyer;

    [Url(ErrorMessage = "Le lien doit être une adresse complète (https://…).")]
    public string? Lien { get; set; }

    [StringLength(2000, ErrorMessage = "Les notes comptent au plus 2000 caractères.")]
    public string? Notes { get; set; }
}
