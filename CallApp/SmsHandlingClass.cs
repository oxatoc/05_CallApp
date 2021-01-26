using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Android.App;
using Android.Content;  
using Android.OS;
//using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using CommonData;

namespace CallApp
{
    //------Интерфейс для работы с sms
    interface ISmsHandling
    {
        bool IsDefaultApplication { get; } //Проверка, установлено ли приложение как приложение по умолчанию - иначе не будет обрабатывать входящие sms
        string SmsListGoogelSheetUrl { get;  set; } //Сохранение ссылки на таблицу Google Sheet куда выгружать лог sms
        string SmsListGoogleSheetID { get;  } //Извлечение ID файла Google для передачи в класс выгрузки данных в таблицу Google Sheet
    }
    class SmsHandlingClass : ISmsHandling
    {
        Context ContextValue;
        string SmsHandlingPropertiesXmlFilePath; //Путь к файлу сохраненных настроек обработчика sms
        string SmsListGoogleSheetIDPropertyName = "SmsListGoogleSheetID"; //Название элемента XML файла, в котором хранится путь к таблице Google для загрузки лога sms

        //Конструктор класса
        public SmsHandlingClass(Context context)
        {
            ContextValue = context;
            SmsHandlingPropertiesXmlFilePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "SmsHandlingProperties.xml");

            //Создание файла свойств приложения
            if (!File.Exists(SmsHandlingPropertiesXmlFilePath))
            {
                XElement SmsHandlingPropertiesXml = new XElement("Table", null);
                SmsHandlingPropertiesXml.Save(SmsHandlingPropertiesXmlFilePath);
            }
        }

        public bool IsDefaultApplication
        {
            get
            {
                return Android.Provider.Telephony.Sms.GetDefaultSmsPackage(ContextValue).Equals(ContextValue.PackageName);
            }
        }

        public string SmsListGoogelSheetUrl 
        {
            get 
            {
                //Чтение значения из файла
                XElement smsHandlingProperties = XElement.Load(SmsHandlingPropertiesXmlFilePath);
                IEnumerable<XElement> userPropertyQuery = from element in smsHandlingProperties.Descendants(SmsListGoogleSheetIDPropertyName)
                                                              //where element.Name.LocalName == propertyName
                                                          select element;
                if (userPropertyQuery.FirstOrDefault() == null)
                {
                    //Если такой элемент отсутствует, то возвращаем пусую строку
                    return string.Empty;
                }
                else
                {
                    return userPropertyQuery.First().Value;
                }
            }
            set 
            {
                //Сохранение ссылки в локальном файле
                XElement smsHandlingProperties = XElement.Load(SmsHandlingPropertiesXmlFilePath);
                IEnumerable<XElement> userPropertyQuery = from element in smsHandlingProperties.Descendants(SmsListGoogleSheetIDPropertyName)
                                                              //where element.Name.LocalName == propertyName
                                                          select element;
                if (userPropertyQuery.FirstOrDefault() == null)
                {
                    //Если такой элемент отсутствует, то создаем
                    smsHandlingProperties.Add(new XElement(SmsHandlingPropertiesXmlFilePath, value));
                }
                else
                {
                    //Сохраняем значение
                    userPropertyQuery.First().Value = value;
                }
           } 
        }

