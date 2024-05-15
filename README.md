# EHentai prometheus metrics collector
![ci](https://img.shields.io/github/actions/workflow/status/mixa3607/EH_metrics/push_branch.yml?branch=master&style=flat-square)
![GitHub Release](https://img.shields.io/github/v/release/mixa3607/EH_metrics?display_name=tag&style=flat-square)
![license](https://img.shields.io/github/license/mixa3607/EH_metrics?style=flat-square)

## About
This soft periodically checks the specified e-hentai pages and extracts metrics compatible with prometheus. Every page presented with scheduled jobs.

> All metrics always have first tag `user` that represent account user id

> Docker-Compose example located in [docker-compose.example](./docker-compose.example)

Example of dashboard [real snapshot](https://snapshots.raintank.io/dashboard/snapshot/iXowOeW8TVsZKV1NtEYxFMAtoOK32Asw) and [json schema](/EH-1715789893565.json)

## Changelog

### v1.1.1 - Features
- add `client_name` to metrics `eh_hath_clients_ranges_groups_number`, `eh_hath_clients_ranges_number`

### v1.1.0 - Fixes/features
- fix job `CollectHathSettingsMetricsJob`
- add metric `eh_hath_clients_ranges_groups_number`
- update grafana schema

### v1.0.0 - Fixes/updates/features
- changes in jobs configuration (array[] => dictionary{}, many jobs enabled by default)
- fix metrics (`eh_hath_regions_miss_percent` => `eh_hath_regions_hits_per_second_ratio`)
- add new versions checker
- update to dotnet 8
- now container is rootless (port changed 80 => 8080)
- add grafana schema

### v0.1.0 - First release
- initial release

## Config
Required site client settings `EhClient{}`:
```js
{
  "MemberId": "9965000",                            //* ipb_member_id cookie
  "PassHash": "bbc23afa07096d2f700b1d48c1ba777f",   //* ipb_pass_hash cookie
  "SessionId": "5529515b6d7a892362de74e2b444c987",  //* ipb_session_id cookie
  // proxy is optional
  //"Proxy": {
  //  "Address": "socks5://127.0.0.1:2080", //* supported http/https/socks4/socks5
  //  "UserName": "<login>",                //  password / login is optional
  //  "Password": "<pass>",                 //  password / login is optional
  //}
}
```

Every job defined with json in `AppQuartz{} => Jobs{<name>:{<info>}}`:
```js
hath_settings_metrics_45044: {                //* unique name
  "Enable": true,                             //* enable/disable job
  "TriggerOnStartup": true,                   //  execute on service start if enabled
  "Group": "eh",                              //* jobs group
  "CronExpression": "0 */10 * ? * *",         //* schedule in cron format (ex. every 10 minutes)
  "JobType": "CollectHathSettingsMetricsJob", //* job type
  "JobData": { "ClientId": 45044 }            //  some job types require additional data
}
```

## Allowed jobs
- [HathStatus](#CollectHathStatusMetricsJob)
- [HathSettings](#CollectHathSettingsMetricsJob)
- [HathPerks](#CollectHathPerksMetricsJob)
- [HomeOverview](#CollectHomeOverviewMetricsJob)
- [CheckNewReleases](#CheckNewReleasesJob)

### See default config at [appsettings.json](/src/ArkProjects.EHentai.MetricsCollector/appsettings.json)

### Collect`HathStatus`MetricsJob
```
  JobType: CollectHathStatusMetricsJob
  JobData: -
Site page: /hentaiathome.php
```
|Name|Desc|Labels|Type|
|----|----|------|----|
|eh_hath_regions_netload_mbps|E-Hentai H@H current network load|region|Gauge|
|eh_hath_regions_hits_per_second_ratio|E-Hentai H@H current hits per second|region|Gauge|
|eh_hath_regions_coverage_percent|E-Hentai H@H coverage|region|Gauge|
|eh_hath_regions_hits_per_gb_ratio|E-Hentai H@H Hits/GB ratio|region|Gauge|
|eh_hath_regions_quality_number|E-Hentai H@H quality|region|Gauge|
|eh_hath_clients_files_served_number|E-Hentai H@H client files served|client_name, client_id|Gauge|
|eh_hath_clients_max_speed_kbps|E-Hentai H@H client max kb/s|client_name, client_id|Gauge|
|eh_hath_clients_trust_number|E-Hentai H@H client trust|client_name, client_id|Gauge|
|eh_hath_clients_quality_number|E-Hentai H@H client quality|client_name, client_id|Gauge|
|eh_hath_clients_hitrate_number|E-Hentai H@H client hits per minute|client_name, client_id|Gauge|
|eh_hath_clients_hathrate_number|E-Hentai H@H client hath per day|client_name, client_id|Gauge|
|eh_hath_clients_status_enum|E-Hentai H@H client status|client_name, client_id|Gauge|


### Collect`HathSettings`MetricsJob
```
  JobType: CollectHathSettingsMetricsJob
  JobData: ClientId = H@H client id
Site page: /hentaiathome.php?cid=<ClientId>&act=settings
```
|Name|Desc|Labels|Type|
|----|----|------|----|
|eh_hath_clients_ranges_number|E-Hentai H@H client static ranges|client_id|Gauge|
|eh_hath_clients_ranges_groups_number|E-Hentai H@H client static ranges per group (Priority[1-4], HighCapacity)|client_id, client_name, group_type|Gauge|


### Collect`HathPerks`MetricsJob
```
  JobType: CollectHathPerksMetricsJob
  JobData: -
Site page: /hathperks.php
```
|Name|Desc|Labels|Type|
|----|----|------|----|
|eh_hath_balance_number|E-Hentai current hath balance||Gauge|


### Collect`HomeOverview`MetricsJob
```
  JobType: CollectHomeOverviewMetricsJob
  JobData: -
Site page: /home.php
```
|Name|Desc|Labels|Type|
|----|----|------|----|
|eh_eht_uploaded_mb|E-Hentai EHTracker uploaded megabytes||Gauge|
|eh_eht_downloaded_mb|E-Hentai EHTracker downloaded megabytes||Gauge|
|eh_eht_seed_minutes|E-Hentai EHTracker seeding minutes||Gauge|
|eh_eht_completes_number|E-Hentai EHTracker completes|type|Gauge|
|eh_eht_up_down_ratio|E-Hentai EHTracker up/down ratio||Gauge|
|eh_gp_gained_number|E-Hentai Total GP Gained from|type|Gauge|

### `CheckNewReleases`Job
```
  JobType: CheckNewReleasesJob
  JobData: -
```

|Name|Desc|Labels|Type|
|----|----|------|----|
|eh_app_version_state|Version check status. 0 - unknown, 1 - latest, 2 - outdated||Gauge|


## Scrapper metrics
|Name|Desc|Labels|Type|
|----|----|------|----|
|eh_job_run_time_seconds|Scheduled job execution run time in seconds|name|Histogram|
|eh_job_failures_number|Scheduled job execution errors|name|Counter|


### Grafana sample dashboard
![grafana](./grafana1.png)
\* `Real clients` graphs filled with metrics from [modified H@H client](https://github.com/mixa3607/EH_hath)
