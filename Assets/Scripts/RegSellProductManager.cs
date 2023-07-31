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

public class RegSellProductManager : MonoBehaviour
{
    public GameObject tagProductItem;
    public GameObject tagProductParent;
    public GameObject productItem;
    public GameObject productParent;
    public GameObject categoryItem;
    public GameObject categoryParent;
    public GameObject menuItem;
    public GameObject menuParent;
    public GameObject barcodeItem;
    public GameObject barcodeParent;

    public GameObject barcodePopup;
    public GameObject regtagPopup;
    public GameObject sellstatusPopup;
    public Text title;
    public Text tagCntTxt;//tag 개수
    public Text chargeTxt;//충전금액
    public Text productTxt;//상품개수
    public Text productPriceTxt;//상품금액
    public Text priceTxt;//총 판매금액
    public Text sumTxt;
    public InputField tagchargeTxt;
    public Text realTxt;
    public InputField rfidTxt;
    public InputField qrTxt;
    public InputField periodTxt;
    public Text menuIdTxt;
    public Toggle selAllTag;
    public Toggle selAllMenu;

    public GameObject select_popup;
    public Text select_str;
    public GameObject err_popup;
    public Text err_msg;
    public QRCodeEncodeController e_qrController;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;

    List<GameObject> m_tagproductObj = new List<GameObject>();
    List<GameObject> m_productObj = new List<GameObject>();
    List<GameObject> m_barcodeObj = new List<GameObject>();
    string firscateno = "";
    string oldSelectedCategoryNo = "";
    List<OrderCartInfo> product_cartlist = new List<OrderCartInfo>();
    int order_price = 0;
    RawImage qrCodeImage;
    DateTime ctime;
    bool is_socket_open = false;

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(checkSdate());
        title.text = Global.userinfo.pub.name;
        SendRequestForMarketInfo();
        StartCoroutine(LoadAllMenulist());
        if (e_qrController != null)
        {
            e_qrController.onQREncodeFinished += qrEncodeFinished;//Add Finished Event
        }
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
            popup_type = 0;
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
            //}
            //else
            {
                err_msg.text = "영업일자가 " + string.Format("{0:D4}-{1:D2}-{2:D2}", ctime.Year, ctime.Month, ctime.Day) + " 로 변경되었습니다.";
            }
            err_popup.SetActive(true);
        }
    }

    void qrEncodeFinished(Texture2D tex)
    {
        if (tex != null && tex != null)
        {
            int width = tex.width;
            int height = tex.height;
            float aspect = width * 1.0f / height;
            //qrCodeImage[cur_bar_index].GetComponent<RectTransform>().sizeDelta = new Vector2(170, 170.0f / aspect);
            qrCodeImage.texture = tex;
        }
        else
        {
        }
    }

    void Encode(string value)
    {
        if (e_qrController != null)
        {
            int errorlog = e_qrController.Encode(value);
        }
    }

    void SendRequestForMarketInfo()
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        WWW www = new WWW(Global.api_url + Global.get_sell_status_api, form);
        StartCoroutine(LoadMarketInfo(www));
    }

    IEnumerator LoadMarketInfo(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if(jsonNode["suc"].AsInt == 1)
            {
                tagCntTxt.text = jsonNode["tagCnt"];
                chargeTxt.text = Global.GetPriceFormat(jsonNode["charge"].AsInt);
                productTxt.text = jsonNode["productCnt"];
                productPriceTxt.text = Global.GetPriceFormat(jsonNode["productPrice"].AsInt);
                priceTxt.text = Global.GetPriceFormat(jsonNode["price"].AsInt);
            }
        }
    }

    IEnumerator LoadAllMenulist()
    {
        while (categoryParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(categoryParent.transform.GetChild(0).gameObject));
        }
        while (categoryParent.transform.childCount > 0)
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
            Debug.Log("category list count = " + c_list.Count);
            if (c_list.Count > 0)
            {
                firscateno = c_list[0]["id"];
            }
            for (int i = 0; i < c_list.Count; i++)
            {
                Debug.Log("loading category list..");
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
                    minfo.contents = m_list[j]["contents"];
                    minfo.barcode = m_list[j]["barcode"];
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

            //UI에 추가
            for (int i = 0; i < c_list.Count; i++)
            {
                GameObject tmp = Instantiate(categoryItem);
                tmp.transform.SetParent(categoryParent.transform);
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
            }

            if (c_list.Count > 0 && firscateno != "")
                StartCoroutine(LoadMenuList(firscateno));
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

    IEnumerator LoadMenuList(string cateno)
    {
        Debug.Log("old = " + oldSelectedCategoryNo + ", new = " + cateno);
        //선택된 카테고리 볼드체로.
        try
        {
            for (int i = 0; i < categoryParent.transform.childCount; i++)
            {
                if(oldSelectedCategoryNo != "")
                {
                    if (categoryParent.transform.GetChild(i).Find("id").GetComponent<Text>().text == oldSelectedCategoryNo.ToString())
                    {
                        categoryParent.transform.GetChild(i).GetComponent<Text>().color = Color.white;
                    }
                }
                if (categoryParent.transform.GetChild(i).Find("id").GetComponent<Text>().text == cateno.ToString())
                {
                    categoryParent.transform.GetChild(i).GetComponent<Text>().color = Global.selected_color;
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
        string category_name = "";
        for (int i = 0; i < Global.categorylist.Count; i++)
        {
            if (Global.categorylist[i].id == cateno)
            {
                minfoList = Global.categorylist[i].menulist;
                category_name = Global.categorylist[i].name;
                break;
            }
        }
        int menuCnt = minfoList.Count;
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
                tmp.transform.Find("barcode").GetComponent<Text>().text = minfoList[i].barcode;

                OrderCartInfo cinfo = new OrderCartInfo();
                cinfo.name = minfoList[i].name;
                cinfo.menu_id = minfoList[i].id;
                cinfo.price = minfoList[i].price;
                cinfo.is_best = minfoList[i].is_best;
                cinfo.product_type = minfoList[i].product_type;
                cinfo.amount = 1;
                cinfo.status = 0;
                cinfo.barcode = minfoList[i].barcode;
                if (minfoList[i].is_soldout == 1)
                {
                    tmp.transform.Find("name").GetComponent<Text>().color = Color.black;
                    tmp.transform.Find("price").GetComponent<Text>().color = Color.black;
                }
                else
                {
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    if(category_name == "TAG")
                    {
                        tmp.GetComponent<Button>().onClick.AddListener(delegate () { addList(cinfo, 1); });
                    }
                    else
                    {
                        tmp.GetComponent<Button>().onClick.AddListener(delegate () { addList(cinfo); });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    public void addList(OrderCartInfo cinfo, int type = 0)
    {
        order_price += cinfo.price;
        if (type == 1)
        {
            //태그 메뉴
            GameObject tmp = Instantiate(tagProductItem);
            tmp.transform.SetParent(tagProductParent.transform);
            //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
            //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
            //float left = 0;
            //float right = 0;
            //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
            //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
            //tmp.transform.localScale = Vector3.one;

            tmp.transform.Find("tag_product").GetComponent<Text>().text = cinfo.name;
            tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(cinfo.price);
            tmp.transform.Find("menu_id").GetComponent<Text>().text = cinfo.menu_id.ToString();
            tmp.transform.Find("barcode").GetComponent<Text>().text = cinfo.barcode;
            string _menuid = cinfo.menu_id;
            int _price = cinfo.price;
            int is_barcode = 0;
            if (tmp.transform.Find("is_barcode").GetComponent<Text>().text == "1")
            {
                is_barcode = 1;
            }
            int sum_price = Global.GetConvertedPrice(sumTxt.text) + cinfo.price;
            sumTxt.text = Global.GetPriceFormat(sum_price);
            tmp.transform.Find("regTagBtn").GetComponent<Button>().onClick.RemoveAllListeners();
            tmp.transform.Find("regTagBtn").GetComponent<Button>().onClick.AddListener(delegate () { RegTag(_menuid, _price, is_barcode); });
            GameObject toggleObj = tmp.transform.Find("check").gameObject;
            tmp.GetComponent<Button>().onClick.RemoveAllListeners();
            tmp.GetComponent<Button>().onClick.AddListener(delegate () { onSelItem(toggleObj); });
            m_tagproductObj.Add(tmp);
            selAllTag.onValueChanged.RemoveAllListeners();
            selAllTag.onValueChanged.AddListener((value) => {
                onSelectAllTag(value);
            }
            );
        }
        else
        {
            //일반 메뉴
            product_cartlist = Global.addOneCartItem(cinfo, product_cartlist);
            bool is_found = false;
            for (int i = 0; i < productParent.transform.childCount; i++)
            {
                if (productParent.transform.GetChild(i).Find("menu_id").GetComponent<Text>().text == cinfo.menu_id.ToString())
                {
                    is_found = true;
                    try
                    {
                        productParent.transform.GetChild(i).Find("amount").GetComponent<Text>().text =
                            (int.Parse(productParent.transform.GetChild(i).Find("amount").GetComponent<Text>().text) + 1).ToString();
                        productParent.transform.GetChild(i).Find("price").GetComponent<Text>().text =
                            Global.GetPriceFormat(Global.GetConvertedPrice(productParent.transform.GetChild(i).Find("price").GetComponent<Text>().text) + cinfo.price);
                        break;
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            if (!is_found)
            {
                GameObject tmp = Instantiate(productItem);
                tmp.transform.SetParent(productParent.transform);
                //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                //float left = 0;
                //float right = 0;
                //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                //tmp.transform.localScale = Vector3.one;
                tmp.transform.Find("product").GetComponent<Text>().text = cinfo.name;
                tmp.transform.Find("amount").GetComponent<Text>().text = cinfo.amount.ToString();
                tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(cinfo.price);
                tmp.transform.Find("menu_id").GetComponent<Text>().text = cinfo.menu_id.ToString();
                tmp.transform.Find("product_type").GetComponent<Text>().text = cinfo.product_type.ToString();
                tmp.transform.Find("barcode").GetComponent<Text>().text = cinfo.barcode;
                GameObject toggleObj = tmp.transform.Find("check").gameObject;
                tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                tmp.GetComponent<Button>().onClick.AddListener(delegate () { onSelItem(toggleObj); });
                m_productObj.Add(tmp);
                selAllMenu.onValueChanged.RemoveAllListeners();
                selAllMenu.onValueChanged.AddListener((value) => {
                    onSelectAllMenu(value);
                }
                );
            }
            int sum_price = Global.GetConvertedPrice(sumTxt.text) + cinfo.price;
            sumTxt.text = Global.GetPriceFormat(sum_price);
        }
    }

    void onSelectAllMenu(bool value)
    {
        for (int i = 0; i < productParent.transform.childCount; i++)
        {
            productParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn = value;
        }
    }

    void onSelectAllTag(bool value)
    {
        for (int i = 0; i < tagProductParent.transform.childCount; i++)
        {
            tagProductParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn = value;
        }
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

    public void onShowSellStatus()
    {
        popup_type = 2;
        sellstatusPopup.SetActive(true);
        SendRequestForMarketInfo();
    }

    void RegTag(string menu_id, int price, int is_barcode)
    {
        if(is_barcode == 0)
        {
            err_msg.text = "먼저 바코드를 발행하세요.";
            popup_type = 0;
            err_popup.SetActive(true);
        }
        else
        {
            popup_type = 3;
            regtagPopup.SetActive(true);
            tagchargeTxt.text = Global.GetPriceFormat(price);
            Debug.Log(price);
            realTxt.text = price.ToString();
            rfidTxt.text = "";
            qrTxt.text = "";
            periodTxt.text = Global.userinfo.pub.prepay_tag_period.ToString();
            menuIdTxt.text = menu_id.ToString();
        }
    }

    public void onSaveTag()
    {
        string qr = qrTxt.text;
        int real_price = 0;
        try
        {
            real_price = int.Parse(realTxt.text);
        }catch(Exception ex)
        {
            Debug.Log(ex);
        }
        if(rfidTxt.text == "" && qr == "" || qr != "" && qr.IndexOf("!") != 0)
        {
            err_msg.text = "태그 정보를 정확히 입력하세요.";
            err_popup.SetActive(true);
            popup_type = 0;
        }else if(Global.GetConvertedPrice(tagchargeTxt.text) == 0 || (real_price > Global.GetConvertedPrice(tagchargeTxt.text)))
        {
            err_msg.text = "충전금액을 정확히 입력하세요.";
            err_popup.SetActive(true);
            popup_type = 0;
        }
        else
        {
            try
            {
                WWWForm form = new WWWForm();
                form.AddField("pub_id", Global.userinfo.pub.id);
                form.AddField("menu_id", menuIdTxt.text);
                form.AddField("rfid", rfidTxt.text);
                if(qr != "")
                {
                    form.AddField("qrcode", qr.Remove(0, 1));
                }
                form.AddField("period", periodTxt.text);
                form.AddField("charge", Global.GetConvertedPrice(tagchargeTxt.text));
                form.AddField("payamt", real_price);
                DateTime dt = Global.GetSdate();
                form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
                WWW www = new WWW(Global.api_url + Global.reg_tag_sell_api, form);
                StartCoroutine(RegTagProcess(www, menuIdTxt.text));
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
    }

    IEnumerator RegTagProcess(WWW www, string menu_idtxt)
    {
        yield return www;
        regtagPopup.SetActive(false);
        if(www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                for(int i = 0; i < m_tagproductObj.Count; i++)
                {
                    if(m_tagproductObj[i].transform.Find("menu_id").GetComponent<Text>().text == menu_idtxt)
                    {
                        int sumPrice = Global.GetConvertedPrice(sumTxt.text) - Global.GetConvertedPrice(m_tagproductObj[i].transform.Find("price").GetComponent<Text>().text);
                        sumTxt.text = Global.GetPriceFormat(sumPrice);
                        DestroyImmediate(m_tagproductObj[i].gameObject);
                        m_tagproductObj.RemoveAt(i);
                        //m_tagproductObj[i].transform.Find("regTagBtn").gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }
    }

    public void onBarcode()
    {
        int product_cnt = tagProductParent.transform.childCount + productParent.transform.childCount;
        if(product_cnt == 0)
        {
            popup_type = 0;
            err_msg.text = "판매할 상품을 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
        popup_type = 4;
        barcodePopup.SetActive(true);
        StartCoroutine(LoadBarcodeItems());
    }

    IEnumerator LoadBarcodeItems()
    {
        while (barcodeParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(barcodeParent.transform.GetChild(0).gameObject));
        }
        while (barcodeParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        m_barcodeObj.Clear();
        for (int i = 0; i < tagProductParent.transform.childCount; i++)
        {
            bool is_found = false;
            int index = -1;
            for(int j = 0; j < m_barcodeObj.Count; j++)
            {
                if(m_barcodeObj[j].transform.Find("menu_id").GetComponent<Text>().text == tagProductParent.transform.GetChild(i).transform.Find("menu_id").GetComponent<Text>().text)
                {
                    is_found = true;
                    index = j;
                    break;
                }
            }
            if (is_found)
            {
                int amount = int.Parse(m_barcodeObj[index].transform.Find("amount").GetComponent<Text>().text);
                m_barcodeObj[index].transform.Find("amount").GetComponent<Text>().text = (amount + 1).ToString();
                m_barcodeObj[index].transform.Find("amount1").GetComponent<Text>().text = (amount + 1).ToString();
                try
                {
                    string menu_id = tagProductParent.transform.GetChild(i).transform.Find("menu_id").GetComponent<Text>().text;
                    int price = Global.GetConvertedPrice(tagProductParent.transform.GetChild(i).transform.Find("price").GetComponent<Text>().text);
                    m_barcodeObj[index].transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(price * (amount + 1));
                    tagProductParent.transform.GetChild(i).transform.Find("regTagBtn").GetComponent<Button>().onClick.RemoveAllListeners();
                    tagProductParent.transform.GetChild(i).transform.Find("regTagBtn").GetComponent<Button>().onClick.AddListener(delegate () { RegTag(menu_id, price, 1); });
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                GameObject tmp = Instantiate(barcodeItem);
                tmp.transform.SetParent(barcodeParent.transform);
                tmp.transform.Find("name").GetComponent<Text>().text = tagProductParent.transform.GetChild(i).transform.Find("tag_product").GetComponent<Text>().text;
                qrCodeImage = tmp.transform.Find("barcode").GetComponent<RawImage>();
                Encode(tagProductParent.transform.GetChild(i).transform.Find("barcode").GetComponent<Text>().text);
                tagProductParent.transform.GetChild(i).transform.Find("is_barcode").GetComponent<Text>().text = "1";
                int is_tagReg = 0;
                try
                {
                    if (!tagProductParent.transform.GetChild(i).transform.Find("regTagBtn").gameObject.activeSelf)
                    {
                        is_tagReg = 1;
                    }
                }
                catch (Exception ex)
                {
                    is_tagReg = 1;
                }
                tmp.transform.Find("tag_reg").GetComponent<Text>().text = is_tagReg.ToString();
                tmp.transform.Find("menu_id").GetComponent<Text>().text = tagProductParent.transform.GetChild(i).transform.Find("menu_id").GetComponent<Text>().text;
                tmp.transform.Find("amount").GetComponent<Text>().text = "1";
                tmp.transform.Find("amount1").GetComponent<Text>().text = "1";
                tmp.transform.Find("price").GetComponent<Text>().text = tagProductParent.transform.GetChild(i).transform.Find("price").GetComponent<Text>().text;
                m_barcodeObj.Add(tmp);
                try
                {
                    string menu_id = tagProductParent.transform.GetChild(i).transform.Find("menu_id").GetComponent<Text>().text;
                    int price = Global.GetConvertedPrice(tagProductParent.transform.GetChild(i).transform.Find("price").GetComponent<Text>().text);
                    tagProductParent.transform.GetChild(i).transform.Find("regTagBtn").GetComponent<Button>().onClick.RemoveAllListeners();
                    tagProductParent.transform.GetChild(i).transform.Find("regTagBtn").GetComponent<Button>().onClick.AddListener(delegate () { RegTag(menu_id, price, 1); });
                }
                catch (Exception ex)
                {

                }
            }
        }

        for (int i = 0; i < productParent.transform.childCount; i++)
        {
            GameObject tmp = Instantiate(barcodeItem);
            tmp.transform.SetParent(barcodeParent.transform);
            tmp.transform.Find("name").GetComponent<Text>().text = productParent.transform.GetChild(i).transform.Find("product").GetComponent<Text>().text;
            qrCodeImage = tmp.transform.Find("barcode").GetComponent<RawImage>();
            Encode(productParent.transform.GetChild(i).transform.Find("barcode").GetComponent<Text>().text);
            tmp.transform.Find("tag_reg").GetComponent<Text>().text = "1";
            tmp.transform.Find("menu_id").GetComponent<Text>().text = productParent.transform.GetChild(i).transform.Find("menu_id").GetComponent<Text>().text;
            tmp.transform.Find("product_type").GetComponent<Text>().text = productParent.transform.GetChild(i).transform.Find("product_type").GetComponent<Text>().text;
            tmp.transform.Find("amount").GetComponent<Text>().text = productParent.transform.GetChild(i).transform.Find("amount").GetComponent<Text>().text;
            tmp.transform.Find("amount1").GetComponent<Text>().text = productParent.transform.GetChild(i).transform.Find("amount").GetComponent<Text>().text;
            tmp.transform.Find("price").GetComponent<Text>().text = productParent.transform.GetChild(i).transform.Find("price").GetComponent<Text>().text;
            m_barcodeObj.Add(tmp);
        }
    }

    public void RegSell()
    {
        //판매등록
        WWWForm form = new WWWForm();
        string oinfo = "[";
        int j = 0;
        for (int i = 0; i < barcodeParent.transform.childCount; i++)
        {
            if (barcodeParent.transform.GetChild(i).Find("tag_reg").GetComponent<Text>().text == "1")
            {
                try
                {
                    string mid = barcodeParent.transform.GetChild(i).Find("menu_id").GetComponent<Text>().text;
                    int cnt = int.Parse(barcodeParent.transform.GetChild(i).Find("amount").GetComponent<Text>().text);
                    int product_type = int.Parse(barcodeParent.transform.GetChild(i).Find("product_type").GetComponent<Text>().text);
                    int price = Global.GetConvertedPrice(barcodeParent.transform.GetChild(i).Find("price").GetComponent<Text>().text);
                    if (j == 0)
                    {
                        oinfo += "{";
                    }
                    else
                    {
                        oinfo += ",{";
                    }
                    j++;
                    oinfo += "\"menu_id\":\"" + mid + "\", \"amount\":\"" + cnt + "\", \"product_type\":\"" + product_type + "\", \"price\":\"" + price + "\"}";
                }
                catch (Exception ex)
                {

                }
            }
        }
        oinfo += "]";
        Debug.Log(oinfo);
        form.AddField("order_info", oinfo);
        form.AddField("pub_id", Global.userinfo.pub.id);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        form.AddField("pos_no", Global.setinfo.pos_no);
        WWW www = new WWW(Global.api_url + Global.reg_sell_api, form);
        StartCoroutine(RegSellProcess(www));
    }

    IEnumerator RegSellProcess(WWW www)
    {
        yield return www;
        ClosePopup();
        if (www.error == null)
        {
            for (int i = 0; i < m_productObj.Count; i++)
            {
                try
                {
                    string item = m_productObj[i].transform.Find("menu_id").GetComponent<Text>().text;
                    //if (m_productObj[i].transform.Find("status").GetComponent<Text>().text == "1")
                    //{
                    //    is_cooking = true;
                    //    string oid_str = m_productObj[i].transform.Find("order_ids").GetComponent<Text>().text;
                    //    string[] oid_tmp = oid_str.Split(',');
                    //    for (int j = 0; j < oid_tmp.Length; j++)
                    //    {
                    //        selected_item_id.Add(int.Parse(oid_tmp[j]));
                    //    }
                    //}
                    //else
                    //{
                    //    //이미 보여진 항목 삭제
                    for (int j = 0; j < product_cartlist.Count; j++)
                    {
                        if (product_cartlist[j].menu_id == item)
                        {
                            int sum_price = Global.GetConvertedPrice(sumTxt.text);
                            sumTxt.text = Global.GetPriceFormat(sum_price - product_cartlist[j].price * product_cartlist[j].amount);
                            product_cartlist.Remove(product_cartlist[j]);
                            DestroyImmediate(m_productObj[i]);
                            m_productObj.Remove(m_productObj[i]);
                            break;
                        }
                    }
                    i--;
                    //}
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }
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
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }
        }
    }

    public void onUsage()
    {
        StartCoroutine(GotoScene("usageSellProduct"));
    }

    List<int> selected_item_id = new List<int>();

    public void onCancelOrder()
    {
        //주문취소
        try
        {
            for (int i = 0; i < m_productObj.Count; i++)
            {
                if (m_productObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    string item = m_productObj[i].transform.Find("menu_id").GetComponent<Text>().text;
                    for (int j = 0; j < product_cartlist.Count; j++)
                    {
                        if (product_cartlist[j].menu_id == item)
                        {
                            int sum_price = Global.GetConvertedPrice(sumTxt.text);
                            sumTxt.text = Global.GetPriceFormat(sum_price - product_cartlist[j].price * product_cartlist[j].amount);
                            product_cartlist.Remove(product_cartlist[j]);
                            DestroyImmediate(m_productObj[i]);
                            m_productObj.Remove(m_productObj[i]);
                            i--;
                            j--;
                            break;
                        }
                    }
                }
            }
            for (int i = 0; i < m_tagproductObj.Count; i++)
            {
                if (m_tagproductObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    int sum_price = Global.GetConvertedPrice(sumTxt.text);
                    int tag_price = Global.GetConvertedPrice(m_tagproductObj[i].transform.Find("price").GetComponent<Text>().text);
                    sumTxt.text = Global.GetPriceFormat(sum_price - tag_price);
                    DestroyImmediate(m_tagproductObj[i]);
                    m_tagproductObj.Remove(m_tagproductObj[i]);
                    i--;
                }
            }
        } catch(Exception ex)
        {
            Debug.Log(ex);
        }
    }

    public void onConfirmPopup()
    {
        //취소
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
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
        form.AddField("order_info", oinfo);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        form.AddField("pos_no", Global.setinfo.pos_no);
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
                popup_type = 0;
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_msg.text = "주문취소시에 알지 못할 오류가 발생하였습니다.";
            popup_type = 0;
            err_popup.SetActive(true);
        }
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

    public void onBack()
    {
        StartCoroutine(GotoScene("main"));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    int popup_type = -1;
    public void ClosePopup()
    {
        switch (popup_type)
        {
            case 0:
                {
                    err_popup.SetActive(false);
                    break;
                };
            case 1:
                {
                    select_popup.SetActive(false);
                    break;
                };
            case 2:
                {
                    sellstatusPopup.SetActive(false);
                    break;
                };
            case 3:
                {
                    regtagPopup.SetActive(false);
                    break;
                };
            case 4:
                {
                    barcodePopup.SetActive(false);
                    break;
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
