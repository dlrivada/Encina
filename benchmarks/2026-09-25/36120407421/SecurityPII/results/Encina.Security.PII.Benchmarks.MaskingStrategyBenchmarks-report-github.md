```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  94.91 ns |   2.901 ns | 1.919 ns |  1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 410.56 ns |   6.481 ns | 3.857 ns |  4.33 |    0.09 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 483.12 ns |   4.325 ns | 2.861 ns |  5.09 |    0.10 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 391.30 ns |   4.319 ns | 2.570 ns |  4.12 |    0.08 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 180.03 ns |   1.432 ns | 0.749 ns |  1.90 |    0.04 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 118.29 ns |   0.882 ns | 0.583 ns |  1.25 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 223.49 ns |   2.291 ns | 1.515 ns |  2.36 |    0.05 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 182.88 ns |   5.754 ns | 3.424 ns |  1.93 |    0.05 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  57.33 ns |   2.394 ns | 1.583 ns |  0.60 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  67.27 ns |   1.799 ns | 1.190 ns |  0.71 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 109.04 ns |   2.929 ns | 1.937 ns |  1.15 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 582.46 ns |   3.730 ns | 2.467 ns |  6.14 |    0.12 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |            |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  96.94 ns |  15.842 ns | 0.868 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 423.58 ns | 143.014 ns | 7.839 ns |  4.37 |    0.08 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 509.87 ns |  27.384 ns | 1.501 ns |  5.26 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 418.85 ns |  40.160 ns | 2.201 ns |  4.32 |    0.04 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 184.76 ns |  14.899 ns | 0.817 ns |  1.91 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 125.89 ns |  12.149 ns | 0.666 ns |  1.30 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 234.43 ns |  10.461 ns | 0.573 ns |  2.42 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 181.93 ns |  10.434 ns | 0.572 ns |  1.88 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  55.82 ns |  12.140 ns | 0.665 ns |  0.58 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  70.75 ns |  24.080 ns | 1.320 ns |  0.73 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 115.33 ns |  14.858 ns | 0.814 ns |  1.19 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 614.05 ns | 103.151 ns | 5.654 ns |  6.33 |    0.07 | 0.0248 |     416 B |        1.86 |
