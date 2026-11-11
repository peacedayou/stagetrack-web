using System.Net;
using System.Text;
using StageTrack.Web.Models;
using StageTrack.Web.Services;

namespace StageTrack.Web.Tests;

/// <summary>Vérifie le client HTTP contre des réponses telles que l'API les renvoie.</summary>
public class HttpClientTests
{
    private sealed class FauxServeur(HttpStatusCode code, string corps) : HttpMessageHandler
    {
        public HttpRequestMessage? Derniere { get; private set; }
        public string? CorpsRecu { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requete, CancellationToken ct)
        {
            Derniere = requete;
            CorpsRecu = requete.Content is null ? null : await requete.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(code) { Content = new StringContent(corps, Encoding.UTF8, "application/json") };
        }
    }

    private static (HttpStageTrackClient client, FauxServeur serveur) Creer(HttpStatusCode code, string corps)
    {
        var serveur = new FauxServeur(code, corps);
        return (new HttpStageTrackClient(new HttpClient(serveur) { BaseAddress = new Uri("http://api.test/") }), serveur);
    }

    [Fact]
    public async Task Lit_les_candidatures_avec_le_statut_en_texte()
    {
        var (client, _) = Creer(HttpStatusCode.OK,
            """[{"id":3,"poste":"Dev .NET","statut":"Entrevue","dateEnvoi":"2026-11-02","lien":null,"notes":null,"entrepriseId":1,"entreprise":"Desjardins"}]""");

        var liste = await client.ListerCandidaturesAsync();

        var c = Assert.Single(liste);
        Assert.Equal(StatutCandidature.Entrevue, c.Statut);
        Assert.Equal(new DateOnly(2026, 11, 2), c.DateEnvoi);
        Assert.Equal("Desjardins", c.Entreprise);
    }

    [Fact]
    public void Url_de_liste_encode_la_recherche_et_le_statut()
    {
        Assert.Equal("api/candidatures", HttpStageTrackClient.UrlCandidatures(null, "  "));
        Assert.Equal("api/candidatures?statut=Offre&q=d%C3%A9v%20web",
            HttpStageTrackClient.UrlCandidatures(StatutCandidature.Offre, " dév web "));
    }

    [Fact]
    public async Task Changer_le_statut_envoie_le_statut_en_texte()
    {
        var (client, serveur) = Creer(HttpStatusCode.OK,
            """{"id":3,"poste":"Dev","statut":"Envoyee","dateEnvoi":"2026-11-10","lien":null,"notes":null,"entrepriseId":1,"entreprise":"X"}""");

        await client.ChangerStatutAsync(3, StatutCandidature.Envoyee);

        Assert.Equal(HttpMethod.Patch, serveur.Derniere!.Method);
        Assert.Equal("/api/candidatures/3/statut", serveur.Derniere.RequestUri!.AbsolutePath);
        Assert.Contains("\"statut\":\"Envoyee\"", serveur.CorpsRecu);
    }

    [Fact]
    public async Task Conflit_409_donne_le_titre_du_ProblemDetails()
    {
        var (client, _) = Creer(HttpStatusCode.Conflict, """{"title":"L'entreprise « Desjardins » existe déjà.","status":409}""");

        var ex = await Assert.ThrowsAsync<StageTrackException>(() =>
            client.CreerEntrepriseAsync(new EntrepriseFormulaire { Nom = "Desjardins" }));

        Assert.Equal(409, ex.CodeHttp);
        Assert.Equal("L'entreprise « Desjardins » existe déjà.", ex.Message);
    }

    [Fact]
    public async Task Erreur_de_validation_400_regroupe_les_messages()
    {
        var (client, _) = Creer(HttpStatusCode.BadRequest,
            """{"title":"One or more validation errors occurred.","status":400,"errors":{"EntrepriseId":["Entreprise introuvable."]}}""");

        var ex = await Assert.ThrowsAsync<StageTrackException>(() =>
            client.CreerCandidatureAsync(new CandidatureFormulaire { Poste = "Dev", EntrepriseId = 99 }));

        Assert.Equal("Entreprise introuvable.", ex.Message);
    }

    [Fact]
    public async Task Erreur_404_sans_corps_donne_un_message_clair()
    {
        var (client, _) = Creer(HttpStatusCode.NotFound, "");

        var ex = await Assert.ThrowsAsync<StageTrackException>(() => client.SupprimerCandidatureAsync(5));

        Assert.Equal(404, ex.CodeHttp);
        Assert.Contains("introuvable", ex.Message);
    }

    [Fact]
    public async Task Creer_une_entreprise_retire_les_champs_vides()
    {
        var (client, serveur) = Creer(HttpStatusCode.Created,
            """{"id":1,"nom":"Desjardins","ville":null,"siteWeb":null,"nombreCandidatures":0}""");

        await client.CreerEntrepriseAsync(new EntrepriseFormulaire { Nom = "  Desjardins ", Ville = " ", SiteWeb = "" });

        Assert.Contains("\"nom\":\"Desjardins\"", serveur.CorpsRecu);
        Assert.Contains("\"ville\":null", serveur.CorpsRecu);
    }
}
