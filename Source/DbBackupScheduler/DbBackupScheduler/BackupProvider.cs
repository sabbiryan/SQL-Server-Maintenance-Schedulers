﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Serilog;

namespace DbBackup
{
    public class BackupProvider
    {
        public static void GenerateBackups(Server server, List<Database> databases, string directory)
        {

            foreach (var database in databases)
            {
                try
                {
                    CreateFullDbBackup(server, database, directory);

                    CleanBackupDb(database, directory, DateTime.Today.AddDays(-3));
                }
                catch (Exception e)
                {
                    //write exception log
                    Log.Error(e.ToString());
                }
            }

        }



        private static void CreateFullDbBackup(Server myServer, Database database, string directory)
        {
            var fileName = directory + database.Name + "_" + DateTime.Today.Date.Year + DateTime.Today.Date.Month + DateTime.Today.Date.Day + ".bak";

            if (File.Exists(fileName)) CleanBackupDb(database, directory, DateTime.Today);

            Backup backup = new Backup
            {
                Action = BackupActionType.Database,
                Database = database.Name
            };
            /* Specify whether you want to back up database or files or log */
            /* Specify the name of the database to back up */
            /* You can take backup on several media type (disk or tape), here I am
             * using File type and storing backup on the file system */
            backup.Devices.AddDevice(fileName, DeviceType.File);
            backup.BackupSetName = database.Name + "database Backup";
            backup.BackupSetDescription = database.Name + " database - Full Backup";
            /* You can specify the expiration date for your backup data
             * after that date backup data would not be relevant */
            backup.ExpirationDate = DateTime.Today.AddDays(30);

            /* You can specify Initialize = false (default) to create a new 
             * backup set which will be appended as last backup set on the media. You
             * can specify Initialize = true to make the backup as first set on the
             * medium and to overwrite any other existing backup sets if the all the
             * backup sets have expired and specified backup set name matches with
             * the name on the medium */
            backup.Initialize = false;

            /* Wiring up events for progress monitoring */
            //bkpDBFull.PercentComplete += CompletionStatusInPercent;
            //bkpDBFull.Complete += Backup_Completed;
            backup.PercentComplete += Target;
            ServerMessageEventHandler restoreComplete = Target;
            backup.Complete += restoreComplete;

            /* SqlBackup method starts to take back up
             * You can also use SqlBackupAsync method to perform the backup 
             * operation asynchronously */
            try
            {
                backup.SqlBackup(myServer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static bool CleanBackupDb(Database database, string directory, DateTime date)
        {

            bool IsClean3DaysOldBackups;
            bool.TryParse(ConfigurationManager.AppSettings["IsClean3DaysOldBackups"],
                out IsClean3DaysOldBackups);
            if (IsClean3DaysOldBackups == false) return false;


            DateTime days = date;

            string fileName = directory + database.Name + "_" + days.Date.Year + days.Month + days.Day + ".bak";

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            return true;
        }


        private static void Target(object sender, PercentCompleteEventArgs percentCompleteEventArgs)
        {
            Console.WriteLine(percentCompleteEventArgs.Percent);
        }

        private static void Target(object sender, ServerMessageEventArgs serverMessageEventArgs)
        {
            Console.WriteLine(serverMessageEventArgs.ToString());
        }

    }
}