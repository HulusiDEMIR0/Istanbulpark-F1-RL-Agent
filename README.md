# IstanbulPark F1 Reinforcement Learning

This project uses Unity and ML-Agents to train an F1 car with reinforcement learning on the Istanbul Park circuit. The car reads track surfaces, obstacles, checkpoints, speed, and sliding behavior through sensors. The PPO model uses these observations to produce two main actions: throttle and steering.

## Project Goal

The goal is to make the F1 car complete laps by following checkpoints in the correct order without leaving the track or crashing into walls/barriers. Track areas are labeled with Unity tags and layers. This allows the AI to distinguish between asphalt, grass, gravel, curbs, white lines, walls, and barriers by converting them into numerical observations.

In this system, sensors tell the car: "Where am I, what is in front of me, where is the target, and how is my speed?" The reward system pushes the car toward correct behavior: passing checkpoints and completing laps are rewarded, while leaving the track or crashing is penalized.

## Main Files

| File | What It Does | Why It Exists |
| --- | --- | --- |
| `Assets/F1Agent.cs` | Main ML-Agents agent; collects observations, applies actions, manages rewards/penalties, and handles speed curriculum logic. | This is the center of the AI decision loop. |
| `Assets/F1SensorSystem.cs` | Reads front obstacles and ground surfaces using raycast sensors. | Allows the AI to understand the track and detect risks. |
| `Assets/Scripts/CarController.cs` | Controls throttle, steering, acceleration, reset behavior, and collision handling. | Converts model actions into vehicle movement. |
| `Assets/Checkpoint.cs` | Sends checkpoint information to the agent when the car enters a checkpoint trigger. | Required for checkpoint order tracking and lap rewards. |
| `Assets/CheckpointManager.cs` | Automatically organizes checkpoint names and indexes in the Unity Editor. | Prevents checkpoint ordering mistakes. |
| `Assets/MultiAgentRaceMonitor.cs` | Tracks laps, crashes, and success statistics during multi-agent training. | Makes it easier to monitor agents trained at different speeds. |
| `ProjectSettings/TagManager.asset` | Stores surface tags and layers such as `Ground`, `Obstacles`, `Agent20/35/50/65/80/95`. | Provides the labeling infrastructure needed by the sensors. |
| `config/*.yaml` | Defines PPO training settings and speed-based behaviors. | Used by ML-Agents training commands. |

## AI Observations

`F1Agent` uses a 19-dimensional vector observation. These observations include checkpoint direction, front distance sensors, surface types, wheel ground sensors, forward ground sensors, speed, forward speed, and sideways sliding information.

Surface labels are not given to the AI as text. `F1SensorSystem.SurfaceType` values are converted into numerical values inside `F1Agent.SurfaceToAI`. For example, asphalt is `0`, curb is `0.10`, white line is `0.15`, grass is `0.25`, gravel is `0.50`, and wall/barrier is `1`.

## Sensor Table

| No | Sensor / Data | Position | What It Reads | Purpose for AI | Observation Form |
| --- | --- | --- | --- | --- | --- |
| 1 | Target checkpoint direction | Car center | Local X/Z direction of the next checkpoint | Helps the car steer toward the next target | 2 numerical values |
| 2 | Front center sensor | Front of the car | Obstacle distance and surface type | Detects risks such as walls/barriers ahead | Distance + surface code |
| 3 | Front left sensor | Front-left of the car | Obstacle distance and surface type | Reads left-side risks during corners | Distance + surface code |
| 4 | Front right sensor | Front-right of the car | Obstacle distance and surface type | Reads right-side risks during corners | Distance + surface code |
| 5 | Front-left wheel ground | Front-left wheel | Surface under the wheel | Detects whether the front side is leaving the track | Surface code |
| 6 | Front-right wheel ground | Front-right wheel | Surface under the wheel | Detects asphalt/grass/gravel under the front side | Surface code |
| 7 | Rear-left wheel ground | Rear-left wheel | Surface under the wheel | Detects rear sliding or off-track behavior | Surface code |
| 8 | Rear-right wheel ground | Rear-right wheel | Surface under the wheel | Reads rear balance and track contact | Surface code |
| 9 | Center and forward ground sensors | Car center + forward left/center/right | Current and upcoming surface type | Lets the car read slightly ahead, not only underneath itself | 4 surface codes |

## Actions

The model produces two continuous actions.

| Action | Meaning |
| --- | --- |
| Steering | `-1` means turn left, `1` means turn right |
| Throttle | Model output is converted into the `0-1` range and used for forward movement |

