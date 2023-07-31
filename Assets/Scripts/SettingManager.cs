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
using LitJson;
using SimpleJSON;
using System;
using System.Text;
using SocketIO;

public class SettingManager : MonoBehaviour
{
    public GameObject posSetPopup;
    public GameObject tablegroupSetPopup;
    public GameObject tableSetPopup;
    public GameObject tapSetPopup;
    public GameObject paymentSetPopup;
    public GameObject printerSetPopup;
    public GameObject monitorSetPopup;
    public GameObject regAdminPopup;
    public GameObject shopSetPopup;
    public GameObject menuSetDisPopup;
    public GameObject menuSetPopup;
    public GameObject paymentPriceSetPopup;
    public GameObject CountBasedTimeSetPopup;
    public GameObject menuOutputSetPopup;
    public GameObject sellSizeSetPopup;
    public GameObject invoiceOutputSetPopup;
    public GameObject pointerSetPopup;
    public GameObject prepayTagBasedTimeSetPopup;
    public GameObject paymentTimeSetPopup;
    public GameObject moveTableoutPopup;
    public GameObject tableMainPopup;
    public GameObject checkSetPopup;
    public GameObject prepayDetailPopup;
    public GameObject regPrepayPopup;
    public GameObject checkPointPopup;
    public GameObject pointDetailPopup;
    public GameObject regClientPopup;
    public GameObject payPopup;
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

    public GameObject tableGroupItem;
    public GameObject tableGroupParent;
    public GameObject tableItem;
    public GameObject tableParent;
    public GameObject adminItem;
    public GameObject adminParent;
    public GameObject shophistoryItem;
    public GameObject shophistoryParent;
    public GameObject categoryItem;
    public GameObject categoryParent;
    public GameObject menuCategoryItem;
    public GameObject menuCategoryParent;
    public GameObject menuItem;
    public GameObject menuParent;
    public GameObject menuOutCateItem;
    public GameObject menuOutCateParent;
    public GameObject menuOutItem;
    public GameObject menuOutParent;
    public GameObject cateSellSizeParent;
    public GameObject cateSellSizeItem;
    public GameObject sellSizeParent;
    public GameObject sellSizeItem;
    public GameObject checkPrepayItem;
    public GameObject checkPrepayParent;
    public GameObject prepayDetailItem;
    public GameObject prepayDetailParent;
    public GameObject checkPointItem;
    public GameObject checkPointParent;
    public GameObject pointDetailItem;
    public GameObject pointDeteailParent;
    public GameObject contentsPopup;
    public InputField contentsPopupTxt;
    public GameObject error_popup;
    public Text error_string;
    public GameObject selectPopup;
    public Text selectStr;
    public GameObject progress_popup;
    public Text progress_str;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket1;

    List<GameObject> m_tableGroupItem = new List<GameObject>();
    List<GameObject> m_tableItem = new List<GameObject>();
    List<GameObject> m_adminItem = new List<GameObject>();
    List<GameObject> m_shopItem = new List<GameObject>();
    List<GameObject> m_categoryItem = new List<GameObject>();
    List<GameObject> m_menuCategoryItem = new List<GameObject>();
    List<GameObject> m_menuItem = new List<GameObject>();
    List<GameObject> m_menuOutCateItem = new List<GameObject>();
    List<GameObject> m_menuOutItem = new List<GameObject>();
    List<GameObject> m_cateSellSizeItem = new List<GameObject>();
    List<GameObject> m_sellSizeItem = new List<GameObject>();
    List<GameObject> m_checkprepayItem = new List<GameObject>();
    List<GameObject> m_checkpointItem = new List<GameObject>();
    List<ClientInfo> clients = new List<ClientInfo>();
    Payment payment = new Payment();
    List<OrderItem> printList = new List<OrderItem>();
    int installment_months = 0;

    string client_id = "";
    string pretag_id = "";
    int popup_type = -1;
    int pay_price = 0;//결제금액
    int prepay_price = 0;//현재 테이블의 선결제금액
    int pay_method = 0;//0-카드결제, 1-현금결제
    int used_prepay = 0;
    int used_point = 0;
    int selected_tablegroup_index = -1; //테이블팝업에서 현재 선택된 테이블그룹번호
    string c_name;//고객명
    string bigo = "";
    string c_no = "";
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
        if (Global.setinfo.paymentDeviceInfo.ip != "")
        {
            ipAdd = System.Net.IPAddress.Parse(Global.setinfo.paymentDeviceInfo.ip);
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            remoteEP = new IPEndPoint(ipAdd, Global.setinfo.paymentDeviceInfo.port);
        }
        //StartCoroutine(checkSdate());
        socketObj = Instantiate(socketPrefab);
        //socketObj = GameObject.Find("SocketIO");
        socket1 = socketObj.GetComponent<SocketIOComponent>();
        socket1.On("open", socketOpen);
        socket1.On("createOrder", createOrder);
        socket1.On("new_notification", new_notification);
        socket1.On("error", socketError);
        socket1.On("close", socketClose);
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
        Debug.Log(ctime);
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
            //        error_string.text = "영업일을 " + string.Format("{0:D4}-{1:D2}-{2:D2}", ctime.Year, ctime.Month, ctime.Day) + " 로 변경하시겠습니까?\n영업일을 변경하시려면 모든 테이블의 결제를 완료하세요.";
            //    }
            //    else
            //    {
            //        error_string.text = "결제를 완료하지 않은 재결제가 있습니다. 영업일 변경을 위해 결제를 완료해주세요.\n취소시간: " + jsonNode["closetime"];
            //    }
            //    error_popup.SetActive(true);
            //}
            //else
            {
                error_popup.SetActive(true);
                error_string.text = "영업일자가 " + string.Format("{0:D4}-{1:D2}-{2:D2}", ctime.Year, ctime.Month, ctime.Day) + " 로 변경되었습니다.";
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

    public void onBack()
    {
        StartCoroutine(GotoScene("main"));
    }

    public void OutTest()
    {
        //결제단말기 출력테스트
        string printString = DocumentFactory.MakePrintData();
        Debug.Log(printString);
        byte[] sendData = NetUtils.StrToBytes(printString);
        Socket_Send(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), sendData);
    }

