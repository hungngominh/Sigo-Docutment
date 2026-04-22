using AllianceMiddlemanWebAPI.Core.Data.BusinessData;
using AllianceMiddlemanWebAPI.Core.DataInfo;
using AllianceMiddlemanWebAPI.Core.DataInfo.Cached;
using AllianceMiddlemanWebAPI.Core.Services;
using AllianceMiddlemanWebAPI.DataShared.Common;
using AllianceMiddlemanWebAPI.Shared.Models;
using AllianceMiddlemanWebAPI.Shared.Services;
using Ezy.ApiService.ReleaseService.Helper;
using Ezy.APIService.Core.DataInfo;
using Ezy.APIService.Core.Services;
using Ezy.APIService.Shared.Services;
using Ezy.Module.BaseService.FrameWork;
using Ezy.Module.Library.Utilities;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.FormulaParsing.ExpressionGraph.FunctionCompilers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ezy.Module.Insurance.Shared.Helpers;
using Ezy.Module.Insurance.Shared.Models;
using Ezy.Module.Insurance.Shared.Providers.Vifo;

namespace AllianceMiddlemanWebAPI.Shared.Helper
{
    public class RentCarHelper : RentalServiceHelper
    {
        public const string GetEarly = "GetEarly";
        public const string FullDay = "FullDay";
        public const string ReturnLate = "ReturnLate";
        public const string PriceByDate = "PriceByDate";
        public const string PriceByWeekDay = "PriceByWeekDay";
        public const string PriceByHours = "PriceByHours";
        /// <summary>
        /// Chủ xe chọn giờ thuê xe là 24h. Giờ bắt đầu sẽ là giờ bắt đầu thuê
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="fromDate"></param>
        /// <returns></returns>
        public static (double, double) GetRentalHourStartEnd(Vehicle_RentalSetting setting, DateTime fromDate)
        {
            double rentalHourStart, rentalHourEnd;
            if (setting.RentalHour_Using24Hours)
            {
                rentalHourStart = fromDate.Hour + (fromDate.Minute / 60.0); rentalHourEnd = rentalHourStart;
            }
            else { rentalHourStart = setting.RentalHourStart ?? 0; rentalHourEnd = setting.RentalHourEnd ?? 0; }
            return (rentalHourStart, rentalHourEnd);
        }

        public static BaseCategoryInfoItem[] GetThingsToKnow(List<ConfigSimpleInfo> cms_Detail, RentalServiceCategorySettingJsonModel settingJson, long? cfNoOfSeatId)
        {
            CancelOrderSettingModel cancelOrderSetting = settingJson.CancelOrderSetting;
            BaseCategoryInfoItem[] result = null;
            var thingsToKnow = cms_Detail.Where(c => !string.IsNullOrEmpty(c.Code) && c.Code.StartsWith("Things_To_Know")).ToArray();
            if (thingsToKnow?.Any() == true)
                result = thingsToKnow.OrderBy(c => c.OrderNo).Select(c =>
                {
                    string content = c.MoreConfig;
                    string description = c.ColorCode;
                    if (c.Code == "Things_To_Know_Cancel_Order")
                    {
                        content = content.Replace("#FullRefundWithinHours#", ConvertToOneDecimalValue(cancelOrderSetting.FullRefundWithinMinutes.Value / 60m));
                        content = content.Replace("#RefundPercent#", Math.Round(100 - settingJson.DepositPercent.Value, 0, MidpointRounding.AwayFromZero).ToString());
                        content = content.Replace("#NoRefundGreaterThanDays#", cancelOrderSetting.NoRefundGreaterThanDays.ToString());
                        content = content.Replace("#FullRefundWithinMinutes#", ConvertToOneDecimalValue(cancelOrderSetting.FullRefundWithinMinutes.Value));
                        description = description.Replace("#FullRefundWithinMinutes#", ConvertToOneDecimalValue(cancelOrderSetting.FullRefundWithinMinutes.Value));
                    }
                    else if (c.Code == "Things_To_Know_Rent_Experience")
                    {
                        var noOfSeat = CachedDataManagement.ConfigVehicleNoOfSeat_Get_Instance_Id(cfNoOfSeatId);
                        content = content.Replace("#NoOfSeat#", noOfSeat?.NumberOfSeat?.ToString());
                    }
                    BaseCategoryInfoItem t = new()
                    {
                        Code = c.Code,
                        Title = c.Name,
                        Description = description,
                        Content = content
                    };
                    return t;
                }).ToArray();
            return result;
        }

        /// <summary>
        /// Hàm chia thời gian thuê thành các đoạn: lấy xe sớm, thuê đủ ngày, trả xe trễ.
        /// Mục đích: phục vụ cho việc tính giá từng đoạn thuê.
        /// </summary>
        /// <param name="model">Thông tin đầu vào gồm ServiceInfo và RentInfo</param>
        /// <returns>Mảng các đoạn thuê (RentCarSegmentModel)</returns>
        #region GetRentalDaySegments
        public static RentCarSegmentModel[] GetRentalDaySegments(RentCarFormulaInputModel model)
        {
            RentCarSegmentModel[] result = null;
            var serviceInfo = model.ServiceInfo;
            var rentInfo = model.RentInfo;

            // Gán lại thông tin ngày giờ cho serviceInfo để đồng bộ dữ liệu
            serviceInfo.FromDate = rentInfo.FromDate;
            serviceInfo.ToDate = rentInfo.ToDate;
            serviceInfo.UI_TimezoneOffset = rentInfo.UI_TimezoneOffset;

            // Kiểm tra đầu vào hợp lệ
            if (serviceInfo != null && rentInfo != null)
            {
                var rentalServiceItem = serviceInfo.RentalServiceItem;
                if (rentalServiceItem == null || rentalServiceItem.Vehicle_RentalSetting == null)
                {
                    // Nếu thiếu cấu hình thì báo lỗi
                    throw new("Error rentalServiceItem == null || rentalServiceItem.Vehicle_RentalSetting == null");
                }
                var setting = rentalServiceItem.Vehicle_RentalSetting;

                // Kiểm tra ngày bắt đầu và kết thúc thuê hợp lệ
                if (rentInfo.FromDate != null && rentInfo.ToDate != null)
                {
                    DateTime fromDate = rentInfo.FromDate.Value, toDate = rentInfo.ToDate.Value;
                    List<RentCarSegmentModel> listDate = new();

                    // Tính giờ bắt đầu và kết thúc thuê dựa vào cấu hình
                    var rentalHour = GetRentalHourStartEnd(setting, fromDate);
                    double rentalHourStart = rentalHour.Item1, rentalHourEnd = rentalHour.Item2;

                    // Số giờ cần cộng thêm để xác định đoạn thuê đủ ngày
                    var hourToAdd = 24 + rentalHourEnd - rentalHourStart;

                    // Tính toán thời điểm thực tế lấy xe (fromC) và thời điểm chuẩn hóa theo cấu hình (fromO)
                    DateTime fromC = fromDate,
                        fromO = fromDate.Date.AddHours(rentalHourStart);

                    // Nếu thời điểm lấy xe thực tế lớn hơn thời điểm chuẩn hóa thì dịch sang ngày tiếp theo
                    if (fromC > fromO)
                    {
                        fromO = fromO.AddHours(24);
                    }

                    // Đoạn lấy xe sớm (nếu có)
                    listDate.Add(new(GetEarly, fromC, fromO, 0));

                    // Tính toán thời điểm trả xe thực tế (toC) và thời điểm chuẩn hóa theo cấu hình (toO)
                    DateTime toC = toDate,
                        toO = toDate.Date.AddHours(rentalHourEnd);

                    // Điều chỉnh lại thời điểm trả xe chuẩn hóa nếu vượt quá thực tế
                    if (toO > toC && toO.AddHours(-24) >= fromC) toO = toO.AddHours(-24);

                    // Đoạn thuê đủ ngày (có thể có nhiều ngày)
                    DateTime date;
                    for (date = fromO; date < toO; date = date.AddDays(1))
                    {
                        var nextDate = date.AddDays(1);
                        if (nextDate <= toC)
                            listDate.Add(new(FullDay, date, nextDate, 1));
                        else if (hourToAdd != 24)
                            listDate.Add(new(FullDay, date, date.AddHours(hourToAdd), 1));
                    }

                    // Xác định có cần thêm đoạn trả xe trễ không
                    bool canAddReturnLate = false;
                    if (fromO <= toO) canAddReturnLate = true;
                    else if (hourToAdd != 24)
                    {
                        /* Kiểm tra cho việc chủ xe cấu hình không đủ 24h
                         */
                        var temp = 24 - (fromO - toO).TotalHours - hourToAdd;
                        if (temp == 0) canAddReturnLate = true;
                    }
                    if (canAddReturnLate)
                        listDate.Add(new(ReturnLate, toO, toC, 0)); // Đoạn trả xe trễ (nếu có)

                    // Chỉ lấy các đoạn có thời gian hợp lệ
                    result = listDate.Where(c => c.FromDate < c.ToDate).ToArray();
                }
            }

            // Trả về mảng các đoạn thuê (lấy sớm, đủ ngày, trả trễ)
            return result;
        }
        #endregion

        /// <summary>
        /// Gắn cứng con số này, nếu lấy sớm hoặc trả trễ lớn hơn con số này thì xem như là một ngày
        /// </summary>
        public const int NumOfHourToBeOneDay = 12;

