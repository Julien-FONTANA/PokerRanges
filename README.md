# PokerRanges

[![build](https://github.com/Julien-FONTANA/PokerRanges/actions/workflows/build.yml/badge.svg)](https://github.com/Julien-FONTANA/PokerRanges/actions/workflows/build.yml)
[![release](https://img.shields.io/github/v/release/Julien-FONTANA/PokerRanges?sort=semver&color=0078D4)](https://github.com/Julien-FONTANA/PokerRanges/releases)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![platform](https://img.shields.io/badge/platform-Windows%20x64-0078D4)

Assistant de décision Texas Hold'em No Limit en tournoi. Une application de bureau qui suit la main
au rythme où elle se joue et répond à une seule question : **qu'est-ce que je fais, et pourquoi ?**

Préflop, la réponse vient d'un chart, et l'application dit lequel et ce qu'elle a dû arrondir pour
l'atteindre. Postflop, elle reconstruit la range de chaque adversaire à partir de ce qu'il a
réellement fait, puis compare l'espérance de chaque action sous un modèle de réponse adverse
explicite. Le raisonnement est affiché en entier : aucun conseil n'arrive sans sa justification.

Interface française et anglaise, changeable à chaud.

---

## Ce qu'elle fait

- **Conseil préflop par chart** — contexte reconnu automatiquement (ouverture, face à une ouverture,
  squeeze, face à un 3-bet, tapis court…), position, profondeur et nombre de joueurs derrière.
  Fréquences mixtes affichées quand plusieurs actions sont jouables.
- **Conseil postflop par espérance** — chaque taille de mise est évaluée contre la sous-range qui
  *paie*, pas contre la range de départ. C'est là que les outils naïfs se trompent.
- **Grille 13×13** — la stratégie du chart préflop, ou la range assignée à l'adversaire postflop,
  avec les combos que le board et vos cartes lui interdisent.
- **Quatre profils adverses** — équilibré, serré, suiveur, agressif. Change le profil, le conseil
  change avec lui.
- **Table de 2 à 8 joueurs**, tapis inégaux, antes classiques ou payées par la grosse blinde.
- **Mode compact** — fenêtre réduite, toujours au premier plan, saisie des cartes au clavier
  (`askd`, `ks8d3c`), pensée pour répondre en moins d'une seconde pendant qu'on joue.
- **Journal des mains** — une entrée conserve la main entière, pas un résumé : on la recharge et on
  rejoue la décision avec un autre profil ou une autre taille.
- **Reprise automatique** — la main en cours et les réglages survivent à la fermeture.

---

## Comment elle raisonne

La partie intéressante est le postflop. Pour chaque adversaire encore en jeu :

1. **Range de départ** — on lit le chart qui correspond à la situation dans laquelle il a agi, et on
   retient la branche qui correspond à son action réelle (il a relancé → sa range de relance).
2. **Retrait des combos impossibles** — le board et vos deux cartes bloquent des combinaisons ; elles
   sortent de la range avant tout calcul.
3. **Resserrement rue par rue** — chaque action postflop réduit la range. Une mise la polarise
   (meilleures mains pour la valeur, queue de mains faibles pour les bluffs) ; un suivi ne garde que
   la part que la fréquence de défense minimale justifie, décalée selon le profil.
4. **Classement par force** — chaque combo est classé par son équité contre la range elle-même sur ce
   board. Un classement mesuré, pas une table de bonus écrite à la main : c'est ce qui valorise
   correctement un tirage couleur face à une petite paire.
5. **Espérance de chaque action** — pour chaque taille envisagée, on calcule la probabilité que
   l'adversaire se couche, et l'équité **contre ce qui continue**. Les cotes implicites d'un tirage,
   la réalisation d'équité en position ou hors de position, et la re-relance adverse en tête-à-tête
   sont modélisées.

Le calcul est déterministe : à situation identique, conseil identique. Les tirages Monte-Carlo
partent d'une graine fixe et l'avis affiche l'erreur-type qu'il a atteinte.

---

## Prise en main

Il faut le **SDK .NET 10**.

```bash
git clone https://github.com/Julien-FONTANA/PokerRanges.git
cd PokerRanges
dotnet run --project src/PokerRanges.App
```

Les tests :

```bash
dotnet test
```

317 tests : 224 pour le domaine, 55 pour les données, 38 qui pilotent la fenêtre principale de bout
en bout. Pour mesurer la couverture, comme le fait l'intégration continue :

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Les trois projets de test produisent chacun leur rapport ; il faut les fusionner pour lire la
couverture réelle. Le workflow le fait et affiche le résultat dans le résumé de l'exécution.

Produire l'exécutable autonome (Windows, aucun .NET requis sur la machine cible) :

```powershell
.\publish.ps1
```

Le script lance les tests d'abord — publier un binaire qu'on n'a pas vérifié, c'est se préparer à le
rappeler. `-SkipTests` pour un aller-retour rapide, `-ReadyToRun` pour un démarrage plus vif contre
un fichier plus gros. Le résultat est un `PokerRanges.exe` unique dans `publish/win-x64/`.

---

## Raccourcis

| Touche | Action |
|---|---|
| `Alt+P` | Passer |
| `Alt+C` | Checker |
| `Alt+S` | Suivre |
| `Alt+R` | Miser / relancer |
| `Ctrl+Z` | Annuler la dernière action |
| `Ctrl+N` | Nouvelle main |
| `F2` | Basculer mode compact / analyse |

Les lettres nues sont réservées à la saisie des cartes, où `c`, `d`, `h` et `s` sont des couleurs et
non des actions.

---

## Les charts préflop

Les charts livrés sont embarqués dans l'application, puis recopiés au premier lancement dans un
dossier éditable :

```
%APPDATA%\PokerRanges\charts\
```

Un fichier de ce dossier remplace le chart livré de même clé. Modifiez une range, cliquez
**Recharger** dans l'application, le conseil change sans redémarrer. **Restaurer l'origine** réécrit
les fichiers livrés par-dessus — on peut casser une range sans crainte.

Le format est du JSON, les ranges en notation habituelle :

```json
{
  "source": "Ranges d'ouverture standard tournoi ~100bb",
  "charts": [
    {
      "context": "RaiseFirstIn",
      "playersLeftToAct": 3,
      "depthInBigBlinds": 100,
      "actions": [
        {
          "kind": "Raise",
          "sizeInBigBlinds": 2.2,
          "range": "22+, A2s+, K6s+, Q8s+, J8s+, T7s+, 96s+, 86s+, 75s+, 65s, 54s, A8o+, KTo+, QTo+, JTo"
        }
      ]
    }
  ]
}
```

Le fold ne s'écrit jamais : c'est ce qui reste une fois les autres actions retirées, donc aucune main
ne peut être oubliée. Quand aucun chart ne correspond exactement, l'application en désigne un seul —
elle ne mélange jamais deux charts — et affiche chaque écart qu'elle a consenti.

---

## Où sont rangés les fichiers

| Chemin | Contenu |
|---|---|
| `%APPDATA%\PokerRanges\charts\` | Les charts préflop, éditables |
| `%APPDATA%\PokerRanges\settings.json` | Réglages de table, profil, langue |
| `%APPDATA%\PokerRanges\journal.json` | Le journal des mains |
| `%LOCALAPPDATA%\PokerRanges\hand-in-progress.json` | La main interrompue, à reprendre |
| `%LOCALAPPDATA%\PokerRanges\logs\` | Les traces d'exécution |

---

## Architecture

```
src/
  PokerRanges.Core    Le domaine. Cartes, ranges, évaluateur, équité, moteur de pot,
                      conseil préflop et postflop. Ne dépend que des abstractions de log.
  PokerRanges.Data    Les charts JSON et la persistance. Ne connaît pas l'interface.
  PokerRanges.App     Avalonia + MVVM. Ne contient aucune règle de poker.
tests/
  …Core.Tests         Le domaine, cas par cas.
  …Data.Tests         Charts, résolution, persistance.
  …App.Tests          La fenêtre principale, pilotée comme un utilisateur la piloterait.
```

Quelques pièces qui valent le détour :

- **`RankCountHandEvaluator`** — évaluation de 5 à 7 cartes par comptage de rangs, sans allocation,
  sans table précalculée.
- **`HandReplay`** — rejoue la main action par action : antes, blindes, engagements par rue,
  tapis, joueurs couchés, et qui doit parler. C'est le board qui fait autorité sur la rue.
- **`EquityCalculator`** — bascule seul entre énumération exhaustive et Monte-Carlo selon le coût,
  échantillonne par rejet pour respecter la loi jointe de ranges qui se recouvrent, et s'arrête sur
  l'erreur-type visée.
- **`ChartResolver`** — choisit le chart le plus proche et retient chaque compromis, pour qu'un
  conseil reste toujours remontable jusqu'à la donnée qui l'a produit.
- **`Language`** — la langue courante *est* `CurrentUICulture`. Les nombres suivent donc sans qu'on y
  pense, et la culture traverse les `await` et le pool de threads.

Les hypothèses du modèle (`PostflopOptions`) sont séparées du coût de calcul (`PostflopBudget`) :
les premières décrivent ce qu'on suppose du jeu, le second ce qu'on accepte de dépenser pour le
mesurer.

---

## Limites connues

Elles sont assumées, pas cachées — l'application en signale plusieurs à l'écran.

- **Les charts sont un point de départ, pas une sortie de solveur.** Plusieurs contextes n'ont
  aucune donnée propre (face à un 3-bet, face à un 4-bet, squeeze, face à des limps) et se rabattent
  sur un contexte voisin. Les profondeurs couvertes sont 10, 25 et 100bb ; entre les deux,
  l'application prend le chart le plus proche et le dit.
- **Pas d'ICM.** Toutes les espérances sont en jetons. Près d'une bulle ou d'une table finale, ce
  n'est pas la bonne monnaie.
- **Décision à un coup.** L'espérance est calculée comme si le coup s'arrêtait là : pas de plan de
  relance sur les rues suivantes.
- **Multi-joueurs approché.** Au-delà du tête-à-tête, la re-relance adverse n'est pas modélisée et
  les adversaires sont traités comme indépendants. L'avis le signale.
- **Pas de pots annexes.** Les tapis inégaux sont modélisés, mais un abattage multi-joueurs à tapis
  ne répartit pas encore le pot en plusieurs parts.
- **Publication Windows x64 uniquement**, bien qu'Avalonia soit multiplateforme.
