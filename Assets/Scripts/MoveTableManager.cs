using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class MoveTableManager : MonoBehaviour
{
    public GameObject table_group_Item;
    public GameObject table_group_parent;
    public GameObject table_item;
    public GameObject table_parent;
    public Text tableName;

    public GameObject popup;
    public Text popup_str;
    public GameObject err_popup;
    public Text err_str;

    GameObject[] m_tableGroupItem;
    GameObject[] m_tableItem;
    int total_table_group_cnt = 0;
    string first_table_group = "";
    string old_tg_no = "";
    string old_t_no = "";

    // Start is called before the first frame update
    void Start()
    {
        tableName.text = Global.cur_tInfo.name;
        total_table_group_cnt = Global.tableGroupList.Count;
        LoadTableGroup();
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

    void LoadTableGroup()
    {
        //UI에 추가
        m_tableGroupItem = new GameObject[total_table_group_cnt];
        try
        {
            first_table_group = Global.tableGroupList[Global.cur_tInfo.tgNo].id;
        }
        catch (Exception ex)
        {

        }
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

        if (total_table_group_cnt > 0 && first_table_group != "")
            StartCoroutine(LoadTableList(first_table_group));
    }

    IEnumerator Destroy_Object(GameObject obj)
    {
        DestroyImmediate(obj);
        yield return null;
    }

    IEnumerator LoadTableList(string id)
    {
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
                if (table_group_parent.transform.GetChild(i).Find("id").GetComponent<Text>().text == old_tg_no.ToString())
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
                    m_tableItem[i].transform.Find("cnt").GetComponent<Text>().color = Global.selected_color;
                    m_tableItem[i].transform.Find("cnt").GetComponent<Text>().text = tbList[i].taglist.Count.ToString();
                }
                TableInfo tinfo = tbList[i];
                m_tableItem[i].GetComponent<Button>().onClick.RemoveAllListeners();
                m_tableItem[i].GetComponent<Button>().onClick.AddListener(delegate () { onMoveTable(tinfo); });
            }
            catch (Exception ex)
            {
                Debug.Log(ex);

            }
            yield return new WaitForFixedUpdate();
        }
    }

    void onMoveTable(TableInfo tinfo)
    {
        //선택된 테이블 노란색으로.
        try
        {
            for (int i = 0; i < table_parent.transform.childCount; i++)
            {
                if (table_parent.transform.GetChild(i).Find("id").GetComponent<Text>().text == old_t_no.ToString())
                {
                    table_parent.transform.GetChild(i).transform.Find("name").GetComponent<Text>().color = Color.white;
                    table_parent.transform.GetChild(i).transform.Find("cnt").GetComponent<Text>().color = Color.white;
                    table_parent.transform.GetChild(i).transform.Find("price").GetComponent<Text>().color = Color.white;
                }
                if (table_parent.transform.GetChild(i).Find("id").GetComponent<Text>().text == tinfo.id.ToString())
                {
                    table_parent.transform.GetChild(i).transform.Find("name").GetComponent<Text>().color = Global.selected_color;
                    table_parent.transform.GetChild(i).transform.Find("cnt").GetComponent<Text>().color = Global.selected_color;
                    table_parent.transform.GetChild(i).transform.Find("price").GetComponent<Text>().color = Global.selected_color;
                }
            }
        }
        catch (Exception ex)
        {

        }
        Global.selected_tableid = tinfo.id;
        Global.selected_tablename = tinfo.name;
        old_t_no = tinfo.id;
        if(Global.moveTableType == 0)
        {
            if (Global.selected_tableid == Global.cur_tInfo.tid)
            {
                return;
            }
            if (tinfo.order_price > 0 || (tinfo.taglist != null && tinfo.taglist.Count > 0))
            {
                popup_str.text = Global.cur_tInfo.name + "와 " + tinfo.name + "을 합석하시겠습니까?\n합석 후에는 취소가 불가합니다.";
                popup.SetActive(true);
            }
            else
            {
                //즉시 합석 가능
                MixTable(Global.cur_tInfo.tid, tinfo.id);
            }
        }
        else
        {
            //선불태그 테이블에 등록
            WWWForm form = new WWWForm();
            form.AddField("pub_id", Global.userinfo.pub.id);
            form.AddField("tag_id", Global.cur_tagInfo.tag_id);
            form.AddField("destination_tableid", tinfo.id);
            form.AddField("type", 1);
            WWW www = new WWW(Global.api_url + Global.move_table_api, form);
            StartCoroutine(onMixTable(www));
        }
    }

    void MixTable(string origin_tableid, string destination_tableid)
    {
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.userinfo.pub.id);
        form.AddField("pos_no", Global.setinfo.pos_no);
        form.AddField("origin_tableid", origin_tableid);
        form.AddField("destination_tableid", destination_tableid);
        form.AddField("type", 0);
        WWW www = new WWW(Global.api_url + Global.move_table_api, form);
        StartCoroutine(onMixTable(www));
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

    IEnumerator onMixTable(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                if (Global.userinfo.pub.move_table_type == 0)
                {
                    try
                    {
                        Debug.Log(jsonNode);
                        //테이블 이동내역 출력
                        string printStr = DocumentFactory.GetTableMove(jsonNode["originTableName"], jsonNode["destinationTableName"]);
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
                    } catch (Exception ex)
                    {

                    }

                    yield return new WaitForSeconds(0.5f);
                    SceneManager.LoadScene("main");
                }
                else
                {
                    //StopCoroutine(checkSdate());
                    SceneManager.LoadScene("main");
                }
            }
            else
            {
                err_str.text = jsonNode["msg"];
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "합석 처리중 알지 못할 오류가 발생하였습니다.";
            err_popup.SetActive(true);
        }
    }

    public void onYes()
    {
        MixTable(Global.cur_tInfo.tid, Global.selected_tableid);
    }

    public void onNo()
    {
        popup.SetActive(false);
    }

    public void onBack()
    {
        SceneManager.LoadScene("tableUsage");
    }

    public void onErrorPop()
    {
        err_popup.SetActive(false);
    }
}
