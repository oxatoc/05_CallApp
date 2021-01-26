using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Telecom;
using Android.Views;
using Android.Widget;

namespace CallApp
{
    static class CallAppGlobalParameters
    {
        //Статический класс для хранения глобальных параметров приложения
        public static Call currentCall; //переменная для хранения объекта текущего звонка

        //public static TextView statusTextView; //Переменная для хранения ссылки на поле статуса текущей активности для обновления при изменении состояния звонка

        public static IncomingCallActivity callActivity; //Переменная для хранения ссылки на активность, чтобы закрывать активность при внешнем инициировании разъединения вызова

        public static InCallService InCallService; //переменная для хранения объекта InCallService

        public static string GetCurrentCallPhoneNumber() //Отображение телефонного номера текущего звонка
        {
            if (currentCall != null)
            {
                return currentCall.GetDetails().GetHandle().SchemeSpecificPart;
            }
            else
            {
                return null;
            }

        }

        public static string GetCurrentCallerDisplayName()
        {
            if (currentCall != null)
            {
                string callerDisplayName = FormatPhoneNumber(GetCurrentCallPhoneNumber());
                Android.Net.Uri uri = Android.Net.Uri.WithAppendedPath(ContactsContract.PhoneLookup.ContentFilterUri, Android.Net.Uri.Encode(callerDisplayName));
                var cursor = Android.App.Application.Context.ContentResolver.Query(uri, null, null, null);
                if (cursor != null)
                {
                    if (cursor.MoveToFirst())
                    {
                        callerDisplayName = cursor.GetString(cursor.GetColumnIndex(ContactsContract.ContactsColumns.DisplayName));
                    }
                    cursor.Close();
                }
                return callerDisplayName;
            }
            else
            {
                return null;
            }


        }

        public static string GetCurrentCallStatusAsString()
        {
            if (currentCall != null)
            {
                switch (currentCall.State)
                {
                    case Android.Telecom.CallState.Active: return "Разговор";
                    case Android.Telecom.CallState.Connecting: return "Соединение";
                    case Android.Telecom.CallState.Dialing: return "Соединение";
                    case Android.Telecom.CallState.Disconnected: return "Разъединено";
                    case Android.Telecom.CallState.Disconnecting: return "Разъединение";
                    case Android.Telecom.CallState.Holding: return "На удержании";
                    case Android.Telecom.CallState.Ringing: return "Входящий звонок";
                    default:
                        return "Состояние звонка не определено";
                }
            }
            else
            {
                return "На социальной дистанции";
            }
        }

        public static string FormatPhoneNumber(string phoneNumber)
        {
            //Приведение номера талефона к формату N (NNN) NNN-NN-NN
            GroupCollection groups = Regex.Match(phoneNumber, "(.*)([0-9]{10}$)").Groups;
            if (groups.Count() == 3)
            {
                string countryCode = groups[1].Value;
                string localNumber = groups[2].Value;
                return $"{countryCode} ({localNumber.Substring(0, 3)}) {localNumber.Substring(3, 3)}-{localNumber.Substring(6, 2)}-{localNumber.Substring(8, 2)}";

            }
            else
            {
                //Короткие номера типа '900' не приводятся к стандартному формату, возвращаем как есть
                return phoneNumber;

            }
        }
    }
}