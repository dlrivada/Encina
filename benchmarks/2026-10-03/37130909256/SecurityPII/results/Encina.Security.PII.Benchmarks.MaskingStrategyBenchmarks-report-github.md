```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  51.43 ns |   1.165 ns |  0.693 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 236.25 ns |  16.962 ns | 11.219 ns |  4.59 |    0.22 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 283.21 ns |  17.141 ns | 11.338 ns |  5.51 |    0.22 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 220.66 ns |   5.038 ns |  2.635 ns |  4.29 |    0.07 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     |  99.17 ns |   2.316 ns |  1.532 ns |  1.93 |    0.04 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  61.43 ns |   1.744 ns |  1.038 ns |  1.19 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 127.33 ns |   3.137 ns |  1.867 ns |  2.48 |    0.05 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     |  93.38 ns |   2.098 ns |  1.249 ns |  1.82 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  36.55 ns |   0.487 ns |  0.322 ns |  0.71 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  37.99 ns |   0.499 ns |  0.330 ns |  0.74 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  56.25 ns |   0.836 ns |  0.497 ns |  1.09 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 306.80 ns |   5.431 ns |  3.592 ns |  5.97 |    0.10 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  54.10 ns |  33.578 ns |  1.841 ns |  1.00 |    0.04 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 226.43 ns |  43.002 ns |  2.357 ns |  4.19 |    0.13 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 268.69 ns |  32.180 ns |  1.764 ns |  4.97 |    0.15 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 222.27 ns |  55.695 ns |  3.053 ns |  4.11 |    0.13 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           |  96.90 ns |  27.047 ns |  1.483 ns |  1.79 |    0.06 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  58.10 ns |  10.011 ns |  0.549 ns |  1.07 |    0.03 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 126.13 ns |  26.481 ns |  1.452 ns |  2.33 |    0.07 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           |  96.81 ns |   7.806 ns |  0.428 ns |  1.79 |    0.05 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  37.13 ns |   5.200 ns |  0.285 ns |  0.69 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  36.83 ns |   5.962 ns |  0.327 ns |  0.68 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  55.97 ns |  29.068 ns |  1.593 ns |  1.04 |    0.04 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 322.98 ns | 250.010 ns | 13.704 ns |  5.97 |    0.28 | 0.0248 |     416 B |        1.86 |
