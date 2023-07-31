using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SocketIO;
using System.Threading.Tasks;

public class PrepayTagManager : MonoBehaviour
{
    public GameObject tagItem;
    public GameObject tagParent;
    public Text regCntTxt;//등록
    public Text usingCntTxt;//사용중
    public Text compltedCntTxt;//사용완료
    public Text expiredCntTxt;//기간만료
    public Text remainSumTxt;//잔액 합계
    public Text expiredSumTxt;//만료 태그 잔액
    public Text usableSumTxt;//유효잔액
    public Toggle selAllCheck;
    public GameObject err_popup;
    public Text err_str;
    public GameObject select_popup;
    public Text select_str;
    public GameObject check_pwPopup;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;

    List<GameObject> m_tagItemObj = new List<GameObject>();
    PrepayTagManageInfo tgInfo = new PrepayTagManageInfo();
    bool no_sort = false;
    bool menu_sort = false;
    bool regtime_sort = false;
    bool extime_sort = false;
    bool lasttime_sort = false;
    bool charge_sort = false;
    bool used_sort = false;
    bool price_sort = false;
    bool is_socket_open = false;

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(checkSdate());
        SendRequest(1);
        //selAllCheck.onValueChanged.RemoveAllListeners();
        selAllCheck.onValueChanged.AddListener((value) => {
            onSelectAll(value);
        }
        );
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
        StartCoroutine(GotoScene("prepayTagManage"));
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
        StartCoroutine(GotoScene("prepayTagManage"));
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

