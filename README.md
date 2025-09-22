# Dizgem CMS
Dizgem, .NET 8 mimarisi üzerine kurulu, açık kaynak kodlu, esnek ve geliştirici dostu bir içerik yönetim sistemi (CMS) çekirdeğidir.

🚀 Genel Bakış
Dizgem, WordPress gibi global CMS platformlarına C# tabanlı bir alternatif sunmayı hedefler. Sadece HTML bilgisiyle bile özel temalar geliştirmenize olanak tanıyarak, sıfırdan bir CMS projesi oluşturma zahmetinden kurtarır. Proje, hem Windows (IIS) hem de Linux (Nginx) sunucularda çalışacak şekilde tasarlanmıştır.

✨ Ana Özellikler
Yazı (Post) ve Sayfa (Page) Yönetimi

Kategori ve Etiket Sistemi

Dinamik Tema Altyapısı

Esnek Menü Yöneticisi

Otomatik ve Manuel SEO Yönetimi

Modern Blok Tabanlı İçerik Editörü

🏁 Başlarken
Gereksinimler
.NET 8 SDK

MSSQL Server

IIS veya Nginx gibi bir web sunucusu

Kurulum
Projeyi sunucunuza yükleyin.

Siteyi tarayıcıda açtığınızda otomatik olarak /Install sayfasına yönlendirileceksiniz.

Kurulum sihirbazındaki adımları (Veritabanı, Site ve Yönetici Bilgileri) takip ederek kurulumu tamamlayın.

Kurulum tamamlandığında "Default" tema aktif hale gelir ve içerik oluşturmaya başlayabilirsiniz.

🎨 Tema Geliştirme
Dizgem'in en güçlü yanlarından biri, kolayca özel temalar geliştirebilmenizdir. Her tema /Themes klasörü altında kendi klasöründe yer alır.

Temel bir tema yapısı şunları içerir:

/assets klasörü (CSS, JS, resimler)

/Home, /Post, /Page, /Shared klasörleri (Razor dosyaları)

theme.json (Tema ayarları ve menü konumları)

screenshot.png (Tema önizleme resmi)

Tema içerisinde verilere erişmek ve menüleri, SEO etiketlerini render etmek için detaylı rehberimize göz atın.

➡️ Detaylı bilgi ve kod örnekleri için [Kullanıcı Dokümantasyonu](https://dizgem.org) sayfasını ziyaret edin.

🤝 Katkıda Bulunma
Projeye katkıda bulunmak isterseniz, pull request'lerinizi ve issue'larınızı heyecanla bekliyoruz! Lütfen katkıda bulunmadan önce projenin genel yapısını ve hedeflerini inceleyin.

📜 Lisans
Bu proje MIT Lisansı altında lisanslanmıştır. Detaylar için LICENSE dosyasına bakınız. Projeyi kullanırken veya geliştirirken orijinal çalışmaya bir credit vermeniz yeterlidir.

Dizgem CMS
Dizgem is an open-source, flexible, and developer-friendly Content Management System (CMS) core built on the .NET 8 architecture.

🚀 Overview
Dizgem aims to provide a C#-based alternative to global CMS platforms like WordPress. It saves you from the hassle of creating a CMS project from scratch by allowing you to develop custom themes with just HTML knowledge. The project is designed to run on both Windows (IIS) and Linux (Nginx) servers.

✨ Main Features
Post and Page Management

Category and Tag System

Dynamic Theming Engine

Flexible Menu Manager

Automatic and Manual SEO Management

Modern Block-Based Content Editor

🏁 Getting Started
Requirements
.NET 8 SDK

MSSQL Server

A web server like IIS or Nginx

Installation
Upload the project files to your server.

When you open the site in your browser, you will be automatically redirected to the /Install page.

Follow the steps in the installation wizard (Database, Site, and Administrator Information) to complete the setup.

Once the installation is complete, the "Default" theme becomes active, and you can start creating content.

🎨 Theme Development
One of Dizgem's greatest strengths is the ability to easily develop custom themes. Each theme resides in its own folder under the /Themes directory.

A basic theme structure includes:

/assets folder (for CSS, JS, and images)

/Home, /Post, /Page, /Shared folders (for Razor files)

theme.json (for theme settings and menu locations)

screenshot.png (for the theme preview image)

To access data and render menus and SEO tags within your theme, please refer to our detailed guide.

➡️ For detailed information and code examples, visit the User [Documentation](https://dizgem.org).

🤝 Contributing
If you'd like to contribute to the project, we eagerly await your pull requests and issues! Please review the general structure and goals of the project before contributing.

📜 License
This project is licensed under the MIT License. See the LICENSE file for details. It is sufficient to provide a credit to the original work when using or developing the project.
