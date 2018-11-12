BTCPay Unity Client
======
Read this in other languages: [English](README.md), [日本語](README.ja.md)


## How to use BTCpay Server Unity Client

This is the BTCpay client for Unity.
There are 2 main classes. One is BTCPayClient class and
the other is Invoice class.

First, you instantiate BTCPayClient class,by passing paring code from BTCPay server (Server Initiated Paring) and hostname of BTCPay server.

Then you create an Invoice object and fill it with the information of the item, product or service you are selling, and customer information.

Then submit the invoice to BTCPay server. In turn , it responses back an Invoice object filled with invoiceID, status, payment destination information.

Then, you can subscribe to the invoice by passing callback function to do whatever you want to do when payment is complete.

## Dependency
BTCpay client has dependencies listed below. You should have those managed dll in hand.
* BitCoinSharp
* BouncyCastle.Crypto
* Newtonsoft.Json
* zxing.unity

or just use the importable unity package found in releases

## .net version
In unity you may need to set the project settings to use .net version 4
`File->Build Settings->Configuration->Scripting Runtime Version-> .Net 4.x` 

## How to generate paring code.
1. Login to BTCPay server as admin role.
2. Go to Store=>Access Token=>Create a new token. without Public key.Set facade as pos
3. Copy the server-initiated paring code from popup

## Default lnd daemon with BTCPay Server
1. Login to BTCPay Server as admin role.
2. Go to Stores=>Settings=>General Settings=>Lightning nodes=>Modify
3. at Connecting string Click "click here",test and Submit

## Classes and Methods

### BTCPayClient class
`new BTCPayClient(String paringCode, String BTCPayServerHost)`  
BTCPayClient class has a constructor.  pass the paring code and BTCpay server host.

`Invoice createInvoice(Invoice invoice)`  
Submit and register an Invoice to BTCPay server. Response is an Invoice filled with Payment destination information. e.g. BOLT invoice String.It uses the limited facade "pos" as default because the private key for this API connection is exposed to game player.

`await subscribeInvoice(String invoiceId,  Func<Invoice, Task> actionOnInvoice)`  
Subscribe to an invoice with callback async method that takes Invoice as a parameter.

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
using System.Collections.Generic;
using UnityEngine;
using BTCPayAPI;
using UnityEngine.UI;
using System.Threading.Tasks;

public class BTCPayUnity : MonoBehaviour {

    public string pairCode;//set pairing code from inspector
    public string btcpayServerHost;//set host from inspector
    public string email;//Optional

    public Text product;
    public Dropdown cmbCurrency;
    public Text amount;
    public GameObject QRcode;

    private BTCPayClient btcPayClient = null;

    void Start()
    {
        //Instantiate the BTCPayClient Object with server-initiated pairing code and hostname of BTCpay server
        //Once instantiated, it will generate a new private key if not there, and SIN ,which is derived from public key.
        //then registered on BTCPay server
        //BTCpayCleintをインスタンス化する。BTCPayServerで取得したペアリングコードをとホスト名をセット
        //秘密鍵ファイルがワーキングディレクトリに作成され、公開鍵から作られたBitcoinアドレスのようなSINがBTCPayServerに登録される。
        btcPayClient = new BTCPayClient(pairCode, btcpayServerHost);
    }

    public async void createInvoice()
    {

        //1.New Invoice Preparation
        //1.インボイス オブジェクトに必要項目をセットする
        string curr = cmbCurrency.options[cmbCurrency.value].text;
        Invoice invoice = new Invoice(double.Parse(amount.text), curr);//金額と通貨
        invoice.BuyerEmail = email;
        invoice.FullNotifications = true;
        invoice.NotificationEmail = email;
        invoice.PosData = "TEST POS DATA";
        invoice.ItemDesc = product.text;//購入アイテムの名称

        //2.Create Invoice with initial data and get the full invoice
        //2.BTCPayServerにインボイスデータをサブミットして、インボイスの詳細データを取得する。
        invoice = btcPayClient.createInvoice(invoice);

        Debug.Log("Invoice Created:" + invoice.Id);
        Debug.Log("Invoice Url:" + invoice.Url);

        //3.Lightning BOLT invoice string
        //3.Lightning BOLT invoice データは以下のプロパティから取得する。
        List<InvoiceCryptoInfo> cryptoInfoList = invoice.CryptoInfo;
        Texture2D texs = btcPayClient.generateQR(cryptoInfoList[0].paymentUrls.BOLT11);//Generate QR code image

        //4.Set the QR code iamge to image Gameobject
        //4.取得したBOLTからQRコードを作成し、ウオレットでスキャンするために表示する。
        QRcode.GetComponent<Image>().sprite = Sprite.Create(texs, new Rect(0.0f, 0.0f, texs.width, texs.height), new Vector2(0.5f, 0.5f), 100.0f);

        //5.Subscribe the an callback method with invoice ID to be monitored
        //5.支払がされたら実行されるasync コールバックを引き渡して、await で実行する
        await btcPayClient.subscribeInvoiceAsync(invoice.Id, printInvoice);

    }

    //Callback method when payment is executed.
    //支払実行時に、呼び出されるコールバック 関数（最新のインボイスオブジェクトが渡される）
    public async Task printInvoice(Invoice invoice)
    {
        //Hide QR code image to Paied Image file
        //ステータス 一覧はこちら。 https://bitpay.com/docs/invoice-states
        if (invoice.Status == "complete")
        {
            //インボイスのステータスがcompleteであれば、全額が支払われた状態なので、支払完了のイメージに変更する
            //Change the image from QR to Paid
            QRcode.GetComponent<Image>().sprite = Resources.Load<Sprite>("image/paid");
            //1 sec Delay to keep paid image/支払済みイメージを1秒間表示
            await Task.Delay(1000);
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
