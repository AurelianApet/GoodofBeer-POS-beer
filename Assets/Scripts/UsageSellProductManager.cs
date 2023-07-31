using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SimpleJSON;
using System;
using System.Threading.Tasks;
using SocketIO;

public class UsageSellProductManager : MonoBehaviour
{
    public Text tagName;
    public Text chargeSumTxt;
    public Text usageSumTxt;
    public Text remainTxt;
    public InputField rfidTxt;
    public InputField qrTxt;
    public InputField periodTxt;
    public Toggle selAllCharge;

    public Text tagCntTxt;//tag 개수
    public Text chargeTxt;//충전금액
    public Text productTxt;//상품개수
    public Text productPriceTxt;//상품금액
    public Text priceTxt;//총 판매금액

    public GameObject chargeItemParent;
    public GameObject chargeItem;
    public GameObject menuUsageParent;
    public GameObject menuUsageItem;

    public GameObject changeTagPopup;
    public GameObject readTagPopup;
    public GameObject err_popup;
    public Text err_str;
    public GameObject select_popup;
    public Text select_str;
    public GameObject sellstatusPopup;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket1;

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
    Socket socket;
    IPEndPoint remoteEP;

    string app_no = "";
    string credit_card_company = "";
    string credit_card_number = "";

    PrepayTagUsage prepaytagUsage = new PrepayTagUsage();
    List<GameObject> m_menuusageItemObj = new List<GameObject>();
    List<string> selected_item_id = new List<string>();
    bool is_socket_open = false;

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(checkSdate());
        tagName.text = Global.userinfo.pub.name;
        if (Global.cur_tagInfo.tag_id != "" && Global.cur_tagInfo.is_pay_after == 0)
        {
            if (Global.cur_tagInfo.rfid != "" && Global.cur_tagInfo.rfid != null)
            {
                checkTag(Global.cur_tagInfo.rfid);
            }
            else if (Global.cur_tagInfo.qrcode != "" && Global.cur_tagInfo.qrcode != null)
            {
                checkTag(Global.cur_tagInfo.qrcode);
            }
        }
        socketObj = Instantiate(socketPrefab);
        //socketObj = GameObject.Find("SocketIO");
        socket1 = socketObj.GetComponent<SocketIOComponent>();
        socket1.On("open", socketOpen);
        socket1.On("createOrder", createOrder);
        socket1.On("new_notification", new_notification);
        socket1.On("error", socketError);
        socket1.On("close", socketClose);
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