    void SendRequest(int type = 0)
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.get_prepaytag_api, form);
        StartCoroutine(LoadInfo(www, type));
    }

    public void noSort()
    {
        no_sort = !no_sort;
        StartCoroutine(reloadUI(1, 1, no_sort));
    }

    public void nameSort()
    {
        menu_sort = !menu_sort;
        StartCoroutine(reloadUI(1, 2, menu_sort));
    }

    public void regtimeSort()
    {
        regtime_sort = !regtime_sort;
        StartCoroutine(reloadUI(1, 3, regtime_sort));
    }

    public void extimeSort()
    {
        extime_sort = !extime_sort;
        StartCoroutine(reloadUI(1, 4, extime_sort));
    }

    public void lasttimeSort()
    {
        lasttime_sort = !lasttime_sort;
        StartCoroutine(reloadUI(1, 5, lasttime_sort));
    }

    public void chargeSort()
    {
        charge_sort = !charge_sort;
        StartCoroutine(reloadUI(1, 6, charge_sort));
    }

    public void usedSort()
    {
        used_sort = !used_sort;
        StartCoroutine(reloadUI(1, 7, used_sort));
    }

    public void priceSort()
    {
        price_sort = !price_sort;
        StartCoroutine(reloadUI(1, 8, price_sort));
    }

    List<PrepayTagManageDetailInfo> sortTagList(int sort_order = 0, bool sort_direction = false)
    {
        for(int i = 0; i < tgInfo.tagList.Count - 1; i++)
        {
            for(int j = i; j < tgInfo.tagList.Count; j++)
            {
                switch (sort_order)
                {
                    case 1:
                        {
                            //no
                            if (sort_direction)
                            {
                                if (tgInfo.tagList[i].no < tgInfo.tagList[j].no)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            else
                            {
                                if (tgInfo.tagList[i].no > tgInfo.tagList[j].no)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            break;
                        };
                    case 2:
                        {
                            //menu
                            if (sort_direction)
                            {
                                if (tgInfo.tagList[i].name.CompareTo(tgInfo.tagList[j].name) > 0)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            else
                            {
                                if (tgInfo.tagList[i].name.CompareTo(tgInfo.tagList[j].name) < 0)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            break;
                        };
                    case 3:
                        {
                            //reg datetime
                            if (sort_direction)
                            {
                                if (tgInfo.tagList[i].reg_datetime.CompareTo(tgInfo.tagList[j].reg_datetime) > 0)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            else
                            {
                                if (tgInfo.tagList[i].reg_datetime.CompareTo(tgInfo.tagList[j].reg_datetime) < 0)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            break;
                        };
                    case 4:
                        {
                            //expired time
                            if (sort_direction)
                            {
                                if (tgInfo.tagList[i].expried_datetime.CompareTo(tgInfo.tagList[j].expried_datetime) > 0)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            else
                            {
                                if (tgInfo.tagList[i].expried_datetime.CompareTo(tgInfo.tagList[j].expried_datetime) < 0)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            break;
                        };
                    case 5:
                        {
                            //first used time
                            if (sort_direction)
                            {
                                if (tgInfo.tagList[i].last_used_datetime.CompareTo(tgInfo.tagList[j].last_used_datetime) > 0)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            else
                            {
                                if (tgInfo.tagList[i].last_used_datetime.CompareTo(tgInfo.tagList[j].last_used_datetime) < 0)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            break;
                        };
                    case 6:
                        {
                            //charge
                            if (sort_direction)
                            {
                                if (tgInfo.tagList[i].charge_price < tgInfo.tagList[j].charge_price)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            else
                            {
                                if (tgInfo.tagList[i].charge_price > tgInfo.tagList[j].charge_price)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            break;
                        };
                    case 7:
                        {
                            //used
                            if (sort_direction)
                            {
                                if (tgInfo.tagList[i].used_price < tgInfo.tagList[j].used_price)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            else
                            {
                                if (tgInfo.tagList[i].used_price > tgInfo.tagList[j].used_price)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            break;
                        };
                    case 8:
                        {
                            //remain
                            if (sort_direction)
                            {
                                if (tgInfo.tagList[i].remain_price < tgInfo.tagList[j].remain_price)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            else
                            {
                                if (tgInfo.tagList[i].remain_price > tgInfo.tagList[j].remain_price)
                                {
                                    PrepayTagManageDetailInfo tmp = tgInfo.tagList[i];
                                    tgInfo.tagList[i] = tgInfo.tagList[j];
                                    tgInfo.tagList[j] = tmp;
                                }
                            }
                            break;
                        }
                }
            }
        }
        return tgInfo.tagList;
    }

    IEnumerator reloadUI(int type, int sort_type = 0, bool sort_direction = false)
    {
        while (tagParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(tagParent.transform.GetChild(0).gameObject));
        }
        while (tagParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        m_tagItemObj.Clear();
        if (type != 0)
        {
            List<PrepayTagManageDetailInfo> sortedList = sortTagList(sort_type, sort_direction);
            for (int i = 0; i < sortedList.Count; i++)
            {
                GameObject tmp = Instantiate(tagItem);
                tmp.transform.SetParent(tagParent.transform);
                //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmp.transform.localScale = Vector3.one;
                try
                {
                    tmp.transform.Find("reg_time").GetComponent<Text>().text = sortedList[i].reg_datetime;
                    tmp.transform.Find("expired_time").GetComponent<Text>().text = sortedList[i].expried_datetime;
                    tmp.transform.Find("last_time").GetComponent<Text>().text = sortedList[i].last_used_datetime;
                    tmp.transform.Find("no").GetComponent<Text>().text = sortedList[i].no.ToString();
                    tmp.transform.Find("tag").GetComponent<Text>().text = sortedList[i].name;
                    tmp.transform.Find("tag_id").GetComponent<Text>().text = sortedList[i].id.ToString();
                    tmp.transform.Find("charge").GetComponent<Text>().text = Global.GetPriceFormat(sortedList[i].charge_price);
                    tmp.transform.Find("used").GetComponent<Text>().text = Global.GetPriceFormat(sortedList[i].used_price);
                    tmp.transform.Find("remain").GetComponent<Text>().text = Global.GetPriceFormat(sortedList[i].remain_price);
                    CurTagInfo cinfo = new CurTagInfo();
                    cinfo.charge = sortedList[i].charge_price;
                    cinfo.is_pay_after = 0;
                    cinfo.period = sortedList[i].period;
                    cinfo.qrcode = sortedList[i].qrcode;
                    cinfo.reg_datetime = sortedList[i].reg_datetime;
                    cinfo.remain = sortedList[i].remain_price;
                    cinfo.rfid = sortedList[i].rfid;
                    cinfo.tag_id = sortedList[i].id;
                    cinfo.tag_name = sortedList[i].name;
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { onGotoUsage(cinfo); });
                }
                catch (Exception ex)
                {

                }
                m_tagItemObj.Add(tmp);
            }
        }
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

    IEnumerator LoadInfo(WWW www, int type)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            tgInfo.completed_cnt = jsonNode["completed_cnt"].AsInt;
            tgInfo.expired_cnt = jsonNode["expired_cnt"].AsInt;
            tgInfo.expired_sum = jsonNode["expired_sum"].AsInt;
            tgInfo.reg_cnt = jsonNode["reg_cnt"].AsInt;
            tgInfo.remain_sum = jsonNode["remain_sum"].AsInt;
            tgInfo.useful_sum = jsonNode["useful_sum"].AsInt;
            tgInfo.using_cnt = jsonNode["using_cnt"].AsInt;

            JSONNode tlist = JSON.Parse(jsonNode["taglist"].ToString()/*.Replace("\"", "")*/);
            tgInfo.tagList = new List<PrepayTagManageDetailInfo>();
            for (int i = 0; i < tlist.Count; i++)
            {
                PrepayTagManageDetailInfo tinfo = new PrepayTagManageDetailInfo();
                tinfo.charge_price = tlist[i]["charge"].AsInt;
                tinfo.expried_datetime = tlist[i]["expired_datetime"];
                tinfo.last_used_datetime = tlist[i]["last_used_datetime"];
                tinfo.name = tlist[i]["name"];
                tinfo.no = tlist[i]["no"].AsInt;
                tinfo.id = tlist[i]["id"];
                tinfo.reg_datetime = tlist[i]["reg_datetime"];
                tinfo.remain_price = tlist[i]["remain_price"].AsInt;
                tinfo.used_price = tlist[i]["used_price"].AsInt;
                tinfo.period = tlist[i]["period"].AsInt;
                tinfo.qrcode = tlist[i]["qrcode"];
                tinfo.rfid = tlist[i]["rfid"];
                tinfo.is_pay_after = 0;
                tgInfo.tagList.Add(tinfo);
            }

            //UI에 추가
            regCntTxt.text = Global.GetPriceFormat(tgInfo.reg_cnt);
            usingCntTxt.text = Global.GetPriceFormat(tgInfo.using_cnt);
            compltedCntTxt.text = Global.GetPriceFormat(tgInfo.completed_cnt);
            expiredCntTxt.text = Global.GetPriceFormat(tgInfo.expired_cnt);
            remainSumTxt.text = Global.GetPriceFormat(tgInfo.remain_sum);
            expiredSumTxt.text = Global.GetPriceFormat(tgInfo.expired_sum);
            usableSumTxt.text = Global.GetPriceFormat(tgInfo.useful_sum);
            StartCoroutine(reloadUI(1, type));
        }
    }

    void onSelectAll(bool value)
    {
        for (int i = 0; i < tagParent.transform.childCount; i++)
        {
            tagParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn = value;
        }
    }

    void onGotoUsage(CurTagInfo tag)
    {
        Global.cur_tagInfo = tag;
        StartCoroutine(GotoScene("prepayTagUsage"));
    }

    public void check()
    {
        //조회
        SendRequest(1);
    }

    void delTag(int type)
    {
        string pw = check_pwPopup.transform.Find("background/val").GetComponent<InputField>().text;
        if(pw == "" || pw == null)
        {
            err_popup.SetActive(true);
            err_str.text = "비밀번호를 입력하세요.";
            return;
        }
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("pw", pw);
        WWW www = new WWW(Global.api_url + Global.check_pw_api, form);
        StartCoroutine(checkPwProcess(www, type));
        check_pwPopup.SetActive(false);
    }

    IEnumerator checkPwProcess(WWW www, int type)
    {
        yield return www;
        if(www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                delTagFunc(type);
            }
            else
            {
                err_popup.SetActive(true);
                err_str.text = "비밀번호가 일치하지 않습니다.";
            }
        }
        else
        {
            err_popup.SetActive(true);
            err_str.text = "서버와의 접속이 원활하지 않습니다.\n잠시후에 다시 시도해주세요.";
        }
    }

    void delTagFunc(int type)
    {
        WWWForm form = new WWWForm();
        switch (type)
        {
            case 0:
                {
                    //선택 태그
                    List<string> selected_taglist = new List<string>();
                    for (int i = 0; i < tagParent.transform.childCount; i++)
                    {
                        if (tagParent.transform.GetChild(i).transform.Find("check").GetComponent<Toggle>().isOn)
                        {
                            try
                            {
                                selected_taglist.Add(tagParent.transform.GetChild(i).transform.Find("tag_id").GetComponent<Text>().text);
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                    if (selected_taglist.Count == 0)
                    {
                        select_popup.SetActive(false);
                        err_popup.SetActive(true);
                        err_str.text = "선택된 태그가 없습니다.";
                        return;
                    }
                    string oinfo = "[";
                    for (int i = 0; i < selected_taglist.Count; i++)
                    {
                        if (i == 0)
                        {
                            oinfo += "{";
                        }
                        else
                        {
                            oinfo += ",{";
                        }
                        oinfo += "\"tag_id\":\"" + selected_taglist[i] + "\"}";
                    }
                    oinfo += "]";
                    Debug.Log(oinfo);
                    form.AddField("tag_info", oinfo);
                    form.AddField("type", 0);//선택태그 삭제
                    break;
                };
            case 1:
                {
                    //완료
                    form.AddField("type", 1);//완료태그 삭제
                    break;
                };
            case 2:
                {
                    //만료
                    form.AddField("type", 2);//만료태그 삭제
                    break;
                }
            case 3:
                {
                    //전체
                    form.AddField("type", 3);//전체태그 삭제
                    break;
                }
        }
        form.AddField("pub_id", Global.userinfo.pub.id);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        WWW www = new WWW(Global.api_url + Global.del_tags_api, form);
        StartCoroutine(delTagProcess(www));
    }

    void RemoveTag(int type)
    {
        check_pwPopup.SetActive(true);
        check_pwPopup.transform.Find("background/val").GetComponent<InputField>().text = "";
        check_pwPopup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
        check_pwPopup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { delTag(type); });
        select_popup.SetActive(false);
    }

    public void delSelectedTag()
    {
        //선택 TAG 삭제
        select_popup.SetActive(true);
        select_str.text = "선택된 TAG를 삭제 하시겠습니까?";
        select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
        select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { RemoveTag(0); });
    }

    IEnumerator delTagProcess(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            SendRequest(1);
        }
    }

    public void delCompletedTag()
    {
        //완료 태그 삭제
        select_popup.SetActive(true);
        select_str.text = "완료 TAG를 삭제 하시겠습니까?";
        select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
        select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { RemoveTag(1); });
    }

    public void delExpiredTag()
    {
        //만료 태그 삭제
        select_popup.SetActive(true);
        select_str.text = "만료 TAG를 삭제 하시겠습니까?";
        select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
        select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { RemoveTag(2); });
    }

    public void delAllTags()
    {
        //전체 태그 삭제
        select_popup.SetActive(true);
        select_str.text = "전체 TAG를 삭제 하시겠습니까?";
        select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
        select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { RemoveTag(3); });
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

    public void onTag()
    {
        //후불태그
        StartCoroutine(GotoScene("tagManage"));
    }

    public void onBack()
    {
        StartCoroutine(GotoScene("main"));
    }

    public void closeErrPopup()
    {
        err_popup.SetActive(false);
        select_popup.SetActive(false);
    }

    public void onCancelPwPopup()
    {
        check_pwPopup.SetActive(false);
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
