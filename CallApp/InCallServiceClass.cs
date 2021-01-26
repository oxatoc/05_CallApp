using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Telecom;
using Android.Views;
using Android.Widget;
using CommonData;
using Java.Lang;

namespace CallApp
{
    [Service(Name = "com.companyname.callapp.InCallServiceImplementation", Permission = "android.permission.BIND_INCALL_SERVICE")]
    [MetaData(name: "android.telecom.IN_CALL_SERVICE_UI", Value ="true")] //Индикатор что приложение может подменять встроенный интерфес пользователя
    [MetaData(name: "android.telecom.IN_CALL_SERVICE_RINGING", Value = "true")] //индикатор что приложение ответственно за воспроизведение рингтона
    [IntentFilter(new[] { "android.telecom.InCallService" })]
    class InCallServiceClass : InCallService
    {
        public override void OnCallAdded(Call call)
        {
            base.OnCallAdded(call);

            //Сохраняем объект активного вызова в глобальной переменной
            CallAppGlobalParameters.currentCall = call;

            //Т.к. код, бывает, подтормаживает когда экран смартфона выключен, то сразу устанавилваем беззвучный режим
            CallNotificationClass callNotification = new CallNotificationClass();
            callNotification.DisableRingtone();

            //Регистрируем метод обратного вызова для отслеживания внешних изменений состояния вызова
            call.RegisterCallback(callCallback);

            //Сохраняем объект сервиса в глобальной переменной для вызова на последующих шагах
            CallAppGlobalParameters.InCallService = this;

            //Запускаем процедуру обратного вызова - для отработки первого изменения состояния звонка
            callCallback.OnStateChanged(call, call.State);

            //Globals.ShowLog("---Отладка: отображение оповещений отключено");
            //return;

            string phoneNumber = CallAppGlobalParameters.GetCurrentCallPhoneNumber();

            //Проверяем попадание номера телефона в blacklist
            AppDataClass appData = new AppDataClass();
            XElement criteriaRow = appData.CheckPhoneNumberCriteria(phoneNumber);

            if (criteriaRow != null)
            {
                //Если результат проверки содержит критерий, по которому проверка не пройдена, то отображаем оповещение с отключенным рингтоном
                callNotification.NotifyMithMutedRingTone(criteriaRow.Attribute("Notes").Value);//Если номер заблокирован, то отображаем оповещение с отключенным рингтоном
            }
            else
            {
                //Включаем нормальный режим звонка
                callNotification.EnableRingtone();
                callNotification.NotifyIncomingCall();//Отображение интерфеса пользователя для приема звонков
            }
        }

        public override void OnCallRemoved(Call call)
        {
            base.OnCallRemoved(call);
            
            CallNotificationClass callNotification = new CallNotificationClass();
            callNotification.EnableRingtone();//Восстанавливаем нормальный режим звонка
            callNotification.RemoveCallNotification(); //Удаляем уведомление о звонке
            CallAppGlobalParameters.currentCall = null; //Удаляем объект текущего звонка
            if (CallAppGlobalParameters.callActivity != null) //Если активность обработки звонка была открыта, то закрываем
            {
                CallAppGlobalParameters.callActivity.Finish();
            }
        }

        private CallCallbackClass callCallback = new CallCallbackClass(); //Функция обратно вызова для отслеживания изменений состояния звонка и соответствующй корректировки поведения активности
    }

    //Класс для описания переопределенного метода обратного вызова
    class CallCallbackClass: Call.Callback
    {
        public override void OnStateChanged(Call call, [GeneratedEnum] CallState state)
        {
            base.OnStateChanged(call, state);

            if (CallAppGlobalParameters.callActivity != null)
            {
                CallAppGlobalParameters.callActivity.CallStateChangeHandle();
            }
        }
    }
}