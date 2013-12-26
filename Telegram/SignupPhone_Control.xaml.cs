using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Shell;
using Telegram.Model;

namespace Telegram {
    public partial class SignupPhone_Control : UserControl {
        private List<CountryModel> countries = new List<CountryModel>() {
            new CountryModel() {Name = "Afghanistan", Ext = "93"},
            new CountryModel() {Name = "Albania", Ext = "355"},
            new CountryModel() {Name = "Algeria", Ext = "213"},
            new CountryModel() {Name = "American Samoa", Ext = "1 684"},
            new CountryModel() {Name = "Andorra", Ext = "376"},
            new CountryModel() {Name = "Angola", Ext = "244"},
            new CountryModel() {Name = "Anguilla", Ext = "1 264"},
            new CountryModel() {Name = "Antarctica", Ext = "672"},
            new CountryModel() {Name = "Antigua and Barbuda", Ext = "1 268"},
            new CountryModel() {Name = "Argentina", Ext = "54"},
            new CountryModel() {Name = "Armenia", Ext = "374"},
            new CountryModel() {Name = "Aruba", Ext = "297"},
            new CountryModel() {Name = "Australia", Ext = "61"},
            new CountryModel() {Name = "Austria", Ext = "43"},
            new CountryModel() {Name = "Azerbaijan", Ext = "994"},
            new CountryModel() {Name = "Bahamas", Ext = "1 242"},
            new CountryModel() {Name = "Bahrain", Ext = "973"},
            new CountryModel() {Name = "Bangladesh", Ext = "880"},
            new CountryModel() {Name = "Barbados", Ext = "1 246"},
            new CountryModel() {Name = "Belarus", Ext = "375"},
            new CountryModel() {Name = "Belgium", Ext = "32"},
            new CountryModel() {Name = "Belize", Ext = "501"},
            new CountryModel() {Name = "Benin", Ext = "229"},
            new CountryModel() {Name = "Bermuda", Ext = "1 441"},
            new CountryModel() {Name = "Bhutan", Ext = "975"},
            new CountryModel() {Name = "Bolivia", Ext = "591"},
            new CountryModel() {Name = "Bosnia and Herzegovina", Ext = "387"},
            new CountryModel() {Name = "Botswana", Ext = "267"},
            new CountryModel() {Name = "Brazil", Ext = "55"},
            new CountryModel() {Name = "British Indian Ocean Territory", Ext = ""},
            new CountryModel() {Name = "British Virgin Islands", Ext = "1 284"},
            new CountryModel() {Name = "Brunei", Ext = "673"},
            new CountryModel() {Name = "Bulgaria", Ext = "359"},
            new CountryModel() {Name = "Burkina Faso", Ext = "226"},
            new CountryModel() {Name = "Burma (Myanmar)", Ext = "95"},
            new CountryModel() {Name = "Burundi", Ext = "257"},
            new CountryModel() {Name = "Cambodia", Ext = "855"},
            new CountryModel() {Name = "Cameroon", Ext = "237"},
            new CountryModel() {Name = "Canada", Ext = "1"},
            new CountryModel() {Name = "Cape Verde", Ext = "238"},
            new CountryModel() {Name = "Cayman Islands", Ext = "1 345"},
            new CountryModel() {Name = "Central African Republic", Ext = "236"},
            new CountryModel() {Name = "Chad", Ext = "235"},
            new CountryModel() {Name = "Chile", Ext = "56"},
            new CountryModel() {Name = "China", Ext = "86"},
            new CountryModel() {Name = "Christmas Island", Ext = "61"},
            new CountryModel() {Name = "Cocos (Keeling) Islands", Ext = "61"},
            new CountryModel() {Name = "Colombia", Ext = "57"},
            new CountryModel() {Name = "Comoros", Ext = "269"},
            new CountryModel() {Name = "Republic of the Congo", Ext = "242"},
            new CountryModel() {Name = "Democratic Republic of the Congo", Ext = "243"},
            new CountryModel() {Name = "Cook Islands", Ext = "682"},
            new CountryModel() {Name = "Costa Rica", Ext = "506"},
            new CountryModel() {Name = "Croatia", Ext = "385"},
            new CountryModel() {Name = "Cuba", Ext = "53"},
            new CountryModel() {Name = "Cyprus", Ext = "357"},
            new CountryModel() {Name = "Czech Republic", Ext = "420"},
            new CountryModel() {Name = "Denmark", Ext = "45"},
            new CountryModel() {Name = "Djibouti", Ext = "253"},
            new CountryModel() {Name = "Dominica", Ext = "1 767"},
            new CountryModel() {Name = "Dominican Republic", Ext = "1 809"},
            new CountryModel() {Name = "Timor-Leste", Ext = "670"},
            new CountryModel() {Name = "Ecuador", Ext = "593"},
            new CountryModel() {Name = "Egypt", Ext = "20"},
            new CountryModel() {Name = "El Salvador", Ext = "503"},
            new CountryModel() {Name = "Equatorial Guinea", Ext = "240"},
            new CountryModel() {Name = "Eritrea", Ext = "291"},
            new CountryModel() {Name = "Estonia", Ext = "372"},
            new CountryModel() {Name = "Ethiopia", Ext = "251"},
            new CountryModel() {Name = "Falkland Islands", Ext = "500"},
            new CountryModel() {Name = "Faroe Islands", Ext = "298"},
            new CountryModel() {Name = "Fiji", Ext = "679"},
            new CountryModel() {Name = "Finland", Ext = "358"},
            new CountryModel() {Name = "France", Ext = "33"},
            new CountryModel() {Name = "French Polynesia", Ext = "689"},
            new CountryModel() {Name = "Gabon", Ext = "241"},
            new CountryModel() {Name = "Gambia", Ext = "220"},
            new CountryModel() {Name = "Gaza Strip", Ext = "970"},
            new CountryModel() {Name = "Georgia", Ext = "995"},
            new CountryModel() {Name = "Germany", Ext = "49"},
            new CountryModel() {Name = "Ghana", Ext = "233"},
            new CountryModel() {Name = "Gibraltar", Ext = "350"},
            new CountryModel() {Name = "Greece", Ext = "30"},
            new CountryModel() {Name = "Greenland", Ext = "299"},
            new CountryModel() {Name = "Grenada", Ext = "1 473"},
            new CountryModel() {Name = "Guam", Ext = "1 671"},
            new CountryModel() {Name = "Guatemala", Ext = "502"},
            new CountryModel() {Name = "Guinea", Ext = "224"},
            new CountryModel() {Name = "Guinea-Bissau", Ext = "245"},
            new CountryModel() {Name = "Guyana", Ext = "592"},
            new CountryModel() {Name = "Haiti", Ext = "509"},
            new CountryModel() {Name = "Honduras", Ext = "504"},
            new CountryModel() {Name = "Hong Kong", Ext = "852"},
            new CountryModel() {Name = "Hungary", Ext = "36"},
            new CountryModel() {Name = "Iceland", Ext = "354"},
            new CountryModel() {Name = "India", Ext = "91"},
            new CountryModel() {Name = "Indonesia", Ext = "62"},
            new CountryModel() {Name = "Iran", Ext = "98"},
            new CountryModel() {Name = "Iraq", Ext = "964"},
            new CountryModel() {Name = "Ireland", Ext = "353"},
            new CountryModel() {Name = "Isle of Man", Ext = "44"},
            new CountryModel() {Name = "Israel", Ext = "972"},
            new CountryModel() {Name = "Italy", Ext = "39"},
            new CountryModel() {Name = "Ivory Coast", Ext = "225"},
            new CountryModel() {Name = "Jamaica", Ext = "1 876"},
            new CountryModel() {Name = "Japan", Ext = "81"},
            new CountryModel() {Name = "Jersey", Ext = ""},
            new CountryModel() {Name = "Jordan", Ext = "962"},
            new CountryModel() {Name = "Kazakhstan", Ext = "7"},
            new CountryModel() {Name = "Kenya", Ext = "254"},
            new CountryModel() {Name = "Kiribati", Ext = "686"},
            new CountryModel() {Name = "Kosovo", Ext = "381"},
            new CountryModel() {Name = "Kuwait", Ext = "965"},
            new CountryModel() {Name = "Kyrgyzstan", Ext = "996"},
            new CountryModel() {Name = "Laos", Ext = "856"},
            new CountryModel() {Name = "Latvia", Ext = "371"},
            new CountryModel() {Name = "Lebanon", Ext = "961"},
            new CountryModel() {Name = "Lesotho", Ext = "266"},
            new CountryModel() {Name = "Liberia", Ext = "231"},
            new CountryModel() {Name = "Libya", Ext = "218"},
            new CountryModel() {Name = "Liechtenstein", Ext = "423"},
            new CountryModel() {Name = "Lithuania", Ext = "370"},
            new CountryModel() {Name = "Luxembourg", Ext = "352"},
            new CountryModel() {Name = "Macau", Ext = "853"},
            new CountryModel() {Name = "Macedonia", Ext = "389"},
            new CountryModel() {Name = "Madagascar", Ext = "261"},
            new CountryModel() {Name = "Malawi", Ext = "265"},
            new CountryModel() {Name = "Malaysia", Ext = "60"},
            new CountryModel() {Name = "Maldives", Ext = "960"},
            new CountryModel() {Name = "Mali", Ext = "223"},
            new CountryModel() {Name = "Malta", Ext = "356"},
            new CountryModel() {Name = "Marshall Islands", Ext = "692"},
            new CountryModel() {Name = "Mauritania", Ext = "222"},
            new CountryModel() {Name = "Mauritius", Ext = "230"},
            new CountryModel() {Name = "Mayotte", Ext = "262"},
            new CountryModel() {Name = "Mexico", Ext = "52"},
            new CountryModel() {Name = "Micronesia", Ext = "691"},
            new CountryModel() {Name = "Moldova", Ext = "373"},
            new CountryModel() {Name = "Monaco", Ext = "377"},
            new CountryModel() {Name = "Mongolia", Ext = "976"},
            new CountryModel() {Name = "Montenegro", Ext = "382"},
            new CountryModel() {Name = "Montserrat", Ext = "1 664"},
            new CountryModel() {Name = "Morocco", Ext = "212"},
            new CountryModel() {Name = "Mozambique", Ext = "258"},
            new CountryModel() {Name = "Namibia", Ext = "264"},
            new CountryModel() {Name = "Nauru", Ext = "674"},
            new CountryModel() {Name = "Nepal", Ext = "977"},
            new CountryModel() {Name = "Netherlands", Ext = "31"},
            new CountryModel() {Name = "Netherlands Antilles", Ext = "599"},
            new CountryModel() {Name = "New Caledonia", Ext = "687"},
            new CountryModel() {Name = "New Zealand", Ext = "64"},
            new CountryModel() {Name = "Nicaragua", Ext = "505"},
            new CountryModel() {Name = "Niger", Ext = "227"},
            new CountryModel() {Name = "Nigeria", Ext = "234"},
            new CountryModel() {Name = "Niue", Ext = "683"},
            new CountryModel() {Name = "Norfolk Island", Ext = "672"},
            new CountryModel() {Name = "Northern Mariana Islands", Ext = "1 670"},
            new CountryModel() {Name = "North Korea", Ext = "850"},
            new CountryModel() {Name = "Norway", Ext = "47"},
            new CountryModel() {Name = "Oman", Ext = "968"},
            new CountryModel() {Name = "Pakistan", Ext = "92"},
            new CountryModel() {Name = "Palau", Ext = "680"},
            new CountryModel() {Name = "Panama", Ext = "507"},
            new CountryModel() {Name = "Papua New Guinea", Ext = "675"},
            new CountryModel() {Name = "Paraguay", Ext = "595"},
            new CountryModel() {Name = "Peru", Ext = "51"},
            new CountryModel() {Name = "Philippines", Ext = "63"},
            new CountryModel() {Name = "Pitcairn Islands", Ext = "870"},
            new CountryModel() {Name = "Poland", Ext = "48"},
            new CountryModel() {Name = "Portugal", Ext = "351"},
            new CountryModel() {Name = "Puerto Rico", Ext = "1"},
            new CountryModel() {Name = "Qatar", Ext = "974"},
            new CountryModel() {Name = "Romania", Ext = "40"},
            new CountryModel() {Name = "Russia", Ext = "7"},
            new CountryModel() {Name = "Rwanda", Ext = "250"},
            new CountryModel() {Name = "Saint Barthelemy", Ext = "590"},
            new CountryModel() {Name = "Samoa", Ext = "685"},
            new CountryModel() {Name = "San Marino", Ext = "378"},
            new CountryModel() {Name = "Sao Tome and Principe", Ext = "239"},
            new CountryModel() {Name = "Saudi Arabia", Ext = "966"},
            new CountryModel() {Name = "Senegal", Ext = "221"},
            new CountryModel() {Name = "Serbia", Ext = "381"},
            new CountryModel() {Name = "Seychelles", Ext = "248"},
            new CountryModel() {Name = "Sierra Leone", Ext = "232"},
            new CountryModel() {Name = "Singapore", Ext = "65"},
            new CountryModel() {Name = "Slovakia", Ext = "421"},
            new CountryModel() {Name = "Slovenia", Ext = "386"},
            new CountryModel() {Name = "Solomon Islands", Ext = "677"},
            new CountryModel() {Name = "Somalia", Ext = "252"},
            new CountryModel() {Name = "South Africa", Ext = "27"},
            new CountryModel() {Name = "South Korea", Ext = "82"},
            new CountryModel() {Name = "Spain", Ext = "34"},
            new CountryModel() {Name = "Sri Lanka", Ext = "94"},
            new CountryModel() {Name = "Saint Helena", Ext = "290"},
            new CountryModel() {Name = "Saint Kitts and Nevis", Ext = "1 869"},
            new CountryModel() {Name = "Saint Lucia", Ext = "1 758"},
            new CountryModel() {Name = "Saint Martin", Ext = "1 599"},
            new CountryModel() {Name = "Saint Pierre and Miquelon", Ext = "508"},
            new CountryModel() {Name = "Saint Vincent and the Grenadines", Ext = "1 784"},
            new CountryModel() {Name = "Sudan", Ext = "249"},
            new CountryModel() {Name = "Suriname", Ext = "597"},
            new CountryModel() {Name = "Svalbard", Ext = ""},
            new CountryModel() {Name = "Swaziland", Ext = "268"},
            new CountryModel() {Name = "Sweden", Ext = "46"},
            new CountryModel() {Name = "Switzerland", Ext = "41"},
            new CountryModel() {Name = "Syria", Ext = "963"},
            new CountryModel() {Name = "Taiwan", Ext = "886"},
            new CountryModel() {Name = "Tajikistan", Ext = "992"},
            new CountryModel() {Name = "Tanzania", Ext = "255"},
            new CountryModel() {Name = "Thailand", Ext = "66"},
            new CountryModel() {Name = "Togo", Ext = "228"},
            new CountryModel() {Name = "Tokelau", Ext = "690"},
            new CountryModel() {Name = "Tonga", Ext = "676"},
            new CountryModel() {Name = "Trinidad and Tobago", Ext = "1 868"},
            new CountryModel() {Name = "Tunisia", Ext = "216"},
            new CountryModel() {Name = "Turkey", Ext = "90"},
            new CountryModel() {Name = "Turkmenistan", Ext = "993"},
            new CountryModel() {Name = "Turks and Caicos Islands", Ext = "1 649"},
            new CountryModel() {Name = "Tuvalu", Ext = "688"},
            new CountryModel() {Name = "United Arab Emirates", Ext = "971"},
            new CountryModel() {Name = "Uganda", Ext = "256"},
            new CountryModel() {Name = "United Kingdom", Ext = "44"},
            new CountryModel() {Name = "Ukraine", Ext = "380"},
            new CountryModel() {Name = "Uruguay", Ext = "598"},
            new CountryModel() {Name = "United States", Ext = "1"},
            new CountryModel() {Name = "Uzbekistan", Ext = "998"},
            new CountryModel() {Name = "Vanuatu", Ext = "678"},
            new CountryModel() {Name = "Holy See (Vatican City)", Ext = "39"},
            new CountryModel() {Name = "Venezuela", Ext = "58"},
            new CountryModel() {Name = "Vietnam", Ext = "84"},
            new CountryModel() {Name = "US Virgin Islands", Ext = "1 340"},
            new CountryModel() {Name = "Wallis and Futuna", Ext = "681"},
            new CountryModel() {Name = "West Bank", Ext = "970"},
            new CountryModel() {Name = "Western Sahara", Ext = ""},
            new CountryModel() {Name = "Yemen", Ext = "967"},
            new CountryModel() {Name = "Zambia", Ext = "260"},
            new CountryModel() {Name = "Zimbabwe", Ext = "263"},
        };

