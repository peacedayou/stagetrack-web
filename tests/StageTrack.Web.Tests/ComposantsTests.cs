using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using StageTrack.Web.Models;
using StageTrack.Web.Pages;
using StageTrack.Web.Services;

namespace StageTrack.Web.Tests;

/// <summary>Tests des pages avec bUnit, branchées sur le mode démonstration.</summary>
public class ComposantsTests : BunitContext
{
    private readonly DemoStageTrackClient _client = new(aujourdhui: () => new DateOnly(2026, 11, 10));

    public ComposantsTests() => Services.AddSingleton<IStageTrackClient>(_client);

    [Fact]
    public void Tableau_de_bord_affiche_les_statistiques_et_les_candidatures()
    {
        var page = Render<Accueil>();

        Assert.Equal("4", page.Find("[data-test=total]").TextContent);
        Assert.Equal("1", page.Find("[data-test=entrevues]").TextContent);
        Assert.Contains("33", page.Find("[data-test=taux]").TextContent);
        Assert.Equal(4, page.FindAll("[data-test=ligne]").Count);
    }

    [Fact]
    public void Filtrer_par_statut_reduit_la_liste()
    {
        var page = Render<Accueil>();

        page.Find("[data-test=filtre-statut]").Change("Refusee");

        page.WaitForAssertion(() => Assert.Single(page.FindAll("[data-test=ligne]")));
        Assert.Contains("Analyste sécurité", page.Find("[data-test=ligne]").TextContent);
    }

    [Fact]
    public void Recherche_sans_resultat_affiche_un_message()
    {
        var page = Render<Accueil>();

        page.Find("[data-test=recherche]").Input("introuvable");

        page.WaitForAssertion(() => Assert.Contains("ne correspond aux filtres", page.Find("[data-test=aucune]").TextContent));
    }

    [Fact]
    public async Task Changer_le_statut_met_a_jour_les_statistiques()
    {
        var page = Render<Accueil>();
        var aEnvoyer = (await _client.ListerCandidaturesAsync(StatutCandidature.AEnvoyer)).Single();
        var index = page.FindAll("[data-test=ligne]").ToList().FindIndex(l => l.TextContent.Contains(aEnvoyer.Poste));

        page.FindAll("[data-test=changer-statut]")[index].Change("Entrevue");

        page.WaitForAssertion(() => Assert.Equal("2", page.Find("[data-test=entrevues]").TextContent));
        Assert.Equal("0", page.Find("[data-test=a-envoyer]").TextContent);
    }

    [Fact]
    public void Entreprise_sans_nom_affiche_le_message_de_validation()
    {
        var page = Render<Entreprises>();

        page.Find("form").Submit();

        Assert.Contains("Le nom est obligatoire.", page.Markup);
    }

    [Fact]
    public void Entreprise_en_double_affiche_l_erreur_de_l_API()
    {
        var page = Render<Entreprises>();

        page.Find("[data-test=nom]").Change("Desjardins");
        page.Find("form").Submit();

        page.WaitForAssertion(() => Assert.Contains("existe déjà", page.Find("[data-test=erreur]").TextContent));
    }

    [Fact]
    public void Ajouter_une_entreprise_l_affiche_dans_la_liste()
    {
        var page = Render<Entreprises>();

        page.Find("[data-test=nom]").Change("Cégep de Lévis");
        page.Find("form").Submit();

        page.WaitForAssertion(() => Assert.Equal(4, page.FindAll("[data-test=entreprise]").Count));
    }

    [Fact]
    public void Supprimer_une_entreprise_demande_confirmation()
    {
        var page = Render<Entreprises>();

        page.FindAll("[data-test=supprimer]")[0].Click();
        Assert.Contains("Supprimer aussi ses", page.Markup);
        Assert.Equal(3, page.FindAll("[data-test=entreprise]").Count);

        page.Find("[data-test=confirmer]").Click();
        page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("[data-test=entreprise]").Count));
    }

    [Fact]
    public async Task Nouvelle_candidature_est_creee_puis_retour_au_tableau_de_bord()
    {
        var entreprise = (await _client.ListerEntreprisesAsync()).First();
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("candidatures/nouvelle");
        var page = Render<NouvelleCandidature>();

        page.Find("[data-test=poste]").Change("Stagiaire DevOps");
        page.Find("[data-test=entreprise]").Change(entreprise.Id.ToString());
        page.Find("form").Submit();

        page.WaitForAssertion(() => Assert.Equal(navigation.BaseUri, navigation.Uri));
        Assert.Contains(await _client.ListerCandidaturesAsync(), c => c.Poste == "Stagiaire DevOps");
    }

    [Fact]
    public void Nouvelle_candidature_exige_une_entreprise()
    {
        var page = Render<NouvelleCandidature>();

        page.Find("[data-test=poste]").Change("Stagiaire DevOps");
        page.Find("form").Submit();

        Assert.Contains("Choisis une entreprise.", page.Markup);
    }
}
