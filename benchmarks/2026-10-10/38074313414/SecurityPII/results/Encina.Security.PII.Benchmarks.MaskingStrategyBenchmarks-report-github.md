```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  53.34 ns |  1.588 ns |  0.945 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 233.10 ns | 14.615 ns |  9.667 ns |  4.37 |    0.19 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 275.34 ns | 12.572 ns |  7.482 ns |  5.16 |    0.16 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 236.21 ns | 16.521 ns | 10.928 ns |  4.43 |    0.21 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     |  94.61 ns |  3.058 ns |  2.023 ns |  1.77 |    0.05 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  58.82 ns |  1.900 ns |  1.257 ns |  1.10 |    0.03 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 119.24 ns |  4.868 ns |  3.220 ns |  2.24 |    0.07 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     |  95.79 ns |  2.478 ns |  1.475 ns |  1.80 |    0.04 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  37.55 ns |  1.291 ns |  0.768 ns |  0.70 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  37.19 ns |  1.208 ns |  0.719 ns |  0.70 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  54.59 ns |  1.797 ns |  1.070 ns |  1.02 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 332.14 ns |  8.963 ns |  5.334 ns |  6.23 |    0.14 | 0.0234 |     392 B |        1.75 |
|                     |            |                |             |           |           |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  51.46 ns |  5.045 ns |  0.277 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 232.65 ns | 30.420 ns |  1.667 ns |  4.52 |    0.04 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 263.71 ns | 29.419 ns |  1.613 ns |  5.12 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 207.04 ns | 27.222 ns |  1.492 ns |  4.02 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           |  95.53 ns | 36.706 ns |  2.012 ns |  1.86 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  55.20 ns |  4.087 ns |  0.224 ns |  1.07 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 118.12 ns | 52.855 ns |  2.897 ns |  2.30 |    0.05 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           |  92.12 ns | 13.761 ns |  0.754 ns |  1.79 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  35.73 ns | 14.665 ns |  0.804 ns |  0.69 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  38.32 ns |  7.867 ns |  0.431 ns |  0.74 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  52.76 ns |  2.989 ns |  0.164 ns |  1.03 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 337.93 ns | 52.468 ns |  2.876 ns |  6.57 |    0.06 | 0.0234 |     392 B |        1.75 |
