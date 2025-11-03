
// ==================================================
// CONNESSIONE SIGNALR AL BACKEND
// ==================================================

if (typeof signalR !== "undefined" && window.MOBISHARE_CONFIG?.jwtToken) {

    //recupera JWT token dalla configurazione globale 
    const token = window.MOBISHARE_CONFIG.jwtToken;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hub/notifiche", {
            accessTokenFactory: () => token //ignorato se non serve
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .configureLogging(signalR.LogLevel.Information)
        .build();



    // ==================================================
    // EVENTI RICEVUTI DAL SERVER
    // ==================================================


    connection.on("CreditoAggiornato", function (nuovoCredito) {
        aggiornaCredito(nuovoCredito);
        showSuccessMessage(`Credito aggiornato: ${mobishare.formatCurrency(nuovoCredito)}`);
    });

    connection.on("PagamentoCompletato", data => {
        showSuccessMessage(`Pagamento completato: ${data.importo}€ (${data.stato})`);
        if (data.nuovoCredito !== undefined) aggiornaCredito(data.nuovoCredito);
    });

    connection.on("PagamentoFallito", data => {
        showErrorMessage(`Pagamento fallito: ${data.importo}€`);
    });

    connection.on("PagamentoAggiornato", data => {
        showInfoMessage(`Pagamento aggiornato: ${data.importo}€ (${data.stato})`);
    });

    connection.on("NuovaTransazione", data => {
        showInfoMessage(`Nuova transazione: ${data.tipo} ${data.importo}€`);

        if (window.location.pathname.includes('/Transazioni')) {
            setTimeout(() => window.location.reload(), 2000);
        }
    });

    connection.on("AccountRiattivato", data => {
        showSuccessMessage(`Account riattivato: ${data.nome}. ${data.messaggio}`);

        // Ricarica la pagina dopo 2 secondi per aggiornare lo stato
        setTimeout(() => {
            window.location.reload();
        }, 2000);
    });


    //BONUS BICI MUSCOLARE
    // Quando l'utente guadagna punti bonus alla fine di una corsa
    connection.on("BonusApplicato", data => {
        showSuccessMessage(`${data.Messaggio} (Totale: ${data.TotalePunti} punti)`);
        console.log("Bonus applicato:", data);
    });


    // Quando l'utente utilizza i punti per ottenere uno sconto
    connection.on("BonusUsato", data => {
        showInfoMessage(`${data.Messaggio}`);
        console.log("Bonus usato:", data);
    });

    // ==================================================
    // GESTIONE NOTIFICHE ADMIN CON DROPDOWN
    // ==================================================

    // Array notifiche lato client (massimo 20)
    let notificheAdmin = [];
    const MAX_NOTIFICHE = 20;

    // Ricevi notifica admin tramite SignalR
    connection.on("RiceviNotificaAdmin", (titolo, testo) => {
        console.log(`[ADMIN NOTIFICA] ${titolo}: ${testo}`);

        // Aggiungi notifica all'array
        const nuovaNotifica = {
            id: Date.now(),
            titolo: titolo,
            testo: testo,
            timestamp: new Date(),
            letta: false
        };

        notificheAdmin.unshift(nuovaNotifica); // Aggiungi all'inizio

        // Mantieni solo le ultime MAX_NOTIFICHE
        if (notificheAdmin.length > MAX_NOTIFICHE) {
            notificheAdmin = notificheAdmin.slice(0, MAX_NOTIFICHE);
        }

        // Aggiorna UI
        aggiornaDropdownNotifiche();
        aggiornaBadgeNotifiche();

        // Mostra anche toast
        showWarningMessage(`🔔 ${titolo}: ${testo}`);
    });

    // Funzione per aggiornare il badge contatore
    function aggiornaBadgeNotifiche() {
        const badge = document.getElementById('notifiche-badge');
        const nonLette = notificheAdmin.filter(n => !n.letta).length;

        if (badge) {
            if (nonLette > 0) {
                badge.textContent = nonLette > 99 ? '99+' : nonLette;
                badge.style.display = 'inline-block';
            } else {
                badge.style.display = 'none';
            }
        }
    }

    // Funzione per aggiornare il dropdown notifiche
    function aggiornaDropdownNotifiche() {
        const lista = document.getElementById('notifiche-lista');
        if (!lista) return;

        if (notificheAdmin.length === 0) {
            lista.innerHTML = `
            <li class="text-center text-muted py-3">
                <i class="bi bi-bell-slash"></i>
                <p class="mb-0 small">Nessuna notifica</p>
            </li>
        `;
            return;
        }

        // Crea HTML per ogni notifica
        let html = '';
        notificheAdmin.forEach(notifica => {
            const dataRelativa = getTempoRelativo(notifica.timestamp);
            const classeNonLetta = notifica.letta ? '' : 'bg-light';

            html += `
            <li>
                <a class="dropdown-item ${classeNonLetta} py-2" href="#" 
                   data-notifica-id="${notifica.id}">
                    <div class="d-flex justify-content-between align-items-start">
                        <div style="flex: 1;">
                            <strong class="d-block text-truncate">${escapeHtml(notifica.titolo)}</strong>
                            <small class="text-muted d-block">${escapeHtml(notifica.testo)}</small>
                            <small class="text-muted fst-italic">${dataRelativa}</small>
                        </div>
                        ${!notifica.letta ? '<span class="badge bg-primary ms-2">Nuovo</span>' : ''}
                    </div>
                </a>
            </li>
        `;
        });

        lista.innerHTML = html;

        // Aggiungi event listener per segnare come letta al click
        lista.querySelectorAll('a[data-notifica-id]').forEach(link => {
            link.addEventListener('click', function (e) {
                e.preventDefault();
                const id = parseInt(this.dataset.notificaId);
                segnaNotificaComeLettura(id);
            });
        });
    }

    // Segna una notifica come letta
    function segnaNotificaComeLettura(id) {
        const notifica = notificheAdmin.find(n => n.id === id);
        if (notifica && !notifica.letta) {
            notifica.letta = true;
            aggiornaDropdownNotifiche();
            aggiornaBadgeNotifiche();
        }
    }

    // Segna tutte le notifiche come lette
    function segnaTutteComeLette() {
        notificheAdmin.forEach(n => n.letta = true);
        aggiornaDropdownNotifiche();
        aggiornaBadgeNotifiche();
    }

    // Funzione helper: tempo relativo (es. "2 minuti fa")
    function getTempoRelativo(data) {
        const secondi = Math.floor((new Date() - data) / 1000);

        if (secondi < 60) return 'Pochi secondi fa';
        if (secondi < 3600) return `${Math.floor(secondi / 60)} minuti fa`;
        if (secondi < 86400) return `${Math.floor(secondi / 3600)} ore fa`;

        return data.toLocaleDateString('it-IT', {
            day: 'numeric',
            month: 'short',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    // Funzione helper: escape HTML per sicurezza
    function escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, m => map[m]);
    }


    // Event listener per pulsante "segna tutte come lette"
    document.addEventListener('DOMContentLoaded', function () {
        const btnCancellaTutte = document.getElementById('cancella-tutte-notifiche');
        if (btnCancellaTutte) {
            btnCancellaTutte.addEventListener('click', function (e) {
                e.preventDefault();
                segnaTutteComeLette();
            });
        }
    });


    // Aggiornamento telemetria (mezzi, batterie, ecc.)
    connection.on("AggiornamentoTelemetria", function (data) {
        console.log("Telemetria ricevuta:", data);

        // Aggiorna la tabella solo se siamo nella pagina Gestione Mezzi
        const path = window.location.pathname.toLowerCase();
        if (path.includes("/dashboardadmin/gestionemezzi")) {
            aggiornaStatoMezzo(data.idMezzo, data.livelloBatteria, data.stato);
        }
    });

    // ==================================================
    // FUNZIONE: aggiornaStatoMezzo()
    // Aggiorna dinamicamente la tabella mezzi in GestioneMezzi
    // ==================================================
    function aggiornaStatoMezzo(idMezzo, livelloBatteria, stato) {
        // Trova la riga corrispondente cercando il numero ID
        const righe = document.querySelectorAll("table tbody tr");
        let rigaTrovata = null;

        righe.forEach(tr => {
            const idCell = tr.querySelector("td strong");
            if (idCell && idCell.textContent.trim() === `#${idMezzo}`) {
                rigaTrovata = tr;
            }
        });

        if (!rigaTrovata) return; // mezzo non trovato

        // ===============================
        // Aggiorna livello batteria
        // ===============================
        const cellaBatteria = rigaTrovata.querySelector("td:nth-child(5)");
        if (cellaBatteria && livelloBatteria !== undefined && livelloBatteria !== null) {
            let colore = "success";
            if (livelloBatteria <= 30) colore = "danger";
            else if (livelloBatteria <= 60) colore = "warning";

            cellaBatteria.innerHTML = `
            <span class="badge bg-${colore}">
                <i class="bi bi-battery-charging"></i> ${livelloBatteria}%
            </span>
        `;
        }

        // ===============================
        // Aggiorna stato
        // ===============================
        const cellaStato = rigaTrovata.querySelector("td:nth-child(6)");
        if (cellaStato && stato) {
            let badgeHtml = "";
            switch (stato.toLowerCase()) {
                case "disponibile":
                    badgeHtml = `<span class="badge bg-success"><i class="bi bi-check-circle"></i> Disponibile</span>`;
                    break;
                case "inuso":
                    badgeHtml = `<span class="badge bg-primary"><i class="bi bi-arrow-right-circle"></i> In Uso</span>`;
                    break;
                case "manutenzione":
                case "inmanutenzione":
                    badgeHtml = `<span class="badge bg-warning"><i class="bi bi-wrench"></i> Manutenzione</span>`;
                    break;
                case "nonprelevabile":
                case "guasto":
                    badgeHtml = `<span class="badge bg-danger"><i class="bi bi-x-circle"></i> Non Prelevabile</span>`;
                    break;
                default:
                    badgeHtml = `<span class="badge bg-secondary">${stato}</span>`;
                    break;
            }
            cellaStato.innerHTML = badgeHtml;
        }

        // ===============================
        // Effetto visivo di aggiornamento
        // ===============================
        rigaTrovata.style.transition = "background-color 0.6s ease";
        rigaTrovata.style.backgroundColor = "#e6f7ff";
        setTimeout(() => (rigaTrovata.style.backgroundColor = ""), 800);
    }


    // Test generico
    connection.on("RiceviMessaggio", function (user, message) {
        console.log(`Messaggio da ${user}: ${message}`);
    });

    // Notifica sospensione utente 
    connection.on("UtenteSospeso", data => {
        // Mostra un alert permanente di sospensione
        const existingAlert = document.querySelector('.alert-sospensione');
        if (existingAlert) {
            existingAlert.remove();
        }

        const alertDiv = document.createElement('div');
        alertDiv.className = 'alert alert-danger alert-sospensione position-fixed top-0 start-50 translate-middle-x mt-3 shadow-lg';
        alertDiv.style.zIndex = '10000';
        alertDiv.style.minWidth = '400px';
        alertDiv.setAttribute('data-auto-dismiss', 'false');
        alertDiv.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="bi bi-exclamation-octagon-fill display-6 me-3"></i>
            <div>
                <h5 class="mb-1"><strong>Account Sospeso</strong></h5>
                <p class="mb-0">${data.messaggio}</p>
            </div>
        </div>
    `;

        document.body.appendChild(alertDiv);

        // Reindirizza dopo 5 secondi
        setTimeout(() => {
            window.location.href = "/Account/Logout";
        }, 5000);
    });

    // ==================================================
    // AGGIORNAMENTO CORSA IN TEMPO REALE
    // ==================================================
    connection.on("AggiornaCorsa", data => {
        console.log("Aggiornamento corsa:", data);

        const durataEl = document.getElementById("durataCorsa");
        const costoEl = document.getElementById("costoCorsa");

        // Aggiorna durata
        if (durataEl && data.durataMinuti !== undefined) {
            const minuti = Math.floor(data.durataMinuti);
            const ore = Math.floor(minuti / 60);
            const min = minuti % 60;
            durataEl.textContent = ore > 0 ? `${ore}h ${min}m` : `${min} min`;

            durataEl.classList.add("updated");
            setTimeout(() => durataEl.classList.remove("updated"), 500);
        }

        // Aggiorna costo
        if (costoEl && data.costoParziale !== undefined) {
            costoEl.textContent = `${data.costoParziale.toFixed(2)} €`;

            costoEl.classList.add("updated");
            setTimeout(() => costoEl.classList.remove("updated"), 500);
        }

        // Mostra feedback visivo opzionale
        if (window.mobishare?.showInfoMessage) {
            mobishare.showInfoMessage(`Corsa aggiornata: ${data.costoParziale.toFixed(2)} € (${Math.round(data.durataMinuti)} min)`);
        }
    });


    // ==================================================
    // GESTIONE RICONNESSIONE E CHIUSURA
    // ==================================================
    connection.onreconnecting(error => {
        console.warn("SignalR: connessione persa, riconnessione in corso...", error);
        showWarningMessage("Connessione persa. Tentativo di riconnessione...");
    });

    connection.onreconnected(connectionId => {
        console.log("SignalR riconnesso:", connectionId);
        showSuccessMessage("Riconnesso al server");
    });

    connection.onclose(error => {
        console.error("SignalR: connessione chiusa:", error);
        showErrorMessage("Connessione persa definitivamente.");
    });

    // ==================================================
    // FUNZIONE DI AVVIO CON RETRY AUTOMATICO
    // ==================================================
    async function startSignalR() {
        try {
            await connection.start();
            console.log("SignalR connesso al backend");
        } catch (err) {
            console.error("Errore durante la connessione SignalR:", err);
            // Riprova dopo 5 secondi
            setTimeout(startSignalR, 5000);
        }
    }

    // Avvio iniziale
    startSignalR();

} else {
    console.log("SignalR non inizializzato (utente non loggato o libreria assente).");
}
