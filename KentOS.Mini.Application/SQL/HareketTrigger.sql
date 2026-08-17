using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KentOS.Mini.Web.Migrations
{
    /// <inheritdoc />
    public partial class TriggersMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION randevu_birim_change_trigger()
                RETURNS TRIGGER AS
                $$
                DECLARE
                    v_username VARCHAR(100);
                    v_eski_ad VARCHAR(100);
                    v_eski_id BIGINT;
                    v_eski_yetkili VARCHAR(100);
                    v_eski_level INT;

                    v_yeni_ad VARCHAR(100);
                    v_yeni_id BIGINT;
                    v_yeni_yetkili VARCHAR(100);
                    v_yeni_level INT;

                    v_kullanici_id BIGINT;
                    v_kullanici_ad VARCHAR(100);
                    v_asagi_hareket BOOLEAN := false;
                BEGIN
                    -- Get Birim names
                    SELECT id, ad, yetkili, level INTO v_yeni_id, v_yeni_ad, v_yeni_yetkili, v_yeni_level FROM birimler WHERE id = NEW.birim_id;
                    SELECT id, ad, yetkili, level INTO v_eski_id, v_eski_ad, v_eski_yetkili, v_eski_level FROM birimler WHERE id = OLD.birim_id;
                    -- Get current user info
                    SELECT id, user_name 
                    INTO v_kullanici_id, v_kullanici_ad
                    FROM ""AspNetUsers""
                    WHERE user_name = NEW.guncelleyen;

                    IF v_yeni_level > v_eski_level THEN 
                        v_asagi_hareket = true; 
                    END IF;

                    -- Insert movement record
                    INSERT INTO randevu_hareketler(
                        kullanici_id,
                        kullanici,
                        yeni_birim_id,
                        yeni_birim,
                        eski_birim_id,
                        eski_birim,
                        tarih,
                        randevu_id,
                        asagi_hareket
                    )
                    VALUES(
                        v_kullanici_id,
                        v_kullanici_ad,
                        NEW.birim_id,
                        CONCAT(v_yeni_ad, ' - ', v_yeni_yetkili),
                        OLD.birim_id,
                        CONCAT(v_eski_ad, ' - ', v_eski_yetkili),
                        CURRENT_TIMESTAMP,
                        NEW.id,
                        v_asagi_hareket
                    );

                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            migrationBuilder.Sql(@"
                -- Create trigger
                CREATE OR REPLACE TRIGGER randevu_birim_change
                AFTER UPDATE OF birim_id ON randevular
                FOR EACH ROW
                WHEN (OLD.birim_id IS DISTINCT FROM NEW.birim_id)
                EXECUTE FUNCTION randevu_birim_change_trigger();
            ");

            migrationBuilder.Sql(@"

                CREATE OR REPLACE FUNCTION update_randevu_on_ajanda_insert()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF NEW.randevu_id IS NOT NULL AND NEW.randevu_id <> 0 THEN
                        UPDATE randevular
                        SET ajanda_id = NEW.id,
                            ajanda_durum = TRUE
                        WHERE id = NEW.randevu_id;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

            ");

            migrationBuilder.Sql(@"
               CREATE OR REPLACE TRIGGER trigger_update_randevu_on_ajanda_insert
                AFTER INSERT ON ajandalar
                FOR EACH ROW
                EXECUTE FUNCTION update_randevu_on_ajanda_insert();
            ");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_randevu_on_ajanda_delete()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF OLD.randevu_id IS NOT NULL AND OLD.randevu_id <> 0 THEN
                        UPDATE randevular
                        SET ajanda_id = 0,
                            ajanda_durum = FALSE
                        WHERE id = OLD.randevu_id;
                    END IF;
                    RETURN OLD;
                END;
                $$ LANGUAGE plpgsql;


            ");

            migrationBuilder.Sql(@"
                -- Create the trigger
                CREATE TRIGGER trigger_update_randevu_on_ajanda_delete
                AFTER DELETE ON ajandalar
                FOR EACH ROW
                EXECUTE FUNCTION update_randevu_on_ajanda_delete();
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trigger_update_randevu_on_ajanda_delete ON ajandalar;
                DROP FUNCTION IF EXISTS update_randevu_on_ajanda_delete();
            ");

            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trigger_update_randevu_on_ajanda_insert ON ajandalar;
                DROP FUNCTION IF EXISTS update_randevu_on_ajanda_insert();
            ");

            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS randevu_birim_change ON randevular;
                DROP FUNCTION IF EXISTS randevu_birim_change_trigger();
            ");
        }
    }
}
