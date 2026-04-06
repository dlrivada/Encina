```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  98.99 ns |   1.233 ns | 0.734 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 415.61 ns |   2.614 ns | 1.729 ns |  4.20 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 493.76 ns |   3.264 ns | 2.159 ns |  4.99 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 409.93 ns |   2.410 ns | 1.434 ns |  4.14 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 183.98 ns |   0.829 ns | 0.493 ns |  1.86 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 122.70 ns |   2.135 ns | 1.412 ns |  1.24 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 217.89 ns |   1.646 ns | 0.979 ns |  2.20 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 181.51 ns |   0.602 ns | 0.315 ns |  1.83 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  55.57 ns |   0.241 ns | 0.143 ns |  0.56 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  68.79 ns |   0.421 ns | 0.278 ns |  0.69 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 105.57 ns |   1.023 ns | 0.676 ns |  1.07 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 592.62 ns |   3.754 ns | 2.483 ns |  5.99 |    0.05 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |            |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  95.63 ns |  14.753 ns | 0.809 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 420.29 ns | 126.414 ns | 6.929 ns |  4.40 |    0.07 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 483.59 ns |  13.745 ns | 0.753 ns |  5.06 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 395.74 ns |  41.692 ns | 2.285 ns |  4.14 |    0.04 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 182.32 ns |   5.447 ns | 0.299 ns |  1.91 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 121.54 ns |  25.787 ns | 1.413 ns |  1.27 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 228.63 ns |  36.858 ns | 2.020 ns |  2.39 |    0.03 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 181.91 ns |  26.432 ns | 1.449 ns |  1.90 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  54.31 ns |  26.833 ns | 1.471 ns |  0.57 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  69.81 ns |   5.385 ns | 0.295 ns |  0.73 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 104.39 ns |  11.412 ns | 0.626 ns |  1.09 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 597.14 ns |  25.753 ns | 1.412 ns |  6.24 |    0.05 | 0.0248 |     416 B |        1.86 |
