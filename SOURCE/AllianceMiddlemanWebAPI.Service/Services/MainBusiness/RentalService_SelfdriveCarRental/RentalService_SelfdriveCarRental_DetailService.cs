using AllianceMiddlemanWebAPI.Core.Data.BusinessData;
using AllianceMiddlemanWebAPI.Core.DataInfo.Cached;
using AllianceMiddlemanWebAPI.Core.Services;
using AllianceMiddlemanWebAPI.Core.Utilities;
using AllianceMiddlemanWebAPI.DataShared.Common;
using AllianceMiddlemanWebAPI.Shared.Helper;
using AllianceMiddlemanWebAPI.Shared.Models;
using Ezy.APIService.Core.DataInfo;
using Ezy.APIService.Core.Services;
using Ezy.APIService.Shared.Helper;
using Ezy.Module.Library.UI;
using Ezy.Module.Library.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace AllianceMiddlemanWebAPI.Shared.Services
{
    public partial class RentalService_SelfdriveCarRentalBaseService<TModel, TParam, TOption>
    {
        #region GetDetail
        private static (string, int?) GetVehicleRating(long? rentalServiceId)
        {
            return RentalServiceHelper.GetRentalServiceRating(rentalServiceId);
        }
        #region Owner
        private (string, int?, int?) GetUserRating(long? ownerId)
        {
            return RentalServiceHelper.GetUserRating(ownerId);
        }
        private string GetOwnerReplyWithin(long? ownerId)
        {
            string result = string.Empty;
            var temp = CachedDataManagement.User_Calculating_Get_Instance_UserId(ownerId);
            if (temp != null)
            {
                var minutes = temp.OwnerResponseTime ?? 0;
                if (minutes > 0)
                {
                    if (minutes < 60) result = $"{ConvertToOneDecimalValue(minutes)} phút";
                    else result = ConvertToOneDecimalValue(minutes / 60m) + " giờ";
                }
            }
            return result;
        }
        #endregion
        #region Related Vehicle
        /// <summary>
        /// Đang lấy tạm 10 đứa vẫn còn hoạt động
        /// </summary>
        /// <param name="param"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private RentalService_SelfdriveCarRental[] GetRelatedRentalServices(long[] addressIds, long id)
        {
            var setting = SearchingVehicleHelper.GetSetting();
            RentalService_SelfdriveCarRental[] result = null;
            using (var repoRentalService = InitRepository())
            {
                var query = repoRentalService.GetQueryable().Where(c => c.HostAddressId != null && c.Id != id && c.IsApproved == true && !c.IsSuspended && !c.IsDeactive);
                if (addressIds != null && addressIds.Any())
                    query = query.Where(c => c.HostAddressId != null && addressIds.Contains(c.HostAddressId.Value));
                else query = query.Where(c => c.Id == -1);
                result = query.Take(setting.RelatedVehicleMaximum ?? 10).Include(c => c.Vehicle).ToArray();
            }
            return result;
        }
        #endregion
        #region Characteristic
        public static BaseIconUrlCategoryItem[] GetCharacteristics(RentalService_SelfdriveCarRental rentalService)
        {
            List<BaseIconUrlCategoryItem> listCharacteristic = new();
            var vehicle = rentalService.Vehicle;
            if (vehicle.VehicleNoOfSeatId != null)
            {
                var cf = CachedDataManagement.ConfigVehicleNoOfSeat_Get_Instance_Id(vehicle.VehicleNoOfSeatId);
                if (cf != null && !cf.IsDisable)
                    listCharacteristic.Add(new() { Text = cf.Name, IconUrl = GetIconUrl(cf) });
            }
            if (vehicle.VehicleTransmissionTypeId != null)
            {
                var cf = CachedDataManagement.ConfigVehicleTransmissionType_Get_Instance_Id(vehicle.VehicleTransmissionTypeId);
                if (cf != null && !cf.IsDisable)
                    listCharacteristic.Add(new() { Text = cf.Name, IconUrl = GetIconUrl(cf) });
            }
            if (vehicle.FuelTypeId != null)
            {
                var cf = CachedDataManagement.ConfigVehicleFuelType_Get_Instance_Id(vehicle.FuelTypeId);
                if (cf != null)
                {
                    var iconUrl = GetIconUrl(cf);
                    if (cf != null && !cf.IsDisable)
                        listCharacteristic.Add(new() { Text = cf.Name, IconUrl = iconUrl });
                    if (!string.IsNullOrEmpty(vehicle.FuelEfficiency))
                    {
                        var text = vehicle.FuelEfficiency;
                        if (!string.IsNullOrEmpty(cf.FuelEfficiencyUnit))
                        {
                            text += cf.FuelEfficiencyUnit;
                        }
                        listCharacteristic.Add(new() { Text = text, IconUrl = iconUrl });
                    }
                }
            }
            return listCharacteristic.ToArray();
        }
        #endregion
        #region Feature
        private BaseIconCategoryItem[] GetFeatures(RentalService_SelfdriveCarRental rentalService)
        {
            BaseIconCategoryItem[] result = null;
            using (var repo = DC_CreateRepository<ServiceItem_Feature_Mapping>())
            {
                var mappings = repo.GetQueryable().Where(c => c.RentalServiceItemId == rentalService.Id && !c.IsDisable && c.IsFeatured).ToArray();
                if (mappings != null && mappings.Any())
                {
                    result = mappings.Select(c =>
                    {
                        BaseIconCategoryItem t = null;
                        var cf = CachedDataManagement.ConfigServiceItemFeature_Get_Instance_Id(c.ServiceItemFeatureId);
                        if (cf != null && !cf.IsDisable) t = new()
                        {
                            Text = cf.Name,
                            IconUrl = GetIconUrl(cf),
                            Icon = cf.Icon,
                            IconMobile = cf.IconMobile
                        };
                        return t;
                    }).Where(c => c != null).ToArray();
                }
            }
            return result;
        }
        private async Task<BaseIconCategoryItem[]> GetFeaturesAsync(RentalService_SelfdriveCarRental rentalService)
        {
            BaseIconCategoryItem[] result = null;
            using (var repo = DC_CreateRepository<ServiceItem_Feature_Mapping>())
            {
                var mappings = await repo.GetQueryable()
                    .Where(c => c.RentalServiceItemId == rentalService.Id && !c.IsDisable && c.IsFeatured)
                    .ToArrayAsync();
                if (mappings != null && mappings.Any())
                {
                    result = mappings.Select(c =>
                    {
                        BaseIconCategoryItem t = null;
                        var cf = CachedDataManagement.ConfigServiceItemFeature_Get_Instance_Id(c.ServiceItemFeatureId);
                        if (cf != null && !cf.IsDisable) t = new()
                        {
                            Text = cf.Name,
                            IconUrl = GetIconUrl(cf),
                            Icon = cf.Icon,
                            IconMobile = cf.IconMobile
                        };
                        return t;
                    }).Where(c => c != null).ToArray();
                }
            }
            return result;
        }
        #endregion
        #region Get Document and Security
        private void GetDocumentAndSecurity(RentalServicePublicModel result, RentalService_SelfdriveCarRental rentalService)
        {
            var iService = CreateServiceInstance<IServiceItem_RentalRequiredItem_MappingService>();
            if (string.IsNullOrEmpty(iService.UserLoginId))
                iService.UserLoginId = CachedDataManagement.UserAdmin.Id.ToString();
            var mappings = iService.GetListSimple(new() { RentalServiceItemId = rentalService.Id.ToString(), IsRequired = true }, out string sMessage);
            if (mappings != null && mappings.Any())
            {
                BaseIconUrl_ExtCategoryItem noRequiredSecurity = null;
                List<BaseIconUrl_ExtCategoryItem> listDoc = new(), listSec = new();
                foreach (var mapping in mappings)
                    if (mapping.IsRequired == true)
                    {
                        var cf = CachedDataManagement.ConfigRentalRequiredItem_Get_Instance_Id(mapping.RentalRequiredItemId);
                        if (cf != null && !cf.IsDisable)
                        {
                            if (mapping.IsManual == true)
                            {
                                if (cf.IsDocument == true) listDoc.Add(new() { Text = mapping.ItemName, IconUrl = GetIconUrl(cf), Icon = cf.Icon });
                                if (cf.IsSecurity == true) listSec.Add(new() { Text = mapping.ItemName, IconUrl = GetIconUrl(cf), Icon = cf.Icon });
                            }
                            else
                            {
                                if (cf.Code == "no_required_security")
                                {
                                    noRequiredSecurity = new() { Text = cf.Name, Description = cf.Description, IconUrl = GetIconUrl(cf), Icon = cf.Icon, Value = cf.Code };
                                }
                                var cfDescription = cf.Description;
                                string name = null;
                                if (cf.Code == "security_money")
                                    name = $"{EzyStringHelper.ToVietnameseShortNumber(mapping.ItemName)}";
                                if (cf.Code == "security_cavet")
                                {
                                    var description = cf.Note?.Replace("#OwnerSetAmount#", $"{EzyStringHelper.ToVietnameseShortNumber(mapping.ItemName)}") ?? cf.Description;
                                    cfDescription = description;
                                    //description = $"{name ?? cf.Name} {description}";
                                }
                                //if (cf.IsDocument == true) listDoc.Add(new() { Text = cf.Name, Description = cf.Description, IconUrl = GetIconUrl(cf), Icon = cf.Icon });
                                //if (cf.IsSecurity == true) listSec.Add(new() { Text = cf.Name, Description = cf.Description, IconUrl = GetIconUrl(cf), Icon = cf.Icon });
                                if (cf.IsSecurity == true) listSec.Add(new() { Text = name ?? cf.Name, Description = cfDescription, IconUrl = GetIconUrl(cf), Icon = cf.Icon, Value = cf.Code });
                                if (cf.IsDocument == true) listDoc.Add(new() { Text = name ?? cf.Name, Description = cfDescription, IconUrl = GetIconUrl(cf), Icon = cf.Icon, Value = cf.Code });

                            }
                        }
                    }
                if (noRequiredSecurity != null)
                {
                    listSec.Clear();
                    listSec.Add(noRequiredSecurity);
                }
                result.Documents = listDoc.ToArray();
                result.Securities = listSec.ToArray();
            }
        }
        private async Task GetDocumentAndSecurityAsync(RentalServicePublicModel result, RentalService_SelfdriveCarRental rentalService)
        {
            var iService = CreateServiceInstance<IServiceItem_RentalRequiredItem_MappingService>();
            if (string.IsNullOrEmpty(iService.UserLoginId))
                iService.UserLoginId = CachedDataManagement.UserAdmin.Id.ToString();
            var (mappings, _) = await iService.GetListSimpleAsync(new() { RentalServiceItemId = rentalService.Id.ToString(), IsRequired = true });
            if (mappings != null && mappings.Any())
            {
                BaseIconUrl_ExtCategoryItem noRequiredSecurity = null;
                List<BaseIconUrl_ExtCategoryItem> listDoc = new(), listSec = new();
                foreach (var mapping in mappings)
                    if (mapping.IsRequired == true)
                    {
                        var cf = CachedDataManagement.ConfigRentalRequiredItem_Get_Instance_Id(mapping.RentalRequiredItemId);
                        if (cf != null && !cf.IsDisable)
                        {
                            // Nếu vừa có xe máy với tiền thì gop lại

                            if (mapping.IsManual == true)
                            {
                                if (cf.IsDocument == true) listDoc.Add(new() { Text = mapping.ItemName, IconUrl = GetIconUrl(cf), Icon = cf.Icon, Value = cf.Code, Text_Display = mapping.ItemName });
                                if (cf.IsSecurity == true) listSec.Add(new() { Text = mapping.ItemName, IconUrl = GetIconUrl(cf), Icon = cf.Icon, Value = cf.Code, Text_Display = mapping.ItemName });
                            }
                            else
                            {
                                var description = cf.Note?.Replace("#OwnerSetAmount#", $"{EzyStringHelper.ToVietnameseShortNumber(mapping.ItemName)}") ?? cf.Description;

                                if (cf.Code == "no_required_security")
                                {
                                    noRequiredSecurity = new() { Text = cf.Name, Description = cf.Description, IconUrl = GetIconUrl(cf), Icon = cf.Icon, Value = cf.Code, Text_Display = description };
                                }

                                var cfDescription = cf.Description;
                                string name = null;
                                if (cf.Code == "security_money")
                                    name = $"{EzyStringHelper.ToVietnameseShortNumber(mapping.ItemName)}";
                                if (cf.Code == "security_cavet")
                                {
                                    cfDescription = description;
                                    description = $"{name ?? cf.Name} {description}";
                                }

                                if (cf.IsSecurity == true) listSec.Add(new() { Text = name ?? cf.Name, Description = cfDescription, IconUrl = GetIconUrl(cf), Icon = cf.Icon, Value = cf.Code, Text_Display = description });
                                if (cf.IsDocument == true) listDoc.Add(new() { Text = name ?? cf.Name, Description = cfDescription, IconUrl = GetIconUrl(cf), Icon = cf.Icon, Value = cf.Code, Text_Display = description });

                            }
                        }
                    }
                if (noRequiredSecurity != null)
                {
                    listSec.Clear();
                    listSec.Add(noRequiredSecurity);
                }
                result.Documents = listDoc.ToArray();
                result.Securities = listSec.ToArray();

            }
        }
        #endregion
        #region Review
        /// <summary>
        /// Lấy đánh giá của người thuê cho dịch vụ này
        /// </summary>
        /// <param name="result"></param>
        /// <param name="rentalServiceId"></param>
        private void GetReview(RentalServicePublicModel result)
        {
            using (var repo = DC_CreateRepository<Order>())
            {
                var orders = repo.GetQueryable().Where(c => c.RentalServiceItemId == RentalServiceItem.Id)
                    .Include(c => c.Order_Rating.Where(c => !c.IsDeleted && c.RateByType == RateByTypes.Renter))
                    .Where(c => c.Order_Rating != null && c.Order_Rating.Any())
                    .Include(c => c.Order_Vehicle.Where(c => !c.IsDeleted)).ThenInclude(c => c.Order_Vehicle_Address)
                    .ToArray();
                if (orders != null && orders.Any())
                {
                    List<RentalServicerReviewByUserPublicModel> reviewByRenters = new();
                    var rateByAnonymous = SystemText_GetValue(TextDisplayKeys.DefaultRateByAnonymous, "Ẩn danh");
                    var unassignedAvatar = EzyPictureHelper.GetFullUrl_Server("/Avatar/Unassigned.png");
                    foreach (var order in orders)
                    {
                        if (order.Order_Rating != null && order.Order_Rating.Any())
                        {
                            reviewByRenters.AddRange(order.Order_Rating.Select(c =>
                            {
                                var rateBy = CachedDataManagement.UserLogin_Get_Instance_Id(c.RateById);
                                string displayName = string.Empty, avatarUrl = string.Empty;
                                if (order.RenterId != c.RateById)
                                {
                                    displayName = rateByAnonymous;
                                    avatarUrl = unassignedAvatar;
                                }
                                else { displayName = rateBy?.DisplayName; avatarUrl = GetAvatarUrl(rateBy); }
                                string address = null;
                                var temp = order.Order_Vehicle?.FirstOrDefault();
                                if (temp != null && temp.Order_Vehicle_Address != null && !string.IsNullOrEmpty(temp.Order_Vehicle_Address.Detail))
                                {
                                    address = RentalServiceHelper.GetOwnerAddress_Type_RENT_CAR(null, new() { Detail = temp.Order_Vehicle_Address.Detail });
                                }
                                var n = new RentalServicerReviewByUserPublicModel()
                                {
                                    Rating = Convert.ToInt32(c.Rate),
                                    ReviewAt = RentalServiceHelper.GetReviewAt(c.Log_CreatedDate),
                                    ReviewBy = displayName,
                                    // Vì note ban đầu là field User, nên giờ phải để
                                    User = displayName,
                                    ReviewByAvatarUrl = avatarUrl,
                                    ReviewContent = c.RateComment,
                                    OrderNo = c.Id,
                                    Address = address
                                };
                                return n;
                            }));
                        }
                    }
                    result.Review = new()
                    {
                        ReviewByRenters = reviewByRenters.OrderByDescending(c => c.OrderNo).ToArray(),
                        RatingByRenterCount = reviewByRenters.Count,
                        TotalRatingCount = reviewByRenters.Count
                    };
                }
            }
        }
        private async Task GetReviewAsync(RentalServicePublicModel result)
        {
            using (var repo = DC_CreateRepository<Order>())
            {
                var orders = await repo.GetQueryableReadOnly().Where(c => c.RentalServiceItemId == RentalServiceItem.Id)
                    .Include(c => c.Order_Rating.Where(c => !c.IsDeleted && c.RateByType == RateByTypes.Renter))
                    .Where(c => c.Order_Rating != null && c.Order_Rating.Any())
                    .Include(c => c.Order_Vehicle.Where(c => !c.IsDeleted)).ThenInclude(c => c.Order_Vehicle_Address)
                    .Select(c => new Order() { Order_Rating = c.Order_Rating, Order_Vehicle = c.Order_Vehicle, RenterId = c.RenterId, OwnerId = c.OwnerId })
                    .ToArrayAsync();
                if (orders?.Length > 0)
                {
                    List<RentalServicerReviewByUserPublicModel> reviewByRenters = new();
                    var rateByAnonymous = SystemText_GetValue(TextDisplayKeys.DefaultRateByAnonymous, "Ẩn danh");
                    var unassignedAvatar = EzyPictureHelper.GetFullUrl_Server("/Avatar/Unassigned.png");
                    foreach (var order in orders)
                    {
                        if (order.Order_Rating != null && order.Order_Rating.Any())
                        {
                            //Sửa logic: Chỉ lấy đánh giá của khách thuê
                            reviewByRenters.AddRange(order.Order_Rating.Where(t => t.RateById == order.RenterId).Select(c =>
                            {
                                var rateBy = CachedDataManagement.UserLogin_Get_Instance_Id(c.RateById);
                                string displayName = string.Empty, avatarUrl = string.Empty;
                                if (order.RenterId != c.RateById)
                                {
                                    displayName = rateByAnonymous;
                                    avatarUrl = unassignedAvatar;
                                }
                                else { displayName = rateBy?.DisplayName; avatarUrl = GetAvatarUrl(rateBy); }
                                string address = null;
                                var temp = order.Order_Vehicle?.FirstOrDefault();
                                if (temp != null && temp.Order_Vehicle_Address != null && !string.IsNullOrEmpty(temp.Order_Vehicle_Address.Detail))
                                {
                                    address = RentalServiceHelper.GetOwnerAddress_Type_RENT_CAR(null, new() { Detail = temp.Order_Vehicle_Address.Detail });
                                }
                                var n = new RentalServicerReviewByUserPublicModel()
                                {
                                    Rating = Convert.ToInt32(c.Rate),
                                    ReviewAt = RentalServiceHelper.GetReviewAt(c.Log_CreatedDate),
                                    ReviewBy = displayName,
                                    // Vì note ban đầu là field User, nên giờ phải để
                                    User = displayName,
                                    ReviewByAvatarUrl = avatarUrl,
                                    ReviewContent = c.RateComment,
                                    OrderNo = c.Id,
                                    Address = address
                                };
                                return n;
                            }));
                        }
                    }
                    result.Review = new()
                    {
                        ReviewByRenters = reviewByRenters.OrderByDescending(c => c.OrderNo).ToArray(),
                        RatingByRenterCount = reviewByRenters.Count,
                        TotalRatingCount = reviewByRenters.Count,
                        Rating = reviewByRenters.Any()
                                ? reviewByRenters.Average(c => c.Rating).ToString("0.0")
                                : "Mới"
                    };
                }
            }
        }
        #endregion
        #region GetExtraSurcharges
        public override RentalServiceBookingInfoItemModel[] GetExtraSurcharges(ExtraSurchargesModel param)
        {
            List<RentalServiceBookingInfoItemModel> result = new();
            Vehicle_RentalSetting vehicle_RentalSetting = RentalServiceItem.Vehicle_RentalSetting;
            var dictCode = DictionaryHelper.BuildDictionary4FisrtItem(CachedDataManagement.ConfigSimple_Get_Instance_ByConfigType(ConfigSimpleTypes.CMSContent_RentCar), c => c.Code);

            #region Số km tối đa
            var cfExcessMileage = dictCode.GetValue_Dic(CMSKeys.ExtraSurcharge_ExcessMileage);
            if (cfExcessMileage != null)
            {
                bool haveExcessMileageSurcharge = vehicle_RentalSetting.HaveExcessMileageSurcharge == true && vehicle_RentalSetting.MaximumMileage > 0 && vehicle_RentalSetting.ExcessMileageSurcharge > 0;
                string content = "Chủ xe không giới hạn số kilomet đối với xe này";
                result.Add(new()
                {
                    TitleLeft = cfExcessMileage.Name,
                    TitleRight = haveExcessMileageSurcharge ? $"{FormatMoney(vehicle_RentalSetting.ExcessMileageSurcharge * 1000)}/km" : "Miễn phí",
                    IconUrl = EzyPictureHelper.GetFullUrl_Server(cfExcessMileage.IconUrl),
                    Content = haveExcessMileageSurcharge ? cfExcessMileage.MoreConfig?.Replace("#ExcessMileage#", $"{ConvertMToKM(vehicle_RentalSetting.MaximumMileage * 1000 * (param.RentalDayCount ?? 1))}") : content
                });
            }
            #endregion

            #region Phí quá giờ
            if (vehicle_RentalSetting.NumOfLateHourToBeOneDay > 0 && vehicle_RentalSetting.LateHourReturnFee > 0)
            {
                var cfExcessHour = dictCode.GetValue_Dic(CMSKeys.ExtraSurcharge_ExcessHour);
                if (cfExcessHour != null)
                {
                    int? fromHour = DateTime_To_ClientDateTime(param.FromDate)?.Hour;
                    var endHour = vehicle_RentalSetting.RentalHourEnd;
                    if (vehicle_RentalSetting.RentalHour_Using24Hours)
                        endHour = fromHour;
                    DateTime toDate = DateTime_To_ClientDateTime(param.ToDate).Value,
                        nowDate = toDate.Date;
                    endHour += Convert.ToInt32(vehicle_RentalSetting.NumOfLateHourToBeOneDay ?? 5);
                    if (endHour > 24) endHour -= 24;
                    var temp = nowDate.AddHours(endHour.Value);
                    if (temp > toDate) nowDate = temp;
                    else nowDate = temp.AddDays(1);
                    var sDate = nowDate.ToString("HH:mm") + ", ngày " + nowDate.ToString("dd") + " thg " + nowDate.ToString("MM") + ", " + nowDate.ToString("yyyy");
                    result.Add(new()
                    {
                        TitleLeft = cfExcessHour.Name,
                        TitleRight = FormatMoney(vehicle_RentalSetting.LateHourReturnFee * 1000) + "/giờ",
                        IconUrl = EzyPictureHelper.GetFullUrl_Server(cfExcessHour.IconUrl),
                        Content = cfExcessHour.MoreConfig?.Replace("#Date#", sDate)
                    });
                }
            }
            #endregion

            #region Phí khác
            #region Phí vệ sinh
            var cfCleaning = dictCode.GetValue_Dic("ExtraSurcharge_Cleaning");
            if (cfCleaning != null)
            {
                bool haveCleaningFee = vehicle_RentalSetting.HaveCleaningFee == true && vehicle_RentalSetting.CleaningFee > 0;
                string content = "Chủ xe không phụ thu phí rửa xe đối với xe này";
                result.Add(new()
                {
                    TitleLeft = cfCleaning.Name,
                    TitleRight = haveCleaningFee ? FormatMoney(vehicle_RentalSetting.CleaningFee * 1000) : "Miễn phí",
                    IconUrl = EzyPictureHelper.GetFullUrl_Server(cfCleaning.IconUrl),
                    Content = haveCleaningFee ? cfCleaning.MoreConfig : content
                });
            }
            #endregion
            //var cfChargingFee = dictCode.GetValue_Dic("ExtraSurcharge_ChargingFee");
            #region Phí sạc
            if (RentalServiceItem.Vehicle != null)
            {
                var cf = CachedDataManagement.ConfigVehicleFuelType_Get_Instance_Id(RentalServiceItem.Vehicle.FuelTypeId);
                if (cf?.Code == "ELECTRIC")
                {
                    //bool haveCleaningFee = vehicle_RentalSetting.HChargingFee == true && vehicle_RentalSetting.ChargingFee > 0;
                    //string content = "Chủ xe không phụ thu phí sạc điện đối với xe này";
                    var chargingFee = dictCode.GetValue_Dic(CMSKeys.ExtraSurcharge_ChargingFee);
                    var fee = vehicle_RentalSetting.ChargingFee ?? 0;
                    result.Add(new()
                    {
                        TitleLeft = chargingFee.Name,
                        TitleRight = FormatMoney(fee * 1000) + "/km",
                        IconUrl = EzyPictureHelper.GetFullUrl_Server(chargingFee.IconUrl),
                        Content = chargingFee.MoreConfig
                    });
                }
            }
            #endregion
            #region Phí khử mùi
            var cfDeodorizingFee = dictCode.GetValue_Dic(CMSKeys.ExtraSurcharge_DeodorizingFee);
            if (cfDeodorizingFee != null)
            {
                bool haveCleaningFee = vehicle_RentalSetting.HaveCleaningFee == true && vehicle_RentalSetting.DeodorizingFee > 0;
                string content = "Chủ xe không phụ thu phí khử mùi đối với xe này";
                var fee = vehicle_RentalSetting.DeodorizingFee ?? 0;
                result.Add(new()
                {
                    TitleLeft = cfDeodorizingFee.Name,
                    TitleRight = haveCleaningFee ? FormatMoney(fee * 1000) + "/lần" : "Miễn phí",
                    IconUrl = EzyPictureHelper.GetFullUrl_Server(cfDeodorizingFee.IconUrl),
                    Content = haveCleaningFee ? cfDeodorizingFee.MoreConfig : content
                });
            }
            #endregion
            #endregion

            return result.ToArray();
        }
        #endregion
        private CancelPolicy_DetailsModel GetCancelRentCarPolicy(string sCancelRentCarPolicy)
        {
            CancelPolicy_DetailsModel result = new();
            if (!string.IsNullOrEmpty(sCancelRentCarPolicy))
            {
                var cancelSetting = SettingJson.CancelOrderSetting;
                result.Hours = (cancelSetting.FullRefundWithinMinutes / 60)?.ToString() + " giờ";
                result.Days = cancelSetting.NoRefundGreaterThanDays?.ToString() + " ngày";
                result.RefundPercent = FormatHelper.FormatString_Percent(100 - SettingJson.DepositPercent);
                sCancelRentCarPolicy = sCancelRentCarPolicy.Replace("#Hours#", result.Hours);
                sCancelRentCarPolicy = sCancelRentCarPolicy.Replace("#RefundPercent#", result.RefundPercent);
                sCancelRentCarPolicy = sCancelRentCarPolicy.Replace("#Days#", result.Days);
                result.Policy = sCancelRentCarPolicy;
            }
            return result;
        }
        /// <summary>
        /// Lưu ý:
        /// - Web UI chỉ gửi được 3 giá trị: Slug, FromDate và ToDate
        /// </summary>
        /// <param name="param"></param>
        /// <param name="sMessage"></param>
        /// <returns></returns>
        public override RentalServicePublicModel GetDetail(TParam param, out string sMessage)
        {
            RentalServicePublicModel result = null;
            sMessage = string.Empty;
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
                    SetRentalServiceItem(param);
                    if (RentalServiceItem != null)
                    {
                        DetailScreen = true;
                        #region Avatar
                        var (dictAvaImage, dictImages) = SearchingVehicleHelper.BuildDictServiceItem_ImageAsync(new long[] { RentalServiceItem.Id }).GetAwaiter().GetResult();
                        var avaImage = dictAvaImage.GetValue_Dic(RentalServiceItem.Id);
                        var images = dictImages.GetValue_Dic(RentalServiceItem.Id);
                        #endregion
                        param.NeedGetCriteriaPoint = true;
                        var totalPriceModel = GetTotalPriceModel(param);
                        sMessage = totalPriceModel.Error;
                        DateTime? fromDate = DateTime_To_ClientDateTime(param.FromDate), toDate = DateTime_To_ClientDateTime(param.ToDate);
                        var rentalDate = RentalServiceHelper.GetRentalDate(fromDate.Value, toDate.Value);
                        #region DeliveryInfo / GetRentalPriceDetails
                        var rentalPriceDetails = GetRentalPriceDetails(totalPriceModel, rentalDate);
                        rentalPriceDetails = GetDeliveryInfo(param, out sMessage, rentalPriceDetails, out RentalServiceDeliveryAddress deliveryInfo);
                        #endregion
                        result = BuildPublicData<RentalServicePublicModel>(totalPriceModel, RentalServiceItem, avaImage);
                        result.DeliveryInfo = deliveryInfo;
                        result.RentalDate = rentalDate;
                        result.CanChangeDeliveryAddress = RentalServiceItem.Vehicle_RentalSetting.HaveDeliverySurcharge ?? false;
                        result.ImageUrls = SearchingVehicleHelper.GetServiceItem_ImageFileUrl(images);
                        result.FullDescription = RentalServiceItem.FullDescription;
                        #region Popular Place
                        var popularPlaces = CachedDataManagement.ConfigSimple_Get_Instance_ByConfigType(ConfigSimpleTypes.Search_Popular_Place);
                        if (popularPlaces?.Any() == true)
                        {
                            popularPlaces = popularPlaces.OrderBy(c => c.OrderNo).ThenByDescending(c => c.Id).ToList();
                            List<PopularPlaceModel> listPopularPlace = new();
                            foreach (var popularPlace in popularPlaces)
                            {
                                var obj = JsonHelper.DeserializeObject<PopularPlaceAddressModel>(popularPlace.MoreConfig) ?? new();
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
                        result.Characteristics = GetCharacteristics(RentalServiceItem);
                        #endregion
                        #region Owner
                        var owner = CachedDataManagement.UserLogin_Get_Instance_Id(RentalServiceItem.OwnerId);
                        #region Đếm số xe mà chủ xe sở hữu
                        int totalServiceCount = 0;
                        using (var repo = InitRepository())
                        {
                            totalServiceCount = CachedDataManagement.RentalServiceItems
                                .Where(c => c.OwnerId == owner.Id && c.IsApproved == true && !c.IsSuspended && !c.IsDeactive && !c.IsAddNew)
                                .Count();
                        }
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
                        #endregion
                        #region Feature
                        result.Features = GetFeatures(RentalServiceItem);
                        #endregion
                        #region Document / Security
                        GetDocumentAndSecurity(result, RentalServiceItem);
                        #endregion
                        #region CMS
                        var cms = CachedDataManagement.ConfigSimple_Get_Instance_ByConfigType(ConfigSimpleTypes.CMSContent_RentCar);
                        var cms_RentCarTerm = cms.FirstOrDefault(c => !c.IsDisable && c.Code == "RENT_CAR_TERM");
                        result.Term = cms_RentCarTerm?.MoreConfig;
                        var cms_CancelRentCarPolicy = cms.FirstOrDefault(c => !c.IsDisable && c.Code == "CANCEL_RENT_CAR_POLICY");
                        result.CancelBookingPolicy = GetCancelRentCarPolicy(cms_CancelRentCarPolicy?.MoreConfig);
                        var cms_Insurance = cms.FirstOrDefault(c => !c.IsDisable && c.Code == "RENT_CAR_INSURANCE");
                        result.InsuranceDescription = cms_Insurance?.MoreConfig;
                        var cms_Detail = CachedDataManagement.ConfigSimple_Get_Instance_ByConfigType(ConfigSimpleTypes.CMSContent_RentCarDetail);
                        DateTime nowClient = DateTime_Now_Client(), nowClientDate = nowClient.Date;
                        #region CancelBookingPolicyInfo
                        result.CancelBookingPolicyInfo = GetCMSContent_RentCarDetailInfo(cms_Detail, "Booking_Info_Cancel_Order");
                        var cancelOrderSetting = SettingJson.CancelOrderSetting;
                        if (result.CancelBookingPolicyInfo != null && cancelOrderSetting != null)
                        {
                            var description = result.CancelBookingPolicyInfo.Description;
                            if (!string.IsNullOrEmpty(description))
                            {
                                var hours = cancelOrderSetting.FullRefundWithinMinutes.Value / 60;
                                description = description.Replace("#FullRefundWithinHours#", ConvertToOneDecimalValue(hours));
                                var temp = fromDate.Value.AddDays(-cancelOrderSetting.NoRefundGreaterThanDays.Value).Date;
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
                                content = content.Replace("#FullRefundWithinHours#", ConvertToOneDecimalValue(cancelOrderSetting.FullRefundWithinMinutes.Value / 60m));
                                content = content.Replace("#RefundPercent#", Math.Round(100 - SettingJson.DepositPercent.Value, 0, MidpointRounding.AwayFromZero).ToString());
                                content = content.Replace("#NoRefundGreaterThanDays#", cancelOrderSetting.NoRefundGreaterThanDays.ToString());
                                content = content.Replace("#FullRefundWithinMinutes#", ConvertToOneDecimalValue(cancelOrderSetting.FullRefundWithinMinutes.Value));
                            }
                            result.CancelBookingPolicyInfo.Content = content;
                        }
                        #endregion
                        #region PaymentInstructionInfo
                        result.PaymentInstructionInfo = GetCMSContent_RentCarDetailInfo(cms_Detail, "Booking_Info_Payment");
                        if (result.PaymentInstructionInfo != null)
                        {
                            var description = result.PaymentInstructionInfo.Description;
                            var depositAmount = Math.Ceiling((totalPriceModel.TotalPrice * (SettingJson.DepositPercent ?? 30) / 100m) ?? 0);
                            double day = nowClient.Day, month = nowClient.Month, year = nowClient.Year;
                            description = description.Replace("#DepositMoney#", FormatMoney(depositAmount)).Replace("#RemainAmount#", FormatMoney(totalPriceModel.TotalPrice - depositAmount))
                                .Replace("#BookingDate#", $"ngày {day} tháng {month}, {year}");
                            day = fromDate.Value.Day; month = fromDate.Value.Month; year = fromDate.Value.Year;
                            description = description.Replace("#StartDate#", $"ngày {day} tháng {month}, {year}");
                            result.PaymentInstructionInfo.Description = description;
                        }
                        #endregion
                        #region GeneralProvisionInfo
                        var Booking_Info_General_Provision = "Booking_Info_General_Provision";
                        if (param.AppName == UIAppNames.sigoapp_new)
                            Booking_Info_General_Provision += "_App_New";
                        result.GeneralProvisionInfo = GetCMSContent_RentCarDetailInfo(cms_Detail, Booking_Info_General_Provision);
                        if (result.GeneralProvisionInfo != null &&
                            !string.IsNullOrEmpty(result.GeneralProvisionInfo.Content))
                        {
                            var g = RentalServiceHelper.GetEndTime(SettingJson.BusinessHourStart, SettingJson.BusinessHourEnd, UI_TimezoneOffset, SettingJson.HourOwner2Confirm);
                            var o = Convert.ToInt32((g - DateTime.UtcNow).TotalHours);
                            result.GeneralProvisionInfo.Content = result.GeneralProvisionInfo.Content.Replace("#RenterWaitingOwnerHours#", o.ToString());
                        }
                        #endregion
                        #endregion
                        #region Review
                        GetReview(result);
                        #endregion
                        #region Voucher
                        result.Vouchers = GetVouchers(param, out sMessage);
                        #endregion
                        #region RentalPriceDetails / TotalPrice
                        result.RentalPriceDetails = rentalPriceDetails;
                        result.RentalDayCount = GetRentalDayCountText(totalPriceModel.RentalDayCount);
                        result.TotalPrice = FormatMoney(totalPriceModel.TotalPrice);
                        #endregion
                        #region ExtraSurcharges
                        result.ExtraSurcharges = GetExtraSurcharges(new() { RentalDayCount = totalPriceModel.RentalDayCount, FromDate = param.FromDate, ToDate = param.ToDate });
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
                                ins.CoverageStartDate <= ins.CoverageEndDate && ins.CoverageStartDate <= now && ins.CoverageEndDate >= now)
                            {
                                insuranceName = "từ chủ xe";
                            }
                        }
                        if (insuranceName == "VNI")
                        {
                            var cms_Booking_Info_Insurance_By_VNI = cms.FirstOrDefault(c => c.Code == CMSKeys.Booking_Info_Insurance_By_VNI);
                            result.InsuranceInfo = new()
                            {
                                Title = cms_Booking_Info_Insurance_By_VNI.Name?.Replace("#InsuranceName#", insuranceName),
                                Description = cms_Booking_Info_Insurance_By_VNI.ColorCode,
                                Content = cms_Booking_Info_Insurance_By_VNI.MoreConfig,
                                Icon = cms_Booking_Info_Insurance_By_VNI.IconUrl,
                            };
                        }
                        else
                        {
                            var cms_Booking_Info_Insurance_By_Owner = cms.FirstOrDefault(c => c.Code == CMSKeys.Booking_Info_Insurance_By_Owner);
                            result.InsuranceInfo = new()
                            {
                                Title = cms_Booking_Info_Insurance_By_Owner.Name?.Replace("#InsuranceName#", insuranceName),
                                Description = cms_Booking_Info_Insurance_By_Owner.ColorCode,
                                Content = cms_Booking_Info_Insurance_By_Owner.MoreConfig,
                                Icon = cms_Booking_Info_Insurance_By_Owner.IconUrl
                            };
                        }
                        #region BookingFeatureInfos
                        var bookingFeatureInfos = CachedDataManagement.ConfigSimple_Get_Instance_ByConfigType(ConfigSimpleTypes.Booking_Feature_Info)
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
                                        var securities = result.Securities.Select(c => ((c.Text ?? string.Empty) + " " + (c.Description ?? string.Empty)).Trim()).ToArray();
                                        t.Description = string.Join("<br>", securities);
                                    }
                                }
                                return t;
                            }).ToArray();
                        #endregion
                        #endregion
                        #region ThingsToKnow
                        result.ThingsToKnow = RentCarHelper.GetThingsToKnow(cms_Detail, SettingJson, RentalServiceItem.Vehicle.VehicleNoOfSeatId);
                        #endregion
                        #region Log View
                        if (totalPriceModel.IsHostAddress)
                            totalPriceModel.DeliveryAddress = RentalServiceItem.HostAddress.Detail;
                        else totalPriceModel.DeliveryAddress = param.DeliveryAddress;
                        RentCarHelper.CreateLogView(GSCurrentUser, RentalServiceItem, totalPriceModel, param, CarViewFromCodes.Detail);
                        #endregion
                        #region ReportUserReasonList
                        var rpReasons = CachedDataManagement.ConfigReportUserReasons.Where(c => !string.IsNullOrEmpty(c.Name) && !c.IsDisable);
                        result.ReportUserReasonList = rpReasons.Select(c => new BaseCategoryShowExtItem()
                        {
                            Text = c.Name,
                            Value = c.Id.ToString(),
                            NeedShowExt = c.ShowExtReason ?? false
                        }).ToArray();
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
            return result;
        }
        public static BaseCategoryInfoItem GetCMSContent_RentCarDetailInfo(List<ConfigSimpleInfo> infos, string code)
        {
            BaseCategoryInfoItem result = null;
            var info = infos.FirstOrDefault(c => c.Code == code);
            if (info != null)
            {
                result = new() { Title = info.Name, Description = info.ColorCode, Content = info.MoreConfig, Icon = info.IconUrl };
            }
            return result;
        }
        public CheckBeforeUpdateBookingInfoResultModel CheckBeforeUpdateBookingInfo(TParam param, out string sMessage)
        {
            sMessage = string.Empty;
            CheckBeforeUpdateBookingInfoResultModel result = new()
            {
                IsValid = true,
                ErrorData = new()
            };
            try
            {
                if (param.FromDate != null)
                {
                    DateTime? fromDateUTC = DateTime_ToServer(param.FromDate),
                                        toDateUTC = null;
                    DateTime? fromDate = fromDateUTC.Value.AddMinutes(-UI_TimezoneOffset.Value).Date,
                        toDate = null,
                        nowDateClient = DateTime_Now_Client().Date;

                    bool isMinimumRentalDayRequired_DontShowWarningMsg = false;
                    if (param.ToDate == null)
                    {
                        var setting = RentalServiceHelper.GetAppSetting();
                        param.ToDate = DateTime_ToClient(fromDateUTC.Value.AddHours(setting.ToTimeAddHour ?? 1));
                        isMinimumRentalDayRequired_DontShowWarningMsg = true;
                    }

                    toDateUTC = DateTime_ToServer(param.ToDate);
                    toDate = toDateUTC.Value.AddMinutes(-UI_TimezoneOffset.Value).Date;

                    SetRentalServiceItem(param);
                    var totalPriceModel = GetTotalPriceModel(param);
                    string error = CheckCanSearchRentalService(param);
                    if (result.IsValid)
                    {
                        #region Kiểm tra lịch thuê tối thiểu
                        ServiceItem_MinimumRentalDayRequired[] items = null;
                        using (var repo = DC_CreateRepository<ServiceItem_MinimumRentalDayRequired>())
                        {
                            items = repo.GetQueryable().Where(c => c.RentalServiceItemId != null && RentalServiceItem.Id == c.RentalServiceItemId &&
                            (c.EffectiveBeforeDate == null || nowDateClient < c.EffectiveBeforeDate) &&
                            c.FromDate <= toDate && c.ToDate >= fromDate
                            ).ToArray();
                        }
                        if (items?.Any() == true)
                        {
                            var max = items.Max(c => c.MinimumRequiredRentalDays ?? 0);
                            if (totalPriceModel.RentalDayCount < max)
                            {
                                result.IsValid = false;
                                var msgInCalendar = TextDisplayHelper.GetValue("msg_minimum_required_rental_days_update_booking_info_msg_in_calendar", "Tối thiểu #MinimumRequiredRentalDays#");
                                var title = TextDisplayHelper.GetValue("msg_minimum_required_rental_days_update_booking_info_title", "Thời gian tối thuê tối thiểu: #MinimumRequiredRentalDays#");
                                var description = TextDisplayHelper.GetValue("msg_minimum_required_rental_days_update_booking_info_description", "Ngày này không đáp ứng các yêu cầu về số ngày thuê tối thiểu");
                                result.InvalidCode = "MinimumRentalDayRequired";
                                result.ErrorData.Add("MinimumRentalDayRequired", new
                                {
                                    ValidToDate = fromDate.Value.AddDays(max).ToString("yyyy-MM-dd"),
                                    MsgInCalendar = msgInCalendar.Replace("#MinimumRequiredRentalDays#", max.ToString()),
                                    WarningMsg = isMinimumRentalDayRequired_DontShowWarningMsg ? null : new
                                    {
                                        Title = title.Replace("#MinimumRequiredRentalDays#", max.ToString()),
                                        Description = description
                                    }
                                });
                            }
                        }
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                SaveLogException(ex, "CheckBeforeUpdateBookingInfo", param);
            }
            return result;
        }
        public override RentalServiceBookingInfoModel UpdateBookingInfo(TParam param, out string sMessage)
        {
            RentalServiceBookingInfoModel result = new();
            try
            {
                bool isUserTakeSelected = false;
                if (param.AppName == UIAppNames.sigoweb || param.AppName == UIAppNames.sigoapp_new)
                {
                    if (param.DeliveryInfo != null)
                    {
                        var deliveryInfo = param.DeliveryInfo;
                        if (deliveryInfo.IsUserTakeSelected)
                        {
                            isUserTakeSelected = deliveryInfo.IsUserTakeSelected;
                        }
                        else
                        {
                            param.DeliveryAddress = deliveryInfo.OwnerShipDeliveryAddress;
                            param.Latitude = deliveryInfo.OwnerShipLat;
                            param.Longitude = deliveryInfo.OwnerShipLng;
                        }
                    }
                    else if (string.IsNullOrEmpty(param.DeliveryAddress))
                    {
                        Uri myUri = new(param.Url.ToLower());
                        param.DeliveryAddress = HttpUtility.ParseQueryString(myUri.Query).Get("from");
                        param.Latitude = (decimal?)EzyObjectHelper.ConvertFromString(HttpUtility.ParseQueryString(myUri.Query).Get("lat"), typeof(decimal?));
                        param.Longitude = (decimal?)EzyObjectHelper.ConvertFromString(HttpUtility.ParseQueryString(myUri.Query).Get("long"), typeof(decimal?));
                    }
                }
                sMessage = CheckGetDetailParam(param);
                if (string.IsNullOrEmpty(sMessage))
                {
                    SetRentalServiceItem(param);
                    if (RentalServiceItem != null)
                    {
                        sMessage = CheckDiscountCodeCanUse(param.VoucherCode);
                        if (string.IsNullOrEmpty(sMessage))
                        {
                            sMessage = CheckRentalServiceNotBusy(param);
                            if (string.IsNullOrEmpty(sMessage))
                            {
                                param.NeedGetCriteriaPoint = true;
                                if (isUserTakeSelected)
                                {
                                    param.Latitude = RentalServiceItem.HostAddress.Latitude;
                                    param.Longitude = RentalServiceItem.HostAddress.Longitude;
                                }
                                var totalPriceModel = GetTotalPriceModel(param);
                                if (string.IsNullOrEmpty(totalPriceModel.Error))
                                {
                                    DateTime? fromDate = DateTime_To_ClientDateTime(param.FromDate),
                                    toDate = DateTime_To_ClientDateTime(param.ToDate);
                                    result.RentalDate = RentalServiceHelper.GetRentalDate(fromDate.Value, toDate.Value);
                                    result.RentalPriceDetails = GetRentalPriceDetails(totalPriceModel, result.RentalDate);
                                    result.ExtraSurcharges = GetExtraSurcharges(new() { RentalDayCount = totalPriceModel.RentalDayCount, FromDate = param.FromDate, ToDate = param.ToDate });
                                    result.BookingInfoItems = result.ExtraSurcharges;
                                    result.TotalPrice = FormatMoney(totalPriceModel.TotalPrice);
                                    result.VoucherCode = param.VoucherCode;
                                    result.RentalPriceOriginal = FormatMoney(totalPriceModel.TotalPrice + totalPriceModel.TotalPromotionMoney);
                                    result.RentalPrice = FormatMoney(totalPriceModel.TotalPrice);
                                    result.RentalDayCount = $"{totalPriceModel.RentalDayCount} ngày";
                                    result.RentalPriceByDayOriginal = FormatMoney(Math.Round(((totalPriceModel.TotalOriginalPrice) / totalPriceModel.RentalDayCount).Value));
                                    result.RentalPriceByDay = FormatMoney(Math.Round(((totalPriceModel.TotalOriginalPrice - totalPriceModel.TotalPromotionMoney) / totalPriceModel.RentalDayCount).Value));
                                    var discountMoney = totalPriceModel.DiscountMoney;
                                    if (string.IsNullOrEmpty(sMessage))
                                        result.VoucherDiscountPrice = FormatMoney(discountMoney);
                                    if ((param.AppName == UIAppNames.sigoweb || param.AppName == UIAppNames.sigoapp_new) && param.DeliveryInfo != null)
                                    {
                                        string ownerShipAddress = param.DeliveryInfo.OwnerShipDeliveryAddress;
                                        decimal? ownerShipLat = param.DeliveryInfo.OwnerShipLat, ownerShipLng = param.DeliveryInfo.OwnerShipLng;
                                        result.RentalPriceDetails = GetDeliveryInfo_Web(param, param.DeliveryInfo.UserSearchLat, param.DeliveryInfo.UserSearchLng, out sMessage, result.RentalPriceDetails, out RentalServiceDeliveryAddress deliveryInfo, isUserTakeSelected);
                                        result.DeliveryInfo = deliveryInfo;
                                        result.DeliveryFee = deliveryInfo.OwnerShipDeliveryFee;
                                        if (isUserTakeSelected)
                                        {
                                            result.DeliveryAddress = deliveryInfo.UserTakeDeliveryAddress;
                                            result.Latitude = deliveryInfo.UserTakeLat;
                                            result.Longitude = deliveryInfo.UserTakeLng;
                                            deliveryInfo.OwnerShipDeliveryAddress = ownerShipAddress;
                                            deliveryInfo.OwnerShipLat = ownerShipLat;
                                            deliveryInfo.OwnerShipLng = ownerShipLng;
                                        }
                                        else
                                        {
                                            result.DeliveryAddress = deliveryInfo.OwnerShipDeliveryAddress;
                                            result.Latitude = deliveryInfo.OwnerShipLat;
                                            result.Longitude = deliveryInfo.OwnerShipLng;
                                        }
                                    }
                                    else
                                    {
                                        var deliveryFee = result.RentalPriceDetails?.FirstOrDefault(c => c.Code == CMSKeys.BookingInfo_DeliveryFee);
                                        if (deliveryFee != null && deliveryFee.Price == 0)
                                            result.RentalPriceDetails = result.RentalPriceDetails.Where(c => c.Code != CMSKeys.BookingInfo_DeliveryFee).ToArray();
                                        if (totalPriceModel.DeliveryFee != null)
                                        {
                                            result.DeliveryFee = FormatMoneyDetail(totalPriceModel.DeliveryFee.Value);
                                        }
                                        result.DeliveryAddress = param.DeliveryAddress;
                                        result.Latitude = param.Latitude;
                                        result.Longitude = param.Longitude;
                                    }
                                    if (result != null)
                                    {
                                        result.CanBooking = (GSCurrentUser == null || GSCurrentUser.Id != RentalServiceItem.OwnerId) &&
                                            RentalServiceItem.IsApproved == true && !RentalServiceItem.IsAddNew && !RentalServiceItem.IsDeactive && !RentalServiceItem.IsSuspended;
                                    }
                                    var cms_Detail = CachedDataManagement.ConfigSimple_Get_Instance_ByConfigType(ConfigSimpleTypes.CMSContent_RentCarDetail);
                                    DateTime nowClient = DateTime_Now_Client(), nowClientDate = nowClient.Date;
                                    #region CancelBookingPolicyInfo
                                    result.CancelBookingPolicyInfo = GetCMSContent_RentCarDetailInfo(cms_Detail, "Booking_Info_Cancel_Order");
                                    var cancelOrderSetting = SettingJson.CancelOrderSetting;
                                    if (result.CancelBookingPolicyInfo != null && cancelOrderSetting != null)
                                    {
                                        var description = result.CancelBookingPolicyInfo.Description;
                                        if (!string.IsNullOrEmpty(description))
                                        {
                                            var hours = cancelOrderSetting.FullRefundWithinMinutes.Value / 60;
                                            description = description.Replace("#FullRefundWithinHours#", ConvertToOneDecimalValue(hours));
                                            var temp = fromDate.Value.AddDays(-cancelOrderSetting.NoRefundGreaterThanDays.Value).Date;
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
                                            content = content.Replace("#FullRefundWithinHours#", ConvertToOneDecimalValue(cancelOrderSetting.FullRefundWithinMinutes.Value / 60m));
                                            content = content.Replace("#RefundPercent#", Math.Round(100 - SettingJson.DepositPercent.Value, 0, MidpointRounding.AwayFromZero).ToString());
                                            content = content.Replace("#NoRefundGreaterThanDays#", cancelOrderSetting.NoRefundGreaterThanDays.ToString());
                                            content = content.Replace("#FullRefundWithinMinutes#", ConvertToOneDecimalValue(cancelOrderSetting.FullRefundWithinMinutes.Value));
                                        }
                                        result.CancelBookingPolicyInfo.Content = content;
                                    }
                                    #endregion
                                    #region PaymentInstructionInfo
                                    result.PaymentInstructionInfo = GetCMSContent_RentCarDetailInfo(cms_Detail, "Booking_Info_Payment");
                                    if (result.PaymentInstructionInfo != null)
                                    {
                                        var description = result.PaymentInstructionInfo.Description;
                                        var depositAmount = Math.Ceiling((totalPriceModel.TotalPrice * (SettingJson.DepositPercent ?? 30) / 100m) ?? 0);
                                        double day = nowClient.Day, month = nowClient.Month, year = nowClient.Year;
                                        description = description.Replace("#DepositMoney#", FormatMoney(depositAmount)).Replace("#RemainAmount#", FormatMoney(totalPriceModel.TotalPrice - depositAmount))
                                            .Replace("#BookingDate#", $"ngày {day} tháng {month}, {year}");
                                        day = fromDate.Value.Day; month = fromDate.Value.Month; year = fromDate.Value.Year;
                                        description = description.Replace("#StartDate#", $"ngày {day} tháng {month}, {year}");
                                        result.PaymentInstructionInfo.Description = description;
                                    }
                                    #endregion
                                }
                                else sMessage = totalPriceModel.Error;
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(sMessage))
                {
                    if (param.DeliveryInfo != null)
                    {
                        result = new() { DeliveryInfo = param.DeliveryInfo };
                        result.DeliveryAddress = param.DeliveryInfo.UserSearchAddress;
                        result.Latitude = param.DeliveryInfo.UserSearchLat;
                        result.Longitude = param.DeliveryInfo.UserSearchLng;
                    }
                }
            }
            catch (Exception ex)
            {
                SaveLogException(ex, "UpdateBookingInfo", param);
                sMessage = Exception_GetMessage(ex);
            }
            return result;
        }
        private RentalServiceBookingPriceModel[] GetDeliveryInfo_Web(TParam param, decimal? userSearchLat, decimal? userSearchLng, out string sMessage, RentalServiceBookingPriceModel[] rentalPriceDetails, out RentalServiceDeliveryAddress deliveryInfo, bool isUserTakeSelected)
        {
            sMessage = string.Empty;
            var vehicle_RentalSetting = RentalServiceItem.Vehicle_RentalSetting;
            bool isOwnerAddress = true, canChangeDeliveryAddress = vehicle_RentalSetting.HaveDeliverySurcharge ?? false, haveDeliveryFee = false, ownerShipCanSelect = true;
            var address = RentalServiceItem.HostAddress;
            RentalServiceItem.OwnerAddress = RentalServiceHelper.GetOwnerAddress_Type_RENT_CAR(null, address);
            var deliveryFee = rentalPriceDetails?.FirstOrDefault(c => c.Code == CMSKeys.BookingInfo_DeliveryFee);
            deliveryInfo = new()
            {
                OwnerShipCanSelect = true,
                IsUserTakeSelected = param.DeliveryInfo?.IsUserTakeSelected ?? false,
                UserSearchAddress = param.DeliveryInfo?.UserSearchAddress,
                UserSearchLat = param.DeliveryInfo?.UserSearchLat,
                UserSearchLng = param.DeliveryInfo?.UserSearchLng,
            };
            #region Tự đến lấy
            deliveryInfo.UserTakeDeliveryAddress = RentalServiceItem.OwnerAddress;
            deliveryInfo.UserTakeLat = address.Latitude;
            deliveryInfo.UserTakeLng = address.Longitude;
            var (dUserTake, _) = DistanceMatrixHelper.GetDistance_Cached(userSearchLat, userSearchLng, address.Latitude, address.Longitude).GetAwaiter().GetResult();
            if (dUserTake != null)
            {
                string sDis = string.Empty;
                if (dUserTake.Distance > 1000) sDis = ConvertToOneDecimalValue(dUserTake.Distance.Value / 1000) + "km";
                else sDis = ConvertToOneDecimalValue(dUserTake.Distance.Value) + "m";
                deliveryInfo.UserTakeDistance = sDis;
                RentalServiceItem.Distance = dUserTake.Distance;
            }
            #endregion
            #region Giao xe tận nơi
            if (canChangeDeliveryAddress)
            {
                var (dOwnerShip, error) = DistanceMatrixHelper.GetDistance_Cached(param.Latitude, param.Longitude, address.Latitude, address.Longitude).GetAwaiter().GetResult();
                sMessage = error;
                if (vehicle_RentalSetting.MaximumDeliveryMileage > 0)
                {
                    if (string.IsNullOrEmpty(sMessage) && dOwnerShip != null)
                    {
                        if (dOwnerShip.Distance <= vehicle_RentalSetting.MaximumDeliveryMileage * 1000)
                        {
                            haveDeliveryFee = true;
                            isOwnerAddress = false;
                        }
                        else
                        {
                            deliveryInfo.OwnerShipDeliveryAddress = TextDisplayHelper.GetValue(TextDisplayKeys.msg_error_delivery_distance_exceed_maximum, "Vượt quá khoảng cách chủ xe hỗ trợ giao xe tận nơi cho bạn");
                            deliveryInfo.OwnerShipLat = param.Latitude;
                            deliveryInfo.OwnerShipLng = param.Longitude;
                            deliveryInfo.OwnerShipDeliveryAddress = deliveryInfo.OwnerShipDeliveryAddress.Replace("#Distance#", ConvertMToKM(vehicle_RentalSetting.MaximumDeliveryMileage));
                            deliveryInfo.IsInvalidOwnerShipAddress = true;
                        }
                    }
                }
                else
                {
                    ownerShipCanSelect = false;
                }
            }
            else ownerShipCanSelect = false;
            if (!ownerShipCanSelect)
            {
                deliveryInfo.OwnerShipCanSelect = false;
                deliveryInfo.OwnerShipDeliveryAddress = "Dịch vụ không hỗ trợ giao xe tận nơi";
                deliveryInfo.IsInvalidOwnerShipAddress = true;
            }
            if (isOwnerAddress)
            {
                RentalServiceItem.DeliveryAddress = RentalServiceItem.OwnerAddress;
                RentalServiceItem.DeliveryLat = address.Latitude;
                RentalServiceItem.DeliveryLng = address.Longitude;
            }
            else
            {
                RentalServiceItem.DeliveryAddress = param.DeliveryAddress;
                RentalServiceItem.DeliveryLat = param.Latitude;
                RentalServiceItem.DeliveryLng = param.Longitude;
            }
            #endregion
            #region Bổ sung Giao xe tận nơi
            if (haveDeliveryFee)
            {
                deliveryInfo.OwnerShipDeliveryAddress = param.DeliveryAddress;
                deliveryInfo.OwnerShipLat = param.Latitude;
                deliveryInfo.OwnerShipLng = param.Longitude;
            }
            if (deliveryFee != null)
            {
                if (deliveryFee.Price == 0 || isUserTakeSelected) rentalPriceDetails = rentalPriceDetails.Where(c => c.Code != CMSKeys.BookingInfo_DeliveryFee).ToArray();
                if (haveDeliveryFee)
                    deliveryInfo.OwnerShipDeliveryFee = deliveryFee.PriceText;
            }
            #endregion
            return rentalPriceDetails;
        }
        /// <summary>
        /// Dùng được cho GetDetail thôi, còn khi UpdateBookingInfo ở web không dùng được nữa
        /// </summary>
        /// <param name="param"></param>
        /// <param name="sMessage"></param>
        /// <param name="rentalPriceDetails"></param>
        /// <param name="deliveryInfo"></param>
        /// <returns></returns>
        private RentalServiceBookingPriceModel[] GetDeliveryInfo(TParam param, out string sMessage, RentalServiceBookingPriceModel[] rentalPriceDetails, out RentalServiceDeliveryAddress deliveryInfo)
        {
            var vehicle_RentalSetting = RentalServiceItem.Vehicle_RentalSetting;
            bool isOwnerAddress = true, canChangeDeliveryAddress = vehicle_RentalSetting.HaveDeliverySurcharge ?? false, haveDeliveryFee = false, ownerShipCanSelect = true;
            var address = RentalServiceItem.HostAddress;
            RentalServiceItem.OwnerAddress = RentalServiceHelper.GetOwnerAddress_Type_RENT_CAR(null, address);
            var deliveryFee = rentalPriceDetails?.FirstOrDefault(c => c.Code == CMSKeys.BookingInfo_DeliveryFee);
            deliveryInfo = new()
            {
                OwnerShipCanSelect = true,
            };
            #region Tự đến lấy
            deliveryInfo.UserTakeDeliveryAddress = RentalServiceItem.OwnerAddress;
            deliveryInfo.UserTakeLat = address.Latitude;
            deliveryInfo.UserTakeLng = address.Longitude;
            if (param.Latitude == null || param.Longitude == null)
            {
                deliveryInfo.UserSearchAddress = RentalServiceItem.OwnerAddress;
                deliveryInfo.UserSearchLat = address.Latitude;
                deliveryInfo.UserSearchLng = address.Longitude;
            }
            else
            {
                deliveryInfo.UserSearchAddress = param.DeliveryAddress;
                deliveryInfo.UserSearchLat = param.Latitude;
                deliveryInfo.UserSearchLng = param.Longitude;
            }
            var (dUserTake, error) = DistanceMatrixHelper.GetDistance_Cached(param.Latitude, param.Longitude, address.Latitude, address.Longitude).GetAwaiter().GetResult();
            sMessage = error;
            if (dUserTake != null)
            {
                string sDis = string.Empty;
                if (dUserTake.Distance > 1000) sDis = ConvertToOneDecimalValue(dUserTake.Distance.Value / 1000) + "km";
                else sDis = ConvertToOneDecimalValue(dUserTake.Distance.Value) + "m";
                deliveryInfo.UserTakeDistance = sDis;
                RentalServiceItem.Distance = dUserTake.Distance;
            }
            #endregion
            #region Giao xe tận nơi
            if (canChangeDeliveryAddress)
            {
                // Chỗ này sẽ lấy theo khoảng cách khách đến chủ xe luôn
                var dOwnerShip = dUserTake;
                if (vehicle_RentalSetting.MaximumDeliveryMileage > 0)
                {
                    if (string.IsNullOrEmpty(sMessage) && dOwnerShip != null)
                    {
                        if (dOwnerShip.Distance <= vehicle_RentalSetting.MaximumDeliveryMileage * 1000)
                        {
                            haveDeliveryFee = true;
                            isOwnerAddress = false;
                        }
                        else
                        {
                            deliveryInfo.OwnerShipDeliveryAddress = TextDisplayHelper.GetValue(TextDisplayKeys.msg_error_delivery_distance_exceed_maximum, "Vượt quá khoảng cách chủ xe hỗ trợ giao xe tận nơi cho bạn");
                            deliveryInfo.OwnerShipLat = param.Latitude;
                            deliveryInfo.OwnerShipLng = param.Longitude;
                            deliveryInfo.OwnerShipDeliveryAddress = deliveryInfo.OwnerShipDeliveryAddress.Replace("#Distance#", ConvertMToKM(vehicle_RentalSetting.MaximumDeliveryMileage));
                            deliveryInfo.IsInvalidOwnerShipAddress = true;
                        }
                    }
                }
                else
                {
                    ownerShipCanSelect = false;
                }
            }
            else ownerShipCanSelect = false;
            if (!ownerShipCanSelect)
            {
                deliveryInfo.OwnerShipCanSelect = false;
                deliveryInfo.OwnerShipDeliveryAddress = "Dịch vụ không hỗ trợ giao xe tận nơi";
                deliveryInfo.IsInvalidOwnerShipAddress = true;
            }
            if (isOwnerAddress)
            {
                RentalServiceItem.DeliveryAddress = RentalServiceItem.OwnerAddress;
                RentalServiceItem.DeliveryLat = address.Latitude;
                RentalServiceItem.DeliveryLng = address.Longitude;
            }
            else
            {
                RentalServiceItem.DeliveryAddress = param.DeliveryAddress;
                RentalServiceItem.DeliveryLat = param.Latitude;
                RentalServiceItem.DeliveryLng = param.Longitude;
            }
            #endregion
            #region Bổ sung Giao xe tận nơi
            if (haveDeliveryFee)
            {
                deliveryInfo.OwnerShipDeliveryAddress = param.DeliveryAddress;
                deliveryInfo.OwnerShipLat = param.Latitude;
                deliveryInfo.OwnerShipLng = param.Longitude;
            }
            else deliveryInfo.IsUserTakeSelected = true;
            if (deliveryFee != null)
            {
                if (deliveryFee.Price == 0) rentalPriceDetails = rentalPriceDetails.Where(c => c.Code != CMSKeys.BookingInfo_DeliveryFee).ToArray();
                if (haveDeliveryFee)
                    deliveryInfo.OwnerShipDeliveryFee = deliveryFee.PriceText;
            }
            #endregion
            return rentalPriceDetails;
        }
        #endregion

        #region Log lại khi user nhấn nút Đặt xe ngay
        public void SaveLog_UserClick_RentCar(TParam param)
        {
            UserActionHistoryHelper.LogUserActionHistory_RentCar(GSCurrentUser, param.Id, param.IPAddress, "Detail", "RentCar", param.AppName);
        }
        #endregion
    }
}
