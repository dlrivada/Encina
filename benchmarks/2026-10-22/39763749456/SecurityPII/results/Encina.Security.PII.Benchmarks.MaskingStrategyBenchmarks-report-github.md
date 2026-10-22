```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.57GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  86.58 ns |   2.459 ns | 1.463 ns |  1.00 |    0.02 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 358.16 ns |   5.997 ns | 3.569 ns |  4.14 |    0.08 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 424.20 ns |   3.807 ns | 2.518 ns |  4.90 |    0.08 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 350.99 ns |   4.105 ns | 2.715 ns |  4.06 |    0.07 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 163.43 ns |   1.549 ns | 0.810 ns |  1.89 |    0.03 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 108.81 ns |   2.258 ns | 1.493 ns |  1.26 |    0.03 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 214.23 ns |   1.715 ns | 1.020 ns |  2.48 |    0.04 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 154.74 ns |   0.806 ns | 0.480 ns |  1.79 |    0.03 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  52.84 ns |   2.100 ns | 1.389 ns |  0.61 |    0.02 | 0.0015 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  60.33 ns |   2.284 ns | 1.359 ns |  0.70 |    0.02 | 0.0014 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  99.98 ns |   2.444 ns | 1.454 ns |  1.16 |    0.02 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 487.14 ns |   1.917 ns | 1.141 ns |  5.63 |    0.09 | 0.0038 |     392 B |        1.75 |
|                     |            |                |             |           |            |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  96.89 ns |   8.442 ns | 0.463 ns |  1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 361.26 ns |  84.284 ns | 4.620 ns |  3.73 |    0.04 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 429.31 ns |  80.012 ns | 4.386 ns |  4.43 |    0.04 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 353.25 ns |  30.607 ns | 1.678 ns |  3.65 |    0.02 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 162.16 ns |  49.407 ns | 2.708 ns |  1.67 |    0.03 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 101.33 ns |  17.100 ns | 0.937 ns |  1.05 |    0.01 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 212.62 ns |  14.027 ns | 0.769 ns |  2.19 |    0.01 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 152.18 ns |  21.507 ns | 1.179 ns |  1.57 |    0.01 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  57.53 ns |   9.260 ns | 0.508 ns |  0.59 |    0.01 | 0.0015 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  70.65 ns |  10.416 ns | 0.571 ns |  0.73 |    0.01 | 0.0014 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 102.33 ns |  52.570 ns | 2.882 ns |  1.06 |    0.03 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 490.14 ns | 172.482 ns | 9.454 ns |  5.06 |    0.09 | 0.0038 |     392 B |        1.75 |
