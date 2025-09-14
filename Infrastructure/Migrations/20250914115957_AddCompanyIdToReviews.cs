using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdToReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Run only on MySQL (Pomelo)
            if (!migrationBuilder.ActiveProvider.Contains("MySql"))
            {
                migrationBuilder.Sql("-- This migration is for MySQL (Pomelo). No-op for other providers.");
                return;
            }

            // 1) Add column company_id if it does NOT exist yet
            migrationBuilder.Sql(@"
                SET @col := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'reviews'
                      AND COLUMN_NAME = 'company_id'
                );
                SET @sql := IF(@col = 0,
                    'ALTER TABLE `reviews` ADD COLUMN `company_id` varchar(255) NULL;',
                    'DO 0');
                PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
            ");

            // 2) Drop the existing FK on order_id (if any) to allow altering the column to NULL
            migrationBuilder.Sql(@"
                SET @fkname := (
                    SELECT CONSTRAINT_NAME
                    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                    WHERE CONSTRAINT_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'reviews'
                      AND COLUMN_NAME = 'order_id'
                      AND REFERENCED_TABLE_NAME = 'orders'
                    LIMIT 1
                );
                SET @sql := IF(@fkname IS NOT NULL,
                    CONCAT('ALTER TABLE `reviews` DROP FOREIGN KEY `', @fkname, '`;'),
                    'DO 0');
                PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
            ");

            // 3) Ensure order_id allows NULL (only alter when it is currently NOT NULL)
            migrationBuilder.Sql(@"
                SET @need := (
                    SELECT CASE
                             WHEN EXISTS (
                               SELECT 1
                               FROM INFORMATION_SCHEMA.COLUMNS
                               WHERE TABLE_SCHEMA = DATABASE()
                                 AND TABLE_NAME = 'reviews'
                                 AND COLUMN_NAME = 'order_id'
                                 AND IS_NULLABLE = 'YES'
                             ) THEN 0
                             ELSE 1
                           END
                );
                SET @sql := IF(@need = 1,
                    'ALTER TABLE `reviews` MODIFY COLUMN `order_id` varchar(255) NULL;',
                    'DO 0');
                PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
            ");

            // 4) Backfill company_id from orders for reviews linked to real orders
            migrationBuilder.Sql(@"
                UPDATE `reviews` r
                JOIN `orders` o ON r.`order_id` = o.`id`
                SET r.`company_id` = o.`company_id`
                WHERE r.`company_id` IS NULL;
            ");

            // 5) Backfill company_id from order_id of the form manual:{companyId}:{guid}
            migrationBuilder.Sql(@"
                UPDATE `reviews`
                SET `company_id` = SUBSTRING(`order_id`, 8, 24)
                WHERE `company_id` IS NULL AND `order_id` LIKE 'manual:%:%';
            ");

            // 6) Delete dirty rows that still lack company_id (if any)
            migrationBuilder.Sql(@"
                DELETE FROM `reviews`
                WHERE `company_id` IS NULL;
            ");

            // 7) Make company_id NOT NULL (only if it is currently NULLABLE)
            migrationBuilder.Sql(@"
                SET @need := (
                    SELECT CASE
                             WHEN EXISTS (
                               SELECT 1
                               FROM INFORMATION_SCHEMA.COLUMNS
                               WHERE TABLE_SCHEMA = DATABASE()
                                 AND TABLE_NAME = 'reviews'
                                 AND COLUMN_NAME = 'company_id'
                                 AND IS_NULLABLE = 'NO'
                             ) THEN 0
                             ELSE 1
                           END
                );
                SET @sql := IF(@need = 1,
                    'ALTER TABLE `reviews` MODIFY COLUMN `company_id` varchar(255) NOT NULL;',
                    'DO 0');
                PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
            ");

            // 8a) Create an index for company_id if there is NO index on this column
            migrationBuilder.Sql(@"
                SET @ix := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME   = 'reviews'
                      AND COLUMN_NAME  = 'company_id'
                );
                SET @sql := IF(@ix = 0,
                    'CREATE INDEX `IX_reviews_company_id` ON `reviews` (`company_id`);',
                    'DO 0');
                PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
            ");

            // 8b) Create an index for order_id if there is NO index on this column
            migrationBuilder.Sql(@"
                SET @ix := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME   = 'reviews'
                      AND COLUMN_NAME  = 'order_id'
                );
                SET @sql := IF(@ix = 0,
                    'CREATE INDEX `IX_reviews_order_id` ON `reviews` (`order_id`);',
                    'DO 0');
                PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
            ");

            // 9a) Add FK company_id -> companies(id) ON DELETE CASCADE if it does NOT exist
            migrationBuilder.Sql(@"
                SET @fk := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                    WHERE CONSTRAINT_SCHEMA     = DATABASE()
                      AND TABLE_NAME            = 'reviews'
                      AND COLUMN_NAME           = 'company_id'
                      AND REFERENCED_TABLE_NAME = 'companies'
                      AND REFERENCED_COLUMN_NAME= 'id'
                );
                SET @sql := IF(@fk = 0,
                    'ALTER TABLE `reviews`
                       ADD CONSTRAINT `FK_reviews_companies_company_id`
                       FOREIGN KEY (`company_id`) REFERENCES `companies`(`id`) ON DELETE CASCADE;',
                    'DO 0');
                PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
            ");

            // 9b) Add FK order_id -> orders(id) ON DELETE SET NULL if it does NOT exist
            migrationBuilder.Sql(@"
                SET @fk := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                    WHERE CONSTRAINT_SCHEMA     = DATABASE()
                      AND TABLE_NAME            = 'reviews'
                      AND COLUMN_NAME           = 'order_id'
                      AND REFERENCED_TABLE_NAME = 'orders'
                      AND REFERENCED_COLUMN_NAME= 'id'
                );
                SET @sql := IF(@fk = 0,
                    'ALTER TABLE `reviews`
                       ADD CONSTRAINT `FK_reviews_orders_order_id`
                       FOREIGN KEY (`order_id`) REFERENCES `orders`(`id`) ON DELETE SET NULL;',
                    'DO 0');
                PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
            ");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
          // 1) Remove FK first (MySQL/PG/SQLServer are all ok)
          migrationBuilder.DropForeignKey(
            name: "FK_reviews_companies_company_id",
            table: "reviews");

          migrationBuilder.DropForeignKey(
            name: "FK_reviews_orders_order_id",
            table: "reviews");

          // 2) Remove indexes created in Up
          migrationBuilder.DropIndex(
            name: "IX_reviews_company_id",
            table: "reviews");

          migrationBuilder.DropIndex(
            name: "IX_reviews_order_id",
            table: "reviews");

          // 3) Before converting order_id back to NOT NULL, ensure there are no NULLs
          if (migrationBuilder.ActiveProvider.Contains("Pomelo") ||               // MySQL (Pomelo)
              migrationBuilder.ActiveProvider.Contains("MySql"))
          {
            migrationBuilder.Sql(@"
              UPDATE reviews
              SET order_id = CONCAT('manual:', company_id, ':reverted')
              WHERE order_id IS NULL;
            ");
          }
          else if (migrationBuilder.ActiveProvider.Contains("Npgsql"))            // PostgreSQL
          {
            migrationBuilder.Sql(@"
              UPDATE reviews
              SET order_id = 'manual:' || company_id || ':reverted'
              WHERE order_id IS NULL;
            ");
          }
          else if (migrationBuilder.ActiveProvider.Contains("SqlServer"))         // SQL Server
          {
            migrationBuilder.Sql(@"
              UPDATE reviews
              SET order_id = 'manual:' + company_id + ':reverted'
              WHERE order_id IS NULL;
            ");
          }
          else
          {
            // Fallback - try to use standard SQL
            migrationBuilder.Sql(@"
              UPDATE reviews
              SET order_id = 'manual:' || company_id || ':reverted'
              WHERE order_id IS NULL;
            ");
          }

          // 4) Convert order_id back to NOT NULL (same column type as in Up; MySQL: varchar(255))
          migrationBuilder.AlterColumn<string>(
            name: "order_id",
            table: "reviews",
            type: "varchar(255)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(255)",
            oldNullable: true);

          // 5) Remove company_id (FK & index were removed above)
          migrationBuilder.DropColumn(
            name: "company_id",
            table: "reviews");
        }

    }
}
