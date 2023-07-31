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
using SimpleJSON;
using System;
using System.Text;
using SocketIO;

public class PaymentManager : MonoBehaviour
{
    public Text curDateTxt;
    public GameObject paymentItem;
    public GameObject paymentParent;
    public GameObject paymentmenuParent;
    public GameObject paymentmenuItem;
    public Text menuSumTxt;
    public GameObject selDayPopup;
    public Text selMonthTxt;
    public Text tableNameTxt;
    public Text tagNameTxt;
    public GameObject err_popup;
    public Text err_str;
    public GameObject select_popup;
    public Text select_str;
    public GameObject progress_popup;
    public GameObject invoicePopup;
    public Dropdown invoiceItem;
    public GameObject moneyPopup;
    public Toggle person;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket1;

    List<PaymentManageInfo> paymentList = new List<PaymentManageInfo>();
    List<GameObject> m_paymentObj = new List<GameObject>();
    List<GameObject> m_paymentMenuObj = new List<GameObject>();
    Payment payment = new Payment();
    List<OrderItem> printList = new List<OrderItem>();

    string first_payment_id = "";
    string old_payment_no = "";
    int Year;
    int Month;
    int Day;
    DateTime selected_date;
    string cur_payId = "";
    int installment_months = 0;
    int device_type = 0;
    string appno = "";

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
    DateTime ctime;
    bool is_socket_open = false;
    int cancelPaymentFlag = 0;

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
        selected_date = Global.GetSdate();
        Year = selected_date.Year;
        Month = selected_date.Month;
        Day = selected_date.Day;
        int _y = 0;
        try
        {
            _y = int.Parse(Year.ToString().Substring(2, 2));
        }
        catch (Exception ex)
        {

        }
        selMonthTxt.text = string.Format("{0:D4}.{1:D2}", Year, Month);
        curDateTxt.text = string.Format("{0:D2}.{1:D2}.{2:D2}", _y, Month, Day);
        if (Global.setinfo.paymentDeviceInfo.ip != "")
        {
            ipAdd = System.Net.IPAddress.Parse(Global.setinfo.paymentDeviceInfo.ip);
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            remoteEP = new IPEndPoint(ipAdd, Global.setinfo.paymentDeviceInfo.port);
        }
        SendRequest();
        socketObj = Instantiate(socketPrefab);
        //socketObj = GameObject.Find("SocketIO");
        socket1 = socketObj.GetComponent<SocketIOComponent>();
        socket1.On("open", socketOpen);
        socket1.On("createOrder", createOrder);
        socket1.On("new_notification", new_notification);
        socket1.On("reloadPayment", reload);
        socket1.On("error", socketError);
        socket1.On("close", socketClose);
    }

    public void reload(SocketIOEvent e)
    {
        StartCoroutine(GotoScene("payment_cancel"));
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
        StartCoroutine(GotoScene("payment"));
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

    void sendCheckSdateApi()
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", ctime.Year, ctime.Month, ctime.Day));
        WWW www = new WWW(Global.api_url + Global.check_sdate_api, form);
        StartCoroutine(CheckSdateProcess(www));
    }

    IEnumerator checkSdate()
    {
        while (true)
        {
            ctime = Global.GetSdate(false);
            if (Global.old_day < ctime)
            {
                sendCheckSdateApi();
            }
            yield return new WaitForSeconds(Global.checktime);
        }
    }

    IEnumerator CheckSdateProcess(WWW www)
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

    void SendRequest()
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("curdate", string.Format("{0:D4}-{1:D2}-{2:D2}", selected_date.Year, selected_date.Month, selected_date.Day));
        form.AddField("type", 1);//결제내역
        WWW www = new WWW(Global.api_url + Global.get_paymentlist_api, form);
        StartCoroutine(LoadInfo(www));
    }

    public void onSelDay()
    {
        if (selDayPopup.activeSelf)
        {
            selDayPopup.SetActive(false);
        }
        else
        {
            selDayPopup.SetActive(true);
            selMonthTxt.text = string.Format("{0:D4}.{1:D2}", Year, Month);
            LoadDays();
        }
    }

    void LoadDays()
    {
        int freedays = 0;
        DateTime cur_date = new DateTime(Year, Month, 1);
        switch (cur_date.DayOfWeek)
        {
            case DayOfWeek.Sunday: freedays = 0; break;
            case DayOfWeek.Monday: freedays = 1; break;
            case DayOfWeek.Tuesday: freedays = 2; break;
            case DayOfWeek.Wednesday: freedays = 3; break;
            case DayOfWeek.Thursday: freedays = 4; break;
            case DayOfWeek.Friday: freedays = 5; break;
            case DayOfWeek.Saturday: freedays = 6; break;
        }
        int daysCnt = DateTime.DaysInMonth(cur_date.Year, cur_date.Month);
        for (int i = 0; i < 42; i++)
        {
            if (i < freedays)
            {
                selDayPopup.transform.Find("background/days").gameObject.transform.GetChild(i).Find("day").GetComponent<Text>().text = "";
                selDayPopup.transform.Find("background/days").gameObject.transform.GetChild(i).GetComponent<Image>().sprite = null;
                selDayPopup.transform.Find("background/days").gameObject.transform.GetChild(i).GetComponent<Button>().onClick.RemoveAllListeners();
            }
            else if (i < freedays + daysCnt)
            {
                selDayPopup.transform.Find("background/days").gameObject.transform.GetChild(i).Find("day").GetComponent<Text>().text = (i - freedays + 1).ToString();
                if (Year == selected_date.Year && Month == selected_date.Month && selected_date.Day == (i - freedays + 1))
                {
                    selDayPopup.transform.Find("background/days").gameObject.transform.GetChild(i).GetComponent<Image>().sprite = Resources.Load<Sprite>("seldate");
                }
                else
                {
                    selDayPopup.transform.Find("background/days").gameObject.transform.GetChild(i).GetComponent<Image>().sprite = null;
                }
                int sel_date = i - freedays + 1;
                selDayPopup.transform.Find("background/days").gameObject.transform.GetChild(i).GetComponent<Button>().onClick.RemoveAllListeners();
                selDayPopup.transform.Find("background/days").gameObject.transform.GetChild(i).GetComponent<Button>().onClick.AddListener(delegate () { onSelectDay(sel_date); });
            }
            else
            {
                selDayPopup.transform.Find("background/days").gameObject.transform.GetChild(i).Find("day").GetComponent<Text>().text = "";
                selDayPopup.transform.Find("background/days").gameObject.transform.GetChild(i).GetComponent<Image>().sprite = null;
                selDayPopup.transform.Find("background/days").gameObject.transform.GetChild(i).GetComponent<Button>().onClick.RemoveAllListeners();
            }
        }
    }

    public void onSelectDay(int day)
    {
        Debug.Log(Year + ":" + Month + ":" + day);
        selected_date = new DateTime(Year, Month, day);
        int _y = 0;
        try
        {
            _y = int.Parse(selected_date.Year.ToString().Substring(2, 2));
        }
        catch (Exception ex)
        {

        }
        curDateTxt.text = string.Format("{0:D2}.{1:D2}.{2:D2}", _y, selected_date.Month, selected_date.Day);
        selDayPopup.SetActive(false);
        SendRequest();
    }

    public void onPreMonth()
    {
        if (Month > 1)
        {
            Month--;
        }
        else
        {
            Year--;
            Month = 12;
        }
        selMonthTxt.text = string.Format("{0:D4}.{1:D2}", Year, Month);
        LoadDays();
    }

    public void onNextMonth()
    {
        DateTime curDate = new DateTime(Year, Month, 1).AddMonths(1);
        Year = curDate.Year;
        Month = curDate.Month;
        selMonthTxt.text = string.Format("{0:D4}.{1:D2}", Year, Month);
        LoadDays();
    }

    IEnumerator LoadInfo(WWW www)
    {
        yield return www;
        while (paymentParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(paymentParent.transform.GetChild(0).gameObject));
        }
        while (paymentParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        while (paymentmenuParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(paymentmenuParent.transform.GetChild(0).gameObject));
        }
        while (paymentmenuParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        m_paymentObj.Clear();
        paymentList.Clear();
        m_paymentMenuObj.Clear();

        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            JSONNode plist = JSON.Parse(jsonNode["paymentlist"].ToString()/*.Replace("\"", "")*/);
            if (plist.Count > 0)
            {
                try
                {
                    first_payment_id = plist[0]["id"];
                }
                catch (Exception ex)
                {

                }
            }
            for (int i = 0; i < plist.Count; i++)
            {
                PaymentManageInfo pinfo = new PaymentManageInfo();
                pinfo.accept_no = plist[i]["accept_no"];
                pinfo.card_no = plist[i]["card_no"];
                pinfo.time = plist[i]["time"];
                pinfo.card_type = plist[i]["card_type"];
                pinfo.price = plist[i]["price"].AsInt;
                pinfo.table_name = plist[i]["table_name"];
                pinfo.tag_name = plist[i]["tag_name"];
                pinfo.type = plist[i]["type"];
                pinfo.id = plist[i]["id"];
                pinfo.device_type = plist[i]["device_type"].AsInt;
                pinfo.pay_time = plist[i]["pay_time"];
                pinfo.payment_type = plist[i]["pay_type"].AsInt;
                pinfo.menulist = new List<PaymentMenuManageInfo>();
                JSONNode mlist = JSON.Parse(plist[i]["menulist"].ToString());
                for (int j = 0; j < mlist.Count; j++)
                {
                    PaymentMenuManageInfo minfo = new PaymentMenuManageInfo();
                    minfo.menu_name = mlist[j]["menu_name"];
                    minfo.time = mlist[j]["time"];
                    minfo.pay_id = mlist[j]["pay_id"];
                    minfo.order_id = mlist[j]["order_id"];
                    minfo.amount = mlist[j]["amount"].AsInt;
                    minfo.price = mlist[j]["price"].AsInt;
                    pinfo.menulist.Add(minfo);
                }
                paymentList.Add(pinfo);
            }
            //UI에 추가
            for (int i = 0; i < paymentList.Count; i++)
            {
                GameObject tmp = Instantiate(paymentItem);
                tmp.transform.SetParent(paymentParent.transform);
                //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmp.transform.localScale = Vector3.one;
                try
                {
                    tmp.transform.Find("time").GetComponent<Text>().text = paymentList[i].time;
                    tmp.transform.Find("card").GetComponent<Text>().text = paymentList[i].card_type;
                    tmp.transform.Find("cardno").GetComponent<Text>().text = paymentList[i].card_no;
                    tmp.transform.Find("receptno").GetComponent<Text>().text = paymentList[i].accept_no;
                    tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(paymentList[i].price);
                    tmp.transform.Find("type").GetComponent<Text>().text = paymentList[i].type;
                    tmp.transform.Find("id").GetComponent<Text>().text = paymentList[i].id.ToString();
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    string _id = paymentList[i].id;
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { StartCoroutine(LoadMenuList(_id)); });
                }
                catch (Exception ex)
                {

                }
                m_paymentObj.Add(tmp);
            }

            if (m_paymentObj.Count > 0 && first_payment_id != "")
                StartCoroutine(LoadMenuList(first_payment_id));
        }
    }

    string tp = "";

    IEnumerator LoadMenuList(string id)
    {
        //UI 내역 초기화
        while (paymentmenuParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(paymentmenuParent.transform.GetChild(0).gameObject));
        }
        while (paymentmenuParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        m_paymentMenuObj.Clear();
        //선택된 테이블그룹 노란색으로.
        try
        {
            if(old_payment_no != "")
            {
                for(int i = 0; i < paymentParent.transform.childCount; i++)
                {
                    if(paymentParent.transform.GetChild(i).transform.Find("id").GetComponent<Text>().text == old_payment_no.ToString())
                    {
                        paymentParent.transform.GetChild(i).transform.Find("card").GetComponent<Text>().color = Color.white;
                        paymentParent.transform.GetChild(i).transform.Find("time").GetComponent<Text>().color = Color.white;
                        paymentParent.transform.GetChild(i).transform.Find("cardno").GetComponent<Text>().color = Color.white;
                        paymentParent.transform.GetChild(i).transform.Find("receptno").GetComponent<Text>().color = Color.white;
                        paymentParent.transform.GetChild(i).transform.Find("price").GetComponent<Text>().color = Color.white;
                        paymentParent.transform.GetChild(i).transform.Find("type").GetComponent<Text>().color = Color.white;
                        break;
                    }
                }
            }
            for(int i = 0; i < paymentParent.transform.childCount; i++)
            {
                if (paymentParent.transform.GetChild(i).Find("id").GetComponent<Text>().text == id.ToString())
                {
                    paymentParent.transform.GetChild(i).transform.Find("card").GetComponent<Text>().color = Global.selected_color;
                    paymentParent.transform.GetChild(i).transform.Find("time").GetComponent<Text>().color = Global.selected_color;
                    paymentParent.transform.GetChild(i).transform.Find("cardno").GetComponent<Text>().color = Global.selected_color;
                    paymentParent.transform.GetChild(i).transform.Find("receptno").GetComponent<Text>().color = Global.selected_color;
                    paymentParent.transform.GetChild(i).transform.Find("price").GetComponent<Text>().color = Global.selected_color;
                    paymentParent.transform.GetChild(i).transform.Find("type").GetComponent<Text>().color = Global.selected_color;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }

        old_payment_no = id;
        cur_payId = id;
        try
        {
            int index = -1;
            List<PaymentMenuManageInfo> mList = new List<PaymentMenuManageInfo>();
            for (int i = 0; i < paymentList.Count; i++)
            {
                if(paymentList[i].id == id)
                {
                    index = i;
                    tp = paymentList[i].type;
                    mList = paymentList[i].menulist;
                    break;
                }
            }
            //UI에 로딩
            for (int i = 0; i < mList.Count; i++)
            {
                GameObject tmp = Instantiate(paymentmenuItem);
                tmp.transform.SetParent(paymentmenuParent.transform);
                //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmp.transform.localScale = Vector3.one;
                try
                {
                    tmp.transform.Find("menu").GetComponent<Text>().text = mList[i].menu_name;
                    tmp.transform.Find("time").GetComponent<Text>().text = mList[i].time;
                    tmp.transform.Find("amount").GetComponent<Text>().text = mList[i].amount.ToString();
                    tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(mList[i].price);
                }
                catch (Exception ex)
                {

                }
                m_paymentMenuObj.Add(tmp);
            }
            menuSumTxt.text = Global.GetPriceFormat(paymentList[index].price);
            tableNameTxt.text = paymentList[index].table_name;
            tagNameTxt.text = paymentList[index].tag_name;
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    public void onCancelUsage()
    {
        //취소내역
        StartCoroutine(GotoScene("payment_cancel"));
    }

    public void onConfirmCancelPayment()
    {
        select_popup.SetActive(false);
        int device_type = 0;
        int price = 0;
        string appno = "";
        string payTime = "";
        for(int i = 0; i < paymentList.Count; i ++)
        {
            if(paymentList[i].id == cur_payId)
            {
                device_type = paymentList[i].device_type;
                price = paymentList[i].price;
                appno = paymentList[i].accept_no;
                payTime = paymentList[i].pay_time;
                break;
            }
        }
        if(device_type != 0)
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
        switch(device_type)
        {
            case 0://단말기 미사용
                {
                    CancelPay();
                    break;
                }
            case 1://KICC 카드
                {
                    _ = KICCApproval(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "D2", price.ToString(), "00", appno, payTime);
                    break;
                }
            case 2://KICC 현금 사용자
                {
                    _ = KICCApproval(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "CR", price.ToString(), "01", appno, payTime);
                    break;
                }
            case 3://KICC 현금 개인
                    _ = KICCApproval(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "CR", price.ToString(), "02", appno, payTime);                {
                    break;
                }
            case 4://KIS 카드
                {
                    _ = KISApprovalAsync(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "D2", price.ToString(), "00", appno, payTime);
                    break;
                }
            case 5://KIS 현금 (사용자, 카드)
                {
                    _ = KISApprovalAsync(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "CR", price.ToString(), "01", appno, payTime);
                    break;
                }
        }
    }

    public void CancelPay()
    {
        if (cancelPaymentFlag == 0)
        {
            WWWForm form = new WWWForm();
            form.AddField("pub_id", Global.userinfo.pub.id);
            form.AddField("pay_id", cur_payId);
            form.AddField("pos_no", Global.setinfo.pos_no);
            WWW www = new WWW(Global.api_url + Global.cancel_pay_api, form);
            StartCoroutine(cancelPaymentProcess(www));
        }
        else
        {
            err_str.text = "결제시 수동판매Draft 사용";
            err_popup.SetActive(true);
            if (progress_popup.activeSelf)
            {
                progress_popup.SetActive(false);
            }
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

    private async Task KISApprovalAsync(string ip, string port, string cmd, string amtstr, string div, string appno, string appdate, bool isReInvoide = false)
    {
        char bcc = Convert.ToChar(0x00);
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
                appno = rcvStr[8];
                if (isReInvoide)
                {
                    appno = rcvStr[8];
                    ChangePayInfo();
                }
                else
                {
                CancelPay();
                }
                Debug.Log("응답수신완료.");
            }
        }
        catch (Exception ex)
        {
            progress_popup.SetActive(false);
            StopCoroutine("checkPaymentResult");
            err_str.text = "신용카드 단말기 조작시 오류가 발생하였습니다.";
            err_popup.SetActive(true);
            Debug.Log(ex.Message);
        }
        socket.Close();
    }

    private async Task KICCApproval(string ip, string port, string cmd, string amtstr, string div, string appno, string appdate, bool isReInvoide = false)
    {
        ushort bcc;
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
                strSend += "30 31";
            }
            else if (strDiv == "02")
            {
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
                progress_popup.SetActive(false);
                StopCoroutine("checkPaymentResult");
                err_str.text = "신용카드 단말기 사용자 종료";
                err_popup.SetActive(true);
            }
            else
            {
                if (isReInvoide)
                {
                    appno = str.Substring(84, 12);
                    ChangePayInfo();
                }
                else
                {
                CancelPay();
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

    public void onCancelPayment()
    {
        if(tp == "선불충전")
        {
            err_popup.SetActive(true);
            err_str.text = "선불충전은 선불관리에서 처리하세요.";
            return;
        }
        if (tp == "예치금결제")
        {
            err_popup.SetActive(true);
            err_str.text = "예치금 적립을 위한 결제는 취소가 불가합니다.";
            return;
        }

        select_popup.SetActive(true);
        select_str.text = "선택한 결제를 취소하시겠습니까?";

        for (int i = 0; i < paymentList.Count; i++)
        {
            if (paymentList[i].id == cur_payId)
            {
                for (int j = 0; j < paymentList[i].menulist.Count; j++)
                {
                    WWWForm form = new WWWForm();
                    form.AddField("order_id", paymentList[i].menulist[j].order_id);
                    form.AddField("pos_type", 0);
                    WWW www = new WWW(Global.api_url + Global.check_ordertype_api, form);
                    StartCoroutine(checkOrdertypeProcess(www));
                }
            }
        }
    }

    IEnumerator cancelPaymentProcess(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if(jsonNode["suc"].AsInt == 1)
            {
                int remain_point = jsonNode["point_remain"].AsInt;
                if(remain_point < 0)
                {
                    is_remain_popup = true;
                    err_str.text = "결제 취소 후 포인트를 확인하세요.\n잔여 포인트 : " + Global.GetPriceFormat(remain_point);
                    err_popup.SetActive(true);
                }
                else
                {
                    if (Global.userinfo.pub.invoice_outtype != 0)
                    {
                        ReoutInvoice();
                    }
                    else
                    {
                        progress_popup.SetActive(false);
                        StopCoroutine("checkPaymentResult");
                        StartCoroutine(GotoScene("payment"));
                    }
                }
            }
            else
            {
                err_str.text = "결제 취소에 실패하였습니다.";
                err_popup.SetActive(true);
            }
        }
    }

    IEnumerator checkOrdertypeProcess(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                cancelPaymentFlag ++;

            }
            //else if (jsonNode["suc"].AsInt == 0)
            //{
            //    cancelPaymentFlag = cancelPaymentFlag;
            //}
        }
    }

    void ReoutInvoice()
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("pay_id", cur_payId);
        WWW www = new WWW(Global.api_url + Global.get_output_info_api, form);
        StartCoroutine(GetOutputInfoCancel(www));
    }

    IEnumerator GetOutputInfoCancel(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                Debug.Log(jsonNode);
                try
                {
                    int output_type = Global.userinfo.pub.invoice_outtype;
                    //영수증 출력
                    string printStr = "";
                    JSONNode paymenInfo = JSON.Parse(jsonNode["payment"].ToString()/*.Replace("\"", "")*/);
                    payment = new Payment();
                    printList.Clear();
                    payment.payment_type = paymenInfo["payment_type"].AsInt;
                    payment.credit_card_company = paymenInfo["credit_card_company"];
                    payment.credit_card_number = paymenInfo["credit_card_number"];
                    payment.installment_months = paymenInfo["installment_months"].AsInt;
                    payment.price = paymenInfo["price"].AsInt;
                    payment.payamt = paymenInfo["payamt"].AsInt;
                    payment.cutamt = paymenInfo["cutamt"].AsInt;
                    payment.reg_datetime = paymenInfo["reg_datetime"];
                    payment.custno = paymenInfo["custno"];
                    payment.custpoint = paymenInfo["custpoint"].AsInt;
                    payment.appno = paymenInfo["appno"];
                    payment.prepayamt = paymenInfo["prepayamt"].AsInt;

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
                    else if (output_type == 2)//영수증 (합산)
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

                    if (output_type == 1)
                    {
                        printStr = DocumentFactory.GetReceiptDetail(payment, printList, title: "[영수증(상세)]");
                    }
                    else if (output_type == 3)
                    {
                        printStr = DocumentFactory.GetReceiptSimple(payment, "", title: "[영수증(간단)]");
                    }
                    else if (output_type == 2)
                    {
                        printStr = DocumentFactory.GetReceiptDetail(payment, printList, title: "[영수증(합산)]");
                    }
                    Debug.Log(printStr);
                    byte[] sendData = NetUtils.StrToBytes(printStr);
                    Socket_Send(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), sendData);
                    progress_popup.SetActive(false);
                    StopCoroutine("checkPaymentResult");
                    StartCoroutine(GotoScene("payment"));
                }
                catch (Exception ex)
                {

                }
            }
        }
    }

    void ChangePayInfo()
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("pay_id", cur_payId);
        form.AddField("installment_months", installment_months);
        form.AddField("appno", appno);
        form.AddField("device_type", device_type);
        WWW www = new WWW(Global.api_url + Global.change_pay_info_api, form);
        StartCoroutine(onChangePayInfo(www));
    }

    IEnumerator onChangePayInfo(WWW www)
    {
        yield return www;
        progress_popup.SetActive(false);
        StopCoroutine("checkPaymentResult");
        if (www.error == null)
        {
            StartCoroutine(GotoScene("payment"));
        }
    }

    public void onCloseInvoice()
    {
        invoicePopup.SetActive(false);
    }

    public void onConfirmInvoice()
    {
        //if(Global.setinfo.paymentDeviceInfo.port == 0 || Global.setinfo.paymentDeviceInfo.ip == "" || Global.setinfo.paymentDeviceInfo.ip == null)
        //{
        //    err_str.text = "결제단말기 세팅을 진행하세요.";
        //    err_popup.SetActive(true);
        //    return;
        //}
        //if(Global.setinfo.paymentDeviceInfo.type == 0)
        //{
        //    err_str.text = "결제단말기 통신방식을 ip로 설정하세요.";
        //    err_popup.SetActive(true);
        //    return;
        //}
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("pay_id", cur_payId);
        WWW www = new WWW(Global.api_url + Global.get_output_info_api, form);
        StartCoroutine(GetOutputInfo(www));
    }

    public void onReOutInvoice()
    {
        //영수증 재발행
        invoicePopup.SetActive(true);
    }

    IEnumerator GetOutputInfo(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if(jsonNode["suc"].AsInt == 1)
            {
                Debug.Log(jsonNode);
                try
                {
                    int output_type = invoiceItem.value;
                    //영수증 출력
                    string printStr = "";
                    JSONNode paymenInfo = JSON.Parse(jsonNode["payment"].ToString()/*.Replace("\"", "")*/);
                    payment = new Payment();
                    printList.Clear();
                    payment.payment_type = paymenInfo["payment_type"].AsInt;
                    payment.credit_card_company = paymenInfo["credit_card_company"];
                    payment.credit_card_number = paymenInfo["credit_card_number"];
                    payment.installment_months = paymenInfo["installment_months"].AsInt;
                    payment.price = paymenInfo["price"].AsInt;
                    payment.payamt = paymenInfo["payamt"].AsInt;
                    payment.cutamt = paymenInfo["cutamt"].AsInt;
                    payment.reg_datetime = paymenInfo["reg_datetime"];
                    payment.custno = paymenInfo["custno"];
                    payment.custpoint = paymenInfo["custpoint"].AsInt;
                    payment.appno = paymenInfo["appno"];
                    payment.prepayamt = paymenInfo["prepayamt"].AsInt;

                    if (output_type == 0)//영수증 (상세)
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
                    else if (output_type == 1)//영수증 (합산)
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

                    if (output_type == 0)
                    {
                        printStr = DocumentFactory.GetReceiptDetail(payment, printList, title: "[영수증(상세)]");
                    }
                    else if (output_type == 2)
                    {
                        printStr = DocumentFactory.GetReceiptSimple(payment, "", title: "[영수증(간단)]");
                    }
                    else if (output_type == 1)
                    {
                        printStr = DocumentFactory.GetReceiptDetail(payment, printList, title: "[영수증(합산)]");
                    }
                    Debug.Log(printStr);
                    byte[] sendData = NetUtils.StrToBytes(printStr);
                    Socket_Send(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), sendData);
                    invoicePopup.SetActive(false);
                }
                catch (Exception ex)
                {

                }
            }
        }
    }

    public void onInvoice()
    {
        //현금영수증
        moneyPopup.SetActive(true);
    }

    public void onCloseMoneyInvoice()
    {
        moneyPopup.SetActive(false);
    }

    public void onConfirmMoneyInvoice()
    {
        if(cur_payId == "" || cur_payId == null)
        {
            err_str.text = "현금영수증 처리를 진행할 결제를 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
        int pay_price = 0;
        for(int i = 0; i < paymentList.Count; i ++)
        {
            if(paymentList[i].id == cur_payId)
            {
                if (paymentList[i].payment_type == 1)//카드결제
                {
                    err_str.text = "현금영수증 처리는 현금 결제시에만 가능합니다.";
                    err_popup.SetActive(true);
                    return;
                }
                else//현금결제
                {
                    if (paymentList[i].device_type != 0)
                    {
                        err_str.text = "현금영수증 처리를 완료한 결제입니다.";
                        err_popup.SetActive(true);
                        return;
                    }
                    else
                    {
                        pay_price = paymentList[i].price;
                    }
                }
            }
        }
        if(pay_price == 0)
        {
            err_str.text = "결제금액을 확인하세요.";
            err_popup.SetActive(true);
            return;
        }

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
        else
        {
            progress_popup.SetActive(true);
            StartCoroutine(checkPaymentResult());
            if (person.isOn)
            {
                //개인
                if (Global.setinfo.paymentDeviceInfo.cat == 1)//kis
                {
                    installment_months = 0;
                    device_type = 5;
                    _ = KISApprovalAsync(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "CC", pay_price.ToString(), "02", "", "", true);
                }
                else//kicc
                {
                    installment_months = 0;
                    device_type = 3;
                    _ = KICCApproval(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "CC", pay_price.ToString(), "02", "", "", true);
                }
        }
        else
        {
            //사업자
                if (Global.setinfo.paymentDeviceInfo.cat == 1)//kis
                {
                    installment_months = 1;
                    device_type = 5;
                    _ = KISApprovalAsync(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "CC", pay_price.ToString(), "01", "", "", true);  
                }
                else//kicc
                {
                    installment_months = 1;
                    device_type = 2;
                    _ = KICCApproval(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), "CC", pay_price.ToString(), "01", "", "", true);
                }
            }
        }
    }

    public void onBack()
    {
        StartCoroutine(GotoScene("main"));
    }

    IEnumerator Destroy_Object(GameObject obj)
    {
        DestroyImmediate(obj);
        yield return null;
    }

    public void closeErrPopup()
    {
        err_popup.SetActive(false);
        select_popup.SetActive(false);
        if(is_remain_popup)
        {
            if (Global.userinfo.pub.invoice_outtype != 0)
            {
                ReoutInvoice();
            }
            else
            {
                progress_popup.SetActive(false);
                StopCoroutine("checkPaymentResult");
                StartCoroutine(GotoScene("payment"));
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    float time = 0f;
    private bool is_remain_popup = false;

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