    public void OutOrderTest()
    {
        //주방프린터 출력테스트
        string printStr = DocumentFactory.MakeOrderPrintData();
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

    IEnumerator ChangeMenuListProcess(WWW www, string category_id, List<MenuInfo> tmpMenuList)
    {
        yield return www;
        if (www.error == null)
        {
            int index = -1;
            for(int i = 0; i < Global.categorylist.Count; i++)
            {
                if(Global.categorylist[i].id == category_id)
                {
                    index = i;
                    break;
                }
            }
            try
            {
                CategoryInfo cinfo = Global.categorylist[index];
                cinfo.menulist = tmpMenuList;
                Global.categorylist[index] = cinfo;
            }catch(Exception ex)
            {

            }
        }
        else
        {
            error_string.text = "서버와의 접속이 원활하지 않습니다.";
            error_popup.SetActive(true);
        }
    }

    IEnumerator ChangeCategoryListProcess(WWW www, List<CategoryInfo> tmpCateList)
    {
        yield return www;
        if (www.error == null)
        {
            Global.categorylist = tmpCateList;
        }
        else
        {
            error_string.text = "서버와의 접속이 원활하지 않습니다.";
            error_popup.SetActive(true);
        }
    }

    IEnumerator ChangeTableGroupInfoProcess(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            JSONNode tg_list = JSON.Parse(jsonNode["tablegrouplist"].ToString()/*.Replace("\"", "")*/);
            List<TableGroup> tmp = new List<TableGroup>();
            for (int i = 0; i < tg_list.Count; i++)
            {
                Debug.Log("loading table group list..");
                TableGroup tgInfo = new TableGroup();
                try
                {
                    tgInfo.id = tg_list[i]["id"];
                    tgInfo.name = tg_list[i]["name"];
                    tgInfo.order = tg_list[i]["order"].AsInt;
                    tgInfo.tbCnt = tg_list[i]["tbCnt"].AsInt;
                    tgInfo.tablelist = new List<TableInfo>();
                }
                catch (Exception ex)
                {
                    Debug.Log("exception");
                }
                JSONNode table_list = JSON.Parse(tg_list[i]["tablelist"].ToString());
                int tableCnt = table_list.Count;
                for (int j = 0; j < tableCnt; j++)
                {
                    TableInfo tinfo = new TableInfo();
                    try
                    {
                        tinfo.id = table_list[j]["id"];
                        tinfo.name = table_list[j]["name"];
                        tinfo.order_price = table_list[j]["order_price"].AsInt;
                        tinfo.order_amount = table_list[j]["order_amount"];
                        tinfo.order = table_list[j]["order"].AsInt;
                        tinfo.taglist = new List<TagInfo>();
                        JSONNode tag_list = JSON.Parse(table_list[j]["taglist"].ToString());
                        int tagCnt = tag_list.Count;
                        for (int k = 0; k < tagCnt; k++)
                        {
                            TagInfo taginfo = new TagInfo();
                            taginfo.id = tag_list[k]["id"];
                            taginfo.name = tag_list[k]["name"];
                            taginfo.status = tag_list[k]["status"].AsInt;
                            taginfo.is_pay_after = tag_list[k]["is_pay_after"].AsInt;
                            tinfo.taglist.Add(taginfo);
                        }

                        tinfo.is_blank = table_list[j]["is_blank"];
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex);
                    }
                    tgInfo.tablelist.Add(tinfo);
                }
                tmp.Add(tgInfo);
            }
            Global.tableGroupList = tmp;
        }
        else
        {
            error_string.text = "서버와의 접속이 원활하지 않습니다.";
            error_popup.SetActive(true);
        }
    }

    IEnumerator ChangeTableInfoProcess(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            JSONNode tg_list = JSON.Parse(jsonNode["tablegrouplist"].ToString()/*.Replace("\"", "")*/);
            List<TableGroup> tmp = new List<TableGroup>();
            for (int i = 0; i < tg_list.Count; i++)
            {
                Debug.Log("loading table group list..");
                TableGroup tgInfo = new TableGroup();
                try
                {
                    tgInfo.id = tg_list[i]["id"];
                    tgInfo.name = tg_list[i]["name"];
                    tgInfo.order = tg_list[i]["order"].AsInt;
                    tgInfo.tbCnt = tg_list[i]["tbCnt"].AsInt;
                    tgInfo.tablelist = new List<TableInfo>();
                }
                catch (Exception ex)
                {
                    Debug.Log("exception");
                }
                JSONNode table_list = JSON.Parse(tg_list[i]["tablelist"].ToString());
                int tableCnt = table_list.Count;
                for (int j = 0; j < tableCnt; j++)
                {
                    TableInfo tinfo = new TableInfo();
                    try
                    {
                        tinfo.id = table_list[j]["id"];
                        tinfo.name = table_list[j]["name"];
                        tinfo.order_price = table_list[j]["order_price"].AsInt;
                        tinfo.order_amount = table_list[j]["order_amount"];
                        tinfo.order = table_list[j]["order"].AsInt;
                        tinfo.taglist = new List<TagInfo>();
                        JSONNode tag_list = JSON.Parse(table_list[j]["taglist"].ToString());
                        int tagCnt = tag_list.Count;
                        for (int k = 0; k < tagCnt; k++)
                        {
                            TagInfo taginfo = new TagInfo();
                            taginfo.id = tag_list[k]["id"];
                            taginfo.name = tag_list[k]["name"];
                            taginfo.status = tag_list[k]["status"].AsInt;
                            taginfo.is_pay_after = tag_list[k]["is_pay_after"].AsInt;
                            tinfo.taglist.Add(taginfo);
                        }
                        tinfo.is_blank = table_list[j]["is_blank"];
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex);
                    }
                    tgInfo.tablelist.Add(tinfo);
                }
                tmp.Add(tgInfo);
            }
            Global.tableGroupList = tmp;
        }
        else
        {
            error_string.text = "서버와의 접속이 원활하지 않습니다.";
            error_popup.SetActive(true);
        }
    }

    void SendRequestForChangeSellSizeList(string reqInfo, string category_id, List<MenuInfo> tmpMenuList)
    {
        WWWForm form = new WWWForm();
        Debug.Log(reqInfo);
        form.AddField("info", reqInfo);
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.change_sellsize_api, form);
        StartCoroutine(ChangeSellSizeProcess(www, category_id, tmpMenuList));
    }

    IEnumerator ChangeSellSizeProcess(WWW www, string category_id, List<MenuInfo> tmpMenuList)
    {
        yield return www;
        if (www.error == null)
        {
            int index = -1;
            for (int i = 0; i < Global.categorylist.Count; i++)
            {
                if (Global.categorylist[i].id == category_id)
                {
                    index = i;
                    break;
                }
            }
            try
            {
                CategoryInfo cinfo = Global.categorylist[index];
                cinfo.menulist = tmpMenuList;
                Global.categorylist[index] = cinfo;
            }
            catch (Exception ex)
            {

            }
        }
        else
        {
            error_string.text = "서버와의 접속이 원활하지 않습니다.";
            error_popup.SetActive(true);
        }
    }

    void SendRequestForChangeMenuList(string reqInfo, string category_id, List<MenuInfo> tmpMenuList)
    {
        WWWForm form = new WWWForm();
        Debug.Log(reqInfo);
        form.AddField("info", reqInfo);
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.change_menulist_api, form);
        StartCoroutine(ChangeMenuListProcess(www, category_id, tmpMenuList));
    }

    void SendRequestForChangeMenuOutList(string reqInfo, string category_id, List<MenuInfo> tmpMenuList)
    {
        WWWForm form = new WWWForm();
        Debug.Log(reqInfo);
        form.AddField("info", reqInfo);
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.change_menuoutlist_api, form);
        StartCoroutine(ChangeMenuListProcess(www, category_id, tmpMenuList));
    }

    void SendRequestForChangeTableGroup(string reqInfo)
    {
        WWWForm form = new WWWForm();
        form.AddField("info", reqInfo);
        Debug.Log("request info = " + reqInfo);
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.change_table_group_api, form);
        StartCoroutine(ChangeTableGroupInfoProcess(www));
    }

    void SendRequestForChangeTable(string reqInfo)
    {
        WWWForm form = new WWWForm();
        form.AddField("info", reqInfo);
        Debug.Log("request info = " + reqInfo);
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.change_table_api, form);
        StartCoroutine(ChangeTableInfoProcess(www));
    }

    void SendRequestForChangedCategorylist(string reqInfo, List<CategoryInfo> tmpCateList)
    {
        WWWForm form = new WWWForm();
        form.AddField("info", reqInfo);
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.change_categorylist_api, form);
        StartCoroutine(ChangeCategoryListProcess(www, tmpCateList));
    }

    void SendRequestForRegAdmin(List<Admin> adminlist)
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        string info = "[";
        for(int i = 0; i < adminlist.Count; i++)
        {
            if (i == 0)
            {
                info += "{";
            }
            else
            {
                info += ",{";
            }
            info += "\"id\":\"" + adminlist[i].id + "\","
                + "\"name\":\"" + adminlist[i].name + "\","
                + "\"code\":\"" + adminlist[i].code + "\"}";
        }
        info += "]";
        Debug.Log(info);
        form.AddField("info", info);
        WWW www = new WWW(Global.api_url + Global.reg_admin_api, form);
        StartCoroutine(RegAdminProcess(www, adminlist));
    }

    IEnumerator RegAdminProcess(WWW www, List<Admin> adminlist)
    {
        yield return www;
        if (www.error == null)
        {
            Global.setinfo.admins = adminlist;
        }
        else
        {
            error_string.text = "관리자 등록에 실패하였습니다.";
            error_popup.SetActive(true);
        }
    }

    IEnumerator SetShopStatusProcess(WWW www, int status)
    {
        yield return www;
        if(www.error == null)
        {
            Global.userinfo.pub.is_open = status;
        }
        else
        {
            error_string.text = "매장 설정에 실패하였습니다.";
            error_popup.SetActive(true);
        }
    }

    IEnumerator ChangeCeilTypeProcess(WWW www, int type)
    {
        yield return www;
        if (www.error == null)
        {
            Global.userinfo.pub.ceiltype = type;
        }
        else
        {
            error_string.text = "매장 설정에 실패하였습니다.";
            error_popup.SetActive(true);
        }
    }

    IEnumerator ChangeClosetimeProcess(WWW www, string closetime)
    {
        yield return www;
        if (www.error == null)
        {
            Global.userinfo.pub.closetime = closetime;
            DateTime tp = DateTime.Now.AddDays(-1);
            Global.old_day = new DateTime(tp.Year, tp.Month, tp.Day);
        }
        else
        {
            error_string.text = "매장 설정에 실패하였습니다.";
            error_popup.SetActive(true);
        }
    }
    
    IEnumerator ChangePointInfoProcess(WWW www, int type, float rate)
    {
        yield return www;
        if (www.error == null)
        {
            Global.userinfo.pub.pointer_type = type;
            Global.userinfo.pub.pointer_rate = rate;
        }
        else
        {
            error_string.text = "매장 설정에 실패하였습니다.";
            error_popup.SetActive(true);
        }
    }

    IEnumerator ChangePrepaytagExpiredtimeProcess(WWW www, int period)
    {
        yield return www;
        if (www.error == null)
        {
            Global.userinfo.pub.prepay_tag_period = period;
        }
        else
        {
            error_string.text = "매장 설정에 실패하였습니다.";
            error_popup.SetActive(true);
        }
    }

    IEnumerator ChangeTapInfoProcess(WWW www, int count, int is_self, int selltype)
    {
        yield return www;
        if (www.error == null)
        {
            Global.userinfo.pub.tap_count = count;
            Global.userinfo.pub.is_self = is_self;
            Global.userinfo.pub.sell_type = selltype;
        }
        else
        {
            error_string.text = "매장 설정에 실패하였습니다.";
            error_popup.SetActive(true);
        }
    }

    IEnumerator CheckTableUsage(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                if(jsonNode["result"].AsInt == 1)
                {
                    bool is_check_blankcard = true;
                    for (int i = 0; i < tableGroupParent.transform.childCount; i++)
                    {
                        if (tableGroupParent.transform.GetChild(i).Find("name").GetComponent<InputField>().text.Trim() == ""
                            || tableGroupParent.transform.GetChild(i).Find("count").GetComponent<InputField>().text.Trim() == "")
                        {
                            is_check_blankcard = false;
                            error_string.text = "테이블 그룹 정보를 정확히 입력하세요.";
                            error_popup.SetActive(true);
                            break;
                        }
                    }

                    if (is_check_blankcard)
                    {
                        for (int i = 0; i < tableGroupParent.transform.childCount - 1; i++)
                        {
                            for (int j = i + 1; j < tableGroupParent.transform.childCount; j++)
                            {
                                try
                                {
                                    if (i != j && int.Parse(tableGroupParent.transform.GetChild(i).Find("no").GetComponent<InputField>().text)
                                        == int.Parse(tableGroupParent.transform.GetChild(j).Find("no").GetComponent<InputField>().text))
                                    {
                                        is_check_blankcard = false;
                                        error_string.text = "순서를 정확히 입력하세요.";
                                        error_popup.SetActive(true);
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    is_check_blankcard = false;
                                    error_string.text = "순서를 정확히 입력하세요.";
                                    error_popup.SetActive(true);
                                    break;
                                }
                            }
                            if (!is_check_blankcard) break;
                        }
                    }
                    if (is_check_blankcard)
                    {
                        string changed_tgInfo = "[";
                        for (int i = 0; i < tableGroupParent.transform.childCount; i++)
                        {
                            if (i == 0)
                            {
                                changed_tgInfo += "{";
                            }
                            else
                            {
                                changed_tgInfo += ",{";
                            }

                            TableGroup tg = new TableGroup();
                            tg.order = int.Parse(tableGroupParent.transform.GetChild(i).Find("no").GetComponent<InputField>().text);
                            try
                            {
                                tg.id = tableGroupParent.transform.GetChild(i).Find("id").GetComponent<Text>().text;
                            }
                            catch (Exception ex)
                            {
                                tg.id = "";
                            }
                            changed_tgInfo += "\"tg_id\":\"" + tg.id + "\"";
                            try
                            {
                                tg.tbCnt = int.Parse(tableGroupParent.transform.GetChild(i).Find("count").GetComponent<InputField>().text);
                            }
                            catch (Exception ex)
                            {
                                tg.tbCnt = 0;
                            }
                            changed_tgInfo += ",\"tbCnt\":\"" + tg.tbCnt + "\"";
                            tg.name = tableGroupParent.transform.GetChild(i).Find("name").GetComponent<InputField>().text;
                            changed_tgInfo += ",\"tg_name\":\"" + tg.name + "\"";
                            changed_tgInfo += ",\"order\":\"" + tg.order + "\"}";
                        }
                        changed_tgInfo += "]";
                        SendRequestForChangeTableGroup(changed_tgInfo);
                        tablegroupSetPopup.SetActive(false);
                    }
                } 
                else
                {
                    error_string.text = "사용 중인 테이블이 있어 수정이 불가합니다.";
                    error_popup.SetActive(true);
                }
            }
            else
            {
                error_string.text = "테이블 그룹정보 변경시 알지 못할 오류가 발생하였습니다.";
                error_popup.SetActive(true);
            }
        }
        else
        {
            error_string.text = "테이블 그룹정보 변경시 알지 못할 오류가 발생하였습니다.";
            error_popup.SetActive(true);
        }
    }

    public void Save()
    {
        Debug.Log("popup type = " + popup_type);
        switch (popup_type)
        {
            case 1:
                {
                    if (posSetPopup.transform.Find("background/val").GetComponent<InputField>().text.Trim() == "")
                    {
                        error_string.text = "POS 번호를 입력하세요.";
                        error_popup.SetActive(true);
                    }
                    else
                    {
                        try
                        {
                            Global.setinfo.pos_no = int.Parse(posSetPopup.transform.Find("background/val").GetComponent<InputField>().text);
                            PlayerPrefs.SetInt("posNo", Global.setinfo.pos_no);
                            posSetPopup.SetActive(false);
                        } catch(Exception ex)
                        {

                        }
                    }
                    break;
                }
            case 2:
                {
                    WWWForm form = new WWWForm();
                    form.AddField("pub_id", Global.userinfo.pub.id);
                    WWW www = new WWW(Global.api_url + Global.check_tableusage_api, form);
                    StartCoroutine(CheckTableUsage(www));
                    break;
                }
            case 3:
                {
                    bool is_check_blankcard = true;
                    if(selected_tablegroup_index == -1)
                    {
                        return;
                    }
                    for (int i = 0; i < Global.tableGroupList[selected_tablegroup_index].tbCnt; i++)
                    {
                        if (tableParent.transform.GetChild(i).Find("tablename").GetComponent<InputField>().text.Trim() == "")
                        {
                            is_check_blankcard = false;
                            error_string.text = "테이블명을 정확히 입력하세요.";
                            error_popup.SetActive(true);
                            break;
                        }
                    }
                    //if (is_check_blankcard)
                    //{
                    //    for (int i = 0; i < tableParent.transform.childCount - 1; i++)
                    //    {
                    //        for (int j = i + 1; j < tableParent.transform.childCount; j++)
                    //        {
                    //            try
                    //            {
                    //                if (i != j && int.Parse(tableParent.transform.GetChild(i).Find("order").GetComponent<InputField>().text)
                    //                    == int.Parse(tableParent.transform.GetChild(j).Find("order").GetComponent<InputField>().text))
                    //                {
                    //                    is_check_blankcard = false;
                    //                    error_string.text = "순서를 정확히 입력하세요.";
                    //                    error_popup.SetActive(true);
                    //                    break;
                    //                }
                    //            }
                    //            catch (Exception ex)
                    //            {
                    //                is_check_blankcard = false;
                    //                error_string.text = "순서를 정확히 입력하세요.";
                    //                error_popup.SetActive(true);
                    //                break;
                    //            }
                    //        }
                    //        if (!is_check_blankcard) break;
                    //    }
                    //}
                    if (is_check_blankcard)
                    {
                        string changed_tInfo = "[";
                        for (int i = 0; i < Global.tableGroupList[selected_tablegroup_index].tbCnt; i++)
                        {
                            if (i == 0)
                            {
                                changed_tInfo += "{";
                            }
                            else
                            {
                                changed_tInfo += ",{";
                            }

                            TableInfo t = new TableInfo();
                            t.order = int.Parse(tableParent.transform.GetChild(i).Find("order").GetComponent<InputField>().text);
                            try
                            {
                                t.id = tableParent.transform.GetChild(i).Find("id").GetComponent<Text>().text;
                            }
                            catch (Exception ex)
                            {
                                t.id = "";
                            }
                            t.name = tableParent.transform.GetChild(i).Find("tablename").GetComponent<InputField>().text;
                            changed_tInfo += "\"tid\":\"" + t.id + "\"";
                            changed_tInfo += ",\"tname\":\"" + t.name + "\"";
                            changed_tInfo += ",\"order\":\"" + t.order + "\"}";
                        }
                        changed_tInfo += "]";
                        SendRequestForChangeTable(changed_tInfo);
                        selected_tablegroup_index = -1;
                        tableSetPopup.SetActive(false);
                    }
                    break;
                }
            case 4:
                {
                    if (tapSetPopup.transform.Find("background/count").GetComponent<InputField>().text.Trim() == "")
                    {
                        error_string.text = "TAP 개수를 입력하세요.";
                        error_popup.SetActive(true);
                    }
                    else
                    {
                        int count = int.Parse(tapSetPopup.transform.Find("background/count").GetComponent<InputField>().text);
                        int is_self = 0;
                        if (tapSetPopup.transform.Find("background/self").GetComponent<Toggle>().isOn)
                        {
                            is_self = 1;
                        }
                        int selltype = 0;
                        if (tapSetPopup.transform.Find("background/selltype/ml").GetComponent<Toggle>().isOn)
                        {
                            selltype = 1;
                        }
                        WWWForm form = new WWWForm();
                        form.AddField("tap_cnt", count);
                        form.AddField("pub_id", Global.userinfo.pub.id);
                        form.AddField("is_self", is_self);
                        form.AddField("sell_type", selltype);
                        WWW www = new WWW(Global.api_url + Global.change_tapinfo_api, form);
                        StartCoroutine(ChangeTapInfoProcess(www, count, is_self, selltype));
                        tapSetPopup.SetActive(false);
                    }
                    break;
                }
            case 5:
                {
                    if (paymentSetPopup.transform.Find("background/ip").GetComponent<InputField>().text.Trim() == "")
                    {
                        error_string.text = "IP를 입력하세요.";
                        error_popup.SetActive(true);
                    }else if (paymentSetPopup.transform.Find("background/port").GetComponent<InputField>().text.Trim() == "")
                    {
                        error_string.text = "Port를 입력하세요.";
                        error_popup.SetActive(true);
                    }
                    else if (paymentSetPopup.transform.Find("background/baudrate").GetComponent<InputField>().text.Trim() == "")
                    {
                        error_string.text = "Baudrate를 입력하세요.";
                        error_popup.SetActive(true);
                    }
                    else if (paymentSetPopup.transform.Find("background/cat/Label").GetComponent<Text>().text.Trim() == "")
                    {
                        error_string.text = "CAT를 입력하세요.";
                        error_popup.SetActive(true);
                    }
                    else if (paymentSetPopup.transform.Find("background/linecount").GetComponent<InputField>().text.Trim() == "")
                    {
                        error_string.text = "Line count를 입력하세요.";
                        error_popup.SetActive(true);
                    }
                    else
                    {
                        try
                        {
                            Global.setinfo.paymentDeviceInfo = new PaymentDeviceInfo();
                            Global.setinfo.paymentDeviceInfo.ip = paymentSetPopup.transform.Find("background/ip").GetComponent<InputField>().text.Trim();
                            Global.setinfo.paymentDeviceInfo.port = int.Parse(paymentSetPopup.transform.Find("background/port").GetComponent<InputField>().text.Trim());
                            Global.setinfo.paymentDeviceInfo.baudrate = float.Parse(paymentSetPopup.transform.Find("background/baudrate").GetComponent<InputField>().text.Trim());
                            Global.setinfo.paymentDeviceInfo.cat = paymentSetPopup.transform.Find("background/cat").GetComponent<Dropdown>().value;
                            Global.setinfo.paymentDeviceInfo.line_count = int.Parse(paymentSetPopup.transform.Find("background/linecount").GetComponent<InputField>().text.Trim());
                            if (paymentSetPopup.transform.Find("background/type/ip").GetComponent<Toggle>().isOn)
                            {
                                Global.setinfo.paymentDeviceInfo.type = 1;
                            }
                            else
                            {
                                Global.setinfo.paymentDeviceInfo.type = 0;
                            }
                            PlayerPrefs.SetString("payDeviceIp", Global.setinfo.paymentDeviceInfo.ip);
                            PlayerPrefs.SetInt("payDevicePort", Global.setinfo.paymentDeviceInfo.port);
                            PlayerPrefs.SetFloat("payDeviceBaudrate", Global.setinfo.paymentDeviceInfo.baudrate);
                            PlayerPrefs.SetInt("payDeviceCat", Global.setinfo.paymentDeviceInfo.cat);
                            PlayerPrefs.SetInt("payDeviceLinecount", Global.setinfo.paymentDeviceInfo.line_count);
                            PlayerPrefs.SetInt("payDeviceType", Global.setinfo.paymentDeviceInfo.type);
                        }
                        catch (Exception ex)
                        {
                            Debug.Log(ex);
                        }
                        paymentSetPopup.SetActive(false);
                    }
                    break;
                }
            case 6:
                {
                    if (printerSetPopup.transform.Find("background/01/name").GetComponent<InputField>().text.Trim() == ""
                        || printerSetPopup.transform.Find("background/02/name").GetComponent<InputField>().text.Trim() == ""
                        || printerSetPopup.transform.Find("background/03/name").GetComponent<InputField>().text.Trim() == ""
                        || printerSetPopup.transform.Find("background/04/name").GetComponent<InputField>().text.Trim() == "")
                    {
                        error_string.text = "프린터명을 입력하세요.";
                        error_popup.SetActive(true);
                    }
                    //else if (printerSetPopup.transform.Find("background/01/port").GetComponent<InputField>().text.Trim() == ""
                    //    || printerSetPopup.transform.Find("background/02/port").GetComponent<InputField>().text.Trim() == ""
                    //    || printerSetPopup.transform.Find("background/03/port").GetComponent<InputField>().text.Trim() == ""
                    //    || printerSetPopup.transform.Find("background/04/port").GetComponent<InputField>().text.Trim() == "")
                    //{
                    //    error_string.text = "Port를 입력하세요.";
                    //    error_popup.SetActive(true);
                    //}
                    //else if (printerSetPopup.transform.Find("background/01/ip").GetComponent<InputField>().text.Trim() == ""
                    //    || printerSetPopup.transform.Find("background/02/ip").GetComponent<InputField>().text.Trim() == ""
                    //    || printerSetPopup.transform.Find("background/03/ip").GetComponent<InputField>().text.Trim() == ""
                    //    || printerSetPopup.transform.Find("background/04/ip").GetComponent<InputField>().text.Trim() == "")
                    //{
                    //    error_string.text = "IP/Baudrate를 입력하세요.";
                    //    error_popup.SetActive(true);
                    //}
                    else
                    {
                        Global.setinfo.printerSet = new PrinterSet();
                        Global.setinfo.printerSet.printer1 = new PrinterInfo();
                        Global.setinfo.printerSet.printer1.name = printerSetPopup.transform.Find("background/01/name").GetComponent<InputField>().text.Trim();
                        Global.setinfo.printerSet.printer1.useset = printerSetPopup.transform.Find("background/01/set").GetComponent<Dropdown>().value;
                        Global.setinfo.printerSet.printer1.port = printerSetPopup.transform.Find("background/01/port").GetComponent<InputField>().text.Trim();
                        Global.setinfo.printerSet.printer1.ip_baudrate = printerSetPopup.transform.Find("background/01/ip").GetComponent<InputField>().text.Trim();
                        Global.setinfo.printerSet.printer2 = new PrinterInfo();
                        Global.setinfo.printerSet.printer2.name = printerSetPopup.transform.Find("background/02/name").GetComponent<InputField>().text.Trim();
                        Global.setinfo.printerSet.printer2.useset = printerSetPopup.transform.Find("background/02/set").GetComponent<Dropdown>().value;
                        Global.setinfo.printerSet.printer2.port = printerSetPopup.transform.Find("background/02/port").GetComponent<InputField>().text.Trim();
                        Global.setinfo.printerSet.printer2.ip_baudrate = printerSetPopup.transform.Find("background/02/ip").GetComponent<InputField>().text.Trim();
                        Global.setinfo.printerSet.printer3 = new PrinterInfo();
                        Global.setinfo.printerSet.printer3.name = printerSetPopup.transform.Find("background/03/name").GetComponent<InputField>().text.Trim();
                        Global.setinfo.printerSet.printer3.useset = printerSetPopup.transform.Find("background/03/set").GetComponent<Dropdown>().value;
                        Global.setinfo.printerSet.printer3.port = printerSetPopup.transform.Find("background/03/port").GetComponent<InputField>().text.Trim();
                        Global.setinfo.printerSet.printer3.ip_baudrate = printerSetPopup.transform.Find("background/03/ip").GetComponent<InputField>().text.Trim();
                        Global.setinfo.printerSet.printer4 = new PrinterInfo();
                        Global.setinfo.printerSet.printer4.name = printerSetPopup.transform.Find("background/04/name").GetComponent<InputField>().text.Trim();
                        Global.setinfo.printerSet.printer4.useset = printerSetPopup.transform.Find("background/04/set").GetComponent<Dropdown>().value;
                        Global.setinfo.printerSet.printer4.port = printerSetPopup.transform.Find("background/04/port").GetComponent<InputField>().text.Trim();
                        Global.setinfo.printerSet.printer4.ip_baudrate = printerSetPopup.transform.Find("background/04/ip").GetComponent<InputField>().text.Trim();
                        Global.setinfo.printerSet.menu_output = printerSetPopup.transform.Find("background/output").GetComponent<Dropdown>().value;
                        PlayerPrefs.SetString("printer1name", Global.setinfo.printerSet.printer1.name);
                        PlayerPrefs.SetString("printer2name", Global.setinfo.printerSet.printer2.name);
                        PlayerPrefs.SetString("printer3name", Global.setinfo.printerSet.printer3.name);
                        PlayerPrefs.SetString("printer4name", Global.setinfo.printerSet.printer4.name);
                        PlayerPrefs.SetInt("printer1useset", Global.setinfo.printerSet.printer1.useset);
                        PlayerPrefs.SetInt("printer2useset", Global.setinfo.printerSet.printer2.useset);
                        PlayerPrefs.SetInt("printer3useset", Global.setinfo.printerSet.printer3.useset);
                        PlayerPrefs.SetInt("printer4useset", Global.setinfo.printerSet.printer4.useset);
                        PlayerPrefs.SetString("printer1port", Global.setinfo.printerSet.printer1.port);
                        PlayerPrefs.SetString("printer2port", Global.setinfo.printerSet.printer2.port);
                        PlayerPrefs.SetString("printer3port", Global.setinfo.printerSet.printer3.port);
                        PlayerPrefs.SetString("printer4port", Global.setinfo.printerSet.printer4.port);
                        PlayerPrefs.SetString("printer1ip", Global.setinfo.printerSet.printer1.ip_baudrate);
                        PlayerPrefs.SetString("printer2ip", Global.setinfo.printerSet.printer2.ip_baudrate);
                        PlayerPrefs.SetString("printer3ip", Global.setinfo.printerSet.printer3.ip_baudrate);
                        PlayerPrefs.SetString("printer4ip", Global.setinfo.printerSet.printer4.ip_baudrate);
                        PlayerPrefs.SetInt("printerMenuOutput", Global.setinfo.printerSet.menu_output);
                        printerSetPopup.SetActive(false);
                    }
                    break;
                }
            case 7:
                {
                    break;
                }
            case 8:
                {
                    bool is_check_blankcard = true;
                    List<Admin> tmpAdmin = new List<Admin>();
                    for (int i = 0; i < adminParent.transform.childCount; i++)
                    {
                        if(adminParent.transform.GetChild(i).Find("name").GetComponent<InputField>().text.Trim() == ""
                            || adminParent.transform.GetChild(i).Find("code").GetComponent<InputField>().text.Trim() == "")
                        {
                            is_check_blankcard = false;
                            error_string.text = "관리자정보를 정확히 입력하세요.";
                            error_popup.SetActive(true);
                            break;
                        }
                    }
                    if (is_check_blankcard)
                    {
                        Debug.Log(adminParent.transform.childCount);
                        for (int i = 0; i < adminParent.transform.childCount; i++)
                        {
                            try
                            {
                                Admin ad = new Admin();
                                ad.code = adminParent.transform.GetChild(i).Find("code").GetComponent<InputField>().text.Trim();
                                ad.name = adminParent.transform.GetChild(i).Find("name").GetComponent<InputField>().text.Trim();
                                ad.id = adminParent.transform.GetChild(i).Find("id").GetComponent<Text>().text.Trim();
                                Debug.Log(ad.id + " added.");
                                tmpAdmin.Add(ad);
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                        SendRequestForRegAdmin(tmpAdmin);
                    }
                    regAdminPopup.SetActive(false);
                    break;
                }
            case 9:
                {
                    int status = 0;
                    if (shopSetPopup.transform.Find("background/type/open").GetComponent<Toggle>().isOn)
                    {
                        status = 1;
                    }
                    WWWForm form = new WWWForm();
                    form.AddField("pub_id", Global.userinfo.pub.id);
                    form.AddField("status", status);
                    WWW www = new WWW(Global.api_url + Global.set_shopstatus_api, form);
                    StartCoroutine(SetShopStatusProcess(www, status));
                    shopSetPopup.SetActive(false);
                    break;
                }
            case 10:
                {
                    string changed_cgInfo = "[";
                    List<CategoryInfo> tmpCateList = new List<CategoryInfo>();
                    for (int i = 0; i < categoryParent.transform.childCount; i++)
                    {
                        if (i == 0)
                        {
                            changed_cgInfo += "{";
                        }
                        else
                        {
                            changed_cgInfo += ",{";
                        }
                        CategoryInfo cinfo = Global.categorylist[i];
                        cinfo.engname = categoryParent.transform.GetChild(i).Find("english").GetComponent<InputField>().text.Trim();
                        cinfo.sort_order = int.Parse(categoryParent.transform.GetChild(i).Find("order").GetComponent<InputField>().text.Trim());
                        if (categoryParent.transform.GetChild(i).Find("pos").GetComponent<Toggle>().isOn)
                        {
                            cinfo.is_pos = 1;
                        }
                        else
                        {
                            cinfo.is_pos = 0;
                        }
                        if (categoryParent.transform.GetChild(i).Find("kiosk").GetComponent<Toggle>().isOn)
                        {
                            cinfo.is_kiosk = 1;
                        }
                        else
                        {
                            cinfo.is_kiosk = 0;
                        }
                        if (categoryParent.transform.GetChild(i).Find("tablet").GetComponent<Toggle>().isOn)
                        {
                            cinfo.is_tablet = 1;
                        }
                        else
                        {
                            cinfo.is_tablet = 0;
                        }
                        if (categoryParent.transform.GetChild(i).Find("mobile").GetComponent<Toggle>().isOn)
                        {
                            cinfo.is_mobile = 1;
                        }
                        else
                        {
                            cinfo.is_mobile = 0;
                        }
                        changed_cgInfo += "\"id\":\"" + cinfo.id + "\"," 
                                        + "\"engname\":\"" + cinfo.engname + "\","
                                        + "\"sort_order\":\"" + cinfo.sort_order + "\","
                                        + "\"is_pos\":\"" + cinfo.is_pos + "\","
                                        + "\"is_kiosk\":\"" + cinfo.is_kiosk + "\","
                                        + "\"is_tablet\":\"" + cinfo.is_tablet + "\","
                                        + "\"is_mobile\":\"" + cinfo.is_mobile + "\"}";
                        tmpCateList.Add(cinfo);
                    }
                    changed_cgInfo += "]";
                    SendRequestForChangedCategorylist(changed_cgInfo, tmpCateList);
                    menuSetDisPopup.SetActive(false);
                    break;
                }
            case 11:
                {
                    int index = -1;
                    string category_id = "";
                    if(menuParent.transform.childCount > 0)
                    {
                        for (int j = 0; j < Global.categorylist.Count; j++)
                        {
                            try
                            {
                                if (Global.categorylist[j].id == menuParent.transform.GetChild(0).Find("cateid").GetComponent<Text>().text.Trim())
                                {
                                    category_id = Global.categorylist[j].id;
                                    index = j;break;
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }

                    }
                    if(index != -1)
                    {
                        List<MenuInfo> tmpMenuList = new List<MenuInfo>();
                        string changed_muInfo = "[";
                        for (int i = 0; i < menuParent.transform.childCount; i++)
                        {
                            if (i == 0)
                            {
                                changed_muInfo += "{";
                            }
                            else
                            {
                                changed_muInfo += ",{";
                            }
                            try
                            {
                                MenuInfo minfo = Global.categorylist[index].menulist[i];
                                minfo.engname = menuParent.transform.GetChild(i).transform.Find("english").GetComponent<InputField>().text.Trim();
                                minfo.contents = menuParent.transform.GetChild(i).transform.Find("contents/Text").GetComponent<Text>().text.Trim();
                                minfo.barcode = menuParent.transform.GetChild(i).transform.Find("barcode").GetComponent<InputField>().text.Trim();
                                minfo.sort_order = int.Parse(menuParent.transform.GetChild(i).transform.Find("order").GetComponent<InputField>().text.Trim());
                                minfo.is_best = Global.GetConvertedPrice(menuParent.transform.GetChild(i).transform.Find("cost").GetComponent<InputField>().text.Trim());
                                if (menuParent.transform.GetChild(i).transform.Find("soldout").GetComponent<Toggle>().isOn)
                                {
                                    minfo.is_soldout = 1;
                                }
                                else
                                {
                                    minfo.is_soldout = 0;
                                }
                                changed_muInfo += "\"id\":\"" + minfo.id + "\","
                                            + "\"engname\":\"" + minfo.engname + "\","
                                            + "\"contents\":\"" + minfo.contents + "\","
                                            + "\"sort_order\":\"" + minfo.sort_order + "\","
                                            + "\"barcode\":\"" + minfo.barcode + "\","
                                            + "\"pack_price\":\"" + minfo.is_best + "\","
                                            + "\"is_soldout\":\"" + minfo.is_soldout + "\"";
                                tmpMenuList.Add(minfo);
                            }
                            catch (Exception ex)
                            {
                                Debug.Log(ex);
                            }
                            changed_muInfo += "}";
                        }
                        changed_muInfo += "]";
                        Debug.Log(changed_muInfo);
                        SendRequestForChangeMenuList(changed_muInfo, category_id, tmpMenuList);
                    }
                    //menuSetPopup.SetActive(false);
                    break;
                }
            case 12:
                {
                    WWWForm form = new WWWForm();
                    int type = paymentPriceSetPopup.transform.Find("background/val").GetComponent<Dropdown>().value;
                    form.AddField("type", type);
                    form.AddField("pub_id", Global.userinfo.pub.id);
                    WWW www = new WWW(Global.api_url + Global.change_ceiltype_api, form);
                    StartCoroutine(ChangeCeilTypeProcess(www, type));
                    paymentPriceSetPopup.SetActive(false);
                    break;
                }
            case 13:
                {
                    if (CountBasedTimeSetPopup.transform.Find("background/val").GetComponent<InputField>().text.Trim() == "")
                    {
                        error_string.text = "정산기준시간을 입력하세요.";
                        error_popup.SetActive(true);
                    }else if (!CheckBasedTimeFormat(CountBasedTimeSetPopup.transform.Find("background/val").GetComponent<InputField>().text.Trim()))
                    {
                        error_string.text = "정산기준시간 형식이 아닙니다.";
                        error_popup.SetActive(true);
                    }
                    else
                    {
                        WWWForm form = new WWWForm();
                        string closetime = CountBasedTimeSetPopup.transform.Find("background/val").GetComponent<InputField>().text.Trim();
                        form.AddField("closetime", closetime);
                        form.AddField("pub_id", Global.userinfo.pub.id);
                        WWW www = new WWW(Global.api_url + Global.change_closetime_api, form);
                        StartCoroutine(ChangeClosetimeProcess(www, closetime));
                        CountBasedTimeSetPopup.SetActive(false);
                    }
                    break;
                }
            case 14:
                {
                    string no = regPrepayPopup.transform.Find("background/val1").GetComponent<InputField>().text.Trim();
                    string name = regPrepayPopup.transform.Find("background/val2").GetComponent<InputField>().text.Trim();
                    int price = 0;
                    try
                    {
                        price = Global.GetConvertedPrice(regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text.Trim());
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex);
                    }
                    bigo = regPrepayPopup.transform.Find("background/val5").GetComponent<InputField>().text.Trim();
                    if(no == "" || name == "" || no == null || name == null)
                    {
                        error_string.text = "정보를 정확히 입력하세요.";
                        error_popup.SetActive(true);
                    }
                    else if (client_id == "" || client_id == null)
                    {
                        error_string.text = "우선 신규고객으로 등록하세요.";
                        error_popup.SetActive(true);
                    }
                    else if (price == 0)
                    {
                        WWWForm form = new WWWForm();
                        form.AddField("client_id", string.Format("", client_id));
                        form.AddField("pub_id", string.Format("", Global.userinfo.pub.id));
                        form.AddField("bigo", string.Format("", bigo));
                        WWW www = new WWW(Global.api_url + Global.change_bigo_api, form);
                        StartCoroutine(ChangeBigoProcess(www));
                    }
                    else
                    {
                        try
                        {
                            device_type = 0;
                            pay_price = price;
                            payPopup.SetActive(true);
                            c_name = name;
                            c_no = no;
                            payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().text = Global.GetPriceFormat(price);
                            payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().readOnly = true;
                            regPrepayPopup.SetActive(false);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    break;
                }
            case 15:
                {
                    int index = -1;
                    string category_id = "";
                    if (menuOutParent.transform.childCount > 0)
                    {
                        for (int j = 0; j < Global.categorylist.Count; j++)
                        {
                            try
                            {
                                if (Global.categorylist[j].id == menuOutParent.transform.GetChild(0).Find("cateid").GetComponent<Text>().text.Trim())
                                {
                                    index = j;
                                    category_id = Global.categorylist[j].id;
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }

                    }
                    if(index != -1)
                    {
                        string changed_muInfo = "[";
                        List<MenuInfo> tmpMenuList = new List<MenuInfo>();
                        for (int i = 0; i < menuOutParent.transform.childCount; i++)
                        {
                            if (i == 0)
                            {
                                changed_muInfo += "{";
                            }
                            else
                            {
                                changed_muInfo += ",{";
                            }
                            try
                            {
                                MenuInfo minfo = Global.categorylist[index].menulist[i];
                                if (menuOutParent.transform.GetChild(i).transform.Find("kit1").GetComponent<Toggle>().isOn)
                                {
                                    minfo.kit1 = 1;
                                }
                                else
                                {
                                    minfo.kit1 = 0;
                                }
                                if (menuOutParent.transform.GetChild(i).transform.Find("kit2").GetComponent<Toggle>().isOn)
                                {
                                    minfo.kit2 = 1;
                                }
                                else
                                {
                                    minfo.kit2 = 0;
                                }
                                if (menuOutParent.transform.GetChild(i).transform.Find("kit3").GetComponent<Toggle>().isOn)
                                {
                                    minfo.kit3 = 1;
                                }
                                else
                                {
                                    minfo.kit3 = 0;
                                }
                                if (menuOutParent.transform.GetChild(i).transform.Find("kit4").GetComponent<Toggle>().isOn)
                                {
                                    minfo.kit4 = 1;
                                }
                                else
                                {
                                    minfo.kit4 = 0;
                                }
                                changed_muInfo += "\"id\":\"" + minfo.id + "\","
                                + "\"kit1\":\"" + minfo.kit1 + "\","
                                + "\"kit2\":\"" + minfo.kit2 + "\","
                                + "\"kit3\":\"" + minfo.kit3 + "\","
                                + "\"kit4\":\"" + minfo.kit4 + "\"";
                                tmpMenuList.Add(minfo);
                                Global.categorylist[index].menulist[i] = minfo;
                            }
                            catch (Exception ex)
                            {

                            }
                            changed_muInfo += "}";
                        }
                        changed_muInfo += "]";
                        SendRequestForChangeMenuOutList(changed_muInfo, category_id, tmpMenuList);
                    }
                    oldSelectedCategoryNo = "";
                    //menuOutputSetPopup.SetActive(false);
                    break;
                }
            case 16:
                {
                    Global.userinfo.pub.invoice_outtype = invoiceOutputSetPopup.transform.Find("background/val").GetComponent<Dropdown>().value;
                    PlayerPrefs.SetInt("invoice_outtype", Global.userinfo.pub.invoice_outtype);
                    invoiceOutputSetPopup.SetActive(false);
                    break;
                }
            case 17:
                {
                    if (pointerSetPopup.transform.Find("background/val").GetComponent<InputField>().text.Trim() == "")
                    {
                        error_string.text = "포인트 적립율을 입력하세요.";
                    }
                    else
                    {
                        int type = 0;//매장
                        if (pointerSetPopup.transform.Find("background/type/1").GetComponent<Toggle>().isOn)
                        {
                            type = 1;//통합
                        }
                        WWWForm form = new WWWForm();
                        form.AddField("type", type);
                        float rate = float.Parse(pointerSetPopup.transform.Find("background/val").GetComponent<InputField>().text.Trim());
                        form.AddField("rate", rate.ToString());
                        form.AddField("pub_id", Global.userinfo.pub.id);
                        WWW www = new WWW(Global.api_url + Global.change_pointinfo_api, form);
                        StartCoroutine(ChangePointInfoProcess(www, type, rate));
                        pointerSetPopup.SetActive(false);
                    }
                    break;
                }
            case 18:
                {
                    if (prepayTagBasedTimeSetPopup.transform.Find("background/val").GetComponent<InputField>().text.Trim() == "")
                    {
                        error_string.text = "선불TAG 유효기간을 입력하세요.";
                    }
                    else
                    {
                        WWWForm form = new WWWForm();
                        int period = int.Parse(prepayTagBasedTimeSetPopup.transform.Find("background/val").GetComponent<InputField>().text.Trim());
                        form.AddField("period", period);
                        form.AddField("pub_id", Global.userinfo.pub.id);
                        WWW www = new WWW(Global.api_url + Global.change_pertagexpiredtime_api, form);
                        StartCoroutine(ChangePrepaytagExpiredtimeProcess(www, period));
                        prepayTagBasedTimeSetPopup.SetActive(false);
                    }
                    break;
                }
            case 19:
                {
                    Global.userinfo.pub.move_table_type = moveTableoutPopup.transform.Find("background/val").GetComponent<Dropdown>().value;
                    PlayerPrefs.SetInt("move_table_type", Global.userinfo.pub.move_table_type);
                    moveTableoutPopup.SetActive(false);
                    break;
                }
            case 20:
                {
                    if (tableMainPopup.transform.Find("background/type/1").GetComponent<Toggle>().isOn)
                    {
                        Global.setinfo.tableMain = 1;//이용내역
                    }
                    else
                    {
                        Global.setinfo.tableMain = 0;//메뉴주문
                    }
                    PlayerPrefs.SetInt("tableMain", Global.setinfo.tableMain);
                    tableMainPopup.SetActive(false);
                    break;
                }
            case 21:
                {
                    checkSetPopup.transform.Find("background/input").GetComponent<InputField>().text = "";
                    checkSetPopup.SetActive(false);
                    break;
                }
            case 26:
                {
                    //결제대기시간
                    if (paymentTimeSetPopup.transform.Find("background/val").GetComponent<InputField>().text.Trim() == "")
                    {
                        error_string.text = "결제 대기시간을 입력하세요.";
                    }
                    else
                    {
                        try
                        {
                            int payment_time = int.Parse(paymentTimeSetPopup.transform.Find("background/val").GetComponent<InputField>().text.Trim());
                            Global.setinfo.payment_time = payment_time;
                            PlayerPrefs.SetInt("payment_time", payment_time);
                        }
                        catch (Exception ex)
                        {
                            Debug.Log(ex);
                        }
                        paymentTimeSetPopup.SetActive(false);
                    }
                    break;
                }
            case 27:
                {
                    int index = -1;
                    string category_id = "";
                    if (sellSizeParent.transform.childCount > 0)
                    {
                        for (int j = 0; j < Global.categorylist.Count; j++)
                        {
                            try
                            {
                                if (Global.categorylist[j].id == sellSizeParent.transform.GetChild(0).Find("cateid").GetComponent<Text>().text.Trim())
                                {
                                    category_id = Global.categorylist[j].id;
                                    index = j; break;
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }

                    }
                    if (index != -1)
                    {
                        List<MenuInfo> tmpMenuList = new List<MenuInfo>();
                        string changed_muInfo = "[";
                        for (int i = 0; i < sellSizeParent.transform.childCount; i++)
                        {
                            if (i == 0)
                            {
                                changed_muInfo += "{";
                            }
                            else
                            {
                                changed_muInfo += ",{";
                            }
                            try
                            {
                                MenuInfo minfo = Global.categorylist[index].menulist[i];
                                minfo.sell_tap = sellSizeParent.transform.GetChild(i).transform.Find("beer").GetComponent<Dropdown>().value;
                                Debug.Log(minfo.sell_tap);
                                minfo.sell_amount = int.Parse(sellSizeParent.transform.GetChild(i).transform.Find("size").GetComponent<InputField>().text.Trim());
                                changed_muInfo += "\"id\":\"" + minfo.id + "\","
                                            + "\"sell_tap\":\"" + minfo.sell_tap + "\","
                                            + "\"sell_amount\":\"" + minfo.sell_amount + "\"";
                                tmpMenuList.Add(minfo);
                            }
                            catch (Exception ex)
                            {
                                Debug.Log(ex);
                            }
                            changed_muInfo += "}";
                        }
                        changed_muInfo += "]";
                        Debug.Log(changed_muInfo);
                        SendRequestForChangeSellSizeList(changed_muInfo, category_id, tmpMenuList);
                    }
                    break;
                }
        }
    }

    IEnumerator ChangeBigoProcess(WWW www)
    {
        yield return www;
        regPrepayPopup.SetActive(false);
        if(www.error == null)
        {

        }
        else
        {
            error_popup.SetActive(true);
            error_string.text = "서버와의 접속이 원활하지 않습니다.\n잠시후에 다시 시도해주세요.";
        }
    }

    bool CheckBasedTimeFormat(string str)
    {
        try
        {
            string[] tp = str.Split(':');
            int h = int.Parse(tp[0]);
            int m = int.Parse(tp[1]);
            return true;
        }catch(Exception ex)
        {

        }
        return false;
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
        form.AddField("preTagType", preTagType + "");
        form.AddField("pretag_id", pretag_id + "");
        form.AddField("price", pay_price + "");
        form.AddField("point", used_point + "");
        form.AddField("prepay", used_prepay + "");
        form.AddField("total_price", (pay_price + used_point + used_prepay) + "");
        form.AddField("client_id", client_id + "");
        form.AddField("credit_card_company", credit_card_company + "");
        form.AddField("credit_card_number", credit_card_number + "");
        form.AddField("device_type", device_type + "");
        form.AddField("installment_months", installment_months + "");
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
        form.AddField("invoice_type", output_type + "");
        form.AddField("pub_id", Global.userinfo.pub.id + "");
        form.AddField("table_id", Global.cur_tInfo.tid + "");
        if (payPopup.transform.Find("background/1/sel").GetComponent<Text>().text == "카드결제")
        {
            form.AddField("pay_type", 1 + "");//카드결제
            form.AddField("app_no", app_no + "");//식별번호
        }
        else
        {
            form.AddField("pay_type", 0 + "");//현금결제
            if (app_no == "" || app_no == null)
            {
                form.AddField("app_no", payPopup.transform.Find("background/1/no").GetComponent<InputField>().text.Trim());
                }
                else
                {
                form.AddField("app_no", app_no + "");//식별번호
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
        if (popup_type == 14)
        {
            //예치금 등록
            form.AddField("type", 2 + "");
            form.AddField("no", c_no + "");
        }
        else if (popup_type == 4)
        {
            //일반 주문 결제
            form.AddField("type", 0 + "");
        }
        form.AddField("bigo", bigo + "");
        form.AddField("client_name", c_name + "");
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        form.AddField("pos_no", Global.setinfo.pos_no + "");
        WWW www = new WWW(Global.api_url + Global.pay_api, form);
        StartCoroutine(payProcess(www));
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
                    error_popup.SetActive(true);
                    error_string.text = "결제금액을 확인하세요.";
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
                        error_string.text = "결제단말기 세팅을 진행하세요.";
                        payPopup.SetActive(false);
                        error_popup.SetActive(true);
                        return;
                    }
                    else if (Global.setinfo.paymentDeviceInfo.type == 0)
                    {
                        //serial
                        error_string.text = "결제단말기 통신방식을 ip로 설정하세요.";
                        error_popup.SetActive(true);
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
                error_popup.SetActive(true);
                error_string.text = "결제금액을 확인하세요.";
            }
            if (payPopup.transform.Find("background/1/pre/prepay").GetComponent<Toggle>().isOn) //임의결제처리
            {
                payFunc();
            }
            else //결제단말기이용
            {
                if (Global.setinfo.paymentDeviceInfo.port == 0 || Global.setinfo.paymentDeviceInfo.ip == "" || Global.setinfo.paymentDeviceInfo.ip == null)
                {
                    error_string.text = "결제단말기 세팅을 진행하세요.";
                    error_popup.SetActive(true);
                    payPopup.SetActive(false);
                    return;
                }
                else if (Global.setinfo.paymentDeviceInfo.type == 0)
                {
                    //serial
                    error_string.text = "결제단말기 통신방식을 ip로 설정하세요.";
                    error_popup.SetActive(true);
                    payPopup.SetActive(false);
                    return;
                }
                else
                {
                    //카드승인
                    string noTxt = payPopup.transform.Find("background/1/no").GetComponent<InputField>().text.Trim();
                    string halbu = "00";
                    if (noTxt != "" && noTxt.Length != 2)
                    {
                        error_string.text = "할부개월을 정확히 입력하세요.";
                        error_popup.SetActive(true);
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
            //Console.WriteLine(DateTime.Now.ToString("mm:ss:fff") + " Waiting Reveive Packet Error....");
            Debug.Log("신용카드 단말기 Nak 수신.");
            progress_popup.SetActive(false);
            StopCoroutine("checkPaymentResult");
            error_string.text = "신용카드 단말기 Nak 수신..";
            error_popup.SetActive(true);
            socket.Close();
            return;
        }

        if (!AckRcv)
        {
            //Console.WriteLine(DateTime.Now.ToString("mm:ss:fff") + " Waiting Reveive Packet Error....");
            Debug.Log("신용카드 단말기 응답이 없습니다.");
            progress_popup.SetActive(false);
            StopCoroutine("checkPaymentResult");
            error_string.text = "신용카드 단말기 응답이 없습니다.";
            error_popup.SetActive(true);
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
            error_string.text = "신용카드 단말기 사용자 종료.";
            error_popup.SetActive(true);
            socket.Close();
            return;
        }
        if (!StxRcv || !EtxRcv)
        {
            progress_popup.SetActive(false);
            StopCoroutine("checkPaymentResult");
            error_string.text = "신용카드 단말기 응답이 없습니다.";
            error_popup.SetActive(true);
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
                error_string.text = "신용카드 단말기 사용자 종료";
                error_popup.SetActive(true);
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
            error_string.text = "신용카드 단말기 조작시 오류가 발생하였습니다.";
            error_popup.SetActive(true);
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
                error_string.text = "현금영수증 구분 오류 입니다( 01 or 02).";
                error_popup.SetActive(true);
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
                error_string.text = "현금영수증 구분 오류 입니다( 01 or 02).";
                error_popup.SetActive(true);
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
            error_string.text = "신용카드 단말기 응답이 없습니다.";
            error_popup.SetActive(true);
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
            progress_popup.SetActive(false);
            StopCoroutine("checkPaymentResult");
            socket.Close();
            error_string.text = "신용카드 단말기 응답이 없습니다.";
            error_popup.SetActive(true);
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
                error_string.text = "신용카드 단말기 사용자 종료";
                error_popup.SetActive(true);
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
            progress_popup.SetActive(false);
            StopCoroutine("checkPaymentResult");
            error_string.text = "신용카드 단말기 조작시 오류가 발생하였습니다.";
            error_popup.SetActive(true);
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
                error_popup.SetActive(true);
                error_string.text = "보유한 예치금이 결제금액보다 적습니다.";
                return;
            }
            else if (popup2_title.text == "선불카드조회")
            {
                error_popup.SetActive(true);
                error_string.text = "보유한 선불카드금액이 결제금액보다 적습니다.";
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
                    error_string.text = "이용 중인 TAG가 있습니다. 이용 완료 후에 결제를 해주세요.";
                    error_popup.SetActive(true);
                }
            }
            else
            {
                error_string.text = "결제시에 알지 못할 오류가 발생하였습니다.";
                error_popup.SetActive(true);
            }
        }
        else
        {
            error_string.text = "결제시에 알지 못할 오류가 발생하였습니다.";
            error_popup.SetActive(true);
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
                    byte[] sendData = NetUtils.StrToBytes(printStr);
                    Socket_Send(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), sendData);
                }
            }
            catch (Exception ex)
            {

            }

            payPopup.SetActive(false);
        }
    }

    public void closePaypopup()
    {
        payPopup.SetActive(false);
    }

    public void setPosNo()
    {
        posSetPopup.SetActive(true);
        try
        {
            posSetPopup.transform.Find("background/val").GetComponent<InputField>().text = Global.setinfo.pos_no.ToString();
        }catch(Exception ex)
        {

        }
        popup_type = 1;//pos번호 셋팅팝업
    }

    IEnumerator LoadTableGroup()
    {
        m_tableGroupItem.Clear();
        while(tableGroupParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(tableGroupParent.transform.GetChild(0).gameObject));
        }
        while (tableGroupParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        //loading tablegrouplist
        for (int i = 0; i < Global.tableGroupList.Count; i++)
        {
            GameObject tmp_tgObj = Instantiate(tableGroupItem);
            tmp_tgObj.transform.SetParent(tableGroupParent.transform);
            //tmp_tgObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
            //tmp_tgObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
            //float left = 0;
            //float right = 0;
            //tmp_tgObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
            //tmp_tgObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
            //tmp_tgObj.transform.localScale = Vector3.one;

            tmp_tgObj.transform.Find("name").GetComponent<InputField>().text = Global.tableGroupList[i].name;
            tmp_tgObj.transform.Find("id").GetComponent<Text>().text = Global.tableGroupList[i].id.ToString();
            tmp_tgObj.transform.Find("no").GetComponent<InputField>().text = Global.tableGroupList[i].order.ToString();
            tmp_tgObj.transform.Find("count").GetComponent<InputField>().text = Global.tableGroupList[i].tablelist.Count.ToString();
            int _i = i;
            tmp_tgObj.transform.Find("del").GetComponent<Button>().onClick.RemoveAllListeners();
            tmp_tgObj.transform.Find("del").GetComponent<Button>().onClick.AddListener(delegate () { onDelTableGroup(_i); });
            m_tableGroupItem.Add(tmp_tgObj);
        }
    }

    public void AddTableGroup()
    {
        int tgCnt = m_tableGroupItem.Count;
        GameObject tmp_tgObj = Instantiate(tableGroupItem);
        tmp_tgObj.transform.SetParent(tableGroupParent.transform);
        //tmp_tgObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
        //tmp_tgObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
        //float left = 0;
        //float right = 0;
        //tmp_tgObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
        //tmp_tgObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
        //tmp_tgObj.transform.localScale = Vector3.one;
        tmp_tgObj.transform.Find("id").GetComponent<Text>().text = "";
        tmp_tgObj.transform.Find("del").GetComponent<Button>().onClick.RemoveAllListeners();
        tmp_tgObj.transform.Find("del").GetComponent<Button>().onClick.AddListener(delegate () { onDelTableGroup(tgCnt); });
        m_tableGroupItem.Add(tmp_tgObj);
    }

    public void AddAdminItem()
    {
        int adminCnt = m_adminItem.Count;
        GameObject tmp_adminObj = Instantiate(adminItem);
        tmp_adminObj.transform.SetParent(adminParent.transform);
        //tmp_adminObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
        //tmp_adminObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
        //float left = 0;
        //float right = 0;
        //tmp_adminObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
        //tmp_adminObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
        //tmp_adminObj.transform.localScale = Vector3.one;
        tmp_adminObj.transform.Find("del").GetComponent<Button>().onClick.RemoveAllListeners();
        tmp_adminObj.transform.Find("del").GetComponent<Button>().onClick.AddListener(delegate () { onDelAdmin(adminCnt); });
        m_adminItem.Add(tmp_adminObj);
    }

    public void setTableGroup()
    {
        StartCoroutine(LoadTableGroup());
        tablegroupSetPopup.SetActive(true);
        popup_type = 2;//테이블그룹셋팅팝업
    }

    void onDelTableGroup(int index)
    {
        StartCoroutine(Destroy_Object(m_tableGroupItem[index]));
        m_tableGroupItem.Remove(m_tableGroupItem[index]);
        for (int i = 0; i < m_tableGroupItem.Count; i++)
        {
            int _i = i;
            m_tableGroupItem[i].transform.Find("del").GetComponent<Button>().onClick.RemoveAllListeners();
            m_tableGroupItem[i].transform.Find("del").GetComponent<Button>().onClick.AddListener(delegate () { onDelTableGroup(_i); });
        }
    }

    IEnumerator Destroy_Object(GameObject obj)
    {
        DestroyImmediate(obj);
        yield return null;
    }

    IEnumerator LoadTables()
    {
        m_tableItem.Clear();
        while (tableParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(tableParent.transform.GetChild(0).gameObject));
        }
        while (tableParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        //loading tablegrouplist
        if(Global.tableGroupList.Count > 0)
        {
            if(selected_tablegroup_index == -1)
            {
                selected_tablegroup_index = 0;
            }
            int itemCount = Global.tableGroupList.Count > Global.tableGroupList[selected_tablegroup_index].tbCnt ? Global.tableGroupList.Count : Global.tableGroupList[selected_tablegroup_index].tbCnt;
            Debug.Log("Selected tablegroup's tablecount : " + Global.tableGroupList[selected_tablegroup_index].tbCnt);
            Debug.Log("Popup item count : " + itemCount);
            for (int i = 0; i < itemCount; i ++)
            {
                GameObject tmp_Obj = Instantiate(tableItem);
                tmp_Obj.transform.SetParent(tableParent.transform);
                //tmp_Obj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmp_Obj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmp_Obj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmp_Obj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmp_Obj.transform.localScale = Vector3.one;
                if(i < Global.tableGroupList.Count)
                {
                    tmp_Obj.transform.Find("groupname").GetComponent<Text>().text = Global.tableGroupList[i].name;
                    if(i == selected_tablegroup_index)
                    {
                        tmp_Obj.transform.Find("groupname").GetComponent<Text>().color = Global.selected_color;
                    }
                    else
                    {
                        tmp_Obj.transform.Find("groupname").GetComponent<Text>().color = Color.white;
                    }
                    int _i = i;
                    tmp_Obj.transform.Find("tgid").GetComponent<Button>().onClick.RemoveAllListeners();
                    tmp_Obj.transform.Find("tgid").GetComponent<Button>().onClick.AddListener(delegate () { onSelectGroupName(_i); });
                }
                if(i < Global.tableGroupList[selected_tablegroup_index].tbCnt)
                {
                    tmp_Obj.transform.Find("tgid").GetComponent<Text>().text = Global.tableGroupList[selected_tablegroup_index].id.ToString();
                    tmp_Obj.transform.Find("order").GetComponent<InputField>().text = Global.tableGroupList[selected_tablegroup_index].tablelist[i].order.ToString();
                    tmp_Obj.transform.Find("tablename").GetComponent<InputField>().text = Global.tableGroupList[selected_tablegroup_index].tablelist[i].name;
                    tmp_Obj.transform.Find("id").GetComponent<Text>().text = Global.tableGroupList[selected_tablegroup_index].tablelist[i].id.ToString();
                }
                m_tableItem.Add(tmp_Obj);
            }
        }
    }

    void onSelectGroupName(int index)
    {
        Debug.Log("selected : " + index);
        selected_tablegroup_index = index;
        StartCoroutine(LoadTables());
    }

    void onDelAdmin(int index)
    {
        StartCoroutine(Destroy_Object(m_adminItem[index]));
        try
        {
            m_adminItem.Remove(m_adminItem[index]);
            Global.setinfo.admins.Remove(Global.setinfo.admins[index]);
            for (int i = 0; i < m_adminItem.Count; i++)
            {
                int _i = i;
                m_adminItem[i].transform.Find("del").GetComponent<Button>().onClick.RemoveAllListeners();
                m_adminItem[i].transform.Find("del").GetComponent<Button>().onClick.AddListener(delegate () { onDelAdmin(_i); });
            }
        }catch(Exception ex)
        {

        }
    }

    public void setTable()
    {
        StartCoroutine(LoadTables());
        tableSetPopup.SetActive(true);
        popup_type = 3;//테이블셋팅팝업
    }

    public void setTap()
    {
        tapSetPopup.SetActive(true);
        try
        {
            tapSetPopup.transform.Find("background/count").GetComponent<InputField>().text = Global.userinfo.pub.tap_count.ToString();
            if(Global.userinfo.pub.is_self == 1)
            {
                tapSetPopup.transform.Find("background/self").GetComponent<Toggle>().isOn = true;
            }
            else
            {
                tapSetPopup.transform.Find("background/self").GetComponent<Toggle>().isOn = false;
            }
            if(Global.userinfo.pub.sell_type == 1)
            {
                tapSetPopup.transform.Find("background/selltype/ml").GetComponent<Toggle>().isOn = true;
            }
            else
            {
                tapSetPopup.transform.Find("background/selltype/ml").GetComponent<Toggle>().isOn = false;
            }
        }
        catch(Exception ex)
        {

        }
        popup_type = 4;//tap 셋팅팝업
    }

    public void setPaymentDevice()
    {
        paymentSetPopup.SetActive(true);
        try
        {
            paymentSetPopup.transform.Find("background/ip").GetComponent<InputField>().text = Global.setinfo.paymentDeviceInfo.ip;
            paymentSetPopup.transform.Find("background/port").GetComponent<InputField>().text = Global.setinfo.paymentDeviceInfo.port.ToString();
            paymentSetPopup.transform.Find("background/baudrate").GetComponent<InputField>().text = Global.setinfo.paymentDeviceInfo.baudrate.ToString();
            paymentSetPopup.transform.Find("background/cat").GetComponent<Dropdown>().value = Global.setinfo.paymentDeviceInfo.cat;
            paymentSetPopup.transform.Find("background/linecount").GetComponent<InputField>().text = Global.setinfo.paymentDeviceInfo.line_count.ToString();
            if(Global.setinfo.paymentDeviceInfo.type == 1)
            {
                paymentSetPopup.transform.Find("background/type/ip").GetComponent<Toggle>().isOn = true;
            }
            else
            {
                paymentSetPopup.transform.Find("background/type/ip").GetComponent<Toggle>().isOn = false;
            }
        }
        catch(Exception ex)
        {

        }
        popup_type = 5;//결제단말기셋팅팝업
    }

    public void setPrint()
    {
        printerSetPopup.SetActive(true);
        try
        {
            if(Global.setinfo.printerSet.printer1.name == "" || Global.setinfo.printerSet.printer1.name == null)
            {
                printerSetPopup.transform.Find("background/01/name").GetComponent<InputField>().text = "주방1";
            }
            else
            {
                printerSetPopup.transform.Find("background/01/name").GetComponent<InputField>().text = Global.setinfo.printerSet.printer1.name;
            }
            printerSetPopup.transform.Find("background/01/set").GetComponent<Dropdown>().value = Global.setinfo.printerSet.printer1.useset;
            printerSetPopup.transform.Find("background/01/port").GetComponent<InputField>().text = Global.setinfo.printerSet.printer1.port;
            printerSetPopup.transform.Find("background/01/ip").GetComponent<InputField>().text = Global.setinfo.printerSet.printer1.ip_baudrate;
            if (Global.setinfo.printerSet.printer2.name == "" || Global.setinfo.printerSet.printer2.name == null)
            {
                printerSetPopup.transform.Find("background/02/name").GetComponent<InputField>().text = "주방2";
            }
            else
            {
                printerSetPopup.transform.Find("background/02/name").GetComponent<InputField>().text = Global.setinfo.printerSet.printer2.name;
            }
            printerSetPopup.transform.Find("background/02/set").GetComponent<Dropdown>().value = Global.setinfo.printerSet.printer2.useset;
            printerSetPopup.transform.Find("background/02/port").GetComponent<InputField>().text = Global.setinfo.printerSet.printer2.port;
            printerSetPopup.transform.Find("background/02/ip").GetComponent<InputField>().text = Global.setinfo.printerSet.printer2.ip_baudrate;
            if (Global.setinfo.printerSet.printer3.name == "" || Global.setinfo.printerSet.printer3.name == null)
            {
                printerSetPopup.transform.Find("background/03/name").GetComponent<InputField>().text = "주방3";
            }
            else
            {
                printerSetPopup.transform.Find("background/03/name").GetComponent<InputField>().text = Global.setinfo.printerSet.printer3.name;
            }
            printerSetPopup.transform.Find("background/03/set").GetComponent<Dropdown>().value = Global.setinfo.printerSet.printer3.useset;
            printerSetPopup.transform.Find("background/03/port").GetComponent<InputField>().text = Global.setinfo.printerSet.printer3.port;
            printerSetPopup.transform.Find("background/03/ip").GetComponent<InputField>().text = Global.setinfo.printerSet.printer3.ip_baudrate;
            if (Global.setinfo.printerSet.printer4.name == "" || Global.setinfo.printerSet.printer4.name == null)
            {
                printerSetPopup.transform.Find("background/04/name").GetComponent<InputField>().text = "주방4";
            }
            else
            {
                printerSetPopup.transform.Find("background/04/name").GetComponent<InputField>().text = Global.setinfo.printerSet.printer4.name;
            }
            printerSetPopup.transform.Find("background/04/set").GetComponent<Dropdown>().value = Global.setinfo.printerSet.printer4.useset;
            printerSetPopup.transform.Find("background/04/port").GetComponent<InputField>().text = Global.setinfo.printerSet.printer4.port;
            printerSetPopup.transform.Find("background/04/ip").GetComponent<InputField>().text = Global.setinfo.printerSet.printer4.ip_baudrate;
            printerSetPopup.transform.Find("background/output").GetComponent<Dropdown>().value = Global.setinfo.printerSet.menu_output;
        }
        catch (Exception)
        {

        }
        popup_type = 6;//주방프린터셋팅팝업
    }

    IEnumerator LoadAdmin(WWW www)
    {
        yield return www;
        Global.setinfo.admins = new List<Admin>();
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                JSONNode admins = JSON.Parse(jsonNode["adminlist"].ToString());
                for (int i = 0; i < admins.Count; i++)
                {
                    Admin adm = new Admin();
                    adm.id = admins[i]["id"];
                    adm.name = admins[i]["name"];
                    adm.code = admins[i]["code"];
                    Global.setinfo.admins.Add(adm);
                }
            }
        }
        m_adminItem.Clear();
        while (adminParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(adminParent.transform.GetChild(0).gameObject));
        }
        while (adminParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        //loading tablegrouplist
        for (int i = 0; i < Global.setinfo.admins.Count; i++)
        {
            GameObject tmpAdminObj = Instantiate(adminItem);
            tmpAdminObj.transform.SetParent(adminParent.transform);
            //tmpAdminObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
            //tmpAdminObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
            //float left = 0;
            //float right = 0;
            //tmpAdminObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
            //tmpAdminObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
            //tmpAdminObj.transform.localScale = Vector3.one;

            tmpAdminObj.transform.Find("name").GetComponent<InputField>().text = Global.setinfo.admins[i].name;
            tmpAdminObj.transform.Find("id").GetComponent<Text>().text = Global.setinfo.admins[i].id;
            tmpAdminObj.transform.Find("code").GetComponent<InputField>().text = Global.setinfo.admins[i].code;
            int _i = i;
            tmpAdminObj.transform.Find("del").GetComponent<Button>().onClick.RemoveAllListeners();
            tmpAdminObj.transform.Find("del").GetComponent<Button>().onClick.AddListener(delegate () { onDelAdmin(_i); });
            m_adminItem.Add(tmpAdminObj);
        }
    }

    public void setAdmin()
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.get_admins_api, form);
        StartCoroutine(LoadAdmin(www));
        regAdminPopup.SetActive(true);
        popup_type = 8;//관리자등록셋팅팝업
    }

    IEnumerator LoadShopHistory(WWW www)
    {
        yield return www;
        Global.setinfo.shopInfo.shHistory = new List<ShopHistory>();
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                Debug.Log(jsonNode.ToString());
                Global.userinfo.pub.is_open = jsonNode["status"].AsInt;
                JSONNode history = JSON.Parse(jsonNode["historylist"].ToString());
                for (int i = 0; i < history.Count; i++)
                {
                    ShopHistory spHistory = new ShopHistory();
                    spHistory.date = history[i]["date"];
                    spHistory.time = history[i]["time"];
                    spHistory.status = history[i]["status"].AsInt;
                    Global.setinfo.shopInfo.shHistory.Add(spHistory);
                }
            }
        }
        m_shopItem.Clear();
        while (shophistoryParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(shophistoryParent.transform.GetChild(0).gameObject));
        }
        while (shophistoryParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        //loading tablegrouplist
        for (int i = 0; i < Global.setinfo.shopInfo.shHistory.Count; i++)
        {
            GameObject tmpHistoryObj = Instantiate(shophistoryItem);
            tmpHistoryObj.transform.SetParent(shophistoryParent.transform);
            //tmpHistoryObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
            //tmpHistoryObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
            //float left = 0;
            //float right = 0;
            //tmpHistoryObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
            //tmpHistoryObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
            //tmpHistoryObj.transform.localScale = Vector3.one;
            tmpHistoryObj.transform.Find("date").GetComponent<Text>().text = Global.setinfo.shopInfo.shHistory[i].date;
            tmpHistoryObj.transform.Find("time").GetComponent<Text>().text = Global.setinfo.shopInfo.shHistory[i].time;
            if(Global.setinfo.shopInfo.shHistory[i].status == 1)
            {
                tmpHistoryObj.transform.Find("action").GetComponent<Text>().text = "Open";
            }
            else
            {
                tmpHistoryObj.transform.Find("action").GetComponent<Text>().text = "Close";
            }
            m_shopItem.Add(tmpHistoryObj);
        }
        if(Global.userinfo.pub.is_open == 1)
        {
            shopSetPopup.transform.Find("background/type/open").GetComponent<Toggle>().isOn = true;
        }
        else
        {
            shopSetPopup.transform.Find("background/type/open").GetComponent<Toggle>().isOn = false;
        }
    }

    public void setShop()
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.get_shop_history_api, form);
        StartCoroutine(LoadShopHistory(www));
        shopSetPopup.SetActive(true);
        popup_type = 9;//shop open/close
    }

    IEnumerator LoadCategory(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                Global.categorylist.Clear();
                JSONNode cateList = JSON.Parse(jsonNode["categorylist"].ToString());
                for (int i = 0; i < cateList.Count; i++)
                {
                    CategoryInfo cinfo = new CategoryInfo();
                    cinfo.id = cateList[i]["id"];
                    cinfo.name = cateList[i]["name"];
                    cinfo.engname = cateList[i]["engname"];
                    cinfo.sort_order = cateList[i]["sort_order"].AsInt;
                    cinfo.is_pos = cateList[i]["is_pos"].AsInt;
                    cinfo.is_kiosk = cateList[i]["is_kiosk"].AsInt;
                    cinfo.is_tablet = cateList[i]["is_tablet"].AsInt;
                    cinfo.is_mobile = cateList[i]["is_mobile"].AsInt;
                    cinfo.menulist = new List<MenuInfo>();
                    JSONNode menulist = JSON.Parse(jsonNode["menulist"].ToString());
                    for (int j = 0; j < menulist.Count; j++)
                    {
                        MenuInfo minfo = new MenuInfo();
                        minfo.name = menulist[j]["name"];
                        minfo.engname = menulist[j]["engname"];
                        minfo.barcode = menulist[j]["barcode"];
                        minfo.contents = menulist[j]["contents"];
                        minfo.sort_order = menulist[j]["sort_order"].AsInt;
                        minfo.pack_price = menulist[j]["pack_price"].AsInt;
                        minfo.id = menulist[j]["id"];
                        minfo.price = menulist[j]["price"];
                        minfo.is_best = menulist[j]["is_best"];
                        minfo.sell_amount = menulist[j]["sell_amount"].AsInt;
                        minfo.sell_tap = menulist[j]["sell_tap"].AsInt;
                        minfo.is_soldout = menulist[j]["is_soldout"];
                        minfo.kit1 = menulist[j]["kit1"].AsInt;
                        minfo.kit2 = menulist[j]["kit2"].AsInt;
                        minfo.kit3 = menulist[j]["kit3"].AsInt;
                        minfo.kit4 = menulist[j]["kit4"].AsInt;
                        cinfo.menulist.Add(minfo);
                    }
                    Global.categorylist.Add(cinfo);
                }
            }
        }
        m_categoryItem.Clear();
        while (categoryParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(categoryParent.transform.GetChild(0).gameObject));
        }
        while (categoryParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        //loading categorylist
        for (int i = 0; i < Global.categorylist.Count; i++)
        {
            GameObject tmpObj = Instantiate(categoryItem);
            tmpObj.transform.SetParent(categoryParent.transform);
            //tmpObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
            //tmpObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
            //float left = 0;
            //float right = 0;
            //tmpObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
            //tmpObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
            //tmpObj.transform.localScale = Vector3.one;
            tmpObj.transform.Find("name").GetComponent<Text>().text = Global.categorylist[i].name;
            tmpObj.transform.Find("english").GetComponent<InputField>().text = Global.categorylist[i].engname;
            tmpObj.transform.Find("order").GetComponent<InputField>().text = Global.categorylist[i].sort_order.ToString();
            if(Global.categorylist[i].is_pos == 1)
            {
                tmpObj.transform.Find("pos").GetComponent<Toggle>().isOn = true;
            }
            else
            {
                tmpObj.transform.Find("pos").GetComponent<Toggle>().isOn = false;
            }
            if(Global.categorylist[i].is_kiosk == 1)
            {
                tmpObj.transform.Find("kiosk").GetComponent<Toggle>().isOn = true;
            }
            else
            {
                tmpObj.transform.Find("kiosk").GetComponent<Toggle>().isOn = false;
            }
            if(Global.categorylist[i].is_tablet == 1)
            {
                tmpObj.transform.Find("tablet").GetComponent<Toggle>().isOn = true;
            }
            else
            {
                tmpObj.transform.Find("tablet").GetComponent<Toggle>().isOn = false;
            }
            if(Global.categorylist[i].is_mobile == 1)
            {
                tmpObj.transform.Find("mobile").GetComponent<Toggle>().isOn = true;
            }
            else
            {
                tmpObj.transform.Find("mobile").GetComponent<Toggle>().isOn = false;
            }
            tmpObj.transform.Find("id").GetComponent<Text>().text = Global.categorylist[i].id.ToString();
            m_categoryItem.Add(tmpObj);
        }
    }

    public void setMenuCategory()
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("is_get_all", 1);
        form.AddField("app_type", Global.app_type);
        WWW www = new WWW(Global.api_url + Global.get_categorylist_api, form);
        StartCoroutine(LoadCategory(www));
        menuSetDisPopup.SetActive(true);
        popup_type = 10;//메뉴분류
    }

    string firscateno = "";
    string oldSelectedCategoryNo = "";

    IEnumerator LoadMenu(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                Global.categorylist.Clear();
                JSONNode cateList = JSON.Parse(jsonNode["categorylist"].ToString());
                for (int i = 0; i < cateList.Count; i++)
                {
                    CategoryInfo cinfo = new CategoryInfo();
                    cinfo.id = cateList[i]["id"];
                    cinfo.name = cateList[i]["name"];
                    cinfo.engname = cateList[i]["engname"];
                    cinfo.sort_order = cateList[i]["sort_order"].AsInt;
                    cinfo.is_pos = cateList[i]["is_pos"].AsInt;
                    cinfo.is_kiosk = cateList[i]["is_kiosk"].AsInt;
                    cinfo.is_tablet = cateList[i]["is_tablet"].AsInt;
                    cinfo.is_mobile = cateList[i]["is_mobile"].AsInt;
                    cinfo.menulist = new List<MenuInfo>();
                    JSONNode menulist = JSON.Parse(cateList[i]["menulist"].ToString());
                    for (int j = 0; j < menulist.Count; j++)
                    {
                        MenuInfo minfo = new MenuInfo();
                        minfo.name = menulist[j]["name"];
                        minfo.engname = menulist[j]["engname"];
                        minfo.barcode = menulist[j]["barcode"];
                        minfo.contents = menulist[j]["contents"];
                        minfo.sell_amount = menulist[j]["sell_amount"].AsInt;
                        minfo.sell_tap = menulist[j]["sell_tap"].AsInt;
                        minfo.sort_order = menulist[j]["sort_order"].AsInt;
                        minfo.pack_price = menulist[j]["pack_price"].AsInt;
                        minfo.id = menulist[j]["id"];
                        minfo.price = menulist[j]["price"].AsInt;
                        minfo.is_best = menulist[j]["is_best"].AsInt;
                        minfo.is_soldout = menulist[j]["is_soldout"].AsInt;
                        minfo.kit1 = menulist[j]["kit1"].AsInt;
                        minfo.kit2 = menulist[j]["kit2"].AsInt;
                        minfo.kit3 = menulist[j]["kit3"].AsInt;
                        minfo.kit4 = menulist[j]["kit4"].AsInt;
                        cinfo.menulist.Add(minfo);
                    }
                    Global.categorylist.Add(cinfo);
                }
            }
        }
        m_menuCategoryItem.Clear();
        while (menuCategoryParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(menuCategoryParent.transform.GetChild(0).gameObject));
        }
        while (menuCategoryParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        //loading categorylist
        if(Global.categorylist.Count > 0)
        {
            firscateno = Global.categorylist[0].id;
        }

        for (int i = 0; i < Global.categorylist.Count; i++)
        {
            GameObject tmpObj = Instantiate(menuCategoryItem);
            tmpObj.transform.SetParent(menuCategoryParent.transform);
            tmpObj.GetComponent<Text>().text = Global.categorylist[i].name;
            tmpObj.transform.Find("id").GetComponent<Text>().text = Global.categorylist[i].id.ToString();
            string _id = Global.categorylist[i].id;
            tmpObj.GetComponent<Button>().onClick.RemoveAllListeners();
            tmpObj.GetComponent<Button>().onClick.AddListener(delegate () { StartCoroutine(LoadCateMenu(_id)); });
            m_menuCategoryItem.Add(tmpObj);
        }
        if (Global.categorylist.Count > 0 && firscateno != "")
            StartCoroutine(LoadCateMenu(firscateno));
    }

    IEnumerator LoadCateMenu(string cateno)
    {
        Debug.Log("old = " + oldSelectedCategoryNo + ", new = " + cateno);
        //선택된 카테고리 볼드체로.
        try
        {
            for (int i = 0; i < menuCategoryParent.transform.childCount; i++)
            {
                if (menuCategoryParent.transform.GetChild(i).Find("id").GetComponent<Text>().text == oldSelectedCategoryNo.ToString())
                {
                    menuCategoryParent.transform.GetChild(i).GetComponent<Text>().color = Color.white;
                }
                if (menuCategoryParent.transform.GetChild(i).Find("id").GetComponent<Text>().text == cateno.ToString())
                {
                    menuCategoryParent.transform.GetChild(i).GetComponent<Text>().color = Global.selected_color;
                }
            }
        }
        catch (Exception ex)
        {

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
        string cateid = "";
        for (int i = 0; i < Global.categorylist.Count; i++)
        {
            if (Global.categorylist[i].id == cateno)
            {
                minfoList = Global.categorylist[i].menulist;
                cateid = Global.categorylist[i].id;
                break;
            }
        }
        m_menuItem.Clear();
        int menuCnt = minfoList.Count;
        for (int i = 0; i < minfoList.Count; i++)
        {
            try
            {
                GameObject tmpMenuObj = Instantiate(menuItem);
                tmpMenuObj.transform.SetParent(menuParent.transform);
                tmpMenuObj.transform.Find("menu").GetComponent<Text>().text = minfoList[i].name;
                tmpMenuObj.transform.Find("english").GetComponent<InputField>().text = minfoList[i].engname;
                tmpMenuObj.transform.Find("contents").GetComponent<Button>().onClick.RemoveAllListeners();
                Text txtObj = tmpMenuObj.transform.Find("contents/Text").GetComponent<Text>();
                string txtCont = minfoList[i].contents;
                if (txtCont == null) txtCont = "";
                tmpMenuObj.transform.Find("contents").GetComponent<Button>().onClick.AddListener(delegate () { onSelectContents(txtCont, txtObj); });
                tmpMenuObj.transform.Find("contents/Text").GetComponent<Text>().text = minfoList[i].contents;
                tmpMenuObj.transform.Find("barcode").GetComponent<InputField>().text = minfoList[i].barcode;
                tmpMenuObj.transform.Find("order").GetComponent<InputField>().text = minfoList[i].sort_order.ToString();
                tmpMenuObj.transform.Find("cost").GetComponent<InputField>().text = Global.GetPriceFormat(minfoList[i].is_best);
                if(minfoList[i].is_soldout == 1)
                {
                    tmpMenuObj.transform.Find("soldout").GetComponent<Toggle>().isOn = true;
                }
                else
                {
                    tmpMenuObj.transform.Find("soldout").GetComponent<Toggle>().isOn = false;
                }
                tmpMenuObj.transform.Find("id").GetComponent<Text>().text = minfoList[i].id.ToString();
                tmpMenuObj.transform.Find("cateid").GetComponent<Text>().text = cateid.ToString();
                m_menuItem.Add(tmpMenuObj);
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
    }

    IEnumerator LoadCateSellMenu(string cateno)
    {
        Debug.Log("old = " + oldSelectedCategoryNo + ", new = " + cateno);
        //선택된 카테고리 볼드체로.
        try
        {
            for (int i = 0; i < cateSellSizeParent.transform.childCount; i++)
            {
                if (cateSellSizeParent.transform.GetChild(i).Find("id").GetComponent<Text>().text == oldSelectedCategoryNo.ToString())
                {
                    cateSellSizeParent.transform.GetChild(i).GetComponent<Text>().color = Color.white;
                }
                if (cateSellSizeParent.transform.GetChild(i).Find("id").GetComponent<Text>().text == cateno.ToString())
                {
                    cateSellSizeParent.transform.GetChild(i).GetComponent<Text>().color = Global.selected_color;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
        //UI 내역 초기화
        while (sellSizeParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(sellSizeParent.transform.GetChild(0).gameObject));
        }
        while (sellSizeParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }

        oldSelectedCategoryNo = cateno;

        //카테고리에 한한 메뉴리스트 가져오기
        List<MenuInfo> minfoList = new List<MenuInfo>();
        string cateid = "";
        for (int i = 0; i < Global.categorylist.Count; i++)
        {
            if (Global.categorylist[i].id == cateno)
            {
                minfoList = Global.categorylist[i].menulist;
                cateid = Global.categorylist[i].id;
                break;
            }
        }
        m_menuItem.Clear();
        int menuCnt = minfoList.Count;
        for (int i = 0; i < minfoList.Count; i++)
        {
            try
            {
                GameObject tmpMenuObj = Instantiate(sellSizeItem);
                tmpMenuObj.transform.SetParent(sellSizeParent.transform);
                tmpMenuObj.transform.Find("menu").GetComponent<Text>().text = minfoList[i].name;
                for (int j = 0; j < Global.tapSellMenuList.Count; j++)
                {
                    Dropdown.OptionData option = new Dropdown.OptionData();
                    option = new Dropdown.OptionData();
                    option.text = ((Global.tapSellMenuList[j].serial_number).ToString() + " " + Global.tapSellMenuList[j].product_name);
                    tmpMenuObj.transform.Find("beer").GetComponent<Dropdown>().options.Add(option);
                }
                tmpMenuObj.transform.Find("beer").GetComponent<Dropdown>().onValueChanged.RemoveAllListeners();
                tmpMenuObj.transform.Find("beer").GetComponent<Dropdown>().onValueChanged.AddListener((value) =>
                {
                    SelectTap(value, tmpMenuObj);
                });

                tmpMenuObj.transform.Find("beer").GetComponent<Dropdown>().value = minfoList[i].sell_tap;
                tmpMenuObj.transform.Find("size").GetComponent<InputField>().text = minfoList[i].sell_amount.ToString();
                if (minfoList[i].sell_tap == 0)
                {
                    tmpMenuObj.transform.Find("size").gameObject.SetActive(false);
                }
                else
                {
                    tmpMenuObj.transform.Find("size").gameObject.SetActive(true);
                }
                tmpMenuObj.transform.Find("id").GetComponent<Text>().text = minfoList[i].id.ToString();
                tmpMenuObj.transform.Find("cateid").GetComponent<Text>().text = cateid.ToString();
                m_menuItem.Add(tmpMenuObj);
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
    }
    void SelectTap(int value, GameObject tmpMenuObj)
    {
        //loading values from the selected tap.
        if (value != 0)
        {
            tmpMenuObj.transform.Find("size").gameObject.SetActive(true);
        }
        else
        {
            tmpMenuObj.transform.Find("size").gameObject.SetActive(false);
        }
    }

    Text contentsTxt;
    void onSelectContents(string contents, Text ContentsTxt)
    {
        if(ContentsTxt.text != "")
        {
            contentsPopupTxt.text = ContentsTxt.text;
        }
        else
        {
            contentsPopupTxt.text = contents;
        }
        contentsTxt = ContentsTxt;
        contentsPopup.SetActive(true);
    }

    public void HideContentsPopup()
    {
        if(contentsTxt != null)
        {
            contentsTxt.text = contentsPopupTxt.text;
            contentsTxt = null;
        }
        contentsPopup.SetActive(false);
    }

    public void setMenu()
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("app_type", Global.app_type);
        WWW www = new WWW(Global.api_url + Global.get_categorylist_api, form);
        StartCoroutine(LoadMenu(www));
        menuSetPopup.SetActive(true);
        popup_type = 11;//메뉴
    }

    public void setpaymentPrice()
    {
        paymentPriceSetPopup.SetActive(true);
        try
        {
            paymentPriceSetPopup.transform.Find("background/val").GetComponent<Dropdown>().value = Global.userinfo.pub.ceiltype;
        }catch(Exception ex)
        {

        }
        popup_type = 12;//결제금액 올림/내림
    }

    public void setCountBasedTime()
    {
        CountBasedTimeSetPopup.SetActive(true);
        try
        {
            CountBasedTimeSetPopup.transform.Find("background/val").GetComponent<InputField>().text = Global.userinfo.pub.closetime.ToString();
        }
        catch (Exception ex)
        {

        }
        popup_type = 13;//정산 기준 시간 셋팅팝업
    }

    public void setPaymentTime()
    {
        paymentTimeSetPopup.SetActive(true);
        try
        {
            paymentTimeSetPopup.transform.Find("background/val").GetComponent<InputField>().text = Global.setinfo.payment_time.ToString();
        }
        catch (Exception ex)
        {

        }
        popup_type = 26;//결제 대기시간 설정
    }

    public void searchClientFromReg()
    {
        string key = regPrepayPopup.transform.Find("background/val1").GetComponent<InputField>().text;
        if (!checkPhoneType(key))
        {
            error_popup.SetActive(true);
            error_string.text = "고객번호 마지막 4자리를 입력하세요.";
        }
        else
        {
            WWWForm form = new WWWForm();
            form.AddField("type", 0);//예치금조회
            form.AddField("phone", regPrepayPopup.transform.Find("background/val1").GetComponent<InputField>().text);
            form.AddField("pub_id", Global.userinfo.pub.id);
            WWW www = new WWW(Global.api_url + Global.check_client_api, form);
            StartCoroutine(FindPrepayClient(www));            
        }
    }

    public void usePoint()
    {
        int point = Global.GetConvertedPrice(popup2_val8.GetComponent<InputField>().text);
        if (pay_price / 2 < point)
        {
            error_popup.SetActive(true);
            error_string.text = "포인트는 결제금액의 50% 까지만 사용이 가능합니다.";
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
                error_popup.SetActive(true);
                error_string.text = "고객번호 마지막 4자리를 입력하세요.";
                return;
            }
        }
        if (payPopup.transform.Find("background/1/payamount").GetComponent<InputField>().text.Trim() == "")
        {
            error_popup.SetActive(true);
            error_string.text = "결제금액을 입력한 후에 조회하세요.";
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

    void viewClientInfo(int type, ClientInfo cinfo)
    {
        client_name.text = cinfo.no;
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

    IEnumerator checkClientProcess(WWW www, int type)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                clients.Clear();
                JSONNode clist = JSON.Parse(jsonNode["clientlist"].ToString()/*.Replace("\"", "")*/);
                for(int i = 0; i < clist.Count; i++)
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
                if(clist.Count == 1)
                {
                    viewClientInfo(type, clients[0]);
                }
                else if(clist.Count > 1)
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
                    multiSel.GetComponent<Dropdown>().onValueChanged.AddListener((value) => {
                        SelectClient(value, type);
                        }
                    );
                    multiSel.GetComponent<Dropdown>().Show();
                }
            }
            else
            {
                error_popup.SetActive(true);
                error_string.text = jsonNode["msg"];
            }
        }
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
        Debug.Log("select client" + value + "," + type);
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

    void select_all_point(int point)
    {
        popup2_val8.GetComponent<InputField>().text = Global.GetPriceFormat(point);
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

    void SelectClientInPrepay(int value)
    {
        if(value == 0)
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

    IEnumerator FindPrepayClient(WWW www)
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
                else
                {
                    regPrepayPopup.transform.Find("background/val1").GetComponent<InputField>().text = "";                    client_id = "";
                    error_popup.SetActive(true);
                    error_string.text = "고객정보가 없습니다. 고객등록 후 진행하세요.";
                }
            }
            else
            {
                regPrepayPopup.transform.Find("background/val1").GetComponent<InputField>().text = ""; client_id = "";
                client_id = "";
                error_popup.SetActive(true);
                error_string.text = "고객정보가 없습니다. 고객등록 후 진행하세요.";
                //selectPopup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
                //selectPopup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { onConfirmSelectPopup(); });
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
        if(www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                client_id = jsonNode["client_id"];
                onCloseSelectPopup();
                error_string.text = "신규고객 등록에 성공했습니다.";
                error_popup.SetActive(true);
            }
            else
            {
                error_string.text = "신규고객 등록에 실패했습니다.";
                error_popup.SetActive(true);
            }
        }
        else
        {
            error_string.text = "신규고객 등록에 실패했습니다.";
            error_popup.SetActive(true);
        }
    }

    void InitPrepayPopup()
    {
        multiPre.SetActive(false);
        regPrepayPopup.transform.Find("background/val1").GetComponent<InputField>().text = "";
        regPrepayPopup.transform.Find("background/val2").GetComponent<InputField>().text = "";
        regPrepayPopup.transform.Find("background/val3").GetComponent<Text>().text = "";
        regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text = "";
        regPrepayPopup.transform.Find("background/val5").GetComponent<InputField>().text = "";
    }

    public void setRegPrepay()
    {
        checkSetPopup.transform.Find("background/input").GetComponent<InputField>().text = "";
        checkSetPopup.SetActive(false);
        regPrepayPopup.SetActive(true);
        InitPrepayPopup();
        popup_type = 14;//예치금 등록
    }

    public void sel1Price()
    {
        string pStr = regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text;
        int price = Global.GetConvertedPrice(pStr);
        regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text = Global.GetPriceFormat(price + 10000);
    }

    public void sel5Price()
    {
        string pStr = regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text;
        int price = Global.GetConvertedPrice(pStr);
        regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text = Global.GetPriceFormat(price + 50000);
    }

    public void sel10Price()
    {
        string pStr = regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text;
        int price = Global.GetConvertedPrice(pStr);
        regPrepayPopup.transform.Find("background/val4").GetComponent<InputField>().text = Global.GetPriceFormat(price + 100000);
    }

    IEnumerator LoadSearchResult(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            List<PrepayInfo> prepayList = new List<PrepayInfo>();
            if (jsonNode["suc"].AsInt == 1)
            {
                JSONNode plist = JSON.Parse(jsonNode["prepayList"].ToString()/*.Replace("\"", "")*/);
                prepayList.Clear();
                for (int i = 0; i < plist.Count; i++)
                {
                    PrepayInfo pinfo = new PrepayInfo();
                    pinfo.id = plist[i]["id"];
                    pinfo.user_name = plist[i]["user_name"];
                    pinfo.no = plist[i]["no"];
                    pinfo.first_reg_time = plist[i]["reg_datetime"];
                    pinfo.charge_price = plist[i]["charge_price"].AsInt;
                    pinfo.used_price = plist[i]["used_price"].AsInt;
                    pinfo.remain_price = plist[i]["remain_price"].AsInt;
                    pinfo.bigo = plist[i]["bigo"];
                    prepayList.Add(pinfo);
                }
            }
            InitPrepay();
            while (checkPrepayParent.transform.childCount > 0)
            {
                yield return new WaitForFixedUpdate();
            }
            for (int i = 0; i < prepayList.Count; i++)
            {
                GameObject tmpObj = Instantiate(checkPrepayItem);
                tmpObj.transform.SetParent(checkPrepayParent.transform);
                //tmpObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmpObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmpObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmpObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmpObj.transform.localScale = Vector3.one;
                tmpObj.transform.Find("name").GetComponent<Text>().text = prepayList[i].user_name;
                tmpObj.transform.Find("no").GetComponent<Text>().text = prepayList[i].no;
                tmpObj.transform.Find("regdate").GetComponent<Text>().text = prepayList[i].first_reg_time;
                tmpObj.transform.Find("charge_price").GetComponent<Text>().text = Global.GetPriceFormat(prepayList[i].charge_price);
                tmpObj.transform.Find("use_price").GetComponent<Text>().text = Global.GetPriceFormat(prepayList[i].used_price);
                tmpObj.transform.Find("remain_price").GetComponent<Text>().text = Global.GetPriceFormat(prepayList[i].remain_price);
                tmpObj.transform.Find("bigo").GetComponent<Text>().text = prepayList[i].bigo;
                tmpObj.transform.Find("id").GetComponent<Text>().text = prepayList[i].id.ToString();
                string _id = prepayList[i].id;
                tmpObj.GetComponent<Button>().onClick.RemoveAllListeners();
                tmpObj.GetComponent<Button>().onClick.AddListener(delegate () { onSelPrepayDetail(_id); });
                m_checkprepayItem.Add(tmpObj);
            }
        }
    }

    IEnumerator LoadPointResult(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            List<PointInfo> pointList = new List<PointInfo>();
            if (jsonNode["suc"].AsInt == 1)
            {
                JSONNode plist = JSON.Parse(jsonNode["pointList"].ToString()/*.Replace("\"", "")*/);
                pointList.Clear();
                for (int i = 0; i < plist.Count; i++)
                {
                    PointInfo pinfo = new PointInfo();
                    pinfo.id = plist[i]["id"];
                    pinfo.user_name = plist[i]["user_name"];
                    pinfo.no = plist[i]["no"];
                    pinfo.first_reg_time = plist[i]["reg_datetime"];
                    pinfo.save_point = plist[i]["save_point"].AsInt;
                    pinfo.used_point = plist[i]["used_point"].AsInt;
                    pinfo.remain_point = plist[i]["remain_point"].AsInt;
                    pointList.Add(pinfo);
                }
            }
            InitPoint();
            while (checkPointParent.transform.childCount > 0)
            {
                yield return new WaitForFixedUpdate();
            }
            for (int i = 0; i < pointList.Count; i++)
            {
                GameObject tmpObj = Instantiate(checkPointItem);
                tmpObj.transform.SetParent(checkPointParent.transform);
                //tmpObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmpObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmpObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmpObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmpObj.transform.localScale = Vector3.one;
                tmpObj.transform.Find("name").GetComponent<Text>().text = pointList[i].user_name;
                tmpObj.transform.Find("no").GetComponent<Text>().text = pointList[i].no;
                tmpObj.transform.Find("regdate").GetComponent<Text>().text = pointList[i].first_reg_time;
                tmpObj.transform.Find("save_point").GetComponent<Text>().text = Global.GetPriceFormat(pointList[i].save_point);
                tmpObj.transform.Find("use_point").GetComponent<Text>().text = Global.GetPriceFormat(pointList[i].used_point);
                tmpObj.transform.Find("remain_point").GetComponent<Text>().text = Global.GetPriceFormat(pointList[i].remain_point);
                tmpObj.transform.Find("id").GetComponent<Text>().text = pointList[i].id.ToString();
                string _id = pointList[i].id;
                tmpObj.GetComponent<Button>().onClick.RemoveAllListeners();
                tmpObj.GetComponent<Button>().onClick.AddListener(delegate () { onSelPointDetail(_id); });
                m_checkpointItem.Add(tmpObj);
            }
        }
    }

    public void onRegPoint()
    {
        checkPointPopup.SetActive(false);
        regClientPopup.SetActive(true);
        regClientPopup.transform.Find("background/no").GetComponent<InputField>().text = "";
        regClientPopup.transform.Find("background/name").GetComponent<InputField>().text = "";
    }

    public void RegClient()
    {
        string no = regClientPopup.transform.Find("background/no").GetComponent<InputField>().text;
        string name = regClientPopup.transform.Find("background/name").GetComponent<InputField>().text;
        if(no == "" || name == "" || no == null || name == null)
        {
            error_popup.SetActive(true);
            error_string.text = "고객정보를 정확히 입력하세요.";
        }
        else
        {
            WWWForm form = new WWWForm();
            form.AddField("pub_id", Global.userinfo.pub.id);
            form.AddField("no", no);
            form.AddField("name", name);
            WWW www = new WWW(Global.api_url + Global.add_client_api, form);
            StartCoroutine(addClientProcess(www));
        }
    }

    IEnumerator addClientProcess(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                client_id = jsonNode["client_id"];
            }
            else
            {
                error_string.text = jsonNode["msg"];
                error_popup.SetActive(true);
            }
        }
        else
        {
            error_string.text = "고객등록시 알지 못할 오류가 발생하였습니다.";
            error_popup.SetActive(true);
        }
        regClientPopup.SetActive(false);
    }

    public void onCloseRegClientPopup()
    {
        regClientPopup.SetActive(false);
    }

    void onSelPointDetail(string id)
    {
        pointDetailPopup.SetActive(true);
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("id", id);
        WWW www = new WWW(Global.api_url + Global.get_point_detail_api, form);
        StartCoroutine(loadPointDetailList(www));
    }

    void onSelPrepayDetail(string id)
    {
        prepayDetailPopup.SetActive(true);
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("id", id);
        WWW www = new WWW(Global.api_url + Global.get_prepay_detail_api, form);
        StartCoroutine(loadPrepayDetailList(www));
    }

    IEnumerator loadPointDetailList(WWW www)
    {
        yield return www;
        while (pointDeteailParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(pointDeteailParent.transform.GetChild(0).gameObject));
        }
        while (pointDeteailParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                JSONNode plist = JSON.Parse(jsonNode["pointList"].ToString()/*.Replace("\"", "")*/);
                for (int i = 0; i < plist.Count; i++)
                {
                    GameObject tmpObj = Instantiate(pointDetailItem);
                    tmpObj.transform.SetParent(pointDeteailParent.transform);
                    //tmpObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                    //tmpObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                    //float left = 0;
                    //float right = 0;
                    //tmpObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                    //tmpObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                    //tmpObj.transform.localScale = Vector3.one;
                    tmpObj.transform.Find("name").GetComponent<Text>().text = plist[i]["user_name"];
                    tmpObj.transform.Find("no").GetComponent<Text>().text = plist[i]["no"];
                    tmpObj.transform.Find("regdate").GetComponent<Text>().text = plist[i]["reg_datetime"];
                    tmpObj.transform.Find("save_point").GetComponent<Text>().text = Global.GetPriceFormat(plist[i]["save_point"].AsInt);
                    tmpObj.transform.Find("use_point").GetComponent<Text>().text = Global.GetPriceFormat(plist[i]["used_point"].AsInt);
                    tmpObj.transform.Find("remain_point").GetComponent<Text>().text = Global.GetPriceFormat(plist[i]["remain_point"].AsInt);
                }
            }
        }
        popup_type = 24;//포인트 상세조회
    }

    IEnumerator loadPrepayDetailList(WWW www)
    {
        yield return www;
        while (prepayDetailParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(prepayDetailParent.transform.GetChild(0).gameObject));
        }
        while (prepayDetailParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                JSONNode plist = JSON.Parse(jsonNode["prepayList"].ToString()/*.Replace("\"", "")*/);
                for (int i = 0; i < plist.Count; i++)
                {
                    GameObject tmpObj = Instantiate(prepayDetailItem);
                    tmpObj.transform.SetParent(prepayDetailParent.transform);
                    //tmpObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                    //tmpObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                    //float left = 0;
                    //float right = 0;
                    //tmpObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                    //tmpObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                    //tmpObj.transform.localScale = Vector3.one;
                    tmpObj.transform.Find("name").GetComponent<Text>().text = plist[i]["user_name"];
                    tmpObj.transform.Find("no").GetComponent<Text>().text = plist[i]["no"];
                    tmpObj.transform.Find("regdate").GetComponent<Text>().text = plist[i]["reg_datetime"];
                    tmpObj.transform.Find("charge_price").GetComponent<Text>().text = Global.GetPriceFormat(plist[i]["charge_price"].AsInt);
                    tmpObj.transform.Find("use_price").GetComponent<Text>().text = Global.GetPriceFormat(plist[i]["used_price"].AsInt);
                    tmpObj.transform.Find("remain_price").GetComponent<Text>().text = Global.GetPriceFormat(plist[i]["remain_price"].AsInt);
                }
            }
        }
        popup_type = 22;//예치금 상세조회
    }

    public void searchPrepay()
    {
        //예치금 조회화면에서 조회버튼
        string key = checkSetPopup.transform.Find("background/input").GetComponent<InputField>().text;
        //if (key == "")
        //{
        //    error_string.text = "고객번호를 정확히 입력하세요.";
        //    error_popup.SetActive(true);
        //}
        //else
        {
            WWWForm form = new WWWForm();
            form.AddField("pub_id", Global.userinfo.pub.id);
            form.AddField("phone", key);
            WWW www = new WWW(Global.api_url + Global.search_prepay_api, form);
            StartCoroutine(LoadSearchResult(www));
        }
    }

    public void searchPoint()
    {
        //고객포인트 조회화면에서 조회버튼
        string key = checkPointPopup.transform.Find("background/input").GetComponent<InputField>().text;
        //if (key == "")
        //{
        //    error_string.text = "고객번호를 정확히 입력하세요.";
        //    error_popup.SetActive(true);
        //}
        //else
        {
            WWWForm form = new WWWForm();
            form.AddField("pub_id", Global.userinfo.pub.id);
            form.AddField("phone", key);
            WWW www = new WWW(Global.api_url + Global.search_point_api, form);
            StartCoroutine(LoadPointResult(www));
        }
    }

    void InitPrepay()
    {
        m_checkprepayItem.Clear();
        while (checkPrepayParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(checkPrepayParent.transform.GetChild(0).gameObject));
        }
    }

    void InitPoint()
    {
        m_checkpointItem.Clear();
        while (checkPointParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(checkPointParent.transform.GetChild(0).gameObject));
        }
    }

    public void setCheckPrepay()
    {
        checkSetPopup.SetActive(true);
        InitPrepay();
        popup_type = 21;//예치금 조회
    }

    public void setPoint()
    {
        //매장포인트
        checkPointPopup.SetActive(true);
        InitPoint();
        popup_type = 23;
    }

    string firstcateoutno = "";
    string oldSelectedCategoryOutNo = "";

    IEnumerator LoadMenuOut(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                Global.categorylist.Clear();
                JSONNode cateList = JSON.Parse(jsonNode["categorylist"].ToString());
                for (int i = 0; i < cateList.Count; i++)
                {
                    CategoryInfo cinfo = new CategoryInfo();
                    cinfo.id = cateList[i]["id"];
                    cinfo.name = cateList[i]["name"];
                    cinfo.engname = cateList[i]["engname"];
                    cinfo.sort_order = cateList[i]["sort_order"].AsInt;
                    cinfo.is_pos = cateList[i]["is_pos"].AsInt;
                    cinfo.is_kiosk = cateList[i]["is_kiosk"].AsInt;
                    cinfo.is_tablet = cateList[i]["is_tablet"].AsInt;
                    cinfo.is_mobile = cateList[i]["is_mobile"].AsInt;
                    cinfo.menulist = new List<MenuInfo>();
                    JSONNode menulist = JSON.Parse(cateList[i]["menulist"].ToString());
                    for (int j = 0; j < menulist.Count; j++)
                    {
                        MenuInfo minfo = new MenuInfo();
                        minfo.name = menulist[j]["name"];
                        minfo.engname = menulist[j]["engname"];
                        minfo.barcode = menulist[j]["barcode"];
                        minfo.contents = menulist[j]["contents"];
                        minfo.sort_order = menulist[j]["sort_order"].AsInt;
                        minfo.pack_price = menulist[j]["pack_price"].AsInt;
                        minfo.sell_amount = menulist[j]["sell_amount"].AsInt;
                        minfo.sell_tap = menulist[j]["sell_tap"].AsInt;
                        minfo.id = menulist[j]["id"];
                        minfo.price = menulist[j]["price"];
                        minfo.is_best = menulist[j]["is_best"];
                        minfo.is_soldout = menulist[j]["is_soldout"].AsInt;
                        minfo.kit1 = menulist[j]["kit1"].AsInt;
                        minfo.kit2 = menulist[j]["kit2"].AsInt;
                        minfo.kit3 = menulist[j]["kit3"].AsInt;
                        minfo.kit4 = menulist[j]["kit4"].AsInt;
                        cinfo.menulist.Add(minfo);
                    }
                    Global.categorylist.Add(cinfo);
                }
            }
        }
        m_menuOutCateItem.Clear();
        while (menuOutCateParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(menuOutCateParent.transform.GetChild(0).gameObject));
        }
        while (menuOutCateParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        //loading categorylist
        if (Global.categorylist.Count > 0)
        {
            firstcateoutno = Global.categorylist[0].id;
        }

        for (int i = 0; i < Global.categorylist.Count; i++)
        {
            GameObject tmpObj = Instantiate(menuOutCateItem);
            tmpObj.transform.SetParent(menuOutCateParent.transform);
            //tmpObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
            //tmpObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
            //float left = 0;
            //float right = 0;
            //tmpObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
            //tmpObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
            //tmpObj.transform.localScale = Vector3.one;
            tmpObj.GetComponent<Text>().text = Global.categorylist[i].name;
            tmpObj.transform.Find("id").GetComponent<Text>().text = Global.categorylist[i].id.ToString();
            string _id = Global.categorylist[i].id;
            tmpObj.GetComponent<Button>().onClick.RemoveAllListeners();
            tmpObj.GetComponent<Button>().onClick.AddListener(delegate () { StartCoroutine(LoadCateOutMenu(_id)); });
            m_menuOutCateItem.Add(tmpObj);
        }
        if (Global.categorylist.Count > 0 && firstcateoutno != "")
            StartCoroutine(LoadCateOutMenu(firstcateoutno));
    }

    IEnumerator LoadCateSellSize(string cateno)
    {
        Debug.Log("old = " + oldSelectedCategoryOutNo + ", new = " + cateno);
        //선택된 카테고리 볼드체로.
        try
        {
            for (int i = 0; i < cateSellSizeParent.transform.childCount; i++)
            {
                if (cateSellSizeParent.transform.GetChild(i).Find("id").GetComponent<Text>().text == oldSelectedCategoryOutNo)
                {
                    cateSellSizeParent.transform.GetChild(i).GetComponent<Text>().color = Color.white;
                }
                if (cateSellSizeParent.transform.GetChild(i).Find("id").GetComponent<Text>().text == cateno)
                {
                    cateSellSizeParent.transform.GetChild(i).GetComponent<Text>().color = Global.selected_color;
                }
            }
        }
        catch (Exception ex)
        {

        }
        //UI 내역 초기화
        while (sellSizeParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(sellSizeParent.transform.GetChild(0).gameObject));
        }
        while (sellSizeParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }

        oldSelectedCategoryOutNo = cateno;

        //카테고리에 한한 메뉴리스트 가져오기
        List<SellMenuInfo> minfoList = new List<SellMenuInfo>();
        string cateid = "";
        for (int i = 0; i < Global.selllist.Count; i++)
        {
            if (Global.selllist[i].id == cateno)
            {
                minfoList = Global.selllist[i].menulist;
                cateid = Global.selllist[i].id;
                break;
            }
        }
        m_sellSizeItem.Clear();
        for (int i = 0; i < minfoList.Count; i++)
        {
            try
            {
                GameObject tmpMenuObj = Instantiate(sellSizeItem);
                tmpMenuObj.transform.SetParent(sellSizeParent.transform);
                tmpMenuObj.transform.Find("menu").GetComponent<Text>().text = minfoList[i].name;
                tmpMenuObj.transform.Find("id").GetComponent<Text>().text = minfoList[i].id;
                tmpMenuObj.transform.Find("cateid").GetComponent<Text>().text = cateid;
                tmpMenuObj.transform.Find("beer").GetComponent<Dropdown>().options.Clear();
                for (int j = 0; j < minfoList[i].beerlist.Count; j++)
                {
                    Dropdown.OptionData option = new Dropdown.OptionData();
                    option.text = minfoList[i].beerlist[j].index + " " + minfoList[i].beerlist[j].name;
                    tmpMenuObj.transform.Find("beer").GetComponent<Dropdown>().options.Add(option);
                }
                tmpMenuObj.transform.Find("beer").GetComponent<Dropdown>().onValueChanged.RemoveAllListeners();
                tmpMenuObj.transform.Find("beer").GetComponent<Dropdown>().onValueChanged.AddListener((value) => {
                    SelectBeerItem(value, minfoList[i], tmpMenuObj.transform.Find("size").gameObject);
                }
                );
                m_sellSizeItem.Add(tmpMenuObj);
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
    }

    public void SelectBeerItem(int value, SellMenuInfo minfo, GameObject sObj)
    {
        try
        {
            sObj.GetComponent<InputField>().text = minfo.beerlist[value].size + " ml";
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    IEnumerator LoadCateOutMenu(string cateno)
    {
        Debug.Log("old = " + oldSelectedCategoryOutNo + ", new = " + cateno);
        //선택된 카테고리 볼드체로.
        try
        {
            for (int i = 0; i < menuOutCateParent.transform.childCount; i++)
            {
                if (menuOutCateParent.transform.GetChild(i).Find("id").GetComponent<Text>().text == oldSelectedCategoryOutNo.ToString())
                {
                    menuOutCateParent.transform.GetChild(i).GetComponent<Text>().color = Color.white;
                }
                if (menuOutCateParent.transform.GetChild(i).Find("id").GetComponent<Text>().text == cateno.ToString())
                {
                    menuOutCateParent.transform.GetChild(i).GetComponent<Text>().color = Global.selected_color;
                }
            }
        }
        catch (Exception ex)
        {

        }
        //UI 내역 초기화
        while (menuOutParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(menuOutParent.transform.GetChild(0).gameObject));
        }
        while (menuOutParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }

        oldSelectedCategoryOutNo = cateno;

        //카테고리에 한한 메뉴리스트 가져오기
        List<MenuInfo> minfoList = new List<MenuInfo>();
        string cateid = "";
        for (int i = 0; i < Global.categorylist.Count; i++)
        {
            if (Global.categorylist[i].id == cateno)
            {
                minfoList = Global.categorylist[i].menulist;
                cateid = Global.categorylist[i].id;
                break;
            }
        }
        m_menuOutItem.Clear();
        int menuCnt = minfoList.Count;
        for (int i = 0; i < minfoList.Count; i++)
        {
            try
            {
                GameObject tmpMenuObj = Instantiate(menuOutItem);
                tmpMenuObj.transform.SetParent(menuOutParent.transform);
                //tmpMenuObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmpMenuObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmpMenuObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmpMenuObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmpMenuObj.transform.localScale = Vector3.one;
                tmpMenuObj.transform.Find("menu").GetComponent<Text>().text = minfoList[i].name;
                if(minfoList[i].kit1 == 1)
                {
                    tmpMenuObj.transform.Find("kit1").GetComponent<Toggle>().isOn = true;
                }
                else
                {
                    tmpMenuObj.transform.Find("kit1").GetComponent<Toggle>().isOn = false;
                }
                if(minfoList[i].kit2 == 1)
                {
                    tmpMenuObj.transform.Find("kit2").GetComponent<Toggle>().isOn = true;
                }
                else
                {
                    tmpMenuObj.transform.Find("kit2").GetComponent<Toggle>().isOn = false;
                }
                if (minfoList[i].kit3 == 1)
                {
                    tmpMenuObj.transform.Find("kit3").GetComponent<Toggle>().isOn = true;
                }
                else
                {
                    tmpMenuObj.transform.Find("kit3").GetComponent<Toggle>().isOn = false;
                }
                if (minfoList[i].kit4 == 1)
                {
                    tmpMenuObj.transform.Find("kit4").GetComponent<Toggle>().isOn = true;
                }
                else
                {
                    tmpMenuObj.transform.Find("kit4").GetComponent<Toggle>().isOn = false;
                }
                tmpMenuObj.transform.Find("id").GetComponent<Text>().text = minfoList[i].id.ToString();
                tmpMenuObj.transform.Find("cateid").GetComponent<Text>().text = cateid.ToString();
                m_menuOutItem.Add(tmpMenuObj);
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
    }

    public void setMenuOut()
    {
        WWWForm form = new WWWForm();
        form.AddField("app_type", Global.app_type);
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.get_categorylist_api, form);
        StartCoroutine(LoadMenuOut(www));
        menuOutputSetPopup.transform.Find("background/notice3").GetComponent<Text>().text = Global.setinfo.printerSet.printer1.name;
        menuOutputSetPopup.transform.Find("background/notice4").GetComponent<Text>().text = Global.setinfo.printerSet.printer2.name;
        menuOutputSetPopup.transform.Find("background/notice5").GetComponent<Text>().text = Global.setinfo.printerSet.printer3.name;
        menuOutputSetPopup.transform.Find("background/notice6").GetComponent<Text>().text = Global.setinfo.printerSet.printer4.name;
        menuOutputSetPopup.SetActive(true);
        popup_type = 15;//메뉴별 출력
    }

    public void setSellSize()
    {
        WWWForm form = new WWWForm();
        form.AddField("app_type", Global.app_type);
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("pos_type", 0);
        WWW www = new WWW(Global.api_url + Global.get_selllist_api, form);
        StartCoroutine(LoadMenuSell(www));
        sellSizeSetPopup.SetActive(true);
        popup_type = 27;
    }

    IEnumerator LoadMenuSell(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                Global.categorylist.Clear();
                JSONNode cateList = JSON.Parse(jsonNode["categorylist"].ToString());
                for (int i = 0; i < cateList.Count; i++)
                {
                    CategoryInfo cinfo = new CategoryInfo();
                    cinfo.id = cateList[i]["id"];
                    cinfo.name = cateList[i]["name"];
                    cinfo.engname = cateList[i]["engname"];
                    cinfo.sort_order = cateList[i]["sort_order"].AsInt;
                    cinfo.is_pos = cateList[i]["is_pos"].AsInt;
                    cinfo.is_kiosk = cateList[i]["is_kiosk"].AsInt;
                    cinfo.is_tablet = cateList[i]["is_tablet"].AsInt;
                    cinfo.is_mobile = cateList[i]["is_mobile"].AsInt;
                    cinfo.menulist = new List<MenuInfo>();
                    JSONNode menulist = JSON.Parse(cateList[i]["menulist"].ToString());
                    for (int j = 0; j < menulist.Count; j++)
                    {
                        MenuInfo minfo = new MenuInfo();
                        minfo.name = menulist[j]["name"];
                        minfo.engname = menulist[j]["engname"];
                        minfo.barcode = menulist[j]["barcode"];
                        minfo.sell_amount = menulist[j]["sell_amount"].AsInt;
                        minfo.sell_tap = menulist[j]["sell_tap"].AsInt;
                        minfo.contents = menulist[j]["contents"];
                        minfo.sort_order = menulist[j]["sort_order"].AsInt;
                        minfo.pack_price = menulist[j]["pack_price"].AsInt;
                        minfo.id = menulist[j]["id"];
                        minfo.price = menulist[j]["price"].AsInt;
                        minfo.is_best = menulist[j]["is_best"].AsInt;
                        minfo.is_soldout = menulist[j]["is_soldout"].AsInt;
                        minfo.kit1 = menulist[j]["kit1"].AsInt;
                        minfo.kit2 = menulist[j]["kit2"].AsInt;
                        minfo.kit3 = menulist[j]["kit3"].AsInt;
                        minfo.kit4 = menulist[j]["kit4"].AsInt;
                        cinfo.menulist.Add(minfo);
                        }
                    Global.categorylist.Add(cinfo);
                    }
                Global.tapSellMenuList.Clear();
                JSONNode taplist = JSON.Parse(jsonNode["taplist"].ToString());
                for (int k = 0; k < taplist.Count; k ++)
                {
                    TapSellMenuInfo tList = new TapSellMenuInfo();
                    tList.serial_number = taplist[k]["serial_number"];
                    tList.product_name = taplist[k]["product_name"];
                    Global.tapSellMenuList.Add(tList);
                }
            }
        }
        m_menuCategoryItem.Clear();
        while (cateSellSizeParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(cateSellSizeParent.transform.GetChild(0).gameObject));
        }
        while (cateSellSizeParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        //loading categorylist
        if (Global.categorylist.Count > 0)
        {
            firscateno = Global.categorylist[0].id;
        }
        for (int i = 0; i < Global.categorylist.Count; i++)
        {
            GameObject tmpObj = Instantiate(cateSellSizeItem);
            tmpObj.transform.SetParent(cateSellSizeParent.transform);
            tmpObj.GetComponent<Text>().text = Global.categorylist[i].name;
            tmpObj.transform.Find("id").GetComponent<Text>().text = Global.categorylist[i].id.ToString();
            string _id = Global.categorylist[i].id;
            tmpObj.GetComponent<Button>().onClick.RemoveAllListeners();
            tmpObj.GetComponent<Button>().onClick.AddListener(delegate () { StartCoroutine(LoadCateSellMenu(_id)); });
            m_menuCategoryItem.Add(tmpObj);
        }
        if (Global.categorylist.Count > 0 && firscateno != "")
            StartCoroutine(LoadCateSellMenu(firscateno));
    }

    public void setInvoiceOut()
    {
        invoiceOutputSetPopup.SetActive(true);
        try
        {
            invoiceOutputSetPopup.transform.Find("background/val").GetComponent<Dropdown>().value = Global.userinfo.pub.invoice_outtype;
        }catch(Exception ex)
        {

        }
        popup_type = 16;//영수증 출력
    }

    public void setPointer()
    {
        pointerSetPopup.SetActive(true);
        try
        {
            if(Global.userinfo.pub.pointer_type == 1)
            {
                pointerSetPopup.transform.Find("background/type/1").GetComponent<Toggle>().isOn = true;
            }
            else
            {
                pointerSetPopup.transform.Find("background/type/2").GetComponent<Toggle>().isOn = true;
            }
            pointerSetPopup.transform.Find("background/val").GetComponent<InputField>().text = Global.userinfo.pub.pointer_rate.ToString();
        }
        catch(Exception ex)
        {

        }
        popup_type = 17;//포인트 적립율
    }

    public void setPretagTime()
    {
        prepayTagBasedTimeSetPopup.SetActive(true);
        try
        {
            prepayTagBasedTimeSetPopup.transform.Find("background/val").GetComponent<InputField>().text = Global.userinfo.pub.prepay_tag_period.ToString();
        }
        catch (Exception ex)
        {

        }
        popup_type = 18;//선불태그 유효기간 설정
    }

    public void setTableMove()
    {
        moveTableoutPopup.SetActive(true);
        try
        {
            moveTableoutPopup.transform.Find("background/val").GetComponent<Dropdown>().value = Global.userinfo.pub.move_table_type;
        }
        catch(Exception ex)
        {

        }
        popup_type = 19;//자리이동출력
    }

    public void setTableMain()
    {
        tableMainPopup.SetActive(true);
        try
        {
            if(Global.setinfo.tableMain == 1)
            {
                tableMainPopup.transform.Find("background/type/1").GetComponent<Toggle>().isOn = true;
            }
            else
            {
                tableMainPopup.transform.Find("background/type/2").GetComponent<Toggle>().isOn = true;
            }
        }
        catch(Exception ex)
        {

        }
        popup_type = 20;//테이블기본화면
    }

    public void onConfirmErrPopup()
    {
        error_popup.SetActive(false);
    }

    public void onClosePopup()
    {
        switch (popup_type)
        {
            case 1: posSetPopup.SetActive(false);break;
            case 2: tablegroupSetPopup.SetActive(false);break;
            case 3: tableSetPopup.SetActive(false);break;
            case 4: tapSetPopup.SetActive(false);break;
            case 5: paymentSetPopup.SetActive(false);break;
            case 6: printerSetPopup.SetActive(false);break;
            case 7: monitorSetPopup.SetActive(false);break;
            case 8: regAdminPopup.SetActive(false);break;
            case 9: shopSetPopup.SetActive(false);break;
            case 10: menuSetDisPopup.SetActive(false);break;
            case 11: menuSetPopup.SetActive(false);break;
            case 12: paymentPriceSetPopup.SetActive(false);break;
            case 13: CountBasedTimeSetPopup.SetActive(false);break;
            case 14: regPrepayPopup.SetActive(false);break;
            case 15: menuOutputSetPopup.SetActive(false);break;
            case 16: invoiceOutputSetPopup.SetActive(false);break;
            case 17: pointerSetPopup.SetActive(false);break;
            case 18: prepayTagBasedTimeSetPopup.SetActive(false);break;
            case 19: moveTableoutPopup.SetActive(false);break;
            case 20: tableMainPopup.SetActive(false);break;
            case 21:
                {
                    checkSetPopup.transform.Find("background/input").GetComponent<InputField>().text = "";
                    checkSetPopup.SetActive(false);
                    break;
                }
            case 22: prepayDetailPopup.SetActive(false); popup_type = 21;  break;
            case 23: checkPointPopup.SetActive(false);break;
            case 24: pointDetailPopup.SetActive(false); popup_type = 23;  break;
            case 25: regClientPopup.SetActive(false); break;
            case 26: paymentTimeSetPopup.SetActive(false); break;
            case 27: sellSizeSetPopup.SetActive(false);break;
        }
    }

    public void onCloseSelectPopup()
    {
        selectPopup.SetActive(false);
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
