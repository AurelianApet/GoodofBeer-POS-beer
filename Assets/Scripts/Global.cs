using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

public struct PaymentDeviceInfo
{
    public string ip;
    public int port;
    public float baudrate;
    public int cat;//kicc, kis
    public int line_count;
    public int type;//1-ip, 0-serial
}

public struct PrinterInfo
{
    public int no;
    public string name;
    public int useset;
    public string port;
    public string ip_baudrate;
}

public struct PrinterSet
{
    public PrinterInfo printer1;
    public PrinterInfo printer2;
    public PrinterInfo printer3;
    public PrinterInfo printer4;
    public int menu_output;//메뉴개별출력
}

public struct MonitorSet
{
    public int use;//1-사용, 0-미사용
    public int type;//0-images, 1-video
    public string images;//출력이미지
    public string video_url;
}

public struct Admin
{
    public string id;
    public string name;
    public string code;
}

public struct ShopHistory
{
    public string date;
    public string time;
    public int status;//1-open, 0-close
}

public struct ShopInfo
{
    public List<ShopHistory> shHistory;
}

public struct SetInfo
{
    public int pos_no;
    public PaymentDeviceInfo paymentDeviceInfo;
    public PrinterSet printerSet;
    public MonitorSet monitorSet;
    public List<Admin> admins;
    public ShopInfo shopInfo;
    public int tableMain;//1-이용내역, 0-메뉴주문
    public int type;//0-와인, 1-맥주
    public int payment_time;//결제 대기시간
}

public struct UserInfo
{
    public int id;
    public string userID;
    public string password;
    public PubInfo pub;
    public bool is_auto_login;
}

public struct PubInfo
{
    public int id;
    public string name;
    public string phone;
    public string address;
    public string representer;
    public int price;//총결제금액
    public int total_cnt;//총결제건수
    public int paid_price;//결제금액
    public int paid_cnt;//결제건수
    public int pending_price;//미결제금액
    public int pending_cnt;//미결제건수
    public string closetime;//정산기준시간
    public int ceiltype;//결제금액 올림/내림
    public int is_open;//1-open, 0-close
    public RemainInfo rinfo;
    public int invoice_outtype;//영수증출력, 0-미출력, 1-상세출력, 2-메뉴합산출력, 3-간단출력
    public int pointer_type;//1-통합, 0-매장
    public float pointer_rate;//포인트 적립율
    public int prepay_tag_period;//선불태그 유효기간
    public int move_table_type;//자리이동출력, 0-출력, 1-미출력
    public int tap_count;
    public int is_self;
    public int sell_type;//1-ml, 0-cup
}

public struct RemainInfo
{
    public string id;
    public string name;
    public int total_amount;
    public int remaining_amount;
    public float temperature;
    public int serial_number;
    public string last_update_datetime;
}

public struct DayInfo
{
    public int day;
    public int sum;
}

public struct TableGroup
{
    public string name;
    public string id;
    public int order;//순서
    public int tbCnt;//테이블 개수
    public List<TableInfo> tablelist;
}

public struct TableInfo
{
    public string name;
    public string id;
    public int order_price;
    public int order_amount;
    public int order;//순서
    public int is_blank;//1-미사용, 0-사용
    public List<TagInfo> taglist;
}

public struct TagInfo
{
    public string id;
    public string name;
    public int is_pay_after;//1-후불, 0-선불
    public int status;//0-테이블에 미등록, 1-테이블에 등록, 2-분실, 3-태그 코드 미등록
    public string rfid;
    public string qrcode;
    public int period;//유효기간
    public int is_blank;//1-blank, 0-using
}

public struct TableSelectedInformation
{
    public string tgId;
    public string tId;
}

public struct CurTableInfo
{
    public int tgNo;
    public int tNo;
    public string tgid;
    public string tid;
    public string name;
}

public struct CurTagInfo
{
    public string tag_id;
    public int remain;
    public int charge;
    public string table_name;
    public string tag_name;
    public string qrcode;
    public string rfid;
    public string reg_datetime;
    public int period;
    public int is_pay_after;
}

public struct CategoryInfo
{
    public string id;
    public string name;
    public string engname;
    public int sort_order;
    public int is_pos;
    public int is_kiosk;
    public int is_tablet;
    public int is_mobile;
    public List<MenuInfo> menulist;
}

public struct SellInfo
{
    public string id;
    public string name;
    public List<SellMenuInfo> menulist;
}

public struct SellMenuInfo
{
    public string id;
    public string name;
    public List<beerInfo> beerlist;
}

public struct beerInfo
{
    public string id;
    public int index;
    public string name;
    public int size;
}

public struct MenuInfo
{
    public string name;
    public string engname;
    public string contents;
    public string barcode;
    public int sort_order;
    public int price;
    public int pack_price;
    public string id;
    public int is_soldout;
    public int is_best;
    public int sell_amount;
    public int sell_tap;
    public int product_type;//0:food, 1:beer, 2:wine
    public int kit1;
    public int kit2;
    public int kit3;
    public int kit4;
}

public struct MyOrderInfo
{
    public List<OrderCartInfo> ordercartinfo;
    public string orderNo;
    public int total_price;
    public int tag_price;
    public int prepay_price;
    public TableSelectedInformation tsInfo;
}

public struct TableMOrderInfo
{
    public string menu_name;
    public int menu_total_amount;
    public int menu_total_price;
    public string menu_id;
    public int type;//0-이미 주문한 상태, 1-추가되는 메뉴
    public int status;//0-모두 준비중일 때, 1-개중 조리중인 상태가 잇을때
    public List<string> order_ids;
    public int is_service;
}

public struct TableOrderInfo
{
    public List<TableMOrderInfo> menuorderinfo;
    public int total_price;
    public int tag_price;
    public int prepay_price;
}

public struct TagOrderInfo
{
    public List<TableMOrderInfo> menuorderinfo;
    public int remain_price;//tag 잔액
    public int order_price;//주문금액
    public int last_remain_price;//최종잔액
}

public struct TableTagUsageInfo
{
    public string tag_id;
    public string tagName;
    public int tagUsageCnt;
    public int tagUsagePrice;
    public int status;
    public int is_pay_after;
    public List<TagMenuOrderInfo> tagMenuOrderList;
}

public struct TableMenuOrderInfo
{
    public string order_id;
    public string menu_name;
    public string reg_datetime;
    public int amount;
    public int price;
    public int is_service;
    public int status;
}

public struct TagMenuOrderInfo
{
    public string order_id;
    public string menu_name;
    public string reg_datetime;
    public int amount;
    public int status;
    public int is_service;
    public int price;
}

public struct TableUsageInfo
{
    public string tableId;
    public List<TableTagUsageInfo> tagUsageList;
    public List<TableMenuOrderInfo> menuOrderList;
}

public struct ChargeItemInfo
{
    public string charge_time;//충전시간
    public string card_type;
    public string card_no;//카드번호
    public int price;//충전금액
    public string id;

    //결제취소용 데이터
    public int device_type;
    public string appno;
    public string payTime;
}

public struct PrepayTagMenuOrderItemInfo
{
    public string order_id;
    public string menu_id;
    public string menu_name;
    public string order_time;
    public int amount;
    public int status;
    public int price;
    public int is_service;
}

public struct PrepayTagUsage
{
    public List<ChargeItemInfo> chargeItemlist;
    public int charge_sum_price;
    public List<PrepayTagMenuOrderItemInfo> menuOrderlist;
    public int order_sum_price;
    public int remain_price;
}

public struct NoticeItemInfo
{
    public string datetime;
    public int tap_no;
    public string content;
}

public struct OrderCartInfo
{
    public string order_id;
    public string tag_name;
    public int product_type;//0:food, 1:beer, 2:wine
    public string menu_id;
    public string name;
    public int price;
    public int amount;
    public string trno;
    public int is_best;
    public int status;
    public string order_time;
    public string barcode;
}

public struct PrepayInfo
{
    //예치금정보
    public string id;
    public string user_name;
    public string no;
    public string first_reg_time;//최초등록일
    public int charge_price;
    public int used_price;
    public int remain_price;
    public string bigo;
}

