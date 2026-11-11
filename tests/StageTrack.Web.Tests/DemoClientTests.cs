using StageTrack.Web.Models;
using StageTrack.Web.Services;

namespace StageTrack.Web.Tests;

/// <summary>Le mode démonstration doit suivre les mêmes règles que l'API.</summary>
public class DemoClientTests
{
    private static readonly DateOnly Aujourdhui = new(2026, 11, 10);

    private static DemoStageTrackClient Vide() => new(avecExemples: false, aujourdhui: () => Aujourdhui);

    [Fact]
    public async Task Nom_d_entreprise_en_double_est_refuse_sans_tenir_compte_de_la_casse()
    {
        var client = Vide();
        await client.CreerEntrepriseAsync(new EntrepriseFormulaire { Nom = "Desjardins" });

        var ex = await Assert.ThrowsAsync<StageTrackException>(() =>
            client.CreerEntrepriseAsync(new EntrepriseFormulaire { Nom = " desjardins " }));

        Assert.Equal(409, ex.CodeHttp);
    }

    [Fact]
    public async Task Passer_a_Envoyee_fixe_la_date_d_envoi_une_seule_fois()
    {
        var client = Vide();
        var e = await client.CreerEntrepriseAsync(new EntrepriseFormulaire { Nom = "Ville de Lévis" });
        var c = await client.CreerCandidatureAsync(new CandidatureFormulaire { Poste = "Soutien TI", EntrepriseId = e.Id });
        Assert.Null(c.DateEnvoi);

        var envoyee = await client.ChangerStatutAsync(c.Id, StatutCandidature.Envoyee);
        Assert.Equal(Aujourdhui, envoyee.DateEnvoi);

        var entrevue = await client.ChangerStatutAsync(c.Id, StatutCandidature.Entrevue);
        Assert.Equal(Aujourdhui, entrevue.DateEnvoi);
    }

    [Fact]
    public async Task Supprimer_une_entreprise_supprime_ses_candidatures()
    {
        var client = Vide();
        var e = await client.CreerEntrepriseAsync(new EntrepriseFormulaire { Nom = "Desjardins" });
        await client.CreerCandidatureAsync(new CandidatureFormulaire { Poste = "Dev", EntrepriseId = e.Id });

        await client.SupprimerEntrepriseAsync(e.Id);

        Assert.Empty(await client.ListerCandidaturesAsync());
    }

    [Fact]
    public async Task Candidature_pour_entreprise_inexistante_est_refusee()
    {
        var ex = await Assert.ThrowsAsync<StageTrackException>(() =>
            Vide().CreerCandidatureAsync(new CandidatureFormulaire { Poste = "Dev", EntrepriseId = 42 }));
        Assert.Equal(400, ex.CodeHttp);
    }

    [Fact]
    public async Task Statistiques_suivent_la_formule_de_l_API()
    {
        // Exemples : 1 entrevue, 1 envoyée, 1 à envoyer, 1 refusée → 1 entrevue sur 3 envoyées = 33,3 %
        var stats = await new DemoStageTrackClient(aujourdhui: () => Aujourdhui).StatistiquesAsync();

        Assert.Equal(4, stats.Total);
        Assert.Equal(33.3, stats.TauxEntrevue);
        Assert.Equal(1, stats.ParStatut["AEnvoyer"]);
    }

    [Fact]
    public async Task Recherche_porte_sur_le_poste_et_l_entreprise()
    {
        var client = new DemoStageTrackClient(aujourdhui: () => Aujourdhui);

        Assert.Equal(2, (await client.ListerCandidaturesAsync(recherche: "desjardins")).Count);
        Assert.Single(await client.ListerCandidaturesAsync(recherche: "SOUTIEN"));
        Assert.Single(await client.ListerCandidaturesAsync(StatutCandidature.Refusee));
    }
}
