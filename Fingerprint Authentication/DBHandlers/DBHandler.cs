﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DPFP;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.ComponentModel;

namespace Fingerprint_Authentication.DB
{
    public class DBHandler
    {
        static DBHandler _instance;
        string id;
        int noOfChangesAllowedForTheId;
        SqlCommand command;
        SqlConnectionStringBuilder connectionStringBuilder;
        SqlConnection connection;
        bool hasFinishedGettingFingerprintsFromDB;

        public event PropertyChangedEventHandler HasFinishedFingerprintTransfer;
        public bool HasFinishedGettingFingerprintsFromDB
        {
            get
            {
                return hasFinishedGettingFingerprintsFromDB;
            }
            private set
            {
                if (hasFinishedGettingFingerprintsFromDB != value)
                {
                    hasFinishedGettingFingerprintsFromDB = value;
                    onHasFinishedGettingFingerPrintsFromDB();
                }
            }
        }
        public static DBHandler Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                else
                {
                    _instance = new DBHandler();
                    return _instance;
                }
            }
        }
        
        private DBHandler()
        {
            noOfChangesAllowedForTheId = 1;
            initialiseSqlStuff();
        }

        private void onHasFinishedGettingFingerPrintsFromDB([CallerMemberName]string propertyName = "")
        {
            HasFinishedFingerprintTransfer?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// This sets the ID/key associated to the user's information in the DB.
        /// </summary>
        /// <remarks>
        /// Note: This can only be set once in the lifetime of this application.
        /// </remarks>
        /// <param name="Id">The ID/key</param>
        public void SetID(string Id)
        {
            if (noOfChangesAllowedForTheId != 0)
            {
                id = Id;
                noOfChangesAllowedForTheId--;
            }
        }

        // Todo: Test this.
        /// <summary>
        /// Stores the serialised fingerprint (of data type byte[]) in the database.
        /// </summary>
        /// <param name="serialisedFingerprint">A <c>byte[]</c> which is the serialised fingerprint.</param>
        /// <returns>A <c>Task<bool></c> which tells if the storage was successful or not.</returns>
        public Task<bool> StoreFingerprintInDBAsync(byte[] serialisedFingerprint)
        {
            bool isDone;
            command.CommandText = @"INSERT INTO [[Put your database's name]] (id, [[Put the name of your fingerprint column]])
                                    VALUES (" + id + ", fingerprintParameter)";
            SqlParameter fingerprintParameter = new SqlParameter("fingerprintParameter", serialisedFingerprint);
            fingerprintParameter.SqlDbType = System.Data.SqlDbType.VarBinary;
            command.Parameters.Add(fingerprintParameter);

            return Task.Run(async () =>
            {
                isDone = false;

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                    try
                    {
                        // Cross-checks to make sure the fingerprint was saved.
                        Task<bool> check = checkIfStorageOfFingerprintWorkedAsync(serialisedFingerprint);
                        bool checkResult = await check;
                        if (checkResult)
                            isDone = true;
                    }
                    catch (CouldNotFindSavedFingerprintException)
                    {

                        throw new CouldNotStoreFingerprintInDBException();
                    }
                    connection.Close();
                }
                catch
                {
                    throw new CouldNotStoreFingerprintInDBException();
                }
                return isDone;

            });
        }

        // Todo: Work on this.

        public Dictionary<byte[], string> GetFingerprintsFromDBAsync()
        {
            return null;
        }

        private void initialiseSqlStuff()
        {
            command = new SqlCommand();
            connectionStringBuilder = new SqlConnectionStringBuilder();
            connection = new SqlConnection();

            connectionStringBuilder.DataSource = "";    // Put in the name or network address of the instance of your SQL server here.
            connectionStringBuilder.InitialCatalog = ""; // Put in the name of the DB here.
            connectionStringBuilder.Password = "";  // Put in the password of your DB here (if there's one).
            connectionStringBuilder.UserID = "";    // Put in the admin ID here.

            connection.ConnectionString = connectionStringBuilder.ConnectionString;
            command.Connection = connection;
        }

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);

        public static bool IsConnectedToTheInternet()
        {
            int description;
            return InternetGetConnectedState(out description, 0);
        }

        private Task<bool> checkIfStorageOfFingerprintWorkedAsync(byte[] originalByte)
        {
            command.CommandText = @"SELECT [[Put the name of your fingerprint column]] 
                                    FROM [[Put your table name here]] 
                                    WHERE id = " + id;
            byte[] byteFromDB;

            return Task.Run(() =>
            {
                try
                {
                    byteFromDB = command.ExecuteScalar() as byte[];
                }
                catch
                {
                    throw new CouldNotFindSavedFingerprintException();
                }

                return originalByte.SequenceEqual(byteFromDB);
            });
        }
    }

    [Serializable]
    public class CouldNotStoreFingerprintInDBException : Exception
    {
        public CouldNotStoreFingerprintInDBException() { }
        public CouldNotStoreFingerprintInDBException(string message) : base(message) { }
        public CouldNotStoreFingerprintInDBException(string message, Exception inner) : base(message, inner) { }
        protected CouldNotStoreFingerprintInDBException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class CouldNotFindSavedFingerprintException : Exception
    {
        public CouldNotFindSavedFingerprintException() { }
        public CouldNotFindSavedFingerprintException(string message) : base(message) { }
        public CouldNotFindSavedFingerprintException(string message, Exception inner) : base(message, inner) { }
        protected CouldNotFindSavedFingerprintException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
