# YMH219 Chat Uygulaması

C# ve Windows Forms ile TCP tabanlı istemci-sunucu sohbet uygulaması.

## Öğrenci

Özgür Ulusoy — 250541083

## Teknolojiler

.NET 10, Windows Forms, TcpListener ve TcpClient. Harici NuGet paketi kullanılmaz.

## Çalıştırma

Windows ve .NET 10 SDK gereklidir. Çözüm dosyasını Visual Studio ile açın veya depo klasöründe çalıştırın:

```powershell
dotnet run --project ChatApp
```

## Mevcut özellikler

- Port seçimi ve sunucuyu başlatma/durdurma.
- Kullanıcı adıyla istemci bağlantısı ve bağlantıyı kesme.
- Sunucu ve istemciler arasında iki yönlü mesajlaşma.
- Birden fazla istemciye mesaj dağıtımı.
- Mesajları gönderen adı ve saat bilgisiyle listeleme.
- Sunucuda bağlı kullanıcı adlarını ve bağlantı adreslerini gösterme.
- Bağlantısı kopan kullanıcıları listeden çıkarma.
- Port kullanım, bağlantı ve mesaj hatalarını gösterme.
- Yeniden bağlanma ve kapanışta bağlantıları temizleme.

## Kullanım

1. Sunucu penceresinde portu seçip **Sunucuyu Başlat** düğmesine basın.
2. **İstemci Aç** ile bir istemci penceresi açın.
3. Adresi `127.0.0.1`, portu sunucudaki değer olarak bırakın. Kullanıcı adını girip **Bağlan** düğmesine basın.
4. Alt alana mesaj yazıp **Gönder** düğmesine veya Enter'a basın. Sunucu da aynı şekilde mesaj gönderebilir.
5. İkinci bir istemci açarak aynı odaya katılabilirsiniz.
6. **Bağlantıyı Kes** istemciyi ayırır. Sunucudaki **Durdur** tüm bağlantıları kapatır.

İstemciyi ayrı bir uygulama işlemi olarak açmak için ikinci terminalde:

```powershell
dotnet run --project ChatApp -- --client
```

## Sınıflar

`ChatServer` TCP bağlantılarını ve mesaj dağıtımını, `ChatClient` istemci bağlantısını yönetir. `Wire` uzunluk başlığı ve JSON içeriği olan paketleri okur/yazar. `ChatMessage` mesaj verisini taşır. `ServerForm` ve `ClientForm`, ortak `ChatWindow` arayüzünü kullanır.

## Kontroller

```powershell
dotnet build ChatApp -c Release
dotnet run --project ChatApp.Checks -c Release
```

Gerçek yerel TCP bağlantılarıyla 16 otomatik kontrol uygulanır: kullanıcı adı doğrulama, sunucu başlatma, dolu port, iki istemci, Türkçe karakterler, çift yönlü mesajlar, zaman bilgisi, boş/uzun mesaj reddi, eş zamanlı gönderim, ayrılma, yeniden bağlanma, bozuk paket, sunucu kapanışı, ulaşılamayan sunucu ve yeniden başlatma.

## Ekran görüntüleri

Ekran görüntüleri henüz eklenmemiştir.

## Sınırlamalar

Sunucu yalnızca aynı bilgisayarda `127.0.0.1` üzerinde dinler. Kullanıcı adı en fazla 24, mesaj en fazla 2000 karakterdir. Aynı kullanıcı adı birden fazla bağlantıda kullanılabilir. Her pencere son 1000 mesaj/olayı bellekte tutar; geçmiş dosyaya kaydedilmez. Kullanıcı hesabı, parola, TLS ve özel mesaj desteği yoktur. Sunucu penceresinden açılan istemci pencereleri sunucu uygulaması kapatıldığında kapanır; ayrı başlatılan istemciler bağlantı kesildi bilgisini gösterir.
