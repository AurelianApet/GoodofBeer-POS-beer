using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class loginManager : MonoBehaviour
{
    public InputField userId;
    public InputField password;
    public Toggle idSave;
    public Toggle autoSave;
    public GameObject err_popup;
    public Text err_str;
    public GameObject select_popup;

    // Start is called before the first frame update
    void Start()
    {
        if (Global.server_address == null)
        {
            Global.last_scene = "splash";
            SceneManager.LoadScene("loginSetting");
        }
        if (Global.is_id_saved)
        {
            userId.text = Global.userinfo.userID;
            idSave.isOn = true;
        }
        if (Global.userinfo.is_auto_login)
        {
            autoSave.isOn = true;
        }
    }

    public void onSet()
    {
        Global.last_scene = "splash";
        SceneManager.LoadScene("loginSetting");
    }

    public void Login()
    {
        if (userId.text.Trim() == "")
        {
            err_str.text = "Username을 입력하세요.";
            err_popup.SetActive(true);
        }
        else if (password.text.Trim() == "")
        {
            err_str.text = "비밀번호를 입력하세요.";
            err_popup.SetActive(true);
        }
        else
        {
            try
            {
                WWWForm form = new WWWForm();
                DateTime dt = Global.GetSdate(false);
                DateTime yesturday = dt.AddDays(-1);
                form.AddField("sdate", string.Format("{0:D4}-{1:D2}-{2:D2}", dt.Year, dt.Month, dt.Day));
                form.AddField("yesturday", string.Format("{0:D4}-{1:D2}-{2:D2}", yesturday.Year, yesturday.Month, yesturday.Day));
                form.AddField("userID", userId.text.Trim());
                form.AddField("password", password.text.Trim());
                form.AddField("type", Global.app_type);
                Debug.Log(Global.api_url + Global.login_api);
                WWW www = new WWW(Global.api_url + Global.login_api, form);
                StartCoroutine(ProcessLogin(www, idSave.isOn, userId.text.Trim(), autoSave.isOn, password.text.Trim()));
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
    }

    IEnumerator ProcessLogin(WWW www, bool is_idsave, string username, bool is_autosave, string password)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                Global.userinfo.id = jsonNode["uid"].AsInt;
                if (is_idsave)
                {
                    PlayerPrefs.SetInt("idSave", 1);
                    PlayerPrefs.SetString("id", username);
                    Global.is_id_saved = true;
                }
                else
                {
                    PlayerPrefs.SetInt("idSave", 0);
                    Global.is_id_saved = false;
                }
                if (is_autosave)
                {
                    Debug.Log("autosave");
                    PlayerPrefs.SetInt("autoSave", 1);
                    PlayerPrefs.SetString("id", username);
                    PlayerPrefs.SetString("pwd", password);
                    Global.userinfo.is_auto_login = true;
                }
                else
                {
                    PlayerPrefs.SetInt("autoSave", 0);
                    Global.userinfo.is_auto_login = false;
                }
                Global.userinfo.userID = username;
                Global.userinfo.password = password;
                Global.userinfo.pub = new PubInfo();
                PubInfo pinfo = new PubInfo();
                pinfo.id = jsonNode["pub_id"];
                pinfo.name = jsonNode["pub_name"];
                pinfo.phone = jsonNode["phone"];
                pinfo.representer = jsonNode["representer"];
                pinfo.address = jsonNode["address"];
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

                SceneManager.LoadScene("main");
            }
            else
            {
                err_str.text = jsonNode["msg"];
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "인터넷 연결을 확인하세요.";
            err_popup.SetActive(true);
        }
    }

    public void onConfirmErrPopup()
    {
        err_popup.SetActive(false);
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
}
