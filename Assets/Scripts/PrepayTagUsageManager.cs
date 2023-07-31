using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SimpleJSON;
using System;
using System.Text;
using SocketIO;

public class PrepayTagUsageManager : MonoBehaviour
{
    public Text tagName;
    public Text chargeSumTxt;
    public Text usageSumTxt;
    public Text remainTxt;

    public GameObject chargeItemParent;
    public GameObject chargeItem;
    public GameObject menuUsageParent;
    public GameObject menuUsageItem;

    public GameObject regTagPopup;
    public GameObject extraMenuPopup;
    public GameObject changeTagPopup;
    public GameObject payPopup;
    public GameObject readTagPopup;
    public GameObject discountPopup;

    public Text payPopup_title;
    public Text payPopup_notice;
    public GameObject notice2;
    public GameObject setInvoice;
    public GameObject invoice;
    public GameObject prepay;
    public GameObject precard;
    public GameObject noprepay;

    public GameObject chargeToogle;
    public GameObject menuToogle;
    //고객조회용 팝업
    public Text popup2_title;
    public GameObject popup2_notice1;
    public GameObject popup2_notice2;
    public GameObject popup2_notice3;
    public GameObject popup2_notice4;
    public GameObject popup2_notice5;
    public GameObject popup2_notice6;
    public GameObject popup2_notice7;
    public GameObject popup2_notice8;
    public GameObject popup2_val1;
    public GameObject popup2_val2;
    public GameObject popup2_val3;
    public GameObject popup2_val4;
    public GameObject popup2_val5;
    public GameObject popup2_val6;
    public GameObject popup2_val7;
    public GameObject popup2_val8;
    public GameObject popup2_all_btn;
    public GameObject popup2_pointBtn;
    public InputField client_name;
    public GameObject multiSel;

    public GameObject err_popup;
    public Text err_str;
    public GameObject select_popup;
    public Text select_str;
    public GameObject progress_popup;
    public Text progress_str;

    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket1;

    PrepayTagUsage prepaytagUsage = new PrepayTagUsage();
    List<GameObject> m_chargeItemObj = new List<GameObject>();
    List<GameObject> m_menuusageItemObj = new List<GameObject>();
    List<ClientInfo> clients = new List<ClientInfo>();

    Payment payment = new Payment();
    List<OrderItem> printList = new List<OrderItem>();

    int pay_price = 0;//결제금액
    int prepay_price = 0;//현재 테이블의 선결제금액
    int pay_method = 0;//0-카드결제, 1-현금결제
    int used_prepay = 0;
    int used_point = 0;
    string client_id = "";
    string pretag_id = "";
    int preTagType = 0;
    int pay_popup_type = 0;
    int device_type = 0; //0 : 미사용, 1 : KICC(카드), 2 : KICC(현금, 사용자), 3 : KICC(현금, 개인), 4 : KIS(카드), 5 : KIS(현금 사용자, 개인)
    int installment_months = 0;
    string cancel_charge_id = ""; //충전취소하려는 결제아이디

    string rcvData = "";
    Thread waitMsg;
    string comRcvStr;                               // serial port receive string
    byte[] Ack = new byte[] { 0x06 };
    bool StxRcv, EtxRcv, EotRcv, EnqRcv, AckRcv, NakRcv, DleRcv;    // 수신 여부 체크값
    bool CrRcv;
    char FS = Convert.ToChar(0x1c);
    bool bWait = false;
    byte[] comRcvByte = new byte[1024];             // serial port receive string
    int rcvCnt = 0;
    IPAddress ipAdd;
    Socket socket;
    IPEndPoint remoteEP;

    string app_no = "";
    string credit_card_company = "";
    string credit_card_number = "";
    bool is_socket_open = false;

    private int Socket_Send(string ip, string port, byte[] sendData)
    {
        // Data buffer for incoming data.  
        byte[] bytes = new Byte[1024];
        try
        {
            ipAdd = System.Net.IPAddress.Parse(ip);
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            remoteEP = new IPEndPoint(ipAdd, int.Parse(port));
            socket.Connect(remoteEP);
            socket.SendTimeout = 300;
            socket.Send(sendData);
            byte[] data = new byte[1024];
            if (sendData[0] != 0x02)
            {
                Task.Delay(200).Wait();
                socket.Close();
                return 0;
            }
            if (sendData[0] != Ack[0])
            {
                int rlen = socket.Receive(data, data.Length, SocketFlags.None);
                if (rlen == 0)
                {
                    return 0;
                }
                DataReceived(data, rlen);
                return rlen;
            }
            else
            {
                return 1;
            }
        }
        catch (Exception ex)
        {
            return 0;
        }
    }

    private int Socket_Server()
    {
        // Data buffer for incoming data.  
        byte[] bytes = new Byte[1024];
        // Get Local IP Address
        string localIP;
        using (Socket udpsocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
        {
            udpsocket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = udpsocket.LocalEndPoint as IPEndPoint;
            localIP = endPoint.Address.ToString();
        }
        Debug.Log("Ip Get ..." + localIP);

        IPAddress ipAddress = IPAddress.Parse(localIP);
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 13189);

        // Create a TCP/IP socket.  
        using (Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
        {

            // Bind the socket to the local endpoint and
            // listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                // Start listening for connections.  
                Debug.Log("Waiting for a connection..." + localIP);
                // Program is suspended while waiting for an incoming connection.  
                Socket handler = listener.Accept();
                int bytesRec = handler.Receive(bytes);
                DataReceived(bytes, bytesRec);
                // 응답전송
                byte[] Ack3 = new byte[] { 0x06, 0x06, 0x06 };
                handler.Send(Ack3);
                Task.Delay(10).Wait();
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                listener.Shutdown(SocketShutdown.Both);
                listener.Close();
                return 1;
            }
            catch (Exception e)
            {
                Debug.Log("Error===============> " + e.Message);
                return 0;
            }
        }
    }

    private async void ReceiveWait()
    {
        while (bWait)
        {
            try
            {
                byte[] data = new byte[1024];
                int rlen = socket.Receive(data, data.Length, SocketFlags.None);
                if (rlen == 0)
                    continue;
                DataReceived(data, rlen);

                rcvData = "";
            }
            catch (Exception ex)
            {
                Debug.Log("Err=[" + ex.ToString() + "]");
                await Task.Delay(500); ;
            }
        }
    }

