// ==============================
// CONNESSIONE SIGNALR
// ==============================

(function initSignalR() { 

    if (window.mobishareConnection) {
        if (window.mobishareConnection.state === "Connected") {
            console.log("SignalR già attivo, non ricreo la connessione.");
            return;
        }
        console.log("SignalR esistente ma disconnesso, ricreo connessione");
    }

    // Recupero token da sessionStorage
    const token = sessionStorage.getItem("jwtToken");

    const API_BASE = "https://localhost:7001";

    if (typeof signalR !== "undefined" && token) {

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE}/hub/notifiche`, {
                accessTokenFactory: () => token,
                transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        connection.serverTimeoutInMilliseconds = 120000;
        connection.keepAliveIntervalInMilliseconds = 15000;

        window.mobishareConnection = connection;



        // ==============================
        // EVENTI UTENTE (OK)
        // ==============================
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

        connection.on("NuovaTransazione", data => {
            showInfoMessage(`Nuova transazione: ${data.tipo} ${data.importo}€`);

            if (window.location.pathname.includes('/Transazioni')) {
                setTimeout(() => window.location.reload(), 2000);
            }
        });

        connection.on("UtenteRiattivato", data => {
            showSuccessMessage(`Account riattivato: ${data.nome}. ${data.messaggio}`);

            // Ricarica la pagina dopo 2 secondi per aggiornare lo stato
            setTimeout(() => {
                window.location.reload();
            }, 2000);
        });

        //BONUS BICI MUSCOLARE
        //Quando l'utente GUADAGNA punti bonus alla fine di una corsa con BiciMuscolare
        connection.on("BonusApplicato", data => {
            console.log("Bonus guadagnato:", data);

            //messaggio utente se esiste badge in pagina attuale
            if (window.mobishare?.showSuccessMessage) {
                mobishare.showSuccessMessage(`${data.Messaggio} (+${data.Punti} punti)`);
            }

            // Aggiorna badge dei punti nella pagina CorsaCorrente
            const puntiEl = document.getElementById("puntiBonus");
            if (puntiEl) {
                puntiEl.textContent = data.TotalePunti;

                const card = puntiEl.closest(".card");
                if (card) {
                    card.style.transition = "background-color 0.6s ease";
                    card.style.backgroundColor = "#fff3cd"; // giallo
                    setTimeout(() => card.style.backgroundColor = "", 600);
                }
            }

            //Effetto evidenziatore su wrapper specifico (es. CorsaCorrente)
            const wrapper = document.getElementById("puntiBonusWrapper");
            if (wrapper) {
                wrapper.style.transition = "background-color 0.6s ease";
                wrapper.style.backgroundColor = "#fff3cd"; // highlight
                setTimeout(() => wrapper.style.backgroundColor = "", 600);
            }
        });

        // Quando l'utente USA i punti per ottenere uno sconto
        connection.on("BonusUsato", data => {
            console.log("Bonus usato:", data);

            // Messaggio utente
            if (window.mobishare?.showInfoMessage) {
                mobishare.showInfoMessage(`${data.Messaggio}`);
            }

            // Aggiorna punti se il badge esiste nella pagina attuale
            const puntiEl = document.getElementById("puntiBonus");
            if (puntiEl) {
                puntiEl.textContent = data.TotalePunti;

                // Effetto evidenziatore rosso sulla card (se esiste)
                const card = puntiEl.closest(".card");
                if (card) {
                    card.style.transition = "background-color 0.6s ease";
                    card.style.backgroundColor = "#ffd6d6"; // rosso
                    setTimeout(() => card.style.backgroundColor = "", 600);
                }


                //Evidenziazione rossa per indicare "punti spesi"
                const wrapper = document.getElementById("puntiBonusWrapper");
                if (wrapper) {
                    wrapper.style.transition = "background-color 0.6s ease";
                    wrapper.style.backgroundColor = "#ffd6d6";
                    setTimeout(() => wrapper.style.backgroundColor = "", 600);
                }
            }
        });

        // AGGIORNAMENTO CORSA IN TEMPO REALE
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






        // ==============================
        // EVENTI ADMIN -> DROPDOWN SOLO CAMPANELLA 
        // ==============================

        // Array notifiche lato client (massimo 20)
        let notificheAdmin = [];
        const MAX_NOTIFICHE = 20;
        const STORAGE_KEY = 'mobishare_notifiche_admin';

        //CARICA le notifiche salvate all'avvio
        try {
            const saved = localStorage.getItem(STORAGE_KEY);
            if (saved) {

                notificheAdmin = JSON.parse(saved);
                notificheAdmin.forEach(n => n.timestamp = new Date(n.timestamp));
                console.log(`Caricate ${notificheAdmin.length} notifiche da localStorage`);

                if (notificheAdmin.length > 0) {
                    if (document.readyState === 'loading') {
                        document.addEventListener('DOMContentLoaded', () => {
                            aggiornaDropdownNotifiche();
                            aggiornaBadgeNotifiche();
                            console.log('UI notifiche aggiornata dopo DOMContentLoaded');
                        });
                    } else {
                        //DOM pronto, aggiorna subito
                        aggiornaDropdownNotifiche();
                        aggiornaBadgeNotifiche();
                        console.log('UI notifiche aggiornata immediatamente');
                    }
                }
            }
        } catch (e) {
            console.error('Errore caricamento notifiche:', e);
            notificheAdmin = [];
        }

        function salvaNotifiche() {
            try {
                localStorage.setItem(STORAGE_KEY, JSON.stringify(notificheAdmin));
                console.log(`Salvate ${notificheAdmin.length} notifiche in localStorage`);
            } catch (e) {
                console.error('Errore salvataggio notifiche:', e);
            }
        }

        // Aggiunge una notifica admin alla campanella
        function addNotificaAdmin(titolo, testo) {

            const nuova = {
                id: Date.now(),
                titolo: titolo, 
                testo: testo,
                timestamp: new Date(),
                letta: false
            };

            notificheAdmin.unshift(nuova);

            if (notificheAdmin.length > MAX_NOTIFICHE) {
                notificheAdmin = notificheAdmin.slice(0, MAX_NOTIFICHE);
            }

            aggiornaDropdownNotifiche();
            aggiornaBadgeNotifiche();
            salvaNotifiche();
        }

        //Telemetria Critica → es. batteria < 20%
        connection.on("TelemetriaCritica", payload => {
            addNotificaAdmin(
                "Batteria critica",
                `Mezzo ${payload.Matricola} al ${payload.Batteria}%`
            );
        });

        //Segnalazione Guasto → dall’utente
        connection.on("SegnalazioneGuasto", payload => {
            addNotificaAdmin(
                "Segnalazione guasto",
                `Mezzo ${payload.Matricola}: ${payload.Dettagli}`
            );
        });

        //Notifica generica → usata dal backend
        connection.on("NotificaAdmin", payload => {
            addNotificaAdmin(payload.titolo, payload.testo); 
        });

        window.debugNotifiche = notificheAdmin; 

        //SistemaStato → monitoraggio sistema (MQTT, Gateway, Background Services)
        connection.on("SistemaStato", payload => {
            addNotificaAdmin(
                `Sistema: ${payload.Tipo}`,
                `${payload.Messaggio} (${payload.Stato})`
            );
        });

        //Aggiornamento telemetria (mezzi, batterie, ecc.)
        connection.on("AggiornamentoTelemetria", function (data) {
            console.log("Telemetria ricevuta:", data);

            // Aggiorna la tabella solo se siamo nella pagina Gestione Mezzi
            const path = window.location.pathname.toLowerCase();
            if (path.includes("/dashboardadmin/gestionemezzi")) {
                aggiornaStatoMezzo(data.idMezzo, data.livelloBatteria, data.stato);
            }
        });

        //notifica per utente sospeso dal sistema -> credito insufficiente 
        connection.on("UtenteSospesoAdmin", payload => {
            addNotificaAdmin(
                "Utente sospeso",
                `L'utente con ID ${payload.IdUtente} è stato sospeso`
            );
        });



        //FUNZIONI DI APPOGGIO PER NOTIFICHE CAMPANELLA 
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
            if (!lista) {
                console.warn("Elemento 'notifiche-lista' non trovato");
                return;
            };

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
                <a class="dropdown-item ${classeNonLetta} py-3 px-3" href="#" 
                   data-notifica-id="${notifica.id}" style="white-space: normal;">
                    <div class="d-flex justify-content-between align-items-start">
                        <div style="flex: 1; min-width: 0;">
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

        //Segna una notifica come letta
        function segnaNotificaComeLettura(id) {
            const notifica = notificheAdmin.find(n => n.id === id);
            if (notifica && !notifica.letta) {
                notifica.letta = true;
                aggiornaDropdownNotifiche();
                aggiornaBadgeNotifiche();
                salvaNotifiche(); 
            }
        }

        //Segna tutte le notifiche come lette
        function segnaTutteComeLette() {
            notificheAdmin.forEach(n => n.letta = true);
            aggiornaDropdownNotifiche();
            aggiornaBadgeNotifiche();
            salvaNotifiche();
        }

        //Funzione helper: tempo relativo (es. "2 minuti fa")
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

        //Funzione helper: escape HTML per sicurezza
        function escapeHtml(text) {
            if (!text) return '';
            const map = {
                '&': '&amp;',
                '<': '&lt;',
                '>': '&gt;',
                '"': '&quot;',
                "'": '&#039;'
            };
            return text.replace(/[&<>"']/g, m => map[m]);
        }

        //Event listener per pulsante "segna tutte come lette"
        document.addEventListener('DOMContentLoaded', function () {
            const btnCancellaTutte = document.getElementById('cancella-tutte-notifiche');
            if (btnCancellaTutte) {
                btnCancellaTutte.addEventListener('click', function (e) {
                    e.preventDefault();
                    segnaTutteComeLette();
                });
            }
        });

        // FUNZIONE: aggiornaStatoMezzo()
        // Aggiorna dinamicamente la tabella mezzi in GestioneMezzi
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

            // Aggiorna livello batteria
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

            // Aggiorna stato
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

            // Effetto visivo di aggiornamento
            rigaTrovata.style.transition = "background-color 0.6s ease";
            rigaTrovata.style.backgroundColor = "#e6f7ff";
            setTimeout(() => (rigaTrovata.style.backgroundColor = ""), 800);
        }


        // GESTIONE RICONNESSIONE E CHIUSURA
        connection.onreconnecting(error => {
            console.warn("SignalR: connessione persa, riconnessione in corso...", error);
        });

        connection.onreconnected(connectionId => {
            console.log("SignalR riconnesso:", connectionId);
        });

        connection.onclose(error => {
            console.error("SignalR: connessione chiusa:", error);
            showErrorMessage("Connessione persa definitivamente.");
        });

        // FUNZIONE DI AVVIO CON RETRY AUTOMATICO
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
})(); 
