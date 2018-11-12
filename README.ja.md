BTCPay Unity クライアント
======

Read this in other languages: [English](README.md), [日本語](README.ja.md)

## BTCpayサーバー Unity クライアントの使用方法

このライブラリーは、Unity用のBtcPay クライアントです。
メインの2つのクラスがあり、１つがBTCPayClientクラスで、もう一つが、Invoiceクラスです。

最初に、 BTCPayClientクラスを、コンストラクターでインスタンス化します。引数には、ペアリングコード（サーバー主導ペアリング）とBTCPay Serverのホスト名を渡します。

次に、Invoiceオブジェクトをインスタンス化し、必須の価格と通貨を引数に渡します。そのたのオプションの商品情報やお客さんのメールアドレスもセット可能です。
そして、クライアントを使用して、BTCPayサーバーへ登録すると、リスポンスとして 支払先情報をセットされたInvoiceオブジェクトがリスポンスとして返ってきます。

必要であれば、その特定のInvoiceの状態の変化にサブスクライブし、コールバック関数を登録できます。

## 依存ライブラリー
BTCpayクライアントは、以下ライブラリーに依存していますので、dllが必要です。
もしくは、BTCpay クライアントの unityパッケージをインポートすることも可能です。

* BitCoinSharp
* BouncyCastle.Crypto
* Newtonsoft.Json
* zxing.unity

## .net のバージョン
Unityの設定で、donetのバージョンを４に変更する必要があります。
`File->Build Settings->Configuration->Scripting Runtime Version-> .Net 4.x` 

## ペアリングコードの作成方法
1. BTCPayサーバーに管理者でログインする。
2. 次の順番で、アクセストークンを作成します。 Store=>Access Token=>Create a new token. ただし、公開鍵は、空白にします。Facadeはposにします。
3. ポップアップに ペアリングコードが、一時的にでるので、コピーしてコードで使用します。

## デフォルトのlndをBTCPay serverに接続する
1. BTCPayサーバーに管理者でログインする。
2. 次の順番で、設定ページに移動します。Stores=>Settings=>General Settings=>Lightning nodes=>Modify
3. Connecting string のところの "click here"をクリックし、接続のテストをして、設定を登録します。

## クラスとメソッド

### BTCPayClient クラス
`new BTCPayClient(String paringCode, String BTCPayServerHost)`  
コンストラクター。引数にペアリングコードとBTCPAYサーバーのホストを渡します。

`Invoice createInvoice(Invoice invoice)`  
InvoiceオブジェクトをBTCPayサーバーに送信し、登録します。配布先で秘密鍵ファイルが見れてしまうので、Facadeの権限は、デフォルトの値のInvoice作成に制限された"pos"になります。リスポンスとして、支払先情報等がアップデートされたInvoiceオブジェクトがもどります。BOLTインボイス文字列が取得できます。

`await subscribeInvoice(String invoiceId,  Func<Invoice, Task> actionOnInvoice)`  
Invoiceを引数にとれるasyncコールバック関数を、モニターするInvoiceのIDを渡し、awaitで実行します。

### Invoice クラス

` new Invoice(double price,String currency)`  
必須情報の価格と通貨で、Invoiceコンストラクターを呼び出します。

追加で、購入者のメールアドレスや購入アイテムの情報を渡すこともできます。そのたのプロパティは、リンクを参照。
https://bitpay.com/docs/create-invoice

invoice.BuyerEmail = jon.doe@g.com  
invoice.NotificationEmail = jon.doe@g.com  
invoice.ItemDesc = "Super Power Star"

## サンプル
以下のサンプルコードは、空のゲームオブジェクトにつけることができます。
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
