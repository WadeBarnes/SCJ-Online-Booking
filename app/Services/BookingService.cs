using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SCJ.Booking.MVC.Data;
using SCJ.Booking.MVC.Models;
using SCJ.Booking.MVC.Utils;
using SCJ.Booking.MVC.ViewModels;
using SCJ.SC.OnlineBooking;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace SCJ.Booking.MVC.Services
{
    public class BookingService
    {
        //Const
        private const int MaxHearingsPerDay = 250;

        // DB Context
        private readonly ApplicationDbContext _dbContext;

        //Http Context
        private readonly HttpContext _httpContext;

        //Environment
        private readonly bool _isLocalDevEnvironment;

        //Init error logger
        private readonly Logger _logger;

        //Constructor
        public BookingService(ApplicationDbContext dbContext, IHttpContextAccessor httpAccessor)
        {
            //setup error logger settings
            _logger = new LoggerConfiguration()
                .WriteTo.Console(LogEventLevel.Error)
                .CreateLogger();

            //DB Context setup
            _dbContext = dbContext;

            //HttpContext
            _httpContext = httpAccessor.HttpContext;

            //test the environment
            string tagName = Environment.GetEnvironmentVariable("TAG_NAME") ?? "";

            if (tagName.ToLower().Equals("localdev"))
            {
                _isLocalDevEnvironment = true;
            }
        }


        /// <summary>
        ///     Populate the dropdown list for locations for the search
        /// </summary>
        public async Task<CaseSearchViewModel> LoadForm(IOnlineBooking client)
        {
            //Load locations from API
            Location[] locationsAsync = await client.getLocationsAsync();

            //Model instance
            return new CaseSearchViewModel
            {
                Registry = new SelectList(
                    locationsAsync.Select(x => new {Id = x.locationID, Value = x.locationName}),
                    "Id", "Value")
            };
        }


        /// <summary>
        ///     Search for available times
        /// </summary>
        public async Task<CaseSearchViewModel> GetResults(CaseSearchViewModel model,
            IOnlineBooking client, int hearingId, int hearingLength)
        {
            // Load locations from API
            Location[] locationsAsync = await client.getLocationsAsync();

            var retval = new CaseSearchViewModel
            {
                Registry = new SelectList(
                    locationsAsync.Select(x => new {Id = x.locationID, Value = x.locationName}),
                    "Id", "Value"),
                ConferenceType = model.ConferenceType,
                SelectedRegistryId = model.SelectedRegistryId,
                CaseNumber = model.CaseNumber,
                TimeslotExpired = model.TimeslotExpired
            };

            //search the current case number
            if (await client.caseNumberValidAsync(await BuildCaseNumber(model.CaseNumber,
                    model.SelectedRegistryId, client)) == 0)
            {
                //case could not be found
                retval.IsValidCaseNumber = false;

                //empty result set
                retval.Results = new AvailableDatesByLocation();
            }
            else
            {
                //valid case number
                retval.IsValidCaseNumber = true;

                retval.Results =
                    await client.AvailableDatesByLocationAsync(
                        Convert.ToInt32(model.SelectedRegistryId), hearingId);

                //set location name
                SelectListItem selectedRegistry =
                    retval.Registry.FirstOrDefault(x =>
                        x.Value == retval.SelectedRegistryId.ToString());

                if (selectedRegistry != null)
                {
                    retval.SelectedRegistryName = selectedRegistry.Text;
                }

                //check for valid date
                if (model.ContainerId > 0)
                {
                    if (!await IsTimeStillAvailable(model.ContainerId, model.SelectedRegistryId,
                        hearingId, client))
                    {
                        retval.TimeslotExpired = true;
                    }

                    //convert JS ticks to .Net date
                    var dt = new DateTime(Convert.ToInt64(model.SelectedCaseDate));

                    //set date properties
                    retval.ContainerId = model.ContainerId;
                    retval.SelectedCaseDate = model.SelectedCaseDate;
                    retval.TimeslotFriendlyName =
                        dt.ToString("MMMM dd") + " from " + dt.ToString("hh:mm tt") + " to " +
                        dt.AddMinutes(hearingLength).ToString("hh:mm tt");
                }
            }

            return retval;
        }


        /// <summary>
        ///     Check if a time slot is still available for a court booking
        /// </summary>
        public async Task<bool> IsTimeStillAvailable(int containerId, int locationId, int hearingId,
            IOnlineBooking client)
        {
            //get available times for the location
            AvailableDatesByLocation locationsAvailable =
                await client.AvailableDatesByLocationAsync(locationId, hearingId);

            //check if the container ID is still available
            return locationsAvailable.AvailableDates.Any(x => x.ContainerID == containerId);
        }


        /// <summary>
        ///     Fetch location-code for specific case ID
        /// </summary>
        public async Task<string> BuildCaseNumber(string caseId, int locationId,
            IOnlineBooking client)
        {
            //load all locations
            Location[] locations = await client.getLocationsAsync();

            //fetch location prefix
            string locationPrefix =
                locations.FirstOrDefault(x => x.locationID == locationId)?.locationCode;

            //return location prefix + case number
            return locationPrefix + caseId;
        }


        /// <summary>
        ///     Fetch the location name based on the location ID
        /// </summary>
        public async Task<string> GetLocationName(int locationId, IOnlineBooking client)
        {
            //load all locations
            Location[] locations = await client.getLocationsAsync();

            //get location name
            string locationName =
                locations.FirstOrDefault(x => x.locationID == locationId)?.locationName;

            // append "Law Courts" to location names
            return locationName + " Law Courts";
        }


        /// <summary>
        ///     Fetch the location name based on the location ID
        /// </summary>
        public async Task<int> GetLocationHearingLength(int locationId, int hearingTypeId,
            IOnlineBooking client)
        {
            //get the available dates object which contains the hearing length
            AvailableDatesByLocation availableDatesByLocation =
                await client.AvailableDatesByLocationAsync(locationId, hearingTypeId);

            //return the booking length
            return availableDatesByLocation.BookingDetails.detailBookingLength;
        }


        /// <summary>
        ///     Book court case
        /// </summary>
        public async Task<CaseConfirmViewModel> BookCourtCase(CaseConfirmViewModel model,
            IOnlineBooking client, int hearingId, int hearingLength, string userId)
        {
            //if the user could not be detected return 
            if (string.IsNullOrWhiteSpace(userId))
            {
                model.IsUserKnown = false;
                return model;
            }

            //we know who the user is.
            model.IsUserKnown = true;

            //ensure time slot is still available
            if (await IsTimeStillAvailable(model.ContainerId, model.LocationId, hearingId,
                client))
            {
                //build object to send to the API
                var bhi = new BookHearingInfo
                {
                    caseID = Convert.ToInt32(Regex.Replace(model.CaseNumber, "[A-Za-z ]", "")),
                    containerID = model.ContainerId,
                    dateTime = model.FullDate,
                    hearingLength = hearingLength,
                    locationID = model.LocationId,
                    requestedBy = "USER",
                    hearingTypeId =
                        (int) Enums.ConferenceHearingType.TRIAL_MANAGEMENT_CONFERENCE
                };

                //submit booking
                BookingHearingResult result = await client.BookingHearingAsync(bhi);

                //test to see if the booking was successful
                if (result.bookingResult.ToLower().StartsWith("success"))
                {
                    //create database entry
                    DbSet<BookingHistory> bookingInfo = _dbContext.Set<BookingHistory>();

                    bookingInfo.Add(new BookingHistory
                    {
                        ContainerId = model.ContainerId, SmGovUserGuid = userId,
                        Timestamp = DateTime.Now
                    });

                    //save to DB
                    _dbContext.SaveChanges();

                    //update model
                    model.IsBooked = true;
                }
                else
                {
                    model.IsBooked = false;
                }
            }
            else
            {
                //The booking is not available anymore
                //user needs to choose a new time slot
                model.IsTimeslotAvailable = false;
                model.IsBooked = false;
            }

            return model;
        }


        /// <summary>
        ///     Get the number of hearings left for the day
        /// </summary>
        /// <returns></returns>
        public HtmlString GetHearingsRemaining()
        {
            int hearingsRemaining = GetUserHearingsTotalRemaining();

            switch (hearingsRemaining)
            {
                case MaxHearingsPerDay:
                    return new HtmlString($"You can book {MaxHearingsPerDay} hearings today.");
                case 1:
                    return new HtmlString("You can book 1 more hearing today.");
                case 0:
                    return new HtmlString("You cannot book anymore hearings today.");
                default:
                    return new HtmlString($"You can book {hearingsRemaining} more hearings today.");
            }
        }

        /// <summary>
        ///     Read the database and get the total number of hearings left for the day
        /// </summary>
        public int GetUserHearingsTotalRemaining()
        {
            //get user GUID
            string uGuid;

            if (!_isLocalDevEnvironment)
            {
                //try and read the header
                if (_httpContext.Request.Headers.ContainsKey("SMGOV-USERGUID"))
                {
                    uGuid = _httpContext.Request.Headers["SMGOV-USERGUID"].ToString();
                }
                else
                {
                    return MaxHearingsPerDay;
                }
            }
            else
            {
                //Dummy user guid
                uGuid = "B8C1EC79-6464-4C62-BF33-05FC00CC21A0";
            }

            //today's date
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);

            //get all entries for logged-in user
            //booked on today
            List<BookingHistory> hearingsBookedForToday = _dbContext.BookingHistory
                .Where(b => b.SmGovUserGuid == uGuid &&
                            b.Timestamp.Day == today.Day &&
                            b.Timestamp.Month == today.Month &&
                            b.Timestamp.Year == today.Year).ToList();

            return MaxHearingsPerDay - hearingsBookedForToday.Count();
        }
    }
}
