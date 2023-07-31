using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJSON;

public class splash : MonoBehaviour
{
    // Start is called before the first frame update
    public float delay_time = 0.5f;

    IEnumerator Start()
    {
#if UNITY_IPHONE
		Global.imgPath = Application.persistentDataPath + "/pos_img/";
#elif UNITY_ANDROID
        Global.imgPath = Application.persistentDataPath + "/pos_img/";
#else
if( Application.isEditor == true ){ 
    	Global.imgPath = "/pos_img/";
} 
#endif

#if UNITY_IPHONE
		Global.prePath = @"file://";
#elif UNITY_ANDROID
        Global.prePath = @"file:///";
#else
		Global.prePath = @"file://" + Application.dataPath.Replace("/Assets","/");
#endif

        //delete all downloaded images
        try
        {
            if (Directory.Exists(Global.imgPath))
            {
                Directory.Delete(Global.imgPath, true);
            }
        }
        catch (Exception)
        {

        }
        if (PlayerPrefs.GetInt("idSave") == 1)
        {
            Global.is_id_saved = true;
            Global.userinfo.userID = PlayerPrefs.GetString("id").Trim();
        }
        else
        {
            Global.is_id_saved = false;
        }
        Global.setinfo.pos_no = PlayerPrefs.GetInt("posNo");
        Global.setinfo.tableMain = PlayerPrefs.GetInt("tableMain");

        if(Global.setinfo.pos_no == 0)
        {
            Global.setinfo.pos_no = (new System.Random().Next() % 99) + 2;
            Debug.Log(Global.setinfo.pos_no);
            PlayerPrefs.SetInt("posNo", Global.setinfo.pos_no);
        }

        Global.setinfo.paymentDeviceInfo.ip = PlayerPrefs.GetString("payDeviceIp");
        Global.setinfo.paymentDeviceInfo.port = PlayerPrefs.GetInt("payDevicePort");
        Global.setinfo.paymentDeviceInfo.baudrate = PlayerPrefs.GetFloat("payDeviceBaudrate");
        Global.setinfo.paymentDeviceInfo.cat = PlayerPrefs.GetInt("payDeviceCat");
        Global.setinfo.paymentDeviceInfo.line_count = PlayerPrefs.GetInt("payDeviceLinecount");
        Global.setinfo.paymentDeviceInfo.type = PlayerPrefs.GetInt("payDeviceType");

        Global.setinfo.printerSet.printer1.name = PlayerPrefs.GetString("printer1name");
        if(Global.setinfo.printerSet.printer1.name == "")
        {
            Global.setinfo.printerSet.printer1.name = "주방1";
        }
        Global.setinfo.printerSet.printer2.name = PlayerPrefs.GetString("printer2name");
        if (Global.setinfo.printerSet.printer2.name == "")
        {
            Global.setinfo.printerSet.printer2.name = "주방2";
        }
        Global.setinfo.printerSet.printer3.name = PlayerPrefs.GetString("printer3name");
        if (Global.setinfo.printerSet.printer3.name == "")
        {
            Global.setinfo.printerSet.printer3.name = "주방2";
        }
        Global.setinfo.printerSet.printer4.name = PlayerPrefs.GetString("printer4name");
        if (Global.setinfo.printerSet.printer4.name == "")
        {
            Global.setinfo.printerSet.printer4.name = "주방2";
        }
        Global.setinfo.printerSet.printer1.useset = PlayerPrefs.GetInt("printer1useset");
        Global.setinfo.printerSet.printer2.useset = PlayerPrefs.GetInt("printer2useset");
        Global.setinfo.printerSet.printer3.useset = PlayerPrefs.GetInt("printer3useset");
        Global.setinfo.printerSet.printer4.useset = PlayerPrefs.GetInt("printer4useset");
        Global.setinfo.printerSet.printer1.port = PlayerPrefs.GetString("printer1port");
        Global.setinfo.printerSet.printer2.port = PlayerPrefs.GetString("printer2port");
        Global.setinfo.printerSet.printer3.port = PlayerPrefs.GetString("printer3port");
        Global.setinfo.printerSet.printer4.port = PlayerPrefs.GetString("printer4port");
        Global.setinfo.printerSet.printer1.ip_baudrate = PlayerPrefs.GetString("printer1ip");
        Global.setinfo.printerSet.printer2.ip_baudrate = PlayerPrefs.GetString("printer2ip");
        Global.setinfo.printerSet.printer3.ip_baudrate = PlayerPrefs.GetString("printer3ip");
        Global.setinfo.printerSet.printer4.ip_baudrate = PlayerPrefs.GetString("printer4ip");
        Global.setinfo.printerSet.menu_output = PlayerPrefs.GetInt("printerMenuOutput");

        Global.setinfo.payment_time = PlayerPrefs.GetInt("payment_time");
        if (Global.setinfo.payment_time == 0)
        {
            Global.setinfo.payment_time = 30;
        }

        Global.server_address = PlayerPrefs.GetString("ip").Trim();
        Global.api_url = "http://" + Global.server_address + ":" + Global.api_server_port + "/";
        Global.socket_server = "ws://" + Global.server_address + ":" + Global.api_server_port;
        if (Global.server_address == "" || Global.server_address == null)
        {
            Global.last_scene = "splash";
            SceneManager.LoadScene("loginSetting");
        }
        if (PlayerPrefs.GetInt("autoSave") == 1)
        {
            Debug.Log("auto save");
            Global.userinfo.is_auto_login = true;
            Global.userinfo.userID = PlayerPrefs.GetString("id").Trim();
            Global.userinfo.password = PlayerPrefs.GetString("pwd").Trim();
            WWWForm form = new WWWForm();
            DateTime dt = Global.GetSdate(false);
            DateTime yesturday = dt.AddDays(-1);
            form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
            form.AddField("yesturday", string.Format("{0:D4}-{1:D2}-{2:D2}", yesturday.Year, yesturday.Month, yesturday.Day));
            form.AddField("userID", Global.userinfo.userID);
            form.AddField("password", Global.userinfo.password);
            form.AddField("type", Global.app_type);
            WWW www = new WWW(Global.api_url + Global.login_api, form);
            StartCoroutine(ProcessLogin(www));
        }
        else
        {
            Global.userinfo.is_auto_login = false;
            yield return new WaitForSeconds(delay_time);
            SceneManager.LoadScene("login");
        }
    }

