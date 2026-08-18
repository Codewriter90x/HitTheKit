# Security Policy

## Versioni supportate

La linea `0.5.x` riceve correzioni di sicurezza durante la fase di pre-release.
Le build e i tag precedenti non sono supportati.

## Segnalare una vulnerabilità

Usa **Security → Report a vulnerability** nel repository GitHub per inviare una
segnalazione privata. Non aprire una issue pubblica e non includere credenziali,
brani commerciali, dati personali o catture MIDI non sanitizzate.

Includi, se possibile:

- versione o commit;
- sistema operativo;
- impatto e condizioni necessarie;
- passi minimi per riprodurre;
- una proposta di mitigazione, senza eseguire azioni distruttive.

Il manutentore confermerà la ricezione appena possibile, valuterà severità e
riproducibilità e coordinerà la pubblicazione della correzione. Non è promesso
un tempo di risposta contrattuale.

## Ambito

Sono particolarmente rilevanti parsing di chart e configurazioni locali,
gestione dei percorsi, packaging, firma, plugin nativi e dipendenze. Problemi
nei servizi Unity o nel sistema operativo vanno segnalati anche al relativo
fornitore quando appropriato.