        /// <summary>
        /// Hàm tính tiền lấy xe sớm
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        #region GetEarlyHourDeliveryFee
        public static async Task<RentCarSegment_PriceModel> GetEarlyHourDeliveryFee(RentCarFormulaInputModel model, DateTime fromDate, DateTime toDate)
        {
            RentCarSegment_PriceModel result = new();
            var serviceInfo = model.ServiceInfo;
            var rentInfo = model.RentInfo;
            var settingJson = serviceInfo.SettingJson;
            if (serviceInfo != null && rentInfo != null)
            {
                var rentalServiceItem = serviceInfo.RentalServiceItem;
                if (rentalServiceItem == null || rentalServiceItem.Vehicle_RentalSetting == null)
                {
                    throw new Exception("Error rentalServiceItem == null || rentalServiceItem.Vehicle_RentalSetting == null");
                }
                var setting = rentalServiceItem.Vehicle_RentalSetting;
                if (fromDate < toDate)
                {
                    var hours = (toDate - fromDate).TotalHours;
                    // hours >= NumOfHourToBeOneDay thì xem như là một ngày luôn

                    /*
                     * Hungnm 20251002: Điều chỉnh lại logic: - Có danh sách logic
                     *  - Table: ConfigDeliveryFee - ConfigDeliveryDetailFee
                     *  - Nếu danh sách logic không hợp lý thì lấy mặc định quá 12h -> 1 ngày
                     *  - Danh sách logic sẽ gồm Operator - thời gian (from - to) - tính theo tiền mỗi giờ/tiền của 1 ngày/số tiền
                     */

                    if (hours >= NumOfHourToBeOneDay || hours > setting.NumOfEarlyHourToBeOneDay)
                    {
                        DateTime date = fromDate;

                        if (fromDate.Date != toDate.Date)
                        {
                            // 22h/19 -> 21h/20 -> Lấy ngày 20, theo khung giờ (21, 21)
                            int rentalHourStart = setting.RentalHourStart ?? settingJson.RentalHourStart.Value, rentalHourEnd = setting.RentalHourEnd ?? settingJson.RentalHourEnd.Value;
                            if (24 - rentalHourStart < rentalHourEnd || (24 - rentalHourStart == rentalHourEnd && rentalHourStart > 12)) date = date.AddDays(1);
                        }
                        else
                        {
                            // 6h/19 -> 11h/19 -> Lấy ngày 18, theo khung giờ (11, 11)
                            int rentalHourStart = setting.RentalHourStart ?? settingJson.RentalHourStart.Value, rentalHourEnd = setting.RentalHourEnd ?? settingJson.RentalHourEnd.Value;
                            if (24 - rentalHourStart > rentalHourEnd || (24 - rentalHourStart == rentalHourEnd && rentalHourStart <= 12)) date = date.AddDays(-1);
                        }
                        var dictPriceByDate = serviceInfo.DictPriceByDate;
                        dictPriceByDate ??= BuildDictPriceByDate(fromDate, toDate, await serviceInfo.PriceByDates());
                        var dictPriceByWeekDay = serviceInfo.DictPriceByWeekDay;
                        dictPriceByWeekDay ??= DictionaryHelper.BuildDictionary4FisrtItem(serviceInfo.PriceByWeekDays, c => c.Weekday.Value);
                        result.IsFullDay = true;
                        BuildPriceModel(dictPriceByWeekDay, dictPriceByDate, result, date);
                    }
                    else
                    {
                        result.Price = Convert.ToDecimal(hours) * (setting.EarlyHourDeliveryFee ?? 0) * 1000;
                        result.PriceDate = ConvertToStringDate(fromDate);
                        result.PriceInfo = PriceByHours;
                    }
                }
            }
            return result;
        }
        public static async Task<RentCarSegment_PriceModel> GetEarlyHourDeliveryFeeV2(RentCarFormulaInputModel model, DateTime fromDate, DateTime toDate)
        {
            RentCarSegment_PriceModel result = new();
            var serviceInfo = model.ServiceInfo;
            var rentInfo = model.RentInfo;
            var settingJson = serviceInfo.SettingJson;
            if (serviceInfo != null && rentInfo != null)
            {
                var rentalServiceItem = serviceInfo.RentalServiceItem;
                if (rentalServiceItem == null || rentalServiceItem.Vehicle_RentalSetting == null)
                {
                    throw new Exception("Error rentalServiceItem == null || rentalServiceItem.Vehicle_RentalSetting == null");
                }
                var setting = rentalServiceItem.Vehicle_RentalSetting;
                if (fromDate < toDate)
                {
                    var hours = (toDate - fromDate).TotalHours;
                    // hours >= NumOfHourToBeOneDay thì xem như là một ngày luôn
                    /*
                     * Hungnm 20251002: Điều chỉnh lại logic: - Có danh sách logic
                     *  - Table: ConfigDeliveryFee - ConfigDeliveryDetailFee
                     *  - Nếu danh sách logic không hợp lý thì lấy mặc định quá 12h -> 1 ngày
                     *  - Danh sách logic sẽ gồm Operator - thời gian (from - to) - tính theo tiền mỗi giờ/tiền của 1 ngày/số tiền
                     */
                    var earlyFees = DeliveryFeeHelper.GetConfigDeliveryFeeByCode(ConfigDeliveryFeeTypes.EARLY_CAR_DELIVERY_FEE);
                    var feeResult = await DeliveryFeeHelper.CalculateDeliveryFee(earlyFees, (decimal)hours, fromDate, toDate, setting, serviceInfo, true);
                    if (feeResult != null)
                    {
                        result.Price = feeResult.Price;
                        result.ServiceFee = feeResult.ServiceFee;
                        result.PriceDate = feeResult.PriceDate;
                        result.PriceInfo = feeResult.PriceInfo;
                        result.IsFullDay = feeResult.IsFullDay;
                        result.IsHalfDay = feeResult.IsHalfDay;
                    }
                    else
                    {
                        GGNotifyHelper.SendMessageGGChat_SystemReport($"Không tìm thấy cấu hình phí ship cho dịch vụ EARLY_CAR_DELIVERY_FEE. Chuyển sang dùng cấu hình mặc định (Hard code)");
                        var resultV1 = await GetEarlyHourDeliveryFee(model, fromDate, toDate);
                        if (resultV1 != null)
                        {
                            result.Price = resultV1.Price;
                            result.ServiceFee = resultV1.ServiceFee;
                            result.PriceDate = resultV1.PriceDate;
                            result.PriceInfo = resultV1.PriceInfo;
                            result.IsFullDay = resultV1.IsFullDay;
                        }
                    }
                }
            }
            return result;
        }
        #endregion

        /// <summary>
        /// Hàm tính tiền trả xe trễ
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        #region GetLateHourReturnFee
        public static async Task<RentCarSegment_PriceModel> GetLateHourReturnFee(RentCarFormulaInputModel model, DateTime fromDate, DateTime toDate)
        {
            RentCarSegment_PriceModel result = new();
            var serviceInfo = model.ServiceInfo;
            var rentInfo = model.RentInfo;
            if (serviceInfo != null && rentInfo != null)
            {
                var rentalServiceItem = serviceInfo.RentalServiceItem;
                if (rentalServiceItem == null || rentalServiceItem.Vehicle_RentalSetting == null)
                {
                    throw new Exception("Error rentalServiceItem == null || rentalServiceItem.Vehicle_RentalSetting == null");
                }
                var setting = rentalServiceItem.Vehicle_RentalSetting;
                if (fromDate < toDate)
                {
                    var hours = (toDate - fromDate).TotalHours;
                    if (hours >= NumOfHourToBeOneDay || hours > setting.NumOfLateHourToBeOneDay)
                    {
                        bool takeF = true;
                        var rentalHour = GetRentalHourStartEnd(setting, fromDate);
                        double rentalHourStart = rentalHour.Item1, rentalHourEnd = rentalHour.Item2;
                        if (24 - rentalHourStart < rentalHourEnd) takeF = false;
                        DateTime date = fromDate;
                        if (!takeF) date = date.AddDays(1);
                        var dictPriceByDate = serviceInfo.DictPriceByDate;
                        dictPriceByDate ??= BuildDictPriceByDate(fromDate, fromDate, await serviceInfo.PriceByDates());
                        var dictPriceByWeekDay = serviceInfo.DictPriceByWeekDay;
                        dictPriceByWeekDay ??= DictionaryHelper.BuildDictionary4FisrtItem(serviceInfo.PriceByWeekDays, c => c.Weekday.Value);
                        result.IsFullDay = true;
                        BuildPriceModel(dictPriceByWeekDay, dictPriceByDate, result, date);
                    }
                    else
                    {
                        result.Price = Convert.ToDecimal(hours) * (setting.LateHourReturnFee ?? 0) * 1000;
                        result.PriceDate = ConvertToStringDate(toDate);
                        result.PriceInfo = PriceByHours;
                    }
                }
            }
            return result;
        }
        public static async Task<RentCarSegment_PriceModel> GetLateHourReturnFeeV2(RentCarFormulaInputModel model, DateTime fromDate, DateTime toDate)
        {
            RentCarSegment_PriceModel result = new();
            var serviceInfo = model.ServiceInfo;
            var rentInfo = model.RentInfo;
            if (serviceInfo != null && rentInfo != null)
            {
                var rentalServiceItem = serviceInfo.RentalServiceItem;
                if (rentalServiceItem == null || rentalServiceItem.Vehicle_RentalSetting == null)
                {
                    throw new Exception("Error rentalServiceItem == null || rentalServiceItem.Vehicle_RentalSetting == null");
                }
                var setting = rentalServiceItem.Vehicle_RentalSetting;
                if (fromDate < toDate)
                {
                    var hours = (toDate - fromDate).TotalHours;
                    var earlyFees = DeliveryFeeHelper.GetConfigDeliveryFeeByCode(ConfigDeliveryFeeTypes.LATE_CAR_DELIVERY_FEE);
                    var feeResult = await DeliveryFeeHelper.CalculateDeliveryFee(earlyFees, (decimal)hours, fromDate, toDate, setting, serviceInfo, false);
                    if (feeResult != null)
                    {
                        result.Price = feeResult.Price;
                        result.ServiceFee = feeResult.ServiceFee;
                        result.PriceDate = feeResult.PriceDate;
                        result.PriceInfo = feeResult.PriceInfo;
                        result.IsFullDay = feeResult.IsFullDay;
                        result.IsHalfDay = feeResult.IsHalfDay;
                    }
                    else
                    {
                        GGNotifyHelper.SendMessageGGChat_SystemReport($"Không tìm thấy cấu hình phí ship cho dịch vụ LATE_CAR_DELIVERY_FEE. Chuyển sang dùng cấu hình mặc định (Hard code)");
                        var resultV1 = await GetLateHourReturnFee(model, fromDate, toDate);
                        if (resultV1 != null)
                        {
                            result.Price = resultV1.Price;
                            result.ServiceFee = resultV1.ServiceFee;
                            result.PriceDate = resultV1.PriceDate;
                            result.PriceInfo = resultV1.PriceInfo;
                            result.IsFullDay = resultV1.IsFullDay;
                            result.IsHalfDay = resultV1.IsHalfDay;
                        }
                    }
                }
            }
            return result;
        }
        #endregion
        public static (decimal?, decimal?) GetPriceByDateWithServiceFeePercentOnByDate(decimal? priceByDate)
        {
            // Lấy % phí dịch vụ, cộng vào thêm trước khi trả về
            decimal? result = priceByDate;
            decimal? serviceFeePercentOnByDate = null;
            if (priceByDate != null)
            {
                var settingJson = GetSettingJson();
                if (settingJson != null && settingJson.ServiceFeePercentOnByDate != null)
                {
                    serviceFeePercentOnByDate = priceByDate * settingJson.ServiceFeePercentOnByDate / 100;
                    result = priceByDate + (serviceFeePercentOnByDate ?? 0);
                }
                else
                {
                    GGNotifyHelper.SendMessageGGChat_SystemReport($"Chưa cấu hình phí dịch vụ ServiceFeePercentOnByDate");
                }
            }
            return (result, serviceFeePercentOnByDate);
        }
        /// <summary>
        /// Hàm lấy tổng giá thuê gốc và số ngày thuê
        /// </summary>
        /// <param name="dictPriceByWeekDay"></param>
        /// <param name="dictPriceByDate"></param>
        /// <param name="m"></param>
        /// <param name="date"></param>
        #region GetOriginalPrice
        public static void BuildPriceModel(Dictionary<int, ServiceItem_WeekdayRentalPriceInfo> dictPriceByWeekDay, Dictionary<string, decimal?> dictPriceByDate, RentCarSegment_PriceModel m, DateTime? date, decimal days = 1)
        {
            string sDate = ConvertToStringDate(date);
            decimal? priceByWeekDay = dictPriceByWeekDay.GetValue_Dic((int)date.Value.DayOfWeek)?.RentalPrice, priceByDate = dictPriceByDate.GetValue_Dic(sDate);
            if (priceByDate != null)
            {
                (var price, var serviceFee) = GetPriceByDateWithServiceFeePercentOnByDate(priceByDate * days);
                m.Price = price;
                m.ServiceFee = serviceFee;
                m.PriceInfo = PriceByDate;
            }
            else
            {
                (var price, var serviceFee) = GetPriceByDateWithServiceFeePercentOnByDate(priceByWeekDay * days);
                m.Price = price;
                m.ServiceFee = serviceFee;
                m.PriceInfo = $"{PriceByWeekDay} - {date.Value.DayOfWeek}";
            }
            m.PriceDate = sDate;
        }

