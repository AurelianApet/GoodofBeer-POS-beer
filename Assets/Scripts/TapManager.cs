using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SimpleJSON;
using System;
using SocketIO;
using System.Threading.Tasks;

public class TapManager : MonoBehaviour
{
    public GameObject tapItem;
    public GameObject tapParent;
    public GameObject mylistItem;
    public GameObject mylistParent;
    public Toggle selAllTap;
    public GameObject select_popup;
    public Text select_str;
    public GameObject err_popup;
    public Text err_str;
    public GameObject socketPrefab;
    public GameObject changeUnitPricePopup;
    public GameObject set_popup;
    GameObject socketObj;
    SocketIOComponent socket;
    List<GameObject> m_tapObj = new List<GameObject>();
    List<GameObject> m_mylistObj = new List<GameObject>();
    string cur_tapid = "";
    bool is_socket_open = false;

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(checkSdate());
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.get_taplist_api, form);
        StartCoroutine(LoadInfo(www));
        socketObj = Instantiate(socketPrefab);
        //socketObj = GameObject.Find("SocketIO");
        socket = socketObj.GetComponent<SocketIOComponent>();
        socket.On("open", socketOpen);
        socket.On("createOrder", createOrder);
        socket.On("reload", reload);
        socket.On("new_notification", new_notification);
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
        socket.Emit("posSetInfo", JSONObject.Create(data));
        Debug.Log("[SocketIO] Open received: " + e.name + " " + e.data);
    }

    public void reload(SocketIOEvent e)
    {
        StartCoroutine(GotoScene("tapManage"));
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
        while (tapParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(tapParent.transform.GetChild(0).gameObject));
        }
        while (tapParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        Global.tapList.Clear();
        m_tapObj.Clear();
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            JSONNode tlist = JSON.Parse(jsonNode["taplist"].ToString()/*.Replace("\"", "")*/);
            for (int i = 0; i < tlist.Count; i++)
            {
                TapInfo tinfo = new TapInfo();
                tinfo.no = tlist[i]["no"].AsInt;
                tinfo.id = tlist[i]["id"];
                tinfo.name = tlist[i]["name"];
                tinfo.cup_capacity = tlist[i]["cup_size"];
                tinfo.unit_price = tlist[i]["unit_price"];
                tinfo.keg_capacity = tlist[i]["keg_size"].AsInt;
                tinfo.remain = tlist[i]["remain"].AsInt;
                tinfo.sell_type = tlist[i]["sell_type"].AsInt;
                tinfo.product_id = tlist[i]["product_id"];
                Global.tapList.Add(tinfo);
            }
            //UI에 추가
            for (int i = 0; i < Global.tapList.Count; i++)
            {
                GameObject tmp = Instantiate(tapItem);
                tmp.transform.SetParent(tapParent.transform);
                //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmp.transform.localScale = Vector3.one;
                try
                {
                    tmp.transform.Find("id").GetComponent<Text>().text = Global.tapList[i].id.ToString();
                    tmp.transform.Find("product_id").GetComponent<Text>().text = Global.tapList[i].product_id.ToString();
                    tmp.transform.Find("no").GetComponent<Text>().text = Global.tapList[i].no.ToString();
                    tmp.transform.Find("name").GetComponent<Text>().text = Global.tapList[i].name;
                    tmp.transform.Find("name").GetComponent<Button>().onClick.RemoveAllListeners();
                    TapInfo _tinfo = Global.tapList[i];
                    tmp.transform.Find("name").GetComponent<Button>().onClick.AddListener(delegate() { onSetTapInfo(_tinfo); });
                    tmp.transform.Find("unit_price").GetComponent<Text>().text = Global.GetPriceFormat(Global.tapList[i].unit_price) + " 원/ml";
                    tmp.transform.Find("size").GetComponent<Text>().text = Global.GetPriceFormat(Global.tapList[i].cup_capacity) + " ml";
                    tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(Global.tapList[i].unit_price * Global.tapList[i].cup_capacity) + " 원";
                    tmp.transform.Find("keg").GetComponent<Text>().text = Global.GetPriceFormat(Global.tapList[i].keg_capacity) + " ml";
                    tmp.transform.Find("remain").GetComponent<Text>().text = Global.GetPriceFormat(Global.tapList[i].remain) + " ml";
                    GameObject toggleObj = tmp.transform.Find("check").gameObject;
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { onSelItem(toggleObj); });
                }
                catch (Exception ex)
                {

                }
                m_tapObj.Add(tmp);
            }
            selAllTap.onValueChanged.RemoveAllListeners();
            selAllTap.onValueChanged.AddListener((value) => {
                onSelectAllTap(value);
            }
            );
        }
    }

    void onSelectAllTap(bool value)
    {
        for (int i = 0; i < tapParent.transform.childCount; i++)
        {
            tapParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn = value;
        }
    }

    string cur_productId = "";
    void onSetTapInfo(TapInfo tinfo)
    {
        cur_tapid = tinfo.id;
        cur_productId = tinfo.product_id;
        set_popup.SetActive(true);
        set_popup.transform.Find("form/1/val1").GetComponent<Text>().text = tinfo.no.ToString();
        set_popup.transform.Find("form/1/val2").GetComponent<InputField>().text = tinfo.name;
        set_popup.transform.Find("form/1/val3").GetComponent<InputField>().text = Global.GetPriceFormat(tinfo.unit_price);
        set_popup.transform.Find("form/1/val4").GetComponent<InputField>().text = Global.GetPriceFormat(tinfo.keg_capacity);
        int stype = 0;
        if(tinfo.product_id == "" || tinfo.product_id == null)
        {
            stype = Global.userinfo.pub.sell_type;
        }
        else
        {
            stype = tinfo.sell_type;
        }
        if(stype == 1)
        {
            //ml 판매인 경우
            set_popup.transform.Find("form/1/val5").GetComponent<Toggle>().isOn = false;
        }
        else
        {
            set_popup.transform.Find("form/1/val5").GetComponent<Toggle>().isOn = true;
        }
        set_popup.transform.Find("form/1/val6").GetComponent<InputField>().text = Global.GetPriceFormat(tinfo.cup_capacity);
        onRefresh();
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

    public void onClosePopup()
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

    public void onChangeUnitPrice()
    {
        int cnt = 0;
        for (int i = 0; i < tapParent.transform.childCount; i++)
        {
            if (tapParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
            {
                cnt++;
            }
        }
        if (cnt == 0)
        {
            err_popup.SetActive(true);
            err_str.text = "선택된 TAP이 없습니다.";
            return;
        }
        changeUnitPricePopup.SetActive(true);
    }

    void changeUnitprice(int unit_price)
    {
        WWWForm form = new WWWForm();
        string oinfo = "[";
        int j = 0;
        for (int i = 0; i < tapParent.transform.childCount; i++)
        {
            if (tapParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
            {
                if (j == 0)
                {
                    oinfo += "{";
                }
                else
                {
                    oinfo += ",{";
                }
                j++;
                oinfo += "\"tap_id\":\"" + tapParent.transform.GetChild(i).Find("id").GetComponent<Text>().text + "\"}";
            }
        }
        oinfo += "]";
        Debug.Log(oinfo);
        form.AddField("tap_info", oinfo);
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("unit_price", unit_price);
        WWW www = new WWW(Global.api_url + Global.change_unitprice_api, form);
        StartCoroutine(tapProcess(www));
    }

    public void onConfirmUnitprice()
    {
        try
        {
            if (changeUnitPricePopup.transform.Find("background/val").GetComponent<InputField>().text.Trim() == "")
            {
                err_popup.SetActive(true);
                err_str.text = "값을 정확히 입력하세요.";
                return;
            }
            int unit_price = int.Parse(changeUnitPricePopup.transform.Find("background/val").GetComponent<InputField>().text);
            select_popup.SetActive(true);
            select_str.text = "선택한 TAP의 unit_price를 변경하시겠습니까?";
            select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
            select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { changeUnitprice(unit_price); });
        }
        catch (Exception ex)
        {

        }
    }

    public void onCloseUnitPrice()
    {
        changeUnitPricePopup.SetActive(false);
    }

    public void onResend()
    {
        //TAP정보 재전송
        WWWForm form = new WWWForm();
        string oinfo = "[";
        int j = 0;
        for (int i = 0; i < tapParent.transform.childCount; i++)
        {
            if (tapParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
            {
                if (j == 0)
                {
                    oinfo += "{";
                }
                else
                {
                    oinfo += ",{";
                }
                j++;
                oinfo += "\"tap_id\":\"" + tapParent.transform.GetChild(i).Find("id").GetComponent<Text>().text + "\"}";
            }
        }
        oinfo += "]";
        Debug.Log(oinfo);
        if(j == 0)
        {
            err_popup.SetActive(true);
            err_str.text = "선택된 TAP이 없습니다.";
            return;
        }
        form.AddField("tap_info", oinfo);
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.resend_tap_api, form);
        StartCoroutine(tapProcess(www));
    }

    public void onCloseSetPopup()
    {
        set_popup.SetActive(false);
    }

    void initTap()
    {
        WWWForm form = new WWWForm();
        string oinfo = "[";
        int j = 0;
        for (int i = 0; i < tapParent.transform.childCount; i++)
        {
            if (tapParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
            {
                if (j == 0)
                {
                    oinfo += "{";
                }
                else
                {
                    oinfo += ",{";
                }
                j++;
                oinfo += "\"tap_id\":\"" + tapParent.transform.GetChild(i).Find("id").GetComponent<Text>().text + "\"}";
            }
        }
        oinfo += "]";
        Debug.Log(oinfo);
        form.AddField("tap_info", oinfo);
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.init_tap_api, form);
        StartCoroutine(tapProcess(www));
    }

    public void InitCapacity()
    {
        //용량 초기화
        int cnt = 0;
        for (int i = 0; i < tapParent.transform.childCount; i++)
        {
            if (tapParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
            {
                cnt++;
            }
        }
        if (cnt == 0)
        {
            err_popup.SetActive(true);
            err_str.text = "선택된 TAP이 없습니다.";
            return;
        }
        select_popup.SetActive(true);
        select_str.text = "선택한 TAP의 용량을 초기화 하시겠습니까?";
        select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
        select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { initTap(); });
    }

    IEnumerator tapProcess(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            StartCoroutine(GotoScene("tapManage"));
        }
        else
        {
            select_popup.SetActive(false);
            changeUnitPricePopup.SetActive(false);
        }
    }

    void removeTap()
    {
        WWWForm form = new WWWForm();
        string oinfo = "[";
        int j = 0;
        for (int i = 0; i < tapParent.transform.childCount; i++)
        {
            if (tapParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
            {
                if (j == 0)
                {
                    oinfo += "{";
                }
                else
                {
                    oinfo += ",{";
                }
                j++;
                oinfo += "\"tap_id\":\"" + tapParent.transform.GetChild(i).Find("id").GetComponent<Text>().text + "\"}";
            }
        }
        oinfo += "]";
        Debug.Log(oinfo);
        form.AddField("tap_info", oinfo);
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.remove_tap_api, form);
        StartCoroutine(tapProcess(www));
    }

    public void onDelTap()
    {
        //TAP에서 삭제
        int cnt = 0;
        for (int i = 0; i < tapParent.transform.childCount; i++)
        {
            if (tapParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn)
            {
                cnt++;
            }
        }
        if (cnt == 0)
        {
            err_popup.SetActive(true);
            err_str.text = "선택된 TAP이 없습니다.";
            return;
        }
        select_popup.SetActive(true);
        select_str.text = "선택한 맥주를 TAP에서 삭제 하시겠습니까?";
        select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.RemoveAllListeners();
        select_popup.transform.Find("background/confirmBtn").GetComponent<Button>().onClick.AddListener(delegate () { removeTap(); });
    }

    public void onSaveInfo()
    {
        try
        {
            string name = set_popup.transform.Find("form/1/val2").GetComponent<InputField>().text;
            int unit_price = Global.GetConvertedPrice(set_popup.transform.Find("form/1/val3").GetComponent<InputField>().text);
            int bottle_size = Global.GetConvertedPrice(set_popup.transform.Find("form/1/val4").GetComponent<InputField>().text);
            int sell_type = 1;
            if (set_popup.transform.Find("form/1/val5").GetComponent<Toggle>().isOn)
            {
                sell_type = 0;
            }
            int cup_size = Global.GetConvertedPrice(set_popup.transform.Find("form/1/val6").GetComponent<InputField>().text);
            WWWForm form = new WWWForm();
            form.AddField("tap_id", cur_tapid);
            form.AddField("name", name);
            form.AddField("unit_price", unit_price);
            form.AddField("bottle_size", bottle_size);
            form.AddField("sell_type", sell_type);
            form.AddField("cup_size", cup_size);
            form.AddField("pub_id", Global.userinfo.pub.id);
            form.AddField("product_id", cur_productId);
            WWW www = new WWW(Global.api_url + Global.change_tapdetail_api, form);
            StartCoroutine(tapProcess(www));
        }
        catch(Exception ex)
        {
            Debug.Log(ex);
        }
    }

    IEnumerator saveTapInfoProcess(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            //for(int i = 0; i < tapParent.transform.childCount; i++)
            //{
            //    if(tapParent.transform.GetChild(i).Find("id").GetComponent<Text>().text == cur_tapid.ToString())
            //    {
            //        try
            //        {
            //            tapParent.transform.GetChild(i).transform.Find("name").GetComponent<InputField>().text = set_popup.transform.Find("form/1/val2").GetComponent<InputField>().text;
            //            tapParent.transform.GetChild(i).transform.Find("unit_price").GetComponent<InputField>().text = set_popup.transform.Find("form/1/val3").GetComponent<InputField>().text + " 원/ml";
            //            tapParent.transform.GetChild(i).transform.Find("size").GetComponent<InputField>().text = set_popup.transform.Find("form/1/val6").GetComponent<InputField>().text + " ml";
            //            tapParent.transform.GetChild(i).transform.Find("keg").GetComponent<InputField>().text = set_popup.transform.Find("form/1/val4").GetComponent<InputField>().text + " ml";
            //        }
            //        catch (Exception ex)
            //        {

            //        }
            //        break;
            //    }
            //}
            //for(int i = 0; i < Global.tapList.Count; i++)
            //{
            //    if(Global.tapList[i].id == cur_tapid)
            //    {
            //        TapInfo tinfo = Global.tapList[i];
            //        tinfo.name = set_popup.transform.Find("form/1/val2").GetComponent<InputField>().text;
            //        tinfo.unit_price = Global.GetConvertedPrice(set_popup.transform.Find("form/1/val3").GetComponent<InputField>().text);
            //        tinfo.cup_capacity = Global.GetConvertedPrice(set_popup.transform.Find("form/1/val6").GetComponent<InputField>().text);
            //        tinfo.keg_capacity = Global.GetConvertedPrice(set_popup.transform.Find("form/1/val4").GetComponent<InputField>().text);
            //        break;
            //    }
            //}
            StartCoroutine(GotoScene("tapManage"));
        }
        //set_popup.SetActive(false);
    }

    public void onRefresh()
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("app_type", Global.app_type);
        WWW www = new WWW(Global.api_url + Global.get_beerlist_api, form);
        StartCoroutine(loadMylistProcess(www));
    }

    IEnumerator loadMylistProcess(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            while (mylistParent.transform.childCount > 0)
            {
                StartCoroutine(Destroy_Object(mylistParent.transform.GetChild(0).gameObject));
            }
            while (mylistParent.transform.childCount > 0)
            {
                yield return new WaitForFixedUpdate();
            }
            m_mylistObj.Clear();
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            JSONNode tlist = JSON.Parse(jsonNode["beerlist"].ToString()/*.Replace("\"", "")*/);
            for (int i = 0; i < tlist.Count; i++)
            {
                GameObject tmp = Instantiate(mylistItem);
                tmp.transform.SetParent(mylistParent.transform);
                //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmp.transform.localScale = Vector3.one;
                try
                {
                    tmp.transform.Find("name").GetComponent<Text>().text = tlist[i]["name"];
                    tmp.transform.Find("size").GetComponent<Text>().text = Global.GetPriceFormat(tlist[i]["size"].AsInt) + "ml";
                    tmp.transform.Find("product_id").GetComponent<Text>().text = tlist[i]["id"];
                    string _name = tlist[i]["name"];
                    string _pid = tlist[i]["product_id"];
                    int _size = tlist[i]["size"].AsInt;
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { onselMylist(_name, _pid, _size); });
                }
                catch (Exception ex)
                {

                }
                m_mylistObj.Add(tmp);
            }
        }
    }

    void onselMylist(string name, string pid, int size)
    {
        set_popup.transform.Find("form/1/val2").GetComponent<InputField>().text = name;
        set_popup.transform.Find("form/1/val4").GetComponent<InputField>().text = Global.GetPriceFormat(size);
        cur_productId = pid;
    }

    public void onBack()
    {
        StartCoroutine(GotoScene("main"));
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