public struct PointInfo
{
    //포인트정보
    public string id;
    public string user_name;
    public string no;
    public string first_reg_time;//최초등록일
    public int save_point;
    public int used_point;
    public int remain_point;
}

public struct CheckSellDayInfo
{
    public int payCnt;
    public int payPrice;
    public int card;
    public int money;
    public int orderPrice;
    public int point;
    public int service;
    public int cutPrice;
    public int sellPrice;
}

public struct CheckSellMenuInfo
{
    public string category_name;
    public string menu_name;
    public int unit_price;
    public int amount;
    public int service_cnt;
    public int price;
}

public struct OrderManageInfo
{
    public int orderSeq;//주문번호
    public string reg_datetime;
    public string table_name;
    public string tag_name;
    public string menu_name;//여러개인 경우 맨 처음 메뉴명
    public int amount;//주문수량
    public string input_from;//주문기기
    public string type;//취소,주문
    public int sum;//주문 합계
    public List<MenuOrderManageInfo> menulist;
}

public struct MenuOrderManageInfo
{
    public string menu_id;
    public string order_id;
    public string menu_name;
    public int amount;
    public int price;
}

public struct PaymentManageInfo
{
    public string time;
    public string card_type;//카드 유형
    public string card_no;//카드번호
    public string accept_no;//승인번호
    public int price;
    public string type;//구분
    public string table_name;
    public string tag_name;
    public string id;
    public int device_type;//단말기이용방식
    public string pay_time;
    public int payment_type;//0:현금, 1:카드
    public List<PaymentMenuManageInfo> menulist;
}

public struct PaymentMenuManageInfo
{
    public string menu_name;
    public string time;
    public int amount;
    public int price;
    public string pay_id;
    public string order_id;
}

public struct PrepayTagManageInfo
{
    public int reg_cnt;//등록
    public int using_cnt;//사용중
    public int completed_cnt;//사용완료
    public int expired_cnt;//기간만료
    public int remain_sum;//잔액 합계
    public int expired_sum;//만료 태그 잔액
    public int useful_sum;//유효잔액
    public List<PrepayTagManageDetailInfo> tagList;
}

public struct TagManageInfo
{
    public int reg_cnt;//등록
    public int using_cnt;//사용중
    public int lost_cnt;//분실
    public List<TagInfo> tagList;
}

public struct PrepayTagManageDetailInfo
{
    public string id;
    public int no;
    public string name;
    public string reg_datetime;
    public string expried_datetime;
    public string last_used_datetime;
    public int charge_price;
    public int used_price;
    public int remain_price;
    public int period;
    public string qrcode;
    public string rfid;
    public int is_pay_after;

}

public struct RemainDetailInfo
{
    public string id;
    public int no;
    public string name;//맥주, 와인명
    public int size;//현재 사용량
    public int total;//총 용량
    public string reg_time;
}

public struct TapSellInfo
{
    public string id;
    public int no;//번호
    public string name;//
    public string start_date;//판매시작일
    public string sell_period;//판매기간
    public int normal_sell_cnt;//정량판매
    public int ml_sell_cnt;//ml판매
    public int cancel_sell_cnt;//판매취소
    public int out_capacity;//추출용량
    public int cancel_capacity;//취소용량
    public int sell_capacity;//판매용량
    public int sell_price;//판매금액
}

public struct TapSellPerDayInfo
{
    public int no;
    public string time;
    public string table_name;
    public string tag_name;
    public string beer_name;
    public int capacity;
    public int price;
    public string bigo;
}

public struct TapInfo
{
    public string id;
    public int no;
    public string name;
    public int unit_price;
    public int cup_capacity;
    public int keg_capacity;
    public int remain;
    public int sell_type;//1-ml, 0-cup
    public string product_id;
}

public struct TapSellMenuInfo
{
    public int serial_number;
    public string product_name;
}

public struct PayOrderInfo
{
    public string order_id;
    public string menu_name;
    public int amount;
    public int is_service;
    public int price;
}

public struct ClientInfo
{
    public string name;
    public string no;
    public string id;
    public string first_visit_date;
    public string last_visit_date;
    public int visit_count;
    public int price;
    public int point;
    public int prepay;
    public string bigo;
}

public struct PretagInfo
{
    public string qrcode;
    public string id;
    public string name;
    public int remain;
}

public class Global
{
    //setting information
    public static bool is_id_saved = false;
    public static int alarm_cnt = 0;
    public static int app_type = 0;//0-beer 1-wine

    public static string last_scene;
    public static string selected_tableid = "";
    public static string selected_tablename = "";

    public static Color selected_color = new Color(1, 200 / 255f, 0f);
    public static SetInfo setinfo = new SetInfo();

    public static UserInfo userinfo = new UserInfo();
    public static List<TableGroup> tableGroupList = new List<TableGroup>();
    public static List<CategoryInfo> categorylist = new List<CategoryInfo>();
    public static List<CategoryInfo> categorylistSell = new List<CategoryInfo>();
    public static List<SellInfo> selllist = new List<SellInfo>();
    public static List<TagInfo> taglist = new List<TagInfo>();
    public static List<TagInfo> cur_tagList = new List<TagInfo>();
    public static CurTagInfo cur_tagInfo = new CurTagInfo();
    public static CurTableInfo cur_tInfo = new CurTableInfo();
    public static List<NoticeItemInfo> noticeList = new List<NoticeItemInfo>();
    public static List<TapInfo> tapList = new List<TapInfo>();
    public static List<TapSellMenuInfo> tapSellMenuList = new List<TapSellMenuInfo>();
    public static TableUsageInfo tableUsageInfo = new TableUsageInfo();
    public static int moveTableType = 0;
    public static int cur_tap_no = 0;
    public static string cur_tap_id = "";
    public static string cur_pay_id = "";
    public static bool is_applied_state = false;//
    public static DateTime old_day;
    public static int checktime = 1800;
    public static bool is_start = false;

    //image download path
    public static string imgPath = "";
    public static string prePath = "";

    //api
    public static string server_address = "";
    public static string api_server_port = "3006";
    public static string api_url = "";
    static string api_prefix = "m-api/pos/";

