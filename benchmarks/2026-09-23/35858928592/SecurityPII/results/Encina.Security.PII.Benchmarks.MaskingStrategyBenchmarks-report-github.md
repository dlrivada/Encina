```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 106.20 ns |  0.648 ns | 0.429 ns |  1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 437.59 ns |  2.679 ns | 1.772 ns |  4.12 |    0.02 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 510.45 ns |  2.800 ns | 1.464 ns |  4.81 |    0.02 | 0.0057 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 417.59 ns |  1.031 ns | 0.613 ns |  3.93 |    0.02 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 192.46 ns |  7.365 ns | 4.872 ns |  1.81 |    0.04 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 119.91 ns |  0.861 ns | 0.512 ns |  1.13 |    0.01 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 242.62 ns |  0.522 ns | 0.311 ns |  2.28 |    0.01 | 0.0043 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 178.69 ns |  1.513 ns | 1.001 ns |  1.68 |    0.01 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  55.15 ns |  0.499 ns | 0.330 ns |  0.52 |    0.00 | 0.0015 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  70.46 ns |  0.510 ns | 0.304 ns |  0.66 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 116.47 ns |  0.454 ns | 0.270 ns |  1.10 |    0.00 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 548.94 ns |  1.344 ns | 0.800 ns |  5.17 |    0.02 | 0.0048 |     416 B |        1.86 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           | 104.68 ns |  1.752 ns | 0.096 ns |  1.00 |    0.00 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 431.26 ns | 36.868 ns | 2.021 ns |  4.12 |    0.02 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 506.00 ns | 47.686 ns | 2.614 ns |  4.83 |    0.02 | 0.0057 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 418.31 ns | 25.453 ns | 1.395 ns |  4.00 |    0.01 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 190.54 ns |  7.826 ns | 0.429 ns |  1.82 |    0.00 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 119.95 ns |  6.503 ns | 0.356 ns |  1.15 |    0.00 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 247.86 ns | 34.412 ns | 1.886 ns |  2.37 |    0.02 | 0.0043 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 179.76 ns | 84.726 ns | 4.644 ns |  1.72 |    0.04 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  55.85 ns |  2.453 ns | 0.134 ns |  0.53 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  71.27 ns |  2.492 ns | 0.137 ns |  0.68 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 118.65 ns |  1.950 ns | 0.107 ns |  1.13 |    0.00 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 555.36 ns | 38.702 ns | 2.121 ns |  5.31 |    0.02 | 0.0048 |     416 B |        1.86 |
