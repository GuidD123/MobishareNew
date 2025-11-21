# Mobishare - Documentazione MQTT Topics

## Indice
1. [Panoramica](#panoramica)
2. [Broker MQTT](#broker-mqtt)
3. [Struttura Gerarchica Topics](#struttura-gerarchica-topics)
4. [Topic: Comandi](#topic-comandi)
5. [Topic: Telemetria](#topic-telemetria)
6. [Topic: Status](#topic-status)
7. [Esempi di Messaggi](#esempi-di-messaggi)
8. [Quality of Service (QoS)](#quality-of-service-qos)

---

## Panoramica

Il sistema Mobishare utilizza il protocollo **MQTT** per la comunicazione tra:
- **API Backend** (Publisher/Subscriber)
- **IoT Gateway** (Publisher/Subscriber) - emula dispositivi hardware sui veicoli
- **Dispositivi IoT** (teoricamente sui mezzi fisici) - in questo progetto emulati

**Libreria utilizzata:** `MQTTnet` (v4.3.7.1207) con `ManagedMqttClient`

---

## Broker MQTT

- **Host:** `localhost` (sviluppo) / IP broker produzione
- **Porta:** `1883` (standard MQTT senza TLS)
- **Protocollo:** MQTT v3.1.1
- **Autenticazione:** Nessuna (sviluppo) / Username/Password (produzione)

**Broker consigliato:** Mosquitto (https://mosquitto.org/)

### Avvio Mosquitto (Windows)
```bash
mosquitto -c mosquitto.conf -v
```

### Configurazione minima (`mosquitto.conf`)
```
listener 1883
allow_anonymous true
log_type all
```

---

## Struttura Gerarchica Topics

```
Parking/
├── {idParcheggio}/
│   ├── Comandi/
│   │   └── {idMezzo}        # API → Gateway (sblocca, blocca, ricarica)
│   ├── Mezzi/
│   │   └── {idMezzo}        # Gateway → API (telemetria: batteria, stato)
│   └── RisposteComandi/
│       └── {idMezzo}        # Gateway → API (ACK comandi)
```

**IMPORTANTE:** La struttura reale implementata usa `Parking/{id}/...` e non `mobishare/mezzi/...`

---

## Topic: Comandi

### Pattern
```
Parking/{idParcheggio}/Comandi/{idMezzo}
```

### Direzione
**API → IoT Gateway**

### Descrizione
Invia comandi di controllo ai mezzi (sblocca, blocca, richiesta stato, ecc.)

### Formato Messaggio (JSON) - ComandoMezzoMessage

#### Comando: Sblocca Mezzo
```json
{
  "commandId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "comando": "Sblocca",
  "mittenteId": "3",
  "parametri": {
    "UtenteId": "3",
    "Timestamp": "2025-11-21T10:30:15Z"
  }
}
```

#### Comando: Blocca Mezzo
```json
{
  "commandId": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
  "comando": "Blocca",
  "mittenteId": "SYSTEM",
  "parametri": {
    "Timestamp": "2025-11-21T12:45:30Z"
  }
}
```

**Classe C#:** `Mobishare.Core.Models.ComandoMezzoMessage`

---

## Topic: Telemetria

### Pattern
```
Parking/{idParcheggio}/Mezzi/{idMezzo}
```

### Direzione
**IoT Gateway → API**

### Descrizione
Telemetria periodica (ogni 10 secondi durante le corse) con stato e livello batteria.

**Consumer:** `MqttIoTBackgroundService` (Infrastructure/IoT/HostedServices)
- Si sottoscrive all'evento `MezzoStatusReceived` di `MqttIoTService`
- Aggiorna il database con stato e batteria
- Inoltra via SignalR (`AggiornamentoTelemetria`) a tutti i client connessi

### Formato Messaggio (JSON) - MezzoStatusMessage

```json
{
  "matricola": "BE002",
  "stato": "InUso",
  "livelloBatteria": 78,
  "gpsLatitude": null,
  "gpsLongitude": null
}
```

**Classe C#:** `Mobishare.Core.Models.MezzoStatusMessage`

**NOTA:** Il vecchio servizio `MqttTelemetryListenerService.cs` (in API/BackgroundServices) è commentato e NON più utilizzato.

---

## Topic: Risposte Comandi

### Pattern
```
Parking/{idParcheggio}/RisposteComandi/{idMezzo}
```

### Direzione
**IoT Gateway → API**

### Descrizione
Acknowledgment (ACK) dei comandi ricevuti ed elaborati dal gateway.

### Formato Messaggio (JSON) - RispostaComandoMessage

```json
{
  "commandId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "comandoOriginale": "Sblocca",
  "successo": true,
  "messaggio": "Mezzo sbloccato con successo",
  "timestamp": "2025-11-21T10:30:16Z"
}
```

**Classe C#:** `Mobishare.Core.Models.RispostaComandoMessage`
  "timestamp": "2025-11-21T11:45:30Z",
  "idCorsa": 42
}
```

#### Comando: Richiedi Status
```json
{
  "azione": "status",
  "matricola": "ME005",
  "timestamp": "2025-11-21T12:00:00Z"
}
```

#### Comando: Ricarica Batteria (per test/manutenzione)
```json
{
  "azione": "ricarica_batteria",
  "matricola": "BE002",
  "livelloBatteria": 100,
  "timestamp": "2025-11-21T14:00:00Z"
}
```

### Azioni Disponibili
| Azione | Descrizione | Parametri Richiesti |
|--------|-------------|---------------------|
| `sblocca` | Sblocca il mezzo per avviare corsa | `matricola`, `idCorsa`, `idUtente` |
| `blocca` | Blocca il mezzo al termine corsa | `matricola`, `idCorsa` |
| `status` | Richiede stato attuale del mezzo | `matricola` |
| `ricarica_batteria` | Simula ricarica batteria (test) | `matricola`, `livelloBatteria` |
| `manutenzione` | Imposta mezzo in manutenzione | `matricola`, `motivo` |

---

## Topic: Telemetria

### Pattern
```
mobishare/mezzi/{matricola}/telemetria
```

### Direzione
**IoT Gateway → API**

### Descrizione
Invia dati di telemetria dai mezzi verso il backend (batteria, GPS, diagnostica)

### Formato Messaggio (JSON)

#### Telemetria: Aggiornamento Batteria
```json
{
  "matricola": "BE002",
  "tipo": "batteria",
  "livelloBatteria": 68,
  "stato": "InUso",
  "timestamp": "2025-11-21T10:35:00Z"
}
```

#### Telemetria: Posizione GPS (se implementato)
```json
{
  "matricola": "ME005",
  "tipo": "gps",
  "latitudine": 45.4642,
  "longitudine": 9.1900,
  "velocita": 18.5,
  "timestamp": "2025-11-21T10:36:12Z"
}
```

#### Telemetria: Diagnostica
```json
{
  "matricola": "BM001",
  "tipo": "diagnostica",
  "pressione_gomme": "OK",
  "freni": "OK",
  "luci": "OK",
  "anomalie": [],
  "timestamp": "2025-11-21T10:37:00Z"
}
```

#### Telemetria: Batteria Critica
```json
{
  "matricola": "BE002",
  "tipo": "batteria_critica",
  "livelloBatteria": 18,
  "stato": "NonPrelevabile",
  "motivoNonPrelevabile": "BatteriaScarica",
  "timestamp": "2025-11-21T11:20:00Z"
}
```

### Tipi di Telemetria
| Tipo | Descrizione | Frequenza |
|------|-------------|-----------|
| `batteria` | Livello batteria corrente | Ogni 10 secondi (durante corsa) |
| `gps` | Posizione GPS del mezzo | Ogni 30 secondi (durante corsa) |
| `diagnostica` | Stato componenti mezzo | Ogni 5 minuti (sempre) |
| `batteria_critica` | Allarme batteria < 20% | Event-driven |

---

## Topic: Status

### Pattern
```
mobishare/mezzi/{matricola}/status
```

### Direzione
**IoT Gateway → API** (heartbeat)

### Descrizione
Invia heartbeat periodico per confermare che il dispositivo è online

### Formato Messaggio (JSON)

#### Status: Heartbeat
```json
{
  "matricola": "BE002",
  "online": true,
  "lastUpdate": "2025-11-21T10:40:00Z",
  "uptime": 3600,
  "versione_firmware": "1.2.3"
}
```

#### Status: Offline
```json
{
  "matricola": "BE002",
  "online": false,
  "lastUpdate": "2025-11-21T08:15:00Z",
  "motivo": "Connessione persa"
}
```

---

## Esempi di Messaggi

### Scenario 1: Avvio Corsa

**1. API pubblica comando sblocca**
```
Topic: mobishare/mezzi/BE002/comandi
Payload:
{
  "azione": "sblocca",
  "matricola": "BE002",
  "timestamp": "2025-11-21T10:30:15Z",
  "idCorsa": 42,
  "idUtente": 3
}
```

**2. Gateway conferma ricezione e avvia simulazione batteria**
```
Topic: mobishare/mezzi/BE002/telemetria
Payload:
{
  "matricola": "BE002",
  "tipo": "batteria",
  "livelloBatteria": 85,
  "stato": "InUso",
  "timestamp": "2025-11-21T10:30:20Z"
}
```

**3. Gateway invia aggiornamenti periodici batteria**
```
Topic: mobishare/mezzi/BE002/telemetria
Payload:
{
  "matricola": "BE002",
  "tipo": "batteria",
  "livelloBatteria": 83,
  "stato": "InUso",
  "timestamp": "2025-11-21T10:30:30Z"
}
```

---

### Scenario 2: Terminazione Corsa

**1. API pubblica comando blocca**
```
Topic: mobishare/mezzi/BE002/comandi
Payload:
{
  "azione": "blocca",
  "matricola": "BE002",
  "timestamp": "2025-11-21T11:45:30Z",
  "idCorsa": 42
}
```

**2. Gateway ferma simulazione batteria e conferma**
```
Topic: mobishare/mezzi/BE002/telemetria
Payload:
{
  "matricola": "BE002",
  "tipo": "batteria",
  "livelloBatteria": 62,
  "stato": "Disponibile",
  "timestamp": "2025-11-21T11:45:35Z"
}
```

---

### Scenario 3: Batteria Critica

**1. Gateway rileva batteria < 20%**
```
Topic: mobishare/mezzi/BE002/telemetria
Payload:
{
  "matricola": "BE002",
  "tipo": "batteria_critica",
  "livelloBatteria": 18,
  "stato": "NonPrelevabile",
  "motivoNonPrelevabile": "BatteriaScarica",
  "timestamp": "2025-11-21T11:20:00Z"
}
```

**2. API aggiorna DB e invia notifica SignalR al gestore**
- Database: `Mezzo.Stato = NonPrelevabile`, `Mezzo.MotivoNonPrelevabile = BatteriaScarica`
- SignalR: Notifica real-time alla dashboard gestore

---

## Quality of Service (QoS)

Il sistema utilizza i seguenti livelli QoS:

| Topic | QoS | Motivo |
|-------|-----|--------|
| `comandi` | **QoS 1** (At least once) | Garantisce che i comandi vengano consegnati almeno una volta |
| `telemetria` | **QoS 0** (At most once) | Dati telemetrici frequenti, perdita accettabile |
| `status` | **QoS 1** (At least once) | Heartbeat importante per monitoraggio |

### Livelli QoS MQTT
- **QoS 0:** Il messaggio viene inviato al massimo una volta (nessuna garanzia)
- **QoS 1:** Il messaggio viene consegnato almeno una volta (con ACK)
- **QoS 2:** Il messaggio viene consegnato esattamente una volta (handshake completo)

---

## Implementazione Codice

### Publisher (API → Gateway) - MqttIoTService.cs

```csharp
public async Task SbloccaMezzoAsync(string matricola)
{
    var topic = $"mobishare/mezzi/{matricola}/comandi";
    var payload = new
    {
        azione = "sblocca",
        matricola = matricola,
        timestamp = DateTime.UtcNow.ToString("o")
    };
    
    var message = new MqttApplicationMessageBuilder()
        .WithTopic(topic)
        .WithPayload(JsonSerializer.Serialize(payload))
        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
        .Build();
    
    await _mqttClient.EnqueueAsync(message);
}
```

### Subscriber (Gateway → API) - MqttTelemetryListenerService.cs

```csharp
private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
{
    var topic = e.ApplicationMessage.Topic;
    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
    
    if (topic.Contains("/telemetria"))
    {
        var telemetria = JsonSerializer.Deserialize<TelemetriaDTO>(payload);
        
        // Aggiorna database
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MobishareDbContext>();
        
        var mezzo = await dbContext.Mezzi
            .FirstOrDefaultAsync(m => m.Matricola == telemetria.Matricola);
        
        if (mezzo != null)
        {
            mezzo.LivelloBatteria = telemetria.LivelloBatteria;
            
            if (telemetria.LivelloBatteria < 20)
            {
                mezzo.Stato = StatoMezzo.NonPrelevabile;
                mezzo.MotivoNonPrelevabile = MotivoNonPrelevabile.BatteriaScarica;
            }
            
            await dbContext.SaveChangesAsync();
        }
    }
}
```

---

## Diagramma Flusso MQTT

```
┌─────────────┐                  ┌──────────────┐                  ┌──────────────┐
│   WebApp    │                  │  MQTT Broker │                  │ IoT Gateway  │
│ (Frontend)  │                  │  (Mosquitto) │                  │  (Emulator)  │
└──────┬──────┘                  └───────┬──────┘                  └──────┬───────┘
       │                                  │                                 │
       │ 1. POST /api/corse/inizia        │                                 │
       ├─────────────────────────────────►│                                 │
       │                                  │                                 │
       │                         2. PUBLISH mobishare/mezzi/BE002/comandi   │
       │                                  ├────────────────────────────────►│
       │                                  │   {"azione": "sblocca"}         │
       │                                  │                                 │
       │                         3. PUBLISH mobishare/mezzi/BE002/telemetria│
       │                                  │◄────────────────────────────────┤
       │                                  │   {"batteria": 85, "stato": "InUso"}
       │                                  │                                 │
       │ 4. SignalR: CorsaInizioAsync     │                                 │
       │◄─────────────────────────────────┤                                 │
       │                                  │                                 │
       │                         5. PUBLISH telemetria (ogni 10s)           │
       │                                  │◄────────────────────────────────┤
       │                                  │   {"batteria": 83, ...}         │
       │                                  │                                 │
```

---

## Note Implementative

1. **Retain Flag:** Non utilizzato (messaggi non persistiti sul broker)
2. **Clean Session:** `true` (stato sessione non salvato tra riconnessioni)
3. **Keep Alive:** 60 secondi (ping automatico ogni 60s)
4. **Reconnect Delay:** Esponenziale (1s → 2s → 4s → max 60s)
5. **Timeout:** 30 secondi per operazioni publish

### Gestione Errori
- Disconnessione broker → retry automatico con backoff esponenziale
- Messaggio non consegnato (QoS 1) → retry fino a 3 tentativi
- Payload malformato → log errore + scarta messaggio

---

## Testing MQTT

### Test con Mosquitto Client

**Subscribe a tutti i topic:**
```bash
mosquitto_sub -h localhost -p 1883 -t "mobishare/#" -v
```

**Publish comando manuale:**
```bash
mosquitto_pub -h localhost -p 1883 \
  -t "mobishare/mezzi/BE002/comandi" \
  -m '{"azione":"sblocca","matricola":"BE002","timestamp":"2025-11-21T10:30:00Z"}'
```

**Subscribe a telemetria specifica:**
```bash
mosquitto_sub -h localhost -p 1883 -t "mobishare/mezzi/+/telemetria" -v
```

---

## Riferimenti

- **MQTT Protocol:** https://mqtt.org/
- **MQTTnet Library:** https://github.com/dotnet/MQTTnet
- **Mosquitto Broker:** https://mosquitto.org/
- **QoS Levels:** https://www.hivemq.com/blog/mqtt-essentials-part-6-mqtt-quality-of-service-levels/

---

**Versione Documento:** 1.0  
**Data:** 21 Novembre 2025  
**Autore:** Mobishare Team