    public static string login_api = api_prefix + "login";
    public static string get_table_group_api = api_prefix + "get-tablegroup";
    public static string change_table_group_api = api_prefix + "change-tablegroup";
    public static string change_table_api = api_prefix + "change-table";
    public static string get_noticelist_api = api_prefix + "get-noticelist";
    public static string confirm_notice_api = api_prefix + "confirm-notice";
    public static string find_tag_api = api_prefix + "find-tag";
    public static string cancel_tags_api = api_prefix + "cancel-tags";
    public static string cancel_tabletag_api = api_prefix + "cancel-tabletags";
    public static string cancel_tag_api = api_prefix + "cancel-tag";
    public static string get_admins_api = api_prefix + "get-admin";
    public static string reg_admin_api = api_prefix + "reg-admin";
    public static string get_shop_history_api = api_prefix + "get-shophistory";
    public static string set_shopstatus_api = api_prefix + "set-shopstatus";
    public static string get_categorylist_api = api_prefix + "get-categorylist";
    public static string get_selllist_api = api_prefix + "get-selllist";
    public static string change_categorylist_api = api_prefix + "change-categorylist";
    public static string change_menulist_api = api_prefix + "change-menulist";
    public static string change_sellsize_api = api_prefix + "change-sellsize";
    public static string change_menuoutlist_api = api_prefix + "change-menuoutlist";
    public static string change_ceiltype_api = api_prefix + "change-ceiltype";
    public static string change_closetime_api = api_prefix + "change-closetime";
    public static string change_pointinfo_api = api_prefix + "change-pointinfo";
    public static string change_pertagexpiredtime_api = api_prefix + "change-prepaytagexpiredtag";
    public static string change_movetabletype_api = api_prefix + "change-movetabletype";
    public static string change_tapinfo_api = api_prefix + "change-tapinfo";
    public static string add_prepay_client_api = api_prefix + "add-prepayClient";
    public static string get_orderlist_api = api_prefix + "get-orderlist";
    public static string get_paymentlist_api = api_prefix + "get-paymentlist";
    public static string get_prepaytag_api = api_prefix + "get-prepaytag";
    public static string get_tapsell_list_api = api_prefix + "get-tapsell";
    public static string get_tapsellday_info_api = api_prefix + "get-tapsellday-info";
    public static string get_tapsel_detail_api = api_prefix + "get-tapsell-detail";
    public static string get_taplist_api = api_prefix + "get-taplist";
    public static string get_taglist_api = api_prefix + "get-taglist";
    public static string get_remain_api = api_prefix + "get-remain";
    public static string find_tag_usage_api = api_prefix + "find-tag-usage";
    public static string cancel_order_api = api_prefix + "cancel-order";
    public static string cancel_prepay_api = api_prefix + "cancel-prepay";
    public static string get_sell_status_api = api_prefix + "get-sellstatus";
    public static string reg_tag_sell_api = api_prefix + "reg-tagsell";
    public static string reg_sell_api = api_prefix + "reg-sell";
    public static string add_discount_api = api_prefix + "add-discount";
    public static string service_order_api = api_prefix + "service-order";
    public static string move_table_api = api_prefix + "move-table";
    public static string get_tableorderlist_api = api_prefix + "get-tableorderlist";
    public static string order_api = api_prefix + "order";
    public static string get_tableusagelist_api = api_prefix + "get-tableusagelist";
    public static string reg_tag_api = api_prefix + "reg-tag";//
    public static string reg_tags_api = api_prefix + "reg-tags";//
    public static string get_month_api = api_prefix + "get-month";
    public static string get_monthday_api = api_prefix + "get-monthday";
    public static string get_day_api = api_prefix + "get-day";
    public static string get_sellprepay_api = api_prefix + "get-sellprepay";
    public static string pay_api = api_prefix + "pay";
    public static string cancel_pay_api = api_prefix + "cancel-pay";
    public static string check_ordertype_api = api_prefix + "check_ordertype";
    public static string check_client_api = api_prefix + "check-client";
    public static string divide_api = api_prefix + "divide";
    public static string check_client_history_api = api_prefix + "check-client-history";
    public static string converted_prepay_api = api_prefix + "convert-prepay";
    public static string del_tags_api = api_prefix + "del-tag";
    public static string add_tags_api = api_prefix + "add-tags";
    public static string lost_tag_api = api_prefix + "lost-tag";
    public static string change_tag_api = api_prefix + "change-tag";
    public static string cancel_charge_api = api_prefix + "cancel-charge";
    public static string convert_tag_api = api_prefix + "convert-tag";
    public static string init_tap_api = api_prefix + "init-tap";
    public static string resend_tap_api = api_prefix + "resend-tap-info";
    public static string remove_tap_api = api_prefix + "remove-tap";
    public static string change_unitprice_api = api_prefix + "change-unitprice";
    public static string get_beerlist_api = api_prefix + "get-beerlist";
    public static string change_tapdetail_api = api_prefix + "change-tapdetail";
    public static string search_prepay_api = api_prefix + "search-prepay";
    public static string get_prepay_detail_api = api_prefix + "get-prepaydetail";
    public static string search_point_api = api_prefix + "search-point";
    public static string get_point_detail_api = api_prefix + "get-pointdetail";
    public static string get_repay_api = api_prefix + "get-repay";
    public static string get_repay_order_api = api_prefix + "get-repayorderlist";
    public static string add_client_api = api_prefix + "reg-client";
    public static string check_sdate_api = api_prefix + "check-sdate";
    public static string modify_api = api_prefix + "modify";
    public static string check_tableusage_api = api_prefix + "check-tableusage";
    public static string check_pw_api = api_prefix + "check-pw";
    public static string get_tabletag_api = api_prefix + "get-tabletags";
    public static string get_tag_status_api = api_prefix + "get-tag-status";
    public static string change_bigo_api = api_prefix + "change-bigo";
    public static string get_output_info_api = api_prefix + "get-outputinfo";
    public static string change_pay_info_api = api_prefix + "change-payinfo";
    public static string get_ordersheet_api = api_prefix + "get-ordersheet";
    public static string get_output_orderlist_api = api_prefix + "get-outputorderlist";
    public static string get_market_status_api = api_prefix + "get-marketstatus";
    public static string check_table_tags_api = api_prefix + "check-tabletags";

    //socket server
    public static string socket_server = "";

    public static void RemoveTag(string tagId)
    {
        bool is_found = false;
        for (int i = 0; i < tableGroupList.Count; i++)
        {
            for (int j = 0; j < tableGroupList[i].tablelist.Count; j++)
            {
                for (int k = 0; k < tableGroupList[i].tablelist[j].taglist.Count; k++)
                {
                    if (tableGroupList[i].tablelist[j].taglist[k].id == tagId)
                    {
                        TableInfo tbinfo = tableGroupList[i].tablelist[j];
                        List<TagInfo> tglist = tbinfo.taglist;
                        tglist.Remove(tglist[k]);
                        tbinfo.taglist = tglist;
                        tableGroupList[i].tablelist[j] = tbinfo;
                        is_found = true;
                        break;
                    }
                }
                if (is_found)
                    break;
            }
            if (is_found)
                break;
        }
    }

    public static int GetPrice(int price)
    {
        switch (userinfo.pub.ceiltype)
        {
            case 0:
                {
                    price = Convert.ToInt32(Math.Ceiling(price / 100f) * 100);
                    break;
                };
            case 1:
                {
                    price = Convert.ToInt32(Math.Floor(price / 100f) * 100);
                    break;
                };
            case 2:
                {
                    price = Convert.ToInt32(Math.Round(price / 100f) * 100);
                    break;
                };
        }
        return price;
    }

    public static string GetPriceFormat(int price)
    {
        return string.Format("{0:N0}", price);
    }

    public static int GetConvertedPrice(string pStr)
    {
        int price = 0;
        while (pStr.IndexOf(',') != -1)
        {
            string p1 = pStr.Substring(0, pStr.IndexOf(','));
            string p2 = pStr.Substring(pStr.IndexOf(',') + 1);
            pStr = p1 + p2;
        }
        try
        {
            price = int.Parse(pStr);
        }catch(Exception ex)
        {

        }
        return price;
    }

    public static string GetNoFormat(int ono)
    {
        return string.Format("{0:D2}", ono);
    }

    public static List<OrderCartInfo> addOneCartItem(OrderCartInfo cinfo, List<OrderCartInfo> cartlist)
    {
        bool is_existing = false;
        for (int i = 0; i < cartlist.Count; i++)
        {
            if (cartlist[i].menu_id == cinfo.menu_id)
            {
                is_existing = true;
                cinfo.amount = cartlist[i].amount + 1;
                cartlist[i] = cinfo;
                break;
            }
        }
        if (!is_existing)
        {
            cartlist.Add(cinfo);
        }
        return cartlist;
    }

