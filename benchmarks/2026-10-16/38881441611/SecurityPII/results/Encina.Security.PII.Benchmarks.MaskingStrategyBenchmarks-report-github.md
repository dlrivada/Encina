```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  98.77 ns |   2.534 ns |  1.676 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 420.21 ns |   5.954 ns |  3.938 ns |  4.26 |    0.08 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 503.98 ns |  13.580 ns |  8.982 ns |  5.10 |    0.12 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 410.45 ns |   3.017 ns |  1.795 ns |  4.16 |    0.07 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 175.07 ns |   0.872 ns |  0.577 ns |  1.77 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 110.26 ns |   0.500 ns |  0.331 ns |  1.12 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 236.04 ns |   2.859 ns |  1.701 ns |  2.39 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 199.44 ns |   2.552 ns |  1.688 ns |  2.02 |    0.04 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  66.03 ns |   0.176 ns |  0.105 ns |  0.67 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  71.10 ns |   0.152 ns |  0.080 ns |  0.72 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 107.37 ns |   0.459 ns |  0.273 ns |  1.09 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 631.21 ns |   2.530 ns |  1.673 ns |  6.39 |    0.11 | 0.0229 |     392 B |        1.75 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  97.11 ns |  29.932 ns |  1.641 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 421.39 ns | 127.491 ns |  6.988 ns |  4.34 |    0.09 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 532.16 ns | 239.267 ns | 13.115 ns |  5.48 |    0.14 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 428.05 ns | 140.268 ns |  7.689 ns |  4.41 |    0.09 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 174.64 ns |   2.106 ns |  0.115 ns |  1.80 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 110.83 ns |  16.129 ns |  0.884 ns |  1.14 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 232.14 ns |  56.833 ns |  3.115 ns |  2.39 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 178.92 ns |  30.543 ns |  1.674 ns |  1.84 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  63.69 ns |   5.840 ns |  0.320 ns |  0.66 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  74.07 ns |  34.044 ns |  1.866 ns |  0.76 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 109.57 ns |  10.770 ns |  0.590 ns |  1.13 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 626.81 ns |  17.595 ns |  0.964 ns |  6.46 |    0.09 | 0.0229 |     392 B |        1.75 |
