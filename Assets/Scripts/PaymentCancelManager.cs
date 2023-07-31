using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SimpleJSON;
using System;
using SocketIO;
using System.Threading.Tasks;

public class PaymentCancelManager : MonoBehaviour
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
    public GameObject invoicePopup;
    public Dropdown invoiceItem;
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
    string cur_payType = "";

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
        StartCoroutine(GotoScene("payment_cancel"));
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
        form.AddField("type", 0);//취소내역
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
                    cur_payType = plist[0]["type"];
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
                pinfo.menulist = new List<PaymentMenuManageInfo>();
                JSONNode mlist = JSON.Parse(plist[i]["menulist"].ToString());
                for (int j = 0; j < mlist.Count; j++)
                {
                    PaymentMenuManageInfo minfo = new PaymentMenuManageInfo();
                    minfo.menu_name = mlist[j]["menu_name"];
                    minfo.time = mlist[j]["time"];
                    minfo.pay_id = mlist[j]["pay_id"];
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
                    string _tp = paymentList[i].type;
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { StartCoroutine(LoadMenuList(_id, _tp)); });
                }
                catch (Exception ex)
                {

                }
                m_paymentObj.Add(tmp);
            }

            if (m_paymentObj.Count > 0 && first_payment_id != "")
                StartCoroutine(LoadMenuList(first_payment_id, cur_payType));
        }
    }

    IEnumerator LoadMenuList(string id, string type)
    {
        //UI 내역 초기화
        cur_payId = id;
        cur_payType = type;
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
            for (int i = 0; i < paymentParent.transform.childCount; i++)
            {
                if (paymentParent.transform.GetChild(i).transform.Find("id").GetComponent<Text>().text == id.ToString())
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

        }

        old_payment_no = id;
        try
        {
            int index = -1;
            List<PaymentMenuManageInfo> mList = new List<PaymentMenuManageInfo>();
            for (int i = 0; i < paymentList.Count; i++)
            {
                if (paymentList[i].id == id)
                {
                    index = i;
                    mList = paymentList[i].menulist; break;
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

        }
    }

    public void onPaymentUsage()
    {
        //결제내역
        StartCoroutine(GotoScene("payment"));
    }

    public void onRepay()
    {
        //재결제
        if (cur_payType == "재결제")
        {
            err_popup.SetActive(true);
            err_str.text = "재결제 처리된 취소건입니다.";
            return;
        }
        if (cur_payType == "선결제")
        {
            err_popup.SetActive(true);
            err_str.text = "선결제 취소건은 재결제가 불가합니다. 테이블에서 다시 선결제를 진행해주세요.";
            return;
        }
        if (cur_payType == "유통")
        {
            err_popup.SetActive(true);
            err_str.text = "유통전용 상품은 재결제가 불가합니다.\n신규 상품으로 등록해주세요.";
            return;
        }
        Global.cur_pay_id = cur_payId;
        StartCoroutine(GotoScene("repayMenuOrder"));
    }

    public void onConfirmInvoice()
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

    public void onCloseInvoice()
    {
        invoicePopup.SetActive(false);
    }

    IEnumerator GetOutputInfo(WWW www)
    {
        yield return www;
        if (www.error == null)
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
                    Socket_Send(Global.setinfo.paymentDeviceInfo.ip.Trim(), Global.setinfo.paymentDeviceInfo.port.ToString(), sendData);
                    invoicePopup.SetActive(false);
                }
                catch (Exception ex)
                {

                }
            }
        }
    }

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
