using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.Accessibility;
using Android.Widget;
using CommonData;

namespace CallApp
{
    class CallNotificationClass
    {
        const int SMALL_ICON_ID = Resource.Drawable.baseline_call_white_18;
        const string NOTIFICATION_TEXT_PREFIX = "Имя: "; //Префикс сообщения в оповещении
        const string NOTIFICATION_SET_UP_WINDOW_CHANNEL_NAME = "Уведомления о звонках"; //название канала, как оно будет отображаться в меню настройки уведомлений приложения
        const string NOTIFICATION_DETAILS_WINDOWS_DESCRIPTION = "Канал уведомлений о звонках"; //Подпись в нижем колонтитуле бледным шрифтом в окне детальной настройки уведомлений
        const string NOTIFICATION_CHANNEL_ID = "Call notification channel"; //Ни на что не влияет
        const int NOTIFICATION_ID = 0; //ID для обращения к одному и тому уведомлению в разных методах класса - уникальный внутри приложения

        private Context context;

        public void NotifyMithMutedRingTone(string blockingCriteriaValue)
        {
            //blockingCriteriaValue - описание критерия, по которому заблокирован звонок

            Context context = Android.App.Application.Context;

            //Отключаем звук звонка
            //DisableRingtone();

            string textMessage = $"Заблокировано по критерию '{blockingCriteriaValue}'\n\r{NOTIFICATION_TEXT_PREFIX} '{CallAppGlobalParameters.GetCurrentCallerDisplayName()}'";

            //Создаем стиль уведомления в виде расширяемого текста
            Notification.BigTextStyle textStyle = new Notification.BigTextStyle();
            textStyle.BigText(textMessage);
            textStyle.SetSummaryText("причина блокировки"); //содержание колонтитула уведомления

            //Создание уведомления
            Notification notification = new Notification.Builder(context, NOTIFICATION_CHANNEL_ID)
                .SetContentTitle("Заблокированный звонок") //Заголовок оповещения жирным шрифтом
                .SetContentText(textMessage)
                .SetStyle(textStyle)
                .SetSmallIcon(SMALL_ICON_ID) //Иконка уведомления
                .Build();
            //Публикация уведомления
            PublushNotification(notification);
        }


        public void NotifyIncomingCall()
        {

            //Создаем интент ожидания - если пользователь тапнет по уведмлению, то откроется полноэкранная активность
            Intent intent = new Intent(Intent.ActionMain, null);
            intent.SetFlags(ActivityFlags.NoUserAction 
                | ActivityFlags.NewTask
                //Закомментровано
                //| ActivityFlags.ClearTop //Два флага вместе: 
                //- если пользователь во время разговора щекой откроет оповещение и нажмет на оповещение
                //, то, вместо создания нового экземпляра активности, отображается только единственный имеющийся экземпляр
                //работает, только если в свойствах активности указать launchMode = singleTask
                );
            intent.SetClass(context, typeof(IncomingCallActivity));

            //Вариант 1 - оповещение с кликом для открытия полноэкранной формы

            //Создание уведомления с кнопками для реагирования пользователем
            //Настройка оповещений вручную:
            //В настройках канала разрешить "Звук и всплывающее окно"
            //В настройках "Дополнительно" назначить звук уведомления - "Без звука"
            //PendingIntentFlags.OneShot - устанавливаем запуск только один раз - для исключения многократного вызова полноэкранной активности
            PendingIntent pendingIntent = PendingIntent.GetActivity(context, 1, intent,flags: PendingIntentFlags.OneShot);

            Notification notification = new Notification.Builder(context, NOTIFICATION_CHANNEL_ID)
                //.SetOngoing(true) //создает постоянно висящее оповещение, которое невозможно скрыть - для событий, требующих немедленной реакции
                //.SetPriority((int)NotificationPriority.High)
                .SetContentIntent(pendingIntent) //устанавливаем интент ожидания действий пользователя
                .SetFullScreenIntent(pendingIntent, true) //устанавливаем полноэкранный интент для включения полноэкранного UI дисплея, когда notification manager будет готов отобразить на полный экран
                .SetContentTitle("Звонок") //Заголовок оповещения жирным шрифтом
                .SetContentText($"{NOTIFICATION_TEXT_PREFIX} '{CallAppGlobalParameters.GetCurrentCallerDisplayName()}'")
                .SetSmallIcon(SMALL_ICON_ID) //Иконка уведомления
                .Build();
            

            //Публикация уведомления
            PublushNotification(notification);

            //Вариант 2 - оповещение, не появляющееся поверх окна, сразу открываем полноэкранную активность
            //Настроить оповещения вручную:
            //Для канала - отключить всплывающее окно и звук оповещения - установить "Без звука"
            //Решение - вариант не удобен - если чатать, например, страницу в браузере, и придет входящий вызов, то сразу вывалится активность на полный экран. Закомментировать.
            //Notification notification = new Notification.Builder(context, NOTIFICATION_CHANNEL_ID)
            //    .SetContentTitle(NOTIFICATION_TITLE) //Заголовок оповещения жирным шрифтом
            //    .SetContentText($"{NOTIFICATION_TEXT_PREFIX} '{CallAppGlobalParameters.GetCurrentCallerDisplayName()}'")
            //    .SetSmallIcon(SMALL_ICON_ID) //Иконка уведомления
            //    .Build();
            //PublushNotification(notification);
            //context.StartActivity(intent);



        }

