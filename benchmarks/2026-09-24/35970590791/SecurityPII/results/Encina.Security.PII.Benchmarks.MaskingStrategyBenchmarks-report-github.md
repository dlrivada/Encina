```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 102.48 ns |   0.756 ns | 0.450 ns |  1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 431.27 ns |   3.782 ns | 2.501 ns |  4.21 |    0.03 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 512.31 ns |   4.900 ns | 3.241 ns |  5.00 |    0.04 | 0.0057 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 419.41 ns |   2.449 ns | 1.620 ns |  4.09 |    0.02 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 191.65 ns |   0.673 ns | 0.400 ns |  1.87 |    0.01 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 117.71 ns |   0.585 ns | 0.348 ns |  1.15 |    0.01 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 242.51 ns |   0.851 ns | 0.563 ns |  2.37 |    0.01 | 0.0043 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 177.10 ns |   1.392 ns | 0.828 ns |  1.73 |    0.01 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  54.28 ns |   0.516 ns | 0.341 ns |  0.53 |    0.00 | 0.0015 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  69.88 ns |   0.195 ns | 0.116 ns |  0.68 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 113.56 ns |   0.618 ns | 0.368 ns |  1.11 |    0.01 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 550.11 ns |   5.429 ns | 3.591 ns |  5.37 |    0.04 | 0.0048 |     416 B |        1.86 |
|                     |            |                |             |           |            |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           | 103.18 ns |   2.963 ns | 0.162 ns |  1.00 |    0.00 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 428.08 ns |  27.296 ns | 1.496 ns |  4.15 |    0.01 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 515.61 ns |  39.296 ns | 2.154 ns |  5.00 |    0.02 | 0.0057 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 422.50 ns | 113.424 ns | 6.217 ns |  4.09 |    0.05 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 189.88 ns |  14.002 ns | 0.767 ns |  1.84 |    0.01 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 117.58 ns |   9.593 ns | 0.526 ns |  1.14 |    0.00 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 250.02 ns |  11.274 ns | 0.618 ns |  2.42 |    0.01 | 0.0043 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 177.19 ns |  21.797 ns | 1.195 ns |  1.72 |    0.01 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  54.09 ns |   3.374 ns | 0.185 ns |  0.52 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  70.00 ns |   7.166 ns | 0.393 ns |  0.68 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 113.46 ns |   5.716 ns | 0.313 ns |  1.10 |    0.00 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 569.79 ns |  53.267 ns | 2.920 ns |  5.52 |    0.03 | 0.0048 |     416 B |        1.86 |
