```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method              | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 100.22 ns |  3.365 ns |  2.226 ns |  99.70 ns |  1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 414.64 ns |  9.493 ns |  6.279 ns | 413.49 ns |  4.14 |    0.11 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 3           | 484.89 ns | 12.471 ns |  8.249 ns | 482.92 ns |  4.84 |    0.13 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 3           | 400.18 ns | 11.812 ns |  7.813 ns | 398.02 ns |  3.99 |    0.11 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 3           | 187.61 ns |  3.708 ns |  2.452 ns | 187.45 ns |  1.87 |    0.05 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 3           | 123.09 ns |  2.432 ns |  1.609 ns | 123.04 ns |  1.23 |    0.03 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 3           | 222.85 ns |  7.274 ns |  4.811 ns | 223.10 ns |  2.22 |    0.07 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 3           | 180.33 ns |  0.998 ns |  0.594 ns | 180.15 ns |  1.80 |    0.04 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | 3           |  55.13 ns |  1.621 ns |  1.072 ns |  55.39 ns |  0.55 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | 3           |  69.45 ns |  1.478 ns |  0.978 ns |  69.82 ns |  0.69 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 3           | 105.68 ns |  6.286 ns |  4.158 ns | 104.74 ns |  1.05 |    0.05 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 3           | 609.50 ns |  7.219 ns |  4.296 ns | 610.52 ns |  6.08 |    0.13 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |             |           |           |           |           |       |         |        |           |             |
| Email_Partial       | MediumRun  | 15             | 2           | 10          |  97.65 ns |  1.198 ns |  1.794 ns |  97.66 ns |  1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | MediumRun  | 15             | 2           | 10          | 423.95 ns |  3.276 ns |  4.802 ns | 425.46 ns |  4.34 |    0.09 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | MediumRun  | 15             | 2           | 10          | 483.49 ns |  3.064 ns |  4.491 ns | 481.78 ns |  4.95 |    0.10 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | MediumRun  | 15             | 2           | 10          | 402.16 ns |  4.529 ns |  6.778 ns | 403.34 ns |  4.12 |    0.10 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | MediumRun  | 15             | 2           | 10          | 181.53 ns |  0.596 ns |  0.892 ns | 181.44 ns |  1.86 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | MediumRun  | 15             | 2           | 10          | 117.76 ns |  0.267 ns |  0.365 ns | 117.80 ns |  1.21 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | MediumRun  | 15             | 2           | 10          | 221.05 ns |  1.218 ns |  1.746 ns | 221.04 ns |  2.26 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | MediumRun  | 15             | 2           | 10          | 178.05 ns |  1.705 ns |  2.499 ns | 178.14 ns |  1.82 |    0.04 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | MediumRun  | 15             | 2           | 10          |  52.97 ns |  0.487 ns |  0.698 ns |  52.91 ns |  0.54 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | MediumRun  | 15             | 2           | 10          |  68.94 ns |  0.340 ns |  0.487 ns |  69.01 ns |  0.71 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | MediumRun  | 15             | 2           | 10          | 102.47 ns |  0.438 ns |  0.628 ns | 102.49 ns |  1.05 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | MediumRun  | 15             | 2           | 10          | 595.80 ns |  9.340 ns | 13.395 ns | 604.87 ns |  6.10 |    0.17 | 0.0248 |     416 B |        1.86 |
