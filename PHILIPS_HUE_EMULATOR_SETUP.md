# 🔆 Philips Hue Emulator - Guida Setup

## 📋 Panoramica
Questa guida spiega come usare l'**Hue Emulator** per testare l'integrazione Philips Hue in Mobishare senza hardware fisico.

---

## 🚀 SEQUENZA DI AVVIO COMPLETA

### **STEP 1: Avvia Hue Emulator** (Primo componente)
```bash
java -jar HueEmulator-v0.8.jar
```
1. Imposta porta: **8000**
2. Click su **Start**
3. Aggiungi almeno 3 luci (File → Add Light)
4. **Stato iniziale**: Luci spente o colore default

### **STEP 2: Avvia Backend API**
```bash
cd Mobishare.API
dotnet run
```
**Console log atteso:**
```
✅ Philips Hue configurato: http://localhost:8000/api/newdeveloper/
🔄 Avvio sincronizzazione iniziale Philips Hue...
Trovati 5 mezzi. Sincronizzazione luci in corso...
💡 Sync: Luce BIKE001 → Verde (Stato: Disponibile)
💡 Sync: Luce BIKE002 → Blu (Stato: InUso)
💡 Sync: Luce MONO001 → Rosso (Stato: NonPrelevabile)
✅ Sincronizzazione Philips Hue completata: 3 successi, 0 fallimenti
```

**Cosa succede:**
- ✅ Il backend si connette all'emulatore
- ✅ Legge tutti i mezzi dal DB
- ✅ **Sincronizza le luci con lo stato attuale** dei mezzi
- ✅ Ogni mezzo ottiene il colore corretto:
  - Disponibile → 🟢 VERDE
  - InUso → 🔵 BLU
  - NonPrelevabile → 🔴 ROSSO
  - Manutenzione → 🟡 GIALLO

### **STEP 3: Avvia Gateway IoT/MQTT** ⚠️ **OBBLIGATORIO**
```bash
cd Mobishare.IoT.Gateway
dotnet run
```
**Il Gateway è NECESSARIO per:**
- ✅ Pubblicare comandi MQTT di sblocco/blocco mezzi
- ✅ Monitorare telemetria in tempo reale (batteria, GPS, stato)
- ✅ Gestire comunicazione bidirezionale con i mezzi fisici

**Senza Gateway: sblocco/blocco mezzi NON funzioneranno**

### **STEP 4: Avvia WebApp**
```bash
cd Mobishare.WebApp
dotnet run
```
Interfaccia utente pronta su https://localhost:7268

---

## 💡 **COMPORTAMENTO LUCI ALL'AVVIO**

### **Scenario A: DB Vuoto (Prima Installazione)**
```
1. Hue Emulator: Luci spente o colore default
2. Backend avvio: "Nessun mezzo trovato nel DB. Skip sincronizzazione."
3. Risultato: Luci rimangono invariate
4. Al primo mezzo creato → Luce si accende con colore corretto
```

### **Scenario B: DB Popolato (Sistema già in uso)** ✅
```
1. Hue Emulator: Luci spente
2. Backend legge DB:
   - BIKE001: Disponibile → Luce VERDE
   - BIKE002: InUso (corsa in corso) → Luce BLU
   - MONO001: NonPrelevabile (batteria 15%) → Luce ROSSA
3. PhilipsHueSyncService sincronizza tutte le luci
4. Risultato: Sistema completamente allineato DB ↔ Luci
```

### **STEP 5: Durante l'Uso (Event-Driven)**
Dopo la sincronizzazione iniziale, le luci cambiano solo quando succede un **evento reale**:

| Evento | Trigger | Luce Before | Luce After |
|--------|---------|-------------|------------|
| Utente inizia corsa | POST /api/corse/inizia | 🟢 VERDE | 🔵 BLU |
| Utente finisce corsa | PUT /api/corse/{id} | 🔵 BLU | 🟢 VERDE |
| Batteria scende <20% | POST /api/mezzi/mqtt-batteria | 🟢 VERDE | 🔴 ROSSA |
| Admin ricarica mezzo | PUT /api/mezzi/{id}/ricarica | 🔴 ROSSA | 🟢 VERDE |
| Utente segnala guasto | PUT /api/mezzi/matricola/{m}/segnala-guasto | 🟢 VERDE | 🔴 ROSSA |
| Admin mette in manutenzione | PUT /api/mezzi/{id}/stato | 🟢 VERDE | 🟡 GIALLA |

