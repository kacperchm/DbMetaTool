using FirebirdSql.Data.FirebirdClient;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DbMetaTool
{
    public static class Program
    {
        // Przykładowe wywołania:
        // DbMetaTool build-db --db-dir "C:\db\fb5" --scripts-dir "C:\scripts"
        // DbMetaTool export-scripts --connection-string "..." --output-dir "C:\out"
        // DbMetaTool update-db --connection-string "..." --scripts-dir "C:\scripts"
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Użycie:");
                Console.WriteLine("  build-db --db-dir <ścieżka> --scripts-dir <ścieżka>");
                Console.WriteLine("  export-scripts --connection-string <connStr> --output-dir <ścieżka>");
                Console.WriteLine("  update-db --connection-string <connStr> --scripts-dir <ścieżka>");
                return 1;
            }

            try
            {
                var command = args[0].ToLowerInvariant();

                switch (command)
                {
                    case "build-db":
                        {
                            string dbDir = GetArgValue(args, "--db-dir");
                            string scriptsDir = GetArgValue(args, "--scripts-dir");

                            BuildDatabase(dbDir, scriptsDir);
                            Console.WriteLine("Baza danych została zbudowana pomyślnie.");
                            return 0;
                        }

                    case "export-scripts":
                        {
                            string connStr = GetArgValue(args, "--connection-string");
                            string outputDir = GetArgValue(args, "--output-dir");

                            ExportScripts(connStr, outputDir);
                            Console.WriteLine("Skrypty zostały wyeksportowane pomyślnie.");
                            return 0;
                        }

                    case "update-db":
                        {
                            string connStr = GetArgValue(args, "--connection-string");
                            string scriptsDir = GetArgValue(args, "--scripts-dir");

                            UpdateDatabase(connStr, scriptsDir);
                            Console.WriteLine("Baza danych została zaktualizowana pomyślnie.");
                            return 0;
                        }

                    default:
                        Console.WriteLine($"Nieznane polecenie: {command}");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Błąd: " + ex.Message);
                return -1;
            }
        }

        private static string GetArgValue(string[] args, string name)
        {
            int idx = Array.IndexOf(args, name);
            if (idx == -1 || idx + 1 >= args.Length)
                throw new ArgumentException($"Brak wymaganego parametru {name}");
            return args[idx + 1];
        }

        /// <summary>
        /// Buduje nową bazę danych Firebird 5.0 na podstawie skryptów.
        /// </summary>
        public static void BuildDatabase(string databaseDirectory, string scriptsDirectory)
        {
            // TODO:
            // 1) Utwórz pustą bazę danych FB 5.0 w katalogu databaseDirectory.
            // 2) Wczytaj i wykonaj kolejno skrypty z katalogu scriptsDirectory
            //    (tylko domeny, tabele, procedury).
            // 3) Obsłuż błędy i wyświetl raport.

            Directory.CreateDirectory(databaseDirectory);

            var dbPath = Path.Combine(databaseDirectory, "database.fdb");

            var errors = new List<string>();

            var connectionString =
                $"User=SYSDBA;" +
                $"Password=masterkey;" +
                $"Database={dbPath};" +
                $"DataSource=localhost;" +
                $"Port=3050;" +
                $"Dialect=3;" +
                $"Charset=UTF8;" +
                $"ServerType=0;";

            if (File.Exists(dbPath))
                File.Delete(dbPath);

            // 1️⃣ Utworzenie pustej bazy
            FbConnection.CreateDatabase(connectionString);

            // 2️⃣ Wykonanie skryptów
            using var conn = new FbConnection(connectionString);
            conn.Open();

            var order = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["domains.sql"] = 1,
                ["tables.sql"] = 2,
                ["procedures.sql"] = 3
            };

            var scripts = Directory
                .GetFiles(scriptsDirectory, "*.sql")
                .OrderBy(f => order.TryGetValue(Path.GetFileName(f), out var o) ? o : int.MaxValue)
                .ThenBy(f => f);

            foreach (var script in scripts)
            {
                try
                {
                    var sql = File.ReadAllText(script);

                    foreach (var statement in SplitSqlStatements(sql, Path.GetFileName(script)))
                    {
                        if (string.IsNullOrWhiteSpace(statement)) continue;
                        using var cmd = new FbCommand(statement, conn);
                        cmd.ExecuteNonQuery();
                    }

                    Console.WriteLine($"OK: {Path.GetFileName(script)}");
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(script)} -> {ex.Message}");
                }
            }

            // 3️⃣ Raport
            if (errors.Count > 0)
            {
                Console.WriteLine("Błędy:");
                foreach (var e in errors)
                    Console.WriteLine(" - " + e);
            }
        }

        /// <summary>
        /// Generuje skrypty metadanych z istniejącej bazy danych Firebird 5.0.
        /// </summary>
        public static void ExportScripts(string connectionString, string outputDirectory)
        {
            // TODO:
            // 1) Połącz się z bazą danych przy użyciu connectionString.
            // 2) Pobierz metadane domen, tabel (z kolumnami) i procedur.
            // 3) Wygeneruj pliki .sql / .json / .txt w outputDirectory.



            Directory.CreateDirectory(outputDirectory);

            using var conn = new FbConnection(connectionString);
            conn.Open();

            ExtractDomains(conn, outputDirectory);
            ExtractTables(conn, outputDirectory);
            ExtractProcedures(conn, outputDirectory);
        }

        /// <summary>
        /// Aktualizuje istniejącą bazę danych Firebird 5.0 na podstawie skryptów.
        /// </summary>
        public static void UpdateDatabase(string connectionString, string scriptsDirectory)
        {
            // TODO:
            // 1) Połącz się z bazą danych przy użyciu connectionString.
            // 2) Wykonaj skrypty z katalogu scriptsDirectory (tylko obsługiwane elementy).
            // 3) Zadbaj o poprawną kolejność i bezpieczeństwo zmian.

            var errors = new List<string>();

            using var conn = new FbConnection(connectionString);
            conn.Open();

            var order = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["domains.sql"] = 1,
                ["tables.sql"] = 2,
                ["procedures.sql"] = 3
            };

            var scripts = Directory
                .GetFiles(scriptsDirectory, "*.sql")
                .OrderBy(f => order.TryGetValue(Path.GetFileName(f), out var o) ? o : int.MaxValue)
                .ThenBy(f => f);

            foreach (var script in scripts)
            {
                try
                {
                    var sql = File.ReadAllText(script);
                    using var tx = conn.BeginTransaction();

                    foreach (var statement in SplitSqlStatements(sql, Path.GetFileName(script)))
                    {
                        if (string.IsNullOrWhiteSpace(statement)) continue;
                        using var cmd = new FbCommand(statement, conn, tx);
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();

                    Console.WriteLine($"OK: {Path.GetFileName(script)}");
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(script)} -> {ex.Message}");
                }
            }

            if (errors.Count > 0)
            {
                Console.WriteLine("Błędy aktualizacji:");
                foreach (var e in errors)
                    Console.WriteLine(" - " + e);
            }

        }

        private static IEnumerable<string> SplitSqlStatements(string sql, string scriptType)
        {
            var statements = new List<string>();
            var sb = new StringBuilder();
            var lines = sql.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var prevLine = "";

            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("--")) continue;

                sb.AppendLine(line);

                if (scriptType.Equals("procedures.sql")) {
                    if (line.TrimEnd().EndsWith(";"))
                    {
                        if(prevLine.TrimEnd().Equals("END", StringComparison.OrdinalIgnoreCase)) 
                        {
                            statements.Add(sb.ToString());
                            sb.Clear();
                        }
                        
                    } 
                    else if(line.TrimEnd().EndsWith("END;"))
                    {
                        statements.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else
                {
                    if (line.TrimEnd().EndsWith(";"))
                    {
                        statements.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                
                prevLine = line;
            }

            if (sb.Length > 0)
                statements.Add(sb.ToString());

            return statements;
        }

        static void ExtractDomains(FbConnection conn, string outputDirectory)
        {
            var sb = new StringBuilder();
            string sql = @"
            SELECT
                f.rdb$field_name,
                f.rdb$field_type,
                f.rdb$field_length,
                f.rdb$field_precision,
                f.rdb$field_scale,
                cs.rdb$character_set_name
            FROM rdb$fields f
            LEFT JOIN rdb$character_sets cs
                   ON f.rdb$character_set_id = cs.rdb$character_set_id
            WHERE f.rdb$system_flag = 0
            ORDER BY f.rdb$field_name";

            using var cmd = new FbCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string name = reader.GetString(0).Trim();
                short type = reader.GetInt16(1);
                short rawLength = reader.GetInt16(2);
                short precision = reader.IsDBNull(3) ? (short)0 : reader.GetInt16(3);
                short scale = reader.IsDBNull(4) ? (short)0 : reader.GetInt16(4);

                string charset = reader.IsDBNull(5)
                    ? "NONE"
                    : reader.GetString(5).Trim();

                short length = rawLength;

                if (charset == "UTF8")
                    length = (short)(rawLength / 4);

                bool isText = type == 37 || type == 14;
                int bytesPerChar = charset == "UTF8" ? 4 : 1;
                int byteSize = length * bytesPerChar;

                string fbType;

                if (isText && byteSize > 32767)
                {
                    fbType = "BLOB SUB_TYPE TEXT";
                }
                else
                {
                    fbType = FirebirdType(type, length, precision, scale);
                }
                if (name.StartsWith("RDB$")) continue;
                sb.AppendLine($"CREATE DOMAIN {name} AS {fbType};");
            }

            sb.AppendLine();
            File.WriteAllText(Path.Combine(outputDirectory, "domains.sql"), sb.ToString());
        }

        static string FirebirdType(short type, short length, short precision, short scale)
        {
            return type switch
            {
                7 => "SMALLINT",
                8 => "INTEGER",
                9 => "QUAD",
                10 => "FLOAT",
                12 => "DATE",
                13 => "TIME",
                14 => $"CHAR({length})",
                16 => precision > 0 ? $"NUMERIC({precision},{-scale})" : "BIGINT",
                27 => "DOUBLE PRECISION",
                35 => "TIMESTAMP",
                37 => $"VARCHAR({length})",
                261 => "BLOB",
                _ => $"UNKNOWN_TYPE({type})"
            };
        }

        static void ExtractTables(FbConnection conn, string outputDirectory)
        {
            var sb = new StringBuilder();

            string sqlTables = @"
        SELECT rdb$relation_name
        FROM rdb$relations
        WHERE rdb$system_flag = 0
          AND rdb$view_source IS NULL
        ORDER BY rdb$relation_name";

            using var cmdTables = new FbCommand(sqlTables, conn);
            using var readerTables = cmdTables.ExecuteReader();

            while (readerTables.Read())
            {
                string tableName = readerTables.GetString(0).Trim();
                sb.AppendLine($"CREATE TABLE {tableName} (");

                var columnLines = new List<string>();

                string sqlCols = @"
                SELECT
                    rf.rdb$field_name,
                    f.rdb$field_type,
                    f.rdb$field_length,
                    f.rdb$field_precision,
                    f.rdb$field_scale,
                    rf.rdb$null_flag,
                    rf.rdb$default_source,
                    rf.rdb$field_source,
                    rf.rdb$identity_type,
                    cs.rdb$character_set_name
                FROM rdb$relation_fields rf
                JOIN rdb$fields f ON rf.rdb$field_source = f.rdb$field_name
                LEFT JOIN rdb$character_sets cs
                       ON f.rdb$character_set_id = cs.rdb$character_set_id
                WHERE rf.rdb$relation_name = @table
                ORDER BY rf.rdb$field_position";

                using var cmdCols = new FbCommand(sqlCols, conn);
                cmdCols.Parameters.AddWithValue("@table", tableName);

                using var readerCols = cmdCols.ExecuteReader();

                while (readerCols.Read())
                {
                    string colName = readerCols.GetString(0).Trim();

                    short type = readerCols.GetInt16(1);
                    short rawLength = readerCols.GetInt16(2);
                    string charset = readerCols.IsDBNull(9)
                        ? "NONE"
                        : readerCols.GetString(9).Trim();

                    short length = rawLength;

                    if (charset == "UTF8")
                        length = (short)(rawLength / 4);
                    short precision = readerCols.IsDBNull(3) ? (short)0 : readerCols.GetInt16(3);
                    short scale = readerCols.IsDBNull(4) ? (short)0 : readerCols.GetInt16(4);
                    bool notNull = !readerCols.IsDBNull(5);
                    string defaultSrc = readerCols.IsDBNull(6) ? null : readerCols.GetString(6).Trim();
                    string domain = readerCols.GetString(7).Trim();
                    bool isIdentity = !readerCols.IsDBNull(8);

                    var line = new StringBuilder($"    {colName} ");

                    if (!domain.StartsWith("RDB$"))
                        line.Append(domain);
                    else
                        line.Append(FirebirdType(type, length, precision, scale));

                    if (isIdentity)
                        line.Append(" GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY");

                    if (defaultSrc != null && !isIdentity)
                        line.Append($" {defaultSrc}");

                    if (notNull && !isIdentity)
                        line.Append(" NOT NULL");

                    columnLines.Add(line.ToString());
                }

                string sqlFK = @"
            SELECT
                sg.rdb$field_name,
                rel2.rdb$relation_name,
                sg2.rdb$field_name
            FROM rdb$relation_constraints rc
            JOIN rdb$ref_constraints ref ON rc.rdb$constraint_name = ref.rdb$constraint_name
            JOIN rdb$relation_constraints rel2 ON ref.rdb$const_name_uq = rel2.rdb$constraint_name
            JOIN rdb$index_segments sg ON rc.rdb$index_name = sg.rdb$index_name
            JOIN rdb$index_segments sg2 ON rel2.rdb$index_name = sg2.rdb$index_name
            WHERE rc.rdb$constraint_type = 'FOREIGN KEY'
              AND rc.rdb$relation_name = @table";

                using var cmdFk = new FbCommand(sqlFK, conn);
                cmdFk.Parameters.AddWithValue("@table", tableName);

                using var fkReader = cmdFk.ExecuteReader();

                while (fkReader.Read())
                {
                    string col = fkReader.GetString(0).Trim();
                    string refTable = fkReader.GetString(1).Trim();
                    string refCol = fkReader.GetString(2).Trim();

                    columnLines.Add(
                        $"    FOREIGN KEY ({col}) REFERENCES {refTable}({refCol})");
                }

                sb.AppendLine(string.Join(",\n", columnLines));
                sb.AppendLine(");");
                sb.AppendLine();
            }

            File.WriteAllText(
                Path.Combine(outputDirectory, "tables.sql"),
                sb.ToString(),
                Encoding.UTF8);
        }

        static void ExtractProcedures(FbConnection conn, string outputDirectory)
        {
            var sb = new StringBuilder();
            string sql = @"
            SELECT rdb$procedure_name, rdb$procedure_source
            FROM rdb$procedures
            ORDER BY rdb$procedure_name";

            using var cmd = new FbCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string name = reader.GetString(0).Trim();
                string source = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();

                if (!String.IsNullOrEmpty(source))
                {
                    var inputs = GetProcedureParams(conn, name, 0);
                    var outputs = GetProcedureParams(conn, name, 1);

                    sb.AppendLine($"CREATE OR ALTER PROCEDURE {name}");

                    if (inputs.Count > 0)
                        sb.AppendLine($"({string.Join(", ", inputs)})");

                    if (outputs.Count > 0)
                        sb.AppendLine("RETURNS (" + string.Join(", ", outputs) + ")");

                    sb.AppendLine("AS");
                    sb.AppendLine(source);
                    sb.AppendLine(";");
                    sb.AppendLine();
                }       
            }

            File.WriteAllText(Path.Combine(outputDirectory, "procedures.sql"), sb.ToString());
        }

        static List<string> GetProcedureParams(FbConnection conn, string proc, short paramType)
        {
            const string sql = @"
            SELECT
                pp.rdb$parameter_name,
                f.rdb$field_type,
                f.rdb$character_length,
                f.rdb$field_precision,
                f.rdb$field_scale
            FROM rdb$procedure_parameters pp
            JOIN rdb$fields f ON pp.rdb$field_source = f.rdb$field_name
            WHERE pp.rdb$procedure_name = @p
              AND pp.rdb$parameter_type = @t
            ORDER BY pp.rdb$parameter_number";

            using var cmd = new FbCommand(sql, conn);
            cmd.Parameters.AddWithValue("@p", proc);
            cmd.Parameters.AddWithValue("@t", paramType);

            using var r = cmd.ExecuteReader();

            var list = new List<string>();

            while (r.Read())
            {
                string name = r[0].ToString().Trim();
                short type = Convert.ToInt16(r[1]);
                short length = r[2] == DBNull.Value ? (short)0 : Convert.ToInt16(r[2]);
                short precision = r[3] == DBNull.Value ? (short)0 : Convert.ToInt16(r[3]);
                short scale = r[4] == DBNull.Value ? (short)0 : Convert.ToInt16(r[4]);

                string fbType = FirebirdType(type, length, precision, scale);

                list.Add($"{name} {fbType}");
            }
            return list;
        }
    }
}
