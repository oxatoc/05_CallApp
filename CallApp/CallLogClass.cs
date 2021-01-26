using System;
using System.Collections.Generic;
using System.Data; //Добавить в проект ссылку на System.Data
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using CommonData;
using Newtonsoft.Json.Converters;
using static Android.Provider.CallLog;

namespace CallApp
{
    class CallLogClass
    {
        //Класс для чтения истории звонков
        //Должны быть установлены разрешения: READ_CONTACTS, READ_CALL_LOG

        public DataTable GetSpamCallsTable()
        {
            DataTable dataTable = new DataTable("SpamCallLog");
            dataTable.Columns.Add("CallDate", Type.GetType("System.String"));
            dataTable.Columns.Add("PhoneNumber", Type.GetType("System.String"));
            dataTable.Columns.Add("CallType", Type.GetType("System.String"));
            

            //Выгрузка спам-звонков в виде таблицы
            var cursor = Application.Context.ContentResolver.Query(Calls.ContentUri, null, null, null, Calls.Date + " DESC");
            if (cursor != null)
            {
                if (cursor.MoveToFirst())
                {
                    do
                    {
                        DataRow newRow = dataTable.NewRow();
                        newRow["CallDate"] = ConvertMillisecondsSinceEpochToDate(cursor.GetLong(cursor.GetColumnIndex(Calls.Date))).ToString("dd.MM.yy HH:mm:ss");
                        newRow["PhoneNumber"] = cursor.GetString(cursor.GetColumnIndex(Calls.Number));
                        newRow["CallType"] = GetCalltypeAsString(cursor.GetInt(cursor.GetColumnIndex(Calls.Type)));
                        dataTable.Rows.Add(newRow);

                    } while (cursor.MoveToNext());
                }
            }

            return dataTable;
        }

        public void GetRecentCallsLog()
        {
            Context context = Android.App.Application.Context;

            //Столбцы для полуения из запроса
            //string[] columns = new string[]
            //{
            //    Calls.Number,
            //    Calls.CachedName,
            //    Calls.Date,
            //    Calls.Type

            //};

            //var cursor = context.ContentResolver.Query(Calls.ContentUri, columns, null, null, Calls.Date + " DESC");
            var cursor = context.ContentResolver.Query(Calls.ContentUri, null, null, null, Calls.Date + " DESC");
            if (cursor != null)
            {
                if (cursor.Count == 0)
                {
                    cursor.Close();
                    Globals.ShowLog("В истории звонков не содержится записей");
                }
                else
                {
                    //Показываем названия всех столбцов
                    //for (int iCol = 0; iCol < cursor.ColumnCount; iCol++){
                    //    Globals.ShowLogDebuging($"{iCol}: {cursor.GetColumnName(iCol)}");
                    //}
                    //Показываем последние 10 звонков
                    cursor.MoveToFirst();

                    for (int iRow = 0; iRow < 3; iRow++)
                    {
                        Globals.ShowLog($"Номер: {cursor.GetString(cursor.GetColumnIndex(Calls.Number))}, имя: {cursor.GetString(cursor.GetColumnIndex(Calls.CachedName))}, дата: {ConvertMillisecondsSinceEpochToDate(cursor.GetLong(cursor.GetColumnIndex(Calls.Date))).ToString("dd.MM.yy HH:mm:ss")}, тип: {GetCalltypeAsString(cursor.GetInt(cursor.GetColumnIndex(Calls.Type)))}");
                        cursor.MoveToNext();
                    }
                    cursor.Close();
                }
            }
        }

        private string GetCalltypeAsString(int callTypeAsInt)
        {
            switch (callTypeAsInt)
            {
                case 1: return "входящий";
                case 2: return "исходящий"; 
                case 3: return "пропущен"; 
                case 4: return "голосовая почта";
                case 5: return "отклонен"; 
                case 6: return "заблокирован автоматически"; 
                case 7: return "принят на другом телефоне"; 
                default:
                    return "тип звонка не определен";
            }
        }

        private DateTime ConvertMillisecondsSinceEpochToDate(long longDate)
        {
            //Начало Unix-эпохи - 01.01.1970 00:00:00 UTC

            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UnixEpoch.AddMilliseconds(longDate), TimeZoneInfo.Local);
        }
    }
}