```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  98.57 ns |  0.862 ns | 0.570 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 414.79 ns |  1.395 ns | 0.830 ns |  4.21 |    0.02 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 486.24 ns |  1.890 ns | 1.125 ns |  4.93 |    0.03 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 399.28 ns |  3.537 ns | 2.339 ns |  4.05 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 178.78 ns |  1.401 ns | 0.834 ns |  1.81 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 119.29 ns |  0.983 ns | 0.650 ns |  1.21 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 217.26 ns |  1.976 ns | 1.176 ns |  2.20 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 181.19 ns |  2.229 ns | 1.474 ns |  1.84 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  55.45 ns |  0.545 ns | 0.361 ns |  0.56 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  69.29 ns |  0.494 ns | 0.327 ns |  0.70 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 104.69 ns |  0.917 ns | 0.607 ns |  1.06 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 602.82 ns |  1.365 ns | 0.812 ns |  6.12 |    0.03 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  96.77 ns | 16.866 ns | 0.924 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 424.42 ns | 29.070 ns | 1.593 ns |  4.39 |    0.04 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 494.32 ns | 17.200 ns | 0.943 ns |  5.11 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 403.98 ns |  6.957 ns | 0.381 ns |  4.17 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 184.30 ns |  4.648 ns | 0.255 ns |  1.90 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 121.18 ns |  7.970 ns | 0.437 ns |  1.25 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 226.05 ns | 23.804 ns | 1.305 ns |  2.34 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 177.12 ns | 11.227 ns | 0.615 ns |  1.83 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  54.82 ns |  3.733 ns | 0.205 ns |  0.57 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  68.68 ns |  2.031 ns | 0.111 ns |  0.71 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 104.10 ns | 32.435 ns | 1.778 ns |  1.08 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 593.59 ns | 37.783 ns | 2.071 ns |  6.13 |    0.05 | 0.0248 |     416 B |        1.86 |
