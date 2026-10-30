# hotkeyd

Bir tuş birleşimine bir iş bağla. `Ctrl+Alt+N`'e bastığında Not Defteri açılsın;
zaten açıksa öne gelsin. Windows için, WPF ile yazılmış, buzlu cam görünümlü
sade bir uygulama.

![hotkeyd](docs/hotkeyd.png)

Görseldeki üç satır programın gerçek bir çalışmasından: biri işletim sistemine
kaydedilmiş ve çalışıyor (**etkin**), biri aynı birleşimi istediği için
**çakışma** veriyor, biri de yazımı hatalı olduğu için **geçersiz**.

## Verdiği söz

Bir kısayol listede **etkin** görünüyorsa, onu işletim sistemi gerçekten kabul
etmiştir. Program "kaydettim" demekle yetinmez; her kısayol için `RegisterHotKey`
çağrısının sonucuna bakar ve yalnızca Windows onayladığında yeşil yakar. Başka
bir uygulama o birleşimi önceden kapmışsa, o satır kırmızıya döner ve nedenini
yazar. Sessizce yarım çalışan bir kısayol yoktur.

Çakışmalar da işletim sistemine hiç dokunmadan, önceden çözülür. Açık iki kısayol
aynı birleşimi isterse, listede önce gelen birleşimi alır; geri kalanlar
**çakışma** olarak işaretlenir ve kaydedilmez. Hangi kısayolun sahibi olduğu
açıkça yazıldığı için, hatanın kaynağını tahmin etmek zorunda kalmazsın.

## Yalnızca senin bağladığın tuşları dinler

hotkeyd bir tuş kaydedici (keylogger) **değildir**; tasarımı gereği olamaz da.
İşletim sisteminin kendi kısayol düzeneğini (`RegisterHotKey`) kullanır: Windows'a
hangi birleşimleri izleyeceğini bildirir, Windows da sadece tam olarak o
birleşimlerden biri basıldığında tek bir bildirim gönderir. Program klavyeye
kanca (hook) takmaz; bağlamadığın hiçbir tuşu görmez ve hiçbir yere yazmaz.

Yönetici hakkı da istemez. Kısayollar sıradan bir kullanıcı işlemiyle kaydedilir;
oturum açılışında başlatma seçeneği de yalnızca o kullanıcıya ait
`HKCU\...\Run` anahtarına yazar.

## Neler yapabilir

- **Program başlat / öne getir** — kapalıysa başlatır, açıksa penceresini öne alır.
- **Dosya veya bağlantı aç** — varsayılan uygulamayla açar.
- **Komut çalıştır** — bir komut satırı işletir.

Her kısayolu tek tek açıp kapatabilirsin. Üstteki **Tümü** anahtarı hepsini bir
anda duraklatır; istediğinde elle geri açarsın. **Girişte başlat** işaretliyse
bilgisayar her açıldığında arka planda (`--tray`) sessizce devreye girer.

Tuş birleşimini kutuya yazmazsın; kutuya tıklayıp birleşime basarsın. Böylece
kaydedilen şey tam olarak bastığın şey olur.

## Nasıl çalıştırılır

```bash
dotnet run --project src/hotkeyd.App
```

Dağıtılabilir sürüm:

```bash
dotnet publish src/hotkeyd.App -c Release
```

Ayarlar `%APPDATA%\hotkeyd\config.json` dosyasında tutulur. Dosyayı elle de
düzenleyebilirsin; bozuk bir dosya sessizce silinmez, uyarı verilir ve program
yine de açılır; zaten düzeltebilmen için.

## Çekirdek neden ölçülebilir

Karar veren her şey — tuş çözümleme, çakışma bulma, hangi kısayolun
kaydedileceği — `hotkeyd.Core` içinde saf koddur. Ne pencereye ne klavyeye
ihtiyaç duyar; aynı girdi her zaman aynı planı üretir. Bu yüzden klavye
olmadan baştan sona sınanabilir.

```bash
dotnet test        # 22 test
```

- **Çözümleyici** kabul edilen her tuş adını tek tek dener ve her reddi adıyla
  bildirir: boş giriş, tuşsuz birleşim, tekrar eden değiştirici, iki tuş,
  tanınmayan sözcük.
- **Plan** şunları doğrular: birleşimi ilk isteyen sahiplenir, kalanlar çakışır;
  kapalı bir kısayol birleşimi işgal etmez; geçersiz bir satır, sonraki geçerli
  satırın kaydını engellemez.
- **Ayar deposu** her alanı yazıp geri okur ve bozuk bir dosyanın, kayıtları yok
  etmeden hata verdiğini doğrular.

İşletim sistemine dokunan kısım (`RegisterHotKey`, pencereyi öne getirme)
uygulama katmanındadır; çekirdek bunu hiç bilmez.

## Lisans

MIT.
