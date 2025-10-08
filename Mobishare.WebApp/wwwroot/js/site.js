// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ============================================
// MOBISHARE - JAVASCRIPT PRINCIPALE
// ============================================

document.addEventListener('DOMContentLoaded', function () {

    // ====================================
    // AUTO-DISMISS ALERTS
    // ====================================
    const alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
    alerts.forEach(alert => {
        // Auto-dismiss dopo 5 secondi
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
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
    // AGGIORNAMENTO CREDITO IN TEMPO REALE
    // (opzionale - se implementi SignalR o polling)
    // ====================================
    function aggiornaCredito() {
        const creditoElement = document.getElementById('credito-utente');
        if (creditoElement) {
            // Qui potresti fare una chiamata AJAX per aggiornare il credito
            // fetch('/api/utente/credito')
            //     .then(response => response.json())
            //     .then(data => creditoElement.textContent = `€ ${data.credito.toFixed(2)}`);
        }
    }

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

// Recupero eventuale token JWT (se usi autenticazione)
const token = sessionStorage.getItem("JwtToken") || localStorage.getItem("JwtToken");

const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7001/hub/notifiche", {
        accessTokenFactory: () => token // verrà ignorato se non serve
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .configureLogging(signalR.LogLevel.Information)
    .build();

// ==================================================
// EVENTI RICEVUTI DAL SERVER
// ==================================================

// Aggiornamento credito utente
connection.on("CreditoAggiornato", function (nuovoCredito) {
    const el = document.getElementById("credito-utente");
    if (el) {
        el.textContent = new Intl.NumberFormat('it-IT', { style: 'currency', currency: 'EUR' })
            .format(nuovoCredito);
        showSuccessMessage(`Credito aggiornato: ${el.textContent}`);
    }
});

// Aggiornamento telemetria (mezzi, batterie, ecc.)
connection.on("AggiornamentoTelemetria", function (data) {
    console.log("Telemetria ricevuta:", data);
    // esempio: aggiorna dashboard o tabella mezzi
    // aggiornaStatoMezzo(data.idMezzo, data.livelloBatteria, data.stato);
});

// Notifiche per admin
connection.on("RiceviNotificaAdmin", function (titolo, testo) {
    showSuccessMessage(`[ADMIN] ${titolo}: ${testo}`);
});

// Test generico
connection.on("RiceviMessaggio", function (user, message) {
    console.log(`Messaggio da ${user}: ${message}`);
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
    // Tenta di riconnettersi anche dopo chiusura
    setTimeout(startSignalR, 5000);
});

// ==================================================
// FUNZIONE DI AVVIO CON RETRY AUTOMATICO
// ==================================================
async function startSignalR() {
    try {
        await connection.start();
        console.log("✅ SignalR connesso al backend");
    } catch (err) {
        console.error("❌ Errore durante la connessione SignalR:", err);
        // Riprova dopo 5 secondi
        setTimeout(startSignalR, 5000);
    }
}

// Avvio iniziale
startSignalR();
