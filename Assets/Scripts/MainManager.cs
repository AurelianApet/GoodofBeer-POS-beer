using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SocketIO;

public class MainManager : MonoBehaviour
{
    public GameObject table_group_Item;
    public GameObject table_group_parent;
    public GameObject table_item;
    public GameObject table_parent;
    public GameObject notice_parent;
    public GameObject notice_item;

    public Text market_name;
    public Text paid_cnt;
    public Text paid_price;
    public Text pending_cnt;
    public Text pending_price;
    public Text price;
    public Text set_market_name;
    public Text alarm_cntTxt;

    public GameObject noticePopup;
    public GameObject extraPopup;
    public GameObject exitPopup;
    public GameObject sellStatusPopup;
    public GameObject readTagPopup;

    public GameObject err_popup;
    public Text err_str;
    public GameObject select_popup;
    public Text select_str;

    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;

    GameObject[] m_tableGroupItem;
    GameObject[] m_tableItem;

    int total_table_group_cnt = -1;
    string first_table_group_id = "";
    string old_tg_no = "";
    bool loading = false;
    bool is_run = false;

    bool is_err_popup = false;
    string err_text = "";

    DateTime ctime;
    bool is_socket_open = false;

    void Awake()
    {
        ctime = Global.GetSdate(false);
        if (!Global.is_start)
        {
            sendCheckSdateApi(true);
            Global.is_start = true;
            is_run = true;
        }
        //StartCoroutine(checkSdate());
    }

