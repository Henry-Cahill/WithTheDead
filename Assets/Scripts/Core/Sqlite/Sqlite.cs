using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CloneZ.Db
{
    /// <summary>
    /// A tiny, self-contained binding to the native SQLite C API — just the handful of
    /// entry points the save store needs (open / exec / prepared statements). No NuGet,
    /// no vendored ORM, no bundled binaries.
    ///
    /// The native library is resolved by <see cref="Lib"/>. On Windows this is the
    /// OS-provided <c>winsqlite3.dll</c> (present in System32 on Windows 10 1803+ and
    /// Windows 11), so nothing has to ship with the project. Elsewhere it's the system's
    /// <c>libsqlite3</c>: the Linux dedicated server's image provides <c>libsqlite3.so</c>
    /// (Tools/Server/docker/Dockerfile).
    /// </summary>
    internal static class SqliteNative
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private const string Lib = "winsqlite3";
#else
        private const string Lib = "sqlite3";
#endif

        // Result codes.
        public const int OK = 0;
        public const int ROW = 100;
        public const int DONE = 101;

        // Open flags.
        public const int OPEN_READWRITE = 0x00000002;
        public const int OPEN_CREATE = 0x00000004;

        // Column type tag (only NULL is interesting to us).
        public const int TYPE_NULL = 5;

        // Passed as the bind_text destructor so SQLite copies the buffer immediately.
        public static readonly IntPtr TRANSIENT = new IntPtr(-1);

        [DllImport(Lib, EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Opens a SQLite database file with the specified flags.</summary>
        public static extern int Open(byte[] filename, out IntPtr db, int flags, IntPtr zVfs);

        [DllImport(Lib, EntryPoint = "sqlite3_close_v2", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Closes a SQLite database handle.</summary>
        public static extern int Close(IntPtr db);

        [DllImport(Lib, EntryPoint = "sqlite3_exec", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Executes a semicolon-separated SQL string without returning rows.</summary>
        public static extern int Exec(IntPtr db, byte[] sql, IntPtr callback, IntPtr arg, out IntPtr errmsg);

        [DllImport(Lib, EntryPoint = "sqlite3_free", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Frees a native pointer allocated by SQLite.</summary>
        public static extern void Free(IntPtr ptr);

        [DllImport(Lib, EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Prepares a SQL statement for later execution.</summary>
        public static extern int Prepare(IntPtr db, byte[] sql, int nByte, out IntPtr stmt, out IntPtr tail);

        [DllImport(Lib, EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Advances the prepared statement to the next step, returning a row code.</summary>
        public static extern int Step(IntPtr stmt);

        [DllImport(Lib, EntryPoint = "sqlite3_reset", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Resets a prepared SQLite statement to its initial state.</summary>
        public static extern int ResetStmt(IntPtr stmt);

        [DllImport(Lib, EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Finalizes (deallocates) a prepared SQLite statement.</summary>
        public static extern int Finalize(IntPtr stmt);

        [DllImport(Lib, EntryPoint = "sqlite3_last_insert_rowid", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Retrieves the last inserted row ID from the connection.</summary>
        public static extern long LastInsertRowId(IntPtr db);

        [DllImport(Lib, EntryPoint = "sqlite3_bind_text", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Binds a UTF‑8 text value to a parameter in a prepared statement.</summary>
        public static extern int BindText(IntPtr stmt, int index, byte[] value, int n, IntPtr destructor);

        [DllImport(Lib, EntryPoint = "sqlite3_bind_int", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Binds a 32‑bit integer value to a parameter in a prepared statement.</summary>
        public static extern int BindInt(IntPtr stmt, int index, int value);

        [DllImport(Lib, EntryPoint = "sqlite3_bind_int64", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Binds a 64‑bit integer value to a parameter in a prepared statement.</summary>
        public static extern int BindInt64(IntPtr stmt, int index, long value);

        [DllImport(Lib, EntryPoint = "sqlite3_bind_double", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Binds a double‑precision floating‑point value to a parameter in a prepared statement.</summary>
        public static extern int BindDouble(IntPtr stmt, int index, double value);

        [DllImport(Lib, EntryPoint = "sqlite3_bind_null", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Binds a NULL value to a parameter in a prepared statement.</summary>
        public static extern int BindNull(IntPtr stmt, int index);

        [DllImport(Lib, EntryPoint = "sqlite3_column_type", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Queries the declared type of a column in the current row.</summary>
        public static extern int ColumnType(IntPtr stmt, int index);

        [DllImport(Lib, EntryPoint = "sqlite3_column_int", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Retrieves a 32‑bit integer value from the given column of the current row.</summary>
        public static extern int ColumnInt(IntPtr stmt, int index);

        [DllImport(Lib, EntryPoint = "sqlite3_column_int64", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Retrieves a 64‑bit integer value from the given column of the current row.</summary>
        public static extern long ColumnInt64(IntPtr stmt, int index);

        [DllImport(Lib, EntryPoint = "sqlite3_column_double", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Retrieves a double‑precision floating‑point value from the given column of the current row.</summary>
        public static extern double ColumnDouble(IntPtr stmt, int index);

        [DllImport(Lib, EntryPoint = "sqlite3_column_text", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Retrieves a UTF‑8 string pointer from the given column of the current row.</summary>
        public static extern IntPtr ColumnText(IntPtr stmt, int index);

        [DllImport(Lib, EntryPoint = "sqlite3_errmsg", CallingConvention = CallingConvention.Cdecl)]
        /// <summary>Gets the most recent SQLite error message for the connection.</summary>
        public static extern IntPtr ErrMsg(IntPtr db);

        /// <summary>Encodes a string as a null-terminated UTF-8 buffer for the C API.</summary>
        public static byte[] Utf8(string s)
        {
            if (s == null) s = string.Empty;
            int len = Encoding.UTF8.GetByteCount(s);
            var bytes = new byte[len + 1];
            Encoding.UTF8.GetBytes(s, 0, s.Length, bytes, 0);
            bytes[len] = 0;
            return bytes;
        }

        /// <summary>Reads a null-terminated UTF-8 C string into a managed string.</summary>
        public static string FromUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0) len++;
            if (len == 0) return string.Empty;
            var bytes = new byte[len];
            Marshal.Copy(ptr, bytes, 0, len);
            return Encoding.UTF8.GetString(bytes);
        }
    }

    /// <summary>Thrown when the native SQLite API returns an error.</summary>
    public sealed class SqliteException : Exception
    {
        public SqliteException(string message) : base(message) { }
    }

    /// <summary>
    /// An open connection to a SQLite database file. Disposing closes the handle.
    /// </summary>
    public sealed class SqliteConnection : IDisposable
    {
        private IntPtr _db;

                /// <summary>Creates a new SQLite connection to the given file path, creating the file if necessary.</summary>
        public SqliteConnection(string path)
        {
            int rc = SqliteNative.Open(SqliteNative.Utf8(path), out _db,
                SqliteNative.OPEN_READWRITE | SqliteNative.OPEN_CREATE, IntPtr.Zero);
            if (rc != SqliteNative.OK)
            {
                string msg = _db != IntPtr.Zero
                    ? SqliteNative.FromUtf8(SqliteNative.ErrMsg(_db))
                    : ("open failed (rc=" + rc + ")");
                if (_db != IntPtr.Zero) { SqliteNative.Close(_db); _db = IntPtr.Zero; }
                throw new SqliteException("sqlite open: " + msg);
            }
        }

        /// <summary>Gets the native handle of the open SQLite connection.</summary>
        internal IntPtr Handle => _db;
                /// <summary>Gets the last error text from the native SQLite connection.</summary>
        internal string LastError => SqliteNative.FromUtf8(SqliteNative.ErrMsg(_db));

        /// <summary>Gets the row ID of the last INSERT operation.</summary>
        public long LastInsertRowId => SqliteNative.LastInsertRowId(_db);

        /// <summary>Runs one or more semicolon-separated statements with no result rows.</summary>
        public void Execute(string sql)
        {
            int rc = SqliteNative.Exec(_db, SqliteNative.Utf8(sql), IntPtr.Zero, IntPtr.Zero, out IntPtr err);
            if (rc != SqliteNative.OK)
            {
                string msg = err != IntPtr.Zero ? SqliteNative.FromUtf8(err) : LastError;
                if (err != IntPtr.Zero) SqliteNative.Free(err);
                throw new SqliteException("sqlite exec: " + msg + "\nSQL: " + sql);
            }
        }

                /// <summary>Creates a prepared statement for the given SQL query.</summary>
        public SqliteStatement Prepare(string sql)
        {
            int rc = SqliteNative.Prepare(_db, SqliteNative.Utf8(sql), -1, out IntPtr stmt, out _);
            if (rc != SqliteNative.OK)
                throw new SqliteException("sqlite prepare: " + LastError + "\nSQL: " + sql);
            return new SqliteStatement(this, stmt);
        }

        /// <summary>Closes the SQLite connection and releases native resources.</summary>
                /// <summary>Releases the native SQLite statement resources.</summary>
        public void Dispose()
        {
            if (_db != IntPtr.Zero) { SqliteNative.Close(_db); _db = IntPtr.Zero; }
        }
    }

    /// <summary>
    /// A prepared statement. Bind parameters (1-based), then either <see cref="Step"/>
    /// once for writes or loop while <see cref="Step"/> returns true for reads. Call
    /// <see cref="Reset"/> before re-binding to reuse the statement for another row.
    /// </summary>
    public sealed class SqliteStatement : IDisposable
    {
        private readonly SqliteConnection _conn;
        private IntPtr _stmt;

        internal SqliteStatement(SqliteConnection conn, IntPtr stmt) { _conn = conn; _stmt = stmt; }

        /// <summary>Binds a string value to the parameter at the given 1‑based index, treating null as NULL.</summary>
        public SqliteStatement Bind(int index, string value)
        {
            int rc;
            if (value == null)
            {
                rc = SqliteNative.BindNull(_stmt, index);
            }
            else
            {
                byte[] b = SqliteNative.Utf8(value);
                rc = SqliteNative.BindText(_stmt, index, b, b.Length - 1, SqliteNative.TRANSIENT);
            }
            Check(rc, "bind text");
            return this;
        }

                /// <summary>Binds a 32‑bit integer value to the parameter at the given 1‑based index.</summary>
        public SqliteStatement Bind(int index, int value) { Check(SqliteNative.BindInt(_stmt, index, value), "bind int"); return this; }
                /// <summary>Binds a 64‑bit integer value to the parameter at the given 1‑based index.</summary>
        public SqliteStatement Bind(int index, long value) { Check(SqliteNative.BindInt64(_stmt, index, value), "bind int64"); return this; }
                /// <summary>Binds a double‑precision floating‑point value to the parameter at the given 1‑based index.</summary>
        public SqliteStatement Bind(int index, double value) { Check(SqliteNative.BindDouble(_stmt, index, value), "bind double"); return this; }
                /// <summary>Binds a float value by converting it to double for the parameter at the given 1‑based index.</summary>
        public SqliteStatement Bind(int index, float value) { return Bind(index, (double)value); }
                /// <summary>Binds a boolean value as 1 (true) or 0 (false) to the parameter at the given 1‑based index.</summary>
        public SqliteStatement Bind(int index, bool value) { return Bind(index, value ? 1 : 0); }

        /// <summary>Advances the cursor. Returns true when a row is available, false when done.</summary>
        public bool Step()
        {
            int rc = SqliteNative.Step(_stmt);
            if (rc == SqliteNative.ROW) return true;
            if (rc == SqliteNative.DONE) return false;
            throw new SqliteException("sqlite step: " + _conn.LastError);
        }

                /// <summary>Resets the SQLite statement to its initial state so it can be re‑used.</summary>
        public void Reset() { SqliteNative.ResetStmt(_stmt); }

                /// <summary>Gets an Int32 column value from the current row.</summary>
        public int GetInt(int col) => SqliteNative.ColumnInt(_stmt, col);
                /// <summary>Gets an Int64 column value from the current row.</summary>
        public long GetInt64(int col) => SqliteNative.ColumnInt64(_stmt, col);
                /// <summary>Gets a single‑precision float column value from the current row.</summary>
        public float GetFloat(int col) => (float)SqliteNative.ColumnDouble(_stmt, col);
                /// <summary>Gets a Boolean column value from the current row (non‑zero is true).</summary>
        public bool GetBool(int col) => SqliteNative.ColumnInt(_stmt, col) != 0;

                /// <summary>Gets a UTF‑8 string column value from the current row, handling NULL as null.</summary>
        public string GetString(int col)
        {
            if (SqliteNative.ColumnType(_stmt, col) == SqliteNative.TYPE_NULL) return null;
            return SqliteNative.FromUtf8(SqliteNative.ColumnText(_stmt, col));
        }

        private void Check(int rc, string what)
        {
            if (rc != SqliteNative.OK) throw new SqliteException("sqlite " + what + ": " + _conn.LastError);
        }

                /// <summary>Releases the native SQLite statement resources.</summary>
        public void Dispose()
        {
            if (_stmt != IntPtr.Zero) { SqliteNative.Finalize(_stmt); _stmt = IntPtr.Zero; }
        }
    }
}