    public static DateTime GetSdate(bool is_consider = true)
    {
        DateTime ts;
        try
        {
            ts = Convert.ToDateTime(userinfo.pub.closetime);
        }
        catch (Exception ex)
        {
            ts = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        }
        DateTime t = DateTime.Now;
        try
        {
#if UNITY_EDITOR
            TimeZoneInfo stimeInfo = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
            t = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, stimeInfo);
#elif UNITY_ANDROID
            t = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Asia/Seoul");
#else
            TimeZoneInfo stimeInfo = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
            t = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, stimeInfo);
#endif
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }

        try
        {
            if (t < ts)
            {
                t = DateTime.Now.AddDays(-1);
            }
            //if (is_consider && !is_applied_state)
            //{
            //    t = DateTime.Now.AddDays(-1);
            //}
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }

        return new DateTime(t.Year, t.Month, t.Day);
    }

    public static string StrToHex(string strData)
    {
        string resultHex = "";
        byte[] arr_byteStr = StrToByte(strData);
        foreach (byte byteStr in arr_byteStr)
        {
            resultHex += string.Format("{0:x2}", byteStr);
        }
        return resultHex;
    }

    public static byte[] StrToByte(string byteData)
    {
        System.Text.ASCIIEncoding asencoding = new System.Text.ASCIIEncoding();
        return Encoding.Default.GetBytes(byteData);
    }

    public static string BytToHexstr(byte[] bytes)
    {
        string sResult = string.Empty;

        for (int i = 0; i < bytes.Length; i++)
        {
            sResult += bytes[i].ToString("X2");
        }

        return sResult;
    }

    public static byte[] HexToByte(string msg)
    {
        byte[] comBuffer = new byte[msg.Length / 2];

        for (int i = 0; i < msg.Length; i += 2)
        {
            try
            {
                comBuffer[i / 2] = (byte)Convert.ToByte(msg.Substring(i, 2), 16);
            }
            catch
            {
                comBuffer = null;
            }
        }
        return comBuffer;

    }

    // Kicc CRC16 Check Sum
    public static ushort get_crc(byte[] data)
    {
        byte temp;
        ushort SEED = 0x8005;
        ushort crc = 0xffff;
        for (int k = 0; k < data.Length; k++)
        {
            temp = (byte)(data[k] & 0xff);
            for (int i = 0; i < 8; i++)
            {
                if (((crc & 0x0001) ^ (temp & 0x01)) != 0)
                    crc = (ushort)((crc >> 1) ^ SEED);
                else
                    crc >>= 1;
                temp >>= 1;
            }
        }
        crc = (ushort)~crc;
        ushort utemp = crc;
        crc = (ushort)((crc << 8) | ((utemp >> 8) & 0xff));
        return crc;
    }

    public static string GetPassFormatStr(string str)
    {
        try
        {
            return str.Substring(0, 3) + "****" + str.Substring(7);
        }
        catch (Exception ex)
        {
            return str;
        }
    }
}

public class DocumentFactory
{
    public static int MaxCount;
    public static int MaxCountPrinter;
    #region 기본 정의 및 함수
    public enum StringAlign : int
    {
        Left = 0,
        Center,
        Right
    }
    const string ESC = "\x1b";
    const char DLE = (char)16;
    public static int GetPaddingSize(string str)
    {
        if (string.IsNullOrEmpty(str))
            return 0;

        int euckrCodepage = 51949;
        Encoding euckr = Encoding.GetEncoding(euckrCodepage);
        return euckr.GetByteCount(str);
        //return Encoding.Default.GetByteCount(str);
    }
    public static string GetCompositePrintString(string left = "", string right = "", int maxLength = 0)
    {
        if (maxLength == 0)
            return "";

        if (left == null)
            left = "";
        if (right == null)
            right = "";

        string printString = "".PadRight(maxLength);

        int leftSize = GetPaddingSize(left);
        int rightSize = GetPaddingSize(right);
        int totalSize = leftSize + rightSize;

        if (totalSize < maxLength)
        {
            if (leftSize > 0)
            {
                printString = left + printString.Remove(0, leftSize);
            }
            if (rightSize > 0)
            {
                printString = printString.Remove(printString.Length - rightSize) + right;
            }
        }
        else
        {
            printString = left + right;
        }

        return printString;
    }
    public static string GetCompositePrintString(string[] contents = null, int[] columnInfos = null, int maxLength = 0)
    {
        if (maxLength == 0)
            return "";

        if (contents == null)
            return "";

        if (contents.Length == 0)
            return "";

        if (columnInfos == null)
        {
            columnInfos = new int[contents.Length];
            for (int i = 0; i < contents.Length; ++i)
            {
                columnInfos[i] = contents[i].Length;
            }
        }

        if (columnInfos.Length != contents.Length)
            return "";

        if (columnInfos.Sum() > maxLength)
        {
            columnInfos[0] = columnInfos[0] - (columnInfos.Sum() - maxLength);
        }
        else if (columnInfos.Sum() < maxLength)
        {
            columnInfos[0] = columnInfos[0] + (maxLength - columnInfos.Sum());
        }

        string printString = "";

        for (int i = 0; i < contents.Length; ++i)
        {
            int contentSize = GetPaddingSize(contents[i]);
            int colSize = columnInfos[i];
            if (contentSize == colSize)
                printString += contents[i];
            else if (colSize > contentSize)
            {
                if (i == 0)
                    printString += contents[i] + "".PadRight(colSize - contentSize);
                else
                    printString += "".PadLeft(colSize - contentSize) + contents[i];
            }
            else
            {
                string temp = contents[i];
                while (GetPaddingSize(temp) > colSize - 4)
                {
                    temp = temp.Substring(0, temp.Length - 1);
                }
                printString += temp + "..  ";
            }
        }

        return printString;
    }
    public static string GetPrintString(string str, StringAlign align, int maxLength)
    {
        string printString = "";

        int size = GetPaddingSize(str);
        if (size < maxLength)
        {
            int totalPadding = maxLength - size;
            switch (align)
            {
                case StringAlign.Left:
                    printString = str + "".PadRight(totalPadding);
                    break;
                case StringAlign.Center:
                    printString = "".PadRight(totalPadding / 2);
                    printString += str;
                    printString += "".PadRight(totalPadding / 2 + (totalPadding % 2));
                    break;
                case StringAlign.Right:
                    printString = "".PadRight(totalPadding) + str;
                    break;
            }
        }
        else
        {
            printString = str;
        }

        return printString;
    }
    public static string GetAlignString(StringAlign align)
    {
        string temp = string.Empty;

        temp += ESC;
        temp += "a";
        if (align == StringAlign.Left)
            temp += "0";
        else if (align == StringAlign.Left)
            temp += "1";
        else
            temp += "2";

        return temp;
    }
    public static string GetFontSizeString(int size)
    {
        if (size > 255)
            size = 255;

        string temp = string.Empty;

        temp += ESC;
        temp += "!";
        temp += (char)size;

        return temp;
    }
    public static string GetBlankLineString(int lineCount, bool isCat = true)
    {
        string temp = string.Empty;

        if (isCat)
        {
            for (int i = 0; i < lineCount; ++i)
                temp += EscPrintFactory.NewLine;
        }
        else
        {
            for (int i = 0; i < lineCount; ++i)
                temp += "".PadRight(MaxCountPrinter - 1) + "\n";
        }

        return temp;
    }
    public static string GetStartString()
    {
        string temp = string.Empty;

        temp += ESC;
        temp += '@';
        temp += ESC;
        temp += '!';
        temp += DLE;

        return temp;
    }
    public static string GetCuttingString()
    {
        string temp = string.Empty;

        temp += ESC + "i";

        return temp;
    }
    #endregion

