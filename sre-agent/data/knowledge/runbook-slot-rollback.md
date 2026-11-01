# Runbook: rollback di una release con lo swap degli slot

Negozio Bottega Aurora, Web App con slot `staging` e `production`. Dopo una promozione lo slot staging contiene la versione precedente; uno swap successivo può ripromuovere la versione difettosa.

## Quando usarlo

Errori o regressioni iniziati subito dopo uno swap di promozione, con prove che li legano alla nuova versione. Non attribuire un nuovo alert a una release precedente senza ricostruire la finestra corrente.

## Indagine e scelta

1. Pubblicare la presa in carico nel thread Teams dell'alert. Cercare il primo errore su `POST /api/checkout` in `AppRequests` (slot `production`, attributo `Properties['app.slot']`) e confrontare numero di 5xx e checkout totali per minuto con almeno 30 minuti precedenti. Controllare gli altri endpoint per delimitare l'impatto.
2. Elencare dal log attività tutti gli swap, i deploy e le modifiche di configurazione da almeno 30 minuti prima del primo errore. Confrontare i minuti sani e guasti prima/dopo ogni cambiamento: uno swap precedente può avere risolto l'incidente e uno successivo averlo riaperto. Le operazioni di sola lettura del profilo di pubblicazione non sono deploy.
3. Leggere `GET https://<web-app>.azurewebsites.net/api/version` e `GET https://<web-app>-staging.azurewebsites.net/api/version` per verificare quale versione serve ciascuno slot **ora**. Controllare la configurazione con `az webapp config show` e `az webapp config appsettings list` per entrambi gli slot (non pubblicare segreti). Se `service.version` non compare nelle proprietà di `AppRequests`, non dedurre la versione da una colonna vuota: correlare endpoint delle versioni, orari degli swap e versione dell'assembly nello stack trace di `AppExceptions`.
4. Quantificare checkout completati/falliti nell'ultimo minuto, incasso ed euro persi oggi **solo** con il registro ordini tramite la procedura `checkout-business-impact`; confrontare il tasso di errore con il periodo sano. Se disponibile, ripetere la lettura per capire se gli euro persi crescono.
5. Se si sospetta la release, confrontare le migrazioni fra le due versioni effettivamente negli slot: `git diff --name-status <tag-precedente> <tag-corrente> -- db/migrations`. Una migrazione nuova o incompatibile richiede valutazione prima dello swap (vedi PM-2025-11). Confrontare stack trace, file, riga e commit per confermare il difetto; non scartare altre modifiche senza prove contrarie.
6. Pubblicare nello **stesso thread Teams** impatto (compresi euro persi), causa probabile, unica azione proposta, rischio, reversibilità e criterio di verifica. Se la versione precedente è compatibile col database e presente in staging, eseguire `az webapp deployment slot swap -g <resource-group> -n <web-app> --slot staging --target-slot production --subscription <subscription-id>`. Lo swap inverso resta la via di ritorno, ma non ripromuovere una versione già dimostrata difettosa.

## Verifica e follow-up

7. Entro cinque minuti verificare `GET /api/version` su entrambi gli slot, i 5xx di `POST /api/checkout` **dopo il completamento dello swap** e una nuova lettura del registro ordini. La metrica dell'ultimo minuto include ancora richieste precedenti allo swap: attendere una finestra interamente successiva prima di dichiarare riuscito o fallito il rollback. Attendere checkout completati vicini al ritmo precedente, falliti vicini a zero ed euro persi che smettono di crescere.
8. Pubblicare nello stesso thread Teams azione ed esito, numeri e rischi residui. Aggiornare l'issue RCA già aperta per lo stesso difetto, oppure crearla; per un difetto di codice non corretto su `main`, preparare fix con test di regressione e patch version.
9. Prima del prossimo rilascio, impedire una nuova promozione dello staging difettoso. Il merge su `main` del codice in `src/` o `tests/` avvia CI/CD, che distribuisce in staging **e scambia automaticamente gli slot**: far verificare al revisore versione attesa, regressione e piano di rilascio prima del merge.
