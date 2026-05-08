DATA INVENTORY — current samples

Attached sample files:
- FAC_EOD_NODO_MONEDA_20240226 (1).csv — CSV with columns: fec_dia, ide_nodo, cod_moneda, SOD, cash_in, cash_out, ship_in, ship_out, EOD

Inferred schema (draft):
- `fec_dia` -> date (format dd/MM/yyyy)
- `ide_nodo` -> string (node identifier)
- `cod_moneda` -> string (currency code)
- `SOD`, `cash_in`, `cash_out`, `ship_in`, `ship_out`, `EOD` -> decimal (nullable)

Notes:
- Date format uses day/month/year — parser must accept locale formats.
- Missing values appear as empty cells — parser must coerce to null.
- Numeric fields may include decimal point and thousands are not present in sample.
- Additional formats (JSON, XML, fixed-width) are expected but samples are pending.
