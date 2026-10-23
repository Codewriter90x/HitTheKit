document.documentElement.classList.add("js");

const copy = {
  it: {
    skip: "Vai al contenuto", menuOpen: "Apri il menu", navDemo: "Video", navHow: "Come funziona", navThemes: "Temi", navLearn: "Impara", navSongs: "Brani", navRoadmap: "Roadmap", navDownload: "Download", navSponsor: "Supporta",
    version: "v0.6 BETA · PRE-RELEASE OPEN SOURCE", heroLine1: "SALI SUL PALCO,", heroLine2: "COLPISCI IL TEMPO.", heroLead: "Il rhythm game costruito per chi vuole imparare davvero a suonare: timing preciso, lezioni progressive e la tua batteria elettronica al centro dell’esperienza.", heroCta: "Guarda il gameplay", repoCta: "Vedi il codice", sponsorCta: "Supporta HitTheKit", factKeyboard: "Tastiera cross-platform", factMidi: "CoreMIDI su macOS", factLessons: "12 lezioni giocabili", heroCaption: "Otto elementi leggibili, una sola timeline musicale.", scroll: "GUARDA",
    demoLabel: "GAMEPLAY REALE", demoTitle: "Quarantadue secondi dentro HitTheKit", demoIntro: "Dal menu alla configurazione, dalla scelta della lezione alla highway: una cattura reale dell’app con contenuti originali.", demoCaption: "Menu → configurazione → lezione → gameplay", demoRights: "Cattura dell’app e colonna sonora originale rights-clean.",
    howLabel: "COME FUNZIONA", howTitle: "Un gioco ritmico costruito per imparare", howIntro: "Ogni colpo passa dallo stesso motore deterministico, che tu stia usando la tastiera o un vero kit MIDI.", featureTimingTitle: "Timing deterministico", featureTimingBody: "Il motore di hit-matching vive in una libreria .NET indipendente da Unity, con test automatici sulle finestre di giudizio.", featureKitTitle: "Batteria elettronica reale", featureKitBody: "Configurazione guidata del kit, feedback visivo sul pezzo richiesto e fallback da tastiera sempre disponibile.", featureLearnTitle: "Percorso strutturato", featureLearnBody: "Lezioni progressive con obiettivi, velocità di studio e valutazioni trasparenti: niente difficoltà artificiale.", flowConnect: "Collega o configura", flowChoose: "Scegli lezione o brano", flowPlay: "Suona e migliora",
    themesLabel: "AMBIENTI", themesTitle: "Tre ambienti, lo stesso gioco", themesIntro: "Cambia il linguaggio visivo senza alterare timing, note o punteggio.", themeArcade: "Runway olografico, note a diamante e una palette ciano/viola.", themeConcert: "Un’arena dal vivo vista da dietro un kit elettronico completo.", themeGrid: "Geometria tecnica, prospettiva piatta e massima leggibilità.",
    learnLabel: "PERCORSO GUIDATO", learnTitle: "24 lezioni, sei moduli", learnIntro: "Il primo semestre è già giocabile: da una pulsazione di grancassa a 64 BPM fino a un groove completo in sedicesimi. Il secondo resta visibile come programma, senza fingere di valutare ciò che il motore non supporta ancora.", metricPlayable: "lezioni giocabili", metricPass: "soglia di superamento", metricMastery: "padronanza", module1: "Fondamenta", module2: "Tecnica di base", module3: "Coordinazione", module4: "Controllo e dinamiche", module5: "Stili musicali", module6: "Musicista da palco", lessons14: "Giocabile · lezioni 1–4", lessons58: "Giocabile · lezioni 5–8", lessons912: "Giocabile · lezioni 9–12", ready: "PRONTO", planned: "In programma",
    songsLabel: "LIBRERIA", songsTitle: "Musica pulita, chart verificabili", songsIntro: "Il repository non distribuisce musica commerciale: include soltanto una demo originale e il confine per i contenuti posseduti dal giocatore.", included: "INCLUSO", neonDescription: "Demo originale · 8 battute · senza batteria preregistrata", yourMusic: "LA TUA MUSICA", localDescription: "Esempio del formato di importazione, senza contenuti di terzi.",
    roadmapTitle: "Onesto su ciò che funziona oggi", roadmapIntro: "Lo stato pubblico distingue ciò che è giocabile da ciò che è ancora in sviluppo.", now: "ORA · v0.6 beta", nowTitle: "Vertical slice giocabile", now1: "Gameplay completo da tastiera", now2: "CoreMIDI su macOS", now3: "12 lezioni e demo originale", now4: "Configurazione guidata del kit", next: "PROSSIMO", nextTitle: "Ampliare il supporto", next1: "Backend MIDI per Windows e Linux", next2: "Più profili di batterie verificati", next3: "Secondo semestre del percorso", next4: "Packaging pubblico ripetibile", later: "PIÙ AVANTI", laterTitle: "Ecosistema della community", later1: "Editor e validazione dei chart", later2: "Condivisione di contenuti rights-clean", later3: "Più ambienti e modalità pratica", later4: "Supporto hardware più ampio",
    downloadLabel: "PROVA LA BETA", downloadTitle: "Studialo, compilalo, contribuisci", downloadBody: "I tester possono scaricare da GitHub Releases una beta macOS firmata e notarizzata per Apple silicon. Su Windows e Linux si compila dai sorgenti seguendo la procedura documentata.", betaDownload: "Beta macOS (Apple silicon)", sourceDownload: "Scarica i sorgenti", viewReleases: "Guida alla build", supportProject: "Supporta il progetto", preRelease: "BETA MACOS + SORGENTI", platformKeyboard: "Codice tastiera: macOS, Windows, Linux", platformMidi: "Codice MIDI: macOS CoreMIDI",
    licenseLabel: "LICENZA", licenseTitle: "Open source, con regole chiare", licenseBody: "Il codice originale del progetto è distribuito con licenza MPL-2.0. Le modifiche ai file coperti restano open source, mentre Unity e le dipendenze mantengono i propri termini.", readLicense: "Leggi la MPL-2.0", licenseDetails: "Dettagli sul percorso commerciale", faqTitle: "Domande frequenti", faq1q: "Serve una batteria elettronica?", faq1a: "No. La tastiera è sempre disponibile; un kit MIDI rende però l’esperienza più vicina allo strumento reale.", faq2q: "Il MIDI funziona su tutti i sistemi?", faq2a: "Non ancora. Il backend di produzione usa CoreMIDI su macOS; Windows e Linux supportano oggi il gameplay da tastiera.", faq3q: "Ci sono canzoni commerciali incluse?", faq3a: "No. Il repository contiene solo musica originale rights-clean e un esempio per i contenuti locali del giocatore.", faq4q: "È già una versione stabile?", faq4a: "No. HitTheKit è in pre-release: la beta macOS per Apple silicon è pensata per i tester e le build Windows sono ancora in validazione.", footerTagline: "Rhythm game open source per batteria elettronica. In sviluppo attivo.", contribute: "Contribuisci", footerLicense: "MPL-2.0 · sorgenti pubblici, binari in validazione", footerTrademark: "Progetto indipendente; Unity, Apple, macOS, CoreMIDI e i nomi hardware appartengono ai rispettivi titolari."
  },
  en: {
    skip: "Skip to content", menuOpen: "Open menu", navDemo: "Video", navHow: "How it works", navThemes: "Themes", navLearn: "Learn", navSongs: "Songs", navRoadmap: "Roadmap", navDownload: "Download", navSponsor: "Sponsor",
    version: "v0.6 BETA · OPEN-SOURCE PRE-RELEASE", heroLine1: "STEP ON STAGE,", heroLine2: "KEEP THE BEAT.", heroLead: "A rhythm game for people who really want to learn the drums: precise timing, progressive lessons, and your electronic drum kit at the center of the experience.", heroCta: "Watch gameplay", repoCta: "View the code", sponsorCta: "Sponsor HitTheKit", factKeyboard: "Cross-platform keyboard play", factMidi: "CoreMIDI on macOS", factLessons: "12 playable lessons", heroCaption: "Eight readable kit pieces, one musical timeline.", scroll: "WATCH",
    demoLabel: "REAL GAMEPLAY", demoTitle: "Forty-two seconds inside HitTheKit", demoIntro: "From the menu through kit setup and lesson selection to the note highway: a real capture of the app with original content.", demoCaption: "Menu → setup → lesson → gameplay", demoRights: "Captured from the app itself, with an original rights-clean soundtrack.",
    howLabel: "HOW IT WORKS", howTitle: "A rhythm game built for learning", howIntro: "Every hit passes through the same deterministic engine, whether you use a keyboard or a real MIDI kit.", featureTimingTitle: "Deterministic timing", featureTimingBody: "The hit-matching engine lives in a Unity-independent .NET library, with automated tests covering the judgment windows.", featureKitTitle: "Real electronic drums", featureKitBody: "Guided kit configuration, clear visual feedback on which piece to hit, and a keyboard fallback that is always available.", featureLearnTitle: "Structured curriculum", featureLearnBody: "Progressive lessons with clear goals, practice speeds, and transparent grading—no artificial difficulty.", flowConnect: "Connect or configure", flowChoose: "Choose a lesson or song", flowPlay: "Play and improve",
    themesLabel: "ENVIRONMENTS", themesTitle: "Three environments, one game", themesIntro: "Switch the look without changing timing, notes, or scoring.", themeArcade: "A holographic runway, diamond notes, and a cyan-violet palette.", themeConcert: "A live arena viewed from behind a complete electronic kit.", themeGrid: "Technical geometry, a flat perspective, and maximum readability.",
    learnLabel: "GUIDED PATH", learnTitle: "24 lessons, six modules", learnIntro: "The first semester is already playable: from a 64 BPM kick pulse to a full sixteenth-note groove. The second semester is shown as a syllabus only, so the game never grades skills the engine cannot measure yet.", metricPlayable: "playable lessons", metricPass: "passing threshold", metricMastery: "mastery threshold", module1: "Fundamentals", module2: "Basic technique", module3: "Coordination", module4: "Control and dynamics", module5: "Musical styles", module6: "Stage performance", lessons14: "Playable · lessons 1–4", lessons58: "Playable · lessons 5–8", lessons912: "Playable · lessons 9–12", ready: "READY", planned: "Planned",
    songsLabel: "LIBRARY", songsTitle: "Rights-clean music, verifiable charts", songsIntro: "The repository does not distribute commercial music. It includes only an original demo and an example of how to add music you own.", included: "INCLUDED", neonDescription: "Original demo · 8 bars · no prerecorded drums", yourMusic: "YOUR MUSIC", localDescription: "An import-format example with no third-party content.",
    roadmapTitle: "Honest about what works today", roadmapIntro: "The public status keeps playable features clearly separate from work in progress.", now: "NOW · v0.6 beta", nowTitle: "Playable vertical slice", now1: "Full keyboard gameplay", now2: "CoreMIDI on macOS", now3: "12 lessons and original demo", now4: "Guided kit configuration", next: "NEXT", nextTitle: "Broader support", next1: "MIDI backends for Windows and Linux", next2: "More verified drum-kit profiles", next3: "Second curriculum semester", next4: "Repeatable public builds", later: "LATER", laterTitle: "Community ecosystem", later1: "Chart editing and validation", later2: "Rights-clean content sharing", later3: "More environments and practice modes", later4: "Broader hardware support",
    downloadLabel: "TRY THE BETA", downloadTitle: "Study it, build it, contribute", downloadBody: "Testers can download a signed and notarized macOS beta for Apple silicon from GitHub Releases. On Windows and Linux, build from source by following the documented procedure.", betaDownload: "macOS beta (Apple silicon)", sourceDownload: "Download source", viewReleases: "Build guide", supportProject: "Support the project", preRelease: "MACOS BETA + SOURCE", platformKeyboard: "Keyboard code: macOS, Windows, Linux", platformMidi: "MIDI code: macOS CoreMIDI",
    licenseLabel: "LICENSE", licenseTitle: "Open source, clear terms", licenseBody: "The project’s original code is distributed under MPL-2.0. Changes to covered files must stay open source, while Unity and other dependencies keep their own terms.", readLicense: "Read MPL-2.0", licenseDetails: "Commercial licensing details", faqTitle: "Frequently asked questions", faq1q: "Do I need an electronic drum kit?", faq1a: "No. Keyboard input is always available, but a MIDI kit brings the experience much closer to the real instrument.", faq2q: "Does MIDI work on every system?", faq2a: "Not yet. MIDI input currently uses CoreMIDI on macOS; on Windows and Linux you can play with the keyboard.", faq3q: "Are commercial songs included?", faq3a: "No. The repository contains only original rights-clean music and an example for player-owned local content.", faq4q: "Is this already a stable release?", faq4a: "No. HitTheKit is in pre-release: the macOS beta for Apple silicon is meant for testers, and Windows builds are still being validated.", footerTagline: "Open-source rhythm game for electronic drum kits. In active development.", contribute: "Contribute", footerLicense: "MPL-2.0 · public source, binaries under validation", footerTrademark: "Independent project; Unity, Apple, macOS, CoreMIDI, and hardware names belong to their respective owners."
  }
};

