using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SocketIO;
using System.Threading.Tasks;

public class PrepayTagOrderManager : MonoBehaviour
{
    public GameObject categoryItemParent;
    public GameObject categoryItem;
    public GameObject order_item;
    public GameObject order_parent;
    public GameObject menuParent;
    public GameObject menuItem;
    public GameObject menuToggle;
    public Text total_priceTxt;
    public Text tagName;
    public Text tagPriceTxt;
    public Toggle selAllCheck;
    public GameObject err_popup;
    public Text err_msg;
    public GameObject select_popup;
    public Text select_str;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;

    TagOrderInfo tagOrderInfo = new TagOrderInfo();
    TableOrderInfo tableorderlist = new TableOrderInfo();
    List<GameObject> m_orderListObj = new List<GameObject>();
    List<GameObject> m_categorylistObj = new List<GameObject>();
    List<GameObject> m_menuListObj = new List<GameObject>();
    List<int> selected_item_id = new List<int>();
    float time = 0f;
    DateTime ctime;
    bool is_socket_open = false;

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(checkSdate());
        tagName.text = Global.cur_tagInfo.tag_name;
        total_priceTxt.text = Global.GetPriceFormat(Global.cur_tagInfo.remain);
        tagPriceTxt.text = Global.GetPriceFormat(Global.cur_tagInfo.remain);
        StartCoroutine(LoadAllMenulist());
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
        JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
        if (jsonNode["tag_id"] == Global.cur_tagInfo.tag_id)
        {
            StartCoroutine(GotoScene("prepayTagMenuOrder"));
        }
    }

    public void createOrder(SocketIOEvent e)
    {
        JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
        try
        {
            //주문서 출력
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
        if (jsonNode["tag_id"] == Global.cur_tagInfo.tag_id)
        {
            StartCoroutine(GotoScene("prepayTagMenuOrder"));
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
            //        err_msg.text = "영업일을 " + string.Format("{0:D4}-{1:D2}-{2:D2}", ctime.Year, ctime.Month, ctime.Day) + " 로 변경하시겠습니까?\n영업일을 변경하시려면 모든 테이블의 결제를 완료하세요.";
            //    }
            //    else
            //    {
            //        err_msg.text = "결제를 완료하지 않은 재결제가 있습니다. 영업일 변경을 위해 결제를 완료해주세요.\n취소시간: " + jsonNode["closetime"];
            //    }
            //    err_popup.SetActive(true);
            //}
            //else
            {
                err_popup.SetActive(true);
                err_msg.text = "영업일자가 " + string.Format("{0:D4}-{1:D2}-{2:D2}", ctime.Year, ctime.Month, ctime.Day) + " 로 변경되었습니다.";
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

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

    void onSelectAll(bool value)
    {
        for (int i = 0; i < order_parent.transform.childCount; i++)
        {
            order_parent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn = value;
        }
    }

    void onSelItem(GameObject toggleObj)
    {
        Debug.Log("Toogle : " + toggleObj.GetComponent<Toggle>().isOn);
        if (toggleObj.GetComponent<Toggle>().isOn)
        {
            toggleObj.GetComponent<Toggle>().isOn = false;
        }
        else
        {
            toggleObj.GetComponent<Toggle>().isOn = true;
        }
    }

    public void OnMenuToggle()
    {
        for(int i = 0; i < order_parent.transform.childCount; i ++)
        {
            try
            {
                order_parent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn = menuToggle.GetComponent<Toggle>().isOn;
            }
            catch (Exception ex)
            {

            }
        }
    }

    IEnumerator LoadAllMenulist()
    {
        while (categoryItemParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(categoryItemParent.transform.GetChild(0).gameObject));
        }
        while (categoryItemParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        Global.categorylist.Clear();
        //모든 메뉴정보 가져오기
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("app_type", Global.app_type);
        WWW www = new WWW(Global.api_url + Global.get_categorylist_api, form);
        StartCoroutine(GetCategorylistFromApi(www));
    }

    IEnumerator GetCategorylistFromApi(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            JSONNode c_list = JSON.Parse(jsonNode["categorylist"].ToString()/*.Replace("\"", "")*/);
            //json구조 해석해서 Global에 값을 대입
            Debug.Log("category list count = " + c_list.Count);
            for (int i = 0; i < c_list.Count; i++)
            {
                if (c_list[i]["name"] != "TAG")      //TAG상품 제외
                {
                    firscateno = c_list[i]["id"];
                    break;
                }
            }
            for (int i = 0; i < c_list.Count; i++)
            {
                Debug.Log("loading category list..");
                if(c_list[i]["name"] != "TAG")  //TAG상품 제외
                {
                    CategoryInfo cateInfo = new CategoryInfo();
                    try
                    {
                        cateInfo.name = c_list[i]["name"];
                        cateInfo.id = c_list[i]["id"];
                    }
                    catch (Exception ex)
                    {

                    }
                    cateInfo.menulist = new List<MenuInfo>();
                    JSONNode m_list = JSON.Parse(c_list[i]["menulist"].ToString()/*.Replace("\"", "")*/);
                    int menuCnt = m_list.Count;
                    for (int j = 0; j < menuCnt; j++)
                    {
                        MenuInfo minfo = new MenuInfo();
                        minfo.name = m_list[j]["name"];
                        minfo.engname = m_list[j]["engname"];
                        minfo.barcode = m_list[j]["barcode"];
                        minfo.contents = m_list[j]["contents"];
                        minfo.sort_order = m_list[j]["sort_order"].AsInt;
                        minfo.id = m_list[j]["id"];
                        minfo.price = m_list[j]["price"];
                        minfo.pack_price = m_list[j]["pack_price"];
                        minfo.is_best = m_list[j]["is_best"];
                        minfo.sell_amount = m_list[j]["sell_amount"].AsInt;
                        minfo.sell_tap = m_list[j]["sell_tap"].AsInt;
                        minfo.is_soldout = m_list[j]["is_soldout"].AsInt;
                        minfo.product_type = c_list[i]["product_type"].AsInt;
                        cateInfo.menulist.Add(minfo);
                    }
                    Global.categorylist.Add(cateInfo);
                }
            }

            //UI에 추가
            m_categorylistObj.Clear();
            for (int i = 0; i < Global.categorylist.Count; i++)
            {
                try
                {
                    GameObject tmp = Instantiate(categoryItem);
                    tmp.transform.SetParent(categoryItemParent.transform);
                    //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                    //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                    //float left = 0;
                    //float right = 0;
                    //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                    //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                    //tmp.transform.localScale = Vector3.one;
                    tmp.GetComponent<Text>().text = Global.categorylist[i].name;
                    tmp.transform.Find("id").GetComponent<Text>().text = Global.categorylist[i].id.ToString();
                    string t_cateNo = Global.categorylist[i].id;
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { StartCoroutine(LoadMenuList(t_cateNo)); });
                    m_categorylistObj.Add(tmp);
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }
            if (c_list.Count > 0 && firscateno != "")
                StartCoroutine(LoadMenuList(firscateno));
        }
        else
        {
            Debug.Log(www.error);
        }
    }

    string firscateno = "";
    string oldSelectedCategoryNo = "";
    List<OrderCartInfo> cartlist = new List<OrderCartInfo>();
    int order_price = 0;

    IEnumerator Destroy_Object(GameObject obj)
    {
        DestroyImmediate(obj);
        yield return null;
    }

    IEnumerator LoadMenuList(string cateno)
    {
        Debug.Log("old = " + oldSelectedCategoryNo + ", new = " + cateno);
        //선택된 카테고리 볼드체로.
        try
        {
            for (int i = 0; i < categoryItemParent.transform.childCount; i++)
            {
                if (categoryItemParent.transform.GetChild(i).transform.Find("id").GetComponent<Text>().text == cateno.ToString())
                {
                    categoryItemParent.transform.GetChild(i).GetComponent<Text>().color = Global.selected_color;
                }
                else
                {
                    categoryItemParent.transform.GetChild(i).GetComponent<Text>().color = Color.white;
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
        for (int i = 0; i < Global.categorylist.Count; i++)
        {
            if (Global.categorylist[i].id == cateno)
            {
                minfoList = Global.categorylist[i].menulist; break;
            }
        }
        int menuCnt = minfoList.Count;
        m_menuListObj.Clear();
        for (int i = 0; i < menuCnt; i++)
        {
            try
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
                tmp.transform.Find("id").GetComponent<Text>().text = minfoList[i].id.ToString();
                tmp.transform.Find("name").GetComponent<Text>().text = minfoList[i].name;
                tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(minfoList[i].price);
                OrderCartInfo cinfo = new OrderCartInfo();
                cinfo.name = minfoList[i].name;
                cinfo.menu_id = minfoList[i].id;
                cinfo.product_type = minfoList[i].product_type;
                cinfo.price = minfoList[i].price;
                cinfo.is_best = minfoList[i].is_best;
                cinfo.amount = 1;
                cinfo.status = 0;
                if (minfoList[i].is_soldout == 1)
                {
                    tmp.transform.Find("name").GetComponent<Text>().color = Color.black;
                    tmp.transform.Find("price").GetComponent<Text>().color = Color.black;
                }
                else
                {
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { addList(cinfo); });
                }
                m_menuListObj.Add(tmp);
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    public void addList(OrderCartInfo cinfo)
    {
        Debug.Log("add list");
        selAllCheck.onValueChanged.RemoveAllListeners();
        selAllCheck.onValueChanged.AddListener((value) => {
            onSelectAll(value);
        }
        );
        if (order_price + cinfo.price > Global.cur_tagInfo.remain)
        {
            err_msg.text = "잔액이 부족하여 주문할수 없습니다.";
            err_popup.SetActive(true);
            return;
        }
        cartlist = Global.addOneCartItem(cinfo, cartlist);
        order_price += cinfo.price;
        total_priceTxt.text = Global.GetPriceFormat(Global.cur_tagInfo.remain - order_price);

        bool is_found = false;
        for (int i = 0; i < order_parent.transform.childCount; i++)
        {
            if (order_parent.transform.GetChild(i).Find("menu_id").GetComponent<Text>().text == cinfo.menu_id.ToString())
            {
                is_found = true;
                try
                {
                    order_parent.transform.GetChild(i).Find("cnt").GetComponent<Text>().text =
                        (int.Parse(order_parent.transform.GetChild(i).Find("cnt").GetComponent<Text>().text) + 1).ToString();
                    order_parent.transform.GetChild(i).Find("price").GetComponent<Text>().text =
                        Global.GetPriceFormat(Global.GetConvertedPrice(order_parent.transform.GetChild(i).Find("price").GetComponent<Text>().text) + cinfo.price);
                    break;
                }
                catch (Exception ex)
                {

                }
            }
        }
        if (!is_found)
        {
            GameObject tmp = Instantiate(order_item);
            tmp.transform.SetParent(order_parent.transform);
            //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
            //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
            //float left = 0;
            //float right = 0;
            //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
            //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
            //tmp.transform.localScale = Vector3.one;
            tmp.transform.Find("name").GetComponent<Text>().text = cinfo.name;
            tmp.transform.Find("cnt").GetComponent<Text>().text = cinfo.amount.ToString();
            tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(cinfo.price);
            //tmp.transform.Find("id").GetComponent<Text>().text = cinfo.order_id.ToString();
            tmp.transform.Find("menu_id").GetComponent<Text>().text = cinfo.menu_id.ToString();
            tmp.transform.Find("product_type").GetComponent<Text>().text = cinfo.product_type.ToString();
            GameObject toggleObj = tmp.transform.Find("check").gameObject;
            tmp.GetComponent<Button>().onClick.RemoveAllListeners();
            tmp.GetComponent<Button>().onClick.AddListener(delegate () { onSelItem(toggleObj); });
            m_orderListObj.Add(tmp);
        }
    }

    public void Pack()
    {
        bool takeout = false;

        float total_price = 0;

        for (int i = 0; i < cartlist.Count; i++)
        {
            if (cartlist[i].is_best >= 999)
            {
                err_msg.text = "포장이 불가한 메뉴입니다.";
                err_popup.SetActive(true);
                takeout = true;
                break;
            }
        }
        if (!takeout)
        {
            //포장처리
            WWWForm form = new WWWForm();
            form.AddField("type", 1);
            string oinfo = "[";
            for (int i = 0; i < cartlist.Count; i++)
            {
                if (i == 0)
                {
                    oinfo += "{";
                }
                else
                {
                    oinfo += ",{";
                }
                float new_price = 0f;
                if (cartlist[i].is_best >= 100)
                {
                    new_price = cartlist[i].price + cartlist[i].is_best;
                }
                else
                {
                    new_price = cartlist[i].price * (100 + cartlist[i].is_best) / 100;
                }
                oinfo += "\"menu_id\":\"" + cartlist[i].menu_id + "\","
                    + "\"menu_name\":\"" + cartlist[i].name + "\","
                    + "\"product_type\":\"" + cartlist[i].product_type + "\","
                    + "\"price\":" + new_price.ToString() + ","
                    + "\"quantity\":" + cartlist[i].amount.ToString() + "}";
                total_price += new_price * cartlist[i].amount;
            }
            oinfo += "]";
            if (total_price > Global.cur_tagInfo.remain)
            {
                err_msg.text = "잔액이 부족하여 주문할수 없습니다.";
                err_popup.SetActive(true);
                return;
            }
            Debug.Log(oinfo);
            form.AddField("is_pay_after", 0);
            form.AddField("order_info", oinfo);
            form.AddField("pub_id", Global.userinfo.pub.id);
            form.AddField("tag_id", Global.cur_tagInfo.tag_id);
            DateTime dt = Global.GetSdate();
            form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
            form.AddField("pos_no", Global.setinfo.pos_no);
            WWW www = new WWW(Global.api_url + Global.order_api, form);
            StartCoroutine(ProcessOrder(www));
        }
    }

    IEnumerator ProcessOrder(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                int orderSeq = jsonNode["orderSeq"].AsInt;
                WWWForm form = new WWWForm();
                form.AddField("pub_id", Global.userinfo.pub.id);
                DateTime dt = Global.GetSdate();
                form.AddField("curdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
                form.AddField("orderSeq", orderSeq);
                WWW www_orderSheet = new WWW(Global.api_url + Global.get_ordersheet_api, form);
                StartCoroutine(GetOrderSheet(www_orderSheet));
            }
            else
            {
                err_msg.text = jsonNode["msg"];
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_msg.text = "주문시 알지 못할 오류가 발생하였습니다.";
            err_popup.SetActive(true);
        }
    }

    IEnumerator GetOrderSheet(WWW www)
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
                    //주문서 출력
                    string kitorderno = Global.GetNoFormat(jsonNode["orderSeq"].AsInt);
                    string tableName = jsonNode["tableName"];
                    string tagName = jsonNode["tagName"];
                    string is_pack = (jsonNode["is_pack"].AsInt) == 0 ? "" : "T";
                    List<OrderItem> orders = new List<OrderItem>();
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
                    Global.cur_tagInfo = new CurTagInfo();
                    StartCoroutine(GotoScene("prepayTagUsage"));
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }
        }
    }

    public void Order()
    {
        //주문 api
        WWWForm form = new WWWForm();
        form.AddField("type", 0);

        string oinfo = "[";
        for (int i = 0; i < cartlist.Count; i++)
        {
            if (i == 0)
            {
                oinfo += "{";
            }
            else
            {
                oinfo += ",{";
            }
            oinfo += "\"menu_id\":\"" + cartlist[i].menu_id + "\","
                + "\"menu_name\":\"" + cartlist[i].name + "\","
                + "\"product_type\":\"" + cartlist[i].product_type + "\","
                + "\"price\":" + cartlist[i].price.ToString() + ","
                + "\"quantity\":" + cartlist[i].amount.ToString() + "}";
        }
        oinfo += "]";
        Debug.Log(oinfo);
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("tag_id", Global.cur_tagInfo.tag_id);
        form.AddField("order_info", oinfo);
        form.AddField("is_pay_after", 0);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        form.AddField("pos_no", Global.setinfo.pos_no);
        WWW www = new WWW(Global.api_url + Global.order_api, form);
        StartCoroutine(ProcessOrder(www));
    }

    public void onCloseErrPopup()
    {
        err_popup.SetActive(false);
    }

    public void onCloseSelPopup()
    {
        select_popup.SetActive(false);
    }

    public void onBack()
    {
        Global.cur_tagInfo = new CurTagInfo();
        StartCoroutine(GotoScene("main"));
    }

    public void AddTag()
    {
        StartCoroutine(GotoScene("tagAdd"));
    }

    public void Usage()
    {
        StartCoroutine(GotoScene("prepayTagUsage"));
    }

    public void onCancelOrder()
    {
        //주문취소
        //selected_item_id.Clear();
        //bool is_cooking = false;
        for (int i = 0; i < m_orderListObj.Count; i++)
        {
            try
            {
                if (m_orderListObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    Debug.Log(i);
                    string item = m_orderListObj[i].transform.Find("menu_id").GetComponent<Text>().text;
                    //if (m_orderListObj[i].transform.Find("status").GetComponent<Text>().text == "1")
                    //{
                    //    is_cooking = true;
                    //    string oid_str = m_orderListObj[i].transform.Find("order_ids").GetComponent<Text>().text;
                    //    string[] oid_tmp = oid_str.Split(',');
                    //    for (int j = 0; j < oid_tmp.Length; j++)
                    //    {
                    //        selected_item_id.Add(int.Parse(oid_tmp[j]));
                    //    }
                    //}
                    //else
                    //{
                    //이미 보여진 항목 삭제
                    for (int j = 0; j < cartlist.Count; j++)
                    {
                        if (cartlist[j].menu_id == item)
                        {
                            order_price -= cartlist[j].price * cartlist[j].amount;
                            total_priceTxt.text = Global.GetPriceFormat(Global.cur_tagInfo.remain - order_price);
                            cartlist.Remove(cartlist[j]);
                            break;
                        }
                    }
                    DestroyImmediate(m_orderListObj[i]);
                    m_orderListObj.Remove(m_orderListObj[i]);
                    i--;
                    //}
                }
            }
            catch (Exception ex)
            {

            }
        }
        //if (selected_item_id.Count == 0)
        //{
        //    err_msg.text = "취소할 주문을 선택하세요.";
        //    err_popup.SetActive(true);
        //}
        //else
        //{
        //    if (is_cooking)
        //    {
        //        select_str.text = "현재 조리 중인 메뉴입니다. 주문을 취소하시겠습니까?";
        //        select_popup.SetActive(true);
        //    }
        //    else
        //    {
        //        onConfirmPopup();
        //    }
        //}
    }

    public void onConfirmPopup()
    {
        //취소
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("tag_id", Global.cur_tagInfo.tag_id);
        form.AddField("pos_no", Global.setinfo.pos_no);
        string oinfo = "[";
        for (int i = 0; i < selected_item_id.Count; i++)
        {
            if (i == 0)
            {
                oinfo += "{";
            }
            else
            {
                oinfo += ",{";
            }
            oinfo += "\"order_id\":\"" + selected_item_id[i] + "\"}";
        }
        oinfo += "]";
        Debug.Log(oinfo);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        form.AddField("order_info", oinfo);
        WWW www = new WWW(Global.api_url + Global.cancel_order_api, form);
        StartCoroutine(CancelOrder(www));
    }

    IEnumerator CancelOrder(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                try
                {
                    //주문서 출력
                    string kitorderno = Global.GetNoFormat(jsonNode["orderSeq"].AsInt);
                    string tableName = jsonNode["tableName"];
                    string tagName = jsonNode["tagName"];
                    string is_pack = (jsonNode["is_pack"].AsInt) == 0 ? "" : "T";
                    List<OrderItem> orders = new List<OrderItem>(); 
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
                    StartCoroutine(GotoScene("main"));
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }
            else
            {
                err_msg.text = jsonNode["msg"];
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_msg.text = "주문취소시에 알지 못할 오류가 발생하였습니다.";
            err_popup.SetActive(true);
        }
    }

    public void onErrorPop()
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
