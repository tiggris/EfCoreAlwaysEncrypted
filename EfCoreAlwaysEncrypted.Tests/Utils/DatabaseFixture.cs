using Azure.Identity;
using EfCoreAlwaysEncrypted.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EfCoreAlwaysEncrypted.Tests.Utils
{
    public class DatabaseFixture : IDisposable
    {
        private static readonly DbContextOptionsBuilder<DataContext> DbContextOptionsBuilder;
        private const string SqlServerConnectionString = @"Server=(localdb)\mssqllocaldb";
        private const string DatabaseName = "AlwaysEncryptedDb";
        public const string MasterEncryptionKeyName = "CMKAuto1";
        public const string ColumnEncryptionKeyName = "CEKAuto1";
        private readonly string _keyVaultUrl;
        private readonly string _keyVaultKeyName;
        private readonly string _keyVaultKeyVersion;
        private readonly SqlColumnEncryptionKeyStoreProvider _keyStoreProvider;

        static DatabaseFixture()
        {
            DbContextOptionsBuilder = new DbContextOptionsBuilder<DataContext>();
            DbContextOptionsBuilder.UseSqlServer(DatabaseConnectionString);
            DbContextOptionsBuilder.ReplaceService<IMigrationsSqlGenerator, AlwaysEncryptedMigrationsSqlGenerator>();
        }
        
        public DatabaseFixture()
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<DatabaseFixture>()
                .Build();

            // Please provide configuration to key vault key store in user secrets:
            //{
            //    "KeyVault:Url": "[key vault url]",
            //    "KeyVault:KeyName": "[key name]",
            //    "KeyVault:KeyVersion": "[key version]"
            //}
            _keyVaultUrl = configuration["KeyVault:Url"];
            _keyVaultKeyName = configuration["KeyVault:KeyName"];
            _keyVaultKeyVersion = configuration["KeyVault:KeyVersion"];

            _keyStoreProvider = new SqlColumnEncryptionAzureKeyVaultProvider(new DefaultAzureCredential(
                new DefaultAzureCredentialOptions { ExcludeSharedTokenCacheCredential = true }));

            SqlConnection.RegisterColumnEncryptionKeyStoreProviders(
                new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>
                {
                    { SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, _keyStoreProvider }
                });

            CreateDatabase();
            CreateEncryptionKeys();
            InitializeDatabase();
        }

        public static string DatabaseConnectionString =>
            $"{SqlServerConnectionString};Database={DatabaseName};Column Encryption Setting=enabled";

        public static DbContextOptions<DataContext> DbContextOptions => DbContextOptionsBuilder.Options;

        public void Dispose()
        {
           DropDatabase();
        }

        private void CreateDatabase()
        {
            using var connection = new SqlConnection(SqlServerConnectionString);
            connection.Open();
            connection.ExecuteRawSql($"CREATE DATABASE {DatabaseName}");
            connection.Close();
        }
        
        private void DropDatabase()
        {
            using var connection = new SqlConnection(DatabaseConnectionString);
            connection.Open();
            SqlConnection.ClearPool(connection);
            connection.ChangeDatabase("master");
            connection.ExecuteRawSql($"DROP DATABASE {DatabaseName}");
            connection.Close();
        }

        private void CreateEncryptionKeys()
        {
            using var connection = new SqlConnection(DatabaseConnectionString);
            connection.Open();
            connection.ExecuteRawSql(CreateColumnMasterKeySql);
            connection.ExecuteRawSql(CreateColumnEncryptionKeySql);
            connection.Close();
        }

        private void InitializeDatabase()
        {
            using var dataContext = new DataContext(DbContextOptions);
            dataContext.Database.EnsureCreated();
        }

        private string CreateColumnMasterKeySql
        {
            get
            {
                const string keyStoreProviderName = SqlColumnEncryptionAzureKeyVaultProvider.ProviderName;
                var keyVaultUrl = $"{_keyVaultUrl}/keys/{_keyVaultKeyName}/{_keyVaultKeyVersion}";

                return
                    $@"CREATE COLUMN MASTER KEY [{ MasterEncryptionKeyName }]
                    WITH (
                        KEY_STORE_PROVIDER_NAME = N'{keyStoreProviderName}',
                        KEY_PATH = N'{keyVaultUrl}'
                    );";
            }
        }

        private string CreateColumnEncryptionKeySql
        {
            get
            {
                // Generate the raw bytes that will be used as a key by using a CSPRNG 
                var keyId = $"{_keyVaultUrl}/keys/{_keyVaultKeyName}/{_keyVaultKeyVersion}";
                var cekRawValue = new byte[32];
                var provider = new RNGCryptoServiceProvider();
                provider.GetBytes(cekRawValue);

                var cekEncryptedValue = _keyStoreProvider.EncryptColumnEncryptionKey(keyId, @"RSA_OAEP", cekRawValue);

                return @$" CREATE COLUMN ENCRYPTION KEY [{ColumnEncryptionKeyName}] 
                WITH VALUES 
                ( 
                    COLUMN_MASTER_KEY = [{MasterEncryptionKeyName}], 
                    ALGORITHM = 'RSA_OAEP', 
                    ENCRYPTED_VALUE = {BytesToHex(cekEncryptedValue)} 
                );";
            }
        }

        private string BytesToHex(byte[] a)
        {
            var temp = BitConverter.ToString(a);
            var len = a.Length;

            // We need to remove the dashes that come from the BitConverter
            var sb = new StringBuilder((len - 2) / 2); // This should be the final size

            foreach (var t in temp.Where(t => t != '-'))
                sb.Append(t);

            return "0x" + sb;
        }
    }
}
