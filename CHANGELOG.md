# Changelog

Le modifiche rilevanti sono documentate qui. Il progetto segue il versionamento
semantico a partire dalla prima release pubblica stabile.

## [Unreleased]

### Added

- backend MIDI sperimentale per Windows tramite WinMM, dietro la stessa interfaccia di CoreMIDI; in attesa di verifica su hardware (#53);
- selezione automatica della batteria MIDI in gameplay: porte virtuali ignorate, profilo riconosciuto preferito e rilevamento al primo colpo quando ci sono più ingressi (#50);
- pulsante dedicato per ripristinare la sola configurazione batteria (#51);
- build e packaging Windows x64 keyboard-only con esclusione verificata di CoreMIDI;
- roadmap pubblica, governance, maintainer, supporto e funding trasparente;
- issue form separati per funzionalità, hardware e lezioni;
- processo release, note 0.5.0 in bozza e runbook per il nuovo repository pubblico;
- contratti CI per versione, documenti pubblici, catalogo rights-clean e claim di licenza;
- onboarding per tastiera e batteria elettronica;
- preferenze persistenti per audio, display, accessibilità e tasti;
- calibrazione separata per tastiera e MIDI con suggerimento robusto;
- diagnostica early/late e indicazione del pezzo da allenare;
- metronomo opzionale sincronizzato al clock DSP della sessione;
- controlli CI, CodeQL, Dependabot e template per contributi;
- documentazione privacy, sicurezza e pubblicazione.

### Changed

- profilo GM generico esteso con note 37, 52, 55, 57 e 59 (#49);
- le note non presenti nella configurazione salvata usano il profilo generico invece di essere ignorate; le note non mappate vengono contate (#51);
- timestamp MIDI preservati fino al clock della canzone;
- catalogo incluso limitato a contenuti originali o esempi locali neutrali;
- toolchain xUnit aggiornata per rimuovere dipendenze transitive vulnerabili;
- identità applicazione impostata su HitTheKit 0.5.0.

### Removed

- metadati, chart e riferimenti inclusi relativi a brani commerciali.

## [0.4.0] - pre-release storica

Build privata di playtest. Non è approvata per la redistribuzione pubblica.
