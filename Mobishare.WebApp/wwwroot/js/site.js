// ============================================
// MOBISHARE - JAVASCRIPT PRINCIPALE
// ============================================

document.addEventListener('DOMContentLoaded', function () {

    // ====================================
    // AUTO-DISMISS ALERTS (CORRETTI)
    // ====================================
    // Chiudi SOLO gli alert temporanei, NON quelli permanenti o con data-auto-dismiss="false"
    const alerts = document.querySelectorAll('.alert:not(.alert-permanent):not([data-auto-dismiss="false"]):not(.no-auto-dismiss)');
    alerts.forEach(alert => {
        // Auto-dismiss SOLO per alert di successo/info temporanei
        if (alert.classList.contains('alert-success') || alert.classList.contains('alert-info')) {
            setTimeout(() => {
                const bsAlert = bootstrap.Alert.getInstance(alert);
                if (bsAlert) {
                    bsAlert.close();
                } else {
                    // Se non è già un'istanza Bootstrap, creala
                    new bootstrap.Alert(alert).close();
                }
            }, 5000);
        }
        // Alert di errore e warning restano aperti fino a dismissione manuale
    });

    // ====================================
    // CONFERMA ELIMINAZIONE
    // ====================================
    const deleteButtons = document.querySelectorAll('[data-confirm-delete]');
    deleteButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            const message = this.getAttribute('data-confirm-delete') ||
                'Sei sicuro di voler eliminare questo elemento?';
            if (!confirm(message)) {
                e.preventDefault();
            }
        });
    });

    // ====================================
    // VALIDAZIONE FORM LATO CLIENT
    // ====================================
    const forms = document.querySelectorAll('.needs-validation');
    forms.forEach(form => {
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });

    // ====================================
    // TOGGLE PASSWORD VISIBILITY
    // ====================================
    const togglePasswordButtons = document.querySelectorAll('.toggle-password');
    togglePasswordButtons.forEach(button => {
        button.addEventListener('click', function () {
            const targetId = this.getAttribute('data-target');
            const input = document.getElementById(targetId);
            const icon = this.querySelector('i');

            if (input.type === 'password') {
                input.type = 'text';
                icon.classList.remove('bi-eye');
                icon.classList.add('bi-eye-slash');
            } else {
                input.type = 'password';
                icon.classList.remove('bi-eye-slash');
                icon.classList.add('bi-eye');
            }
        });
    });

    // ====================================
    // FORMATTAZIONE VALUTA
    // ====================================
    const currencyElements = document.querySelectorAll('[data-format="currency"]');
    currencyElements.forEach(element => {
        const value = parseFloat(element.textContent);
        if (!isNaN(value)) {
            element.textContent = new Intl.NumberFormat('it-IT', {
                style: 'currency',
                currency: 'EUR'
            }).format(value);
        }
    });

    // ====================================
    // FORMATTAZIONE DATE
    // ====================================
    const dateElements = document.querySelectorAll('[data-format="datetime"]');
    dateElements.forEach(element => {
        const dateStr = element.textContent.trim();
        const date = new Date(dateStr);
        if (!isNaN(date.getTime())) {
            element.textContent = new Intl.DateTimeFormat('it-IT', {
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            }).format(date);
        }
    });

    // ====================================
    // TOOLTIP BOOTSTRAP
    // ====================================
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));

    // ====================================
    // POPOVER BOOTSTRAP
    // ====================================
    const popoverTriggerList = document.querySelectorAll('[data-bs-toggle="popover"]');
    [...popoverTriggerList].map(popoverTriggerEl => new bootstrap.Popover(popoverTriggerEl));

    // ====================================
    // SMOOTH SCROLL PER ANCHOR
    // ====================================
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            if (href !== '#' && href !== '#!') {
                const target = document.querySelector(href);
                if (target) {
                    e.preventDefault();
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            }
        });
    });

    // ====================================
    // LOADING SPINNER SU FORM SUBMIT
    // ====================================
    const formsWithLoader = document.querySelectorAll('form[data-show-loader]');
    formsWithLoader.forEach(form => {
        form.addEventListener('submit', function () {
            const submitBtn = this.querySelector('button[type="submit"]');
            if (submitBtn && !submitBtn.disabled) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Caricamento...';
            }
        });
    });

    // ====================================
    // COUNTDOWN TIMER PER CORSE
    // ====================================
    const countdownElements = document.querySelectorAll('[data-countdown]');
    countdownElements.forEach(element => {
        const startTime = new Date(element.getAttribute('data-countdown'));

        setInterval(() => {
            const now = new Date();
            const diff = now - startTime;

            const hours = Math.floor(diff / (1000 * 60 * 60));
            const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
            const seconds = Math.floor((diff % (1000 * 60)) / 1000);

            element.textContent = `${hours}h ${minutes}m ${seconds}s`;
        }, 1000);
    });

    // ====================================
    // FILTRI DINAMICI (per pagina Mezzi)
    // ====================================
    const filtroTipo = document.getElementById('filtro-tipo');
    const filtroParcheggio = document.getElementById('filtro-parcheggio');

    if (filtroTipo) {
        filtroTipo.addEventListener('change', function () {
            applicaFiltri();
        });
    }

    if (filtroParcheggio) {
        filtroParcheggio.addEventListener('change', function () {
            applicaFiltri();
        });
    }

    function applicaFiltri() {
        const tipo = filtroTipo ? filtroTipo.value : '';
        const parcheggio = filtroParcheggio ? filtroParcheggio.value : '';

        const url = new URL(window.location);
        if (tipo) url.searchParams.set('TipoFiltro', tipo);
        else url.searchParams.delete('TipoFiltro');

        if (parcheggio) url.searchParams.set('ParcheggioFiltro', parcheggio);
        else url.searchParams.delete('ParcheggioFiltro');

        window.location.href = url.toString();
    }

    // ====================================
    // GESTIONE ERRORI IMMAGINI
    // ====================================
    const images = document.querySelectorAll('img[data-fallback]');
    images.forEach(img => {
        img.addEventListener('error', function () {
            this.src = this.getAttribute('data-fallback') || '/images/placeholder.png';
        });
    });

    // ====================================
    // CONFERMA LOGOUT
    // ====================================
    const logoutLinks = document.querySelectorAll('a[href*="/Account/Logout"]');
    logoutLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            if (!this.hasAttribute('data-no-confirm')) {
                if (!confirm('Sei sicuro di voler uscire?')) {
                    e.preventDefault();
                }
            }
        });
    });

    // ====================================
    // CONFERMA NOLEGGIO MEZZO
    // ====================================
    const noleggiaButtons = document.querySelectorAll('[data-confirm-noleggio]');
    noleggiaButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            const matricola = this.getAttribute('data-matricola');
            const tipo = this.getAttribute('data-tipo');
            const tariffa = this.getAttribute('data-tariffa');

            const message = `Confermi il noleggio del ${tipo} (${matricola})?\nTariffa: €${tariffa}/minuto`;

            if (!confirm(message)) {
                e.preventDefault();
            }
        });
    });

    // ====================================
    // UTILITY FUNCTIONS
    // ====================================
    window.mobishare = window.mobishare || {};

    window.mobishare.formatCurrency = function (value) {
        return new Intl.NumberFormat('it-IT', {
            style: 'currency',
            currency: 'EUR'
        }).format(value);
    };

    window.mobishare.formatDateTime = function (dateStr) {
        const date = new Date(dateStr);
        return new Intl.DateTimeFormat('it-IT', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        }).format(date);
    };

});

