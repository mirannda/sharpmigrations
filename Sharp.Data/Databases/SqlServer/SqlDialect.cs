using System;
using System.Text;
using System.Data;
using Sharp.Data.Util;
using System.Collections.Generic;
using System.Globalization;
using Sharp.Data.Schema;
using Sharp.Util;

namespace Sharp.Data.Dialects {
    public class SqlDialect : Dialect {

		public override string ParameterPrefix {
            get {
                return "@";
            }
        }

        public override DbType GetDbType(string sqlType, int dataPrecision) {
            throw new NotImplementedException();
        }

        public override string[] GetCreateTableSqls(Table table) {

            List<string> sqls = new List<string>();
            List<string> primaryKeyColumns = new List<string>();
            
            //create table
            StringBuilder sb = new StringBuilder();
            sb.Append("create table ").Append(table.Name).AppendLine(" ( ");

            int size = table.Columns.Count;
            for (int i = 0; i < size; i++) {
                sb.Append(GetColumnToSqlWhenCreate(table.Columns[i]));
                if (i != size - 1) {
                    sb.AppendLine(",");
                }
                if (table.Columns[i].IsPrimaryKey) {
                    primaryKeyColumns.Add(table.Columns[i].ColumnName);
                }
            }
            sb.AppendLine(")");

            sqls.Add(sb.ToString());
            
            //primary key
            if (primaryKeyColumns.Count > 0) {
                sqls.Add(GetPrimaryKeySql(String.Format("pk_{0}", table.Name), table.Name, primaryKeyColumns.ToArray()));
            }
            return sqls.ToArray();
        }

        public override string GetColumnToSqlWhenCreate(Column col) {
            string colType = GetDbTypeString(col.Type, col.Size);
            string colNullable = col.IsNullable ? WordNull : WordNotNull;
            string colAutoIncrement = col.IsAutoIncrement ? "identity(1,1)" : "";

            string colDefault = (col.DefaultValue != null) ?
                String.Format("default ({0})", GetColumnValueToSql(col.DefaultValue)) : "";

            //name type default nullable autoIncrement
            return String.Format("{0} {1} {2} {3} {4}", col.ColumnName, colType, colNullable, colDefault, colAutoIncrement);
        }

        public override string[] GetDropTableSqls(string tableName) {
            string sql = String.Format("drop table {0}", tableName);
            return new string[1] { sql };
        }

        public override string[] GetDropColumnSql(string table, string columnName) {
            string sql1 = String.Format("alter table {0} drop constraint DF_{0}_{1}", table, columnName);
            string sql2 = String.Format("alter table {0} drop column {1}", table, columnName);
            return new[] { sql1, sql2};
        }

