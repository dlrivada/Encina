```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  93.90 ns |  0.983 ns | 0.650 ns |  1.00 |    0.01 | 0.0088 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 420.24 ns |  1.103 ns | 0.657 ns |  4.48 |    0.03 | 0.0205 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 497.91 ns |  1.164 ns | 0.693 ns |  5.30 |    0.04 | 0.0210 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 410.47 ns |  1.389 ns | 0.919 ns |  4.37 |    0.03 | 0.0205 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 185.11 ns |  1.058 ns | 0.700 ns |  1.97 |    0.01 | 0.0110 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 113.85 ns |  1.739 ns | 1.150 ns |  1.21 |    0.01 | 0.0130 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 241.27 ns |  4.405 ns | 2.913 ns |  2.57 |    0.03 | 0.0153 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 180.55 ns |  1.157 ns | 0.765 ns |  1.92 |    0.01 | 0.0105 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  53.46 ns |  0.990 ns | 0.655 ns |  0.57 |    0.01 | 0.0051 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  65.51 ns |  0.379 ns | 0.198 ns |  0.70 |    0.01 | 0.0050 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 105.00 ns |  1.644 ns | 0.978 ns |  1.12 |    0.01 | 0.0130 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 604.08 ns |  4.769 ns | 3.154 ns |  6.43 |    0.05 | 0.0162 |     416 B |        1.86 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  97.37 ns | 14.962 ns | 0.820 ns |  1.00 |    0.01 | 0.0088 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 425.19 ns | 21.123 ns | 1.158 ns |  4.37 |    0.03 | 0.0205 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 500.15 ns | 15.541 ns | 0.852 ns |  5.14 |    0.04 | 0.0210 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 416.10 ns |  6.721 ns | 0.368 ns |  4.27 |    0.03 | 0.0205 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 189.33 ns | 12.933 ns | 0.709 ns |  1.94 |    0.02 | 0.0110 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 119.38 ns |  6.695 ns | 0.367 ns |  1.23 |    0.01 | 0.0129 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 240.75 ns | 23.303 ns | 1.277 ns |  2.47 |    0.02 | 0.0153 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 179.36 ns | 17.970 ns | 0.985 ns |  1.84 |    0.02 | 0.0105 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  55.52 ns | 14.408 ns | 0.790 ns |  0.57 |    0.01 | 0.0050 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  64.77 ns |  2.273 ns | 0.125 ns |  0.67 |    0.00 | 0.0050 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 101.21 ns | 29.074 ns | 1.594 ns |  1.04 |    0.02 | 0.0130 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 591.03 ns | 24.973 ns | 1.369 ns |  6.07 |    0.05 | 0.0162 |     416 B |        1.86 |