//centralizza aggiornamento del credito in un'unica funzione, chiamata da tutti gli eventi SignalR che lo toccano 
function aggiornaCredito(nuovoCredito) {
    const creditoElements = document.querySelectorAll('.credito-value, [data-credito], #credito-utente');

    creditoElements.forEach(el => {
        el.textContent = new Intl.NumberFormat('it-IT', {
            style: 'currency',
            currency: 'EUR'
        }).format(nuovoCredito);

        el.classList.remove('text-success', 'text-warning', 'text-danger');
        if (nuovoCredito > 5) el.classList.add('text-success');
        else if (nuovoCredito > 0) el.classList.add('text-warning');
        else el.classList.add('text-danger');
    });
}


// ============================================
// FUNZIONI GLOBALI
// ============================================

// Mostra messaggio di successo temporaneo
function showSuccessMessage(message) {
    const alert = document.createElement('div');
    alert.className = 'alert alert-success alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3';
    alert.style.zIndex = '9999';
    alert.innerHTML = `
        <i class="bi bi-check-circle-fill me-2"></i>${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(alert);

    setTimeout(() => {
        alert.remove();
    }, 3000);
}

// Mostra messaggio di errore temporaneo
function showErrorMessage(message) {
    const alert = document.createElement('div');
    alert.className = 'alert alert-danger alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3';
    alert.style.zIndex = '9999';
    alert.innerHTML = `
        <i class="bi bi-exclamation-triangle-fill me-2"></i>${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(alert);

    setTimeout(() => {
        alert.remove();
    }, 5000);
}

// Mostra messaggio di avviso temporaneo (giallo)
function showWarningMessage(message) {
    const alert = document.createElement('div');
    alert.className = 'alert alert-warning alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3';
    alert.style.zIndex = '9999';
    alert.innerHTML = `
        <i class="bi bi-exclamation-triangle-fill me-2"></i>${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(alert);

    setTimeout(() => alert.remove(), 4000);
}

// Mostra messaggio informativo temporaneo (azzurro)
function showInfoMessage(message) {
    const alert = document.createElement('div');
    alert.className = 'alert alert-info alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3';
    alert.style.zIndex = '9999';
    alert.innerHTML = `
        <i class="bi bi-info-circle-fill me-2"></i>${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(alert);

    setTimeout(() => alert.remove(), 4000);
}

// Conferma azione
function confirmAction(message, callback) {
    if (confirm(message)) {
        callback();
    }
}

console.log('Mobishare JavaScript caricato correttamente');

// ==================================================
// CONNESSIONE SIGNALR AL BACKEND
// ==================================================

// Recupero eventuale token JWT
const token = window.MOBISHARE_CONFIG?.jwtToken || "";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7001/hub/notifiche", {
        accessTokenFactory: () => token //ignorato se non serve
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .configureLogging(signalR.LogLevel.Information)
    .build();



// ==================================================
// EVENTI RICEVUTI DAL SERVER
// ==================================================

// Aggiornamento credito utente - AGGIORNATO per cercare l'elemento corretto
/*connection.on("CreditoAggiornato", function (nuovoCredito) {
    // Cerca tutti gli elementi che mostrano il credito
    const creditoElements = document.querySelectorAll('.credito-value, [data-credito], #credito-utente');

    creditoElements.forEach(el => {
        if (el) {
            const formattedCredito = new Intl.NumberFormat('it-IT', {
                style: 'currency',
                currency: 'EUR'
            }).format(nuovoCredito);

            el.textContent = formattedCredito;

            // Aggiorna il colore in base al valore
            el.classList.remove('text-success', 'text-danger', 'text-warning');
            if (nuovoCredito > 5) {
                el.classList.add('text-success');
            } else if (nuovoCredito > 0) {
                el.classList.add('text-warning');
            } else {
                el.classList.add('text-danger');
            }
        }
    });

    showSuccessMessage(`Credito aggiornato: ${new Intl.NumberFormat('it-IT', { style: 'currency', currency: 'EUR' }).format(nuovoCredito)}`);
});

connection.on("PagamentoCompletato", data => {
    showSuccessMessage(`Pagamento completato: ${data.importo}€ (${data.stato})`);

    // Aggiorna credito sul posto se incluso nei dati
    if (data.nuovoCredito !== undefined) {
        const creditoElements = document.querySelectorAll('.credito-value, [data-credito], #credito-utente');
        creditoElements.forEach(el => {
            el.textContent = new Intl.NumberFormat('it-IT', {
                style: 'currency',
                currency: 'EUR'
            }).format(data.nuovoCredito);

            el.classList.remove('text-success', 'text-warning', 'text-danger');
            if (data.nuovoCredito > 5) el.classList.add('text-success');
            else if (data.nuovoCredito > 0) el.classList.add('text-warning');
            else el.classList.add('text-danger');
        });
    }
});*/

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
    // esempio: aggiorna dashboard o tabella mezzi
    // aggiornaStatoMezzo(data.idMezzo, data.livelloBatteria, data.stato);
});

// Test generico
connection.on("RiceviMessaggio", function (user, message) {
    console.log(`Messaggio da ${user}: ${message}`);
});

// Notifica sospensione utente - MIGLIORATO
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