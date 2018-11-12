using System.Collections.Generic;
using UnityEngine;
using BTCPayAPI;
using UnityEngine.UI;
using System.Threading.Tasks;

public class BTCPayUnity : MonoBehaviour {

    public string pairCode;
    public string btcpayServerHost;
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
        //3.Lightning BOLTデータは以下のプロパティから取得する。
        List<InvoiceCryptoInfo> cryptoInfoList = invoice.CryptoInfo;
        Texture2D texs = btcPayClient.generateQR(cryptoInfoList[0].paymentUrls.BOLT11);//Generate QR code image

        //4.Set the QR code iamge to image Gameobject
        //4.取得したBOLTからQRコードを作成し、ウオレットでスキャンするために表示する。
        QRcode.GetComponent<Image>().sprite = Sprite.Create(texs, new Rect(0.0f, 0.0f, texs.width, texs.height), new Vector2(0.5f, 0.5f), 100.0f);

        //5.Subscribe the callback method with invoice ID to be monitored
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