        public static async Task<RentCarOriginalPriceModel> GetOriginalPrice(RentCarFormulaInputModel model)
        {
            //Chia thời gian thuê thành các đoạn (lấy xe sớm, thuê đủ ngày, trả xe trễ) và tính giá từng đoạn.
            RentCarOriginalPriceModel result = null;

            // Bước 1: Chia thời gian thuê thành các đoạn (lấy xe sớm, thuê đủ ngày, trả xe trễ)
            var segments = GetRentalDaySegments(model);

            // Nếu có các đoạn thuê hợp lệ
            if (segments != null && segments.Any())
            {
                var serviceInfo = model.ServiceInfo;
                var rentInfo = model.RentInfo;
                var rentalServiceItem = serviceInfo.RentalServiceItem;
                var setting = rentalServiceItem.Vehicle_RentalSetting;
                result = new();
                DateTime? fromDate = rentInfo.FromDate, toDate = rentInfo.ToDate;

                // Bước 2: Chuẩn bị dữ liệu giá theo ngày và theo thứ trong tuần
                List<RentCarSegment_PriceModel> listPrice = new();
                Dictionary<string, decimal?> dictPriceByDate = null;
                if (serviceInfo.DictPriceByDate == null)
                {
                    // Lấy giá theo từng ngày cụ thể (nếu có cấu hình)
                    dictPriceByDate = BuildDictPriceByDate(fromDate, toDate, await serviceInfo.PriceByDates());
                    serviceInfo.DictPriceByDate = dictPriceByDate;
                }
                else dictPriceByDate = serviceInfo.DictPriceByDate;
                Dictionary<int, ServiceItem_WeekdayRentalPriceInfo> dictPriceByWeekDay = null;
                if (serviceInfo.DictPriceByWeekDay == null)
                {
                    // Lấy giá theo thứ trong tuần (nếu có cấu hình)
                    dictPriceByWeekDay = DictionaryHelper.BuildDictionary4FisrtItem(serviceInfo.PriceByWeekDays, c => (int)c.Weekday);
                    serviceInfo.DictPriceByWeekDay = dictPriceByWeekDay;
                }
                else dictPriceByWeekDay = serviceInfo.DictPriceByWeekDay;

                // Bước 3: Xác định các đoạn thuê: lấy sớm, đủ ngày, trả trễ
                var early = segments.FirstOrDefault(c => c.Code == GetEarly);
                var fullDays = segments.Where(c => c.Code == FullDay).ToArray();
                var late = segments.FirstOrDefault(c => c.Code == ReturnLate);
                int rentalDayCount = 0;
                bool hasOneDayEarly = false, hasFullDay = false, hasOneDayLate = false;
                bool hasHalfDayEarly = false, hasHalfDayLate = false;
                #region Tính giá cho từng đoạn thuê
                // Tính giá cho đoạn lấy xe sớm
                if (early != null)
                {
                    RentCarSegment_PriceModel m = new();
                    EzyObjectHelper.CopyProperties(early, m);
                    fromDate = early.FromDate;
                    toDate = early.ToDate;
                    // Nếu lấy xe sớm vượt quá số giờ quy định thì tính như một ngày
                    var info = await GetEarlyHourDeliveryFeeV2(model, fromDate.Value, toDate.Value);
                    m.Price = info.Price;
                    m.ServiceFee = info.ServiceFee;
                    m.PriceDate = info.PriceDate;
                    m.PriceInfo = info.PriceInfo;
                    hasOneDayEarly = info.IsFullDay;
                    hasHalfDayEarly = info.IsHalfDay;
                    m.IsFullDay = hasOneDayEarly;
                    m.IsHalfDay = hasHalfDayEarly;
                    if (hasOneDayEarly) rentalDayCount++;
                    listPrice.Add(m);
                }
                // Tính giá cho các đoạn thuê đủ ngày
                if (fullDays != null && fullDays.Any())
                {
                    bool takeF = false;
                    var rentalHour = GetRentalHourStartEnd(setting, fromDate.Value);
                    double rentalHourStart = rentalHour.Item1, rentalHourEnd = rentalHour.Item2;
                    if (24 - rentalHourStart >= rentalHourEnd) takeF = true;
                    foreach (var full in fullDays)
                    {
                        RentCarSegment_PriceModel m = new();
                        EzyObjectHelper.CopyProperties(full, m);
                        DateTime? date = null;
                        if (takeF) date = full.FromDate;
                        else date = full.ToDate;
                        // Tính giá theo ngày hoặc theo thứ trong tuần
                        BuildPriceModel(dictPriceByWeekDay, dictPriceByDate, m, date);
                        rentalDayCount += (full.NumOfFullDay ?? 0);
                        m.IsFullDay = true;
                        listPrice.Add(m);
                    }
                    hasFullDay = true;
                }
                // Tính giá cho đoạn trả xe trễ
                if (late != null)
                {
                    RentCarSegment_PriceModel m = new();
                    EzyObjectHelper.CopyProperties(late, m);
                    fromDate = late.FromDate;
                    toDate = late.ToDate;
                    var info = await GetLateHourReturnFeeV2(model, fromDate.Value, toDate.Value);
                    m.Price = info.Price;
                    m.ServiceFee = info.ServiceFee;
                    m.PriceDate = info.PriceDate;
                    m.PriceInfo = info.PriceInfo;
                    hasOneDayLate = info.IsFullDay;
                    hasHalfDayLate = info.IsHalfDay;
                    m.IsFullDay = hasOneDayLate;
                    m.IsHalfDay = hasHalfDayLate;
                    if (hasOneDayLate) rentalDayCount++;
                    listPrice.Add(m);
                }
                #endregion
                if (hasHalfDayLate && hasHalfDayEarly)
                {
                    rentalDayCount += 1;
                }
                // Nếu không có ngày thuê nào hợp lệ thì mặc định là 1 ngày
                if (rentalDayCount == 0) rentalDayCount = 1;

                // Bước 4: Tổng hợp giá gốc và số ngày thuê
                result.TotalOriginalPrice = listPrice.Sum(c => (c.Price ?? 0));
                result.RentalDayCount = rentalDayCount;
                result.OriginalPriceByDay = result.TotalOriginalPrice / rentalDayCount;
                result.Segments = listPrice.ToArray();
                result.ServiceFee = listPrice.Where(t => t.IsFullDay || t.IsHalfDay).Sum(c => (c.ServiceFee ?? 0));
                #region Xử lý cho vấn đề không đủ 1 ngày thuê (chỉ có một đoạn nhỏ)
                if (!hasOneDayEarly && !hasFullDay && !hasOneDayLate)
                {
                    RentCarSegment_PriceModel m = new();
                    BuildPriceModel(dictPriceByWeekDay, dictPriceByDate, m, fromDate);
                    result.TotalOriginalPrice = m.Price;
                    result.OriginalPriceByDay = m.Price;
                    var modelPriceFullDay = listPrice.LastOrDefault();
                    modelPriceFullDay.Price = m.Price;
                    modelPriceFullDay.IsFullDay = true;
                    modelPriceFullDay.IsHalfDay = false;
                    result.Segments = new[] { modelPriceFullDay };
                    result.ServiceFee = m.ServiceFee;
                }
                #endregion
            }
            // Trả về kết quả gồm tổng giá gốc, giá trung bình mỗi ngày, các đoạn thuê và giá từng đoạn
            return result;
        }
        #endregion

