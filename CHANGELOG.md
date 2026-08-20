# Changelog

Le modifiche rilevanti sono documentate qui. Il progetto segue il versionamento
semantico a partire dalla prima release pubblica stabile.

## [Unreleased]

## [0.6.0-beta.1] - 2026-08-20

### Added

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
- Chart Creator locale con import audio, registrazione, editing delle note,
  waveform, scrubbing e pacchetti portabili `.htksong` senza audio incorporato;
- Practice Lab con loop di sezioni e punti A/B;
- Performance Error Map e Auto Tempo Coach basati sui risultati reali;
- sound check guidato per la compensazione locale della latenza;
- Ghost Replay visuale locale, isolato da matcher e punteggio;
- palco reattivo procedurale con movimento ridotto e contrasto elevato.

### Changed

- timestamp MIDI preservati fino al clock della canzone;
- catalogo incluso limitato a contenuti originali o esempi locali neutrali;
- toolchain xUnit aggiornata per rimuovere dipendenze transitive vulnerabili;
- identità applicazione impostata su HitTheKit 0.6.0.

### Removed

- metadati, chart e riferimenti inclusi relativi a brani commerciali.

## [0.4.0] - pre-release storica

Build privata di playtest. Non è approvata per la redistribuzione pubblica.
