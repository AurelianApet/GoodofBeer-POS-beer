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
using System;
using SimpleJSON;
using System.Text;
using SocketIO;

public class TableUsageManager : MonoBehaviour
{
    public Text tableName;
    public Text totalPriceTxt;
    public Text selectedPriceTxt;
    public Text tagCntTxt;
    public Text tagSumPriceTxt;
    public Text orderSumPriceTxt;
    public Text usageSumPriceTxt;

    public GameObject tagItem;
    public GameObject tagItemParent;
    public GameObject menuItem;
    public GameObject menuItemParent;
    public GameObject tableUsageItem;
    public GameObject tableUsageItemParent;

    public GameObject tagToggle;
    public GameObject menuToggle;
    public GameObject tableUsageToggle;

    public GameObject extraPopup;
    public GameObject payPopup;
    public GameObject regPrepayPopup;
    public GameObject discountPopup;

    public Text payPopup_title;
    public Text payPopup_notice;
    public GameObject notice2;
    public GameObject setInvoice;
    public GameObject invoice;
    public GameObject prepay;
    public GameObject precard;
    public GameObject noprepay;

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
    public GameObject multiPre;

    public GameObject select_popup;
    public Text select_str;
    public GameObject err_popup;
    public Text err_str;
    public GameObject progress_popup;
    public Text progress_str;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket1;

    List<GameObject> mtagObj = new List<GameObject>();
    List<GameObject> mMenuObj = new List<GameObject>();
    List<GameObject> mtableUsageObj = new List<GameObject>();
    List<PayOrderInfo> payinfoList = new List<PayOrderInfo>();
    List<string> selectedList = new List<string>();
    List<ClientInfo> clients = new List<ClientInfo>();
    Payment payment = new Payment();
    List<OrderItem> printList = new List<OrderItem>();
    int installment_months = 0;

    int total_price = 0;//전체금액
    int selected_price = 0;//선택금액
    int using_price = 0;//이용합계
    int order_price = 0;//주문합계
    int tag_price = 0;//태그합계
    int pay_price = 0;//결제금액
    int real_price = 0;//실지결제금액(결제금액 올림/내림 처리 하지 않은 금액 : 절사금액땜에 이용)
    int prepay_price = 0;//현재 테이블의 선결제금액
    int popup_type = -1;
    int pay_method = 0;//0-카드결제, 1-현금결제
    int used_prepay = 0;
    string client_id = "";
    string pretag_id = "";
    int used_point = 0;
    int device_type = 0; //0 : 미사용, 1 : KICC(카드), 2 : KICC(현금, 사용자), 3 : KICC(현금, 개인), 4 : KIS(카드), 5 : KIS(현금 사용자, 개인)
    bool is_allPay = false; //전체 결제시 결제후 모든 태그회수처리하는 공정때문에 이용
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
    int preTagType = 0;

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
        tableName.text = Global.cur_tInfo.name;
        if (Global.setinfo.paymentDeviceInfo.ip != "")
        {
            ipAdd = System.Net.IPAddress.Parse(Global.setinfo.paymentDeviceInfo.ip);
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            remoteEP = new IPEndPoint(ipAdd, Global.setinfo.paymentDeviceInfo.port);
        }
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("table_id", Global.cur_tInfo.tid);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        WWW www = new WWW(Global.api_url + Global.get_tableusagelist_api, form);
        StartCoroutine(LoadInfo(www));
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
        StartCoroutine(GotoScene("tableUsage"));
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
        if (jsonNode["table_id"] == Global.cur_tInfo.tid)
        {
            StartCoroutine(GotoScene("tableUsage"));
        }
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

    IEnumerator LoadInfo(WWW www)
    {
        yield return www;
        mtagObj.Clear();
        mMenuObj.Clear();
        mtableUsageObj.Clear();
        Global.tableUsageInfo.tableId = Global.tableGroupList[Global.cur_tInfo.tgNo].tablelist[Global.cur_tInfo.tNo].id;
        Global.tableUsageInfo.tagUsageList = new List<TableTagUsageInfo>();
        Global.tableUsageInfo.menuOrderList = new List<TableMenuOrderInfo>();
        while (tagItemParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(tagItemParent.transform.GetChild(0).gameObject));
        }
        while (tagItemParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        while (menuItemParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(menuItemParent.transform.GetChild(0).gameObject));
        }
        while (menuItemParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        while (tableUsageItemParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(tableUsageItemParent.transform.GetChild(0).gameObject));
        }
        while (tableUsageItemParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }

        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            prepay_price = jsonNode["prepay"].AsInt;
            JSONNode tableTag_list = JSON.Parse(jsonNode["tableTagUsageInfo"].ToString()/*.Replace("\"", "")*/);
            tag_price = 0;
            using_price = 0;

            JSONNode menuorder_list = JSON.Parse(jsonNode["tableMenuOrderInfo"].ToString()/*.Replace("\"", "")*/);
            order_price = 0;
            for (int i = 0; i < menuorder_list.Count; i++)
            {
                TableMenuOrderInfo moInfo = new TableMenuOrderInfo();
                moInfo.order_id = menuorder_list[i]["order_id"];
                moInfo.menu_name = menuorder_list[i]["menu_name"];
                moInfo.price = menuorder_list[i]["price"].AsInt;
                moInfo.reg_datetime = menuorder_list[i]["reg_datetime"];
                moInfo.amount = menuorder_list[i]["amount"].AsInt;
                moInfo.is_service = menuorder_list[i]["is_service"].AsInt;
                moInfo.status = menuorder_list[i]["status"].AsInt;
                GameObject tmp = Instantiate(menuItem);
                tmp.transform.SetParent(menuItemParent.transform);
                //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmp.transform.localScale = Vector3.one;
                tmp.transform.Find("name").GetComponent<Text>().text = moInfo.menu_name;
                tmp.transform.Find("order_id").GetComponent<Text>().text = moInfo.order_id.ToString();
                tmp.transform.Find("time").GetComponent<Text>().text = moInfo.reg_datetime;
                tmp.transform.Find("amount").GetComponent<Text>().text = moInfo.amount.ToString();
                tmp.transform.Find("is_service").GetComponent<Text>().text = moInfo.is_service.ToString();
                tmp.transform.Find("status").GetComponent<Text>().text = moInfo.status.ToString();
                if (moInfo.is_service == 1)
                {
                    tmp.transform.Find("price").GetComponent<Text>().text = "0";
                }
                else
                {
                    tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(moInfo.price);
                    order_price += moInfo.price;
                    total_price += moInfo.price;
                }
                GameObject toggleObj = tmp.transform.Find("check").gameObject;
                tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                tmp.GetComponent<Button>().onClick.AddListener(delegate () { onClickOrderItem(toggleObj, moInfo.order_id, 2); });
                mMenuObj.Add(tmp);
                Global.tableUsageInfo.menuOrderList.Add(moInfo);
            }
            orderSumPriceTxt.text = Global.GetPriceFormat(order_price);

