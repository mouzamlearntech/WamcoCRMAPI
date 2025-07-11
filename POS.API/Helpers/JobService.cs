using POS.MediatR.CommandAndQuery;
using POS.Repository;
using Hangfire;
using MediatR;
using System;
using POS.MediatR.Stock.Commands;

namespace POS.API.Helpers
{
    public class JobService
    {
        public IMediator _mediator { get; set; }
        private readonly IConnectionMappingRepository _connectionMappingRepository;

        public JobService(IMediator mediator,
            IConnectionMappingRepository connectionMappingRepository)
        {
            _mediator = mediator;
            _connectionMappingRepository = connectionMappingRepository;
        }
        public void StartScheduler()
        {
            // * * * * *
            // 1 2 3 4 5

            // field #   meaning        allowed values
            // -------   ------------   --------------
            //    1      minute         0-59
            //    2      hour           0-23
            //    3      day of month   1-31
            //    4      month          1-12 (or use names)
            //    5      day of week    0-7 (0 or 7 is Sun, or use names)


            //Daily stock
            RecurringJob.AddOrUpdate("DailyStock", () => DailyStockScheduler(), Cron.Daily(0, 5), new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }); // Every 24 hours

            //Daily Reminder
            RecurringJob.AddOrUpdate("DailyReminder", () => DailyReminder(), Cron.Daily(0, 10), new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }); // Every 24 hours

            //Weekly Reminder
            RecurringJob.AddOrUpdate("WeeklyReminder", () => WeeklyReminder(), Cron.Daily(0, 15), new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }); // Every 24 hours

            //Monthy Reminder
            RecurringJob.AddOrUpdate("MonthlyReminder", () => MonthyReminder(), Cron.Daily(0, 20), new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }); // Every 24 hours

            //Quarterly Reminder
            RecurringJob.AddOrUpdate("QuarterlyReminder", () => QuarterlyReminder(), Cron.Daily(0, 30), new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }); // Every 24 hours

            //HalfYearly Reminder
            RecurringJob.AddOrUpdate("HalfYearlyReminder", () => HalfYearlyReminder(), Cron.Daily(0, 40), new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }); // Every 24 hours

            //Yearly Reminder                                                                                
            RecurringJob.AddOrUpdate("YearlyReminder", () => YearlyReminder(), Cron.Daily(0, 50), new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }); // Every 24 hours

            //Customer Date
            RecurringJob.AddOrUpdate("CustomDateReminder", () => CustomDateReminderSchedule(), Cron.Daily(0, 59), new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }); // Every 24 hours

            //Reminder Scheduler To Send Email
            RecurringJob.AddOrUpdate("ReminderSchedule", () => ReminderSchedule(), "*/10 * * * *", new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }); // Every 10 minutes

        }


        public bool DailyReminder()
        {
            return _mediator.Send(new DailyReminderServicesQuery()).GetAwaiter().GetResult();
        }
        public bool WeeklyReminder()
        {
            return _mediator.Send(new WeeklyReminderServicesQuery()).GetAwaiter().GetResult();

        }
        public bool MonthyReminder()
        {
            return _mediator.Send(new MonthlyReminderServicesQuery()).GetAwaiter().GetResult();
        }
        public bool QuarterlyReminder()
        {
            return _mediator.Send(new QuarterlyReminderServiceQuery()).GetAwaiter().GetResult();
        }

        public bool HalfYearlyReminder()
        {
            return _mediator.Send(new HalfYearlyReminderServiceQuery()).GetAwaiter().GetResult();
        }

        public bool YearlyReminder()
        {
            return _mediator.Send(new YearlyReminderServicesQuery()).GetAwaiter().GetResult();
        }

        public bool ReminderSchedule()
        {
            var schedulerStatus = _connectionMappingRepository.GetSchedulerServiceStatus();
            if (!schedulerStatus)
            {
                _connectionMappingRepository.SetSchedulerServiceStatus(true);
                var result = _mediator.Send(new ReminderSchedulerServiceQuery()).GetAwaiter().GetResult();
                _connectionMappingRepository.SetSchedulerServiceStatus(false);
                return result;
            }
            return true;
        }

        public bool CustomDateReminderSchedule()
        {
            return _mediator.Send(new CustomDateReminderServicesQuery()).GetAwaiter().GetResult();
        }

        public bool DailyStockScheduler()
        {
            return _mediator.Send(new DailyStockSchedulerQuery()).GetAwaiter().GetResult();
        }
    }
}