const languageButton = document.querySelector("[data-language]");
const menuButton = document.querySelector("[data-menu-toggle]");
const navigation = document.querySelector("[data-nav]");
const header = document.querySelector("[data-header]");

function getSavedLanguage() {
  try {
    const value = localStorage.getItem("htk-lang");
    if (value === "en" || value === "it") return value;
  } catch {
    // A private or locked-down browser may make storage unavailable.
  }

  return null;
}

function getBrowserLanguage() {
  const browserLanguage = navigator.languages?.[0] || navigator.language || "";
  return browserLanguage.toLowerCase().startsWith("it") ? "it" : "en";
}

function saveLanguage(language) {
  try { localStorage.setItem("htk-lang", language); } catch { /* storage can be unavailable */ }
}

function routeForLanguage(language) {
  const suffix = `${window.location.search}${window.location.hash}`;
  if (language === "en") return `en/${suffix}`;
  return `../${suffix}`;
}

function applyLanguage(language) {
  const dictionary = copy[language];
  document.documentElement.lang = language;
  document.querySelectorAll("[data-i18n]").forEach((element) => {
    const value = dictionary[element.dataset.i18n];
    if (value) element.textContent = value;
  });

  languageButton.textContent = language === "it" ? "EN" : "IT";
  languageButton.setAttribute("aria-label", language === "it" ? "Switch to English" : "Passa all’italiano");
  document.title = language === "it"
    ? "HitTheKit — Rhythm game per batteria elettronica"
    : "HitTheKit — Rhythm game for electronic drums";
  const description = document.querySelector('meta[name="description"]');
  if (description) {
    description.content = language === "it"
      ? "HitTheKit è il rhythm game open source per batteria elettronica: timing deterministico, lezioni progressive e supporto CoreMIDI su macOS."
      : "HitTheKit is an open-source rhythm game for electronic drums: deterministic timing, progressive lessons, and CoreMIDI support on macOS.";
  }
}

