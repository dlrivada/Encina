```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  76.20 ns |   1.014 ns |  0.531 ns |  1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 326.79 ns |  22.194 ns | 14.680 ns |  4.29 |    0.19 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 389.81 ns |  29.254 ns | 19.350 ns |  5.12 |    0.24 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 301.56 ns |   1.335 ns |  0.698 ns |  3.96 |    0.03 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 138.34 ns |   0.964 ns |  0.573 ns |  1.82 |    0.01 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  90.83 ns |   6.408 ns |  4.238 ns |  1.19 |    0.05 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 188.97 ns |  14.527 ns |  9.609 ns |  2.48 |    0.12 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 133.74 ns |   1.147 ns |  0.600 ns |  1.76 |    0.01 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  44.99 ns |   1.562 ns |  0.930 ns |  0.59 |    0.01 | 0.0015 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  52.80 ns |   2.246 ns |  1.485 ns |  0.69 |    0.02 | 0.0015 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  87.72 ns |   6.744 ns |  4.461 ns |  1.15 |    0.06 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 417.97 ns |  10.814 ns |  6.435 ns |  5.49 |    0.09 | 0.0043 |     392 B |        1.75 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  76.42 ns |  17.262 ns |  0.946 ns |  1.00 |    0.02 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 310.04 ns |  29.853 ns |  1.636 ns |  4.06 |    0.05 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 366.43 ns |   5.579 ns |  0.306 ns |  4.80 |    0.05 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 331.14 ns | 258.212 ns | 14.153 ns |  4.33 |    0.17 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 136.50 ns |  11.958 ns |  0.655 ns |  1.79 |    0.02 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  84.57 ns |   5.860 ns |  0.321 ns |  1.11 |    0.01 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 183.58 ns | 202.560 ns | 11.103 ns |  2.40 |    0.13 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 139.96 ns | 179.746 ns |  9.853 ns |  1.83 |    0.11 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  51.87 ns |  19.831 ns |  1.087 ns |  0.68 |    0.01 | 0.0015 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  53.10 ns |  17.806 ns |  0.976 ns |  0.69 |    0.01 | 0.0015 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  85.20 ns |  67.801 ns |  3.716 ns |  1.11 |    0.04 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 411.02 ns |  11.224 ns |  0.615 ns |  5.38 |    0.06 | 0.0043 |     392 B |        1.75 |