    IEnumerator ProcessLogin(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                Global.userinfo.id = jsonNode["uid"].AsInt;
                Global.userinfo.pub = new PubInfo();
                PubInfo pinfo = new PubInfo();
                pinfo.id = jsonNode["pub_id"];
                pinfo.name = jsonNode["pub_name"];
                pinfo.address = jsonNode["address"];
                pinfo.phone = jsonNode["phone"];
                pinfo.representer = jsonNode["representer"];
                pinfo.paid_price = jsonNode["paid_price"].AsInt;
                pinfo.paid_cnt = jsonNode["paid_cnt"].AsInt;
                pinfo.price = jsonNode["price"].AsInt;
                pinfo.total_cnt = jsonNode["total_cnt"].AsInt;
                pinfo.pending_price = jsonNode["pending_price"].AsInt;
                pinfo.pending_cnt = jsonNode["pending_cnt"].AsInt;
                pinfo.closetime = jsonNode["closetime"];
                pinfo.ceiltype = jsonNode["ceiltype"].AsInt;
                pinfo.invoice_outtype = PlayerPrefs.GetInt("invoice_outtype");
                pinfo.move_table_type = PlayerPrefs.GetInt("move_table_type");
                pinfo.pointer_rate = jsonNode["pointer_rate"].AsFloat;
                pinfo.pointer_type = jsonNode["pointer_type"].AsInt;
                pinfo.prepay_tag_period = jsonNode["prepay_tag_period"].AsInt;
                pinfo.is_open = jsonNode["is_open"].AsInt;
                pinfo.is_self = jsonNode["is_self"].AsInt;
                pinfo.sell_type = jsonNode["sell_type"].AsInt;
                pinfo.tap_count = jsonNode["tap_cnt"].AsInt;
                Global.alarm_cnt = jsonNode["alarm_cnt"].AsInt;
                Global.userinfo.pub = pinfo;
                yield return new WaitForSeconds(delay_time);
                SceneManager.LoadScene("main");
            }
            else
            {
                yield return new WaitForSeconds(delay_time);
                SceneManager.LoadScene("login");
            }
        }
        else
        {
            yield return new WaitForSeconds(delay_time);
            SceneManager.LoadScene("login");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
