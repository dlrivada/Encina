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
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 103.06 ns |  0.561 ns | 0.334 ns |  1.00 |    0.00 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 423.62 ns |  1.125 ns | 0.670 ns |  4.11 |    0.01 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 501.17 ns |  4.513 ns | 2.360 ns |  4.86 |    0.03 | 0.0057 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 406.98 ns |  0.556 ns | 0.291 ns |  3.95 |    0.01 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 188.13 ns |  0.905 ns | 0.598 ns |  1.83 |    0.01 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 115.69 ns |  0.148 ns | 0.078 ns |  1.12 |    0.00 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 243.10 ns |  1.033 ns | 0.683 ns |  2.36 |    0.01 | 0.0043 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 175.85 ns |  0.917 ns | 0.607 ns |  1.71 |    0.01 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  57.17 ns |  0.241 ns | 0.144 ns |  0.55 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  71.74 ns |  3.217 ns | 1.914 ns |  0.70 |    0.02 | 0.0014 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 113.92 ns |  0.781 ns | 0.517 ns |  1.11 |    0.01 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 564.20 ns |  8.179 ns | 4.867 ns |  5.47 |    0.05 | 0.0038 |     392 B |        1.75 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           | 103.87 ns |  5.221 ns | 0.286 ns |  1.00 |    0.00 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 461.39 ns |  8.608 ns | 0.472 ns |  4.44 |    0.01 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 506.59 ns | 19.027 ns | 1.043 ns |  4.88 |    0.01 | 0.0057 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 413.80 ns |  7.449 ns | 0.408 ns |  3.98 |    0.01 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 191.03 ns |  3.630 ns | 0.199 ns |  1.84 |    0.00 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 122.60 ns |  2.646 ns | 0.145 ns |  1.18 |    0.00 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 248.79 ns | 45.933 ns | 2.518 ns |  2.40 |    0.02 | 0.0043 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 180.10 ns |  2.635 ns | 0.144 ns |  1.73 |    0.00 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  57.80 ns |  1.431 ns | 0.078 ns |  0.56 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  71.23 ns | 19.036 ns | 1.043 ns |  0.69 |    0.01 | 0.0014 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 116.09 ns | 49.383 ns | 2.707 ns |  1.12 |    0.02 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 564.37 ns | 22.018 ns | 1.207 ns |  5.43 |    0.02 | 0.0038 |     392 B |        1.75 |
