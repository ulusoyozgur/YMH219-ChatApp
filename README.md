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
- Aynı bilgisayardan TCP bağlantısı kabul etme.
- Bağlantı olaylarını saat bilgisiyle listeleme.
- Port kullanım hatasını gösterme ve kapanışta bağlantıları kapatma.

## Geliştirme durumu

Sunucu altyapısı tamamlanmıştır. Kullanıcı adıyla istemci bağlantısı, mesaj gönderme/alma, istemci listesi ve iki ekran görüntüsü sonraki aşamada eklenecektir. Bu sürüm henüz mesajlaşma sağlamaz.

## Sınırlamalar

Sunucu yalnızca 127.0.0.1 üzerinde dinler. Bağlanan TCP istemcileri durdurma işlemine kadar tutulur; bağlantı kopma takibi henüz yoktur. Olay listesi son 1000 kaydı gösterir.
