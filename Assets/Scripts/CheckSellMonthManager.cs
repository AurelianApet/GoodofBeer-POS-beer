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

public class CheckSellMonthManager : MonoBehaviour
{
    public GameObject detailPopup;
    public Text payCntTxt;//결제건수
    public Text payPriceTxt;//결제금액
    public Text cardTxt;//카드
    public Text moneyTxt;//현금
    public Text orderPriceTxt;//주문금액
    public Text pointTxt;//할인/포인트
    public Text serviceTxt;//서비스
    public Text deadpriceTxt;//절사금액
    public Text realSelPriceTxt;//실매출액
    public Text selMonthTxt;
    public GameObject weekSumObj;
    public GameObject daySumObj;
    public GameObject totalSumObj;
    public Text totalSumPriceTxt;
    public Text curDateTxt;
    public GameObject err_popup;
    public Text err_str;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;

    int Year;
    int Month;
    int Day;

    List<CheckSellDayInfo> week_csInfo = new List<CheckSellDayInfo>();//주간 합계
    List<CheckSellDayInfo> day_csInfo = new List<CheckSellDayInfo>();//요일 합계
    CheckSellDayInfo total_csinfo = new CheckSellDayInfo();//총 합계
    bool is_socket_open = false;

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(checkSdate());
        DateTime sdate = Global.GetSdate();
        Year = sdate.Year;
        Month = sdate.Month;
        Day = sdate.Day;
        selMonthTxt.text = string.Format("{0:D4}.{1:D2}", Year, Month);
        LoadDays();
        socketObj = Instantiate(socketPrefab);
        //socketObj = GameObject.Find("SocketIO");
        socket = socketObj.GetComponent<SocketIOComponent>();
        socket.On("open", socketOpen);
        socket.On("createOrder", createOrder);
        socket.On("new_notification", new_notification);
        socket.On("reloadPayment", reload);
        socket.On("reload", reload);
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
        StartCoroutine(GotoScene("checkSellmonth"));
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
        StartCoroutine(GotoScene("checkSellmonth"));
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

    void LoadDays()
    {
        WWWForm form = new WWWForm();
        form.AddField("curmonth", string.Format("{0:D4}-{1:D2}", Year, Month));
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("curday", string.Format("{0:D4}-{1:D2}-{2:D2}", Global.old_day.Year, Global.old_day.Month, Global.old_day.Day));
        WWW www = new WWW(Global.api_url + Global.get_month_api, form);
        StartCoroutine(GetMonthInfo(www));
    }

