# Mobishare - Documentazione per Relazione

## 📋 Indice dei File

Questa cartella contiene tutta la documentazione necessaria per la relazione del progetto Mobishare.

---

## ✅ Fase di Analisi

### 1. Casi d'Uso
**File:** `casi-uso.puml`  
**Descrizione:** Diagramma UML dei casi d'uso completo con 12 use case principali:
- UC1: Gestione autenticazione
- UC2: Gestione utenti (Gestore)
- UC3: Consultazione mezzi
- UC4: Amministrazione mezzi (Gestore)
- UC5: Consultazione parcheggi
- UC6: Amministrazione parcheggi (Gestore)
- UC7: Consultazione corse
- UC8: Gestione corsa (Utente)
- UC9: Gestione ricariche (Utente)
- UC10: Consultazione transazioni
- UC11: Gestione feedback
- UC12: Dashboard Gestore

**Attori:**
- Utente (utente finale della piattaforma)
- Gestore (amministratore del sistema)

### 2. Diagramma Classi del Dominio
**File:** `domain_model.puml`  
**Descrizione:** Diagramma delle classi del dominio senza metodi implementativi. Include:
- Entità: Utente, Mezzo, Parcheggio, Corsa, Ricarica, Pagamento, Feedback
- Enumerazioni: UserRole, TipoMezzo, StatoMezzo, StatoCorsa, ecc.
- Relazioni tra entità (aggregazione, composizione, associazione)
- Annotazioni con vincoli di business

**Regole di Business:**
- Un utente può avere al massimo una corsa attiva
- Un mezzo in uso non può essere prenotato da altri
- Credito insufficiente → utente sospeso
- Batteria < 20% → mezzo non prelevabile
- Sistema punti bonus: ogni 10 corse completate → sconto

---

## ✅ Fase di Progettazione

### 3. Diagramma delle Classi Implementate
**File:** `diagramma_classi.puml`  
**Descrizione:** Diagramma delle classi implementate con architettura a microservizi. Include:

**Packages:**
- `Mobishare.Core.Models` - Entità persistite su database
- `Mobishare.Core.DTOs` - Data Transfer Objects per API
- `Mobishare.Core.Enums` - Enumerazioni condivise
- `Mobishare.API.Controllers` - Controller REST API
- `Mobishare.Infrastructure` - DbContext, Services, SignalR Hubs
- `Mobishare.IoT.Gateway` - **Emulatore dispositivi IoT** (architettura multi-gateway)

**Classi Chiave IoT.Gateway:**
- `MqttGatewayManager`: Orchestratore di N gateway (uno per parcheggio)
- `MqttGatewayEmulatorService`: Emulatore singolo dispositivo IoT
- `GatewaySyncBackgroundService`: Sincronizzazione automatica DB ↔ Gateway (ogni 20s)
- `MqttGatewayHostedService`: Gestione lifecycle gateway

**Pattern Architetturali:**
- Repository Pattern (DbContext + EF Core)
- Service Layer (MqttIoTService, RideMonitoringService)
- Background Services (monitoring corse, telemetria MQTT)
- Dependency Injection (ASP.NET Core DI Container)

### 4. Diagrammi di Sequenza
**File:** `sequence_diagrams.puml`  
**Descrizione:** 4 diagrammi di sequenza per interazioni complesse:

**Scenario 1: Avvio Corsa**
- Utente → WebApp → API → DbContext (verifica credito + disponibilità mezzo)
- API → MqttIoTService → MQTT Broker → IoT Gateway (comando sblocca)
- Gateway → simulazione scarica batteria
- API → SignalR Hub → Utente (notifica real-time "Corsa iniziata!")

**Scenario 2: Aggiornamento Telemetria Batteria**
- Gateway → MQTT Broker → API (MqttTelemetryListenerService)
- API → DbContext (aggiorna livello batteria)
- Se batteria < 20%: API → DbContext (Stato = NonPrelevabile)
- API → SignalR Hub → Dashboard Gestore (notifica batteria critica)

**Scenario 3: Terminazione Corsa con Pagamento**
- Utente → WebApp → API → DbContext (BEGIN TRANSACTION)
- API: Calcolo costo (0.50€ base + minuti extra × tariffa)
- API → DbContext: Aggiorna Corsa, Crea Pagamento, Decrementa Credito
- Se credito < 0: Sospendi utente
- Se corse % 10 == 0: Incrementa punti bonus + notifica SignalR
- API → MqttIoTService (comando blocca)
- Gateway: Ferma simulazione batteria

**Scenario 4: Sincronizzazione Gateway con DB**
- GatewaySyncBackgroundService → MqttGatewayManager (ogni 20s)
- Manager → DbContext (carica tutti i mezzi)
- Manager → Gateway N (GetMezziInMemoriaAsync)
- Confronto stato DB vs in-memory
- Aggiunge/rimuove mezzi dinamicamente
- Gestisce spostamenti tra parcheggi

---

## ✅ Descrizione API REST e Topic MQTT