    public void onShowSellStatus()
    {
        sellstatusPopup.SetActive(true);
        SendRequestForMarketInfo();
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
            if (jsonNode["suc"].AsInt == 1)
            {
                tagCntTxt.text = jsonNode["tagCnt"];
                chargeTxt.text = Global.GetPriceFormat(jsonNode["charge"].AsInt);
                productTxt.text = jsonNode["productCnt"];
                productPriceTxt.text = Global.GetPriceFormat(jsonNode["productPrice"].AsInt);
                priceTxt.text = Global.GetPriceFormat(jsonNode["price"].AsInt);
            }
        }
    }

    public void onChangeTag()
    {
        if (Global.cur_tagInfo.tag_id == "" || Global.cur_tagInfo.tag_id == null)
        {
            err_popup.SetActive(true);
            err_str.text = "먼저 태그를 선택하세요.";
        }
        else
        {
            changeTagPopup.SetActive(true);
            rfidTxt.text = Global.cur_tagInfo.rfid;
            qrTxt.text = Global.cur_tagInfo.qrcode;
            periodTxt.text = Global.cur_tagInfo.period.ToString();
        }
    }

    public void onCloseTagSearchPopup()
    {
        readTagPopup.SetActive(false);
    }

    public void SaveTagInfo()
    {
        string qr = qrTxt.text;
        if (rfidTxt.text == "" && qr == "" || qr != "" && qr.IndexOf("!") != 0)
        {
            err_str.text = "태그정보를 정확히 입력하세요.";
            err_popup.SetActive(true);
        }
        else
        {
            WWWForm form = new WWWForm();
            form.AddField("tag_id", Global.cur_tagInfo.tag_id);
            form.AddField("pub_id", Global.userinfo.pub.id);
            if (rfidTxt.text != "")
            {
                form.AddField("rfid", rfidTxt.text);
            }
            try
            {
                if (qr != "")
                {
                    form.AddField("qrcode", qr.Remove(0, 1));
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
            if (periodTxt.text != "")
            {
                form.AddField("period", periodTxt.text);
            }
            form.AddField("is_pay_after", 0);
            DateTime dt = Global.GetSdate();
            form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
            WWW www = new WWW(Global.api_url + Global.reg_tag_api, form);
            StartCoroutine(ChangeTagProcess(www));
            closePopup();
        }
    }

    IEnumerator ChangeTagProcess(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                try
                {
                    Global.cur_tagInfo.rfid = jsonNode["rfid"];
                    Global.cur_tagInfo.qrcode = jsonNode["qrcode"];
                    Global.cur_tagInfo.tag_name = jsonNode["tag_name"];
                    Global.cur_tagInfo.period = jsonNode["period"];

                    int remain_period = jsonNode["remain_period"].AsInt;
                    tagName.text = Global.cur_tagInfo.tag_name + " / 잔여 " + remain_period + "일";
                }
                catch (Exception)
                {

                }
            }
        }
    }

    public void onCloseErrPopup()
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

    public void closePopup()
    {
        changeTagPopup.SetActive(false);
        readTagPopup.SetActive(false);
        sellstatusPopup.SetActive(false);
    }

    public void closeErrPopup()
    {
        err_popup.SetActive(false);
    }

    public void onBack()
    {
        Global.cur_tagInfo = new CurTagInfo();
        StartCoroutine(GotoScene("main"));
    }

    public void onRegSellProduct()
    {
        Global.cur_tagInfo = new CurTagInfo();
        StartCoroutine(GotoScene("regSellProduct"));
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
        //태그 선택
        WWWForm form = new WWWForm();
        form.AddField("tag_data", str);
        form.AddField("pub_id", Global.userinfo.pub.id);
        DateTime dt = Global.GetSdate();
        form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
        WWW www = new WWW(Global.api_url + Global.find_tag_usage_api, form);
        StartCoroutine(SelTagProcess(www));
        readTagPopup.transform.Find("tag").GetComponent<InputField>().text = "";
        readTagPopup.SetActive(false);
        send_time = 0f;
    }

    public void onSelTagPopup()
    {
        readTagPopup.SetActive(true);
        readTagPopup.transform.Find("tag").GetComponent<InputField>().Select();
        readTagPopup.transform.Find("tag").GetComponent<InputField>().ActivateInputField();
        readTagPopup.transform.Find("tag").GetComponent<InputField>().onValueChanged.AddListener((value) =>
        {
            checkTag(value);
        }
        );
    }

    IEnumerator SelTagProcess(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["is_pay_after"].AsInt == 1)
            {
                err_str.text = "등록된 TAG가 아닙니다.";
                err_popup.SetActive(true);
            }
            else
            {
                if (jsonNode["suc"].AsInt == 0)
                {
                    err_str.text = jsonNode["msg"];
                    err_popup.SetActive(true);
                    yield break;
                }
                while (chargeItemParent.transform.childCount > 0)
                {
                    StartCoroutine(Destroy_Object(chargeItemParent.transform.GetChild(0).gameObject));
                }
                while (chargeItemParent.transform.childCount > 0)
                {
                    yield return new WaitForFixedUpdate();
                }

                while (menuUsageParent.transform.childCount > 0)
                {
                    StartCoroutine(Destroy_Object(menuUsageParent.transform.GetChild(0).gameObject));
                }
                while (menuUsageParent.transform.childCount > 0)
                {
                    yield return new WaitForFixedUpdate();
                }
                Global.cur_tagInfo.charge = jsonNode["charge"].AsInt;
                Global.cur_tagInfo.period = jsonNode["period"].AsInt;
                Global.cur_tagInfo.is_pay_after = 0;
                Global.cur_tagInfo.reg_datetime = jsonNode["reg_datetime"];
                Global.cur_tagInfo.remain = jsonNode["remain"].AsInt;
                Global.cur_tagInfo.tag_id = jsonNode["tag_id"];
                Global.cur_tagInfo.qrcode = jsonNode["qrcode"];
                Global.cur_tagInfo.rfid = jsonNode["rfid"];
                Global.cur_tagInfo.tag_name = jsonNode["tag_name"];
                int remain_period = jsonNode["remain_period"].AsInt;
                tagName.text = Global.cur_tagInfo.tag_name + " / 잔여 " + remain_period + "일";
                JSONNode charge_list = JSON.Parse(jsonNode["chargelist"].ToString()/*.Replace("\"", "")*/);
                prepaytagUsage.chargeItemlist = new List<ChargeItemInfo>();
                prepaytagUsage.charge_sum_price = 0;
                for (int i = 0; i < charge_list.Count; i++)
                {
                    ChargeItemInfo chargeInfo = new ChargeItemInfo();
                    chargeInfo.card_no = charge_list[i]["card_no"];
                    chargeInfo.card_type = charge_list[i]["card_type"];
                    chargeInfo.price = charge_list[i]["price"].AsInt;
                    chargeInfo.charge_time = charge_list[i]["charge_time"];
                    chargeInfo.id = charge_list[i]["id"];
                    prepaytagUsage.chargeItemlist.Add(chargeInfo);

                    GameObject tmp = Instantiate(chargeItem);
                    tmp.transform.SetParent(chargeItemParent.transform);
                    //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                    //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                    //float left = 0;
                    //float right = 0;
                    //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                    //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                    //tmp.transform.localScale = Vector3.one;
                    tmp.transform.Find("time").GetComponent<Text>().text = chargeInfo.charge_time;
                    tmp.transform.Find("card").GetComponent<Text>().text = chargeInfo.card_type;
                    tmp.transform.Find("cardno").GetComponent<Text>().text = chargeInfo.card_no;
                    tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(chargeInfo.price);
                }
                prepaytagUsage.charge_sum_price = jsonNode["charge"].AsInt;
                chargeSumTxt.text = Global.GetPriceFormat(prepaytagUsage.charge_sum_price);
                m_menuusageItemObj.Clear();
                JSONNode order_list = JSON.Parse(jsonNode["orderlist"].ToString()/*.Replace("\"", "")*/);
                prepaytagUsage.menuOrderlist = new List<PrepayTagMenuOrderItemInfo>();
                prepaytagUsage.order_sum_price = 0;
                for (int i = 0; i < order_list.Count; i++)
                {
                    PrepayTagMenuOrderItemInfo orderInfo = new PrepayTagMenuOrderItemInfo();
                    orderInfo.order_id = order_list[i]["order_id"];
                    orderInfo.menu_id = order_list[i]["menu_id"];
                    orderInfo.amount = order_list[i]["amount"].AsInt;
                    orderInfo.price = order_list[i]["price"].AsInt;
                    orderInfo.menu_name = order_list[i]["menu_name"];
                    orderInfo.status = order_list[i]["status"].AsInt;
                    orderInfo.order_time = order_list[i]["order_time"];
                    orderInfo.is_service = order_list[i]["is_service"];
                    prepaytagUsage.menuOrderlist.Add(orderInfo);

                    GameObject tmp = Instantiate(menuUsageItem);
                    tmp.transform.SetParent(menuUsageParent.transform);
                    //tmp.transform.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                    //tmp.transform.GetComponent<RectTransform>().anchorMax = Vector3.one;
                    //float left = 0;
                    //float right = 0;
                    //tmp.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2((left - right) / 2, 0f);
                    //tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-(left + right), 0);
                    //tmp.transform.localScale = Vector3.one;
                    tmp.transform.Find("time").GetComponent<Text>().text = orderInfo.order_time;
                    tmp.transform.Find("menu").GetComponent<Text>().text = orderInfo.menu_name;
                    tmp.transform.Find("cnt").GetComponent<Text>().text = orderInfo.amount.ToString();
                    tmp.transform.Find("menu_id").GetComponent<Text>().text = orderInfo.menu_id.ToString();
                    tmp.transform.Find("order_id").GetComponent<Text>().text = orderInfo.order_id.ToString();
                    tmp.transform.Find("status").GetComponent<Text>().text = orderInfo.status.ToString();
                    if (orderInfo.is_service == 1)
                    {
                        tmp.transform.Find("price").GetComponent<Text>().text = "0";
                    }
                    else
                    {
                        tmp.transform.Find("price").GetComponent<Text>().text = Global.GetPriceFormat(orderInfo.price);
                        prepaytagUsage.order_sum_price += orderInfo.price;
                    }
                    GameObject toggleObj = tmp.transform.Find("check").gameObject;
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { onSelItem(toggleObj); });
                    m_menuusageItemObj.Add(tmp);
                }
                usageSumTxt.text = Global.GetPriceFormat(prepaytagUsage.order_sum_price);
                prepaytagUsage.remain_price = jsonNode["remain"].AsInt;
                remainTxt.text = Global.GetPriceFormat(prepaytagUsage.remain_price);
                selAllCharge.onValueChanged.RemoveAllListeners();
                selAllCharge.onValueChanged.AddListener((value) =>
                {
                    onSelectAllCharge(value);
                }
                );
                string payInfo = getJsonResult();
                Debug.Log(payInfo);
                socket1.Emit("prepayInfo", JSONObject.Create(payInfo));
            }
        }
    }

    string getJsonResult()
    {
        string str = "{\"tagName\":\"" + Global.cur_tagInfo.tag_name + "\"";
        str += ",\"chargePrice\":\"" + prepaytagUsage.charge_sum_price + "\"";
        str += ",\"totalPrice\":\"" + prepaytagUsage.order_sum_price + "\"";
        str += ",\"payPrice\":\"" + 0 + "\"";
        str += ",\"chargelist\":[";
        try
        {
            for (int j = 0; j < prepaytagUsage.chargeItemlist.Count; j++)
            {
                if (j > 0)
                {
                    str += ",";
                }
                str += "{\"regtime\":\"" + prepaytagUsage.chargeItemlist[j].charge_time + "\"";
                str += ",\"card\":\"" + prepaytagUsage.chargeItemlist[j].card_type + "\"";
                str += ",\"cardno\":\"" + prepaytagUsage.chargeItemlist[j].card_no + "\"";
                str += ",\"price\":\"" + prepaytagUsage.chargeItemlist[j].price + "\"}";
            }
            str += "],\"usagelist\":[";
            for (int i = 0; i < prepaytagUsage.menuOrderlist.Count; i++)
            {
                if (i > 0)
                {
                    str += ",";
                }
                str += "{\"menu_name\":\"" + prepaytagUsage.menuOrderlist[i].menu_name + "\"";
                str += ",\"regtime\":\"" + prepaytagUsage.menuOrderlist[i].order_time + "\"";
                str += ",\"size\":\"" + prepaytagUsage.menuOrderlist[i].amount + "\"";
                str += ",\"price\":\"" + prepaytagUsage.menuOrderlist[i].price + "\"}";
            }
        }
        catch (Exception ex)
        {
            return "";
        }
        str += "]}";
        return str;
    }

    void onSelectAllCharge(bool value)
    {
        for (int i = 0; i < menuUsageParent.transform.childCount; i++)
        {
            menuUsageParent.transform.GetChild(i).Find("check").GetComponent<Toggle>().isOn = value;
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

    public void outputUsage()
    {
        //이용내역 출력
        if (Global.cur_tagInfo.tag_id == "" || Global.cur_tagInfo.tag_id == null)
        {
            err_str.text = "먼저 TAG를 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
        if (Global.setinfo.paymentDeviceInfo.port == 0 || Global.setinfo.paymentDeviceInfo.ip == "" || Global.setinfo.paymentDeviceInfo.ip == null)
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
        form.AddField("tag_id", Global.cur_tagInfo.tag_id);
        WWW www = new WWW(Global.api_url + Global.get_output_orderlist_api, form);
        StartCoroutine(getOutputOrderlist(www));
    }

    IEnumerator getOutputOrderlist(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                Debug.Log(jsonNode);
                int total_price = jsonNode["total_price"].AsInt;
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
                string printStr = "";
                printStr = DocumentFactory.GetOrderListDetail(orders, total_price, title: "[이용내역]");
                Debug.Log(printStr);
                byte[] sendData = NetUtils.StrToBytes(printStr);
                Socket_Send(Global.setinfo.paymentDeviceInfo.ip, Global.setinfo.paymentDeviceInfo.port.ToString(), sendData);
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

    public void onCancelOrder()
    {
        //주문취소
        selected_item_id.Clear();
        bool is_cooking = false;
        for (int i = 0; i < m_menuusageItemObj.Count; i++)
        {
            try
            {
                if (m_menuusageItemObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                {
                    if (m_menuusageItemObj[i].transform.Find("status").GetComponent<Text>().text == "2" || m_menuusageItemObj[i].transform.Find("status").GetComponent<Text>().text == "3")
                    {
                        is_cooking = true;
                    }
                    selected_item_id.Add(m_menuusageItemObj[i].transform.Find("order_id").GetComponent<Text>().text);
                }
            }
            catch (Exception ex)
            {

            }
        }
        if (selected_item_id.Count == 0)
        {
            err_str.text = "취소할 상품을 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
        if (is_cooking)
        {
            select_str.text = "현재 조리 중인 메뉴입니다. 주문을 취소하시겠습니까?";
            select_popup.SetActive(true);
        }
        else
        {
            select_str.text = "선택한 상품을 취소 하시겠습니까?";
            select_popup.SetActive(true);
        }
    }

    public void onConfirmPopup()
    {
        //취소
        if (Global.cur_tagInfo.tag_id == "" || Global.cur_tagInfo.tag_id == null)
        {
            err_str.text = "먼저 TAG를 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
        WWWForm form = new WWWForm();
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
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("tag_id", Global.cur_tagInfo.tag_id);//유통전용에서 주문 취소시 해당 금액을 태그에 복귀시키기 위해 이용.
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
                for (int i = 0; i < m_menuusageItemObj.Count; i++)
                {
                    if (m_menuusageItemObj[i].transform.Find("check").GetComponent<Toggle>().isOn)
                    {
                        StartCoroutine(Destroy_Object(m_menuusageItemObj[i]));
                        m_menuusageItemObj.Remove(m_menuusageItemObj[i]);
                    }
                }
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
                StartCoroutine(GotoScene("usageSellProduct"));
            }
            else
            {
                err_str.text = jsonNode["msg"];
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "주문취소시에 알지 못할 오류가 발생하였습니다.";
            err_popup.SetActive(true);
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

    IEnumerator GotoScene(string sceneName)
    {
        if (Global.setinfo.pos_no == 1)
        {
            socket1.Emit("endpay");
        }
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