    private void DataReceived(byte[] buff, int len)
    {
        //byte[] buff = Encoding.Default.GetBytes(data);

        // Size 만큼 Read...
        for (int i = 0; i < buff.Length; i++)
        {
            switch (buff[i])
            {
                case 0x02:
                    rcvCnt = 0;
                    StxRcv = true;
                    break;
                case 0x03:
                    EtxRcv = true;
                    break;
                case 0x04:
                    EotRcv = true;
                    break;
                case 0x05:
                    EnqRcv = true;
                    rcvCnt = 0;
                    break;
                case 0x06:
                    AckRcv = true;
                    break;
                case 0x15:
                    NakRcv = true;
                    break;
                case 0x10:
                    DleRcv = true;
                    rcvCnt = 0;
                    break;
                case 0x0d:
                    if (!StxRcv)
                    {
                        CrRcv = true;
                        rcvCnt = 0;
                    }
                    break;
                default:
                    break;
            }
            //comRcvStr += buff[i].ToString("X2");
            if (rcvCnt < 1024)
                comRcvByte[rcvCnt++] = buff[i];
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(checkSdate());
        tagName.text = "";
        if(Global.cur_tagInfo.tag_id != "" && Global.cur_tagInfo.is_pay_after == 0)
        {
            if(Global.cur_tagInfo.rfid != "" && Global.cur_tagInfo.rfid != null)
            {
                SendRequest(Global.cur_tagInfo.rfid);
            }
            else if(Global.cur_tagInfo.qrcode != "" && Global.cur_tagInfo.qrcode != null)
            {
                SendRequest(Global.cur_tagInfo.qrcode);
            }
        }
        if(Global.setinfo.paymentDeviceInfo.ip != "")
        {
            ipAdd = System.Net.IPAddress.Parse(Global.setinfo.paymentDeviceInfo.ip);
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            remoteEP = new IPEndPoint(ipAdd, Global.setinfo.paymentDeviceInfo.port);
        }
        socketObj = Instantiate(socketPrefab);
        //socketObj = GameObject.Find("SocketIO");
        socket1 = socketObj.GetComponent<SocketIOComponent>();
        socket1.On("open", socketOpen);
        socket1.On("createOrder", createOrder);
        socket1.On("new_notification", new_notification);
        socket1.On("reload", reload);
        socket1.On("reloadPayment", reload);
        socket1.On("error", socketError);
        socket1.On("close", socketClose);
    }

    public void new_notification(SocketIOEvent e)
    {
        Global.alarm_cnt++;
    }

    public void reload(SocketIOEvent e)
    {
        if (payPopup.activeSelf)
            return;
        JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
        if (jsonNode["tag_id"] == Global.cur_tagInfo.tag_id)
        {
            StartCoroutine(GotoScene("prepayTagUsage"));
        }
    }

    public void createOrder(SocketIOEvent e)
    {
        JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
        try
        {
            //주문서 출력
            Debug.Log(jsonNode);
            if (jsonNode["is_tableChanged"].AsInt == 1)
            {
                //테이블 이동내역 출력
                string printStr = DocumentFactory.GetTableMove(jsonNode["old_tablename"], jsonNode["new_tablename"]);
                Debug.Log(printStr);
                byte[] sendData = NetUtils.StrToBytes(printStr);
                if (Global.setinfo.printerSet.printer1.useset != 1 && Global.setinfo.printerSet.printer1.ip_baudrate != "")
                {
                    Socket_Send(Global.setinfo.printerSet.printer1.ip_baudrate, Global.setinfo.printerSet.printer1.port.ToString(), sendData);
                }
                if (Global.setinfo.printerSet.printer2.useset != 1 && Global.setinfo.printerSet.printer2.ip_baudrate != "")
                {
                    Socket_Send(Global.setinfo.printerSet.printer2.ip_baudrate, Global.setinfo.printerSet.printer2.port.ToString(), sendData);
                }
                if (Global.setinfo.printerSet.printer3.useset != 1 && Global.setinfo.printerSet.printer3.ip_baudrate != "")
                {
                    Socket_Send(Global.setinfo.printerSet.printer3.ip_baudrate, Global.setinfo.printerSet.printer3.port.ToString(), sendData);
                }
                if (Global.setinfo.printerSet.printer4.useset != 1 && Global.setinfo.printerSet.printer4.ip_baudrate != "")
                {
                    Socket_Send(Global.setinfo.printerSet.printer4.ip_baudrate, Global.setinfo.printerSet.printer4.port.ToString(), sendData);
                }
            }
            else
            {
                string kitorderno = Global.GetNoFormat(jsonNode["orderSeq"].AsInt);
                string tableName = jsonNode["tableName"];
                string tagName = jsonNode["tagName"];
                string is_pack = (jsonNode["is_pack"].AsInt) == 0 ? "" : "T";
                List<OrderItem> orders = new List<OrderItem>();
                JSONNode orderlist = JSON.Parse(jsonNode["menulist"].ToString()/*.Replace("\"", "")*/);
                for (int i = 0; i < orderlist.Count; i++)
                {
                    OrderItem order = new OrderItem();
                    order.product_name = orderlist[i]["product_name"];
                    order.quantity = orderlist[i]["quantity"].AsInt;
                    order.product_unit_price = orderlist[i]["product_unit_price"].AsInt;
                    order.paid_price = orderlist[i]["paid_price"].AsInt;
                    order.is_service = orderlist[i]["is_service"].AsInt;
                    order.kit01 = orderlist[i]["kit01"].AsInt;
                    order.kit02 = orderlist[i]["kit02"].AsInt;
                    order.kit03 = orderlist[i]["kit03"].AsInt;
                    order.kit04 = orderlist[i]["kit04"].AsInt;
                    orders.Add(order);
                }
                DateTime orderTime = DateTime.Parse(jsonNode["reg_datetime"]);
                if (Global.setinfo.printerSet.menu_output == 0) //메뉴개별출력 사용
                {
                    for (int i = 0; i < orders.Count; i++)
                    {
                        if (orders[i].kit01 == 1 && Global.setinfo.printerSet.printer1.useset != 1 && Global.setinfo.printerSet.printer1.ip_baudrate != "")
                        {
                            string printStr = DocumentFactory.GetOrderItemSheet("1", kitorderno, orders[i], orderTime, tableName, is_pack, isCat: false);
                            byte[] sendData = NetUtils.StrToBytes(printStr);
                            Socket_Send(Global.setinfo.printerSet.printer1.ip_baudrate, Global.setinfo.printerSet.printer1.port.ToString(), sendData);
                        }
                        if (orders[i].kit02 == 1 && Global.setinfo.printerSet.printer2.useset != 1 && Global.setinfo.printerSet.printer2.ip_baudrate != "")
                        {
                            string printStr = DocumentFactory.GetOrderItemSheet("2", kitorderno, orders[i], orderTime, tableName, is_pack, isCat: false);
                            byte[] sendData = NetUtils.StrToBytes(printStr);
                            Socket_Send(Global.setinfo.printerSet.printer2.ip_baudrate, Global.setinfo.printerSet.printer2.port.ToString(), sendData);
                        }
                        if (orders[i].kit03 == 1 && Global.setinfo.printerSet.printer3.useset != 1 && Global.setinfo.printerSet.printer3.ip_baudrate != "")
                        {
                            string printStr = DocumentFactory.GetOrderItemSheet("3", kitorderno, orders[i], orderTime, tableName, is_pack, isCat: false);
                            byte[] sendData = NetUtils.StrToBytes(printStr);
                            Socket_Send(Global.setinfo.printerSet.printer3.ip_baudrate, Global.setinfo.printerSet.printer3.port.ToString(), sendData);
                        }
                        if (orders[i].kit04 == 1 && Global.setinfo.printerSet.printer4.useset != 1 && Global.setinfo.printerSet.printer4.ip_baudrate != "")
                        {
                            string printStr = DocumentFactory.GetOrderItemSheet("4", kitorderno, orders[i], orderTime, tableName, is_pack, isCat: false);
                            byte[] sendData = NetUtils.StrToBytes(printStr);
                            Socket_Send(Global.setinfo.printerSet.printer4.ip_baudrate, Global.setinfo.printerSet.printer4.port.ToString(), sendData);
                        }
                    }
                }
                else //미사용
                {
                    if (Global.setinfo.printerSet.printer1.useset != 1 && Global.setinfo.printerSet.printer1.ip_baudrate != "")
                    {
                        int cnt = 0;
                        while (cnt < orders.Count)
                        {
                            if (orders[cnt].kit01 != 1)
                            {
                                orders.RemoveAt(cnt);
                            }
                            else
                            {
                                cnt++;
                            }
                        }
                        string printStr = DocumentFactory.GetOrderSheet("1", kitorderno, orders, orderTime, tableName, is_pack, isCat: false);
                        byte[] sendData = NetUtils.StrToBytes(printStr);
                        Socket_Send(Global.setinfo.printerSet.printer1.ip_baudrate, Global.setinfo.printerSet.printer1.port.ToString(), sendData);
                    }
                    if (Global.setinfo.printerSet.printer2.useset != 1 && Global.setinfo.printerSet.printer2.ip_baudrate != "")
                    {
                        int cnt = 0;
                        while (cnt < orders.Count)
                        {
                            if (orders[cnt].kit02 != 1)
                            {
                                orders.RemoveAt(cnt);
                            }
                            else
                            {
                                cnt++;
                            }
                        }
                        string printStr = DocumentFactory.GetOrderSheet("2", kitorderno, orders, orderTime, tableName, is_pack, isCat: false);
                        byte[] sendData = NetUtils.StrToBytes(printStr);
                        Socket_Send(Global.setinfo.printerSet.printer2.ip_baudrate, Global.setinfo.printerSet.printer2.port.ToString(), sendData);
                    }
                    if (Global.setinfo.printerSet.printer3.useset != 1 && Global.setinfo.printerSet.printer3.ip_baudrate != "")
                    {
                        int cnt = 0;
                        while (cnt < orders.Count)
                        {
                            if (orders[cnt].kit03 != 1)
                            {
                                orders.RemoveAt(cnt);
                            }
                            else
                            {
                                cnt++;
                            }
                        }
                        string printStr = DocumentFactory.GetOrderSheet("3", kitorderno, orders, orderTime, tableName, is_pack, isCat: false);
                        byte[] sendData = NetUtils.StrToBytes(printStr);
                        Socket_Send(Global.setinfo.printerSet.printer3.ip_baudrate, Global.setinfo.printerSet.printer3.port.ToString(), sendData);
                    }
                    if (Global.setinfo.printerSet.printer4.useset != 1 && Global.setinfo.printerSet.printer4.ip_baudrate != "")
                    {
                        int cnt = 0;
                        while (cnt < orders.Count)
                        {
                            if (orders[cnt].kit04 != 1)
                            {
                                orders.RemoveAt(cnt);
                            }
                            else
                            {
                                cnt++;
                            }
                        }
                        string printStr = DocumentFactory.GetOrderSheet("4", kitorderno, orders, orderTime, tableName, is_pack, isCat: false);
                        byte[] sendData = NetUtils.StrToBytes(printStr);
                        Socket_Send(Global.setinfo.printerSet.printer4.ip_baudrate, Global.setinfo.printerSet.printer4.port.ToString(), sendData);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
        if (jsonNode["tag_id"] == Global.cur_tagInfo.tag_id)
        {
            StartCoroutine(GotoScene("prepayTagUsage"));
        }
    }

    public void socketOpen(SocketIOEvent e)
    {
        if (is_socket_open)
            return;
        is_socket_open = true;
        string data = "{\"pub_id\":\"" + Global.userinfo.pub.id + "\"," +
            "\"no\":\"" + Global.setinfo.pos_no + "\"}";
        Debug.Log(data);
        socket1.Emit("posSetInfo", JSONObject.Create(data));
        Debug.Log("[SocketIO] Open received: " + e.name + " " + e.data);
    }

    public void socketError(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Error received: " + e.name + " " + e.data);
    }

    public void socketClose(SocketIOEvent e)
    {
        is_socket_open = false;
        Debug.Log("[SocketIO] Close received: " + e.name + " " + e.data);
    }

    void sendCheckSdateApi(DateTime ctime)
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", ctime.Year, ctime.Month, ctime.Day));
        WWW www = new WWW(Global.api_url + Global.check_sdate_api, form);
        StartCoroutine(CheckSdateProcess(www, ctime));
    }

    IEnumerator checkSdate()
    {
        while (true)
        {
            DateTime ctime = Global.GetSdate(false);
            if (Global.old_day < ctime)
            {
                sendCheckSdateApi(ctime);
            }
            yield return new WaitForSeconds(Global.checktime);
        }
    }

    IEnumerator CheckSdateProcess(WWW www, DateTime ctime)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["result"].AsInt == 1)
            {
                //결제 완료
                Global.is_applied_state = true;
                Global.old_day = ctime;
            }
            else
            {
                Global.is_applied_state = false;
            }
            //if (!Global.is_applied_state)
            //{
            //    int mode = jsonNode["mode"].AsInt;
            //    if (mode == 0)
            //    {
            //        err_str.text = "영업일을 " + string.Format("{0:D4}-{1:D2}-{2:D2}", ctime.Year, ctime.Month, ctime.Day) + " 로 변경하시겠습니까?\n영업일을 변경하시려면 모든 테이블의 결제를 완료하세요.";
            //    }
            //    else
            //    {
            //        err_str.text = "결제를 완료하지 않은 재결제가 있습니다. 영업일 변경을 위해 결제를 완료해주세요.\n취소시간: " + jsonNode["closetime"];
            //    }
            //    err_popup.SetActive(true);
            //}
            //else
            {
                err_popup.SetActive(true);
                err_str.text = "영업일자가 " + string.Format("{0:D4}-{1:D2}-{2:D2}", ctime.Year, ctime.Month, ctime.Day) + " 로 변경되었습니다.";
            }
        }
    }

    IEnumerator Destroy_Object(GameObject obj)
    {
        DestroyImmediate(obj);
        yield return null;
    }

    public void onRegTag()
    {
        extraMenuPopup.SetActive(false);
        regTagPopup.transform.Find("background/title").GetComponent<Text>().text = "TAG 등록";
        regTagPopup.SetActive(true);
        regTagPopup.transform.Find("background/val1").GetComponent<InputField>().text = "";
        regTagPopup.transform.Find("background/val2").GetComponent<InputField>().text = "";
        regTagPopup.transform.Find("background/val3").GetComponent<InputField>().text = Global.userinfo.pub.prepay_tag_period.ToString();
        regTagPopup.transform.Find("background/saveBtn").GetComponent<Button>().onClick.RemoveAllListeners();
        regTagPopup.transform.Find("background/saveBtn").GetComponent<Button>().onClick.AddListener(delegate () { onConfirmRegPopup(); });
    }

    public void onBack()
    {
        Global.cur_tagInfo = new CurTagInfo();
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("endpay");
        }
        StartCoroutine(GotoScene("main"));
    }

    public void onCancelDiscount()
    {
        discountPopup.SetActive(false);
    }

    public void onSaveDiscount()
    {
        string price = discountPopup.transform.Find("background/val").GetComponent<InputField>().text;
        if (price == "" || price == null)
        {
            err_popup.SetActive(true);
            err_str.text = "할인금액을 정확히 입력하세요.";
        }
        else
        {
            WWWForm form = new WWWForm();
            form.AddField("pub_id", Global.userinfo.pub.id);
            form.AddField("tag_id", Global.cur_tagInfo.tag_id);
            form.AddField("price", price);
            DateTime dt = Global.GetSdate();
            form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
            WWW www = new WWW(Global.api_url + Global.add_discount_api, form);
            StartCoroutine(AddDiscount(www));
        }
    }

