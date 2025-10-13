// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

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
            if (!confirm('Sei sicuro di voler uscire?')) {
                e.preventDefault();
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
const token = sessionStorage.getItem("JwtToken") || localStorage.getItem("JwtToken");

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
connection.on("CreditoAggiornato", function (nuovoCredito) {
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

    // Ricarica la pagina se siamo sulla dashboard o ricariche
    if (window.location.pathname.includes('/Dashboard') || window.location.pathname.includes('/Ricariche')) {
        setTimeout(() => {
            window.location.reload();
        }, 2000);
    }
});

connection.on("PagamentoFallito", data => {
    showErrorMessage(`Pagamento fallito: ${data.importo}€`);
});

connection.on("PagamentoAggiornato", data => {
    showInfoMessage(`Pagamento aggiornato: ${data.importo}€ (${data.stato})`);
});

connection.on("NuovaTransazione", data => {
    showInfoMessage(`Nuova transazione: ${data.tipo} ${data.importo}€`);
});

connection.on("AccountRiattivato", data => {
    showSuccessMessage(`Account riattivato: ${data.nome}. ${data.messaggio}`);

    // Ricarica la pagina dopo 2 secondi per aggiornare lo stato
    setTimeout(() => {
        window.location.reload();
    }, 2000);
});

connection.on("RiceviNotificaAdmin", (titolo, testo) => {
    showInfoMessage(`[ADMIN] ${titolo}: ${testo}`);
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