function setMenu(open) {
  navigation.classList.toggle("open", open);
  menuButton.setAttribute("aria-expanded", String(open));
  document.body.classList.toggle("menu-open", open);
  const label = menuButton.querySelector(".sr-only");
  if (label) label.textContent = open
    ? (document.documentElement.lang === "it" ? "Chiudi il menu" : "Close menu")
    : copy[document.documentElement.lang].menuOpen;
}

languageButton.addEventListener("click", () => {
  const language = document.documentElement.lang === "it" ? "en" : "it";
  saveLanguage(language);
  window.location.assign(routeForLanguage(language));
});

menuButton.addEventListener("click", () => {
  setMenu(menuButton.getAttribute("aria-expanded") !== "true");
});

navigation.querySelectorAll("a").forEach((link) => {
  link.addEventListener("click", () => setMenu(false));
});

document.addEventListener("keydown", (event) => {
  if (event.key !== "Escape") return;
  const wasOpen = menuButton.getAttribute("aria-expanded") === "true";
  setMenu(false);
  // Return keyboard focus to the control that opened the overlay instead of leaving it behind it.
  if (wasOpen) menuButton.focus();
});

window.addEventListener("resize", () => {
  if (window.innerWidth > 1080) setMenu(false);
});

function updateHeader() {
  header.classList.toggle("scrolled", window.scrollY > 20);
}
window.addEventListener("scroll", updateHeader, { passive: true });
updateHeader();

