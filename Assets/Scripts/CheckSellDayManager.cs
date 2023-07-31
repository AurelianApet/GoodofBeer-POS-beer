using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using SimpleJSON;
using SocketIO;
using System.Threading.Tasks;

public class CheckSellDayManager : MonoBehaviour
{
    public GameObject selDayPopup;
    public Text selDayTxt;
    public Text selMonthTxt;
    public Text payCntTxt;//결제건수
    public Text payPriceTxt;//결제금액
    public Text cardTxt;//카드
    public Text moneyTxt;//현금
    public Text orderPriceTxt;//주문금액
    public Text pointTxt;//할인/포인트
    public Text serviceTxt;//서비스
    public Text deadpriceTxt;//절사금액
    public Text realSelPriceTxt;//실매출액
    public Text orderSumTxt;
    public GameObject menuParent;
    public GameObject menuItem;
    public GameObject err_popup;
    public Text err_str;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;

    int Year;
    int Month;
    int Day;
    CheckSellDayInfo csInfo = new CheckSellDayInfo();
    List<CheckSellMenuInfo> csMenuList = new List<CheckSellMenuInfo>();
    List<GameObject> m_menuObj = new List<GameObject>();

    DateTime selected_date;
    bool is_socket_open = false;

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
        selDayTxt.text = string.Format("{0:D2}.{1:D2}.{2:D2}", _y, Month, Day);
        SendRequest();
        socketObj = Instantiate(socketPrefab);
        //socketObj = GameObject.Find("SocketIO");
        socket = socketObj.GetComponent<SocketIOComponent>();
        socket.On("open", socketOpen);
        socket.On("createOrder", createOrder);
        socket.On("new_notification", new_notification);
        socket.On("reload", reload);
        socket.On("reloadPayment", reload);
        socket.On("error", socketError);
        socket.On("close", socketClose);
    }

    public void new_notification(SocketIOEvent e)
    {
        Global.alarm_cnt++;
    }

    string rcvData = "";
    string comRcvStr;                               // serial port receive string
    byte[] Ack = new byte[] { 0x06 };
    bool StxRcv, EtxRcv, EotRcv, EnqRcv, AckRcv, NakRcv, DleRcv;    // 수신 여부 체크값
    bool CrRcv;
    char FS = Convert.ToChar(0x1c);
    bool bWait = false;
    byte[] comRcvByte = new byte[1024];             // serial port receive string
    int rcvCnt = 0;
    IPAddress ipAdd;
    Socket socket1;
    IPEndPoint remoteEP;

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

    private int Socket_Send(string ip, string port, byte[] sendData)
    {
        // Data buffer for incoming data.  
        byte[] bytes = new Byte[1024];
        try
        {
            ipAdd = System.Net.IPAddress.Parse(ip);
            socket1 = new Socket(SocketType.Stream, ProtocolType.Tcp);
            remoteEP = new IPEndPoint(ipAdd, int.Parse(port));
            socket1.Connect(remoteEP);
            socket1.SendTimeout = 300;
            socket1.Send(sendData);
            byte[] data = new byte[1024];
            if (sendData[0] != 0x02)
            {
                Task.Delay(200).Wait();
                socket1.Close();
                return 0;
            }
            if (sendData[0] != Ack[0])
            {
                int rlen = socket1.Receive(data, data.Length, SocketFlags.None);
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

    public void reload(SocketIOEvent e)
    {
        StartCoroutine(GotoScene("checkSellperday"));
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
        StartCoroutine(GotoScene("checkSellperday"));
    }

    public void socketOpen(SocketIOEvent e)
    {
        if (is_socket_open)
            return;
        is_socket_open = true;
        string data = "{\"pub_id\":\"" + Global.userinfo.pub.id + "\"," +
            "\"no\":\"" + Global.setinfo.pos_no + "\"}";
        Debug.Log(data);
        socket.Emit("posSetInfo", JSONObject.Create(data));
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

    void SendRequest()
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("curdate", string.Format("{0:D4}-{1:D2}-{2:D2}", selected_date.Year, selected_date.Month, selected_date.Day));
        WWW www = new WWW(Global.api_url + Global.get_day_api, form);
        StartCoroutine(GetDayInfo(www));
    }

    // Update is called once per frame
    void Update()
    {
        
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
                if(Year == selected_date.Year && Month == selected_date.Month && selected_date.Day == (i - freedays + 1))
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

    void onSelectDay(int day)
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
        selDayTxt.text = string.Format("{0:D2}.{1:D2}.{2:D2}", _y, selected_date.Month, selected_date.Day);
        selDayPopup.SetActive(false);
        SendRequest();
    }

    IEnumerator GetDayInfo(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                csInfo.card = jsonNode["card"].AsInt;
                csInfo.cutPrice = jsonNode["cutPrice"].AsInt;
                csInfo.money = jsonNode["money"].AsInt;
                csInfo.orderPrice = jsonNode["orderPrice"].AsInt;
                csInfo.payCnt = jsonNode["payCnt"].AsInt;
                csInfo.payPrice = jsonNode["payPrice"].AsInt;
                csInfo.point = jsonNode["point"].AsInt;
                csInfo.sellPrice = jsonNode["sellPrice"].AsInt;
                csInfo.service = jsonNode["service"].AsInt;
                csMenuList.Clear();
                JSONNode cs_list = JSON.Parse(jsonNode["orderlist"].ToString()/*.Replace("\"", "")*/);
                for(int i = 0; i < cs_list.Count; i++)
                {
                    CheckSellMenuInfo cmInfo = new CheckSellMenuInfo();
                    cmInfo.amount = cs_list[i]["amount"].AsInt;
                    cmInfo.category_name = cs_list[i]["category_name"];
                    cmInfo.menu_name = cs_list[i]["menu_name"];
                    cmInfo.price = cs_list[i]["price"].AsInt;
                    cmInfo.service_cnt = cs_list[i]["service_cnt"].AsInt;
                    cmInfo.unit_price = cs_list[i]["unit_price"].AsInt;
                    csMenuList.Add(cmInfo);
                }
                StartCoroutine(UpdateUI());
            }
        }
    }

    IEnumerator Destroy_Object(GameObject obj)
    {
        DestroyImmediate(obj);
        yield return null;
    }

    IEnumerator UpdateUI()
    {
        payCntTxt.text = Global.GetPriceFormat(csInfo.payCnt);
        payPriceTxt.text = Global.GetPriceFormat(csInfo.payPrice);
        cardTxt.text = Global.GetPriceFormat(csInfo.card);
        moneyTxt.text = Global.GetPriceFormat(csInfo.money);
        orderPriceTxt.text = Global.GetPriceFormat(csInfo.orderPrice);
        pointTxt.text = Global.GetPriceFormat(csInfo.point);
        serviceTxt.text = Global.GetPriceFormat(csInfo.service);
        deadpriceTxt.text = Global.GetPriceFormat(csInfo.cutPrice);
        realSelPriceTxt.text = Global.GetPriceFormat(csInfo.sellPrice);
        m_menuObj.Clear();
        while (menuParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(menuParent.transform.GetChild(0).gameObject));
        }
        while (menuParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }

        int sumPrice = 0;
        for (int i = 0; i < csMenuList.Count; i++)
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
            try
            {
                tmp.transform.Find("no").GetComponent<Text>().text = csMenuList[i].category_name;
                tmp.transform.Find("menu").GetComponent<Text>().text = csMenuList[i].menu_name;
                tmp.transform.Find("unit").GetComponent<Text>().text = Global.GetPriceFormat(csMenuList[i].unit_price);
                tmp.transform.Find("amount").GetComponent<Text>().text = Global.GetPriceFormat(csMenuList[i].amount);
                tmp.transform.Find("service").GetComponent<Text>().text = Global.GetPriceFormat(csMenuList[i].service_cnt);
                tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(csMenuList[i].price);
                sumPrice += csMenuList[i].price;
            }
            catch (Exception ex)
            {

            }
            m_menuObj.Add(tmp);
        }
        orderSumTxt.text = Global.GetPriceFormat(sumPrice);
    }

    public void onCloseSelDayPopup()
    {
        selDayPopup.SetActive(false);
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

    public void onMonthSell()
    {
        //월매출
        StartCoroutine(GotoScene("checkSellmonth"));
    }

    public void onPrepaySell()
    {
        //선불매출
        StartCoroutine(GotoScene("checkSellprepay"));
    }

    public void onOutput()
    {
        if (Global.setinfo.paymentDeviceInfo.port == 0 || Global.setinfo.paymentDeviceInfo.ip.Trim() == "" || Global.setinfo.paymentDeviceInfo.ip == null)
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

        string datetime = selected_date.Year + "년 " + (selected_date.Month < 10 ? "0" + selected_date.Month.ToString() : selected_date.Month.ToString()) + "월 " + (selected_date.Day < 10 ? "0" + selected_date.Day.ToString() : selected_date.Day.ToString()) + "일";
        string printStr = DocumentFactory.GetPrintDayDoc(csInfo, datetime);
        Debug.Log(printStr);
        byte[] sendData = NetUtils.StrToBytes(printStr);
        Socket_Send(Global.setinfo.paymentDeviceInfo.ip.Trim(), Global.setinfo.paymentDeviceInfo.port.ToString(), sendData);
    }

    public void onBack()
    {
        StartCoroutine(GotoScene("main"));
    }

    public void closeErrPopup()
    {
        err_popup.SetActive(false);
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
        if (socket != null)
        {
            socket.Close();
            socket.OnDestroy();
            socket.OnApplicationQuit();
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
        if (socket != null)
        {
            socket.Close();
            socket.OnDestroy();
            socket.OnApplicationQuit();
        }
    }
}
