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

namespace CallApp
{
    [Activity(Label = "SettingsActivity")]
    public class SettingsActivity : Activity
    {
        private AppDataClass appDataClass = new AppDataClass();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.SettingsLayout);

            EditText phoneCriteriaSheetIDEditText = FindViewById<EditText>(Resource.Id.phoneCriteriaSheetIDEditText);
            phoneCriteriaSheetIDEditText.Text = appDataClass.PhoneCriteriaSheetID;

            Button savePhoneCriteriaSheetIDButton = FindViewById<Button>(Resource.Id.savePhoneCriteriaSheetIDButton);
            savePhoneCriteriaSheetIDButton.Click += (sender, e) =>
            {
                appDataClass.PhoneCriteriaSheetID = phoneCriteriaSheetIDEditText.Text;
            };
        }

        protected override void OnDestroy()
        {
            //Globals.AddTextToLogControl("Отладка: активность уничтожена");
            base.OnDestroy();
        }
    }
}