using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using BTCPayAPI;
using UnityEngine.UI;
using System.Threading.Tasks;
using WebSocketSharp;
using ZXing;
using ZXing.QrCode;

public class BTCPayUnity : MonoBehaviour {

    public string pairCode;
    public string email;

    public Text product;
    public Dropdown cmbCurrency;
    public Text amount;
    public GameObject QRcode;

    private BTCPay btcpay = null;

    void Start()
    {
        btcpay = new BTCPay(pairCode);
    }

    public void createInvoice()
    {

        //New Invoice Prep 
        string curr = cmbCurrency.options[cmbCurrency.value].text;
        Invoice invoice = new Invoice(double.Parse(amount.text), curr);
        invoice.BuyerEmail = email;
        invoice.FullNotifications = true;
        invoice.NotificationEmail = email;
        invoice.PosData = "POST DATA POS DATA";
        invoice.ItemDesc = product.text;

        //Create Invoice 
        invoice = btcpay.createInvoice(invoice, "merchant");

        Debug.Log("Invoice Created:" + invoice.Id);
        Debug.Log("Invoice Url:" + invoice.Url);

        //Open BTCPay site with WR
        //        Application.OpenURL(invoice.Url);
        //Websocketes to wait for payment event
        //subscribeInvoice(invoice.Id);
        //        StartCoroutine(subscribeInvoice(invoice.Id, Debug.Log));

        List<InvoiceCryptoInfo> cryptoInfoList = invoice.CryptoInfo;
        Texture2D texs = generateQR(cryptoInfoList[0].paymentUrls.BOLT11);
        QRcode.GetComponent<Image>().sprite = Sprite.Create(texs, new Rect(0.0f, 0.0f, texs.width, texs.height), new Vector2(0.5f, 0.5f), 100.0f);
        StartCoroutine(subscribeInvoice(invoice.Id, printInvoice));

    }

    public void printInvoice(Invoice invoice)
    {
        //
        QRcode.GetComponent<Image>().sprite = Resources.Load<Sprite>("image/paid");
    }


    public Invoice getInvoice(string invoiceId)
    {
        //Get Invoice 
        Invoice inv = btcpay.getInvoice(invoiceId, BTCPay.FACADE_MERCHANT);
        Debug.Log("Invoice Created:" + inv.Id);
        Debug.Log("Invoice Url:" + inv.Url);
        return inv;
    }

    public IEnumerator subscribeInvoice(string invoiceId, Action<Invoice> actionOnInvoice)
    {
        //Get Invoice 
        //Task<Invoice> t = btcpay.GetInvoiceAsync(invoiceId);
        //t.Wait();
        //Invoice inv = t.Result;
        //        Debug.Log("Invoice Status:" + inv.Status);

//        StartCoroutine(SubscribeInvoiceCoroutine(invoiceId));

        CoroutineWithData cd = new CoroutineWithData(this, SubscribeInvoiceCoroutine(invoiceId));
        yield return cd.coroutine;

        Debug.Log("result is " + cd.result);

        Invoice resultInv = (Invoice)cd.result;
        actionOnInvoice(resultInv);
    }

    public IEnumerator SubscribeInvoiceCoroutine(string invoiceId)
    {
        Invoice inv = null;
        string _serverHost = "btcpayreg.indiesquare.net";
        //WebSocket  loop
        using (var ws = new WebSocket("wss://" + _serverHost + "/i/" + invoiceId + "/status/ws"))
        {
            ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            ws.OnMessage += (sender, e) =>
            {
                Debug.Log("WS Message: " + e.Data);
                //Get Invoice 
//                inv = getInvoice(invoiceId, BTCPay.FACADE_MERCHANT);
                inv = getInvoice(invoiceId);
                Debug.Log("Got invoice : " + inv.Status);
                ws.Close();
            };
            ws.OnClose += (sender, e) =>
            {
                //Console.WriteLine("WS Closed: " + e.Code);
            };
            ws.OnError += (sender, e) =>
            {
                //Console.WriteLine("WS Err: " + e.Exception);
            };
            ws.OnOpen += (sender, e) =>
              Debug.Log("WS Opened.");

            ws.Connect();

            //Wait connection is closed when invoice is gotten or exception
            while (ws.IsAlive)
            {
                //Thread.Sleep(500);
                yield return new WaitForSeconds(0.5f);
                Debug.Log("Sleep 500ms.");
            }
        }//Close websocket
        yield return inv;
    }

    private static Color32[] Encode(string textForEncoding,
      int width, int height)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = height,
                Width = width
            }
        };
        return writer.Write(textForEncoding);
    }
    public Texture2D generateQR(string text)
    {
        var encoded = new Texture2D(384, 384);
        var color32 = Encode(text, encoded.width, encoded.height);
        encoded.SetPixels32(color32);
        encoded.Apply();
        return encoded;
    }


}
