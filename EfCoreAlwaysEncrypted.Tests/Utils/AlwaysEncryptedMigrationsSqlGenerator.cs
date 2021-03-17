using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EfCoreAlwaysEncrypted.Tests.Utils
{
    public class AlwaysEncryptedMigrationsSqlGenerator : SqlServerMigrationsSqlGenerator
    {
        public AlwaysEncryptedMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies, IRelationalAnnotationProvider migrationsAnnotations) : base(dependencies, migrationsAnnotations)
        {
        }

        protected override void ColumnDefinition(AddColumnOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            if (operation.Name != "Id")
            {
                var collation = operation.ClrType == typeof(string) ? "COLLATE Latin1_General_BIN2" : string.Empty;
                var encryption =
                    $"ENCRYPTED WITH(ENCRYPTION_TYPE = DETERMINISTIC, ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256', COLUMN_ENCRYPTION_KEY = {DatabaseFixture.ColumnEncryptionKeyName})";
                var column = $"[{operation.Name}] {operation.ColumnType} {collation} {encryption} {(operation.IsNullable ? "NULL" : "NOT NULL")}";
                builder.Append(column);
            }
            else
            {
                base.ColumnDefinition(operation, model, builder);
            }
        }
    }
}
