﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Duplicati.Library.Main.Database
{
    internal class LocalDatabase : IDisposable
    {    	
        protected readonly System.Data.IDbConnection m_connection;
        protected readonly long m_operationid = -1;

        private readonly System.Data.IDbCommand m_updateremotevolumeCommand;
        private readonly System.Data.IDbCommand m_selectremotevolumesCommand;
        private readonly System.Data.IDbCommand m_selectremotevolumeCommand;
        private readonly System.Data.IDbCommand m_removeremotevolumeCommand;
		private readonly System.Data.IDbCommand m_selectremotevolumeIdCommand;
        private readonly System.Data.IDbCommand m_createremotevolumeCommand;

        private readonly System.Data.IDbCommand m_insertlogCommand;
        private readonly System.Data.IDbCommand m_insertremotelogCommand;
        private readonly System.Data.IDbCommand m_insertIndexBlockLink;

        protected BasicResults m_result;

        public const long FOLDER_BLOCKSET_ID = -100;
        public const long SYMLINK_BLOCKSET_ID = -200;

        public DateTime OperationTimestamp { get; private set; }

        internal System.Data.IDbConnection Connection { get { return m_connection; } }
        
        public bool IsDisposed { get; private set; }

        protected static System.Data.IDbConnection CreateConnection(string path)
        {
        	path = System.IO.Path.GetFullPath(path);
            var c = (System.Data.IDbConnection)Activator.CreateInstance(Duplicati.Library.SQLiteHelper.SQLiteLoader.SQLiteConnectionType);
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(path)))
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

            Library.SQLiteHelper.DatabaseUpgrader.UpgradeDatabase(c, path, typeof(LocalDatabase));
            
            return c;
        }

        /// <summary>
        /// Creates a new database instance and starts a new operation
        /// </summary>
        /// <param name="path">The path to the database</param>
        /// <param name="operation">The name of the operation</param>
        public LocalDatabase(string path, string operation)
            : this(CreateConnection(path), operation)
        {
        }

        /// <summary>
        /// Creates a new database instance and starts a new operation
        /// </summary>
        /// <param name="path">The path to the database</param>
        /// <param name="operation">The name of the operation</param>
        public LocalDatabase(LocalDatabase db)
        	: this(db.m_connection)
		{
			this.OperationTimestamp = db.OperationTimestamp;
			this.m_connection = db.m_connection;
			this.m_operationid = db.m_operationid;
            this.m_result = db.m_result;
		}
		
        /// <summary>
        /// Creates a new database instance and starts a new operation
        /// </summary>
        /// <param name="path">The path to the database</param>
        /// <param name="operation">The name of the operation</param>
        public LocalDatabase(System.Data.IDbConnection connection, string operation)
        	: this(connection)
        {
            this.OperationTimestamp = DateTime.UtcNow;
            m_connection = connection;

            if (m_connection.State != System.Data.ConnectionState.Open)
                m_connection.Open();

            using (var cmd = m_connection.CreateCommand())
                m_operationid = Convert.ToInt64(cmd.ExecuteScalar( @"INSERT INTO ""Operation"" (""Description"", ""Timestamp"") VALUES (?, ?); SELECT last_insert_rowid();", operation, NormalizeDateTimeToEpochSeconds(OperationTimestamp)));
		}
		
		private LocalDatabase(System.Data.IDbConnection connection)
		{
            m_updateremotevolumeCommand = connection.CreateCommand();
            m_selectremotevolumesCommand = connection.CreateCommand();
            m_selectremotevolumeCommand = connection.CreateCommand();
            m_insertlogCommand = connection.CreateCommand();
            m_insertremotelogCommand = connection.CreateCommand();
            m_removeremotevolumeCommand = connection.CreateCommand();
			m_selectremotevolumeIdCommand = connection.CreateCommand();
			m_createremotevolumeCommand = connection.CreateCommand();
            m_insertIndexBlockLink = connection.CreateCommand();

            m_insertlogCommand.CommandText = @"INSERT INTO ""LogData"" (""OperationID"", ""Timestamp"", ""Type"", ""Message"", ""Exception"") VALUES (?, ?, ?, ?, ?)";
            m_insertlogCommand.AddParameters(5);

            m_insertremotelogCommand.CommandText = @"INSERT INTO ""RemoteOperation"" (""OperationID"", ""Timestamp"", ""Operation"", ""Path"", ""Data"") VALUES (?, ?, ?, ?, ?)";
            m_insertremotelogCommand.AddParameters(5);

            m_updateremotevolumeCommand.CommandText = @"UPDATE ""Remotevolume"" SET ""OperationID"" = ?, ""State"" = ?, ""Hash"" = ?, ""Size"" = ? WHERE ""Name"" = ?";
            m_updateremotevolumeCommand.AddParameters(5);

            m_selectremotevolumesCommand.CommandText = @"SELECT ""Name"", ""Type"", ""Size"", ""Hash"", ""State"" FROM ""Remotevolume""";

            m_selectremotevolumeCommand.CommandText = @"SELECT ""Type"", ""Size"", ""Hash"", ""State"" FROM ""Remotevolume"" WHERE ""Name"" = ?";
            m_selectremotevolumeCommand.AddParameter();

            m_removeremotevolumeCommand.CommandText = @"DELETE FROM ""Remotevolume"" WHERE ""Name"" = ?";
            m_removeremotevolumeCommand.AddParameter();

			m_selectremotevolumeIdCommand.CommandText = @"SELECT ""ID"" FROM ""Remotevolume"" WHERE ""Name"" = ?";

			m_createremotevolumeCommand.CommandText = @"INSERT INTO ""Remotevolume"" (""OperationID"", ""Name"", ""Type"", ""State"", ""VerificationCount"") VALUES (?, ?, ?, ?, ?); SELECT last_insert_rowid();";
            m_createremotevolumeCommand.AddParameters(5);

            m_insertIndexBlockLink.CommandText = @"INSERT INTO ""IndexBlockLink"" (""IndexVolumeID"", ""BlockVolumeID"") VALUES (?, ?)";
            m_insertIndexBlockLink.AddParameters(2);
		}

        internal void SetResult(BasicResults result)
        {
            m_result = result;
        }
		
        /// <summary>
        /// Normalizes a DateTime instance floor'ed to seconds and in UTC
        /// </summary>
        /// <returns>The normalised date time</returns>
        /// <param name="input">The input time</param>
        public static DateTime NormalizeDateTime(DateTime input)
        {
            var ticks = input.ToUniversalTime().Ticks;
            ticks -= ticks % TimeSpan.TicksPerSecond;
            return new DateTime(ticks, DateTimeKind.Utc);
        }
        
        public static long NormalizeDateTimeToEpochSeconds(DateTime input)
        {
            return (long)Math.Floor((NormalizeDateTime(input) - Library.Utility.Utility.EPOCH).TotalSeconds);
        }
        
        /// <summary>
        /// Creates a DateTime instance by adding the specified number of seconds to the EPOCH value
        /// </summary>        
        public static DateTime ParseFromEpochSeconds(long seconds)
        {
            return Library.Utility.Utility.EPOCH.AddSeconds(seconds);
        }
        
		public void UpdateRemoteVolume(string name, RemoteVolumeState state, long size, string hash, System.Data.IDbTransaction transaction = null)
        {
            m_updateremotevolumeCommand.Transaction = transaction;
            m_updateremotevolumeCommand.SetParameterValue(0, m_operationid);
            m_updateremotevolumeCommand.SetParameterValue(1, state.ToString());
            m_updateremotevolumeCommand.SetParameterValue(2, hash);
            m_updateremotevolumeCommand.SetParameterValue(3, size);
            m_updateremotevolumeCommand.SetParameterValue(4, name);
            var c = m_updateremotevolumeCommand.ExecuteNonQuery();
            if (c != 1)
            	throw new Exception("Unexpected number of remote volumes detected!");
            	
           	if (state == RemoteVolumeState.Deleted)
           		RemoveRemoteVolume(name, transaction);
        }
        
        public IEnumerable<KeyValuePair<long, DateTime>> FilesetTimes
        { 
            get 
            {
                using(var cmd = m_connection.CreateCommand())
                using(var rd = cmd.ExecuteReader(@"SELECT ""ID"", ""Timestamp"" FROM ""Fileset"" ORDER BY ""Timestamp"" DESC"))
                    while (rd.Read())
                        yield return new KeyValuePair<long, DateTime>(Convert.ToInt64(rd.GetValue(0)), ParseFromEpochSeconds(Convert.ToInt64(rd.GetValue(1))).ToLocalTime());
            }
        }

		public Tuple<string, object[]> GetFilelistWhereClause(DateTime time, long[] versions, IEnumerable<KeyValuePair<long, DateTime>> filesetslist = null)
		{
			var filesets = (filesetslist ?? this.FilesetTimes).ToArray();
			string query = "";
			var args = new List<object>();
            if (time.Ticks > 0 || (versions != null && versions.Length > 0))
            {
                var hasTime = false;
                if (time.Ticks > 0)
                {
                    if (time.Kind == DateTimeKind.Unspecified)
                        throw new Exception("Invalid DateTime given, must be either local or UTC");
            
                    query += @" ""Timestamp"" <= ?";
                    // Make sure the resolution is the same (i.e. no milliseconds)
                    args.Add(NormalizeDateTimeToEpochSeconds(time));
                    hasTime = true;
                }
                
                if (versions != null && versions.Length > 0)
                {
                    var qs = "";
                    
                    foreach(var v in versions)
                        if (v >= 0 && v < filesets.Length)
                        {
                            args.Add(filesets[v].Key);
                            qs += "?,";
                        }
                        else
                            m_result.AddWarning(string.Format("Skipping invalid version: {0}", v), null);
                            
                        
                    if (qs.Length > 0)
                    {
                        qs = qs.Substring(0, qs.Length - 1);
                        
                        if (hasTime)
                            query += " OR ";
                                            
                        query += @" ""ID"" IN (" + qs + ")";
                    }
                }
                
                if (!string.IsNullOrEmpty(query))
                    query = " WHERE " + query;

            }
            
            return new Tuple<string, object[]>(query, args.ToArray());
        }

        public long GetRemoteVolumeID(string file, System.Data.IDbTransaction transaction = null)
		{
			m_selectremotevolumeIdCommand.Transaction = transaction;
			var o = m_selectremotevolumeIdCommand.ExecuteScalar(null, file);
			if (o == null || o == DBNull.Value)
				return -1;
			else
				return Convert.ToInt64(o);
		}

        public bool GetRemoteVolume(string file, out string hash, out long size, out RemoteVolumeType type, out RemoteVolumeState state)
        {
            m_selectremotevolumeCommand.SetParameterValue(0, file);
            using (var rd = m_selectremotevolumeCommand.ExecuteReader())
                if (rd.Read())
                {
                    hash = (rd.GetValue(2) == null || rd.GetValue(2) == DBNull.Value) ? null : rd.GetValue(3).ToString();
                    size = (rd.GetValue(1) == null || rd.GetValue(1) == DBNull.Value) ? -1 : Convert.ToInt64(rd.GetValue(2));
                    type = (RemoteVolumeType)Enum.Parse(typeof(RemoteVolumeType), rd.GetValue(0).ToString());
                    state = (RemoteVolumeState)Enum.Parse(typeof(RemoteVolumeState), rd.GetValue(3).ToString());
                    return true;
                }

            hash = null;
            size = -1;
            type = (RemoteVolumeType)(-1);
            state = (RemoteVolumeState)(-1);
            return false;
        }

        public IEnumerable<RemoteVolumeEntry> GetRemoteVolumes()
        {
            using (var rd = m_selectremotevolumesCommand.ExecuteReader())
            {
                while (rd.Read())
                {
                    yield return new RemoteVolumeEntry(
                        rd.GetValue(0).ToString(),
                        (rd.GetValue(3) == null || rd.GetValue(3) == DBNull.Value) ? null : rd.GetValue(3).ToString(),
                        (rd.GetValue(2) == null || rd.GetValue(2) == DBNull.Value) ? -1 : Convert.ToInt64(rd.GetValue(2)),
                        (RemoteVolumeType)Enum.Parse(typeof(RemoteVolumeType), rd.GetValue(1).ToString()),
                        (RemoteVolumeState)Enum.Parse(typeof(RemoteVolumeState), rd.GetValue(4).ToString())
                    );
                }
            }
        }

        /// <summary>
        /// Log an operation performed on the remote backend
        /// </summary>
        /// <param name="operation">The operation performed</param>
        /// <param name="path">The path involved</param>
        /// <param name="data">Any data relating to the operation</param>
        public void LogRemoteOperation(string operation, string path, string data, System.Data.IDbTransaction transaction)
        {
        	m_insertremotelogCommand.Transaction = transaction;
            m_insertremotelogCommand.SetParameterValue(0, m_operationid);
            m_insertremotelogCommand.SetParameterValue(1, NormalizeDateTimeToEpochSeconds(DateTime.UtcNow));
            m_insertremotelogCommand.SetParameterValue(2, operation);
            m_insertremotelogCommand.SetParameterValue(3, path);
            m_insertremotelogCommand.SetParameterValue(4, data);
            m_insertremotelogCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        /// <param name="type">The message type</param>
        /// <param name="message">The message</param>
        /// <param name="exception">An optional exception</param>
        public void LogMessage(string type, string message, Exception exception, System.Data.IDbTransaction transaction)
        {
        	m_insertlogCommand.Transaction = transaction;
            m_insertlogCommand.SetParameterValue(0, m_operationid);
            m_insertlogCommand.SetParameterValue(1, NormalizeDateTimeToEpochSeconds(DateTime.UtcNow));
            m_insertlogCommand.SetParameterValue(2, type);
            m_insertlogCommand.SetParameterValue(3, message);
            m_insertlogCommand.SetParameterValue(4, exception == null ? null : exception.ToString());
            m_insertlogCommand.ExecuteNonQuery();
        }

        public void RemoveRemoteVolume(string name, System.Data.IDbTransaction transaction = null)
        {

            using (var tr = new TemporaryTransactionWrapper(m_connection, transaction))
            using (var deletecmd = m_connection.CreateCommand())
            {
                deletecmd.Transaction = tr.Parent;
            	var volumeid = GetRemoteVolumeID(name, tr.Parent);
                
				// If the volume is a block volume, this will update the crosslink table, otherwise nothing will happen
				deletecmd.ExecuteNonQuery(@"DELETE FROM ""IndexBlockLink"" WHERE ""BlockVolumeID"" = ? ", volumeid);
				
                // If the volume is a fileset, this will remove the fileset, otherwise nothing will happen
                deletecmd.ExecuteNonQuery(@"DELETE FROM ""FilesetEntry"" WHERE ""FilesetID"" IN (SELECT ""ID"" FROM ""Fileset"" WHERE ""VolumeID"" = ?)", volumeid);
                deletecmd.ExecuteNonQuery(@"DELETE FROM ""Fileset"" WHERE ""VolumeID"" = ?", volumeid);
                                                
				var subQuery = @"(SELECT DISTINCT ""BlocksetEntry"".""BlocksetID"" FROM ""BlocksetEntry"", ""Block"" WHERE ""BlocksetEntry"".""BlockID"" = ""Block"".""ID"" AND ""Block"".""VolumeID"" = ?)";

				deletecmd.ExecuteNonQuery(@"DELETE FROM ""File"" WHERE ""BlocksetID"" IN " + subQuery, volumeid);
				deletecmd.ExecuteNonQuery(@"DELETE FROM ""Metadataset"" WHERE ""BlocksetID"" IN " + subQuery, volumeid);
				deletecmd.ExecuteNonQuery(@"DELETE FROM ""Blockset"" WHERE ""ID"" IN " + subQuery, volumeid);
				deletecmd.ExecuteNonQuery(@"DELETE FROM ""BlocksetEntry"" WHERE ""BlocksetID"" IN " + subQuery, volumeid);
				
				deletecmd.ExecuteNonQuery(@"DELETE FROM ""Block"" WHERE ""VolumeID"" = ?", volumeid);
				deletecmd.ExecuteNonQuery(@"DELETE FROM ""DeletedBlock"" WHERE ""VolumeID"" = ?", volumeid);

                ((System.Data.IDataParameter)m_removeremotevolumeCommand.Parameters[0]).Value = name;
                m_removeremotevolumeCommand.Transaction = tr.Parent;
                m_removeremotevolumeCommand.ExecuteNonQuery();

                tr.Commit();
            }
        }
        
        public void Vacuum()
        {
            using(var cmd = m_connection.CreateCommand())
                cmd.ExecuteNonQuery("VACUUM");
        }

		public long RegisterRemoteVolume(string name, RemoteVolumeType type, RemoteVolumeState state, System.Data.IDbTransaction transaction = null)
		{
        	using(var tr = new TemporaryTransactionWrapper(m_connection, transaction))
        	{
                m_createremotevolumeCommand.SetParameterValue(0, m_operationid);
                m_createremotevolumeCommand.SetParameterValue(1, name);
                m_createremotevolumeCommand.SetParameterValue(2, type.ToString());
                m_createremotevolumeCommand.SetParameterValue(3, state.ToString());
                m_createremotevolumeCommand.SetParameterValue(4, 0);
                m_createremotevolumeCommand.Transaction = tr.Parent;
                var r = Convert.ToInt64(m_createremotevolumeCommand.ExecuteScalar());
                tr.Commit();
                return r;
            }
        }

        public long GetFilesetID(DateTime restoretime, long[] versions)
        {
            return GetFilesetIDs(restoretime, versions).First();
        }        

        public IEnumerable<long> GetFilesetIDs(DateTime restoretime, long[] versions)
        {
            if (restoretime.Kind == DateTimeKind.Unspecified)
                throw new Exception("Invalid DateTime given, must be either local or UTC");

            var tmp = GetFilelistWhereClause(restoretime, versions);
            string query = tmp.Item1;
            var args = tmp.Item2;

            var res = new List<long>();
            using(var cmd = m_connection.CreateCommand())
            {            
                using(var rd = cmd.ExecuteReader(@"SELECT ""ID"" FROM ""Fileset"" " + query  + @" ORDER BY ""Timestamp"" DESC", args))
                    while (rd.Read())
                        res.Add(Convert.ToInt64(rd.GetValue(0)));
                        
                if (res.Count == 0)
                {
                    cmd.Parameters.Clear();
                    using(var rd = cmd.ExecuteReader(@"SELECT ""ID"" FROM ""Fileset"" ORDER BY ""Timestamp"" DESC "))
                    while (rd.Read())
                        res.Add(Convert.ToInt64(rd.GetValue(0)));
                    
                    if (res.Count == 0)
                        throw new Exception("No backup at the specified date");
                    else
                        m_result.AddWarning(string.Format("Restore time or version did not match any existing backups, selecting newest backup"), null);
                }

                return res;
            }
        }

        public System.Data.IDbTransaction BeginTransaction()
        {
            return m_connection.BeginTransaction();
        }

        protected class TemporaryTransactionWrapper : IDisposable
        {
            private System.Data.IDbTransaction m_parent;
            private bool m_isTemporary;

            public TemporaryTransactionWrapper(System.Data.IDbConnection connection, System.Data.IDbTransaction transaction)
            {
                if (transaction != null)
                {
                    m_parent = transaction;
                    m_isTemporary = false;
                }
                else
                {
                    m_parent = connection.BeginTransaction();
                    m_isTemporary = true;
                }
            }

            public System.Data.IDbConnection Connection { get { return m_parent.Connection; } }
            public System.Data.IsolationLevel IsolationLevel { get { return m_parent.IsolationLevel; } }

            public void Commit() 
            { 
                if (m_isTemporary) 
                    m_parent.Commit(); 
            }

            public void Rollback()
            {
                if (m_isTemporary)
                    m_parent.Rollback(); 
            }

            public void Dispose() 
            {
                if (m_isTemporary)
                    m_parent.Dispose();
            }

            public System.Data.IDbTransaction Parent { get { return m_parent; } }
        }
        
        private class LocalFileEntry : ILocalFileEntry
        {
            private System.Data.IDataReader m_reader;
            public LocalFileEntry(System.Data.IDataReader reader)
            {
                m_reader = reader;
            }

            public string Path
            {
                get 
                {
                    var c = m_reader.GetValue(0);
                    if (c == null || c == DBNull.Value)
                        return null;
                    return c.ToString();
                }
            }

            public long Length
            {
                get
                {
                    var c = m_reader.GetValue(1);
                    if (c == null || c == DBNull.Value)
                        return -1;
                    return Convert.ToInt64(c);
                }
            }

            public string Hash
            {
                get
                {
                    var c = m_reader.GetValue(2);
                    if (c == null || c == DBNull.Value)
                        return null;
                    return c.ToString();
                }
            }

            public string Metahash
            {
                get
                {
                    var c = m_reader.GetValue(3);
                    if (c == null || c == DBNull.Value)
                        return null;
                    return c.ToString();
                }
            }
        }
        
        public IEnumerable<ILocalFileEntry> GetFiles(long filesetId)
        {
            using(var cmd = m_connection.CreateCommand())
            using(var rd = cmd.ExecuteReader(@"SELECT ""A"".""Path"", ""B"".""Length"", ""B"".""FullHash"", ""D"".""FullHash"" FROM ""File"" A, ""Blockset"" B, ""Metadataset"" C, ""Blockset"" D, ""FilesetEntry"" E WHERE ""A"".""BlocksetID"" = ""B"".""ID"" AND ""A"".""MetadataID"" = ""C"".""ID"" AND ""C"".""BlocksetID"" = ""D"".""ID"" AND ""A"".""ID"" = ""E"".""FileID"" AND ""E"".""FilesetID"" = ? ", filesetId))
            while(rd.Read())
            	yield return new LocalFileEntry(rd);
        }

		private IEnumerable<KeyValuePair<string, string>> GetDbOptionList()
		{
            using(var cmd = m_connection.CreateCommand())
            using(var rd = cmd.ExecuteReader(@"SELECT ""Key"", ""Value"" FROM ""Configuration"" "))
            while(rd.Read())
            	yield return new KeyValuePair<string, string>(rd.GetValue(0).ToString(), rd.GetValue(1).ToString());
		}
		
		public IDictionary<string, string> GetDbOptions()
		{
			return GetDbOptionList().ToDictionary(x => x.Key, x => x.Value);	
		}
		
		public void SetDbOptions(IDictionary<string, string> options, System.Data.IDbTransaction transaction = null)
		{
			using(var tr = new TemporaryTransactionWrapper(m_connection, transaction))
            using(var cmd = m_connection.CreateCommand())
			{
				cmd.Transaction = tr.Parent;
				cmd.ExecuteNonQuery(@"DELETE FROM ""Configuration"" ");
				foreach(var kp in options)
					cmd.ExecuteNonQuery(@"INSERT INTO ""Configuration"" (""Key"", ""Value"") VALUES (?, ?) ", kp.Key, kp.Value);
				
				tr.Commit();
			}
		}

		public long GetBlocksLargerThan(long fhblocksize)
		{
            using(var cmd = m_connection.CreateCommand())
            	return Convert.ToInt64(cmd.ExecuteScalar(@"SELECT COUNT(*) FROM ""Block"" WHERE ""Size"" > ?", fhblocksize));
		}

        public void VerifyConsistency(System.Data.IDbTransaction transaction)
        {
            using (var cmd = m_connection.CreateCommand())
            {
            	cmd.Transaction = transaction;

                // Calculate the lengths for each blockset                
                var combinedLengths = @"SELECT ""BlocksetEntry"".""BlocksetID"" AS ""BlocksetID"", SUM(""Block"".""Size"") AS ""CalcLen"", ""Blockset"".""Length"" AS ""Length"" FROM ""Block"", ""BlocksetEntry"", ""Blockset"" WHERE ""BlocksetEntry"".""BlockID"" = ""Block"".""ID"" AND ""BlocksetEntry"".""BlocksetID"" = ""Blockset"".""ID"" GROUP BY ""BlocksetEntry"".""BlocksetID""";
                // For each blockset with wrong lengths, fetch the file path
                var reportDetails = @"SELECT ""CalcLen"", ""Length"", ""A"".""BlocksetID"", ""File"".""Path"" FROM (" + combinedLengths + @") A, ""File"" WHERE ""A"".""BlocksetID"" = ""File"".""BlocksetID"" AND ""A"".""CalcLen"" != ""A"".""Length"" ";
                
                using(var rd = cmd.ExecuteReader(reportDetails))
                	if (rd.Read())
                	{
                		var sb = new StringBuilder();
                		sb.AppendLine("Found inconsistency in the following files while validating database: ");
                		var c = 0;
                		do
                		{
                			if (c < 5)
                				sb.AppendFormat("{0}, actual size {1}, dbsize {2}, blocksetid: {3}{4}", rd.GetValue(3), rd.GetValue(1), rd.GetValue(0), rd.GetValue(2), Environment.NewLine);
                			c++;
                		} while(rd.Read());
                		
                		c -= 5;
                		if (c > 0)
                			sb.AppendFormat("... and {0} more", c);
                		
	                    throw new InvalidDataException(sb.ToString());
                	}
            }
        }

		public interface IBlock
		{
			string Hash { get; }
			long Size { get; }
		}
		
		internal class Block : IBlock
		{
			public string Hash { get; private set; }
			public long Size { get; private set; }
			
			public Block(string hash, long size)
			{
				this.Hash = hash;
				this.Size = size;
			}
		}		

		public IEnumerable<IBlock> GetBlocks(long volumeid)
		{
			using(var cmd = m_connection.CreateCommand())
			using(var rd = cmd.ExecuteReader(@"SELECT DISTINCT ""Hash"", ""Size"" FROM ""Block"" WHERE ""VolumeID"" = ?", volumeid))
				while (rd.Read())
					yield return new Block(rd.GetValue(0).ToString(), Convert.ToInt64(rd.GetValue(1)));
		}

        private class BlocklistHashEnumerable : IEnumerable<string>
        {
            private class BlocklistHashEnumerator : IEnumerator<string>
            {
                private System.Data.IDataReader m_reader;
                private BlocklistHashEnumerable m_parent;
                private string m_path = null;
                private bool m_first = true;
                private string m_current = null;

                public BlocklistHashEnumerator(BlocklistHashEnumerable parent, System.Data.IDataReader reader)
                {
                    m_reader = reader;
                    m_parent = parent;
                }

                public string Current { get{ return m_current; } }

                public void Dispose()
                {
                }

                object System.Collections.IEnumerator.Current { get { return this.Current; } }

                public bool MoveNext()
                {
                    m_first = false;

                    if (m_path == null)
                    {
                        m_path = m_reader.GetValue(0).ToString();
                        m_current = m_reader.GetValue(6).ToString();
                        return true;
                    }
                    else
                    {
                        if (m_current == null)
                            return false;

                        if (!m_reader.Read())
                        {
                            m_current = null;
                            m_parent.MoreData = false;
                            return false;
                        }

                        var np = m_reader.GetValue(0).ToString();
                        if (m_path != np)
                        {
                            m_current = null;
                            return false;
                        }

                        m_current = m_reader.GetValue(6).ToString();
                        return true;
                    }
                }

                public void Reset()
                {
                    if (!m_first)
                        throw new Exception("Iterator reset not supported");

                    m_first = false;
                }
            }

            private System.Data.IDataReader m_reader;

            public BlocklistHashEnumerable(System.Data.IDataReader reader)
            {
                m_reader = reader;
                this.MoreData = true;
            }

            public bool MoreData { get; protected set; }

            public IEnumerator<string> GetEnumerator()
            {
                return new BlocklistHashEnumerator(this, m_reader);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public void WriteFileset(Volumes.FilesetVolumeWriter filesetvolume, System.Data.IDbTransaction transaction, long filesetId)
        {
            using (var cmd = m_connection.CreateCommand())
            {
            	cmd.Transaction = transaction;
                cmd.CommandText = @"SELECT ""B"".""BlocksetID"", ""B"".""ID"", ""B"".""Path"", ""D"".""Length"", ""D"".""FullHash"", ""A"".""Scantime"" FROM ""FilesetEntry"" A, ""File"" B, ""Metadataset"" C, ""Blockset"" D WHERE ""A"".""FileID"" = ""B"".""ID"" AND ""B"".""MetadataID"" = ""C"".""ID"" AND ""C"".""BlocksetID"" = ""D"".""ID"" AND (""B"".""BlocksetID"" = ? OR ""B"".""BlocksetID"" = ?) AND ""A"".""FilesetID"" = ? ";
                cmd.AddParameter(FOLDER_BLOCKSET_ID);
                cmd.AddParameter(SYMLINK_BLOCKSET_ID);
                cmd.AddParameter(filesetId);

                using (var rd = cmd.ExecuteReader())
                while(rd.Read())
                {
                    var blocksetID = Convert.ToInt64(rd.GetValue(0));
                    var path = rd.GetValue(2).ToString();
                    var metalength = Convert.ToInt64(rd.GetValue(3));
                    var metahash = rd.GetValue(4).ToString();

                    if (blocksetID == FOLDER_BLOCKSET_ID)
                        filesetvolume.AddDirectory(path, metahash, metalength);
                    else if (blocksetID == SYMLINK_BLOCKSET_ID)
                        filesetvolume.AddSymlink(path, metahash, metalength);
                }

                cmd.CommandText = @"SELECT ""F"".""Path"", ""F"".""Scantime"", ""F"".""Filelength"", ""F"".""Filehash"", ""F"".""Metahash"", ""F"".""Metalength"", ""G"".""Hash"" FROM (SELECT ""A"".""Path"" AS ""Path"", ""D"".""Scantime"" AS ""Scantime"", ""B"".""Length"" AS ""Filelength"", ""B"".""FullHash"" AS ""Filehash"", ""E"".""FullHash"" AS ""Metahash"", ""E"".""Length"" AS ""Metalength"", ""A"".""BlocksetID"" AS ""BlocksetID"" FROM ""File"" A, ""Blockset"" B, ""Metadataset"" C, ""FilesetEntry"" D, ""Blockset"" E WHERE ""A"".""ID"" = ""D"".""FileID"" AND ""D"".""FilesetID"" = ? AND ""A"".""BlocksetID"" = ""B"".""ID"" AND ""A"".""MetadataID"" = ""C"".""ID"" AND ""E"".""ID"" = ""C"".""BlocksetID"") F LEFT OUTER JOIN ""BlocklistHash"" G ON ""G"".""BlocksetID"" = ""F"".""BlocksetID"" ORDER BY ""F"".""Path"", ""G"".""Index"" ";
                cmd.Parameters.Clear();
                cmd.AddParameter(filesetId);

                using (var rd = cmd.ExecuteReader())
                if (rd.Read())
                {
                    var more = false;
                    do
                    {
                        var path = rd.GetValue(0).ToString();
                        var filehash = rd.GetValue(3).ToString();
                        var size = Convert.ToInt64(rd.GetValue(2));
                        var scantime = ParseFromEpochSeconds(Convert.ToInt64(rd.GetValue(1)));
                        var metahash = rd.GetValue(4).ToString();
                        var metasize = Convert.ToInt64(rd.GetValue(5));
                        var p = rd.GetValue(6);
                        var blrd = (p == null || p == DBNull.Value) ? null : new BlocklistHashEnumerable(rd);

                        filesetvolume.AddFile(path, filehash, size, scantime, metahash, metasize, blrd);
                        if (blrd == null)
                            more = rd.Read();
                        else
                            more = blrd.MoreData;

                    } while (more);
                }
            }
        }
        
        /// <summary>
        /// Keeps a list of filenames in a temporary table with a single columne Path
        ///</summary>
        public class FilteredFilenameTable : IDisposable
        {
            public string Tablename { get; private set; }
            private System.Data.IDbConnection m_connection;
            
            public FilteredFilenameTable(System.Data.IDbConnection connection, Library.Utility.IFilter filter, System.Data.IDbTransaction transaction)
            {
                m_connection = connection;
                Tablename = "Filenames-" + Library.Utility.Utility.ByteArrayAsHexString(Guid.NewGuid().ToByteArray());
                var type = Library.Utility.FilterType.Regexp;
                if (filter is Library.Utility.FilterExpression)
                	type = ((Library.Utility.FilterExpression)filter).Type;
                
                if (type == Library.Utility.FilterType.Regexp)
                {
                    using(var cmd = m_connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery(string.Format(@"CREATE TEMPORARY TABLE ""{0}"" (""Path"" TEXT NOT NULL)", Tablename));
                        using(var tr = new TemporaryTransactionWrapper(m_connection, transaction))
                        {
                            cmd.CommandText = string.Format(@"INSERT INTO ""{0}"" (""Path"") VALUES (?)", Tablename);
                            cmd.AddParameter();
                            cmd.Transaction = tr.Parent;
                            using(var c2 = m_connection.CreateCommand())
                            using(var rd = c2.ExecuteReader(@"SELECT DISTINCT ""Path"" FROM ""File"" "))
                                while(rd.Read())
                                {
                                    var p = rd.GetValue(0).ToString();
                                    if(Library.Utility.FilterExpression.Matches(filter, p))
                                    {
                                        cmd.SetParameterValue(0, p);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            
                            
                            tr.Commit();
                        }
                    }
                }
                else
                {
                    var sb = new StringBuilder();
                    var args = new List<object>();
                    foreach(var f in ((Library.Utility.FilterExpression)filter).GetSimpleList())
                    {
                        if (f.Contains('*') || f.Contains('?'))
                        {
                            sb.Append(@"""Path"" LIKE ? OR ");
                            args.Add(f.Replace('*', '%').Replace('?', '_'));
                        }
                        else
                        {
                            sb.Append(@"""Path"" = ? OR ");
                            args.Add(f);
                        }
                    }
                    
                    sb.Length = sb.Length - " OR ".Length;
                    
                    using(var cmd = m_connection.CreateCommand())
                    using(var tr = new TemporaryTransactionWrapper(m_connection, transaction))
                    {
                        cmd.Transaction = tr.Parent;
                        cmd.ExecuteNonQuery(string.Format(@"CREATE TEMPORARY TABLE ""{0}"" (""Path"" TEXT NOT NULL)", Tablename));
                        cmd.ExecuteNonQuery(string.Format(@"INSERT INTO ""{0}"" SELECT DISTINCT ""Path"" FROM ""File"" WHERE " + sb.ToString(), Tablename), args.ToArray());
                        tr.Commit();
                    }
                }
            }
            
            public void Dispose()
            {
                if (Tablename != null)
                    try 
                    { 
                        using(var cmd = m_connection.CreateCommand())
                            cmd.ExecuteNonQuery(string.Format(@"DROP TABLE IF EXISTS ""{0}"" ", Tablename));
                    }
                    catch { }
                    finally { Tablename = null; }
            }                
        }
        
        public void RenameRemoteFile(string oldname, string newname, System.Data.IDbTransaction transaction)
        {
            using(var tr = new TemporaryTransactionWrapper(m_connection, transaction))
            using(var cmd = m_connection.CreateCommand())
            {
                cmd.Transaction = tr.Parent;
                
                //Rename the old entry, to preserve ID links
                var c = cmd.ExecuteNonQuery(@"UPDATE ""Remotevolume"" SET ""Name"" = ? WHERE ""Name"" = ?", newname, oldname);
                if (c != 1)
                    throw new Exception(string.Format("Unexpected result from renaming \"{0}\" to \"{1}\", expected {2} got {3}", oldname, newname, 1, c));
                
                // Grab the type of entry
                var type = (RemoteVolumeType)Enum.Parse(typeof(RemoteVolumeType), cmd.ExecuteScalar(@"SELECT ""Type"" FROM ""Remotevolume"" WHERE ""Name"" = ?", newname).ToString(), true);
                
                //Create a fake new entry with the old name and mark as deleting
                // as this ensures we will remove it, if it shows up in some later listing
                RegisterRemoteVolume(oldname, type, RemoteVolumeState.Deleting, tr.Parent);
                
                tr.Commit();
            }
        }
        
        /// <summary>
        /// Creates a timestamped backup operation to correctly associate the fileset with the time it was created.
        /// </summary>
        /// <param name="volumeid">The ID of the fileset volume to update</param>
        /// <param name="timestamp">The timestamp of the operation to create</param>
        /// <param name="transaction">An optional external transaction</param>
        public virtual long CreateFileset(long volumeid, DateTime timestamp, System.Data.IDbTransaction transaction = null)
        {
            using (var cmd = m_connection.CreateCommand())
            using (var tr = new TemporaryTransactionWrapper(m_connection, transaction))
            {
                cmd.Transaction = tr.Parent;                
                var id = Convert.ToInt64(cmd.ExecuteScalar(@"INSERT INTO ""Fileset"" (""OperationID"", ""Timestamp"", ""VolumeID"") VALUES (?, ?, ?); SELECT last_insert_rowid();", m_operationid, NormalizeDateTimeToEpochSeconds(timestamp), volumeid));
                tr.Commit();
                return id;
            }
        }
        
        public void AddIndexBlockLink(long indexVolumeID, long blockVolumeID, System.Data.IDbTransaction transaction)
        {
            m_insertIndexBlockLink.Transaction = transaction;
            m_insertIndexBlockLink.SetParameterValue(0, indexVolumeID);
            m_insertIndexBlockLink.SetParameterValue(1, blockVolumeID);
            m_insertIndexBlockLink.ExecuteNonQuery();
        }
        
        public virtual void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            
        }

        public void WriteResults()
        {
            if (IsDisposed)
                return;

            if (m_connection != null && m_result != null)
            {
                m_result.FlushLog();
                LogMessage("Result", Library.Utility.Utility.PrintSerializeObject(m_result, (StringBuilder)null, x => !typeof(System.Collections.IEnumerable).IsAssignableFrom(x.PropertyType)).ToString(), null, null);
            }
        }
    }
}
