
# DbMetaTool

Narzędzie do budowy, eksportu i aktualizacji baz danych Firebird 5.0.

---

## Wymagania

- .NET 8.0  
- Firebird 5.0  
- IBExpert (opcjonalnie do przeglądania bazy)

---

## Struktura katalogów

DbMetaTool/  
├─ scripts/ # skrypty do tworzenia bazy (domeny, tabele, procedury)  
├─ uscripts/ # skrypty do aktualizacji bazy  
├─ db/ # katalog z plikiem .fdb  
├─ outscripts/ # katalog, do którego eksportowane są skrypty  

---

## Sposób użycia

Program obsługuje trzy polecenia:

### 1. Budowa nowej bazy

```bash
DbMetaTool build-db --db-dir "<ścieżka_do_db>" --scripts-dir "<ścieżka_do_skryptów>"
```
Tworzy nową bazę Firebird 5.0 w katalogu <db-dir>

Wykonuje skrypty z katalogu <scripts-dir> w kolejności: domains.sql, tables.sql, procedures.sql

### 2. Eksport metadanych
```bash
DbMetaTool export-scripts --connection-string "<connection_string>" --output-dir "<ścieżka_do_wyjścia>"
```
Pobiera strukturę bazy (domeny, tabele, procedury) i zapisuje w <output-dir>

Generuje pliki: domains.sql, tables.sql, procedures.sql

### 3. Aktualizacja istniejącej bazy
```bash
DbMetaTool update-db --connection-string "<connection_string>" --scripts-dir "<ścieżka_do_skryptów>"
```

Wykonuje skrypty aktualizacyjne z katalogu <scripts-dir>

Zachowuje kolejność: domains.sql, tables.sql, procedures.sql

Wszystkie zmiany wykonywane są w ramach transakcji

### Testowanie w Visual Studio
##### 1. Otwórz projekt w Visual Studio
##### 2. W zakładce Debug utwórz trzy profile uruchamiania:

		 Build Database → wywołanie polecenia build-db

		Export Scripts → wywołanie polecenia export-scripts

		Update Database → wywołanie polecenia update-db

##### 3. Ustaw odpowiednie ścieżki do bazy i katalogów skryptów

##### 4. Uruchom wybrany profil DbMetaTool