    void sendCheckSdateApi(bool is_start = false)
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", ctime.Year, ctime.Month, ctime.Day));
        WWW www = new WWW(Global.api_url + Global.check_sdate_api, form);
        StartCoroutine(CheckSdateProcess(www, is_start));
    }

    IEnumerator checkSdate()
    {
        while (true)
        {
            if (is_run)
            {
                yield return new WaitForSeconds(Global.checktime);
                is_run = false;
            }
            ctime = Global.GetSdate(false);
            if (Global.old_day < ctime)
            {
                sendCheckSdateApi();
            }
            yield return new WaitForSeconds(Global.checktime);
        }
    }

    IEnumerator CheckSdateProcess(WWW www, bool is_start)
    {
        yield return www;
        if(www.error == null)
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
                Global.old_day = ctime.AddDays(-1);
            }
            if (is_start)
            {
                err_popup.SetActive(true);
                if (!Global.is_applied_state)
                {
                    ctime = ctime.AddDays(-1);
                }
                err_str.text = string.Format("{0:D4}-{1:D2}-{2:D2}", ctime.Year, ctime.Month, ctime.Day) + " 일 영업을 시작합니다.";
                yield break;
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

    // Start is called before the first frame update
    void Start()
    {
        Global.tableGroupList.Clear();
        StartCoroutine(LoadTableGroup());
        Debug.Log(Global.userinfo.pub.name);
        market_name.text = Global.userinfo.pub.name;
        set_market_name.text = Global.userinfo.pub.name;
        paid_cnt.text = Global.userinfo.pub.paid_cnt.ToString();
        paid_price.text = Global.GetPriceFormat(Global.userinfo.pub.paid_price);
        pending_cnt.text = Global.userinfo.pub.pending_cnt.ToString();
        pending_price.text = Global.GetPriceFormat(Global.userinfo.pub.pending_price);
        price.text = Global.GetPriceFormat(Global.userinfo.pub.price);
        alarm_cntTxt.text = Global.alarm_cnt.ToString();

        DateTime curtime = Global.GetSdate(false);
        curtime = curtime.AddDays(-1);
        WWWForm form = new WWWForm();
        form.AddField("curdate", string.Format("{0:D4}-{1:D2}-{2:D2}", curtime.Year, curtime.Month, curtime.Day));
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.modify_api, form);

        socketObj = Instantiate(socketPrefab);
        //socketObj = GameObject.Find("SocketIO");
        socket = socketObj.GetComponent<SocketIOComponent>();
        socket.On("open", socketOpen);
        socket.On("new_notification", new_notification);
        socket.On("createOrder", createOrder);
        socket.On("reload", reload);
        socket.On("reloadPayment", reload);
        socket.On("error", socketError);
        socket.On("close", socketClose);
        //socket.On("login", socketRun);
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
        StartCoroutine(GotoScene("main"));
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
        StartCoroutine(GotoScene("main"));
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

    public void new_notification(SocketIOEvent e)
    {
        Global.alarm_cnt++;
        alarm_cntTxt.text = Global.alarm_cnt.ToString();
        GameObject.Find("alarm").GetComponent<AudioSource>().Play();
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

    IEnumerator LoadNotice()
    {
        while (notice_parent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(notice_parent.transform.GetChild(0).gameObject));
        }
        while (notice_parent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        Global.noticeList.Clear();
        WWWForm form = new WWWForm();
        form.AddField("uid", Global.userinfo.id);
        WWW www = new WWW(Global.api_url + Global.get_noticelist_api, form);
        StartCoroutine(LoadNoticeList(www));
    }

    IEnumerator LoadNoticeList(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            if (jsonNode["suc"].AsInt == 1)
            {
                JSONNode tg_list = JSON.Parse(jsonNode["alarmlist"].ToString()/*.Replace("\"", "")*/);
                Global.alarm_cnt = jsonNode["alarmCnt"].AsInt;
                alarm_cntTxt.text = Global.alarm_cnt.ToString();
                for (int i = 0; i < tg_list.Count; i++)
                {
                    NoticeItemInfo noInfo = new NoticeItemInfo();
                    noInfo.tap_no = tg_list[i]["tap_no"].AsInt;
                    noInfo.datetime = tg_list[i]["datetime"];
                    noInfo.content = tg_list[i]["content"];
                    Global.noticeList.Add(noInfo);
                    GameObject m_noticeObj = Instantiate(notice_item);
                    m_noticeObj.transform.SetParent(notice_parent.transform);
                    //m_noticeObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                    //m_noticeObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                    //float left = 0;
                    //float right = 0;
                    //m_noticeObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                    //m_noticeObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                    //m_noticeObj.transform.localScale = Vector3.one;
                    m_noticeObj.transform.Find("time").GetComponent<Text>().text = noInfo.datetime.Substring(2);
                    m_noticeObj.transform.Find("tap").GetComponent<Text>().text = noInfo.tap_no.ToString();
                    m_noticeObj.transform.Find("content").GetComponent<Text>().text = noInfo.content;
                }
            }
            else
            {
                Global.alarm_cnt = 0;
                alarm_cntTxt.text = "0";
            }
        }
        else
        {
            Global.alarm_cnt = 0;
            alarm_cntTxt.text = "0";
        }
    }

    IEnumerator onConfirmNoticeProcess(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            Global.noticeList.Clear();
            Global.alarm_cnt = 0;
            alarm_cntTxt.text = "0";
        }
    }

    IEnumerator LoadTableGroup()
    {
        while (table_group_parent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(table_group_parent.transform.GetChild(0).gameObject));
        }
        while (table_group_parent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        Debug.Log("get table group list--");
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.get_table_group_api, form);
        StartCoroutine(GetTableGrouplist(www));
    }

    IEnumerator GetTableGrouplist(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            JSONNode tg_list = JSON.Parse(jsonNode["tablegrouplist"].ToString()/*.Replace("\"", "")*/);
            total_table_group_cnt = tg_list.Count;
            if (total_table_group_cnt > 0)
            {
                try
                {
                    first_table_group_id = tg_list[0]["id"];
                }
                catch (Exception ex)
                {

                }
            }
            for (int i = 0; i < total_table_group_cnt; i++)
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
                    Debug.Log(ex);
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
                        for(int k = 0; k < tagCnt; k ++)
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
                Global.tableGroupList.Add(tgInfo);
            }
            //UI에 추가
            m_tableGroupItem = new GameObject[total_table_group_cnt];
            for (int i = 0; i < total_table_group_cnt; i++)
            {
                m_tableGroupItem[i] = Instantiate(table_group_Item);
                m_tableGroupItem[i].transform.SetParent(table_group_parent.transform);

                //m_tableGroupItem[i].transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //m_tableGroupItem[i].transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //m_tableGroupItem[i].transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //m_tableGroupItem[i].transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //m_tableGroupItem[i].transform.localScale = Vector3.one;

                try
                {
                    m_tableGroupItem[i].transform.Find("name").GetComponent<Text>().text = Global.tableGroupList[i].name;
                    m_tableGroupItem[i].transform.Find("id").GetComponent<Text>().text = Global.tableGroupList[i].id.ToString();
                    string tg_id = Global.tableGroupList[i].id;
                    m_tableGroupItem[i].GetComponent<Button>().onClick.RemoveAllListeners();
                    m_tableGroupItem[i].GetComponent<Button>().onClick.AddListener(delegate () { StartCoroutine(LoadTableList(tg_id)); });
                }
                catch (Exception ex)
                {

                }
            }

            if (!loading && total_table_group_cnt > 0 && first_table_group_id != "")
                StartCoroutine(LoadTableList(first_table_group_id));
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

    bool is_loading_tablelist = false;

    IEnumerator LoadTableList(string id)
    {
        if (is_loading_tablelist)
        {
            yield break;
        }
        is_loading_tablelist = true;
        //UI 내역 초기화
        while (table_parent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(table_parent.transform.GetChild(0).gameObject));
        }
        while (table_parent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        //선택된 테이블그룹 노란색으로.
        try
        {
            for (int i = 0; i < table_group_parent.transform.childCount; i++)
            {
                if(table_group_parent.transform.GetChild(i).Find("id").GetComponent<Text>().text == old_tg_no.ToString())
                {
                    table_group_parent.transform.GetChild(i).transform.Find("name").GetComponent<Text>().color = Color.white;
                }
                if (table_group_parent.transform.GetChild(i).Find("id").GetComponent<Text>().text == id.ToString())
                {
                    table_group_parent.transform.GetChild(i).transform.Find("name").GetComponent<Text>().color = Global.selected_color;
                }
            }
        }
        catch (Exception ex)
        {

        }

        old_tg_no = id;
        List<TableInfo> tbList = new List<TableInfo>();
        for (int i = 0; i < Global.tableGroupList.Count; i++)
        {
            if (Global.tableGroupList[i].id == id)
            {
                tbList = Global.tableGroupList[i].tablelist;
                break;
            }
        }
        //UI에 로딩
        int tbCnt = tbList.Count;
        m_tableItem = new GameObject[tbCnt];
        loading = true;
        for (int i = 0; i < tbCnt; i++)
        {
            m_tableItem[i] = Instantiate(table_item);
            m_tableItem[i].transform.SetParent(table_parent.transform);
            //m_tableItem[i].transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
            //m_tableItem[i].transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
            //float left = 0;
            //float right = 0;
            //m_tableItem[i].transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
            //m_tableItem[i].transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
            //m_tableItem[i].transform.localScale = Vector3.one;
            try
            {
                m_tableItem[i].transform.Find("id").GetComponent<Text>().text = tbList[i].id.ToString();
                m_tableItem[i].transform.Find("name").GetComponent<Text>().text = tbList[i].name;
                if (tbList[i].is_blank == 0)
                {
                    m_tableItem[i].transform.Find("name").GetComponent<Text>().color = Global.selected_color;
                    m_tableItem[i].transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(tbList[i].order_price);
                    m_tableItem[i].transform.Find("price").GetComponent<Text>().color = Global.selected_color;
                    m_tableItem[i].transform.Find("cnt").GetComponent<Text>().text = tbList[i].taglist.Count.ToString();
                    m_tableItem[i].transform.Find("cnt").GetComponent<Text>().color = Global.selected_color;
                }
                string tid = tbList[i].id;
                m_tableItem[i].GetComponent<Button>().onClick.RemoveAllListeners();
                m_tableItem[i].GetComponent<Button>().onClick.AddListener(delegate () { onTable(id, tid); });
            }
            catch (Exception ex)
            {
                Debug.Log(ex);

            }
            yield return new WaitForFixedUpdate();
        }
        loading = false;
        is_loading_tablelist = false;
    }

    void onTable(string id, string tid)
    {
        Global.cur_tInfo.tgid = id;
        Global.cur_tInfo.tid = tid;
        for (int i = 0; i < Global.tableGroupList.Count; i++)
        {
            if (Global.tableGroupList[i].id == id)
            {
                Global.cur_tInfo.tgNo = i;
                for(int j = 0; j < Global.tableGroupList[i].tablelist.Count; j++)
                {
                    if(Global.tableGroupList[i].tablelist[j].id == tid)
                    {
                        Global.cur_tInfo.tNo = j;
                        Global.cur_tInfo.name = Global.tableGroupList[i].tablelist[j].name;
                        break;
                    }
                }
                break;
            }
        }
        if(Global.setinfo.tableMain == 0)
        {
            StartCoroutine(GotoScene("tableMenuOrder"));
        }
        else
        {
            StartCoroutine(GotoScene("tableUsage"));
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

    public void onSetting()
    {
        extraPopup.SetActive(true);
    }

    public void prepayManage()
    {
        //선불관리
        StartCoroutine(GotoScene("prepayTagUsage"));
    }

    public void onCloseSettingPopup()
    {
        extraPopup.SetActive(false);
    }

    public void onCloseTagSearchPopup()
    {
        readTagPopup.SetActive(false);
    }

    public void onSelTag()
    {
        readTagPopup.SetActive(true);
        readTagPopup.transform.Find("tag").GetComponent<InputField>().Select();
        readTagPopup.transform.Find("tag").GetComponent<InputField>().ActivateInputField();
        readTagPopup.transform.Find("tag").GetComponent<InputField>().text = "";
        readTagPopup.transform.Find("tag").GetComponent<InputField>().onValueChanged.AddListener((value) => {
                    checkTag(value);
                }
        );
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
        WWWForm form = new WWWForm();
        form.AddField("data", str);
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.find_tag_api, form);
        StartCoroutine(onCheckTagProcess(www));
        readTagPopup.transform.Find("tag").GetComponent<InputField>().text = "";
        send_time = 0f;
    }

    IEnumerator onCheckTagProcess(WWW www)
    {
        yield return www;
        Global.cur_tagInfo = new CurTagInfo();
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                int st = jsonNode["status"].AsInt;
                int is_p = jsonNode["period"].AsInt;
                switch (st)
                {
                    case 0:
                        {
                            err_str.text = "등록되지 않은 태그입니다.";
                            err_popup.SetActive(true);
                            break;
                        };
                    case 1:
                        {
                            err_str.text = "테이블에 등록된 TAG가 아닙니다.";
                            err_popup.SetActive(true);
                            break;
                        }
                    case 2:
                        {
                            Global.cur_tagInfo.table_name = jsonNode["table_name"];
                            Global.cur_tagInfo.charge = jsonNode["charge"].AsInt;
                            Global.cur_tagInfo.period = is_p;
                            Global.cur_tagInfo.reg_datetime = jsonNode["reg_datetime"];
                            Global.cur_tagInfo.remain = jsonNode["remain"].AsInt;
                            Global.cur_tagInfo.qrcode = jsonNode["qrcode"];
                            Global.cur_tagInfo.rfid = jsonNode["rfid"];
                            Global.cur_tagInfo.tag_id = jsonNode["id"];
                            Global.cur_tagInfo.tag_name = jsonNode["name"];
                            Global.cur_tagInfo.is_pay_after = jsonNode["is_pay_after"].AsInt;
                            if (Global.cur_tagInfo.is_pay_after == 0)
                            {
                                //선불태그
                                StartCoroutine(GotoScene("prepayTagUsage"));
                            }
                            else
                            {
                                bool is_found = false;
                                for (int i = 0; i < Global.tableGroupList.Count; i++)
                                {
                                    for (int j = 0; j < Global.tableGroupList[i].tablelist.Count; j++)
                                    {
                                        for (int k = 0; k < Global.tableGroupList[i].tablelist[j].taglist.Count; k++)
                                        {
                                            if (Global.tableGroupList[i].tablelist[j].taglist[k].id == Global.cur_tagInfo.tag_id)
                                            {
                                                Global.cur_tInfo.tgNo = i;
                                                Global.cur_tInfo.tNo = j;
                                                Global.cur_tInfo.name = Global.tableGroupList[i].tablelist[j].name;
                                                Global.cur_tInfo.tgid = Global.tableGroupList[i].id;
                                                Global.cur_tInfo.tid = Global.tableGroupList[i].tablelist[j].id;
                                                is_found = true;
                                                break;
                                            }
                                        }
                                        if (is_found)
                                            break;
                                    }
                                    if (is_found)
                                        break;
                                }
                                StartCoroutine(GotoScene("tableUsage"));
                            }
                            break;
                        };
                    case 3:
                        {
                            select_str.text = "분실된 TAG입니다. 회수 처리하시겠습니까?";
                            select_popup.SetActive(true);
                            select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
                            string _tagId = jsonNode["id"];
                            select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { onCancelTag(_tagId); });
                            break;
                        };
                    case 4:
                        {
                            err_str.text = "현재 셀프 이용 중인 TAG 입니다.";
                            err_popup.SetActive(true);
                            break;
                        }
                }
            }
            else
            {
                readTagPopup.SetActive(false);
                err_str.text = "이용할 수 없는 태그입니다.";
                err_popup.SetActive(true);
            }
        }
        else
        {
            readTagPopup.SetActive(false);
            err_str.text = "서버접속이 원활하지 않습니다.\n 후에 다시 시도해주세요.";
            err_popup.SetActive(true);
        }
    }

    void onCancelTag(string tagId)
    {
        WWWForm form = new WWWForm();
        form.AddField("tag_id", tagId);
        WWW www = new WWW(Global.api_url + Global.cancel_tag_api, form);
        StartCoroutine(onCancelTagProcess(www, tagId));
    }

    IEnumerator onCancelTagProcess(WWW www, string tagId)
    {
        yield return www;
        select_popup.SetActive(false);
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                Global.RemoveTag(tagId);
            }
        }
    }

    public void onCloseSelectPopup()
    {
        select_popup.SetActive(false);
    }

    public void onShowSellStatus()
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", ctime.Year, ctime.Month, ctime.Day));
        WWW www = new WWW(Global.api_url + Global.get_market_status_api, form);
        StartCoroutine(showMarketStatus(www));
        sellStatusPopup.SetActive(true);
    }

    IEnumerator showMarketStatus(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Global.userinfo.pub.paid_cnt = jsonNode["paid_cnt"].AsInt;
            Global.userinfo.pub.pending_cnt = jsonNode["pending_cnt"].AsInt;
            Global.userinfo.pub.paid_price = jsonNode["paid_price"].AsInt;
            Global.userinfo.pub.pending_price = jsonNode["pending_price"].AsInt;
            Global.userinfo.pub.price = jsonNode["price"].AsInt;
            paid_cnt.text = Global.userinfo.pub.paid_cnt.ToString();
            paid_price.text = Global.GetPriceFormat(Global.userinfo.pub.paid_price);
            pending_cnt.text = Global.userinfo.pub.pending_cnt.ToString();
            pending_price.text = Global.GetPriceFormat(Global.userinfo.pub.pending_price);
            price.text = Global.GetPriceFormat(Global.userinfo.pub.price);
        }
    }

    public void onConfirmSellStatusPopup()
    {
        sellStatusPopup.SetActive(false);
    }

    public void showNotice()
    {
        noticePopup.SetActive(true);
        StartCoroutine(LoadNotice());
    }

    public void onConfirmAlarmPopup()
    {
        //알람 처리
        WWWForm form = new WWWForm();
        form.AddField("uid", Global.userinfo.id);
        WWW www = new WWW(Global.api_url + Global.confirm_notice_api, form);
        StartCoroutine(onConfirmNoticeProcess(www));
        noticePopup.SetActive(false);
    }

    public void onExit()
    {
        exitPopup.SetActive(true);
    }

    public void onCancelExitPopup()
    {
        exitPopup.SetActive(false);
    }

    public void onConfirmExitPopup()
    {
        Application.Quit();
    }

    public void onPosSet()
    {
        Global.last_scene = "";
        StartCoroutine(GotoScene("loginSetting"));
    }

    public void onTagManage()
    {
        StartCoroutine(GotoScene("tagManage"));
    }

    public void onTapManage()
    {
        StartCoroutine(GotoScene("tapManage"));
    }

    public void onRemainManage()
    {
        StartCoroutine(GotoScene("remainManage"));
    }

    public void onUsage()
    {
        StartCoroutine(GotoScene("usageSellProduct"));
    }

    public void onTapSellManage()
    {
        StartCoroutine(GotoScene("tapselManage"));
    }

    public void onOrderManage()
    {
        StartCoroutine(GotoScene("orderManage"));
    }

    public void onPaymentMange()
    {
        StartCoroutine(GotoScene("payment"));
    }

    public void onCheckSell()
    {
        StartCoroutine(GotoScene("checkSellmonth"));
    }

    public void onSetOption()
    {
        StartCoroutine(GotoScene("setting"));
    }

    public void onErrPopup()
    {
        err_popup.SetActive(false);
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
