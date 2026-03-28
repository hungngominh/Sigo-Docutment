using AllianceMiddlemanWebAPI.Core.Data.BusinessData;
using AllianceMiddlemanWebAPI.Core.DataInfo.Cached;
using AllianceMiddlemanWebAPI.DataShared.Common;
using AllianceMiddlemanWebAPI.Shared.Helper;
using AllianceMiddlemanWebAPI.Shared.Models;
using Ezy.APIService.Core.DataInfo;
using Ezy.APIService.Shared.Helper;
using Ezy.Module.Library.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AllianceMiddlemanWebAPI.Shared.Services
{
    public partial class RentalServiceItemBaseService<T, TModel, TParam, TOption>
    {
        protected static string ConvertToOneDecimalValue(decimal rate)
        {
            return RentalServiceHelper.ConvertToOneDecimalValue(rate);
        }
        protected static string FormatMoneyDetail(decimal money)
        {
            return RentalServiceHelper.FormatMoneyDetail(money);
        }

        #region Show in App only
        /// <summary>
        /// Dùng ở App
        /// </summary>
        /// <returns></returns>
        public virtual string GetVoucherShowInDetail()
        {
            return "Giảm 10% cho khách đặt xe lần đầu";
        }
        #endregion

        #region GetRentalPriceDetails
        /// <summary>
        /// Hàm tạo nhanh model
        /// </summary>
        /// <param name="cf"></param>
        /// <param name="price"></param>
        /// <returns></returns>
        static RentalServiceBookingPriceModel CreateBookingPriceModel(ConfigSimpleInfo cf, decimal? price)
        {
            return new()
            {
                Title = cf.Name,
                PriceText = FormatMoneyDetail(price ?? 0),
                Information = cf.MoreConfig,
                Code = cf.Code,
                Price = price,
            };
        }

        #region GetServiceFee
        static void GetServiceFee(ConfigSimpleInfo cf, List<RentalServiceBookingPriceModel> listItem, decimal? price)
        {
            if (cf != null)
            {
                listItem.Add(CreateBookingPriceModel(cf, price));
            }
        }
        #endregion
        #region GetInsuranceFee
        static void GetInsuranceFee(ConfigSimpleInfo cf, List<RentalServiceBookingPriceModel> listItem, decimal? price)
        {
            if (cf != null)
            {
                string priceText = FormatMoneyDetail(price ?? 0);
                var temp = CreateBookingPriceModel(cf, price);
                temp.Detail = new() { Title = cf.MoreConfig, IsShowTitleOnly = true };
                temp.PriceText = priceText;
                listItem.Add(temp);
            }
        }
        #endregion
        #region GetDeliveryFee
        static void GetDeliveryFee(ConfigSimpleInfo cf, List<RentalServiceBookingPriceModel> listItem, decimal? price)
        {
            if (cf != null)
            {
                if (price >= 0)
                {
                    var item = CreateBookingPriceModel(cf, price);
                    item.Group = 2;
                    item.Information = "Phí giao nhận xe được tính dựa trên khoảng cách từ chủ xe đến vị trí bạn đã lựa chọn trên ứng dụng.";
                    item.Detail = new() { Title = item.Information, IsShowTitleOnly = true };
                    listItem.Add(item);
                }
            }
        }
        #endregion
        #region GetTotalPriceByDay
        /// <summary>
        /// Tổng giá thuê cho một ngày. Hiện thị theo dạng {PriceByDay} x {RentalDayCount}
        /// </summary>
        /// <param name="cf"></param>
        /// <param name="listItem"></param>
        /// <param name="totalPriceByDay"></param>
        /// <param name="rentalDayCount"></param>
        static void GetTotalPriceByDay(ConfigSimpleInfo cf, List<RentalServiceBookingPriceModel> listItem, RentalServiceTotalPriceModel totalPriceModel, string rentalDate, bool isShowAllDayRentalSegment = false)
        {
            if (cf != null)
            {
                RentalServiceBookingPriceDetailModel detail = null;
                decimal? totalPriceByDay = totalPriceModel.TotalOriginalPrice - totalPriceModel.TotalPromotionMoney;// + totalPriceModel.ServiceFee;
                string priceText = FormatMoney(totalPriceByDay);
                if (totalPriceModel.Segments?.Any() == true)
                {
                    detail = new();
                    //Cộng dồn quá giờ vào ngày có giá
                    var segments = totalPriceModel.Segments
                                .Select(s =>
                                {
                                    DateTime parsedDate;
                                    if (!DateTime.TryParseExact(
                                            s.PriceDate,
                                            FormatHelper.DateDisplay_dd_Slash_MM_Slash_yyyy,
                                            CultureInfo.InvariantCulture,
                                            DateTimeStyles.None,
                                            out parsedDate))
                                    {
                                        GGNotifyHelper.SendMessageGGChat_SystemReport("Lỗi parse date PriceDate trong GetTotalPriceByDay - RentalServiceItemBaseService");
                                        parsedDate = DateTime.MinValue;
                                    }

                                    return new RentCarSegment_PriceExtraModel
                                    {
                                        Code = s.Code,
                                        Price = s.Price,
                                        PriceInfo = s.PriceInfo,
                                        PriceDate = s.PriceDate,
                                        Date = parsedDate
                                    };
                                })
                                .OrderBy(s => s.Date)
                                .ToList();

                    for (int i = 0; i < segments.Count; i++)
                    {
                        var current = segments[i];

                        // GetEarly + PriceByHours → cộng vào ngày sau có PriceInfo != PriceByHours
                        if (current.Code == RentCarHelper.GetEarly && current.PriceInfo == RentCarHelper.PriceByHours)
                        {
                            var next = segments
                                .FirstOrDefault(s => s.Date >= current.Date && s.PriceInfo != RentCarHelper.PriceByHours);

                            if (next != null)
                            {
                                next.Price += current.Price;
                                current.Price = 0;
                            }
                        }

                        // ReturnLate + PriceByHours → cộng vào ngày trước có PriceInfo != PriceByHours
                        else if (current.Code == RentCarHelper.ReturnLate && current.PriceInfo == RentCarHelper.PriceByHours)
                        {
                            var prev = segments
                                .LastOrDefault(s => s.Date <= current.Date && s.PriceInfo != RentCarHelper.PriceByHours);

                            if (prev != null)
                            {
                                prev.Price += current.Price;
                                current.Price = 0;
                            }
                        }
                    }

                    // Kết quả cuối
                    var days = segments
                        .Where(s => s.Price > 0)
                        .GroupBy(s => s.PriceDate)
                        .Select(g => new
                        {
                            PriceDate = g.Key,
                            Price = g.Sum(x => x.Price)
                        })
                        .ToArray();
                    //var days = totalPriceModel.Segments.GroupBy(c => c.PriceDate).Select(c => new
                    //{
                    //    PriceDate = c.Key,
                    //    Price = c.Sum(c => c.Price)
                    //}).ToArray();

                    if (days.Length > 7 && !isShowAllDayRentalSegment)
                    {
                        detail.IsShowTitleOnly = true;
                        detail.Title = "Giá trung bình hàng ngày được làm tròn";
                    }
                    else
                    {
                        detail.Title = "Chi tiết giá cơ sở";
                        var temp = days.Select(c => new RentalServiceBookingPriceModel()
                        {
                            Title = c.PriceDate,
                            PriceText = FormatMoney(c.Price),
                            Group = 0
                        }).ToList();
                        if (totalPriceModel.TotalPromotionMoney > 0)
                            temp.Add(new() { Title = "Giảm giá", PriceText = FormatMoney(totalPriceModel.TotalPromotionMoney), Group = 1 });
                        temp.Add(new() { Title = "Tổng giá cơ sở", PriceText = priceText, Group = 2 });
                        detail.Data = temp;
                    }
                }
                listItem.Add(new()
                {
                    Title = rentalDate,
                    PriceText = priceText,
                    Information = cf.MoreConfig,
                    Code = cf.Code,
                    Price = totalPriceByDay,
                    Detail = detail
                });
            }
        }
        #endregion
        protected string GetRentalDayCountText(int? rentalDayCount)
        {
            return RentalServiceHelper.GetRentalDayCountText(rentalDayCount);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalPriceModel"></param>
        /// <param name="rentalDate"></param>
        /// <returns></returns>
        public virtual RentalServiceBookingPriceModel[] GetRentalPriceDetails(RentalServiceTotalPriceModel totalPriceModel, string rentalDate, bool isShowAllDayRentalSegment = false)
        {
            List<RentalServiceBookingPriceModel> listItem = new();
            if (totalPriceModel != null)
            {
                var dictSimple = DictionaryHelper.BuildDictionary4FisrtItem(CachedDataManagement.ConfigSimple_Get_Instance_ByConfigType(ConfigSimpleTypes.CMSContent_RentCar), c => c.Code);

                var totalPriceByDay = dictSimple.GetValue_Dic(CMSKeys.BookingInfo_TotalPriceByDay);
                GetTotalPriceByDay(totalPriceByDay, listItem, totalPriceModel, rentalDate, isShowAllDayRentalSegment);

                ConfigSimpleInfo insuranceFee = null;
                if (totalPriceModel.TotalInsuranceFee > 0)
                    insuranceFee = dictSimple.GetValue_Dic(CMSKeys.BookingInfo_InsuranceFee);
                else insuranceFee = dictSimple.GetValue_Dic(CMSKeys.BookingInfo_InsuranceFee_Owner);
                GetInsuranceFee(insuranceFee, listItem, totalPriceModel.TotalInsuranceFee);

                if (UIAppName != UIAppNames.sigoapp_new)
                {
                    //var serviceFee = dictSimple.GetValue_Dic(CMSKeys.BookingInfo_ServiceFee);
                    //GetServiceFee(serviceFee, listItem, totalPriceModel.ServiceFee);
                }

                var deliveryFee = dictSimple.GetValue_Dic(CMSKeys.BookingInfo_DeliveryFee);
                GetDeliveryFee(deliveryFee, listItem, totalPriceModel.DeliveryFee);
            }
            return listItem.ToArray();
        }
        #endregion

        #region GetExtraSurcharges
        public virtual RentalServiceBookingInfoItemModel[] GetExtraSurcharges(ExtraSurchargesModel param)
        {
            return null;
        }
        #endregion

        #region Voucher
        public RentalServiceVoucherPublicModel[] GetVouchers(TParam param, out string sMessage)
        {
            var iService = CreateServiceInstance<IDiscountCodeService>();
            return iService.GetVouchers(new()
            {
                RentalServiceCategoryCode = param.RentalServiceCategoryCode,
                TextSearch_DiscountCode = param.VoucherCode,
                RentalServiceId = param.Id
            }, out sMessage);
        }
        #endregion

        #region Busy Schedule
        public async Task<double[]> GetBusySchedules(TParam param)
        {
            var result = new double[0];
            List<DateTime> busyDates = new();
            var settingJson = RentalServiceHelper.GetSettingJson();
            var hoursBusyFromDate = settingJson?.RentalSchedule?.BusyStartCutoffHour ?? 21;
            var hoursBusyToDate = settingJson?.RentalSchedule?.BusyEndCutoffHour ?? 17;
            DateTime todayClient = DateTime.UtcNow.Date;
            DateTime todayUTC = DateTime.UtcNow.Date;

            // ---------------------------
            // Launch all 3 DB queries in parallel
            // ---------------------------
            var repo1 = DC_CreateRepository<ServiceItem_BookedRentalSchedule>();
            var repo2 = DC_CreateRepository<ServiceItem_WeekdaysBusyRentalSchedule>();
            var repo3 = DC_CreateRepository<ServiceItem_DateBusyRentalSchedule>();

            try
            {
                var bookedTask = repo1.GetQueryableReadOnly()
                    .Where(c => c.RentalServiceItemId == RentalServiceItem.Id &&
                                c.IsBooked == true &&
                                c.ToDate >= todayUTC)
                    .ToArrayAsync();

                var weekdaysTask = repo2.GetQueryableReadOnly()
                    .Where(c => c.RentalServiceItemId == RentalServiceItem.Id && c.IsBusy == true)
                    .ToArrayAsync();

                var dateBusyTask = repo3.GetQueryableReadOnly()
                    .Where(c => c.RentalServiceItemId == RentalServiceItem.Id &&
                                c.IsBusy == true &&
                                c.ToDate >= todayUTC)
                    .ToArrayAsync();

                await Task.WhenAll(bookedTask, weekdaysTask, dateBusyTask);

                var items_booked = bookedTask.Result;
                var weekItems = weekdaysTask.Result;
                var items_dateBusy = dateBusyTask.Result;

                // ---------------------------
                // Lịch đã đặt (Booked)
                // ---------------------------
                foreach (var item in items_booked)
                {
                    if (item.FromDate == null || item.ToDate == null)
                        continue;

                    DateTime from = item.FromDate.Value;
                    DateTime to = item.ToDate.Value;

                    for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
                    {
                        if (d == from.Date)
                        {
                            // Giờ bắt đầu <= hoursBusyFromDateh → bận
                            if (from.TimeOfDay <= TimeSpan.FromHours(hoursBusyFromDate))
                                busyDates.Add(d);
                        }
                        else if (d == to.Date)
                        {
                            // Giờ kết thúc >= hoursBusyToDateh → bận
                            if (to.TimeOfDay >= TimeSpan.FromHours(hoursBusyToDate))
                                busyDates.Add(d);
                        }
                        else
                        {
                            // Ngày ở giữa → luôn bận
                            busyDates.Add(d);
                        }
                    }
                }

                // ---------------------------
                // Ngày bận trong tuần (định kỳ)
                // ---------------------------
                if (weekItems.Any())
                {
                    var weekDays = weekItems.Select(c => c.Weekday).ToArray(); // 0 = CN, 1 = Thứ 2, ...
                    DateTime endDateClient = todayClient.AddMonths(1); // ví dụ: chỉ kiểm tra 1 tháng tới

                    for (var date = todayClient; date <= endDateClient; date = date.AddDays(1))
                    {
                        if (weekDays.Contains((int?)date.DayOfWeek))
                            busyDates.Add(date);
                    }
                }

                // ---------------------------
                // Ngày bận cố định (manual)
                // ---------------------------
                foreach (var item in items_dateBusy)
                {
                    if (item.FromDate == null || item.ToDate == null)
                        continue;

                    DateTime from = item.FromDate.Value;
                    DateTime to = item.ToDate.Value;

                    for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
                    {
                        if (d == from.Date)
                        {
                            if (from.TimeOfDay <= TimeSpan.FromHours(hoursBusyFromDate))
                                busyDates.Add(d);
                        }
                        else if (d == to.Date)
                        {
                            if (to.TimeOfDay >= TimeSpan.FromHours(hoursBusyToDate))
                                busyDates.Add(d);
                        }
                        else
                        {
                            busyDates.Add(d);
                        }
                    }
                }
            }
            finally
            {
                repo1.Dispose();
                repo2.Dispose();
                repo3.Dispose();
            }

            // ---------------------------
            // 4️⃣ Kết quả cuối cùng
            // ---------------------------
            result = busyDates
                .Where(x => x >= todayClient)
                .Distinct()
                .OrderBy(x => x)
                .Select(t => DateTime_ToClient(t) ?? 0)
                .ToArray();

            return result;
        }

        #endregion  

        /// <summary>
        /// Lấy thông tin chi tiết của dịch vụ để thuê dịch vụ
        /// </summary>
        /// <param name="param"></param>
        /// <param name="sMessage"></param>
        /// <returns></returns>
        public virtual RentalServicePublicModel GetDetail(TParam param, out string sMessage)
        {
            sMessage = string.Empty;
            return null;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của dịch vụ để thuê dịch vụ
        /// </summary>
        /// <param name="param"></param>
        /// <param name="sMessage"></param>
        /// <returns></returns>
        public virtual Task<(RentalServicePublicModel data, string error)> GetDetailAsync(TParam param)
        {
            return Task.FromResult<(RentalServicePublicModel, string)>((null, null));
        }
    }
}