## Reward and Penalty Logic

The agent receives a reward when it passes checkpoints in the correct order. When it passes the finish checkpoint, the lap is completed and a larger reward is given. The car also receives a small progress reward when it moves toward the next checkpoint.

Penalties increase depending on how many wheels are on grass or gravel. Sideways sliding is also penalized slightly. If the car crashes, it receives a large penalty and the episode ends. A small time penalty is applied at every step to prevent the car from wasting time.

## Training Structure

Training is done with PPO. The main settings are stored in `config/ppo_istanbulpark.yaml`. For multi-speed training, `config/ppo_istanbulpark - Kopya.yaml` defines the behaviors `IstanbulPark_20`, `IstanbulPark_35`, `IstanbulPark_50`, `IstanbulPark_65`, `IstanbulPark_80`, and `IstanbulPark_95`.

In the scene, each speed has a separate agent/behavior. As speed increases, the front sensor length and forward ground sensor length are also increased. This is because a faster car needs earlier information to make decisions safely.

Saved models are stored in the `Assets/Model` folder.

## Note

The `Istanbulpark.glb` file could not be uploaded because it exceeds GitHub's 100 MB file size limit. If you need the file, feel free to contact me via email and I can share it with you.
Email : 05.hdemir@gmail.com

TR   

# IstanbulPark F1 Reinforcement Learning

Bu proje, Unity ve ML-Agents ile Istanbul Park pisti uzerinde bir F1 aracinin pekistirmeli ogrenme kullanarak surus yapmasini hedefler. Arac; pist yuzeylerini, engelleri, checkpointleri, hizini ve kayma durumunu sensorler ile okur. PPO modeli bu verileri kullanarak iki temel karar uretir: gaz ve direksiyon.

## Projenin Amaci

Amac, F1 aracinin pist disina cikmadan ve duvar/bariyerlere carpmadan checkpointleri dogru sirayla takip ederek tur tamamlamasidir. Pist uzerindeki alanlar tag ve layer sistemiyle etiketlenmistir. Bu sayede AI; asfalt, cim, cakil, curb, beyaz cizgi, duvar ve bariyer gibi farkli durumlari sayisal gozleme donusturerek karar verebilir.

Bu sistemde sensorler araca "neredeyim, onumde ne var, hedef nerede, hizim nasil" bilgisini verir. Odul sistemi ise araci dogru davranisa iter: checkpoint gecmek ve tur tamamlamak odul, pist disina cikmak veya carpmak ceza olarak kullanilir.

## Ana Dosyalar

| Dosya | Ne Yapar | Neden Var |
| --- | --- | --- |
| `Assets/F1Agent.cs` | ML-Agents ajanidir; gozlem toplar, aksiyon uygular, odul/ceza ve hiz curriculum mantigini yonetir. | AI karar dongusunun merkezi burasidir. |
| `Assets/F1SensorSystem.cs` | Raycast sensorleriyle on engelleri ve zemin yuzeylerini okur. | AI'nin pisti ve tehlikeleri algilamasini saglar. |
| `Assets/Scripts/CarController.cs` | Gaz, direksiyon, hizlanma, reset ve carpisma davranisini kontrol eder. | Model kararlarini fiziksel arac hareketine cevirir. |
| `Assets/Checkpoint.cs` | Arac checkpoint triggerina girdiginde ajana checkpoint bilgisini yollar. | Sira takibi ve tur tamamlama odulleri icin gereklidir. |
| `Assets/CheckpointManager.cs` | Editor icinde checkpoint isimlerini ve indexlerini otomatik duzenler. | Checkpoint siralamasinin karismasini onler. |
| `Assets/MultiAgentRaceMonitor.cs` | Coklu ajan egitiminde tur, kaza ve basari istatistiklerini izler. | Farkli hizlardaki ajanlari takip etmeyi kolaylastirir. |
| `ProjectSettings/TagManager.asset` | Yuzey taglerini ve `Ground`, `Obstacles`, `Agent20/35/50/65/80/95` layerlarini tutar. | Sensorlerin dogru seyi okumasini saglayan etiket altyapisidir. |
| `config/*.yaml` | PPO egitim ayarlarini ve hiz bazli davranislari tanimlar. | ML-Agents egitim komutlari bu ayarlari kullanir. |

## AI Gozlemleri

