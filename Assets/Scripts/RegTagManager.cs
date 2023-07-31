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

public class RegTagManager : MonoBehaviour
{
    public GameObject tagItem;
    public GameObject tagItemParent;
    public GameObject tagSelParent;
    public GameObject tagSelItem;
    public GameObject tagPreSelItem;
    public InputField readTag;

    public GameObject popup;
    public Text popup_str;
    public GameObject err_popup;
    public Text err_str;
    public Text tableName;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;

    List<GameObject> m_tagItem = new List<GameObject>();
    List<string> m_originTagId = new List<string>();
    List<string> m_destTagId = new List<string>();
    List<GameObject> m_selTagObj = new List<GameObject>();
    int tmp_index = -1;
    float send_time = 0f;
    bool is_selected_change = false;
    string is_selected_tag_id = "";
    List<TagInfo> mytag = new List<TagInfo>();
    bool is_socket_open = false;

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(checkSdate());
        tableName.text = Global.tableGroupList[Global.cur_tInfo.tgNo].tablelist[Global.cur_tInfo.tNo].name;
        StartCoroutine(LoadMyTags());
        StartCoroutine(LoadTags());
        readTag.onValueChanged.AddListener((value) => {
            checkTag(value);
            }
        );
        readTag.Select();
        readTag.ActivateInputField();
        socketObj = Instantiate(socketPrefab);
        //socketObj = GameObject.Find("SocketIO");
        socket = socketObj.GetComponent<SocketIOComponent>();
        socket.On("open", socketOpen);
        socket.On("createOrder", createOrder);
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
        readTag.text = "";
        send_time = 0f;
    }

    IEnumerator onCheckTagProcess(WWW www)
    {
        yield return www;
        Global.cur_tagInfo = new CurTagInfo();
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            if (jsonNode["suc"].AsInt == 1)
            {
                if(jsonNode["table_id"].AsInt != 0)
                {
                    is_pre_regTag = true;
                    preTagData = jsonNode;
                    popup_str.text = "다른 테이블에 등록된 TAG입니다.\n테이블을 이동하시겠습니까?";
                    popup.SetActive(true);
                }
                else
                {
                    RegPreTag(jsonNode);
                }
            }
            else
            {
                err_str.text = "등록되지 않은 태그입니다.";
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "서버접속이 원활하지 않습니다.\n 후에 다시 시도해주세요.";
            err_popup.SetActive(true);
        }
    }

    void RegPreTag(JSONNode jsonNode)
    {
        int st = jsonNode["status"].AsInt;
        int is_p = jsonNode["period"].AsInt;
        int is_pay_after = jsonNode["is_pay_after"].AsInt;
        TagInfo tagInfo = new TagInfo();
        tagInfo.id = jsonNode["id"];
        tagInfo.name = jsonNode["name"];
        tagInfo.is_pay_after = is_pay_after;
        tagInfo.status = jsonNode["status"].AsInt;
        tagInfo.is_blank = jsonNode["is_blank"].AsInt;
        for (int i = 0; i < m_selTagObj.Count; i++)
        {
            if (m_selTagObj[i].transform.Find("tag/id").GetComponent<Text>().text == tagInfo.id.ToString())
            {
                err_popup.SetActive(true);
                err_str.text = "이미 선택되어있습니다.";
                return;
            }
        }
        mytag.Add(tagInfo);

        GameObject tmpObj;
        if (tagInfo.is_pay_after == 1)
        {
            tmpObj = Instantiate(tagSelItem);
        }
        else
        {
            tmpObj = Instantiate(tagPreSelItem);
        }
        tmpObj.transform.SetParent(tagSelParent.transform);
        //tmpObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
        //tmpObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
        //float left = 0;
        //float right = 0;
        //tmpObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
        //tmpObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
        //tmpObj.transform.localScale = Vector3.one;
        tmpObj.transform.Find("tag/name").GetComponent<Text>().text = tagInfo.name;
        tmpObj.transform.Find("tag/id").GetComponent<Text>().text = tagInfo.id.ToString();
        tmpObj.transform.Find("tag/is_pay_after").GetComponent<Text>().text = tagInfo.is_pay_after.ToString();
        tmpObj.transform.Find("tag/status").GetComponent<Text>().text = tagInfo.status.ToString();
        tmpObj.transform.Find("tag/is_blank").GetComponent<Text>().text = tagInfo.is_blank.ToString();

        if (tagInfo.is_blank == 1)
        {
            tmpObj.transform.Find("tag/name").GetComponent<Text>().color = Color.white;
            string _i = tagInfo.id;
            tmpObj.transform.Find("Btn").GetComponent<Text>().text = "삭제";
            tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.RemoveAllListeners();
            tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.AddListener(delegate () { onDelTagItem(_i); });
        }
        else
        {
            tmpObj.transform.Find("tag/name").GetComponent<Text>().color = Color.green;
            string _i = tagInfo.id;
            tmpObj.transform.Find("Btn").GetComponent<Text>().text = "교환";
            tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.RemoveAllListeners();
            tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.AddListener(delegate () { onChangeTagItem(_i); });
        }
        m_selTagObj.Add(tmpObj);
    }

    // Update is called once per frame
    void Update()
    {

    }

    IEnumerator LoadTags()
    {
        Global.taglist.Clear();
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.get_taglist_api, form);
        StartCoroutine(LoadTaglist(www));
        while (tagItemParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(tagItemParent.transform.GetChild(0).gameObject));
        }
        while (tagItemParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator Destroy_Object(GameObject obj)
    {
        DestroyImmediate(obj);
        yield return null;
    }

    IEnumerator LoadMyTags()
    {
        while (tagSelParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(tagSelParent.transform.GetChild(0).gameObject));
        }
        while (tagSelParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }

        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("table_id", Global.cur_tInfo.tid);
        WWW www = new WWW(Global.api_url + Global.get_tabletag_api, form);
        StartCoroutine(LoadMyTaglist(www));
    }

    IEnumerator LoadMyTaglist(WWW www)
    {
        yield return www;
        mytag.Clear();
        if(www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            JSONNode tag_list = JSON.Parse(jsonNode["taglist"].ToString()/*.Replace("\"", "")*/);
            for (int i = 0; i < tag_list.Count; i++)
            {
                TagInfo tagInfo = new TagInfo();
                tagInfo.id = tag_list[i]["id"];
                tagInfo.name = tag_list[i]["name"];
                tagInfo.is_pay_after = tag_list[i]["is_pay_after"].AsInt;
                tagInfo.status = tag_list[i]["status"].AsInt;
                tagInfo.is_blank = tag_list[i]["is_blank"].AsInt;
                mytag.Add(tagInfo);
            }
        }
        m_selTagObj.Clear();
        for (int i = 0; i < mytag.Count; i++)
        {
            GameObject tmpObj;
            if(mytag[i].is_pay_after == 1)
            {
                tmpObj = Instantiate(tagSelItem);
            }
            else
            {
                tmpObj = Instantiate(tagPreSelItem);
            }
            tmpObj.transform.SetParent(tagSelParent.transform);
            //tmpObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
            //tmpObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
            //float left = 0;
            //float right = 0;
            //tmpObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
            //tmpObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
            //tmpObj.transform.localScale = Vector3.one;
            tmpObj.transform.Find("tag/name").GetComponent<Text>().text = mytag[i].name;
            tmpObj.transform.Find("tag/id").GetComponent<Text>().text = mytag[i].id.ToString();
            tmpObj.transform.Find("tag/is_pay_after").GetComponent<Text>().text = mytag[i].is_pay_after.ToString();
            tmpObj.transform.Find("tag/status").GetComponent<Text>().text = mytag[i].status.ToString();
            tmpObj.transform.Find("tag/is_blank").GetComponent<Text>().text = mytag[i].is_blank.ToString();

            if (mytag[i].is_blank == 1)
            {
                tmpObj.transform.Find("tag/name").GetComponent<Text>().color = Color.white;
                tmpObj.transform.Find("Btn").GetComponent<Text>().text = "삭제";
                string _i = mytag[i].id;
                tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.RemoveAllListeners();
                tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.AddListener(delegate () { onDelTagItem(_i); });
            }
            else
            {
                tmpObj.transform.Find("tag/name").GetComponent<Text>().color = Color.green;
                tmpObj.transform.Find("Btn").GetComponent<Text>().text = "교환";
                string _i = mytag[i].id;
                tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.RemoveAllListeners();
                tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.AddListener(delegate () { onChangeTagItem(_i); });
            }
            m_selTagObj.Add(tmpObj);
        }
    }

    IEnumerator LoadTaglist(WWW www)
    {
        yield return www;
        m_tagItem.Clear();
        if(www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            JSONNode tag_list = JSON.Parse(jsonNode["taglist"].ToString()/*.Replace("\"", "")*/);
            for (int i = 0; i < tag_list.Count; i++)
            {
                TagInfo tagInfo = new TagInfo();
                tagInfo.id = tag_list[i]["id"];
                tagInfo.name = tag_list[i]["name"];
                tagInfo.is_pay_after = tag_list[i]["is_pay_after"].AsInt;
                tagInfo.status = tag_list[i]["status"].AsInt;
                tagInfo.qrcode = tag_list[i]["qrcode"];
                tagInfo.rfid = tag_list[i]["rfid"];
                tagInfo.is_blank = tag_list[i]["is_blank"];
                Global.taglist.Add(tagInfo);
            }
        }
        //UI
        for (int i = 0; i < Global.taglist.Count; i++)
        {
            GameObject tmpObj = Instantiate(tagItem);
            tmpObj.transform.SetParent(tagItemParent.transform);
            //tmpObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
            //tmpObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
            //float left = 0;
            //float right = 0;
            //tmpObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
            //tmpObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
            //tmpObj.transform.localScale = Vector3.one;
            tmpObj.transform.Find("name").GetComponent<Text>().text = Global.taglist[i].name;
            tmpObj.transform.Find("id").GetComponent<Text>().text = Global.taglist[i].id.ToString();
            tmpObj.transform.Find("is_pay_after").GetComponent<Text>().text = Global.taglist[i].is_pay_after.ToString();
            tmpObj.transform.Find("status").GetComponent<Text>().text = Global.taglist[i].status.ToString();
            tmpObj.transform.Find("is_blank").GetComponent<Text>().text = Global.taglist[i].is_blank.ToString();

            //0-테이블에 미등록, 1-테이블에 등록, 2-분실, 3-태그 코드 미등록
            if (Global.taglist[i].status == 0)
            {
                tmpObj.transform.Find("name").GetComponent<Text>().color = new Color(0.3f, 0.3f, 0.3f);
            }
            else if(Global.taglist[i].status == 1)
            {
                tmpObj.transform.Find("name").GetComponent<Text>().color = Color.white;
            }
            else if(Global.taglist[i].status == 2)
            {
                tmpObj.transform.Find("name").GetComponent<Text>().color = Color.green;
            }
            else if (Global.taglist[i].status == 3)
            {
                tmpObj.transform.Find("name").GetComponent<Text>().color = Color.red;
            }
            else
            {
                tmpObj.transform.Find("name").GetComponent<Text>().color = new Color(0.3f, 0.3f, 0.3f);
            }
            int _i = i;
            int _status = Global.taglist[i].status;
            tmpObj.transform.GetComponent<Button>().onClick.RemoveAllListeners();
            if(Global.taglist[i].status == 2 || Global.taglist[i].status == 1)
            {
                tmpObj.transform.GetComponent<Button>().onClick.AddListener(delegate () { onSelTagItem(_i, _status); });
            }
            m_tagItem.Add(tmpObj);
        }
    }

    void onSelTagItem(int index, int status)
    {
        Debug.Log(index + "," + status + ", " + is_selected_change);
        for(int i = 0; i < mytag.Count; i++)
        {
            if(mytag[i].id == Global.taglist[index].id)
            {
                err_popup.SetActive(true);
                err_str.text = "이미 등록된 태그입니다.";
                return;
            }
        }
        if(status == 0 || status > 3)
        {
            err_str.text = "등록되지 않은 태그입니다.";
            err_popup.SetActive(true);
            return;
        }
        else if(status == 3)
        {
            err_str.text = "분실된 태그입니다.";
            err_popup.SetActive(true);
            return;
        }
        for(int i = 0; i < m_selTagObj.Count; i++)
        {
            if(m_selTagObj[i].transform.Find("tag/id").GetComponent<Text>().text == Global.taglist[index].id.ToString())
            {
                err_popup.SetActive(true);
                err_str.text = "이미 선택되어있습니다.";
                return;
            }
        }
        if (is_selected_change)
        {
            if (Global.taglist[index].status == 2)
            {
                tmp_index = index;
                popup_str.text = "다른 테이블에 등록된 TAG입니다.\n테이블을 이동하시겠습니까?";
                popup.SetActive(true);
            }
            else
            {
                for (int i = 0; i < tagSelParent.transform.childCount; i++)
                {
                    if (tagSelParent.transform.GetChild(i).Find("tag/id").GetComponent<Text>().text == is_selected_tag_id.ToString())
                    {
                        m_selTagObj[i].transform.Find("tag/id").GetComponent<Text>().text = Global.taglist[index].id.ToString();
                        m_selTagObj[i].transform.Find("tag/name").GetComponent<Text>().text = Global.taglist[index].name;
                        m_selTagObj[i].transform.Find("tag/is_pay_after").GetComponent<Text>().text = Global.taglist[index].is_pay_after.ToString();
                        m_selTagObj[i].transform.Find("tag/status").GetComponent<Text>().text = Global.taglist[index].status.ToString();
                        m_selTagObj[i].transform.Find("tag/is_blank").GetComponent<Text>().text = Global.taglist[index].is_blank.ToString();
                        //if(Global.taglist[index].is_blank == 1)
                        //{
                        //    m_selTagObj[i].transform.Find("tag/name").GetComponent<Text>().color = Color.white;
                        //    m_selTagObj[i].transform.Find("Btn").GetComponent<Text>().color = Color.white;
                        //    m_selTagObj[i].transform.Find("Btn").GetComponent<Text>().text = "삭제";
                        //    int _i = Global.taglist[index].id;
                        //    m_selTagObj[i].transform.Find("Btn").GetComponent<Button>().onClick.RemoveAllListeners();
                        //    m_selTagObj[i].transform.Find("Btn").GetComponent<Button>().onClick.AddListener(delegate () { onDelTagItem(_i); });
                        //}
                        //else
                        //{
                            m_selTagObj[i].transform.Find("tag/name").GetComponent<Text>().color = Color.green;
                            m_selTagObj[i].transform.Find("Btn").GetComponent<Text>().color = Color.white;
                            m_selTagObj[i].transform.Find("Btn").GetComponent<Text>().text = "교환";
                            string _i = Global.taglist[index].id;
                            m_selTagObj[i].transform.Find("Btn").GetComponent<Button>().onClick.RemoveAllListeners();
                            m_selTagObj[i].transform.Find("Btn").GetComponent<Button>().onClick.AddListener(delegate () { onChangeTagItem(_i); });
                        //}
                        is_selected_change = false;
                        bool is_found = false;
                        for(int j = 0; j < m_destTagId.Count; j ++)
                        {
                            if(m_destTagId[j] == is_selected_tag_id)
                            {
                                m_destTagId[j] = Global.taglist[index].id;
                                is_found = true;
                                break;
                            }
                        }
                        if(!is_found)
                        {
                            m_originTagId.Add(is_selected_tag_id);
                            m_destTagId.Add(Global.taglist[index].id);
                        }
                        break;
                    }
                }
            }
        }
        else
        {
            if (status == 0 || status == 3)
            {
                err_str.text = "선택하신 TAG는 등록이 불가합니다.\n사유 : 분실된 TAG(or 비활성 TAG)";
                err_popup.SetActive(true);
            }
            else
            {
                if (status == 2)
                {
                    tmp_index = index;
                    popup_str.text = "다른 테이블에 등록된 TAG입니다.\n테이블을 이동하시겠습니까?";
                    popup.SetActive(true);
                }
                else
                {
                    GameObject tmpObj = Instantiate(tagSelItem);
                    tmpObj.transform.SetParent(tagSelParent.transform);
                    //tmpObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                    //tmpObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                    //float left = 0;
                    //float right = 0;
                    //tmpObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                    //tmpObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                    //tmpObj.transform.localScale = Vector3.one;
                    tmpObj.transform.Find("tag/name").GetComponent<Text>().text = Global.taglist[index].name;
                    tmpObj.transform.Find("tag/id").GetComponent<Text>().text = Global.taglist[index].id.ToString();
                    tmpObj.transform.Find("tag/is_pay_after").GetComponent<Text>().text = Global.taglist[index].is_pay_after.ToString();
                    tmpObj.transform.Find("tag/status").GetComponent<Text>().text = Global.taglist[index].status.ToString();
                    tmpObj.transform.Find("tag/is_blank").GetComponent<Text>().text = Global.taglist[index].is_blank.ToString();
                    if(Global.taglist[index].is_blank == 1)
                    {
                        tmpObj.transform.Find("tag/name").GetComponent<Text>().color = Color.white;
                        string _i = Global.taglist[index].id;
                        tmpObj.transform.Find("Btn").GetComponent<Text>().text = "삭제";
                        tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.RemoveAllListeners();
                        tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.AddListener(delegate () { onDelTagItem(_i); });
                    }
                    else
                    {
                        tmpObj.transform.Find("tag/name").GetComponent<Text>().color = Color.green;
                        string _i = Global.taglist[index].id;
                        tmpObj.transform.Find("Btn").GetComponent<Text>().text = "교환";
                        tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.RemoveAllListeners();
                        tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.AddListener(delegate () { onChangeTagItem(_i); });
                    }
                    m_selTagObj.Add(tmpObj);
                }
            }
        }
    }

    void onChangeTagItem(string id)
    {
        for (int i = 0; i < tagSelParent.transform.childCount; i++)
        {
            if (tagSelParent.transform.GetChild(i).Find("tag/id").GetComponent<Text>().text == id.ToString())
            {
                m_selTagObj[i].transform.Find("tag/name").GetComponent<Text>().color = Color.black;
                m_selTagObj[i].transform.Find("Btn").GetComponent<Text>().color = Color.black;
                is_selected_tag_id = id;
                is_selected_change = true;
                break;
            }
        }
    }

    void onDelTagItem(string id)
    {
        for(int i = 0; i < tagSelParent.transform.childCount; i++)
        {
            if(tagSelParent.transform.GetChild(i).Find("tag/id").GetComponent<Text>().text == id.ToString())
            {
                DestroyImmediate(m_selTagObj[i].gameObject);
                m_selTagObj.Remove(m_selTagObj[i]);
                break;
            }
        }
    }

    public void onConfirm()
    {
        if(is_pre_regTag)
        {
            is_pre_regTag = false;
            popup.SetActive(false);
            RegPreTag(preTagData);
            return;
        }
        if (tmp_index < 0)
        {
            popup.SetActive(false);
            return;
        }
        if (is_selected_change)
        {
            for (int i = 0; i < tagSelParent.transform.childCount; i++)
            {
                if (tagSelParent.transform.GetChild(i).Find("tag/id").GetComponent<Text>().text == is_selected_tag_id.ToString())
                {
                    m_selTagObj[i].transform.Find("tag/id").GetComponent<Text>().text = Global.taglist[tmp_index].id.ToString();
                    m_selTagObj[i].transform.Find("tag/name").GetComponent<Text>().text = Global.taglist[tmp_index].name;
                    m_selTagObj[i].transform.Find("tag/is_pay_after").GetComponent<Text>().text = Global.taglist[tmp_index].is_pay_after.ToString();
                    m_selTagObj[i].transform.Find("tag/status").GetComponent<Text>().text = Global.taglist[tmp_index].status.ToString();
                    m_selTagObj[i].transform.Find("tag/is_blank").GetComponent<Text>().text = Global.taglist[tmp_index].is_blank.ToString();
                    //if(Global.taglist[tmp_index].is_blank == 0)
                    //{
                        m_selTagObj[i].transform.Find("tag/name").GetComponent<Text>().color = Color.green;
                        string _i = Global.taglist[tmp_index].id;
                        m_selTagObj[i].transform.Find("Btn").GetComponent<Text>().text = "교환";
                        m_selTagObj[i].transform.Find("Btn").GetComponent<Button>().onClick.RemoveAllListeners();
                        m_selTagObj[i].transform.Find("Btn").GetComponent<Button>().onClick.AddListener(delegate () { onChangeTagItem(_i); });
                    //}
                    //else
                    //{
                    //    m_selTagObj[i].transform.Find("tag/name").GetComponent<Text>().color = Color.white;
                    //    int _i = Global.taglist[tmp_index].id;
                    //    m_selTagObj[i].transform.Find("Btn").GetComponent<Text>().text = "삭제";
                    //    m_selTagObj[i].transform.Find("Btn").GetComponent<Button>().onClick.RemoveAllListeners();
                    //    m_selTagObj[i].transform.Find("Btn").GetComponent<Button>().onClick.AddListener(delegate () { onDelTagItem(_i); });
                    //}
                    m_selTagObj[i].transform.Find("Btn").GetComponent<Text>().color = Color.white;
                    is_selected_change = false;
                    bool is_found = false;
                    for (int j = 0; j < m_destTagId.Count; j++)
                    {
                        if (m_destTagId[j] == is_selected_tag_id)
                        {
                            m_destTagId[j] = Global.taglist[tmp_index].id;
                            is_found = true;
                            break;
                        }
                    }
                    if (!is_found)
                    {
                        m_originTagId.Add(is_selected_tag_id);
                        m_destTagId.Add(Global.taglist[tmp_index].id);
                    }
                    break;
                }
            }
        }
        else
        {
            GameObject tmpObj = Instantiate(tagSelItem);
            tmpObj.transform.SetParent(tagSelParent.transform);
            //tmpObj.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
            //tmpObj.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
            //float left = 0;
            //float right = 0;
            //tmpObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
            //tmpObj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
            //tmpObj.transform.localScale = Vector3.one;
            tmpObj.transform.Find("tag/name").GetComponent<Text>().text = Global.taglist[tmp_index].name;
            tmpObj.transform.Find("tag/id").GetComponent<Text>().text = Global.taglist[tmp_index].id.ToString();
            tmpObj.transform.Find("tag/is_pay_after").GetComponent<Text>().text = Global.taglist[tmp_index].is_pay_after.ToString();
            tmpObj.transform.Find("tag/status").GetComponent<Text>().text = Global.taglist[tmp_index].status.ToString();
            tmpObj.transform.Find("tag/is_blank").GetComponent<Text>().text = Global.taglist[tmp_index].is_blank.ToString();
            if(Global.taglist[tmp_index].is_blank == 0)
            {
                tmpObj.transform.Find("tag/name").GetComponent<Text>().color = Color.green;
                tmpObj.transform.Find("Btn").GetComponent<Text>().text = "교환";
                string _i = Global.taglist[tmp_index].id;
                tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.RemoveAllListeners();
                tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.AddListener(delegate () { onChangeTagItem(_i); });
            }
            else
            {
                tmpObj.transform.Find("tag/name").GetComponent<Text>().color = Color.white;
                tmpObj.transform.Find("Btn").GetComponent<Text>().text = "삭제";
                string _i = Global.taglist[tmp_index].id;
                tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.RemoveAllListeners();
                tmpObj.transform.Find("Btn").GetComponent<Button>().onClick.AddListener(delegate () { onDelTagItem(_i); });
            }
            m_selTagObj.Add(tmpObj);
        }
        popup.SetActive(false);
    }

    public void Reg()
    {
        List<TagInfo> tglist = new List<TagInfo>();
        string tag_ids = "[";
        for (int i = 0; i < tagSelParent.transform.childCount; i ++)
        {
            if (i == 0)
            {
                tag_ids += "{";
            }
            else
            {
                tag_ids += ",{";
            }
            tag_ids += "\"id\":\"" + tagSelParent.transform.GetChild(i).Find("tag/id").GetComponent<Text>().text + "\"}";
            try
            {
                TagInfo tginfo = new TagInfo();
                tginfo.id = tagSelParent.transform.GetChild(i).Find("tag/id").GetComponent<Text>().text;
                tginfo.is_pay_after = int.Parse(tagSelParent.transform.GetChild(i).Find("tag/is_pay_after").GetComponent<Text>().text);
                tginfo.status = int.Parse(tagSelParent.transform.GetChild(i).Find("tag/status").GetComponent<Text>().text);
                tginfo.name = tagSelParent.transform.GetChild(i).Find("tag/name").GetComponent<Text>().text;
                tginfo.is_blank = int.Parse(tagSelParent.transform.GetChild(i).Find("tag/is_blank").GetComponent<Text>().text);
                if(tginfo.is_pay_after == 1)
                {
                    tglist.Add(tginfo);
                }
            }catch(Exception ex)
            {

            }
        }
        tag_ids += "]";
        string convert_tag_ids = "[";
        if(m_originTagId.Count == m_destTagId.Count && m_originTagId.Count > 0)
        {
            for(int i = 0; i < m_originTagId.Count; i ++)
            {
                if(i == 0)
                {
                    convert_tag_ids += "{";
                } else
                {
                    convert_tag_ids += ",{";
                }
                convert_tag_ids += "\"origin_id\":\"" + m_originTagId[i] + "\",\"dest_id\":\"" + m_destTagId[i] + "\"}";
            }
        }
        convert_tag_ids += "]";
        WWWForm form = new WWWForm();
        form.AddField("tag_ids", tag_ids);
        form.AddField("convert_tag_ids", convert_tag_ids);
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("table_id", Global.cur_tInfo.tid);
        WWW www = new WWW(Global.api_url + Global.reg_tags_api, form);
        StartCoroutine(RegTags(www, tglist));
    }

    IEnumerator RegTags(WWW www, List<TagInfo> taglist)
    {
        yield return www;
        if(www.error == null)
        {
            TableInfo tmp = Global.tableGroupList[Global.cur_tInfo.tgNo].tablelist[Global.cur_tInfo.tNo];
            tmp.taglist = taglist;
            Global.tableGroupList[Global.cur_tInfo.tgNo].tablelist[Global.cur_tInfo.tNo] = tmp;
            StartCoroutine(GotoScene("tableUsage"));
        }
        else
        {
            err_str.text = "태그 등록에 실패하였습니다.";
            err_popup.SetActive(true);
        }
    }

    public void onCancel()
    {
        popup.SetActive(false);
    }

    public void onBack()
    {
        StartCoroutine(GotoScene("tableUsage"));
    }

    public void onErrorPop()
    {
        err_popup.SetActive(false);
        readTag.Select();
        readTag.ActivateInputField();
    }

    float time = 0f;
    private bool is_pre_regTag = false;
    private JSONNode preTagData;

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