    #region 주문서 문자열 생성
    public static string GetOrderSheet(
        string kitno,
        string kitorderno,
        List<OrderItem> orders,
        DateTime datetime,
        string name = "",
        string takeout = "",
        bool isCat = true)
    {
        //GetOrderSheet(printList, DateTime.Now, tagName, isCat:false);
        int cnt = 0;
        int maxLength = 42;
        MaxCountPrinter = 42;

        StringBuilder sb = new StringBuilder();
        sb.Append(GetStartString());
        sb.Append(GetFontSizeString(16));
        sb.Append(GetPrintString("[주문서]", StringAlign.Center, maxLength));
        sb.Append(GetBlankLineString(1, isCat: isCat));
        string str = "주문번호 : " + kitorderno;
        if (takeout.Equals("T"))
            sb.Append(GetCompositePrintString(str, "[포장]", maxLength) + "\n");
        else
            sb.Append(GetPrintString(str, StringAlign.Left, maxLength) + "\n");
        sb.Append(GetCompositePrintString(datetime.ToString("MM-dd HH:mm"), name, maxLength) + "\n");
        sb.Append(GetFontSizeString(4));
        sb.Append(GetPrintString("".PadLeft(maxLength, '='), StringAlign.Left, maxLength) + "\n");
        sb.Append(GetPrintString("  메뉴                               수량", StringAlign.Left, maxLength) + "\n");
        sb.Append(GetPrintString("".PadLeft(maxLength, '='), StringAlign.Left, maxLength) + "\n");
        sb.Append(GetFontSizeString(16));
        foreach (var order in orders)
        {
            if ((kitno == "1" && order.kit01 == 1) ||
                (kitno == "2" && order.kit02 == 1) ||
                (kitno == "3" && order.kit03 == 1) ||
                (kitno == "4" && order.kit04 == 1))
            {
                cnt++;
                sb.Append(GetCompositePrintString(order.product_name, order.quantity.ToString(), maxLength) + "\n");
            }
        }
        sb.Append(GetFontSizeString(4));
        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength) + "\n");
        sb.Append(GetBlankLineString((int)(5), isCat: isCat));
        //sb.Append(GetBlankLineString((int)(5 + (float)orders.Count * 0.7), isCat: isCat));
        sb.Append(GetCuttingString());
        if (cnt == 0)
            return string.Empty;
        return sb.ToString();
    }
    #endregion

    #region 주문서 메뉴개별출력 문자열 생성
    public static string GetOrderItemSheet(
        string kitno,
        string kitorderno,
        OrderItem order,
        DateTime datetime,
        string name = "",
        string takeout = "",
        bool isCat = true)
    {
        //GetOrderSheet(printList, DateTime.Now, tagName, isCat:false);
        int cnt = 0;
        int maxLength = 42;
        MaxCountPrinter = 42;

        StringBuilder sb = new StringBuilder();
        sb.Append(GetStartString());
        sb.Append(GetFontSizeString(16));
        sb.Append(GetPrintString("[주문서]", StringAlign.Center, maxLength));
        sb.Append(GetBlankLineString(1, isCat: isCat));
        string str = "주문번호 : " + kitorderno;
        if (takeout.Equals("T"))
            sb.Append(GetCompositePrintString(str, "[포장]", maxLength) + "\n");
        else
            sb.Append(GetPrintString(str, StringAlign.Left, maxLength) + "\n");
        sb.Append(GetCompositePrintString(datetime.ToString("MM-dd HH:mm"), name, maxLength) + "\n");
        sb.Append(GetFontSizeString(4));
        sb.Append(GetPrintString("".PadLeft(maxLength, '='), StringAlign.Left, maxLength) + "\n");
        sb.Append(GetPrintString("  메뉴                               수량", StringAlign.Left, maxLength) + "\n");
        sb.Append(GetPrintString("".PadLeft(maxLength, '='), StringAlign.Left, maxLength) + "\n");
        sb.Append(GetFontSizeString(16));
        if ((kitno == "1" && order.kit01 == 1) ||
            (kitno == "2" && order.kit02 == 1) ||
            (kitno == "3" && order.kit03 == 1) ||
            (kitno == "4" && order.kit04 == 1))
        {
            cnt++;
            sb.Append(GetCompositePrintString(order.product_name, order.quantity.ToString(), maxLength) + "\n");
        }
        sb.Append(GetFontSizeString(4));
        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength) + "\n");
        sb.Append(GetBlankLineString((int)(5), isCat: isCat));
        //sb.Append(GetBlankLineString((int)(5 + (float)orders.Count * 0.7), isCat: isCat));
        sb.Append(GetCuttingString());
        if (cnt == 0)
            return string.Empty;
        return sb.ToString();
    }
    #endregion

    #region 간단 영수증 문자열 생성
    public static string GetReceiptSimple(
        Payment payment,
        string name = "",
        string title = "[영수증]")
    {
        int maxLength = Global.setinfo.paymentDeviceInfo.line_count == 0 ? 48 : Global.setinfo.paymentDeviceInfo.line_count;
        MaxCountPrinter = 48;
        StringBuilder sb = new StringBuilder();
        sb.Append(GetStartString());
        sb.Append(GetFontSizeString(4));
        sb.Append(GetPrintString(title, StringAlign.Center, maxLength));
        sb.Append(GetBlankLineString(1));
        sb.Append(GetPrintString(Global.userinfo.pub.name, StringAlign.Left, maxLength));
        sb.Append(GetPrintString(Global.userinfo.pub.phone, StringAlign.Left, maxLength));
        sb.Append(GetPrintString("대표자 : " + Global.userinfo.pub.representer, StringAlign.Left, maxLength));
        sb.Append(GetPrintString(Global.userinfo.pub.address, StringAlign.Left, maxLength));
        sb.Append(GetBlankLineString(1));
        sb.Append(GetCompositePrintString("결제일시 : " + payment.reg_datetime, name, maxLength));
        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength));
        sb.Append(GetCompositePrintString("합      계", Global.GetPriceFormat((int)(payment.payamt)), maxLength));
        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength));
        if (payment.cutamt > 0)
        {
            sb.Append(GetCompositePrintString("절사금액", Global.GetPriceFormat((int)(payment.cutamt)), maxLength));
        }
        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength));
        sb.Append(GetPrintString((payment.payment_type == 0 ? "현금" : "카드"), StringAlign.Left, maxLength));

        if (payment.payment_type == 1)
        {
            sb.Append(GetPrintString("[카드사명] " + payment.credit_card_company, StringAlign.Left, maxLength));
            sb.Append(GetPrintString("[카드번호] " + payment.credit_card_number, StringAlign.Left, maxLength));
            sb.Append(GetPrintString("[할부개월] " + payment.installment_months, StringAlign.Left, maxLength));
            sb.Append(GetPrintString("[승인번호] " + payment.appno, StringAlign.Left, maxLength));
        }
        else
        {
            if (payment.appno != null && payment.appno != "")
            {
                if (payment.installment_months == 0)
                    sb.Append(GetPrintString("** 소 득 공 제 ** ", StringAlign.Center, maxLength));
                else
                    sb.Append(GetPrintString("** 지 출 증 빙 ** ", StringAlign.Center, maxLength));
                sb.Append(GetCompositePrintString("증빙번호", payment.credit_card_number, maxLength));
                sb.Append(GetCompositePrintString("승인번호", payment.appno, maxLength));
            }
        }

        if (payment.custpoint > 0)
        {
            int addpoint = (int)Math.Round((float)payment.payamt * Global.userinfo.pub.pointer_rate / 100f);

            sb.Append(GetBlankLineString(2));
            sb.Append(GetCompositePrintString("회 원 번 호", Global.GetPassFormatStr(payment.custno), maxLength));
            sb.Append(GetCompositePrintString("적립 포인트", Global.GetPriceFormat(addpoint), maxLength));
            sb.Append(GetCompositePrintString("누적 포인트", Global.GetPriceFormat((int)(payment.custpoint)), maxLength));
        }
        sb.Append(GetBlankLineString(5));
        sb.Append(GetCuttingString()); return sb.ToString();
    }
    #endregion

    #region 상세 영수증 문자열 생성
    public static string GetReceiptDetail(
        Payment payment,
        List<OrderItem> orders,
        string name = "",
        string title = "[영수증(상세)]")
    {
        int maxLength = Global.setinfo.paymentDeviceInfo.line_count == 0 ? 48 : Global.setinfo.paymentDeviceInfo.line_count;
        MaxCountPrinter = 48;
        StringBuilder sb = new StringBuilder();
        sb.Append(GetStartString());
        sb.Append(GetFontSizeString(4));
        sb.Append(GetPrintString(title, StringAlign.Center, maxLength));
        sb.Append(GetBlankLineString(1));
        sb.Append(GetPrintString(Global.userinfo.pub.name, StringAlign.Left, maxLength));
        sb.Append(GetPrintString(Global.userinfo.pub.phone, StringAlign.Left, maxLength));
        sb.Append(GetPrintString("대표자 : " + Global.userinfo.pub.representer, StringAlign.Left, maxLength));
        sb.Append(GetPrintString(Global.userinfo.pub.address, StringAlign.Left, maxLength));
        sb.Append(GetBlankLineString(1));
        sb.Append(GetCompositePrintString("결제일시 : " + payment.reg_datetime, name, maxLength));
        sb.Append(GetPrintString("".PadLeft(maxLength, '='), StringAlign.Left, maxLength));
        sb.Append(GetCompositePrintString(new string[] { "메뉴명", "단가", "수량", "금액" }, new int[] { (int)(18 * (maxLength / 48f)), (int)(10 * (maxLength / 48f)), (int)(6 * (maxLength / 48f)), (int)(14 * (maxLength / 48f)) }, maxLength));
        sb.Append(GetPrintString("".PadLeft(maxLength, '='), StringAlign.Left, maxLength));
        foreach (var order in orders)
        {
            string product_name = order.product_name;
            string product_unit_price = order.product_unit_price != 0 ? Global.GetPriceFormat((int)(order.product_unit_price)) : "";
            string product_quantity = order.quantity !=0 ? order.quantity.ToString() : "";
            string paid_price = Global.GetPriceFormat((int)(order.is_service == 1 ? 0 : order.paid_price));
            sb.Append(GetCompositePrintString(
                new string[] { product_name, product_unit_price, product_quantity, paid_price },
                new int[] { (int)(18 * (maxLength / 48f)), (int)(10 * (maxLength / 48f)), (int)(6 * (maxLength / 48f)), (int)(14 * (maxLength / 48f)) },
                maxLength));
        }
        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength));
        if (payment.prepayamt > 0)
            sb.Append(GetCompositePrintString("선결제", Global.GetPriceFormat((int)(-payment.prepayamt)), maxLength));
        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength));
        sb.Append(GetCompositePrintString("합      계", Global.GetPriceFormat((int)(payment.payamt)), maxLength));
        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength));
        if (payment.cutamt != 0)
        {
            sb.Append(GetCompositePrintString("절사금액", Global.GetPriceFormat((int)(payment.cutamt)), maxLength));
        }
        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength));
        sb.Append(GetPrintString((payment.payment_type == 0 ? "현금" : "카드"), StringAlign.Left, maxLength));

        if (payment.payment_type == 1)
        {
            sb.Append(GetPrintString("[카드사명] " + payment.credit_card_company, StringAlign.Left, maxLength));
            sb.Append(GetPrintString("[카드번호] " + payment.credit_card_number, StringAlign.Left, maxLength));
            sb.Append(GetPrintString("[할부개월] " + payment.installment_months, StringAlign.Left, maxLength));
            sb.Append(GetPrintString("[승인번호] " + payment.appno, StringAlign.Left, maxLength));
        }
        else
        {
            if (payment.appno != null && payment.appno != "")
            {
                if (payment.installment_months == 0)
                    sb.Append(GetPrintString("** 소 득 공 제 ** ", StringAlign.Center, maxLength));
                else
                    sb.Append(GetPrintString("** 지 출 증 빙 ** ", StringAlign.Center, maxLength));
                sb.Append(GetCompositePrintString("증빙번호", payment.credit_card_number, maxLength));
                sb.Append(GetCompositePrintString("승인번호", payment.appno, maxLength));
            }
        }
        if (payment.custpoint > 0)
        {
            int addpoint = (int)Math.Round((float)payment.payamt * Global.userinfo.pub.pointer_rate / 100f);
            sb.Append(GetBlankLineString(2));
            sb.Append(GetCompositePrintString("회 원 번 호", Global.GetPassFormatStr(payment.custno), maxLength));
            sb.Append(GetCompositePrintString("적립 포인트", Global.GetPriceFormat(addpoint), maxLength));
            sb.Append(GetCompositePrintString("누적 포인트", Global.GetPriceFormat((int)(payment.custpoint)), maxLength));
        }
        sb.Append(GetBlankLineString(5));
        sb.Append(GetCuttingString());

        return sb.ToString();
    }
    #endregion


    #region 일매출내역 출력
    public static string GetPrintDayDoc(
            CheckSellDayInfo dayInfo,
            string datetime = "")
    {
        int maxLength = Global.setinfo.paymentDeviceInfo.line_count == 0 ? 48 : Global.setinfo.paymentDeviceInfo.line_count;
        MaxCountPrinter = 48;
        StringBuilder sb = new StringBuilder();
        sb.Append(GetStartString());
        sb.Append(GetFontSizeString(16));
        sb.Append(GetPrintString("일매출", StringAlign.Center, maxLength));
        sb.Append(GetBlankLineString(1));
        sb.Append(GetPrintString(datetime, StringAlign.Right, maxLength));
        sb.Append(GetBlankLineString(1));
        sb.Append(GetFontSizeString(4));
        sb.Append(GetPrintString("결제", StringAlign.Left, maxLength));
        sb.Append(GetCompositePrintString(" 결제건수", dayInfo.payCnt.ToString(), maxLength));
        sb.Append(GetCompositePrintString(" 결제금액", Global.GetPriceFormat(dayInfo.payPrice), maxLength));
        sb.Append(GetCompositePrintString(" 카드", Global.GetPriceFormat(dayInfo.card), maxLength));
        sb.Append(GetCompositePrintString(" 현금", Global.GetPriceFormat(dayInfo.money), maxLength));
        sb.Append(GetBlankLineString(1));
        sb.Append(GetPrintString("매출", StringAlign.Left, maxLength));
        sb.Append(GetCompositePrintString(" 주문금액", Global.GetPriceFormat(dayInfo.orderPrice), maxLength));
        sb.Append(GetCompositePrintString(" 할인/포인트", Global.GetPriceFormat(dayInfo.point), maxLength));
        sb.Append(GetCompositePrintString(" 서비스  ", Global.GetPriceFormat(dayInfo.service), maxLength));
        sb.Append(GetCompositePrintString(" 절사금액  ", Global.GetPriceFormat(dayInfo.cutPrice), maxLength));
        sb.Append(GetCompositePrintString(" 실매출액 ", Global.GetPriceFormat(dayInfo.sellPrice), maxLength));
        sb.Append(GetBlankLineString(5));
        sb.Append(GetCuttingString());

        return sb.ToString();
    }
    #endregion


    #region 주문내역 문자열 생성
    public static string GetOrderListDetail(
        List<OrderItem> orders,
        int total_price = 0,
        string name = "",
        string title = "주문내역")
    {
        int maxLength = Global.setinfo.paymentDeviceInfo.line_count == 0 ? 48 : Global.setinfo.paymentDeviceInfo.line_count;
        MaxCountPrinter = 48;
        StringBuilder sb = new StringBuilder();
        sb.Append(GetStartString());
        sb.Append(GetFontSizeString(4));
        sb.Append(GetPrintString(title, StringAlign.Center, maxLength));
        sb.Append(GetBlankLineString(1));
        sb.Append(GetPrintString(Global.userinfo.pub.name, StringAlign.Left, maxLength));
        sb.Append(GetPrintString(Global.userinfo.pub.phone, StringAlign.Left, maxLength));
        sb.Append(GetPrintString(Global.userinfo.pub.address, StringAlign.Left, maxLength));
        sb.Append(GetBlankLineString(1));
        sb.Append(GetPrintString("".PadLeft(maxLength, '='), StringAlign.Left, maxLength));
        sb.Append(GetCompositePrintString(new string[] { "메뉴명", "단가", "수량", "금액" }, new int[] { (int)(18 * (maxLength / 48f)), (int)(10 * (maxLength / 48f)), (int)(6 * (maxLength / 48f)), (int)(14 * (maxLength / 48f)) }, maxLength));
        sb.Append(GetPrintString("".PadLeft(maxLength, '='), StringAlign.Left, maxLength));
        foreach (var order in orders)
        {
            string product_name = order.product_name;
            string product_unit_price = Global.GetPriceFormat((int)(order.product_unit_price));
            string product_quantity = "";
            if (order.is_self == 0)
            {
                product_quantity = order.quantity.ToString();
            } else
            {
                product_quantity = order.capacity.ToString();
            }

            string paid_price = Global.GetPriceFormat((int)(order.is_service == 1 ? 0 : order.paid_price));
            sb.Append(GetCompositePrintString(
                new string[] { product_name, product_unit_price, product_quantity, paid_price },
                new int[] { (int)(18 * (maxLength / 48f)), (int)(10 * (maxLength / 48f)), (int)(6 * (maxLength / 48f)), (int)(14 * (maxLength / 48f)) },
                maxLength));
        }
        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength));
        sb.Append(GetCompositePrintString("합      계", Global.GetPriceFormat(total_price), maxLength));
        sb.Append(GetBlankLineString(5));
        sb.Append(GetCuttingString());

        return sb.ToString();
    }
    #endregion

    #region 메뉴리스트 출력
    public static string GetPrintMenu(
        string txtDate)
    {
        int maxLength = Global.setinfo.paymentDeviceInfo.line_count == 0 ? 48 : Global.setinfo.paymentDeviceInfo.line_count;
        MaxCount = 48;
        StringBuilder sb = new StringBuilder();
        sb.Append(GetStartString());
        sb.Append(GetFontSizeString(4));
        sb.Append(GetPrintString("메뉴 판매내역", StringAlign.Center, maxLength));
        sb.Append(DocumentFactory.GetPrintString(Global.userinfo.pub.name, DocumentFactory.StringAlign.Left, maxLength));
        sb.Append(DocumentFactory.GetPrintString("영업일자 : " + txtDate, DocumentFactory.StringAlign.Left, maxLength));
        sb.Append(DocumentFactory.GetPrintString("".PadLeft(maxLength, '='), DocumentFactory.StringAlign.Left, maxLength));
        sb.Append(DocumentFactory.GetBlankLineString(1));
        sb.Append(GetCompositePrintString(new string[] { "메뉴명", "단가", "수량", "금액" }, new int[] { (int)(18 * (maxLength / 48f)), (int)(10 * (maxLength / 48f)), (int)(6 * (maxLength / 48f)), (int)(14 * (maxLength / 48f)) }, maxLength));
        sb.Append(GetPrintString("".PadLeft(maxLength, '='), StringAlign.Left, maxLength));
        //for (int k = 0; k < orders.Rows.Count - 1; k++)
        //{
        //    string product_name = orders.Rows[k].Cells[0].Value.ToString();
        //    string price = orders.Rows[k].Cells[1].Value.ToString();
        //    string qty = orders.Rows[k].Cells[2].Value.ToString();
        //    string amt = orders.Rows[k].Cells[3].Value.ToString();
        //    sb.Append(GetCompositePrintString(
        //            new string[] { product_name, price, qty, amt },
        //            new int[] { 18, 8, 10, 12 },
        //            maxLength));
        //    if (k == 0)
        //        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength));

        //}
        sb.Append(DocumentFactory.GetPrintString("".PadLeft(maxLength, '='), DocumentFactory.StringAlign.Left, maxLength));

        sb.Append(GetBlankLineString(5));
        sb.Append(GetCuttingString());

        return sb.ToString();
    }
    #endregion

    #region 테이블 이동 생성
    public static string GetTableMove(
        string originTableName = "",
        string destinationTableName = "",
        bool isCat = true)
    {
        //GetOrderSheet(printList, DateTime.Now, tagName, isCat:false);
        int maxLength = 42;
        MaxCountPrinter = 42;

        StringBuilder sb = new StringBuilder();
        sb.Append(GetStartString());
        sb.Append(GetFontSizeString(16));
        sb.Append(GetPrintString("[테이블 이동]", StringAlign.Center, maxLength));
        sb.Append(GetBlankLineString(1, isCat: isCat));
        sb.Append(GetPrintString(DateTime.Now.ToString("MM-dd HH:mm"), StringAlign.Left, maxLength) + "\n");
        sb.Append(GetFontSizeString(4));
        sb.Append(GetPrintString("".PadLeft(maxLength, '='), StringAlign.Left, maxLength) + "\n");
        sb.Append(GetFontSizeString(16));
        sb.Append(GetPrintString(originTableName + " -> " + destinationTableName, StringAlign.Left, maxLength) + "\n");
        sb.Append(GetFontSizeString(4));
        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength) + "\n");
        sb.Append(GetBlankLineString((int)(5), isCat: isCat));
        //sb.Append(GetBlankLineString((int)(5 + (float)orders.Count * 0.7), isCat: isCat));
        sb.Append(GetCuttingString());
        return sb.ToString();
    }
    #endregion

    #region 시험출력데이터
    public static string MakePrintData()
    {
        int maxLength = Global.setinfo.paymentDeviceInfo.line_count == 0 ? 48 : Global.setinfo.paymentDeviceInfo.line_count;
        MaxCountPrinter = 48;
        StringBuilder sb = new StringBuilder();
        sb.Append(GetStartString());
        sb.Append(GetFontSizeString(16));
        sb.Append(GetPrintString("영수증", StringAlign.Center, maxLength));
        sb.Append(GetBlankLineString(1));
        sb.Append(GetFontSizeString(4));
        sb.Append(GetPrintString("맥주가게", StringAlign.Left, maxLength));
        sb.Append(GetPrintString("123-45-67890", StringAlign.Left, maxLength));
        sb.Append(GetPrintString("대표자 : " + Global.userinfo.pub.representer, StringAlign.Left, maxLength));
        sb.Append(GetPrintString("서울시 종로구 맥주대로 111", StringAlign.Left, maxLength));
        sb.Append(GetPrintString("결제일시 : 2022-01-07 15:00", StringAlign.Left, maxLength));
        sb.Append(GetPrintString("".PadLeft(maxLength, '='), StringAlign.Left, maxLength));
        sb.Append(GetCompositePrintString(new string[] { "메뉴명", "단가", "수량", "금액" }, new int[] { (int)(18 * (maxLength / 48f)), (int)(10 * (maxLength / 48f)), (int)(6 * (maxLength / 48f)), (int)(14 * (maxLength / 48f)) }, maxLength));
        sb.Append(GetPrintString("".PadLeft(maxLength, '='), StringAlign.Left, maxLength));
        sb.Append(GetCompositePrintString(
                     new string[] { "고르곤졸라피자", "10,000", "1", "10,000" },
                     new int[] { (int)(18 * (maxLength / 48f)), (int)(10 * (maxLength / 48f)), (int)(6 * (maxLength / 48f)), (int)(14 * (maxLength / 48f)) },
                     maxLength));
        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength));
        sb.Append(GetCompositePrintString("합      계", "10,000", maxLength));
        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength));
        sb.Append(GetPrintString("카      드", StringAlign.Left, maxLength));
        sb.Append(GetPrintString("[카드사명]  하나", StringAlign.Left, maxLength));
        sb.Append(GetPrintString("[카드번호]  123456**********", StringAlign.Left, maxLength));
        sb.Append(GetPrintString("[할부개월]  0", StringAlign.Left, maxLength));
        sb.Append(GetPrintString("[승인번호]  12345678", StringAlign.Left, maxLength));
        sb.Append(GetBlankLineString(5));
        sb.Append(GetCuttingString());

        return sb.ToString();
    }
    #endregion

    #region 시험주방프린터출력데이터
    public static string MakeOrderPrintData()
    {
        //GetOrderSheet(printList, DateTime.Now, tagName, isCat:false);
        int maxLength = 42;
        MaxCountPrinter = 42;

        StringBuilder sb = new StringBuilder();
        sb.Append(GetStartString());
        sb.Append(GetFontSizeString(16));
        sb.Append(GetPrintString("[주문서]", StringAlign.Center, maxLength));
        sb.Append(GetBlankLineString(1));
        string str = "주문번호 : " + 10;
        sb.Append(GetCompositePrintString("01-01 10:10", "1층-1", maxLength) + "\n");
        sb.Append(GetFontSizeString(4));
        sb.Append(GetPrintString("".PadLeft(maxLength, '='), StringAlign.Left, maxLength) + "\n");
        sb.Append(GetPrintString("  메뉴                               수량", StringAlign.Left, maxLength) + "\n");
        sb.Append(GetPrintString("".PadLeft(maxLength, '='), StringAlign.Left, maxLength) + "\n");
        sb.Append(GetFontSizeString(16));
        sb.Append(GetCompositePrintString("피자피자", "1", maxLength) + "\n");
        sb.Append(GetFontSizeString(4));
        sb.Append(GetPrintString("".PadLeft(maxLength, '-'), StringAlign.Left, maxLength) + "\n");
        sb.Append(GetBlankLineString(5));
        sb.Append(GetCuttingString());
        return sb.ToString();
    }
    #endregion
}