        public string SmsListGoogleSheetID
        {
            get
            {
                //Получение ID таблицы Google
                string url = SmsListGoogelSheetUrl;
                if (url.Length == 0)
                {
                    Globals.ShowLog("Ошибка настроек - не указан url таблицы google для сохранения лога sms");
                    return null;
                }
                else
                {
                    return Regex.Match(url, "(?<=/spreadsheets/d/)([a-zA-Z0-9-_]+)").Value;
                }
            }
        }
    }


    //------Интерфейс к XML-файлу с данными sms
    interface ISmsItems
    {
        void AddSmsItem(ISmsItem smsItem);
        IEnumerable<ISmsItem> smsItems { get; } //Все смс, находящиеся файле данных
        void ClearSmsItems(); //Удаление всех Sms из файла данных
        List<object[]> SmsItemsAsIList { get; } //Получаем перечень всех sms в формате интерфейса IList, пригодном для вставки данных в файл GoogleSheet
    }
    //Класс реализации интерфейса
    class SmsItemsClass : ISmsItems
    {
        string SmsItemsXElementPath;
        
        //Конструктор класса
        public SmsItemsClass()
        {
            //Задаем путь к файлу данных
            string basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            SmsItemsXElementPath = Path.Combine(basePath, "PhoneNumberCriteriaTable.xml");
        }


        public IEnumerable<ISmsItem> smsItems
        {
            get
            {
                XElement smsItems = XElement.Load(SmsItemsXElementPath);
                return from smsItem in smsItems.Descendants("Row")
                       select new SMSItemFromXMLClass(smsItem);
            }
        }

        public List<object[]> SmsItemsAsIList
        {
            get
            {
                XElement smsItems = XElement.Load(SmsItemsXElementPath);
                var elementsQuery = from element in smsItems.Descendants("Row")
                                    let smsItem = new SMSItemFromXMLClass(element)
                                    orderby smsItem.SmsDate ascending, smsItem.OriginatingAddress ascending, smsItem.DestinationAddress
                                    let objectsArray = new object[] 
                                    { 
                                        smsItem.SmsDate
                                        , smsItem.DestinationAddress
                                        , smsItem.OriginatingAddress
                                        , smsItem.PDU
                                    }
                                    select objectsArray;

                List<object[]> objectsList = new List<object[]>();

                foreach (var objectArray in elementsQuery)
                {
                    objectsList.Add(objectArray);
                }

                return objectsList;
            }
        }

        public void AddSmsItem(ISmsItem smsItem)
        {
            XElement smsItems = XElement.Load(SmsItemsXElementPath);
            smsItems.Add(smsItem.XElementValue);
            smsItems.Save(SmsItemsXElementPath);
        }

        public void ClearSmsItems()
        {
            XElement smsItems = new XElement("Table", null);
            smsItems.Save(SmsItemsXElementPath);
        }
    }


    //------Интерфейс единичной sms - входящей и исходящей
    interface ISmsItem
    {
        DateTime SmsDate { get; } //Метка времени когда было получено/создано Sms
        string OriginatingAddress { get; } //Адрес отправителя
        string DestinationAddress { get; } //Адрес получателя
        byte[] PDU { get; }
        XElement XElementValue { get; } //Исходный элемент XML для добавления в общую структуру элементов
    }
    //Базовый класс реализации интерфейса
    class SmsItemParentClass : ISmsItem
    {
        protected XElement SmsItemElement;

        //Реализация интерфейса
        public virtual DateTime SmsDate => throw new NotImplementedException();

        public virtual string OriginatingAddress => throw new NotImplementedException();

        public virtual string DestinationAddress => throw new NotImplementedException();

        public virtual byte[] PDU => throw new NotImplementedException();

        public XElement XElementValue => SmsItemElement;
    }
    //Класс-наследник - создание элемента из элементов файла XML
    class SMSItemFromXMLClass : SmsItemParentClass
    {

        //Конструктор класса
        public SMSItemFromXMLClass(XElement xElement)
        {
            SmsItemElement = xElement;
        }

        //Переопределяемые функции
        public override DateTime SmsDate => DateTime.Parse(SmsItemElement.Attribute("SmsDate").Value);
        public override string DestinationAddress => SmsItemElement.Attribute("DestinationAddress").Value;
        public override string OriginatingAddress => SmsItemElement.Attribute("OriginatingAddress").Value;
        public override byte[] PDU
        {
            get
            {
                string bytesAsString = SmsItemElement.Attribute("PDU").Value;
                //Преобразовываем в массив байтов
                List<byte> bytesList = new List<byte>();

                string byteAsStr = string.Empty;

                for (int iChar = 0; iChar < bytesAsString.Length; iChar++)
                {
                    byteAsStr += bytesAsString[iChar];
                    Math.DivRem(iChar + 1, 2, out int rem);
                    if (rem == 0)
                    {
                        byte convertedByte = Convert.ToByte(byteAsStr, 16);
                        //Globals.ShowLog(convertedByte.ToString());
                        bytesList.Add(convertedByte);

                        byteAsStr = string.Empty;
                    }
                }
                return bytesList.ToArray();
            }
        }
    }
    //Класс-наследник - создание элемента из набора параметров
    class SmsItemFromParemeters : SmsItemParentClass
    {
        //конструктор класса
        public SmsItemFromParemeters(DateTime smsDate, string destinationAddress, string originatingAddress, byte[] pdus)
        {
            //Преобразовываем массив байтов в строу для сохранения в элементе XML
            string bytesAsString = BitConverter.ToString(pdus).Replace("-", "");


            SmsItemElement = new XElement("Row", 
                new XAttribute("SmsDate", smsDate.ToString("dd.MM.yy HH:mm:ss"))
                , new XAttribute("DestinationAddress", destinationAddress)
                , new XAttribute("OriginatingAddress", originatingAddress)
                , new XAttribute("PDU", bytesAsString)
                );
        }
    }


    //------Стандартные классы данные, которые должны быть реализованя для корректной работы приложения

    //Должны быть установлены все разрешения: 
    //BROADCAST_SMS
    //BROADCAST_WAP_PUSH
    //RECEIVE_WAP_PUSH
    //SEND_SMS
    //RECEIVE_SMS
    //READ_SMS
    //WRITE_SMS
    //RECEIVE_SMS
    //SEND_RESPOND_VIA_MESSAGE


    //BroadcastRceiver для получения смс
    [BroadcastReceiver(Enabled = true, Exported = true, Permission = "android.permission.BROADCAST_SMS")]
    [IntentFilter(new[] { Android.Provider.Telephony.Sms.Intents.SmsDeliverAction })] 
    public class SmsBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {

            //Создаем новый объект SMS
            DateTime smsDate = DateTime.Now;
            string destinationAddress = "test destination";
            string originationgAddress = "test origination";
            ISmsItem smsItem = new SmsItemFromParemeters(
                smsDate
                , destinationAddress
                , originationgAddress
                , intent.Extras.GetByteArray("pdus"));

            //Сохраняем элемент в файле
            ISmsItems smsItems = new SmsItemsClass();
            smsItems.AddSmsItem(smsItem);

            Globals.ShowLog("SMS received");
        }
    }

    //BroadcastReceiver для получения ММС
    [BroadcastReceiver(Enabled = true, Exported = true, Permission = "android.permission.BROADCAST_WAP_PUSH")]
    [IntentFilter(new[] 
    { 
        Android.Provider.Telephony.Sms.Intents.WapPushDeliverAction 
    }
    , DataMimeType = "application/vnd.wap.mms-message"
        )] //Для приема MMS
    public class MmsBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Globals.ShowLog("MMS received");
        }
    }


    //Активность для получения sms от других приложений
    [Activity(Label = "ComposeSmsActivity")]
    [IntentFilter(
        actions: (new[] 
        { 
            Intent.ActionSend, 
            Intent.ActionSendto 
        })
        , Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }
        , DataSchemes = new[] { "sms", "smsto", "mms", "mmsto" }
        )]
    public class ComposeSmsActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

        }
    }

    //Сервис для ответа на входящие звонки мгновенными сообщениями типа "перезвоните позже"
    [Service(Exported = true, Enabled = true, Permission = "android.permission.SEND_RESPOND_VIA_MESSAGE")]
    [IntentFilter(
        actions: new[] { Android.Telephony.TelephonyManager.ActionRespondViaMessage }
        , Categories = new[] { Intent.CategoryDefault }
        , DataSchemes = new[] { "sms", "smsto", "mms", "mmsto" }
    )]
    class QuickResponseServiceClass : Service
    {
        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            return base.OnStartCommand(intent, flags, startId);
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
    }

}