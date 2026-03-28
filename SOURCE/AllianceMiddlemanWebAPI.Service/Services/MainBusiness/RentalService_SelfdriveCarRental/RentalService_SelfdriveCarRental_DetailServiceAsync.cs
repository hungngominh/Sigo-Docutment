using AllianceMiddlemanWebAPI.Core.Data.BusinessData;
using AllianceMiddlemanWebAPI.Core.DataInfo.Cached;
using AllianceMiddlemanWebAPI.DataShared.Common;
using AllianceMiddlemanWebAPI.Shared.Helper;
using AllianceMiddlemanWebAPI.Shared.Models;
using Ezy.Module.Library.UI;
using Ezy.Module.Library.Utilities;
using OfficeOpenXml.FormulaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AllianceMiddlemanWebAPI.Shared.Services
{
    public partial class RentalService_SelfdriveCarRentalBaseService<TModel, TParam, TOption>
    {
        public override async Task<(RentalServicePublicModel data, string error)> GetDetailAsync(TParam param)
        {

            RentalServicePublicModel result = null;
            string sMessage = string.Empty;
            var perfTracker = new DetailPerformanceTracker(param.Id ?? "unknown");
            try
            {
                sMessage = CheckGetDetailParam(param);
                string msgWarning = string.Empty;
                if (!string.IsNullOrEmpty(sMessage) && sMessage.StartsWith("[Warning]"))
                {
                    msgWarning = sMessage;
                    sMessage = string.Empty;
                }

                if (string.IsNullOrEmpty(sMessage))
                {
                    param.IsGetVehicleMultidayRentalDiscount_Detail = true;
                    param.IsGetVehicle_InclueInsurance = true;

                    perfTracker.Start("SetRentalServiceItemAsync");
                    await SetRentalServiceItemAsync(param);
                    perfTracker.Stop("SetRentalServiceItemAsync");
                    if (RentalServiceItem != null)
                    {
                        DetailScreen = true;

                        #region Avatar

                        perfTracker.Start("BuildDictServiceItem_ImageAsync");
                        var (dictAvaImage, dictImages) =
                            await SearchingVehicleHelper.BuildDictServiceItem_ImageAsync(new long[]
                                { RentalServiceItem.Id });
                        var avaImage = dictAvaImage.GetValue_Dic(RentalServiceItem.Id);
                        var images = dictImages.GetValue_Dic(RentalServiceItem.Id);
                        perfTracker.Stop("BuildDictServiceItem_ImageAsync");

                        #endregion

                        param.NeedGetCriteriaPoint = true;
                        perfTracker.Start("GetTotalPriceModel");
                        var totalPriceModel = GetTotalPriceModel(param);
                        perfTracker.Stop("GetTotalPriceModel");
                        sMessage = totalPriceModel.Error;
                        DateTime? fromDate = DateTime_To_ClientDateTime(param.FromDate),
                            toDate = DateTime_To_ClientDateTime(param.ToDate);
                        var rentalDate = RentalServiceHelper.GetRentalDate(fromDate.Value, toDate.Value);

                        #region DeliveryInfo / GetRentalPriceDetails

                        perfTracker.Start("GetRentalPriceDetails+GetDeliveryInfo");
                        var rentalPriceDetails = GetRentalPriceDetails(totalPriceModel, rentalDate);
                        rentalPriceDetails = GetDeliveryInfo(param, out sMessage, rentalPriceDetails,
                            out RentalServiceDeliveryAddress deliveryInfo);
                        perfTracker.Stop("GetRentalPriceDetails+GetDeliveryInfo");

                        #endregion

                        result = BuildPublicData<RentalServicePublicModel>(totalPriceModel, RentalServiceItem,
                            avaImage);
                        result.DeliveryInfo = deliveryInfo;
                        result.RentalDate = rentalDate;
                        result.CanChangeDeliveryAddress =
                            RentalServiceItem.Vehicle_RentalSetting.HaveDeliverySurcharge ?? false;
                        result.ImageUrls = SearchingVehicleHelper.GetServiceItem_ImageFileUrl(images);
                        result.FullDescription = RentalServiceItem.FullDescription;

                        #region Popular Place

                        var popularPlaces =
                            CachedDataManagement.ConfigSimple_Get_Instance_ByConfigType(ConfigSimpleTypes
                                .Search_Popular_Place);
                        if (popularPlaces?.Any() == true)
                        {
                            popularPlaces = popularPlaces.OrderBy(c => c.OrderNo).ThenByDescending(c => c.Id).ToList();
                            List<PopularPlaceModel> listPopularPlace = new();
                            foreach (var popularPlace in popularPlaces)
                            {
                                var obj =
                                    JsonHelper.DeserializeObject<PopularPlaceAddressModel>(popularPlace.MoreConfig) ??
                                    new();
                                listPopularPlace.Add(new()
                                {
                                    Name = popularPlace.Name,
                                    Address = obj.Address,
                                    Latitude = obj.Latitude,
                                    Longitude = obj.Longitude,
                                    SearchVersion = obj.SearchVersion,
                                    ProvinceId = obj.ProvinceId,
                                    DistrictId = obj.DistrictId,
                                    WardId = obj.WardId
                                });
                            }

                            result.PopularPlaces = listPopularPlace.ToArray();
                        }

                        #endregion

                        #region Characteristics

                        perfTracker.Start("GetCharacteristics");
                        result.Characteristics = GetCharacteristics(RentalServiceItem);
                        perfTracker.Stop("GetCharacteristics");

                        #endregion

                        #region Owner

                        perfTracker.Start("OwnerInfo");
                        var owner = CachedDataManagement.UserLogin_Get_Instance_Id(RentalServiceItem.OwnerId);

                        #region Đếm số xe mà chủ xe sở hữu

                        int totalServiceCount = 0;
                        totalServiceCount = CachedDataManagement.RentalServiceItems
                            .Where(c => c.OwnerId == owner.Id && c.IsApproved == true && !c.IsSuspended &&
                                        !c.IsDeactive && !c.IsAddNew)
                            .Count();

                        #endregion

                        if (owner != null)
                        {
                            var rating = GetUserRating(owner.Id);
                            result.OwnerInfo = new()
                            {
                                Id = owner.Id.ToString(),
                                AvatarUrl = GetAvatarUrl(owner),
                                Name = owner.DisplayName,
                                Rating = rating.Item1,
                                ServedCount = rating.Item2,
                                ReplyWithin = GetOwnerReplyWithin(owner.Id),
                                InfoUrl = RentalServiceHelper.GetUserInfoUrl(owner.Id),
                                TotalServiceCount = totalServiceCount,
                                TotalRatingCount = rating.Item3,
                            };
                            var user_ext = CachedDataManagement.UserLogin_Ext_Get_Instance_Id(owner.Id);
                            if (user_ext != null && user_ext.JoinedDate != null)
                            {
                                var date = user_ext.JoinedDate.Value.ToUniversalTime();
                                result.OwnerInfo.JoinedDate = ToStringClientDate(date);
                                result.OwnerInfo.JoinedWhen = RentalServiceHelper.GetJoinedWhenText(date);
                            }
                        }
                        perfTracker.Stop("OwnerInfo");

                        #endregion

                        #region Feature

                        perfTracker.Start("GetFeatures");
                        result.Features = GetFeatures(RentalServiceItem);
                        perfTracker.Stop("GetFeatures");

                        #endregion

                        #region Document / Security

                        perfTracker.Start("GetDocumentAndSecurityAsync");
                        await GetDocumentAndSecurityAsync(result, RentalServiceItem);
                        perfTracker.Stop("GetDocumentAndSecurityAsync");

                        #endregion

                        #region CMS

                        var cms = CachedDataManagement.ConfigSimple_Get_Instance_ByConfigType(ConfigSimpleTypes
                            .CMSContent_RentCar);
                        var cms_RentCarTerm = cms.FirstOrDefault(c => !c.IsDisable && c.Code == "RENT_CAR_TERM");
                        result.Term = cms_RentCarTerm?.MoreConfig;
                        var cms_CancelRentCarPolicy =
                            cms.FirstOrDefault(c => !c.IsDisable && c.Code == "CANCEL_RENT_CAR_POLICY");
                        result.CancelBookingPolicy = GetCancelRentCarPolicy(cms_CancelRentCarPolicy?.MoreConfig);
                        var cms_Insurance = cms.FirstOrDefault(c => !c.IsDisable && c.Code == "RENT_CAR_INSURANCE");
                        result.InsuranceDescription = cms_Insurance?.MoreConfig;
                        var cms_Detail =
                            CachedDataManagement.ConfigSimple_Get_Instance_ByConfigType(ConfigSimpleTypes
                                .CMSContent_RentCarDetail);
                        DateTime nowClient = DateTime_Now_Client(), nowClientDate = nowClient.Date;

                        #region CancelBookingPolicyInfo

                        result.CancelBookingPolicyInfo =
                            GetCMSContent_RentCarDetailInfo(cms_Detail, "Booking_Info_Cancel_Order");
                        var cancelOrderSetting = SettingJson.CancelOrderSetting;
                        if (result.CancelBookingPolicyInfo != null && cancelOrderSetting != null)
                        {
                            var description = result.CancelBookingPolicyInfo.Description;
                            if (!string.IsNullOrEmpty(description))
                            {
                                var hours = cancelOrderSetting.FullRefundWithinMinutes.Value / 60;
                                description = description.Replace("#FullRefundWithinHours#",
                                    ConvertToOneDecimalValue(hours));
                                var temp = fromDate.Value.AddDays(-cancelOrderSetting.NoRefundGreaterThanDays.Value)
                                    .Date;
                                if (nowClientDate < temp)
                                {
                                    string text = " Bạn được hoàn tiền một phần nếu hủy trước ngày #Date#";
                                    description += text.Replace("#Date#", temp.ToString("dd/MM/yyyy"));
                                }

                                result.CancelBookingPolicyInfo.Description = description;
                            }

                            var content = result.CancelBookingPolicyInfo.Content;
                            if (!string.IsNullOrEmpty(content))
                            {
                                content = content.Replace("#FullRefundWithinHours#",
                                    ConvertToOneDecimalValue(cancelOrderSetting.FullRefundWithinMinutes.Value / 60m));
                                content = content.Replace("#RefundPercent#",
                                    Math.Round(100 - SettingJson.DepositPercent.Value, 0, MidpointRounding.AwayFromZero)
                                        .ToString());
                                content = content.Replace("#NoRefundGreaterThanDays#",
                                    cancelOrderSetting.NoRefundGreaterThanDays.ToString());
                                content = content.Replace("#FullRefundWithinMinutes#",
                                    ConvertToOneDecimalValue(cancelOrderSetting.FullRefundWithinMinutes.Value));
                            }

                            result.CancelBookingPolicyInfo.Content = content;
                        }

                        #endregion

                        #region PaymentInstructionInfo

                        result.PaymentInstructionInfo =
                            GetCMSContent_RentCarDetailInfo(cms_Detail, "Booking_Info_Payment");
                        if (result.PaymentInstructionInfo != null)
                        {
                            var description = result.PaymentInstructionInfo.Description;
                            var depositAmount =
                                Math.Ceiling((totalPriceModel.TotalPrice * (SettingJson.DepositPercent ?? 30) / 100m) ??
                                             0);
                            double day = nowClient.Day, month = nowClient.Month, year = nowClient.Year;
                            description = description.Replace("#DepositMoney#", FormatMoney(depositAmount))
                                .Replace("#RemainAmount#", FormatMoney(totalPriceModel.TotalPrice - depositAmount))
                                .Replace("#BookingDate#", $"ngày {day} tháng {month}, {year}");
                            day = fromDate.Value.Day;
                            month = fromDate.Value.Month;
                            year = fromDate.Value.Year;
                            description = description.Replace("#StartDate#", $"ngày {day} tháng {month}, {year}");
                            result.PaymentInstructionInfo.Description = description;
                        }

                        #endregion

                        #region GeneralProvisionInfo

                        var Booking_Info_General_Provision = "Booking_Info_General_Provision";
                        if (param.AppName == UIAppNames.sigoapp_new)
                            Booking_Info_General_Provision += "_App_New";
                        result.GeneralProvisionInfo =
                            GetCMSContent_RentCarDetailInfo(cms_Detail, Booking_Info_General_Provision);
                        if (result.GeneralProvisionInfo != null &&
                            !string.IsNullOrEmpty(result.GeneralProvisionInfo.Content))
                        {
                            var g = RentalServiceHelper.GetEndTime(SettingJson.BusinessHourStart,
                                SettingJson.BusinessHourEnd, UI_TimezoneOffset, SettingJson.HourOwner2Confirm);
                            var o = Convert.ToInt32((g - DateTime.UtcNow).TotalHours);
                            result.GeneralProvisionInfo.Content =
                                result.GeneralProvisionInfo.Content.Replace("#RenterWaitingOwnerHours#", o.ToString());
                        }

                        #endregion

                        #endregion

                        #region Review

                        perfTracker.Start("GetReviewAsync");
                        await GetReviewAsync(result);
                        perfTracker.Stop("GetReviewAsync");

                        #endregion

                        #region Voucher

                        perfTracker.Start("GetVouchers");
                        param.Id = RentalServiceItem.Id.ToString();
                        result.Vouchers = GetVouchers(param, out sMessage);
                        perfTracker.Stop("GetVouchers");

                        #endregion

                        #region RentalPriceDetails / TotalPrice

                        result.RentalPriceDetails = rentalPriceDetails;
                        result.RentalDayCount = GetRentalDayCountText(totalPriceModel.RentalDayCount);
                        result.TotalPrice = FormatMoney(totalPriceModel.TotalPrice);

                        #endregion

                        #region ExtraSurcharges

                        result.ExtraSurcharges = GetExtraSurcharges(new()
                        {
                            RentalDayCount = totalPriceModel.RentalDayCount,
                            FromDate = param.FromDate,
                            ToDate = param.ToDate
                        });
                        result.BookingInfoItems = result.ExtraSurcharges;

                        #endregion

                        #region BookingFeatureInfos

                        string insuranceName = "VNI";
                        var setting = RentalServiceItem.Vehicle_RentalSetting;
                        if (setting?.HaveInsurance == true)
                        {
                            var ins = RentalServiceItem.Vehicle.Vehicle_InsuranceInformation?.FirstOrDefault();
                            var now = DateTime.Now;
                            if (ins != null && ins.IsVerified == true &&
                                ins.CoverageStartDate <= ins.CoverageEndDate && ins.CoverageStartDate <= now &&
                                ins.CoverageEndDate >= now)
                            {
                                insuranceName = "từ chủ xe";
                            }
                        }

                        if (insuranceName == "VNI")
                        {
                            var cms_Booking_Info_Insurance_By_VNI =
                                cms.FirstOrDefault(c => c.Code == CMSKeys.Booking_Info_Insurance_By_VNI);
                            result.InsuranceInfo = new()
                            {
                                Title = cms_Booking_Info_Insurance_By_VNI.Name?.Replace("#InsuranceName#",
                                    insuranceName),
                                Description = cms_Booking_Info_Insurance_By_VNI.ColorCode,
                                Content = cms_Booking_Info_Insurance_By_VNI.MoreConfig,
                                Icon = cms_Booking_Info_Insurance_By_VNI.IconUrl,
                            };
                        }
                        else
                        {
                            var cms_Booking_Info_Insurance_By_Owner =
                                cms.FirstOrDefault(c => c.Code == CMSKeys.Booking_Info_Insurance_By_Owner);
                            result.InsuranceInfo = new()
                            {
                                Title = cms_Booking_Info_Insurance_By_Owner.Name?.Replace("#InsuranceName#",
                                    insuranceName),
                                Description = cms_Booking_Info_Insurance_By_Owner.ColorCode,
                                Content = cms_Booking_Info_Insurance_By_Owner.MoreConfig,
                                Icon = cms_Booking_Info_Insurance_By_Owner.IconUrl
                            };
                        }

                        #region BookingFeatureInfos

                        var bookingFeatureInfos = CachedDataManagement
                            .ConfigSimple_Get_Instance_ByConfigType(ConfigSimpleTypes.Booking_Feature_Info)
                            .Where(c => !c.IsDisable).OrderBy(c => c.OrderNo).ToArray();
                        result.BookingFeatureInfos = bookingFeatureInfos.Select(c =>
                        {
                            BaseCategoryInfoItem t = new()
                            {
                                Title = c.Name,
                                Description = c.ColorCode,
                                Icon = c.IconUrl,
                                IconMobile = c.MoreConfig
                            };
                            if (c.Code == "Car_Insurance")
                            {
                                t.Title = t.Title.Replace("#InsuranceName#", insuranceName);
                            }
                            else if (c.Code == "Mortgage_When_Receiving_Car")
                            {
                                if (result.Securities?.Any() == true)
                                {
                                    //var securities = result.Securities.Select(c => ((c.Text ?? string.Empty) + " " + (c.Description ?? string.Empty)).Trim()).ToArray();
                                    var securities = result.Securities
                                        .Where(t => t.Value != null && t.Value.ToString() != "security_money" &&
                                                    t.Value.ToString() != "security_cavet")
                                        .Select(c =>
                                            string.IsNullOrWhiteSpace(c.Text_Display)
                                                ? c.Text ?? string.Empty
                                                : c.Text_Display)
                                        .ToList();
                                    var description = string.Join(" hoặc ",
                                        result.Securities
                                            .Where(t => t.Value != null &&
                                                        (t.Value.ToString() == "security_money" ||
                                                         t.Value.ToString() == "security_cavet"))
                                            .Select(c => string.IsNullOrWhiteSpace(c.Text_Display)
                                                ? c.Text ?? string.Empty
                                                : c.Text_Display)
                                    );
                                    //securities.Add(description);
                                    t.Description = string.Join("<br>", securities);
                                    if (!string.IsNullOrEmpty(description))
                                        t.Description = description + "<br>" + t.Description;
                                }
                            }

                            return t;
                        }).ToArray();

                        #endregion

                        #endregion

                        #region ThingsToKnow

                        result.ThingsToKnow = RentCarHelper.GetThingsToKnow(cms_Detail, SettingJson,
                            RentalServiceItem.Vehicle.VehicleNoOfSeatId);

                        #endregion

                        #region Log View

                        if (totalPriceModel.IsHostAddress)
                            totalPriceModel.DeliveryAddress = RentalServiceItem.HostAddress.Detail;
                        else totalPriceModel.DeliveryAddress = param.DeliveryAddress;
                        RentCarHelper.CreateLogView(GSCurrentUser, RentalServiceItem, totalPriceModel, param,
                            CarViewFromCodes.Detail);

                        #endregion

                        #region ReportUserReasonList

                        var rpReasons =
                            CachedDataManagement.ConfigReportUserReasons.Where(c =>
                                !string.IsNullOrEmpty(c.Name) && !c.IsDisable);
                        result.ReportUserReasonList = rpReasons.Select(c => new BaseCategoryShowExtItem()
                        {
                            Text = c.Name,
                            Value = c.Id.ToString(),
                            NeedShowExt = c.ShowExtReason ?? false
                        }).ToArray();

                        #endregion

                        #region Busy Schedules
                        perfTracker.Start("GetBusySchedules");
                        result.BusySchedules = await GetBusySchedules(param);
                        perfTracker.Stop("GetBusySchedules");

                        #endregion
                        #region Get default note
                        perfTracker.Start("GetDefaultNote");
                        using (var repoO = DC_CreateRepository<Order>())
                        {
                            var now = DateTime_Now().Date;
                            var userLoginId = ID_ConvertStringToPKId(UserLoginId);
                            // Lấy cái cuối cùng
                            var order = repoO.GetQueryable(t => t.RenterId == userLoginId && t.StatusCode == OrderStatus.OWNER2CONFIRM).OrderByDescending(t => t.Id).FirstOrDefault();
                            if (order != null)
                            {
                                result.MessageToOwner = order.MessageToOwner;
                            }
                            else
                            {
                                // lấy cái cuối cùng
                                order = repoO.GetQueryable(t => t.RenterId == userLoginId && (t.StatusCode == OrderStatus.CUSCANCEL || t.StatusCode == OrderStatus.OWNERCANCEL || t.StatusCode == OrderStatus.SYSTEMCANCEL) && t.Log_CreatedDate.HasValue && t.Log_CreatedDate.Value.Date == now).OrderByDescending(t => t.Id).FirstOrDefault();
                                if (order != null)
                                    result.MessageToOwner = order.MessageToOwner;
                            }
                        }
                        perfTracker.Stop("GetDefaultNote");

                        #endregion
                    }
                    else sMessage = "Không tìm thấy dữ liệu";

                }

                if (string.IsNullOrEmpty(sMessage) && !string.IsNullOrEmpty(msgWarning))
                {
                    sMessage = msgWarning;
                }
            }
            catch (Exception ex)
            {
                SaveLogException(ex, "GetDetail", param);
                sMessage = Exception_GetMessage(ex);
            }

            if (!string.IsNullOrEmpty(sMessage))
            {
                result ??= new();
                result.CanBooking = false;
            }

            try
            {
                var perfLog = perfTracker.GetLogOutput();
                System.Diagnostics.Debug.WriteLine(perfLog);
            }
            catch { /* don't let perf logging break the flow */ }

            return (result, sMessage);
        }
    }
}