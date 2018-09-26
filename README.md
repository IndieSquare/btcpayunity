BTCPay Unity Client
======
Read this in other languages: [English](README.md), [日本語](README.ja.md)


## How to use BTCpay Server Unity Client

This is the BTCpay client for Unity.
There are 2 main classes. One is BTCPayClient class and
the other is Invoice class.

First, you instantiate BTCPayClient class,by passing paring code from BTCPay server (Server Initiated Paring).

Then you create an Invoice object and fill it with the information of the item, product or service you are selling, and customer information.

Then submit the invoice to BTCPay server. In turn , it responses back an Invoice object filled with invoiceID, status, payment destination information.

Then, you can subscribe to the invoice by passing callback function to do whatever you want to do when payment is complete.

## Dependency
BTCpay client has dependencies listed below. You should have those managed dll in hand.
* BitCoinSharp
* BouncyCastle.Crypto
* Newtonsoft.Json
* websocket-sharp
* zxing.unity

## How to generate paring code.
1. Login to BTCPay server as admin role.
2. Go to Store=>Access Token=>Create a new token. without Public key.
3. Copy the server-initiated paring code from popup

## Default lnd daemon with BTCPay Server
1. Login to BTCPay Server as admin role.
2. Go to Stores=>Settings=>General Settings=>Lightning nodes=>Modify
3. at Connecting string Click "click here",test and Submit

## Classes and Methods

### BTCPayClient class
`new BTCPayClient(String paringCode, String BTCPayServerHost)`  
BTCPayClient class has a constructor.  

`Invoice createInvoice(Invoice invoice, String facade)`  
Submit and register an Invoice to BTCPay server. Response is an Invoice filled with Payment destination information. e.g. BOLT invoice String.

`void subscribeInvoice(String invoiceId, Action<Invoice> callbackWithInvoice,GameObject gameObject)`  
Subscribe to an invoice with callback method that takes Invoice as a parameter.

### Invoice class

` new Invoice(double price,String currency)`  

Additionally, you can set several properties.  
https://bitpay.com/docs/create-invoice

invoice.BuyerEmail = jon.doe@g.com  
invoice.NotificationEmail = jon.doe@g.com  
invoice.ItemDesc = "Super Power Star"

## Sample Code
This script is attached to empty object in the hierachy.

```csharp
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

    private BTCPayClient btcPayClient = null;

    void Start()
    {
        //BTCpayCleintをインスタンス化する。BTCPayServerで取得したペアリングコードをセット
        //秘密鍵ファイルがワーキングディレクトリに作成され、公開鍵がBTCPayServerに登録される。
        btcPayClient = new BTCPayClient(pairCode);
    }

    public void createInvoice()
    {

        //1.New Invoice Preparation
        //1.インボイス オブジェクトに必要項目をセットする
        string curr = cmbCurrency.options[cmbCurrency.value].text;
        Invoice invoice = new Invoice(double.Parse(amount.text), curr);//金額と通貨
        invoice.BuyerEmail = email;
        invoice.FullNotifications = true;
        invoice.NotificationEmail = email;
        invoice.PosData = "POST DATA POS DATA";
        invoice.ItemDesc = product.text;//購入アイテムの名称

        //2.Create Invoice with initial data and get the full invoice
        //2.BTCPayServerにインボイスデータをサブミットして、インボイスの詳細データを取得する。
        invoice = btcPayClient.createInvoice(invoice, "merchant");

        Debug.Log("Invoice Created:" + invoice.Id);
        Debug.Log("Invoice Url:" + invoice.Url);

        //3.Lightning BOLT invoice string
        //3.Lightning BOLTデータは以下のプロパティから取得する。
        List<InvoiceCryptoInfo> cryptoInfoList = invoice.CryptoInfo;
        Texture2D texs = btcPayClient.generateQR(cryptoInfoList[0].paymentUrls.BOLT11);//Generate QR code image

        //4.Set the QR code iamge to image Gameobject
        //4.取得したBOLTからQRコードを作成し、ウオレットでスキャンするために表示する。
        QRcode.GetComponent<Image>().sprite = Sprite.Create(texs, new Rect(0.0f, 0.0f, texs.width, texs.height), new Vector2(0.5f, 0.5f), 100.0f);

        //5.Subscribe the callback method with invoice ID to be monitored
        //5.支払がされたら実行されるコールバックを引き渡して、コールーチンで実行する
        StartCoroutine(btcPayClient.subscribeInvoice(invoice.Id, printInvoice,this));
    }

    //Callback method when payment is executed.
    //支払実行時に、呼び出されるコールバック 関数（最新のインボイスオブジェクトが渡される）
    public void printInvoice(Invoice invoice)
    {
        //Hide QR code image to Paied Image file
        //ステータス 一覧はこちら。 https://bitpay.com/docs/invoice-states
        if (invoice.Status == "complete")
        {
            //インボイスのステータスがcompleteであれば、全額が支払われた状態なので、支払完了のイメージに変更する
            QRcode.GetComponent<Image>().sprite = Resources.Load<Sprite>("image/paid");
            Debug.Log("payment is complete");
        }else
        {
            //StartCoroutine(btcPayClient.subscribeInvoice(invoice.Id, printInvoice, this));
            //全額支払いでない場合には、なにか処理をおこなう。以下は、ただ　ステータスを表示して終了。
            Debug.Log("payment is not completed:" + invoice.Status);
        }

    }
}
```
