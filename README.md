BTCPay Unity Client
======
Read this in other languages: [English](README.md), [日本語](README.ja.md)


## How to use BTCpay Server Unity Client

This project is the BTCpay client for Unity and it allows Unity developers to develop games/apps backed by a BTCpay server.
Some usecases are in-app purchasing and paywall to get to the special levels/features in a game or an app.
There are 2 main classes. One is BTCPayClient class and the other is Invoice class.

First, you instantiate BTCPayClient class,by passing paring code from BTCPay server (Server Initiated Paring) and the hostname of BTCPay server.

Then you create an Invoice object and fill it with the information of the item, product or service you are selling, and customer information if required.

Then, submit the invoice to BTCPay server. In turn , it responses back an Invoice object filled with invoiceID, status, payment destination information(e.g. Bitcoin address/BOLT11 invoice string). Then you can show the QR code for those addresses with which player can pay by bitcoin or lightning.

Then, you can subscribe to the invoice by passing callback function to do whatever you want to do when payment is complete.

## DLL Dependency
BTCpay client has dependencies listed below. You should have those managed dll in hand.
* BouncyCastle.Crypto for ecptic curve key processing
* NBitcoin for ecptic curve key processing
* websocket-sharp for websocket for non-webGL
* log4net
* zxing.unity for QR code generation
* Multiformats.Base  for some encoding
* JsonDotNet   json processing

or just use the importable unity package found in releases

## .net version
In unity you may need to set the project settings to use .net version 4
`File->Build Settings->Configuration->Scripting Runtime Version-> .Net 4.x` 

## How to generate server-initiated paring code
1. Login to BTCPay server as admin role.
2. Go to Store=>Access Token=>Create a new token. without Public key.Set facade as pos (Don't use Marchant facade!!)
3. Copy the server-initiated paring code from popup
(Don't forget the setting for store wallet.)

## Default lnd daemon with BTCPay Server
1. Login to BTCPay Server as admin role.
2. Go to Stores=>Settings=>General Settings=>Lightning nodes=>Modify
3. at Connecting string Click "click here",test and Submit

## private key
When you start the game in Unity Editor for the first time, it creates private key as Assets/Resources/poskey.txt. And it is loaded as an Asset.You may need to refresh the project folder after it is created because unity sometimes doesnot pick up the file created. Once it is created/recognized, it keep using this private key mapped with the paring code. If you want to use new key, you just remove the file from Unity editor. The SDK will try to create new one with a new paring code.

## Classes and Methods

### BTCPayClient class
`new BTCPayClient(String paringCode, String BTCPayServerHost)`  
BTCPayClient class has a constructor.  pass the paring code and BTCpay server host.

`IEnumerator createInvoice(Invoice invoice,Action<Invoice> invoiceAction)`  
Run with Coroutine by passing a new partialy filled Invoice to BTCPay server.  Callback should be passed and it processes the Invoice returned, which is filled with Payment destination information. e.g. BOLT invoice String.It uses the limited facade "pos" as default because the private key for this API connection is exposed to players with the game.

### Invoice class

` new Invoice(double price,String currency)`  

Additionally, you can set several more properties.  
https://bitpay.com/docs/create-invoice

invoice.BuyerEmail = jon.doe@g.com  
invoice.NotificationEmail = jon.doe@g.com  
invoice.ItemDesc = "Super Power Star"

## Sample Code
This script is attached to empty object in the hierachy.

```csharp
using System.Collections.Generic;
using UnityEngine;
using BTCPayAPI;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
public class BTCPayUnity : MonoBehaviour {

    public string pairCode;//set pairing code from inspector
    public string btcpayServerHost;//set host from inspector
    public string email;//Optional

    public Text product;
    public Dropdown cmbCurrency;
    public Text amount;
    public GameObject QRcode;

    private BTCPayClient btcPayClient = null;

    public void Start()
    {
        //Instantiate the BTCPayClient Object with server-initiated pairing code and hostname of BTCpay server
        //Once instantiated, it will generate a new private key if not there, and SIN ,which is derived from public key.
        //then registered on BTCPay server
        //BTCpayCleintをインスタンス化する。BTCPayServerで取得したペアリングコードをとホスト名をセット
        //秘密鍵ファイルがワーキングディレクトリに作成され、公開鍵から作られたBitcoinアドレスのようなSINがBTCPayServerに登録される。
        btcPayClient = new BTCPayClient(this,pairCode, btcpayServerHost);
        StartCoroutine(btcPayClient.initialize());
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
        invoice.ItemDesc = product.text;//購入アイテムの名称

        string json = JsonConvert.SerializeObject(invoice);
        JObject jo = JObject.Parse(json);

        //2.Create Invoice with initial data and get the full invoice
        //2.BTCPayServerにインボイスデータをサブミットして、インボイスの詳細データを取得する。
        StartCoroutine(btcPayClient.createInvoice(invoice ,handleInvoice));

    }

    private void handleInvoice(Invoice invoice)
    {
        //3.Lightning BOLT invoice string
        //3.Lightning BOLT invoice データは以下のプロパティから取得する。
        List<InvoiceCryptoInfo> cryptoInfoList = invoice.CryptoInfo;
        string boltInvoice=null;
        foreach(InvoiceCryptoInfo info in cryptoInfoList){
            if (info.paymentType == "LightningLike")
            {
                Debug.Log("bolt :" + info.paymentUrls.BOLT11);
                boltInvoice = info.paymentUrls.BOLT11;
            }
        }
        if(string.IsNullOrEmpty(boltInvoice))
        {
            Debug.Log("bolt Invoice is not set in Invoice in reponse.Check the BTCpay server's lightning setup");
            return;
        }

        Texture2D texs = btcPayClient.generateQR(boltInvoice);//Generate QR code image

        //4.Set the QR code iamge to image Gameobject
        //4.取得したBOLTからQRコードを作成し、ウオレットでスキャンするために表示する。
        QRcode.GetComponent<Image>().sprite = Sprite.Create(texs, new Rect(0.0f, 0.0f, texs.width, texs.height), new Vector2(0.5f, 0.5f), 100.0f);

        //5.Subscribe the an callback method with invoice ID to be monitored
        //5.支払がされたら実行されるコールバックを引き渡して、コールーチンで実行する
        StartCoroutine(btcPayClient.SubscribeInvoiceCoroutine(invoice.Id, printInvoice));
        //StartCoroutine(btcPayClient.listenInvoice(invoice.Id, printInvoice));
        

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
            //Change the image from QR to Paid
            QRcode.GetComponent<Image>().sprite = Resources.Load<Sprite>("image/paid");
            Debug.Log("payment is complete");
        }else
        {
             //for example, if the amount paid is not full, do something.the line below just print the status.
            //全額支払いでない場合には、なにか処理をおこなう。以下は、ただ　ステータスを表示して終了。
            Debug.Log("payment is not completed:" + invoice.Status);
        }

    }
}

```
