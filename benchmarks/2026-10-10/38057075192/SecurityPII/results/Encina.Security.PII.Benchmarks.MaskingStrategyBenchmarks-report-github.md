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
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 105.69 ns |  0.287 ns | 0.171 ns |  1.00 |    0.00 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 427.07 ns |  1.900 ns | 1.257 ns |  4.04 |    0.01 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 506.11 ns |  2.433 ns | 1.448 ns |  4.79 |    0.01 | 0.0057 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 413.42 ns |  1.137 ns | 0.752 ns |  3.91 |    0.01 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 191.06 ns |  0.897 ns | 0.534 ns |  1.81 |    0.01 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 118.78 ns |  0.603 ns | 0.399 ns |  1.12 |    0.00 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 244.71 ns |  0.809 ns | 0.535 ns |  2.32 |    0.01 | 0.0043 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 178.65 ns |  1.442 ns | 0.954 ns |  1.69 |    0.01 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  59.47 ns |  0.282 ns | 0.187 ns |  0.56 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  71.92 ns |  0.188 ns | 0.099 ns |  0.68 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 119.10 ns |  1.374 ns | 0.909 ns |  1.13 |    0.01 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 573.83 ns |  2.707 ns | 1.790 ns |  5.43 |    0.02 | 0.0038 |     392 B |        1.75 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           | 105.87 ns |  4.139 ns | 0.227 ns |  1.00 |    0.00 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 432.24 ns | 28.975 ns | 1.588 ns |  4.08 |    0.02 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 515.15 ns | 20.143 ns | 1.104 ns |  4.87 |    0.01 | 0.0057 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 422.26 ns |  9.295 ns | 0.510 ns |  3.99 |    0.01 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 195.33 ns | 16.188 ns | 0.887 ns |  1.85 |    0.01 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 125.98 ns |  5.364 ns | 0.294 ns |  1.19 |    0.00 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 249.43 ns | 15.864 ns | 0.870 ns |  2.36 |    0.01 | 0.0043 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 182.23 ns | 36.375 ns | 1.994 ns |  1.72 |    0.02 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  59.49 ns |  2.793 ns | 0.153 ns |  0.56 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  72.52 ns |  1.220 ns | 0.067 ns |  0.68 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 117.63 ns |  5.676 ns | 0.311 ns |  1.11 |    0.00 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 571.97 ns | 15.998 ns | 0.877 ns |  5.40 |    0.01 | 0.0038 |     392 B |        1.75 |