    IEnumerator AddDiscount(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            StartCoroutine(GotoScene("prepayTagUsage"));
        }
        discountPopup.SetActive(false);
    }

    public void menuOrder()
    {
        if (Global.cur_tagInfo.tag_id == "" || Global.cur_tagInfo.tag_id == null)
        {
            err_str.text = "먼저 TAG를 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("endpay");
        }
        StartCoroutine(GotoScene("prepayTagMenuOrder"));
    }

    public void onConvertTag()
    {
        extraMenuPopup.SetActive(false);
        if (Global.cur_tagInfo.tag_id == "" || Global.cur_tagInfo.tag_id == null)
        {
            err_str.text = "먼저 TAG를 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
        regTagPopup.transform.Find("background/title").GetComponent<Text>().text = "TAG 교환";
        regTagPopup.SetActive(true);
        regTagPopup.transform.Find("background/val1").GetComponent<InputField>().text = Global.cur_tagInfo.rfid;
        regTagPopup.transform.Find("background/val2").GetComponent<InputField>().text = "!" + Global.cur_tagInfo.qrcode;
        regTagPopup.transform.Find("background/val3").GetComponent<InputField>().text = Global.userinfo.pub.prepay_tag_period.ToString();
        regTagPopup.transform.Find("background/saveBtn").GetComponent<Button>().onClick.RemoveAllListeners();
        regTagPopup.transform.Find("background/saveBtn").GetComponent<Button>().onClick.AddListener(delegate () { ConvertTag(); });
    }

    void ConvertTag()
    {
        string rfid = regTagPopup.transform.Find("background/val1").GetComponent<InputField>().text;
        string qr = regTagPopup.transform.Find("background/val2").GetComponent<InputField>().text;
        if (rfid == "" && qr == "" || qr != "" && qr.IndexOf("!") != 0)
        {
            err_popup.SetActive(true);
            err_str.text = "TAG 정보를 정확히 입력하세요.";
            return;
        }
        WWWForm form = new WWWForm();
        form.AddField("tag_id", Global.cur_tagInfo.tag_id);
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("rfid", rfid);
        if(qr != "")
        {
            try
            {
                qr = qr.Remove(0, 1);
                form.AddField("qrcode", qr);
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
        form.AddField("period", regTagPopup.transform.Find("background/val3").GetComponent<InputField>().text);
        form.AddField("is_pay_after", 0);
        WWW www = new WWW(Global.api_url + Global.convert_tag_api, form);
        StartCoroutine(RegTagProcess(www));
    }

    public void onCancelRegPopup()
    {
        regTagPopup.SetActive(false);
    }

    void onConfirmRegPopup()
    {
        string rfid = regTagPopup.transform.Find("background/val1").GetComponent<InputField>().text;
        string qr = regTagPopup.transform.Find("background/val2").GetComponent<InputField>().text;
        if(rfid == "" && qr == "" || qr != "" && qr.IndexOf("!") != 0)
        {
            err_popup.SetActive(true);
            err_str.text = "TAG 정보를 정확히 입력하세요.";
            return;
        }
        WWWForm form = new WWWForm();
        form.AddField("rfid", rfid);
        try
        {
            if (qr != "")
            {
                form.AddField("qrcode", qr.Remove(0, 1));
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
        form.AddField("period", regTagPopup.transform.Find("background/val3").GetComponent<InputField>().text);
        form.AddField("is_pay_after", 0);
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.reg_tag_api, form);
        StartCoroutine(RegTagProcess(www));
    }

    IEnumerator RegTagProcess(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            regTagPopup.SetActive(false);
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            if (jsonNode["suc"].AsInt == 1)
            {
                StartCoroutine(LoadPrepayTagUsageInfo(jsonNode));
            }
            else
            {
                if (Global.setinfo.pos_no == 1)
                {
                    socket1.Emit("endpay");
                }
                err_popup.SetActive(true);
                err_str.text = jsonNode["msg"];
            }
        }
        else
        {
            if (Global.setinfo.pos_no == 1)
            {
                socket1.Emit("endpay");
            }
            err_str.text = "태그 등록에 실패하였습니다.";
            err_popup.SetActive(true);
        }
    }

    void SendRequest(string data)
    {
        if(data == null)
        {
            return;
        }
        WWWForm form = new WWWForm();
        form.AddField("tag_data", data);
        form.AddField("pub_id", Global.userinfo.pub.id);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        WWW www = new WWW(Global.api_url + Global.find_tag_usage_api, form);
        StartCoroutine(SelTagProcess(www, data));
    }

    float send_time = 0f;
    void checkTag(string value)
    {
        if (value == "" || value == null)
            return;
        send_time += Time.deltaTime;
        StartCoroutine(sendCheckTag(value, send_time));
    }

    IEnumerator sendCheckTag(string str, float stime)
    {
        yield return new WaitForSeconds(0.1f);
        if (send_time != stime)
        {
            yield break;
        }
        SendRequest(str);
        readTagPopup.transform.Find("tag").GetComponent<InputField>().text = "";
        send_time = 0f;
        readTagPopup.SetActive(false);
    }

    public void onSelTagPopup()
    {
        readTagPopup.SetActive(true);
        readTagPopup.transform.Find("tag").GetComponent<InputField>().Select();
        readTagPopup.transform.Find("tag").GetComponent<InputField>().ActivateInputField();
        readTagPopup.transform.Find("tag").GetComponent<InputField>().onValueChanged.AddListener((value) => {
                    checkTag(value);
                }
        );
    }

    IEnumerator SelTagProcess(WWW www, string data)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            StartCoroutine(LoadPrepayTagUsageInfo(jsonNode));
        }
        else
        {
            if (Global.setinfo.pos_no == 1)
            {
                socket1.Emit("endpay");
            }
            Global.cur_tagInfo = new CurTagInfo();
        }
    }

    IEnumerator LoadPrepayTagUsageInfo(JSONNode jsonNode)
    {
        while (chargeItemParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(chargeItemParent.transform.GetChild(0).gameObject));
        }
        while (chargeItemParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }

        while (menuUsageParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(menuUsageParent.transform.GetChild(0).gameObject));
        }
        while (menuUsageParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }

        if(jsonNode["suc"].AsInt == 1)
        {
            if(jsonNode["is_pay_after"].AsInt == 1)
            {
                Global.cur_tagInfo = new CurTagInfo();
                tagName.text = "";
                prepaytagUsage.remain_price = 0;
                err_str.text = "후불TAG 입니다.";
                err_popup.SetActive(true);
            }
            else
            {
                int status = jsonNode["status"].AsInt;
                switch (status)
                {
                    case 1:
                        {
                            Global.cur_tagInfo = new CurTagInfo();
                            tagName.text = "";
                            prepaytagUsage.remain_price = 0;
                            err_str.text = "이용할 수 없는 태그입니다.";
                            err_popup.SetActive(true);
                        }
                        break;
                    case 2:
                        {
                            Global.cur_tagInfo.charge = jsonNode["charge"].AsInt;
                            Global.cur_tagInfo.period = jsonNode["period"].AsInt;
                            Global.cur_tagInfo.reg_datetime = jsonNode["reg_datetime"];
                            Global.cur_tagInfo.remain = jsonNode["remain"].AsInt;
                            Global.cur_tagInfo.qrcode = jsonNode["qrcode"];
                            Global.cur_tagInfo.rfid = jsonNode["rfid"];
                            Global.cur_tagInfo.tag_id = jsonNode["tag_id"];
                            Global.cur_tagInfo.tag_name = jsonNode["tag_name"];
                            Global.cur_tagInfo.is_pay_after = jsonNode["is_pay_after"].AsInt;
                            Global.cur_tagInfo.table_name = jsonNode["table_name"];
                            prepaytagUsage.remain_price = Global.cur_tagInfo.remain;
                            tagName.text = Global.cur_tagInfo.tag_name;
                            if (Global.cur_tagInfo.table_name != "" && Global.cur_tagInfo.table_name != null)
                            {
                                tagName.text += " / " + Global.cur_tagInfo.table_name;
                            }
                            JSONNode charge_list = JSON.Parse(jsonNode["chargelist"].ToString()/*.Replace("\"", "")*/);
                            prepaytagUsage.chargeItemlist = new List<ChargeItemInfo>();
                            prepaytagUsage.charge_sum_price = 0;

                            for (int i = 0; i < charge_list.Count; i++)
                            {
                                ChargeItemInfo chargeInfo = new ChargeItemInfo();
                                chargeInfo.card_no = charge_list[i]["card_no"];
                                chargeInfo.card_type = charge_list[i]["card_type"];
                                chargeInfo.price = charge_list[i]["price"].AsInt;
                                chargeInfo.charge_time = charge_list[i]["charge_time"];
                                chargeInfo.id = charge_list[i]["id"];
                                chargeInfo.device_type = charge_list[i]["device_type"].AsInt;
                                chargeInfo.appno = charge_list[i]["appno"];
                                chargeInfo.payTime = charge_list[i]["payTime"];
                                prepaytagUsage.chargeItemlist.Add(chargeInfo);
                                prepaytagUsage.charge_sum_price += chargeInfo.price;
                                GameObject tmp = Instantiate(chargeItem);
                                tmp.transform.SetParent(chargeItemParent.transform);
                                //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                                //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                                //float left = 0;
                                //float right = 0;
                                //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                                //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                                //tmp.transform.localScale = Vector3.one;
                                tmp.transform.Find("time").GetComponent<Text>().text = chargeInfo.charge_time;
                                tmp.transform.Find("card").GetComponent<Text>().text = chargeInfo.card_type;
                                tmp.transform.Find("cardno").GetComponent<Text>().text = chargeInfo.card_no;
                                tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(chargeInfo.price);
                                tmp.transform.Find("id").GetComponent<Text>().text = chargeInfo.id.ToString();
                                GameObject toggleObj = tmp.transform.Find("check").gameObject;
                                tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                                tmp.GetComponent<Button>().onClick.AddListener(delegate () { onSelChargeItem(toggleObj); });
                                m_chargeItemObj.Add(tmp);
                            }
                            chargeSumTxt.text = Global.GetPriceFormat(prepaytagUsage.charge_sum_price);

                            JSONNode order_list = JSON.Parse(jsonNode["orderlist"].ToString()/*.Replace("\"", "")*/);
                            prepaytagUsage.menuOrderlist = new List<PrepayTagMenuOrderItemInfo>();
                            prepaytagUsage.order_sum_price = 0;
                            for (int i = 0; i < order_list.Count; i++)
                            {
                                PrepayTagMenuOrderItemInfo orderInfo = new PrepayTagMenuOrderItemInfo();
                                orderInfo.order_id = order_list[i]["order_id"];
                                orderInfo.menu_id = order_list[i]["menu_id"];
                                orderInfo.amount = order_list[i]["amount"].AsInt;
                                orderInfo.price = order_list[i]["price"].AsInt;
                                orderInfo.status = order_list[i]["status"].AsInt;
                                orderInfo.menu_name = order_list[i]["menu_name"];
                                orderInfo.order_time = order_list[i]["order_time"];
                                orderInfo.is_service = order_list[i]["is_service"];
                                prepaytagUsage.menuOrderlist.Add(orderInfo);
                                GameObject tmp = Instantiate(menuUsageItem);
                                tmp.transform.SetParent(menuUsageParent.transform);
                                //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                                //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                                //float left = 0;
                                //float right = 0;
                                //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                                //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                                //tmp.transform.localScale = Vector3.one;
                                tmp.transform.Find("time").GetComponent<Text>().text = orderInfo.order_time;
                                tmp.transform.Find("menu").GetComponent<Text>().text = orderInfo.menu_name;
                                tmp.transform.Find("cnt").GetComponent<Text>().text = orderInfo.amount.ToString();
                                tmp.transform.Find("menu_id").GetComponent<Text>().text = orderInfo.menu_id.ToString();
                                tmp.transform.Find("order_id").GetComponent<Text>().text = orderInfo.order_id.ToString();
                                tmp.transform.Find("is_service").GetComponent<Text>().text = orderInfo.is_service.ToString();
                                if (orderInfo.is_service == 1)
                                {
                                    tmp.transform.Find("price").GetComponent<Text>().text = "0";
                                }
                                else
                                {
                                    tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(orderInfo.price);
                                    prepaytagUsage.order_sum_price += orderInfo.price;
                                }
                                GameObject toggleObj = tmp.transform.Find("check").gameObject;
                                tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                                tmp.GetComponent<Button>().onClick.AddListener(delegate () { onSelItem(toggleObj); });
                                m_menuusageItemObj.Add(tmp);
                            }
                            usageSumTxt.text = Global.GetPriceFormat(prepaytagUsage.order_sum_price);
                            remainTxt.text = Global.GetPriceFormat(prepaytagUsage.remain_price);
                            string payInfo = getJsonResult();
                            Debug.Log(payInfo);
                            socket1.Emit("prepayInfo", JSONObject.Create(payInfo));
                        }
                        break;
                    case 3:
                        {
                            Global.cur_tagInfo = new CurTagInfo();
                            tagName.text = "";
                            prepaytagUsage.remain_price = 0;
                            err_str.text = "이용할 수 없는 태그입니다.";
                            err_popup.SetActive(true);
                        }
                        break;
                    case 4:
                        {
                            Global.cur_tagInfo = new CurTagInfo();
                            tagName.text = "";
                            prepaytagUsage.remain_price = 0;
                            err_str.text = "현재 셀프 이용 중인 TAG 입니다.";
                            err_popup.SetActive(true);
                        }
                        break;
                }
            }
        }
        else
        {
            Global.cur_tagInfo = new CurTagInfo();
            tagName.text = "";
            prepaytagUsage.remain_price = 0;
            err_str.text = jsonNode["msg"];
            err_popup.SetActive(true);
        }
    }

    void onSelItem(GameObject toggleObj)
    {
        toggleObj.GetComponent<Toggle>().isOn = !toggleObj.GetComponent<Toggle>().isOn;
    }

    void onSelChargeItem(GameObject toggleObj)
    {
        bool status = !toggleObj.GetComponent<Toggle>().isOn;
        for (int i = 0; i < m_chargeItemObj.Count; i ++)
        {
            m_chargeItemObj[i].transform.Find("check").GetComponent<Toggle>().isOn = false;
        }
        toggleObj.GetComponent<Toggle>().isOn = status;
    }

    public void onChargeToggle()
    {
        for (int i = 0; i < chargeItemParent.transform.childCount; i++)
        {
            try
            {
                chargeItemParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn = chargeToogle.GetComponent<Toggle>().isOn;
            }
            catch (Exception ex)
            {

            }

        }
    }

    public void onMenuToggle()
    {
        for (int i = 0; i < menuUsageParent.transform.childCount; i++)
        {
            try
            {
                menuUsageParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn = menuToogle.GetComponent<Toggle>().isOn;
            }
            catch (Exception ex)
            {

            }

        }
    }

    public void onExtraMenu()
    {
        if (extraMenuPopup.activeSelf)
        {
            extraMenuPopup.SetActive(false);
        }
        else
        {
            extraMenuPopup.SetActive(true);
        }
    }

    public void onCloseTagSearchPopup()
    {
        readTagPopup.SetActive(false);
    }

    public void onCancelOrder()
    {
        extraMenuPopup.SetActive(false);
        if (Global.cur_tagInfo.tag_id == "" || Global.cur_tagInfo.tag_id == null)
        {
            err_str.text = "먼저 TAG를 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
        int cancel_order_cnt = 0;
        string cinfo = "[";
        for (int i = 0; i < menuUsageParent.transform.childCount; i++)
        {
            try
            {
                if (menuUsageParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
                {
                    if (cancel_order_cnt == 0)
                    {
                        cinfo += "{";
                    }
                    else
                    {
                        cinfo += ",{";
                    }
                    cancel_order_cnt++;
                    cinfo += "\"order_id\":\"" + menuUsageParent.transform.GetChild(i).Find("order_id").GetComponent<Text>().text + "\"}";
                }
            }
            catch (Exception ex)
            {

            }

        }
        cinfo += "]";
        if(cinfo == "[]")
        {
            err_popup.SetActive(true);
            err_str.text = "취소할 주문을 선택하세요.";
        }
        else
        {
            WWWForm form = new WWWForm();
            form.AddField("order_info", cinfo);
            form.AddField("pub_id", Global.userinfo.pub.id);
            form.AddField("tag_id", Global.cur_tagInfo.tag_id);//선불관리에서 주문취소시 해당 취소금액을 선불태그에 복귀시키기 위해 선불관리에서만 이용.
            DateTime dt = Global.GetSdate();
            form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
            form.AddField("pos_no", Global.setinfo.pos_no);
            WWW www = new WWW(Global.api_url + Global.cancel_order_api, form);
            StartCoroutine(OutputSheetProcess(www));
        }
    }

    public void onService()
    {
        extraMenuPopup.SetActive(false);
        if (Global.cur_tagInfo.tag_id == "" || Global.cur_tagInfo.tag_id == null)
        {
            err_str.text = "먼저 TAG를 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
        string cinfo = "[";
        int selected_cnt = 0;
        for (int i = 0; i < menuUsageParent.transform.childCount; i++)
        {
            try
            {
                if (menuUsageParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
                {
                    if (selected_cnt == 0)
                    {
                        cinfo += "{";
                    }
                    else
                    {
                        cinfo += ",{";
                    }
                    selected_cnt++;
                    cinfo += "\"order_id\":\"" + menuUsageParent.transform.GetChild(i).Find("order_id").GetComponent<Text>().text + "\"}";
                }
            }
            catch (Exception ex)
            {

            }

        }
        cinfo += "]";
        if(cinfo == "[]")
        {
            err_str.text = "서비스 설정할 주문을 선택하세요.";
            err_popup.SetActive(true);
        }
        else
        {
            WWWForm form = new WWWForm();
            form.AddField("order_info", cinfo);
            form.AddField("pub_id", Global.userinfo.pub.id);
            form.AddField("tag_id", Global.cur_tagInfo.tag_id);//선불관리에서 서비스처리시 해당 금액을 선불태그에 복귀시키기 위해 이용
            DateTime dt = Global.GetSdate();
            form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
            form.AddField("pos_no", Global.setinfo.pos_no);
            WWW www = new WWW(Global.api_url + Global.service_order_api, form);
            StartCoroutine(OutputSheetProcess(www));
        }
    }

    public void onDiscount()
    {
        extraMenuPopup.SetActive(false);
        if (Global.cur_tagInfo.tag_id == "" || Global.cur_tagInfo.tag_id == null)
        {
            err_str.text = "먼저 TAG를 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
        discountPopup.SetActive(true);
    }

    public void onCloseTagPopup()
    {
        changeTagPopup.SetActive(false);
    }

    public void onSaveTagInfo()
    {
        string rfid = changeTagPopup.transform.Find("background/val1").GetComponent<InputField>().text;
        string qr = changeTagPopup.transform.Find("background/val2").GetComponent<InputField>().text;
        string period = changeTagPopup.transform.Find("background/val3").GetComponent<InputField>().text;
        if(rfid == "" && qr == "")
        {
            err_popup.SetActive(true);
            err_str.text = "태그 정보를 정확히 입력하세요.";
        }
        else
        {
            WWWForm form = new WWWForm();
            if (rfid != "")
            {
                form.AddField("rfid", rfid);
            }
            if (qr != "")
            {
                try
                {
                    qr = qr.Remove(0, 1);
                    form.AddField("qr", qr);
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }
            if (period != "")
            {
                form.AddField("period", period);
            }
            form.AddField("tag_id", Global.cur_tagInfo.tag_id);
            form.AddField("pub_id", Global.userinfo.pub.id);
            WWW www = new WWW(Global.api_url + Global.change_tag_api, form);
            StartCoroutine(ProcessChangeTagInfo(www));
        }
    }

    IEnumerator ProcessChangeTagInfo(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            try
            {
                bool is_set = false;
                if (changeTagPopup.transform.Find("background/val2").GetComponent<InputField>().text != "")
                {
                    string qr = changeTagPopup.transform.Find("background/val2").GetComponent<InputField>().text;
                    Global.cur_tagInfo.qrcode = qr.Remove(0, 1);
                    if(Global.cur_tagInfo.qrcode != "" && Global.cur_tagInfo.qrcode != null)
                    {
                        Global.cur_tagInfo.tag_name = Global.cur_tagInfo.qrcode;
                        is_set = true;
                    }
                }
                if (changeTagPopup.transform.Find("background/val1").GetComponent<InputField>().text != "")
                {
                    Global.cur_tagInfo.rfid = changeTagPopup.transform.Find("background/val1").GetComponent<InputField>().text;
                    if(Global.cur_tagInfo.rfid != "" && Global.cur_tagInfo.rfid != null && !is_set)
                    {
                        Global.cur_tagInfo.tag_name = Global.cur_tagInfo.rfid;
                        is_set = true;
                }
                }
                if (changeTagPopup.transform.Find("background/val3").GetComponent<InputField>().text != "")
                {
                    Global.cur_tagInfo.period = int.Parse(changeTagPopup.transform.Find("background/val3").GetComponent<InputField>().text);
                }

                tagName.text = Global.cur_tagInfo.tag_name;
                if (Global.cur_tagInfo.table_name != "" && Global.cur_tagInfo.table_name != null)
                {
                    tagName.text += " / " + Global.cur_tagInfo.table_name;
                }
            }
            catch (Exception ex)
            {

            }
        }
        changeTagPopup.SetActive(false);
    }

    public void onChangeTag()
    {
        extraMenuPopup.SetActive(false);
        if(Global.cur_tagInfo.tag_id != "")
        {
            changeTagPopup.SetActive(true);
            changeTagPopup.transform.Find("background/val1").GetComponent<InputField>().text = Global.cur_tagInfo.rfid;
            changeTagPopup.transform.Find("background/val2").GetComponent<InputField>().text = "!" + Global.cur_tagInfo.qrcode;
            if (Global.cur_tagInfo.period == 0)
            {
                changeTagPopup.transform.Find("background/val3").GetComponent<InputField>().text = Global.userinfo.pub.prepay_tag_period.ToString();
            }
            else
            {
                changeTagPopup.transform.Find("background/val3").GetComponent<InputField>().text = Global.cur_tagInfo.period.ToString();
            }
        }
    }

    public void outputOrder()
    {
        extraMenuPopup.SetActive(false);
        //if (Global.cur_tagInfo.tag_id == "" || Global.cur_tagInfo.tag_id == null)
        //{
        //    err_str.text = "먼저 TAG를 선택하세요.";
        //    err_popup.SetActive(true);
        //    return;
        //}
        //if (Global.setinfo.paymentDeviceInfo.port == 0 || Global.setinfo.paymentDeviceInfo.ip == "" || Global.setinfo.paymentDeviceInfo.ip == null)
        //{
        //    err_str.text = "결제단말기 세팅을 진행하세요.";
        //    err_popup.SetActive(true);
        //    return;
        //}
        //if (Global.setinfo.paymentDeviceInfo.type == 0)
        //{
        //    err_str.text = "결제단말기 통신방식을 ip로 설정하세요.";
        //    err_popup.SetActive(true);
        //    return;
        //}
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("tag_id", Global.cur_tagInfo.tag_id);
        WWW www = new WWW(Global.api_url + Global.get_output_orderlist_api, form);
        StartCoroutine(getOutputOrderlist(www));
    }

    IEnumerator getOutputOrderlist(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                Debug.Log(jsonNode);
                int total_price = jsonNode["total_price"].AsInt;
                List<OrderItem> orders = new List<OrderItem>();
                JSONNode orderlist = JSON.Parse(jsonNode["orderItemList"].ToString()/*.Replace("\"", "")*/);
                for (int i = 0; i < orderlist.Count; i++)
                {
                    OrderItem order = new OrderItem();
                    order.product_name = orderlist[i]["product_name"];
                    order.quantity = orderlist[i]["quantity"].AsInt;
                    order.product_unit_price = orderlist[i]["product_unit_price"].AsInt;
                    order.paid_price = orderlist[i]["paid_price"].AsInt;
                    order.is_service = orderlist[i]["is_service"].AsInt;
                    order.is_self = orderlist[i]["is_self"].AsInt;
                    order.capacity = orderlist[i]["capacity"].AsInt;
                    order.kit01 = orderlist[i]["kit01"].AsInt;
                    order.kit02 = orderlist[i]["kit02"].AsInt;
                    order.kit03 = orderlist[i]["kit03"].AsInt;
                    order.kit04 = orderlist[i]["kit04"].AsInt;
                    orders.Add(order);
                    Debug.Log(order.product_name);

                }
                string printStr = "";
                printStr = DocumentFactory.GetOrderListDetail(orders, total_price, title: "[이용내역]");
                Debug.Log(printStr);
                byte[] sendData = NetUtils.StrToBytes(printStr);
                Socket_Send(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), sendData);
            }
        }
    }

    public void ontableMove()
    {
        extraMenuPopup.SetActive(false);
        if (Global.cur_tagInfo.tag_id == "" || Global.cur_tagInfo.tag_id == null)
        {
            err_str.text = "먼저 TAG를 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
        Global.moveTableType = 1;//선불태그 테이블에 등록
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("endpay");
        }
        StartCoroutine(GotoScene("tableMove"));
    }

    public void onCancelCharge()
    {
        device_type = 0;
        app_no = "";
        credit_card_company = "";
        credit_card_number = "";
        if (Global.cur_tagInfo.tag_id == "" || Global.cur_tagInfo.tag_id == null)
        {
            err_str.text = "먼저 TAG를 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
        //충전합계와 잔액이 같아야 하며 이용합계가 0일때만 충전취소 가능
        if(chargeSumTxt.text != remainTxt.text || usageSumTxt.text != "0")
        {
            err_str.text = "정산을 먼저 한 후에 취소가 가능합니다.";
            err_popup.SetActive(true);
            return;
        }
        //충전취소
        int selected_cnt = 0;
        for(int i = 0; i < chargeItemParent.transform.childCount; i ++)
        {
            if(chargeItemParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
            {
                selected_cnt++;
            }
        }
        if(selected_cnt == 0)
        {
            err_popup.SetActive(true);
            err_str.text = "취소할 충전을 선택하세요.";
        }
        else
        {
            select_str.text = "선택한 충전을 결제취소 하시겠습니까?";
            select_popup.SetActive(true);
            select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
            select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { onConfirmCancelCharge(); });
        }
    }

    void onConfirmCancelCharge()
    {
        for (int i = 0; i < chargeItemParent.transform.childCount; i++)
        {
            try
            {
                if (chargeItemParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
                {
                    for (int j = 0; j < prepaytagUsage.chargeItemlist.Count; j++)
                    {
                        if (prepaytagUsage.chargeItemlist[j].id == chargeItemParent.transform.GetChild(i).Find("id").GetComponent<Text>().text)
                        {
                            ChargeItemInfo itemInfo = prepaytagUsage.chargeItemlist[j];
                            cancel_charge_id = itemInfo.id;
                            if (itemInfo.device_type != 0)
                            {
                                if (Global.setinfo.paymentDeviceInfo.port == 0 || Global.setinfo.paymentDeviceInfo.ip == "" || Global.setinfo.paymentDeviceInfo.ip == null)
                                {
                                    err_str.text = "결제단말기 세팅을 진행하세요.";
                                    err_popup.SetActive(true);
                                    return;
                                }
                                else if (Global.setinfo.paymentDeviceInfo.type == 0)
                                {
                                    //serial
                                    err_str.text = "결제단말기 통신방식을 ip로 설정하세요.";
                                    err_popup.SetActive(true);
                                    return;
                                }
                            }
                            progress_popup.SetActive(true);
                            StartCoroutine(checkPaymentResult());
                            switch (itemInfo.device_type)
                            {
                                case 0://단말기 미사용
                                    {
                                        CancelChargeFunc();
                                        break;
                                    }
                                case 1://KICC 카드
                                    {
                                        _ = KICCApproval(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "D2", itemInfo.price.ToString(), "00", itemInfo.appno, itemInfo.payTime, 1);
                                        break;
                                    }
                                case 2://KICC 현금 사용자
                                    {
                                        _ = KICCApproval(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "CR", itemInfo.price.ToString(), "01", itemInfo.appno, itemInfo.payTime, 1);
                                        break;
                                    }
                                case 3://KICC 현금 개인
                                    _ = KICCApproval(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "CR", itemInfo.price.ToString(), "02", itemInfo.appno, itemInfo.payTime, 1);
                                    {
                                        break;
                                    }
                                case 4://KIS 카드
                                    {
                                        _ = KISApprovalAsync(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "D2", itemInfo.price.ToString(), "00", itemInfo.appno, itemInfo.payTime, 1);
                                        break;
                                    }
                                case 5://KIS 현금 (사용자, 카드)
                                    {
                                        _ = KISApprovalAsync(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "CR", itemInfo.price.ToString(), "01", itemInfo.appno, itemInfo.payTime);
                                        break;
                                    }
                            }
                        }
                    }
                    break;
                }
            }
            catch (Exception ex)
            {

            }

        }
    }

    void CancelChargeFunc()
    {
        WWWForm form = new WWWForm();
        form.AddField("charge_id", cancel_charge_id);
        WWW www = new WWW(Global.api_url + Global.cancel_charge_api, form);
        StartCoroutine(CancelChargeProcess(www));
    }

    IEnumerator CancelChargeProcess(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            StartCoroutine(GotoScene("prepayTagUsage"));
        }
    }

    IEnumerator OutputSheetProcess(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                try
                {
                    //주문서 출력
                    string kitorderno = Global.GetNoFormat(jsonNode["orderSeq"].AsInt);
                    string tableName = jsonNode["tableName"];
                    string tagName = jsonNode["tagName"];
                    string is_pack = (jsonNode["is_pack"].AsInt) == 0 ? "" : "T";
                    List<OrderItem> orders = new List<OrderItem>();
                    JSONNode orderlist = JSON.Parse(jsonNode["orderItemList"].ToString()/*.Replace("\"", "")*/);
                    for (int i = 0; i < orderlist.Count; i++)
                    {
                        OrderItem order = new OrderItem();
                        order.product_name = orderlist[i]["product_name"];
                        order.quantity = orderlist[i]["quantity"].AsInt;
                        order.product_unit_price = orderlist[i]["product_unit_price"].AsInt;
                        order.paid_price = orderlist[i]["paid_price"].AsInt;
                        order.is_service = orderlist[i]["is_service"].AsInt;
                        order.kit01 = orderlist[i]["kit01"].AsInt;
                        order.kit02 = orderlist[i]["kit02"].AsInt;
                        order.kit03 = orderlist[i]["kit03"].AsInt;
                        order.kit04 = orderlist[i]["kit04"].AsInt;
                        orders.Add(order);
                    }
                    DateTime orderTime = DateTime.Parse(jsonNode["reg_datetime"]);
                    if (Global.setinfo.printerSet.menu_output == 0) //메뉴개별출력 사용
                    {
                        for (int i = 0; i < orders.Count; i++)
                        {
                            if (Global.setinfo.printerSet.printer1.useset != 1 && Global.setinfo.printerSet.printer1.ip_baudrate != "")
                            {
                                string printStr = DocumentFactory.GetOrderItemSheet("1", kitorderno, orders[i], orderTime, tableName, is_pack, isCat: false);
                                byte[] sendData = NetUtils.StrToBytes(printStr);
                                Socket_Send(Global.setinfo.printerSet.printer1.ip_baudrate, Global.setinfo.printerSet.printer1.port.ToString(), sendData);
                            }
                            if (Global.setinfo.printerSet.printer2.useset != 1 && Global.setinfo.printerSet.printer2.ip_baudrate != "")
                            {
                                string printStr = DocumentFactory.GetOrderItemSheet("2", kitorderno, orders[i], orderTime, tableName, is_pack, isCat: false);
                                byte[] sendData = NetUtils.StrToBytes(printStr);
                                Socket_Send(Global.setinfo.printerSet.printer2.ip_baudrate, Global.setinfo.printerSet.printer2.port.ToString(), sendData);
                            }
                            if (Global.setinfo.printerSet.printer3.useset != 1 && Global.setinfo.printerSet.printer3.ip_baudrate != "")
                            {
                                string printStr = DocumentFactory.GetOrderItemSheet("3", kitorderno, orders[i], orderTime, tableName, is_pack, isCat: false);
                                byte[] sendData = NetUtils.StrToBytes(printStr);
                                Socket_Send(Global.setinfo.printerSet.printer3.ip_baudrate, Global.setinfo.printerSet.printer3.port.ToString(), sendData);
                            }
                            if (Global.setinfo.printerSet.printer4.useset != 1 && Global.setinfo.printerSet.printer4.ip_baudrate != "")
                            {
                                string printStr = DocumentFactory.GetOrderItemSheet("4", kitorderno, orders[i], orderTime, tableName, is_pack, isCat: false);
                                byte[] sendData = NetUtils.StrToBytes(printStr);
                                Socket_Send(Global.setinfo.printerSet.printer4.ip_baudrate, Global.setinfo.printerSet.printer4.port.ToString(), sendData);
                            }
                        }
                    }
                    else //미사용
                    {
                        if (Global.setinfo.printerSet.printer1.useset != 1 && Global.setinfo.printerSet.printer1.ip_baudrate != "")
                        {
                            string printStr = DocumentFactory.GetOrderSheet("1", kitorderno, orders, orderTime, tableName, is_pack, isCat: false);
                            byte[] sendData = NetUtils.StrToBytes(printStr);
                            Socket_Send(Global.setinfo.printerSet.printer1.ip_baudrate, Global.setinfo.printerSet.printer1.port.ToString(), sendData);
                        }
                        if (Global.setinfo.printerSet.printer2.useset != 1 && Global.setinfo.printerSet.printer2.ip_baudrate != "")
                        {
                            string printStr = DocumentFactory.GetOrderSheet("2", kitorderno, orders, orderTime, tableName, is_pack, isCat: false);
                            byte[] sendData = NetUtils.StrToBytes(printStr);
                            Socket_Send(Global.setinfo.printerSet.printer2.ip_baudrate, Global.setinfo.printerSet.printer2.port.ToString(), sendData);
                        }
                        if (Global.setinfo.printerSet.printer3.useset != 1 && Global.setinfo.printerSet.printer3.ip_baudrate != "")
                        {
                            string printStr = DocumentFactory.GetOrderSheet("3", kitorderno, orders, orderTime, tableName, is_pack, isCat: false);
                            byte[] sendData = NetUtils.StrToBytes(printStr);
                            Socket_Send(Global.setinfo.printerSet.printer3.ip_baudrate, Global.setinfo.printerSet.printer3.port.ToString(), sendData);
                        }
                        if (Global.setinfo.printerSet.printer4.useset != 1 && Global.setinfo.printerSet.printer4.ip_baudrate != "")
                        {
                            string printStr = DocumentFactory.GetOrderSheet("4", kitorderno, orders, orderTime, tableName, is_pack, isCat: false);
                            byte[] sendData = NetUtils.StrToBytes(printStr);
                            Socket_Send(Global.setinfo.printerSet.printer4.ip_baudrate, Global.setinfo.printerSet.printer4.port.ToString(), sendData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }
            StartCoroutine(GotoScene("prepayTagUsage"));
        }
    }

    public void onChargePrepay()
    {
        device_type = 0;
        app_no = "";
        credit_card_company = "";
        credit_card_number = "";
        if (Global.cur_tagInfo.tag_id == "" || Global.cur_tagInfo.tag_id == null)
        {
            err_str.text = "먼저 TAG를 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
        //선불충전
        used_point = 0;
        used_prepay = 0;
        payPopup.SetActive(true);
        clearPopup();
        pay_popup_type = 0;//선불충전
        payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().readOnly = false;
    }

    public void onSelPayMethod()
    {
        if (pay_method == 0)
        {
            //이미 카드결제가 선택되어잇는 경우 현금결제방식으로 절환
            pay_method = 1;
            notice2.SetActive(true);
            setInvoice.SetActive(true);
            setInvoice.GetComponent<Toggle>().isOn = false;
            setInvoice.GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
            setInvoice.GetComponent<Toggle>().onValueChanged.AddListener((value) => {
                onSelectInvoice(value);
            }
            );
            payPopup_title.text = "현금결제";
            payPopup_notice.text = "카드결제";
            payPopup.transform.Find("background/1/Notice3").GetComponent<Text>().text = "식별번호";
            prepay.transform.Find("Label").GetComponent<Text>().text = "예치금 결제";
            prepay.GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
            prepay.GetComponent<Toggle>().onValueChanged.AddListener((value) => {
                onSelectPrepayMethod(value);
            }
            );
            precard.SetActive(true);
            precard.GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
            precard.GetComponent<Toggle>().onValueChanged.AddListener((value) => {
                onSelectPrecardMethod(value);
            }
            );
            noprepay.GetComponent<Toggle>().isOn = true;
            noprepay.GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
            noprepay.GetComponent<Toggle>().onValueChanged.AddListener((value) => {
                onSelectNoprepayMethod(value);
            }
            );
            used_prepay = 0;
        }
        else
        {
            pay_method = 0;
            payPopup_title.text = "카드결제";
            payPopup_notice.text = "현금결제";
            notice2.SetActive(false);
            setInvoice.SetActive(false);
            invoice.SetActive(false);
            precard.SetActive(false);
            payPopup.transform.Find("background/1/Notice3").GetComponent<Text>().text = "할부개월";
            prepay.transform.Find("Label").GetComponent<Text>().text = "임의결제처리";
            prepay.GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
            precard.GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
            noprepay.GetComponent<Toggle>().isOn = true;
            noprepay.GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
            noprepay.GetComponent<Toggle>().onValueChanged.AddListener((value) => {
                onSelectNoprepayMethod(value);
            }
            );
            pay_price += used_prepay;
            used_prepay = 0;
        }
    }

    void onSelectInvoice(bool value)
    {
        if (value)
        {
            invoice.SetActive(true);
        }
        else
        {
            invoice.SetActive(false);
        }
    }

    void onSelectPrecardMethod(bool value)
    {
        if (value)
        {
            popup2_title.text = "선불카드조회";
        }
        clients.Clear();
        multiSel.SetActive(false);
        multiSel.GetComponent<Dropdown>().options.Clear();
        pay_price += used_prepay;
        used_prepay = 0;
        used_point = 0;
        pretag_id = "";
        ShowClientCheckUI(false);
    }

    void onSelectPrepayMethod(bool value)
    {
        if (value)
        {
            //예치금 결제 옵션 선택시
            popup2_title.text = "예치금결제";
            if (used_point > 0)
            {
                pay_price += used_point;
                payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().text = pay_price.ToString();
            }
        }
        clients.Clear();
        multiSel.SetActive(false);
        multiSel.GetComponent<Dropdown>().options.Clear();
        pay_price += used_prepay;
        used_prepay = 0;
        used_point = 0;
        client_id = "";
        ShowClientCheckUI(false);
    }

    void onSelectNoprepayMethod(bool value)
    {
        if (value)
        {
            popup2_title.text = "고객조회";
        }
        clients.Clear();
        multiSel.SetActive(false);
        multiSel.GetComponent<Dropdown>().options.Clear();
        pay_price += used_prepay;
        used_prepay = 0;
        used_point = 0;
        client_id = "";
        ShowClientCheckUI(false);
    }

    void ShowClientCheckUI(bool status)
    {
        popup2_notice1.SetActive(status);
        popup2_notice2.SetActive(status);
        popup2_notice3.SetActive(status);
        popup2_notice4.SetActive(status);
        popup2_notice5.SetActive(status);
        popup2_notice6.SetActive(status);
        popup2_notice7.SetActive(status);
        popup2_notice8.SetActive(status);
        popup2_val1.SetActive(status);
        popup2_val2.SetActive(status);
        popup2_val3.SetActive(status);
        popup2_val4.SetActive(status);
        popup2_val5.SetActive(status);
        popup2_val6.SetActive(status);
        popup2_val7.SetActive(status);
        popup2_val8.SetActive(status);
        popup2_pointBtn.SetActive(status);
        popup2_all_btn.SetActive(status);
        popup2_val1.GetComponent<Text>().text = "";
        popup2_val2.GetComponent<Text>().text = "";
        popup2_val3.GetComponent<Text>().text = "";
        popup2_val4.GetComponent<Text>().text = "";
        popup2_val5.GetComponent<Text>().text = "";
        popup2_val6.GetComponent<Text>().text = "";
        popup2_val7.GetComponent<Text>().text = "";
        popup2_val8.GetComponent<InputField>().text = "";
    }

    void payFunc()
    {
        WWWForm form = new WWWForm();
        form.AddField("preTagType", preTagType);
        form.AddField("pretag_id", pretag_id);
        form.AddField("price", pay_price);
        form.AddField("point", used_point);
        form.AddField("prepay", used_prepay);
        form.AddField("total_price", pay_price + used_point + used_prepay);
        form.AddField("client_id", client_id);
        form.AddField("credit_card_company", credit_card_company);
        form.AddField("credit_card_number", credit_card_number);
        form.AddField("device_type", device_type);
        form.AddField("installment_months", installment_months);
        int output_type = 0;//상세출력
        if (payPopup.transform.Find("background/1/output/summary").GetComponent<Toggle>().isOn)
        {
            output_type = 1;//간단출력
        }
        else if (payPopup.transform.Find("background/1/output/no").GetComponent<Toggle>().isOn)
        {
            output_type = 2;//미출력
        }
        form.AddField("pub_id", Global.userinfo.pub.id);
        if (payPopup.transform.Find("background/1/sel").GetComponent<Text>().text == "카드결제")
        {
            form.AddField("pay_type", 1);//카드결제
            form.AddField("app_no", app_no);//식별번호
        }
        else
        {
            form.AddField("pay_type", 0);//현금결제
            if (app_no == "" || app_no == null)
                {
                form.AddField("app_no", payPopup.transform.Find("background/1/no").GetComponent<InputField>().text);
                }
                else
                {
                form.AddField("app_no", app_no);//식별번호
            }
        }
        if (pay_popup_type == 0)
        {
            form.AddField("tag_id", Global.cur_tagInfo.tag_id);
            form.AddField("type", 3);//선불충전
        }
        else if (pay_popup_type == 1)
        {
            form.AddField("tag_id", Global.cur_tagInfo.tag_id);
            form.AddField("type", 4);//정산
        }
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        form.AddField("pos_no", Global.setinfo.pos_no);
        WWW www = new WWW(Global.api_url + Global.pay_api, form);
        StartCoroutine(payProcess(www));
    }

    string getJsonResult()
    {
        string str = "{\"tagName\":\"" + Global.cur_tagInfo.tag_name + "\"";
        str += ",\"chargePrice\":\"" + prepaytagUsage.charge_sum_price + "\"";
        str += ",\"totalPrice\":\"" + prepaytagUsage.order_sum_price + "\"";
        str += ",\"payPrice\":\"" + pay_price + "\"";
        str += ",\"chargelist\":[";
        try
        {
            for (int j = 0; j < prepaytagUsage.chargeItemlist.Count; j++)
            {
                if (j > 0)
                {
                    str += ",";
                }
                str += "{\"regtime\":\"" + prepaytagUsage.chargeItemlist[j].charge_time + "\"";
                str += ",\"card\":\"" + prepaytagUsage.chargeItemlist[j].card_type + "\"";
                str += ",\"cardno\":\"" + prepaytagUsage.chargeItemlist[j].card_no + "\"";
                str += ",\"price\":\"" + prepaytagUsage.chargeItemlist[j].price + "\"}";
            }
            str += "],\"usagelist\":[";
            for (int i = 0; i < prepaytagUsage.menuOrderlist.Count; i++)
            {
                if (i > 0)
                {
                    str += ",";
                }
                str += "{\"menu_name\":\"" + prepaytagUsage.menuOrderlist[i].menu_name + "\"";
                str += ",\"regtime\":\"" + prepaytagUsage.menuOrderlist[i].order_time + "\"";
                str += ",\"size\":\"" + prepaytagUsage.menuOrderlist[i].amount + "\"";
                str += ",\"price\":\"" + prepaytagUsage.menuOrderlist[i].price + "\"}";
            }
        }
        catch (Exception ex)
        {
            return "";
        }
        str += "]}";
        return str;
    }

    IEnumerator checkPaymentResult()
    {
        yield return new WaitForSeconds(Global.setinfo.payment_time);
        if (progress_popup.activeSelf)
        {
            progress_popup.SetActive(false);
        }
    }

    void payProcess()
    {
        if (pay_popup_type == 0)//선불충전
        {
            pay_price = int.Parse(payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().text) - used_prepay;
        }
        if (payPopup.transform.Find("background/1/sel").GetComponent<Text>().text == "현금결제")
        {
            if (used_prepay > 0 && payPopup.transform.Find("background/1/pre/prepay").GetComponent<Toggle>().isOn)
            {
                preTagType = 0;
                payFunc();
            }
            else if (used_prepay > 0 && payPopup.transform.Find("background/1/pre/precard").GetComponent<Toggle>().isOn)
            {
                preTagType = 1;
                payFunc();
            }
            else
            {
                if (pay_price == 0)
                {
                    err_popup.SetActive(true);
                    err_str.text = "결제금액을 확인하세요.";
                }
                if (setInvoice.GetComponent<Toggle>().isOn) //현금영수증 사용
                {
                    int invoice_type = 0;
                    if (invoice.transform.Find("bus").GetComponent<Toggle>().isOn)
                    {
                        invoice_type = 1;//사업자
                    }
                    else
                    {
                        invoice_type = 0;//개인
                    }
                    if (Global.setinfo.paymentDeviceInfo.port == 0 || Global.setinfo.paymentDeviceInfo.ip == "" || Global.setinfo.paymentDeviceInfo.ip == null)
                    {
                        err_str.text = "결제단말기 세팅을 진행하세요.";
                        err_popup.SetActive(true);
                        payPopup.SetActive(false);
                        return;
                    }
                    else if(Global.setinfo.paymentDeviceInfo.type == 0)
                    {
                        //serial
                        err_str.text = "결제단말기 통신방식을 ip로 설정하세요.";
                        err_popup.SetActive(true);
                        payPopup.SetActive(false);
                        return;
                    }
                    else
                    {
                        progress_popup.SetActive(true);
                        StartCoroutine(checkPaymentResult());
                        //현금승인
                        if (Global.setinfo.paymentDeviceInfo.cat == 1)//kis
                        {
                            //구분값 "CC"  승인금액,  "01" - 사업자지출증빙 "02-소득공제, 승인번호, 승인일자(6자리)
                            if (invoice_type == 1)//사업자지출증빙
                            {
                                installment_months = 1;
                            _ = KISApprovalAsync(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "CC", pay_price.ToString(), "01", "", "");
                            }
                            else//소득공제
                            {
                                installment_months = 0;
                                _ = KISApprovalAsync(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "CC", pay_price.ToString(), "02", "", "");
                            }
                        }
                        else if (Global.setinfo.paymentDeviceInfo.cat == 0)//kicc
                        {
                            //구분값 "CC"  승인금액,  "01" - 사업자지출증빙 "02" - 소득공제, 승인번호, 승인일자(6자리)
                            if (invoice_type == 1)//사업자지출증빙
                            {
                                installment_months = 1;
                                _ = KICCApproval(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "CC", pay_price.ToString(), "01", "", "");
                            }
                            else//소득공제
                            {
                                installment_months = 0;
                                _ = KICCApproval(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "CC", pay_price.ToString(), "02", "", "");
                            }
                        }
                    }
                }
                else                            //미사용
                {
                    payFunc();
                }
            }
        }
        else
        {
            if (pay_price == 0)
            {
                err_popup.SetActive(true);
                err_str.text = "결제금액을 확인하세요.";
            }
            if (payPopup.transform.Find("background/1/pre/prepay").GetComponent<Toggle>().isOn) //임의결제처리
            {
                payFunc();
            }
            else //결제단말기이용
            {
                if (Global.setinfo.paymentDeviceInfo.port == 0 || Global.setinfo.paymentDeviceInfo.ip == "" || Global.setinfo.paymentDeviceInfo.ip == null)
                {
                    err_str.text = "결제단말기 세팅을 진행하세요.";
                    err_popup.SetActive(true);
                    payPopup.SetActive(false);
                    return;
                }
                else if (Global.setinfo.paymentDeviceInfo.type == 0)
                {
                    //serial
                    err_str.text = "결제단말기 통신방식을 ip로 설정하세요.";
                    err_popup.SetActive(true);
                    payPopup.SetActive(false);
                    return;
                }
                else
                {
                    //카드승인
                    string noTxt = payPopup.transform.Find("background/1/no").GetComponent<InputField>().text;
                    string halbu = "00";
                    if (noTxt != "" && noTxt.Length != 2)
                    {
                        err_str.text = "할부개월을 정확히 입력하세요.";
                        err_popup.SetActive(true);
                        return;
                    }
                    progress_popup.SetActive(true);
                    StartCoroutine(checkPaymentResult());
                    if (noTxt != "")
                    {
                        halbu = noTxt;
                    }
                    installment_months = int.Parse(halbu);
                    if (Global.setinfo.paymentDeviceInfo.cat == 1)//kis
                    {
                        //구분값 "D1"  승인금액 ,  할부기간 2자리 "00",  "", ""
                        _ = KISApprovalAsync(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "D1", pay_price.ToString(), halbu, "", "");
                    }
                    else if (Global.setinfo.paymentDeviceInfo.cat == 0)//kicc
                    {
                        //구분값 "D1"  승인금액 ,  할부기간 2자리 "00",  "", ""
                        _ = KICCApproval(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "D1", pay_price.ToString(), halbu, "", "");
                    }
                }
            }
        }
    }

    private async Task KISApprovalAsync(string ip, string port, string cmd, string amtstr, string div, string appno, string appdate, int mode = 0)
    {
        char bcc = Convert.ToChar(0x00);
        int type = 0;
        string strSend = "";
        if (cmd.Equals("D2"))
        {   //승인취소
            strSend = "02 CF 20 44 32 1C 1C 1C ";
            if (div.Length > 0)
            {
                strSend += Global.StrToHex(div) + "1C";
            }
            else
                strSend += "1C";
            string sendAmt = amtstr.Trim().Replace(",", "") + Convert.ToChar(0x1C) + ((int)Math.Round(int.Parse(amtstr.Trim().Replace(",", "")) / 11.0, 0)).ToString();
            strSend += Global.StrToHex(sendAmt);
            strSend += "1C 30 1C ";
            strSend += Global.StrToHex(appdate);                                          //승인일자
            strSend += "1C ";
            strSend += Global.StrToHex(appno);                                              //승인번호
            strSend += "1C 1C 1C 1C 1C 1C 1C 1C 1C 1C 1C 59 03 00";

        }
        else if (cmd.Equals("D1"))
        {   //승인
            strSend = "02 CF 20 44 31 1C 1C 1C";
            if (div.Length > 0)
            {
                strSend += Global.StrToHex(div) + "1C";
            }
            else
                strSend += "1C";
            string sendAmt = amtstr.Trim().Replace(",", "") + Convert.ToChar(0x1C) + ((int)Math.Round(int.Parse(amtstr.Trim().Replace(",", "")) / 11.0, 0)).ToString();
            strSend += Global.StrToHex(sendAmt);
            strSend += "1C 30 1C 1C 1C 1C 1C 1C 1C 1C 1C 1C 1C 1C 1C 59 03 00";
        }
        else if (cmd.Equals("CC"))
        {   //승인
            strSend = "02 CF 20 43 43 1C 1C 1C";
            if (div.Length > 0)
            {
                type = 1;
                strSend += Global.StrToHex(div) + "1C";
            }
            else
                strSend += "1C";
            string sendAmt = amtstr.Trim().Replace(",", "") + Convert.ToChar(0x1C) + ((int)Math.Round(int.Parse(amtstr.Trim().Replace(",", "")) / 11.0, 0)).ToString();
            strSend += Global.StrToHex(sendAmt);
            strSend += "1C 30 1C 1C 1C 1C 1C 1C 1C 1C 1C 1C 1C 1C 1C 59 03 00";
        }
        else if (cmd.Equals("CR"))
        {   //승인 취소
            strSend = "02 CF 20 43 52 1C 1C 1C ";
            if (div.Length > 0)
            {
                strSend += Global.StrToHex(div) + "1C";
            }
            else
                strSend += "1C";
            string sendAmt = amtstr.Trim().Replace(",", "") + Convert.ToChar(0x1C) + ((int)Math.Round(int.Parse(amtstr.Trim().Replace(",", "")) / 11.0, 0)).ToString();
            strSend += Global.StrToHex(sendAmt);
            strSend += "1C 30 1C ";
            strSend += Global.StrToHex(appdate);                                          //승인일자
            strSend += "1C ";
            strSend += Global.StrToHex(appno);                                              //승인번호
            strSend += "1C 1C 1C 1C 1C 1C 1C 1C 1C 1C 1C 59 03 00";
        }

        byte[] arraySend = Global.HexToByte(strSend.Replace(" ", ""));
        // BCC 생성.
        for (int k = 1; k < arraySend.Length; k++)
        {
            bcc ^= (char)arraySend[k];
            if (arraySend[k] == 0x03)
                break;
        }
        bcc |= (char)0x20;
        arraySend[arraySend.Length - 1] = (byte)bcc;

        comRcvStr = "";
        Array.Clear(comRcvByte, 0, comRcvByte.Length);
        StxRcv = EtxRcv = EotRcv = EnqRcv = AckRcv = NakRcv = false;
        Debug.Log("단말기에 요청 전문 전송.");
        await Task.Delay(1);
        int iRet = Socket_Send(ip, port, arraySend);
        for (int k = 0; k < 3000; k++)
        {
            if (NakRcv || AckRcv)
            {
                bWait = false;
                break;
            }
            Debug.Log("응답수신대기." + k.ToString());
            await Task.Delay(20);
        }
        if (NakRcv)
        {
            progress_popup.SetActive(false);
            StopCoroutine("checkPaymentResult");
            err_str.text = "신용카드 단말기 Nak 수신..";
            err_popup.SetActive(true);
            socket.Close();
            return;
        }

        if (!AckRcv)
        {
            progress_popup.SetActive(false);
            StopCoroutine("checkPaymentResult");
            err_str.text = "신용카드 단말기 응답이 없습니다.";
            err_popup.SetActive(true);
            socket.Close();
            return;
        }
        comRcvStr = "";
        Array.Clear(comRcvByte, 0, comRcvByte.Length);
        StxRcv = EtxRcv = EotRcv = EnqRcv = AckRcv = false;
        bWait = true;
        waitMsg = new Thread(new ThreadStart(ReceiveWait));
        waitMsg.Start();
        Debug.Log("응답수신대기.");

        for (int k = 0; k < 50000; k++)
        {
            if (StxRcv && EtxRcv)
            {
                bWait = false;
                break;
            }
            if (EotRcv)
            {
                bWait = false;
                break;
            }
            Debug.Log("응답수신대기." + k.ToString());
            await Task.Delay(20);
        }
        Debug.Log("응답수신대기. 종료");
        if (EotRcv)
        {
            progress_popup.SetActive(false);
            StopCoroutine("checkPaymentResult");
            err_str.text = "신용카드 단말기 사용자 종료.";
            err_popup.SetActive(true);
            socket.Close();
            return;
        }
        if (!StxRcv || !EtxRcv)
        {
            progress_popup.SetActive(false);
            StopCoroutine("checkPaymentResult");
            err_str.text = "신용카드 단말기 응답이 없습니다.";
            err_popup.SetActive(true);
            socket.Close();
            return;
        }
        //응답 Format 분석
        try
        {
            Debug.Log("Encoder 세팅.");
            int euckrCodepage = 51949;
            Encoding euckr = Encoding.GetEncoding(euckrCodepage);
            string str = euckr.GetString(comRcvByte).TrimEnd('\0');
            Debug.Log("Encoding 완료.");
            string[] rcvStr = str.Split(FS);
            if (rcvStr[8].Trim() == "")
            {
                credit_card_company = "";
                credit_card_number = "";
                progress_popup.SetActive(false);
                StopCoroutine("checkPaymentResult");
                err_str.text = "신용카드 단말기 사용자 종료";
                err_popup.SetActive(true);
            }
            else
            {
                app_no = rcvStr[8];
                credit_card_number = rcvStr[3];
                credit_card_company = rcvStr[15];
                device_type = 4 + type;
                if(mode == 0)
                {
                    payFunc();
                }
                else
                {
                    CancelChargeFunc();
                }

                Debug.Log("응답수신완료.");
            }
        }
        catch (Exception ex)
        {
            //await DisplayAlert("Error", ex.Message, "OK");
            progress_popup.SetActive(false);
            StopCoroutine("checkPaymentResult");
            err_str.text = "신용카드 단말기 조작시 오류가 발생하였습니다.";
            err_popup.SetActive(true);
            Debug.Log(ex.Message);
        }
        socket.Close();

    }

    private async Task KICCApproval(string ip, string port, string cmd, string amtstr, string div, string appno, string appdate, int mode = 0)
    {
        ushort bcc;
        int type = 0;
        string strSend = "";
        if (cmd.Equals("D2"))
        {   //승인취소
            strSend = "02 00 6f 04 fd 44 32 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20";
            if (div.Length > 0)
            {
                strSend += Global.StrToHex(div.PadLeft(2, '0'));
            }
            else
                strSend += "30 30";
            strSend += Global.StrToHex(appdate);                       //승인일자
            strSend += Global.StrToHex(appno.PadRight(12, ' '));       //승인번호
            string strAmt = amtstr.Trim().Replace(",", "").PadLeft(8, ' ') + "       0" + ((int)Math.Round(int.Parse(amtstr.Trim().Replace(",", "")) / 11.0, 0)).ToString().PadLeft(8, ' ');
            strSend += Global.StrToHex(strAmt);
            strSend += "20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 30 30 30 31 03";

        }
        else if (cmd.Equals("D1"))
        {   //승인
            strSend = "02 00 6f 04 fd 44 31 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 ";
            if (div.Length > 0)
            {
                strSend += Global.StrToHex(div.PadLeft(2, '0'));
            }
            else
                strSend += "30 30";
            strSend += "20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 ";
            string strAmt = amtstr.Trim().Replace(",", "").PadLeft(8, ' ') + "       0" + ((int)Math.Round(int.Parse(amtstr.Trim().Replace(",", "")) / 11.0, 0)).ToString().PadLeft(8, ' ');
            strSend += Global.StrToHex(strAmt);
            strSend += "20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 30 30 30 31 03";

        }
        else if (cmd.Equals("CC"))
        {   //승인
            strSend = "02 00 6f 04 fd 42 31 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 ";
            string strDiv = div.PadLeft(2, '0');
            if (strDiv == "01")
            {
                type = 1;
                strSend += "30 31";
            }
            else if (strDiv == "02")
            {
                type = 2;
                strSend += "30 30";
            }
            else
            {
                progress_popup.SetActive(false);
                StopCoroutine("checkPaymentResult");
                err_str.text = "현금영수증 구분 오류 입니다( 01 or 02).";
                err_popup.SetActive(true);
                return;
            }
            strSend += "20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 ";
            string strAmt = amtstr.Trim().Replace(",", "").PadLeft(8, ' ') + "       0" + ((int)Math.Round(int.Parse(amtstr.Trim().Replace(",", "")) / 11.0, 0)).ToString().PadLeft(8, ' ');
            strSend += Global.StrToHex(strAmt);
            strSend += "20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 30 30 30 31 03";
        }
        else if (cmd.Equals("CR"))
        {   //승인 취소
            strSend = "02 00 6f 04 fd 42 32 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20";
            string strDiv = div.PadLeft(2, '0');
            if (strDiv == "01")
            {
                strSend += "30 31";
            }
            else if (strDiv == "02")
                strSend += "30 30";
            else
            {
                progress_popup.SetActive(false);
                StopCoroutine("checkPaymentResult");
                err_str.text = "현금영수증 구분 오류 입니다( 01 or 02).";
                err_popup.SetActive(true);
                return;
            }
            strSend += Global.StrToHex(appdate);                       //승인일자
            strSend += Global.StrToHex(appno.PadRight(12, ' '));       //승인번호
            string strAmt = amtstr.Trim().Replace(",", "").PadLeft(8, ' ') + "       0" + ((int)Math.Round(int.Parse(amtstr.Trim().Replace(",", "")) / 11.0, 0)).ToString().PadLeft(8, ' ');
            strSend += Global.StrToHex(strAmt);
            strSend += "20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 30 30 30 31 03";
        }
        comRcvStr = "";
        Array.Clear(comRcvByte, 0, comRcvByte.Length);
        StxRcv = EtxRcv = EotRcv = EnqRcv = AckRcv = false;
        string tStr = strSend.Replace(" ", "");
        byte[] arraySend = Global.HexToByte(tStr.Substring(2, tStr.Length - 2));

        bcc = Global.get_crc(arraySend);
        tStr += bcc.ToString("X4");
        //Console.WriteLine("bcc =[" + bcc.ToString("X4") + "]");
        arraySend = Global.HexToByte(tStr);
        //CommLog("PC=>COM " + Cls_StrFnc.BytToHexstr(arraySend));
        Debug.Log("단말기에 요청 전문 전송.");
        await Task.Delay(1);
        int iRet = Socket_Send(ip, port, arraySend);
        if (!AckRcv)
        {
            progress_popup.SetActive(false);
            StopCoroutine("checkPaymentResult");
            err_str.text = "신용카드 단말기 응답이 없습니다.";
            err_popup.SetActive(true);
            socket.Close();
            return;
        }
        await Task.Delay(10);
        comRcvStr = "";
        Array.Clear(comRcvByte, 0, comRcvByte.Length);
        StxRcv = EtxRcv = EotRcv = EnqRcv = AckRcv = false;
        Debug.Log(DateTime.Now.ToString() + "server start......");
        int ret = Socket_Server();
        Debug.Log(DateTime.Now.ToString() + "server End......");
        if (!StxRcv || !EtxRcv)
        {
            socket.Close();
            progress_popup.SetActive(false);
            StopCoroutine("checkPaymentResult");
            err_str.text = "신용카드 단말기 응답이 없습니다.";
            err_popup.SetActive(true);
            return;
        }
        //응답 Format 분석
        try
        {
            int euckrCodepage = 51949;
            Encoding euckr = Encoding.GetEncoding(euckrCodepage);
            string str = euckr.GetString(comRcvByte).TrimEnd('\0');
            Console.WriteLine("str = " + str.Substring(3, str.Length - 3));
            if (str.Substring(5, 4) != "0000")
            {
                payPopup.transform.Find("background/1/no").GetComponent<InputField>().text = "";
                credit_card_number = "";
                credit_card_company = "";
                progress_popup.SetActive(false);
                StopCoroutine("checkPaymentResult");
                err_str.text = "신용카드 단말기 사용자 종료";
                err_popup.SetActive(true);
            }
            else
            {
                app_no = str.Substring(84, 12);
                credit_card_number = str.Substring(18, 16);
                credit_card_company = euckr.GetString(comRcvByte, 113, 20).TrimEnd('\0');
                device_type = 1 + type;
                if(mode == 0)
                {
                    Debug.Log("66666666666666666666666666666666666666666666");
                    payFunc();
                }
                else
                {
                    CancelChargeFunc();
                }

                Debug.Log("응답수신완료.");
            }
        }
        catch (Exception ex)
        {
            //await DisplayAlert("Error", ex.Message, "OK");
            progress_popup.SetActive(false);
            StopCoroutine("checkPaymentResult");
            err_str.text = "신용카드 단말기 조작시 오류가 발생하였습니다.";
            err_popup.SetActive(true);
            Debug.Log(ex.Message);
        }
        socket.Close();
    }

    public void usePoint()
    {
        int point = Global.GetConvertedPrice(popup2_val8.GetComponent<InputField>().text);
        if (pay_price / 2 < point)
        {
            err_popup.SetActive(true);
            err_str.text = "포인트는 결제금액의 50% 까지만 사용이 가능합니다.";
        }
        else
        {
            pay_price -= point;
            used_point = point;
            payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().text = Global.GetPriceFormat(pay_price);
        }
    }

    void select_all_point(int point)
    {
        popup2_val8.GetComponent<InputField>().text = Global.GetPriceFormat(point);
    }

    public void onPayBtn()
    {
        //결제버튼
        if (used_prepay > 0 && pay_price > 0)
        {
            if (popup2_title.text == "고객조회")
            {
                err_popup.SetActive(true);
                err_str.text = "보유한 예치금이 결제금액보다 적습니다.";
                return;
            }
            else if (popup2_title.text == "선불카드조회")
            {
                err_popup.SetActive(true);
                err_str.text = "보유한 선불카드금액이 결제금액보다 적습니다.";
                return;
            }
        }
        else
        {
            WWWForm form = new WWWForm();
            form.AddField("pub_id", Global.userinfo.pub.id + "");
            form.AddField("table_id", Global.cur_tInfo.tid + "");
            WWW www = new WWW(Global.api_url + Global.check_table_tags_api, form);
            StartCoroutine(checkTableTags(www));
        }
    }

    IEnumerator checkTableTags(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                if (jsonNode["result"].AsInt == 1)
                {
                    payProcess();
                }
                else
                {
                    err_str.text = "이용 중인 TAG가 있습니다. 이용 완료 후에 결제를 해주세요.";
                    err_popup.SetActive(true);
                }
            }
            else
            {
                err_str.text = "결제시에 알지 못할 오류가 발생하였습니다.";
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "결제시에 알지 못할 오류가 발생하였습니다.";
            err_popup.SetActive(true);
        }
    }

    IEnumerator payProcess(WWW www)
    {
        yield return www;
        progress_popup.SetActive(false);
        StopCoroutine("checkPaymentResult");
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            try
            {
                int output_type = 0;
                if (payPopup.transform.Find("background/1/output/detail").GetComponent<Toggle>().isOn)
                {
                    output_type = 1;
                }
                else if (payPopup.transform.Find("background/1/output/summary").GetComponent<Toggle>().isOn)
                {
                    output_type = 2;
                }
                else if (payPopup.transform.Find("background/1/output/sum").GetComponent<Toggle>().isOn)
                {
                    output_type = 3;
                }
                //영수증 출력
                string printStr = "";
                payment = new Payment();
                printList.Clear();
                payment.payment_type = jsonNode["payment_type"].AsInt;
                payment.credit_card_company = jsonNode["credit_card_company"];
                payment.credit_card_number = jsonNode["credit_card_number"];
                payment.installment_months = jsonNode["installment_months"].AsInt;
                payment.price = jsonNode["price"].AsInt;
                payment.payamt = jsonNode["payamt"].AsInt;
                payment.cutamt = jsonNode["cutamt"].AsInt;
                payment.reg_datetime = jsonNode["reg_datetime"];
                payment.custno = jsonNode["custno"];
                payment.custpoint = jsonNode["custpoint"].AsInt;
                payment.appno = jsonNode["appno"];
                payment.prepayamt = jsonNode["prepayamt"].AsInt;

                if (output_type == 1)//영수증 (상세)
                {
                    JSONNode orderlist = JSON.Parse(jsonNode["orderItemList"].ToString()/*.Replace("\"", "")*/);
                    for (int i = 0; i < orderlist.Count; i++)
                    {
                        OrderItem order = new OrderItem();
                        order.product_name = orderlist[i]["product_name"];
                        order.quantity = orderlist[i]["quantity"].AsInt;
                        order.product_unit_price = orderlist[i]["product_unit_price"].AsInt;
                        order.paid_price = orderlist[i]["paid_price"].AsInt;
                        order.is_service = orderlist[i]["is_service"].AsInt;
                        order.kit01 = orderlist[i]["kit01"].AsInt;
                        order.kit02 = orderlist[i]["kit02"].AsInt;
                        order.kit03 = orderlist[i]["kit03"].AsInt;
                        order.kit04 = orderlist[i]["kit04"].AsInt;
                        printList.Add(order);
                    }
                }
                else if (output_type == 3)//영수증 (합산)
                {
                    JSONNode orderlist = JSON.Parse(jsonNode["orderItemList"].ToString()/*.Replace("\"", "")*/);
                    for (int i = 0; i < orderlist.Count; i++)
                    {
                        bool is_found = false;
                        for (int j = 0; j < printList.Count; j++)
                        {
                            if (printList[j].product_name == orderlist[i]["product_name"] && printList[j].is_service == orderlist[i]["is_service"].AsInt)
                            {
                                is_found = true;
                                OrderItem temp = printList[j];
                                temp.quantity = temp.quantity + orderlist[i]["quantity"].AsInt;
                                temp.paid_price = temp.paid_price + orderlist[i]["paid_price"].AsInt;
                                printList[j] = temp;
                                Debug.Log("Count : " + printList[j].product_name + " : " + printList[j].quantity);
                                break;
                            }
                        }
                        if (!is_found)
                        {
                            OrderItem order = new OrderItem();
                            order.product_name = orderlist[i]["product_name"];
                            order.quantity = orderlist[i]["quantity"].AsInt;
                            order.product_unit_price = orderlist[i]["product_unit_price"].AsInt;
                            order.paid_price = orderlist[i]["paid_price"].AsInt;
                            order.is_service = orderlist[i]["is_service"].AsInt;
                            order.kit01 = orderlist[i]["kit01"].AsInt;
                            order.kit02 = orderlist[i]["kit02"].AsInt;
                            order.kit03 = orderlist[i]["kit03"].AsInt;
                            order.kit04 = orderlist[i]["kit04"].AsInt;
                            printList.Add(order);
                        }
                    }
                }

                if (payPopup.transform.Find("background/1/output/detail").GetComponent<Toggle>().isOn)
                {
                    printStr = DocumentFactory.GetReceiptDetail(payment, printList, title: "[영수증(상세)]");
                }
                else if (payPopup.transform.Find("background/1/output/summary").GetComponent<Toggle>().isOn)
                {
                    printStr = DocumentFactory.GetReceiptSimple(payment, "", title: "[영수증(간단)]");
                }
                else if (payPopup.transform.Find("background/1/output/sum").GetComponent<Toggle>().isOn)
                {
                    printStr = DocumentFactory.GetReceiptDetail(payment, printList, title: "[영수증(합산)]");
                }
                if (!payPopup.transform.Find("background/1/output/no").GetComponent<Toggle>().isOn)
                {
                    Debug.Log(printStr);
                    byte[] sendData = NetUtils.StrToBytes(printStr);
                    Socket_Send(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), sendData);
                }
            }
            catch (Exception ex)
            {

            }
            StartCoroutine(GotoScene("prepayTagUsage"));
        }
    }

    bool checkPhoneType(string str)
    {
        try
        {
            if (str.Length == 4)
            {
                return true;
            }
        }catch(Exception ex)
        {
        }
        return false;
    }

    public void searchClient()
    {
        string phone = client_name.text;
        //고객조회
        if (popup2_title.text == "고객조회" || popup2_title.text == "예치금결제")
        {
            if (!checkPhoneType(phone))
            {
                err_popup.SetActive(true);
                err_str.text = "고객번호 마지막 4자리를 입력하세요.";
                return;
            }
        }
        if (payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().text.Trim() == "")
        {
            err_popup.SetActive(true);
            err_str.text = "결제금액을 입력한 후에 조회하세요.";
            return;
        }
        WWWForm form = new WWWForm();
        int type = 0;//예치금조회
        if (popup2_title.text == "고객조회")
        {
            type = 1;//포인트조회
        }
        else if (popup2_title.text == "선불카드조회")
        {
            type = 2;//선불카드조회
        }
        form.AddField("type", type);
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("phone", phone);
        WWW www = new WWW(Global.api_url + Global.check_client_api, form);
        StartCoroutine(checkClientProcess(www, type));
    }

    IEnumerator checkClientProcess(WWW www, int type)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                if (type == 0 || type == 1)
                {
                    clients.Clear();
                    JSONNode clist = JSON.Parse(jsonNode["clientlist"].ToString()/*.Replace("\"", "")*/);
                    for (int i = 0; i < clist.Count; i++)
                    {
                        ClientInfo cinfo = new ClientInfo();
                        cinfo.name = clist[i]["name"];
                        cinfo.id = clist[i]["client_id"];
                        cinfo.prepay = clist[i]["prepay"].AsInt;
                        cinfo.first_visit_date = clist[i]["first_visit_date"];
                        cinfo.last_visit_date = clist[i]["last_visit_date"];
                        cinfo.visit_count = clist[i]["visit_count"].AsInt;
                        cinfo.price = clist[i]["price"].AsInt;
                        cinfo.point = clist[i]["point"].AsInt;
                        cinfo.no = clist[i]["no"];
                        clients.Add(cinfo);
                    }
                    if (clist.Count == 1)
                    {
                        viewClientInfo(type, clients[0]);
                    }
                    else if (clist.Count > 1)
                    {
                        multiSel.SetActive(true);
                        multiSel.GetComponent<Dropdown>().options.Clear();
                        Dropdown.OptionData option = new Dropdown.OptionData();
                        option.text = " ";
                        multiSel.GetComponent<Dropdown>().options.Add(option);
                        for (int i = 0; i < clients.Count; i++)
                        {
                            option = new Dropdown.OptionData();
                            option.text = clients[i].no + " " + clients[i].name;
                            multiSel.GetComponent<Dropdown>().options.Add(option);
                        }
                        multiSel.GetComponent<Dropdown>().onValueChanged.RemoveAllListeners();
                        multiSel.GetComponent<Dropdown>().onValueChanged.AddListener((value) =>
                        {
                            SelectClient(value, type);
                        }
                    );
                        multiSel.GetComponent<Dropdown>().Show();
                    }
                }
                else if (type == 2)
                {
                    JSONNode pretags = JSON.Parse(jsonNode["pretagItem"].ToString()/*.Replace("\"", "")*/);
                    PretagInfo pinfo = new PretagInfo();
                    pinfo.qrcode = pretags["qrcode"];
                    pinfo.name = pretags["name"];
                    pinfo.id = pretags["tag_sid"];
                    pinfo.remain = pretags["remain"];
                    viewPretagInfo(type, pinfo);
                }
            }
            else
            {
                err_popup.SetActive(true);
                err_str.text = jsonNode["msg"];
            }
        }
    }

    void clearPopup()
    {
        pay_method = 1;
        onSelPayMethod();
        payPopup.transform.Find("background/1/no").GetComponent<InputField>().text = "";
        if (Global.userinfo.pub.invoice_outtype == 0)
        {
            payPopup.transform.Find("background/1/output/no").GetComponent<Toggle>().isOn = true;
        }
        else if (Global.userinfo.pub.invoice_outtype == 1)
        {
            payPopup.transform.Find("background/1/output/detail").GetComponent<Toggle>().isOn = true;
        }
        else if (Global.userinfo.pub.invoice_outtype == 2)
        {
            payPopup.transform.Find("background/1/output/sum").GetComponent<Toggle>().isOn = true;
        }
        else if (Global.userinfo.pub.invoice_outtype == 3)
        {
            payPopup.transform.Find("background/1/output/summary").GetComponent<Toggle>().isOn = true;
        }
        payPopup.transform.Find("background/1/pre/prepay").GetComponent<Toggle>().isOn = false;
        client_name.text = "";
        ShowClientCheckUI(false);
        multiSel.SetActive(false);
    }

    void viewPretagInfo(int type, PretagInfo pinfo)
    {
        client_name.text = pinfo.qrcode;
        if (type == 2)
        {
            //선불금액조회
            popup2_notice1.GetComponent<Text>().text = "카드번호";
            popup2_notice1.SetActive(true);
            popup2_notice2.SetActive(true);
            popup2_notice2.GetComponent<Text>().text = "카드잔액";
            popup2_notice3.SetActive(true);
            popup2_notice3.GetComponent<Text>().text = "사용금액";
            popup2_notice4.SetActive(true);
            popup2_notice4.GetComponent<Text>().text = "최종잔액";
            popup2_val1.SetActive(true);
            popup2_val2.SetActive(true);
            popup2_val3.SetActive(true);
            popup2_val4.SetActive(true);
            popup2_val1.GetComponent<Text>().text = pinfo.name;
            int p_price = pinfo.remain;
            pretag_id = pinfo.id;
            if (pay_price > p_price)
            {
                pay_price -= p_price;
                used_prepay = p_price;
            }
            else
            {
                used_prepay = pay_price;
                pay_price = 0;
            }
            popup2_val1.GetComponent<Text>().text = pinfo.qrcode;
            popup2_val2.GetComponent<Text>().text = Global.GetPriceFormat(p_price) + " 원";
            popup2_val3.GetComponent<Text>().text = Global.GetPriceFormat(used_prepay) + " 원";
            popup2_val4.GetComponent<Text>().text = Global.GetPriceFormat(p_price - used_prepay) + " 원";
        }
        multiSel.SetActive(false);
    }

    public void SelectClient(int value, int type)
    {
        if (value == 0)
            return;
        try
        {
            viewClientInfo(type, clients[value - 1]);
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    void viewClientInfo(int type, ClientInfo cinfo)
    {
        client_name.text = cinfo.no;
        if (pay_popup_type == 0)//선불충전
        {
            if (payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().text.Trim() == "")
            {
                pay_price = 0;
            }
            else
            {
                pay_price = int.Parse(payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().text);
            }
        }
        if (type == 0)
        {
            //예치금 조회
            popup2_notice1.SetActive(true);
            popup2_notice2.SetActive(true);
            popup2_notice2.GetComponent<Text>().text = "보유예치금";
            popup2_notice3.SetActive(true);
            popup2_notice3.GetComponent<Text>().text = "사용예치금";
            popup2_notice4.SetActive(true);
            popup2_notice4.GetComponent<Text>().text = "잔여예치금";
            popup2_val1.SetActive(true);
            popup2_val2.SetActive(true);
            popup2_val3.SetActive(true);
            popup2_val4.SetActive(true);
            popup2_val1.GetComponent<Text>().text = cinfo.name;
            int p_price = cinfo.prepay;
            client_id = cinfo.id;
            if (pay_price > p_price)
            {
                pay_price -= p_price;
                used_prepay = p_price;
            }
            else
            {
                used_prepay = pay_price;
                pay_price = 0;
            }
            popup2_val2.GetComponent<Text>().text = Global.GetPriceFormat(p_price) + " 원";
            popup2_val3.GetComponent<Text>().text = Global.GetPriceFormat(used_prepay) + " 원";
            popup2_val4.GetComponent<Text>().text = Global.GetPriceFormat(p_price - used_prepay) + " 원";
        }
        else
        {
            //포인트 조회
            ShowClientCheckUI(true);
            popup2_val1.GetComponent<Text>().text = cinfo.name;
            popup2_notice2.GetComponent<Text>().text = "최초방문일";
            popup2_val2.GetComponent<Text>().text = cinfo.first_visit_date;
            popup2_notice3.GetComponent<Text>().text = "최근방문일";
            popup2_val3.GetComponent<Text>().text = cinfo.last_visit_date;
            popup2_notice4.GetComponent<Text>().text = "방문회수";
            popup2_val4.GetComponent<Text>().text = cinfo.visit_count + "회";
            popup2_val5.GetComponent<Text>().text = Global.GetPriceFormat(cinfo.price) + "원";
            int point = cinfo.point;
            popup2_val6.GetComponent<Text>().text = Global.GetPriceFormat(point) + "P";
            popup2_all_btn.GetComponent<Button>().onClick.RemoveAllListeners();
            popup2_all_btn.GetComponent<Button>().onClick.AddListener(delegate () { select_all_point(point); });
            client_id = cinfo.id;
            int p = Convert.ToInt32(Math.Floor(pay_price * Global.userinfo.pub.pointer_rate / 100));
            popup2_val7.GetComponent<Text>().text = Global.GetPriceFormat(p) + "P";
            popup2_val8.GetComponent<InputField>().text = "0";
        }
        multiSel.SetActive(false);
    }

    public void onCount()
    {
        device_type = 0;
        app_no = "";
        credit_card_company = "";
        credit_card_number = "";
        if (Global.cur_tagInfo.tag_id == "" || Global.cur_tagInfo.tag_id == null)
        {
            err_str.text = "먼저 TAG를 선택하세요.";
            err_popup.SetActive(true);
            return;
        }

        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("tag_id", Global.cur_tagInfo.tag_id);
        WWW www = new WWW(Global.api_url + Global.get_tag_status_api, form);
        StartCoroutine(GetTagStatus(www));
    }

    IEnumerator GetTagStatus(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if(jsonNode["suc"].AsInt == 1)
            {
                int status = jsonNode["status"].AsInt;
                if(status == 4)
                {
                    err_popup.SetActive(true);
                    err_str.text = "이용 중인 TAG가 있습니다. 이용 완료 후에 결제를 해주세요.";
                }
                else
                {
                    //정산
                    pay_popup_type = 1;
                    try
                    {
                        pay_price = Global.GetConvertedPrice(usageSumTxt.text);
                        payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().text = pay_price.ToString();
                        payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().readOnly = true;
                        payPopup.SetActive(true);
                        string payInfo = getJsonResult();
                        Debug.Log(payInfo);
                        socket1.Emit("prepayInfo", JSONObject.Create(payInfo));
                        clearPopup();
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }
    }

    public void onClosePaypopup()
    {
        payPopup.SetActive(false);
    }

    public void onCloseErrpopup()
    {
        err_popup.SetActive(false);
    }

    public void onCancelSelpopup()
    {
        select_popup.SetActive(false);
    }

    public void onConfirmSelPopup()
    {
        select_popup.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    float time = 0f;
    void FixedUpdate()
    {
        if (!Input.anyKey)
        {
            time += Time.deltaTime;
        }
        else
        {
            if (time != 0f)
            {
                GameObject.Find("touch").GetComponent<AudioSource>().Play();
                time = 0f;
            }
        }
    }

    IEnumerator GotoScene(string sceneName)
    {
        //StopCoroutine(checkSdate());
        if (socket1 != null)
        {
            socket1.Close();
            socket1.OnDestroy();
            socket1.OnApplicationQuit();
        }
        if (socketObj != null)
        {
            DestroyImmediate(socketObj);
        }
        yield return new WaitForSeconds(0.1f);
        SceneManager.LoadScene(sceneName);
    }

    public void OnApplicationQuit()
    {
        if (socket1 != null)
        {
            socket1.Close();
            socket1.OnDestroy();
            socket1.OnApplicationQuit();
        }
    }
}
