```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 100.08 ns |   0.705 ns | 0.420 ns |  1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 390.79 ns |   5.382 ns | 3.560 ns |  3.90 |    0.04 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 456.28 ns |  12.815 ns | 8.476 ns |  4.56 |    0.08 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 375.63 ns |   8.410 ns | 5.563 ns |  3.75 |    0.06 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 180.19 ns |   3.641 ns | 2.408 ns |  1.80 |    0.02 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 112.85 ns |   2.539 ns | 1.511 ns |  1.13 |    0.02 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 230.92 ns |   1.872 ns | 1.238 ns |  2.31 |    0.01 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 164.30 ns |   1.759 ns | 1.163 ns |  1.64 |    0.01 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  54.00 ns |   0.430 ns | 0.256 ns |  0.54 |    0.00 | 0.0015 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  65.55 ns |   0.429 ns | 0.284 ns |  0.66 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 106.42 ns |   1.525 ns | 1.009 ns |  1.06 |    0.01 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 510.24 ns |   2.497 ns | 1.652 ns |  5.10 |    0.03 | 0.0038 |     392 B |        1.75 |
|                     |            |                |             |           |            |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  96.94 ns |  21.513 ns | 1.179 ns |  1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 387.17 ns |  25.939 ns | 1.422 ns |  3.99 |    0.04 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 468.14 ns | 118.322 ns | 6.486 ns |  4.83 |    0.08 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 386.89 ns |  90.716 ns | 4.972 ns |  3.99 |    0.06 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 179.09 ns |  15.665 ns | 0.859 ns |  1.85 |    0.02 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 117.02 ns |  11.018 ns | 0.604 ns |  1.21 |    0.01 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 233.51 ns |  28.783 ns | 1.578 ns |  2.41 |    0.03 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 169.11 ns |   5.986 ns | 0.328 ns |  1.74 |    0.02 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  53.82 ns |   3.968 ns | 0.218 ns |  0.56 |    0.01 | 0.0015 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  65.67 ns |   6.596 ns | 0.362 ns |  0.68 |    0.01 | 0.0014 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 108.13 ns |  12.750 ns | 0.699 ns |  1.12 |    0.01 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 517.56 ns |  25.528 ns | 1.399 ns |  5.34 |    0.06 | 0.0038 |     392 B |        1.75 |
