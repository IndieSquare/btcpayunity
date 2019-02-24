BTCPay Unity クライアント
======

Read this in other languages: [English](README.md), [日本語](README.ja.md)

## BTCpayサーバー Unity クライアントの使用方法

このライブラリーは、Unity用のBtcPay Server クライアントです。
このライブラリーを使うことで、Unityで作るゲームやアプリに、BTCPAY SERVER経由での支払い機能を追加することができます。
メインの2つのクラスがあり、１つがBTCPayClientクラスで、もう一つが、Invoiceクラスです。

最初に、 BTCPayClientクラスを、コンストラクターでインスタンス化します。引数には、ペアリングコード（サーバー主導ペアリング）とBTCPay Serverのホスト名を渡します。

次に、Invoiceオブジェクトをインスタンス化し、必須の価格と通貨を引数に渡します。そのたのオプションの商品情報やお客さんのメールアドレスもセット可能です。
そして、クライアントを使用して、BTCPayサーバーへ登録すると、リスポンスとして 支払先情報をセットされたInvoiceオブジェクトがリスポンスとして返ってきます。

必要であれば、支払先アドレス（BTCアドレスやBOLT11インボイス）のQRコードを表示し、その特定のInvoiceの状態の変化にサブスクライブした、コールバック関数を登録できます。

## 依存ライブラリー
BTCpayクライアントは、以下ライブラリーに依存していますので、dllが必要です。
もしくは、BTCpay クライアントの リリースページにある unityパッケージをインポートすることでバージョンの心配が不要です。

* BouncyCastle.Crypto NBitcoinの依存先
* NBitcoin エリプティックカーブを使った鍵の生成や署名
* websocket-sharp WEBGL以外用のWEBSOCKETライブラリー
* log4net
* zxing.unity QR コード生成
* Multiformats.Base  エンコーディング
* JsonDotNet   jsonの処理

## .net のバージョン
Unityの設定で、donetのバージョンを４に変更する必要があります。
`File->Build Settings->Configuration->Scripting Runtime Version-> .Net 4.x` 

## ペアリングコードの作成方法
1. BTCPayサーバーに管理者でログインする。
2. 次の順番で、アクセストークンを作成します。 Store=>Access Token=>Create a new token. ただし、公開鍵は、空白にします。Facadeは必ずposにします。
3. ポップアップに ペアリングコードが、一時的にでるので、コピーしてコードで使用します。
（ストアのWalletの設定は必須です。）

## デフォルトのlndをBTCPay serverに接続する
1. BTCPayサーバーに管理者でログインする。
2. 次の順番で、設定ページに移動します。Stores=>Settings=>General Settings=>Lightning nodes=>Modify
3. Connecting string のところの "click here"をクリックし、接続のテストをして、設定を登録します。

## 秘密鍵の生成について
このUnityプロジェクトを初めて実行すると、設定したペアリングコードにマップされる秘密鍵が生成されます。(Assets/Resources/poskey.txt)。たまに、Unityがこのファイルの生成にきづかないので、フォルダをリフレッシュしてUnityEditorからこのファイルが見えることを確認します。  ビルドすると、実行ファイルやWasmに、組み込まれるので、全プレーヤーで共通の秘密鍵が使用されます。新しい秘密鍵を使用したいときは、ファイルをUnityから消せばSDKが新しい秘密鍵を生成しますが前のは期限切れなので、新しいペアリングコードが必要です。

## クラスとメソッド

### BTCPayClient クラス
`new BTCPayClient(String paringCode, String BTCPayServerHost)`  
コンストラクター。引数にペアリングコードとBTCPAYサーバーのホストを渡します。

`IEnumerator createInvoice(Invoice invoice,Action<Invoice> invoiceAction)`  
コールーチンで非同期に実行します。InvoiceオブジェクトをBTCPayサーバー送りつつ、Invoiceを引数にとれるコールバック関数を、渡して、リスポンスでもどってきたInvoiceオブジェクトの処理をできるようにします。配布バイナリに秘密鍵ファイルが含まれるので、Facadeの権限は、デフォルトの値のInvoice作成に制限された"pos"になります。

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