`F1Agent` toplam 19 boyutlu vektor gozlem kullanir. Bu gozlemler checkpoint yonu, on mesafe sensorleri, yuzey tipleri, teker zeminleri, ileri zemin sensorleri, hiz, ileri hiz ve yanal kayma bilgisinden olusur.

Yuzeyler AI'ya dogrudan metin olarak verilmez. `F1SensorSystem.SurfaceType` degerleri `F1Agent.SurfaceToAI` icinde sayisal koda cevrilir. Ornek olarak asfalt `0`, curb `0.10`, beyaz cizgi `0.15`, cim `0.25`, cakil `0.50`, duvar/bariyer `1` olarak kullanilir.

## Sensor Tablosu

| No | Sensor / Veri | Konum | Ne Okur | AI Icin Amaci | Gozlem Sekli |
| --- | --- | --- | --- | --- | --- |
| 1 | Hedef checkpoint yonu | Arac merkezi | Sonraki checkpointin lokal X/Z yonu | Aracin siradaki hedefe donmesini saglar | 2 sayisal deger |
| 2 | On merkez sensoru | Aracin onu | Engel mesafesi ve yuzey tipi | Onde duvar/bariyer gibi riskleri erken gormek | Mesafe + yuzey kodu |
| 3 | On sol sensoru | Aracin on solu | Engel mesafesi ve yuzey tipi | Sol viraj ve sol kenar risklerini okumak | Mesafe + yuzey kodu |
| 4 | On sag sensoru | Aracin on sagi | Engel mesafesi ve yuzey tipi | Sag viraj ve sag kenar risklerini okumak | Mesafe + yuzey kodu |
| 5 | Sol on teker zemini | Sol on teker | Teker altindaki yuzey | On tarafin pist disina tasip tasmadigini anlamak | Yuzey kodu |
| 6 | Sag on teker zemini | Sag on teker | Teker altindaki yuzey | On tarafin asfalt/cim/cakil durumunu anlamak | Yuzey kodu |
| 7 | Sol arka teker zemini | Sol arka teker | Teker altindaki yuzey | Arka tarafin kayma veya pist disi durumunu yakalamak | Yuzey kodu |
| 8 | Sag arka teker zemini | Sag arka teker | Teker altindaki yuzey | Aracin arka dengesini ve pistte kalma durumunu okumak | Yuzey kodu |
| 9 | Merkez ve ileri zeminler | Arac merkezi + ileri sol/merkez/sag | Mevcut ve yaklasan zemin tipi | Aracin sadece altini degil, biraz ilerisini de okuyarak erken karar vermesi | 4 yuzey kodu |

## Aksiyonlar

Model iki surekli aksiyon uretir.

| Aksiyon | Anlami |
| --- | --- |
| Direksiyon | `-1` sola, `1` saga donus |
| Gaz | Model cikisi `0-1` araligina cevrilir ve ileri hareket icin kullanilir |

## Odul ve Ceza Mantigi

Ajan checkpointleri dogru sirayla gectiginde odul alir. Finish checkpointi gecilince tur tamamlanir ve daha buyuk odul verilir. Arac siradaki checkpointe dogru hizlandiginda kucuk ilerleme odulu kazanir.

Cim ve cakil uzerindeki teker sayisi arttikca ceza artar. Yanal kayma kucuk ceza ile azaltilir. Kaza olursa ajan buyuk ceza alir ve episode biter. Her adimda verilen kucuk zaman cezasi, aracin gereksiz oyalanmasini engellemek icindir.

## Egitim Yapisi

Egitim PPO ile yapilir. Temel ayarlar `config/ppo_istanbulpark.yaml` icindedir. Coklu hiz egitimi icin `config/ppo_istanbulpark - Kopya.yaml` dosyasi `IstanbulPark_20`, `IstanbulPark_35`, `IstanbulPark_50`, `IstanbulPark_65`, `IstanbulPark_80` ve `IstanbulPark_95` davranislarini tanimlar.

Sahnede her hiz icin ayri ajan/behavior bulunur. Hiz arttikca on sensor ve ileri zemin sensor mesafeleri de buyutulmustur. Bunun nedeni, hizli giden aracin daha erken bilgiye ihtiyac duymasidir.

Kayitli modeller `Assets/Model` klasorinde tutulur. 

## Not 
İstanbulpark.glb dosyası 100Mb sınırını aştığı için yüklenememiştir.İsteyen kişilere mail üzerinden paylaşabilirim bana yazmanız yeterlidir. 
Mail : 05.hdemir@gmail.com