        /// <summary>
        /// Tính giá tiền sau khi giảm giá
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        #region GetPriceByDay
        public static async Task<RentCarPriceByDayModel> GetPriceByDay(RentCarFormulaInputModel model)
        {
            // Khởi tạo biến kết quả
            RentCarPriceByDayModel result = null;

            // Bước 1: Tính giá thuê gốc (chưa giảm giá) và các đoạn thuê (lấy sớm, đủ ngày, trả trễ)
            var originalPrice = await GetOriginalPrice(model);
            if (originalPrice != null)
            {
                // Khởi tạo đối tượng kết quả, copy thông tin từ giá gốc
                result = new();
                EzyObjectHelper.CopyProperties(originalPrice, result);
                result.Segments = originalPrice.Segments;
                result.ServiceFee = originalPrice.ServiceFee;
                var serviceInfo = model.ServiceInfo;
                decimal TotalOriginalPrice = originalPrice.TotalOriginalPrice ?? 0,
                    totalPromotionMoney = 0;
                var rentalDayCount = originalPrice.RentalDayCount;

                // Bước 2: Kiểm tra và áp dụng giảm giá khi thuê nhiều ngày (nếu có)
                if (serviceInfo.RentalServiceItem.Vehicle_RentalSetting.HaveMultidayRentalDiscount == true)
                {
                    var multiRentDayDiscounts = serviceInfo.MultiRentDayDiscounts;

                    // Nếu cấu hình yêu cầu bỏ qua giảm giá khi có yêu cầu thuê tối thiểu
                    if (serviceInfo.SettingJson.IgnoreMultidayRentalDiscount_InMinimumRentalDayRequired)
                        if (serviceInfo.MinimumRentalDayRequireds != null && serviceInfo.MinimumRentalDayRequireds.Length > 0)
                        {
                            DateTime fromDate = model.RentInfo.FromDate.Value.Date, toDate = model.RentInfo.ToDate.Value.Date;
                            serviceInfo.MinimumRentalDayRequireds = serviceInfo.MinimumRentalDayRequireds.Where(c =>
                            (c.FromDate <= fromDate && fromDate <= c.ToDate) ||
                            (c.FromDate <= toDate && toDate <= c.ToDate) ||
                            (fromDate <= c.FromDate && c.ToDate <= toDate)).ToArray();
                            int reqRentalDayCount = 0;
                            if (serviceInfo.MinimumRentalDayRequireds.Any())
                                reqRentalDayCount = serviceInfo.MinimumRentalDayRequireds.Max(c => (c.MinimumRequiredRentalDays ?? 0));
                            if (reqRentalDayCount > 0)
                                multiRentDayDiscounts = null;
                        }

                    // Áp dụng giảm giá nhiều ngày nếu đủ điều kiện
                    if (multiRentDayDiscounts != null && multiRentDayDiscounts.Length > 0)
                    {
                        decimal discountPercent = 0;
                        int numOfMinDay = 0;
                        foreach (var item in multiRentDayDiscounts)
                        {
                            var cf = CachedDataManagement.ConfigVehicleMultidayRentalDiscount_Get_Instance_Id(item.MultidayRentalDiscountId);
                            if (cf != null && !cf.IsDisable && cf.NumOfMinDay <= rentalDayCount)
                            {
                                // Chọn mức giảm giá cao nhất phù hợp
                                if (discountPercent < item.DiscountPercent)
                                {
                                    numOfMinDay = cf.NumOfMinDay ?? 0;
                                    discountPercent = item.DiscountPercent ?? 0;
                                }
                            }
                        }
                        if (discountPercent > 0)
                        {
                            // Ghi chú về giảm giá áp dụng
                            result.PromotionNote = $"NumOfMinDay {numOfMinDay}. Discount {FormatHelper.FormatString_Percent(discountPercent)}";
                            result.PromotionNumOfMinDay = numOfMinDay;
                            totalPromotionMoney = (TotalOriginalPrice * discountPercent) / 100;
                        }
                    }
                }
                bool isDefault = true;

                // Bước 3: Kiểm tra và áp dụng giảm giá từ mã voucher khi tìm kiếm (nếu có)
                if (model.RentInfo.ShowDiscountInSearch)
                {
                    if (serviceInfo.DiscountCodes?.Length > 0 && string.IsNullOrEmpty(model.RentInfo.VoucherCode))
                    {
                        // Nếu có mã giảm giá mặc định và chưa nhập voucher, áp dụng giảm giá này
                        decimal discountMoney = GetDiscountMoney(TotalOriginalPrice - totalPromotionMoney, serviceInfo.DiscountCodes.FirstOrDefault().Code);
                        result.SubTotal = TotalOriginalPrice - totalPromotionMoney - discountMoney;
                        result.TotalPromotionMoney = totalPromotionMoney + discountMoney;
                        result.PriceByDay = result.SubTotal / rentalDayCount;
                        result.PromotionMoneyByDay = (totalPromotionMoney + discountMoney) / rentalDayCount;
                        isDefault = false;
                    }
                }

                // Bước 4: Nếu không có giảm giá đặc biệt, chỉ áp dụng giảm giá nhiều ngày (nếu có)
                if (isDefault)
                {
                    result.SubTotal = TotalOriginalPrice - totalPromotionMoney;
                    result.TotalPromotionMoney = totalPromotionMoney;
                    result.PriceByDay = result.SubTotal / rentalDayCount;
                    result.PromotionMoneyByDay = totalPromotionMoney / rentalDayCount;
                }
            }
            // Trả về kết quả gồm giá sau giảm giá, giá trung bình mỗi ngày, ghi chú giảm giá, v.v.
            return result;
        }
        #endregion