        private void PublushNotification(Notification notification)
        {
            NotificationManager notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);

            //Настройка звука и всплывающего оповещения
            //Программно - не работает, только вручную, в настройках приложения:
            //1) включить появление всплывающего окна - иначе не будет показываться пуш-уведомление, некуда будет тапать, полноэкранная активность не откроется
            //2) назначить звук уведомления "без звука" - чтобы звонил только рингтон
            //Уровень важности - ни на что не влияет, звук и всплывание окна определяется только ручными настройками

            //Создаем канал уведомления
            NotificationImportance notificationImportance = NotificationImportance.High;//Есть подозрение:
            //чтобы, при выключенном экране при поступлении входящего звонка отображалось не уведомление, а сразу запускалась полноэкранная активность ответа на звонок,
            //важность уведомления должна быть "High", а никакая другая, включая "Max"
            //NotificationImportance notificationImportance = NotificationImportance.Low; //По результатам гуглинга приоритет Low не имеет звука оповещения
            NotificationChannel channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, NOTIFICATION_SET_UP_WINDOW_CHANNEL_NAME, notificationImportance)//, NotificationImportance.Default)
            {
                Description = NOTIFICATION_DETAILS_WINDOWS_DESCRIPTION
            };
            //Установка звука уведомления - до вызова создания канала. Звук должен быть для уведомлений с важностью Default и выше
            channel.SetSound(null, null); //Если в настройках приложения звук уведомления включен, то эта функция звук отключает
            notificationManager.CreateNotificationChannel(channel);

            //Публикация уведомления
            notificationManager.Notify(NOTIFICATION_ID, notification);

        }

        public void DisableRingtone()
        {
            AudioManager audioManager = (AudioManager)context.GetSystemService(Context.AudioService);
            try
            {
                audioManager.RingerMode = RingerMode.Vibrate;
            }
            catch
            {

            }
            //audioManager.RingerMode = RingerMode.Silent;
        }

        public void EnableRingtone()
        {
            AudioManager audioManager = (AudioManager)context.GetSystemService(Context.AudioService);
            try
            {
                audioManager.RingerMode = RingerMode.Normal;
                
            }
            catch
            {

            }
        }

        public void RemoveCallNotification()
        {
            //Удаляем оповещение о звонке
            
            NotificationManager notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);
            notificationManager.Cancel(NOTIFICATION_ID);
            
        }

        public CallNotificationClass()
        {
            //Устанавливаем значения по умолчанию, чтобы каждый раз не запускать один и тот же код
            context = Android.App.Application.Context;
        }


    }
}