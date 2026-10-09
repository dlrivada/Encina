```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  54.57 ns |   1.809 ns |  1.076 ns |  1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 237.10 ns |   6.278 ns |  3.736 ns |  4.35 |    0.10 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 298.28 ns |   7.069 ns |  4.675 ns |  5.47 |    0.13 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 227.65 ns |  16.223 ns | 10.730 ns |  4.17 |    0.20 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     |  99.71 ns |   1.629 ns |  0.969 ns |  1.83 |    0.04 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  57.90 ns |   1.500 ns |  0.992 ns |  1.06 |    0.03 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 120.60 ns |   3.133 ns |  2.072 ns |  2.21 |    0.05 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     |  99.66 ns |   4.618 ns |  3.054 ns |  1.83 |    0.06 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  38.78 ns |   1.848 ns |  1.222 ns |  0.71 |    0.03 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  38.79 ns |   1.210 ns |  0.720 ns |  0.71 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  58.37 ns |   3.027 ns |  2.002 ns |  1.07 |    0.04 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 345.53 ns |   9.032 ns |  5.375 ns |  6.33 |    0.15 | 0.0234 |     392 B |        1.75 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  55.79 ns |  10.119 ns |  0.555 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 242.40 ns | 117.243 ns |  6.426 ns |  4.35 |    0.11 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 284.41 ns |  21.348 ns |  1.170 ns |  5.10 |    0.05 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 237.01 ns | 152.257 ns |  8.346 ns |  4.25 |    0.13 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 101.80 ns |  31.355 ns |  1.719 ns |  1.82 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  61.04 ns |   4.488 ns |  0.246 ns |  1.09 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 135.20 ns | 100.490 ns |  5.508 ns |  2.42 |    0.09 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           |  98.15 ns |  13.480 ns |  0.739 ns |  1.76 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  39.93 ns |   1.953 ns |  0.107 ns |  0.72 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  40.89 ns |  26.931 ns |  1.476 ns |  0.73 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  64.29 ns |  30.094 ns |  1.650 ns |  1.15 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 353.36 ns |   9.786 ns |  0.536 ns |  6.33 |    0.06 | 0.0234 |     392 B |        1.75 |
