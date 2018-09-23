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

        //New Invoice Preparation
        string curr = cmbCurrency.options[cmbCurrency.value].text;
        Invoice invoice = new Invoice(double.Parse(amount.text), curr);
        invoice.BuyerEmail = email;
        invoice.FullNotifications = true;
        invoice.NotificationEmail = email;
        invoice.PosData = "POST DATA POS DATA";
        invoice.ItemDesc = product.text;

        //Create Invoice with initial data and get the full invoice
        invoice = btcpay.createInvoice(invoice, "merchant");
        Debug.Log("Invoice Created:" + invoice.Id);
        Debug.Log("Invoice Url:" + invoice.Url);
        //Lightning BOLT invoice string
        List<InvoiceCryptoInfo> cryptoInfoList = invoice.CryptoInfo;

        //Generate QR code image
        Texture2D texs = btcpay.generateQR(cryptoInfoList[0].paymentUrls.BOLT11);
        //Set the QR code iamge to image Gameobject
        QRcode.GetComponent<Image>().sprite = Sprite.Create(texs, new Rect(0.0f, 0.0f, texs.width, texs.height), new Vector2(0.5f, 0.5f), 100.0f);

        //Subscribe the callback method with invoice ID to be monitored
        StartCoroutine(btcpay.subscribeInvoice(invoice.Id, printInvoice,this));
    }

    //Callback method when payment is executed.
    public void printInvoice(Invoice invoice)
    {
        //Hide QR code image to Paied Image file
        if (invoice.Status == "complete")
        {
            QRcode.GetComponent<Image>().sprite = Resources.Load<Sprite>("image/paid");
            Debug.Log("payment is complete");
        }else
        {
//            StartCoroutine(btcpay.subscribeInvoice(invoice.Id, printInvoice, this));
            Debug.Log("payment is not completed:" + invoice.Status);
        }

    }
}