    IEnumerator GetMonthInfo(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            if (jsonNode["suc"].AsInt == 1)
            {
                JSONNode dayinfo = JSON.Parse(jsonNode["dayInfo"].ToString());
                List<DayInfo> mInfo = new List<DayInfo>();
                for (int i = 0; i < dayinfo.Count; i++)
                {
                    DayInfo d = new DayInfo();
                    d.day = dayinfo[i]["day"].AsInt;
                    d.sum = dayinfo[i]["day_sum"].AsInt;
                    mInfo.Add(d);
                }
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
                        GameObject.Find("Canvas/field/days").transform.GetChild(i).Find("day").GetComponent<Text>().text = "";
                        GameObject.Find("Canvas/field/days").transform.GetChild(i).Find("price").GetComponent<Text>().text = "";
                        GameObject.Find("Canvas/field/days").transform.GetChild(i).GetComponent<Button>().onClick.RemoveAllListeners();
                    }
                    else if (i < freedays + daysCnt)
                    {
                        GameObject.Find("Canvas/field/days").transform.GetChild(i).Find("day").GetComponent<Text>().text = (i - freedays + 1).ToString();
                        for (int j = 0; j < mInfo.Count; j++)
                        {
                            if (mInfo[j].day == i - freedays + 1)
                            {
                                GameObject.Find("Canvas/field/days").transform.GetChild(i).Find("price").GetComponent<Text>().text = Global.GetPriceFormat(mInfo[j].sum);
                            }
                        }
                        int sel_date = i - freedays;
                        GameObject.Find("Canvas/field/days").transform.GetChild(i).GetComponent<Button>().onClick.RemoveAllListeners();
                        GameObject.Find("Canvas/field/days").transform.GetChild(i).GetComponent<Button>().onClick.AddListener(delegate () { onSelectDay(sel_date); });
                    }
                    else
                    {
                        GameObject.Find("Canvas/field/days").transform.GetChild(i).Find("day").GetComponent<Text>().text = "";
                        GameObject.Find("Canvas/field/days").transform.GetChild(i).Find("price").GetComponent<Text>().text = "";
                        GameObject.Find("Canvas/field/days").transform.GetChild(i).GetComponent<Button>().onClick.RemoveAllListeners();
                    }
                }
                week_csInfo.Clear();
                JSONNode weekSum = JSON.Parse(jsonNode["weekSum"].ToString());
                for(int i = 0; i < weekSumObj.transform.childCount; i++)
                {
                    try
                    {
                        weekSumObj.transform.GetChild(i).Find("price").GetComponent<Text>().text = Global.GetPriceFormat(weekSum[i]["sumPrice"].AsInt);
                        CheckSellDayInfo cinfo = new CheckSellDayInfo();
                        cinfo.card = weekSum[i]["card"].AsInt;
                        cinfo.cutPrice = weekSum[i]["cutPrice"].AsInt;
                        cinfo.money = weekSum[i]["money"].AsInt;
                        cinfo.orderPrice = weekSum[i]["orderPrice"].AsInt;
                        cinfo.payCnt = weekSum[i]["payCnt"].AsInt;
                        cinfo.payPrice = weekSum[i]["payPrice"].AsInt;
                        cinfo.point = weekSum[i]["point"].AsInt;
                        cinfo.sellPrice = weekSum[i]["sellPrice"].AsInt;
                        cinfo.service = weekSum[i]["service"].AsInt;
                        week_csInfo.Add(cinfo);
                        weekSumObj.transform.GetChild(i).GetComponent<Button>().onClick.RemoveAllListeners();
                        int sel_week = i + 1;
                        weekSumObj.transform.GetChild(i).GetComponent<Button>().onClick.AddListener(delegate () { onSelectWeek(sel_week); });
                    }catch(Exception ex)
                    {

                    }
                }
                day_csInfo.Clear();
                JSONNode daySum = JSON.Parse(jsonNode["daySum"].ToString());
                for (int i = 0; i < daySumObj.transform.childCount; i++)
                {
                    try
                    {
                        daySumObj.transform.GetChild(i).Find("price").GetComponent<Text>().text = Global.GetPriceFormat(daySum[i]["price"].AsInt);
                        CheckSellDayInfo cinfo = new CheckSellDayInfo();
                        cinfo.card = daySum[i]["card"].AsInt;
                        cinfo.cutPrice = daySum[i]["cutPrice"].AsInt;
                        cinfo.money = daySum[i]["money"].AsInt;
                        cinfo.orderPrice = daySum[i]["orderPrice"].AsInt;
                        cinfo.payCnt = daySum[i]["payCnt"].AsInt;
                        cinfo.payPrice = daySum[i]["payPrice"].AsInt;
                        cinfo.point = daySum[i]["point"].AsInt;
                        cinfo.sellPrice = daySum[i]["sellPrice"].AsInt;
                        cinfo.service = daySum[i]["service"].AsInt;
                        day_csInfo.Add(cinfo);
                        int sel_week = i + 1;
                        daySumObj.transform.GetChild(i).GetComponent<Button>().onClick.RemoveAllListeners();
                        daySumObj.transform.GetChild(i).GetComponent<Button>().onClick.AddListener(delegate () { onSelectDayofWeek(sel_week); });
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex);
                    }
                }
                totalSumObj.transform.GetComponent<Button>().onClick.RemoveAllListeners();
                totalSumPriceTxt.text = Global.GetPriceFormat(jsonNode["total_sum"].AsInt);
                total_csinfo.card = jsonNode["card"].AsInt;
                total_csinfo.cutPrice = jsonNode["cutPrice"].AsInt;
                total_csinfo.money = jsonNode["money"].AsInt;
                total_csinfo.orderPrice = jsonNode["orderPrice"].AsInt;
                total_csinfo.payCnt = jsonNode["payCnt"].AsInt;
                total_csinfo.payPrice = jsonNode["payPrice"].AsInt;
                total_csinfo.point = jsonNode["point"].AsInt;
                total_csinfo.sellPrice = jsonNode["sellPrice"].AsInt;
                total_csinfo.service = jsonNode["service"].AsInt;
                totalSumObj.transform.GetComponent<Button>().onClick.AddListener(delegate () { onSelectTotal(); });
            }
        }
    }

    string [] dayOfWeek = {"일요일", "월요일", "화요일", "수요일", "목요일", "금요일", "토요일" };

    void onSelectDayofWeek(int weekIndex)
    {
        if (weekIndex < 1)
        {
            return;
        }
        detailPopup.SetActive(true);
        try
        {
            weekIndex -= 1;
            curDateTxt.text = string.Format("{0:D4}.{1:D2}", Year, Month) + " " + dayOfWeek[weekIndex] + " 합계";
            cardTxt.text = Global.GetPriceFormat(day_csInfo[weekIndex].card);
            deadpriceTxt.text = Global.GetPriceFormat(day_csInfo[weekIndex].cutPrice);
            moneyTxt.text = Global.GetPriceFormat(day_csInfo[weekIndex].money);
            orderPriceTxt.text = Global.GetPriceFormat(day_csInfo[weekIndex].orderPrice);
            payCntTxt.text = day_csInfo[weekIndex].payCnt.ToString();
            payPriceTxt.text = Global.GetPriceFormat(day_csInfo[weekIndex].payPrice);
            pointTxt.text = Global.GetPriceFormat(day_csInfo[weekIndex].point);
            realSelPriceTxt.text = Global.GetPriceFormat(day_csInfo[weekIndex].sellPrice);
            serviceTxt.text = Global.GetPriceFormat(day_csInfo[weekIndex].service);
        }
        catch (Exception ex)
        {

        }
    }

    void onSelectWeek(int weekIndex)
    {
        if(weekIndex < 1)
        {
            return;
        }
        detailPopup.SetActive(true);
        try
        {
            curDateTxt.text = string.Format("{0:D4}.{1:D2}", Year, Month) + " " + weekIndex + "주차 합계";
            cardTxt.text = Global.GetPriceFormat(week_csInfo[weekIndex - 1].card);
            deadpriceTxt.text = Global.GetPriceFormat(week_csInfo[weekIndex - 1].cutPrice);
            moneyTxt.text = Global.GetPriceFormat(week_csInfo[weekIndex - 1].money);
            orderPriceTxt.text = Global.GetPriceFormat(week_csInfo[weekIndex - 1].orderPrice);
            payCntTxt.text = week_csInfo[weekIndex - 1].payCnt.ToString();
            payPriceTxt.text = Global.GetPriceFormat(week_csInfo[weekIndex - 1].payPrice);
            pointTxt.text = Global.GetPriceFormat(week_csInfo[weekIndex - 1].point);
            realSelPriceTxt.text = Global.GetPriceFormat(week_csInfo[weekIndex - 1].sellPrice);
            serviceTxt.text = Global.GetPriceFormat(week_csInfo[weekIndex - 1].service);
        }
        catch(Exception ex)
        {

        }
    }

    void onSelectTotal()
    {
        curDateTxt.text = string.Format("{0:D4}.{1:D2}", Year, Month) + " 월 합계";
        cardTxt.text = Global.GetPriceFormat(total_csinfo.card);
        deadpriceTxt.text = Global.GetPriceFormat(total_csinfo.cutPrice);
        moneyTxt.text = Global.GetPriceFormat(total_csinfo.money);
        orderPriceTxt.text = Global.GetPriceFormat(total_csinfo.orderPrice);
        payCntTxt.text = total_csinfo.payCnt.ToString();
        payPriceTxt.text = Global.GetPriceFormat(total_csinfo.payPrice);
        pointTxt.text = Global.GetPriceFormat(total_csinfo.point);
        realSelPriceTxt.text = Global.GetPriceFormat(total_csinfo.sellPrice);
        serviceTxt.text = Global.GetPriceFormat(total_csinfo.service);
        detailPopup.SetActive(true);
    }

    void onSelectDay(int day)
    {
        day = day + 1;
        DateTime selected_date = new DateTime(Year, Month, day);
        detailPopup.SetActive(true);
        SendRequest(selected_date);
    }

    void SendRequest(DateTime selected_date)
    {
        curDateTxt.text = string.Format("{0:D4}.{1:D2}.{2:D2}", selected_date.Year, selected_date.Month, selected_date.Day);
        WWWForm form = new WWWForm();
        form.AddField("curdate", string.Format("{0:D4}-{1:D2}-{2:D2}", selected_date.Year, selected_date.Month, selected_date.Day));
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.get_monthday_api, form);
        StartCoroutine(GetDayInfo(www));
    }

    IEnumerator GetDayInfo(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                cardTxt.text = Global.GetPriceFormat(jsonNode["card"].AsInt);
                deadpriceTxt.text = Global.GetPriceFormat(jsonNode["cutPrice"].AsInt);
                moneyTxt.text = Global.GetPriceFormat(jsonNode["money"].AsInt);
                orderPriceTxt.text = Global.GetPriceFormat(jsonNode["orderPrice"].AsInt);
                payCntTxt.text = jsonNode["payCnt"];
                payPriceTxt.text = Global.GetPriceFormat(jsonNode["payPrice"].AsInt);
                pointTxt.text = Global.GetPriceFormat(jsonNode["point"].AsInt);
                realSelPriceTxt.text = Global.GetPriceFormat(jsonNode["sellPrice"].AsInt);
                serviceTxt.text = Global.GetPriceFormat(jsonNode["service"].AsInt);
            }
        }
    }

    public void onCloseDetailPopup()
    {
        detailPopup.SetActive(false);
    }

    public void onDaySell()
    {
        //일매출
        StartCoroutine(GotoScene("checkSellperday"));
    }

    public void onPrepaySell()
    {
        //선불매출
        StartCoroutine(GotoScene("checkSellprepay"));
    }

    public void onPrevMonth()
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

    // Update is called once per frame
    void Update()
    {
        
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
