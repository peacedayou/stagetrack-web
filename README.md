# StageTrack Web

![CI](https://github.com/peacedayou/stagetrack-web/actions/workflows/ci.yml/badge.svg)

Interface **Blazor WebAssembly** (.NET 10) pour [StageTrack API](https://github.com/peacedayou/stagetrack-api) :
suivre ses candidatures de stage, leurs statuts et son taux d'entrevue.

**Démo en ligne :** https://peacedayou.github.io/stagetrack-web/ (mode démonstration, données dans le navigateur)

## Fonctionnalités

- **Tableau de bord** : nombre de candidatures, entrevues et offres, taux d'entrevue, candidatures à envoyer
- **Liste filtrable** par statut et par recherche (poste ou entreprise), changement de statut en un clic
- **Formulaires validés** avant l'envoi (`DataAnnotations`, mêmes règles que l'API) et erreurs de l'API
  affichées en clair (doublon 409, validation 400, élément introuvable 404)
- **Suppression avec confirmation** quand une entreprise a des candidatures (suppression en cascade côté API)
- **Mode démonstration** : sans API configurée, un client en mémoire applique les mêmes règles métier
- Accessible au clavier, libellés pour les lecteurs d'écran, mise en page adaptée au téléphone

## Architecture

```
src/StageTrack.Web/
  Models/       DTO de l'API, formulaires validés, libellés des statuts
  Services/     IStageTrackClient : client HTTP de l'API + client de démonstration en mémoire
  Pages/        Accueil (tableau de bord), Entreprises, NouvelleCandidature
  Components/   PastilleStatut
  wwwroot/      index.html, styles, appsettings.json (ApiBaseUrl)
tests/StageTrack.Web.Tests/
  HttpClientTests     sérialisation, URL, traduction des ProblemDetails
  DemoClientTests     règles métier du mode démonstration
  ComposantsTests     pages rendues avec bUnit (filtres, formulaires, confirmation, navigation)
```

Le choix de l'implémentation se fait au démarrage (`Program.cs`) : si `ApiBaseUrl` est vide,
l'application passe en mode démonstration.

## Démarrage avec l'API

```bash
# 1. L'API (dépôt stagetrack-api), sur http://localhost:5000
dotnet run --project src/StageTrack.Api

# 2. L'interface, sur http://localhost:5100 (appsettings.Development.json pointe vers l'API)
dotnet run --project src/StageTrack.Web
```

L'API autorise `http://localhost:5100` et `https://peacedayou.github.io` grâce à sa configuration CORS
(`Cors:Origins`).

## Tests

```bash
dotnet test tests/StageTrack.Web.Tests
```

## Déploiement

À chaque poussée sur `main`, la CI exécute les tests, publie l'application et la déploie sur GitHub Pages
(adresse de base réécrite en `/stagetrack-web/`, page `404.html` pour les routes de l'application).