public class EscPrintFactory
{
    /// <summary>NUL</summary>
    public static string NUL = Convert.ToString((char)0);
    /// <summary>SOH</summary>
    public static string SOH = Convert.ToString((char)1);
    /// <summary>STX</summary>
    public static string STX = Convert.ToString((char)2);
    /// <summary>ETX</summary>
    public static string ETX = Convert.ToString((char)3);
    /// <summary>EOT</summary>
    public static string EOT = Convert.ToString((char)4);
    /// <summary>ENQ</summary>
    public static string ENQ = Convert.ToString((char)5);
    /// <summary>ACK</summary>
    public static string ACK = Convert.ToString((char)6);
    /// <summary>BEL</summary>
    public static string BEL = Convert.ToString((char)7);
    /// <summary>BS</summary>
    public static string BS = Convert.ToString((char)8);
    /// <summary>TAB</summary>
    public static string TAB = Convert.ToString((char)9);
    /// <summary>VT</summary>
    public static string VT = Convert.ToString((char)11);
    /// <summary>FF</summary>
    public static string FF = Convert.ToString((char)12);
    /// <summary>CR</summary>
    public static string CR = Convert.ToString((char)13);
    /// <summary>SO</summary>
    public static string SO = Convert.ToString((char)14);
    /// <summary>SI</summary>
    public static string SI = Convert.ToString((char)15);
    /// <summary>DLE</summary>
    public static string DLE = Convert.ToString((char)16);
    /// <summary>DC1</summary>
    public static string DC1 = Convert.ToString((char)17);
    /// <summary>DC2</summary>
    public static string DC2 = Convert.ToString((char)18);
    /// <summary>DC3</summary>
    public static string DC3 = Convert.ToString((char)19);
    /// <summary>DC4</summary>
    public static string DC4 = Convert.ToString((char)20);
    /// <summary>NAK</summary>
    public static string NAK = Convert.ToString((char)21);
    /// <summary>SYN</summary>
    public static string SYN = Convert.ToString((char)22);
    /// <summary>ETB</summary>
    public static string ETB = Convert.ToString((char)23);
    /// <summary>CAN</summary>
    public static string CAN = Convert.ToString((char)24);
    /// <summary>EM</summary>
    public static string EM = Convert.ToString((char)25);
    /// <summary>SUB</summary>
    public static string SUB = Convert.ToString((char)26);
    /// <summary>ESC</summary>
    public static string ESC = Convert.ToString((char)27);
    /// <summary>FS</summary>
    public static string FS = Convert.ToString((char)28);
    /// <summary>GS</summary>
    public static string GS = Convert.ToString((char)29);
    /// <summary>RS</summary>
    public static string RS = Convert.ToString((char)30);
    /// <summary>US</summary>
    public static string US = Convert.ToString((char)31);
    /// <summary>Space</summary>
    public static string Space = Convert.ToString((char)32);

