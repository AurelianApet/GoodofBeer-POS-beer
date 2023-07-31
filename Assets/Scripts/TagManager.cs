using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using SimpleJSON;
using System.Text;
using SocketIO;
using System.Threading.Tasks;

public class TagManager : MonoBehaviour
{
    public GameObject tagItem;
    public GameObject tagItemParent;
    public Text regCntTxt;
    public Text usingCntTxt;
    public Text lostCntTxt;

    public Text tagName;
    public InputField rfidTxt;
    public InputField qrcodeTxt;
    //public InputField periodTxt;
    public InputField delStartTxt;
    public InputField delEndTxt;
    public InputField addStartTxt;
    public InputField addEndTxt;

    public GameObject addTagPopup;
    public GameObject delTagPopup;
    public GameObject changeTagPopup;
    public GameObject err_popup;
    public Text err_str;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;

    List<GameObject> m_tagItemObj = new List<GameObject>();
    TagManageInfo tgInfo = new TagManageInfo();
    string cur_tag_id = "";
    bool is_socket_open = false;

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(checkSdate());
        SendRequest(1);
        socketObj = Instantiate(socketPrefab);
        //socketObj = GameObject.Find("SocketIO");
        socket = socketObj.GetComponent<SocketIOComponent>();
        socket.On("open", socketOpen);
        socket.On("createOrder", createOrder);
        socket.On("new_notification", new_notification);
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
        StartCoroutine(GotoScene("tagManage"));
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
        StartCoroutine(GotoScene("tagManage"));
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

    void SendRequest(int type = 0)
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        WWW www = new WWW(Global.api_url + Global.get_taglist_api, form);
        StartCoroutine(LoadTaglist(www, type));
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