const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
const revealElements = document.querySelectorAll(".reveal");
if (reducedMotion || !("IntersectionObserver" in window)) {
  revealElements.forEach((element) => element.classList.add("visible"));
} else {
  const revealObserver = new IntersectionObserver((entries, observer) => {
    entries.forEach((entry) => {
      if (!entry.isIntersecting) return;
      entry.target.classList.add("visible");
      observer.unobserve(entry.target);
    });
  }, { threshold: 0.12, rootMargin: "0px 0px -40px" });
  revealElements.forEach((element) => revealObserver.observe(element));
}

const pageSections = [...document.querySelectorAll("main section[id]")];
const navLinks = [...navigation.querySelectorAll('a[href^="#"]')];
if ("IntersectionObserver" in window) {
  const sectionObserver = new IntersectionObserver((entries) => {
    const visible = entries.filter((entry) => entry.isIntersecting).sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];
    if (!visible) return;
    navLinks.forEach((link) => {
      const active = link.getAttribute("href") === `#${visible.target.id}`;
      link.classList.toggle("active", active);
      if (active) link.setAttribute("aria-current", "location");
      else link.removeAttribute("aria-current");
    });
  }, { rootMargin: "-30% 0px -60%", threshold: [0, .1, .3] });
  pageSections.forEach((section) => sectionObserver.observe(section));
}

const pageLanguage = document.documentElement.dataset.locale === "en" ? "en" : "it";
const preferredLanguage = getSavedLanguage() || getBrowserLanguage();

// The root is the canonical Italian entry point. A first-time non-Italian
// visitor is sent to the stable English route; direct /en/ visits remain stable.
if (pageLanguage === "it" && preferredLanguage === "en") {
  window.location.replace(routeForLanguage("en"));
} else {
  applyLanguage(pageLanguage);
}
