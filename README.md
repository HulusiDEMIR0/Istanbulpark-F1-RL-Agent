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

Kayitli modeller `Assets/Model` ve `results` klasorlerinde tutulur. `results/IstanbulPark_v2/IstanbulParkAgent/speed_curriculum_state.json` dosyasinda hiz curriculum durumunun tamamlandigi gorunur.