    #region 기능 커맨드 모음
    /// <summary> 프린터 초기화</summary>
    public static string InitializePrinter = ESC + "@";

    /// <summary>ASCII LF</summary>
    public static string NewLine = Convert.ToString((char)10);

    /// <summary>
    /// 라인피드
    /// </summary>
    /// <param name="val">라인피드시킬 줄 수</param>
    /// <returns>변환된 문자열</returns>
    public static string LineFeed(int val)
    {
        return EscPrintFactory.ESC + "d" + EscPrintFactory.DecimalToCharString(val);
    }

    /// <summary>볼드 On</summary>
    public static string BoldOn = ESC + "E" + EscPrintFactory.DecimalToCharString(1);

    /// <summary>볼드 Off</summary>
    public static string BoldOff = ESC + "E" + EscPrintFactory.DecimalToCharString(0);

    /// <summary>언더라인 On</summary>
    public static string UnderlineOn = ESC + "-" + EscPrintFactory.DecimalToCharString(1);

    /// <summary>언더라인 Off</summary>
    public static string UnderlineOff = ESC + "-" + EscPrintFactory.DecimalToCharString(0);

    /// <summary>흑백반전 On</summary>
    public static string ReverseOn = GS + "B" + EscPrintFactory.DecimalToCharString(1);