        #region GetTotalPrice
        private static async Task<(decimal? deliveryFee, decimal distance, string error)> GetDeliveryFee(RentCarFormulaInputModel model)
        {
            string sMessage = string.Empty;
            decimal? deliveryFee = null;
            decimal distance = 0;
            var param = model.RentInfo;
            var rentalServiceItem = model.ServiceInfo.RentalServiceItem;
            var vehicle_RentalSetting = rentalServiceItem.Vehicle_RentalSetting;
            if (vehicle_RentalSetting != null)
            {
                HostAddress address = rentalServiceItem.HostAddress;
                if (vehicle_RentalSetting.HaveDeliverySurcharge != true)
                {
                    if (address != null && param.Latitude != null && param.Longitude != null
                        && (address.Latitude != param.Latitude || address.Longitude != param.Longitude))
                        sMessage = "Dịch vụ không hỗ trợ giao xe tận nơi";
                }
                if (string.IsNullOrEmpty(sMessage))
                {
                    if (!string.IsNullOrEmpty(param.Address))
                    {
                        if (param.Latitude == null || param.Longitude == null) sMessage = "Thông tin điểm giao nhận xe không có. Vui lòng liên hệ admin để được hỗ trợ";
                        if (string.IsNullOrEmpty(sMessage))
                        {
                            // Địa chỉ phải khác nhau thì mới tính
                            if (address != null && (address.Latitude != param.Latitude || address.Longitude != param.Longitude))
                            {
                                if (string.IsNullOrEmpty(sMessage))
                                {
                                    var cached = model.ServiceInfo.DictSearching_Distance?.GetValue_Dic($"{address.Latitude}_{address.Longitude}");
                                    DistanceItemResultModel d;
                                    if (cached == null)
                                        d = (await DistanceMatrixHelper.GetDistance_Cached(param.Latitude, param.Longitude, address.Latitude, address.Longitude)).data;
                                    else d = new() { Distance = cached.Distance, Lat = cached.Lat, Lng = cached.Lng };
                                    if (string.IsNullOrEmpty(sMessage) && d != null)
                                    {
                                        distance = d.Distance ?? 0;
                                        if (distance > 0)
                                        {
                                            var setting = rentalServiceItem.Vehicle_RentalSetting;
                                            if (setting != null)
                                            {
                                                decimal maximumDeliveryMileage = (setting.MaximumDeliveryMileage ?? 0) * 1000,
                                                    freeDeliveryMileage = (setting.FreeDeliveryMileage ?? 0) * 1000;
                                                if (distance > maximumDeliveryMileage)
                                                {
                                                    sMessage = TextDisplayHelper.GetValue(TextDisplayKeys.msg_error_delivery_distance_exceed_maximum, "Điểm giao nhận xe vượt quá #Distance#. Vui lòng chọn địa chỉ khác");
                                                    sMessage = sMessage.Replace("#Distance#", ConvertMToKM(maximumDeliveryMileage));
                                                }
                                                else
                                                {
                                                    if (distance > freeDeliveryMileage) // Không trừ số km miễn phí nữa
                                                        deliveryFee = distance * (setting.DeliverySurcharge ?? 0);
                                                    else deliveryFee = 0;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else deliveryFee = 0;
                        }
                    }
                }
            }
            return (Math.Round(deliveryFee ?? 0, 0, MidpointRounding.AwayFromZero), distance, sMessage);
        }
        private static decimal GetInsuranceFee(RentCarFormulaInputModel model, bool isBooking, decimal totalPrice, out long? insuranceCompanyId, out string ownerInsuranceContractNumber, out string sMessage)
        {
            #region Log method version
            MethodBase.GetCurrentMethod().LogMethodVersion("20240515-0919", "Cập nhật logic, IsIgnoreInsuranceValidCheck = true thì bảo hiểm cấu hình lỗi, sẽ lấy của Sigo", "https://sigo-tasks.allianceitsc.com/task/SIGO-41");
            #endregion Log method version

            sMessage = string.Empty;
            decimal result = 0;
            insuranceCompanyId = null;
            ownerInsuranceContractNumber = null;
            var rentalServiceItem = model.ServiceInfo.RentalServiceItem;
            var rentInfo = model.RentInfo;
            var setting = rentalServiceItem.Vehicle_RentalSetting;
            bool needGetInsurance = false, needLogLogic = false;
            Vehicle_InsuranceInformationInfo vehicle_InsuranceInformation = null;
            var ui_TimezoneOffset = rentInfo.UI_TimezoneOffset.Value;
            string message = null;
            /* Có bảo hiểm thì phải tính xem ngày hết hạn
             */
            if (setting.HaveInsurance)
            {
                DateTime fromDateUTC = rentInfo.FromDate.Value.AddMinutes(ui_TimezoneOffset),
                toDateUTC = rentInfo.ToDate.Value.AddMinutes(ui_TimezoneOffset);
                var vehicleId = rentalServiceItem.CarId;
                vehicle_InsuranceInformation = CachedDataManagement.Vehicle_InsuranceInformation_Get_Instance_VehicleId(vehicleId);
                if (vehicle_InsuranceInformation != null &&
                    vehicle_InsuranceInformation.CoverageStartDate != null &&
                    vehicle_InsuranceInformation.CoverageEndDate != null &&
                    vehicle_InsuranceInformation.IsVerified == true &&
                    vehicle_InsuranceInformation.CoverageStartDate <= vehicle_InsuranceInformation.CoverageEndDate)
                {
                    DateTime startDate = vehicle_InsuranceInformation.CoverageStartDate.Value.ToUniversalTime(),
                        endDate = vehicle_InsuranceInformation.CoverageEndDate.Value.ToUniversalTime().AddDays(1).AddSeconds(-1);
                    if (startDate <= fromDateUTC && toDateUTC <= endDate)
                    {
                        insuranceCompanyId = vehicle_InsuranceInformation.InsuranceProviderId;
                        ownerInsuranceContractNumber = vehicle_InsuranceInformation.PolicyNumber;
                    }
                    else
                    {
                        if (endDate >= DateTime.UtcNow)
                        {
                            sMessage = TextDisplayHelper.GetValue(TextDisplayKeys.msg_error_car_insurance_expired, "Ngày thuê không có bảo hiểm. Vui lòng thuê trong khoảng #StartDate# đến #EndDate#");
                            sMessage = sMessage.Replace("#StartDate#", startDate.AddMinutes(-ui_TimezoneOffset).ToString("dd/MM/yyyy")).Replace("#EndDate#", endDate.AddMinutes(-ui_TimezoneOffset).ToString("dd/MM/yyyy"));
                        }
                        else sMessage = "Bảo hiểm của xe đã hết hạn";
                    }
                }
                else sMessage = "Cấu hình bảo hiểm của xe đang không đúng. Vui lòng chọn xe khác để thuê";
                if (!string.IsNullOrEmpty(sMessage))
                {
                    if (model.ServiceInfo.SettingJson.IsIgnoreInsuranceValidCheck)
                    {
                        needGetInsurance = true;
                        if (isBooking)
                        {
                            needLogLogic = true;
                            message = $"Chuyển sang lấy phí bảo hiểm mặc định. {sMessage}";
                        }
                        sMessage = string.Empty;

                    }
                    else
                    {
                        needLogLogic = true;
                        message = sMessage;
                    }
                }
            }
            else needGetInsurance = true;
            if (needGetInsurance)
            {
                decimal? insurancePercentPerDay = null;
                var vehicle = rentalServiceItem.Vehicle;
                var cfVehicle = CachedDataManagement.ConfigVehicleType_Get_Instance_Id(vehicle.VehicleTypeId);
                if (cfVehicle != null)
                {
                    var cfCompany = CachedDataManagement.ConfigVehicleInsuranceCompanys.FirstOrDefault(c => c.VehicleTypeId == cfVehicle.Id && c.IsDefault);
                    if (cfCompany != null && cfCompany.InsurancePercentPerDay > 0)
                    {
                        insurancePercentPerDay = cfCompany.InsurancePercentPerDay;
                        insuranceCompanyId = cfCompany.Id;
                    }
                }
                insurancePercentPerDay ??= (model.ServiceInfo.SettingJson.InsurancePercentPerDay ?? 0);
                result = totalPrice * (insurancePercentPerDay.Value / 100m);
            }
            if (needLogLogic)
            {
                GGNotifyHelper.SendMessageGGChat_SystemReport($"Có lỗi xảy khi lấy phí bảo hiểm: {message}. Xe có slug {rentalServiceItem.Slug}");
                var error = new LogLogicErrorModel()
                {
                    ClientData = JsonHelper.SerializeObject(new
                    {
                        RentInfo = new { model.ServiceInfo.SettingJson.IsIgnoreInsuranceValidCheck, FromDate = model.RentInfo.FromDate.Value.ToString("yyyy/MM/dd HH:mm"), ToDate = model.RentInfo.ToDate.Value.ToString("yyyy/MM/dd HH:mm"), model.RentInfo.Url, model.RentInfo.IP },
                        Insurance = vehicle_InsuranceInformation
                    }),
                    MainEntityId = rentalServiceItem.Id.ToString(),
                    MainEntityType = "RentalServiceItem",
                    Message = message,
                    Method = "GetInsuranceFee",
                    StartAt = DateTime.UtcNow
                };
                LogLogicHelper.LogError(error, "GetInsuranceFee", "System", null, null);
            }
            return result;
        }
        /// <summary>
        /// Gọi Vifo /v2/insurance/total-price để lấy phí bảo hiểm thực.
        /// Trả false nếu xe thiếu thông tin bắt buộc hoặc Vifo lỗi.
        /// Không gọi API và không gửi GGChat nếu xe thiếu thông tin.
        /// </summary>
        private static bool TryGetVifoTotalPrice(RentCarFormulaInputModel model, VifoConfig vifoConfig, out decimal price, out string error)
        {
            price = 0;
            error = null;
            var rentalServiceItem = model.ServiceInfo.RentalServiceItem;
            var vehicle = rentalServiceItem.Vehicle;
            var rentInfo = model.RentInfo;

            if (string.IsNullOrEmpty(vehicle.PlateNumber) || vehicle.YearModel == null || vehicle.VehicleNoOfSeatId == null)
                return false;

            if (rentInfo.UI_TimezoneOffset == null || rentInfo.FromDate == null || rentInfo.ToDate == null)
                return false;

            if (!InsuranceHelper.HasProvider("VIFO"))
                InsuranceHelper.RegisterVifo(vifoConfig);

            var ui_TimezoneOffset = rentInfo.UI_TimezoneOffset.Value;
            var now = DateTime.UtcNow.AddMinutes(ui_TimezoneOffset).Date;
            DateTime fromDate = rentInfo.FromDate.Value.AddMinutes(ui_TimezoneOffset).Date,
                     toDate   = rentInfo.ToDate.Value.AddMinutes(ui_TimezoneOffset).Date;
            if (fromDate < now) fromDate = now;

            var brand         = CachedDataManagement.ConfigVehicleMake_Get_Instance_Id(vehicle.VehicleMakeId);
            var carModel      = CachedDataManagement.ConfigVehicleModel_Get_Instance_Id(vehicle.VehicleModelId);
            var numberOfSeats = CachedDataManagement.ConfigVehicleNoOfSeat_Get_Instance_Id(vehicle.VehicleNoOfSeatId);

            var priceResult = InsuranceHelper.GetTotalPrice(new InsurancePriceRequest
            {
                ProviderCode = "VIFO",
                FamilyCode   = vifoConfig.ProductFamilyCode,
                Payload      = new VifoCarShortPayload
                {
                    Phone        = "",
                    Fullname     = "",
                    Email        = vifoConfig.Email,
                    FamilyCode   = vifoConfig.ProductFamilyCode,
                    ProviderCode = vifoConfig.ProductProviderCode,
                    StartDate    = fromDate.ToString("yyyy-MM-dd"),
                    EndDate      = toDate.ToString("yyyy-MM-dd"),
                    PlateNo      = vehicle.PlateNumber,
                    Year         = vehicle.YearModel,
                    Brand        = brand?.Name,
                    Model        = carModel?.Name,
                    Seat         = numberOfSeats?.NumberOfSeat
                }
            });

            if (priceResult.IsSuccessful)
            {
                price = priceResult.FinalAmount;
                return true;
            }

            error = priceResult.ErrorMessage;
            return false;
        }
        private static decimal GetDiscountMoney(decimal? TotalOriginalPrice, string voucherCode)
        {
            decimal result = 0;
            var discountCode = CachedDataManagement.DiscountCode_Get_Instance_Code(voucherCode);
            if (discountCode != null)
            {
                if (discountCode.DiscountMethodCode == DiscountTypeCodes.Percent)
                {
                    if (discountCode.DiscountPercent != null)
                        result = (TotalOriginalPrice * discountCode.DiscountPercent / 100).Value;
                    if (discountCode.MaximumDiscountMoney > 0)
                        result = Math.Min(result, discountCode.MaximumDiscountMoney.Value);
                }
                else if (discountCode.DiscountMethodCode == DiscountTypeCodes.Money)
                {
                    if (discountCode.DiscountMoney != null)
                        result = discountCode.DiscountMoney.Value;
                    if (result > TotalOriginalPrice)
                        result = TotalOriginalPrice.Value;
                }
            }
            return result;
        }
        public static async Task<RentalServiceTotalPriceModel> GetTotalPriceAsync(RentCarFormulaInputModel model)
        {
            // Khởi tạo biến kết quả
            RentalServiceTotalPriceModel result = null;

            // Bước 1: Tính giá thuê và phí giao xe song song (không phụ thuộc nhau)
            var priceTask = GetPriceByDay(model);
            var deliveryTask = GetDeliveryFee(model);
            await Task.WhenAll(priceTask, deliveryTask);

            var price = priceTask.Result;
            var (deliveryFee, distance, error_1) = deliveryTask.Result;

            if (price != null)
            {
                var rentInfo = model.RentInfo;
                var settingJson = model.ServiceInfo.SettingJson;

                // Bước 2: Tính tiền giảm giá từ mã voucher (nếu có)
                decimal discountMoney = GetDiscountMoney(price.SubTotal, rentInfo.VoucherCode),
                    //, serviceFee = settingJson.ServiceFee ?? 0;
                    serviceFee = price.ServiceFee ?? 0;

                // Bước 3: Khởi tạo đối tượng kết quả, lưu các thông tin giá cơ bản
                result = new()
                {
                    RentalDayCount = price.RentalDayCount,              // Số ngày thuê
                    TotalOriginalPrice = price.TotalOriginalPrice,      // Tổng giá gốc chưa giảm
                    TotalPromotionMoney = price.TotalPromotionMoney,    // Tổng tiền giảm giá
                    PromotionNumOfMinDay = price.PromotionNumOfMinDay,  // Số ngày tối thiểu để được giảm giá
                    ServiceFee = serviceFee,                            // Phí dịch vụ
                    PriceByDay = price.PriceByDay,                      // Giá trung bình mỗi ngày
                    Segments = price.Segments,                          // Các đoạn thuê (lấy sớm, đủ ngày, trả trễ)
                    SubTotal = price.SubTotal,                          // Giá sau giảm giá
                    DiscountMoney = discountMoney                       // Tiền giảm giá từ voucher
                };
                decimal subTotal = price.SubTotal.Value;

                // Bước 4: Áp dụng kết quả phí giao xe (đã tính song song ở trên)

                if (string.IsNullOrEmpty(error_1))
                {
                    /* Logic chỗ này: Nếu là từ đơn nhanh, và là tự đến lấy, thì không tính phí giao xe, chỉ lấy khoảng cách
                     */
                    if (rentInfo.IsFromQuickOrder && !rentInfo.IsHaveDeliverySurcharge)
                    {
                        deliveryFee = 0;
                    }
                    else result.DeliveryFee = deliveryFee;
                    result.DeliveryDistance = distance;
                }
                else result.IsHostAddress = true;

                // Bước 5: Tính phí bảo hiểm cho đơn thuê
                decimal totalInsuranceFee = GetInsuranceFee(model, model.RentInfo.IsBooking, subTotal, out long? insuranceCompanyId, out string ownerInsuranceContractNumber, out string error_2);

                if (string.IsNullOrEmpty(error_2))
                {
                    result.TotalInsuranceFee = totalInsuranceFee;
                    result.InsuranceFeeByDay = totalInsuranceFee / price.RentalDayCount;
                    result.InsuranceCompanyId = insuranceCompanyId;
                    result.OwnerInsuranceContractNumber = ownerInsuranceContractNumber;
                }

                // Bước 6: Tổng hợp các khoản phí để ra tổng tiền cuối cùng
                result.TotalPrice = subTotal + totalInsuranceFee + (deliveryFee ?? 0) - discountMoney; // + serviceFee -> Hungnm đã cộng phí dịch vụ vào tiền tổng rồi
                result.TotalPriceByDay = result.TotalPrice / price.RentalDayCount;

                // Ghi nhận lỗi nếu có
                result.Error = (error_1 + " - " + error_2).Trim(' ', '-');

                // Bước 7: Tính điểm ưu tiên (criteria point) nếu cần cho việc sắp xếp dịch vụ
                if (rentInfo.NeedGetCriteriaPoint)
                {
                    if (model.ServiceInfo.SettingJson.ApplySortByPriorityPoint == true)
                    {
                        if (model.ServiceInfo.OwnerCancelOrderRatios == null)
                        {
                            using (var repo = SQLDataContextHelper.CreateRepository<OwnerCancelOrderRatio>(""))
                            {
                                var oldDate = DateTime.UtcNow.Date;
                                while (oldDate.DayOfWeek != DayOfWeek.Monday)
                                    oldDate = oldDate.AddDays(-1);
                                model.ServiceInfo.OwnerCancelOrderRatios = (await repo.GetQueryableReadOnly().Where(c => c.CreatedAtUTC >= oldDate).OrderByDescending(c => c.CreatedAtUTC).ToArrayAsync()) ?? Array.Empty<OwnerCancelOrderRatio>();
                            }
                        }
                        result.CriteriaPriorityPoint = GetCriteriaPriority(model, result, out List<RentalServiceBookingPriceModel> details);
                        result.CriteriaPriorityPointDetails = details;
                    }
                }
            }
            // Trả về kết quả cuối cùng
            return result;
        }
        #endregion

        #region ReturnDepositAmount
        /// <summary>
        /// Tính toán tiền hoàn cọc. Time truyền vào phải là UTC
        /// </summary>
        /// <param name="subTotal"></param>
        /// <param name="depositDoneAt"></param>
        /// <param name="cancelBy">Nhận 2 giá trị: Renter và Owner</param>
        /// <param name="startDate"></param>
        /// <returns></returns>
        public static ReturnDepositModel CalcRerturnDepositAmount(RentalServiceCategorySettingJsonModel setting, decimal? subTotal, decimal? depositAmount, DateTime? depositDoneAt, DateTime? startDate, string cancelBy)
        {
            ReturnDepositModel result = new();
            if (subTotal > 0 && depositDoneAt != null && startDate != null)
            {
                var cancelOrderSetting = setting.CancelOrderSetting;
                var now = DateTime.UtcNow;
                decimal renterReceive = 0, ownerReceive = 0, serviceReceive = 0;
                var diffDeposit = (now - depositDoneAt).Value.TotalMinutes;
                if (diffDeposit <= cancelOrderSetting.FullRefundWithinMinutes)
                {
                    renterReceive = depositAmount.Value;
                }
                else if (diffDeposit > cancelOrderSetting.FullRefundWithinMinutes)
                {
                    var diffRental = (startDate - now).Value.TotalDays;

                    #region Trường hợp hủy > NoRefundGreaterThanDays
                    // Khách hủy thì mất % cọc, chủ xe nhận % cọc
                    if (diffRental > cancelOrderSetting.NoRefundGreaterThanDays)
                    {
                        decimal depositPercent = (setting.DepositPercent ?? 30) / 100m,
                            penaltyPercent = (setting.PenaltyDepositPercent ?? 30) / 100m,
                        refundPercent = (1 - penaltyPercent),
                        servicePercent = setting.CompletionFeePercentage.Value / 100m,
                        renterPercent = penaltyPercent - servicePercent;
                        if (servicePercent > 0 && renterPercent > 0)
                        {
                            if (setting.DepositAmount > 0)
                            {
                                #region Cọc dựa trên một số tiền cố định
                                if (cancelBy == UserTypes.Renter)
                                {
                                    renterReceive = depositAmount.Value * refundPercent;
                                    var moneyLeft = depositAmount.Value - renterReceive;
                                    ownerReceive = depositAmount.Value * (penaltyPercent - servicePercent);
                                    /* Đổi công thức tính tiền đến bù cho Chủ xe, sẽ lấy (Subtotal * DepositPercent) * PenaltyDepositPercent
                                     */
                                    //ownerReceive = (subTotal.Value * depositPercent) * penaltyPercent;
                                    serviceReceive = moneyLeft - ownerReceive;
                                }
                                else if (cancelBy == UserTypes.Owner)
                                {
                                    decimal chargeMoney = depositAmount.Value * penaltyPercent,
                                    renterMoney = chargeMoney * renterPercent / penaltyPercent;
                                    renterReceive = depositAmount.Value;
                                    serviceReceive = chargeMoney - renterMoney;
                                    ownerReceive = -chargeMoney;
                                }
                                #endregion
                            }
                            else
                            {
                                #region Cọc dựa trên %
                                if (cancelBy == UserTypes.Renter)
                                {
                                    renterReceive = depositAmount.Value * refundPercent;
                                    var moneyLeft = depositAmount.Value - renterReceive;
                                    ownerReceive = subTotal.Value * depositPercent * (penaltyPercent - servicePercent);
                                    serviceReceive = moneyLeft - ownerReceive;
                                }
                                else if (cancelBy == UserTypes.Owner)
                                {
                                    decimal chargeMoney = depositAmount.Value * penaltyPercent,
                                    renterMoney = chargeMoney * renterPercent / penaltyPercent;
                                    renterReceive = depositAmount.Value;
                                    serviceReceive = chargeMoney - renterMoney;
                                    ownerReceive = -chargeMoney;
                                }
                                #endregion
                            }
                        }
                    }
                    #endregion
                    #region Trường hợp hủy <= NoRefundGreaterThanDays
                    else
                    {
                        decimal depositPercent = (setting.DepositPercent ?? 30) / 100m,
                        servicePercent = setting.CompletionFeePercentage.Value / 100m;
                        if (setting.DepositAmount > 0)
                        {
                            #region Cọc dựa trên số tiền cố định
                            if (cancelBy == RateByTypes.Renter)
                            {
                                ownerReceive = depositAmount.Value * (1 - servicePercent);
                                serviceReceive = depositAmount.Value - ownerReceive;
                            }
                            else if (cancelBy == RateByTypes.Owner)
                            {
                                ownerReceive = -depositAmount.Value;
                                serviceReceive = depositAmount.Value * servicePercent;
                                renterReceive = depositAmount.Value;
                            }
                            #endregion
                        }
                        else
                        {
                            #region Cọc dựa trên %
                            if (cancelBy == RateByTypes.Renter)
                            {
                                ownerReceive = subTotal.Value * depositPercent * (1 - servicePercent);
                                serviceReceive = depositAmount.Value - ownerReceive;
                            }
                            else if (cancelBy == RateByTypes.Owner)
                            {
                                ownerReceive = -depositAmount.Value;
                                serviceReceive = depositAmount.Value * servicePercent;
                                renterReceive = depositAmount.Value;
                            }
                            #endregion
                        }
                    }
                    #endregion
                }
                result.RefundAmount_Renter = renterReceive;
                result.RefundAmount_Owner = ownerReceive;
                result.RefundAmount_Service = serviceReceive;
            }
            return result;
        }
        #endregion

        #region Build Criteria Priority
        public static decimal GetCriteriaPriority(RentCarFormulaInputModel model, RentalServiceTotalPriceModel result, out List<RentalServiceBookingPriceModel> details)
        {
            details = new();
            decimal criteriaPriority = 0;
            var data = BuildCriteriaPriority(model, result);
            if (data != null)
            {
                var rules = CachedDataManagement.ConfigCriteriaPriorities.Where(c => !c.IsDisable && c.ParentId != null && c.Point != null && !string.IsNullOrEmpty(c.Operator)).ToArray();
                var dictRule = DictionaryHelper.BuildDictionary(rules, c => c.ParentId, true);
                var dictProperty = DictionaryHelper.BuildDictionary4FisrtItem(typeof(CriteriaPriority_CarModel).GetProperties(), c => c.Name);
                foreach (var parentId in dictRule.Keys)
                {
                    var parent = CachedDataManagement.ConfigCriteriaPriority_Get_Instance_Id(parentId);
                    if (parent != null && !parent.IsDisable)
                    {
                        var property = dictProperty.GetValue_Dic(parent.FieldName);
                        if (property != null)
                        {
                            decimal point = 0;
                            string rule = null;
                            var value = (decimal?)property.GetValue(data) ?? 0;
                            if (parent.FieldName == "PointDefault" && value != -1)
                                point = value;
                            else
                            {
                                var children = dictRule[parentId].OrderByDescending(c => c.Point).ToArray();
                                foreach (var child in children)
                                {
                                    bool isCorrect = false;
                                    if (child.Operator == ">" && value > child.FirstValue) isCorrect = true;
                                    else if (child.Operator == ">=" && value >= child.FirstValue) isCorrect = true;
                                    else if (child.Operator == "<" && value < child.FirstValue) isCorrect = true;
                                    else if (child.Operator == "<=" && value <= child.FirstValue) isCorrect = true;
                                    else if (child.Operator == "=" && value == child.FirstValue) isCorrect = true;
                                    else if (child.Operator == "!=" && value != child.FirstValue) isCorrect = true;
                                    else if (child.Operator == "InRange(< and <=)" && child.FirstValue < value && value <= child.SecondValue) isCorrect = true;
                                    if (isCorrect)
                                    {
                                        point = child.Point ?? 0;
                                        if (child.Operator == "InRange(< and <=)")
                                            rule = $"{ConvertToOneDecimalValue(child.FirstValue.Value)} < {ConvertToTwoDecimalValue(value)} <= {ConvertToOneDecimalValue(child.SecondValue.Value)}";
                                        else
                                            rule = $"{ConvertToTwoDecimalValue(value)} {child.Operator} {ConvertToOneDecimalValue(child.FirstValue.Value)}";
                                        break;
                                    }
                                }
                                if (string.IsNullOrEmpty(rule)) rule = ConvertToTwoDecimalValue(value);
                            }
                            details.Add(new() { Title = $"{parent.Name}: {rule}", Price = point, PriceText = ConvertToOneDecimalValue(point) });
                            criteriaPriority += point;
                        }
                    }
                }
            }
            return criteriaPriority;
        }
        public static CriteriaPriority_CarModel BuildCriteriaPriority(RentCarFormulaInputModel model, RentalServiceTotalPriceModel result)
        {
            CriteriaPriority_CarModel data = new();
            var rentalServiceItem = model.ServiceInfo.RentalServiceItem;
            data.Distance = model.ServiceInfo.RentalServiceItem.Distance;
            var rentalServiceItemId = model.ServiceInfo.RentalServiceItem.Id;
            #region PointDefault
            var field_PointDefault = CachedDataManagement.ConfigCriteriaPriority_Get_Instance_FieldName("PointDefault");
            if (field_PointDefault != null && !field_PointDefault.IsDisable)
            {
                var items = CachedDataManagement.RentalServiceItemPriorityList_Get_Instance_RentalServiceItemId(rentalServiceItemId.ToString());
                if (items?.Any() == true)
                {
                    items = items.Where(c => c.Point != null).ToList();
                    var now = DateTime.UtcNow;
                    var unlimited = items.Where(c => c.ToDate == null).ToArray();
                    if (unlimited.Any())
                    {
                        data.PointDefault = unlimited.Max(c => c.Point);
                    }
                    else
                    {
                        var temp = items.Where(c => now <= c.ToDateUTC).ToArray();
                        if (temp.Any())
                            data.PointDefault = temp.Max(c => c.Point);
                    }
                    data.PointDefault ??= -1;
                }
                else data.PointDefault = 0;
            }
            #endregion
            var cached_RentalServiceItem = CachedDataManagement.RentalServiceItem_Calculating_Get_Instance_RentalServiceItemId(rentalServiceItemId);
            if (cached_RentalServiceItem != null)
            {
                data.SearchCount = cached_RentalServiceItem.SearchCount;
                data.TripDoneCount = cached_RentalServiceItem.ServedCount;
                data.IsRentalPriceSmallerThanMedium = !(cached_RentalServiceItem.IsGreaterThanMedPrice ?? false) ? 1 : 0;
            }
            var ownerId = model.ServiceInfo.RentalServiceItem.OwnerId;
            var cached_User = CachedDataManagement.User_Calculating_Get_Instance_UserId(ownerId);
            if (cached_User != null)
            {
                data.OwnerResponseTime = cached_User.OwnerResponseTime;
                data.IsOwnerUpdatedBusyDate = cached_User.Owner_IsUpdatedBusyDate == true ? 1 : 0;
            }
            UserActionHistoryHelper.UserInteractWithApp.TryGetValue(ownerId, out DateTime? interactWithApp);
            if (interactWithApp != null)
            {
                data.LastInteractWithAppInMinute = Convert.ToDecimal((DateTime.UtcNow - interactWithApp.Value).TotalMinutes);
            }
            var vehicle_RentalSetting = rentalServiceItem.Vehicle_RentalSetting;
            if (vehicle_RentalSetting?.HaveMultidayRentalDiscount == true)
            {
                data.TotalDiscountCount = vehicle_RentalSetting.VehicleMultidayRentalDiscount_Detail.Where(c => c.DiscountPercent > 0)?.Count() ?? 0;
            }
            if (model.ServiceInfo.OwnerCancelOrderRatios?.Any() == true)
            {
                var temp = model.ServiceInfo.OwnerCancelOrderRatios.FirstOrDefault(c => c.OwnerId == ownerId);
                if (temp != null) data.CancelOrderPercent = temp.Ratio;
            }
            var userUseDevice = CachedDataManagement.UserUseDevice_Get_Instance_UserLoginId(ownerId);
            data.IsOwnerLogin = (userUseDevice != null) ? 1 : 0;
            return data;
        }
        #endregion

        #region Create Log View
        public static void CreateLogView(SolidStaffInfo user, RentalService_SelfdriveCarRental rentalServiceItem, RentalServiceTotalPriceModel totalPriceModel, RentalServiceItemParamModel param, string viewFrom)
        {
            CreateLogViews(user, new RentalService_SelfdriveCarRental[] { rentalServiceItem }, totalPriceModel, param, viewFrom);
        }
        public static void CreateLogViews(SolidStaffInfo user, RentalService_SelfdriveCarRental[] rentalServiceItems, RentalServiceTotalPriceModel totalPriceModel, RentalServiceItemParamModel param, string viewFrom)
        {
            EzyBaseThreadHelper.RunActionInBackgroundAsync(async () =>
            {
                var ip = param.IPAddress;
                if (rentalServiceItems?.Any() == true)
                {
                    if (ip == "::1") ip = "118.69.35.16";
                    if (ip != "::1")
                    {
                        string key = ip;
                        var fItem = rentalServiceItems.FirstOrDefault();
                        if (viewFrom == CarViewFromCodes.Detail)
                        {
                            key = fItem.Id + "_" + ip;
                        }
                        using var _lock = await SQLDataContextHelper.CheckLockByValueAsync(typeof(RentCarHelper), key);
                        var userId = SQLDataContextHelper.ConvertToStringId(user?.Id);
                        var rentalServiceItemIds = rentalServiceItems.Select(c => c.Id).ToArray();
                        Dictionary<long?, RentalServiceItemViewHistory> dictLog = null;
                        var pastDate = DateTime.UtcNow.AddDays(-1);
                        using (var repo = SQLDataContextHelper.CreateRepository<RentalServiceItemViewHistory>(userId))
                        {
                            var query = repo.GetQueryableReadOnly().Where(c => c.RentalServiceItemId != null && rentalServiceItemIds.Contains(c.RentalServiceItemId.Value) &&
                            c.ViewIP == ip && c.ViewFrom == viewFrom && c.Log_CreatedDate >= pastDate)
                            .GroupBy(c => new { c.ViewIP, c.ViewFrom, c.RentalServiceItemId }).Select(c => new RentalServiceItemViewHistory
                            {
                                ViewIP = c.Key.ViewIP,
                                ViewFrom = c.Key.ViewFrom,
                                ViewAt = c.Max(c => c.ViewAt),
                                RentalServiceItemId = c.Key.RentalServiceItemId
                            });
                            var logs = await query.ToArrayAsync();
                            dictLog = DictionaryHelper.BuildDictionary4FisrtItem(logs, c => c.RentalServiceItemId);
                        }
                        List<RentalServiceItemViewHistory> listLog = new();
                        var settingJson = JsonHelper.DeserializeObject<RentalServiceCategorySettingJsonModel>(CachedDataManagement.ConfigRentalServiceCategory_Get_Instance_Id(fItem.RentalServiceCategoryId).SettingJson);
                        var coolDownMinutes = settingJson.LogViewCoolDownMinutes;
                        string viewOn = "App";
                        if (param.AppName == UIAppNames.sigoweb) viewOn = "Website";
                        foreach (var rentalServiceItem in rentalServiceItems)
                            if (user == null || user.Id != rentalServiceItem.OwnerId)
                            {
                                bool canInsert = true;
                                var item = dictLog.GetValue_Dic(rentalServiceItem.Id);
                                if (item != null)
                                {
                                    var temp = (DateTime.UtcNow - item.ViewAt.Value.ToUniversalTime()).TotalMinutes;
                                    if (temp <= coolDownMinutes) canInsert = false;
                                }
                                if (canInsert)
                                {
                                    var log = new RentalServiceItemViewHistory()
                                    {
                                        RentalServiceItemId = rentalServiceItem.Id,
                                        ViewIP = ip,
                                        ViewAt = DateTime.UtcNow,
                                        ViewBy = user?.Username,
                                        ViewFrom = viewFrom,
                                        ViewOn = viewOn,
                                        ViewRank = rentalServiceItem.Rank,
                                        ViewUrl = param.Url
                                    };
                                    if (totalPriceModel != null)
                                        log.MoreInfo = JsonHelper.SerializeObject(new
                                        {
                                            totalPriceModel.DeliveryAddress,
                                            totalPriceModel.IsHostAddress
                                        });
                                    listLog.Add(log);
                                }
                            }
                        if (listLog.Any())
                            using (var repo = SQLDataContextHelper.CreateRepository<RentalServiceItemViewHistory>(userId))
                            {
                                repo.Insert(listLog);
                            }
                    }
                }
            });
        }
        #endregion

        #region Format Date
        public static string RentalDate_Full(double? from, double? to, int? ui_TimezoneOffset)
        {
            string result = null;
            if (from != null && to != null)
            {
                ui_TimezoneOffset ??= UI_Timezone_OffsetKeys.VN;
                result = RentalDate_Full(RentalService.DateTimeUTC_ToServer(from).Value.AddMinutes(-ui_TimezoneOffset.Value),
                    RentalService.DateTimeUTC_ToServer(to).Value.AddMinutes(-ui_TimezoneOffset.Value));
            }
            return result;
        }
        /// <summary>
        /// Lưu ý truyền vào From và To của Client. Mẫu 8:00 ngày 18 tháng 4 - 09:00 ngày 25 tháng 4, 2024
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="toClient"></param>
        /// <returns></returns>
        public static string RentalDate_Full(DateTime? fromClient, DateTime? toClient)
        {
            string result = null;
            if (fromClient != null && toClient != null)
            {
                if (fromClient.Value.Year != toClient.Value.Year)
                    result = $"{fromClient:HH:mm}, {fromClient:dd} tháng {fromClient:MM}, {fromClient:yyyy} - {toClient:HH:mm}, {toClient:dd} tháng {toClient:MM}, {toClient:yyyy}";
                else
                    result = $"{fromClient:HH:mm}, {fromClient:dd} tháng {fromClient:MM} - {toClient:HH:mm}, {toClient:dd} tháng {toClient:MM}, {toClient:yyyy}";
            }
            return result;
        }

        /// <summary>
        /// Th 5, 25 tháng 04, 2024 <xuống dòng> 08:00
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string DateToFormat_1(DateTime? date)
        {
            string result = null;
            if (date != null)
            {
                DateTime dateV = date.Value;
                int year = date.Value.Year;
                string dayOfWeek = CachedDataManagement.DictWeekday.GetValue_Dic((int)date.Value.DayOfWeek)?.Name;
                result = $"{dayOfWeek}, {dateV:dd} tháng {dateV:MM}, {year}\n{dateV:HH:mm}";
            }
            return result;
        }
        #endregion
    }

    public class RentCarHelperIntergrationTest
    {
        public class RentalServiceInputParamModel
        {
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
        }
        public class RentalServicePriceOutputParamModel
        {
            public decimal? TotalOriginalPriceInput { get; set; }
            public decimal? TotalOriginalPriceOutput { get; set; }

        }
        public class RentalServiceBookingInfoTestPramModel
        {
            public bool IsDefault { get; set; }
            public bool HaveMultidayRentalDiscount { get; set; }
            public bool IsDefaultHasServiceFee { get; set; }
            public bool HaveMultidayRentalDiscountHasServiceFee { get; set; }
            public bool HasVoucher { get; set; }
        }
        public class RentalServiceBookingInfoTestModel
        {
            public bool IsCorrect { get; set; }
            public RentalServiceInputParamModel Input { get; set; }
            public RentalServicePriceOutputParamModel Output { get; set; }
        }
        /// <summary>
        /// Tạo danh sách model mẫu để test hàm UpdateBookingInfo với các giá trị FromDate/ToDate khác nhau.
        /// </summary>
        /// <param name="service">Service cần test</param>
        /// <param name="dateRanges">Danh sách tuple (fromDate, toDate) dạng milliseconds từ epoch</param>
        /// <returns>Danh sách kết quả trả về từ UpdateBookingInfo</returns>
        public static RentalServiceBookingInfoTestModel[] RunUpdateBookingInfoIntegrationTest(RentalServiceBookingInfoTestPramModel p)
        {
            var listInfo = new RentalServiceBookingInfoTestModel[0];
            string id = null;
            string slug = null;
            string voucherCode = null;
            if (p.IsDefault)
            {
                id = "ac0d3227-9a93-4763-aa3e-7217d438e1ba";
                slug = "/vinfast-vf9-plus-2025/N07J6U";
                listInfo = CreateListInfoSimple();
            }
            else if (p.HaveMultidayRentalDiscount)
            {
                id = "fa02c5a3-352f-4a2e-be40-c5b61d554301";
                slug = "/vinfast-vf9-eco-2025/SABSDB";
                listInfo = CreateListInfoHaveMultidayRentalDiscount();
            }
            else if (p.IsDefaultHasServiceFee)
            {
                id = "ac0d3227-9a93-4763-aa3e-7217d438e1ba";
                slug = "/vinfast-vf9-plus-2025/N07J6U";
                listInfo = CreateListInfoServiceFeeSimple();
            }
            else if (p.HaveMultidayRentalDiscountHasServiceFee)
            {
                id = "fa02c5a3-352f-4a2e-be40-c5b61d554301";
                slug = "/vinfast-vf9-eco-2025/SABSDB";
                listInfo = CreateListInfoServiceFeeHaveMultidayRentalDiscount();
            }
            if (p.HasVoucher)
            {
                voucherCode = "FORTESTER";
            }
            var results = new List<RentalServiceBookingInfoTestModel>();
            var param = new RentalServiceParamModel
            {
                RentalServiceCategoryCode = "RENT_CAR",
                Id = id,
                Slug = slug,
                DeliveryAddress = "Thủ Đức, Hồ Chí Minh",
                Latitude = 10.839005m,
                Longitude = 106.839948m,
                DeliveryByOwner = false,
                VoucherCode = voucherCode,
                DeliveryInfo = new RentalServiceDeliveryAddress
                {
                    UserTakeDistance = "0m",
                    UserTakeDeliveryAddress = "Thủ Đức, Hồ Chí Minh",
                    UserTakeLat = 10.839005m,
                    UserTakeLng = 106.839948m,
                    OwnerShipDeliveryAddress = "Thủ Đức, Hồ Chí Minh",
                    OwnerShipDeliveryFee = "Miễn phí",
                    OwnerShipLat = 10.839005m,
                    OwnerShipLng = 106.839948m,
                    OwnerShipCanSelect = true,
                    IsInvalidOwnerShipAddress = false,
                    UserSearchAddress = "Thủ Đức, Hồ Chí Minh",
                    UserSearchLat = 10.839005m,
                    UserSearchLng = 106.839948m,
                    IsUserTakeSelected = false
                },
                UI_TimezoneOffset = -420,
                UI_StartAt = 1759471433502,
                AppName = UIAppNames.sigoweb
            };
            var x = CachedDataManagement.ConfigAddresses.FirstOrDefault();
            var y = x.Code;
            foreach (var item in listInfo)
            {
                param.FromDate = EzyBaseDateTimeHelper.DateTime_FromServerToClient(item.Input.FromDate);
                param.ToDate = EzyBaseDateTimeHelper.DateTime_FromServerToClient(item.Input.ToDate);
                string sMessage;
                var iService = EzyFrameWorkManagement.CreateInstance<IRentalService>("");
                iService.UI_TimezoneOffset = -420;
                var result = iService.UpdateBookingInfo(param, out sMessage);
                if (result != null && string.IsNullOrEmpty(sMessage))
                {
                    var totalOriginalPriceInput = result.RentalPriceDetails.FirstOrDefault(t => t.Code == "BookingInfo_TotalPriceByDay").Price;
                    var discountVoucher = FormatHelper.ParseCurrencyToNumber(result.VoucherDiscountPrice);
                    var output = new RentalServicePriceOutputParamModel()
                    {
                        TotalOriginalPriceInput = item.Output.TotalOriginalPriceInput,
                        TotalOriginalPriceOutput = Math.Round(totalOriginalPriceInput - discountVoucher ?? 0, 1)
                    };
                    if (item.Output.TotalOriginalPriceInput == totalOriginalPriceInput - discountVoucher)
                    {
                        results.Add(new RentalServiceBookingInfoTestModel
                        {
                            Input = item.Input,
                            Output = output,
                            IsCorrect = true
                        });
                    }
                    else
                    {
                        results.Add(new RentalServiceBookingInfoTestModel
                        {
                            Input = item.Input,
                            Output = output,
                            IsCorrect = false
                        });
                    }

                }
            }
            return results.OrderBy(t => t.IsCorrect).ToArray();
        }
        private static RentalServiceBookingInfoTestModel[] CreateListInfoSimple()
        {
            var listInfo = new List<RentalServiceBookingInfoTestModel>();
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("19:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 12/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 1350000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("16:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 12/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 1800000

                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("11:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 12/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 2400000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("23:00 12/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 1350000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("05:00 13/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 1800000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("13:00 13/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 2400000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("17:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("03:00 13/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 2400000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("18:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 12/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 1425000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("18:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 12/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 1425000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("12:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 12/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 1800000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("01:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 12/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 2400000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("00:00 13/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 1425000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("12:00 13/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 1800000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("17:00 13/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 2400000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("19:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("23:00 12/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 1500000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("15:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("09:00 13/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 2400000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("10:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("15:00 13/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 3600000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 14/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 3600000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("07:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 4200000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("08:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("08:00 12/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 1800000
                }
            });
            return listInfo.ToArray();
        }
        private static RentalServiceBookingInfoTestModel[] CreateListInfoHaveMultidayRentalDiscount()
        {
            var listInfo = new List<RentalServiceBookingInfoTestModel>();

            //------------- 3 Days
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("19:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 4702500
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("16:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 5130000

                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("11:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 5700000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("23:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 4702500
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("05:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 3990000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("13:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 4560000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("17:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("03:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 4560000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("18:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 4773750
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("18:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 4773750
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("12:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 5130000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("01:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 5700000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("00:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 3633750
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("12:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 3990000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("17:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 4560000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("19:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("23:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 4845000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("15:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("09:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 4560000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("10:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("15:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 5700000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 4560000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("07:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 3990000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("08:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("08:00 15/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 5130000
                }
            });
            //----------------------- 7 Days
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("19:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 8775000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("16:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 9180000

                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("11:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 9720000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("23:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 8775000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("05:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 8100000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("13:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 8640000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("17:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("03:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 8640000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("18:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 8842500
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("18:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 8842500
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("12:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 9180000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("01:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 9720000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("00:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 7762500
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("12:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 8100000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("17:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 8640000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("19:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("23:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 8910000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("15:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("09:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 8640000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("10:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("15:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 9720000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 8640000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("07:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 8100000
                }
            });
            listInfo.Add(new RentalServiceBookingInfoTestModel
            {
                Input = new RentalServiceInputParamModel
                {
                    FromDate = FormatHelper.FormatStringToDateTime("08:00 11/10/2025"),
                    ToDate = FormatHelper.FormatStringToDateTime("08:00 19/10/2025")
                },
                Output = new RentalServicePriceOutputParamModel()
                {
                    TotalOriginalPriceInput = 9180000
                }
            });

            return listInfo.ToArray();
        }

        private static RentalServiceBookingInfoTestModel[] CreateListInfoServiceFeeSimple()
        {
            var listInfo = new List<RentalServiceBookingInfoTestModel>();
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("23:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5430000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5280000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("23:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10710000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10560000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("00:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 4185000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("00:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 9465000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("05:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 4620000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("05:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 9900000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("07:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 4620000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("07:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 9900000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("12:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 4620000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("12:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 9900000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("13:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5280000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("13:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10560000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("17:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5280000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("17:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10560000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("19:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5430000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("19:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("23:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5580000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("19:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10710000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("19:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("23:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10860000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("18:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5505000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("18:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10785000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("17:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("03:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5280000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("17:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("03:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10560000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("16:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5940000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("16:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 11220000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("15:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("09:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5280000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("15:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("09:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10560000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("12:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5940000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("12:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 11220000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("11:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 6600000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("11:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 11880000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("10:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("15:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 6600000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("10:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("15:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 11880000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("08:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("08:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5940000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("08:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("08:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 11220000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("01:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 6600000 } });
            listInfo.Add(new RentalServiceBookingInfoTestModel { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("01:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 11880000 } });

            return listInfo.ToArray();
        }

        private static RentalServiceBookingInfoTestModel[] CreateListInfoServiceFeeHaveMultidayRentalDiscount()
        {
            var listInfo = new List<RentalServiceBookingInfoTestModel>();
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("23:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5158500 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5016000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("23:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 9639000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 9504000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("00:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 3975750 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("00:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 8518500 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("05:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 4389000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("05:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 8910000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("07:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 4389000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("07:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 8910000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("12:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 4389000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("12:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 8910000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("13:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5016000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("13:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 9504000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("17:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5016000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("21:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("17:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 9504000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("19:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5158500 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("19:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("23:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5301000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("19:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 9639000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("19:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("23:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 9774000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("18:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5229750 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("18:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 9706500 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("17:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("03:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5016000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("17:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("03:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 9504000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("16:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5643000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("16:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10098000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("15:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("09:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5016000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("15:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("09:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 9504000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("12:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5643000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("12:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10098000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("11:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 6270000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("11:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10692000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("10:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("15:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 6270000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("10:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("15:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10692000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("08:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("08:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 5643000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("08:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("08:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10098000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("01:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 15/10/2025") }, Output = new() { TotalOriginalPriceInput = 6270000 } });
            listInfo.Add(new() { Input = new() { FromDate = FormatHelper.FormatStringToDateTime("01:00 11/10/2025"), ToDate = FormatHelper.FormatStringToDateTime("21:00 19/10/2025") }, Output = new() { TotalOriginalPriceInput = 10692000 } });

            return listInfo.ToArray();
        }
    }

}