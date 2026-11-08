```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method              | Job        | IterationCount | LaunchCount | WarmupCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |-----:|------:|------:|--------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| Email_Short         | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
|                     |            |                |             |             |      |       |       |         |             |
| Email_Partial       | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| Phone_Partial       | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| CreditCard_Partial  | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| SSN_Partial         | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| Name_Partial        | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| Address_Partial     | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| DateOfBirth_Partial | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| IPAddress_Partial   | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| Custom_FullMasking  | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| Email_Short         | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| Email_Long          | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| RegexPattern        | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |

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
  MaskingStrategyBenchmarks.Email_Partial: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  MaskingStrategyBenchmarks.Phone_Partial: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  MaskingStrategyBenchmarks.CreditCard_Partial: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  MaskingStrategyBenchmarks.SSN_Partial: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  MaskingStrategyBenchmarks.Name_Partial: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  MaskingStrategyBenchmarks.Address_Partial: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  MaskingStrategyBenchmarks.DateOfBirth_Partial: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  MaskingStrategyBenchmarks.IPAddress_Partial: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  MaskingStrategyBenchmarks.Custom_FullMasking: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  MaskingStrategyBenchmarks.Email_Short: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  MaskingStrategyBenchmarks.Email_Long: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  MaskingStrategyBenchmarks.RegexPattern: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
