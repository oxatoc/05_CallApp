using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Data;
using CommonData;
//Для использования XElement добавить в проект ссылку на сборку System.Xml.Linq



namespace CallApp
{
    class AppDataClass
    {
        //Класс методов работы со всеми данными приложения
        private string phoneNumberCriteriaTablePath = "";
        private XElement phoneNumberCriteriaTable;

        //Настройка таблицы данных приложения
        private string appDataTablePath = ""; //Файл данных приложения
        private XElement appDataTable;

        public string PHONE_CRITERIA_SHEET_ID_FIELD_NAME = "PhoneCriteriaSheetID"; //Поле в базе данных, хранящее ID файла Excel с критериями фильтрации телефонных номеров

        public DataTable GetPhoneNumberCriteriasAsDataTable()
        {
            DataTable criteriasTable = new DataTable("CriteriasTable");
            criteriasTable.Columns.Add("Criteria", Type.GetType("System.String"));
            criteriasTable.Columns.Add("Notes", Type.GetType("System.String"));
            criteriasTable.Columns.Add("ImportDate", Type.GetType("System.DateTime"));

            XElement criteriaXElements = XElement.Load(phoneNumberCriteriaTablePath);

            foreach (var element in criteriaXElements.Descendants("Row"))
            {
                DataRow newRow = criteriasTable.NewRow();
                newRow["Criteria"] = element.Attribute("Criteria").Value;
                newRow["Notes"] = element.Attribute("Notes").Value;
                newRow["ImportDate"] = DateTime.Parse(element.Attribute("ImportDate").Value);
                criteriasTable.Rows.Add(newRow);
            }

            return criteriasTable;
        }

        public string GetPhoneNumberCriteriaTableContent()
        {
            //Отображение содержимого таблицы для пользователя
            phoneNumberCriteriaTable = XElement.Load(phoneNumberCriteriaTablePath);
            var query = from element in phoneNumberCriteriaTable.Descendants("Row") //Ищем только элементы с LocalName = "Row"
                        orderby element.Attribute("Notes").Value ascending, element.Attribute("Criteria").Value ascending
                        select new
                        {
                            criteria = element.Attribute("Criteria").Value,
                            notes = element.Attribute("Notes").Value,
                            importDate = element.Attribute("ImportDate").Value
                        };
            string str = "\r\n";
            foreach (var item in query)
            {
                str += $"{item.notes}: {item.criteria}\r\n";
            }
            return str;
        }

        public XElement CheckPhoneNumberCriteria(string phoneNumber)
        {
            IEnumerable<XElement> regExQuery = from element in phoneNumberCriteriaTable.Descendants("Row")
                                               where Regex.IsMatch(phoneNumber, element.Attribute("Criteria").Value)
                                               select element;

            //Из БД возвращаем данные критерия, которому соответствует номер телефона
            //Возвращаем результат проверки в виде объекта XElement - чтобы можно было отразить, по какому критерию заблокирован звонок
            return regExQuery.FirstOrDefault();
        }

        public void AddPhoneNumberCriteriaToTable(object[] criteriaItemArray)
        {
            //Добавление новой записи критерия телефонного номера в таблицу
            //Создаем новый элемент
            DateTime dt = DateTime.Now;
            XElement newRow = new XElement(
                    "Row",
                        new XAttribute(
                            "Criteria",
                            criteriaItemArray[0]
                        ),
                        new XAttribute(
                            "Notes",
                            criteriaItemArray[1]
                        ),
                        new XAttribute(
                            "ImportDate",
                            dt.ToString("dd.MM.yy HH:mm:ss")
                        )
                    );
            //Globals.AddTextToLogControl(newRow.ToString());
            phoneNumberCriteriaTable.Add(newRow);
            phoneNumberCriteriaTable.Save(phoneNumberCriteriaTablePath);
        }

        public void ClearPhoneNumberCriteriaTable()
        {
            Globals.ShowLog("Сарт очистки содержимого базы данных критериев телефонных номеров");

            phoneNumberCriteriaTable = new XElement(
                    "Table", null);
            phoneNumberCriteriaTable.Save(phoneNumberCriteriaTablePath);
        }

        public AppDataClass() //Конструктор класса
        {
            string basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            phoneNumberCriteriaTablePath = Path.Combine(basePath, "PhoneNumberCriteriaTable.xml");

            //Создание файла таблицы критериев телефоных номеров
            if (!File.Exists(phoneNumberCriteriaTablePath))
            {
                ClearPhoneNumberCriteriaTable();
            }
            //Загрузка таблицы в оперативную память чтобы каждый раз не считывать с диска
            phoneNumberCriteriaTable = XElement.Load(phoneNumberCriteriaTablePath);

            //Создание файла таблицы данных приложения
            appDataTablePath = Path.Combine(basePath, "AppDataTable.xml");
            //Создание файла таблицы критериев телефоных номеров
            if (!File.Exists(appDataTablePath))
            {
                appDataTable = new XElement(
                    "Table", null);
                appDataTable.Save(appDataTablePath);
            }

            //Загрузка таблицы в оперативныю память, чтобы каждый раз не считывать с диска
            appDataTable = XElement.Load(appDataTablePath);
        }

        public string GetUserProperty(string propertyName)
        {
            IEnumerable<string> userPropertyQuery = from element in appDataTable.Descendants(propertyName)
                                                    //where element.Name.LocalName == propertyName
                                                    select element.Value;
            return userPropertyQuery.FirstOrDefault();
        }

        private void SetUserProperty(string propertyName, string propertyValue)
        {
            //Если такого свойства нет, то добавляем
            if (GetUserProperty(propertyName) == null)
            {
                appDataTable.Add(new XElement(propertyName, propertyValue));
            }
            else
            {
                //Получаем ссылку на узел
                IEnumerable<XElement> userPropertyElementQuery = from element in appDataTable.Descendants(propertyName)
                                                        //where element.Name.LocalName == propertyName
                                                        select element;
                //Сохраняем значение
                userPropertyElementQuery.First().Value = propertyValue;
            }
            appDataTable.Save(appDataTablePath);
        }

        public string PhoneCriteriaSheetID
        {
            //Чтение из базы данных ID таблицы с критериями фильтрации телефонных номеров
            get
            {
                if (GetUserProperty(PHONE_CRITERIA_SHEET_ID_FIELD_NAME) == null)
                {
                    return null;
                }
                else
                {
                    
                    return GetUserProperty(PHONE_CRITERIA_SHEET_ID_FIELD_NAME);
                }
            }
            set
            {
                SetUserProperty(PHONE_CRITERIA_SHEET_ID_FIELD_NAME, value);
            }
        }

    }
}