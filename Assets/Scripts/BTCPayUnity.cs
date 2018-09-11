using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BTCPayAPI;
using UnityEngine.UI;
using System.Threading.Tasks;
public class BTCPayUnity : MonoBehaviour {

    public string pairCode;
    public string email;

    public Text product;
    public Dropdown cmbCurrency;
    public Text amount;

    public Text log;

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

        Application.OpenURL(invoice.Url);

        subscribeInvoice(invoice.Id);
    }

    public void getInvoice(string invoiceId)
    {
        //Get Invoice 
        Invoice inv = btcpay.getInvoice(invoiceId, BTCPay.FACADE_MERCHANT);
        Debug.Log("Invoice Created:" + inv.Id);
        Debug.Log("Invoice Url:" + inv.Url);
    }

    public void subscribeInvoice(string invoiceId)
    {
        //Get Invoice 
        Task<Invoice> t = btcpay.GetInvoiceAsync(invoiceId);
        t.Wait();
        Invoice inv = t.Result;
        Debug.Log("Invoice Status:" + inv.Status);
    }

}
