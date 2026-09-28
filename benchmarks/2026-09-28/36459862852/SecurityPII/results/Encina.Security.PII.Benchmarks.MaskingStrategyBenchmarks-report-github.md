```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  90.78 ns |   2.015 ns |  1.333 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 431.15 ns |   3.812 ns |  2.268 ns |  4.75 |    0.07 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 515.08 ns |   7.729 ns |  4.599 ns |  5.68 |    0.09 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 412.19 ns |   3.892 ns |  2.316 ns |  4.54 |    0.07 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 167.63 ns |   0.945 ns |  0.625 ns |  1.85 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 101.63 ns |   0.654 ns |  0.389 ns |  1.12 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 215.70 ns |   1.805 ns |  1.194 ns |  2.38 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 169.49 ns |   3.848 ns |  2.545 ns |  1.87 |    0.04 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  56.05 ns |   0.168 ns |  0.088 ns |  0.62 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  64.47 ns |   1.463 ns |  0.968 ns |  0.71 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  96.24 ns |   1.921 ns |  1.270 ns |  1.06 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 537.62 ns |   3.674 ns |  2.186 ns |  5.92 |    0.09 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  89.29 ns |  11.193 ns |  0.614 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 430.91 ns | 247.175 ns | 13.548 ns |  4.83 |    0.13 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 500.00 ns |  16.784 ns |  0.920 ns |  5.60 |    0.03 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 419.06 ns |  64.659 ns |  3.544 ns |  4.69 |    0.04 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 167.75 ns |  14.208 ns |  0.779 ns |  1.88 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 101.90 ns |   8.722 ns |  0.478 ns |  1.14 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 221.63 ns |  24.017 ns |  1.316 ns |  2.48 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 166.97 ns |   7.919 ns |  0.434 ns |  1.87 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  54.53 ns |   2.893 ns |  0.159 ns |  0.61 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  64.47 ns |   5.525 ns |  0.303 ns |  0.72 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  96.01 ns |   2.782 ns |  0.152 ns |  1.08 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 529.94 ns |  25.434 ns |  1.394 ns |  5.94 |    0.04 | 0.0248 |     416 B |        1.86 |
