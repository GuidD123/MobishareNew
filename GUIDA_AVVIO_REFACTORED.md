# 🚀 Guida Rapida Avvio Mobishare (Architettura Refactorizzata)

## ⚠️ IMPORTANTE: Ordine di Avvio

**DEVI seguire questo ordine preciso:**

1. **MQTT Broker** (Mosquitto)
2. **Mobishare.IoT.Gateway** ← 🆕 NUOVO (gestisce tutti i gateway)
3. **Mobishare.API** (backend)
4. **Mobishare.WebApp** (frontend)

---

## 📋 Step-by-Step

### **1️⃣ MQTT Broker (deve essere già attivo)**
```bash
# Verifica che Mosquitto sia in esecuzione su localhost:1883
# Su Windows: controlla nei servizi o avvia manualmente
mosquitto -v
```

---

### **2️⃣ Mobishare.IoT.Gateway** 🆕 **[NUOVO PROCESSO]**

**Terminal 1:**
```bash
cd "c:\Users\guido\OneDrive - Università del Piemonte Orientale\Desktop\PISSIR+APPWEB\Mobishare\Mobishare.IoT.Gateway"
dotnet run
```

**Output Atteso:**
```
╔══════════════════════════════════════════════════════════════╗
║         🚲 MOBISHARE IoT GATEWAY MULTI-PARKING 🛴          ║
╚══════════════════════════════════════════════════════════════╝

Avvio gateway MQTT per tutti i parcheggi attivi...

✅ Gateway IoT avviato con successo!
Premi Ctrl+C per terminare.

info: Mobishare.IoT.Gateway.Services.MqttGatewayHostedService[0]
      🚀 Avvio automatico Gateway MQTT per tutti i parcheggi attivi...
info: Mobishare.IoT.Gateway.Services.MqttGatewayManager[0]
      Avvio gateway per 5 parcheggi attivi
info: Mobishare.IoT.Gateway.Services.MqttGatewayHostedService[0]
      ✅ Gateway MQTT avviati con successo: 5 gateway attivi
```

**✅ Verifica:** Dovresti vedere "5 gateway attivi" (uno per ogni parcheggio)

---

### **3️⃣ Mobishare.API**

**Terminal 2:**
```bash
cd "c:\Users\guido\OneDrive - Università del Piemonte Orientale\Desktop\PISSIR+APPWEB\Mobishare\Mobishare.API"
dotnet run
```

**Output Atteso:**
```
info: Mobishare.Infrastructure.IoT.Services.MqttIoTService[0]
      Avvio servizio MQTT IoT...
info: Mobishare.Infrastructure.IoT.Services.MqttIoTService[0]
      Client MQTT connesso a localhost:1883
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
```

---

### **4️⃣ Mobishare.WebApp**

**Terminal 3:**
```bash
cd "c:\Users\guido\OneDrive - Università del Piemonte Orientale\Desktop\PISSIR+APPWEB\Mobishare\Mobishare.WebApp"
dotnet run
```

**Output Atteso:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7268
```

**⚠️ NOTA:** Non dovresti più vedere log di gateway nella WebApp (sono stati spostati)

---

## ✅ Verifica Architettura Corretta

### **1. Log di IoT.Gateway (Terminal 1):**
```
✅ Deve mostrare: "Gateway MQTT avviati con successo: 5 gateway attivi"
✅ Deve mostrare: "Caricamento X mezzi dal database per parcheggio Y"
```

### **2. Log di API (Terminal 2):**
```
✅ Deve mostrare: "Client MQTT connesso"
✅ Deve mostrare: "Status ricevuto per mezzo XXX"
```

### **3. Log di WebApp (Terminal 3):**
```
❌ NON deve mostrare: "Avvio gateway" o "MqttGatewayManager"
✅ Solo log di avvio applicazione ASP.NET
```

---

## 🔍 Troubleshooting

### **Problema: "Gateway non partono"**
```bash
# Controlla che MQTT Broker sia attivo
netstat -an | findstr :1883

# Verifica connection string in appsettings.json
cd Mobishare.IoT.Gateway
notepad appsettings.json
# Deve contenere: "DefaultConnection": "Data Source=../Mobishare.Core/Data/mobishare.db"
```

### **Problema: "Database non trovato"**
```bash
# Verifica che mobishare.db esista
dir "Mobishare.Core\Data\mobishare.db"
```

### **Problema: "Conflitti MQTT (messaggi duplicati)"**
```
⚠️ ASSICURATI di NON avviare più istanze di IoT.Gateway contemporaneamente!
Ogni gateway deve gestire un singolo parcheggio in modo esclusivo.
```

---

## 🎯 Test Rapido

Una volta avviati tutti i componenti:

1. Apri browser: `https://localhost:7268`
2. Login come admin
3. Vai in "Dashboard Mezzi"
4. **Verifica:** Dovresti vedere tutti i mezzi con stato/batteria aggiornati
5. **Test:** Prova a sbloccare un mezzo → verifica log in Terminal 1 (IoT.Gateway)

---

## 📊 Architettura Visuale

```
Terminal 1 (MQTT Broker):  🟢 localhost:1883
Terminal 2 (IoT.Gateway):  🟢 Gestisce 5 gateway MQTT
Terminal 3 (API):          🟢 https://localhost:7001
Terminal 4 (WebApp):       🟢 https://localhost:7268
```

---

## ⚠️ VECCHIA vs NUOVA Architettura

### **❌ PRIMA (Sbagliato):**
```
WebApp (Frontend + Gateway IoT) ← MIXED CONCERNS
API (Backend)
```

### **✅ DOPO (Corretto):**
```
IoT.Gateway (SOLO Gateway MQTT) ← Separation of Concerns
API (Backend + Business Logic)
WebApp (SOLO Frontend Razor Pages)
```

---

## 📝 Checklist Pre-Demo/Esame

- [ ] ✅ MQTT Broker attivo (localhost:1883)
- [ ] ✅ IoT.Gateway avviato e mostra "5 gateway attivi"
- [ ] ✅ API avviata e connessa a MQTT
- [ ] ✅ WebApp avviata (porta 7268)
- [ ] ✅ Philips Hue Emulator attivo (localhost:8000)
- [ ] ✅ Test sblocco/blocco mezzo funzionante
- [ ] ✅ SignalR notifiche real-time operative
- [ ] ✅ Scarico batteria durante corsa attivo

---

**Data:** 21 Novembre 2025  
**Versione:** 2.0 (Architettura Refactorizzata)  
**Note:** IoT.Gateway ora è un processo standalone separato da WebApp