    /// <summary>흑백반전 Off</summary>
    public static string ReverseOff = GS + "B" + EscPrintFactory.DecimalToCharString(0);

    /// <summary>좌측정렬</summary>
    public static string AlignLeft = EscPrintFactory.ESC + "a" + EscPrintFactory.DecimalToCharString(0);

    /// <summary>가운데정렬</summary>
    public static string AlignCenter = EscPrintFactory.ESC + "a" + EscPrintFactory.DecimalToCharString(1);

    /// <summary>우측정렬</summary>
    public static string AlignRight = EscPrintFactory.ESC + "a" + EscPrintFactory.DecimalToCharString(2);
    /// <summary>커트</summary>
    public static string Cut = EscPrintFactory.GS + "V" + EscPrintFactory.DecimalToCharString(1);
    #endregion 기능 커맨드 모음 끝

    /// <summary>
    /// Decimal을 캐릭터 변환 후 스트링을 반환 합니다.
    /// </summary>
    /// <param name="val">커맨드 숫자</param>
    /// <returns>변환된 문자열</returns>
    public static string DecimalToCharString(decimal val)
    {
        string result = "";

        try
        {
            result = Convert.ToString((char)val);
        }
        catch { }

        return result;
    }

    /// <summary>
    /// FONT 명령어의 글자사이즈 속성을 변환 합니다.
    /// </summary>
    /// <param name="width">가로</param>
    /// <param name="height">세로</param>
    /// <returns>가로 x 세로</returns>
    private string ConvertFontSize(int width, int height)
    {
        string result = "0";
        int _w, _h;

        //가로변환
        if (width == 1)
            _w = 0;
        else if (width == 2)
            _w = 16;
        else if (width == 3)
            _w = 32;
        else if (width == 4)
            _w = 48;
        else if (width == 5)
            _w = 64;
        else if (width == 6)
            _w = 80;
        else if (width == 7)
            _w = 96;
        else if (width == 8)
            _w = 112;
        else _w = 0;

        //세로변환
        if (height == 1)
            _h = 0;
        else if (height == 2)
            _h = 1;
        else if (height == 3)
            _h = 2;
        else if (height == 4)
            _h = 3;
        else if (height == 5)
            _h = 4;
        else if (height == 6)
            _h = 5;
        else if (height == 7)
            _h = 6;
        else if (height == 8)
            _h = 7;
        else _h = 0;

        //가로x세로
        int sum = _w + _h;
        result = EscPrintFactory.GS + "!" + EscPrintFactory.DecimalToCharString(sum);

        return result;
    }
}

public struct Payment
{
    public int? payment_type { get; set; }
    public string credit_card_company { get; set; }
    public string credit_card_number { get; set; }
    public int? installment_months { get; set; }
    public decimal? price { get; set; }
    public decimal? payamt { get; set; }
    public decimal? cutamt { get; set; }
    public string reg_datetime { get; set; }
    public string custno { get; set; }
    public decimal? custpoint { get; set; }
    public string appno { get; set; }
    public int? prepayamt { get; set; }
    public int? type {  get; set; }
}

public struct OrderItem
{
    public string product_name { get; set; }
    public int? quantity { get; set; }
    public int? product_unit_price { get; set; }
    public decimal? paid_price { get; set; }
    public int? is_service { get; set; }
    public int? kit01 { get; set; }
    public int? kit02 { get; set; }
    public int? kit03 { get; set; }
    public int? kit04 { get; set; }
    public int? is_self { get; set; }
    public int? capacity { get; set; }
}