    IEnumerator LoadTaglist(WWW www, int type)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            tgInfo.reg_cnt = jsonNode["reg_cnt"].AsInt;
            tgInfo.using_cnt = jsonNode["using_cnt"].AsInt;
            tgInfo.lost_cnt = jsonNode["lost_cnt"].AsInt;
            tgInfo.tagList = new List<TagInfo>();
            JSONNode tlist = JSON.Parse(jsonNode["taglist"].ToString()/*.Replace("\"", "")*/);
            Debug.Log(jsonNode);
            for (int i = 0; i < tlist.Count; i++)
            {
                TagInfo tinfo = new TagInfo();
                tinfo.id = tlist[i]["id"];
                tinfo.is_pay_after = tlist[i]["is_pay_after"].AsInt;
                tinfo.name = tlist[i]["name"];
                tinfo.status = tlist[i]["status"].AsInt;
                tinfo.rfid = tlist[i]["rfid"];
                tinfo.qrcode = tlist[i]["qrcode"];
                tinfo.period = tlist[i]["period"].AsInt;
                tgInfo.tagList.Add(tinfo);
            }
            //UI에 추가
            regCntTxt.text = tgInfo.reg_cnt.ToString();
            usingCntTxt.text = tgInfo.using_cnt.ToString();
            lostCntTxt.text = tgInfo.lost_cnt.ToString();
            if (type == 1)
            {
                while (tagItemParent.transform.childCount > 0)
                {
                    try
                    {
                        DestroyImmediate(tagItemParent.transform.GetChild(0).gameObject);
                    }
                    catch (Exception ex)
                    {

                    }
                    yield return new WaitForFixedUpdate();
                }
                for (int i = 0; i < tgInfo.tagList.Count; i++)
                {
                    GameObject tmp = Instantiate(tagItem);
                    tmp.transform.SetParent(tagItemParent.transform);
                    //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                    //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                    //float left = 0;
                    //float right = 0;
                    //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                    //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                    //tmp.transform.localScale = Vector3.one;
                    try
                    {
                        tmp.transform.Find("name").GetComponent<Text>().text = tgInfo.tagList[i].name;
                        tmp.transform.Find("id").GetComponent<Text>().text = tgInfo.tagList[i].id.ToString();
                        tmp.transform.Find("is_pay_after").GetComponent<Text>().text = tgInfo.tagList[i].is_pay_after.ToString();
                        tmp.transform.Find("status").GetComponent<Text>().text = tgInfo.tagList[i].status.ToString();
                        string _i = tgInfo.tagList[i].id;
                        tmp.transform.GetComponent<Button>().onClick.RemoveAllListeners();
                        tmp.transform.GetComponent<Button>().onClick.AddListener(delegate () { onTagPopup(_i); });
                        //0-테이블에 미등록, 1-테이블에 등록, 2-분실, 3-태그 코드 미등록
                        if (tgInfo.tagList[i].status == 0)
                        {
                            tmp.transform.Find("name").GetComponent<Text>().color = new Color(0.3f, 0.3f, 0.3f);
                        }
                        else if (tgInfo.tagList[i].status == 1)
                        {
                            tmp.transform.Find("name").GetComponent<Text>().color = Color.white;
                        }
                        else if (tgInfo.tagList[i].status == 2)
                        {
                            tmp.transform.Find("name").GetComponent<Text>().color = Color.green;
                        }
                        else if (tgInfo.tagList[i].status == 3)
                        {
                            tmp.transform.Find("name").GetComponent<Text>().color = Color.red;
                        }
                        else
                        {
                            tmp.transform.Find("name").GetComponent<Text>().color = new Color(0.3f, 0.3f, 0.3f);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex);
                    }
                    m_tagItemObj.Add(tmp);
                }
            }
        }
    }

    int popup_type = 0;//0-change popup, 1-del popup, 2-add popup

    void onTagPopup(string id)
    {
        changeTagPopup.SetActive(true);
        popup_type = 0;
        for (int i = 0; i < tagItemParent.transform.childCount; i++)
        {
            if (tagItemParent.transform.GetChild(i).Find("id").GetComponent<Text>().text == id.ToString())
            {
                tagName.text = tgInfo.tagList[i].name;
                rfidTxt.text = tgInfo.tagList[i].rfid;
                if(tgInfo.tagList[i].qrcode != "")
                {
                    qrcodeTxt.text = "!" + tgInfo.tagList[i].qrcode;
                }
                else
                {
                    qrcodeTxt.text = "";
                }
                //periodTxt.text = tgInfo.tagList[i].period.ToString();
                cur_tag_id = id;
                if (tgInfo.tagList[i].status == 3)
                {
                    changeTagPopup.transform.Find("background/lostBtn/Text").GetComponent<Text>().text = "회수";
                    changeTagPopup.transform.Find("background/lostBtn").GetComponent<Button>().onClick.RemoveAllListeners();
                    string _i = id;
                    changeTagPopup.transform.Find("background/lostBtn").GetComponent<Button>().onClick.AddListener(delegate() { cancelTag(_i); });
                }
                else
                {
                    changeTagPopup.transform.Find("background/lostBtn/Text").GetComponent<Text>().text = "분실";
                    changeTagPopup.transform.Find("background/lostBtn").GetComponent<Button>().onClick.RemoveAllListeners();
                    string _i = id;
                    changeTagPopup.transform.Find("background/lostBtn").GetComponent<Button>().onClick.AddListener(delegate () { lostTag(_i); });
                }
                break;
            }
        }
    }

    void cancelTag(string id)
    {
        WWWForm form = new WWWForm();
        form.AddField("tag_id", id);
        WWW www = new WWW(Global.api_url + Global.cancel_tag_api, form);
        StartCoroutine(tagProcess(www));
    }

    IEnumerator tagProcess(WWW www)
    {
        yield return www;
        changeTagPopup.SetActive(false);
        if(www.error == null)
        {
            SendRequest(1);
        }
    }

    void lostTag(string id)
    {
        WWWForm form = new WWWForm();
        form.AddField("tag_id", id);
        WWW www = new WWW(Global.api_url + Global.lost_tag_api, form);
        StartCoroutine(tagProcess(www));
    }

    void onDelTagItem(int id)
    {
        for (int i = 0; i < tagItemParent.transform.childCount; i++)
        {
            if (tagItemParent.transform.GetChild(i).Find("tag/id").GetComponent<Text>().text == id.ToString())
            {
                DestroyImmediate(m_tagItemObj[i].gameObject);
                m_tagItemObj.Remove(m_tagItemObj[i]);
                break;
            }
        }
    }

    List<string> checkTagName(string startName, string endName)
    {
        List<string> nameList = new List<string>();
        try
        {
            int start_index = int.Parse(startName.Substring(1, 2));
            char start_char = char.Parse(startName.Substring(0, 1));
            int end_index = int.Parse(endName.Substring(1, 2));
            char end_char = char.Parse(endName.Substring(0, 1));
            int char_diff = end_char - start_char;
            byte[] _t = Encoding.ASCII.GetBytes(start_char.ToString());
            for (int i = 0; i <= char_diff; i++)
            {
                char[] _c = Encoding.ASCII.GetChars(_t);
                byte[] _tt;
                if (i > 0)
                {
                    _tt = Encoding.ASCII.GetBytes("01");
                }
                else
                {
                    _tt = Encoding.ASCII.GetBytes(start_index.ToString());
                }
                for (int j = 0; j <= 99; j ++)
                {
                    if (i == 0 && j < start_index)
                    {
                        continue;
                    }
                    if (i == char_diff && j >= end_index)
                    {
                        break;
                    }
                    char[] _cc = Encoding.ASCII.GetChars(_tt);
                    int _s;
                    if (_cc.Length > 1)
                    {
                        //Debug.Log(_cc[0].ToString() + _cc[1].ToString());
                        _s = int.Parse(_cc[0].ToString() + _cc[1].ToString());
                    }
                    else
                    {
                        _s = int.Parse(_cc[0].ToString());
                    }
                    if (_s != 0)
                    {
                        string tag_name = _c[0] + string.Format("{0:D2}", _s);
                        nameList.Add(tag_name);
                    }
                    byte[] tmp = new byte[1];
                    if(_tt.Length > 1)
                    {
                        tmp[0] = _tt[1];
                        char t = Encoding.ASCII.GetChars(tmp)[0];
                        if (t == '9' && j < 99)
                        {
                            _tt[0]++;
                            char[] _ss = { '0' };
                            _tt[1] = Encoding.ASCII.GetBytes(_ss)[0];
                            if(_tt[0] == 58)
                            {
                                _tt[0] = Encoding.ASCII.GetBytes(_ss)[0];
                            }
                        }
                        else if (j < 99)
                        {
                            _tt[1]++;
                        }
                    }
                    else
                    {
                        char t = Encoding.ASCII.GetChars(_tt)[0];
                        if(t == '9')
                        {
                            byte []_tmpTT = new byte[2];
                            _tmpTT[0] = Encoding.ASCII.GetBytes(new char[] { '1' })[0];
                            _tmpTT[1] = Encoding.ASCII.GetBytes(new char[] { '0' })[0];
                            _tt = _tmpTT;
                        }
                        else
                        {
                            _tt[0]++;
                        }
                    }
                }
                if (i < char_diff)
                {
                    _t[0]++;
                }
            }
        }
        catch (Exception ex)
        {
        }
        return nameList;
    }

    IEnumerator ProcessChangeTagInfo(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                for (int i = 0; i < tgInfo.tagList.Count; i++)
                {
                    if (tgInfo.tagList[i].id == cur_tag_id)
                    {
                        try
                        {
                            TagInfo tinfo = tgInfo.tagList[i];
                            if (rfidTxt.text != "")
                            {
                                tinfo.rfid = rfidTxt.text;
                            }
                            if (qrcodeTxt.text != "")
                            {
                                tinfo.qrcode = qrcodeTxt.text.Remove(0, 1);
                            }
                            //if (periodTxt.text != "")
                            //{
                            //    tinfo.period = int.Parse(periodTxt.text);
                            //}
                            tgInfo.tagList[i] = tinfo;
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
            else
            {
                err_str.text = jsonNode["msg"];
                err_popup.SetActive(true);
            }
        }
        changeTagPopup.SetActive(false);
        SendRequest(1);
    }

    public void onSave()
    {
        switch (popup_type)
        {
            case 0:
                {
                    string rfid = rfidTxt.text;
                    string qr = qrcodeTxt.text;
                    //string period = periodTxt.text;
                    if(rfid == "" && qr == "" || qr != "" && qr.IndexOf("!") != 0)
                    {
                        err_popup.SetActive(true);
                        err_str.text = "태그 정보를 정확히 입력하세요.";
                    }
                    else
                    {
                        if(rfid != "")
                        {
                            for (int i = 0; i < tgInfo.tagList.Count; i++)
                            {
                                if (tgInfo.tagList[i].rfid == rfid)
                                {
                                    if(tgInfo.tagList[i].id != cur_tag_id)
                                    {
                                        err_popup.SetActive(true);
                                        err_str.text = "rfid 정보를 확인하세요.";
                                        return;
                                    }
                                }
                            }
                        }
                        if (qr != "")
                        {
                            try
                            {
                                qr = qr.Remove(0, 1);
                                for (int i = 0; i < tgInfo.tagList.Count; i++)
                                {
                                    if (tgInfo.tagList[i].qrcode == qr)
                                    {
                                        if (tgInfo.tagList[i].id != cur_tag_id)
                                        {
                                            err_popup.SetActive(true);
                                            err_str.text = "qrcode 정보를 확인하세요.";
                                            return;
                                        }
                                    }
                                }
                            }
                            catch(Exception ex)
                            {
                                Debug.Log(ex);
                            }
                        }
                        WWWForm form = new WWWForm();
                        if (rfid != "")
                        {
                            form.AddField("rfid", rfid);
                        }
                        if(qr != "")
                        {
                            form.AddField("qr", qr);
                        }
                        //if(period != "")
                        //{
                        //    form.AddField("period", period);
                        //}
                        form.AddField("tag_id", cur_tag_id);
                        form.AddField("pub_id", Global.userinfo.pub.id);
                        WWW www = new WWW(Global.api_url + Global.change_tag_api, form);
                        StartCoroutine(ProcessChangeTagInfo(www));
                    }
                    break;
                }
            case 1:
                {
                    //del popup;
                    string startTagName = delTagPopup.transform.Find("background/val1").GetComponent<InputField>().text;
                    string endTagName = delTagPopup.transform.Find("background/val2").GetComponent<InputField>().text;
                    WWWForm form = new WWWForm();
                    form.AddField("start_tagname", startTagName);
                    form.AddField("end_tagname", endTagName);
                    form.AddField("pub_id", Global.userinfo.pub.id);
                    form.AddField("type", 1);
                    WWW www = new WWW(Global.api_url + Global.add_tags_api, form);
                    StartCoroutine(ProcessTags(www));
                    break;
                };
            case 2:
                {
                    //add popup;
                    string startTagName = addTagPopup.transform.Find("background/val1").GetComponent<InputField>().text;
                    string endTagName = addTagPopup.transform.Find("background/val2").GetComponent<InputField>().text;
                    WWWForm form = new WWWForm();
                    form.AddField("start_tagname", startTagName);
                    form.AddField("end_tagname", endTagName);
                    form.AddField("pub_id", Global.userinfo.pub.id);
                    form.AddField("type", 0);
                    WWW www = new WWW(Global.api_url + Global.add_tags_api, form);
                    StartCoroutine(ProcessTags(www));
                    break;
                };
        }
    }

    IEnumerator ProcessTags(WWW www)
    {
        yield return www;
        if(popup_type == 2)
        {
            addTagPopup.SetActive(false);
        }
        if (popup_type == 1)
        {
            delTagPopup.SetActive(false);
        }
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
            SendRequest(1);
            }
            else
            {
                err_popup.SetActive(true);
                err_str.text = jsonNode["msg"];
            }
        }
    }

    public void onClose()
    {
        switch (popup_type)
        {
            case 0:{
                    //change popup
                    changeTagPopup.SetActive(false);
                    break;
                };
            case 1:
                {
                    //del popup;
                    delTagPopup.SetActive(false);
                    break;
                };
            case 2:
                {
                    //add popup;
                    addTagPopup.SetActive(false);
                    break;
                };
        }
    }

    public void onRegTag()
    {
        popup_type = 2;
        addTagPopup.SetActive(true);
    }

    public void onBack()
    {
        StartCoroutine(GotoScene("main"));
    }

    public void onDelTag()
    {
        popup_type = 1;
        delTagPopup.SetActive(true);
    }

    public void onPrepayTag()
    {
        StartCoroutine(GotoScene("prepayTagManage"));
    }

    public void onCheck()
    {
        //조회
        SendRequest(1);
    }

    public void onCloseErrPopup()
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
