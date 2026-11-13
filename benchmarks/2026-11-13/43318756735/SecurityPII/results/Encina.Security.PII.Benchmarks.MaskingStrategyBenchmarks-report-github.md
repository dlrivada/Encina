```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |-----:|------:|------:|--------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| Name_Partial        | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| Email_Short         | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| Email_Long          | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| RegexPattern        | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
|                     |            |                |             |      |       |       |         |             |
| Email_Partial       | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| Phone_Partial       | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| CreditCard_Partial  | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| SSN_Partial         | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| Name_Partial        | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| Address_Partial     | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| DateOfBirth_Partial | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| IPAddress_Partial   | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| Custom_FullMasking  | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| Email_Short         | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| Email_Long          | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| RegexPattern        | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  MaskingStrategyBenchmarks.Email_Partial: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  MaskingStrategyBenchmarks.Phone_Partial: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  MaskingStrategyBenchmarks.CreditCard_Partial: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  MaskingStrategyBenchmarks.SSN_Partial: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  MaskingStrategyBenchmarks.Name_Partial: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  MaskingStrategyBenchmarks.Address_Partial: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  MaskingStrategyBenchmarks.DateOfBirth_Partial: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  MaskingStrategyBenchmarks.IPAddress_Partial: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  MaskingStrategyBenchmarks.Custom_FullMasking: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  MaskingStrategyBenchmarks.Email_Short: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  MaskingStrategyBenchmarks.Email_Long: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  MaskingStrategyBenchmarks.RegexPattern: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  MaskingStrategyBenchmarks.Email_Partial: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  MaskingStrategyBenchmarks.Phone_Partial: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  MaskingStrategyBenchmarks.CreditCard_Partial: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  MaskingStrategyBenchmarks.SSN_Partial: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  MaskingStrategyBenchmarks.Name_Partial: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  MaskingStrategyBenchmarks.Address_Partial: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  MaskingStrategyBenchmarks.DateOfBirth_Partial: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  MaskingStrategyBenchmarks.IPAddress_Partial: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  MaskingStrategyBenchmarks.Custom_FullMasking: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  MaskingStrategyBenchmarks.Email_Short: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  MaskingStrategyBenchmarks.Email_Long: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  MaskingStrategyBenchmarks.RegexPattern: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