---

## 🚀 STEP 1: Download e Installazione

### Download Emulatore
- **Versione più recente**: [HueEmulator-v0.8.jar](https://github.com/SteveyO/Hue-Emulator/raw/master/HueEmulator-v0.8.jar)
- **Repository GitHub**: https://github.com/SteveyO/Hue-Emulator

### Requisiti
- **Java Runtime Environment (JRE)** installato
- Verifica con: `java -version`

---

## ⚙️ STEP 2: Avvio dell'Emulatore

### Comando di avvio
```bash
java -jar HueEmulator-v0.8.jar
```

### Configurazione Iniziale
1. **Porta**: Imposta `8000` (deve corrispondere a `appsettings.json`)
2. Click su **Start** per avviare il server HTTP
3. L'emulatore creerà alcune luci di default

---

## 🔍 STEP 3: Verifica Connessione

### Test Base URL
```bash
curl http://localhost:8000/api/newdeveloper
```

**Output atteso**: JSON con configurazione del bridge
```json
{
  "lights": {
    "1": { "name": "Hue Lamp 1", "state": {...}, ... },
    "2": { "name": "Hue Lamp 2", "state": {...}, ... }
  },
  "config": {...},
  "groups": {...}
}
```

### Test Cambio Stato Luce
```bash
curl -X PUT http://localhost:8000/api/newdeveloper/lights/1/state -H "Content-Type: application/json" -d "{\"on\":true,\"hue\":25500,\"sat\":254,\"bri\":254}"
```

**Output atteso**: `[{"success":{...}}]`

---

## 💡 STEP 4: Configurazione Luci

### Aggiungere Luci nell'Emulatore
1. **Menu** → **File** → **Add Light**
2. Scegli un **ID** per la luce (es: 1, 2, 3, ...)
3. (Opzionale) Assegna un nome descrittivo

### Strategia di Mapping

#### Opzione A: ID Numerici (consigliata per test rapidi)
```
Luce ID 1 → BIKE001
Luce ID 2 → BIKE002
Luce ID 3 → MONO001
```

**Configurazione in `PhilipsHueControl.cs`:**
```csharp
private readonly Dictionary<string, string> _matricolaToLightId = new()
{
    { "BIKE001", "1" },
    { "BIKE002", "2" },
    { "MONO001", "3" }
};
```

#### Opzione B: Usa Matricole come Light ID
Crea luci con ID = matricola direttamente:
```
Luce ID "BIKE001"
Luce ID "BIKE002"
Luce ID "MONO001"
```

**Configurazione in `PhilipsHueControl.cs`:**
```csharp
// Lascia il dizionario vuoto
private readonly Dictionary<string, string> _matricolaToLightId = new();
```

---

## 🎨 STEP 5: Test Integrazione con Mobishare

### 1. Verifica Configurazione
**File**: `appsettings.json`
```json
"Hue": {
  "BaseUrl": "http://localhost:8000/api/newdeveloper/",
  "Host": "localhost",
  "Port": "8000",
  "Username": "newdeveloper"
}
```

### 2. Avvia Mobishare API
```bash
dotnet run --project Mobishare.API
```

**Output console atteso:**
```
✅ Philips Hue configurato: http://localhost:8000/api/newdeveloper/
```

### 3. Test Scenario Completo

#### Scenario: Inizio Corsa → Luce BLU
```bash
POST /api/corse/inizia
{
  "matricolaMezzo": "BIKE001",
  "idParcheggioPrelievo": 1
}
```

**Risultato atteso:**
- ✅ Corsa avviata nel DB
- ✅ MQTT pubblica comando sblocco
- 💡 **Luce nell'emulatore diventa BLU**
- 📋 Log: `💡 Philips Hue: Luce BIKE001 cambiata a BLU (InUso)`

#### Scenario: Fine Corsa → Luce VERDE
```bash
PUT /api/corse/{id}
{
  "dataOraFineCorsa": "2025-11-21T15:30:00",
  "idParcheggioRilascio": 2
}
```

**Risultato atteso:**
- ✅ Corsa terminata, pagamento processato
- ✅ MQTT pubblica comando blocco
- 💡 **Luce nell'emulatore diventa VERDE**
- 📋 Log: `💡 Philips Hue: Luce BIKE001 cambiata a VERDE (fine corsa, stato: Disponibile)`

---

## 🛠️ STEP 6: Debugging

### Log da Monitorare
```
💡 Philips Hue: Luce {Matricola} cambiata a {Colore}
⚠️ Impossibile controllare Philips Hue per mezzo {Matricola}
```

### Problemi Comuni

#### Emulatore non risponde
**Causa**: Emulatore non avviato o porta sbagliata
**Soluzione**: 
1. Verifica che l'emulatore sia in esecuzione
2. Controlla che la porta sia `8000`
3. Test: `curl http://localhost:8000/api/newdeveloper`

#### Luce non cambia colore
**Causa**: Light ID non trovato nell'emulatore
**Soluzione**:
1. Verifica mapping in `PhilipsHueControl.cs`
2. Controlla che la luce esista: `curl http://localhost:8000/api/newdeveloper/lights`
3. Guarda i log per vedere quale Light ID viene usato

#### Timeout HTTP
**Causa**: Emulatore lento o non risponde
**Soluzione**:
1. Aumenta timeout in `Program.cs`: `client.Timeout = TimeSpan.FromSeconds(10);`
2. Riavvia l'emulatore

---

## 📊 Mapping Colori

| Colore Mobishare | Hue Value | Saturazione | Brightness | Stato Mezzo |
|------------------|-----------|-------------|------------|-------------|
| 🔴 **ROSSO** | 0 | 254 | 254 | NonPrelevabile |
| 🟢 **VERDE** | 25500 | 254 | 254 | Disponibile |
| 🔵 **BLU** | 46920 | 254 | 254 | InUso |
| 🟡 **GIALLO** | 12750 | 254 | 254 | Manutenzione |
| ⚫ **SPENTA** | - | - | - | Offline |

---

## 🎯 Scenari di Test Completi

### Test 1: Ciclo Vita Corsa
```
1. Mezzo Disponibile → Luce VERDE
2. Utente avvia corsa → Luce BLU
3. Utente termina corsa → Luce VERDE
```

### Test 2: Batteria Scarica
```
1. Mezzo al 30% batteria → Luce VERDE (disponibile)
2. Batteria scende a 19% → Luce ROSSA (non prelevabile)
3. Admin ricarica al 100% → Luce VERDE (disponibile)
```

### Test 3: Guasto Segnalato
```
1. Utente segnala guasto → Luce ROSSA
2. Admin cambia stato in Manutenzione → Luce GIALLA
3. Admin cambia stato in Disponibile → Luce VERDE
```

---

## 🔐 Sicurezza

- ✅ **Fail-safe**: Se l'emulatore è offline, l'app continua a funzionare
- ✅ **Try-catch**: Ogni chiamata Hue è protetta da exception handling
- ✅ **Non bloccante**: Operazioni critiche (DB, pagamenti) completate prima di Hue
- ✅ **Timeout**: 5 secondi massimo per evitare attese infinite

---

## 📚 Risorse Utili

- **Documentazione Philips Hue API**: https://developers.meethue.com/
- **Hue Emulator GitHub**: https://github.com/SteveyO/Hue-Emulator
- **Hue Emulator Download**: https://github.com/SteveyO/Hue-Emulator/raw/master/HueEmulator-v0.8.jar

---

## ✅ Checklist Pre-Test

- [ ] Java Runtime installato (`java -version`)
- [ ] HueEmulator-v0.8.jar scaricato
- [ ] Emulatore avviato su porta 8000
- [ ] Test connessione OK: `curl http://localhost:8000/api/newdeveloper`
- [ ] Luci create nell'emulatore (almeno 3)
- [ ] Mapping configurato in `PhilipsHueControl.cs` (se necessario)
- [ ] `appsettings.json` configurato con `Hue:BaseUrl`
- [ ] Mobishare.API avviato e log conferma connessione Hue

---

**Buon testing! 🚀💡**
