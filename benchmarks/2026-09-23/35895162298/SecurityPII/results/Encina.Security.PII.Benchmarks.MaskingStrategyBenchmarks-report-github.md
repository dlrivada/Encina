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
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 101.33 ns |   1.017 ns | 0.672 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 420.95 ns |   2.693 ns | 1.602 ns |  4.15 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 500.17 ns |   6.151 ns | 4.069 ns |  4.94 |    0.05 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 404.32 ns |   4.497 ns | 2.974 ns |  3.99 |    0.04 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 183.11 ns |   2.255 ns | 1.492 ns |  1.81 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 120.72 ns |   1.729 ns | 1.143 ns |  1.19 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 230.93 ns |   3.187 ns | 2.108 ns |  2.28 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 182.78 ns |   3.008 ns | 1.989 ns |  1.80 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  57.05 ns |   2.297 ns | 1.519 ns |  0.56 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  70.12 ns |   1.115 ns | 0.738 ns |  0.69 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 101.70 ns |   1.743 ns | 1.153 ns |  1.00 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 591.95 ns |   2.585 ns | 1.538 ns |  5.84 |    0.04 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |            |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  98.25 ns |  27.538 ns | 1.509 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 427.19 ns |  17.449 ns | 0.956 ns |  4.35 |    0.06 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 509.98 ns | 104.912 ns | 5.751 ns |  5.19 |    0.09 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 418.05 ns |  58.330 ns | 3.197 ns |  4.26 |    0.06 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 184.39 ns |  36.887 ns | 2.022 ns |  1.88 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 122.37 ns |  32.800 ns | 1.798 ns |  1.25 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 239.19 ns |  23.255 ns | 1.275 ns |  2.44 |    0.03 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 180.01 ns |  32.515 ns | 1.782 ns |  1.83 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  57.05 ns |  17.055 ns | 0.935 ns |  0.58 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  70.86 ns |   7.382 ns | 0.405 ns |  0.72 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 108.97 ns |  35.205 ns | 1.930 ns |  1.11 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 594.43 ns | 113.962 ns | 6.247 ns |  6.05 |    0.10 | 0.0248 |     416 B |        1.86 |
