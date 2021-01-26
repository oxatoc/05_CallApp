using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using CommonData;
using Android.Telecom;
using Android.Content;
using System.ComponentModel;
using Android.Media;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Xml;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Android.Graphics;
//Чтобы работали методы DataTable - добавить в ссылки проекта сборку System.Data.DataSetExtensions

namespace CallApp
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        public const string OAUTH_IMPORT_CRITERIA_ACTION = "ImportCriteria";
        public const string OAUTH_UPLOAD_SPAM_LOG_ACTION = "UploadSpamLog";


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            Globals.logTextBox = FindViewById<EditText>(Resource.Id.logEditText);

            //Проверка, является ли приложение приложением по умолчанию
            TelecomManager telecomManager = (TelecomManager)GetSystemService(Context.TelecomService);
            //Globals.ShowLog($"Приложение по умолчанию: '{telecomManager.DefaultDialerPackage}'");
            if (PackageName != telecomManager.DefaultDialerPackage)
            {
                Globals.ShowLog("---Ошибка: приложение не установлено по умолчанию в качестве телефона");
            }
            else
            {
                Globals.ShowLog("Приложение используется по умолчанию");
            }

            //Тест разрешения на установку беззвучного режима
            //Есть ли возможность применения permission ACCESS_NOTIFICATION_POLICY
            //На 25.07.20 - даже если permission ACCESS_NOTIFICATION_POLICY проставлено, права не предоставляются все равно
            NotificationManager notificationManager = (NotificationManager)this.GetSystemService(Context.NotificationService);
            if (notificationManager.IsNotificationPolicyAccessGranted)
            {
                Globals.ShowLog("Права на установку беззвучного режима предоставлены");
            }
            else
            {
                Globals.ShowLog("Права на установку беззвучного режима не предоставлены");
            }

            //Тест истории звонков
            //CallLogClass callLog = new CallLogClass();
            //callLog.GetRecentCallsLog();

            Button importPhoneNumberCriteriasButton = FindViewById<Button>(Resource.Id.importPhoneNumberCriteriasButton);
            importPhoneNumberCriteriasButton.Click += (sender, e) =>
            {
                OAuthClass oAuth = new OAuthClass();
                //Задаем функцию обратного вызова
                oAuth.GetToken(
                    AppPrivateData.GOOGLE_API_CLIENT_ID,
                    "https://www.googleapis.com/auth/spreadsheets",
                    "https://accounts.google.com/o/oauth2/auth",
                    this,
                    OAUTH_IMPORT_CRITERIA_ACTION,
                    OAuthCallbackFunction
                    );
            };

            Button uploadSpamLog = FindViewById<Button>(Resource.Id.uploadSpamLog);
            uploadSpamLog.Click += (sender, e) =>
            {

                OAuthClass oAuth = new OAuthClass();
                //Задаем функциою обратного вызова
                oAuth.GetToken(
                    AppPrivateData.GOOGLE_API_CLIENT_ID,
                    "https://www.googleapis.com/auth/spreadsheets",
                    "https://accounts.google.com/o/oauth2/auth",
                    this,
                    OAUTH_UPLOAD_SPAM_LOG_ACTION,
                    OAuthCallbackFunction
                    );
            };

            Button settingsButton = FindViewById<Button>(Resource.Id.settingsButton);
            settingsButton.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(SettingsActivity));
                StartActivity(intent);
            };

            Button showCallWindowButton = FindViewById<Button>(Resource.Id.showCallWindow);
            showCallWindowButton.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(IncomingCallActivity));
                StartActivity(intent);
            };



            Button showPhoneNumberCriteriasButton = FindViewById<Button>(Resource.Id.showPhoneNumberCriteriasButton);
            showPhoneNumberCriteriasButton.Click += (sender, e) =>
            {
                AppDataClass appData = new AppDataClass();
                Globals.ShowLog("Критерии телефонных номеров:");
                Globals.ShowLog(appData.GetPhoneNumberCriteriaTableContent());
            };

            Button clearLogWindowButton = FindViewById<Button>(Resource.Id.clearLogWindowButton);
            clearLogWindowButton.Click += (sender, e) =>
            {
                Globals.logTextBox.Text = "";
            };

            //Объекты для обработки sms
            ISmsHandling smsHandlingObject = new SmsHandlingClass(this);
            if (!smsHandlingObject.IsDefaultApplication)
            {
                Globals.ShowLog("---Ошибка: приложение не установлено как приложение по умолчанию для обработки sms");
            }

            //Тест аудиозвонка после обнволения на Андроид 10
            //AudioManager audioManager = (AudioManager)GetSystemService(Context.AudioService);
            //Globals.ShowLog("Режим звонка: " + audioManager.RingerMode.ToString());
        }

        private bool OAuthCallbackFunction(string tokenActionID, string token, DateTime expiriedAt)
        {
            //Globals.ShowLog("Значение токена: " + token);
            //Globals.ShowLog("Дата окончания действия: " + expiriedAt.ToString("dd.MM.yy HH:mm:ss"));
            GoogleSheetAPIClass googleSheetAPI;
            switch (tokenActionID)
            {
                case OAUTH_IMPORT_CRITERIA_ACTION:
                    //Запуск в потоке
                    googleSheetAPI = new GoogleSheetAPIClass();
                    ThreadPool.QueueUserWorkItem(o => googleSheetAPI.ImportPhoneNumberCriteriasToTable(token, AppPrivateData.GOOGLE_API_CLIENT_ID));
                    break;
                case OAUTH_UPLOAD_SPAM_LOG_ACTION:
                    //Запуск в потоке
                    ThreadPool.QueueUserWorkItem(o => 
                    {
                        Globals.ShowLog("Старт записи лога спама");
                        CallLogClass callLog = new CallLogClass();
                        //Получаем таблицу лога звонков
                        DataTable callLogTable = callLog.GetSpamCallsTable();
                        callLogTable.Columns.Add("SpamNotes", typeof(string));
                        callLogTable.Columns.Add("PhoneNumberFormatted", typeof(string));
                        callLogTable.Columns.Add("Criteria", typeof(string));
                        //Добавляем к таблице столбец с названием записи блокировки спама
                        AppDataClass appData = new AppDataClass();
                        DataTable criteriasTable = appData.GetPhoneNumberCriteriasAsDataTable();



                        for (int iRow = callLogTable.Rows.Count - 1; iRow >= 0; iRow--)
                        {
                            DataRow dataRow = callLogTable.Rows[iRow];
                            //string foundNotes = (from criteriaRow in criteriasTable.AsEnumerable()
                            //                     where Regex.IsMatch(dataRow.Field<string>("PhoneNumber"), criteriaRow.Field<string>("Criteria"))
                            //                     select criteriaRow.Field<string>("Notes")).FirstOrDefault();

                            DataRow foundCriteriaRow = (from criteriaRow in criteriasTable.AsEnumerable()
                                                        where Regex.IsMatch(dataRow.Field<string>("PhoneNumber"), criteriaRow.Field<string>("Criteria"))
                                                        select criteriaRow).FirstOrDefault();

                            //Если найден критерий спама, то записываем в столбец примечаний, если не найден, то строку удаляем
                            if (foundCriteriaRow != null)
                            {
                                dataRow["SpamNotes"] = foundCriteriaRow.Field<string>("Notes");
                                //Форматируем телефонный номер
                                dataRow["PhoneNumberFormatted"] = CallAppGlobalParameters.FormatPhoneNumber(dataRow.Field<string>("PhoneNumber"));
                                dataRow["Criteria"] = foundCriteriaRow.Field<string>("Criteria");
                            }
                            else
                            {
                                dataRow.Delete();
                            }
                        }

                        //Удаляем столбец с неформатированным телефонным номером
                        callLogTable.Columns.Remove("PhoneNumber");

                        //Записываем данные в Google Sheet
                        googleSheetAPI = new GoogleSheetAPIClass();
                        string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + " Spam call log";
                        string spreadsheetID = googleSheetAPI.CreateSpreadsheet(token, AppPrivateData.GOOGLE_API_CLIENT_ID, fileName);
                        string firstSheetName = googleSheetAPI.GetSheetName(token, AppPrivateData.GOOGLE_API_CLIENT_ID, spreadsheetID, 0);
                        //Вставляем строку заголовка
                        List<object[]> headersList = new List<object[]>();
                        headersList.Add(callLogTable.Columns.Cast<DataColumn>().Select(row => row.ColumnName).ToArray());
                        googleSheetAPI.InsertValueRange(token, AppPrivateData.GOOGLE_API_CLIENT_ID, spreadsheetID, headersList, $"{firstSheetName}!A1");
                        //Добавляем таблицу данных
                        var rowsList = (from row in callLogTable.AsEnumerable()
                                                           select row.ItemArray).ToList();
                        string appendedRange = googleSheetAPI.AppendValueRange(token, AppPrivateData.GOOGLE_API_CLIENT_ID, spreadsheetID, rowsList, $"{firstSheetName}!A1");
                        Globals.ShowLog($"Файл '{fileName}' сохранен");
                        Globals.ShowLog($"Измененный диапазон: {appendedRange}");

                        //googleSheetAPI.UploadSpamLog(token, GOOGLE_API_CLIENT_ID, callLogTable);
                    });
                    break;
                default:
                    Globals.ShowLog($"Ошибка алгоритма: отсутствует обработчик действия OAuth '{tokenActionID}'");
                    break;
            }

            return true;
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}