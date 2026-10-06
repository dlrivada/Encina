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
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  71.21 ns |   1.113 ns |  0.736 ns |  1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 301.90 ns |   8.524 ns |  5.638 ns |  4.24 |    0.09 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 394.23 ns |  38.861 ns | 25.704 ns |  5.54 |    0.35 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 300.46 ns |  12.608 ns |  8.339 ns |  4.22 |    0.12 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 129.27 ns |   2.735 ns |  1.627 ns |  1.82 |    0.03 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  83.16 ns |   1.787 ns |  1.063 ns |  1.17 |    0.02 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 172.95 ns |   4.512 ns |  2.984 ns |  2.43 |    0.05 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 123.97 ns |   1.993 ns |  1.043 ns |  1.74 |    0.02 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  40.43 ns |   1.651 ns |  0.982 ns |  0.57 |    0.01 | 0.0015 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  53.24 ns |   5.397 ns |  3.570 ns |  0.75 |    0.05 | 0.0015 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  86.99 ns |   1.172 ns |  0.697 ns |  1.22 |    0.02 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 411.74 ns |  11.127 ns |  5.820 ns |  5.78 |    0.10 | 0.0043 |     392 B |        1.75 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  73.29 ns |  20.907 ns |  1.146 ns |  1.00 |    0.02 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 298.66 ns |   8.545 ns |  0.468 ns |  4.08 |    0.06 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 365.50 ns | 105.162 ns |  5.764 ns |  4.99 |    0.10 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 293.66 ns |  27.662 ns |  1.516 ns |  4.01 |    0.06 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 135.14 ns |   9.604 ns |  0.526 ns |  1.84 |    0.03 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  85.98 ns | 100.109 ns |  5.487 ns |  1.17 |    0.07 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 170.21 ns |  62.971 ns |  3.452 ns |  2.32 |    0.05 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 126.53 ns |   2.713 ns |  0.149 ns |  1.73 |    0.02 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  47.06 ns |  47.861 ns |  2.623 ns |  0.64 |    0.03 | 0.0015 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  51.01 ns |  10.073 ns |  0.552 ns |  0.70 |    0.01 | 0.0015 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  78.88 ns |  14.475 ns |  0.793 ns |  1.08 |    0.02 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 409.60 ns |  48.754 ns |  2.672 ns |  5.59 |    0.08 | 0.0043 |     392 B |        1.75 |