            for (int i = 0; i < tableTag_list.Count; i++)
            {
                TableTagUsageInfo tgInfo = new TableTagUsageInfo();
                tgInfo.tagName = tableTag_list[i]["tag_name"];
                tgInfo.tagUsageCnt = tableTag_list[i]["usage_cnt"].AsInt;
                tgInfo.tagUsagePrice = tableTag_list[i]["usage_price"].AsInt;
                tgInfo.is_pay_after = tableTag_list[i]["is_pay_after"].AsInt;
                tgInfo.tag_id = tableTag_list[i]["tag_id"];
                tgInfo.status = tableTag_list[i]["status"].AsInt;
                tgInfo.tagMenuOrderList = new List<TagMenuOrderInfo>();
                JSONNode tagorder_list = JSON.Parse(tableTag_list[i]["tabletagOrderInfo"].ToString()/*.Replace("\"", "")*/);
                for (int j = 0; j < tagorder_list.Count; j++)
                {
                    TagMenuOrderInfo ttInfo = new TagMenuOrderInfo();
                    ttInfo.order_id = tagorder_list[j]["order_id"];
                    ttInfo.menu_name = tagorder_list[j]["menu_name"];
                    ttInfo.price = tagorder_list[j]["price"].AsInt;
                    ttInfo.reg_datetime = tagorder_list[j]["reg_datetime"];
                    ttInfo.amount = tagorder_list[j]["amount"].AsInt;
                    ttInfo.status = tagorder_list[j]["status"].AsInt;
                    ttInfo.is_service = tagorder_list[j]["is_service"].AsInt;
                    tgInfo.tagMenuOrderList.Add(ttInfo);
                }
                Global.tableUsageInfo.tagUsageList.Add(tgInfo);
                GameObject tmp = Instantiate(tagItem);
                tmp.transform.SetParent(tagItemParent.transform);
                //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmp.transform.localScale = Vector3.one;
                tmp.transform.Find("tagName").GetComponent<Text>().text = tgInfo.tagName;
                if(tgInfo.status == 3 || tgInfo.status == 1)
                {
                    tmp.transform.Find("tagName").GetComponent<Text>().color = Color.red;
                }
                tmp.transform.Find("tag_id").GetComponent<Text>().text = tgInfo.tag_id.ToString();
                tmp.transform.Find("usageCnt").GetComponent<Text>().text = tgInfo.tagUsageCnt.ToString();
                tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(tgInfo.tagUsagePrice);
                GameObject toggleObj = tmp.transform.Find("check").gameObject;
                int _index = i;
                if (Global.cur_tagInfo.tag_id == tgInfo.tag_id)
                {
                    onClickOrderItem(toggleObj, tgInfo.tag_id, 1, _index);
                }
                Text tagIdText = tmp.transform.Find("tag_id").GetComponent<Text>();
                tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                tmp.GetComponent<Button>().onClick.AddListener(delegate () { onClickOrderItem(toggleObj, tgInfo.tag_id, 1, _index); });
                tag_price += tgInfo.tagUsagePrice;
                mtagObj.Add(tmp);
            }
            tagCntTxt.text = tableTag_list.Count.ToString();
            usageSumPriceTxt.text = Global.GetPriceFormat(using_price);
            total_price = tag_price + order_price;
            totalPriceTxt.text = Global.GetPriceFormat(total_price);
            tagSumPriceTxt.text = Global.GetPriceFormat(tag_price);
            //selectedPriceTxt.text = Global.GetPriceFormat(selected_price);
        }
        string payInfo = getJsonResult();
        Debug.Log(payInfo);
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("payInfo", JSONObject.Create(payInfo));
        }
    }

    void onClickOrderItem(GameObject toggleObj, string id, int type = 0, int tag_index = -1)
    {
        if (toggleObj.GetComponent<Toggle>().isOn)
        {
            toggleObj.GetComponent<Toggle>().isOn = false;
            Debug.Log("선택해제" + type);
            if (type == 1)
            {
                selected_price -= Global.tableUsageInfo.tagUsageList[tag_index].tagUsagePrice;
                selectedPriceTxt.text = Global.GetPriceFormat(selected_price);
                //태그 선택해제
                for (int i = 0; i < mtableUsageObj.Count; i++)
                {
                    if (mtableUsageObj[i].transform.Find("tag_id").GetComponent<Text>().text == id.ToString())
                    {
                        DestroyImmediate(mtableUsageObj[i].gameObject);
                        mtableUsageObj.Remove(mtableUsageObj[i]);
                        i--;
                    }
                }
            }
            else if (type == 2)
            {
                //주문 메뉴 선택해제
                for (int i = 0; i < mMenuObj.Count; i++)
                {
                    if (mMenuObj[i].transform.Find("order_id").GetComponent<Text>().text == id.ToString())
                    {
                        selected_price -= Global.GetConvertedPrice(mMenuObj[i].transform.Find("price").GetComponent<Text>().text);
                        selectedPriceTxt.text = Global.GetPriceFormat(selected_price);
                    }
                }
            }
            else
            {
                //이용내역 선택해제
                for (int i = 0; i < mtableUsageObj.Count; i++)
                {
                    if (mtableUsageObj[i].transform.Find("order_id").GetComponent<Text>().text == id.ToString())
                    {
                        using_price -= Global.GetConvertedPrice(mtableUsageObj[i].transform.Find("price").GetComponent<Text>().text);
                        usageSumPriceTxt.text = Global.GetPriceFormat(using_price);
                    }
                }
            }
        }
        else
        {
            Debug.Log("선택" + type);
            toggleObj.GetComponent<Toggle>().isOn = true;
            if (type == 1)
            {
                selected_price += Global.tableUsageInfo.tagUsageList[tag_index].tagUsagePrice;
                selectedPriceTxt.text = Global.GetPriceFormat(selected_price);
                //태그 선택
                for (int j = 0; j < Global.tableUsageInfo.tagUsageList[tag_index].tagMenuOrderList.Count; j++)
                {
                    GameObject tmp = Instantiate(tableUsageItem);
                    tmp.transform.SetParent(tableUsageItemParent.transform);
                    //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                    //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                    //float left = 0;
                    //float right = 0;
                    //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                    //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                    //tmp.transform.localScale = Vector3.one;
                    tmp.transform.Find("tag").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[tag_index].tagName;
                    tmp.transform.Find("menu").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[tag_index].tagMenuOrderList[j].menu_name;
                    tmp.transform.Find("time").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[tag_index].tagMenuOrderList[j].reg_datetime;
                    tmp.transform.Find("cnt").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[tag_index].tagMenuOrderList[j].amount.ToString();
                    tmp.transform.Find("tag_id").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[tag_index].tag_id.ToString();
                    tmp.transform.Find("order_id").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[tag_index].tagMenuOrderList[j].order_id.ToString();
                    tmp.transform.Find("is_service").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[tag_index].tagMenuOrderList[j].is_service.ToString();
                    if (Global.tableUsageInfo.tagUsageList[tag_index].tagMenuOrderList[j].is_service == 1)
                    {
                        tmp.transform.Find("price").GetComponent<Text>().text = "0";
                    }
                    else
                    {
                        tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(Global.tableUsageInfo.tagUsageList[tag_index].tagMenuOrderList[j].price);
                    }
                    GameObject tObj = tmp.transform.Find("check").gameObject;
                    string _order_id = Global.tableUsageInfo.tagUsageList[tag_index].tagMenuOrderList[j].order_id;
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { onClickOrderItem(tObj, _order_id, 0); });
                    mtableUsageObj.Add(tmp);
                }
            }
            else if (type == 2)
            {
                //주문 메뉴 선택
                for (int i = 0; i < mMenuObj.Count; i++)
                {
                    if (mMenuObj[i].transform.Find("order_id").GetComponent<Text>().text == id.ToString())
                    {
                        selected_price += Global.GetConvertedPrice(mMenuObj[i].transform.Find("price").GetComponent<Text>().text);
                        selectedPriceTxt.text = Global.GetPriceFormat(selected_price);
                    }
                }
            }
            else
            {
                for (int i = 0; i < mtableUsageObj.Count; i++)
                {
                    if (mtableUsageObj[i].transform.Find("order_id").GetComponent<Text>().text == id.ToString())
                    {
                        using_price += Global.GetConvertedPrice(mtableUsageObj[i].transform.Find("price").GetComponent<Text>().text);
                        usageSumPriceTxt.text = Global.GetPriceFormat(using_price);
                    }
                }
            }
        }
        string payInfo = getJsonResult();
        Debug.Log(payInfo);
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("payInfo", JSONObject.Create(payInfo));
        }
    }

    public void AddTag()
    {
        Global.cur_tagInfo = new CurTagInfo();
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("endPay");
        }
        StartCoroutine(GotoScene("tagAdd"));
    }

    public void MenuOrder()
    {
        Global.cur_tagInfo = new CurTagInfo();
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("endpay");
        }
        StartCoroutine(GotoScene("tableMenuOrder"));
    }

    void onZeroPay(int mode = 0)//0 : select pay, 1 : all pay
    {
        //결제처리
        WWWForm form = new WWWForm();
        form.AddField("price", pay_price);
        form.AddField("point", used_point);
        form.AddField("prepay", used_prepay);
        form.AddField("total_price", pay_price + used_point + used_prepay);
        form.AddField("client_id", client_id);
        form.AddField("app_no", "");//식별번호
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("table_id", Global.cur_tInfo.tid);
        form.AddField("pay_type", 0);//현금결제
        form.AddField("credit_card_company", credit_card_company);
        form.AddField("credit_card_number", credit_card_number);
        form.AddField("device_type", device_type);
        form.AddField("installment_months", installment_months);

        if(mode == 1)
        {
            form.AddField("is_allPay", 1);
        }

        string pinfo = "[";
        for (int i = 0; i < payinfoList.Count; i++)
        {
            if (i == 0)
            {
                pinfo += "{";
            }
            else
            {
                pinfo += ",{";
            }
            pinfo += "\"order_id\":\"" + payinfoList[i].order_id + "\","
                + "\"menu_name\":\"" + payinfoList[i].menu_name + "\","
                + "\"is_service\":\"" + payinfoList[i].is_service + "\","
                + "\"amount\":\"" + payinfoList[i].amount + "\","
                + "\"price\":\"" + payinfoList[i].price + "\"}";
        }
        pinfo += "]";

        string tinfo = "[";
        for (int i = 0; i < selectedList.Count; i++)
        {
            if (i == 0)
            {
                tinfo += "{";
            }
            else
            {
                tinfo += ",{";
            }
            tinfo += "\"tag_id\":\"" + selectedList[i] + "\"}";
        }
        tinfo += "]";

        form.AddField("pay_info", pinfo);
        form.AddField("tag_info", tinfo);
        form.AddField("type", 0);
        form.AddField("is_pay_after", 1);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        form.AddField("pos_no", Global.setinfo.pos_no);
        WWW www = new WWW(Global.api_url + Global.pay_api, form);
        StartCoroutine(payProcess(www));
    }

    void clearPopup()
    {
        pay_method = 1;
        onSelPayMethod();
        payPopup.transform.Find("background/1/no").GetComponent<InputField>().text = "";
        if(Global.userinfo.pub.invoice_outtype == 0)
        {
            payPopup.transform.Find("background/1/output/no").GetComponent<Toggle>().isOn = true;
        }else if(Global.userinfo.pub.invoice_outtype == 1)
        {
            payPopup.transform.Find("background/1/output/detail").GetComponent<Toggle>().isOn = true;
        }else if(Global.userinfo.pub.invoice_outtype == 2)
        {
            payPopup.transform.Find("background/1/output/sum").GetComponent<Toggle>().isOn = true;
        }
        else if(Global.userinfo.pub.invoice_outtype == 3)
        {
            payPopup.transform.Find("background/1/output/summary").GetComponent<Toggle>().isOn = true;
        }
        payPopup.transform.Find("background/1/pre/prepay").GetComponent<Toggle>().isOn = false;
        client_name.text = "";
        ShowClientCheckUI(false);
        multiSel.SetActive(false);
    }

    public void SelectPay()
    {
        payinfoList.Clear();
        for (int i = 0; i < mMenuObj.Count; i++)
        {
            try
            {
                if (mMenuObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    PayOrderInfo pinfo = new PayOrderInfo();
                    pinfo.order_id = mMenuObj[i].transform.Find("order_id").GetComponent<Text>().text;
                    pinfo.amount = int.Parse(mMenuObj[i].transform.Find("amount").GetComponent<Text>().text);
                    pinfo.price = Global.GetConvertedPrice(mMenuObj[i].transform.Find("price").GetComponent<Text>().text);
                    pinfo.menu_name = mMenuObj[i].transform.Find("name").GetComponent<Text>().text;
                    pinfo.is_service = int.Parse(mMenuObj[i].transform.Find("is_service").GetComponent<Text>().text);
                    payinfoList.Add(pinfo);
                }
            }
            catch (Exception ex)
            {

            }
        }
        selectedList.Clear();
        for (int i = 0; i < mtagObj.Count; i++)
        {
            try
            {
                if (mtagObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    selectedList.Add(mtagObj[i].transform.Find("tag_id").GetComponent<Text>().text);
                }
            }
            catch (Exception ex)
            {

            }
        }

        device_type = 0;
        app_no = "";
        credit_card_company = "";
        credit_card_number = "";
        pay_price = Global.GetPrice(selected_price);
        real_price = selected_price;
        if (pay_price == 0)
        {
            bool is_found = false;
            for(int i = 0; i < menuItemParent.transform.childCount; i++)
            {
                if (menuItemParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
                {
                    is_found = true;break;
                }
            }
            if (is_found)
            {
                select_popup.SetActive(true);
                select_str.text = "결제금액이 0원입니다. 결제처리하시겠습니까?";
                select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
                select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { onZeroPay(); });
                return;
            }
            err_popup.SetActive(true);
            err_str.text = "결제금액이 없습니다.";
            return;
        }
        if (pay_price < 0)
        {
            err_popup.SetActive(true);
            err_str.text = "결제금액이 0보다 작습니다.";
            return;
        }
        List<TagInfo> taglist = Global.tableGroupList[Global.cur_tInfo.tgNo].tablelist[Global.cur_tInfo.tNo].taglist;
        for (int i = 0; i < taglist.Count; i++)
        {
            if (taglist[i].status == 4)
            {
                err_popup.SetActive(true);
                err_str.text = "이용 중인 TAG가 있습니다. 이용 완료 후에 결제를 해주세요.";
                return;
            }
        }

        is_allPay = false;
        popup_type = 4;//결제팝업
        payPopup.SetActive(true);
        string payInfo = getJsonResult();
        Debug.Log(payInfo);
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("payInfo", JSONObject.Create(payInfo));
        }
        clearPopup();
        used_point = 0;
        used_prepay = 0;
        payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().text = Global.GetPriceFormat(pay_price);
        payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().readOnly = true;
    }

    public void AllPay()
    {
        is_allPay = true;
        device_type = 0;
        app_no = "";
        credit_card_company = "";
        credit_card_number = "";
        pay_price = Global.GetPrice(total_price);
        real_price = total_price;
        payinfoList.Clear();
        for (int i = 0; i < mMenuObj.Count; i++)
        {
            try
            {
                PayOrderInfo pinfo = new PayOrderInfo();
                pinfo.order_id = mMenuObj[i].transform.Find("order_id").GetComponent<Text>().text;
                pinfo.amount = int.Parse(mMenuObj[i].transform.Find("amount").GetComponent<Text>().text);
                pinfo.price = Global.GetConvertedPrice(mMenuObj[i].transform.Find("price").GetComponent<Text>().text);
                pinfo.menu_name = mMenuObj[i].transform.Find("name").GetComponent<Text>().text;
                payinfoList.Add(pinfo);
            }
            catch (Exception ex)
            {

            }
        }
        selectedList.Clear();
        for (int i = 0; i < mtagObj.Count; i++)
        {
            try
            {
                selectedList.Add(mtagObj[i].transform.Find("tag_id").GetComponent<Text>().text);
            }
            catch (Exception ex)
            {

            }
        }
        if (pay_price == 0)
        {
            bool is_found = false;
            for (int i = 0; i < menuItemParent.transform.childCount; i++)
            {
                is_found = true; break;
            }
            if (is_found)
            {
                select_popup.SetActive(true);
                select_str.text = "결제금액이 0원입니다. 결제처리하시겠습니까?";
                select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
                select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { onZeroPay(); });
                return;
            }

            err_popup.SetActive(true);
            err_str.text = "결제금액이 없습니다.";
            return;
        }
        if (pay_price < 0)
        {
            err_popup.SetActive(true);
            err_str.text = "결제금액이 0보다 작습니다.";
            return;
        }
        List<TagInfo> taglist = Global.tableGroupList[Global.cur_tInfo.tgNo].tablelist[Global.cur_tInfo.tNo].taglist;
        for (int i = 0; i < taglist.Count; i++)
        {
            if (taglist[i].status == 4)
            {
                err_popup.SetActive(true);
                err_str.text = "이용 중인 TAG가 있습니다. 이용 완료 후에 결제를 해주세요.";
                return;
            }
        }

        popup_type = 4;//결제팝업
        used_point = 0;
        used_prepay = 0;
        payPopup.SetActive(true);
        string payInfo = getJsonResult();
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("payInfo", JSONObject.Create(payInfo));
        }
        clearPopup();
        payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().text = Global.GetPriceFormat(pay_price);
        payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().readOnly = true;
    }

    public void onExtraPopup()
    {
        if (extraPopup.activeSelf)
        {
            extraPopup.SetActive(false);
        }
        else
        {
            extraPopup.SetActive(true);
        }
    }

    void onSelItem(GameObject toggleObj)
    {
        if (toggleObj.GetComponent<Toggle>().isOn)
        {
            toggleObj.GetComponent<Toggle>().isOn = false;
        }
        else
        {
            toggleObj.GetComponent<Toggle>().isOn = true;
        }
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

    void onSelectInvoice(bool value)
    {
        invoice.SetActive(value);
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
        form.AddField("client_id", client_id);
        form.AddField("credit_card_company", credit_card_company);
        form.AddField("credit_card_number", credit_card_number);
        form.AddField("device_type", device_type);
        form.AddField("installment_months", installment_months);
        int output_type = 0;
        if (payPopup.transform.Find("background/1/output/detail").GetComponent<Toggle>().isOn)
        {
            output_type = 1;
        }
        else if (payPopup.transform.Find("background/1/output/sum").GetComponent<Toggle>().isOn)
        {
            output_type = 2;
        }
        else if (payPopup.transform.Find("background/1/output/summary").GetComponent<Toggle>().isOn)
        {
            output_type = 3;
        }
        form.AddField("invoice_type", output_type);
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("table_id", Global.cur_tInfo.tid);
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

        string pinfo = "[";
        for (int i = 0; i < payinfoList.Count; i++)
        {
            if (i == 0)
            {
                pinfo += "{";
            }
            else
            {
                pinfo += ",{";
            }
            pinfo += "\"order_id\":\"" + payinfoList[i].order_id + "\","
                + "\"menu_name\":\"" + payinfoList[i].menu_name + "\","
                + "\"is_service\":\"" + payinfoList[i].is_service + "\","
                + "\"amount\":\"" + payinfoList[i].amount + "\","
                + "\"price\":\"" + payinfoList[i].price + "\"}";
        }
        pinfo += "]";

        string tinfo = "[";
        for (int i = 0; i < selectedList.Count; i++)
        {
            if (i == 0)
            {
                tinfo += "{";
            }
            else
            {
                tinfo += ",{";
            }
            tinfo += "\"tag_id\":\"" + selectedList[i] + "\"}";
        }
        tinfo += "]";

        form.AddField("pay_info", pinfo);
        form.AddField("tag_info", tinfo);
        if(popup_type == 6)
        {
            //선결제
            form.AddField("type", 1);
            form.AddField("total_price", pay_price + used_point + used_prepay);
        }
        else if (popup_type == 4)
        {
            //일반 주문 결제
            form.AddField("type", 0);
            form.AddField("total_price", real_price);
        }
        form.AddField("is_pay_after", 1);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        form.AddField("pos_no", Global.setinfo.pos_no);
        WWW www = new WWW(Global.api_url + Global.pay_api, form);
        StartCoroutine(payProcess(www));
    }

    string getJsonResult()
    {
        string str = "{\"tagPrice\":\"" + tag_price + "\"";
        try
        {
            str += ",\"totalPrice\":\"" + total_price + "\"";
            str += ",\"menuPrice\":\"" + order_price + "\"";
            str += ",\"payPrice\":\"" + pay_price + "\"";
            str += ",\"taglist\":[";
            for (int i = 0; i < mtagObj.Count; i++)
            {
                if (i > 0)
                {
                    str += ",";
                }
                str += "{\"tagName\":\"" + Global.tableUsageInfo.tagUsageList[i].tagName + "\"";
                str += ",\"count\":\"" + Global.tableUsageInfo.tagUsageList[i].tagUsageCnt + "\"";
                str += ",\"price\":\"" + Global.tableUsageInfo.tagUsageList[i].tagUsagePrice + "\"";
                str += ",\"status\":\"" + Global.tableUsageInfo.tagUsageList[i].status + "\"";
                if (mtagObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    str += ",\"is_checked\":\"" + true + "\"";
                }
                else
                {
                    str += ",\"is_checked\":\"" + false + "\"";
                }
                str += ",\"tagUsage\":[";
                for (int j = 0; j < Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList.Count; j++)
                {
                    if (j > 0)
                    {
                        str += ",";
                    }
                    str += "{\"name\":\"" + Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList[j].menu_name + "\"";
                    str += ",\"amount\":\"" + Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList[j].amount + "\"";
                    str += ",\"price\":\"" + Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList[j].price + "\"";
                    str += ",\"reg_datetime\":\"" + Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList[j].reg_datetime + "\"";
                    str += ",\"status\":\"" + Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList[j].status + "\"}";
                }
                str += "]}";
            }
            str += "],\"menulist\":[";
            for (int i = 0; i < mMenuObj.Count; i++)
            {
                if (i > 0)
                {
                    str += ",";
                }
                str += "{\"amount\":\"" + Global.tableUsageInfo.menuOrderList[i].amount + "\"";
                str += ",\"name\":\"" + Global.tableUsageInfo.menuOrderList[i].menu_name + "\"";
                str += ",\"price\":\"" + Global.tableUsageInfo.menuOrderList[i].price + "\"";
                str += ",\"status\":\"" + Global.tableUsageInfo.menuOrderList[i].status + "\"";
                str += ",\"reg_datetime\":\"" + Global.tableUsageInfo.menuOrderList[i].reg_datetime + "\"";
                if (mMenuObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    str += ",\"is_checked\":\"" + true + "\"}";
                }
                else
                {
                    str += ",\"is_checked\":\"" + false + "\"}";
                }
            }
            str += "]}";
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
            return "";
        }
        return str;
    }

    IEnumerator checkPaymentResult()
    {
        yield return new WaitForSeconds(Global.setinfo.payment_time);
        if (progress_popup.activeSelf)
        {
            progress_popup.SetActive(false);
            Debug.Log("close progress");
        }
    }

    void payProcess()
    {
        if(popup_type == 6)
        {
            pay_price = int.Parse(payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().text) - used_prepay;
        }
        int total_price = pay_price + used_prepay + used_point;
        if (payPopup.transform.Find("background/1/sel").GetComponent<Text>().text == "현금결제")
        {
            if (used_prepay > 0 && payPopup.transform.Find("background/1/pre/prepay").GetComponent<Toggle>().isOn)
            {
                preTagType = 0;
                payFunc();
            }
            if (used_prepay > 0 && payPopup.transform.Find("background/1/pre/precard").GetComponent<Toggle>().isOn)
            {
                preTagType = 1;
                payFunc();
            }
            else
            {
                if (total_price == 0)
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
                            if(invoice_type == 1)//사업자지출증빙
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
                else//미사용
                {
                    payFunc();
                }
            }
        }
        else
        {
            if (total_price == 0)
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
                    if(noTxt != "" && noTxt.Length != 2)
                    {
                        err_str.text = "할부개월을 정확히 입력하세요.";
                        err_popup.SetActive(true);
                        return;
                    }
                    progress_popup.SetActive(true);
                    Debug.Log("progress popup");
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
    
    private async Task KISApprovalAsync(string ip, string port, string cmd, string amtstr, string div, string appno, string appdate)
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
            Debug.Log("close progress");
            StopCoroutine("checkPaymentResult");
            err_str.text = "신용카드 단말기 Nak 수신..";
            err_popup.SetActive(true);
            socket.Close();
            return;
        }

        if (!AckRcv)
        {
            progress_popup.SetActive(false);
            Debug.Log("close progress");
            StopCoroutine("checkPaymentResult");
            if (Global.setinfo.pos_no == 1)
            {
                socket1.Emit("endpay");
            }
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
            Debug.Log("close progress");
            StopCoroutine("checkPaymentResult");
            if (Global.setinfo.pos_no == 1)
            {
                socket1.Emit("endpay");
            }
            err_str.text = "신용카드 단말기 사용자 종료.";
            err_popup.SetActive(true);
            socket.Close();
            return;
        }
        if (!StxRcv || !EtxRcv)
        {
            progress_popup.SetActive(false);
            Debug.Log("close progress");
            StopCoroutine("checkPaymentResult");
            if (Global.setinfo.pos_no == 1)
            {
                socket1.Emit("endpay");
            }
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
                Debug.Log("close progress");
                StopCoroutine("checkPaymentResult");
                if (Global.setinfo.pos_no == 1)
                {
                    socket1.Emit("endpay");
                }
                err_str.text = "신용카드 단말기 사용자 종료";
                err_popup.SetActive(true);
            }
            else
            {
                app_no = rcvStr[8];
                credit_card_number = rcvStr[3];
                credit_card_company = rcvStr[15];
                device_type = 4 + type;
                payFunc();
                Debug.Log("응답수신완료.");
            }
        }
        catch (Exception ex)
        {
            progress_popup.SetActive(false);
            Debug.Log("close progress");
            StopCoroutine("checkPaymentResult");
            err_str.text = "신용카드 단말기 조작시 오류가 발생하였습니다.";
            err_popup.SetActive(true);
            Debug.Log(ex.Message);
        }
        socket.Close();

    }

    private async Task KICCApproval(string ip, string port, string cmd, string amtstr, string div, string appno, string appdate)
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
                Debug.Log("close progress");
                StopCoroutine("checkPaymentResult");
                if (Global.setinfo.pos_no == 1)
                {
                    socket1.Emit("endpay");
                }
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
                Debug.Log("close progress");
                StopCoroutine("checkPaymentResult");
                if (Global.setinfo.pos_no == 1)
                {
                    socket1.Emit("endpay");
                }
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
            Debug.Log("close progress");
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
            Debug.Log("close progress");
            StopCoroutine("checkPaymentResult");
            if (Global.setinfo.pos_no == 1)
            {
                socket1.Emit("endpay");
            }
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
                app_no = "";
                credit_card_number = "";
                credit_card_company = "";
                progress_popup.SetActive(false);
                Debug.Log("close progress");
                StopCoroutine("checkPaymentResult");
                if (Global.setinfo.pos_no == 1)
                {
                    socket1.Emit("endpay");
                }
                err_str.text = "신용카드 단말기 사용자 종료";
                err_popup.SetActive(true);
            }
            else
            {
                app_no = str.Substring(84, 12);
                credit_card_number = str.Substring(18, 16);
                credit_card_company = euckr.GetString(comRcvByte, 113, 20).TrimEnd('\0');
                device_type = 1 + type;
                payFunc();
                Debug.Log("응답수신완료.");
            }
        }
        catch (Exception ex)
        {
            //await DisplayAlert("Error", ex.Message, "OK");
            progress_popup.SetActive(false);
            Debug.Log("close progress");
            StopCoroutine("checkPaymentResult");
            err_str.text = "신용카드 단말기 조작시 오류가 발생하였습니다.";
            err_popup.SetActive(true);
            Debug.Log(ex.Message);
        }
        socket.Close();
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
            form.AddField("pub_id", Global.userinfo.pub.id);
            form.AddField("table_id", Global.cur_tInfo.tid);
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
        Debug.Log("close progress");
        StopCoroutine("checkPaymentResult");
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("endpay");
        }
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
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

                    if(output_type == 1)//영수증 (상세)
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
                    else if(output_type == 3)//영수증 (합산)
                    {
                        JSONNode orderlist = JSON.Parse(jsonNode["orderItemList"].ToString()/*.Replace("\"", "")*/);
                        for (int i = 0; i < orderlist.Count; i++)
                        {
                            bool is_found = false;
                            for(int j = 0; j < printList.Count; j ++)
                            {
                                if(printList[j].product_name == orderlist[i]["product_name"] && printList[j].is_service == orderlist[i]["is_service"].AsInt)
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
                            if(!is_found)
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
                        byte[] sendData = NetUtils.StrToBytes(printStr);
                        Socket_Send(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), sendData);
                    }
                } catch(Exception ex)
                {

                }

                if (is_allPay)
                {
                    cancelAllTags();
                }
                else
                {
                    StartCoroutine(GotoScene("tableUsage"));
                }
            }
            else
            {
                err_str.text = jsonNode["msg"];
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "결제시에 알지 못할 오류가 발생하였습니다.";
            err_popup.SetActive(true);
        }
    }

    void cancelAllTags()
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("table_id", Global.cur_tInfo.tid);
        WWW www = new WWW(Global.api_url + Global.cancel_tabletag_api, form);
        StartCoroutine(cancelAllTags(www));
    }

    IEnumerator cancelAllTags(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                if (is_allPay)
                {
                    StartCoroutine(GotoScene("main"));
                }
                else
                {
                    StartCoroutine(GotoScene("tableUsage"));
                }
            }
            else
            {
                err_str.text = jsonNode["msg"];
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "태그 회수시에 알지 못할 오류가 발생하였습니다.";
            err_popup.SetActive(true);
        }
    }

    public void prepayBtn()
    {
        device_type = 0;
        app_no = "";
        credit_card_company = "";
        credit_card_number = "";
        //선결제
        payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().text = "";
        payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().readOnly = false;
        popup_type = 6;
        extraPopup.SetActive(false);
        payPopup.SetActive(true);
        clearPopup();
    }

    public void usePoint()
    {
        int point = Global.GetConvertedPrice(popup2_val8.GetComponent<InputField>().text);
        if(pay_price / 2 < point)
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

    bool checkPhoneType(string str)
    {
        try
        {
            if (str.Length == 4)
            {
                return true;
            }
        }
        catch (Exception ex)
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

    void onTagItem(bool value)
    {
        StartCoroutine(ReloadTableUsage(true));
    }

    public void OnTagToggle()
    {
        for (int i = 0; i < tagItemParent.transform.childCount; i++)
        {
            try
            {
                if(tagItemParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
                {
                    selected_price -= Global.GetConvertedPrice(tagItemParent.transform.GetChild(i).Find("price").GetComponent<Text>().text);
                }
                if(tagToggle.GetComponent<Toggle>().isOn)
                {
                    selected_price += Global.GetConvertedPrice(tagItemParent.transform.GetChild(i).Find("price").GetComponent<Text>().text);
                }
                tagItemParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn = tagToggle.GetComponent<Toggle>().isOn;
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
        selectedPriceTxt.text = Global.GetPriceFormat(selected_price);
        StartCoroutine(ReloadTableUsage(tagToggle.GetComponent<Toggle>().isOn));
    }

    public void OnMenuToggle()
    {
        for (int i = 0; i < menuItemParent.transform.childCount; i++)
        {
            try
            {
                if(menuItemParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
                {
                    selected_price -= Global.GetConvertedPrice(menuItemParent.transform.GetChild(i).Find("price").GetComponent<Text>().text);
                }
                if(menuToggle.GetComponent<Toggle>().isOn)
                {
                    selected_price += Global.GetConvertedPrice(menuItemParent.transform.GetChild(i).Find("price").GetComponent<Text>().text);
                }
                menuItemParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn = menuToggle.GetComponent<Toggle>().isOn;
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
        selectedPriceTxt.text = Global.GetPriceFormat(selected_price);
        string payInfo = getJsonResult();
        Debug.Log(payInfo);
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("payInfo", JSONObject.Create(payInfo));
        }
    }

    void addTagUsageItem(string tag_id)
    {
        for(int i = 0; i < Global.tableUsageInfo.tagUsageList.Count; i++)
        {
            if(Global.tableUsageInfo.tagUsageList[i].tag_id == tag_id)
            {
                for (int j = 0; j < Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList.Count; j++)
                {
                    GameObject tmpObj = Instantiate(tableUsageItem);
                    tmpObj.transform.SetParent(tableUsageItemParent.transform);
                    //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                    //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                    //float left = 0;
                    //float right = 0;
                    //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                    //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                    //tmp.transform.localScale = Vector3.one;
                    tmpObj.transform.Find("tag").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[i].tagName;
                    tmpObj.transform.Find("menu").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList[j].menu_name;
                    tmpObj.transform.Find("time").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList[j].reg_datetime;
                    tmpObj.transform.Find("cnt").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList[j].amount.ToString();
                    tmpObj.transform.Find("tag_id").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[i].tag_id.ToString();
                    tmpObj.transform.Find("order_id").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList[j].order_id.ToString();
                    tmpObj.transform.Find("status").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList[j].status.ToString();
                    tmpObj.transform.Find("is_service").GetComponent<Text>().text = Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList[j].is_service.ToString();
                    if(Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList[j].is_service == 1)
                    {
                        tmpObj.transform.Find("price").GetComponent<Text>().text = "0";
                    }
                    else
                    {
                        tmpObj.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList[j].price);
                    }
                    GameObject tgObj = tmpObj.transform.Find("check").gameObject;
                    string _order_id = Global.tableUsageInfo.tagUsageList[i].tagMenuOrderList[j].order_id;
                    tmpObj.GetComponent<Button>().onClick.RemoveAllListeners();
                    tmpObj.GetComponent<Button>().onClick.AddListener(delegate () { onClickOrderItem(tgObj, _order_id, 0); });
                    mtableUsageObj.Add(tmpObj);
                }
                break;
            }
        }
    }

    IEnumerator ReloadTableUsage(bool isChecked)
    {
        mtableUsageObj.Clear();
        while (tableUsageItemParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(tableUsageItemParent.transform.GetChild(0).gameObject));
        }
        while (tableUsageItemParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        if(isChecked)
        {
            for(int i = 0; i < mtagObj.Count; i ++)
            {
                if (mtagObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    try
                    {
                        string id = mtagObj[i].transform.Find("tag_id").GetComponent<Text>().text;
                        addTagUsageItem(id);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex);
                    }
                }

            }
        }
        string payInfo = getJsonResult();
        Debug.Log(payInfo);
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("payInfo", JSONObject.Create(payInfo));
        }
        yield return null;
    }

    public void OnTableUsageToggle()
    {
        for (int i = 0; i < tableUsageItemParent.transform.childCount; i++)
        {
            try
            {
                if (tableUsageItemParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
                {
                    //selected_price -= Global.GetConvertedPrice(tableUsageItemParent.transform.GetChild(i).Find("price").GetComponent<Text>().text);
                    using_price -= Global.GetConvertedPrice(tableUsageItemParent.transform.GetChild(i).Find("price").GetComponent<Text>().text);
                }
                if (tableUsageToggle.GetComponent<Toggle>().isOn)
                {
                    //selected_price += Global.GetConvertedPrice(tableUsageItemParent.transform.GetChild(i).Find("price").GetComponent<Text>().text);
                    using_price += Global.GetConvertedPrice(tableUsageItemParent.transform.GetChild(i).Find("price").GetComponent<Text>().text);
                }
                tableUsageItemParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn = tableUsageToggle.GetComponent<Toggle>().isOn;
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
        //selectedPriceTxt.text = Global.GetPriceFormat(selected_price);
        usageSumPriceTxt.text = Global.GetPriceFormat(using_price);
        string payInfo = getJsonResult();
        Debug.Log(payInfo);
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("payInfo", JSONObject.Create(payInfo));
        }
    }

    IEnumerator checkClientProcess(WWW www, int type)
    {
        yield return www;
        if(www.error == null)
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

    void viewPretagInfo(int type, PretagInfo pinfo)
    {
        client_name.text = pinfo.qrcode;
        if (popup_type == 6)
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
        if(popup_type == 6)
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

    void select_all_point(int point)
    {
        popup2_val8.GetComponent<InputField>().text = Global.GetPriceFormat(point);
    }

    public void onCancelTag()
    {
        List<string> selected_tag_id = new List<string>();
        for (int i = 0; i < tagItemParent.transform.childCount; i++)
        {
            try
            {
                if (tagItemParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
                {
                    selected_tag_id.Add(tagItemParent.transform.GetChild(i).Find("tag_id").GetComponent<Text>().text);
                }
            }
            catch (Exception ex)
            {

            }
        }
        if(selected_tag_id.Count == 0)
        {
            err_str.text = "회수할 TAG를 선택하세요.";
            err_popup.SetActive(true);
        }
        bool is_used = false;
        for (int i = 0; i < selected_tag_id.Count; i++)
        {
            for (int j = 0; j < Global.tableUsageInfo.tagUsageList.Count; j++)
            {
                if (selected_tag_id[i] == Global.tableUsageInfo.tagUsageList[j].tag_id)
                {
                    if (Global.tableUsageInfo.tagUsageList[j].tagUsageCnt != 0)
                    {
                        is_used = true; break;
                    }
                }
            }
            if (is_used)
            {
                break;
            }
        }
        if (is_used)
        {
            err_str.text = "TAG회수는 결제를 완료한 후에 가능합니다.";
            err_popup.SetActive(true);
        }
        else
        {
            cancelTagFunc(selected_tag_id);
        }
    }

    public void onClosePopup()
    {
        switch (popup_type)
        {
            case 2:
                {
                    regPrepayPopup.SetActive(false);
                    break;
                };
            case 3:
                {
                    discountPopup.SetActive(false);
                    break;
                };
            case 4:
                {
                    payPopup.SetActive(false);
                    break;
                };
            case 6:
                {
                    payPopup.SetActive(false);
                    break;
                }
        }
    }

    void cancelTagFunc(List<string> selected_tag_id)
    {
        //취소
        WWWForm form = new WWWForm();
        string tag_ids = "[";
        for (int i = 0; i < selected_tag_id.Count; i++)
        {
            if (i == 0)
            {
                tag_ids += "{";
            }
            else
            {
                tag_ids += ",{";
            }
            tag_ids += "\"id\":\"" + selected_tag_id[i] + "\"}";
        }
        tag_ids += "]";
        form.AddField("tag_ids", tag_ids);
        WWW www = new WWW(Global.api_url + Global.cancel_tags_api, form);
        StartCoroutine(CancelTags(www, selected_tag_id));
    }

    IEnumerator CancelTags(WWW www, List<string> tags)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                for (int j = 0; j < tags.Count; j++)
                {
                    for (int i = 0; i < Global.tableUsageInfo.tagUsageList.Count; i++)
                    {
                        if (Global.tableUsageInfo.tagUsageList[i].tag_id == tags[j])
                        {
                            Global.tableUsageInfo.tagUsageList.RemoveAt(i);
                            for(int k = 0; k < mtagObj.Count; k++)
                            {
                                if(mtagObj[k].transform.Find("tag_id").GetComponent<Text>().text == tags[j].ToString())
                                {
                                    StartCoroutine(Destroy_Object(mtagObj[k]));
                                    mtagObj.Remove(mtagObj[k]);
                                    break;
                                }
                            }
                            Global.RemoveTag(tags[j]);
                            break;
                        }
                    }
                }
                string payInfo = getJsonResult();
                Debug.Log(payInfo);
                if (Global.setinfo.pos_no == 1)
                {
                    socket1.Emit("payInfo", JSONObject.Create(payInfo));
                }
            }
            else
            {
                err_str.text = "태그회수에 실패하였습니다.";
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "태그회수시에 알지 못할 오류가 발생하였습니다.";
            err_popup.SetActive(true);
        }
    }

    public void dividebyN()
    {
        selectedList.Clear();
        for (int i = 0; i < menuItemParent.transform.childCount; i++)
        {
            try
            {
                if (menuItemParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
                {
                    selectedList.Add(menuItemParent.transform.GetChild(i).Find("order_id").GetComponent<Text>().text);
                }
            }
            catch (Exception ex)
            {

            }
        }
        if (selectedList.Count == 0)
        {
            err_str.text = "메뉴를 선택하세요.";
            err_popup.SetActive(true);
        }
        else if(Global.tableUsageInfo.tagUsageList.Count == 0)
        {
            err_str.text = "테이블에 등록된 태그가 없습니다.";
            err_popup.SetActive(true);
        }
        else
        {
            select_str.text = "선택한 메뉴를 1/N 처리 하시겠습니까?";
            select_popup.SetActive(true);
            select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
            select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { onDivide(); });
        }
    }

    void onDivide()
    {
        selectedList.Clear();
        for (int i = 0; i < mMenuObj.Count; i++)
        {
            try
            {
                if (mMenuObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    selectedList.Add(mMenuObj[i].transform.Find("order_id").GetComponent<Text>().text);
                }
            }
            catch (Exception ex)
            {

            }
        }
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("table_id", Global.cur_tInfo.tid);
        string oinfo = "[";
        for (int i = 0; i < selectedList.Count; i++)
        {
            if (i == 0)
            {
                oinfo += "{";
            }
            else
            {
                oinfo += ",{";
            }
            oinfo += "\"order_id\":\"" + selectedList[i] + "\"}";
        }
        oinfo += "]";
        form.AddField("order_info", oinfo);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        WWW www = new WWW(Global.api_url + Global.divide_api, form);
        StartCoroutine(divideProcess(www));
    }

    IEnumerator divideProcess(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            StartCoroutine(GotoScene("tableUsage"));
        }
    }

    public void CancelOrder()
    {
        //주문취소
        extraPopup.SetActive(false);
        selectedList.Clear();
        bool is_cooking = false;
        bool is_contain_prepay = false;
        for (int i = 0; i < mtableUsageObj.Count; i++)
        {
            try
            {
                if (mtableUsageObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    if (mtableUsageObj[i].transform.Find("status").GetComponent<Text>().text == "2" || mtableUsageObj[i].transform.Find("status").GetComponent<Text>().text == "3")
                    {
                        is_cooking = true;
                    }
                    selectedList.Add(mtableUsageObj[i].transform.Find("order_id").GetComponent<Text>().text);
                }
            }
            catch (Exception ex)
            {

            }
        }
        for (int i = 0; i < mMenuObj.Count; i++)
        {
            try
            {
                if (mMenuObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    if (mMenuObj[i].transform.Find("status").GetComponent<Text>().text == "2" || mMenuObj[i].transform.Find("status").GetComponent<Text>().text == "3")
                    {
                        is_cooking = true;
                    }
                    if(mMenuObj[i].transform.Find("name").GetComponent<Text>().text == "선결제")
                    {
                        is_contain_prepay = true;
                    }
                    selectedList.Add(mMenuObj[i].transform.Find("order_id").GetComponent<Text>().text);
                }
            }
            catch (Exception ex)
            {

            }
        }
        if(is_contain_prepay)
        {
            err_popup.SetActive(true);
            err_str.text = "선결제 취소는 결제관리에서 취소가 가능합니다.";
            return;
        }

        if (selectedList.Count == 0)
        {
            err_popup.SetActive(true);
            err_str.text = "취소할 주문을 선택하세요.";
        }
        else
        {
            if (is_cooking)
            {
                select_str.text = "현재 조리 중인 메뉴입니다. 주문을 취소하시겠습니까?";
                select_popup.SetActive(true);
                select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
                select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { onConfirmPopup(); });
            }
            else
            {
                onConfirmPopup();
            }
        }
    }

    public void onConfirmPopup()
    {
        //취소
        WWWForm form = new WWWForm();
        string oinfo = "[";
        for (int i = 0; i < selectedList.Count; i++)
        {
            if (i == 0)
            {
                oinfo += "{";
            }
            else
            {
                oinfo += ",{";
            }
            oinfo += "\"order_id\":\"" + selectedList[i] + "\"}";
        }
        oinfo += "]";
        Debug.Log(oinfo);
        form.AddField("order_info", oinfo);
        form.AddField("table_id", Global.cur_tInfo.tid);
        form.AddField("pub_id", Global.userinfo.pub.id);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        form.AddField("pos_no", Global.setinfo.pos_no);
        WWW www = new WWW(Global.api_url + Global.cancel_order_api, form);
        StartCoroutine(CancelOrder(www));
    }

    IEnumerator CancelOrder(WWW www)
    {
        yield return www;
        if (www.error == null)
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
                    StartCoroutine(GotoScene("tableUsage"));
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }
            else
            {
                err_str.text = jsonNode["msg"];
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "주문취소시에 알지 못할 오류가 발생하였습니다.";
            err_popup.SetActive(true);
        }
    }

    public void onCloseErrPopup()
    {
        if (err_popup.activeSelf)
        {
            err_popup.SetActive(false);
        }
        if (select_popup.activeSelf)
        {
            select_popup.SetActive(false);
        }
    }

    IEnumerator ServiceOrderProcess(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                StartCoroutine(GotoScene("tableUsage"));
            }
            else
            {
                err_str.text = jsonNode["msg"];
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "서비스 설정시에 알지 못할 오류가 발생하였습니다.";
            err_popup.SetActive(true);
        }
    }

    public void onService()
    {
        extraPopup.SetActive(false);
        selectedList.Clear();
        for (int i = 0; i < mtableUsageObj.Count; i++)
        {
            try
            {
                if (mtableUsageObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    selectedList.Add(mtableUsageObj[i].transform.Find("order_id").GetComponent<Text>().text);
                }
            }
            catch (Exception ex)
            {

            }
        }
        for (int i = 0; i < mMenuObj.Count; i++)
        {
            try
            {
                if (mMenuObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    selectedList.Add(mMenuObj[i].transform.Find("order_id").GetComponent<Text>().text);
                }
            }
            catch (Exception ex)
            {

            }
        }
        if (selectedList.Count == 0)
        {
            err_popup.SetActive(true);
            err_str.text = "서비스 처리할 주문을 선택하세요.";
        }
        else
        {
            select_popup.SetActive(true);
            select_str.text = "선택한 메뉴를 서비스 처리/취소 하시겠습니까?";
            select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
            select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { ServiceOrder(); });
        }
    }

    void ServiceOrder()
    {
        WWWForm form = new WWWForm();
        string oinfo = "[";
        for (int i = 0; i < selectedList.Count; i++)
        {
            if (i == 0)
            {
                oinfo += "{";
            }
            else
            {
                oinfo += ",{";
            }
            oinfo += "\"order_id\":\"" + selectedList[i] + "\"}";
        }
        oinfo += "]";
        Debug.Log(oinfo);
        form.AddField("order_info", oinfo);
        form.AddField("pub_id", Global.userinfo.pub.id);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        form.AddField("pos_no", Global.setinfo.pos_no);
        WWW www = new WWW(Global.api_url + Global.service_order_api, form);
        StartCoroutine(ServiceOrderProcess(www));
    }

    public void onDiscount()
    {
        //할인
        extraPopup.SetActive(false);
        popup_type = 3;//할인금액 입력 팝업
        discountPopup.SetActive(true);
    }

    public void onConfirmDiscount()
    {
        //할인금액 입력 확인
        if (discountPopup.transform.Find("background/val").GetComponent<InputField>().text.Trim() == ""
            || discountPopup.transform.Find("background/val").GetComponent<InputField>().text == "0")
        {
            err_str.text = "할인금액을 정확히 입력하세요.";
            err_popup.SetActive(true);
        }
        else
        {
            try
            {
                WWWForm form = new WWWForm();
                form.AddField("table_id", Global.cur_tInfo.tid);
                form.AddField("pub_id", Global.userinfo.pub.id);
                form.AddField("price", int.Parse(discountPopup.transform.Find("background/val").GetComponent<InputField>().text));
                DateTime dt = Global.GetSdate();
                form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
                WWW www = new WWW(Global.api_url + Global.add_discount_api, form);
                StartCoroutine(onAddDiscountProcess(www));
            }catch (Exception ex)
            {

            }

        }
    }

    IEnumerator onAddDiscountProcess(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            try
            {
                JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
                Debug.Log(jsonNode);
                if (jsonNode["suc"].AsInt == 1)
                {
                    TableMenuOrderInfo moInfo = new TableMenuOrderInfo();
                    moInfo.order_id = jsonNode["order_id"];
                    moInfo.menu_name = "할인";
                    moInfo.price = jsonNode["price"].AsInt;
                    moInfo.reg_datetime = jsonNode["reg_datetime"];
                    moInfo.is_service = 0;
                    moInfo.amount = 1;

                    GameObject tmp = Instantiate(menuItem);
                    tmp.transform.SetParent(menuItemParent.transform);
                    //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                    //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                    //float left = 0;
                    //float right = 0;
                    //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                    //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                    //tmp.transform.localScale = Vector3.one;
                    tmp.transform.Find("name").GetComponent<Text>().text = moInfo.menu_name;
                    tmp.transform.Find("order_id").GetComponent<Text>().text = moInfo.order_id.ToString();
                    tmp.transform.Find("time").GetComponent<Text>().text = moInfo.reg_datetime;
                    tmp.transform.Find("amount").GetComponent<Text>().text = moInfo.amount.ToString();
                    tmp.transform.Find("is_service").GetComponent<Text>().text = moInfo.is_service.ToString();
                    tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(moInfo.price);
                    tmp.transform.Find("product_type").GetComponent<Text>().text = "0";
                    GameObject toggleObj = tmp.transform.Find("check").gameObject;
                    Text menuIdText = tmp.transform.Find("order_id").GetComponent<Text>();
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { onClickOrderItem(toggleObj, moInfo.order_id, 2); });
                    order_price += moInfo.price;
                    orderSumPriceTxt.text = Global.GetPriceFormat(order_price);
                    total_price += moInfo.price;
                    totalPriceTxt.text = Global.GetPriceFormat(total_price);
                    mMenuObj.Add(tmp);
                    Global.tableUsageInfo.menuOrderList.Add(moInfo);
                    string payInfo = getJsonResult();
                    Debug.Log(payInfo);
                    if (Global.setinfo.pos_no == 1)
                    {
                        socket1.Emit("payInfo", JSONObject.Create(payInfo));
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.Log(ex);
            }
        }
        onClosePopup();
    }

    public void onConvertPrepayToSavedPrice()
    {
        if(prepay_price <= 0)
        {
            err_popup.SetActive(true);
            err_str.text = "전환할 선결제 금액이 없습니다.";
        }
        else
        {
            popup_type = 2;//선결제 예치금 전환
            extraPopup.SetActive(false);
            regPrepayPopup.SetActive(true);
            clearPopup();
            regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text = Global.GetPriceFormat(prepay_price);
            regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().readOnly = true;
        }
    }

    public void sel1Price()
    {
        //string pStr = regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text;
        //int price = Global.GetConvertedPrice(pStr);
        //regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text = Global.GetPriceFormat(price + 10000);
    }

    public void sel5Price()
    {
        //string pStr = regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text;
        //int price = Global.GetConvertedPrice(pStr);
        //regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text = Global.GetPriceFormat(price + 50000);
    }

    public void sel10Price()
    {
        //string pStr = regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text;
        //int price = Global.GetConvertedPrice(pStr);
        //regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text = Global.GetPriceFormat(price + 100000);
    }

    public void onCheckPrepay()
    {
        //예치금 조회
        string phone = regPrepayPopup.transform.Find("background/val1").GetComponent<InputField>().text;
        if (!checkPhoneType(phone))
        {
            err_popup.SetActive(true);
            err_str.text = "고객번호 마지막 4자리를 입력하세요.";
        }
        else
        {
            WWWForm form = new WWWForm();
            form.AddField("type", 0);
            form.AddField("pub_id", Global.userinfo.pub.id);
            form.AddField("phone", phone);
            WWW www = new WWW(Global.api_url + Global.check_client_api, form);
            StartCoroutine(checkPrepayProcess(www));
        }
    }

    IEnumerator checkPrepayProcess(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            if (jsonNode["suc"].AsInt == 1)
            {
                JSONNode clist = JSON.Parse(jsonNode["clientlist"].ToString()/*.Replace("\"", "")*/);
                clients.Clear();
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
                    cinfo.bigo = clist[i]["bigo"];
                    clients.Add(cinfo);
                }
                if (clist.Count == 1)
                {
                    viewclientInPrepay(clients[0]);
                }
                else if (clist.Count > 1)
                {
                    multiPre.SetActive(true);
                    multiPre.GetComponent<Dropdown>().options.Clear();
                    Dropdown.OptionData option = new Dropdown.OptionData();
                    option.text = " ";
                    multiPre.GetComponent<Dropdown>().options.Add(option);
                    for (int i = 0; i < clients.Count; i++)
                    {
                        option = new Dropdown.OptionData();
                        option.text = clients[i].no + " " + clients[i].name;
                        multiPre.GetComponent<Dropdown>().options.Add(option);
                    }
                    multiPre.GetComponent<Dropdown>().onValueChanged.RemoveAllListeners();
                    multiPre.GetComponent<Dropdown>().onValueChanged.AddListener((value) => {
                        SelectClientInPrepay(value);
                    }
                    );
                    multiPre.GetComponent<Dropdown>().Show();
                }
            }
            else
            {
                client_id = "";
                err_popup.SetActive(true);
                err_str.text = "고객정보가 없습니다. 고객등록 후 진행하세요.";
                //select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
                //select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { onConfirmSelectPopup(); });
            }
        }
    }

    void onConfirmSelectPopup()
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("no", regPrepayPopup.transform.Find("background/val1").GetComponent<InputField>().text);
        WWW www = new WWW(Global.api_url + Global.add_prepay_client_api, form);
        StartCoroutine(AddPrepayClient(www));
    }

    IEnumerator AddPrepayClient(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                client_id = jsonNode["client_id"];
                onClosePopup();
                err_str.text = "신규고객 등록에 성공했습니다.";
                err_popup.SetActive(true);
            }
            else
            {
                err_str.text = "신규고객 등록에 실패했습니다.";
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "신규고객 등록에 실패했습니다.";
            err_popup.SetActive(true);
        }
    }

    void SelectClientInPrepay(int value)
    {
        if (value == 0)
        {
            return;
        }
        try
        {
            viewclientInPrepay(clients[value - 1]);
            multiPre.SetActive(false);
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    void viewclientInPrepay(ClientInfo cinfo)
    {
        Debug.Log("view client prepay");
        client_id = cinfo.id;
        regPrepayPopup.transform.Find("background/val1").GetComponent<InputField>().text = cinfo.no;
        regPrepayPopup.transform.Find("background/val2").GetComponent<InputField>().text = cinfo.name;
        regPrepayPopup.transform.Find("background/val3").GetComponent<Text>().text = Global.GetPriceFormat(cinfo.prepay);
        regPrepayPopup.transform.Find("background/val5").GetComponent<InputField>().text = cinfo.bigo;
    }

    public void onSaveConvertedPrepay()
    {
        //선결제 예치금 전환 등록
        string bigo = regPrepayPopup.transform.Find("background/val5").GetComponent<InputField>().text;
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("table_id", Global.cur_tInfo.tid);
        form.AddField("charge", prepay_price);
        form.AddField("bigo", bigo);
        form.AddField("client_id", client_id);
        WWW www = new WWW(Global.api_url + Global.converted_prepay_api, form);
        StartCoroutine(convertPrepayProcess(www));
    }

    IEnumerator convertPrepayProcess(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                StartCoroutine(GotoScene("tableUsage"));
            }
        }
    }

    public void outputOrderInfo()
    {
        extraPopup.SetActive(false);

        if (Global.setinfo.paymentDeviceInfo.port == 0 || Global.setinfo.paymentDeviceInfo.ip == "" || Global.setinfo.paymentDeviceInfo.ip == null)
        {
            err_str.text = "결제단말기 세팅을 진행하세요.";
            err_popup.SetActive(true);
            return;
        }
        if (Global.setinfo.paymentDeviceInfo.type == 0)
        {
            err_str.text = "결제단말기 통신방식을 ip로 설정하세요.";
            err_popup.SetActive(true);
            return;
        }
        //주문내역 출력
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("table_id", Global.cur_tInfo.tid);
        WWW www = new WWW(Global.api_url + Global.get_output_orderlist_api, form);
        StartCoroutine(getOutputOrderlist(www));
        }

    IEnumerator getOutputOrderlist(WWW www)
    {
        yield return www;
        if(www.error == null)
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
                    order.kit01 = orderlist[i]["kit01"].AsInt;
                    order.kit02 = orderlist[i]["kit02"].AsInt;
                    order.kit03 = orderlist[i]["kit03"].AsInt;
                    order.kit04 = orderlist[i]["kit04"].AsInt;
                    orders.Add(order);
                }
                string printStr = "";
                printStr = DocumentFactory.GetOrderListDetail(orders, total_price, title: "[주문내역]");
                Debug.Log(printStr);
                byte[] sendData = NetUtils.StrToBytes(printStr);
                Socket_Send(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), sendData);
            }
        }
    }

    public void tableMove()
    {
        extraPopup.SetActive(false);
        Global.moveTableType = 0;
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("endpay");
        }
        StartCoroutine(GotoScene("tableMove"));
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
