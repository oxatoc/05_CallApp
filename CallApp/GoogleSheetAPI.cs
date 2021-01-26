using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Android.Content.Res;
using Xamarin.Essentials;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using System.Data;
using System.Text.RegularExpressions;
using CommonData;
using System.Security.Cryptography;

namespace CallApp
{
    class GoogleSheetAPIClass
    {
        //private string tokenValue;
        private string applicationName = "SpamFilter"; //только название приложения для сохранения в логах обращения к таблицам Google
        private AppDataClass appDataClass = new AppDataClass();
        //Конструктор класса
        //public GoogleSheetAPIClass(string token)
        //{
        //    tokenValue = token;
        //}

        private SheetsService GetSheetsService(string tokenValue, string clientId)
        {
            ClientSecrets secrets = new ClientSecrets()
            {
                ClientId = clientId
            };

            var token = new TokenResponse { RefreshToken = tokenValue };

            var credentials = new UserCredential(new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = secrets
                }),
                "user",
                token);
            // Create Google Sheets API service.
            var service = new SheetsService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credentials,
                ApplicationName = applicationName
            });
            return service;
        }

        private string GetSheetID(string userPropertyName)
        {
            //Получение ID таблицы Google
            string url = appDataClass.GetUserProperty(userPropertyName);
            if (url == null)
            {
                Globals.ShowLog("Ошибка настроек - не указан url таблицы google с критериями отбора телефонных номеров");
                return null;
            }
            else
            {
                return Regex.Match(url, "(?<=/spreadsheets/d/)([a-zA-Z0-9-_]+)").Value;
            }
        }

        public void ImportPhoneNumberCriteriasToTable(string tokenValue, string clientId)
        {
            Globals.ShowLog("Старт скачивания Google Sheet");

            //Выгружаем весь файл
            SheetsService service = GetSheetsService(tokenValue, clientId);
            SpreadsheetsResource.GetRequest request = service.Spreadsheets.Get(GetSheetID(appDataClass.PHONE_CRITERIA_SHEET_ID_FIELD_NAME));
            request.IncludeGridData = true;//Флаг выгрузки содержания
            try
            {
                Spreadsheet spreadsheet = request.Execute();

                //Если обращение к сервису не вызвало исключения, то удаляем имеющиеся данные
                appDataClass.ClearPhoneNumberCriteriaTable();

                Globals.ShowLog("Старт переноса критериев в базу данных смартфона");

                Sheet regExSsheet = spreadsheet.Sheets[0];//Читаем данные всегда из первого листа



                if (regExSsheet.Data.Count > 0)
                {
                    GridData gridData = regExSsheet.Data[0];
                    object[] itemArray = new object[3];
                    //Начинаем читать данные со 2-й строки, пропускаем строку заголовка
                    for (int rowDataItem = 1; rowDataItem < gridData.RowData.Count; rowDataItem++)
                    {
                        //Читаем данные из 3-х первых столбцов таблицы
                        for (int colItem = 0; colItem < 3; colItem++)
                        {
                            itemArray[colItem] = gridData.RowData[rowDataItem].Values[colItem].UserEnteredValue.StringValue;
                        }

                        appDataClass.AddPhoneNumberCriteriaToTable(itemArray);
                        //Globals.ShowCounter("Импортировано критериев", rowDataItem, -1, 10);
                    }
                    Globals.ShowLog("Импортировано критериев всего: " + (gridData.RowData.Count - 1));
                }
            }
            catch (Exception e)
            {
                Globals.ShowLog(e.Message);
            }
        }

        public string CreateSpreadsheet(string tokenValue, string clientId, string fileName) 
        {
            //Создание файла Google Sheet
            //Возвращаемое значение - ID файла Google Sheet

            SheetsService service = GetSheetsService(tokenValue, clientId);
            Spreadsheet spreadsheet = new Spreadsheet();
            spreadsheet.Properties = new SpreadsheetProperties();
            spreadsheet.Properties.Title = fileName;
            string spreadsheetID = null;
            try
            {
                SpreadsheetsResource.CreateRequest createRequest = service.Spreadsheets.Create(spreadsheet);
                createRequest.Fields = "spreadsheetId";
                spreadsheet = createRequest.Execute();
                spreadsheetID = spreadsheet.SpreadsheetId;
            }
            catch (Exception e)
            {
                Globals.ShowLog(e.Message);
            }
            return spreadsheetID;
        }

        public string GetSheetName(string tokenValue, string clientId, string speadsheetID, int sheetIndex)
        {
            //Получение названия листа с индексом sheetIndex - для корректноо указания диапазона - в какие ячейки вставлять данные
            string sheetName = null;
            SheetsService service = GetSheetsService(tokenValue, clientId);
            SpreadsheetsResource.GetRequest spreadsheetRequest = service.Spreadsheets.Get(speadsheetID);
            try
            {
                Spreadsheet speadsheetData = spreadsheetRequest.Execute();
                sheetName = speadsheetData.Sheets[sheetIndex].Properties.Title;
            }
            catch (Exception e)
            {
                Globals.ShowLog(e.Message);
            }

            return sheetName;

        }

        public string InsertValueRange(string tokenValue, string clientId, string speadsheetID, List<object[]> valuesList, string range)
        {
            //range - первая ячейка диапазона, куда вставлять данные
            //возвращаемое значение - обновленный диапазон

            SheetsService service = GetSheetsService(tokenValue, clientId);
            //List<object[]> list = new List<object[]>();
            //list.Add(objArray);
            ValueRange valueUpdateRange = new ValueRange();
            valueUpdateRange.Values = valuesList.ToArray();
            SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum valueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            SpreadsheetsResource.ValuesResource.UpdateRequest updateRequest = service.Spreadsheets.Values.Update(valueUpdateRange, speadsheetID, range);
            updateRequest.ValueInputOption = valueInputOption;
            try
            {
                UpdateValuesResponse updateValuesResponse = updateRequest.Execute();
                return updateValuesResponse.UpdatedRange;
            }
            catch (Exception e)
            {
                Globals.ShowLog(e.Message);
                return null;
            }
        }

        public string AppendValueRange(string tokenValue, string clientId, string speadsheetID, List<object[]> valuesList, string range)
        {
            //range - первая ячейка диапазона, куда вставлять данные
            //возвращаемое значение - обновленный диапазон
            SheetsService service = GetSheetsService(tokenValue, clientId);
            ValueRange valueUpdateRange = new ValueRange();
            valueUpdateRange.Values = valuesList.ToArray();
            valueUpdateRange.MajorDimension = "ROWS";
            SpreadsheetsResource.ValuesResource.AppendRequest appendRequest = service.Spreadsheets.Values.Append(valueUpdateRange, speadsheetID, range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;

            try
            {
                AppendValuesResponse response = appendRequest.Execute();
                return response.Updates.UpdatedRange;
            }
            catch (Exception e)
            {
                Globals.ShowLog(e.Message);
                return null;
            }

        }

    }
}