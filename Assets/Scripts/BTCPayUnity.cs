﻿using System.Collections.Generic;
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