        private string[] codeIndex = new string[] { "93", "355", "213", "1 684", "376", "244", "1 264", "672", "1 268", "54", "374", "297", "61", "43", "994", "1 242", "973", "880", "1 246", "375", "32", "501", "229", "1 441", "975", "591", "387", "267", "55", "", "1 284", "673", "359", "226", "95", "257", "855", "237", "1", "238", "1 345", "236", "235", "56", "86", "61", "61", "57", "269", "242", "243", "682", "506", "385", "53", "357", "420", "45", "253", "1 767", "1 809", "670", "593", "20", "503", "240", "291", "372", "251", "500", "298", "679", "358", "33", "689", "241", "220", "970", "995", "49", "233", "350", "30", "299", "1 473", "1 671", "502", "224", "245", "592", "509", "504", "852", "36", "354", "91", "62", "98", "964", "353", "44", "972", "39", "225", "1 876", "81", "", "962", "7", "254", "686", "381", "965", "996", "856", "371", "961", "266", "231", "218", "423", "370", "352", "853", "389", "261", "265", "60", "960", "223", "356", "692", "222", "230", "262", "52", "691", "373", "377", "976", "382", "1 664", "212", "258", "264", "674", "977", "31", "599", "687", "64", "505", "227", "234", "683", "672", "1 670", "850", "47", "968", "92", "680", "507", "675", "595", "51", "63", "870", "48", "351", "1", "974", "40", "7", "250", "590", "685", "378", "239", "966", "221", "381", "248", "232", "65", "421", "386", "677", "252", "27", "82", "34", "94", "290", "1 869", "1 758", "1 599", "508", "1 784", "249", "597", "", "268", "46", "41", "963", "886", "992", "255", "66", "228", "690", "676", "1 868", "216", "90", "993", "1 649", "688", "971", "256", "44", "380", "598", "1", "998", "678", "39", "58", "84", "1 340", "681", "970", "", "967", "260", "263" };

        public SignupPhone_Control() {
            InitializeComponent();
            CountryCodePicker.ItemsSource = countries;
            CountryCodePicker.SelectedIndex = 173; // Россия, Раша, RSA! УРА! УРА!
        }

        private void CountryCodePicker_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            CountryCodeBox.Text = "+" + ((CountryModel)this.CountryCodePicker.SelectedItem).Ext;
            PhoneNumberBox.Focus();
        }

        private void CountryCodeBox_OnTextChanged(object sender, TextChangedEventArgs e) {
            int countryIndex = ValidateCode();
            
            if (countryIndex == -1)
                return;

            CountryCodePicker.SelectedIndex = countryIndex;
        }

        /// <summary>
        /// If code is valid, returns index in the country list, else -1
        /// </summary>
        /// <returns></returns>
        private int ValidateCode() {
            string code = CountryCodeBox.Text;

            if (code.StartsWith("+")) {
                // only + symbol
                if (code.Length == 1)
                    return -1;

                code = code.Substring(1, code.Length - 1);
            }

            string parsedCode = code.Where(Char.IsNumber).Aggregate("", (current, t) => current + t);

            // invalid format, no numbers found
            if (parsedCode == "")
                return -1;

            int countryIndex = Array.LastIndexOf(codeIndex, parsedCode);

            // no such country
            if (countryIndex == -1)
                return -1;

            return countryIndex;
        }

        public bool CodeValid() {
            return ValidateCode() != -1;
        }

        public bool PhoneValid() {
            string phone = PhoneNumberBox.Text;
            
            if (phone.Length == 0)
                return false;

            string parsedPhone = phone.Where(Char.IsNumber).Aggregate("", (current, t) => current + t);

            return parsedPhone.Length != 0;
        }

        public string GetPhone() {
            string phone = PhoneNumberBox.Text;
            string code = CountryCodeBox.Text;

            if (!code.StartsWith("+")) {
                code = "+" + code;
            }

            string parsedCode = code.Where(Char.IsNumber).Aggregate("", (current, t) => current + t);
            string parsedPhone = phone.Where(Char.IsNumber).Aggregate("", (current, t) => current + t);

            return parsedCode + parsedPhone;
        }

        public bool FormValid() {
            return CodeValid() && PhoneValid();
        }
    }
}