### 5. Documentazione API REST (OpenAPI 3.0)
**File:** `mobishare-api.yaml`  
**Descrizione:** Specifica OpenAPI 3.0 completa con:

**Endpoint principali:**
- `/api/utenti` - Registrazione, login, gestione utenti
- `/api/mezzi` - CRUD mezzi, ricerca per matricola, segnalazione guasti
- `/api/corse` - Avvio, terminazione, storico corse
- `/api/parcheggi` - Gestione parcheggi, disponibilità posti
- `/api/ricariche` - Ricariche credito, conferma pagamento
- `/api/pagamenti` - Transazioni finanziarie
- `/api/feedback` - Feedback utenti, statistiche
- `/api/dashboard` - Statistiche aggregate per gestore

**Autenticazione:** JWT Bearer Token  
**Formati Supportati:** JSON  
**Status Codes:** 200, 201, 204, 400, 401, 403, 404

**Schemas completi:**
- DTOs per tutte le entità (UtenteDTO, MezzoResponseDTO, CorsaResponseDTO, ecc.)
- Request models per creazione/aggiornamento
- Error responses standardizzati

**Utilizzo:**
- Importare in **Swagger Editor** (https://editor.swagger.io/)
- Generare client API automatici (C#, JavaScript, Python, ecc.)
- Testing con Swagger UI

### 6. Documentazione Topic MQTT
**File:** `mqtt_topics.md`  
**Descrizione:** Documentazione completa del protocollo MQTT. Include:

**Struttura Gerarchica:**
```
mobishare/
├── mezzi/{matricola}/
│   ├── comandi       # API → Gateway (sblocca, blocca)
│   ├── telemetria    # Gateway → API (batteria, GPS)
│   └── status        # Gateway → API (heartbeat)
└── sistema/
    └── notifiche     # Notifiche broadcast
```

**Topic: Comandi** (`mobishare/mezzi/{matricola}/comandi`)
- Direzione: API → IoT Gateway
- QoS: 1 (At least once)
- Azioni: `sblocca`, `blocca`, `status`, `ricarica_batteria`, `manutenzione`

**Topic: Telemetria** (`mobishare/mezzi/{matricola}/telemetria`)
- Direzione: IoT Gateway → API
- QoS: 0 (At most once)
- Tipi: `batteria`, `gps`, `diagnostica`, `batteria_critica`
- Frequenza: Ogni 10 secondi (durante corsa)

**Formato Messaggi (JSON):**
```json
// Comando Sblocca
{
  "azione": "sblocca",
  "matricola": "BE002",
  "timestamp": "2025-11-21T10:30:15Z",
  "idCorsa": 42,
  "idUtente": 3
}

// Telemetria Batteria
{
  "matricola": "BE002",
  "tipo": "batteria",
  "livelloBatteria": 68,
  "stato": "InUso",
  "timestamp": "2025-11-21T10:35:00Z"
}
```

**Broker MQTT:**
- Software: Mosquitto (https://mosquitto.org/)
- Host: localhost (sviluppo)
- Porta: 1883 (standard MQTT)
- Protocollo: MQTT v3.1.1

**Testing:**
```bash
# Subscribe a tutti i topic
mosquitto_sub -h localhost -p 1883 -t "mobishare/#" -v

# Publish comando manuale
mosquitto_pub -h localhost -p 1883 \
  -t "mobishare/mezzi/BE002/comandi" \
  -m '{"azione":"sblocca","matricola":"BE002"}'
```

---

## 📊 Requisiti Funzionali e Non Funzionali

### Requisiti Funzionali (derivati dai casi d'uso)

**RF1 - Autenticazione e Autorizzazione**
- Sistema di registrazione utenti con email e password
- Login con autenticazione JWT
- Ruoli: Utente, Gestore
- Cambio password e reset password

**RF2 - Gestione Mezzi**
- CRUD completo sui mezzi (solo Gestore)
- Ricerca mezzi per matricola, tipo, stato
- Visualizzazione mezzi disponibili (filtro per batteria > 20%)
- Segnalazione guasti
- Aggiornamento telemetria batteria via MQTT

**RF3 - Gestione Corse**
- Avvio corsa con prenotazione mezzo
- Controllo credito sufficiente prima dell'avvio
- Vincolo: 1 corsa attiva per utente
- Terminazione corsa con calcolo automatico costo
- Storico corse per utente
- Segnalazione problemi durante la corsa

**RF4 - Sistema Tariffario**
- Costo base: 0.50€ (primi 30 minuti)
- Tariffa per minuto extra:
  - Monopattino Elettrico: 0.25€/min
  - Bici Elettrica: 0.20€/min
  - Bici Muscolare: 0.10€/min

**RF5 - Gestione Credito**
- Ricariche credito utente
- Webhook per conferma pagamento esterno
- Decremento automatico credito al termine corsa
- Sospensione automatica utente se credito < 0

**RF6 - Sistema Punti Bonus**
- Ogni 10 corse completate → +1 punto bonus
- Notifica real-time via SignalR all'utente

**RF7 - Dashboard Gestore**
- Statistiche aggregate: utenti, mezzi, corse, credito totale
- Notifiche real-time (batteria critica, guasti, ecc.)
- Gestione utenti sospesi
- Feedback utenti negativi

**RF8 - Feedback**
- Invio feedback con voto (1-5 stelle) e commento
- Visualizzazione feedback recenti e negativi (Gestore)

### Requisiti Non Funzionali

**RNF1 - Architettura**
- Separazione frontend (Razor Pages) e backend (REST API)
- IoT Gateway standalone per gestione dispositivi
- Pattern: Repository, Service Layer, Background Services

**RNF2 - Comunicazione IoT**
- Protocollo: MQTT v3.1.1
- Broker: Mosquitto
- QoS: 1 per comandi, 0 per telemetria

**RNF3 - Database**
- SQLite per sviluppo (mobishare.db)
- Entity Framework Core 8.0.7
- Migrazioni per versionamento schema

**RNF4 - Real-time**
- SignalR per notifiche push
- WebSocket per dashboard gestore
- Aggiornamento UI senza page reload

**RNF5 - Sicurezza**
- Autenticazione JWT con expiration
- Password hashate (BCrypt/PBKDF2)
- HTTPS in produzione

**RNF6 - Performance**
- Background service per monitoring corse (ogni 1 minuto)
- Sincronizzazione gateway ogni 20 secondi
- Telemetria batteria ogni 10 secondi (solo durante corsa)

**RNF7 - Concorrenza**
- Concurrency token (RowVersion) su Mezzo e Corsa
- Transazioni ACID per operazioni critiche (avvio/termina corsa)

**RNF8 - Tecnologie**
- ASP.NET Core 8.0 (C#)
- MQTTnet 4.3.7.1207
- Entity Framework Core 8.0.7
- SignalR
- Razor Pages (frontend)

---

## 📂 Struttura Progetto

```
Mobishare.sln
├── Mobishare.API/              # REST API Backend
│   ├── Controllers/            # Endpoint REST
│   ├── BackgroundServices/     # Monitoring corse, MQTT listener
│   └── Program.cs
├── Mobishare.Core/             # Entità, DTOs, Enums
│   ├── Models/                 # Entità domain
│   ├── DTOs/                   # Data Transfer Objects
│   └── Enums/                  # Enumerazioni
├── Mobishare.Infrastructure/   # DbContext, Services
│   ├── Services/               # MqttIoTService, RideMonitoring
│   ├── SignalRHubs/            # NotificheHub
│   └── Data/                   # MobishareDbContext
├── Mobishare.IoT.Gateway/      # Emulatore dispositivi IoT
│   ├── Services/               # MqttGatewayManager, Emulator
│   └── Program.cs              # Console app standalone
└── Mobishare.WebApp/           # Frontend Razor Pages
    ├── Pages/                  # UI utente e gestore
    └── wwwroot/                # CSS, JS, immagini
```

---

## 🚀 Avvio Sistema

**1. Avvia MQTT Broker**
```bash
mosquitto -c mosquitto.conf -v
```

**2. Avvia IoT Gateway (5 gateway per 5 parcheggi)**
```bash
cd Mobishare.IoT.Gateway
dotnet run
```

**3. Avvia API Backend**
```bash
cd Mobishare.API
dotnet run
# Swagger UI: https://localhost:7234/swagger
```

**4. Avvia WebApp Frontend**
```bash
cd Mobishare.WebApp
dotnet run
# UI: https://localhost:7099
```

---

## 📖 Guide Complete

- **Architettura IoT Gateway:** `ARCHITETTURA_IOT_GATEWAY.md`
- **Guida Avvio Refactored:** `GUIDA_AVVIO_REFACTORED.md`
- **Setup Philips Hue:** `PHILIPS_HUE_EMULATOR_SETUP.md`
- **Scenari Test Manuali:** `SCENARI_TEST_MANUALI.md`

---

## 📝 Note per la Relazione

### Fase di Analisi (Completata)
✅ Casi d'uso UML con 12 UC principali  
✅ Diagramma classi del dominio (senza metodi)  
✅ Requisiti funzionali derivati dai casi d'uso  
✅ Requisiti non funzionali (architettura, tecnologie)

### Fase di Progettazione (Completata)
✅ Diagramma classi implementate (multi-microservizi)  
✅ Diagrammi di sequenza per 4 scenari complessi  
✅ Documentazione API REST (OpenAPI 3.0)  
✅ Documentazione topic MQTT con esempi  
✅ Pattern architetturali documentati

### Implementazione (Completata)
✅ Tutti i microservizi implementati e funzionanti  
✅ Database SQLite con 18 mezzi su 5 parcheggi  
✅ Emulatore IoT con 5 gateway attivi  
✅ Real-time updates (SignalR + JavaScript timer)  
✅ Testing end-to-end superato

---

**Versione:** 1.0  
**Data:** 21 Novembre 2025  
**Progetto:** Mobishare - Piattaforma Bike/Scooter Sharing
