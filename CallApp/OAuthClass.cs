using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using CommonData;
using Java.Lang;
using Xamarin.Auth;

//Должны быть установлениы пакеты NuGet:
//-Camarin.Auth

namespace CallApp
{
    class OAuthClass
    {
        //Класс для получения токена авторизации в асинхронном режиме, чтобы не блокировать основной поток пользователя
        //Сценарий работы:
        //Стартовый метод - GetToken
        //В параметрах метода передать функцию выполнения действий с токеном

        public static OAuth2Authenticator auth;
        public delegate bool CallbackEventHandler(string tokenActionID, string token, DateTime expireAt);
        public event CallbackEventHandler Callback;

        private string mTokenActionID;

        //Стартовый метод класса
        public void GetToken(string clientID, string scope, string authorizeURL, Context parentActivityContext, string tokenActionID, Func<string, string, DateTime, bool> callbackFunction)
        {
            //TOKEN_ACTION_ID - идентификатор действий, чтобы знать, для чего получался конкретный токен при обработке токена в функции обратного вызова
            //parentActivityContext - запускаем из контекста родительской активности, чтобы не создавать альтернативный поток выполнения и не устанавливать флаг FLAG_ACTIVITY_NEW_TASK

            mTokenActionID = tokenActionID;

            Callback = new CallbackEventHandler(callbackFunction);

            auth = new OAuth2Authenticator(
                clientId: clientID,
                clientSecret: null,
                scope: scope,
                authorizeUrl: new Uri(authorizeURL),

                //Правила создания redrectUrl:
                //префикс - как в названии пакета, зарегистрированного на console.developers.google.com - "com.companyname.spamfilter" для данного clientId
                //Суффикс - любой
                //Префикс необходимо вписать в параметр DataSchema активности авторизации, суффикс - в параметр DataPath
                redirectUrl: new Uri("com.companyname.spamfilter:/oauth2redirect"), 
                //Должно совпадать с параметрами класса перехвата интента, иначе окно барузера не будет закрываться после авторизации
                accessTokenUrl: new Uri("https://oauth2.googleapis.com/token"),
                null,
                isUsingNativeUI: true
            )
            {
                AllowCancel = true,
            };
            auth.Completed += OnAuthCompleted;
            parentActivityContext.StartActivity(auth.GetUI(parentActivityContext));
        }

        private void OnAuthCompleted(object sender, AuthenticatorCompletedEventArgs authCompletedArgs)
        {
            string token = null;
            DateTime expireAt = DateTime.MinValue;
            DateTime requestDt = DateTime.Now;
            if (authCompletedArgs.IsAuthenticated)
            {
                token = authCompletedArgs.Account.Properties.ContainsKey("access_token")
                ? authCompletedArgs.Account.Properties["access_token"]
                : null;
                var expInString = authCompletedArgs.Account.Properties.ContainsKey("expires_in")
                    ? authCompletedArgs.Account.Properties["expires_in"]
                    : null;

                int expireIn = Convert.ToInt32(expInString);
                expireAt = requestDt.AddSeconds(expireIn);
            }

            //Вызов функции для выполнения действий с токеном
            Callback(mTokenActionID, token, expireAt);
        }
    }

    [Activity(Label = "OAuthActivity")]
    [IntentFilter(actions: new[] { Intent.ActionView },
      Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
      DataSchemes = new[]
      {
                  // First part of the redirect url (Package name)
                  "com.companyname.spamfilter" //Должно совпадать с redirectURL в OAuth2Authenticator, иначе окно барузера не будет закрываться после авторизации
      },
      DataPaths = new[]
      {
                  // Second part of the redirect url (Path)
                  "/oauth2redirect" //Должно совпадать с redirectURL в OAuth2Authenticator, иначе окно барузера не будет закрываться после авторизации
      })]
    public class OAuthActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //Globals.ShowLog("Старт обработки обратного вызова после получения разрешений пользователя");

            //Инициирование события OnAuthCompleted
            global::Android.Net.Uri uri_android = Intent.Data;
            Uri uri_netfx = new Uri(uri_android.ToString());
            OAuthClass.auth?.OnPageLoading(uri_netfx);

            //Возврат в главную активность
            //Если закомментировать, то DuckDuckGo подтармаживает секунд на 10, потом возвращается на пустую страницу и остается висеть окно браузера, приходится переходить вручную
            var intent = new Intent(this, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            StartActivity(intent);

            this.Finish();//Закрытие активности авторизации, для Google активность закрывается и без Finish(), для других сервисов возможны ньюансы

            //return; //Непонятно зачем нужно, взято из примера
        }





    }
}