# Checklist per la prima release pubblica

Questa checklist è un gate: una voce non verificata non deve essere presentata
come completata.

## Codice e contenuti

- [x] Catalogo corrente privo di brani e metadati commerciali inclusi.
- [x] Provenienza e SHA-256 di tutti gli asset visuali correnti registrati e verificati in CI.
- [x] Test core e MIDI disponibili senza Unity.
- [x] CI, CodeQL e aggiornamenti dipendenze configurati.
- [x] Licenza Community migrata a MPL-2.0 per evitare il precedente conflitto di copyleft dell'intera opera con Unity.
- [ ] Verifica della licenza MPL, dei termini Unity e del modello contributivo sull'esatto candidato binario.
- [x] Prima pubblicazione limitata a sorgenti e sito; nessun binario Unity pubblico.
- [x] Strategia scelta: nuovo repository pubblico pulito da uno snapshot dell'HEAD, senza cronologia privata.
- [x] Repository storico destinato a `HitTheKit-private-archive`, con tag e release mantenuti privati.

Per il percorso non distruttivo, dopo un commit verificato esegui
`scripts/create-public-source-snapshot.sh /percorso/HitTheKit-source.tar.gz`.
Lo script esporta solo `HEAD`, non include `.git`, rifiuta un worktree sporco e
produce lo SHA-256. Il tarball può inizializzare un nuovo repository pubblico;
non modifica né riscrive quello privato.

## Esperienza e compatibilità

- [x] Primo avvio, rimappatura tastiera, calibrazione e preferenze persistenti.
- [x] Fallback tastiera quando CoreMIDI non è disponibile.
- [ ] Test manuale con Unity 6000.5.6f1 su un Mac pulito.
- [ ] Test del pacchetto Windows x64 su installazioni pulite di Windows 10 e Windows 11.
- [ ] Test hardware con almeno due batterie elettroniche o moduli MIDI distinti.
- [ ] Verifica latenza con buffer richiesto a 256 campioni e almeno due interfacce audio.
- [ ] Verifica accessibilità con sola tastiera, alto contrasto e movimento ridotto.
- [ ] Supporto MIDI Windows/Linux oppure dichiarazione evidente “MIDI macOS only”.

## Distribuzione macOS

- [x] Script di firma, notarizzazione, DMG e smoke test presenti e coperti da test di contratto.
- [ ] Certificato Developer ID, profilo notarile e identità del publisher disponibili fuori dal repository.
- [ ] Build Release firmata, notarizzata e verificata con Gatekeeper su macchina pulita.
- [ ] SHA-256 e note di release pubblicati per ogni artefatto.

## Distribuzione Windows

- [x] Build e packaging Windows x64 keyboard-only ripetibili dal Mac di release.
- [x] CoreMIDI escluso esplicitamente dal player Windows.
- [ ] Windows Build Support (Mono) installato per Unity 6000.5.6f1 sul Mac di release.
- [ ] Build candidata prodotta e testata sui sistemi dichiarati.
- [ ] Percorso di firma pubblica scelto e verificato: Microsoft Store oppure certificato/servizio attendibile.
- [ ] Firma, timestamp, SmartScreen, SHA-256 e contenuto dello ZIP verificati sull'artefatto scaricato.

## Repository pubblico

- [x] Privacy, security policy, codice di condotta e guida contributi presenti.
- [x] Template issue/PR e scansione automatica presenti.
- [x] Roadmap, supporto, governance, maintainer e CODEOWNERS documentati.
- [x] Processo release, bozza note 0.5.0 e runbook del nuovo repository preparati.
- [x] Workflow GitHub Pages attivo nel repository pubblico.
- [x] Workflow snapshot sorgente con attestazione attivo e vincolato alla versione richiesta.
- [x] Protezione di `main`, required checks e private vulnerability reporting abilitati su GitHub.
- [ ] Screenshot e video finali acquisiti dalla build candidata, senza asset di terzi.
- [ ] Release notes coerenti con limiti reali e piattaforme testate.
- [x] Workflow Pages attivato solo nel nuovo repository e sito verificato da sessione non autenticata.
- [x] Prima esecuzione del workflow snapshot completata e attestazione verificata con `gh` (`v0.5.0-source-preview.2`, run `32234078583`, attestazione `41563124`).

## Sostenibilità

- [x] Obiettivi, limiti e trasparenza del finanziamento documentati.
- [x] Template repository per GitHub Sponsors preparato senza attivare un link incompleto.
- [x] Profilo GitHub Sponsors approvato e attivo per `Codewriter90x`.
- [x] `.github/FUNDING.yml` attivo con il profilo approvato.
- [x] Link donazione verificato da una sessione non autenticata.
- [ ] Cadenza degli aggiornamenti di trasparenza avviata dopo la prima donazione.
