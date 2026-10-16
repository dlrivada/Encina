```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 107.51 ns |  0.228 ns | 0.136 ns |  1.00 |    0.00 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 425.61 ns |  1.723 ns | 1.139 ns |  3.96 |    0.01 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 499.89 ns |  2.952 ns | 1.953 ns |  4.65 |    0.02 | 0.0057 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 418.49 ns |  1.704 ns | 1.127 ns |  3.89 |    0.01 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 196.34 ns |  1.388 ns | 0.918 ns |  1.83 |    0.01 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 123.72 ns |  0.471 ns | 0.311 ns |  1.15 |    0.00 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 253.65 ns |  1.875 ns | 1.240 ns |  2.36 |    0.01 | 0.0043 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 183.88 ns |  0.702 ns | 0.418 ns |  1.71 |    0.00 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  59.86 ns |  0.360 ns | 0.238 ns |  0.56 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  73.31 ns |  0.538 ns | 0.356 ns |  0.68 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 119.52 ns |  0.563 ns | 0.372 ns |  1.11 |    0.00 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 582.01 ns |  1.704 ns | 1.014 ns |  5.41 |    0.01 | 0.0038 |     392 B |        1.75 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           | 107.27 ns |  3.247 ns | 0.178 ns |  1.00 |    0.00 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 456.38 ns | 21.515 ns | 1.179 ns |  4.25 |    0.01 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 509.27 ns | 85.303 ns | 4.676 ns |  4.75 |    0.04 | 0.0057 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 426.35 ns |  6.759 ns | 0.370 ns |  3.97 |    0.01 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 200.46 ns |  9.559 ns | 0.524 ns |  1.87 |    0.01 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 131.32 ns |  2.121 ns | 0.116 ns |  1.22 |    0.00 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 257.09 ns |  8.774 ns | 0.481 ns |  2.40 |    0.01 | 0.0043 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 186.69 ns |  2.000 ns | 0.110 ns |  1.74 |    0.00 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  59.61 ns |  0.721 ns | 0.040 ns |  0.56 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  73.45 ns |  2.688 ns | 0.147 ns |  0.68 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 119.26 ns | 10.411 ns | 0.571 ns |  1.11 |    0.00 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 598.54 ns | 51.348 ns | 2.815 ns |  5.58 |    0.02 | 0.0038 |     392 B |        1.75 |
