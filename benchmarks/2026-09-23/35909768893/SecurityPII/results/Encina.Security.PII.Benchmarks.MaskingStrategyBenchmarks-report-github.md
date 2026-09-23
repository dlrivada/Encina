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
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  49.84 ns |   0.532 ns |  0.352 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 231.97 ns |   3.427 ns |  2.267 ns |  4.65 |    0.05 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 281.78 ns |   6.279 ns |  4.153 ns |  5.65 |    0.09 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 221.68 ns |   1.841 ns |  1.096 ns |  4.45 |    0.04 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     |  95.97 ns |   2.178 ns |  1.296 ns |  1.93 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  57.15 ns |   1.393 ns |  0.829 ns |  1.15 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 124.95 ns |   2.686 ns |  1.777 ns |  2.51 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     |  94.10 ns |   2.507 ns |  1.492 ns |  1.89 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  32.84 ns |   0.552 ns |  0.365 ns |  0.66 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  35.21 ns |   1.388 ns |  0.918 ns |  0.71 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  51.82 ns |   0.727 ns |  0.481 ns |  1.04 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 327.14 ns |   2.503 ns |  1.490 ns |  6.56 |    0.05 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  49.76 ns |   4.190 ns |  0.230 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 237.70 ns |  82.923 ns |  4.545 ns |  4.78 |    0.08 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 284.53 ns | 103.220 ns |  5.658 ns |  5.72 |    0.10 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 230.23 ns |  87.913 ns |  4.819 ns |  4.63 |    0.09 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 110.65 ns |  67.546 ns |  3.702 ns |  2.22 |    0.07 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  56.41 ns |   5.144 ns |  0.282 ns |  1.13 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 128.56 ns |  49.441 ns |  2.710 ns |  2.58 |    0.05 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           |  91.83 ns |   8.817 ns |  0.483 ns |  1.85 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  32.51 ns |   9.640 ns |  0.528 ns |  0.65 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  33.29 ns |  11.024 ns |  0.604 ns |  0.67 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  52.63 ns |  51.336 ns |  2.814 ns |  1.06 |    0.05 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 327.10 ns | 390.823 ns | 21.422 ns |  6.57 |    0.37 | 0.0248 |     416 B |        1.86 |