        public override string GetPrimaryKeySql(string pkName, string table, params string[] columnNames) {
            if (columnNames.Length == 0) {
                throw new ArgumentException("No columns specified for primary key");
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("alter table {0} add constraint {1} primary key (", table, pkName);
            int size = columnNames.Length;
            for (int i = 0; i < size; i++) {    
                sb.Append(columnNames[i]);
                if (i != size - 1) {
                    sb.AppendLine(",");
                }
            }
            sb.Append(")");
            return sb.ToString();
        }

        public override string GetForeignKeySql(string fkName, string table, string column, string referencingTable, string referencingColumn, OnDelete onDelete) {
            string onDeleteSql;
            switch (onDelete) {
                case OnDelete.Cascade: onDeleteSql = "on delete cascade"; break;
                case OnDelete.SetNull: onDeleteSql = "on delete set null"; break;
                case OnDelete.NoAction: onDeleteSql = "on delete no action"; break;
                default: onDeleteSql = ""; break;
            }

            return String.Format("alter table {0} add constraint {1} foreign key ({2}) references {3}({4}) {5}",
                    table,
                    fkName,
                    column,
                    referencingTable,
                    referencingColumn,
                    onDeleteSql);
        }

        public override string GetUniqueKeySql(string ukName, string table, params string[] columnNames) {
            return String.Format("alter table {0} add constraint {1} unique ({2})",
                                  table,
                                  ukName,
                                  StringHelper.Implode(columnNames, ","));
        }

        public override string GetDropUniqueKeySql(string uniqueKeyName, string tableName) {
            return String.Format("alter table {0} drop constraint {1}", tableName, uniqueKeyName);
        }

        public override string GetInsertReturningColumnSql(string table, string[] columns, object[] values, string returningColumnName, string returningParameterName) {
            //declare @var int;
            //declare @tempTable TABLE (id varchar)
            //INSERT INTO Animal(Description)
            //OUTPUT       inserted.id into @tempTable
            //VALUES        ('asdf')
            //select @var = id from @tempTable
            string sql = GetInsertSql(table, columns, values);
            sql = sql.Replace(") values (", ") output Inserted." + returningColumnName + " into @tempTable values (");

            return String.Format("declare @tempTable TABLE (id int); {0}; select @{1} = id from @tempTable",
                                  sql, returningParameterName);
        }

    	public override string WrapSelectSqlWithPagination(string sql, int skipRows, int numberOfRows) {
    		throw new NotImplementedException();
    	}

    	protected override string GetDbTypeString(DbType type, int precision) {
            switch (type) {
                case DbType.AnsiStringFixedLength:
                    if (precision <= 0) return "CHAR(255)";
                    if (precision.Between(1, 255)) return String.Format("CHAR({0})", precision);
                    if (precision.Between(256, 65535)) return "TEXT";
                    if (precision.Between(65536, 16777215)) return "MEDIUMTEXT";
                    break;
                case DbType.AnsiString:
                    if (precision <= 0) return "VARCHAR(255)";
                    if (precision.Between(1, 255)) return String.Format("VARCHAR({0})", precision);
                    if (precision.Between(256, 65535)) return "TEXT";
                    if (precision.Between(65536, 16777215)) return "MEDIUMTEXT";
                    break;
                case DbType.Binary: return "BINARY";
                case DbType.Boolean: return "BIT";
                case DbType.Byte: return "TINYINT UNSIGNED";
                case DbType.Currency: return "MONEY";
                case DbType.Date: return "DATETIME";
                case DbType.DateTime: return "DATETIME";
                case DbType.Decimal:
                    if (precision <= 0) return "NUMERIC(19,5)";
                    else return String.Format("NUMERIC(19,{0})", precision);
                case DbType.Double: return "FLOAT";
                case DbType.Guid: return "VARCHAR(40)";
                case DbType.Int16: return "SMALLINT";
                case DbType.Int32: return "INTEGER";
                case DbType.Int64: return "BIGINT";
                case DbType.Single: return "FLOAT";
                case DbType.StringFixedLength:
                    if (precision <= 0) return "CHAR(255)";
                    if (precision.Between(1, 255)) return String.Format("CHAR({0})", precision);
                    if (precision.Between(256, 65535)) return "TEXT";
                    if (precision.Between(65536, 16777215)) return "MEDIUMTEXT";
                    break;
                case DbType.String:
                    if (precision <= 0) return "VARCHAR(255)";
                    if (precision.Between(1, 255)) return String.Format("VARCHAR({0})", precision);
                    if (precision.Between(256, 65535)) return "TEXT";
                    if (precision.Between(65536, 16777215)) return "MEDIUMTEXT";
                    break;
                case DbType.Time: return "TIME";
            }
            throw new DataTypeNotAvailableException(String.Format("The type {0} is no available for sqlserver", type.ToString()));
        }

        //protected override string GetDefaultValueString(object defaultValue) {
        //    return defaultValue.ToString();
        //}

        public override string GetColumnValueToSql(object value) {
            if (value is bool) {
                return ((bool)value) ? "true" : "false";
            }

            if ((value is Int16) || (value is Int32) || (value is Int64) || (value is double) || (value is float) || (value is decimal)) {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }

            if (value is DateTime) {
                DateTime dt = (DateTime)value;
                return String.Format("'{0}'", dt.ToString("s"));
            }

            return String.Format("'{0}'", value);
        }

		public override string GetTableExistsSql(string tableName) {
			return String.Format("SELECT count(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{0}'", tableName);
		}
    }
}