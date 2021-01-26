using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Telecom;
using Android.Views;
using Android.Widget;

namespace CallApp
{
    [Activity(Label = "IncominCallActivity"
        , LaunchMode = Android.Content.PM.LaunchMode.SingleTask //Чтобы, если пользователь во время разовора щекой откроет оповещение и нажмет на него,
        //то не запускался бы новый экземпляр актиности
        )]
    //Атрибуты активности для реализации класса InCallService
    [IntentFilter(actions: new[] { "android.intent.action.DIAL" }, Categories = new[] { "android.intent.category.DEFAULT" })] //Интент интерфейса для набора номера
    [IntentFilter(actions: new[] { "android.intent.action.DIAL" }, Categories = new[] { "android.intent.category.DEFAULT" }, DataScheme = "tel")] //Интент для обработки активных звонков

    public class IncomingCallActivity : Activity
    {
        private ImageButton callButton;
        private ImageButton muteButton;
        //private ImageButton speakerphoneButton;
        private TextView statusTextView;
        private TextView callerDisplayNameTextView;

        private bool isMuted; //Текущее состояние отключение микрофона

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            //SetContentView(Resource.Layout.activity_incoming_call);
            SetContentView(Resource.Layout.activity_call);


            //Сохраняем значение глобальных переменных для управления активностью
            CallAppGlobalParameters.callActivity = this;
            statusTextView = FindViewById<TextView>(Resource.Id.statusTextView);
            callerDisplayNameTextView = FindViewById<TextView>(Resource.Id.callerDisplayNameTextView);


            //Определяем компоненты формы
            callButton = FindViewById<ImageButton>(Resource.Id.callButton);
            callButton.Click += (sender, e) =>
            {
                bool offHookStatus = GetOffHookStatus();
                if (CallAppGlobalParameters.currentCall != null)
                {
                    if (CallAppGlobalParameters.currentCall.State == Android.Telecom.CallState.Ringing & !offHookStatus)
                    {
                        CallAppGlobalParameters.currentCall.Answer(Android.Telecom.VideoProfileState.AudioOnly);
                    }
                    else if (offHookStatus)
                    {
                        AssureDisconnect();
                    }
                }
            };

            muteButton = FindViewById<ImageButton>(Resource.Id.muteButton);
            muteButton.Click += (sender, e) =>
            {
                if (GetOffHookStatus())
                {
                    SetCallMutedState(!isMuted); //Меняем состояние микрофона на противоположное
                }
            };

            //speakerphoneButton = FindViewById<ImageButton>(Resource.Id.speakerphoneButton);
            //speakerphoneButton.Click += (sender, e) =>
            //{
            //    if (GetOffHookStatus())
            //    {
            //        //Недоделано - не работает
            //        //Если трубка снята
            //        //AudioManager audioManager = (AudioManager)Application.Context.GetSystemService(Context.AudioService);
            //        //audioManager.Mode = Mode.Normal;
            //        //audioManager.SpeakerphoneOn = !audioManager.SpeakerphoneOn;
            //        //ShowSpeakerPhoneStatus();
            //        //CallAppGlobalParameters.InCallService.SetAudioRoute((VideoQuality)Route.Speaker);

            //    }
            //};



            //Посе объявления всех переменных отрабатываем изменение статуса звонка при первом запуске активности
            CallStateChangeHandle();
            //Отрисовываем текущее состояние громкой связи
            //ShowSpeakerPhoneStatus();
            //Задаем и визуализируем начальное состояние микрофона - включен
            SetCallMutedState(false);
        }

        private void SetCallMutedState(bool targetState)
        {
            isMuted = targetState;
            if (GetOffHookStatus())
            {
                CallAppGlobalParameters.InCallService.SetMuted(isMuted);
            }
            //Визуализация состояния микрофона
            if (isMuted)
            {
                muteButton.SetImageResource(Resource.Mipmap.baseline_mic_off_black_48);
            }
            else
            {
                muteButton.SetImageResource(Resource.Mipmap.baseline_mic_black_48);
            }
        }

        //private void ShowSpeakerPhoneStatus()
        //{
        //    //Отрисовка статуса громкой связи
        //    //Визуализация состояния громкой связи
        //    AudioManager audioManager = (AudioManager)Application.Context.GetSystemService(Context.AudioService);
        //    audioManager.Mode = Mode.InCall;
        //    if (audioManager.SpeakerphoneOn)
        //    {
        //        speakerphoneButton.SetImageResource(Resource.Mipmap.baseline_volume_up_black_48);
        //    }
        //    else
        //    {
        //        speakerphoneButton.SetImageResource(Resource.Mipmap.baseline_volume_off_black_48);
        //    }
        //}

        private bool GetOffHookStatus()
        {
            //Определение положения - снята ли трубка

            bool offHook; //Если трубка снята, то true, иначе false
            if (CallAppGlobalParameters.currentCall == null)
            {
                offHook = false;
            }
            else
            {
                switch (CallAppGlobalParameters.currentCall.State)
                {
                    case CallState.Ringing: offHook = false; break;
                    case CallState.Disconnected: offHook = false; break;
                    case CallState.New: offHook = false; break;
                    default:
                        offHook = true;
                        break;
                }
            };
            return offHook;
        }

        protected override void OnDestroy()
        {
            //При любом закрытии формы вызываем процедуру гарантированного разъединения
            AssureDisconnect();

            base.OnDestroy();
        }

        public void CallStateChangeHandle()
        {
            //Отработка изменений статусов телефонного звонка
            statusTextView.Text = CallAppGlobalParameters.GetCurrentCallStatusAsString();
            callerDisplayNameTextView.Text = CallAppGlobalParameters.GetCurrentCallerDisplayName();

            //Отрисовка состояния кнопки положения телефонной трубки
            if (GetOffHookStatus())
            {
                //Если трубка снята
                callButton.SetImageResource(Resource.Mipmap.baseline_call_black_48);
            }
            else
            {
                //Если трубка положена
                callButton.SetImageResource(Resource.Mipmap.baseline_call_end_black_48);
            }


        }


        private void AssureDisconnect()
        {
            //Риск - если звонок принят, то метод Reject не работает, нужно вызывать Disconnect
            //Если вызвать Reject и закрыть форму, то телефон останется в режиме вызова, микрофон будет включен, абонент будет все слышать.
            //Поэтому реализуем метод гарантированного разрыва соединения
            if (CallAppGlobalParameters.currentCall != null)
            {
                if (CallAppGlobalParameters.currentCall.State == Android.Telecom.CallState.Ringing)
                {
                    CallAppGlobalParameters.currentCall.Reject(false, null);
                }
                else
                {
                    CallAppGlobalParameters.currentCall.Disconnect();
                }
            }
        }
    }
}