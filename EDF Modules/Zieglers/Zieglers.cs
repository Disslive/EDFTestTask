using Databox.Libs.Zieglers;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Scraper.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Zieglers;


namespace WheelsScraper
{
    public class Zieglers : BaseScraper
    {
        public Zieglers()
        {
            Name = "Zieglers";
            Url = "https://www.Zieglers.com/";
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)12288 
                                     | SecurityProtocolType.Tls12
                                     | SecurityProtocolType.Tls11
                                     | SecurityProtocolType.Tls;

            PageRetriever.Referer = Url;
            WareInfoList = new List<ExtWareInfo>();
            Wares.Clear();
            BrandItemType = 2; 
            SpecialSettings = new ExtSettings();
            WebHeaderCollection wbc= new WebHeaderCollection();
          
            PageRetriever.Headers = wbc;
        }

        private ExtSettings extSett
        {
            get
            {
                return (ExtSettings)Settings.SpecialSettings;
            }
        }

        public override Type[] GetTypesForXmlSerialization()
        {
            return new Type[] { typeof(ExtSettings) };
        }

        public override System.Windows.Forms.Control SettingsTab
        {
            get
            {
                var frm = new ucExtSettings();
                frm.Sett = Settings;
                return frm;
            }
        }

        public override WareInfo WareInfoType
        {
            get
            {
                return new ExtWareInfo();
            }
        }

        protected override bool Login()
        {
            return true;
        }

        protected override void RealStartProcess()
        {
            lstProcessQueue.Add(new ProcessQueueItem { URL = Url, ItemType = 1 });
            StartOrPushPropertiesThread();
        }

