## Cosa cambia

Descrivi il comportamento modificato e il motivo.

## Autorizzazione e provenienza

- [ ] Il maintainer ha autorizzato questo contributo secondo `CONTRIBUTING.md`,
      oppure la modifica è una piccola correzione documentale.
- [ ] Possiedo i diritti necessari su tutto il materiale inserito e ne ho
      documentato la provenienza quando richiesto.

## Verifica

- [ ] `dotnet test HitTheKit.sln`
- [ ] `./scripts/check-nuget-vulnerabilities.sh`
- [ ] Unity EditMode/PlayMode eseguiti, oppure limite dichiarato
- [ ] Nessun audio, chart, marchio o asset di terzi aggiunto senza provenienza e licenza
- [ ] Nessun dato personale, cattura MIDI privata o credenziale inclusa
- [ ] Documentazione, changelog e limiti pubblici aggiornati quando applicabile

## Evidenza manuale

Indica sistema operativo, versione Unity, dispositivo di input e risultato.
Per modifiche Unity, registra separatamente i totali EditMode e PlayMode usando
il gate descritto in `docs/development/unity-test-gate.md`. Se il job automatico
e disattivato o non riceve i secret nelle PR da fork, questa evidenza e
obbligatoria prima del merge.

- Commit testato:
- Unity Editor:
- Sistema/architettura:
- EditMode (passed/failed/skipped):
- PlayMode (passed/failed/skipped):
- Motivo di eventuali test non eseguiti:
