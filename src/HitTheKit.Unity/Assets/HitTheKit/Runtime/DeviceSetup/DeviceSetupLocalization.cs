using System;
using System.Collections.Generic;

namespace HitTheKit.Unity.DeviceSetup
{
    public sealed class DictionaryLocalizedTextProvider : ILocalizedTextProvider
    {
        private readonly IReadOnlyDictionary<string, string> italian;
        private readonly IReadOnlyDictionary<string, string> english;

        public DictionaryLocalizedTextProvider(DeviceSetupLanguage language = DeviceSetupLanguage.Italian)
        {
            Language = language;
            italian = Italian();
            english = English();
        }

        public DeviceSetupLanguage Language { get; set; }

        public string Get(string key, string fallback = null)
        {
            if (string.IsNullOrWhiteSpace(key)) return fallback ?? "Testo non disponibile";
            IReadOnlyDictionary<string, string> selected = Language == DeviceSetupLanguage.Italian ? italian : english;
            if (selected.TryGetValue(key, out string value)) return value;
            IReadOnlyDictionary<string, string> alternate = Language == DeviceSetupLanguage.Italian ? english : italian;
            if (alternate.TryGetValue(key, out value)) return value;
            return fallback ?? (Language == DeviceSetupLanguage.Italian ? "Testo non disponibile" : "Text unavailable");
        }

        private static IReadOnlyDictionary<string, string> Italian() => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["deviceSetup.title"] = "Configura la tua batteria elettronica",
            ["deviceSetup.subtitle"] = "Scegli un dispositivo, conferma la struttura del kit e segui la configurazione guidata.",
            ["deviceSetup.privacy"] = "HitTheKit legge soltanto eventi MIDI: non registra audio, microfono o video.",
            ["deviceSetup.start"] = "Inizia configurazione",
            ["deviceSetup.backToGame"] = "Torna al menu",
            ["deviceSetup.selectDevice"] = "Seleziona dispositivo",
            ["deviceSetup.refresh"] = "Aggiorna",
            ["deviceSetup.useKnownProfile"] = "Usa come punto di partenza",
            ["deviceSetup.configureNewKit"] = "Configura da zero",
            ["deviceSetup.profile"] = "Profilo candidato",
            ["deviceSetup.structure"] = "Struttura fisica del kit",
            ["deviceSetup.structureCopy"] = "Scegli la configurazione che assomiglia alla tua batteria. Il numero indica i tamburi: piatti e pedali sono descritti sotto ogni immagine.",
            ["deviceSetup.presetMinimalTitle"] = "Essenziale · 3 pezzi",
            ["deviceSetup.presetMinimalCopy"] = "Grancassa + rullante + charleston. Nessun tom.",
            ["deviceSetup.presetStandardTitle"] = "Standard · 5 pezzi",
            ["deviceSetup.presetStandardCopy"] = "Grancassa + rullante + 3 tom, con charleston, crash e ride.",
            ["deviceSetup.presetExtendedTitle"] = "Elettronica estesa",
            ["deviceSetup.presetExtendedCopy"] = "Più tom, più piatti e zone aggiuntive come bordi e campane.",
            ["deviceSetup.choosePreset"] = "Scegli questo kit",
            ["deviceSetup.presetSelected"] = "Kit selezionato",
            ["deviceSetup.beginWizard"] = "Avvia configurazione guidata",
            ["deviceSetup.guidedMapping"] = "Configurazione guidata",
            ["deviceSetup.conflicts"] = "Conflitti da risolvere",
            ["deviceSetup.review"] = "Riepilogo configurazione",
            ["deviceSetup.testKit"] = "Test del kit",
            ["deviceSetup.complete"] = "Configurazione completata",
            ["deviceSetup.confirm"] = "Conferma",
            ["deviceSetup.continueWhenReady"] = "Prosegui a conteggio completo",
            ["deviceSetup.confirmAndContinue"] = "Conferma e prosegui",
            ["deviceSetup.retry"] = "Ripeti",
            ["deviceSetup.skip"] = "Salta",
            ["deviceSetup.back"] = "Indietro",
            ["deviceSetup.saveDraft"] = "Termina e salva bozza",
            ["deviceSetup.simulate"] = "Simula colpi",
            ["deviceSetup.continueTest"] = "Continua al test",
            ["deviceSetup.finish"] = "Completa",
            ["deviceSetup.language"] = "Lingua",
            ["deviceSetup.midiWaiting"] = "○ In attesa dell'ingresso MIDI",
            ["deviceSetup.midiConnected"] = "● Ingresso MIDI connesso",
            ["deviceSetup.simulationConnected"] = "● Simulazione connessa",
            ["deviceSetup.noMidiDevices"] = "Nessuna batteria MIDI rilevata. Collega il modulo via USB e premi Aggiorna.",
            ["deviceSetup.waitingMidiEvents"] = "In attesa di colpi dalla batteria MIDI…",
            ["deviceSetup.waitingSimulatedEvents"] = "In attesa degli eventi MIDI simulati…",
            ["deviceSetup.hitNow"] = "ORA COLPISCI IL PEZZO ILLUMINATO",
            ["deviceSetup.guidedHelp"] = "Esegui colpi separati e regolari. Quando il conteggio è completo, il rilevamento si ferma e il pulsante Prosegui si attiva.",
            ["deviceSetup.step"] = "Passaggio",
            ["deviceSetup.required"] = "Obbligatorio",
            ["deviceSetup.optional"] = "Facoltativo",
            ["deviceSetup.hitsProgress"] = "{0} di {1} colpi rilevati",
            ["deviceSetup.candidateHint"] = "Mappatura suggerita: {0} · affidabilità {1} · richiede conferma",
            ["deviceSetup.captureWaiting"] = "In attesa del primo colpo…",
            ["deviceSetup.captureReceiving"] = "Colpo rilevato ({0}/{1}). Continua sullo stesso pezzo.",
            ["deviceSetup.captureReady"] = "Rilevamento completato. Premi “Conferma e prosegui”.",
            ["deviceSetup.captureConflict"] = "I colpi non corrispondono allo stesso trigger.",
            ["deviceSetup.captureDisconnected"] = "Ingresso MIDI disconnesso. Ricollega la batteria per continuare.",
            ["deviceSetup.captureUnsupported"] = "Questo passaggio non dispone di dati di simulazione.",
            ["deviceSetup.captureCompleted"] = "Tutti i passaggi sono completi.",
            ["deviceSetup.audioRouting"] = "MIDI trasporta i colpi, non l'audio. Per sentire base e batteria nelle stesse cuffie, scegli il modulo come uscita audio macOS (se supporta USB Audio) oppure collega il Mac all'ingresso AUX / MIX IN del modulo.",
            ["wizard.kick.title"] = "Grancassa",
            ["wizard.kick.instruction"] = "Premi il pedale della grancassa cinque volte.",
            ["wizard.snareHead.title"] = "Centro del rullante",
            ["wizard.snareHead.instruction"] = "Colpisci il centro del rullante.",
            ["wizard.snareRim.title"] = "Bordo del rullante",
            ["wizard.snareRim.instruction"] = "Colpisci il cerchio o la zona esterna del rullante.",
            ["wizard.rideBow.title"] = "Corpo del ride",
            ["wizard.rideBow.instruction"] = "Colpisci la superficie larga del ride, evitando la campana centrale.",
            ["wizard.rideBell.title"] = "Campana del ride",
            ["wizard.rideBell.instruction"] = "Colpisci la parte centrale rialzata del ride.",
            ["wizard.hihatClosed.title"] = "Charleston chiuso",
            ["wizard.hihatClosed.instruction"] = "Tieni premuto il pedale e colpisci il charleston.",
            ["wizard.hihatOpen.title"] = "Charleston aperto",
            ["wizard.hihatOpen.instruction"] = "Solleva il pedale e colpisci il charleston.",
            ["wizard.hihatPedal.title"] = "Pedale del charleston",
            ["wizard.hihatPedal.instruction"] = "Premi il pedale senza colpire il charleston.",
            ["wizard.hihatContinuous.title"] = "Movimento del pedale",
            ["wizard.hihatContinuous.instruction"] = "Muovi lentamente il pedale da aperto a chiuso e viceversa.",
        };

        private static IReadOnlyDictionary<string, string> English() => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["deviceSetup.title"] = "Configure your electronic drum kit",
            ["deviceSetup.subtitle"] = "Choose a device, confirm the physical kit and follow the guided setup.",
            ["deviceSetup.privacy"] = "HitTheKit reads MIDI events only: it does not record audio, microphone or video.",
            ["deviceSetup.start"] = "Start setup",
            ["deviceSetup.backToGame"] = "Back to menu",
            ["deviceSetup.selectDevice"] = "Select device",
            ["deviceSetup.refresh"] = "Refresh",
            ["deviceSetup.useKnownProfile"] = "Use as starting point",
            ["deviceSetup.configureNewKit"] = "Configure from scratch",
            ["deviceSetup.profile"] = "Candidate profile",
            ["deviceSetup.structure"] = "Physical kit structure",
            ["deviceSetup.structureCopy"] = "Choose the setup that looks like your drum kit. The number counts drums; cymbals and pedals are listed below each image.",
            ["deviceSetup.presetMinimalTitle"] = "Essential · 3-piece",
            ["deviceSetup.presetMinimalCopy"] = "Kick + snare + hi-hat. No toms.",
            ["deviceSetup.presetStandardTitle"] = "Standard · 5-piece",
            ["deviceSetup.presetStandardCopy"] = "Kick + snare + 3 toms, with hi-hat, crash and ride.",
            ["deviceSetup.presetExtendedTitle"] = "Extended electronic",
            ["deviceSetup.presetExtendedCopy"] = "More toms, more cymbals and extra zones such as rims and bells.",
            ["deviceSetup.choosePreset"] = "Choose this kit",
            ["deviceSetup.presetSelected"] = "Kit selected",
            ["deviceSetup.beginWizard"] = "Begin guided setup",
            ["deviceSetup.guidedMapping"] = "Guided mapping",
            ["deviceSetup.conflicts"] = "Conflicts to resolve",
            ["deviceSetup.review"] = "Configuration review",
            ["deviceSetup.testKit"] = "Test kit",
            ["deviceSetup.complete"] = "Setup completed",
            ["deviceSetup.confirm"] = "Confirm",
            ["deviceSetup.continueWhenReady"] = "Continue when the count is complete",
            ["deviceSetup.confirmAndContinue"] = "Confirm and continue",
            ["deviceSetup.retry"] = "Retry",
            ["deviceSetup.skip"] = "Skip",
            ["deviceSetup.back"] = "Back",
            ["deviceSetup.saveDraft"] = "Finish and save draft",
            ["deviceSetup.simulate"] = "Simulate hits",
            ["deviceSetup.continueTest"] = "Continue to test",
            ["deviceSetup.finish"] = "Complete",
            ["deviceSetup.language"] = "Language",
            ["deviceSetup.midiWaiting"] = "○ Waiting for MIDI input",
            ["deviceSetup.midiConnected"] = "● MIDI input connected",
            ["deviceSetup.simulationConnected"] = "● Simulation connected",
            ["deviceSetup.noMidiDevices"] = "No MIDI drum kit detected. Connect the module over USB and press Refresh.",
            ["deviceSetup.waitingMidiEvents"] = "Waiting for hits from the MIDI drum kit…",
            ["deviceSetup.waitingSimulatedEvents"] = "Waiting for simulated MIDI events…",
            ["deviceSetup.hitNow"] = "HIT THE LIT PIECE NOW",
            ["deviceSetup.guidedHelp"] = "Play separate, even hits. When the count is complete, detection stops and the Continue button becomes available.",
            ["deviceSetup.step"] = "Step",
            ["deviceSetup.required"] = "Required",
            ["deviceSetup.optional"] = "Optional",
            ["deviceSetup.hitsProgress"] = "{0} of {1} hits detected",
            ["deviceSetup.candidateHint"] = "Suggested mapping: {0} · confidence {1} · confirmation required",
            ["deviceSetup.captureWaiting"] = "Waiting for the first hit…",
            ["deviceSetup.captureReceiving"] = "Hit detected ({0}/{1}). Keep playing the same piece.",
            ["deviceSetup.captureReady"] = "Detection complete. Press “Confirm and continue”.",
            ["deviceSetup.captureConflict"] = "The hits did not match the same trigger.",
            ["deviceSetup.captureDisconnected"] = "MIDI input disconnected. Reconnect the kit to continue.",
            ["deviceSetup.captureUnsupported"] = "No simulation data is available for this step.",
            ["deviceSetup.captureCompleted"] = "All steps are complete.",
            ["deviceSetup.audioRouting"] = "MIDI carries hits, not audio. To hear the backing track and drum kit in the same headphones, select the module as the macOS audio output (if it supports USB Audio), or connect the Mac to the module AUX / MIX IN.",
            ["wizard.kick.title"] = "Kick",
            ["wizard.kick.instruction"] = "Press the kick pedal five times.",
            ["wizard.snareHead.title"] = "Snare center",
            ["wizard.snareHead.instruction"] = "Strike the center of the snare.",
            ["wizard.snareRim.title"] = "Snare rim",
            ["wizard.snareRim.instruction"] = "Strike the rim or outer zone of the snare.",
            ["wizard.rideBow.title"] = "Ride bow",
            ["wizard.rideBow.instruction"] = "Strike the wide ride surface and avoid the center bell.",
            ["wizard.rideBell.title"] = "Ride bell",
            ["wizard.rideBell.instruction"] = "Strike the raised center of the ride.",
            ["wizard.hihatClosed.title"] = "Closed hi-hat",
            ["wizard.hihatClosed.instruction"] = "Hold the pedal down and strike the hi-hat.",
            ["wizard.hihatOpen.title"] = "Open hi-hat",
            ["wizard.hihatOpen.instruction"] = "Release the pedal and strike the hi-hat.",
            ["wizard.hihatPedal.title"] = "Hi-hat pedal",
            ["wizard.hihatPedal.instruction"] = "Press the pedal without striking the hi-hat.",
            ["wizard.hihatContinuous.title"] = "Continuous hi-hat pedal",
            ["wizard.hihatContinuous.instruction"] = "Move the pedal slowly from open to closed and back.",
        };
    }
}