        protected void ProcessBrandsListPage(ProcessQueueItem pqi)
        {
            if (cancel)
                return;


            //var html = PageRetriever.ReadFromServer(extSett.CategoryToScrap);
            var html = PageRetriever.ReadFromServer("https://www.zieglers.com/sitemap/categories/");
            var document = CreateDoc(html);
            pqi.Processed = true;
            var categories = document.DocumentNode.SelectNodes("//ul/li/ul/li/ul/li");

            foreach (var cat in categories.ToArray())
            {
                
                var categoryurl = cat.SelectSingleNode(".//a").AttributeOrNull("href");
                var htmlcreate = PageRetriever.ReadFromServer(categoryurl);
                var doc = new HtmlDocument();
                doc = CreateDoc(htmlcreate);
                pqi.Processed = true;
                var nextpage = doc.DocumentNode.SelectSingleNode(".//div[@class='pagination']/ul[@class='pagination-list']/li[@class ='pagination-item pagination-item--next']");
                categories.Add(nextpage);
                HtmlNodeCollection htmlNodeCollection = doc.DocumentNode.SelectNodes("//*[@class = 'productGrid']/li[@class = 'product']");
                HtmlNodeCollection products = htmlNodeCollection;
                if (products != null)
                    foreach (var prod in products)
                    {

                        var name = prod.SelectSingleNode(".//h4[@class='card-title']/a[@href]").InnerTextOrNull();
                        if (string.IsNullOrEmpty(name))
                            continue;
                        var url = prod.SelectSingleNode(".//figure[@class ='card-figure']/a").AttributeOrNull("href");



                        var html2 = PageRetriever.ReadFromServer(url);
                        var doc2 = CreateDoc(html2);
                        var optiontitle = doc2.DocumentNode.SelectNodes("//div[@data-product-option-change]/div[@class='form-field']");

                        var wi = new ExtWareInfo();
                        if (optiontitle != null)
                        {
                            foreach (var ot in optiontitle)
                            {
                                var pot = ot.SelectSingleNode(".//label[@class='form-label form-label--alternate form-label--inlineSmall']").InnerTextOrNull();
                                pot = pot.Replace(" ", "");
                                pot = pot.Replace("Required", "");
                                pot = pot.Replace("\n\n", "");
                                var optionchoice = ot.SelectNodes(".//label[@class='form-option form-option-swatch']");
                                var optionchoice2 = ot.SelectNodes(".//label[@class='form-label']");
                                var opt = ot.SelectNodes(".//input");
                                if (opt != null)
                                {
                                    for (int i = 0; i < opt.ToArray().Length; i++)
                                    {
                                        var wi2 = new ExtWareInfo();
                                        wi2.PrimaryOptionTitle = pot;
                                        wi2.ProductTitle = name;
                                        wi2.ProductURL = url;
                                        if (optionchoice2 != null) wi2.PrimaryOptionChoice = optionchoice2[i].InnerTextOrNull();
                                        if (optionchoice != null) wi2.PrimaryOptionChoice = optionchoice[i].SelectSingleNode(".//span").AttributeOrNull("title");
                                        wi2.GeneralImage = doc2.DocumentNode.SelectSingleNode(".//div[@class='productView-img-container'][1]/a").GetAttributeValue("href", "");
                                        if (!String.IsNullOrEmpty(opt[i].AttributeOrNull("value")) && !String.IsNullOrEmpty(opt[i].AttributeOrNull("name")))
                                        {
                                            SendAdditionalRequest(opt[i].AttributeOrNull("name") + "=" + opt[i].AttributeOrNull("value"), ref wi2);
                                            AddWareInfo(wi2);
                                            lock (this)
                                                lstProcessQueue.Add(new ProcessQueueItem { Name = name, ItemType = 10, Item = wi2, URL = url });
                                        }
                                    }
                                }

                                var select = ot.SelectSingleNode(".//select");
                                var opt2 = ot.SelectNodes(".//select/option");

                                if (opt2 != null)
                                {
                                    var selectName = select.AttributeOrNull("name");
                                    for (int i = 0; i < opt2.ToArray().Length; i++)
                                    {

                                        var wi3 = new ExtWareInfo();
                                        wi3.PrimaryOptionChoice = opt2[i].NextSibling.InnerTextOrNull();
                                        wi3.PrimaryOptionTitle = pot;
                                        wi3.ProductTitle = name;
                                        wi3.ProductURL = url;
                                        wi3.GeneralImage = doc2.DocumentNode.SelectSingleNode(".//div[@class='productView-img-container'][1]/a").GetAttributeValue("href", "");
                                        if (!String.IsNullOrEmpty(opt2[i].AttributeOrNull("value")) && selectName != null)
                                        {
                                            SendAdditionalRequest(selectName + "=" + opt2[i].AttributeOrNull("value"), ref wi3);
                                            AddWareInfo(wi3);
                                            lock (this)
                                                lstProcessQueue.Add(new ProcessQueueItem { Name = name, ItemType = 10, Item = wi3, URL = url });
                                        }
                                    }


                                }
                            }


                            //foreach (var ot in optiontitle)
                            //{

                            //    var pot = ot.SelectSingleNode(".//label[@class='form-label form-label--alternate form-label--inlineSmall']").InnerTextOrNull();
                            //    pot = pot.Replace(" ", "");
                            //    pot = pot.Replace("Required", "");
                            //    pot = pot.Replace("\n\n", "");
                            //    var optionchoice = ot.SelectNodes(".//label[@class='form-option form-option-swatch']");
                            //    var optionchoice2 = ot.SelectNodes("//select[@class='form-select form-select--small']/option");
                            //    if (optionchoice2 != null)
                            //    {
                            //        foreach (var oc in optionchoice2) {
                            //            wi = new ExtWareInfo();
                            //            wi.ProductTitle = name;
                            //            wi.ProductURL = url;
                            //            wi.PrimaryOptionChoice = oc.InnerText;
                            //            wi.PrimaryOptionTitle = pot;
                            //            AddWareInfo(wi);
                            //            lock (this)
                            //                lstProcessQueue.Add(new ProcessQueueItem { Name = name, ItemType = 10, Item = wi, URL = url });
                            //        }
                            //    }
                            //    if (optionchoice != null)
                            //    {

                            //        for (int i = 0; i < optionchoice.ToArray().Length; i++)
                            //        {
                            //            wi = new ExtWareInfo();
                            //            wi.ProductTitle = name;
                            //            wi.ProductURL = url;
                            //            var choice = optionchoice[i].SelectSingleNode(".//span").AttributeOrNull("title");

                            //            if (choice != null)
                            //            {
                            //                wi.PrimaryOptionChoice = choice;
                            //                wi.GeneralImage = doc2.DocumentNode.SelectSingleNode(".//div[@class='productView-img-container'][1]/a").GetAttributeValue("href", "");
                            //                wi.PrimaryOptionTitle = pot;
                            //                wi.WebPrice = ParsePrice(doc2.DocumentNode.SelectSingleNode(".//div[@class='price-section price-section--withoutTax '][1]/span").InnerTextOrNull());

                            //                var opt = ot.SelectSingleNode(".//input");
                            //                SendAdditionalRequest(opt.AttributeOrNull("name") + "=" + opt.AttributeOrNull("value"), ref wi);

                            //                AddWareInfo(wi);
                            //                lock (this)
                            //                    lstProcessQueue.Add(new ProcessQueueItem { Name = name, ItemType = 10, Item = wi, URL = url });
                            //            }


                            //        }
                            //    }

                            //}
                        }
                        else
                        {

                            wi.ProductTitle = name;
                            wi.ProductURL = url;
                            wi.PrimaryOptionTitle = null;
                            AddWareInfo(wi);
                            lock (this)
                                lstProcessQueue.Add(new ProcessQueueItem { Name = name, ItemType = 10, Item = wi, URL = url });
                        }



                    }

                OnItemLoaded(null);


                pqi.Processed = true;
                MessagePrinter.PrintMessage("Brands list processed");
                StartOrPushPropertiesThread();
            } 
        }
        private void ProcessProductPage(ProcessQueueItem pqi)
        {
            var wi = (ExtWareInfo)pqi.Item;
            MessagePrinter.PrintMessage("Processing " + wi.ProductTitle);
            var html = PageRetriever.ReadFromServer(pqi.URL);
            var doc = CreateDoc(html);
            pqi.Processed = true;
            if (doc != null)
            {
                var desc = doc.DocumentNode.SelectSingleNode(".//article[@class='productView-description']/div[@class='tabs-contents']/div[@id='tab-description']").InnerTextOrNull();
                desc = desc.Replace("\n", " ");
                desc = desc.Replace("\r", " ");
                if (wi.WebPrice == 0) wi.WebPrice = ParsePrice(doc.DocumentNode.SelectSingleNode(".//div[@class='price-section price-section--withoutTax '][1]/span").InnerTextOrNull());
                if (wi.ProductPartNumber == null) wi.ProductPartNumber = doc.DocumentNode.SelectSingleNode(".//dl[@class='productView-info']/dd[@data-product-sku]").InnerTextOrNull();
                var genimg = doc.DocumentNode.SelectSingleNode(".//div[@class='productView-img-container'][1]/a").GetAttributeValue("href", "");
                var itemw = ParseDouble(doc.DocumentNode.SelectSingleNode(".//dl[@class='productView-info']/dd[@data-product-weight]").InnerTextOrNull());

                wi.ProductDescription = desc;
                wi.GeneralImage = genimg;
                wi.ItemWeight = itemw;
            }
             

        }
        protected override Action<ProcessQueueItem> GetItemProcessor(ProcessQueueItem item)
        {
            Action<ProcessQueueItem> act;
            if (item.ItemType == 1)
                act = ProcessBrandsListPage;
            else if (item.ItemType == 10)
                act = ProcessProductPage;
            else act = null;

            return act;
        }
        public void SendAdditionalRequest(string attribute, ref ExtWareInfo wi)
        {

            if (cancel)
                return;

            Uri baseUri = new Uri("http://www.zieglers.com/remote.php");
            if (wi.GeneralImage != null)
            {
                string[] urlparts = wi.GeneralImage.Split('/');
                for (int i = 0; i < urlparts.Length; i++)
                {
                    if (urlparts[i] == "products") wi.ProdId = urlparts[i + 1];
                }
                string postData = "action=add&product_id=" + wi.ProdId + "&" + attribute + "&qty[]=1&w=getProductAttributeDetails";

                var request = (HttpWebRequest)WebRequest.Create(baseUri.AbsoluteUri);
                var data = Encoding.ASCII.GetBytes(postData);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                if (!string.IsNullOrEmpty(responseString))
                {
                    dynamic jsonVal = JsonConvert.DeserializeObject(responseString);

                    wi.ProductPartNumber = wi.Jobber = (string)jsonVal.details.sku;
                    wi.MSRP = wi.WebPrice = wi.Cost = Double.Parse((string)jsonVal.details.unformattedPrice, new CultureInfo("en"));
                    wi.ImageUrl = wi.ApplicationSpecificImage = (string)jsonVal.details.image;
                }
            }
        }

    }
}
