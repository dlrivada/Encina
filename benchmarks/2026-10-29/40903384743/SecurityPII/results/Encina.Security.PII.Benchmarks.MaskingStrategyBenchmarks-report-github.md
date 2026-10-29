```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  86.06 ns |  2.265 ns | 1.348 ns |  1.00 |    0.02 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 363.56 ns |  0.605 ns | 0.400 ns |  4.23 |    0.06 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 427.20 ns |  4.125 ns | 2.455 ns |  4.97 |    0.08 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 344.83 ns |  6.374 ns | 4.216 ns |  4.01 |    0.08 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 159.61 ns |  1.274 ns | 0.842 ns |  1.86 |    0.03 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  92.67 ns |  3.573 ns | 2.364 ns |  1.08 |    0.03 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 208.81 ns |  5.655 ns | 3.741 ns |  2.43 |    0.06 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 149.08 ns |  2.530 ns | 1.506 ns |  1.73 |    0.03 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  45.48 ns |  0.928 ns | 0.614 ns |  0.53 |    0.01 | 0.0015 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  57.60 ns |  0.884 ns | 0.585 ns |  0.67 |    0.01 | 0.0014 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  87.51 ns |  1.743 ns | 1.153 ns |  1.02 |    0.02 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 486.08 ns |  2.316 ns | 1.532 ns |  5.65 |    0.09 | 0.0038 |     392 B |        1.75 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  84.21 ns |  7.792 ns | 0.427 ns |  1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 365.05 ns |  8.707 ns | 0.477 ns |  4.34 |    0.02 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 435.61 ns | 15.208 ns | 0.834 ns |  5.17 |    0.02 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 358.20 ns | 12.151 ns | 0.666 ns |  4.25 |    0.02 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 166.47 ns |  3.289 ns | 0.180 ns |  1.98 |    0.01 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 104.20 ns | 14.459 ns | 0.793 ns |  1.24 |    0.01 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 222.91 ns |  2.772 ns | 0.152 ns |  2.65 |    0.01 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 160.55 ns |  3.034 ns | 0.166 ns |  1.91 |    0.01 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  49.16 ns |  2.493 ns | 0.137 ns |  0.58 |    0.00 | 0.0015 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  63.02 ns | 43.128 ns | 2.364 ns |  0.75 |    0.02 | 0.0014 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  92.79 ns | 20.504 ns | 1.124 ns |  1.10 |    0.01 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 491.99 ns | 14.490 ns | 0.794 ns |  5.84 |    0.03 | 0.0038 |     392 B |        1.75 |
