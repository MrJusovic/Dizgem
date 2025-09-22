namespace Dizgem
{
    // 1) Bir provider tanımı
    public interface IConnectionStringProvider
    {
        string Current { get; }
        void Set(string cs); // installer sonrası güncellemek için
    }

    public class ConnectionStringProvider : IConnectionStringProvider
    {
        private string _cs;
        public ConnectionStringProvider(IConfiguration cfg)
        {
            // appsettings.json’daki varsa onu al; yoksa boş
            _cs = cfg.GetConnectionString("DefaultConnection") ?? "";
        }
        public string Current => _cs;
        public void Set(string cs) => _cs = cs;
    }
}
