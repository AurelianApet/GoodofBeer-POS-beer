using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text;
using SocketIO;

public class RepayMenuOrderManager : MonoBehaviour
{
    public GameObject categoryItemParent;
    public GameObject categoryItem;
    public GameObject order_item;
    public GameObject order_parent;
    public GameObject menuParent;
    public GameObject menuItem;
    public GameObject payPopup;
    public GameObject extraPopup;
    public GameObject discountPopup;
    public GameObject menuToggle;

    public Text total_priceTxt;
    public Text tableNameShow;
    public Text tagPriceTxt;
    public Text prepayTxt;

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

    public GameObject err_popup;
    public Text err_str;
    public GameObject select_popup;
    public Text select_str;
    public GameObject progress_popup;
    public Text progress_str;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket1;

    TableOrderInfo tableorderlist = new TableOrderInfo();
    List<GameObject> m_orderListObj = new List<GameObject>();
    List<GameObject> m_categorylistObj = new List<GameObject>();
    List<GameObject> m_menuListObj = new List<GameObject>();
    List<PayOrderInfo> payinfoList = new List<PayOrderInfo>();
    List<string> menuIdList = new List<string>();     //전체결제시 메뉴들의 아이디
    List<int> productTypeList = new List<int>();    //전체결제시 메뉴들의 종류 0:food, 1:beer, 2:wine
    List<string> selectedList = new List<string>();
    List<ClientInfo> clients = new List<ClientInfo>();
    Payment payment = new Payment();
    List<OrderItem> printList = new List<OrderItem>();
    int installment_months = 0;

    int total_price = 0;//전체금액
    int pay_price = 0;//결제금액
    int real_price = 0;//실지결제금액(결제금액 올림/내림 처리 하지 않은 금액 : 절사금액땜에 이용)
    int prepay_price = 0;//현재 테이블의 선결제금액
    int popup_type = -1;
    int pay_method = 0;//0-카드결제, 1-현금결제
    int used_prepay = 0;
    string client_id = "";
    int preTagType = 0;
    string pretag_id = "";
    string firscateno = "";
    string oldSelectedCategoryNo = "";
    List<OrderCartInfo> cartlist = new List<OrderCartInfo>();
    int device_type = 0; //0 : 미사용, 1 : KICC(카드), 2 : KICC(현금, 사용자), 3 : KICC(현금, 개인), 4 : KIS(카드), 5 : KIS(현금 사용자, 개인)

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
        tableNameShow.text = "재결제";
        StartCoroutine(LoadOrderList());
        StartCoroutine(LoadAllMenulist());
        if (Global.setinfo.paymentDeviceInfo.ip != "")
        {
            ipAdd = System.Net.IPAddress.Parse(Global.setinfo.paymentDeviceInfo.ip);
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            remoteEP = new IPEndPoint(ipAdd, Global.setinfo.paymentDeviceInfo.port);
        }
        socketObj = Instantiate(socketPrefab);
        //socketObj = GameObject.Find("SocketIO");
        socket1 = socketObj.GetComponent<SocketIOComponent>();
        socket1.On("open", socketOpen);
        socket1.On("error", socketError);
        socket1.On("reloadPayment", reload);
        socket1.On("createOrder", createOrder);
        socket1.On("new_notification", new_notification);
        socket1.On("close", socketClose);
    }

    public void reload(SocketIOEvent e)
    {
        if (payPopup.activeSelf)
            return;
        StartCoroutine(GotoScene("repayMenuOrder"));
    }

    public void new_notification(SocketIOEvent e)
    {
        Global.alarm_cnt++;
    }

    public void createOrder(SocketIOEvent e)
    {
        try
        {
            //주문서 출력
            JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
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

    IEnumerator LoadOrderList()
    {
        while (order_parent.transform.childCount > 0)
        {
            try
            {
                DestroyImmediate(order_parent.transform.GetChild(0).gameObject);
            }
            catch (Exception ex)
            {

            }
            yield return new WaitForFixedUpdate();
        }

        //주문정보 가져오기 api
        WWWForm form = new WWWForm();
        form.AddField("payment_id", Global.cur_pay_id);
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.get_repay_order_api, form);
        StartCoroutine(GetTableorderlistFromApi(www));
    }

    IEnumerator GetTableorderlistFromApi(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            tableorderlist.total_price = jsonNode["total_price"].AsInt;
            tableorderlist.prepay_price = jsonNode["prepay_price"].AsInt;
            tableorderlist.menuorderinfo = new List<TableMOrderInfo>();
            JSONNode olist = JSON.Parse(jsonNode["orderlist"].ToString()/*.Replace("\"", "")*/);
            List<string> oid_str = new List<string>();
            for (int i = 0; i < olist.Count; i++)
            {
                TableMOrderInfo minfo = new TableMOrderInfo();
                minfo.menu_name = olist[i]["menu_name"];
                minfo.menu_total_amount = olist[i]["menu_total_amount"].AsInt;
                minfo.menu_total_price = olist[i]["menu_total_price"].AsInt;
                minfo.menu_id = olist[i]["menu_id"];
                minfo.type = 0;
                minfo.status = olist[i]["status"].AsInt;
                minfo.is_service = olist[i]["is_service"].AsInt;
                minfo.order_ids = new List<string>();
                JSONNode order_ids = JSON.Parse(olist[i]["order_info"].ToString());
                string str = "";
                for(int j = 0; j < order_ids.Count; j++)
                {
                    minfo.order_ids.Add(order_ids[j]["order_id"]);
                    str += order_ids[j]["order_id"];
                    if(j < order_ids.Count - 1)
                    {
                        str += ",";
                    }
                }
                oid_str.Add(str);
                tableorderlist.menuorderinfo.Add(minfo);
            }

            tagPriceTxt.text = "0";
            prepayTxt.text = Global.GetPriceFormat(tableorderlist.prepay_price);
            //total_price = tableorderlist.total_price + tableorderlist.tag_price - tableorderlist.prepay_price;
            total_price = tableorderlist.total_price;
            total_priceTxt.text = Global.GetPriceFormat(tableorderlist.total_price);

            m_orderListObj.Clear();
            for (int i = 0; i < tableorderlist.menuorderinfo.Count; i++)
            {
                //UI
                GameObject tmp = Instantiate(order_item);
                tmp.transform.SetParent(order_parent.transform);
                //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmp.transform.localScale = Vector3.one;
                tmp.transform.Find("name").GetComponent<Text>().text = tableorderlist.menuorderinfo[i].menu_name;
                tmp.transform.Find("cnt").GetComponent<Text>().text = tableorderlist.menuorderinfo[i].menu_total_amount.ToString();
                tmp.transform.Find("menu_id").GetComponent<Text>().text = tableorderlist.menuorderinfo[i].menu_id.ToString();
                tmp.transform.Find("product_type").GetComponent<Text>().text = "0";
                tmp.transform.Find("type").GetComponent<Text>().text = tableorderlist.menuorderinfo[i].type.ToString();
                tmp.transform.Find("status").GetComponent<Text>().text = tableorderlist.menuorderinfo[i].status.ToString();
                tmp.transform.Find("is_service").GetComponent<Text>().text = tableorderlist.menuorderinfo[i].is_service.ToString();
                tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(tableorderlist.menuorderinfo[i].menu_total_price);
                tmp.transform.Find("order_ids").GetComponent<Text>().text = oid_str[i];
                GameObject toggleObj = tmp.transform.Find("check").gameObject;
                tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                tmp.GetComponent<Button>().onClick.AddListener(delegate () { onSelItem(toggleObj); });
                m_orderListObj.Add(tmp);
            }
            total_priceTxt.text = Global.GetPriceFormat(tableorderlist.total_price);

            Global.tableUsageInfo.tableId = "";
            Global.tableUsageInfo.tagUsageList = new List<TableTagUsageInfo>();
            Global.tableUsageInfo.menuOrderList = new List<TableMenuOrderInfo>();
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

    IEnumerator LoadAllMenulist()
    {
        while (categoryItemParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(categoryItemParent.transform.GetChild(0).gameObject));
        }
        while (categoryItemParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        Global.categorylist.Clear();
        //모든 메뉴정보 가져오기
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("app_type", Global.app_type);
        WWW www = new WWW(Global.api_url + Global.get_categorylist_api, form);
        StartCoroutine(GetCategorylistFromApi(www));
    }

    IEnumerator GetCategorylistFromApi(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            JSONNode c_list = JSON.Parse(jsonNode["categorylist"].ToString()/*.Replace("\"", "")*/);
            //json구조 해석해서 Global에 값을 대입
            Debug.Log("category list count = " + c_list.Count);
            for(int i = 0; i < c_list.Count; i++)
            {
                if (c_list[i]["name"] != "TAG")      //TAG상품 제외
                {
                    firscateno = c_list[i]["id"];
                    break;
                }
            }
            for (int i = 0; i < c_list.Count; i++)
            {
                if(c_list[i]["name"] != "TAG")      //TAG상품 제외
                {
                    Debug.Log("loading category list..");
                    CategoryInfo cateInfo = new CategoryInfo();
                    try
                    {
                        cateInfo.name = c_list[i]["name"];
                        cateInfo.id = c_list[i]["id"];
                    }
                    catch (Exception ex)
                    {

                    }
                    cateInfo.menulist = new List<MenuInfo>();
                    JSONNode m_list = JSON.Parse(c_list[i]["menulist"].ToString()/*.Replace("\"", "")*/);
                    int menuCnt = m_list.Count;
                    for (int j = 0; j < menuCnt; j++)
                    {
                        MenuInfo minfo = new MenuInfo();
                        minfo.name = m_list[j]["name"];
                        minfo.engname = m_list[j]["engname"];
                        minfo.barcode = m_list[j]["barcode"];
                        minfo.contents = m_list[j]["contents"];
                        minfo.sort_order = m_list[j]["sort_order"].AsInt;
                        minfo.id = m_list[j]["id"];
                        minfo.price = m_list[j]["price"].AsInt;
                        minfo.pack_price = m_list[j]["pack_price"].AsInt;
                        minfo.is_best = m_list[j]["is_best"].AsInt;
                        minfo.is_soldout = m_list[j]["is_soldout"].AsInt;
                        minfo.product_type = c_list[i]["product_type"].AsInt;
                        cateInfo.menulist.Add(minfo);
                    }
                    Global.categorylist.Add(cateInfo);
                }
            }

            //UI에 추가
            m_categorylistObj.Clear();
            for (int i = 0; i < Global.categorylist.Count; i++)
            {
                try
                {
                    GameObject tmp = Instantiate(categoryItem);
                    tmp.transform.SetParent(categoryItemParent.transform);
                    //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                    //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                    //float left = 0;
                    //float right = 0;
                    //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                    //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                    //tmp.transform.localScale = Vector3.one;
                    tmp.GetComponent<Text>().text = Global.categorylist[i].name;
                    tmp.transform.Find("id").GetComponent<Text>().text = Global.categorylist[i].id.ToString();
                    string t_cateNo = Global.categorylist[i].id;
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { StartCoroutine(LoadMenuList(t_cateNo)); });
                    m_categorylistObj.Add(tmp);
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }

            if (c_list.Count > 0 && firscateno != "")
                StartCoroutine(LoadMenuList(firscateno));
        }
        else
        {
            Debug.Log(www.error);
        }
    }

    IEnumerator Destroy_Object(GameObject obj)
    {
        DestroyImmediate(obj);
        yield return null;
    }

    IEnumerator LoadMenuList(string cateno)
    {
        Debug.Log("old = " + oldSelectedCategoryNo + ", new = " + cateno);
        //선택된 카테고리 볼드체로.
        try
        {
            for (int i = 0; i < categoryItemParent.transform.childCount; i++)
            {
                if (categoryItemParent.transform.GetChild(i).transform.Find("id").GetComponent<Text>().text == cateno.ToString())
                {
                    categoryItemParent.transform.GetChild(i).GetComponent<Text>().color = Global.selected_color;
                }
                else
                {
                    categoryItemParent.transform.GetChild(i).GetComponent<Text>().color = Color.white;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
        //UI 내역 초기화
        while (menuParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(menuParent.transform.GetChild(0).gameObject));
        }
        while (menuParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }

        oldSelectedCategoryNo = cateno;

        //카테고리에 한한 메뉴리스트 가져오기
        List<MenuInfo> minfoList = new List<MenuInfo>();
        for (int i = 0; i < Global.categorylist.Count; i++)
        {
            if (Global.categorylist[i].id == cateno)
            {
                minfoList = Global.categorylist[i].menulist; break;
            }
        }
        int menuCnt = minfoList.Count;
        m_menuListObj.Clear();
        for (int i = 0; i < menuCnt; i++)
        {
            try
            {
                GameObject tmp = Instantiate(menuItem);
                tmp.transform.SetParent(menuParent.transform);
                //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmp.transform.localScale = Vector3.one;
                tmp.transform.Find("id").GetComponent<Text>().text = minfoList[i].id.ToString();
                tmp.transform.Find("name").GetComponent<Text>().text = minfoList[i].name;
                tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(minfoList[i].price);
                OrderCartInfo cinfo = new OrderCartInfo();
                cinfo.name = minfoList[i].name;
                cinfo.menu_id = minfoList[i].id;
                cinfo.price = minfoList[i].price;
                cinfo.is_best = minfoList[i].is_best;
                cinfo.product_type = minfoList[i].product_type;
                cinfo.amount = 1;
                cinfo.status = 0;
                if (minfoList[i].is_soldout == 1)
                {
                    tmp.transform.Find("name").GetComponent<Text>().color = Color.black;
                    tmp.transform.Find("price").GetComponent<Text>().color = Color.black;
                }
                else
                {
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { addList(cinfo); });
                }
                m_menuListObj.Add(tmp);
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    public void addList(OrderCartInfo cinfo)
    {
        Debug.Log("add list");
        cartlist = Global.addOneCartItem(cinfo, cartlist);
        bool is_found = false;
        total_price += cinfo.price;
        total_priceTxt.text = Global.GetPriceFormat(total_price);
        for (int i = 0; i < order_parent.transform.childCount; i++)
        {
            if(order_parent.transform.GetChild(i).Find("menu_id").GetComponent<Text>().text == cinfo.menu_id.ToString()
                && order_parent.transform.GetChild(i).Find("type").GetComponent<Text>().text == "1")
            {
                is_found = true;
                try
                {
                    order_parent.transform.GetChild(i).Find("cnt").GetComponent<Text>().text =
                        (int.Parse(order_parent.transform.GetChild(i).Find("cnt").GetComponent<Text>().text) + 1).ToString();
                    order_parent.transform.GetChild(i).Find("price").GetComponent<Text>().text =
                        Global.GetPriceFormat(Global.GetConvertedPrice(order_parent.transform.GetChild(i).Find("price").GetComponent<Text>().text) + cinfo.price);
                }
                catch (Exception ex)
                {

                }
                break;
            }
        }
        if (!is_found)
        {
            GameObject tmp = Instantiate(order_item);
            tmp.transform.SetParent(order_parent.transform);
            //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
            //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
            //float left = 0;
            //float right = 0;
            //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
            //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
            //tmp.transform.localScale = Vector3.one;
            tmp.transform.Find("name").GetComponent<Text>().text = cinfo.name;
            tmp.transform.Find("cnt").GetComponent<Text>().text = cinfo.amount.ToString();
            tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(cinfo.price);
            //tmp.transform.Find("id").GetComponent<Text>().text = cinfo.order_id.ToString();
            tmp.transform.Find("menu_id").GetComponent<Text>().text = cinfo.menu_id.ToString();
            tmp.transform.Find("product_type").GetComponent<Text>().text = cinfo.product_type.ToString();
            tmp.transform.Find("type").GetComponent<Text>().text = "1";
            tmp.transform.Find("is_service").GetComponent<Text>().text = "0";
            tmp.transform.Find("name").GetComponent<Text>().color = Global.selected_color;
            tmp.transform.Find("cnt").GetComponent<Text>().color = Global.selected_color;
            tmp.transform.Find("price").GetComponent<Text>().color = Global.selected_color;
            GameObject toggleObj = tmp.transform.Find("check").gameObject;
            tmp.GetComponent<Button>().onClick.RemoveAllListeners();
            tmp.GetComponent<Button>().onClick.AddListener(delegate () { onSelItem(toggleObj); });
            m_orderListObj.Add(tmp);
        }
    }

    public void Pack()
    {
        err_str.text = "메뉴 추가 등록 후 바로 결제를 진행해주세요.";
        err_popup.SetActive(true);
        /*
        bool takeout = false;
        for (int i = 0; i < cartlist.Count; i++)
        {
            if (cartlist[i].is_best == 999)
            {
                err_str.text = "포장이 불가한 메뉴입니다.";
                err_popup.SetActive(true);
                takeout = true;
                break;
            }
        }
        if (!takeout)
        {
            //포장처리
            WWWForm form = new WWWForm();
            form.AddField("type", 1);
            form.AddField("pub_id", Global.userinfo.pub.id);
            form.AddField("tag_id", Global.cur_tagInfo.tag_id);
            form.AddField("table_id", Global.cur_tInfo.tid);
            string oinfo = "[";
            for (int i = 0; i < cartlist.Count; i++)
            {
                if (i == 0)
                {
                    oinfo += "{";
                }
                else
                {
                    oinfo += ",{";
                }
                float new_price = 0f;
                if (cartlist[i].is_best >= 100)
                {
                    new_price = cartlist[i].price + cartlist[i].is_best;
                }
                else
                {
                    new_price = cartlist[i].price * (100 + cartlist[i].is_best) / 100;
                }
                oinfo += "\"menu_id\":\"" + cartlist[i].menu_id + "\","
                    + "\"menu_name\":\"" + cartlist[i].name + "\","
                    + "\"product_type\":\"" + cartlist[i].product_type + "\","
                    + "\"price\":" + new_price.ToString() + ","
                    + "\"quantity\":" + cartlist[i].amount.ToString() + "}";
            }
            oinfo += "]";
            Debug.Log(oinfo);
            form.AddField("order_info", oinfo);
            form.AddField("is_pay_after", 1);
            DateTime dt = Global.GetSdate();
            form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
            form.AddField("pos_no", Global.setinfo.pos_no);
            WWW www = new WWW(Global.api_url + Global.order_api, form);
            StartCoroutine(ProcessOrder(www));
        }*/
    }

    IEnumerator ProcessOrder(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                if (Global.setinfo.pos_no == 1)
                {
                    socket1.Emit("endpay");
                }
                StartCoroutine(GotoScene("main"));
            }
            else
            {
                err_str.text = jsonNode["msg"];
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "주문시 알지 못할 오류가 발생하였습니다.";
            err_popup.SetActive(true);
        }
    }

    public void Order()
    {/*
        //주문 api
        WWWForm form = new WWWForm();
        form.AddField("type", 0);
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("tag_id", Global.cur_tagInfo.tag_id);
        form.AddField("table_id", Global.cur_tInfo.tid);
        form.AddField("is_pay_after", 1);
        string oinfo = "[";
        for (int i = 0; i < cartlist.Count; i++)
        {
            if (i == 0)
            {
                oinfo += "{";
            }
            else
            {
                oinfo += ",{";
            }
            oinfo += "\"menu_id\":\"" + cartlist[i].menu_id + "\","
                + "\"menu_name\":\"" + cartlist[i].name + "\","
                + "\"product_type\":\"" + cartlist[i].product_type + "\","
                + "\"price\":" + cartlist[i].price.ToString() + ","
                + "\"quantity\":" + cartlist[i].amount.ToString() + "}";
        }
        oinfo += "]";
        Debug.Log(oinfo);
        form.AddField("order_info", oinfo);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        form.AddField("pos_no", Global.setinfo.pos_no);
        WWW www = new WWW(Global.api_url + Global.order_api, form);
        StartCoroutine(ProcessOrder(www));*/
    }

    void onZeroPay()
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
        form.AddField("pay_type", 0);//현금결제
        form.AddField("credit_card_company", credit_card_company);
        form.AddField("credit_card_number", credit_card_number);
        form.AddField("device_type", device_type);
        form.AddField("installment_months", installment_months);
        form.AddField("is_repay", 1);
        form.AddField("repay_id", Global.cur_pay_id);
        form.AddField("is_allPay", 1);

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
                + "\"menu_id\":\"" + menuIdList[i] + "\","
                + "\"product_type\":\"" + productTypeList[i] + "\","
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

    public void AllPay()
    {
        payinfoList.Clear();
        for (int i = 0; i < m_orderListObj.Count; i++)
        {
            try
            {
                string oids = m_orderListObj[i].transform.Find("order_ids").GetComponent<Text>().text;
                //전체결제시 api에 보내는 payinfo의 amount값에 관한 론리////
                if (oids == "" || oids == null)
                {
                    PayOrderInfo pinfo = new PayOrderInfo();
                    pinfo.amount = int.Parse(m_orderListObj[i].transform.Find("cnt").GetComponent<Text>().text);
                    pinfo.price = Global.GetConvertedPrice(m_orderListObj[i].transform.Find("price").GetComponent<Text>().text);
                    pinfo.menu_name = m_orderListObj[i].transform.Find("name").GetComponent<Text>().text;
                    pinfo.order_id = "";
                    string menu_id = m_orderListObj[i].transform.Find("menu_id").GetComponent<Text>().text;
                    int product_type = int.Parse(m_orderListObj[i].transform.Find("product_type").GetComponent<Text>().text);
                    productTypeList.Add(product_type);
                    menuIdList.Add(menu_id);
                    payinfoList.Add(pinfo);
                }
                else
                {
                    string[] oid_tmp = oids.Split(',');
                    for (int j = 0; j < oid_tmp.Length; j++)
                    {
                        if (oid_tmp[j] == "" || oid_tmp[j] == null)
                        {
                            continue;
                        }
                        PayOrderInfo pinfo = new PayOrderInfo();
                        pinfo.amount = int.Parse(m_orderListObj[i].transform.Find("cnt").GetComponent<Text>().text) / oid_tmp.Length;
                        pinfo.price = Global.GetConvertedPrice(m_orderListObj[i].transform.Find("price").GetComponent<Text>().text) / oid_tmp.Length;
                        pinfo.menu_name = m_orderListObj[i].transform.Find("name").GetComponent<Text>().text;
                        pinfo.order_id = oid_tmp[j];
                        string menu_id = m_orderListObj[i].transform.Find("menu_id").GetComponent<Text>().text;
                        menuIdList.Add(menu_id);
                        int product_type = int.Parse(m_orderListObj[i].transform.Find("product_type").GetComponent<Text>().text);
                        productTypeList.Add(product_type);
                        payinfoList.Add(pinfo);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        selectedList.Clear();
        for (int i = 0; i < m_orderListObj.Count; i++)
        {
            try
            {
                selectedList.Add(m_orderListObj[i].transform.Find("tag_id").GetComponent<Text>().text);
            }
            catch (Exception ex)
            {

            }
        }

        device_type = 0;
        app_no = "";
        credit_card_company = "";
        credit_card_number = "";
        pay_price = Global.GetPrice(total_price);
        real_price = total_price;
        if(pay_price == 0)
        {
            bool is_found = false;
            for (int i = 0; i < order_parent.transform.childCount; i++)
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
        if(pay_price < 0)
        {
            err_popup.SetActive(true);
            err_str.text = "결제금액이 0보다 작습니다.";
            return;
        }
        popup_type = 4;//결제팝업
        used_point = 0;
        used_prepay = 0;
        payPopup.SetActive(true);
        clearPopup();
        string payInfo = getJsonResult();
        Debug.Log(payInfo);
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("payInfo", JSONObject.Create(payInfo));
        }
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
        form.AddField("is_repay", 1);
        form.AddField("repay_id", Global.cur_pay_id);
        int output_type = 0;
        if (payPopup.transform.Find("background/1/output/detail").GetComponent<Toggle>().isOn)
        {
            output_type = 3;
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
            //int invoice_type = -1;//영수증 미사용
            //if (setInvoice.GetComponent<Toggle>().isOn)
            //{
            //    if (invoice.transform.Find("bus").GetComponent<Toggle>().isOn)
            //    {
            //        invoice_type = 1;//사업자
            //    }
            //    else
            //    {
            //        invoice_type = 0;//개인
            //    }
            //}
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
                + "\"menu_id\":\"" + menuIdList[i] + "\","
                + "\"product_type\":\"" + productTypeList[i] + "\","
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
        if (popup_type == 6)
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
            //테이블 메뉴주문의 주문내역에서 나오는 이미 주문된 메뉴들은 모두 메뉴별로 수량이 합산된 주문들이므로
            //pInfo로 보내는 amount와 price를 그대로 이용할수 없으므로 api쪽에서 orderItem으로부터 해당 정보얻기위해 이용함.
            form.AddField("is_allPay", 1);
        }
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        form.AddField("pos_no", Global.setinfo.pos_no);
        WWW www = new WWW(Global.api_url + Global.pay_api, form);
        StartCoroutine(payProcess(www));
    }

    string getJsonResult()
    {
        int tagSum = 0;
        int orderSum = 0;
        string str = "{\"taglist\":[";
        try
        {
            for (int i = 0; i < Global.tableUsageInfo.tagUsageList.Count; i++)
            {
                if (i > 0)
                {
                    str += ",";
                }
                str += "{\"tagName\":\"" + Global.tableUsageInfo.tagUsageList[i].tagName + "\"";
                str += ",\"count\":\"" + Global.tableUsageInfo.tagUsageList[i].tagUsageCnt + "\"";
                str += ",\"price\":\"" + Global.tableUsageInfo.tagUsageList[i].tagUsagePrice + "\"";
                str += ",\"status\":\"" + Global.tableUsageInfo.tagUsageList[i].status + "\"";
                str += ",\"is_checked\":\"" + true + "\"";
                tagSum += Global.tableUsageInfo.tagUsageList[i].tagUsagePrice;
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
            for (int i = 0; i < Global.tableUsageInfo.menuOrderList.Count; i++)
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
                str += ",\"is_checked\":\"" + true + "\"}";
                orderSum += Global.tableUsageInfo.menuOrderList[i].price;
            }
            for (int i = 0; i < order_parent.transform.childCount; i++)
            {
                if (order_parent.transform.GetChild(i).Find("type").GetComponent<Text>().text == "1")
                {
                    try
                    {
                        int _price = Global.GetConvertedPrice(order_parent.transform.GetChild(i).Find("price").GetComponent<Text>().text);
                        str += ",{\"amount\":\"" + order_parent.transform.GetChild(i).Find("cnt").GetComponent<Text>().text + "\"";
                        str += ",\"name\":\"" + order_parent.transform.GetChild(i).Find("name").GetComponent<Text>().text + "\"";
                        str += ",\"price\":\"" + _price + "\"";
                        str += ",\"status\":\"" + order_parent.transform.GetChild(i).Find("status").GetComponent<Text>().text + "\"";
                        str += ",\"is_checked\":\"" + true + "\"}";
                        orderSum += _price;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex);
                    }
                }
            }
            str += "],\"tagPrice\":\"" + tagSum + "\"";
            str += ",\"totalPrice\":\"" + total_price + "\"";
            str += ",\"menuPrice\":\"" + orderSum + "\"";
            str += ",\"payPrice\":\"" + pay_price + "\"}";
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
        }
    }

    void payProcess()
    {
        if (popup_type == 6)
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
            if (used_prepay > 0 && payPopup.transform.Find("background/1/pre/precard").GetComponent<Toggle>().isOn)
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
                payFunc();
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
        StopCoroutine("checkPaymentResult");
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
                        output_type = 3;
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
                        byte[] sendData = NetUtils.StrToBytes(printStr);
                        Socket_Send(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), sendData);
                    }
                }
                catch (Exception ex)
                {

                }

                cancelAllTags();
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
                if (Global.setinfo.pos_no == 1)
                {
                    socket1.Emit("endpay");
                }
                StartCoroutine(GotoScene("main"));
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
        popup_type = 6;
        extraPopup.SetActive(false);
        payPopup.SetActive(true);
        clearPopup();
    }

    int used_point = 0;
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

    IEnumerator ServiceOrderProcess(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                if (Global.setinfo.pos_no == 1)
                {
                    socket1.Emit("endpay");
                }
                StartCoroutine(GotoScene("repayMenuOrder"));
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
        string oinfo = "[";
        int newOrder_cnt = 0;
        for (int i = 0; i < m_orderListObj.Count; i++)
        {
            try
            {
                if (m_orderListObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    if(m_orderListObj[i].transform.Find("order_ids").GetComponent<Text>().text.Trim() == "")
                    {
                        if (newOrder_cnt == 0)
                        {
                            oinfo += "{";
                        }
                        else
                        {
                            oinfo += ",{";
                        }
                        Debug.Log(oinfo);
                        oinfo += "\"menu_id\":\"" + int.Parse(m_orderListObj[i].transform.Find("menu_id").GetComponent<Text>().text) + "\","
                            + "\"menu_name\":\"" + m_orderListObj[i].transform.Find("name").GetComponent<Text>().text + "\","
                            + "\"product_type\":\"" + int.Parse(m_orderListObj[i].transform.Find("product_type").GetComponent<Text>().text) + "\","
                            + "\"price\":" + Global.GetConvertedPrice(m_orderListObj[i].transform.Find("price").GetComponent<Text>().text) + ","
                            + "\"quantity\":" + int.Parse(m_orderListObj[i].transform.Find("cnt").GetComponent<Text>().text) + "}";
                        Debug.Log(oinfo);
                        newOrder_cnt++;
                    }
                    else
                    {
                        string oids = m_orderListObj[i].transform.Find("order_ids").GetComponent<Text>().text;
                        string[] oid_tmp = oids.Split(',');
                        for (int j = 0; j < oid_tmp.Length; j++)
                        {
                            if (oid_tmp[j] == "" || oid_tmp[j] == null)
                            {
                                continue;
                            }
                            selectedList.Add(oid_tmp[j]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
        oinfo += "]";
        Debug.Log(oinfo);
        if (selectedList.Count == 0 && newOrder_cnt == 0)
        {
            err_popup.SetActive(true);
            err_str.text = "서비스 처리할 주문을 선택하세요.";
        }
        else
        {
            select_popup.SetActive(true);
            select_str.text = "선택한 메뉴를 서비스 처리/취소 하시겠습니까?";
            select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
            select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { ServiceOrder(oinfo); });
        }
    }

    void ServiceOrder(string newOrderInfo)
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
        form.AddField("new_order_info", newOrderInfo);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        form.AddField("pos_no", Global.setinfo.pos_no);
        WWW www = new WWW(Global.api_url + Global.service_order_api, form);
        StartCoroutine(ServiceOrderProcess(www));
    }

    public void onDiscount()
    {
        //할인
        popup_type = 3;//할인금액 입력 팝업
        extraPopup.SetActive(false);
        discountPopup.SetActive(true);
    }

    public void OnMenuToggle()
    {
        for (int i = 0; i < order_parent.transform.childCount; i++)
        {
            try
            {
                order_parent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn = menuToggle.GetComponent<Toggle>().isOn;
            }
            catch (Exception ex)
            {

            }
        }
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
            onClosePopup();
            try
            {
                TableMenuOrderInfo moInfo = new TableMenuOrderInfo();
                moInfo.order_id = "";
                moInfo.menu_name = "할인";
                moInfo.price = int.Parse(discountPopup.transform.Find("background/val").GetComponent<InputField>().text) * -1;
                moInfo.reg_datetime = DateTime.Now.ToString("HH:mm");
                moInfo.amount = 1;
                moInfo.is_service = 0;

                GameObject tmp = Instantiate(order_item);
                tmp.transform.SetParent(order_parent.transform);
                //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmp.transform.localScale = Vector3.one;
                tmp.transform.Find("name").GetComponent<Text>().text = moInfo.menu_name;
                tmp.transform.Find("order_ids").GetComponent<Text>().text = moInfo.order_id.ToString();
                tmp.transform.Find("cnt").GetComponent<Text>().text = moInfo.amount.ToString();
                tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(moInfo.price);
                tmp.transform.Find("is_service").GetComponent<Text>().text = moInfo.is_service.ToString();
                GameObject toggleObj = tmp.transform.Find("check").gameObject;
                tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                tmp.GetComponent<Button>().onClick.AddListener(delegate () { onSelItem(toggleObj); });
                total_price += moInfo.price;
                total_priceTxt.text = Global.GetPriceFormat(total_price);
                m_orderListObj.Add(tmp);
            }
            catch (Exception ex)
            {

            }

        }
    }

    IEnumerator onAddDiscountProcess(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            try
            {
                JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
                if (jsonNode["suc"].AsInt == 1)
                {
                    TableMenuOrderInfo moInfo = new TableMenuOrderInfo();
                    moInfo.order_id = jsonNode["order_id"];
                    moInfo.menu_name = "할인";
                    moInfo.price = jsonNode["price"].AsInt;
                    moInfo.reg_datetime = jsonNode["reg_datetime"];
                    moInfo.amount = 1;
                    moInfo.is_service = 0;

                    GameObject tmp = Instantiate(order_item);
                    tmp.transform.SetParent(order_parent.transform);
                    //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                    //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                    //float left = 0;
                    //float right = 0;
                    //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                    //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                    //tmp.transform.localScale = Vector3.one;
                    tmp.transform.Find("name").GetComponent<Text>().text = moInfo.menu_name;
                    tmp.transform.Find("order_ids").GetComponent<Text>().text = moInfo.order_id.ToString();
                    tmp.transform.Find("cnt").GetComponent<Text>().text = moInfo.amount.ToString();
                    tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(moInfo.price);
                    tmp.transform.Find("is_service").GetComponent<Text>().text = moInfo.is_service.ToString();
                    tmp.transform.Find("product_type").GetComponent<Text>().text = "0";
                    GameObject toggleObj = tmp.transform.Find("check").gameObject;
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { onSelItem(toggleObj); });
                    total_price += moInfo.price;
                    total_priceTxt.text = Global.GetPriceFormat(total_price);
                    m_orderListObj.Add(tmp);
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
        onClosePopup();
    }

    public void onClosePopup()
    {
        switch (popup_type)
        {
            case 3:
                {
                    discountPopup.SetActive(false);
                    break;
                };
            case 4:
                {
                    if (Global.setinfo.pos_no == 1)
                    {
                        socket1.Emit("endpay");
                    }
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

    void onClickOrderItem(GameObject toggleObj, int id, int type = 0, int tag_index = -1)
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

    public void onConvertPrepayToSavedPrice()
    {
        extraPopup.SetActive(false);
    }

    public void outputOrderInfo()
    {
        //주문내역 출력
        extraPopup.SetActive(false);
    }
    
    public void tableMove()
    {
        extraPopup.SetActive(false);
    }

    public void onCloseErrPopup()
    {
        err_popup.SetActive(false);
    }

    public void onCloseSelPopup()
    {
        select_popup.SetActive(false);
    }

    public void onBack()
    {
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("endpay");
        }
        StartCoroutine(GotoScene("main"));
    }

    public void AddTag()
    {
    }

    public void Usage()
    {
        StartCoroutine(GotoScene("repay"));
    }

    public void onCancelOrder()
    {
        extraPopup.SetActive(false);
        //주문취소
        selectedList.Clear();
        int selected_cnt = 0;
        for (int i = 0; i < m_orderListObj.Count; i++)
        {
            try
            {
                if (m_orderListObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    selected_cnt++;
                    //이미 보여진 항목 삭제
                    if (m_orderListObj[i].transform.Find("order_ids").GetComponent<Text>().text.Trim() == "")
                    {
                        string item = m_orderListObj[i].transform.Find("menu_id").GetComponent<Text>().text;
                        for (int j = 0; j < cartlist.Count; j++)
                        {
                            if (cartlist[j].menu_id == item)
                            {
                                total_price -= cartlist[j].price * cartlist[j].amount;
                                total_priceTxt.text = Global.GetPriceFormat(total_price);
                                cartlist.Remove(cartlist[j]);
                                break;
                            }
                        }
                    }
                    else
                    {
                        total_price -= Global.GetConvertedPrice(m_orderListObj[i].transform.Find("price").GetComponent<Text>().text);
                    }
                    DestroyImmediate(m_orderListObj[i]);
                    m_orderListObj.Remove(m_orderListObj[i]);
                    i--;
                }
            }
            catch (Exception ex)
            {

            }
        }
        total_priceTxt.text = Global.GetPriceFormat(total_price);
        if (selected_cnt == 0)
        {
            err_str.text = "취소할 주문를 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
    }
    
    public void onErrorPop()
    {
        err_popup.SetActive(false);
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
